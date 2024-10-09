# Commands

## Invoking Eco Commands
Eco commands are invoked by typing /DiscordLink [CommandName] into the Eco chat.
Typing only /DiscordLink or the alias /DL will invoke the help message and provide you with information about the available commands.

## Invoking Discord Commands
Discord commands are invoked by typing "/" followed by the command you wish to run.

## Commands
All Eco and Discord commands are case insensitive.

# **Plugin Management**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| DiscordLink                | DL                  | Eco               |                                                                          | User            | Yes      | Displays a list of all available DiscordLink commands. This is the parent command of all other DiscordLink commands. |
| RestartPlugin              |                     | Both              |                                                                          | Admin           | Yes      | Restarts the plugin.                                                                                                 |
| ReloadConfig               |                     | Both              |                                                                          | Admin           | Yes      | Reloads the DiscordLink config.                                                                                      |
| ResetPersistentData        |                     | Both              |                                                                          | Admin           | Yes      | Removes all persistent storage data.                                                                                 |
| ResetWorldData             |                     | Both              |                                                                          | Admin           | Yes      | Resets world data as if a new world had been created.                                                                |
| Update                     |                     | Both              |                                                                          | Admin           | Yes      | Forces an update of all modules.                                                                                     |
| ClearRoles                 |                     | Both              |                                                                          | Admin           | Yes      | Deletes all Discord roles created and tracked by DiscordLink.                                                        |
| PersistentStorageData      |                     | Both              |                                                                          | Admin           | No       | Displays a description of the persistent storage data.                                                               |
| WorldStorageData           |                     | Both              |                                                                          | Admin           | No       | Displays a description of the world storage data.                                                                    |


# <a id="SAT"></a>**Setup and Troubleshooting**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| VerifyConfig               |                     | Both              |                                                                          | Admin           | No       | Checks configuration setup and reports any errors.                                                                   |
| VerifyPermissions          |                     | Both              |                                                                          | Admin           | No       | Checks all permissions and intents needed for the current configuration and reports any missing ones.                |
| VerifyIntents              |                     | Both              |                                                                          | Admin           | No       | Checks all intents needed and reports any missing ones.                                                              |
| VerifyServerPermissions    |                     | Both              |                                                                          | Admin           | No       | Checks all server permissions needed and reports any missing ones.                                                   |
| VerifyChannelPermissions   |                     | Both              | ChannelNameOrID*                                                         | Admin           | No       | Checks all permissions needed for the given channel and reports any missing ones.                                    |
| Echo                       |                     | Discord           | Message                                                                  | Admin           | N/A      | Sends the provided message to Eco and back to Discord again if a chat link is configured for the channel.            |


# **Server Management**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| EcoCommand                 |                     | Discord           | Eco Command                                                              | Dynamic         | N/A      | Executes the inputted eco command. The parameter should be styled as a full ingame Eco command.                      |
| Announce                   |                     | Both              | Message, MessageType*, recipientUserNameOrID*                            | Admin           | Yes      | Sends the message formatted as [MessageType] to everyone on the server or the specified recipient.                   |
| ServerShutdown             |                     | Both              |                                                                          | Admin           | Yes      | Shuts down the eco server.                                                                                           |


# **Info**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| Version                    |                     | Both              |                                                                          | User            | Yes      | Displays the installed and latest available plugin version.                                                          |
| About                      |                     | Both              |                                                                          | User            | No       | Displays information about the DiscordLink plugin.                                                                   |
| Documentation              |                     | Both              |                                                                          | User            | No       | Opens the documentation web page.                                                                                    |
| LinkInformation            | LinkInfo            | Both              |                                                                          | User            | No       | Presents information about account linking.                                                                          |
| PluginStatus               |                     | Both              | Verbose (true/false)*                                                    | Admin           | No       | Displays the plugin status. Displays more information if the verbose parameter is set to true.                       |
| ListLinkedChannels         |                     | Both              |                                                                          | Admin           | No       | Presents a list of all channel links.                                                                                |
| ServerStatus               |                     | Discord           |                                                                          | User            | N/A      | Displays the Server Info status.                                                                                     |
| PlayerList                 |                     | Discord           |                                                                          | User            | N/A      | Lists the players currently online on the server.                                                                    |


