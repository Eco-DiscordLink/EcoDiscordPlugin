# DiscordLink Development Environment Setup Guide

1. **Clone the repository and make sure you are on the correct branch.**
2. **Download the modkit Eco server DLLs from [Play.Eco](https://play.eco).** 
3a. **Copy the reference assemblies into the "Dependencies" subdirectory.** 
OR 
3b (Dev Tier). **Place the Eco repository at ../Eco and build the server.** 
4. **Place a copy of the game server at "../EcoServer" from the repository root.** 
5. **Open the DiscordLink .sln file using Visual Studio 2019 or newer.** 
3. **Install the required NuGets via the Visual Studio NuGet browser.** 
5. **Attempt to build and run the mod.** 
This will:  
   1. Create a new build of the selected configuration. 
   2. Copy the relevant DLLs and PDB files to the server. 
   3. Execute the server .exe. 
   4. Connect the debugger to the server application, allowing you to use breakpoints and other runtime debugging tools. 
