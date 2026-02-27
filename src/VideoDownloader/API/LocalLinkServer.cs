using System.Net;

namespace VideoDownloader.API;

public sealed class LocalLinkServer
{
    private HttpListener? _listener;

    public event Action<string>? UrlReceived;

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:5577/");
        _listener.Start();

        _ = Loop();
    }

    private async Task Loop()
    {
        while (_listener!.IsListening)
        {
            var ctx = await _listener.GetContextAsync();

            var url = ctx.Request.QueryString["url"];

            if (!string.IsNullOrEmpty(url))
                UrlReceived?.Invoke(url);

            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
        }
    }
}