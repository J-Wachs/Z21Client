using System.Net;
using System.Net.Sockets;

namespace Z21Client;

/// <summary>
/// Provides UDP client functionality for communicating with a Z21 device, including methods for binding, sending, and
/// receiving UDP datagrams.
/// </summary>
/// <remarks>This class manages the lifecycle of a UDP client and exposes methods for asynchronous communication
/// with a Z21 device over UDP. Before sending or receiving data, the client must be bound using one of the Bind
/// methods. The class is not thread-safe; callers should ensure thread safety if accessing instances from multiple
/// threads.</remarks>
public class Z21UdpClient : IZ21UdpClient
{
    private UdpClient? _udpClient;

    /// <inheritdoc/>
    public Socket Client
    {
        get
        {
            if (_udpClient is null)
            {
                throw new InvalidOperationException("Client is not bound.");
            }
            return _udpClient.Client;
        }
        set
        {
            if (_udpClient is null)
            {
                throw new InvalidOperationException("Client is not bound.");
            }
            _udpClient.Client = value;
        }
    }

    /// <inheritdoc/>
    public bool EnableBroadcast
    {
        get
        {
            if (_udpClient is null)
            {
                throw new InvalidOperationException("Client is not bound.");
            }
            return _udpClient.EnableBroadcast;
        }
        set
        {
            if (_udpClient is null)
            {
                throw new InvalidOperationException("Client is not bound.");
            }
            _udpClient.EnableBroadcast = value;
        }
    }

    /// <inheritdoc/>
    public void Bind()
    {
        Close();

        _udpClient = new UdpClient();

        if (OperatingSystem.IsWindows())
        {
            _udpClient.AllowNatTraversal(true);
        }
    }

    /// <inheritdoc/>
    public void Bind(int port)
    {
        Close();

        _udpClient = new UdpClient(port);

        if (OperatingSystem.IsWindows())
        {
            _udpClient.AllowNatTraversal(true);
        }
    }

    /// <inheritdoc/>
    public void Close()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        _udpClient = null;
    }

    /// <inheritdoc/>
    public async Task<int> SendAsync(byte[] datagram, int bytes, IPEndPoint? endPoint)
    {
        if (_udpClient is null)
        {
            throw new InvalidOperationException("Client is not bound.");
        }
        return await _udpClient.SendAsync(datagram, bytes, endPoint);
    }

    /// <inheritdoc/>
    public async Task<UdpReceiveResult> ReceiveAsync()
    {
        if (_udpClient is null)
        {
            throw new InvalidOperationException("Client is not bound.");
        }
        return await _udpClient.ReceiveAsync();
    }

    /// <inheritdoc/>
    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        if (_udpClient is null)
        {
            throw new InvalidOperationException("Client is not bound.");
        }
        return await _udpClient.ReceiveAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
