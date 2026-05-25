namespace Watchdog.Core;

public sealed class WatchdogFileLogger
{
    private static readonly string[] RecoveryEventKeywords =
    [
        "未发现进程",
        "进程已退出",
        "进程无响应",
        "文件夹内文件堆积",
        "正在启动",
        "正在重启",
        "进程已启动",
        "进程已重启"
    ];

    private readonly string _logDirectory;
    private readonly Func<DateTimeOffset> _clock;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public WatchdogFileLogger(string logDirectory, Func<DateTimeOffset>? clock = null)
    {
        _logDirectory = logDirectory;
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public async Task WriteIfRecoveryEventAsync(string message, CancellationToken cancellationToken)
    {
        if (!IsRecoveryEvent(message))
        {
            return;
        }

        var now = _clock();
        var filePath = Path.Combine(_logDirectory, $"watchdog-{now:yyyyMMdd}.log");
        var line = $"{now:yyyy-MM-dd HH:mm:ss.fff}\t{message}{Environment.NewLine}";

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_logDirectory);
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Run(() => File.AppendAllText(filePath, line), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static bool IsRecoveryEvent(string message)
    {
        return RecoveryEventKeywords.Any(keyword => message.IndexOf(keyword, StringComparison.Ordinal) >= 0);
    }
}
