using System.Net.Sockets;
using System.Text;

namespace MudClient
{
    public class MudClient
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private StreamWriter? _writer;
        private StreamReader? _reader;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected;
        private string? _serverHost;
        private int _serverPort;
        
        // Console color management
        private readonly Lock _consoleLock = new();
        
        public MudClient()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        public async Task ConnectAsync(string host = "localhost", int port = 4000)
        {
            try
            {
                _serverHost = host;
                _serverPort = port;
                
                WriteToConsole($"Connecting to {host}:{port}...", ConsoleColor.Yellow);
                
                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                
                _stream = _client.GetStream();
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                _reader = new StreamReader(_stream, Encoding.UTF8);
                
                _isConnected = true;
                
                WriteToConsole($"Connected to MUD server!", ConsoleColor.Green);
                WriteToConsole("Type 'help' for available commands once logged in.", ConsoleColor.Cyan);
                WriteToConsole("Use '/quit' to disconnect from the server.", ConsoleColor.Cyan);
                WriteToConsole("Use '/reconnect' to reconnect if connection is lost.", ConsoleColor.Cyan);
                WriteToConsole("----------------------------------------", ConsoleColor.Gray);
                
                // Start message receiving task
                _ = Task.Run(ReceiveMessagesAsync);
                
                // Start input handling task
                await HandleUserInputAsync();
            }
            catch (Exception ex)
            {
                WriteToConsole($"Connection failed: {ex.Message}", ConsoleColor.Red);
                _isConnected = false;
            }
        }
        
        private async Task ReceiveMessagesAsync()
        {
            try
            {
                while (_isConnected && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (_reader == null) break;
                    string? message = await _reader.ReadLineAsync();
                    if (message == null)
                    {
                        WriteToConsole("Server disconnected.", ConsoleColor.Red);
                        _isConnected = false;
                        break;
                    }
                    
                    // Process and display the message
                    ProcessServerMessage(message);
                }
            }
            catch (Exception ex)
            {
                if (_isConnected)
                {
                    WriteToConsole($"Connection error: {ex.Message}", ConsoleColor.Red);
                    _isConnected = false;
                }
            }
        }
        
        private void ProcessServerMessage(string message)
        {
            // Determine message type and color
            ConsoleColor color = ConsoleColor.White;
            
            if (message.Contains("says:") || message.Contains("You say:"))
            {
                color = ConsoleColor.Yellow;
            }
            else if (message.Contains("attacks") || message.Contains("damage") || message.Contains("defeated"))
            {
                color = ConsoleColor.Red;
            }
            else if (message.Contains("arrives") || message.Contains("leaves") || message.Contains("enters") || message.Contains("left"))
            {
                color = ConsoleColor.Magenta;
            }
            else if (message.Contains("picks up") || message.Contains("You pick up"))
            {
                color = ConsoleColor.Green;
            }
            else if (message.Contains("===") || message.Contains("Exits:") || message.Contains("Items here:"))
            {
                color = ConsoleColor.Cyan;
            }
            else if (message.Contains("Level") || message.Contains("Experience") || message.Contains("Congratulations"))
            {
                color = ConsoleColor.Yellow;
            }
            else if (message.Contains("Welcome") || message.Contains("Enter"))
            {
                color = ConsoleColor.Green;
            }
            else if (message.Contains("Error") || message.Contains("can't") || message.Contains("Invalid"))
            {
                color = ConsoleColor.Red;
            }
            
            WriteServerMessage(message, color);
        }
        
