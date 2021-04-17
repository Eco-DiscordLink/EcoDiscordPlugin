using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class SnippetInput : Input
    {
        public override string ToString()
        {
            return "Snippet Input";
        }

        public override string GetDisplayText(string childInfo, bool verbose)
        {
            string info = $"Registered Snippets: {DLStorage.Instance.Snippets.Count}";
            info += $"\r\n{childInfo}";
            return base.GetDisplayText(info, verbose);
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordMessageSent | DLEventType.DiscordMessageEdited | DLEventType.DiscordMessageDeleted;
        }

        protected override bool ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.SnippetInputChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task Initialize()
        {
            _ = ReloadSnippets();
            await base.Initialize();
        }

        protected override async Task HandleConfigChanged(object sender, EventArgs e)
        {
            using (await _overlapLock.LockAsync()) // Avoid crashes caused by data being manipulated and used simultaneously
            {
                await ReloadSnippets();
            }
            await base.HandleConfigChanged(sender, e);
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (!(data[0] is DiscordMessage message)) return;
            if (message.IsDm()) return;

            foreach (ChannelLink link in DLConfig.Data.SnippetInputChannels)
            {
                if (!link.IsValid())
                    continue;

                if (message.Channel.Guild.HasNameOrID(link.DiscordServer) && message.Channel.HasNameOrID(message.Channel.Name))
                {
                    await ReloadSnippets();
                    break;
                }
            }
        }

        private async Task ReloadSnippets()
        {
            DiscordLink plugin = DiscordLink.Obj;
            foreach (ChannelLink snippetLink in DLConfig.Data.SnippetInputChannels)
            {
                IReadOnlyList<DiscordMessage> snippetChannelMessages = await plugin.Client.GetMessagesAsync(snippetLink.Channel);
                if (snippetChannelMessages == null)
                    continue;

                // Go though all the found messages and look for snippet messages matching our regex
                DLStorage.Instance.Snippets.Clear();
                foreach (DiscordMessage channelMessage in snippetChannelMessages)
                {
                    Match match = MessageUtil.SnippetRegex.Match(channelMessage.Content);
                    if (match.Groups.Count == 3)
                    {
                        string key = match.Groups[1].Value;
                        string content = match.Groups[2].Value;
                        var snippets = DLStorage.Instance.Snippets;
                        if (!snippets.ContainsKey(key))
                            snippets.Add(key, content);
                        else
                            Logger.Info($"Found duplicate Snippet key \"{key}\". Only the first instance of this Snippet will be loaded.");
                    }
                }
            }
        }
    }
}
