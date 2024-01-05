# Commands

## Invoking Eco Commands
Eco commands are invoked by typing /DiscordLink [CommandName] into the Eco chat.
Typing only /DiscordLink or the alias /DL will invoke the help message and provide you with information about the available commands.

## Invoking Discord Commands
Discord commands are invoked by typing "/" followed by the command you wish to run.

## Commads
All Eco and Discord commands are case insensitive.

# **Plugin Management**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters**                                                          | **Permissions** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|-------------------------------------------------------------------------|-----------------|----------------------------------------------------------------------------------------------------------------------|
| DiscordLink                | DL                  | Eco               |                                                                         | User            | Displays a list of all available DiscordLink commands. This is the parent command of all other DiscordLink commands. |
| RestartPlugin              | 			           | Both              |                                                                         | Admin           | Restarts the plugin.                                                                                                 |
| ResetPersistentData        |                     | Both              |                                                                         | Admin           | Removes all persistent storage data.                                                                                 |
| ResetWorldData             |                     | Both              |                                                                         | Admin           | Resets world data as if a new world had been created.                                                                |
| Update                     |                     | Both              |                                                                         | Admin           | Forces an update of all modules.                                                                                     |
| ClearRoles                 |                     | Both              |                                                                         | Admin           | Deletes all Discord roles created and tracked by DiscordLink.                                                        |


# <a id="SAT"></a>**Setup and Troubleshooting**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters**                                                          | **Permissions** | **Description**                                                                                                      |
|----------------------------|-------------------- |-------------------|-------------------------------------------------------------------------|-----------------|----------------------------------------------------------------------------------------------------------------------|
| VerifyConfig               |                     | Both              |                                                                         | Admin           | Checks configuration setup and reports any errors.                                                                   |
| VerifyPermissions          |                     | Both              |                                                                         | Admin           | Checks all permissions and intents needed for the current configuration and reports any missing ones.                |
| VerifyIntents              |                     | Both              |                                                                         | Admin           | Checks all intents needed and reports any missing ones.                                                              |
| VerifyServerPermissions    |                     | Both              |                                                                         | Admin           | Checks all server permissions needed and reports any missing ones.                                                   |
| VerifyChannelPermissions   |                     | Both              | ChannelNameOrID                                                         | Admin           | Checks all permissions needed for the given channel and reports any missing ones.                                    |
| Echo                       |                     | Discord           | Message                                                                 | Admin           | Sends the provided message to Eco and back to Discord again if a chat link is configured for the channel.            |


# **Utility**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters**                                                          | **Permissions** | **Description**                                                                                                      |              
|----------------------------|---------------------|-------------------|-------------------------------------------------------------------------|-----------------|----------------------------------------------------------------------------------------------------------------------|
| EcoCommand          		 |                     | Discord           | Eco Command                                                             | Dynamic         | Executes the inputted eco command. The parameter should be styled as a full ingame Eco command.                      |


# **Info**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters**                                                          | **Permissions** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|-------------------------------------------------------------------------|-----------------|----------------------------------------------------------------------------------------------------------------------|
| Version                    |                     | Both              |                                                                         | User            | Displays the installed and latest available plugin version.                                                          |
| About                      |                     | Both              |                                                                         | User            | Displays information about the DiscordLink plugin.                                                                   |
| LinkInformation            |                     | Both              |                                                                         | User            | Presents information about account linking.                                                                          |
| PluginStatus               |                     | Both              | Verbose (true/false)                                                    | Admin           | Displays the plugin status. Displays more information if the verbose parameter is set to true.                       |
| ListLinkedChannels         |                     | Both              |                                                                         | Admin           | Presents a list of all channel links.                                                                                |
| ServerStatus               |                     | Discord           |                                                                         | User            | Displays the Server Info status.                                                                                     |
| PlayerList                 |                     | Discord           |                                                                         | User            | Lists the players currently online on the server.                                                                    |


