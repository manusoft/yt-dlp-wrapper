namespace VideoDownloader.Core;

public sealed class SpeedMonitor
{
    private long _lastBytes;
    private DateTime _lastTime = DateTime.Now;

    public double Update(long totalBytes)
    {
        var now = DateTime.Now;
        var deltaBytes = totalBytes - _lastBytes;
        var deltaTime = (now - _lastTime).TotalSeconds;

        _lastBytes = totalBytes;
        _lastTime = now;

        if (deltaTime == 0)
            return 0;

        return deltaBytes / deltaTime;
    }
}