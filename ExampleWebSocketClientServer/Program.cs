// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;
using ExampleWebSocketClientServer;
using ExampleWebSocketClientServer.Client;
using ExampleWebSocketClientServer.Server;
using Serilog;
using Serilog.Events;

CancellationTokenSource cts = new();
Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console() 
        .CreateLogger();

List<Task> tasks = new()
{
    Task.Run(ServerTask),
};

await Task.Delay(500);
tasks.Add(Task.Run(ClientTask));

await Task.WhenAll(tasks);

async Task ServerTask()
{
    await using WebSocketMessagingClient socket = await WebSocketFactory.ListenOn();
    Log.Information("Server: Listening on {socket}", socket);

    ServerManager server = new();
    server.AddClient(socket);
    
    while (!cts.IsCancellationRequested)
    {
        server.Update();
        await Task.Delay(TimeSpan.FromSeconds(0.1));
    }
}

async Task ClientTask()
{
    await using WebSocketMessagingClient socket = await WebSocketFactory.ConnectTo();
    Log.Information("Client: Connected to {socket}", socket);

    ClientManager client = new(socket);
    
    // Wait 5 seconds then send a very long running task
    await Task.Delay(TimeSpan.FromSeconds(5));

    client.DoLongRunningRequest();
    client.TaskComplete.WaitOne();
    cts.Cancel();
}