# **User Settings**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| Optin                      |                     | Eco               |                                                                          | User            | No       | Opts the calling user into chat synchronization.                                                                     |
| OptOut                     |                     | Eco               |                                                                          | User            | No       | Opts the calling user out of chat synchronization.                                                                   |


# **Account Linking**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| LinkAccount                |                     | Eco               | DiscordName                                                              | User            | No       | Links the calling user account to a Discord account. DiscordName is qualified or unqualified name. No nicknames.     |
| UnlinkAccount              |                     | Both              |                                                                          | User            | No       | Unlinks the Eco account from a linked Discord account.                                                               |

# **Images**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| ShowLayer                  |                     | Discord           | LayerName, ShowLayerHistory*, ShowTerrainComparison*                     | User            | N/A      | Posts a link to the requested layer image. Admins can look up any layer while users are limited to visible layers.   |
| ShowMap                    |                     | Discord           |                                                                          | User            | N/A      | Posts a link to an image showing the world map.                                                                      |
| ShowWorldHistory           |                     | Discord           |                                                                          | User            | N/A      | Posts a link to a gif showing the world history.                                                                     |

# **Invites**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| PostInviteMessage          |                     | Both              |                                                                          | User            | Yes      | Posts a Discord invite message to the Eco chat.                                                                      |
| InviteMe                   |                     | Eco               |                                                                          | User            | No       | Opens an invite to the Discord server.                                                                               |


# **Trades**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| Trades                     | DLT                 | Both              | SearchName                                                               | User            | No       | Displays available trades by player, tag, item or store. The search name is case insensitive and will auto complete. |
| WatchTradeDisplay          |                     | Both              | SearchName                                                               | User            | No       | Creates a live updated display of available trades by player, tag, item or store.                                    |
| UnwatchTradeDisplay        |                     | Both              | SearchName                                                               | User            | No       | Removes the live updated display of available trades for a player, tag, item or store.                               |
| WatchTradeFeed             |                     | Both              | SearchName                                                               | User            | No       | Creates a feed where the bot will post trade reports filtered by the search query, as they occur ingame.             |
| UnwatchTradeFeed           |                     | Both              | SearchName                                                               | User            | No       | Removes the trade watcher feed for a player, tag, item or store.                                                     |
| ListTradeWatchers          |                     | Both              |                                                                          | User            | No       | Lists all trade watchers for the calling user.                                                                       |


# **Reports**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| PlayerReport               |                     | Both              | PlayerNameOrID, Report                                                   | User            | No       | Displays the requested report for the given player. Gives a full report if the Report parameter is omitted.          |
| CurrencyReport             |                     | Both              | CurrencyNameOrID, TopHoldersCount*, useTradeCount*, useBackingInfo*      | User            | No       | Displays the Currency Report for the given currency.                                                                 |
| CurrenciesReport           |                     | Both              | CurrencyType, MaxCurrenciesPerType*, HoldersPerCurrency*                 | User            | No       | Displays a report for the top used currencies.                                                                       |
| ElectionReport             |                     | Both              | ElectionNameOrID                                                         | User            | No       | Displays the Election Report for the given election.                                                                 |
| ElectionsReport            |                     | Both              |                                                                          | User            | No       | Displays a report for the currently active elections.                                                                |
| WorkPartyReport            |                     | Both              | WorkPartyNameOrID                                                        | User            | No       | Displays the Work Party Report for the given work party.                                                             |
| WorkPartiesReport          |                     | Both              |                                                                          | User            | No       | Displays a report for the currently active work parties.                                                             |


# **Snippets**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters** (* = Optional)                                            | **Permissions** | **RCON** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|--------------------------------------------------------------------------|-----------------|----------|----------------------------------------------------------------------------------------------------------------------|
| Snippet                    |                     | Both              | SnippetKey                                                               | User            | No       | Posts a predefined snippet to where the command was called from.                                                     |
| EcoSnippet                 |                     | Discord           | SnippetKey                                                               | User            | N/A      | Posts a predefined snippet to Eco.                                                                                   |
