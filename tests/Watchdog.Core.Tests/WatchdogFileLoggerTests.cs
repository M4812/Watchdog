using Watchdog.Core;

namespace Watchdog.Core.Tests;

[TestClass]
public sealed class WatchdogFileLoggerTests
{
    [TestMethod]
    public async Task WriteIfRecoveryEventAsync_WritesRecoveryEventsToDailyLogFile()
    {
        using var folder = new TemporaryFolder();
        var logger = new WatchdogFileLogger(
            folder.Path,
            () => new DateTimeOffset(2026, 5, 25, 19, 30, 0, TimeSpan.Zero));

        await logger.WriteIfRecoveryEventAsync("进程已退出，正在重启。", CancellationToken.None);
        await logger.WriteIfRecoveryEventAsync("进程运行正常。", CancellationToken.None);

        var logFile = Path.Combine(folder.Path, "watchdog-20260525.log");
        Assert.IsTrue(File.Exists(logFile));

        var content = await File.ReadAllTextAsync(logFile);
        StringAssert.Contains(content, "进程已退出，正在重启。");
        Assert.IsFalse(content.Contains("进程运行正常。", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task WriteIfRecoveryEventAsync_CreatesLogDirectoryWhenMissing()
    {
        using var folder = new TemporaryFolder();
        var logDirectory = Path.Combine(folder.Path, "log");
        var logger = new WatchdogFileLogger(
            logDirectory,
            () => new DateTimeOffset(2026, 5, 25, 19, 30, 0, TimeSpan.Zero));

        await logger.WriteIfRecoveryEventAsync("进程已重启。", CancellationToken.None);

        Assert.IsTrue(Directory.Exists(logDirectory));
        Assert.IsTrue(File.Exists(Path.Combine(logDirectory, "watchdog-20260525.log")));
    }

    private sealed class TemporaryFolder : IDisposable
    {
        public TemporaryFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"watchdog-file-log-tests-{Guid.NewGuid():N}");
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
}
