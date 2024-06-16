# DiscordLink Development Environment Setup Guide

1. **Clone the repository and make sure you are on the correct branch.**
2. **Download the modkit Eco server DLLs from [Play.Eco](https://play.eco).**  
3a. **Copy the reference assemblies into the "Dependencies" subdirectory.**  
OR  
3b (Dev Tier). **Place the Eco repository at ../Eco and build the server.** Note that this is also required in order to attach a debugger.
4. **Download the latest [MightyMooseCore](https://mod.io/g/eco/m/mightymoosecore) dll and place it into the "Dependencies" subdirectory.**
4. **Open the DiscordLink .sln file using Visual Studio 2022 or newer.** 
5. **Install the required NuGets via the Visual Studio NuGet browser.** 
6. **Attempt to build and run the mod.** 
This will:  
   1. Create a new build of the selected configuration. 
   2. Copy the relevant DLLs and PDB files to the server. 
   3. Execute the server .exe. 
   4. Connect the debugger to the server application, allowing you to use breakpoints and other runtime debugging tools. 
