using Watchdog.Core;

namespace Watchdog.Core.Tests;

[TestClass]
public sealed class ProcessWatchdogTests
{
    [TestMethod]
    public async Task CheckNowAsync_StartsProcessWhenItIsMissing()
    {
        var controller = new FakeProcessController();
        var watchdog = CreateWatchdog(controller);

        await watchdog.CheckNowAsync(CancellationToken.None);

        Assert.AreEqual(1, controller.StartCount);
        Assert.IsTrue(controller.IsRunning);
        CollectionAssert.Contains(controller.Logs, "未发现进程，正在启动。");
    }

    [TestMethod]
    public async Task CheckNowAsync_RestartsProcessWhenItHasExited()
    {
        var controller = new FakeProcessController { IsRunning = true };
        var watchdog = CreateWatchdog(controller);

        await watchdog.CheckNowAsync(CancellationToken.None);
        controller.IsRunning = false;
        await watchdog.CheckNowAsync(CancellationToken.None);

        Assert.AreEqual(1, controller.StartCount);
        Assert.IsTrue(controller.IsRunning);
        CollectionAssert.Contains(controller.Logs, "进程已退出，正在重启。");
    }

    [TestMethod]
    public async Task CheckNowAsync_RestartsProcessWhenItIsNotResponding()
    {
        var controller = new FakeProcessController { IsRunning = true, IsResponding = false };
        var watchdog = CreateWatchdog(controller);

        await watchdog.CheckNowAsync(CancellationToken.None);

        Assert.AreEqual(1, controller.KillCount);
        Assert.AreEqual(1, controller.StartCount);
        Assert.IsTrue(controller.IsRunning);
        CollectionAssert.Contains(controller.Logs, "进程无响应，正在结束并重启。");
    }

    [TestMethod]
    public async Task StopAsync_StopsTimerWithoutKillingMonitoredProcess()
    {
        var controller = new FakeProcessController { IsRunning = true };
        var watchdog = CreateWatchdog(controller);

        await watchdog.StartAsync(CancellationToken.None);
        await watchdog.StopAsync();

        Assert.IsFalse(watchdog.IsMonitoring);
        Assert.AreEqual(0, controller.KillCount);
        CollectionAssert.Contains(controller.Logs, "已停止监控。");
    }

    private static ProcessWatchdog CreateWatchdog(FakeProcessController controller)
    {
        var options = new WatchdogOptions(
            ExecutablePath: @"C:\Apps\Demo.exe",
            CheckInterval: TimeSpan.FromSeconds(30),
            UnresponsiveTimeout: TimeSpan.FromSeconds(5));

        var watchdog = new ProcessWatchdog(controller, options);
        watchdog.LogReceived += (_, message) => controller.Logs.Add(message.Message);
        return watchdog;
    }

    private sealed class FakeProcessController : IProcessController
    {
        public bool IsRunning { get; set; }
        public bool IsResponding { get; set; } = true;
        public int StartCount { get; private set; }
        public int KillCount { get; private set; }
        public List<string> Logs { get; } = new();

        public Task<bool> IsRunningAsync(string executablePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(IsRunning);
        }

        public Task<bool> IsRespondingAsync(string executablePath, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(IsResponding);
        }

        public Task StartAsync(string executablePath, CancellationToken cancellationToken)
        {
            StartCount++;
            IsRunning = true;
            IsResponding = true;
            return Task.CompletedTask;
        }

        public Task KillAsync(string executablePath, CancellationToken cancellationToken)
        {
            KillCount++;
            IsRunning = false;
            return Task.CompletedTask;
        }
    }
}
