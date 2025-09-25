
namespace MudServer.Commands
{
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
            if (Server.Instance == null) return;
            var room = Server.Instance.GetRoom(player.CurrentRoom);
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
}
