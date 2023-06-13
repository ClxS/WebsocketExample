using System.Net;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;
using ExampleWebSocketClientServer.Messages;
using Serilog;

namespace ExampleWebSocketClientServer;

public class WebSocketMessagingClient : IAsyncDisposable
{
    private readonly WebSocket socket;
    private readonly IDisposable outerDispose;
    private readonly List<Task> subtasks = new();
    private readonly CancellationTokenSource cts = new();

    private readonly Channel<Memory<byte>> outgoingQueue = Channel.CreateUnbounded<Memory<byte>>();
    private readonly Channel<Memory<byte>> incomingQueue = Channel.CreateUnbounded<Memory<byte>>();
    
    private readonly Subject<object>[] messageSubjects = new Subject<object>[(int) MessageType.MaxValue];
    
    public WebSocketMessagingClient(WebSocket socket, IDisposable outerDispose)
    {
        this.socket = socket;
        this.outerDispose = outerDispose;
        for (var messageIndex = 0; messageIndex < (int)MessageType.MaxValue; messageIndex++)
        {
            messageSubjects[messageIndex] = new Subject<object>();
        }
        
        this.subtasks.Add(Task.Run(this.ReceiveJob));
        this.subtasks.Add(Task.Run(this.SendJob));
        this.subtasks.Add(Task.Run(this.MessagePump));
    }

    private async Task ReceiveJob()
    {
        try
        {
            while (!this.cts.IsCancellationRequested)
            {
                Memory<byte> data = new(new byte[1024]);
                ValueWebSocketReceiveResult result = await this.socket.ReceiveAsync(data, this.cts.Token);
                if (result.Count == 0)
                {
                    continue;
                }

                await this.incomingQueue.Writer.WriteAsync(data[.. result.Count]);
            }
        }
        catch (Exception e)
        {
            if (!this.cts.IsCancellationRequested)
            {
                //Log.Error(e, "ReceiveJob failed");
            }
        }
        
    }

    private async Task SendJob()
    {
        try
        {
            await foreach (Memory<byte> message in this.outgoingQueue.Reader.ReadAllAsync())
            {
                await this.socket.SendAsync(message, WebSocketMessageType.Binary, true, this.cts.Token);
            }
        }
        catch (Exception e)
        {
            if (!this.cts.IsCancellationRequested)
            {
                Log.Error(e, "SendJob failed");
            }
        }
    }

    private async Task MessagePump()
    {
        try
        {
            await foreach (Memory<byte> messagePayload in this.incomingQueue.Reader.ReadAllAsync())
            {
                IMessage message = MessageSerializer.Deserialize(messagePayload.Span);
                this.messageSubjects[(int)message.MessageType].OnNext(message);
            }
        }
        catch (Exception e)
        {
            if (!this.cts.IsCancellationRequested)
            {
                Log.Error(e, "MessagePump failed");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        this.cts.Cancel();
        this.incomingQueue.Writer.Complete();
        this.outgoingQueue.Writer.Complete();
        await Task.WhenAll(this.subtasks);
        this.outerDispose.Dispose();
    }

    public void Disconnect()
    {
        throw new NotImplementedException();
    }

    public void Send<TMessage>(TMessage heartbeat) where TMessage : IMessage
    {
        this.outgoingQueue.Writer.TryWrite(MessageSerializer.Serialize(heartbeat));
    }

    public IObservable<ClientMessage<TContext, TMessage>> WhenMessage<TContext, TMessage>(TContext context) where TMessage : IMessage
    {
        return messageSubjects[(int)TMessage.StaticMessageType]
            .Select(message => new ClientMessage<TContext, TMessage>(context, (TMessage)message));
    }

    public record ClientMessage<TContext, TMessage>(TContext Context, TMessage Message);
}

public static class WebSocketFactory
{
    public static async Task<WebSocketMessagingClient> ListenOn()
    {
        HttpListener httpListener = new();
        httpListener.Prefixes.Add("http://localhost:13432/");
        httpListener.Start();

        HttpListenerContext context = await httpListener.GetContextAsync();
        if (!context.Request.IsWebSocketRequest)
        {
            throw new NotSupportedException();
        }

        HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
        WebSocket webSocket = webSocketContext.WebSocket;
        return new WebSocketMessagingClient(webSocket, httpListener);
    }
    
    
    public static async Task<WebSocketMessagingClient> ConnectTo()
    {
        ClientWebSocket clientWebSocket = new();
        await clientWebSocket.ConnectAsync(new Uri("ws://localhost:13432/"), CancellationToken.None);
        return new WebSocketMessagingClient(clientWebSocket, clientWebSocket);
    }
}