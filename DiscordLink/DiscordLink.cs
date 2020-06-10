using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.WebSocket;
using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Services;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink
{
    public class DiscordLink : IModKitPlugin, IInitializablePlugin, IConfigurablePlugin
    {
        public const string InviteCommandLinkToken = "[LINK]";
        public const string DiscordCommandPrefix = "?";
        protected string NametagColor = "7289DAFF";
        private PluginConfig<DiscordConfig> _configOptions;
        private DiscordConfig _prevConfigOptions; // Used to detect differances when the config is saved
        private DiscordClient _discordClient;
        private CommandsNextModule _commands;
        private string _currentToken;
        private string _status = "No Connection Attempt Made";
        private StreamWriter _chatLogWriter;


        private static readonly Regex TagStripRegex = new Regex("<[^>]*>");

        protected ChatNotifier chatNotifier;

        public override string ToString()
        {
            return "DiscordLink";
        }

        public IPluginConfig PluginConfig
        {
            get { return _configOptions; }
        }

        public DiscordConfig DiscordPluginConfig
        {
            get { return PluginConfig.GetConfig() as DiscordConfig; }
        }

        public string GetStatus()
        {
            return _status;
        }

        public void Initialize(TimedTask timer)
        {
            if (_discordClient == null) return;
            ConnectAsync();
            StartChatNotifier();
        }

        private void StartChatNotifier()
        {
            chatNotifier.Initialize();
            new Thread(() => { chatNotifier.Run(); })
            {
                Name = "ChatNotifierThread"
            }.Start();
        }

        public DiscordLink()
        {
            SetupConfig();
            chatNotifier = new ChatNotifier(new IChatMessageProviderChatServerWrapper());
            SetUpClient();
            VerifyConfig(); // Requires SetUpClient to run first so that the DiscordClient exists
            if (_configOptions.Config.LogChat)
            {
                StartChatlog();
            }
        }

        ~DiscordLink()
        {
            if (_configOptions.Config.LogChat)
            {
                StopChatlog();
            }
        }

        private void SetupConfig()
        {
            _configOptions = new PluginConfig<DiscordConfig>("DiscordPluginSpoffy");
            _prevConfigOptions = (DiscordConfig)_configOptions.Config.Clone();
            DiscordPluginConfig.PlayerConfigs.CollectionChanged += (obj, args) => { OnConfigChanged(); };
            DiscordPluginConfig.ChannelLinks.CollectionChanged += (obj, args) => { OnConfigChanged(); };
        }

        #region DiscordClient Management

        private async Task<object> DisposeOfClient()
        {
            if (_discordClient != null)
            {
                await DisconnectAsync();
                _discordClient.Dispose();
            }

            return null;
        }

        private bool SetUpClient()
        {
            DisposeOfClient();
            _status = "Setting up client";
            // Loading the configuration
            _currentToken = String.IsNullOrWhiteSpace(DiscordPluginConfig.BotToken)
                ? "ThisTokenWillNeverWork" //Whitespace isn't allowed, and it should trigger an obvious authentication error rather than crashing.
                : DiscordPluginConfig.BotToken;

            try
            {
                // Create the new client
                _discordClient = new DiscordClient(new DiscordConfiguration
                {
                    AutoReconnect = true,
                    Token = _currentToken,
                    TokenType = TokenType.Bot
                });
                _discordClient.SetWebSocketClient<WebSocket4NetClient>();

                _discordClient.Ready += async args => { Logger.Info("Connected and Ready"); };
                _discordClient.ClientErrored += async args => { Logger.Error(args.EventName + " " + args.Exception.ToString()); };
                _discordClient.SocketErrored += async args => { Logger.Error(args.Exception.ToString()); };
                _discordClient.SocketClosed += async args => { Logger.DebugVerbose("Socket Closed: " + args.CloseMessage + " " + args.CloseCode); };
                _discordClient.Resumed += async args => { Logger.Info("Resumed connection"); };

                // Set up the client to use CommandsNext
                _commands = _discordClient.UseCommandsNext(new CommandsNextConfiguration
                {
                    StringPrefix = DiscordCommandPrefix
                });

                _commands.RegisterCommands<DiscordDiscordCommands>();

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("ERROR: Unable to create the discord client. Error message was: " + e.Message + "\n");
                Logger.Error("Backtrace: " + e.StackTrace);
            }

            return false;
        }

        public async Task<bool> RestartClient()
        {
            var result = SetUpClient();
            await ConnectAsync();
            return result;
        }
        public async Task<object> ConnectAsync()
        {
            try
            {
                _status = "Attempting connection...";
                await _discordClient.ConnectAsync();
                BeginRelaying();
                Logger.Info("Connected to Discord.\n");
                _status = "Connection successful";


            }
            catch (Exception e)
            {
                Logger.Error("Error connecting to discord: " + e.Message + "\n");
                _status = "Connection failed";
            }

            return null;
        }

        public async Task<object> DisconnectAsync()
        {
            try
            {
                StopRelaying();
                await _discordClient.DisconnectAsync();
            }
            catch (Exception e)
            {
                Logger.Error("Disconnecting from discord: " + e.Message + "\n");
                _status = "Connection failed";
            }

            return null;
        }

        public DiscordClient DiscordClient => _discordClient;

        #endregion

        #region Discord Guild Access

        public string[] GuildNames => _discordClient.GuildNames();
        public DiscordGuild DefaultGuild => _discordClient.DefaultGuild();
        
        public DiscordGuild GuildByName(string name)
        {
            return _discordClient.GuildByName(name);
        }

        public DiscordGuild GuildByNameOrId(string nameOrId)
        {
            var maybeGuildId = DSharpExtensions.TryParseSnowflakeId(nameOrId);
            return maybeGuildId != null ? _discordClient.Guilds[maybeGuildId.Value] : GuildByName(nameOrId);
        }

        #endregion

        #region Message Sending

        public async Task<string> SendMessage(string message, string channelNameOrId, string guildNameOrId)
        {
            if (_discordClient == null) return "No discord client";

            var guild = GuildByNameOrId(guildNameOrId);
            if (guild == null) return "No guild of that name found";

            var channel = guild.ChannelByNameOrId(channelNameOrId);
            return await SendMessage(message, channel);
        }

        private string FormatMessageFromUsername(string message, string username)
        {
            return $"**{username}**: {StripTags(message)}";
        }

        public async Task<string> SendMessage(string message, DiscordChannel channel)
        {
            if (_discordClient == null) return "No discord client";
            if (channel == null) return "No channel of that name or ID found in that guild";

            await _discordClient.SendMessageAsync(channel, message);
            return "Message sent successfully!";
        }

        public async Task<string> SendMessageAsUser(string message, User user, string channelName, string guildName)
        {
            return await SendMessage(FormatMessageFromUsername(message, user.Name), channelName, guildName);
        }

        public async Task<String> SendMessageAsUser(string message, User user, DiscordChannel channel)
        {
            return await SendMessage(FormatMessageFromUsername(message, user.Name), channel);
        }

        #endregion

        #region MessageRelaying

        private string EcoUserSteamId = "DiscordLinkSteam";
        private string EcoUserSlgId = "DiscordLinkSlg";
        private string EcoUserName = "Discord";
        private bool _relayInitialised = false;

        private User _ecoUser;
        public User EcoUser =>
            _ecoUser ?? (_ecoUser = UserManager.GetOrCreateUser(EcoUserSteamId, EcoUserSlgId, EcoUserName));

        private void BeginRelaying()
        {
            if (!_relayInitialised)
            {
                chatNotifier.OnMessageReceived.Add(OnMessageReceivedFromEco);
                _discordClient.MessageCreated += OnDiscordMessageCreateEvent;
            }

            _relayInitialised = true;
        }

        private void StopRelaying()
        {
            if (_relayInitialised)
            {
                chatNotifier.OnMessageReceived.Remove(OnMessageReceivedFromEco);
                _discordClient.MessageCreated -= OnDiscordMessageCreateEvent;
            }

            _relayInitialised = false;
        }

        private ChannelLink GetLinkForEcoChannel(string discordChannelNameOrId)
        {
            return DiscordPluginConfig.ChannelLinks.FirstOrDefault(link => link.DiscordChannel == discordChannelNameOrId);
        }

        private ChannelLink GetLinkForDiscordChannel(string ecoChannelName)
        {
            var lowercaseEcoChannelName = ecoChannelName.ToLower();
            return DiscordPluginConfig.ChannelLinks.FirstOrDefault(link => link.EcoChannel.ToLower() == lowercaseEcoChannelName);
        }

        public static string StripTags(string toStrip)
        {
            return TagStripRegex.Replace(toStrip, String.Empty);
        }

        public void LogEcoMessage(ChatMessage message)
        {
            Logger.DebugVerbose("Eco Message Processed:");
            Logger.DebugVerbose("Message: " + message.Text);
            Logger.DebugVerbose("Tag: " + message.Tag);
            Logger.DebugVerbose("Category: " + message.Category);
            Logger.DebugVerbose("Temporary: " + message.Temporary);
            Logger.DebugVerbose("Sender: " + message.Sender);
        }

        public void LogDiscordMessage(DiscordMessage message)
        {
            Logger.DebugVerbose("Discord Message Processed");
            Logger.DebugVerbose("Message: " + message.Content);
            Logger.DebugVerbose("Channel: " + message.Channel.Name);
            Logger.DebugVerbose("Sender: " + message.Author);
        }

        public void OnMessageReceivedFromEco(ChatMessage message)
        {
            LogEcoMessage(message);
            if (message.Sender == EcoUser.Name) { return; }
            if (String.IsNullOrWhiteSpace(message.Sender)) { return; };

            //Remove the # character from the start.
            var channelLink = GetLinkForDiscordChannel(message.Tag.Substring(1));
            var channel = channelLink?.DiscordChannel;
            var guild = channelLink?.DiscordGuild;

            if (!String.IsNullOrWhiteSpace(channel) && !String.IsNullOrWhiteSpace(guild))
            {
                ForwardMessageToDiscordChannel(message, channel, guild);
            }
        }

        public async Task OnDiscordMessageCreateEvent(MessageCreateEventArgs messageArgs)
        {
            OnMessageReceivedFromDiscord(messageArgs.Message);
        }

        public void OnMessageReceivedFromDiscord(DiscordMessage message)
        {
            LogDiscordMessage(message);
            if (message.Author == _discordClient.CurrentUser) { return; }
            if (message.Content.StartsWith(DiscordCommandPrefix)) { return; }
            
            var channelLink = GetLinkForEcoChannel(message.Channel.Name) ?? GetLinkForEcoChannel(message.Channel.Id.ToString());
            var channel = channelLink?.EcoChannel;
            if (!String.IsNullOrWhiteSpace(channel))
            {
                ForwardMessageToEcoChannel(message, channel);
            }
        }

        private async void ForwardMessageToEcoChannel(DiscordMessage message, string channelName)
        {
            Logger.DebugVerbose("Sending message to Eco channel: " + channelName);
            var author = await message.Channel.Guild.MaybeGetMemberAsync(message.Author.Id);
            var nametag = author != null
                ? Text.Bold(Text.Color(NametagColor, author.DisplayName))
                : message.Author.Username;
            var text = $"#{channelName} {nametag}: {GetReadableContent(message)}";
            ChatManager.SendChat(text, EcoUser);

            if (_chatlogInitialized)
            {
                _chatLogWriter.WriteLine("[Discord] (" + DateTime.Now.ToShortDateString() + ":" + DateTime.Now.ToShortTimeString() + ") " + $"{StripTags(message.Author.Username) + ": " + StripTags(message.Content)}");
            }
        }

        private void ForwardMessageToDiscordChannel(ChatMessage message, string channel, string guild)
        {
            Logger.DebugVerbose("Sending Eco message to Discord");
            SendMessage(FormatMessageFromUsername(message.Text, message.Sender), channel, guild);

            if (_chatlogInitialized)
            {
                _chatLogWriter.WriteLine("[Eco] (" + DateTime.Now.ToShortDateString() + ":" + DateTime.Now.ToShortTimeString() + ") " + $"{StripTags(message.Sender) + ": " + StripTags(message.Text)}");
            }
        }

        private String GetReadableContent(DiscordMessage message)
        {
            var content = message.Content;
            foreach (var user in message.MentionedUsers)
            {
                if (user == null) { continue; }
                DiscordMember member = message.Channel.Guild.Members.FirstOrDefault(m => m?.Id == user.Id);
                if (member == null) { continue; }
                String name = "@" + member.DisplayName;
                content = content.Replace($"<@{user.Id}>", name)
                        .Replace($"<@!{user.Id}>", name);
            }
            foreach (var role in message.MentionedRoles)
            {
                if (role == null) continue;
                content = content.Replace($"<@&{role.Id}>", $"@{role.Name}");
            }
            foreach (var channel in message.MentionedChannels)
            {
                if (channel == null) continue;
                content = content.Replace($"<#{channel.Id}>", $"#{channel.Name}");
            }
            return content;
        }

        #endregion

        #region Configuration

        public static DiscordLink Obj
        {
            get { return PluginManager.GetPlugin<DiscordLink>(); }
        }

        public object GetEditObject()
        {
            return _configOptions.Config;
        }

        public void OnEditObjectChanged(object o, string param)
        {
            OnConfigChanged();
        }

        public void OnConfigChanged()
        {
            SaveConfig();
            VerifyConfig();
        }

        protected void SaveConfig()
        {
            Logger.DebugVerbose("Saving Config");
            _configOptions.Save();

            if (DiscordPluginConfig.BotToken != _currentToken)
            {
                //Reinitialise client.
                Logger.Info("Discord Token changed, reinitialising client.\n");
                RestartClient();
            }

            if(_configOptions.Config.LogChat && !_prevConfigOptions.LogChat)
            {
                Logger.Info("Chatlog enabled");
                StartChatlog();
            }
            else if(!_configOptions.Config.LogChat && _prevConfigOptions.LogChat)
            {
                Logger.Info("Chatlog disabled");
                StopChatlog();
            }

            if( _configOptions.Config.ChatlogPath != _prevConfigOptions.ChatlogPath)
            {
                Logger.Info("Chatlog path changed. New path: " + _configOptions.Config.ChatlogPath);
                RestartChatlog();
            }

            if(string.IsNullOrEmpty(_configOptions.Config.EcoCommandChannel))
            {
                _configOptions.Config.EcoCommandChannel = DiscordConfig.DefaultValues.EcoCommandChannel;
            }

            if (string.IsNullOrEmpty(_configOptions.Config.InviteMessage))
            {
                _configOptions.Config.InviteMessage = DiscordConfig.DefaultValues.InviteMessage;
            }

            _prevConfigOptions = (DiscordConfig)_configOptions.Config.Clone();
        }

        private void VerifyConfig()
        {
            List<string> errorMessages = new List<string>();

            // Server IP
            if (!string.IsNullOrWhiteSpace(_configOptions.Config.ServerIP))
            {
                IPAddress address;
                if (!IPAddress.TryParse(_configOptions.Config.ServerIP, out address)
                    || (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork
                    && address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6))
                {
                    errorMessages.Add("[ServerIP] Not a valid IPv4 or IPv6 address");
                }
            }

            // Player configs
            foreach (DiscordPlayerConfig playerConfig in _configOptions.Config.PlayerConfigs)
            {
                bool found = false;
                foreach(User user in UserManager.Users)
                {
                    if (user.Name == playerConfig.Username)
                    {
                        found = true;
                        break;
                    }
                }
                if(!found)
                {
                    errorMessages.Add("[Player Configs] No user with name \"" + playerConfig.Username + "\" was found");
                }
            }

            // Channel links
            if (_discordClient != null)
            {
                foreach (ChannelLink link in _configOptions.Config.ChannelLinks)
                {
                    var guild = GuildByNameOrId(link.DiscordGuild);
                    if(guild == null)
                    {
                        errorMessages.Add("[Channel Links] No Discord Guild with the name \"" + link.DiscordGuild + "\" could be found");
                        continue; // The channel will always fail if the guild fails
                    }
                    var channel = guild.ChannelByNameOrId(link.DiscordChannel);
                    if (channel == null)
                    {
                        errorMessages.Add("[Channel Links] No Channel with the name \"" + link.DiscordGuild + "\" could be found in the Guild \"" + link.DiscordGuild + "\"" );
                    }
                }
            }
            else
            {
                errorMessages.Add("[Verification] No Discord Client available.");
            }

            // Eco command channel
            if (_configOptions.Config.EcoCommandChannel.Contains('#'))
            {
                errorMessages.Add("[Eco Command Channel] Channel name contains a channel indicator (#). The channel indicator will be added automatically and adding one manually may cause message sending to fail");
            }

            if (!_configOptions.Config.InviteMessage.Contains(InviteCommandLinkToken))
            {
                errorMessages.Add("[Invite Message] Message does not contain the invite link token " + InviteCommandLinkToken + ". If the invite link has been added manually, consider adding it to the network config instead");
            }

            // Report errors
            if (errorMessages.Count <= 0)
            {
                Logger.Info("Configuration verification completed without errors");
            }
            else
            {
                string concatenatedMessages = "";
                foreach (string message in errorMessages)
                {
                    concatenatedMessages += message + "\n";
                }
                Logger.Error("Configuration errors detected!\n" + concatenatedMessages);
            }
        }

        #endregion

        #region Player Configs

        public DiscordPlayerConfig GetOrCreatePlayerConfig(string identifier)
        {
            var config = DiscordPluginConfig.PlayerConfigs.FirstOrDefault(user => user.Username == identifier);
            if (config == null)
            {
                config = new DiscordPlayerConfig
                {
                    Username = identifier
                };
                AddOrReplacePlayerConfig(config);
            }

            return config;
        }

        public bool AddOrReplacePlayerConfig(DiscordPlayerConfig config)
        {
            var removed = DiscordPluginConfig.PlayerConfigs.Remove(config);
            DiscordPluginConfig.PlayerConfigs.Add(config);
            SavePlayerConfig();
            return removed;
        }

        public void SavePlayerConfig()
        {
            _configOptions.Save();
        }

        public DiscordChannel GetDefaultChannelForPlayer(string identifier)
        {
            var playerConfig = GetOrCreatePlayerConfig(identifier);
            if (playerConfig.DefaultChannel == null
                || String.IsNullOrEmpty(playerConfig.DefaultChannel.Guild)
                || String.IsNullOrEmpty(playerConfig.DefaultChannel.Channel))
            {
                return null;
            }

            return GuildByName(playerConfig.DefaultChannel.Guild).ChannelByName(playerConfig.DefaultChannel.Channel);
        }


        public void SetDefaultChannelForPlayer(string identifier, string guildName, string channelName)
        {
            var playerConfig = GetOrCreatePlayerConfig(identifier);
            playerConfig.DefaultChannel.Guild = guildName;
            playerConfig.DefaultChannel.Channel = channelName;
            SavePlayerConfig();
        }

        #endregion

        #region Chatlog
        bool _chatlogInitialized = false;

        private void StartChatlog()
        {
            try
            {
                _chatLogWriter = new StreamWriter(_configOptions.Config.ChatlogPath, append: true);
                _chatLogWriter.AutoFlush = true;
                _chatlogInitialized = true;
            }
            catch(Exception)
            {
                Logger.Error("Failed to initialize chat logger using path \"" + _configOptions.Config.ChatlogPath + "\"");
            }
        }

        private void StopChatlog()
        {
            _chatLogWriter?.Close();
            _chatLogWriter = null;
            _chatlogInitialized = false;
        }

        private void RestartChatlog()
        {
            StopChatlog();
            StartChatlog();
        }
        #endregion
    }

    public class DiscordConfig : ICloneable
    {
        public static class DefaultValues
        {
            public const string EcoCommandChannel = "General";
            public const string InviteMessage = "Join us on Discord!\n" + DiscordLink.InviteCommandLinkToken;
        }

        public object Clone() // Be careful not to change the original object here as that will trigger endless recursion.
        {
            return new DiscordConfig
            {
                BotToken = this.BotToken,
                ServerName = this.ServerName,
                ServerDescription = this.ServerDescription,
                ServerLogo = this.ServerLogo,
                ServerIP = this.ServerIP,
                Debug = this.Debug,
                LogChat = this.LogChat,
                ChatlogPath = this.ChatlogPath,
                PlayerConfigs = new ObservableCollection<DiscordPlayerConfig>(this.PlayerConfigs.Select(t => t.Clone()).Cast<DiscordPlayerConfig>()),
                ChannelLinks = new ObservableCollection<ChannelLink>(this.ChannelLinks.Select(t => t.Clone()).Cast<ChannelLink>())
            };
        }

        [Description("The token provided by the Discord API to allow access to the bot. This setting can be changed while the server is running and will in that case trigger a reconnection to Discord."), Category("Bot Configuration")]
        public string BotToken { get; set; }

        [Description("The name of the Eco server, overriding the name configured within Eco. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerName { get; set; }

        [Description("The description of the Eco server, overriding the description configured within Eco. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerDescription { get; set; }

        [Description("The logo of the server as a URL. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerLogo { get; set; }

        [Description("IP of the server. Overrides the automatically detected IP. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerIP { get; set; }

        private ObservableCollection<DiscordPlayerConfig> _playerConfigs = new ObservableCollection<DiscordPlayerConfig>();

        [Description("A mapping from user to user config parameters. This setting can be changed while the server is running.")]
        public ObservableCollection<DiscordPlayerConfig> PlayerConfigs
        {
            get
            {
                return _playerConfigs;
            }
            set
            {
                _playerConfigs = value;
            }
        }

        [Description("Channels to connect together. This setting can be changed while the server is running."), Category("Channel Configuration")]
        public ObservableCollection<ChannelLink> ChannelLinks { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Enables debugging output to the console. This setting can be changed while the server is running."), Category("Debugging")]
        public bool Debug { get; set; } = false;

        [Description("Enables logging of chat messages into the file at Chatlog Path. This setting can be changed while the server is running."), Category("Chatlog Configuration")]
        public bool LogChat { get; set; } = false;

        [Description("The path to the chatlog file, including file name and extension. This setting can be changed while the server is running, but the existing chatlog will not transfer."), Category("Chatlog Configuration")]
        public string ChatlogPath { get; set; } = Directory.GetCurrentDirectory() + "\\Logs\\DiscordLinkChatlog.txt";

        [Description("The Eco chat channel to use for commands that outputs public messages, excluding the initial # character. This setting can be changed while the server is running."), Category("Command Settings")]
        public string EcoCommandChannel { get; set; } = DefaultValues.EcoCommandChannel;

        [Description("The message to use for the /DiscordInvite command. The invite link is fetched from the network config and will replace the token " + DiscordLink.InviteCommandLinkToken + ". This setting can be changed while the server is running."), Category("Command Settings")]
        public string InviteMessage { get; set; } = DefaultValues.InviteMessage;
    }

    public class DiscordPlayerConfig : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        [Description("ID of the user")]
        public string Username { get; set; }

        private DiscordChannelIdentifier _defaultChannel = new DiscordChannelIdentifier();
        public DiscordChannelIdentifier DefaultChannel
        {
            get { return _defaultChannel; }
            set { _defaultChannel = value; }
        }

        public class DiscordChannelIdentifier
        {
            public string Guild { get; set; }
            public string Channel { get; set; }
        }
    }

    public class ChannelLink : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        [Description("Discord Guild channel is in by name or ID. Case sensitive.")]
        public string DiscordGuild { get; set; }

        [Description("Discord Channel to use by name or ID. Case sensitive.")]
        public string DiscordChannel { get; set; }

        [Description("Eco Channel to use. Case sensitive.")]
        public string EcoChannel { get; set; }
    }
}
