namespace ExampleWebSocketClientServer.Messages;

public class Heartbeat : IMessage
{
    public static MessageType StaticMessageType => MessageType.Heartbeat;
    
    public MessageType MessageType => StaticMessageType;
}