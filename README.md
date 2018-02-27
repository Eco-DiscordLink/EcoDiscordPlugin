![DiscordLink Logo](images/DiscordLinkLogo_Nameless_Small.png)
# DiscordLink
## Introduction

This Eco Global Survival plugin connects Discord servers to the game server, providing commands in both the game and Discord that support a variety of functions.

## Usage

From Discord:

* `?help` - Lists available commands
* `?ecostatus` - Prints the status of the Eco Server.

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

1. Download a release of the plugin, which should contain DiscordLink.dll.
2. Download DLLs for DSharpPlus and DSharpPlus.CommandsNext. These MAY be included in the above release.
3. Place all 3 .dlls in "Eco Server/Mods/DiscordLink" (Or wherever you fancy, really, as along as all of them are loaded."
4. Follow this guide: https://discordpy.readthedocs.io/en/rewrite/discord.html to set up a Discord bot user on your server.
5. Copy the token (see above guide) into the appropriate field in the server config GUI.
6. Set the other settings in the server config GUI to your liking and enjoy!

## Configuration

All configuration is done via the Server GUI.


DiscordLink Logo &copy; 2018 Phlo