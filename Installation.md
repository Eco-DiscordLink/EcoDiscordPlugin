# How to install the DiscordLink plugin on your Eco Server.

Note: If you do _*not*_ have access to the server GUI, copy the config file from the _"ExampleConfig"_ folder and fill in the values directly in the file. Documentation for doing this can be found in the [configuration guide](ConfigurationNoGUI.md).

### 1. Download the latest DiscordLink release .zip file from <https://github.com/Spoffy/EcoDiscordPlugin/releases>

![Download page](images/installation/1.png)

### 2. Go to your server's mods folder (Eco Server/Mods/) and extract the .zip there and start your server.

![Unzip dialog](images/installation/2.png)

### 3. When the server has loaded, navigate to the DiscordLink configuration tab. You'll see a configuration box labelled "BotToken". We need to go create one.

![Config box](images/installation/3.png)

### 4. Navigate to <https://discordapp.com/developers/applications/me>. Login to Discord and click "New App".

![Discord app page](images/installation/4.png)

### 5. Name your bot appropriately (this will be the bot's name on your Discord server).

![Discord new app page](images/installation/5.png)

### 6. Click the section labelled "Bot". Click "Create a Bot User".

![Create bot user](images/installation/6.png)

### 7. Click "Add Bot"

![Add bot](images/installation/7.png)

### 8. This section will now contain the token we need. Click the copy button to get the token into your clipboard.

![Discord token](images/installation/8.png)

### 9. Paste the token into the "BotToken" section of the server GUI. If you check the server's console, you should see "Connected to Discord". If you don't, double check your token.

![Token installation message](images/installation/9.png)

### 10. Navigate back to the bot's page on the Discord website (see above). Copy the "Client ID" that appears at the top - you will need it in the next section.

![Discord client id](images/installation/10.png)

### 11. Go to <https://discordapi.com/permissions.html#216064>. Paste the Client ID in the bottom left field labelled "Client ID", and add any extra permissions you want the bot to have. Click the link at the bottom.

![Setting permissions](images/installation/11.png)

### 12. Set the server you want to invite the bot to, then click "Continue".

![Invite to server](images/installation/12.png)

### 13. Check your Discord server to see that the bot is online when the server is running.
**Offline**
![Invite to server](images/installation/13_1.png)
**Online**
![Invite to server](images/installation/13_2.png)

### 14 Run the _"Echo"_ command in a Discord channel where the bot has permissions, to see that it is working properly.

**Echo command**
![Invite to server](images/installation/14_1.png)

**Discord server GUI chat display showing that the message reached the Eco server**
![Invite to server](images/installation/14_2.png)

**Discord bot crosspost if a [Channel Link](ConfigurationGUI.md) has been set up**
![Invite to server](images/installation/14_3.png)

### 15. You're done! [Configure](ConfigurationGUI.md) any other options you want in server GUI, or change the Bot's permissions as if they're any other user in Discord.
