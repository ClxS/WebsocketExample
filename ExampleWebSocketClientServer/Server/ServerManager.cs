using ExampleWebSocketClientServer.Messages;
using Serilog;

namespace ExampleWebSocketClientServer.Server;

public class ServerManager
{
    private List<Client> clients = new();
    
    public void AddClient(WebSocketMessagingClient socket)
    {
        Client context = new Client(socket);
        this.clients.Add(context);    
        socket.WhenMessage<Client, Heartbeat>(context).Subscribe(OnHeartbeat);
        socket.WhenMessage<Client, LongRunningTaskRequest>(context).Subscribe(OnLongRunningRequest);
    }

    public void Update()
    {
        foreach (Client client in clients)
        {
            if (client.LastHeartbeat < DateTime.Now.AddSeconds(-30))
            {
                client.Socket.Disconnect();
            }

            if (client.LastHeartbeat < DateTime.Now.AddSeconds(-3))
            {
                Log.Information("Server sending heartbeat");
                client.Socket.Send(new Heartbeat());
            }
        }
    }

    private static void OnHeartbeat(WebSocketMessagingClient.ClientMessage<Client, Heartbeat> message)
    {
        Log.Information("Server received heartbeat");
        message.Context.LastHeartbeat = DateTime.Now;
    }

    private static void OnLongRunningRequest(WebSocketMessagingClient.ClientMessage<Client, LongRunningTaskRequest> message)
    {
        Log.Information("Server received LongRunningTaskRequest... working");
        Task.Run(() =>
        {
            Task.Delay(TimeSpan.FromSeconds(20))
                .ContinueWith((_) =>
                {
                    Log.Information("Server finished computing response, sending");
                    message.Context.Socket.Send(new LongRunningTaskResponse());
                });
        });
    }

    private class Client
    {
        public Client(WebSocketMessagingClient socket)
        {
            this.Socket = socket;
        }

        public WebSocketMessagingClient Socket { get; }
        
        public DateTime LastHeartbeat { get; set; } = DateTime.Now;
    }
}