# **User Settings**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters**                                                          | **Permissions** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|-------------------------------------------------------------------------|-----------------|----------------------------------------------------------------------------------------------------------------------|
| Optin                      |                     | Eco               |                                                                         | User            | Opts the calling user into chat synchronization.                                                                     |
| OptOut                     |                     | Eco               |                                                                         | User            | Opts the calling user out of chat synchronization.                                                                   |


# **Account Linking**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters**                                                          | **Permissions** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|-------------------------------------------------------------------------|-----------------|----------------------------------------------------------------------------------------------------------------------|
| LinkAccount                |                     | Eco               | DiscordName                                                             | User            | Links the calling user account to a Discord account. DiscordName is qualified or unqualified name. No nicknames.     |
| UnlinkAccount              |                     | Eco               |                                                                         | User            | Unlinks the Eco account from a linked Discord account.                                                               |


# **Invites**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters**                                                          | **Permissions** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|-------------------------------------------------------------------------|-----------------|----------------------------------------------------------------------------------------------------------------------|
| PostInviteMessage          |                     | Both              |                                                                         | User            | Posts a Discord invite message to the Eco chat.                                                                      |
| InviteMe                   |                     | Eco               |                                                                         | User            | Opens an invite to the Discord server.                                                                               |


# **Trades**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters**                                                          | **Permissions** | **Description**                                                                                                      |
|----------------------------|-------------------- |-------------------|-------------------------------------------------------------------------|-----------------|----------------------------------------------------------------------------------------------------------------------|
| Trades                     | DLT                 | Both              | SearchName                                                              | User            | Displays available trades by player, tag, item or store. The search name is case insensitive and will auto complete. |
| WatchTradeDisplay          |                     | Both              | SearchName                                                              | User            | Creates a live updated display of available trades by player, tag, item or store.                                    |
| UnwatchTradeDisplay        |                     | Both              | SearchName                                                              | User            | Removes the live updated display of available trades for a player, tag, item or store.                               |
| WatchTradeFeed             |                     | Both              | SearchName                                                              | User            | Creates a feed where the bot will post trade reports filtered by the search query, as they occur ingame.             |
| UnwatchTradeFeed           |                     | Both              | SearchName                                                              | User            | Removes the trade watcher feed for a player, tag, item or store.                                                     |
| ListTradeWatchers          |                     | Both              |                                                                         | User            | Lists all trade watchers for the calling user.                                                                       |


# **Reports**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters**                                                          | **Permissions** | **Description**                                                                                                      |
|----------------------------|-------------------- |-------------------|-------------------------------------------------------------------------|-----------------|----------------------------------------------------------------------------------------------------------------------|
| PlayerReport               |                     | Both              | PlayerNameOrID, Report                                                  | User            | Displays the requested report for the given player. Gives a full report if the Report parameter is omitted.          |
| CurrencyReport             |                     | Both              | CurrencyNameOrID                                                        | User            | Displays the Currency Report for the given currency.                                                                 |
| CurrenciesReport           |                     | Both              | CurrencyType, MaxCurrenciesPerType, HoldersPerCurrency                  | User            | Displays a report for the top used currencies.                                                                       |
| ElectionReport             |                     | Both              | ElectionNameOrID                                                        | User            | Displays the Election Report for the given election.                                                                 |
| ElectionsReport            |                     | Both              |                                                                         | User            | Displays a report for the currently active elections.                                                                |
| WorkPartyReport            |                     | Both              | WorkPartyNameOrID                                                       | User            | Displays the Work Party Report for the given work party.                                                             |
| WorkPartiesReport          |                     | Both              |                                                                         | User            | Displays a report for the currently active work parties.                                                             |


# **Snippets**
| **Name**                   | **Alias**           | **Discord/Eco**   | **Parameters**                                                          | **Permissions** | **Description**                                                                                                      |
|----------------------------|---------------------|-------------------|-------------------------------------------------------------------------|-----------------|----------------------------------------------------------------------------------------------------------------------|
| Snippet                    |                     | Both              | SnippetKey                                                              | User            | Posts a predefined snippet to where the command was called from.                                                     |
| EcoSnippet                 |                     | Discord           | SnippetKey                                                              | User            | Posts a predefined snippet to Eco.                                                                                   |
