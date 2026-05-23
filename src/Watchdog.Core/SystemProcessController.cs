using System.Diagnostics;

namespace Watchdog.Core;

public sealed class SystemProcessController : IProcessController
{
    public Task<bool> IsRunningAsync(string executablePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FindProcesses(executablePath).Any());
    }

    public async Task<bool> IsRespondingAsync(string executablePath, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var processes = FindProcesses(executablePath).ToArray();
        if (processes.Length == 0)
        {
            return false;
        }

        foreach (var process in processes)
        {
            using (process)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    process.Refresh();
                    continue;
                }

                if (!await WaitForInputIdleAsync(process, timeout, cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }

                if (!HasRespondedToRefresh(process))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public Task StartAsync(string executablePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory,
            UseShellExecute = true
        };

        Process.Start(startInfo);
        return Task.CompletedTask;
    }

    public async Task KillAsync(string executablePath, CancellationToken cancellationToken)
    {
        foreach (var process in FindProcesses(executablePath))
        {
            using (process)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (process.HasExited)
                {
                    continue;
                }

                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static IEnumerable<Process> FindProcesses(string executablePath)
    {
        var processName = Path.GetFileNameWithoutExtension(executablePath);

        foreach (var process in Process.GetProcessesByName(processName))
        {
            string? path = null;

            try
            {
                path = process.MainModule?.FileName;
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }

            if (string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
            {
                yield return process;
            }
            else
            {
                process.Dispose();
            }
        }
    }

    private static bool HasRespondedToRefresh(Process process)
    {
        try
        {
            process.Refresh();
            return !process.HasExited && process.Responding;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static async Task<bool> WaitForInputIdleAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(() => process.WaitForInputIdle((int)timeout.TotalMilliseconds), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return HasRespondedToRefresh(process);
        }
    }
}
