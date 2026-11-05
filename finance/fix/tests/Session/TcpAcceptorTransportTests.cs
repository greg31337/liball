using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class TcpAcceptorTransportTests
{
    private readonly List<TcpClient> Clients = [];
    private readonly List<TcpAcceptorTransport> Transports = [];

    [TearDown]
    public void CloseNetworkResources()
    {
        foreach (TcpClient client in Clients)
        {
            client.Close();
        }
        foreach (TcpAcceptorTransport transport in Transports)
        {
            transport.CloseTransport();
        }
    }

    [Test]
    public void ConstructorAndBindEndpointValidationAreExplicit()
    {
        SessionSettings invalidSettings = new SessionSettings { ConnectTimeoutMilliseconds = 0 };
        TcpAcceptorTransport transport = Track(new TcpAcceptorTransport(Settings(100)));
        SessionEndpoint invalidEndpoint = new SessionEndpoint { Host = "", Port = -1 };

        TransportWriteResult invalid = transport.OpenTransport(invalidEndpoint, new SessionTlsSettings());
        TcpAcceptorTransport unopened = Track(new TcpAcceptorTransport(Settings(100)));
        TransportReadResult read = unopened.ReadTransport(new byte[1]);
        TransportWriteResult write = unopened.WriteTransport([1]);

        Assert.Multiple(() =>
        {
            Assert.Throws<FixTransportException>(() => new TcpAcceptorTransport(null!));
            Assert.Throws<FixTransportException>(() => new TcpAcceptorTransport(invalidSettings));
            AssertFailure(invalid, TransportState.Faulted, "invalid-tcp-bind-endpoint");
            Assert.That(read.ErrorCode, Is.EqualTo("transport-not-connected"));
            Assert.That(write.ErrorCode, Is.EqualTo("transport-not-connected"));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BindsIpv4AndIpv6AndCapturesLocalAndPeerEndpoints(bool ipv6)
    {
        if (ipv6 && !Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not available on this host.");
        }
        string host = ipv6 ? "::1" : "127.0.0.1";
        SessionEndpoint endpoint = new SessionEndpoint { Host = host, Port = 0, UseTls = true };
        TcpAcceptorTransport transport = Track(new TcpAcceptorTransport(Settings(2000)));

        AcceptedConnection connection = Connect(transport, endpoint);
        IPEndPoint peer = (IPEndPoint)connection.Client.Client.LocalEndPoint!;

        Assert.Multiple(() =>
        {
            AssertSuccess(connection.Result, TransportState.Connected, 0);
            Assert.That(endpoint.Port, Is.GreaterThan(0));
            Assert.That(endpoint.Host, Is.EqualTo(host));
            Assert.That(transport.BoundEndpoint.Host, Is.EqualTo(endpoint.Host));
            Assert.That(transport.BoundEndpoint.Port, Is.EqualTo(endpoint.Port));
            Assert.That(transport.PeerEndpoint.Host, Is.EqualTo(peer.Address.ToString()));
            Assert.That(transport.PeerEndpoint.Port, Is.EqualTo(peer.Port));
            Assert.That(transport.PeerEndpoint.UseTls, Is.True);
            Assert.That(transport.ListenerSocket, Is.Null);
            Assert.That(transport.CurrentSocket, Is.Not.Null);
        });
    }

    [Test]
    public void AcceptTimeoutIsBoundedAndTheUnconsumedInstanceCanRetry()
    {
        SessionEndpoint endpoint = new SessionEndpoint { Host = "127.0.0.1", Port = 0 };
        TcpAcceptorTransport transport = Track(new TcpAcceptorTransport(Settings(40)));
        Stopwatch elapsed = Stopwatch.StartNew();

        TransportWriteResult timeout = transport.OpenTransport(endpoint, new SessionTlsSettings());
        elapsed.Stop();
        AcceptedConnection retried = Connect(transport, endpoint);

        Assert.Multiple(() =>
        {
            AssertFailure(timeout, TransportState.Faulted, "tcp-accept-timeout");
            Assert.That(elapsed.ElapsedMilliseconds, Is.LessThan(1000));
            Assert.That(transport.HasAcceptedConnection, Is.True);
            AssertSuccess(retried.Result, TransportState.Connected, 0);
        });
    }

    [Test]
    public void AcceptedConnectionSupportsSynchronousReadWriteAndPeerEof()
    {
        SessionEndpoint endpoint = new SessionEndpoint { Host = "127.0.0.1", Port = 0 };
        TcpAcceptorTransport transport = Track(new TcpAcceptorTransport(Settings(2000)));
        AcceptedConnection connection = Connect(transport, endpoint);
        connection.Client.Client.Send([10, 11, 12], SocketFlags.None);
        byte[] incoming = new byte[8];

        TransportReadResult emptyRead = transport.ReadTransport(Span<byte>.Empty);
        TransportWriteResult emptyWrite = transport.WriteTransport(ReadOnlySpan<byte>.Empty);
        TransportReadResult read = transport.ReadTransport(incoming);
        TransportWriteResult written = transport.WriteTransport([20, 21, 22, 23]);
        byte[] outgoing = new byte[8];
        int received = connection.Client.Client.Receive(outgoing, SocketFlags.None);
        connection.Client.Client.Shutdown(SocketShutdown.Send);
        TransportReadResult eof = transport.ReadTransport(incoming);

        Assert.Multiple(() =>
        {
            AssertRead(emptyRead, TransportState.Connected, 0, false);
            AssertSuccess(emptyWrite, TransportState.Connected, 0);
            AssertRead(read, TransportState.Connected, 3, false);
            Assert.That(incoming.AsSpan(0, read.BytesRead).ToArray(), Is.EqualTo(new byte[] { 10, 11, 12 }));
            AssertSuccess(written, TransportState.Connected, 4);
            Assert.That(received, Is.EqualTo(4));
            Assert.That(outgoing.AsSpan(0, received).ToArray(), Is.EqualTo(new byte[] { 20, 21, 22, 23 }));
            AssertRead(eof, TransportState.Closed, 0, true);
        });
    }

    [Test]
    public void InstanceNeverAcceptsASecondConnectionAfterItsFirstPeer()
    {
        SessionEndpoint endpoint = new SessionEndpoint { Host = "127.0.0.1", Port = 0 };
        TcpAcceptorTransport transport = Track(new TcpAcceptorTransport(Settings(2000)));
        AcceptedConnection connection = Connect(transport, endpoint);

        TransportWriteResult whileConnected = transport.OpenTransport(endpoint, new SessionTlsSettings());
        TransportWriteResult closed = transport.CloseTransport();
        TransportWriteResult consumed = transport.OpenTransport(endpoint, new SessionTlsSettings());

        Assert.Multiple(() =>
        {
            AssertSuccess(connection.Result, TransportState.Connected, 0);
            AssertSuccess(whileConnected, TransportState.Connected, 0);
            AssertSuccess(closed, TransportState.Closed, 0);
            AssertFailure(consumed, TransportState.Closed, "acceptor-connection-consumed");
            Assert.That(transport.ListenerSocket, Is.Null);
            Assert.That(transport.CurrentSocket, Is.Null);
        });
    }

    [Test]
    public void CloseWhileListeningUnblocksThePendingAccept()
    {
        SessionEndpoint endpoint = new SessionEndpoint { Host = "127.0.0.1", Port = 0 };
        TcpAcceptorTransport transport = Track(new TcpAcceptorTransport(Settings(2000)));
        Task<TransportWriteResult> opening = Task.Run(() => transport.OpenTransport(endpoint, new SessionTlsSettings()));
        Assert.That(SpinWait.SpinUntil(() => transport.CurrentState == TransportState.Listening, TimeSpan.FromSeconds(2)), Is.True);

        TransportWriteResult closed = transport.CloseTransport();
        Assert.That(opening.Wait(TimeSpan.FromSeconds(2)), Is.True);
        TransportWriteResult interrupted = opening.Result;

        Assert.Multiple(() =>
        {
            AssertSuccess(closed, TransportState.Closed, 0);
            AssertFailure(interrupted, TransportState.Closed, "transport-closed");
            Assert.That(transport.ListenerSocket, Is.Null);
            Assert.That(transport.CurrentSocket, Is.Null);
        });
    }

    private AcceptedConnection Connect(TcpAcceptorTransport transport, SessionEndpoint endpoint)
    {
        Task<TransportWriteResult> opening = Task.Run(() => transport.OpenTransport(endpoint, new SessionTlsSettings()));
        Assert.That(SpinWait.SpinUntil(() => transport.CurrentState == TransportState.Listening, TimeSpan.FromSeconds(2)), Is.True);
        IPAddress address = IPAddress.Parse(endpoint.Host);
        TcpClient client = new TcpClient(address.AddressFamily);
        Clients.Add(client);
        client.Connect(address, endpoint.Port);
        Assert.That(opening.Wait(TimeSpan.FromSeconds(2)), Is.True);
        AcceptedConnection connection = new AcceptedConnection();
        connection.Client = client;
        connection.Result = opening.Result;
        return connection;
    }

    private TcpAcceptorTransport Track(TcpAcceptorTransport transport)
    {
        Transports.Add(transport);
        return transport;
    }

    private static SessionSettings Settings(int timeout)
    {
        return new SessionSettings { ConnectTimeoutMilliseconds = timeout };
    }

    private static void AssertRead(TransportReadResult result, TransportState state, int count, bool eof)
    {
        Assert.That(result.State, Is.EqualTo(state));
        Assert.That(result.BytesRead, Is.EqualTo(count));
        Assert.That(result.EndOfStream, Is.EqualTo(eof));
        Assert.That(result.WouldBlock, Is.False);
        Assert.That(result.ErrorCode, Is.Empty);
    }

    private static void AssertSuccess(TransportWriteResult result, TransportState state, int count)
    {
        Assert.That(result.State, Is.EqualTo(state));
        Assert.That(result.BytesWritten, Is.EqualTo(count));
        Assert.That(result.WouldBlock, Is.False);
        Assert.That(result.ErrorCode, Is.Empty);
    }

    private static void AssertFailure(TransportWriteResult result, TransportState state, string code)
    {
        Assert.That(result.State, Is.EqualTo(state));
        Assert.That(result.ErrorCode, Is.EqualTo(code));
        Assert.That(result.ErrorMessage, Is.Not.Empty);
    }

    private class AcceptedConnection
    {
        public TcpClient Client = null!;
        public TransportWriteResult Result = null!;
    }
}
