
using Alpitronic.TestSystem.Application;

namespace Alpitronic.TestSystem.Infrastructure;

/// <summary>
/// Skeleton volutamente semplice. In produzione applicare retry solo agli errori
/// transitori/idempotenti e classificare time-out, HTTP 5xx e disconnessioni.
/// </summary>
public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _firstDelay;

    public ExponentialBackoffRetryPolicy(int maxAttempts = 3, TimeSpan? firstDelay = null)
    {
        _maxAttempts = maxAttempts >= 1 ? maxAttempts : throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        _firstDelay = firstDelay ?? TimeSpan.FromMilliseconds(250);
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await ExecuteAsync(async ct => { await operation(ct); return true; }, cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        Exception? latest = null;
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try { return await operation(cancellationToken); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (attempt < _maxAttempts)
            {
                latest = ex;
                var delay = TimeSpan.FromMilliseconds(_firstDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken);
            }
        }
        throw new InvalidOperationException("Retry exhausted.", latest);
    }
}

public sealed class InMemorySessionLogger : ISessionLogger
{
    private readonly List<string> _entries = new();
    public IReadOnlyList<string> Entries => _entries;
    public void Info(string message) => _entries.Add($"{DateTimeOffset.UtcNow:O} INFO {message}");
    public void Error(string message, Exception exception) => _entries.Add($"{DateTimeOffset.UtcNow:O} ERROR {message}: {exception.Message}");
}
