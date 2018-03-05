![DiscordLink Logo](images/DiscordLinkLogo_Nameless_Small.png)
# DiscordLink
## Introduction

This Eco Global Survival plugin connects Discord servers to the game server. It allows you link a Discord Channel to an Eco Channel for (reasonably) seamless communication between the two. 
DiscordLink also adds a number of commands to Discord and Eco that provide a variety of helpful features.

## Usage

From Discord:

* `?help` - Lists available commands
* `?ecostatus` - Prints the status of the Eco Server.
* `?players` - Displays the currently online players.

From Eco:
* `/verifydiscord` - Confirms the plugin is loaded

* `/discordguilds` - Lists all servers that this bot is connected to
* `/discordchannels [guildname]` - Lists all channels in a specific server.
* `/discorddefaultchannel [guildname],[channelname]` - Sets the channel that `/discordmessage` sends to for you and only you.

* `/discordsendtochannel [guildname],[channelname],[message]` - Sends a message to the given channel, if the Bot has access to that channel.
* `/discordmessage [message]` - Sends a message to the default channel.

Note: When writing messages to send from Eco, **do not include commas in your messages.** 
Eco will consider them extra parameters to the commands, and any text after the comma will be lost, due to the way Eco handles commands. 
Alternative methods of sending long text strings will be implemented in future versions.

For example: `/discordmessage Hello everyone, my name is Bob` will come out as "Hello everyone" in Discord.

## Installation

See the [installation guide](Installation.md)

## Configuration

All configuration is done via the Server GUI, and should be straightforward.

For more information, see the [configuration guide](Configuration.md)


DiscordLink Logo &copy; 2018 Phlo