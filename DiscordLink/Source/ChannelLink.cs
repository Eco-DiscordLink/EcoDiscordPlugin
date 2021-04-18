using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Serialization;
using Eco.Shared.Utils;
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
        [Browsable(false), JsonIgnore]
        public DiscordGuild Guild { get; private set; } = null;

        [Browsable(false), JsonIgnore]
        public DiscordChannel Channel { get; private set; } = null;

        [Description("Discord Server by name or ID.")]
        public string DiscordServer { get; set; } = string.Empty;

        [Description("Discord Channel by name or ID.")]
        public string DiscordChannel { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{DiscordServer} - {DiscordChannel}";
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override bool IsValid() => !string.IsNullOrWhiteSpace(DiscordServer) && !string.IsNullOrWhiteSpace(DiscordChannel) && Guild != null && Channel != null;

        public virtual bool Verify()
        {
            if (string.IsNullOrWhiteSpace(DiscordServer) || string.IsNullOrWhiteSpace(DiscordChannel))
                return false;

            DiscordGuild guild = DiscordLink.Obj.Client.GuildByNameOrID(DiscordServer);
            if (guild == null)
                return false; // The channel will always fail if the guild fails

            DiscordChannel channel = Guild.ChannelByNameOrID(DiscordChannel);
            if (guild == null)
                return false;

            Guild = guild;
            Channel = channel;
            return true;
        }

        public virtual bool MakeCorrections()
        {
            if (string.IsNullOrWhiteSpace(DiscordChannel))
                return false;

            bool correctionMade = false;
            string original = DiscordChannel;
            string channelNameLower = DiscordChannel.ToLower();
            if (DiscordChannel != channelNameLower) // Discord channels are always lowercase
                DiscordChannel = channelNameLower;

            if (DiscordChannel.Contains(" "))
                DiscordChannel = DiscordChannel.Replace(' ', '-'); // Discord channels always replace spaces with dashes

            if (DiscordChannel != original)
            {
                correctionMade = true;
                Logger.Info($"Corrected Discord channel name with Guild name/ID \"{DiscordServer}\" from \"{original}\" to \"{DiscordChannel}\"");
            }

            return correctionMade;
        }

        public bool IsGuild(DiscordGuild guild)
        {
            if (ulong.TryParse(DiscordServer, out ulong guildID))
                return guildID == guild.Id;

            return DiscordServer.EqualsCaseInsensitive(guild.Name);
        }

        public bool IsChannel(DiscordChannel channel)
        {
            if (ulong.TryParse(DiscordChannel, out ulong channelID))
                return channelID == channel.Id;

            return DiscordChannel.EqualsCaseInsensitive(channel.Name);
        }

        public bool HasGuildNameOrID(string guildNameOrID)
        {
            if (ulong.TryParse(DiscordServer, out ulong guildID) && ulong.TryParse(guildNameOrID, out ulong guildIDParam))
                return guildID == guildIDParam;

            return DiscordChannel.EqualsCaseInsensitive(guildNameOrID);
        }

        public bool HasChannelNameOrID(string channelNameOrID)
        {
            if (ulong.TryParse(DiscordChannel, out ulong channelID) && ulong.TryParse(channelNameOrID, out ulong channelIDParam))
                return channelID == channelIDParam;

            return DiscordChannel.EqualsCaseInsensitive(channelNameOrID);
        }
    }

    public class EcoChannelLink : ChannelLink
    {
        [Description("Eco channel to use (omit # prefix).")]
        public string EcoChannel { get; set; } = string.Empty;
        public override string ToString()
        {
            return $"{DiscordServer} #{DiscordChannel} <--> {EcoChannel}";
        }

        public override bool IsValid() => !string.IsNullOrWhiteSpace(DiscordServer) && !string.IsNullOrWhiteSpace(DiscordChannel) && !string.IsNullOrWhiteSpace(EcoChannel);

        public override bool MakeCorrections()
        {
            bool correctionMade = base.MakeCorrections();
            string original = EcoChannel;
            EcoChannel = EcoChannel.Trim('#');
            if (EcoChannel != original)
            {
                correctionMade = true;
                Logger.Info($"Corrected Eco channel name with Guild name/ID \"{DiscordServer}\" and Discord Channel name/ID \"{DiscordChannel}\" from \"{original}\" to \"{EcoChannel}\"");
            }
            return correctionMade;
        }
    }

    public class ChatChannelLink : EcoChannelLink
    {
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
        [Description("Display the server name in the message.")]
        public bool UseName { get; set; } = true;

        [Description("Display the server description in the message.")]
        public bool UseDescription { get; set; } = false;

        [Description("Display the server logo in the message.")]
        public bool UseLogo { get; set; } = true;

        [Description("Display the server IP address and port in the message.")]
        public bool UseConnectionInfo { get; set; } = true;

        [Description("Display the number of online players in the message.")]
        public bool UsePlayerCount { get; set; } = true;

        [Description("Display a list of online players in the message.")]
        public bool UsePlayerList { get; set; } = true;

        [Description("Display how long the players in the playerlist has been logged in for.")]
        public bool UsePlayerListLoggedInTime { get; set; } = false;

        [Description("Display the current server time.")]
        public bool UseCurrentTime { get; set; } = true;

        [Description("Display the time remaining until meteor impact in the message.")]
        public bool UseTimeRemaining { get; set; } = true;

        [Description("Display a boolean for if the meteor has hit yet or not, in the message.")]
        public bool UseMeteorHasHit { get; set; } = false;

        [Description("Display the number of active elections in the message.")]
        public bool UseElectionCount { get; set; } = false;

        [Description("Display a list of all active elections in the message.")]
        public bool UseElectionList { get; set; } = true;

        [Description("Display the number of active laws in the message.")]
        public bool UseLawCount { get; set; } = false;

        [Description("Display a list of all active laws in the message.")]
        public bool UseLawList { get; set; } = true;
    }
}
