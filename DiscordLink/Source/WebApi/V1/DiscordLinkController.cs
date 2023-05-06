using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Eco.WebServer.Web.Authorization;
using Eco.Shared;
using Eco.Shared.Utils;
using DiscordLink.Source.WebApi.V1.Models;

namespace Eco.Plugins.DiscordLink
{
    /// <summary>MVC <seealso cref="Controller"/> instance for accessing and interacting with DiscordLink.</summary>
    [Route("discordlink/api/v1")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public class DiscordLinkController : Controller
    {

        /// <summary>Force restart DiscordLink</summary>
        [HttpPost("restart")]
        public async Task<bool> PostReloadPlugin()
        {
            return await SharedCommands.RestartPlugin(SharedCommands.CommandInterface.WebCommand, null);
        }

        /// <summary>Post an Invite to the discord server.</summary>
        [HttpPost("/chat/invite")]
        public async Task<bool> PostInviteMessage()
        {
            bool sent = await SharedCommands.PostInviteMessage(SharedCommands.CommandInterface.WebCommand, null);
            Response.StatusCode = sent ? 201 : 500;
            return sent;
        }

        /// <summary>Get the status of Discordlink.</summary>
        [HttpGet("status/plugin")]
        public DiscordLinkPluginStatusV1 GetPluginStatus()
        {
            DiscordLink plugin = DiscordLink.Obj;
            DLDiscordClient client = plugin.Client;
            return new DiscordLinkPluginStatusV1
            {
                DiscordStatus = client.Status,
                PluginStatus = plugin.GetStatus(),
                ServerVersion = EcoVersion.VersionNumber,
                PluginVersion = plugin.PluginVersion.ToString(),
                LinkedUsers = DLStorage.PersistentData.LinkedUsers.Count,
                OptInUsers = DLStorage.PersistentData.OptedInUsers.Count,
                OptOutUsers = DLStorage.PersistentData.OptedOutUsers.Count,
                ClientName = client.DiscordClient.CurrentUser.Username
            };
        }

        /// <summary>Get the status of the current Configuration.</summary>
        [HttpGet("status/config")]
        public DiscordLinkConfigStatusV1 GetConfigValidationStatus()
        {

            DLConfigData config = DLConfig.Data;
            List<ChannelLinkStatusV1> channelLinks = new();

            if (DiscordLink.Obj.Client.ConnectionStatus == DLDiscordClient.ConnectionState.Connected)
            {
                // Discord guild and channel information isn't available the first time this function is called
                if (DiscordLink.Obj.Client.Guild != null && DLConfig.GetChannelLinks(verifiedLinksOnly: false).Count > 0)
                {
                    foreach (ChannelLink link in DLConfig.GetChannelLinks(verifiedLinksOnly: false))
                    {
                        channelLinks.Add(new ChannelLinkStatusV1
                        {
                            Id = link.DiscordChannelId.ToString(),
                            Valid = link.IsValid()
                        });
                    }
                }
            }
            return new DiscordLinkConfigStatusV1
            {
                ClientConnected = DiscordLink.Obj.Client.ConnectionStatus == DLDiscordClient.ConnectionState.Connected,
                BotToken = !string.IsNullOrWhiteSpace(config.BotToken),
                InviteMessage = !(!string.IsNullOrWhiteSpace(config.InviteMessage) && !config.InviteMessage.ContainsCaseInsensitive(DLConstants.INVITE_COMMAND_TOKEN)),
                ServerId = config.DiscordServerID != 0,
                ChannelLinks = channelLinks
            };
        }
    }
}
