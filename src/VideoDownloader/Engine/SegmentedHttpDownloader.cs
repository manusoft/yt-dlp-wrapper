namespace VideoDownloader.Engine;

public sealed class SegmentedHttpDownloader
{
    public int Segments { get; set; } = 8;

    public async Task DownloadAsync(
        string url,
        string output,
        Action<long, long>? progress = null)
    {
        using var client = new HttpClient();

        var head = new HttpRequestMessage(HttpMethod.Head, url);
        var headResp = await client.SendAsync(head);

        var totalSize = headResp.Content.Headers.ContentLength ?? 0;

        if (totalSize == 0)
            throw new Exception("Server does not support content-length.");

        using var fs = new FileStream(
            output,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Write);

        fs.SetLength(totalSize);

        var segmentSize = totalSize / Segments;

        long downloaded = 0;

        var tasks = new List<Task>();

        for (int i = 0; i < Segments; i++)
        {
            var start = segmentSize * i;
            var end = (i == Segments - 1)
                ? totalSize - 1
                : start + segmentSize - 1;

            tasks.Add(DownloadSegment(
                url,
                output,
                start,
                end,
                bytes =>
                {
                    Interlocked.Add(ref downloaded, bytes);
                    progress?.Invoke(downloaded, totalSize);
                }));
        }

        await Task.WhenAll(tasks);
    }

    private async Task DownloadSegment(
        string url,
        string file,
        long start,
        long end,
        Action<int> progress)
    {
        using var client = new HttpClient();

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

        using var resp = await client.SendAsync(req);

        using var stream = await resp.Content.ReadAsStreamAsync();

        using var fs = new FileStream(
            file,
            FileMode.Open,
            FileAccess.Write,
            FileShare.Write);

        fs.Seek(start, SeekOrigin.Begin);

        byte[] buffer = new byte[81920];

        int read;

        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, read));
            progress(read);
        }
    }
}