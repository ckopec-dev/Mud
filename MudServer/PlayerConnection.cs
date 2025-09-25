using System.Net.Sockets;
using System.Text;

namespace MudServer
{
    // Network connection handling
    public class PlayerConnection
    {
        public TcpClient Client { get; }
        public NetworkStream Stream { get; }
        public StreamWriter Writer { get; }
        public StreamReader Reader { get; }
        public string PlayerId { get; set; } = "";

        public PlayerConnection(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
            Writer = new StreamWriter(Stream, Encoding.UTF8) { AutoFlush = true };
            Reader = new StreamReader(Stream, Encoding.UTF8);
        }

        public void SendMessage(string message)
        {
            try
            {
                Writer.WriteLine(message);
            }
            catch (Exception)
            {
                // Connection lost
                MudServer.Instance?.DisconnectPlayer(PlayerId);
            }
        }

        public async Task<string?> ReadLineAsync()
        {
            try
            {
                if (Reader.EndOfStream) return null;
                return await Reader.ReadLineAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Close()
        {
            try
            {
                Writer?.Close();
                Reader?.Close();
                Stream?.Close();
                Client?.Close();
            }
            catch (Exception) { }
        }
    }
}
