# How to set up the development environment for DiscordLink

Note: This guide assumes you have access to the Eco server DLLs either via dev tier game access or by other means.

##### 1. Clone the repository and make sure you are on the correct branch.
###### Which branch you should be using should be clear from looking at the branch list.
##### 2. Open the .sln file using Visual Studio 2019.
##### 3. Install the relevant NuGets
###### Note that DSharpPlus needs to be downloaded from [SlimGet](https://nuget.emzi0767.com/gallery/packages).
##### 4. Copy the Eco Server DLLs into the "Dependencies" subdirectory.
##### 5. Place a copy of the game server at "../EcoServer" from the repository root.
##### 6. Attempt to build and run the mod.
###### This will:
1. Create a new build of the selected configuration.
2. Copy the relevant DLLs and PDB files to the server.
3. Execute the server .exe
4. Connect the debugger to the server application, allowing you to use breakpoints and other runtime debugging tools.
