namespace Watchdog.Core;

public sealed record WatchdogOptions(
    string ExecutablePath,
    TimeSpan CheckInterval,
    TimeSpan UnresponsiveTimeout)
{
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
    }
}
