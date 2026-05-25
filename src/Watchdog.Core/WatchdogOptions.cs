namespace Watchdog.Core;

public sealed class WatchdogOptions
{
    public WatchdogOptions(
        string executablePath,
        TimeSpan checkInterval,
        TimeSpan unresponsiveTimeout,
        string? watchedFolderPath = null,
        TimeSpan? folderBacklogTimeout = null)
    {
        ExecutablePath = executablePath;
        CheckInterval = checkInterval;
        UnresponsiveTimeout = unresponsiveTimeout;
        WatchedFolderPath = watchedFolderPath;
        FolderBacklogTimeout = folderBacklogTimeout;
    }

    public string ExecutablePath { get; }

    public TimeSpan CheckInterval { get; }

    public TimeSpan UnresponsiveTimeout { get; }

    public string? WatchedFolderPath { get; }

    public TimeSpan? FolderBacklogTimeout { get; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ExecutablePath))
        {
            throw new ArgumentException("请选择要监控的 exe 文件。", nameof(ExecutablePath));
        }

        if (CheckInterval < TimeSpan.FromSeconds(1))
        {
            throw new ArgumentOutOfRangeException(nameof(CheckInterval), "检查间隔不能小于 1 秒。");
        }

        if (UnresponsiveTimeout < TimeSpan.FromSeconds(1))
        {
            throw new ArgumentOutOfRangeException(nameof(UnresponsiveTimeout), "无响应等待时间不能小于 1 秒。");
        }

        if (!string.IsNullOrWhiteSpace(WatchedFolderPath) &&
            (FolderBacklogTimeout == null || FolderBacklogTimeout < TimeSpan.FromSeconds(1)))
        {
            throw new ArgumentOutOfRangeException(nameof(FolderBacklogTimeout), "文件夹堆积判定时间不能小于 1 秒。");
        }
    }
}
