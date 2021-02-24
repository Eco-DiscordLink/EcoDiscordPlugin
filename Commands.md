# Commands
When using commands:
- All Eco and Discord commands are case insensitive.
- [] indicates a command alias.
These are short forms of the command name and can be used to remove the need of preceding the command with /DiscordLink for Eco commands.
- () Indicates a parameter.
For Eco commands, the first parameter does not require a comma to separate it from the command name.

## Eco Commands
Eco commands are invoked by typing /DiscordLink [CommandName] into the Eco chat.
Typing only /DiscordLink or the alias /DL will invoke the help message and provide you with information about the available commands.

#### Admin commands
* <b>Restart [DL-Restart]</b> - Restarts the plugin.
* <b>ResetWorldData [DL-ResetData]</b> - Resets world data as if a new world had been created.
* <b>SendMessageToDiscordChannel (Server, Channel, Message)</b> - Sends a message to a specific server and channel.
* <b>ListGuilds</b> - Lists Discord servers the bot is in.
* <b>ListChannels</b> - Lists channels available to the bot in a specific server.
* <b>SendServerMessage [DL-ServerMessage] (message, Target Username, optional:temporary/permanent)</b> - Sends a server message to the target user.
* <b>BroadcastServerMessage [DL-BroadcastServerMessage] (message, optional:temporary/permanent)</b> - Sends a server message to all online users.  
[Example](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/features/commands/servermessage.png?raw=true)

* <b>SendPopup [DL-Popup] (message, Target Username)</b> - Sends a popup to the target user.  
* <b>BroadcastPopup [DL-BroadcastPopup] (message)</b> - Sends a popup to all online users.  
[Example](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/features/commands/popupmessage.png)

* <b>SendAnnouncement [DL-Announcement] (title, message, Target Username)</b> - Sends an announcement message box to the target user.
* <b>BroadcastAnnouncement [DL-BroadcastAnnouncement] (title, message)</b> - Sends an announcement message box to all online users.  
[Example](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/features/commands/announcementmessage.png?raw=true)

* <b>PluginStatus [DL-Status]</b> - Shows the plugin status.
* <b>PluginStatusVerbose [DL-StatusVerbose]</b> - Shows the plugin status including verbose debug level information.

#### User Commands
* <b>About [DL-About]</b> - Displays information about the DiscordLink plugin.
* <b>Invite [DL-Invite]</b> - Displays the Discord invite message.
* <b>Snippet [DL-Snippet] (Optional: Snippet Key)</b> - Post a predefined snippet from Discord. Will display a list of available snippets if no Snippet Key is supplied.
* <b>LinkDiscordAccount [DL-Link] (Discord User name)</b> - Initiates the process for linking thecalling Eco user to the supplied Discord account. Will trigger a confirmation request from the Discord bot in a Discord DM.
<details>
  <summary>Account Linking Process</summary>
  1. Run _dl-Link_ command from Eco and receive the verification message from the bot.  
  2. Run _dl-VerifyLink_ command as a response to the verification message.  
  ![Account Linking Verification](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/features/commands/accountverification.png?raw=true)
  
</details>

* <b>UnlinkDiscordAccount [DL-Unlink]</b> - Removes any existing link between the calling Eco account and Discord. Can be used to abort an unfinished linking process.
* <b>Trades [DL-Trades] [Trade] [dlt] (Item Name or Username)</b> - Displays available trades by person or by item.  
[Example](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/features/commands/ecotrades.png?raw=true)

* <b>TrackTrades [DL-TrackTrades] (Item name or Username)</b> - Configures a Trade Tracker Display showing up to date information on the requested item in a Discord DM with the calling user. Requires the calling user having linked their Eco account to Discord using _/dl-link_. Note that there may be a limit to how many tracked trades each user is allowed to have.  
[Example](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/features/commands/discordtrades.png?raw=true)

* <b>StopTrackTrades [DL-StopTrackTrades] (Item Name or Username)</b> - Removes the Trade Tracker Display for the requested item for the calling user.
* <b>ListTrackedTrades [DL-ListTrackedTrades]</b> - Lists all Trade Tracker Displays registered for the calling user.

## Discord Commands
Discord commands are invoked by typing the command prefix character (default '?') followed by a command name.
Command Parameters that contain spaces need to be enclosed in quotes (Example: "Basic Upgrade 1")

#### Admin commands
* <b>Restart [DL-Restart]</b> - Restarts the plugin.
* <b>ResetWorldData [DL-ResetData]</b> - Resets world data as if a new world had been created.
* <b>Print (Message)</b> - Reposts the inputted message. Can be used to create tags for ordering display tags within a channel.
* <b>Echo (Optional: Message )</b> - Tests message forwarding by sending a message to Eco to be picked up by the Chat Links.
* <b>SendServerMessage [DL-ServerMessage] (message, Target Username, optional:temporary/permanent)</b> - Sends a server message to the target user.
* <b>BroadcastServerMessage [DL-BroadcastServerMessage] (message, optional:temporary/permanent)</b> - Sends a server message to all online users.  
[Example](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/features/commands/servermessage.png?raw=true)

* <b>SendPopup [DL-Popup] (message, Target Username)</b> - Sends a popup to the target user.
* <b>BroadcastPopup [DL-BroadcastPopup] (message)</b> - Sends a popup to all online users.  
[Example](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/features/commands/popupmessage.png)

* <b>SendAnnouncement [DL-Announcement] (title, message, Target Username)</b> - Sends an announcement message box to the target user.
* <b>BroadcastAnnouncement [DL-BroadcastAnnouncement] (title, message)</b> - Sends an announcement message box to all online users.  
[Example](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/features/commands/announcementmessage.png?raw=true)

* <b>PluginStatus [DL-Status] [Status]</b> - Shows the plugin status.
* <b>PluginStatusVerbose [DL-StatusVerbose] [StatusVerbose]</b> - Shows the plugin status including verbose debug level information.

#### User Commands
* <b>Ping</b> - Checks if the bot is online.
* <b>ServerStatus [DL-EcoStatus] [DL-ServerInfo] [EcoStatus]</b> - Prints the Server Info status.
* <b>PlayerList [players] [DL-Players]</b> - Prints the list of online players.
* <b>DiscordInvite [DL-Invite]</b> - Posts the Discord invite message to the Eco chat.
* <b>DiscordLinkAbout [DL-About]</b> - Posts a message describing what the DiscordLink plugin is.
* <b>VerifyLink [DL-VerifyLink]</b> - Accepts an account linking request sent via /DL-Link.
* <b>Trades [Trade] [DL-Trades] [DL-Trade] [dlt] (Item name or Username)</b> - Displays available trades by person or by item.
* <b>TrackTrades [DL-TrackTrades] (Item name or Username)</b> - Configures a Trade Tracker Display showing up to date information on the requested item in a Discord DM with the calling user. Requires the calling user having linked their Eco account to Discord using _/dl-link_ from inside the game.  
[Example](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/blob/develop/images/features/commands/discordtrades.png?raw=true)

* <b>StopTrackTrades [DL-StopTrackTrades] (Item name or Username)</b> - Removes the Trade Tracker Display for the requested item for the calling user.
* <b>ListTrackedTrades [DL-ListTrackedTrades]</b> - Lists all Trade Tracker Displays registered for the calling user.
