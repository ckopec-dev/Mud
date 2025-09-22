# Core Features

## Networking & Connections:

TCP socket server that handles multiple simultaneous connections
Asynchronous client handling with proper connection management
Graceful disconnection and error handling

## Player System:

Character creation with names
Health, mana, level, experience, and gold systems
Inventory and equipment management
Level progression with stat increases

## World System:

Room-based world with descriptions and connections
Items that can be picked up and stored
Monster spawning and combat system
Multiple interconnected areas (town, forest, cave, shop, tavern)

## Command System:

Extensible command architecture
Built-in commands: say, look, go, attack, inventory, get, help, quit
Shortcuts (l for look, i for inventory, n/s/e/w for movement)
Real-time command processing

## Game Mechanics:

Turn-based combat with monsters
Experience and gold rewards
Health/mana regeneration over time
Monster respawning system
Room-based chat system

## How to Use

Compile and run the server (default port 4000)
Connect with a telnet client: telnet localhost 4000
Enter your character name when prompted
Start playing with commands like:

look - examine your surroundings
go north - move between rooms
say hello - talk to other players
attack wolf - fight monsters
get sword - pick up items
help - see all available commands


# Architecture Highlights

Modular design with separate classes for Player, Room, Monster, Item, and Command
Thread-safe player management with concurrent collections
Extensible command system - easy to add new commands
Game loop for continuous world updates (health regen, monster spawning)
Error handling throughout for network issues and game logic

The server creates a small fantasy world with a town square, dark forest, mysterious cave, weapon shop, and tavern. Players can explore, fight monsters, collect items, gain experience, and interact with each other in real-time.
This implementation provides a solid foundation that can be extended with features like player persistence, more complex combat, crafting systems, guilds, and much more!


# Here's how to compile and run the MUD server:

## Method 1: Using .NET CLI (Recommended)

Install .NET SDK (if not already installed):

Download from https://dotnet.microsoft.com/download
Install .NET 6.0 or later

Create a new console project:

~~~bash
$ mkdir MudServer
$ cd MudServer
$ dotnet new console
~~~

Replace the Program.cs file with the MUD server code from the artifact
Compile and run:

~~~bash
$ dotnet run
~~~

## Method 2: Using Visual Studio

Create a new project:

Open Visual Studio
File → New → Project
Select "Console App (.NET)"
Name it "MudServer"


Replace Program.cs with the MUD server code
Build and run:

Press Ctrl+F5 to run without debugging
Or press F5 to run with debugging



## Method 3: Command Line Compilation

Save the code as Program.cs in a folder
Compile directly:

~~~bash
$ csc Program.cs
# (Requires Visual Studio Build Tools or SDK installed)
~~~

Run the executable:

~~~bash
$ Program.exe
~~~

# Testing the Server
Once compiled and running, you should see:
MUD Server started on port 4000
Press 'q' to quit
Connect to test:

## Using Telnet:

~~~bash
$ telnet localhost 4000
~~~

## Using PuTTY (Windows):

Set connection type to "Raw" or "Telnet"
Host: localhost, Port: 4000


## Using netcat (Linux/Mac):

~~~bash
$ nc localhost 4000
~~~ 

## Troubleshooting

If you get compilation errors:

Make sure you're using .NET 6.0 or later (the code uses modern C# features)
Check that all using statements are included

If the server won't start:

Make sure port 4000 isn't already in use
Try running as administrator (if on Windows)
Check firewall settings

Connection issues:

Verify the server is running and shows "MUD Server started on port 4000"
Make sure your telnet client supports the connection type
Try connecting from the same machine first before testing remotely

The server will accept multiple simultaneous connections, so you can open several telnet sessions to test multiplayer functionality!