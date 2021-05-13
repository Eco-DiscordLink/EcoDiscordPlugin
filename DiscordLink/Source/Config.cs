using DSharpPlus.CommandsNext.Attributes;
using Eco.Core.Plugins;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Description = System.ComponentModel.DescriptionAttribute;

namespace Eco.Plugins.DiscordLink
{
    public sealed class DLConfig
    {
        public enum VerificationFlags
        {
            Static = 1 << 0,
            ChannelLinks = 1 << 1,
            BotData = 1 << 2,
            All = ~0
        }

        public static class DefaultValues
        {
            public static Logger.LogLevel PluginLogLevel = Logger.LogLevel.Information;
            public static Microsoft.Extensions.Logging.LogLevel BackendLogLevel = Microsoft.Extensions.Logging.LogLevel.None;
            public static readonly string[] AdminRoles = { "Admin", "Administrator", "Moderator" };
            public const string DiscordCommandPrefix = "?";
            public const string EcoCommandOutputChannel = "General";
            public const string InviteMessage = "Join us on Discord!\n" + DLConstants.INVITE_COMMAND_TOKEN;
            public const string EcoBotName = "DiscordLink";
            public const int MaxMintedCurrencies = 1;
            public const int MaxPersonalCurrencies = 3;
            public const int MaxTopCurrencyHolderCount = 3;
            public const int MaxTrackedTradesPerUser = 5;
            public const DiscordLinkEmbed.EmbedSize MinEmbedSizeForFooter = DiscordLinkEmbed.EmbedSize.Medium;
        }

        public static readonly DLConfig Instance = new DLConfig();
        public static DLConfigData Data { get { return Instance._config.Config; } }
        public static List<ChannelLink> ChannelLinks { get { return Instance._channelLinks; } }
        public PluginConfig<DLConfigData> PluginConfig { get { return Instance._config; } }

        public static ChannelLink ChannelLinkForDiscordChannel(string discordGuildName, string discordChannelName) =>
            ChannelLinks.FirstOrDefault(link
                => link.IsValid()
                && link.DiscordServer.EqualsCaseInsensitive(discordGuildName)
                && link.DiscordChannel.EqualsCaseInsensitive(discordChannelName));

        public static ChatChannelLink ChatLinkForEcoChannel(string ecoChannelName) => Data.ChatChannelLinks.FirstOrDefault(link
                => link.IsValid()
                && link.EcoChannel.EqualsCaseInsensitive(ecoChannelName));

        public static ChatChannelLink ChatLinkForDiscordChannel(string discordGuildName, string discordChannelName) =>
            Data.ChatChannelLinks.FirstOrDefault(link
                => link.IsValid()
                && link.DiscordServer.EqualsCaseInsensitive(discordGuildName)
                && link.DiscordChannel.EqualsCaseInsensitive(discordChannelName));

        public delegate Task OnConfigChangedDelegate(object sender, EventArgs e);
        public event OnConfigChangedDelegate OnConfigChanged;
        public event EventHandler OnConfigSaved;

        public const string InviteCommandLinkToken = "[LINK]";

        private DLConfigData _prevConfig; // Used to detect differences when the config is saved

        private PluginConfig<DLConfigData> _config;
        private readonly List<ChannelLink> _channelLinks = new List<ChannelLink>();

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static DLConfig()
        {
        }

        private DLConfig()
        {
        }

        public void Initialize()
        {
            _config = new PluginConfig<DLConfigData>("DiscordLink");
            _prevConfig = (DLConfigData)Data.Clone();

            Data.ChatChannelLinks.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.TradeFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.CraftingFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.ServerStatusFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.PlayerStatusFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.ElectionFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.ServerInfoDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.WorkPartyDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.PlayerListDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.ElectionDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.CurrencyDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.SnippetInputChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            Data.DiscordCommandChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };

            BuildChanneLinkList();
        }

        public void HandleCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            Logger.Debug("Config Changed");

