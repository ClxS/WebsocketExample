namespace ExampleWebSocketClientServer.Messages;

public enum MessageType
{
    Heartbeat,
    SomeLongRunningTaskRequest,
    SomeLongRunningTaskResponse,
    MaxValue,
}