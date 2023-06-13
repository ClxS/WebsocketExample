using System.Reactive;
using ExampleWebSocketClientServer.Messages;
using Serilog;

namespace ExampleWebSocketClientServer.Client;

public class ClientManager
{
    private DateTime lastHeartbeat = DateTime.Now;
    private WebSocketMessagingClient socket;
    private AutoResetEvent taskComplete = new (false);
    
    public ClientManager(WebSocketMessagingClient socket)
    {
        this.socket = socket;
        this.socket.WhenMessage<Unit, Heartbeat>(Unit.Default).Subscribe(OnHeartbeat);
        this.socket.WhenMessage<Unit, LongRunningTaskResponse>(Unit.Default).Subscribe(OnLongRunningResponse);
    }
    
    public AutoResetEvent TaskComplete => this.taskComplete;
    
    public void Update()
    {
        if (lastHeartbeat < DateTime.Now.AddSeconds(-30))
        {
            socket.Disconnect();
        }
    }

    private void OnHeartbeat(WebSocketMessagingClient.ClientMessage<Unit, Heartbeat> message)
    {
        Log.Information("Client received heartbeat");
        this.lastHeartbeat = DateTime.Now;
        socket.Send(new Heartbeat());
    }

    private void OnLongRunningResponse(WebSocketMessagingClient.ClientMessage<Unit, LongRunningTaskResponse> message)
    {
        Log.Information("Client received long running response");
        this.taskComplete.Set();
    }

    public void DoLongRunningRequest()
    {
        Log.Information("Client is requesting long running task");
        this.socket.Send(new LongRunningTaskRequest());
    }
}