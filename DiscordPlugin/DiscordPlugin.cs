using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Gameplay.Players;
using Eco.Gameplay.Stats;
using Eco.Gameplay.Systems.Chat;
using Eco.Mods.TechTree;
using Eco.Shared.Utils;

namespace Eco.Spoffy
{
    public class DiscordPlugin : IModKitPlugin, IInitializablePlugin, IConfigurablePlugin
    {
        private PluginConfig<DiscordConfig> configOptions;
        private DiscordClient _discordClient;
        private CommandsNextModule _commands;
        private string _currentToken;
        private string _status = "No Connection Attempt Made";
        
        public IPluginConfig PluginConfig
        {
            get { return configOptions; }
        }

        public DiscordConfig DiscordPluginConfig
        {
            get { return PluginConfig.GetConfig() as DiscordConfig; }
        }

        public string GetStatus()
        {
            return _status;
        }

        public void Initialize()
        {
            if (_discordClient == null) return;
            ConnectAsync();
        }

        public DiscordPlugin()
        {
            configOptions = new PluginConfig<DiscordConfig>("DiscordPluginSpoffy");
            SetUpClient();
        }

        private async Task<object> DisposeOfClient()
        {
            if (_discordClient != null)
            {
                await _discordClient.DisconnectAsync();
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
                    Token = _currentToken,
                    TokenType = TokenType.Bot
                });

                // Set up the client to use CommandsNext
                _commands = _discordClient.UseCommandsNext(new CommandsNextConfiguration()
                {
                    StringPrefix = "?"
                });

                _commands.RegisterCommands<DiscordDiscordCommands>();

                return true;
            }
            catch (Exception e)
            {
                Log.Write("ERROR: Unable to create the discord client. Error message was: " + e.Message + "\n");
                Log.Write("Backtrace: " + e.StackTrace);
            }

            return false;
        }

        public async Task<bool> RestartClient()
        {
            var result = SetUpClient();
            await ConnectAsync();
            return result;
        }

        public string[] GuildNames
        {
            get {
                if (_discordClient != null)
                {
                    return _discordClient.Guilds.Values.Select((guild => guild.Name)).ToArray();
                }
                return null;
            }
        }

        public DiscordGuild DefaultGuild
        {
            get {
                if (_discordClient != null)
                {
                    return _discordClient.Guilds.FirstOrDefault().Value;
                }

                return null;
            }
        }

        public string[] ChannelsInGuild(DiscordGuild guild)
        {
            return guild != null ? guild.Channels.Select(channel => channel.Name).ToArray() : new string[0];
        }

        public string[] ChannelsByGuildName(string guildName)
        {
            if (_discordClient == null) return new string[0];
            return ChannelsInGuild(GuildByName(guildName));
        }

        public DiscordGuild GuildByName(string name)
        {
            if (_discordClient == null) return null;
            return _discordClient.Guilds.Values.FirstOrDefault(guild => guild.Name == name);

        }

        public DiscordChannel ChannelByName(string guildName, string channelName)
        {
            var guild = GuildByName(guildName);
            return ChannelByName(guild, channelName);
        }

        public DiscordChannel ChannelByName(DiscordGuild guild, string channelName)
        {
            if (guild == null)
            {
                return null;
            }

            return guild.Channels.FirstOrDefault(channel => channel.Name == channelName);
        }

        public async Task<string> SendMessage(string message, string channelName, string guildName)
        {
            if (_discordClient == null) return "No discord client";
            var guild = GuildByName(guildName);
            if (guild == null) return "No guild of that name found";

            var channel = guild.Channels.FirstOrDefault(currChannel => currChannel.Name == channelName);
            return await SendMessage(message, channel);
        }
        
        public async Task<string> SendMessage(string message, DiscordChannel channel)
        {
            if (_discordClient == null) return "No discord client";
            if (channel == null) return "No channel of that name found in that guild";

            await _discordClient.SendMessageAsync(channel, message);
            return "Message sent successfully!";
        }

        public async Task<string> SendMessageAsUser(string message, User user, string channelName, string guildName)
        {
            return await SendMessage(String.Format("*{0}*: {1}", user.Name, message), channelName, guildName);
        }
        
        public async Task<String> SendMessageAsUser(string message, User user, DiscordChannel channel)
        {
            return await SendMessage(String.Format("*{0}*: {1}", user.Name, message), channel);
        }
        
        public async Task<object> ConnectAsync()
        {
            try
            {
                _status = "Attempting connection...";
                await _discordClient.ConnectAsync();
                Log.Write("Connected to Discord.\n");
                _status = "Connection successful";
            } 
            catch (Exception e)
            {
                Log.Write("Error connecting to discord: " + e.Message + "\n");
                _status = "Connection failed";
            }

            return null;
        }

        public static DiscordPlugin Obj
        {
            get { return PluginManager.GetPlugin<DiscordPlugin>(); }
        }
        
        public object GetEditObject()
        {
            return configOptions.Config;
        }

        public void OnEditObjectChanged(object o, string param)
        {
            
            configOptions.Save();
            if (DiscordPluginConfig.BotToken != _currentToken)
            {
                //Reinitialise client.
                Log.Write("Discord Token changed, reinitialising client.\n");
                RestartClient();
            }
        }
        
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
            configOptions.Save();
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

            return ChannelByName(playerConfig.DefaultChannel.Guild, playerConfig.DefaultChannel.Channel);
        }
        
        
        public void SetDefaultChannelForPlayer(string identifier, string guildName, string channelName)
        {
            var playerConfig = GetOrCreatePlayerConfig(identifier);
            playerConfig.DefaultChannel.Guild = guildName;
            playerConfig.DefaultChannel.Channel = channelName;
            SavePlayerConfig();
        }
    }

    public class DiscordConfig
    {
        [Description("The token provided by the Discord API to allow access to the bot"), Category("Bot Configuration")]
        public string BotToken { get; set; }
        
        [Description("The name of the Eco server, overriding the name configured within Eco."), Category("Server Details")]
        public string ServerName { get; set; }
        
        [Description("The description of the Eco server, overriding the description configured within Eco."), Category("Server Details")]
        public string ServerDescription { get; set; }
        
        [Description("The logo of the server as a URL."), Category("Server Details")]
        public string ServerLogo { get; set; }

        private List<DiscordPlayerConfig> _playerConfigs = new List<DiscordPlayerConfig>();
        
        [Description("A mapping from user to user config parameters.")]
        public List<DiscordPlayerConfig> PlayerConfigs {
            get {
                return _playerConfigs;
            }
            set
            {
                _playerConfigs = value;
            }
        }
    }

    public class DiscordPlayerConfig
    {
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
    
}