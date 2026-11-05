using whois;

namespace whois.Tests;

public class TcpConnectionFactoryTests
{
    [Test]
    public void ConnectsOnlyToApprovedLiteralAddress()
    {
        System.Net.Sockets.TcpListener listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = "not-resolved.invalid";
        endpoint.Port = port;
        System.Net.IPAddress approvedAddress = System.Net.IPAddress.Loopback;
        IWhoisConnectionFactory factory = new WhoisTcpConnectionFactory();

        WhoisConnection connection = factory.OpenConnection(endpoint, approvedAddress, new WhoisTimeoutOptions(), default(CancellationToken));
        using System.Net.Sockets.Socket accepted = listener.AcceptSocket();

        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(connection.ApprovedAddress, Is.SameAs(approvedAddress));
        Assert.That(connection.Stream, Is.TypeOf<System.Net.Sockets.NetworkStream>());
        Assert.That(connection.Stream?.CanRead, Is.True);
        Assert.That(connection.Stream?.CanWrite, Is.True);
        Assert.That(connection.Error, Is.Null);
        Assert.That(connection.Duration, Is.GreaterThanOrEqualTo(TimeSpan.Zero));

        connection.Stream?.Dispose();
        listener.Stop();
    }

    [Test]
    public void ConnectsOverIpv6LoopbackWhenAvailable()
    {
        if (!System.Net.Sockets.Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not available on this host.");
        }

        System.Net.Sockets.TcpListener listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.IPv6Loopback, 0);
        try
        {
            listener.Server.DualMode = false;
            listener.Start();
        }
        catch (System.Net.Sockets.SocketException)
        {
            listener.Stop();
            Assert.Ignore("IPv6 loopback binding is not available on this host.");
            return;
        }

        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = "unused.example";
        endpoint.Port = port;
        System.Net.IPAddress approvedAddress = System.Net.IPAddress.IPv6Loopback;
        IWhoisConnectionFactory factory = new WhoisTcpConnectionFactory();

        WhoisConnection connection = factory.OpenConnection(endpoint, approvedAddress, new WhoisTimeoutOptions(), default(CancellationToken));
        using System.Net.Sockets.Socket accepted = listener.AcceptSocket();

        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(connection.ApprovedAddress, Is.SameAs(approvedAddress));
        Assert.That(accepted.RemoteEndPoint, Is.TypeOf<System.Net.IPEndPoint>());
        Assert.That(((System.Net.IPEndPoint)accepted.RemoteEndPoint!).AddressFamily, Is.EqualTo(System.Net.Sockets.AddressFamily.InterNetworkV6));

        connection.Stream?.Dispose();
        listener.Stop();
    }

    [Test]
    public void RefusedLoopbackEndpointReturnsConnectFailure()
    {
        System.Net.Sockets.TcpListener reservation = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        reservation.Start();
        int refusedPort = ((System.Net.IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = "unused.example";
        endpoint.Port = refusedPort;
        System.Net.IPAddress approvedAddress = System.Net.IPAddress.Loopback;
        IWhoisConnectionFactory factory = new WhoisTcpConnectionFactory();

        WhoisConnection connection = factory.OpenConnection(endpoint, approvedAddress, new WhoisTimeoutOptions(), default(CancellationToken));

        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.ConnectFailed));
        Assert.That(connection.ApprovedAddress, Is.SameAs(approvedAddress));
        Assert.That(connection.Stream, Is.Null);
        Assert.That(connection.Error, Is.TypeOf<System.Net.Sockets.SocketException>());
    }

    [Test]
    public async Task DisposingReturnedStreamClosesUnderlyingSocket()
    {
        System.Net.Sockets.TcpListener listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = "unused.example";
        endpoint.Port = port;
        IWhoisConnectionFactory factory = new WhoisTcpConnectionFactory();

        WhoisConnection connection = factory.OpenConnection(endpoint, System.Net.IPAddress.Loopback, new WhoisTimeoutOptions(), default(CancellationToken));
        using System.Net.Sockets.Socket accepted = await listener.AcceptSocketAsync();
        connection.Stream?.Dispose();
        using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        byte[] buffer = new byte[1];

        int received = await accepted.ReceiveAsync(buffer, System.Net.Sockets.SocketFlags.None, timeout.Token);

        Assert.That(received, Is.Zero);
        listener.Stop();
    }

    [Test]
    public void FaultedConnectTaskReturnsUnderlyingFailureAndDisposesSocket()
    {
        System.Net.IPAddress approvedAddress = System.Net.IPAddress.Loopback;
        System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(approvedAddress.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        System.Net.Sockets.SocketException failure = new System.Net.Sockets.SocketException(61);
        Task task = Task.FromException(failure);

        WhoisConnection connection = WhoisTcpConnectionFactory.CreateConnectionFromTask(task, task, socket, approvedAddress, default(CancellationToken), TimeSpan.FromMilliseconds(4));

        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.ConnectFailed));
        Assert.That(connection.ApprovedAddress, Is.SameAs(approvedAddress));
        Assert.That(connection.Stream, Is.Null);
        Assert.That(connection.Error, Is.SameAs(failure));
        Assert.That(connection.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(4)));
        Assert.That(socket.SafeHandle.IsClosed, Is.True);
    }

    [Test]
    public void ConnectTaskLosingDeadlineRaceReturnsTimeoutAndDisposesSocket()
    {
        System.Net.IPAddress approvedAddress = System.Net.IPAddress.IPv6Loopback;
        System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(approvedAddress.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        TaskCompletionSource source = new TaskCompletionSource();

        WhoisConnection connection = WhoisTcpConnectionFactory.CreateConnectionFromTask(source.Task, Task.CompletedTask, socket, approvedAddress, default(CancellationToken), TimeSpan.FromMilliseconds(6));

        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(connection.ApprovedAddress, Is.SameAs(approvedAddress));
        Assert.That(connection.Stream, Is.Null);
        Assert.That(connection.Error, Is.TypeOf<TimeoutException>());
        Assert.That(socket.SafeHandle.IsClosed, Is.True);
    }

    [Test]
    public void CallerCancellationTakesPrecedenceAndDisposesSocket()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        System.Net.IPAddress approvedAddress = System.Net.IPAddress.Loopback;
        System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(approvedAddress.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        TaskCompletionSource source = new TaskCompletionSource();
        cancellation.Cancel();

        WhoisConnection connection = WhoisTcpConnectionFactory.CreateConnectionFromTask(source.Task, Task.CompletedTask, socket, approvedAddress, cancellation.Token, TimeSpan.FromMilliseconds(2));

        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(connection.Stream, Is.Null);
        Assert.That(connection.Error, Is.TypeOf<OperationCanceledException>());
        Assert.That(socket.SafeHandle.IsClosed, Is.True);
    }

    [Test]
    public void PreCancelledFactoryCallReturnsWithoutOpeningSocket()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = "not-resolved.invalid";
        endpoint.Port = 9;
        System.Net.IPAddress approvedAddress = System.Net.IPAddress.Loopback;
        IWhoisConnectionFactory factory = new WhoisTcpConnectionFactory();
        cancellation.Cancel();

        WhoisConnection connection = factory.OpenConnection(endpoint, approvedAddress, new WhoisTimeoutOptions(), cancellation.Token);

        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(connection.ApprovedAddress, Is.SameAs(approvedAddress));
        Assert.That(connection.Stream, Is.Null);
        Assert.That(connection.Error, Is.TypeOf<OperationCanceledException>());
    }
}
