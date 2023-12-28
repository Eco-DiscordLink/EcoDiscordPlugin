using DSharpPlus;
using Eco.Plugins.DiscordLink.Events;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public abstract class RoleModule : Module
    {
        protected override DLEventType GetTriggers()
        {
            return DLEventType.ForceUpdate;
        }

        protected override async Task<bool> ShouldRun()
        {
            return Plugin.Obj.Client.BotHasPermission(Permissions.ManageRoles);
        }
    }
}
