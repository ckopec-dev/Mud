using System.Text;

namespace MudServer.Commands
{
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
}