            if (args.Action == NotifyCollectionChangedAction.Add
                || args.Action == NotifyCollectionChangedAction.Remove
                || args.Action == NotifyCollectionChangedAction.Replace)
            {
                HandleConfigChanged();
            }
            else
            {
                Save(); // Remove isn't reported properly so we should save on other events to make sure the changes are saved
            }
        }

        public void HandleConfigChanged()
        {
            // Do not verify if change occurred as this function is going to be called again in that case
            // Do not verify the config in case the bot token has been changed, as the client will be restarted and that will trigger verification
            bool tokenChanged = Data.BotToken != _prevConfig.BotToken;
            bool correctionMade = !Save();

            BuildChanneLinkList();

            if (tokenChanged)
            {
                Logger.Info("Discord Bot Token changed - Restarting");
                bool restarted = false;
                SystemUtils.SynchronousThreadExecute(() => // Avoid deadlocks caused by calling async functions on the GUI thread
                {
                    restarted = DiscordLink.Obj.Restart().Result;
                });

                if (!restarted)
                    Logger.Info("Restart failed or a restart was already in progress");

                return; // The token changing will trigger a reset
            }

            if (!correctionMade) // If a correction was made, this function will be called again
            {
                VerifyConfig();

                // This function executes on the GUI thread and therefore async calls will trigger deadlocks.
                // We execute the callbacks on a separate joined thread to avoid these deadlocks.
                SystemUtils.SynchronousThreadExecute(() =>
                {
                    OnConfigChanged?.Invoke(this, EventArgs.Empty);
                });
            }
        }

        public bool Save() // Returns true if no correction was needed
        {
            bool correctionMade = false;

            // Eco Bot Name
            if (string.IsNullOrEmpty(Data.EcoBotName))
            {
                Data.EcoBotName = DefaultValues.EcoBotName;
                correctionMade = true;
            }

            // Discord Command Prefix
            if (Data.DiscordCommandPrefix != _prevConfig.DiscordCommandPrefix)
            {
                if (string.IsNullOrEmpty(Data.DiscordCommandPrefix))
                {
                    Data.DiscordCommandPrefix = DefaultValues.DiscordCommandPrefix;
                    correctionMade = true;

                    Logger.Info("Command prefix found empty - Resetting to default.");
                }
                Logger.Info("Command prefix changed - Restart required to take effect.");
            }

            // Channel Links
            foreach (ChannelLink link in _channelLinks)
            {
                if (link.MakeCorrections())
                {
                    correctionMade = true;
                }
            }

            // Max tracked trades per user
            if(Data.MaxTrackedTradesPerUser < 0)
            {
                Data.MaxTrackedTradesPerUser = DLConfig.DefaultValues.MaxTrackedTradesPerUser;
            }

            // Invite Message
            if (string.IsNullOrEmpty(Data.InviteMessage))
            {
                Data.InviteMessage = DefaultValues.InviteMessage;
                correctionMade = true;
            }

            // Currency channels
            foreach(CurrencyChannelLink link in Data.CurrencyDisplayChannels)
            {
                if (link.MaxMintedCount < 0)
                {
                    link.MaxMintedCount = DefaultValues.MaxMintedCurrencies;
                    correctionMade = true;
                }

                if (link.MaxPersonalCount < 0)
                {
                    link.MaxPersonalCount = DefaultValues.MaxPersonalCurrencies;
                    correctionMade = true;
                }

                if(link.MaxTopCurrencyHolderCount < 0 || link.MaxTopCurrencyHolderCount > DLConstants.MAX_TOP_CURRENCY_HOLDER_DISPLAY_LIMIT)
                {
                    link.MaxTopCurrencyHolderCount = DefaultValues.MaxTopCurrencyHolderCount;
                    correctionMade = true;
                }
            }

            _config.SaveAsync().Wait();
            OnConfigSaved?.Invoke(this, EventArgs.Empty);
            _prevConfig = (DLConfigData)Data.Clone();

            return !correctionMade;
        }

        public void VerifyConfig(VerificationFlags verificationFlags = VerificationFlags.All)
        {
            List<string> errorMessages = new List<string>();
            if (DiscordLink.Obj.Client.ConnectionStatus != DLDiscordClient.ConnectionState.Connected)
            {
                errorMessages.Add("[General Verification] Discord Client not connected.");
            }

            if (verificationFlags.HasFlag(VerificationFlags.Static))
            {
                // Bot Token
                if (string.IsNullOrWhiteSpace(Data.BotToken))
                {
                    errorMessages.Add("[Bot Token] Bot token not configured. See Github page for install instructions.");
                }

                if (!string.IsNullOrWhiteSpace(Data.InviteMessage) && !Data.InviteMessage.ContainsCaseInsensitive(DLConstants.INVITE_COMMAND_TOKEN))
                {
                    errorMessages.Add("[Invite Message] Message does not contain the invite link token " + DLConstants.INVITE_COMMAND_TOKEN + ".");
                }

                // Report errors
                if (errorMessages.Count <= 0)
                {
                    Logger.Info("Static configuration verification completed without errors");
                }
                else
                {
                    string concatenatedMessages = "";
                    foreach (string message in errorMessages)
                    {
                        concatenatedMessages += message + "\n";
                    }
                    Logger.Error("Static configuration errors detected!\n" + concatenatedMessages.Trim());
                }
            }

            if (DiscordLink.Obj.Client.ConnectionStatus == DLDiscordClient.ConnectionState.Connected)
            {
                // Discord guild and channel information isn't available the first time this function is called
                if (verificationFlags.HasFlag(VerificationFlags.ChannelLinks) && ChannelLinks.Count > 0)
                {
                    List<ChannelLink> verifiedLinks = new List<ChannelLink>();
                    foreach (ChannelLink link in _channelLinks)
                    {
                        if (link.Verify())
                        {
                            if (!verifiedLinks.Contains(link))
                            {
                                verifiedLinks.Add(link);
                                Logger.Info($"Channel Link Verified: {link}");
                            }
                        }
                    }

                    if (verifiedLinks.Count >= _channelLinks.Count)
                    {
                        Logger.Info("All channel links sucessfully verified");
                    }
                    else
                    {
                        List<ChannelLink> unverifiedLinks = new List<ChannelLink>();
                        foreach (ChannelLink link in _channelLinks)
                        {
                            if (!link.IsValid()) continue;

                            if (!verifiedLinks.Contains(link))
                                unverifiedLinks.Add(link);
                        }

                        if (unverifiedLinks.Count > 0)
                            Logger.Info($"Unverified channels detected:\n * " + string.Join("\n * ", unverifiedLinks));
                    }
                }

                if (verificationFlags.HasFlag(VerificationFlags.BotData))
                {
                    if (!DiscordLink.Obj.Client.BotHasIntent(DSharpPlus.DiscordIntents.GuildMembers))
                        Logger.Warning("Bot is not configured to allow reading of full server member list as it lacks the Server Members Intent. Some features will be unavailable. See install instructions for help with adding intents.");
                }
            }
        }

        private void BuildChanneLinkList()
        {
            _channelLinks.Clear();
            _channelLinks.AddRange(_config.Config.ChatChannelLinks);
            _channelLinks.AddRange(_config.Config.TradeFeedChannels);
            _channelLinks.AddRange(_config.Config.CraftingFeedChannels);
            _channelLinks.AddRange(_config.Config.ServerStatusFeedChannels);
            _channelLinks.AddRange(_config.Config.PlayerStatusFeedChannels);
            _channelLinks.AddRange(_config.Config.ElectionFeedChannels);
            _channelLinks.AddRange(_config.Config.ServerInfoDisplayChannels);
            _channelLinks.AddRange(_config.Config.WorkPartyDisplayChannels);
            _channelLinks.AddRange(_config.Config.PlayerListDisplayChannels);
            _channelLinks.AddRange(_config.Config.ElectionDisplayChannels);
            _channelLinks.AddRange(_config.Config.CurrencyDisplayChannels);
            _channelLinks.AddRange(_config.Config.SnippetInputChannels);
            _channelLinks.AddRange(_config.Config.DiscordCommandChannels);
        }
    }

    public class DLConfigData : ICloneable
    {
        public object Clone() // Be careful not to change the original object here as that will trigger endless recursion.
        {
            return new DLConfigData
            {
                BotToken = this.BotToken,
                EcoBotName = this.EcoBotName,
                DiscordCommandPrefix = this.DiscordCommandPrefix,
                ServerName = this.ServerName,
                ServerDescription = this.ServerDescription,
                ServerLogo = this.ServerLogo,
                ServerAddress = this.ServerAddress,
                LogLevel = this.LogLevel,
                MaxTrackedTradesPerUser = this.MaxTrackedTradesPerUser,
                InviteMessage = this.InviteMessage,
                AdminRoles = new ObservableCollection<string>(this.AdminRoles.Select(t => t.Clone()).Cast<string>()),
                ChatChannelLinks = new ObservableCollection<ChatChannelLink>(this.ChatChannelLinks.Select(t => t.Clone()).Cast<ChatChannelLink>()),
                TradeFeedChannels = new ObservableCollection<ChannelLink>(this.TradeFeedChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                CraftingFeedChannels = new ObservableCollection<ChannelLink>(this.CraftingFeedChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                ServerStatusFeedChannels = new ObservableCollection<ChannelLink>(this.ServerStatusFeedChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                PlayerStatusFeedChannels = new ObservableCollection<ChannelLink>(this.PlayerStatusFeedChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                ElectionFeedChannels = new ObservableCollection<ChannelLink>(this.ElectionFeedChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                ServerInfoDisplayChannels = new ObservableCollection<ServerInfoChannel>(this.ServerInfoDisplayChannels.Select(t => t.Clone()).Cast<ServerInfoChannel>()),
                WorkPartyDisplayChannels = new ObservableCollection<ChannelLink>(this.WorkPartyDisplayChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                PlayerListDisplayChannels = new ObservableCollection<PlayerListChannelLink>(this.PlayerListDisplayChannels.Select(t => t.Clone()).Cast<PlayerListChannelLink>()),
                ElectionDisplayChannels = new ObservableCollection<ChannelLink>(this.ElectionDisplayChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                CurrencyDisplayChannels = new ObservableCollection<CurrencyChannelLink>(this.CurrencyDisplayChannels.Select(t => t.Clone()).Cast<CurrencyChannelLink>()),
                SnippetInputChannels = new ObservableCollection<ChannelLink>(this.SnippetInputChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                DiscordCommandChannels = new ObservableCollection<ChannelLink>(this.DiscordCommandChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
            };
        }

        [Description("The token provided by the Discord API to allow access to the bot. This setting can be changed while the server is running and will in that case trigger a reconnection to Discord."), Category("Bot Configuration")]
        public string BotToken { get; set; }

        [Description("The name of the bot user in Eco. This setting can be changed while the server is running, but changes will only take effect after a world reset."), Category("Bot Configuration")]
        public string EcoBotName { get; set; } = DLConfig.DefaultValues.EcoBotName;

        [Description("The prefix to put before commands in order for the Discord bot to recognize them as such. This setting requires a plugin restart to take effect."), Category("Command Settings")]
        public string DiscordCommandPrefix { get; set; } = DLConfig.DefaultValues.DiscordCommandPrefix;

        [Description("The roles recognized as having admin permissions on Discord. This setting requires a plugin restart to take effect."), Category("Command Settings")]
        public ObservableCollection<string> AdminRoles { get; set; } = new ObservableCollection<string>(DLConfig.DefaultValues.AdminRoles);

        [Description("The name of the Eco server, overriding the name configured within Eco. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerName { get; set; }

        [Description("The description of the Eco server, overriding the description configured within Eco. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerDescription { get; set; }

        [Description("The logo of the server as a URL. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerLogo { get; set; }

        [Description("The address (URL or IP) of the server. Overrides the automatically detected IP. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerAddress { get; set; }

        [Description("Discord and Eco Channels to connect together. This setting can be changed while the server is running."), Category("Feeds")]
        public ObservableCollection<ChatChannelLink> ChatChannelLinks { get; set; } = new ObservableCollection<ChatChannelLink>();

        [Description("Discord Channels in which trade events will be posted. This setting can be changed while the server is running."), Category("Feeds")]
        public ObservableCollection<ChannelLink> TradeFeedChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which crafting events will be posted. This setting can be changed while the server is running."), Category("Feeds")]
        public ObservableCollection<ChannelLink> CraftingFeedChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which server status events will be posted. This setting can be changed while the server is running."), Category("Feeds")]
        public ObservableCollection<ChannelLink> ServerStatusFeedChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which player status events will be posted. This setting can be changed while the server is running."), Category("Feeds")]
        public ObservableCollection<ChannelLink> PlayerStatusFeedChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which election events will be posted. This setting can be changed while the server is running."), Category("Feeds")]
        public ObservableCollection<ChannelLink> ElectionFeedChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which to keep the Server Info display. DiscordLink will post one server info message in these channel and keep it updated through edits. This setting can be changed while the server is running."), Category("Displays")]
        public ObservableCollection<ServerInfoChannel> ServerInfoDisplayChannels { get; set; } = new ObservableCollection<ServerInfoChannel>();

        [Description("Discord channels in which to keep ongoing work parties. DiscordLink will post messages in these channel and keep them updated through edits. This setting can be changed while the server is running."), Category("Displays")]
        public ObservableCollection<ChannelLink> WorkPartyDisplayChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which to keep the Player List display. DiscordLink will post one Player List message in these channel and keep it updated through edits. This setting can be changed while the server is running."), Category("Displays")]
        public ObservableCollection<PlayerListChannelLink> PlayerListDisplayChannels { get; set; } = new ObservableCollection<PlayerListChannelLink>();

        [Description("Discord channels in which to keep the Election display. DiscordLink will post election messages in these channel and keep it updated through edits. This setting can be changed while the server is running."), Category("Displays")]
        public ObservableCollection<ChannelLink> ElectionDisplayChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which to keep the currency display. DiscordLink will post currency messages in these channel and keep it updated through edits. This setting can be changed while the server is running."), Category("Displays")]
        public ObservableCollection<CurrencyChannelLink> CurrencyDisplayChannels { get; set; } = new ObservableCollection<CurrencyChannelLink>();

        [Description("Discord channels in which to search for snippets for the Snippet command. This setting can be changed while the server is running."), Category("Inputs")]
        public ObservableCollection<ChannelLink> SnippetInputChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which to allow commands. If no channels are specified, commands will be allowed in all channels. This setting can be changed while the server is running."), Category("Command Settings")]
        public ObservableCollection<ChannelLink> DiscordCommandChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Max amount of tracked trades allowed per user. This setting can be changed while the server is running, but does not apply retroactively."), Category("Command Settings")]
        public int MaxTrackedTradesPerUser { get; set; } = DLConfig.DefaultValues.MaxTrackedTradesPerUser;

        [Description("Determines what message types will be printed to the server log. All message types below the selected one will be printed as well. This setting can be changed while the server is running."), Category("Miscellaneous")]
        public Logger.LogLevel LogLevel { get; set; } = DLConfig.DefaultValues.PluginLogLevel;

        [Description("Determines what backend message types will be printed to the server log. All message types below the selected one will be printed as well. This setting requires a plugin restart to take effect."), Category("Miscellaneous")]
        public Microsoft.Extensions.Logging.LogLevel BackendLogLevel { get; set; } = DLConfig.DefaultValues.BackendLogLevel;

        [Description("Determines what sizes of embeds to show the footer containing meta information about posted embeds. All embeds of sizes bigger than the selected one will have footers as well. This setting can be changed while the server is running."), Category("Miscellaneous")]
        public DiscordLinkEmbed.EmbedSize MinEmbedSizeForFooter { get; set; } = DLConfig.DefaultValues.MinEmbedSizeForFooter;

        [Description("The message to use for the /DiscordInvite command. The invite link is fetched from the network config and will replace the token " + DLConstants.INVITE_COMMAND_TOKEN + ". This setting can be changed while the server is running."), Category("Command Settings")]
        public string InviteMessage { get; set; } = DLConfig.DefaultValues.InviteMessage;
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
            public string Guild { get; set; } = string.Empty;
            public string Channel { get; set; } = string.Empty;
        }
    }
}
