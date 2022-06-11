![DiscordLink Logo](images/DiscordLinkLogo_Nameless_Small.png)
# DiscordLink

## Introduction

This Eco Global Survival plugin connects Discord servers to the game server.

### Features
* `Seamless communication`  
Connect your Eco chat to one or multiple Discord servers and channels for seamless and automated communication between Eco and Discord.
* `Discord Displays`  
See live updated information on Server Status, Elections, Work parties, Store Contents and more in Discord.  
For a full list of all supported display modules, see the [display module list](Modules.md#displays).  
* `Discord Feeds`  
See feeds of ingame events such as Trades and Crafts in Discord.  
For a full list of all supported feed modules, see the [feed module list](Modules.md#feeds).  
* `Discord Inputs`  
Add predefined messages in Discord and invoke them ingame using the /Snippet command.  
For a full list of all supported input modules, see the [input module list](Modules.md#inputs).  
* `Discord Roles`  
Automatically assign roles to users based on their ingame specializations and demograhics.    
For a full list of all supported role modules, see the [role module list](Modules.md#roles).  
* `Assisted Configuration`  
DiscordLink will run verification passes on your configuration upon startup and configuration changes and output the result in the server log, helping you diagnose configuration errors.
* `Helpful Commands`  
DiscordLink features a number of helpful commands both from within the game and from Discord.  
_/DiscordLink Invite_ will help you invite players ingame to your Discord server, while _?Trade_ will help you figure out who has the best deal on your favorite dish!  
For a full list of commands, see the [command list](Commands.md).

## Usage

### Eco <--> Discord Chat Synchronization
In order to synchronize the ingame chat with a Discord channel, you will need to set up a Discord bot and connect it to your Eco server via the DiscordLink config.
See [installation guide](Installation.md) for information on how to do this.

### Modules
DiscordLink offers a variety of modules that can show information and allow interaction with the game from Discord, such as player lists, election voting, existing laws and currencies.
See the [Modules Feature List](Modules.md) for more information.

### Commands
See the [command list](Commands.md) for available commands and how to use them.

### Notes:
1. **Emojis**  
When sending Emojis from Discord to Eco, bear in mind that these may either be removed or show up ingame as a □ character.

2. **Discord Mentions**  
Make sure that `@` or `#` is not the first character in your message when writing Discord mentions.
Eco will consider them ingame mentions of players or channels and your message will open a chat channel ingame instead of being sent to the chat you intended and will therefore never get sent to Discord.

## Installation

See the [installation guide](Installation.md).

## Configuration

### Server GUI  
See this [configuration guide](ConfigurationGUI.md) for self hosted servers with access to the server GUI.

### Config file
See this [configuration guide](ConfigurationNoGUI.md) for servers hosted by third parties, where you lack access to the server GUI.

## Discord

Do you have suggestions, questions or maybe a problem you need help with?
Join the Eco [Community Discord](https://discord.gg/pCkWfzQ) server!
DiscordLink has its own corner where you can talk to other users and the developer!

## I want to contribute!

Pull requests are very welcome!
For information on how to set up the development environment, see the [project setup guide](ProjectSetup.md).

## History & Credits

### Maintainers
**Spoffy** - Original author of the plugin and maintainer 2018 -> 2020.  
**MonzUn** - Maintainer 2020 -> Present. 

### Additional Contributors
**Phlo** - DiscordLink logo.  
**demodude4u** - Original author of the Discord ?trades command.  
**bozoweed** - Eco 8.0 update compatibility.  



\
\
\
DiscordLink Logo &copy; 2018 Phlo
