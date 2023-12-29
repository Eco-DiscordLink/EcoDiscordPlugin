using Eco.Plugins.DiscordLink.Events;
using System.Threading.Tasks;
using static DSharpPlus.Permissions;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class NameSyncModule : Module
    {
        public override string ToString()
        {
            return "Name Synchronization Module";
        }

        protected override async Task<bool> ShouldRun()
        {
            return DLConfig.Data.UseNameSynchronization;
        }

        protected override DLEventType GetTriggers()
        {
            return  DLEventType.AccountLinkVerified;
        }

        protected override async Task UpdateInternal(Eco.Plugins.DiscordLink.DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            DiscordClient client = plugin.Client;
            if (!client.BotHasPermission(ManageNicknames))
                return;

            if (!(data[0] is LinkedUser linkedUser))
                return;

            if(linkedUser.DiscordMember.DisplayName != linkedUser.EcoUser.Name)
            {
                await client.SetMemberNickname(linkedUser.DiscordMember, linkedUser.EcoUser.Name);
            }
        }
    }
}
