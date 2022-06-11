# DiscordLink Configuration
As we don't have access to the eco server GUI, we will need to do the configuration via the DiscordLink config file.
The config file is named "DiscordLink.eco" is generated inside the _"Config"_ directory of the Eco Server after having started the server once with the mod loaded.

#### Sections
* [Sample Config](#sample-config)
* [Base Configuration - Discord](#base-configuration---discord)
* [Base Configuration - Eco](#base-configuration---eco)
* [Linking Chat Channels](#linking-chat-channels)
* [Command Settings](#command-settings)
* [Displays, Feeds and Inputs](#displays-feeds-and-inputs)
* [Plugin Configuration](#plugin-configuration)
* [Roles](#roles)

## Sample Config
This is an example of what a filled out configuration file (DiscordLink.eco) could look like.
 
NOTE: YOU CANNOT COPY THIS ONE AND EXPECT IT TO WORK!  
The config data needs to match your Eco server, Discord server and Discord bot.
<details>
  <summary>Configuration File Example</summary>

```
{  
  "DiscordServer": "EcoWorld",  
  "BotToken": "xxXXxxxXxXXxxxxxxXxxxxXXXXxxx.XxxXxx.xXXXxxxxXXxxxxXxxxXXXXXXXxxxxxxxX",  
  "EcoBotName": "DiscordLink",  
    "AdminRoles": [  
    "admin",  
    "administrator",  
    "moderator"  
  ],  
  "MinEmbedSizeForFooter": "Medium",
  "ServerName": "TheEcoServer",  
  "ServerDescription": "The place to play Eco!",  
  "ConnectionInfo": "xxx.xxx.xxx.xx",  
  "ServerLogo": "https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/DiscordLinkLogo_Nameless.png",  
  "ChatChannelLinks": [  
   {  
    "AllowUserMentions": true,  
    "AllowRoleMentions": true,  
    "AllowChannelMentions": true,  
    "Direction": "Duplex",  
    "HereAndEveryoneMentionPermission": "Forbidden",  
    "EcoChannel": "generala",  
    "DiscordChannel": "general"  
   }  
  ],  
  "TradeFeedChannels": [  
    {  
      "DiscordChannel": "trades"  
    }  
  ],  
  "CraftingFeedChannels": [  
    {  
      "DiscordChannel": "crafting"  
    }  
  ],  
  "ServerStatusFeedChannels": [  
    {  
      "DiscordChannel": "general"  
    }  
  ],  
  "PlayerStatusFeedChannels": [  
    {  
      "DiscordChannel": "general"  
    }  
  ],  
  "ElectionFeedChannels": [  
    {  
      "DiscordChannel": "election-feed"  
    }  
  ],  
  "ServerLogFeedChannels": [  
    {  
      "LogLevel": "Information",  
      "DiscordChannel": "server-log"  
    }  
  ],  
  "ServerInfoDisplayChannels": [  
    {  
      "UseName": true,  
      "UseDescription": false,  
      "UseLogo": true,  
      "UseConnectionInfo": true,  
      "UseWebServerAddress": true,  
      "UsePlayerCount": false,  
      "UsePlayerList": true,  
      "UsePlayerListLoggedInTime": false,  
      "UsePlayerListExhaustionTime": false,  
      "UseIngameTime": true,  
      "UseTimeRemaining": true,  
      "UseServerTime": true,  
      "UseExhaustionResetServerTime": false,  
      "UseExhaustionResetTimeLeft": false,  
      "UseExhaustedPlayerCount": false,  
      "UseElectionCount": false,  
      "UseElectionList": true,  
      "UseLawCount": false,  
      "UseLawList": true,  
      "DiscordChannel": "server-info"  
    }  
  ],  
  "WorkPartyDisplayChannels": [  
    {  
      "DiscordChannel": "work-parties"  
    }  
  ],  
  "ElectionDisplayChannels": [  
    {  
      "DiscordChannel": "elections"  
    }  
  ],  
  "CurrencyDisplayChannels": [  
    {  
      "UseMintedCurrency": "MintedExists",  
      "UsePersonalCurrency": "NoMintedExists",  
      "MaxMintedCount": 1,  
      "MaxPersonalCount": 3,  
      "MaxTopCurrencyHolderCount": 6,  
      "UseTradeCount": true,  
      "UseBackingInfo": false,  
      "DiscordGuild": "EcoDiscordServer",  
      "DiscordChannel": "currency"  
    }  
  ],  
  "SnippetDisplayChannels": [  
    {  
      "DiscordChannel": "snippets"  
    }  
  ],  
  "UseLinkedAccountRole": true,  
  "UseDemographicRoles": true,  
  "DemographicReplacementRoles": [  
    {  
      "DemographicName": "everyone",  
      "RoleName": "Eco Everyone"  
    },  
    {  
      "DemographicName": "admins",  
      "RoleName": "Eco Admins"  
    }  
  ],  
  "UseSpecialtyRoles": true,  
  "DiscordCommandChannels": [  
    {  
      "DiscordChannel": "commands"  
    }  
  ],  
  "MaxTradeWatcherDisplaysPerUser": 5,
  "InviteMessage": "Join us on Discord!\n[LINK]",
  "LogLevel": "Information",  
  "BackendLogLevel": "Error",  
}  
```  

</details>


## Base Configuration - Discord

**Discord Server and Bot Token**  
See the [installation guide](Installation.md).

**Eco Bot Name**  
The name the bot should use when posting in Eco.
Note that the bot user is created when the server starts for the first time after a world reset and therefore, changing this will only take effect after the next world reset.

**Admin Roles**  
Names of Discord roles which DiscordLink should consider as having admin privileges.
The admin role names are not case sensitive.

**Min Embed Size for Footer**  
Determines for what sizes of embeds to show the footer containing meta information about posted embeds. All embeds of sizes bigger than the selected one will have footers as well.

## Base Configuration - Eco

**Server Name and Server Description**  
The name and description to use in output instead of the ones configured in the network config.

**Server Logo**  
The logo of the server as a URL, to use when the bot posts embed messages.

**Connection Info**  
The text to display when showing connection information for the game server. It is recommended to use the Server ID or an IP for this field.

**Web Server Address**  
The base address (URL or IP) of the web server to use in web server links. If the web server traffic is being routed through a different port than the configured \"Web Server Port\" from the Network config, then also qualify this address with the rereouted port number. Do not point this field to any specific page on the web server.

## Linking Chat Channels
1. Copy the _"ChatChannelLinks"_ section of the sample config into your config file.
2. Set the _"DiscordChannel"_ field to the name or ID of the Discord channel you wish to synchronize with a channel in Eco.
3. Set the _"EcoChannel"_ field to the name of the Eco channel you wish to synchronize with the Discord channel in the previous step.
5. **Optional**: Configure the three flags for Discord mention tag permissions according to your preference of allowing role, user and Channel mentions to be used from Eco.
5. **Optional** Configure the _"Direction"_ field to only allow messages to be forwarded in one direction.

## Command Settings
**Discord Command Prefix**  
The prefix to put before commands in order for the Discord bot to recognize them as such.  
In all command examples `?` is used as Discord command prefix as this is the default prefix.
Eco commands always use `/` as command prefix as this is hard coded into the game client.

**Admin Roles**  
The Discord roles for which to allow the use of admin commands. Role names are case insensitive.

**Eco Command Channel**  
The Eco chat channel to use for commands that outputs public messages, excluding the initial # character.

**Max Trade Watcher Displays Per User**  
The maximum amount of Trade Watcher Displays allowed for each user.
Note that lowering this will not remove any existing watchers.

**Invite Message**  
The message to use for the /DiscordInvite command. The invite link is fetched from the Network configuration (the _Discord Address_ field) and will replace the [LINK] token. The message needs to include at least one [LINK] token in order to function and the _Network_ configuration needs to have the _Discord Address_ field filled out.

## Displays, Feeds and Inputs
All displays, feeds and inputs require a [Channel Link](#linking-chat-channels) and will be considered turned off until a valid one exists.  
For more information, see the [Modules Page](Modules.md).

## Plugin Configuration
**Log Level and Backend Log Level**
The _Log Level_ and _Backend Log Level_ should generally be kept at their defaults unless you are troubleshooting an issue or want to turn off the output in the Eco server log.

The potential values for Log Level is:
* Debug Verbose
* Debug
* Warning
* Information (Default)
* Error
* Silent

The potential values for Backend Log Level is:
* Trace
* Debug
* Information
* Warning
* Error
* Critical
* None (Default)

**Notes**
* All message types below the selected one will be printed as well.
* All non-verbose and non-backend log messages are written to a separate logs in "Logs/DiscordLink", regardless of log settings.
* If the backend log level is raised, it is normal to see warnings like "Pre-emptive ratelimit triggered". This simply means that the underlying communications library delayed a message in order to make sure that you do not hit Discord's rate limit.

**Use Verbose Display**
Determines if the output in the display tab of the server GUI should be verbose or not.
Since we are not using the GUI, this setting doesn't do anything for us.

## Roles
**UseLinkedAccountRole**
Determines if a Discord role will be granted to users who link their Discord accounts.

**UseDemographicRoles**
Determines if Discord roles matching ingame demographics will be granted to users who have linked their accounts.

**DemographicReplacementRoles**
Roles that will be used (and created if needed) for the given demographics.

**UseSpecialtyRoles**
Determines if Discord roles matching ingame specialties will be granted to users who have linked their accounts.
