using System.Net;
using System.Net.Sockets;

namespace Z21Client;

/// <summary>
/// Defines the contract for a UDP client that supports asynchronous send and receive operations, binding to local
/// endpoints, and broadcast capabilities.
/// </summary>
/// <remarks>Implementations of this interface provide methods for sending and receiving UDP datagrams
/// asynchronously, as well as managing socket options such as broadcast support. The interface extends <see
/// cref="IDisposable"/>, so instances should be disposed when no longer needed to release underlying resources. Thread
/// safety and specific socket behaviors depend on the implementation.</remarks>
public interface IZ21UdpClient : IDisposable
{
    /// <summary>
    /// Gets or sets the underlying network socket used for client communication.
    /// </summary>
    /// <remarks>Assigning a new value to this property replaces the current socket instance used for network
    /// operations. Ensure that the socket is properly configured and connected before use.</remarks>
    Socket Client { set; get; }

    /// <summary>
    /// Gets or sets a value indicating whether broadcast packets are enabled for the socket.
    /// </summary>
    /// <remarks>When enabled, the socket can send and receive broadcast messages. This property is typically
    /// used for scenarios such as network discovery or communication with multiple devices on the same subnet. Changing
    /// this value may require appropriate network permissions.</remarks>
    bool EnableBroadcast { set; get; }
    
    /// <summary>
    /// Establishes the necessary associations or connections required for the object to function as intended.
    /// </summary>
    void Bind();

    /// <summary>
    /// Binds the server to the specified port, enabling it to listen for incoming network connections.
    /// </summary>
    /// <param name="port">The port number on which the server will listen. Must be in the range 0 to 65535.</param>
    void Bind(int port);
    
    /// <summary>
    /// Closes the current resource and releases any associated system resources.
    /// </summary>
    /// <remarks>After calling this method, the resource cannot be used until it is reopened or reinitialized.
    /// This method should be called when the resource is no longer needed to ensure proper cleanup.</remarks>
    void Close();

    /// <summary>
    /// Asynchronously sends a datagram to the specified network endpoint.
    /// </summary>
    /// <param name="datagram">The byte array containing the data to send. Cannot be null.</param>
    /// <param name="bytes">The number of bytes from <paramref name="datagram"/> to send. Must be greater than zero and less than or equal
    /// to the length of <paramref name="datagram"/>.</param>
    /// <param name="endPoint">The destination network endpoint. If null, the default remote endpoint will be used.</param>
    /// <returns>A task that represents the asynchronous send operation. The value of the task result is the number of bytes
    /// sent.</returns>
    Task<int> SendAsync(byte[] datagram, int bytes, IPEndPoint? endPoint);

    /// <summary>
    /// Receives a UDP datagram asynchronously from a remote host.
    /// </summary>
    /// <remarks>This method does not block the calling thread. The returned task completes when a datagram is
    /// received. If the underlying socket is closed before a datagram is received, the task will be canceled.</remarks>
    /// <returns>A task that represents the asynchronous receive operation. The result contains the received UDP datagram and the
    /// endpoint from which it was sent.</returns>
    Task<UdpReceiveResult> ReceiveAsync();

    /// <summary>
    /// Receives a UDP datagram asynchronously from a remote endpoint.
    /// </summary>
    /// <remarks>If the cancellation token is triggered before a datagram is received, the returned task is
    /// canceled. This method does not guarantee that datagrams are received in the order they were sent.</remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the receive operation before completion.</param>
    /// <returns>A task that represents the asynchronous receive operation. The result contains the received UDP datagram and the
    /// remote endpoint information.</returns>
    Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken);
}
