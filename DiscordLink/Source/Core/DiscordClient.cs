using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Eco.Core.Utils;
using Eco.Moose.Tools.Logger;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
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

        public DSharpPlus.DiscordClient DSharpClient { get; private set; }
        public DateTime LastConnectionTime { get; private set; } = DateTime.MinValue;
        public ConnectionState ConnectionStatus { get; private set; } = ConnectionState.Disconnected;
        public ConnectionError LastConnectionError { get; private set; } = ConnectionError.None;
        public DiscordGuild Guild { get; private set; } = null;
        public DiscordMember BotMember { get; private set; } = null;

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
        private SlashCommandsExtension _commands = null;

        #region Connection Handling

        public ThreadSafeAction OnConnecting = new ThreadSafeAction();
        public ThreadSafeAction OnConnected = new ThreadSafeAction();

        public ThreadSafeAction OnDisconnecting = new ThreadSafeAction();
        public ThreadSafeAction OnDisconnected = new ThreadSafeAction();

        public async Task Start()
        {
            Logger.Debug("Client Starting");

            LastConnectionError = ConnectionError.None;

            if (string.IsNullOrWhiteSpace(DLConfig.Data.BotToken))
            {
                Logger.Error("Bot token not configured - See Github page for install instructions.");
                LastConnectionError = ConnectionError.InvalidToken;
                return; // Do not attempt to initialize if the bot token is empty
            }

            if (DLConfig.Data.DiscordServerId == 0)
            {
                Logger.Error("Discord Server not configured - See Github page for install instructions => \"https://github.com/Eco-DiscordLink/EcoDiscordPlugin\"");
                LastConnectionError = ConnectionError.InvalidGuild;
                return; // Do not attempt to initialize if the server name/id is empty
            }

            if (!await CreateAndConnectClient())
                return;

            await Task.Delay(DLConstants.POST_SERVER_CONNECTION_WAIT_MS);
            if (DSharpClient != null)
                DSharpClient.SocketClosed -= HandleSocketClosedOnConnection; // Stop waiting for aborted connections caused by faulty connection attempts

            if (ConnectionStatus == ConnectionState.Disconnected)
            {
                DSharpClient = null;
                _commands = null;
                Status = "Discord connection failed";
                return; // If the second connection attempt also fails we give up
            }

            // Connection process continues when GuildDownloadCompleted is invoked.
        }

        private async Task<bool> CreateAndConnectClient()
        {
            Status = "Creating Discord Client";
            ConnectionStatus = ConnectionState.Connecting;

            // Create client
            try
            {
                DSharpClient = new DSharpPlus.DiscordClient(new DiscordConfiguration
                {
                    AutoReconnect = true,
                    Token = DLConfig.Data.BotToken,
                    TokenType = TokenType.Bot,
                    MinimumLogLevel = DLConfig.Data.BackendLogLevel,
                    LoggerFactory = new DSharpPlusLogWrapperFactory(DLConfig.Data.BackendLogLevel, DLConfig.Data.TraceFileLogging),
                    Intents = DLConstants.REQUESTED_INTENTS.Aggregate((current, next) => current | next)
                });

                // Register Discord commands
                _commands = DSharpClient.UseSlashCommands(new SlashCommandsConfiguration());
                _commands.RegisterCommands<DiscordCommands>(DLConfig.Data.DiscordServerId);
            }
            catch (Exception e)
            {
                DSharpClient = null;
                _commands = null;
                ConnectionStatus = ConnectionState.Disconnected;
                LastConnectionError = ConnectionError.CreateClientFailed;
                Status = "Failed to create Discord Client";
                Logger.Exception($"Error occurred while creating the Discord client", e);
                return false;
            }

            DSharpClient.SocketClosed += HandleSocketClosedOnConnection;

            // Connect client
            Status = "Connecting to Discord...";
            OnConnecting.Invoke();
            try
            {
                await DSharpClient.ConnectAsync(new DiscordActivity(MessageBuilder.Discord.GetActivityString(), ActivityType.Watching));
            }
            catch (Exception e)
            {
                if (e.InnerException is UnauthorizedException)
                {
                    Logger.Error($"An authentication error occurred while connecting to Discord using token \"{DLConfig.Data.BotToken}\". Please verify that your token is valid. See Github page for install instructions.");
                }
                else
                {
                    Logger.Exception($"An error occurred while connecting to Discord", e);
                }

                DSharpClient = null;
                _commands = null;
                ConnectionStatus = ConnectionState.Disconnected;
                LastConnectionError = ConnectionError.DiscordConnectionFailed;
                Status = "Discord connection failed";

                return false;
            }

            DSharpClient.GuildDownloadCompleted += HandleGuildDownloadCompleted;

            return true;
        }

        private async Task HandleGuildDownloadCompleted(DSharpPlus.DiscordClient client, GuildDownloadCompletedEventArgs args)
        {
            Status = "Resolving Discord server...";
            Guild = DSharpClient.Guilds.Values.FirstOrDefault(guild => guild.Id == DLConfig.Data.DiscordServerId);

            if (Guild == null)
            {
                DSharpClient = null;
                _commands = null;
                ConnectionStatus = ConnectionState.Disconnected;
                LastConnectionError = ConnectionError.GuildConnectionFailed;
                Status = "Failed to find configured Discord server";
                Logger.Error($"Failed to find Discord server \"{DLConfig.Data.DiscordServerId}\". Make sure the Bot is invited to your Server and the Server ID is correct. See Github page for install instructions.");
                return;
            }

            BotMember = Guild.CurrentMember;
            ConnectionStatus = ConnectionState.Connected;
            Status = "Connected to Discord";
            LastConnectionTime = DateTime.Now;

            RegisterEventListeners();
            OnConnected?.Invoke();
        }

        public async Task<bool> Stop()
        {
            UnregisterEventListeners();

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

            DSharpClient = null;
            ConnectionStatus = ConnectionState.Disconnected;
            Status = "Disconnected from Discord";
            Guild = null;
            BotMember = null;

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

        private void RegisterEventListeners()
        {
            DSharpClient.ClientErrored += HandleClientError;
            DSharpClient.SocketErrored += HandleSocketError;
            DSharpClient.MessageCreated += HandleDiscordMessageCreated;
            DSharpClient.MessageUpdated += HandleDiscordMessageEdited;
            DSharpClient.MessageDeleted += HandleDiscordMessageDeleted;
            DSharpClient.MessageReactionAdded += HandleDiscordReactionAdded;
            DSharpClient.MessageReactionRemoved += HandleDiscordReactionRemoved;
            DSharpClient.GuildMemberRemoved += HandleMemberRemoved;
        }

        private void UnregisterEventListeners()
        {
            DSharpClient.ClientErrored -= HandleClientError;
            DSharpClient.SocketErrored -= HandleSocketError;
            DSharpClient.MessageCreated -= HandleDiscordMessageCreated;
            DSharpClient.MessageUpdated -= HandleDiscordMessageEdited;
            DSharpClient.MessageDeleted -= HandleDiscordMessageDeleted;
            DSharpClient.MessageReactionAdded -= HandleDiscordReactionAdded;
            DSharpClient.MessageReactionRemoved -= HandleDiscordReactionRemoved;
            DSharpClient.GuildMemberRemoved -= HandleMemberRemoved;
        }

        #endregion

        #region Event Handlers

        private async Task HandleDiscordMessageCreated(DSharpPlus.DiscordClient client, MessageCreateEventArgs args)
        {
            DiscordMessage message = args.Message;
            Logger.Trace($"Discord Message Received\n{message.FormatForLog()}");

            if (args.Author == DSharpClient.CurrentUser)
                return; // Ignore messages sent by our own bot

            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordMessageSent, message);
        }

        private async Task HandleDiscordMessageEdited(DSharpPlus.DiscordClient client, MessageUpdateEventArgs args)
        {
            if (args.Author == DSharpClient.CurrentUser)
                return; // Ignore messages edits made by our own bot

            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordMessageEdited, args.Message, args.MessageBefore);
        }

        private async Task HandleDiscordMessageDeleted(DSharpPlus.DiscordClient client, MessageDeleteEventArgs args)
        {
            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordMessageDeleted, args.Message);
        }

        private async Task HandleDiscordReactionAdded(DSharpPlus.DiscordClient client, MessageReactionAddEventArgs args)
        {
            if (args.User == client.CurrentUser)
                return; // Ignore reactions sent by our own bot

            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordReactionAdded, args.User, args.Message, args.Emoji);
        }

        private async Task HandleDiscordReactionRemoved(DSharpPlus.DiscordClient client, MessageReactionRemoveEventArgs args)
        {
            if (args.User == client.CurrentUser)
                return; // Ignore reactions sent by our own bot

            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordReactionRemoved, args.User, args.Message, args.Emoji);
        }

        private async Task HandleMemberRemoved(DSharpPlus.DiscordClient client, GuildMemberRemoveEventArgs args)
        {
            await DiscordLink.Obj.HandleEvent(DlEventType.DiscordMemberRemoved, args.Member);
        }

        private async Task HandleClientError(DSharpPlus.DiscordClient client, ClientErrorEventArgs args)
        {
            Logger.DebugException($"A Discord client error occurred. Event: \"{args.EventName}\"", args.Exception);
        }

        private async Task HandleSocketError(DSharpPlus.DiscordClient client, SocketErrorEventArgs args)
        {
            Logger.DebugException($"A socket error occurred", args.Exception);
        }

        private async Task HandleSocketClosedOnConnection(DSharpPlus.DiscordClient client, SocketCloseEventArgs args)
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

        #endregion

        #region Information Fetching

        public DiscordChannel ChannelByNameOrId(string channelNameOrId)
        {
            return channelNameOrId.TryParseSnowflakeId(out ulong channelId)
                ? Guild.Channels.Values.FirstOrDefault(channel => channel.Id == channelId)
                : Guild.Channels.Values.FirstOrDefault(guild => guild.Name.EqualsCaseInsensitive(channelNameOrId));
        }

        public bool ChannelHasPermission(DiscordChannel channel, Permissions permission)
        {
            if (BotMember == null)
            {
                Logger.Error($"BotMember was null when evaluating channel permissions for channel \"{channel.Name}\"");
                return false;
            }

            if (channel.IsPrivate)
                return true; // Assume permission is given for DMs

            return channel.PermissionsFor(BotMember).HasPermission(permission);
        }

        public bool BotHasPermission(Permissions permission)
        {
            if (BotMember == null)
            {
                Logger.Error($"BotMember was null when evaluating bot permissions");
                return false;
            }

            bool hasPermission = false;
            foreach (DiscordRole role in BotMember.Roles)
            {
                if (role.CheckPermission(permission) == PermissionLevel.Allowed)
                {
                    hasPermission = true;
                    break;
                }
            }
            return hasPermission;
        }

        public bool BotHasIntent(DiscordIntents intent)
        {
            return (DSharpClient.Intents & intent) != 0;
        }

        public bool MemberIsAdmin(DiscordMember member)
        {
            foreach (string adminRole in DLConfig.Data.AdminRoles)
            {
                if (adminRole.TryParseSnowflakeId(out ulong adminRoleId) && member.Roles.Any(role => role.Id == adminRoleId))
                    return true;

                if (member.Roles.Any(role => role.Name.EqualsCaseInsensitive(adminRole)))
                    return true;
            }

            return false;
        }

        public IEnumerable<Permissions> FindMissingGuildPermissions()
        {
            List<Permissions> missingPermissions = new List<Permissions>();
            foreach (Permissions permission in DLConstants.REQUESTED_GUILD_PERMISSIONS)
            {
                if (!BotHasPermission(permission))
                    missingPermissions.Add(permission);
            }
            return missingPermissions;
        }

        public IEnumerable<Permissions> FindMissingChannelPermissions(DiscordChannel channel)
        {
            List<Permissions> missingPermissions = new List<Permissions>();
            foreach (Permissions permission in DLConstants.REQUESTED_CHANNEL_PERMISSIONS)
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

        public async Task<DiscordMember> GetMemberAsync(string memberIdStr)
        {
            if (!memberIdStr.TryParseSnowflakeId(out ulong memberId))
                return null;

            try
            {
                Logger.Trace($"Fetching member with ID \"{memberId}\"");
                return await Guild.GetMemberAsync(memberId);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred when attempting to fetch member with ID \"{memberIdStr}\"", e);
                return null;
            }
            catch (Exception e)
            {
                Logger.Exception($"Error occurred when attempting to fetch member with ID \"{memberIdStr}\"", e);
                return null;
            }
        }

        public async Task<DiscordMessage> GetMessageAsync(DiscordChannel channel, ulong messageId)
        {
            if (!ChannelHasPermission(channel, Permissions.ReadMessageHistory))
                return null;

            try
            {
                Logger.Trace($"Fetching message with ID \"{messageId}\" from channel \"{channel.Name}\"");
                return await channel.GetMessageAsync(messageId);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred when attempting to read message with ID {messageId} from channel \"{channel.Name}\"", e);
                return null;
            }
            catch (Exception e)
            {
                Logger.Exception($"Error occurred when attempting to read message with ID {messageId} from channel \"{channel.Name}\"", e);
                return null;
            }
        }

        public async Task<IReadOnlyList<DiscordMessage>> GetMessagesAsync(DiscordChannel channel)
        {
            if (channel == null || !ChannelHasPermission(channel, Permissions.ReadMessageHistory))
                return null;

            try
            {
                Logger.Trace($"Fetching recent messages from channel \"{channel.Name}\"");
                return await channel.GetMessagesAsync();
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while fetching messages from channel \"{channel.Name}", e);
                return null;
            }
            catch (Exception e)
            {
                Logger.Exception($"Error occurred when attempting to read message history from channel \"{channel.Name}\"", e);
                return null;
            }
        }

        public async Task<IReadOnlyCollection<DiscordMember>> GetMembersAsync()
        {
            if (!BotHasIntent(DiscordIntents.GuildMembers))
            {
                Logger.Error("Attempted to get full guild member list but the bot does not have the privileged GuildMembers intent");
                return null;
            }

            try
            {
                Logger.Trace("Fetching guild member list");
                return await Guild.GetAllMembersAsync();
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while fetching all guild members", e);
                return null;
            }
            catch (Exception e)
            {
                Logger.Exception($"Error occured when attempting to fetch all guild members", e);
                return null;
            }
        }

        #endregion

        #region Manipulation

        public async Task<DiscordMessage> SendMessageAsync(DiscordChannel channel, string textContent, DiscordLinkEmbed embedContent = null)
        {
            if (channel == null)
                return null;

            DiscordMessage createdMessage = null;
            try
            {
                if (!ChannelHasPermission(channel, Permissions.SendMessages))
                {
                    Logger.Warning($"Attempted to send message to channel `{channel}` but the bot user is lacking permissions for this action");
                    return null;
                }

                // Either make sure we have permission to use embeds or convert the embed to text
                string fullTextContent = (embedContent == null || ChannelHasPermission(channel, Permissions.EmbedLinks)) ? textContent : $"{textContent}\n{embedContent.AsDiscordText()}";

                // If needed; split the message into multiple parts
                ICollection<string> stringParts = MessageUtils.SplitStringBySize(fullTextContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
                ICollection<DiscordEmbed> embedParts = MessageUtils.BuildDiscordEmbeds(embedContent);

                Logger.Trace($"Sending message to channel \"{channel.Name}\" containing {stringParts.Count} raw string parts and {embedParts.Count} embed parts");
                if (stringParts.Count <= 1 && embedParts.Count == 1)
                {
                    createdMessage = await channel.SendMessageAsync(fullTextContent, embedParts.First());
                }
                else
                {
                    foreach (string textMessagePart in stringParts)
                    {
                        createdMessage = await channel.SendMessageAsync(textMessagePart);
                    }
                    foreach (DiscordEmbed embedPart in embedParts)
                    {
                        createdMessage = await channel.SendMessageAsync(embedPart);
                    }
                }
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while sending message to channel \"{channel.Name}\"", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send message to channel {channel.Name}", e);
            }
            return createdMessage;
        }

        public async Task<DiscordMessage> SendDmAsync(DiscordMember recipientMember, string textContent, DiscordLinkEmbed embedContent = null)
        {
            if (recipientMember == null)
                return null;

            DiscordMessage createdMessage = null;
            try
            {
                // If needed; split the message into multiple parts
                ICollection<string> stringParts = MessageUtils.SplitStringBySize(textContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
                ICollection<DiscordEmbed> embedParts = MessageUtils.BuildDiscordEmbeds(embedContent);

                Logger.Trace($"Sending DM to user \"{recipientMember.Username}\" containing {stringParts.Count} raw string parts and {embedParts.Count} embed parts");
                if (stringParts.Count <= 1 && embedParts.Count <= 1)
                {
                    DiscordEmbed embed = (embedParts.Count >= 1) ? embedParts.First() : null;
                    createdMessage = await recipientMember.SendMessageAsync(textContent, embed);
                }
                else
                {
                    foreach (string textMessagePart in stringParts)
                    {
                        createdMessage = await recipientMember.SendMessageAsync(textMessagePart, null);
                    }
                    foreach (DiscordEmbed embedPart in embedParts)
                    {
                        createdMessage = await recipientMember.SendMessageAsync(null, embedPart);
                    }
                }
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while sending message to member \"{recipientMember.Username}\"", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send DM message to {recipientMember.Username}", e);
            }
            return createdMessage;
        }

        public async Task<DiscordMessage> ModifyMessageAsync(DiscordMessage message, string textContent, DiscordLinkEmbed embedContent = null)
        {
            if (message == null)
                return null;

            DiscordMessage editedMessage = null;
            try
            {
                DiscordChannel channel = message.GetChannel();
                if (!ChannelHasPermission(channel, Permissions.ManageMessages))
                {
                    Logger.Error($"Attempted to modify message in channel `{channel}` but the bot user is lacking permissions for this action");
                    return null;
                }

                if (embedContent == null)
                {
                    Logger.Trace($"Editing raw message in channel \"{message.Channel.Name}\"");
                    editedMessage = await message.ModifyAsync(textContent);
                }
                else
                {
                    // Either make sure we have permission to use embeds or convert the embed to text
                    if (ChannelHasPermission(channel, Permissions.EmbedLinks))
                    {
                        List<DiscordEmbed> splitEmbeds = MessageUtils.BuildDiscordEmbeds(embedContent);
                        Logger.Trace($"Editing embed message with {splitEmbeds.Count} pieces in channel \"{message.Channel.Name}\"");
                        if (splitEmbeds.Count > 0)
                            editedMessage = await message.ModifyAsync(textContent, splitEmbeds[0]); // TODO: Actually keep track of split messages instead of only overwriting the first one
                    }
                    else
                    {
                        Logger.Trace($"Editing converted embed message with in channel \"{message.Channel.Name}\"");
                        await message.ModifyEmbedSuppressionAsync(true); // Remove existing embeds
                        editedMessage = await message.ModifyAsync($"{textContent}\n{embedContent.AsDiscordText()}");
                    }
                }
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while modifying message in channel \"{message.Channel.Name}\"", e);
            }
            catch (Exception e)
            {
                string channelName = message?.Channel?.Name;
                if (string.IsNullOrWhiteSpace(channelName))
                    channelName = "Unknown channel";

                Logger.Exception($"Failed to modify message in channel \"{channelName}\"", e);
            }
            return editedMessage;
        }

        public async Task<bool> DeleteMessageAsync(DiscordMessage message)
        {
            if (message == null)
                return false;

            DiscordChannel channel = message.GetChannel();
            if (!ChannelHasPermission(channel, Permissions.ManageMessages))
            {
                Logger.Warning($"Attempted to delete message in channel \"{channel}\" but the bot user is lacking permissions for this action");
                return false;
            }

            bool result = false;
            try
            {
                Logger.Trace($"Deleting message \"{message.Id}\" from channel \"{channel.Name}\"");
                await message.DeleteAsync("Deleted by DiscordLink");
                result = true;
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while deleting message in channel \"{message.Channel.Name}\"", e);
            }
            catch (Exception e)
            {
                string channelName = message?.Channel?.Name;
                if (string.IsNullOrWhiteSpace(channelName))
                    channelName = "Unknown channel";

                Logger.Exception($"Failed to delete message from channel \"{channelName}\"", e);
            }
            return result;
        }

        public async Task<DiscordRole> CreateRoleAsync(DiscordLinkRole dlRole)
        {
            try
            {
                Logger.Trace($"Creating role \"{dlRole.Name}\"");
                DiscordRole role = await Guild.CreateRoleAsync(dlRole.Name, dlRole.Permissions, dlRole.Color, dlRole.Hoist, dlRole.Mentionable, dlRole.AddReason);
                if (role != null)
                {
                    DLStorage.PersistentData.RoleIds.Add(role.Id);
                    DLStorage.Instance.Write(); // Save immediately after creating so that we don't lose track of the roles in case of an ungraceful exit
                }
                else
                {
                    Logger.Error($"Failed to create role \"{dlRole.Name}\".");
                }

                return role;
            }
            catch (UnauthorizedException e)
            {
                Logger.Exception($"DiscordLink was not allowed to create the role \"{dlRole.Name}\". Ensure that your bot user is assigned a role with higher permission level than all roles it manages.", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while creating role \"{dlRole.Name}\"", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to create role \"{dlRole.Name}\"", e);
            }
            return await Task.FromResult<DiscordRole>(null);
        }

        public DiscordRole GetRoleById(ulong roleId)
        {
            return Guild.GetRoleById(roleId);
        }

        public DiscordRole GetRoleByName(string roleName)
        {
            return Guild.GetRoleByName(roleName);
        }

        public async Task AddRoleAsync(DiscordMember member, DiscordLinkRole dlRole)
        {
            DiscordRole discordRole = Guild.GetRoleByName(dlRole.Name);
            if (discordRole == null)
                discordRole = await CreateRoleAsync(dlRole);

            if (discordRole != null)
                await AddRoleAsync(member, discordRole);
        }

        public async Task AddRoleAsync(DiscordMember member, DiscordRole role)
        {
            if (member == null || role == null)
                return;
            if (member.HasRole(role))
                return; // Member already has the role

            try
            {
                Logger.Trace($"Adding role \"{role.Name}\" to member \"{member.Username}\"");
                await member.GrantRoleAsync(role, "Added by DiscordLink");
            }
            catch (UnauthorizedException e)
            {
                Logger.Exception($"DiscordLink was not allowed to grant the role \"{role.Name}\" to member \"{member.Username}\". Ensure that your bot user is assigned a role with higher permission level than all roles it manages.", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while adding role \"{role.Name}\" to member \"{member.Username}\"", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to grant role \"{role.Name}\" to member \"{member.Username}\"", e);
            }
        }

        public async Task RemoveRoleAsync(DiscordMember member, string roleName)
        {
            DiscordRole role = Guild.GetRoleByName(roleName);
            if (role == null)
            {
                Logger.Debug($"Attempting to remove nonexistent role \"{roleName}\" from user \"{member.DisplayName}\"");
                return;
            }

            await RemoveRoleAsync(member, role);
        }

        public async Task RemoveRoleAsync(DiscordMember member, DiscordRole role)
        {
            if (member == null || role == null)
                return;
            if (!member.HasRole(role))
                return; // Member doesn't have the role

            try
            {
                Logger.Trace($"Removing role \"{role.Name}\" from member \"{member.Username}\"");
                await member.RevokeRoleAsync(role, "Removed by DiscordLink");
            }
            catch (UnauthorizedException e)
            {
                Logger.Exception($"DiscordLink was not allowed to revoke the role \"{role.Name}\" from member \"{member.Username}\". Ensure that your bot user is assigned a role with higher permission level than all roles it manages. This role was most likely not created by the current bot. Deleting it manually will resolve this Issue.", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while removing role \"{role.Name}\" from member \"{member.Username}\"", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to revoke role \"{role.Name}\" from member \"{member.Username}\"", e);
            }
        }

        public async Task DeleteRoleAsync(DiscordRole role)
        {
            if (role == null)
                return;

            try
            {
                Logger.Trace($"Deleting role \"{role.Name}\"");
                await role.DeleteAsync("Deleted by DiscordLink");
            }
            catch (UnauthorizedException e)
            {
                Logger.Exception($"DiscordLink was not allowed to delete the role \"{role.Name}\". Ensure that your bot user is assigned a role with higher permission level than all roles it manages.", e);
            }
            catch (ServerErrorException e)
            {
                Logger.DebugException($"ServerErrorException occurred while deleting role \"{role.Name}\"", e);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to delete role \"{role.Name}\"", e);
            }
        }

        #endregion
    }
}
