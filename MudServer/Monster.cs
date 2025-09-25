
namespace MudServer
{
    public class Monster
    {
        public string Name { get; set; } = "";
        public int Health { get; set; } = 50;
        public int MaxHealth { get; set; } = 50;
        public int AttackPower { get; set; } = 10;
        public int Experience { get; set; } = 25;
        public int Gold { get; set; } = 10;
        public List<string> Loot { get; set; } = [];
        public bool IsAlive => Health > 0;

        public void TakeDamage(int damage)
        {
            Health = Math.Max(0, Health - damage);
        }
    }
}
