![DiscordLink Logo](images/DiscordLinkLogo_Nameless_Small.png)
# DiscordLink

## Introduction

This Eco Global Survival plugin connects Discord servers to the game server.

### Features
* `Seamless communication`  
Connect your Eco chat to one or multiple Discord servers and channels for seamless and automated communication between Eco and Discord.
* `Live Eco Server Status Display`  
Display live updates of online players, remaining time, your server logo and more in a Discord channel.
* `Chat logging`  
Record the combined Discord and Eco chat in a chat log that persists between server restarts.
* `Assisted Configuration`  
DiscordLink will run verification passes on your configuration upon startup and configuration changes and output the result in the server log, helping you diagnose configuration errors.
* `Helpful Commands`  
DiscordLink features a number of helpful commands both from within the game and from Discord.
_/DiscordInvite_ will help you invite players ingame to your Discord server, while _?Trade_ will help you figure out who has the best deal on those yummy 
huckleberry muffins!

## Usage

### Eco <--> Discord Chat Synchronization
In order to synchronize the ingame chat with a Discord channel, you will need to set up a Discord bot and connect it to your Eco server via the DiscordLink config.
See [installation guide](Installation.md) for information on how to do this.

### Commands
##### From Discord:
* `?Help` - Lists available commands.
* `?EcoStatus` - Prints the status of the Eco Server.
* `?Players` - Displays the currently online players.
* `?Trades [player or item name]` - Lists all of the items sold by a player, or all of the shops that sell an item.
* `?NextPage` - Continues onto the next page of a trade listing.
* `?DiscordInvite` - Posts a message with the Discord invite code into the Eco chat.
* `?Ping` - Checks if the bot is online.
* `?Echo [message]` - Sends the provided message to Eco and back to Discord again.

##### From Eco:
* `/VerifyDiscord` - Confirms the plugin is loaded.
* `/DiscordGuilds` - Lists all servers that this bot is connected to.
* `/DiscordChannels [guildname]` - Lists all channels in a specific server.
* `/DiscordDefaultChannel [guildname], [channelname]` - Sets the channel that `/discordmessage` sends to for you and only you.
* `/DiscordMessage [message]` - Sends a message to the default server and channel.
* `/DiscordSendToChannel [guildname], [channelname], [message]` - Sends a message to a specific server and channel.
* `/DiscordInvite` - Displays Discord invite message.

##### Notes:
1. **Emojis**  
When sending Emojis from Discord to Eco, bear in mind that these may either be removed or show up ingame as a ? character.
This means that some sentences may appear to be questions when read from within Eco if they end with an emoji.
As an example; _"We have trucks :D"_ may become _"We have trucks ?"_ which may cause some confusion.

2. **Discord Mentions**  
Make sure that `@` or `#` is not the first character in your message when writing Discord mentions.
Eco will consider them ingame mentions of players or channels and your message will open a chat channel ingame instead of being sent to the chat you intended and will therefore never get sent to Discord.

## Installation

See the [installation guide](Installation.md)

## Configuration

### Server GUI
[configuration guide](ConfigurationGUI.md) for self hosted servers with access to the server GUI.

### Config file
[configuration guide](ConfigurationNoGUI.md) for servers hosted by third parties, where you lack access to the server GUI.

## I want to contribute!

Pull requests are very welcome!
For information on how to set up the development environment, see the [project setup guide](ProjectSetup.md)

\
\
\
DiscordLink Logo &copy; 2018 Phlo
