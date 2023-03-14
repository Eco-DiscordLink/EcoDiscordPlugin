
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
  "DiscordServerID": "112233445566778899",  
  "BotToken": "xxXXxxxXxXXxxxxxxXxxxxXXXXxxx.XxxXxx.xXXXxxxxXXxxxxXxxxXXXXXXXxxxxxxxX",  
    "AdminRoles": [  
    "admin",  
    "administrator",  
    "moderator",  
    "Eco Admins"  
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
    "EcoChannel": "General",  
    "DiscordChannelId": 980963363205025815,  
    "UseTimestamp": true  
   }  
  ],  
  "TradeFeedChannels": [  
    {  
      "DiscordChannelId": 980963363205025815  
    }  
  ],  
  "CraftingFeedChannels": [  
    {  
      "DiscordChannelId": 980963363205025815  
    }  
  ],  
  "ServerStatusFeedChannels": [  
    {  
      "DiscordChannelId": 980963363205025815  
    }  
  ],  
  "PlayerStatusFeedChannels": [  
    {  
      "DiscordChannelId": 980963363205025815  
    }  
  ],  
  "ElectionFeedChannels": [  
    {  
      "DiscordChannelId": 980963363205025815  
    }  
  ],  
  "ServerLogFeedChannels": [  
    {  
      "LogLevel": "Information",  
      "DiscordChannelId": 980963363205025815  
    }  
  ],  
  "ServerInfoDisplayChannels": [  
    {  
      "UseName": true,  
      "UseDescription": false,  
      "UseLogo": true,  
      "UseConnectionInfo": true,  
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
      "DiscordChannelId": 980963363205025815  
    }  
  ],  
  "WorkPartyDisplayChannels": [  
    {  
      "DiscordChannelId": 980963363205025815
    }  
  ],  
  "ElectionDisplayChannels": [  
    {  
      "DiscordChannelId": 980963363205025815
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
      "DiscordChannelId": 980963363205025815
    }  
  ],  
  "SnippetDisplayChannels": [  
    {  
      "DiscordChannelId": 980963363205025815 
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
      "DiscordChannelId": 980963363205025815
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
**Discord Server ID and Bot Token**  
See the [installation guide](Installation.md).

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
The text to display when showing connection information for the game server. It is recommended to use the Server ID or an IP for this field. You can create a clickable link for joining your server using this syntax: `<eco://connect/<ServerID>>`

## <a id="ChatLink"></a>Linking Chat Channels
1. Copy the _"ChatChannelLinks"_ section of the sample config into your config file.
2. Set the _"DiscordChannelId"_ field to the ID of the Discord channel you wish to synchronize with a channel in Eco.
3. Set the _"EcoChannel"_ field to the name of the Eco channel you wish to synchronize with the Discord channel in the previous step.
4. **Optional**: Configure the three flags for Discord mention tag permissions according to your preference of allowing role, user and Channel mentions to be used from Eco.
5. **Optional** Configure the _"Direction"_ field to only allow messages to be forwarded in one direction.
6. **Optional** Configure the _"UseTimestamp"_ field to set if a Discord timestamp should be added to the start of each chat message DiscordLink posts to the Discord channel.

## Command Settings
**Admin Roles**  
The Discord roles (case sensitive!) for which to allow the use of admin commands. Note that users with these roles will also be able to use ingame admin commands via the `ExecuteEcoCommand` command.

**Max Trade Watcher Displays Per User**  
The maximum amount of Trade Watcher Displays allowed for each user.  
Note that lowering this will not remove any existing watchers.

**Invite Message**  
The message to use for the /PostInviteMessage command. The invite link is fetched from the Network configuration (the _Discord Address_ field) and will replace the [LINK] token. The message needs to include at least one [LINK] token in order to function and the _Network_ configuration needs to have the _Discord Address_ field filled out.

## Displays, Feeds and Inputs
All displays, feeds and inputs require a [Channel Link](#linking-chat-channels) and will be considered turned off until a valid one exists.  
For more information, see the [Modules Page](Modules.md).

## Plugin Configuration
**Log Level**
Controls what type of DiscordLink log messages are printed to the console. This should generally be kept at the default unless you are troubleshooting an issue or want to turn off the output in the Eco server log.

The potential values for Log Level are:
* Debug Verbose
* Debug
* Warning
* Information (Default)
* Error
* Silent

**Backend Log Level**
Controls what type of Discord/DSharp log messages should be printed to the console. This should almost always be kept at the default unless you are troubleshooting an issue related to Discord rather than DiscordLink.

The potential values for Backend Log Level are:
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
