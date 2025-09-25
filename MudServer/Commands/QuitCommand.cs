
namespace MudServer.Commands
{
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
}
