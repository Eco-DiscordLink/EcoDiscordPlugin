# How to install the DiscordLink plugin on your Eco Server.

### 1. Download the latest DiscordLink release .zip file from <https://github.com/Spoffy/EcoDiscordPlugin/releases>

![Download page](images/tutorials/1.png)

### 2. Go to your server's mods folder (Eco Server/Mods/) and extract the .zip there.

![Unzip dialog](images/tutorials/2.png)

### 3. When the server has loaded, navigate to the DiscordLink configuration tab. You'll see a configuration box labelled "BotToken". We need to go create one.

![Config box](images/tutorials/3.png)

### 4. Navigate to <https://discordapp.com/developers/applications/me> . Login to Discord and click "New App".

![Discord app page](images/tutorials/4.png)

### 5. Name your bot appropriately (this will be the bot's name on your Discord server). Add a description if you fancy.

![Discord new app page](images/tutorials/5.png)

### 6. Scroll down to the section labelled "Bot". Click "Create a Bot User".

![Create bot user](images/tutorials/6.png)

### 7. This section will now contain the token we need. Click to reveal the token (if necessary), then copy the token.


![Discord token](images/tutorials/7_1.png)

### 8. Copy the token into the "BotToken" section of the server GUI. If you check the server's console, you should see "Connected to Discord". If you don't, double check your token.

![Token installation](images/tutorials/8.png)

![Token installation message](images/tutorials/8_2.png)

### 9. Navigate back to the bot's page on the Discord website (see above). Copy the "Client ID" that appears at the top - you'll need it in the next section.

![Discord client id](images/tutorials/9.png)

### 10. Go to <https://discordapi.com/permissions.html#216064> . Paste the Client ID in the bottom left field labelled "Client ID", and add any extra permissions you want the bot to have. Click the link at the bottom.

![Setting permissions](images/tutorials/10.png)

### 11. Set the server you want to invite the bot to, then click "Authorize".

![Invite to server](images/tutorials/11.png)

### 12. You're done! Configure any other options you want in server GUI, or change the Bot's permissions as if they're any other user in Discord.
