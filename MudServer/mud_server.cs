using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MudServer
{
    // Core game entities
    public class Player
    {
        public string Name { get; set; } = "";
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int Mana { get; set; } = 50;
        public int MaxMana { get; set; } = 50;
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int Gold { get; set; } = 100;
        public string CurrentRoom { get; set; } = "town_square";
        public List<string> Inventory { get; set; } = [];
        public Dictionary<string, string> Equipment { get; set; } = [];
        public PlayerConnection Connection { get; set; }
        
        public Player(PlayerConnection connection)
        {
            Connection = connection;
            Equipment = new Dictionary<string, string>
            {
                ["weapon"] = "",
                ["armor"] = "",
                ["helmet"] = "",
                ["boots"] = ""
            };
        }
        
        public void SendMessage(string message)
        {
            Connection?.SendMessage(message);
        }
        
        public void TakeDamage(int damage)
        {
            Health = Math.Max(0, Health - damage);
        }
        
        public void Heal(int amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
        }
        
        public bool IsAlive => Health > 0;
        
        public void GainExperience(int exp)
        {
            Experience += exp;
            CheckLevelUp();
        }
        
        private void CheckLevelUp()
        {
            int expNeeded = Level * 100;
            if (Experience >= expNeeded)
            {
                Level++;
                MaxHealth += 10;
                MaxMana += 5;
                Health = MaxHealth;
                Mana = MaxMana;
                SendMessage($"Congratulations! You've reached level {Level}!");
            }
        }
    }
    
    public class Room
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public Dictionary<string, string> Exits { get; set; } = [];
        public List<string> Items { get; set; } = [];
        public List<Monster> Monsters { get; set; } = [];
        public HashSet<string> Players { get; set; } = [];
        
        public void AddPlayer(string playerName)
        {
            Players.Add(playerName);
        }
        
        public void RemovePlayer(string playerName)
        {
            Players.Remove(playerName);
        }
        
        public void BroadcastMessage(string message, string excludePlayer = "")
        {
            var server = MudServer.Instance;
            if (server == null) return;

            foreach (var playerName in Players)
            {
                if (playerName != excludePlayer && server.Players.TryGetValue(playerName, out Player? value))
                {
                    value.SendMessage(message);
                }
            }
        }
    }
    
    public class Monster
    {
        public string Name { get; set; } = "";
        public int Health { get; set; } = 50;
        public int MaxHealth { get; set; } = 50;
        public int AttackPower { get; set; } = 10;
        public int Experience { get; set; } = 25;
        public int Gold { get; set; } = 10;
        public List<string> Loot { get; set; } = [];
        public bool IsAlive => Health > 0;
        
        public void TakeDamage(int damage)
        {
            Health = Math.Max(0, Health - damage);
        }
    }
    
    public class Item
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = ""; // weapon, armor, consumable, misc
        public int Value { get; set; } = 0;
        public Dictionary<string, int> Stats { get; set; } = [];
    }
    
    // Network connection handling
    public class PlayerConnection
    {
        public TcpClient Client { get; }
        public NetworkStream Stream { get; }
        public StreamWriter Writer { get; }
        public StreamReader Reader { get; }
        public string PlayerId { get; set; } = "";
        
        public PlayerConnection(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
            Writer = new StreamWriter(Stream, Encoding.UTF8) { AutoFlush = true };
            Reader = new StreamReader(Stream, Encoding.UTF8);
        }
        
        public void SendMessage(string message)
        {
            try
            {
                Writer.WriteLine(message);
            }
            catch (Exception)
            {
                // Connection lost
                MudServer.Instance?.DisconnectPlayer(PlayerId);
            }
        }
        
        public async Task<string?> ReadLineAsync()
        {
            try
            {
                if (Reader.EndOfStream) return null;    
                return await Reader.ReadLineAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        public void Close()
        {
            try
            {
                Writer?.Close();
                Reader?.Close();
                Stream?.Close();
                Client?.Close();
            }
            catch (Exception) { }
        }
    }
    
    // Command system
    public abstract class Command
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract void Execute(Player player, string[] args);
    }
    
    public class SayCommand : Command
    {
        public override string Name => "say";
        public override string Description => "Say something to players in the same room";
        
        public override void Execute(Player player, string[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("Say what?");
                return;
            }
            
            string message = string.Join(" ", args);
            if (MudServer.Instance == null) return; 
            var room = MudServer.Instance.GetRoom(player.CurrentRoom);
            room?.BroadcastMessage($"{player.Name} says: {message}", player.Name);
            player.SendMessage($"You say: {message}");
        }
    }
    
    public class LookCommand : Command
    {
        public override string Name => "look";
        public override string Description => "Look around the current room";
        
        public override void Execute(Player player, string[] args)
        {
            if (MudServer.Instance == null) return;
            var room = MudServer.Instance.GetRoom(player.CurrentRoom);
            if (room == null) return;
            
            var sb = new StringBuilder();
            sb.AppendLine($"=== {room.Name} ===");
            sb.AppendLine(room.Description);
            
            if (room.Items.Count > 0)
            {
                sb.AppendLine("\nItems here:");
                foreach (var item in room.Items)
                {
                    sb.AppendLine($"  - {item}");
                }
            }
            
            if (room.Monsters.Count > 0)
            {
                sb.AppendLine("\nCreatures here:");
                foreach (var monster in room.Monsters.Where(m => m.IsAlive))
                {
                    sb.AppendLine($"  - {monster.Name}");
                }
            }
            
            if (room.Players.Count > 1)
            {
                sb.AppendLine("\nOther players here:");
                foreach (var p in room.Players.Where(p => p != player.Name))
                {
                    sb.AppendLine($"  - {p}");
                }
            }
            
            if (room.Exits.Count > 0)
            {
                sb.AppendLine("\nExits:");
                foreach (var exit in room.Exits)
                {
                    sb.AppendLine($"  {exit.Key} - {exit.Value}");
                }
            }
            
            player.SendMessage(sb.ToString());
        }
    }
    
    public class MoveCommand : Command
    {
        public override string Name => "go";
        public override string Description => "Move to another room";
        
        public override void Execute(Player player, string[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("Go where? Use: go <direction>");
                return;
            }
            
            string direction = args[0].ToLower();
            if (MudServer.Instance == null) return;
            var currentRoom = MudServer.Instance.GetRoom(player.CurrentRoom);
            
            if (currentRoom?.Exits.ContainsKey(direction) != true)
            {
                player.SendMessage($"You can't go {direction} from here.");
                return;
            }
            
            string newRoomId = currentRoom.Exits[direction];
            var newRoom = MudServer.Instance.GetRoom(newRoomId);
            
            if (newRoom == null)
            {
                player.SendMessage("That way seems to be blocked.");
                return;
            }
            
            // Move player
            currentRoom.RemovePlayer(player.Name);
            currentRoom.BroadcastMessage($"{player.Name} leaves to the {direction}.");
            
            player.CurrentRoom = newRoomId;
            newRoom.AddPlayer(player.Name);
            newRoom.BroadcastMessage($"{player.Name} arrives.", player.Name);
            
            // Show new room
            new LookCommand().Execute(player, []);
        }
    }
    
    public class AttackCommand : Command
    {
        public override string Name => "attack";
        public override string Description => "Attack a monster";
        
        public override void Execute(Player player, string[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("Attack what?");
                return;
            }
            
            string targetName = string.Join(" ", args).ToLower();
            if (MudServer.Instance == null) return;
            var room = MudServer.Instance.GetRoom(player.CurrentRoom);
            var monster = room?.Monsters.FirstOrDefault(m => 
                m.Name.Contains(targetName, StringComparison.CurrentCultureIgnoreCase) && m.IsAlive);
            
            if (monster == null)
            {
                player.SendMessage("That's not here to attack.");
                return;
            }
            
            // Player attacks monster
            int damage = new Random().Next(10, 21); // 10-20 damage
            monster.TakeDamage(damage);
            if (room == null) return;
            room.BroadcastMessage($"{player.Name} attacks {monster.Name} for {damage} damage!");
            
            if (!monster.IsAlive)
            {
                room.BroadcastMessage($"{monster.Name} has been defeated!");
                player.GainExperience(monster.Experience);
                player.Gold += monster.Gold;
                
                // Add loot to room
                room.Items.AddRange(monster.Loot);
                room.Monsters.Remove(monster);
                return;
            }
            
            // Monster counter-attacks
            int monsterDamage = new Random().Next(5, monster.AttackPower + 1);
            player.TakeDamage(monsterDamage);
            room.BroadcastMessage($"{monster.Name} attacks {player.Name} for {monsterDamage} damage!");
            
            if (!player.IsAlive)
            {
                room.BroadcastMessage($"{player.Name} has been defeated!");
                // Respawn logic could go here
                player.Health = player.MaxHealth / 2;
                player.CurrentRoom = "town_square";
                player.SendMessage("You have been defeated and respawned in town!");
            }
        }
    }
    
    public class InventoryCommand : Command
    {
        public override string Name => "inventory";
        public override string Description => "Show your inventory";
        
        public override void Execute(Player player, string[] args)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Inventory ===");
            sb.AppendLine($"Gold: {player.Gold}");
            sb.AppendLine($"Health: {player.Health}/{player.MaxHealth}");
            sb.AppendLine($"Mana: {player.Mana}/{player.MaxMana}");
            sb.AppendLine($"Level: {player.Level} (XP: {player.Experience})");
            
            if (player.Inventory.Count > 0)
            {
                sb.AppendLine("\nItems:");
                foreach (var item in player.Inventory)
                {
                    sb.AppendLine($"  - {item}");
                }
            }
            else
            {
                sb.AppendLine("\nYour inventory is empty.");
            }
            
            sb.AppendLine("\nEquipment:");
            foreach (var eq in player.Equipment)
            {
                string equipped = string.IsNullOrEmpty(eq.Value) ? "none" : eq.Value;
                sb.AppendLine($"  {eq.Key}: {equipped}");
            }
            
            player.SendMessage(sb.ToString());
        }
    }
    
    public class GetCommand : Command
    {
        public override string Name => "get";
        public override string Description => "Pick up an item";
        
        public override void Execute(Player player, string[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("Get what?");
                return;
            }
            
            string itemName = string.Join(" ", args).ToLower();
            if (MudServer.Instance == null) return;
            var room = MudServer.Instance.GetRoom(player.CurrentRoom);
            var item = room?.Items.FirstOrDefault(i => i.Contains(itemName, StringComparison.CurrentCultureIgnoreCase));
            
            if (item == null)
            {
                player.SendMessage("That's not here to take.");
                return;
            }
            
            room?.Items.Remove(item);
            player.Inventory.Add(item);
            room?.BroadcastMessage($"{player.Name} picks up {item}.");
            player.SendMessage($"You pick up {item}.");
        }
    }
    
    public class HelpCommand : Command
    {
        public override string Name => "help";
        public override string Description => "Show available commands";
        
        public override void Execute(Player player, string[] args)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Available Commands ===");
            
            if (MudServer.Instance == null) return;
            foreach (var cmd in MudServer.Instance.Commands.Values)
            {
                sb.AppendLine($"{cmd.Name.PadRight(12)} - {cmd.Description}");
            }
            
            sb.AppendLine("\nShortcuts:");
            sb.AppendLine("l            - look");
            sb.AppendLine("i            - inventory");
            sb.AppendLine("n/s/e/w      - go north/south/east/west");
            
            player.SendMessage(sb.ToString());
        }
    }
    
    public class QuitCommand : Command
    {
        public override string Name => "quit";
        public override string Description => "Exit the game";
        
        public override void Execute(Player player, string[] args)
        {
            player.SendMessage("Goodbye!");
            MudServer.Instance?.DisconnectPlayer(player.Name);
        }
    }
    
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