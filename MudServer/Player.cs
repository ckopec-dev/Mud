
namespace MudServer
{
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
}
