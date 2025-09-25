using MudServer.Commands;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace MudServer
{
    // Main server class
    public class MudServer
    {
        public static MudServer? Instance { get; private set; }
        
        private TcpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly Lock _playersLock = new();
        
        public ConcurrentDictionary<string, Player> Players { get; } = new();
        public Dictionary<string, Room> Rooms { get; } = [];
        public Dictionary<string, Command> Commands { get; } = [];
        public Dictionary<string, Item> Items { get; } = [];
        private static readonly string[] sourceArray = ["n", "s", "e", "w"];

        private MudServer()
        {
            Instance = this;
            InitializeCommands();
            InitializeWorld();
            InitializeItems();
        }
        
        private void InitializeCommands()
        {
            var commands = new Command[]
            {
                new SayCommand(),
                new LookCommand(),
                new MoveCommand(),
                new AttackCommand(),
                new InventoryCommand(),
                new GetCommand(),
                new HelpCommand(),
                new QuitCommand()
            };
            
            foreach (var cmd in commands)
            {
                Commands[cmd.Name] = cmd;
            }
        }
        
        private void InitializeItems()
        {
            Items["sword"] = new Item
            {
                Name = "Iron Sword",
                Description = "A sturdy iron sword",
                Type = "weapon",
                Value = 50,
                Stats = new() { ["attack"] = 15 }
            };
            
            Items["armor"] = new Item
            {
                Name = "Leather Armor",
                Description = "Basic leather protection",
                Type = "armor",
                Value = 30,
                Stats = new() { ["defense"] = 10 }
            };
            
            Items["potion"] = new Item
            {
                Name = "Health Potion",
                Description = "Restores 30 health",
                Type = "consumable",
                Value = 20,
                Stats = new() { ["heal"] = 30 }
            };
        }
        
        private void InitializeWorld()
        {
            // Town Square
            Rooms["town_square"] = new Room
            {
                Id = "town_square",
                Name = "Town Square",
                Description = "A bustling town square with a fountain in the center. Merchants hawk their wares and adventurers gather here.",
                Exits = new() { ["north"] = "forest", ["east"] = "shop", ["west"] = "tavern" },
                Items = ["potion"]
            };
            
            // Forest
            Rooms["forest"] = new Room
            {
                Id = "forest",
                Name = "Dark Forest",
                Description = "A dense, dark forest. Strange sounds echo from the shadows.",
                Exits = new() { ["south"] = "town_square", ["north"] = "cave" },
                Monsters =
                [
                    new Monster
                    {
                        Name = "Wolf",
                        Health = 30,
                        MaxHealth = 30,
                        AttackPower = 15,
                        Experience = 15,
                        Gold = 5,
                        Loot = ["wolf_pelt"]
                    }
                ]
            };
            
            // Cave
            Rooms["cave"] = new Room
            {
                Id = "cave",
                Name = "Mysterious Cave",
                Description = "A damp cave with glittering crystals on the walls. Something lurks in the depths.",
                Exits = new() { ["south"] = "forest" },
                Items = ["sword"],
                Monsters =
                [
                    new Monster
                    {
                        Name = "Goblin",
                        Health = 40,
                        MaxHealth = 40,
                        AttackPower = 12,
                        Experience = 30,
                        Gold = 15,
                        Loot = ["goblin_ear", "rusty_dagger"]
                    }
                ]
            };
            
            // Shop
            Rooms["shop"] = new Room
            {
                Id = "shop",
                Name = "Weapon Shop",
                Description = "A cluttered weapon shop filled with gleaming blades and sturdy armor.",
                Exits = new() { ["west"] = "town_square" },
                Items = ["armor", "sword"]
            };
            
            // Tavern
            Rooms["tavern"] = new Room
            {
                Id = "tavern",
                Name = "The Prancing Pony",
                Description = "A cozy tavern with a warm fire and the smell of ale in the air.",
                Exits = new() { ["east"] = "town_square" },
                Items = ["potion", "bread"]
            };
        }
        
        public Room? GetRoom(string roomId)
        {
            Rooms.TryGetValue(roomId, out var room);
            return room;
        }
        
        public async Task StartAsync(int port = 4000)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _cancellationTokenSource = new CancellationTokenSource();
            
            _listener.Start();
            Console.WriteLine($"MUD Server started on port {port}");
            
            // Accept connections
            _ = Task.Run(AcceptConnectionsAsync);
            
            // Game loop
            _ = Task.Run(GameLoopAsync);
            
            // Wait for shutdown
            Console.WriteLine("Press 'q' to quit");
            while (Console.ReadKey().KeyChar != 'q') { }
            
            await StopAsync();
        }
        
        private async Task AcceptConnectionsAsync()
        {
            if (_listener == null || _cancellationTokenSource == null) return;
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(tcpClient));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting connection: {ex.Message}");
                }
            }
        }
        
        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            var connection = new PlayerConnection(tcpClient);
            
            try
            {
                // Login process
                connection.SendMessage("Welcome to the MUD Server!");
                connection.SendMessage("Enter your character name: ");
                
                string? playerName = await connection.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    connection.SendMessage("Invalid name. Disconnecting.");
                    connection.Close();
                    return;
                }
                
                playerName = playerName.Trim();
                
                // Check if player already exists
                lock (_playersLock)
                {
                    if (Players.ContainsKey(playerName))
                    {
                        connection.SendMessage("That name is already taken. Disconnecting.");
                        connection.Close();
                        return;
                    }
                    
                    // Create new player
                    var player = new Player(connection)
                    {
                        Name = playerName
                    };
                    
                    connection.PlayerId = playerName;
                    Players[playerName] = player;
                    
                    // Add to starting room
                    var startRoom = GetRoom(player.CurrentRoom);
                    startRoom?.AddPlayer(playerName);
                    startRoom?.BroadcastMessage($"{playerName} has entered the world!", playerName);
                }
                
                connection.SendMessage($"Welcome, {playerName}!");
                new LookCommand().Execute(Players[playerName], []);
                new HelpCommand().Execute(Players[playerName], []);
                
                // Command loop
                if (_cancellationTokenSource == null) return;
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    string? input = await connection.ReadLineAsync();
                    if (input == null) break;
                    
                    ProcessCommand(playerName, input.Trim());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }
            finally
            {
                DisconnectPlayer(connection.PlayerId);
                connection.Close();
            }
        }
        
        private void ProcessCommand(string playerName, string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            if (!Players.TryGetValue(playerName, out var player)) return;
            
            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string commandName = parts[0].ToLower();
            string[] args = parts.Length > 1 ? parts[1..] : [];
            
            // Handle shortcuts
            commandName = commandName switch
            {
                "l" => "look",
                "i" => "inventory",
                "n" => "go",
                "s" => "go",
                "e" => "go",
                "w" => "go",
                _ => commandName
            };
            
            // Special handling for direction shortcuts
            if (sourceArray.Contains(parts[0].ToLower()))
            {
                string direction = parts[0].ToLower() switch
                {
                    "n" => "north",
                    "s" => "south",
                    "e" => "east",
                    "w" => "west",
                    _ => parts[0]
                };
                args = [direction];
            }
            
            if (Commands.TryGetValue(commandName, out var command))
            {
                try
                {
                    command.Execute(player, args);
                }
                catch (Exception ex)
                {
                    player.SendMessage($"Error executing command: {ex.Message}");
                    Console.WriteLine($"Command error: {ex}");
                }
            }
            else
            {
                player.SendMessage("Unknown command. Type 'help' for available commands.");
            }
        }
        
        public void DisconnectPlayer(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return;
            
            lock (_playersLock)
            {
                if (Players.TryRemove(playerName, out var player))
                {
                    var room = GetRoom(player.CurrentRoom);
                    room?.RemovePlayer(playerName);
                    room?.BroadcastMessage($"{playerName} has left the world.");
                    
                    Console.WriteLine($"Player {playerName} disconnected");
                }
            }
        }
        
        private async Task GameLoopAsync()
        {
            if (_cancellationTokenSource == null) return;   
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Regenerate health/mana for players
                    foreach (var player in Players.Values)
                    {
                        if (player.Health < player.MaxHealth)
                        {
                            player.Health = Math.Min(player.MaxHealth, player.Health + 1);
                        }
                        if (player.Mana < player.MaxMana)
                        {
                            player.Mana = Math.Min(player.MaxMana, player.Mana + 1);
                        }
                    }
                    
                    // Respawn monsters (simple logic)
                    var random = new Random();
                    foreach (var room in Rooms.Values)
                    {
                        if (room.Monsters.Count == 0 && random.Next(100) < 5) // 5% chance per tick
                        {
                            if (room.Id == "forest")
                            {
                                room.Monsters.Add(new Monster
                                {
                                    Name = "Wolf",
                                    Health = 30,
                                    MaxHealth = 30,
                                    AttackPower = 15,
                                    Experience = 15,
                                    Gold = 5,
                                    Loot = ["wolf_pelt"]
                                });
                                room.BroadcastMessage("A wolf emerges from the shadows!");
                            }
                        }
                    }
                    
                    await Task.Delay(5000); // 5 second tick
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Game loop error: {ex.Message}");
                }
            }
        }
        
        public async Task StopAsync()
        {
            await Task.Run(() =>
            {
                Console.WriteLine("Shutting down server...");

                _cancellationTokenSource?.Cancel();

                // Disconnect all players
                var playerNames = Players.Keys.ToList();
                foreach (var name in playerNames)
                {
                    DisconnectPlayer(name);
                }

                _listener?.Stop();
                Console.WriteLine("Server stopped.");
            });
        }
        
        // Entry point
        public static async Task Main()
        {
            var server = new MudServer();
            await server.StartAsync();
        }
    }
}