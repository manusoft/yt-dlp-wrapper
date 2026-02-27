namespace VideoDownloader.Core;

public static class RetryPolicy
{
    public static async Task RunAsync(Func<Task> action, int retries = 3)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                await action();
                return;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }
    }
}