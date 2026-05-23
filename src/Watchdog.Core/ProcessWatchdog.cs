namespace Watchdog.Core;

public sealed class ProcessWatchdog : IAsyncDisposable
{
    private readonly IProcessController _processController;
    private readonly SemaphoreSlim _checkLock = new(1, 1);
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;
    private bool _hasObservedRunningProcess;

    public ProcessWatchdog(IProcessController processController, WatchdogOptions options)
    {
        _processController = processController;
        Options = options;
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
                await _processController.KillAsync(Options.ExecutablePath, cancellationToken).ConfigureAwait(false);
                await _processController.StartAsync(Options.ExecutablePath, cancellationToken).ConfigureAwait(false);
                WriteLog("进程已重启。");
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
