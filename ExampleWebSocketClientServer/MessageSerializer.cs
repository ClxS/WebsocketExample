using System.Text;
using System.Text.Json;
using ExampleWebSocketClientServer.Messages;

namespace ExampleWebSocketClientServer;

public static class MessageSerializer
{
    public static TMessage Deserialize<TMessage>(ReadOnlySpan<byte> message) where TMessage : IMessage
    {
        string json = Encoding.UTF8.GetString(message);
        return JsonSerializer.Deserialize<TMessage>(json);
    }

    public static IMessage Deserialize(ReadOnlySpan<byte> message)
    {
        string json = Encoding.UTF8.GetString(message);
        MessageBase? messageBase = JsonSerializer.Deserialize<MessageBase>(json);

        return messageBase.MessageType switch
        {
            MessageType.Heartbeat => Deserialize<Heartbeat>(message),
            MessageType.SomeLongRunningTaskRequest => Deserialize<LongRunningTaskRequest>(message),
            MessageType.SomeLongRunningTaskResponse => Deserialize<LongRunningTaskResponse>(message),
            MessageType.MaxValue => throw new NotSupportedException(),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public static Memory<byte> Serialize<TMessage>(TMessage heartbeat) where TMessage : IMessage
    {
        string json = JsonSerializer.Serialize(heartbeat);
        return new Memory<byte>(Encoding.UTF8.GetBytes(json));
    }
}