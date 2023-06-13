namespace ExampleWebSocketClientServer.Messages;

public class LongRunningTaskResponse : IMessage
{
    public static MessageType StaticMessageType => MessageType.SomeLongRunningTaskResponse;
    
    public MessageType MessageType => StaticMessageType;
    
    public string Message { get; set; }
    
}