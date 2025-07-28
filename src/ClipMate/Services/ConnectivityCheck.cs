namespace ClipMate.Services;

public class ConnectivityCheck : IDisposable
{
    private readonly Action<string> _onStatusChanged;

    public ConnectivityCheck(Action<string> onStatusChanged)
    {
        _onStatusChanged = onStatusChanged;
        Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
    }

    private void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.ConstrainedInternet)
            _onStatusChanged?.Invoke("Internet access is limited.");
        else if (e.NetworkAccess != NetworkAccess.Internet)
            _onStatusChanged?.Invoke("Internet access is lost.");
        else
            _onStatusChanged?.Invoke(string.Empty); // Clear when internet is fine
    }

    public void Dispose() =>
        Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;

    ~ConnectivityCheck() => Dispose();
}