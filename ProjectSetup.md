# How to set up the development environment for DiscordLink
Note: This guide assumes you have access to the Eco server DLLs either via dev tier game access or by other means.

1. **Clone the repository and make sure you are on the correct branch.
2. **Open the .sln file using Visual Studio 2019.**
3. **Install the relevant NuGets.
4. **Copy the Eco Server DLLs**(\*1) **into the "Dependencies" subdirectory, or place the Eco repository at ../Eco and build the server.** 
5. **Place a copy of the game server at "../EcoServer" from the repository root.**
6. **Attempt to build and run the mod.**
This will:  
   1. Create a new build of the selected configuration.
   2. Copy the relevant DLLs and PDB files to the server.
   3. Execute the server .exe.
   4. Connect the debugger to the server application, allowing you to use breakpoints and other runtime debugging tools.

(\*1) The Eco reference assemblies are found in the modkit, which can be downloaded from [Play.Eco](https://play.eco).
