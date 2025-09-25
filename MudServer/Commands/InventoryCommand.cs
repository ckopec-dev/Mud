using System.Text;

namespace MudServer.Commands
{
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
}
