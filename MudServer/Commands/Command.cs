
namespace MudServer.Commands
{
    // Command system
    public abstract class Command
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract void Execute(Player player, string[] args);
    }
}
