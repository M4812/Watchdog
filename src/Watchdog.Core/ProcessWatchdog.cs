namespace Watchdog.Core;

public sealed class ProcessWatchdog : IAsyncDisposable
{
    private readonly IProcessController _processController;
    private readonly Func<DateTimeOffset> _clock;
    private readonly SemaphoreSlim _checkLock = new(1, 1);
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;
    private bool _hasObservedRunningProcess;
    private DateTimeOffset? _folderBacklogStartedAt;

    public ProcessWatchdog(
        IProcessController processController,
        WatchdogOptions options,
        Func<DateTimeOffset>? clock = null)
    {
        _processController = processController;
        Options = options;
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public event EventHandler<WatchdogLogEntry>? LogReceived;

    public WatchdogOptions Options { get; }

    public bool IsMonitoring { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (IsMonitoring)
        {
            return Task.CompletedTask;
        }

        Options.Validate();
        IsMonitoring = true;
        _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitoringTask = MonitorLoopAsync(_monitoringCts.Token);
        WriteLog("已开始监控。");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!IsMonitoring)
        {
            return;
        }

        IsMonitoring = false;
        _monitoringCts?.Cancel();

        if (_monitoringTask is not null)
        {
            try
            {
                await _monitoringTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _monitoringCts?.Dispose();
        _monitoringCts = null;
        _monitoringTask = null;
        _folderBacklogStartedAt = null;
        WriteLog("已停止监控。");
    }

    public async Task CheckNowAsync(CancellationToken cancellationToken)
    {
        await _checkLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            Options.Validate();

            if (!await _processController.IsRunningAsync(Options.ExecutablePath, cancellationToken).ConfigureAwait(false))
            {
                WriteLog(_hasObservedRunningProcess ? "进程已退出，正在重启。" : "未发现进程，正在启动。");
                await _processController.StartAsync(Options.ExecutablePath, cancellationToken).ConfigureAwait(false);
                _hasObservedRunningProcess = true;
                _folderBacklogStartedAt = null;
                WriteLog("进程已启动。");
                return;
            }

            _hasObservedRunningProcess = true;

            if (!await _processController.IsRespondingAsync(
                    Options.ExecutablePath,
                    Options.UnresponsiveTimeout,
                    cancellationToken).ConfigureAwait(false))
            {
                WriteLog("进程无响应，正在结束并重启。");
                await RestartProcessAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            if (await ShouldRestartForFolderBacklogAsync(cancellationToken).ConfigureAwait(false))
            {
                WriteLog($"文件夹内文件堆积超过 {Options.FolderBacklogTimeout!.Value.TotalSeconds:0} 秒，正在重启进程。");
                await RestartProcessAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            WriteLog("进程运行正常。");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            WriteLog($"监控检查失败：{ex.Message}");
        }
        finally
        {
            _checkLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _checkLock.Dispose();
    }

    private async Task<bool> ShouldRestartForFolderBacklogAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Options.WatchedFolderPath) || Options.FolderBacklogTimeout is null)
        {
            _folderBacklogStartedAt = null;
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(Options.WatchedFolderPath))
        {
            _folderBacklogStartedAt = null;
            WriteLog("监控文件夹不存在，跳过文件夹堆积检查。");
            return false;
        }

        var hasBacklog = Directory.EnumerateFileSystemEntries(Options.WatchedFolderPath).Any();
        if (!hasBacklog)
        {
            _folderBacklogStartedAt = null;
            return false;
        }

        var now = _clock();
        _folderBacklogStartedAt ??= now;
        await Task.CompletedTask.ConfigureAwait(false);
        return now - _folderBacklogStartedAt >= Options.FolderBacklogTimeout;
    }

    private async Task RestartProcessAsync(CancellationToken cancellationToken)
    {
        await _processController.KillAsync(Options.ExecutablePath, cancellationToken).ConfigureAwait(false);
        await _processController.StartAsync(Options.ExecutablePath, cancellationToken).ConfigureAwait(false);
        _folderBacklogStartedAt = null;
        WriteLog("进程已重启。");
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await CheckNowAsync(cancellationToken).ConfigureAwait(false);
            await Task.Delay(Options.CheckInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private void WriteLog(string message)
    {
        LogReceived?.Invoke(this, new WatchdogLogEntry(DateTimeOffset.Now, message));
    }
}
