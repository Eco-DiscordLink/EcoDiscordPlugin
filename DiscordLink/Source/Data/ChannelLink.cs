using DSharpPlus.Entities;
using Eco.EW.Tools;
using Eco.Shared.Serialization;
using System;
using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public enum GlobalMentionPermission
    {
        AnyUser,        // Any user may use mentions
        Admin,          // Only admins may use mentions
        Forbidden       // All use of mentions is forbidden
    };

    public enum ChatSyncDirection
    {
        DiscordToEco,   // Send Discord messages to Eco
        EcoToDiscord,   // Send Eco messages to Discord
        Duplex,         // Send Discord messages to Eco and Eco messages to Discord
    }

    public enum CurrencyTypeDisplayCondition
    {
        Never,          // Never show the currency type
        MintedExists,   // Only show the currency type if a minted currency exists
        NoMintedExists, // Do NOT show the currency type if a minted currency exists
        Always,         // Always show the curreny type
    }

    public abstract class DiscordTarget
    {
        public abstract bool IsValid();
    }

    public class UserLink : DiscordTarget
    {
        public UserLink(DiscordMember member)
        {
            this.Member = member;
        }

        public DiscordMember Member { get; set; }

        public override bool IsValid() => Member != null;
    }

    public class ChannelLink : DiscordTarget, ICloneable
    {
        private const string DISCORD_CHANNEL_PROPERTY_DEPRECATED = "_This field is not supported anymore. Please use DiscordChannelId instead_";

        [Browsable(false), JsonIgnore]
        public DiscordChannel Channel { get; private set; } = null;

        [Description("Discord Channel by ID.")]
        [TypeConverter(typeof(DiscordChannelPropertyConverter))]
        public ulong DiscordChannelId { get; set; } = 0;

        // Legacy Property, to support migrating to the new ID-Based Solution.
        [Browsable(false)]
        [Obsolete("Please use DiscordChannelId instead. This only exists for migration of the old Format.")]
        public string DiscordChannel { get; set;  } = DISCORD_CHANNEL_PROPERTY_DEPRECATED;

        public override string ToString()
        {
            return IsValid() ? $"#{Channel.Name}" : $"<Unknown Channel Name> ({DiscordChannelId})";
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override bool IsValid() => DiscordChannelId != 0 && Channel != null;

        public virtual bool Initialize()
        {

            MigrateOldChannelName();
            
            if (DiscordChannelId == 0)
                return false;

            DiscordChannel channel = DiscordLink.Obj.Client.Guild.GetChannel(DiscordChannelId);
            if (channel == null)
                return false;

            Channel = channel;
            return true;
        }

        // Migrates the old Property 'DiscordChannel' to the 'DiscordChannelId' Property. 
        private void MigrateOldChannelName()
        {
#pragma warning disable CS0618 // Migration
            if (DiscordChannelId != 0 || DiscordChannel.Equals(DISCORD_CHANNEL_PROPERTY_DEPRECATED)) return;

            if (ulong.TryParse(DiscordChannel, out ulong channelId))
            {
                DiscordChannelId = channelId;
            }
            else
            {
               DiscordChannelId = DiscordChannelId = DiscordLink.Obj.Client.ChannelByNameOrID(DiscordChannel)?.Id ?? 0;
            }
            // Mark as Deprecated and show note in config file.
            DiscordChannel = DISCORD_CHANNEL_PROPERTY_DEPRECATED;
#pragma warning restore CS0618 // // Migration
        }

        public virtual bool MakeCorrections()
        {
            return false;
        }

        public bool IsChannel(DiscordChannel channel)
        {  
            return DiscordChannelId == channel.Id;
        }
    }

    public class EcoChannelLink : ChannelLink
    {
        [Description("Eco channel to use (omit # prefix).")]
        public string EcoChannel { get; set; } = string.Empty;
        public override string ToString()
        {
            string discordChannelName = IsValid() ? Channel.Name : "<Invalid Channel>";
            return $"#{discordChannelName} <--> {EcoChannel}";
        }

        public override bool IsValid() => base.IsValid() && !string.IsNullOrWhiteSpace(EcoChannel);

        public override bool MakeCorrections()
        {
            bool correctionMade = base.MakeCorrections();
            string original = EcoChannel;
            EcoChannel = EcoChannel.Trim('#');
            if (EcoChannel != original)
            {
                correctionMade = true;
                Logger.Info($"Corrected Eco channel name linked with Discord Channel name/ID \"{Channel?.Name ?? DiscordChannelId.ToString()}\" from \"{original}\" to \"{EcoChannel}\"");
            }
            return correctionMade;
        }
    }

    public class ChatLinkMentionPermissions
    {
        public bool AllowChannelMentions { get; set; }
        public bool AllowRoleMentions { get; set; }
        public bool AllowMemberMentions { get; set; }
    }

    public class ChatChannelLink : EcoChannelLink
    {
        [Browsable(false)]
        public ChatLinkMentionPermissions MentionPermissions => new()
        {
            AllowRoleMentions = AllowRoleMentions,
            AllowMemberMentions = AllowUserMentions,
            AllowChannelMentions = AllowChannelMentions,
        };

        [Description("Allow mentions of usernames to be forwarded from Eco to the Discord channel.")]
        public bool AllowUserMentions { get; set; } = true;

        [Description("Allow mentions of roles to be forwarded from Eco to the Discord channel.")]
        public bool AllowRoleMentions { get; set; } = true;

        [Description("Allow mentions of channels to be forwarded from Eco to the Discord channel.")]
        public bool AllowChannelMentions { get; set; } = true;

        [Description("Sets which direction chat should synchronize in.")]
        public ChatSyncDirection Direction { get; set; } = ChatSyncDirection.Duplex;

        [Description("Permissions for who is allowed to forward mentions of @here or @everyone from Eco to the Discord channel.")]
        public GlobalMentionPermission HereAndEveryoneMentionPermission { get; set; } = GlobalMentionPermission.Forbidden;

        [Description("Determines if timestamps should be added to each message forwarded to Discord.")]
        public bool UseTimestamp { get; set; } = false;
    }

    public class ServerLogFeedChannelLink : ChannelLink
    {
        [Description("Determines what log message types will be printed to the channel log. All message types below the selected one will be printed as well.")]
        public Logger.LogLevel LogLevel { get; set; } = Logger.LogLevel.Information;
    }

    public class PlayerListChannelLink : ChannelLink
    {
        [Description("Display the number of online players in the message.")]
        public bool UsePlayerCount { get; set; } = true;

        [Description("Display how long the player has been logged in for.")]
        public bool UseLoggedInTime { get; set; } = false;
    }

    public class CurrencyChannelLink : ChannelLink
    {
        [Description("Conditions for showing minted currencies.")]
        public CurrencyTypeDisplayCondition UseMintedCurrency { get; set; } = CurrencyTypeDisplayCondition.MintedExists;

        [Description("Conditions for showing personal currencies.")]
        public CurrencyTypeDisplayCondition UsePersonalCurrency { get; set; } = CurrencyTypeDisplayCondition.NoMintedExists;

        [Description("Max minted currencies to show.")]
        public int MaxMintedCount { get; set; } = DLConfig.DefaultValues.MaxMintedCurrencies;

        [Description("Max personal currencies to show.")]
        public int MaxPersonalCount { get; set; } = DLConfig.DefaultValues.MaxPersonalCurrencies;

        [Description("Max currency holders to show.")]
        public int MaxTopCurrencyHolderCount { get; set; } = DLConfig.DefaultValues.MaxTopCurrencyHolderCount;

        [Description("Display the amount of trades for each currency.")]
        public bool UseTradeCount { get; set; } = false;

        [Description("Display the backing information (if such exists) for each currency.")]
        public bool UseBackingInfo { get; set; } = false;
    }

    public class ServerInfoChannel : ChannelLink
    {
        [Description("Display the server name.")]
        public bool UseName { get; set; } = true;

        [Description("Display the server description.")]
        public bool UseDescription { get; set; } = false;

        [Description("Display the server logo.")]
        public bool UseLogo { get; set; } = true;

        [Description("Display the connection information for the game server.")]
        public bool UseConnectionInfo { get; set; } = true;

        [Description("Display the web server address.")]
        public bool UseWebServerAddress { get; set; } = true;

        [Description("Display the number of online players.")]
        public bool UsePlayerCount { get; set; } = false;

        [Description("Display a list of online players.")]
        public bool UsePlayerList { get; set; } = true;

        [Description("Display how long the players in the player list has been logged in for.")]
        public bool UsePlayerListLoggedInTime { get; set; } = false;

        [Description("Display how long the players in the player list has left before they get exhausted.")]
        public bool UsePlayerListExhaustionTime { get; set; } = false;

        [Description("Display the current ingame time.")]
        public bool UseIngameTime { get; set; } = true;

        [Description("Display the time remaining until meteor impact.")]
        public bool UseTimeRemaining { get; set; } = true;

        [Description("Display the current server time.")]
        public bool UseServerTime { get; set; } = true;

        [Description("Display the server time at which exhaustion resets.")]
        public bool UseExhaustionResetServerTime { get; set; } = false;

        [Description("Display the time remaining before exhaustion resets.")]
        public bool UseExhaustionResetTimeLeft { get; set; } = false;

        [Description("Display the amount of exhausted players.")]
        public bool UseExhaustedPlayerCount { get; set; } = false;

        [Description("Display the number of active elections.")]
        public bool UseElectionCount { get; set; } = false;

        [Description("Display a list of all active elections.")]
        public bool UseElectionList { get; set; } = true;

        [Description("Display the number of active laws.")]
        public bool UseLawCount { get; set; } = false;

        [Description("Display a list of all active laws.")]
        public bool UseLawList { get; set; } = true;
    }
}
