using NLog;

namespace MudClient
{
    public class Program
    {
        // Enhanced main method with argument parsing
        public static async Task Main(string[] args)
        {
            // Set up logging
            var Logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog-console.config").GetCurrentClassLogger();
            Logger.Info("Application started.");

            var client = new Client();

            Console.Title = "MUD Client";

            Client.WriteWelcomeMessage();

            string host = "localhost";
            int port = 4000;

            // Parse command line arguments
            if (args.Length >= 1)
            {
                if (args[0].Contains(':'))
                {
                    string[] parts = args[0].Split(':');
                    host = parts[0];
                    if (parts.Length > 1 && int.TryParse(parts[1], out int p))
                    {
                        port = p;
                    }
                }
                else
                {
                    host = args[0];
                }
            }

            if (args.Length >= 2 && int.TryParse(args[1], out int portArg))
            {
                port = portArg;
            }

            client = new Client();

            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine();
                await client.DisconnectAsync();
                Environment.Exit(0);
            };

            await client.ConnectAsync(host, port);
        }
    }
}