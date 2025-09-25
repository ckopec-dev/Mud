
namespace MudServer.Commands
{
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
            if (Server.Instance == null) return;
            var room = Server.Instance.GetRoom(player.CurrentRoom);
            room?.BroadcastMessage($"{player.Name} says: {message}", player.Name);
            player.SendMessage($"You say: {message}");
        }
    }
}
