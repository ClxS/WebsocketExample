namespace ExampleWebSocketClientServer.Messages;

public interface IMessage
{
    static abstract MessageType StaticMessageType { get; }
    
    MessageType MessageType { get; }
}

public class MessageBase
{
    public MessageType MessageType { get; set; }
}