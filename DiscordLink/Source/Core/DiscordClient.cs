using DSharpPlus;
using DSharpPlus.Clients;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.InteractionNamingPolicies;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Extensions;
using DSharpPlus.Net;
using DSharpPlus.Net.Gateway;
using Eco.Core.Utils;
using Eco.Moose.Tools.Logger;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Logging;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class DiscordClient
    {
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected
        }

        public enum ConnectionError
        {
            None,
            InvalidToken,
            InvalidGuild,
            CreateClientFailed,
            DiscordConnectionFailed,
            GuildConnectionFailed,
            ConnectionAbortedMissingIntents,
            ConnectionAborted,
        }

        public DateTime LastConnectionTime { get; private set; } = DateTime.MinValue;
        public bool IsConnected => ConnectionStatus == ConnectionState.Connected;
        public ConnectionState ConnectionStatus { get; private set; } = ConnectionState.Disconnected;
        public ConnectionError LastConnectionError { get; private set; } = ConnectionError.None;

        public string BotName => BotMember?.DisplayName ?? "Unknown - Disconnected";
        public string DSharpVersion => DSharpClient?.VersionString ?? "Unknown - Disconnected";

        private DSharpPlus.DiscordClient DSharpClient { get; set; } = null;
        private DiscordGuild Guild { get; set; } = null;
        private DiscordMember BotMember { get; set; } = null;

        public string Status
        {
            get { return _status; }
            private set
            {
                Logger.Debug($"Discord Client status changed from \"{_status}\" to \"{value}\"");
                _status = value;
            }
        }
        private string _status = "Uninitialized";

        private IServiceProvider _serviceProvider;

        #region Connection Handling

        public ThreadSafeAction OnConnecting = new ThreadSafeAction();
        public ThreadSafeAction OnConnected = new ThreadSafeAction();

        public ThreadSafeAction OnDisconnecting = new ThreadSafeAction();
        public ThreadSafeAction OnDisconnected = new ThreadSafeAction();

        public async Task Start()
        {
            Logger.Debug("Client Starting");
            LastConnectionError = ConnectionError.None;

            if (string.IsNullOrWhiteSpace(DiscordLinkConfig.BotToken))
            {
                Logger.Error($"Bot token not configured - {GITHUB_HELP_TEXT}");
                LastConnectionError = ConnectionError.InvalidToken;
                return; // Do not attempt to initialize if the bot token is empty
            }

            if (DiscordLinkConfig.DiscordServerId == 0)
            {
                Logger.Error($"Discord Server not configured - {GITHUB_HELP_TEXT}");
                LastConnectionError = ConnectionError.InvalidGuild;
                return; // Do not attempt to initialize if the server name/id is empty
            }

            if (!await CreateAndConnectClient())
                return;

            // Connection process continues when GuildDownloadCompleted is invoked.
        }

        private async Task<bool> CreateAndConnectClient()
        {
            Status = "Creating Discord Client";
            ConnectionStatus = ConnectionState.Connecting;

            try
            {
                DiscordIntents intents = DLConstants.REQUESTED_INTENTS.Aggregate((current, next) => current | next);
                IServiceCollection services = new ServiceCollection();
                services.AddDiscordClient(DiscordLinkConfig.BotToken, intents);
                services.AddOrReplace<IGatewayController, ReconnectingGatewayController>(ServiceLifetime.Singleton);
                services.Configure<RestClientOptions>(options => { });
                services.Configure<ShardingOptions>(options => { });
                services.Configure<DiscordConfiguration>(options => { });
                services.Configure<GatewayClientOptions>(options =>
                {
                    options.Intents = intents;
                });
                services.ConfigureEventHandlers
                (
                    builder => builder.HandleGuildDownloadCompleted(HandleGuildDownloadCompleted)
                    .HandleSocketClosed(HandleSocketClosed)
                    .HandleMessageCreated(HandleDiscordMessageCreated)
                    .HandleMessageUpdated(HandleDiscordMessageUpdated)
                    .HandleMessageDeleted(HandleDiscordMessageDeleted)
                    .HandleMessageReactionAdded(HandleDiscordReactionAdded)
                    .HandleMessageReactionRemoved(HandleDiscordReactionRemoved)
                    .HandleGuildMemberRemoved(HandleMemberRemoved)
                    .HandleGuildMemberUpdated(HandleMemberUpdated)
                );
                services.AddCommandsExtension
                (
                    (provider, extension) =>
                    {
                        extension.AddCommands([typeof(DiscordCommands)]);
                        SlashCommandProcessor commandProcessor = new SlashCommandProcessor(
                            new SlashCommandConfiguration
                            {
                                NamingPolicy = new LowercaseNamingPolicy(),
                            });
                        extension.AddProcessor(commandProcessor);
                    },
                    new CommandsConfiguration() { }
                );
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddDiscordLinkMicrosoftLogger();
                    builder.SetMinimumLevel(DiscordLinkConfig.BackendLogLevel);
                });

                _serviceProvider = services.BuildServiceProvider();
                DSharpClient = _serviceProvider.GetRequiredService<DSharpPlus.DiscordClient>();
            }
            catch (Exception e)
            {
                Cleanup();
                ConnectionStatus = ConnectionState.Disconnected;
                LastConnectionError = ConnectionError.CreateClientFailed;
                Status = "Failed to create Discord Client";
                Logger.Exception($"Error occurred while creating the Discord client", e);
                return false;
            }

            // Connect client
            Status = "Connecting to Discord...";
            OnConnecting.Invoke();
            try
            {
                await DSharpClient.ConnectAsync(new DiscordActivity(MessageBuilder.Discord.GetActivityString(), DiscordActivityType.Watching));
            }
            catch (Exception e)
            {
                if (e is UnauthorizedException || e.InnerException is UnauthorizedException)
                {
                    Logger.Error($"An authentication error occurred while connecting to Discord - Please verify that your token is valid - {GITHUB_HELP_TEXT}");
                }
                else
                {
                    Logger.Exception($"An error occurred while connecting to Discord", e);
                }

                Cleanup();
                ConnectionStatus = ConnectionState.Disconnected;
                LastConnectionError = ConnectionError.DiscordConnectionFailed;
                Status = "Discord connection failed";

                return false;
            }

            return true;
        }

        private async Task HandleGuildDownloadCompleted(DSharpPlus.DiscordClient client, GuildDownloadCompletedEventArgs args)
        {
            Status = "Resolving Discord server...";

            Guild = DSharpClient.Guilds.Values.FirstOrDefault(guild => guild.Id == DiscordLinkConfig.DiscordServerId);
            if (Guild == null)
            {
                Cleanup();
                ConnectionStatus = ConnectionState.Disconnected;
                LastConnectionError = ConnectionError.GuildConnectionFailed;
                Status = "Failed to find configured Discord server";
                Logger.Error($"Failed to find Discord server \"{DiscordLinkConfig.DiscordServerId}\" - Make sure the Bot is invited to your Server and the Server ID is correct - {GITHUB_HELP_TEXT}");
                return;
            }

            BotMember = Guild.CurrentMember;
            ConnectionStatus = ConnectionState.Connected;
            Status = "Connected to Discord";
            LastConnectionTime = DateTime.Now;

            OnConnected?.Invoke();
        }

        private void Cleanup()
        {
            DSharpClient = null;
            Guild = null;
            BotMember = null;
        }

        public async Task<bool> Stop()
        {
            // Disconnect
            Status = "Disconnecting from Discord";
            OnDisconnecting?.Invoke();
            try
            {
                await DSharpClient.DisconnectAsync();
                DSharpClient.Dispose();
            }
            catch (Exception e)
            {
                Logger.Exception($"An error occurred when disconnecting from Discord", e);
                Status = "Discord disconnection failed";
                return false;
            }

            Cleanup();
            ConnectionStatus = ConnectionState.Disconnected;
            Status = "Disconnected from Discord";

            OnDisconnected?.Invoke();
            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordClientDisconnected);
            return true;
        }

        public async Task<bool> Restart()
        {
            Status = "Restarting...";

            if (ConnectionStatus == ConnectionState.Connected || ConnectionStatus == ConnectionState.Connecting)
                await Stop();

            if (ConnectionStatus == ConnectionState.Disconnected)
                await Start();

            return ConnectionStatus == ConnectionState.Connected;
        }

        #endregion

        #region Event Handlers

        private async Task HandleDiscordMessageCreated(DSharpPlus.DiscordClient client, MessageCreatedEventArgs args)
        {
            if (IsUserDiscordLinkBot(args.Author))
                return; // Ignore messages sent by our own bot

            Logger.Trace($"Discord message {args.Message.GetLogId()} received\n{args.Message.GetLogData()}");
            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordMessageSent, args.Message);
        }

        private async Task HandleDiscordMessageUpdated(DSharpPlus.DiscordClient client, MessageUpdatedEventArgs args)
        {
            if (IsUserDiscordLinkBot(args.Author))
                return; // Ignore messages edited by our own bot

            Logger.Trace($"Discord message {args.Message.GetLogId()} edit received\n--- Before ---\n{args.MessageBefore.GetLogData()}\n--- After ---\n{args.Message.GetLogData()}");
            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordMessageEdited, args.Message, args.MessageBefore);
        }

        private async Task HandleDiscordMessageDeleted(DSharpPlus.DiscordClient client, MessageDeletedEventArgs args)
        {
            Logger.Trace($"Discord message {args.Message.GetLogId()} deleted\n{args.Message.GetLogData()}");
            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordMessageDeleted, args.Message);
        }

        private async Task HandleDiscordReactionAdded(DSharpPlus.DiscordClient client, MessageReactionAddedEventArgs args)
        {
            if (IsUserDiscordLinkBot(args.User))
                return; // Ignore reactions sent by our own bot

            Logger.Trace($"Discord reaction added\nReaction: {args.Emoji.GetLogName()}\nMesage ->\n{args.Message.GetLogData()}");
            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordReactionAdded, args.User, args.Message, args.Emoji);
        }

        private async Task HandleDiscordReactionRemoved(DSharpPlus.DiscordClient client, MessageReactionRemovedEventArgs args)
        {
            if (IsUserDiscordLinkBot(args.User))
                return; // Ignore reactions sent by our own bot

            Logger.Trace($"Discord reaction removed\nReaction: {args.Emoji.GetLogName()}\nMessage ->\n{args.Message.GetLogData()}");
            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordReactionRemoved, args.User, args.Message, args.Emoji);
        }

        private async Task HandleMemberRemoved(DSharpPlus.DiscordClient client, GuildMemberRemovedEventArgs args)
        {
            Logger.Trace($"Discord member removed\nUser: {args.Member.GetLogName()}");
            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordMemberRemoved, args.Member);
        }

        private async Task HandleMemberUpdated(DSharpPlus.DiscordClient client, GuildMemberUpdatedEventArgs args)
        {
            IEnumerable<DiscordRole> revokedRoles = args.RolesBefore.Except(args.RolesAfter);
            IEnumerable<DiscordRole> grantedRoles = args.RolesAfter.Except(args.RolesBefore);

            Logger.Trace($"Discord member update received\nMember: {args.Member.GetLogName()}\nGranted roles: {string.Join(", ", grantedRoles)}\nRevoked roles:{string.Join(", ", revokedRoles)}");

            if (revokedRoles.Count() > 0)
            {
                await DiscordLink.Obj.HandleEvent(DlEventType.DiscordRolesRevoked, args.Member, revokedRoles);
            }
            if (grantedRoles.Count() > 0)
            {
                await DiscordLink.Obj.HandleEvent(DlEventType.DiscordRolesGranted, args.Member, grantedRoles);
            }

            UserLinkManager.UpdateMemberCache(args.MemberAfter);
        }

        private async Task HandleSocketClosed(DSharpPlus.DiscordClient client, SocketClosedEventArgs args)
        {
            if (ConnectionStatus == ConnectionState.Connecting)
            {
                if (args.CloseCode == 4014) // Application does not have the requested privileged intents
                {
                    Logger.Error("Bot application is not configured to have the required intents. See install instructions for help with adding intents.");
                    LastConnectionError = ConnectionError.ConnectionAbortedMissingIntents;
                }
                else
                {
                    LastConnectionError = ConnectionError.ConnectionAborted;
                }

                ConnectionStatus = ConnectionState.Disconnected;
            }
        }

        #endregion

        #region Information Fetching

        public IEnumerable<DiscordChannel> GetChannelsOfType(params DiscordChannelType[] channelTypes)
        {
            return Guild.Channels.Values.Where(channel => channelTypes.Any(type => type == channel.Type));
        }

        public async Task<IEnumerable<DiscordChannel>> FetchChannels(params DiscordChannelType[] channelTypes)
        {
            Logger.Trace("Fetching channel list");
            IReadOnlyList<DiscordChannel> channels = await Guild.GetChannelsAsync();
            return channels.Where(channel => channelTypes.Any(type => type == channel.Type));
        }

        public DiscordChannel GetChannelByNameOrId(string channelNameOrId)
        {
            return channelNameOrId.TryParseSnowflakeId(out ulong channelId)
                ? GetChannelById(channelId)
                : GetChannelByName(channelNameOrId);
        }

        public DiscordChannel GetChannelById(ulong channelId)
        {
            return Guild.Channels.Values.FirstOrDefault(channel => channel.Id == channelId, Guild.Threads.Values.FirstOrDefault(channel => channel.Id == channelId));
        }

        public DiscordChannel GetChannelByName(string channelName)
        {
            return Guild.Channels.Values.FirstOrDefault(guild => guild.Name.EqualsCaseInsensitive(channelName));
        }

        public IEnumerable<DiscordMember> GetMembers()
        {
            return Guild.Members.Values;
        }

        public DiscordMember GetMemberById(ulong memberId)
        {
            Guild.Members.TryGetValue(memberId, out DiscordMember member);
            return member;
        }

        public async Task<DiscordMember> FetchMemberAsync(string memberIdStr, bool updateCache = false, bool expectNotFound = false)
        {
            if (!memberIdStr.TryParseSnowflakeId(out ulong memberId))
                return null;

            return await FetchMemberAsync(memberId, updateCache, expectNotFound);
        }

        public async Task<DiscordMember> FetchMemberAsync(ulong memberId, bool updateCache = false, bool expectNotFound = false)
        {
            DiscordMember member = null;
            try
            {
                Logger.Trace($"Fetching member with ID \"{memberId}\"");
                member = await Guild.GetMemberAsync(memberId, updateCache);
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while attempting to fetch member with ID \"{memberId}\"", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while attempting to fetch member with ID \"{memberId}\"", e);
            }
            catch (NotFoundException e)
            {
                if (!expectNotFound)
                    Logger.Exception($"NotFoundException occurred while attempting to fetch member with ID \"{memberId}\"", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Error occurred while attempting to fetch member with ID \"{memberId}\"", e);
            }
            return member;
        }

        public async Task<DiscordMessage> FetchMessageAsync(DiscordChannel channel, ulong messageId, bool expectNotFound = false)
        {
            if (!ChannelHasPermission(channel, DiscordPermission.ReadMessageHistory))
            {
                Logger.Error($"Failed to fetch specific message from channel {channel.GetLogName()} as the bot lacks permission for reading message history");
                return null;
            }

            DiscordMessage message = null;
            try
            {
                Logger.Trace($"Fetching message with ID \"{messageId}\" from channel {channel.GetLogName()}");
                message = await channel.GetMessageAsync(messageId);
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while attempting to fetch message with ID {messageId} from channel \"{channel.Name}\"", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while attempting to fetch message with ID {messageId} from channel \"{channel.Name}\"", e);
            }
            catch (NotFoundException e)
            {
                if (!expectNotFound)
                    Logger.Exception($"ServerErrorException occurred while attempting to fetch message with ID {messageId} from channel \"{channel.Name}\"", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Error occurred while attempting to fetch message with ID {messageId} from channel \"{channel.Name}\"", e);
            }
            return message;
        }

        public async Task<IReadOnlyList<DiscordMessage>> FetchMessagesAsync(DiscordChannel channel)
        {
            if(channel == null)
            {
                Logger.DebugWarning("Attempted to fetch message from null channel");
                return null;
            }

            if (!ChannelHasPermission(channel, DiscordPermission.ReadMessageHistory))
            {
                Logger.Error($"Failed to fetch messages from channel {channel.GetLogName()} as the bot lacks permission for reading message history");
                return null;
            }

            IReadOnlyList<DiscordMessage> messages = null;
            try
            {
                Logger.Trace($"Fetching recent messages from channel {channel.GetLogName()}");
                messages = await channel.GetMessagesAsync().ToListAsync();
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while fetching messages from channel \"{channel.GetLogName()}", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while fetching messages from channel \"{channel.GetLogName()}", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Error occurred when attempting to read message history from channel \"{channel.GetLogName()}\"", e);
            }
            return messages;
        }

        public async Task<IReadOnlyList<DiscordMember>> FetchMembersAsync()
        {
            IReadOnlyList<DiscordMember> members = new List<DiscordMember>();

            if (!BotHasIntent(DiscordIntents.GuildMembers))
            {
                Logger.Error("Attempted to get full guild member list but the bot does not have the privileged GuildMembers intent");
                return members;
            }

            try
            {
                Logger.Trace("Fetching guild member list");
                members = await Guild.GetAllMembersAsync().ToListAsync();
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while fetching all guild members", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while fetching all guild members", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Error occured when attempting to fetch all guild members", e);
            }
            return members;
        }

        public DiscordRole GetRoleById(ulong roleId)
        {
            return Guild.GetRoleById(roleId);
        }

        public DiscordRole GetRoleByName(string roleName)
        {
            return Guild.GetRoleByName(roleName);
        }

        public DiscordEmoji GetEmojiByName(string emojiName)
        {
            return DiscordEmoji.FromName(DSharpClient, emojiName);
        }

        #endregion

        #region Permission Management

        public bool BotHasPermission(DiscordPermissions permission)
        {
            if (BotMember == null)
            {
                Logger.Error($"BotMember was null when evaluating bot permissions");
                return false;
            }

            bool hasPermission = false;
            foreach (DiscordRole role in BotMember.Roles)
            {
                if (role.CheckPermission(permission) == DiscordPermissionLevel.Allowed)
                {
                    hasPermission = true;
                    break;
                }
            }

            return hasPermission;
        }

        public bool ChannelHasPermission(DiscordChannel channel, DiscordPermission permission)
        {
            if (BotMember == null)
            {
                Logger.Error($"BotMember was null when evaluating channel permissions for channel {channel.GetLogName()}");
                return false;
            }

            if (channel.IsPrivate)
                return true; // Assume permission is given for DMs

            return channel.PermissionsFor(BotMember).HasPermission(permission);
        }

        public bool BotHasIntent(DiscordIntents intent)
        {
            return (DSharpClient.Intents & intent) != 0;
        }

        public IEnumerable<DiscordPermissions> FindMissingGuildPermissions()
        {
            List<DiscordPermissions> missingPermissions = new List<DiscordPermissions>();
            foreach (DiscordPermissions permission in DLConstants.REQUESTED_GUILD_PERMISSIONS)
            {
                if (!BotHasPermission(permission))
                    missingPermissions.Add(permission);
            }
            return missingPermissions;
        }

        public IEnumerable<DiscordPermissions> FindMissingChannelPermissions(DiscordChannel channel)
        {
            List<DiscordPermissions> missingPermissions = new List<DiscordPermissions>();
            foreach (DiscordPermission permission in DLConstants.REQUESTED_CHANNEL_PERMISSIONS)
            {
                if (!ChannelHasPermission(channel, permission))
                    missingPermissions.Add(permission);
            }
            return missingPermissions;
        }

        public IEnumerable<DiscordIntents> FindMissingIntents()
        {
            List<DiscordIntents> missingIntents = new List<DiscordIntents>();
            foreach (DiscordIntents intent in DLConstants.REQUESTED_INTENTS)
            {
                if (!BotHasIntent(intent))
                    missingIntents.Add(intent);
            }
            return missingIntents;
        }

        #endregion

        #region Bot Management

        public async Task ReinstallCommands()
        {
            await DSharpClient.BulkOverwriteGuildApplicationCommandsAsync(Guild.Id, new List<DiscordApplicationCommand>());
        }

        #endregion

        #region Manipulation

        public SendReadyMessage FormatMessageForSending(DiscordChannel channel, string textContent, DiscordLinkEmbed embedContent = null)
        {
            if (channel == null)
            {
                Logger.Error("Attempted to format message for sending to null channel");
                return null;
            }

            // Either make sure we have permission to use embeds or convert the embed to text
            string fullTextContent = (embedContent == null || ChannelHasPermission(channel, DiscordPermission.EmbedLinks)) ? textContent : $"{textContent}\n{embedContent.AsDiscordText()}";

            // If needed; split the message into multiple parts
            ICollection<string> stringParts = MessageUtils.SplitStringBySize(fullTextContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
            ICollection<DiscordEmbed> embedParts = MessageUtils.BuildDiscordEmbeds(embedContent);

            return new SendReadyMessage(stringParts, embedParts);
        }

        public async Task<IEnumerable<DiscordMessage>> SendMessageAsync(DiscordChannel channel, string textContent, DiscordLinkEmbed embedContent = null)
        {
            if (channel == null)
            {
                Logger.Error("Attempted to send message to null channel");
                return null;
            }

            SendReadyMessage messageData = FormatMessageForSending(channel, textContent, embedContent);
            return await SendMessageAsync(channel, messageData);
        }

        public async Task<IEnumerable<DiscordMessage>> SendMessageAsync(DiscordChannel channel, SendReadyMessage messageData)
        {
            if (channel == null)
            {
                Logger.Error("Attempted to send message to null channel");
                return null;
            }

            if (!ChannelHasPermission(channel, DiscordPermission.SendMessages))
            {
                Logger.Warning($"Attempted to send message to channel `{channel}` but the bot user is lacking permissions for this action");
                return null;
            }

            List<DiscordMessage> createdMessages = new List<DiscordMessage>();
            try
            {
                Logger.Trace($"Sending message to channel {channel.GetLogName()} containing {messageData.StringParts.Count} raw string parts and {messageData.EmbedParts.Count} embed parts");
                if (messageData.StringParts.Count <= 1 && messageData.EmbedParts.Count == 1)
                {
                    createdMessages.Add(await channel.SendMessageAsync(messageData.StringParts.FirstOrDefault(), messageData.EmbedParts.First()));
                }
                else
                {
                    foreach (string textMessagePart in messageData.StringParts)
                    {
                        createdMessages.Add(await channel.SendMessageAsync(textMessagePart));
                    }
                    foreach (DiscordEmbed embedPart in messageData.EmbedParts)
                    {
                        createdMessages.Add(await channel.SendMessageAsync(embedPart));
                    }
                }
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while sending message to channel {channel.GetLogName()}", e);
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while sending message to channel {channel.GetLogName()}", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send message to channel {channel.GetLogName()}", e);
            }
            return createdMessages;
        }

        public async Task<IEnumerable<DiscordMessage>> SendDmAsync(DiscordMember recipientMember, string textContent, DiscordLinkEmbed embedContent = null)
        {
            if (recipientMember == null)
            {
                Logger.Error("Attempted to send DM to null user");
                return null;
            }

            DiscordChannel DmChannel = await recipientMember.CreateDmChannelAsync();
            if (DmChannel == null)
            {
                Logger.Error($"Failed to create DM channel for sending message to user {recipientMember.GetLogName()}");
                return null;
            }

            Logger.Trace($"Sending DM to user {recipientMember.GetLogName()}");
            IEnumerable<DiscordMessage> createdMessages = await SendMessageAsync(DmChannel, textContent, embedContent);

            return createdMessages;
        }

        public async Task<IEnumerable<DiscordMessage>> ModifyMessageAsync(DiscordMessage message, string textContent, DiscordLinkEmbed embedContent = null)
        {
            if (message == null)
            {
                Logger.Error("Attempted to modify null message");
                return null;
            }

            SendReadyMessage messageData = FormatMessageForSending(message.Channel, textContent, embedContent);
            return await ModifyMessageAsync(message, messageData);
        }

        public async Task<IEnumerable<DiscordMessage>> ModifyMessageAsync(DiscordMessage message, SendReadyMessage newMessageData)
        {
            if (message == null)
            {
                Logger.Error("Attempted to modify null message");
                return null;
            }

            List<DiscordMessage> createdMessages = new List<DiscordMessage>();
            try
            {
                DiscordChannel channel = message.GetChannel();
                if (!ChannelHasPermission(channel, DiscordPermission.ManageMessages))
                {
                    Logger.Error($"Attempted to modify message {message.GetLogId()} in channel {channel.GetLogName()} but the bot user is lacking permissions for this action");
                    return null;
                }

                if (newMessageData.StringParts.Count <= 1 && newMessageData.EmbedParts.Count == 1)
                {
                    Logger.Trace($"Editing embed message {message.GetLogId()} in channel {message.Channel.GetLogName()}");
                    await message.ModifyAsync(newMessageData.StringParts.FirstOrDefault(), newMessageData.EmbedParts.First());
                }
                else
                {
                    bool messageEdited = false;
                    foreach (string stringPart in newMessageData.StringParts)
                    {
                        if (!messageEdited)
                        {
                            Logger.Trace($"Editing text message {message.GetLogId()} in channel {message.Channel.GetLogName()}");
                            await message.ModifyAsync(stringPart);
                            messageEdited = true;
                        }
                        else
                        {
                            Logger.Trace($"Sending text edit overflow message to channel \"{message.Channel.GetLogName()}");
                            createdMessages.AddRange(await SendMessageAsync(message.Channel, new SendReadyMessage(stringPart)));
                        }
                    }

                    foreach (DiscordEmbed embedPart in newMessageData.EmbedParts)
                    {
                        if (!messageEdited)
                        {
                            Logger.Trace($"Editing embed message {message.GetLogId()} in channel {message.Channel.GetLogName()}");
                            await message.ModifyAsync(embedPart);
                            messageEdited = true;
                        }
                        else
                        {
                            Logger.Trace($"Sending embed edit overflow message to channel {message.Channel.GetLogName()}");
                            createdMessages.AddRange(await SendMessageAsync(message.Channel, new SendReadyMessage(embedPart)));
                        }
                    }
                }
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while modifying message in channel {message.Channel.GetLogName()}", e);
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while modifying message in channel {message.Channel.GetLogName()}", e);
            }
            catch (Exception e)
            {
                string channelName = message?.Channel?.Name;
                if (string.IsNullOrWhiteSpace(channelName))
                    channelName = "\"Unknown channel\"";
                else
                    channelName = $"\"{channelName}\" ({message.ChannelId})";
                Logger.Exception($"Failed to modify message in channel {channelName}", e);
            }
            return createdMessages;
        }

        public async Task<bool> DeleteMessageAsync(ulong channelId, ulong messageId, string? reason = null, bool suppressMissingMessageWarning = false)
        {
            DiscordChannel channel = await Guild.GetChannelAsync(channelId);
            if (channel == null)
            {
                Logger.Warning($"Attempted to delete message with ID {messageId} from non existent channel with ID {channelId}");
                return false;
            }

            return await DeleteMessageAsync(channel, messageId, reason, suppressMissingMessageWarning);
        }

        public async Task<bool> DeleteMessageAsync(DiscordChannel channel, ulong messageId, string? reason = null, bool expectNotFound = false)
        {
            DiscordMessage message = await FetchMessageAsync(channel, messageId, expectNotFound);
            if (message == null)
            {
                if (!expectNotFound)
                    Logger.Warning($"Attempted to delete non existent message with ID {messageId} from channel {channel.GetLogName()}");

                return false;
            }

            return await DeleteMessageAsync(message);
        }

        public async Task<bool> DeleteMessageAsync(DiscordMessage message, string? reason = null)
        {
            if (message == null)
            {
                Logger.Error("Attempted to delete null message");
                return false;
            }

            DiscordChannel channel = message.GetChannel();
            if (!ChannelHasPermission(channel, DiscordPermission.ManageMessages))
            {
                Logger.Warning($"Attempted to delete message in channel {channel.GetLogName()} but the bot user is lacking permissions for this action");
                return false;
            }

            bool result = false;
            try
            {
                Logger.Trace($"Deleting message {message.GetLogId()} from channel {channel.GetLogName()}");
                await message.DeleteAsync(reason ?? "Deleted by DiscordLink");
                result = true;
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while deleting message in channel {channel.GetLogName()}", e);
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while deleting message in channel {channel.GetLogName()}", e);
            }
            catch (Exception e)
            {
                string channelName = channel?.Name;
                if (string.IsNullOrWhiteSpace(channelName))
                    channelName = "\"Unknown channel\"";
                else
                    channelName = $"\"{channelName}\" ({message.ChannelId})";

                Logger.Exception($"Failed to delete message from channel {channelName}", e);
            }
            return result;
        }

        public async Task<DiscordDmChannel> GetOrCreateDmChannelAsync(DiscordMember member)
        {
            try
            {
                Logger.Trace($"Creating/Fetchng DM channel for member {member.GetLogName()}");
                return await member.CreateDmChannelAsync();
            }
            catch (NotFoundException e)
            {
                Logger.Warning($"Failed to get or create DM channel for member {member.GetLogName()} - Bot was unable to find member");
                return null;
            }
            catch (UnauthorizedException e)
            {
                Logger.DebugException($"Failed to get or create DM channel for member {member.GetLogName()} - Bot was refused access to member DM channel", e);
                return null;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to get or create DM channel for member {member.GetLogName()}", e);
                return null;
            }
        }

        public async Task<DiscordRole> CreateRoleAsync(DiscordLinkRole dlRole)
        {
            try
            {
                Logger.Trace($"Creating role {dlRole.GetLogName()}");
                DiscordRole role = await Guild.CreateRoleAsync(dlRole.Name, dlRole.Permissions, dlRole.Color, dlRole.Hoist, dlRole.Mentionable, dlRole.AddReason);
                if (role != null)
                {
                    DLStorage.PersistentData.RoleIds.Add(role.Id);
                    DLStorage.Instance.Write(); // Save immediately after creating so that we don't lose track of the roles in case of an ungraceful exit
                }
                else
                {
                    Logger.Error($"Failed to create role {dlRole.GetLogName()}.");
                }

                return role;
            }
            catch (UnauthorizedException e)
            {
                Logger.Exception($"DiscordLink was not allowed to create the role {dlRole.GetLogName()}. Ensure that your bot user is assigned a role with higher permission level than all roles it manages.", e);
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while creating role {dlRole.GetLogName()}", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while creating role {dlRole.GetLogName()}", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to create role {dlRole.GetLogName}", e);
            }
            return await Task.FromResult<DiscordRole>(null);
        }

        public async Task DeleteRoleAsync(ulong roleId)
        {
            DiscordRole role = GetRoleById(roleId);
            if (role == null)
            {
                Logger.Error($"Failed to delete role - No role with ID \"{roleId}\" could be found");
                return;
            }

            await DeleteRoleAsync(role);
        }

        public async Task DeleteRoleAsync(DiscordRole role)
        {
            if (role == null)
            {
                Logger.Error($"Failed to delete role - Role was null");
                return;
            }

            try
            {
                Logger.Trace($"Deleting role {role.GetLogName()}");
                await role.DeleteAsync("Deleted by DiscordLink");
            }
            catch (UnauthorizedException e)
            {
                Logger.Error($"DiscordLink was not allowed to delete the role {role.GetLogName()} - Ensure that your bot user is assigned a role with higher permission level than all roles it manages.");
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while deleting role {role.GetLogName()}", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while deleting role {role.GetLogName()}", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to delete role {role.GetLogName()}", e);
            }
        }

        public async Task GrantRoleAsync(DiscordMember member, DiscordLinkRole dlRole)
        {
            DiscordRole discordRole = Guild.GetRoleByName(dlRole.Name);
            if (discordRole == null)
                discordRole = await CreateRoleAsync(dlRole);

            if (discordRole != null)
                await GrantRoleAsync(member, discordRole);
        }

        public async Task GrantRoleAsync(DiscordMember member, DiscordRole role)
        {
            if (member == null || role == null)
                return;
            if (member.HasRole(role))
                return; // Member already has the role

            try
            {
                Logger.Trace($"Adding role {role.GetLogName()} to member {member.GetLogName()}");
                await member.GrantRoleAsync(role, "Added by DiscordLink");
            }
            catch (UnauthorizedException e)
            {
                Logger.Error($"DiscordLink was not allowed to grant the role {role.GetLogName()} to member {member.GetLogName()} - Ensure that your bot user is assigned a role with higher permission level than all roles it manages.");
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while adding role {role.GetLogName()} to member {member.GetLogName()}", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while adding role {role.GetLogName()} to member {member.GetLogName()}", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to grant role {role.GetLogName()} to member {member.GetLogName()}", e);
            }
        }

        public async Task RevokeRoleAsync(DiscordMember member, string roleName)
        {
            DiscordRole role = Guild.GetRoleByName(roleName);
            if (role == null)
            {
                Logger.Debug($"Attempting to remove nonexistent role \"{roleName}\" from user {member.GetLogName()}");
                return;
            }

            await RevokeRoleAsync(member, role);
        }

        public async Task RevokeRoleAsync(DiscordMember member, DiscordRole role)
        {
            if (member == null || role == null)
                return;

            if (!member.HasRole(role))
                return; // Member doesn't have the role

            try
            {
                Logger.Trace($"Removing role {role.GetLogName()} from member {member.GetLogName()}");
                await member.RevokeRoleAsync(role, "Removed by DiscordLink");
            }
            catch (UnauthorizedException e)
            {
                Logger.Error($"DiscordLink was not allowed to revoke the role {role.GetLogName()} from member {member.GetLogName()} - Ensure that your bot user is assigned a role with higher permission level than all roles it manages. This role was most likely not created by the current bot. Deleting it manually will resolve this Issue.");
            }
            catch (RateLimitException e)
            {
                Logger.DebugException($"RateLimitException occurred while removing role {role.GetLogName()} from member {member.GetLogName()}", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while removing role {role.GetLogName()} from member {member.GetLogName()}", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to revoke role {role.GetLogName()} from member {member.GetLogName()}", e);
            }
        }

        public async Task SetStatusStringAsync(string activityString, DiscordActivityType activityType)
        {
            Logger.Trace($"Updating bot status message to {Enum.GetNames(typeof(DiscordActivityType))[(int)activityType]} {activityString}");
            await DSharpClient.UpdateStatusAsync(new DiscordActivity(activityString, activityType));
        }

        public async Task RemoveStatusStringAsync()
        {
            Logger.Trace($"Removing bot status message");
            await DSharpClient.UpdateStatusAsync();
        }

        #endregion

        #region Utilities

        const string GITHUB_HELP_TEXT = "See Github page for install instructions => \"https://github.com/Eco-DiscordLink/EcoDiscordPlugin\"";

        public bool IsUserDiscordLinkBot(DiscordUser user)
        {
            return user == BotMember;
        }

        public bool MemberIsAdmin(DiscordMember member)
        {
            if (DiscordLinkConfig.DiscordServerOwnerIsAdmin && member.IsOwner)
                return true;

            foreach (string adminRole in DiscordLinkConfig.AdminRoles)
            {
                if (adminRole.TryParseSnowflakeId(out ulong adminRoleId) && member.Roles.Any(role => role.Id == adminRoleId))
                    return true;

                if (member.Roles.Any(role => role.Name.EqualsCaseInsensitive(adminRole)))
                    return true;
            }

            return false;
        }

        #endregion
    }
}
