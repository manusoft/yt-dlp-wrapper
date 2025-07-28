namespace ClipMate.Services;

public class ConnectivityCheck : IDisposable
{
    private readonly Action<ConnectionState> _onStatusChanged;

    public ConnectivityCheck(Action<ConnectionState> onStatusChanged)
    {
        _onStatusChanged = onStatusChanged;
        Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
    }

    private void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.ConstrainedInternet)
            _onStatusChanged?.Invoke(ConnectionState.Limited);
        else if (e.NetworkAccess != NetworkAccess.Internet)
            _onStatusChanged?.Invoke(ConnectionState.Lost);
        else
            _onStatusChanged?.Invoke(ConnectionState.Available); // Clear when internet is fine
    }

    public void Dispose() =>
        Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;

    ~ConnectivityCheck() => Dispose();
}

public enum ConnectionState
{
    Available,
    Lost,
    Limited,
}