using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class TcpInitiatorTransportTests
{
    private readonly List<TcpListener> Listeners = [];
    private readonly List<Socket> AcceptedSockets = [];
    private readonly List<TcpInitiatorTransport> Transports = [];

    [TearDown]
    public void CloseNetworkResources()
    {
        foreach (TcpInitiatorTransport transport in Transports)
        {
            transport.CloseTransport();
        }
        foreach (Socket socket in AcceptedSockets)
        {
            socket.Close();
        }
        foreach (TcpListener listener in Listeners)
        {
            listener.Stop();
        }
    }

    [Test]
    public void ConstructorRequiresSettingsAndPositiveTimeout()
    {
        SessionSettings invalid = new SessionSettings { ConnectTimeoutMilliseconds = 0 };

        Assert.Multiple(() =>
        {
            Assert.Throws<FixTransportException>(() => new TcpInitiatorTransport(null!));
            Assert.Throws<FixTransportException>(() => new TcpInitiatorTransport(invalid));
        });
    }

    [Test]
    public void EndpointsUseStablePriorityOrderAndStandardSocketOptions()
    {
        TcpListener lowerPriority = StartListener(IPAddress.Loopback);
        TcpListener higherPriority = StartListener(IPAddress.Loopback);
        SessionEndpoint later = Endpoint(lowerPriority, 10);
        SessionEndpoint first = Endpoint(higherPriority, 1);
        SessionSettings settings = Settings([later, first], 1000);
        TcpInitiatorTransport transport = Track(new TcpInitiatorTransport(settings));

        TransportWriteResult opened = transport.OpenTransport(later, new SessionTlsSettings());
        Socket accepted = Accept(higherPriority);
        TransportWriteResult reopened = transport.OpenTransport(later, new SessionTlsSettings());
        byte[] keepAliveOption = transport.CurrentSocket!.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 4);

        Assert.Multiple(() =>
        {
            AssertSuccess(opened, TransportState.Connected, 0);
            AssertSuccess(reopened, TransportState.Connected, 0);
            Assert.That(lowerPriority.Pending(), Is.False);
            Assert.That(transport.CurrentSocket, Is.Not.Null);
            Assert.That(transport.CurrentSocket!.AddressFamily, Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(transport.CurrentSocket.NoDelay, Is.True);
            Assert.That(BitConverter.ToInt32(keepAliveOption), Is.Not.Zero);
            Assert.That(accepted.Connected, Is.True);
        });
    }

    [Test]
    public void MissingEndpointAndPreConnectionIoHaveStableFailures()
    {
        TcpInitiatorTransport transport = Track(new TcpInitiatorTransport(Settings([], 100)));

        TransportReadResult read = transport.ReadTransport(new byte[1]);
        TransportWriteResult write = transport.WriteTransport([1]);
        TransportWriteResult opened = transport.OpenTransport(null!, new SessionTlsSettings());

        Assert.Multiple(() =>
        {
            Assert.That(read.ErrorCode, Is.EqualTo("transport-not-connected"));
            Assert.That(write.ErrorCode, Is.EqualTo("transport-not-connected"));
            Assert.That(opened.State, Is.EqualTo(TransportState.Faulted));
            Assert.That(opened.ErrorCode, Is.EqualTo("missing-tcp-endpoint"));
        });
    }

    [Test]
    public void UnreachableEndpointFailsOverToNextPriority()
    {
        TcpListener unavailable = StartListener(IPAddress.Loopback);
        int unavailablePort = Port(unavailable);
        unavailable.Stop();
        Listeners.Remove(unavailable);
        TcpListener available = StartListener(IPAddress.Loopback);
        SessionEndpoint first = new SessionEndpoint { Host = "127.0.0.1", Port = unavailablePort, Priority = 0 };
        SessionEndpoint second = Endpoint(available, 1);
        TcpInitiatorTransport transport = Track(new TcpInitiatorTransport(Settings([second, first], 1000)));

        TransportWriteResult opened = transport.OpenTransport(second, new SessionTlsSettings());
        Socket accepted = Accept(available);

        Assert.Multiple(() =>
        {
            AssertSuccess(opened, TransportState.Connected, 0);
            Assert.That(accepted.Connected, Is.True);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ConnectsToIpv4AndIpv6LiteralEndpoints(bool ipv6)
    {
        if (ipv6 && !Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not available on this host.");
        }
        IPAddress address = ipv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
        TcpListener listener = StartListener(address);
        SessionEndpoint endpoint = Endpoint(listener, 0);
        TcpInitiatorTransport transport = Track(new TcpInitiatorTransport(Settings([endpoint], 1000)));

        TransportWriteResult opened = transport.OpenTransport(endpoint, new SessionTlsSettings());
        Socket accepted = Accept(listener);

        Assert.Multiple(() =>
        {
            AssertSuccess(opened, TransportState.Connected, 0);
            Assert.That(transport.CurrentSocket!.AddressFamily, Is.EqualTo(address.AddressFamily));
            Assert.That(accepted.AddressFamily, Is.EqualTo(address.AddressFamily));
        });
    }

    [Test]
    public void SynchronousReadWriteAndPeerEofHaveExplicitOutcomes()
    {
        TcpListener listener = StartListener(IPAddress.Loopback);
        SessionEndpoint endpoint = Endpoint(listener, 0);
        TcpInitiatorTransport transport = Track(new TcpInitiatorTransport(Settings([endpoint], 1000)));
        transport.OpenTransport(endpoint, new SessionTlsSettings());
        Socket server = Accept(listener);
        server.Send([10, 11, 12], SocketFlags.None);
        byte[] incoming = new byte[8];

        TransportReadResult emptyRead = transport.ReadTransport(Span<byte>.Empty);
        TransportWriteResult emptyWrite = transport.WriteTransport(ReadOnlySpan<byte>.Empty);
        TransportReadResult read = transport.ReadTransport(incoming);
        TransportWriteResult written = transport.WriteTransport([20, 21, 22, 23]);
        byte[] outgoing = new byte[8];
        int received = server.Receive(outgoing, SocketFlags.None);
        server.Shutdown(SocketShutdown.Send);
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
    public void CloseAndFailedOpenCanBothReconnectSafely()
    {
        TcpListener listener = StartListener(IPAddress.Loopback);
        SessionEndpoint endpoint = Endpoint(listener, 0);
        TcpInitiatorTransport transport = Track(new TcpInitiatorTransport(Settings([endpoint], 500)));
        TransportWriteResult firstOpen = transport.OpenTransport(endpoint, new SessionTlsSettings());
        Socket firstAccepted = Accept(listener);
        TransportWriteResult closed = transport.CloseTransport();
        TransportWriteResult secondOpen = transport.OpenTransport(endpoint, new SessionTlsSettings());
        Socket secondAccepted = Accept(listener);
        transport.CloseTransport();
        listener.Stop();
        Listeners.Remove(listener);

        TransportWriteResult failed = transport.OpenTransport(endpoint, new SessionTlsSettings());
        TcpListener replacement = new TcpListener(IPAddress.Loopback, endpoint.Port);
        replacement.Start();
        Listeners.Add(replacement);
        TransportWriteResult recovered = transport.OpenTransport(endpoint, new SessionTlsSettings());
        Socket recoveredAccepted = Accept(replacement);

        Assert.Multiple(() =>
        {
            AssertSuccess(firstOpen, TransportState.Connected, 0);
            Assert.That(firstAccepted.Connected, Is.True);
            AssertSuccess(closed, TransportState.Closed, 0);
            AssertSuccess(secondOpen, TransportState.Connected, 0);
            Assert.That(secondAccepted.Connected, Is.True);
            Assert.That(failed.State, Is.EqualTo(TransportState.Faulted));
            Assert.That(failed.ErrorCode, Is.Not.Empty);
            AssertSuccess(recovered, TransportState.Connected, 0);
            Assert.That(recoveredAccepted.Connected, Is.True);
        });
    }

    [Test]
    public void ConnectionAttemptIsBoundedByConfiguredTimeout()
    {
        SessionEndpoint endpoint = new SessionEndpoint { Host = "203.0.113.1", Port = 65000, Priority = 0 };
        TcpInitiatorTransport transport = Track(new TcpInitiatorTransport(Settings([endpoint], 30)));
        Stopwatch elapsed = Stopwatch.StartNew();

        TransportWriteResult result = transport.OpenTransport(endpoint, new SessionTlsSettings());
        elapsed.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(TransportState.Faulted));
            Assert.That(result.ErrorCode, Is.Not.Empty);
            Assert.That(elapsed.ElapsedMilliseconds, Is.LessThan(3000));
        });
    }

    private TcpListener StartListener(IPAddress address)
    {
        TcpListener listener = new TcpListener(address, 0);
        listener.Start();
        Listeners.Add(listener);
        return listener;
    }

    private Socket Accept(TcpListener listener)
    {
        Socket socket = listener.AcceptSocket();
        AcceptedSockets.Add(socket);
        return socket;
    }

    private TcpInitiatorTransport Track(TcpInitiatorTransport transport)
    {
        Transports.Add(transport);
        return transport;
    }

    private static SessionSettings Settings(List<SessionEndpoint> endpoints, int timeout)
    {
        return new SessionSettings { Endpoints = endpoints, ConnectTimeoutMilliseconds = timeout };
    }

    private static SessionEndpoint Endpoint(TcpListener listener, int priority)
    {
        IPEndPoint endpoint = (IPEndPoint)listener.LocalEndpoint;
        return new SessionEndpoint { Host = endpoint.Address.ToString(), Port = endpoint.Port, Priority = priority };
    }

    private static int Port(TcpListener listener)
    {
        return ((IPEndPoint)listener.LocalEndpoint).Port;
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
}
