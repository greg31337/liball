using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class EndpointAndBindingTests
{
    [Test]
    public void ResolvesTextualIpv4EndpointAndSelectsMatchingWildcard()
    {
        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = "127.0.0.1";
        endpoint.Port = 1069;

        TftpEndpointResolutionResult result =
            TftpEndpointResolver.ResolveClientEndpoint(endpoint, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.AddressFamily, Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(result.RemoteEndPoint.Address, Is.EqualTo(IPAddress.Loopback));
            Assert.That(result.RemoteEndPoint.Port, Is.EqualTo(1069));
            Assert.That(result.LocalEndPoint.Address, Is.EqualTo(IPAddress.Any));
            Assert.That(result.LocalEndPoint.Port, Is.Zero);
        });
    }

    [Test]
    public void ResolvesHostnameUsingRequestedIpv4Family()
    {
        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = "localhost";
        endpoint.Port = 69;
        endpoint.AddressFamily = AddressFamily.InterNetwork;

        TftpEndpointResolutionResult result =
            TftpEndpointResolver.ResolveClientEndpoint(endpoint, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.AddressFamily, Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(result.RemoteEndPoint.Address.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(result.RemoteEndPoint.Port, Is.EqualTo(69));
            Assert.That(result.LocalEndPoint.Address, Is.EqualTo(IPAddress.Any));
        });
    }

    [Test]
    public void ExplicitLocalAddressSelectsRemoteFamily()
    {
        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = "localhost";
        endpoint.Port = 1069;
        endpoint.LocalAddress = IPAddress.Loopback;
        endpoint.AddressFamily = AddressFamily.Unspecified;

        TftpEndpointResolutionResult result =
            TftpEndpointResolver.ResolveClientEndpoint(endpoint, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.AddressFamily, Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(result.RemoteEndPoint.Address.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(result.LocalEndPoint.Address, Is.EqualTo(IPAddress.Loopback));
            Assert.That(result.LocalEndPoint.Port, Is.Zero);
        });
    }

    [Test]
    public void ResolvesTextualIpv6EndpointAndSelectsMatchingWildcard()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not supported on this host.");
        }

        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = "::1";
        endpoint.Port = 1069;
        endpoint.AddressFamily = AddressFamily.InterNetworkV6;

        TftpEndpointResolutionResult result =
            TftpEndpointResolver.ResolveClientEndpoint(endpoint, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.AddressFamily, Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(result.RemoteEndPoint.Address, Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(result.RemoteEndPoint.Port, Is.EqualTo(1069));
            Assert.That(result.LocalEndPoint.Address, Is.EqualTo(IPAddress.IPv6Any));
        });
    }

    [Test]
    public void RejectsRemoteLiteralOutsideRequestedFamily()
    {
        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = "127.0.0.1";
        endpoint.AddressFamily = AddressFamily.InterNetworkV6;

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => TftpEndpointResolver.ResolveClientEndpoint(
                endpoint,
                CancellationToken.None));

        Assert.That(exception.Message, Does.Contain("no address"));
    }

    [TestCase("")]
    [TestCase(" ")]
    public void RejectsEmptyRemoteHost(string host)
    {
        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = host;

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => TftpEndpointResolver.ResolveClientEndpoint(
                endpoint,
                CancellationToken.None));

        Assert.That(exception.Message, Does.Contain("host cannot be empty"));
    }

    [Test]
    public void RejectsMissingEndpoint()
    {
        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => TftpEndpointResolver.ResolveClientEndpoint(
                null!,
                CancellationToken.None));

        Assert.That(exception.Message, Does.Contain("endpoint cannot be null"));
    }

    [TestCase(0)]
    [TestCase(65536)]
    public void RejectsInvalidRemotePort(int port)
    {
        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = "127.0.0.1";
        endpoint.Port = port;

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => TftpEndpointResolver.ResolveClientEndpoint(
                endpoint,
                CancellationToken.None));

        Assert.That(exception.Message, Does.Contain("port is outside"));
    }

    [Test]
    public void RejectsUnsupportedAndMismatchedAddressFamilies()
    {
        TftpEndpoint unsupported = new TftpEndpoint();
        unsupported.Host = "127.0.0.1";
        unsupported.AddressFamily = AddressFamily.Unix;
        TftpEndpoint mismatched = new TftpEndpoint();
        mismatched.Host = "127.0.0.1";
        mismatched.AddressFamily = AddressFamily.InterNetworkV6;
        mismatched.LocalAddress = IPAddress.Loopback;

        TftpProtocolException unsupportedException =
            Assert.Throws<TftpProtocolException>(
                () => TftpEndpointResolver.ResolveClientEndpoint(
                    unsupported,
                    CancellationToken.None));
        TftpProtocolException mismatchException =
            Assert.Throws<TftpProtocolException>(
                () => TftpEndpointResolver.ResolveClientEndpoint(
                    mismatched,
                    CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(unsupportedException.Message, Does.Contain("IPv4, IPv6"));
            Assert.That(mismatchException.Message, Does.Contain("does not match"));
        });
    }

    [Test]
    public void ResolutionHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = "localhost";

        Assert.Throws<OperationCanceledException>(
            () => TftpEndpointResolver.ResolveClientEndpoint(
                endpoint,
                cancellation.Token));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BindsIpv4DynamicPortOnLoopbackAndWildcard(bool wildcard)
    {
        IPAddress address = wildcard ? IPAddress.Any : IPAddress.Loopback;
        UdpDatagramSocket? socket = null;
        try
        {
            socket = UdpDatagramSocket.BindLocalSocket(
                address,
                0,
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(socket.LocalEndPoint.Address, Is.EqualTo(address));
                Assert.That(socket.LocalEndPoint.Port, Is.GreaterThan(0));
                Assert.That(socket.Socket.AddressFamily,
                    Is.EqualTo(AddressFamily.InterNetwork));
                Assert.That(socket.Socket.SocketType, Is.EqualTo(SocketType.Dgram));
                Assert.That(socket.Socket.ProtocolType, Is.EqualTo(ProtocolType.Udp));
                Assert.That(socket.Socket.IsBound, Is.True);
            });
        }
        finally
        {
            socket?.CloseSocket();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BindsIpv6DynamicPortOnLoopbackAndWildcard(bool wildcard)
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not supported on this host.");
        }

        IPAddress address = wildcard ? IPAddress.IPv6Any : IPAddress.IPv6Loopback;
        UdpDatagramSocket? socket = null;
        try
        {
            socket = UdpDatagramSocket.BindLocalSocket(
                address,
                0,
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(socket.LocalEndPoint.Address, Is.EqualTo(address));
                Assert.That(socket.LocalEndPoint.Port, Is.GreaterThan(0));
                Assert.That(socket.Socket.AddressFamily,
                    Is.EqualTo(AddressFamily.InterNetworkV6));
                Assert.That(socket.Socket.DualMode, Is.False);
                Assert.That(socket.Socket.IsBound, Is.True);
            });
        }
        finally
        {
            socket?.CloseSocket();
        }
    }

    [Test]
    public void BindsResolvedExplicitLocalAddress()
    {
        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = "127.0.0.1";
        endpoint.LocalAddress = IPAddress.Loopback;
        TftpEndpointResolutionResult resolution =
            TftpEndpointResolver.ResolveClientEndpoint(endpoint, CancellationToken.None);
        UdpDatagramSocket? socket = null;
        try
        {
            socket = UdpDatagramSocket.BindLocalSocket(
                resolution.LocalEndPoint.Address,
                resolution.LocalEndPoint.Port,
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(socket.LocalEndPoint.Address, Is.EqualTo(IPAddress.Loopback));
                Assert.That(socket.LocalEndPoint.Port, Is.GreaterThan(0));
                Assert.That(socket.Socket.AddressFamily,
                    Is.EqualTo(resolution.AddressFamily));
            });
        }
        finally
        {
            socket?.CloseSocket();
        }
    }

    [Test]
    public void BindsRequestedNonzeroPort()
    {
        UdpDatagramSocket first = UdpDatagramSocket.BindLocalSocket(
            IPAddress.Loopback,
            0,
            CancellationToken.None);
        int port = first.LocalEndPoint.Port;
        first.CloseSocket();
        UdpDatagramSocket? rebound = null;
        try
        {
            rebound = UdpDatagramSocket.BindLocalSocket(
                IPAddress.Loopback,
                port,
                CancellationToken.None);

            Assert.That(rebound.LocalEndPoint.Port, Is.EqualTo(port));
        }
        finally
        {
            rebound?.CloseSocket();
        }
    }

    [TestCase(-1)]
    [TestCase(65536)]
    public void RejectsInvalidLocalPort(int port)
    {
        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => UdpDatagramSocket.BindLocalSocket(
                IPAddress.Loopback,
                port,
                CancellationToken.None));

        Assert.That(exception.Message, Does.Contain("port is outside"));
    }

    [Test]
    public void RejectsMissingLocalAddress()
    {
        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => UdpDatagramSocket.BindLocalSocket(
                null!,
                0,
                CancellationToken.None));

        Assert.That(exception.Message, Does.Contain("address cannot be null"));
    }

    [Test]
    public void BindingHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => UdpDatagramSocket.BindLocalSocket(
                IPAddress.Loopback,
                0,
                cancellation.Token));
    }
}
