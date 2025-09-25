using System.Text;

namespace MudServer.Commands
{
    public class LookCommand : Command
    {
        public override string Name => "look";
        public override string Description => "Look around the current room";

        public override void Execute(Player player, string[] args)
        {
            if (Server.Instance == null) return;
            var room = Server.Instance.GetRoom(player.CurrentRoom);
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
}
