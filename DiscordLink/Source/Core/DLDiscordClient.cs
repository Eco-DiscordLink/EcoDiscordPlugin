using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
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
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected
        }

        public DiscordClient DiscordClient { get; private set; }
        public DateTime LastConnectionTime { get; private set; } = DateTime.MinValue;
        public ConnectionState ConnectionStatus { get; private set; } = ConnectionState.Disconnected;
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

            if (!await CreateAndConnectClient(useFullIntents: true))
                return false;

            await Task.Delay(DLConstants.POST_SERVER_CONNECTION_WAIT_MS);
            DiscordClient.SocketClosed -= HandleSocketClosedOnConnection; // Stop waiting for aborted connections caused by faulty connection attempts
            if (ConnectionStatus == ConnectionState.Disconnected)
            {
                // Make another connection attempt without privileged intents
                if (!await CreateAndConnectClient(useFullIntents: false))
                    return false;
            }

            await Task.Delay(DLConstants.POST_SERVER_CONNECTION_WAIT_MS);
            DiscordClient.SocketClosed -= HandleSocketClosedOnConnection;
            if (ConnectionStatus == ConnectionState.Disconnected)
            {
                DiscordClient = null;
                _commands = null;
                Status = "Discord connection failed";
                return false; // If the second connection attempt also fails we give up
            }

            ConnectionStatus = ConnectionState.Connected;
            Status = "Connected to Discord";
            LastConnectionTime = DateTime.Now;

            Guild = GuildByNameOrID(DLConfig.Data.DiscordServer);
            BotMember = Guild.CurrentMember;

            RegisterEventListeners();
            OnConnected?.Invoke();
            return true;
        }

        private async Task<bool> CreateAndConnectClient(bool useFullIntents)
        {
            Status = "Creating Discord Client";
            ConnectionStatus = ConnectionState.Connecting;

            // Create client
            try
            {
                DiscordClient = new DiscordClient(new DiscordConfiguration
                {
                    AutoReconnect = true,
                    Token = DLConfig.Data.BotToken,
                    TokenType = TokenType.Bot,
                    MinimumLogLevel = DLConfig.Data.BackendLogLevel,
                    Intents = useFullIntents ? DiscordIntents.GuildMembers | DiscordIntents.AllUnprivileged : DiscordIntents.AllUnprivileged
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
                DiscordClient = null;
                _commands = null;
                ConnectionStatus = ConnectionState.Disconnected;
                Status = "Failed to create Discord Client";
                Logger.Error($"Error occurred while creating the Discord client. Error message: {e}");
                return false;
            }

            DiscordClient.SocketClosed += HandleSocketClosedOnConnection;

            // Connect client
            Status = "Connecting to Discord...";
            OnConnecting.Invoke();
            try
            {
                await DiscordClient.ConnectAsync(new DiscordActivity(MessageBuilder.Discord.GetActivityString(), ActivityType.Watching));
            }
            catch (Exception e)
            {
                DiscordClient = null;
                _commands = null;
                Logger.Error($"Error occurred while connecting to Discord. Error message: {e}");
                ConnectionStatus = ConnectionState.Disconnected;
                Status = "Discord connection failed";
                return false;
            }

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
                await DiscordClient.DisconnectAsync();
                DiscordClient.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error($"An Error occurred when disconnecting from Discord: Error message: {e.Message}");
                Status = "Discord disconnection failed";
                return false;
            }

            DiscordClient = null;
            ConnectionStatus = ConnectionState.Disconnected;
            Status = "Disconnected from Discord";
            Guild = null;
            BotMember = null;

            OnDisconnected?.Invoke();
            DiscordLink.Obj.HandleEvent(DLEventType.DiscordClientDisconnected);
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
            DiscordClient.ClientErrored += HandleClientError;
            DiscordClient.SocketErrored += HandleSocketError;
            DiscordClient.MessageCreated += HandleDiscordMessageCreated;
            DiscordClient.MessageUpdated += HandleDiscordMessageEdited;
            DiscordClient.MessageDeleted += HandleDiscordMessageDeleted;
            DiscordClient.MessageReactionAdded += HandleDiscordReactionAdded;
            DiscordClient.MessageReactionRemoved += HandleDiscordReactionRemoved;
        }

        private void UnregisterEventListeners()
        {
            DiscordClient.ClientErrored -= HandleClientError;
            DiscordClient.SocketErrored -= HandleSocketError;
            DiscordClient.MessageCreated -= HandleDiscordMessageCreated;
            DiscordClient.MessageUpdated -= HandleDiscordMessageEdited;
            DiscordClient.MessageDeleted -= HandleDiscordMessageDeleted;
            DiscordClient.MessageReactionAdded -= HandleDiscordReactionAdded;
            DiscordClient.MessageReactionRemoved -= HandleDiscordReactionRemoved;
        }

        #endregion

        #region Event Handlers

        private async Task HandleDiscordMessageCreated(DiscordClient client, MessageCreateEventArgs args)
        {
            DiscordMessage message = args.Message;
            Logger.DebugVerbose($"Discord Message Received\n{message.FormatForLog()}");

            if (args.Author == DiscordClient.CurrentUser)
                return; // Ignore messages sent by our own bot

            if (!string.IsNullOrWhiteSpace(message.Content) && message.Content.StartsWith(DLConfig.Data.DiscordCommandPrefix))
                return; // Ignore commands

            DiscordLink.Obj.HandleEvent(DLEventType.DiscordMessageSent, message);
        }

        private async Task HandleDiscordMessageEdited(DiscordClient client, MessageUpdateEventArgs args)
        {
            if (args.Author == DiscordClient.CurrentUser)
                return; // Ignore messages edits made by our own bot

            DiscordMessage message = args.Message;

            if (!string.IsNullOrWhiteSpace(message.Content) && message.Content.StartsWith(DLConfig.Data.DiscordCommandPrefix))
                return;

            DiscordLink.Obj.HandleEvent(DLEventType.DiscordMessageEdited, args.Message, args.MessageBefore);
        }

        private async Task HandleDiscordMessageDeleted(DiscordClient client, MessageDeleteEventArgs args)
        {
            DiscordLink.Obj.HandleEvent(DLEventType.DiscordMessageDeleted, args.Message);
        }

        private async Task HandleDiscordReactionAdded(DiscordClient client, MessageReactionAddEventArgs args)
        {
            if (args.User == client.CurrentUser)
                return; // Ignore reactions sent by our own bot

            DiscordLink.Obj.HandleEvent(DLEventType.DiscordReactionAdded, args.User, args.Message, args.Emoji);
        }

        private async Task HandleDiscordReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs args)
        {
            if (args.User == client.CurrentUser)
                return; // Ignore reactions sent by our own bot

            DiscordLink.Obj.HandleEvent(DLEventType.DiscordReactionRemoved, args.User, args.Message, args.Emoji);
        }

        private async Task HandleClientError(DiscordClient client, ClientErrorEventArgs args)
        {
            Logger.Debug($"A Discord client error occurred. Error message: {args.EventName} {args.Exception}");
        }

        private async Task HandleSocketError(DiscordClient client, SocketErrorEventArgs args)
        {
            Logger.Debug($"A socket error occurred. Error message: {args.Exception}");
        }

        private async Task HandleSocketClosedOnConnection(DiscordClient client, SocketCloseEventArgs args)
        {
            if (args.CloseCode == 4014) // Application does not have the requested privileged intents
            {
                Logger.Warning("Bot application is not configured to allow reading of full server member list as it lacks the Server Members Intent. Some features will be unavailable. See install instructions for help with adding intents.");
            }
            ConnectionStatus = ConnectionState.Disconnected;
        }

        #endregion

        #region Information Fetching

        public DiscordGuild GuildByNameOrID(string guildNameOrID)
        {
            return Utilities.Utils.TryParseSnowflakeID(guildNameOrID, out ulong ID)
                ? DiscordClient.Guilds.Values.FirstOrDefault(guild => guild.Id == ID)
                : DiscordClient.Guilds.Values.FirstOrDefault(guild => guild.Name.EqualsCaseInsensitive(guildNameOrID));
        }

        public DiscordChannel ChannelByNameOrID(string channelNameOrID)
        {
            return Utilities.Utils.TryParseSnowflakeID(channelNameOrID, out ulong ID)
                ? Guild.Channels.Values.FirstOrDefault(channel => channel.Id == ID)
                : Guild.Channels.Values.FirstOrDefault(guild => guild.Name.EqualsCaseInsensitive(channelNameOrID));
        }

        public DiscordMember MemberByNameOrID(string memberNameOrID)
        {
            return Utilities.Utils.TryParseSnowflakeID(memberNameOrID, out ulong ID)
                ? Guild.Members[ID]
                : Guild.Members.Values.FirstOrDefault(member => member.DisplayName.EqualsCaseInsensitive(memberNameOrID));
        }

        public bool ChannelHasPermission(DiscordChannel channel, Permissions permission)
        {
            if (channel.IsPrivate)
                return true; // Assume permission is given for DMs

            DiscordMember member = channel.Guild.CurrentMember;
            if (member == null)
            {
                Logger.Error($"CurrentMember was null when evaluating channel permissions for channel {channel.Name} ");
                return false;
            }

            return channel.PermissionsFor(member).HasPermission(permission);
        }

        public bool BotHasPermission(Permissions permission)
        {
            bool hasPermission = false;
            foreach(DiscordRole role in Guild.Roles.Values)
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
            if (guild == null)
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
            catch (ServerErrorException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (NotFoundException e)
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
            if (channel == null || !ChannelHasPermission(channel, Permissions.ReadMessageHistory))
                return null;

            try
            {
                return await channel.GetMessagesAsync();
            }
            catch (ServerErrorException e)
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

        public async Task<IReadOnlyCollection<DiscordMember>> GetGuildMembersAsync()
        {
            if (!BotHasIntent(DiscordIntents.GuildMembers))
            {
                Logger.Error("Attempted to get full guild member list without the bot having the privileged GuildMembers intent");
                return null;
            }

            try
            {
                return await Guild.GetAllMembersAsync();
            }
            catch (Exception e)
            {
                Logger.Error($"Error occured when attempting to fetch all guild members. Error message: {e}");
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
                string fullTextContent = ChannelHasPermission(channel, Permissions.EmbedLinks) ? textContent : $"{textContent}\n{embedContent.AsText()}";

                // If needed; split the message into multiple parts
                ICollection<string> stringParts = MessageUtils.SplitStringBySize(fullTextContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
                ICollection<DiscordEmbed> embedParts = MessageUtils.BuildDiscordEmbeds(embedContent);

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
            catch (Exception e)
            {
                Logger.Warning($"Failed to send message to channel {channel.Name}. Error message: {e}");
            }
            return createdMessage;
        }

        public async Task<DiscordMessage> SendDMAsync(DiscordMember targetMember, string textContent, DiscordLinkEmbed embedContent = null)
        {
            if (targetMember == null)
                return null;

            DiscordMessage createdMessage = null;
            try
            {
                // If needed; split the message into multiple parts
                ICollection<string> stringParts = MessageUtils.SplitStringBySize(textContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
                ICollection<DiscordEmbed> embedParts = MessageUtils.BuildDiscordEmbeds(embedContent);

                if (stringParts.Count <= 1 && embedParts.Count <= 1)
                {
                    DiscordEmbed embed = (embedParts.Count >= 1) ? embedParts.First() : null;
                    createdMessage = await targetMember.SendMessageAsync(textContent, embed);
                }
                else
                {
                    foreach (string textMessagePart in stringParts)
                    {
                        createdMessage = await targetMember.SendMessageAsync(textMessagePart, null);
                    }
                    foreach (DiscordEmbed embedPart in embedParts)
                    {
                        createdMessage = await targetMember.SendMessageAsync(null, embedPart);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Debug($"Failed to send DM message to {targetMember.DisplayName}. Error message: {e}");
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
                    Logger.Warning($"Attempted to modify message in channel `{channel}` but the bot user is lacking permissions for this action");
                    return null;
                }

                if (embedContent == null)
                {
                    editedMessage = await message.ModifyAsync(textContent);
                }
                else
                {
                    // Either make sure we have permission to use embeds or convert the embed to text
                    if (ChannelHasPermission(channel, Permissions.EmbedLinks))
                    {
                        List<DiscordEmbed> splitEmbeds = MessageUtils.BuildDiscordEmbeds(embedContent);
                        if (splitEmbeds.Count > 0)
                            editedMessage = await message.ModifyAsync(textContent, splitEmbeds[0]); // TODO: Actually keep track of split messages instead of only overwriting the first one
                    }
                    else
                    {
                        await message.ModifyEmbedSuppressionAsync(true); // Remove existing embeds
                        editedMessage = await message.ModifyAsync($"{textContent}\n{embedContent.AsText()}");
                    }
                }
            }
            catch (Exception e)
            {
                string channelName = message?.Channel?.Name;
                if (string.IsNullOrWhiteSpace(channelName))
                    channelName = "Unknown channel";

                Logger.Warning($"Failed to modify message in channel \"{channelName}\". Error message: {e}");
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
                await message.DeleteAsync("Deleted by DiscordLink");
                result = true;
            }
            catch (Exception e)
            {
                string channelName = message?.Channel?.Name;
                if (string.IsNullOrWhiteSpace(channelName))
                    channelName = "Unknown channel";

                Logger.Warning($"Failed to delete message from channel \"{channelName}\". Error message: {e}");
            }
            return result;
        }

        public async Task<DiscordRole> CreateRoleAsync(DiscordLinkRole dlRole)
        {
            try
            {
                return await Guild.CreateRoleAsync(dlRole.Name, dlRole.Permissions, dlRole.Color, dlRole.Hoist, dlRole.Mentionable, dlRole.AddReason);
            }
            catch (Exception e)
            {
                Logger.Warning($"Failed to create role \"{dlRole.Name}\". Error message: {e}");
            }
            return await Task.FromResult<DiscordRole>(null);
        }

        public async Task AddRoleAsync(DiscordMember member, DiscordLinkRole dlRole)
        {
            DiscordRole role = Guild.RoleByName(dlRole.Name);
            if (role == null)
                role = await CreateRoleAsync(dlRole);

            if(role != null)
                await AddRoleAsync(member, role);
        }

        public async Task AddRoleAsync(DiscordMember member, DiscordRole role)
        {
            if (member == null || role == null)
                return;
            if (member.HasRole(role))
                return; // Member already has the role

            try
            {
                await member.GrantRoleAsync(role, "Added by DiscordLink");
            }
            catch (Exception e)
            {
                Logger.Warning($"Failed to grant role \"{role.Name}\" to member \"{member.DisplayName}\". Error message: {e}");
            }
        }

        public async Task RemoveRoleAsync(DiscordMember member, string roleName)
        {
            DiscordRole role = Guild.RoleByName(roleName);
            if (role == null)
            {
                Logger.Warning($"Attempting to remove nonexistent role \"{roleName}\" from user \"{member.DisplayName}\"");
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
                await member.RevokeRoleAsync(role, "Removed by DiscordLink");
            }
            catch (Exception e)
            {
                Logger.Warning($"Failed to revoke role \"{role.Name}\" from member \"{member.DisplayName}\". Error message: {e}");
            }
        }

        #endregion
    }
}
