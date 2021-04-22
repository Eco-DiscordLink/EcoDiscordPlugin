using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Eco.Core.Utils;
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
    public class DLDiscordClient
    {
        public DiscordClient DiscordClient { get; private set; }
        public DateTime LastConnectionTime { get; private set; } = DateTime.MinValue;
        public bool Connected { get; private set; } = false;

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
        private CommandsNextExtension _commands = null;

        #region Connection Handling

        public ThreadSafeAction OnConnecting = new ThreadSafeAction();
        public ThreadSafeAction OnConnected = new ThreadSafeAction();

        public ThreadSafeAction OnDisconnecting = new ThreadSafeAction();
        public ThreadSafeAction OnDisconnected = new ThreadSafeAction();

        public async Task<bool> Start()
        {
            Logger.Debug("Client Starting");

            bool BotTokenIsNull = string.IsNullOrWhiteSpace(DLConfig.Data.BotToken);
            if (BotTokenIsNull)
            {
                DLConfig.Instance.VerifyConfig(DLConfig.VerificationFlags.Static); // Make the user aware of the empty bot token
                return false; // Do not attempt to initialize if the bot token is empty
            }

            Status = "Creating Discord Client";
            try
            {
                // Create client
                DiscordClient = new DiscordClient(new DiscordConfiguration
                {
                    AutoReconnect = true,
                    Token = DLConfig.Data.BotToken,
                    TokenType = TokenType.Bot,
                    MinimumLogLevel = DLConfig.Data.BackendLogLevel
                });

                // Register Discord commands
                _commands = DiscordClient.UseCommandsNext(new CommandsNextConfiguration
                {
                    StringPrefixes = DLConfig.Data.DiscordCommandPrefix.SingleItemAsEnumerable()
                });
                _commands.RegisterCommands<DiscordCommands>();
            }
            catch (Exception e)
            {
                Status = "Failed to create Discord Client";
                Logger.Error($"Error occurred while creating the Discord client. Error message: {e}");
                return false;
            }

            // Connect client
            Status = "Connecting to Discord...";
            OnConnecting.Invoke();
            try
            {
                await DiscordClient.ConnectAsync();
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while connecting to Discord. Error message: " + e);
                Status = "Discord connection failed";
                return false;
            }

            Connected = true;
            Status = "Connected to Discord";
            LastConnectionTime = DateTime.Now;

            RegisterEventListeners();
            OnConnected?.Invoke();
            return true;
        }

        public async Task<bool> Stop()
        {
            UnregisterEventListeners();

            // Disconnect
            Status = "Disconnecting from Discord";
            OnDisconnecting?.Invoke();
            try
            {
                // If DisconnectAsync() is called in the GUI thread, it will cause a deadlock
                SystemUtils.SynchronousThreadExecute(() =>
                {
                    DiscordClient.DisconnectAsync().Wait();
                });
                DiscordClient.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error($"An Error occurred when disconnecting from Discord: Error message: {e.Message}");
                Status = "Discord disconnection failed";
                return false;
            }

            DiscordClient = null;
            Connected = false;
            Status = "Disconnected from Discord";

            OnDisconnected?.Invoke();
            DiscordLink.Obj.HandleEvent(DLEventType.DiscordClientStarted);
            return true;
        }

        public async Task<bool> Restart()
        {
            Status = "Restarting...";

            if(Connected)
                await Stop();

            if (!Connected)
                await Start();

            return Connected;
        }

        private void RegisterEventListeners()
        {
            DiscordClient.ClientErrored += HandleClientError;
            DiscordClient.SocketErrored += HandleSocketError;
            DiscordClient.MessageCreated += HandleDiscordMessageCreated;
            DiscordClient.MessageUpdated += HandleDiscordMessageEdited;
            DiscordClient.MessageDeleted += HandleDiscordMessageDeleted;
        }

        private void UnregisterEventListeners()
        {
            DiscordClient.ClientErrored -= HandleClientError;
            DiscordClient.SocketErrored -= HandleSocketError;
            DiscordClient.MessageCreated -= HandleDiscordMessageCreated;
            DiscordClient.MessageUpdated -= HandleDiscordMessageEdited;
            DiscordClient.MessageDeleted -= HandleDiscordMessageDeleted;
        }

        #endregion

        #region Event Handlers

        private async Task HandleDiscordMessageCreated(DiscordClient client, MessageCreateEventArgs args)
        {
            DiscordMessage message = args.Message;
            Logger.DebugVerbose($"Discord Message Received\n{message.FormatForLog()}");

            // Ignore messages sent by our bot
            if (args.Author == DiscordClient.CurrentUser)
                return;

            // Ignore commands
            if (!string.IsNullOrWhiteSpace(message.Content) && message.Content.StartsWith(DLConfig.Data.DiscordCommandPrefix))
                return;

            DiscordLink.Obj.HandleEvent(DLEventType.DiscordMessageSent, message);
        }

        private async Task HandleDiscordMessageEdited(DiscordClient client, MessageUpdateEventArgs args)
        {
            DiscordMessage message = args.Message;

            // Ignore commands and messages sent by our bot
            if (args.Author == DiscordClient.CurrentUser) return;
            if (!string.IsNullOrWhiteSpace(message.Content) && message.Content.StartsWith(DLConfig.Data.DiscordCommandPrefix)) return;

            DiscordLink.Obj.HandleEvent(DLEventType.DiscordMessageEdited, args.Message, args.MessageBefore);
        }

        private async Task HandleDiscordMessageDeleted(DiscordClient client, MessageDeleteEventArgs args)
        {
            DiscordLink.Obj.HandleEvent(DLEventType.DiscordMessageDeleted, args.Message);
        }

        private async Task HandleClientError(DiscordClient client, ClientErrorEventArgs args)
        {
            Logger.Debug($"A Discord client error occurred. Error messages: {args.EventName} {args.Exception}");
            await Restart();
        }

        private async Task HandleSocketError(DiscordClient client, SocketErrorEventArgs args)
        {
            Logger.Debug($"A socket error occurred. Error message: {args.Exception}");
            await Restart();
        }

        #endregion

        #region Information Fetching

        public DiscordGuild GuildByNameOrID(string guildNameOrID)
        {
            return Utilities.Utils.TryParseSnowflakeID(guildNameOrID, out ulong ID)
                ? DiscordClient.Guilds[ID]
                : DiscordClient.Guilds.Values.FirstOrDefault(guild => guild.Name.EqualsCaseInsensitive(guildNameOrID));
        }

        public DiscordChannel ChannelByNameOrID(string guildNameOrID, string channelNameOrID)
        {
            DiscordGuild guild = GuildByNameOrID(guildNameOrID);
            if (guild == null)
                return null;

            return Utilities.Utils.TryParseSnowflakeID(channelNameOrID, out ulong ID)
                ? guild.Channels[ID]
                : guild.Channels.Values.FirstOrDefault(guild => guild.Name.EqualsCaseInsensitive(guildNameOrID));
        }

        public DiscordMember MemberByNameOrID(string guildNameOrID, string memberNameOrID)
        {
            DiscordGuild guild = GuildByNameOrID(guildNameOrID);
            if (guild == null)
                return null;

            return MemberByNameOrID(guild, memberNameOrID);
        }

        public DiscordMember MemberByNameOrID(DiscordGuild guild, string memberNameOrID)
        {
            return Utilities.Utils.TryParseSnowflakeID(memberNameOrID, out ulong ID)
                ? guild.Members[ID]
                : guild.Members.Values.FirstOrDefault(member => member.DisplayName.EqualsCaseInsensitive(memberNameOrID));
        }

        public bool ChannelHasPermission(DiscordChannel channel, Permissions permission)
        {
            if (channel as DiscordDmChannel != null)
                return true; // Assume permission is given for DMs

            DiscordMember member = channel.Guild.CurrentMember;
            if (member == null)
            {
                Logger.Debug($"CurrentMember was null when evaluating channel permissions for channel {channel.Name} ");
                return false;
            }

            return channel.PermissionsFor(member).HasPermission(permission);
        }

        public bool BotHasIntent(DiscordIntents intent)
        {
            return (DiscordClient.Intents & intent) != 0;
        }

        public bool MemberIsAdmin(DiscordMember member)
        {
            foreach (string adminRole in DLConfig.Data.AdminRoles)
            {
                if (Utilities.Utils.TryParseSnowflakeID(adminRole, out ulong adminRoleID) && member.Roles.Any(role => role.Id == adminRoleID))
                    return true;

                if (member.Roles.Any(role => role.Name.EqualsCaseInsensitive(adminRole)))
                    return true;
            }

            return false;
        }

        public async Task<DiscordUser> GetUserAsync(string userID)
        {
            if (!Utilities.Utils.TryParseSnowflakeID(userID, out ulong ID))
                return null;

            return await GetUserAsync(ID);
        }

        public async Task<DiscordUser> GetUserAsync(ulong userID)
        {
            return await DiscordClient.GetUserAsync(userID);
        }

        public async Task<DiscordMember> GetMemberAsync(string guildID, string userID)
        {
            if (!Utilities.Utils.TryParseSnowflakeID(userID, out ulong userSnowflakeID))
                return null;

            if (!Utilities.Utils.TryParseSnowflakeID(guildID, out ulong guildSnowflakeID))
                return null;

            return await GetMemberAsync(guildSnowflakeID, userSnowflakeID);
        }

        public async Task<DiscordMember> GetMemberAsync(ulong guildID, ulong userID)
        {
            DiscordGuild guild = await DiscordClient.GetGuildAsync(guildID);
            if(guild == null)
                return null;

            return await GetMemberAsync(guild, userID);
        }

        public async Task<DiscordMember> GetMemberAsync(DiscordGuild guild, string userID)
        {
            if (!Utilities.Utils.TryParseSnowflakeID(userID, out ulong ID))
                return null;

            return await GetMemberAsync(guild, ID);
        }

        public async Task<DiscordMember> GetMemberAsync(DiscordGuild guild, ulong userID)
        {
            return await guild.GetMemberAsync(userID);
        }

        public async Task<DiscordMessage> GetMessageAsync(DiscordChannel channel, ulong messageID)
        {
            if (!ChannelHasPermission(channel, Permissions.ReadMessageHistory))
                return null;

            try
            {
                return await channel.GetMessageAsync(messageID);
            }
            catch (DSharpPlus.Exceptions.ServerErrorException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (DSharpPlus.Exceptions.NotFoundException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Error occurred when attempting to read message with ID {messageID} from channel \"{channel.Name}\". Error message: {e}");
                return null;
            }
        }

        public async Task<IReadOnlyList<DiscordMessage>> GetMessagesAsync(DiscordChannel channel)
        {
            if (!ChannelHasPermission(channel, Permissions.ReadMessageHistory))
                return null;

            try
            {
                return await channel.GetMessagesAsync();
            }
            catch (DSharpPlus.Exceptions.ServerErrorException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Error occurred when attempting to read message history from channel \"{channel.Name}\". Error message: {e}");
                return null;
            }
        }

        public async Task<IReadOnlyCollection<DiscordMember>> GetGuildMembersAsync(DiscordGuild guild)
        {
            if (!BotHasIntent(DiscordIntents.GuildMembers))
            {
                Logger.Error("Attempted to get full guild member list without the bot having the privileged GuildMembers intent");
                return null;
            }

            try
            {
                return await guild.GetAllMembersAsync();
            }
            catch (Exception e)
            {
                Logger.Error($"Error occured when attempting to fetch all guild members. Error message: {e}");
                return null;
            }
        }

        #endregion

        #region Manipulation

        public async Task SendMessageAsync(DiscordChannel channel, string textContent, DiscordLinkEmbed embedContent = null)
        {
            try
            {
                if (!ChannelHasPermission(channel, Permissions.SendMessages))
                {
                    Logger.Warning($"Attempted to send message to channel `{channel}` but the bot user is lacking permissions for this action");
                    return;
                }

                // Either make sure we have permission to use embeds or convert the embed to text
                string fullTextContent = ChannelHasPermission(channel, Permissions.EmbedLinks) ? textContent : $"{textContent}\n{embedContent.AsText()}";

                // If needed; split the message into multiple parts
                ICollection<string> stringParts = MessageUtils.SplitStringBySize(fullTextContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
                ICollection<DiscordEmbed> embedParts = MessageUtils.BuildDiscordEmbeds(embedContent);

                if (stringParts.Count <= 1 && embedParts.Count <= 1)
                {
                    DiscordEmbed embed = (embedParts.Count >= 1) ? embedParts.First() : null;
                    await channel.SendMessageAsync(fullTextContent, tts: false, embed);
                }
                else
                {
                    foreach (string textMessagePart in stringParts)
                    {
                        await channel.SendMessageAsync(textMessagePart, tts: false, null);
                    }
                    foreach (DiscordEmbed embedPart in embedParts)
                    {
                        await channel.SendMessageAsync(null, tts: false, embedPart);
                    }
                }
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Logger.Debug(e.ToString());
            }
            catch (Exception e)
            {
                Logger.Error($"Error occurred while attempting to send Discord message to channel \"{channel.Name}\". Error message: {e}");
            }
        }

        public async Task SendDMAsync(DiscordMember targetMember, string textContent, DiscordLinkEmbed embedContent = null)
        {
            try
            {
                // If needed; split the message into multiple parts
                ICollection<string> stringParts = MessageUtils.SplitStringBySize(textContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
                ICollection<DiscordEmbed> embedParts = MessageUtils.BuildDiscordEmbeds(embedContent);

                if (stringParts.Count <= 1 && embedParts.Count <= 1)
                {
                    DiscordEmbed embed = (embedParts.Count >= 1) ? embedParts.First() : null;
                    await targetMember.SendMessageAsync(textContent, is_tts: false, embed);
                }
                else
                {
                    foreach (string textMessagePart in stringParts)
                    {
                        await targetMember.SendMessageAsync(textMessagePart, is_tts: false, null);
                    }
                    foreach (DiscordEmbed embedPart in embedParts)
                    {
                        await targetMember.SendMessageAsync(null, is_tts: false, embedPart);
                    }
                }
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Logger.Debug(e.ToString());
            }
            catch (Exception e)
            {
                Logger.Error($"Error occurred while attempting to send Discord message to user \"{targetMember.DisplayName}\". Error message: {e}");
            }
        }

        public async Task ModifyMessageAsync(DiscordMessage message, string textContent, DiscordLinkEmbed embedContent = null)
        {
            try
            {
                if (!ChannelHasPermission(message.Channel, Permissions.ManageMessages))
                {
                    Logger.Warning($"Attempted to modify message in channel `{message.Channel}` but the bot user is lacking permissions for this action");
                    return;
                }

                if (embedContent == null)
                {
                    await message.ModifyAsync(textContent);
                }
                else
                {
                    try
                    {
                        // Either make sure we have permission to use embeds or convert the embed to text
                        if (ChannelHasPermission(message.Channel, Permissions.EmbedLinks))
                        {
                            await message.ModifyAsync(textContent, MessageUtils.BuildDiscordEmbed(embedContent)); // TODO: Not safe! May require splitting!
                        }
                        else
                        {
                            await message.ModifyEmbedSuppressionAsync(true); // Remove existing embeds
                            await message.ModifyAsync($"{textContent}\n{embedContent.AsText()}");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Warning($"Failed to modify message. The message may be too long. Error message: {e}");
                    }
                }
            }
            catch (DSharpPlus.Exceptions.ServerErrorException e)
            {
                Logger.Debug(e.ToString());
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Logger.Debug(e.ToString());
            }
            catch (DSharpPlus.Exceptions.NotFoundException e)
            {
                Logger.Debug(e.ToString());
            }
            catch (Exception e)
            {
                Logger.Error($"Error occurred while attempting to modify Discord message. Error message: {e}");
            }
        }

        public async Task DeleteMessageAsync(DiscordMessage message)
        {
            if (!ChannelHasPermission(message.Channel, Permissions.ManageMessages))
            {
                Logger.Warning($"Attempted to delete message in channel `{message.Channel}` but the bot user is lacking permissions for this action");
                return;
            }

            try
            {
                await message.DeleteAsync("Deleted by DiscordLink");
            }
            catch (Exception e)
            {
                Logger.Error($"Error occurred while attempting to delete Discord message. Error message: {e}");
            }
        }

        #endregion
    }
}
