using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Gameplay.Players;
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

        public IPluginConfig PluginConfig
        {
            get { return configOptions; }
            private set { }
        }

        public string GetStatus()
        {
            return "Who knows";
        }

        public void Initialize()
        {
            if (_discordClient == null) return;
            ConnectAsync();
        }

        public DiscordPlugin()
        {
            // Loading the configuration
            configOptions = new PluginConfig<DiscordConfig>("DiscordPluginSpoffy");
            configOptions.Config.BotToken = configOptions.Config.BotToken ?? "";

            // Create the new client
            _discordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = configOptions.Config.BotToken,
                TokenType = TokenType.Bot
            });

            // Set up the client to use CommandsNext
            _commands = _discordClient.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefix = "?"
            });
            
            _commands.RegisterCommands<DiscordDiscordCommands>();
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
                    return _discordClient.Guilds.First().Value;
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
            return _discordClient.Guilds.Values.First(guild => guild.Name == name);
        }

        public async Task<string> SendMessage(string message, string channelName, string guildName)
        {
            if (_discordClient == null) return "No discord client";
            var guild = GuildByName(guildName);
            if (guild == null) return "No guild of that name found";

            var channel = guild.Channels.First(currChannel => currChannel.Name == channelName);
            if (channel == null) return "No channel of that name found in that guild";

            await this._discordClient.SendMessageAsync(channel, message);
            return "Message sent successfully!";
        }

        public async Task<String> SendMessageAsUser(string message, string channelName, string guildName, User user)
        {
            return await SendMessage(String.Format("*{0}*: {1}", user.Name, message), channelName, guildName);
        }
        
        public async void ConnectAsync()
        {
            try
            {
                await _discordClient.ConnectAsync();
                Log.Write("Connected to Discord.");
            } 
            catch (Exception e)
            {
                Log.Write("Error connecting to discord: " + e.Message);
            }
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
        }
    }

    public class DiscordConfig
    {
        [Description("The token provided by the Discord API to allow access to the bot"), Category("Bot Configuration")]
        public string BotToken { get; set; }
    }
}