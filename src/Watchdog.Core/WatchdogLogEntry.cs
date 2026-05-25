namespace Watchdog.Core;

public sealed class WatchdogLogEntry
{
    public WatchdogLogEntry(DateTimeOffset timestamp, string message)
    {
        Timestamp = timestamp;
        Message = message;
    }

    public DateTimeOffset Timestamp { get; }

    public string Message { get; }

    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message}";
    }
}
