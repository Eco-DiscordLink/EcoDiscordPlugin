# How to install the DiscordLink plugin on your Eco Server.

Note: If you do _*not*_ have access to the server GUI, you can still use this quide, but will need to input the required values manually. Copy the config template file in the _config_ folder of your server, name it _"DiscordLink.eco"_ and fill in the values directly in the file. Documentation for doing this can be found in the [configuration guide](ConfigurationNoGUI.md).

If you encounter problems, remember that Discordlink has [troubleshooting commands](Commands.md#SAT) and that you can get support in the [Eco Modding Discord](https://discord.gg/pCkWfzQ), just scroll down to the *DiscordLink* section and post in the *help* channel!

---------------

### 1. Download and install EcoWorldCore from [eco.mod.io](https://mod.io/g/eco/m/ecoworldcore1). This is a required dependency for DiscordLink.

### 2. Download the latest DiscordLink release .zip file from [Github](https://github.com/Eco-DiscordLink/EcoDiscordPlugin/releases) or [eco.mod.io](https://mod.io/g/eco/m/discordlink).

### 3. Go to your server's main folder (The one with the .exe), extract the .zip there. Do **NOT** unzip into a separate folder. 

### 4. Start your Eco Server and wait for it to load fully.

![Unzip dialog](images/installation/unzip.png)

### 5. When the server has loaded, navigate to the DiscordLink configuration tab. You'll see a configuration box labelled "BotToken". We need to go create one.

![Config box](images/installation/bot_token.png)

If you are not using the server GUI, you instead need to fill out this field in the _"DiscordLink.eco"_ config file.

![Config field](images/installation/config_field.png)

### 6. Navigate to <https://discordapp.com/developers/applications/me>. Login to Discord and click "New Application".

![Discord app page](images/installation/discord_app.png)

### 7. Name your bot appropriately (this will be the bot's name on your Discord server). Note that the name may not contain the word "Discord".

![Discord new app page](images/installation/new_app.png)

### 8. Click the section labelled "Bot". Click "Create a Bot User".

![Create bot user](images/installation/create_bot_user.png)

### 9. Click "Add Bot". Note that this will trigger a 2FA challenge.

![Add bot](images/installation/add_bot.png)

### 10. This section will now contain the token we need. Click the copy button to get the token into your clipboard.

![Discord token](images/installation/token.png)

### 11. Go back to the Eco server configuration and paste the token into the "BotToken" field.

![Config box](images/installation/bot_token.png)

### 12. Enter the ID of your Discord server into the "Discord Server ID" field. To get this ID, right click your server in Discord and select "Copy ID" and it will be copied to your clipboard.

![Discord Server Name](images/installation/server_name.png)

### 13. Restart the DiscordLink plugin to make the changes take effect.

![Plugin Restart](images/installation/plugin_restart.png)

### 14. Navigate back to the bot's page and the "General Information" tab on the Discord website (see above). Copy the "Application ID" - you will need it in the next section.

![Discord application id](images/installation/application_id.png)

### 15. Before you leave the bot's page, enable the "Server Members Intent" and "Message Content Intent" for your bot.
* The Server members intent allows DiscordLink to search for users in your Discord server when trying to link an Eco account to a Discord account.
* The Message Content Intent is needed for DiscordLink to read chat messages.

![Intents](images/installation/intents.png)

### 16. Go to <https://discordapi.com/permissions.html#268659776>. Paste the Application ID in the bottom left field labelled "Client ID", and add any extra permissions you want the bot to have. Click the link at the bottom.

![Setting permissions](images/installation/permissions_setup.png)

### 17. Set the server you want to invite the bot to, then click "Continue".

![Invite to server](images/installation/invite_bot.png)

### 18. Check your Discord server to see that the bot is online when the server is running.
**Offline**
![Offline bot](images/installation/offline_bot.png)
**Online**
![Online bot](images/installation/online_bot.png)

### 19. Set up a [Chat Channel Link](ConfigurationGUI.md#ChatLink). If you do not have access to the server GUI, use this [guide](ConfigurationNoGUI.md#ChatLink) instead.

### 20. Verify that the config is correct by running the [VerifyConfig](Commands.md#SAT) command or by selecting it in the DiscordLink dropdown in the server GUI.

### 21. Make sure that the bot has the required permissions by running the [VerifyPermissions](#Commands.md#SAT) command or by selecting it in the DiscordLink dropdown in the server GUI.

### 22. Run the _"Echo"_ [command](#Commands.md#SAT) in the Discord channel specified in the Chat Channel Link to see that it is properly sending message to Eco and receiving messages back. 

**Echo command**  
![Echo Command](images/installation/echo_command.png)  

**Discord server GUI chat display showing that the message reached the Eco server.**  
![Echo Command Response Eco](images/installation/echo_eco.png)  

**Crosspost back to Discord showing that DiscordLink caught the message and forwarded it as configured in the Chat Channel Link cofiguration.**  
![Echo command Response Discord](images/installation/echo_discord.png)  

### 22. You're done! [Configure](ConfigurationGUI.md) any other options and features you want and enjoy using DiscordLink!  
