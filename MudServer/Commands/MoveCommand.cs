
namespace MudServer.Commands
{
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
}
