namespace Watchdog.Core;

public sealed record WatchdogLogEntry(DateTimeOffset Timestamp, string Message)
{
    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message}";
    }
}
