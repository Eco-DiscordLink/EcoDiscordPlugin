using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    class EcoStatusDisplay : Display
    {
        private readonly Dictionary<EcoStatusChannel, ulong> _ecoStatusMessages = new Dictionary<EcoStatusChannel, ulong>();

        public EcoStatusDisplay()
        {
            _triggerTypeField = TriggerType.Startup | TriggerType.Login;
            _timerStartDelayMS = 0;
            _timerUpdateIntervalMS = 60000;
        }

        public override void OnConfigChanged()
        {
            _ecoStatusMessages.Clear(); // The status channels may have changed so we should find the messages again;
            base.OnConfigChanged();
        }

        protected override void UpdateInternal(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (plugin == null) return;
            foreach (EcoStatusChannel statusChannel in DLConfig.Data.EcoStatusChannels)
            {
                if (!statusChannel.IsValid()) continue;
                DiscordGuild discordGuild = plugin.GuildByName(statusChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByName(statusChannel.DiscordChannel);
                if (discordChannel == null) continue;

                if (!DiscordUtil.ChannelHasPermission(discordChannel, Permissions.ReadMessageHistory)) continue;
                bool HasEmbedPermission = DiscordUtil.ChannelHasPermission(discordChannel, Permissions.EmbedLinks);

                DiscordMessage ecoStatusMessage = null;
                bool created = false;
                ulong statusMessageID;
                if (_ecoStatusMessages.TryGetValue(statusChannel, out statusMessageID))
                {
                    try
                    {
                        ecoStatusMessage = discordChannel.GetMessageAsync(statusMessageID).Result;
                    }
                    catch (System.AggregateException)
                    {
                        _ecoStatusMessages.Remove(statusChannel); // The message has been removed, take it out of the list
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error occurred when attempting to read message with ID " + statusMessageID + " from channel \"" + discordChannel.Name + "\". Error message: " + e);
                        continue;
                    }
                }
                else
                {
                    IReadOnlyList<DiscordMessage> ecoStatusChannelMessages = DiscordUtil.GetMessagesAsync(discordChannel).Result;
                    if (ecoStatusChannelMessages == null) continue;

                    foreach (DiscordMessage message in ecoStatusChannelMessages)
                    {
                        // We assume that it's our status message if it has parts of our string in it
                        if (message.Author == plugin.DiscordClient.CurrentUser
                            && (HasEmbedPermission ? (message.Embeds.Count == 1 && message.Embeds[0].Title.Contains("Live Server Status**")) : message.Content.Contains("Live Server Status**")))
                        {
                            ecoStatusMessage = message;
                            break;
                        }
                    }

                    // If we couldn't find a status message, create a new one
                    if (ecoStatusMessage == null)
                    {
                        ecoStatusMessage = DiscordUtil.SendAsync(discordChannel, null, MessageBuilder.GetEcoStatus(GetEcoStatusFlagForChannel(statusChannel), isLiveMessage: true)).Result;
                        created = true;
                    }

                    if (ecoStatusMessage != null) // SendAsync may return null in case an exception is raised
                    {
                        _ecoStatusMessages.Add(statusChannel, ecoStatusMessage.Id);
                    }
                }

                if (ecoStatusMessage != null && !created) // It is pointless to update the message if it was just created
                {
                    _ = DiscordUtil.ModifyAsync(ecoStatusMessage, "", MessageBuilder.GetEcoStatus(GetEcoStatusFlagForChannel(statusChannel), isLiveMessage: true));
                }
            }
        }

        private static MessageBuilder.EcoStatusComponentFlag GetEcoStatusFlagForChannel(EcoStatusChannel statusChannel)
        {
            MessageBuilder.EcoStatusComponentFlag statusFlag = 0;
            if (statusChannel.UseName)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.Name;
            if (statusChannel.UseDescription)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.Description;
            if (statusChannel.UseLogo)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.Logo;
            if (statusChannel.UseAddress)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.ServerAddress;
            if (statusChannel.UsePlayerCount)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.PlayerCount;
            if (statusChannel.UsePlayerList)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.PlayerList;
            if (statusChannel.UseTimeSinceStart)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.TimeSinceStart;
            if (statusChannel.UseTimeRemaining)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.TimeRemaining;
            if (statusChannel.UseMeteorHasHit)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.MeteorHasHit;

            return statusFlag;
        }
    }
}
