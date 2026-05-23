namespace Watchdog.Core;

public interface IProcessController
{
    Task<bool> IsRunningAsync(string executablePath, CancellationToken cancellationToken);

    Task<bool> IsRespondingAsync(string executablePath, TimeSpan timeout, CancellationToken cancellationToken);

    Task StartAsync(string executablePath, CancellationToken cancellationToken);

    Task KillAsync(string executablePath, CancellationToken cancellationToken);
}
