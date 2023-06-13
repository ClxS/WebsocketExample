namespace ExampleWebSocketClientServer.Messages;

public class LongRunningTaskRequest : IMessage
{
    public static MessageType StaticMessageType => MessageType.SomeLongRunningTaskRequest;
    
    public MessageType MessageType => StaticMessageType;
}