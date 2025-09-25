
namespace MudServer
{
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
            var server = Server.Instance;
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
}
