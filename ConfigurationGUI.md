# DiscordLink Configuration
Navigate to your server GUI, and navigate to the tab "DiscordLink". 
From here, you can manipulate all configuration options available and most of them will take effect without requiring a server restart.

#### Sections
* [Base Configuration - Discord](#base-configuration---discord)
* [Base Configuration - Eco](#base-configuration---eco)
* [Linking Chat Channels](#linking-chat-channels)
* [Command Settings](#command-settings)
* [Displays, Feeds and Inputs](#displays-feeds-and-inputs)
* [Plugin Configuration](#plugin-configuration)
* [Roles](#roles)

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

## Linking Chat Channels

1. The box you're interested in is called "Chat Channel Links" and it is located in the subcategory "Feeds". Click on the three dots next to the box saying "(Collection)". This may be hidden until you mouse over it.

![Opening Collection Window](images/configuration/channellinking/1.png)

2. In the new window that just appeared, click "Add" in the bottom left. This adds a new link.

![Add new link](images/configuration/channellinking/2.png)

3. Enter the parameters for the channel link. _"DiscordChannel"_ should be the Discord channel you want to link to Eco. For example, "general". _"EcoChannel"_ is the channel in Eco you want to link to Discord, for example "General". Once entered, hit "OK".  

4. You're done! All messages sent into the configured Discord channel and Eco chat channel should now be cross posting all player messages.

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

**Notes**
* All message types below the selected one will be printed as well.
* All non-verbose and non-backend log messages are written to a separate logs in "Logs/DiscordLink", regardless of log settings.
* If the backend log level is raised, it is normal to see warnings like "Pre-emptive ratelimit triggered". This simply means that the underlying communications library delayed a message in order to make sure that you do not hit Discord's rate limit.

**Use Verbose Display**
Determines if the output in the display tab of the server GUI should be verbose or not.

## Roles
**UseLinkedAccountRole**
Determines if a Discord role will be granted to users who link their Discord accounts.

**UseDemographicRoles**
Determines if Discord roles matching ingame demographics will be granted to users who have linked their accounts.

**DemographicReplacementRoles**
Roles that will be used (and created if needed) for the given demographics.

**UseSpecialtyRoles**
Determines if Discord roles matching ingame specialties will be granted to users who have linked their accounts.
