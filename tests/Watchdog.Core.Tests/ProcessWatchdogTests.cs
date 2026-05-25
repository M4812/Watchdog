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
    }

    [TestMethod]
    public async Task CheckNowAsync_RestartsProcessWhenWatchedFolderHasBacklogPastTimeout()
    {
        using var folder = new TemporaryFolder();
        File.WriteAllText(Path.Combine(folder.Path, "pending.txt"), "pending");
        var controller = new FakeProcessController { IsRunning = true };
        var now = new DateTimeOffset(2026, 5, 25, 10, 0, 0, TimeSpan.Zero);
        var watchdog = CreateWatchdog(
            controller,
            watchedFolderPath: folder.Path,
            folderBacklogTimeout: TimeSpan.FromSeconds(10),
            clock: () => now);

        await watchdog.CheckNowAsync(CancellationToken.None);
        now = now.AddSeconds(11);
        await watchdog.CheckNowAsync(CancellationToken.None);

        Assert.AreEqual(1, controller.KillCount);
        Assert.AreEqual(1, controller.StartCount);
    }

    [TestMethod]
    public async Task CheckNowAsync_DoesNotRestartProcessWhenWatchedFolderClearsBeforeTimeout()
    {
        using var folder = new TemporaryFolder();
        var pendingFile = Path.Combine(folder.Path, "pending.txt");
        File.WriteAllText(pendingFile, "pending");
        var controller = new FakeProcessController { IsRunning = true };
        var now = new DateTimeOffset(2026, 5, 25, 10, 0, 0, TimeSpan.Zero);
        var watchdog = CreateWatchdog(
            controller,
            watchedFolderPath: folder.Path,
            folderBacklogTimeout: TimeSpan.FromSeconds(10),
            clock: () => now);

        await watchdog.CheckNowAsync(CancellationToken.None);
        File.Delete(pendingFile);
        now = now.AddSeconds(11);
        await watchdog.CheckNowAsync(CancellationToken.None);

        Assert.AreEqual(0, controller.KillCount);
        Assert.AreEqual(0, controller.StartCount);
    }

    private static ProcessWatchdog CreateWatchdog(
        FakeProcessController controller,
        string? watchedFolderPath = null,
        TimeSpan? folderBacklogTimeout = null,
        Func<DateTimeOffset>? clock = null)
    {
        var options = new WatchdogOptions(
            executablePath: @"C:\Apps\Demo.exe",
            checkInterval: TimeSpan.FromSeconds(30),
            unresponsiveTimeout: TimeSpan.FromSeconds(5),
            watchedFolderPath: watchedFolderPath,
            folderBacklogTimeout: folderBacklogTimeout);

        return new ProcessWatchdog(controller, options, clock);
    }

    private sealed class TemporaryFolder : IDisposable
    {
        public TemporaryFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"watchdog-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class FakeProcessController : IProcessController
    {
        public bool IsRunning { get; set; }
        public bool IsResponding { get; set; } = true;
        public int StartCount { get; private set; }
        public int KillCount { get; private set; }

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
