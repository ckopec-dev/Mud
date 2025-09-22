# Key Features

Enhanced User Interface:

Color-coded messages (combat in red, chat in yellow, movement in magenta, etc.)
Clean console interface with proper input/output handling
Welcome screen and help system

## Connection Management:

Connect to any MUD server (not just the one we created)
Auto-reconnection attempts when connection is lost
Manual reconnect functionality
Connection status monitoring

## Client Commands:

/help - Show all client commands
/quit - Exit the client
/reconnect - Reconnect to last server
/connect host:port - Connect to different server
/status - Show connection status
/clear - Clear console screen
/time - Show current time

## Smart Message Processing:

Automatically colors different types of messages
Preserves input line while receiving server messages
Real-time message display without interrupting typing

## Command Line Support:

MudClient.exe (connects to localhost:4000)
MudClient.exe server.com (connects to server.com:4000)
MudClient.exe server.com:5000 (connects to server.com:5000)

# How to Compile

## Method 1: .NET CLI

~~~bash
$ mkdir MudClient
$ cd MudClient
$ dotnet new console
# Replace Program.cs with the client code
$ dotnet run
~~~

## Method 2: Visual Studio

Create new Console App project
Replace Program.cs with the client code
Build and run (Ctrl+F5)

## How to Use

### Run the client:

~~~bash
$ dotnet run
# # or
$ MudClient.exe localhost:4000
~~~

### Connect and play:

Client automatically connects to the server
Enter your character name when prompted
Use game commands like look, go north, say hello
Use client commands like /help, /status

### Example session:

   > look
   > go north
   > attack wolf
   > get sword
   > say Hello everyone!
   > /status
   > /help

## Advanced Features

### Error Handling:

Graceful handling of connection losses
Automatic reconnection attempts
Clear error messages

### Multi-threading:

Separate threads for sending/receiving
Non-blocking input/output
Proper resource cleanup

### Extensibility:

Easy to add new client commands
Configurable message coloring
Modular design for additional features

The client works perfectly with the MUD server we created earlier, but it's also compatible with other MUD servers that use standard telnet-style communication. You can run multiple client instances to test multiplayer functionality!
Pro tip: You can run the server in one terminal and multiple clients in other terminals to simulate a full multiplayer experience.