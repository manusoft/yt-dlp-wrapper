namespace VideoDownloader.Core;

public sealed class ProcessPool
{
    private readonly SemaphoreSlim _pool;

    public ProcessPool(int max)
    {
        _pool = new SemaphoreSlim(max);
    }

    public async Task RunAsync(Func<Task> action)
    {
        await _pool.WaitAsync();

        try
        {
            await action();
        }
        finally
        {
            _pool.Release();
        }
    }
}