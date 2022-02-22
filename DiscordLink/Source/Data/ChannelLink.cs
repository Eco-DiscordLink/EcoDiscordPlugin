using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Serialization;
using Eco.Shared.Utils;
using System;
using System.ComponentModel;
using System.Linq;

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
        public DiscordChannel Channel { get; private set; } = null;

        [Description("Discord Channel by name or ID.")]
        public string DiscordChannel { get; set; } = string.Empty;

        [Description("Discord Server by name or ID. only needed if connected to more that one Discord server.")]
        public string DiscordServer { get; set; } = string.Empty;

        public override string ToString()
        {
            string channelName = $"#{(IsValid() ? Channel.Name : DiscordChannel)}";
            if (DLConfig.Data.DiscordServers.Count > 1)
            {
                channelName = $"{(IsValid() ? Channel.Guild.Name : DiscordServer)}{channelName}";
            }

            return channelName;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override bool IsValid() =>
            !string.IsNullOrWhiteSpace(DiscordChannel)
            && (!string.IsNullOrWhiteSpace(DiscordServer) || DLConfig.Data.DiscordServers.Count != 1)
            && Channel != null;

        public virtual bool Initialize()
        {
            if (string.IsNullOrWhiteSpace(DiscordServer) && DLConfig.Data.DiscordServers.Count != 1)
                return false;

            if (string.IsNullOrWhiteSpace(DiscordChannel))
                return false;

            DiscordGuild guild = DLConfig.Data.DiscordServers.Count == 1
                ? DiscordLink.Obj.Client.Guilds.Single()
                : DiscordLink.Obj.Client.GuildByNameOrID(DiscordServer);

            DiscordChannel channel = guild?.ChannelByNameOrID(DiscordChannel);
            if (channel == null)
                return false;

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

        public virtual bool IsChannel(DiscordChannel channel)
        {
            if (ulong.TryParse(DiscordChannel, out ulong channelID))
                return channelID == channel.Id;

            return DiscordChannel.EqualsCaseInsensitive(channel.Name);
        }

        public virtual bool HasChannelNameOrID(string channelNameOrID)
        {
            if (ulong.TryParse(DiscordChannel, out ulong channelID) && ulong.TryParse(channelNameOrID, out ulong channelIDParam))
                return channelID == channelIDParam;

            return DiscordChannel.EqualsCaseInsensitive(channelNameOrID);
        }
    }

    public class RelayChannelLink : ChannelLink, ICloneable
    {
        [Browsable(false), JsonIgnore]
        public DiscordChannel SecondChannel { get; private set; } = null;

        [Description("Second Discord Channel by name or ID.")]
        public string SecondDiscordChannel { get; set; } = string.Empty;

        [Description("Discord Server by name or ID. only needed if connected to more that one Discord server.")]
        public string SecondDiscordServer { get; set; } = string.Empty;

        [Description("Allow mentions of usernames to be forwarded.")]
        public bool AllowUserMentions { get; set; } = true;

        [Description("Allow mentions of roles to be forwarded.")]
        public bool AllowRoleMentions { get; set; } = true;

        [Description("Allow mentions of channels to be forwarded.")]
        public bool AllowChannelMentions { get; set; } = true;

        [Description("Permissions for who is allowed to forward mentions of @here or @everyone.")]
        public GlobalMentionPermission HereAndEveryoneMentionPermission { get; set; } = GlobalMentionPermission.Forbidden;

        public override string ToString()
        {
            string discordChannelName = base.ToString();
            string secondChannelName = $"#{(IsValid() ? SecondChannel.Name : SecondDiscordChannel)}";
            if (DLConfig.Data.DiscordServers.Count > 1)
            {
                secondChannelName = $"{(IsValid() ? Channel.Guild.Name : DiscordServer)}{secondChannelName}";
            }
            return $"{discordChannelName} <--> {secondChannelName}";
        }

        public override bool IsValid() => base.IsValid()
            && !string.IsNullOrWhiteSpace(SecondDiscordChannel)
            && (!string.IsNullOrWhiteSpace(SecondDiscordServer) || DLConfig.Data.DiscordServers.Count != 1)
            && SecondChannel != null;

        public override bool Initialize()
        {
            bool initialized = base.Initialize();
            if (!initialized) return false;

            if (string.IsNullOrWhiteSpace(SecondDiscordServer) && DLConfig.Data.DiscordServers.Count != 1)
                return false;

            if (string.IsNullOrWhiteSpace(SecondDiscordChannel))
                return false;

            DiscordGuild guild = DLConfig.Data.DiscordServers.Count == 1
                ? DiscordLink.Obj.Client.Guilds.Single()
                : DiscordLink.Obj.Client.GuildByNameOrID(SecondDiscordServer);

            DiscordChannel channel = guild?.ChannelByNameOrID(SecondDiscordChannel);
            if (channel == null)
                return false;

            SecondChannel = channel;
            return true;
        }

        public override bool MakeCorrections()
        {
            bool correctionMade = base.MakeCorrections();

            if (string.IsNullOrWhiteSpace(SecondDiscordChannel))
                return false || correctionMade;

            string original = SecondDiscordChannel;
            string channelNameLower = SecondDiscordChannel.ToLower();
            if (SecondDiscordChannel != channelNameLower) // Discord channels are always lowercase
                SecondDiscordChannel = channelNameLower;

            if (SecondDiscordChannel.Contains(" "))
                SecondDiscordChannel = SecondDiscordChannel.Replace(' ', '-'); // Discord channels always replace spaces with dashes

            if (SecondDiscordChannel != original)
            {
                correctionMade = true;
                Logger.Info($"Corrected Discord channel name {(string.IsNullOrWhiteSpace(DiscordServer) ? string.Empty : $"with Guild name/ID {DiscordServer}")} from \"{original}\" to \"{DiscordChannel}\"");
            }

            return correctionMade;
        }

        public override bool IsChannel(DiscordChannel channel)
        {
            if (ulong.TryParse(DiscordChannel, out ulong channelID) && channelID == channel.Id)
                return true;
            if (ulong.TryParse(SecondDiscordChannel, out channelID) && channelID == channel.Id)
                return true;

            return DiscordChannel.EqualsCaseInsensitive(channel.Name) || SecondDiscordChannel.EqualsCaseInsensitive(channel.Name);
        }

        public override bool HasChannelNameOrID(string channelNameOrID)
        {
            if (ulong.TryParse(channelNameOrID, out ulong channelIDParam))
            {
                if (ulong.TryParse(DiscordChannel, out ulong channelID))
                {
                    return channelID == channelIDParam;
                }
                if (ulong.TryParse(SecondDiscordChannel, out channelID))
                {
                    return channelID == channelIDParam;
                }
            }

            return DiscordChannel.EqualsCaseInsensitive(channelNameOrID) || SecondDiscordChannel.EqualsCaseInsensitive(channelNameOrID);
        }
    }

    public class EcoChannelLink : ChannelLink
    {
        [Description("Eco channel to use (omit # prefix).")]
        public string EcoChannel { get; set; } = string.Empty;
        public override string ToString()
        {
            string discordChannelName = base.ToString();
            return $"{discordChannelName} <--> {EcoChannel}";
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
                Logger.Info($"Corrected Eco channel name linked with Discord Channel name/ID \"{DiscordChannel}\" from \"{original}\" to \"{EcoChannel}\"");
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
