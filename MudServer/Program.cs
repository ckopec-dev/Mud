
namespace MudServer
{
    class Program
    {
        // Entry point
        public static async Task Main()
        {
            var server = new Server();
            await server.StartAsync();
        }
    }
}