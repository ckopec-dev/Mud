
namespace MudServer.Commands
{
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
}
