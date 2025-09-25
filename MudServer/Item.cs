
namespace MudServer
{
    public class Item
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = ""; // weapon, armor, consumable, misc
        public int Value { get; set; } = 0;
        public Dictionary<string, int> Stats { get; set; } = [];
    }
}