        private void WriteServerMessage(string message, ConsoleColor color)
        {
            lock (_consoleLock)
            {
                // Clear current input line
                int currentLine = Console.CursorTop;
                Console.SetCursorPosition(0, currentLine);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, currentLine);
                
                // Write server message
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
                
                // Restore input prompt
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("> ");
                Console.ResetColor();
            }
        }
        
        private void WriteToConsole(string message, ConsoleColor color)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }
        
        private async Task HandleUserInputAsync()
        {
            WriteToConsole("Enter commands (or '/help' for client commands):", ConsoleColor.Gray);
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("> ");
                Console.ResetColor();
                
                string? input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                
                // Handle client commands
                if (input.StartsWith('/'))
                {
                    await HandleClientCommand(input);
                    continue;
                }
                
                // Send command to server
                if (_isConnected)
                {
                    try
                    {
                        if (_writer == null) continue;
                        await _writer.WriteLineAsync(input);
                    }
                    catch (Exception ex)
                    {
                        WriteToConsole($"Failed to send command: {ex.Message}", ConsoleColor.Red);
                        _isConnected = false;
                    }
                }
                else
                {
                    WriteToConsole("Not connected to server. Use '/reconnect' to connect.", ConsoleColor.Red);
                }
            }
        }
        
        private async Task HandleClientCommand(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLower();
            
            switch (cmd)
            {
                case "/help":
                    ShowClientHelp();
                    break;
                    
                case "/quit":
                case "/exit":
                    WriteToConsole("Disconnecting from server...", ConsoleColor.Yellow);
                    await DisconnectAsync();
                    Environment.Exit(0);
                    break;
                    
                case "/reconnect":
                    await ReconnectAsync();
                    break;
                    
                case "/status":
                    ShowConnectionStatus();
                    break;
                    
                case "/clear":
                    Console.Clear();
                    WriteToConsole("Console cleared.", ConsoleColor.Green);
                    break;
                    
                case "/connect":
                    if (parts.Length >= 2)
                    {
                        string[] hostPort = parts[1].Split(':');
                        string host = hostPort[0];
                        int port = hostPort.Length > 1 && int.TryParse(hostPort[1], out int p) ? p : 4000;
                        
                        if (_isConnected)
                        {
                            await DisconnectAsync();
                        }
                        
                        await ConnectAsync(host, port);
                    }
                    else
                    {
                        WriteToConsole("Usage: /connect <host:port> (port defaults to 4000)", ConsoleColor.Yellow);
                    }
                    break;
                    
                case "/time":
                    WriteToConsole($"Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", ConsoleColor.Cyan);
                    break;
                    
                default:
                    WriteToConsole($"Unknown client command: {cmd}", ConsoleColor.Red);
                    WriteToConsole("Type '/help' for available client commands.", ConsoleColor.Yellow);
                    break;
            }
        }
        
        private void ShowClientHelp()
        {
            WriteToConsole("=== MUD Client Commands ===", ConsoleColor.Cyan);
            WriteToConsole("/help        - Show this help message", ConsoleColor.White);
            WriteToConsole("/quit        - Disconnect and exit client", ConsoleColor.White);
            WriteToConsole("/reconnect   - Reconnect to the last server", ConsoleColor.White);
            WriteToConsole("/connect <host:port> - Connect to a different server", ConsoleColor.White);
            WriteToConsole("/status      - Show connection status", ConsoleColor.White);
            WriteToConsole("/clear       - Clear the console screen", ConsoleColor.White);
            WriteToConsole("/time        - Show current time", ConsoleColor.White);
            WriteToConsole("", ConsoleColor.White);
            WriteToConsole("Game Commands (sent to server):", ConsoleColor.Cyan);
            WriteToConsole("help         - Show game commands", ConsoleColor.White);
            WriteToConsole("look (l)     - Look around current room", ConsoleColor.White);
            WriteToConsole("go <dir>     - Move in a direction (n/s/e/w)", ConsoleColor.White);
            WriteToConsole("say <msg>    - Say something to other players", ConsoleColor.White);
            WriteToConsole("inventory (i) - Show your inventory", ConsoleColor.White);
            WriteToConsole("get <item>   - Pick up an item", ConsoleColor.White);
            WriteToConsole("attack <monster> - Attack a monster", ConsoleColor.White);
            WriteToConsole("quit         - Quit the game (server side)", ConsoleColor.White);
            WriteToConsole("=============================", ConsoleColor.Cyan);
        }
        
        private void ShowConnectionStatus()
        {
            if (_isConnected)
            {
                WriteToConsole($"Connected to: {_serverHost}:{_serverPort}", ConsoleColor.Green);
            }
            else
            {
                WriteToConsole("Not connected to any server", ConsoleColor.Red);
                WriteToConsole($"Last server: {_serverHost}:{_serverPort}", ConsoleColor.Yellow);
            }
        }
        
        private async Task ReconnectAsync()
        {
            if (_isConnected)
            {
                WriteToConsole("Already connected. Disconnecting first...", ConsoleColor.Yellow);
                await DisconnectAsync();
            }
            
            if (string.IsNullOrEmpty(_serverHost))
            {
                WriteToConsole("No previous server to reconnect to. Use /connect <host:port>", ConsoleColor.Red);
                return;
            }
            
            await ConnectAsync(_serverHost, _serverPort);
        }
        
        public async Task DisconnectAsync()
        {
            try
            {
                await Task.Run(() => 
                {
                    _isConnected = false;
                    _cancellationTokenSource?.Cancel();

                    _writer?.Close();
                    _reader?.Close();
                    _stream?.Close();
                    _client?.Close();

                    WriteToConsole("Disconnected from server.", ConsoleColor.Yellow);
                });
                
            }
            catch (Exception ex)
            {
                WriteToConsole($"Error during disconnect: {ex.Message}", ConsoleColor.Red);
            }
        }
        
        // Auto-reconnect functionality
        private async Task AttemptAutoReconnect()
        {
            if (string.IsNullOrEmpty(_serverHost)) return;
            
            WriteToConsole("Connection lost. Attempting to reconnect in 3 seconds...", ConsoleColor.Yellow);
            await Task.Delay(3000);
            
            int attempts = 0;
            const int maxAttempts = 5;
            
            while (attempts < maxAttempts && !_isConnected)
            {
                attempts++;
                WriteToConsole($"Reconnection attempt {attempts}/{maxAttempts}...", ConsoleColor.Yellow);
                
                try
                {
                    await ConnectAsync(_serverHost, _serverPort);
                    if (_isConnected)
                    {
                        WriteToConsole("Reconnected successfully!", ConsoleColor.Green);
                        return;
                    }
                }
                catch (Exception)
                {
                    // Ignore connection errors during auto-reconnect
                }
                
                await Task.Delay(2000); // Wait 2 seconds between attempts
            }
            
            WriteToConsole($"Failed to reconnect after {maxAttempts} attempts.", ConsoleColor.Red);
            WriteToConsole("Use '/reconnect' to try again manually.", ConsoleColor.Yellow);
        }
        
        // Enhanced main method with argument parsing
        public static async Task Main(string[] args)
        {
            Console.Title = "MUD Client";
            
            WriteWelcomeMessage();
            
            string host = "localhost";
            int port = 4000;
            
            // Parse command line arguments
            if (args.Length >= 1)
            {
                if (args[0].Contains(':'))
                {
                    string[] parts = args[0].Split(':');
                    host = parts[0];
                    if (parts.Length > 1 && int.TryParse(parts[1], out int p))
                    {
                        port = p;
                    }
                }
                else
                {
                    host = args[0];
                }
            }
            
            if (args.Length >= 2 && int.TryParse(args[1], out int portArg))
            {
                port = portArg;
            }
            
            var client = new MudClient();
            
            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine();
                await client.DisconnectAsync();
                Environment.Exit(0);
            };
            
            await client.ConnectAsync(host, port);
        }
        
        private static void WriteWelcomeMessage()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════╗");
            Console.WriteLine("║            MUD Client v1.0            ║");
            Console.WriteLine("║     Multi-User Dungeon Client         ║");
            Console.WriteLine("╚═══════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Usage: MudClient.exe [host[:port]]");
            Console.WriteLine("Example: MudClient.exe localhost:4000");
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}