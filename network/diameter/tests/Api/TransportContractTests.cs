using System;
using System.Net;

namespace diameter.Tests;

public class TransportContractTests
{
    [Test]
    public void TransportDataDefaultsAreUsableAndIndependentlyOwned()
    {
        DiameterTransportMetadata firstTransport = new DiameterTransportMetadata();
        DiameterTransportMetadata secondTransport = new DiameterTransportMetadata();
        DiameterSecurityMetadata firstSecurity = new DiameterSecurityMetadata();
        DiameterSecurityMetadata secondSecurity = new DiameterSecurityMetadata();
        DiameterListenerMetadata firstListener = new DiameterListenerMetadata();
        DiameterListenerMetadata secondListener = new DiameterListenerMetadata();
        DiameterConnectionMetadata firstConnection = new DiameterConnectionMetadata();
        DiameterConnectionMetadata secondConnection = new DiameterConnectionMetadata();
        DiameterTransportSupportResult support = new DiameterTransportSupportResult();
        DiameterListenerResult listener = new DiameterListenerResult();
        DiameterConnectionResult connection = new DiameterConnectionResult();
        DiameterTransportIoResult io = new DiameterTransportIoResult();
        DiameterTransportOperationResult operation = new DiameterTransportOperationResult();

        Assert.Multiple(() =>
        {
            Assert.That(firstTransport.Kind, Is.EqualTo(DiameterTransportKind.Tcp));
            Assert.That(firstTransport.SupportsActiveConnections, Is.False);
            Assert.That(firstTransport.SupportsListeners, Is.False);
            Assert.That(firstTransport.SupportsIpv4, Is.False);
            Assert.That(firstTransport.SupportsIpv6, Is.False);
            Assert.That(firstTransport.MessageOriented, Is.False);
            Assert.That(firstTransport.MaximumInboundStreams, Is.EqualTo(1));
            Assert.That(firstTransport.MaximumOutboundStreams, Is.EqualTo(1));
            Assert.That(firstTransport.PayloadProtocolIdentifier, Is.Zero);

            Assert.That(firstSecurity.Mode, Is.EqualTo(DiameterSecurityMode.None));
            Assert.That(firstSecurity.Authenticated, Is.False);
            Assert.That(firstSecurity.Encrypted, Is.False);
            Assert.That(firstSecurity.MutuallyAuthenticated, Is.False);
            Assert.That(firstSecurity.Protocol, Is.Empty);
            Assert.That(firstSecurity.CipherSuite, Is.Empty);
            Assert.That(firstSecurity.LocalCertificateSubject, Is.Empty);
            Assert.That(firstSecurity.PeerCertificateSubject, Is.Empty);
            Assert.That(firstSecurity.PeerCertificateIssuer, Is.Empty);
            Assert.That(firstSecurity.PeerCertificateThumbprint, Is.Empty);
            Assert.That(firstSecurity.PeerCertificateNotBefore,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(firstSecurity.PeerCertificateNotAfter,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(firstSecurity.PeerCertificateDnsNames, Is.Empty);
            Assert.That(firstSecurity.PeerCertificateDnsNames,
                Is.Not.SameAs(secondSecurity.PeerCertificateDnsNames));

            Assert.That(firstListener.LocalEndpoint, Is.Not.Null);
            Assert.That(firstListener.LocalEndpoint,
                Is.Not.SameAs(secondListener.LocalEndpoint));
            Assert.That(firstListener.Transport, Is.Not.SameAs(secondListener.Transport));
            Assert.That(firstListener.SecurityMode, Is.EqualTo(DiameterSecurityMode.None));
            Assert.That(firstListener.Open, Is.False);

            Assert.That(firstConnection.ConnectionId, Is.Empty);
            Assert.That(firstConnection.LocalEndpoint,
                Is.Not.SameAs(secondConnection.LocalEndpoint));
            Assert.That(firstConnection.RemoteEndpoint,
                Is.Not.SameAs(secondConnection.RemoteEndpoint));
            Assert.That(firstConnection.Transport,
                Is.Not.SameAs(secondConnection.Transport));
            Assert.That(firstConnection.Security,
                Is.Not.SameAs(secondConnection.Security));
            Assert.That(firstConnection.Direction,
                Is.EqualTo(DiameterConnectionDirection.Inbound));
            Assert.That(firstConnection.InboundStream, Is.Zero);
            Assert.That(firstConnection.OutboundStream, Is.Zero);
            Assert.That(firstConnection.EstablishedAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(firstConnection.Open, Is.False);

            Assert.That(support.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(support.Metadata, Is.Not.Null);
            Assert.That(support.Code, Is.Empty);
            Assert.That(support.Message, Is.Empty);
            Assert.That(listener.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(listener.Listener, Is.Null);
            Assert.That(listener.Metadata, Is.Not.Null);
            Assert.That(connection.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(connection.Connection, Is.Null);
            Assert.That(connection.Metadata, Is.Not.Null);
            Assert.That(io.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(io.TransferredCount, Is.Zero);
            Assert.That(io.EndOfStream, Is.False);
            Assert.That(operation.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(operation.Code, Is.Empty);
            Assert.That(operation.Message, Is.Empty);
            Assert.That(firstTransport, Is.Not.SameAs(secondTransport));
        });
    }

    [Test]
    public void MetadataCarriesTransportAndNegotiatedSecurityDetails()
    {
        DateTimeOffset notBefore = DateTimeOffset.FromUnixTimeSeconds(1704067200L);
        DateTimeOffset notAfter = notBefore.AddDays(30);
        DiameterConnectionMetadata metadata = CreateConnectionMetadata();
        metadata.Security.Mode = DiameterSecurityMode.Tls;
        metadata.Security.Authenticated = true;
        metadata.Security.Encrypted = true;
        metadata.Security.MutuallyAuthenticated = true;
        metadata.Security.Protocol = "TLS 1.3";
        metadata.Security.CipherSuite = "TLS_AES_128_GCM_SHA256";
        metadata.Security.LocalCertificateSubject = "CN=local.example";
        metadata.Security.PeerCertificateSubject = "CN=peer.example";
        metadata.Security.PeerCertificateIssuer = "CN=Test CA";
        metadata.Security.PeerCertificateThumbprint = "001122";
        metadata.Security.PeerCertificateNotBefore = notBefore;
        metadata.Security.PeerCertificateNotAfter = notAfter;
        metadata.Security.PeerCertificateDnsNames.Add("peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(metadata.ConnectionId, Is.EqualTo("connection-1"));
            Assert.That(metadata.LocalEndpoint.Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.1")));
            Assert.That(metadata.RemoteEndpoint.Address,
                Is.EqualTo(IPAddress.Parse("198.51.100.2")));
            Assert.That(metadata.Direction, Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(metadata.Transport.Kind, Is.EqualTo(DiameterTransportKind.Sctp));
            Assert.That(metadata.Transport.SupportsActiveConnections, Is.True);
            Assert.That(metadata.Transport.SupportsListeners, Is.True);
            Assert.That(metadata.Transport.SupportsIpv4, Is.True);
            Assert.That(metadata.Transport.SupportsIpv6, Is.True);
            Assert.That(metadata.Transport.MessageOriented, Is.True);
            Assert.That(metadata.Transport.MaximumInboundStreams, Is.EqualTo(8));
            Assert.That(metadata.Transport.MaximumOutboundStreams, Is.EqualTo(4));
            Assert.That(metadata.Transport.PayloadProtocolIdentifier, Is.EqualTo(46U));
            Assert.That(metadata.Security.Mode, Is.EqualTo(DiameterSecurityMode.Tls));
            Assert.That(metadata.Security.Authenticated, Is.True);
            Assert.That(metadata.Security.Encrypted, Is.True);
            Assert.That(metadata.Security.MutuallyAuthenticated, Is.True);
            Assert.That(metadata.Security.Protocol, Is.EqualTo("TLS 1.3"));
            Assert.That(metadata.Security.CipherSuite,
                Is.EqualTo("TLS_AES_128_GCM_SHA256"));
            Assert.That(metadata.Security.PeerCertificateNotBefore, Is.EqualTo(notBefore));
            Assert.That(metadata.Security.PeerCertificateNotAfter, Is.EqualTo(notAfter));
            Assert.That(metadata.Security.PeerCertificateDnsNames,
                Is.EqualTo(new[] { "peer.example" }));
            Assert.That(metadata.InboundStream, Is.EqualTo(2));
            Assert.That(metadata.OutboundStream, Is.EqualTo(3));
            Assert.That(metadata.Open, Is.True);
        });
    }

    [Test]
    public void TransportContractsSupportProbeListenConnectIoAndIdempotentClose()
    {
        ScriptedDiameterConnection scriptedConnection = new ScriptedDiameterConnection();
        ScriptedDiameterListener scriptedListener = new ScriptedDiameterListener();
        scriptedListener.Connection = scriptedConnection;
        ScriptedDiameterTransportFactory scriptedFactory =
            new ScriptedDiameterTransportFactory();
        scriptedFactory.Connection = scriptedConnection;
        scriptedFactory.Listener = scriptedListener;
        IDiameterTransportFactory factory = scriptedFactory;
        DiameterTransportConfiguration transport = new DiameterTransportConfiguration();
        DiameterSecurityConfiguration security = new DiameterSecurityConfiguration();
        DiameterEndpoint endpoint = CreateEndpoint("peer.example", "198.51.100.2", 3868);

        DiameterTransportSupportResult support = factory.ProbeDiameterTransportSupport(
            transport,
            security);
        DiameterListenerResult listenerResult = factory.CreateDiameterListener(
            transport,
            security);
        DiameterConnectionResult accepted = listenerResult.Listener!
            .AcceptDiameterConnection(1000);
        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            endpoint,
            transport,
            security,
            1000);
        byte[] outbound = new byte[] { 1, 2, 3, 4 };
        DiameterTransportIoResult sent = connected.Connection!
            .SendDiameterConnectionBytes(outbound, 1000);
        byte[] inbound = new byte[4];
        DiameterTransportIoResult received = accepted.Connection!
            .ReceiveDiameterConnectionBytes(inbound, 1000);
        DiameterConnectionMetadata connectionMetadata = connected.Connection!
            .ReadDiameterConnectionMetadata();
        DiameterListenerMetadata listenerMetadata = listenerResult.Listener!
            .ReadDiameterListenerMetadata();
        DiameterTransportOperationResult connectionClose = connected.Connection!
            .CloseDiameterConnection();
        DiameterTransportOperationResult repeatedConnectionClose = connected.Connection!
            .CloseDiameterConnection();
        DiameterTransportOperationResult listenerClose = listenerResult.Listener!
            .CloseDiameterListener();
        DiameterTransportOperationResult repeatedListenerClose = listenerResult.Listener!
            .CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(support.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(support.Metadata.SupportsActiveConnections, Is.True);
            Assert.That(support.Metadata.SupportsListeners, Is.True);
            Assert.That(listenerResult.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(connected.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(accepted.Connection, Is.SameAs(scriptedConnection));
            Assert.That(connected.Connection, Is.SameAs(scriptedConnection));
            Assert.That(sent.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(sent.TransferredCount, Is.EqualTo(4));
            Assert.That(scriptedConnection.SentBytes, Is.EqualTo(outbound));
            Assert.That(received.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(received.TransferredCount, Is.EqualTo(4));
            Assert.That(inbound, Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
            Assert.That(connectionMetadata.ConnectionId, Is.EqualTo("scripted"));
            Assert.That(listenerMetadata.Open, Is.True);
            Assert.That(connectionClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(repeatedConnectionClose.Status,
                Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(listenerClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(repeatedListenerClose.Status,
                Is.EqualTo(DiameterTransportStatus.Success));
        });
    }

    private static DiameterConnectionMetadata CreateConnectionMetadata()
    {
        DiameterConnectionMetadata metadata = new DiameterConnectionMetadata();
        metadata.ConnectionId = "connection-1";
        metadata.LocalEndpoint = CreateEndpoint("local.example", "192.0.2.1", 3868);
        metadata.RemoteEndpoint = CreateEndpoint("peer.example", "198.51.100.2", 3868);
        metadata.Direction = DiameterConnectionDirection.Outbound;
        metadata.Transport.Kind = DiameterTransportKind.Sctp;
        metadata.Transport.SupportsActiveConnections = true;
        metadata.Transport.SupportsListeners = true;
        metadata.Transport.SupportsIpv4 = true;
        metadata.Transport.SupportsIpv6 = true;
        metadata.Transport.MessageOriented = true;
        metadata.Transport.MaximumInboundStreams = 8;
        metadata.Transport.MaximumOutboundStreams = 4;
        metadata.Transport.PayloadProtocolIdentifier = 46U;
        metadata.InboundStream = 2;
        metadata.OutboundStream = 3;
        metadata.EstablishedAt = DateTimeOffset.FromUnixTimeSeconds(1704067200L);
        metadata.Open = true;
        return metadata;
    }

    private static DiameterEndpoint CreateEndpoint(string host, string address, int port)
    {
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = host;
        endpoint.Host = host;
        endpoint.Address = IPAddress.Parse(address);
        endpoint.Port = port;
        return endpoint;
    }
}

public class ScriptedDiameterTransportFactory : IDiameterTransportFactory
{
    public ScriptedDiameterListener? Listener;
    public ScriptedDiameterConnection? Connection;

    public DiameterTransportSupportResult ProbeDiameterTransportSupport(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterTransportSupportResult result = new DiameterTransportSupportResult();
        result.Metadata.SupportsActiveConnections = true;
        result.Metadata.SupportsListeners = true;
        result.Metadata.SupportsIpv4 = true;
        result.Metadata.SupportsIpv6 = true;
        return result;
    }

    public DiameterListenerResult CreateDiameterListener(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterListenerResult result = new DiameterListenerResult();
        result.Listener = Listener;
        if (Listener != null)
        {
            result.Metadata = Listener.ReadDiameterListenerMetadata();
        }

        return result;
    }

    public DiameterConnectionResult ConnectDiameterEndpoint(
        DiameterEndpoint? endpoint,
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security,
        int timeoutMilliseconds)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Connection = Connection;
        if (Connection != null)
        {
            result.Metadata = Connection.ReadDiameterConnectionMetadata();
        }

        return result;
    }
}

public class ScriptedDiameterListener : IDiameterListener
{
    public ScriptedDiameterConnection? Connection;
    public bool Closed;

    public DiameterConnectionResult AcceptDiameterConnection(int timeoutMilliseconds)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Connection = Connection;
        if (Connection != null)
        {
            result.Metadata = Connection.ReadDiameterConnectionMetadata();
        }

        return result;
    }

    public DiameterListenerMetadata ReadDiameterListenerMetadata()
    {
        DiameterListenerMetadata metadata = new DiameterListenerMetadata();
        metadata.Open = !Closed;
        return metadata;
    }

    public DiameterTransportOperationResult CloseDiameterListener()
    {
        Closed = true;
        return new DiameterTransportOperationResult();
    }
}

public class ScriptedDiameterConnection : IDiameterConnection
{
    public byte[] PendingBytes = new byte[] { 5, 6, 7, 8 };
    public byte[] SentBytes = new byte[0];
    public bool Closed;

    public DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds)
    {
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        if (Closed)
        {
            result.Status = DiameterTransportStatus.Closed;
            return result;
        }

        SentBytes = source.ToArray();
        result.TransferredCount = source.Length;
        return result;
    }

    public DiameterTransportIoResult ReceiveDiameterConnectionBytes(
        Span<byte> destination,
        int timeoutMilliseconds)
    {
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        if (Closed)
        {
            result.Status = DiameterTransportStatus.Closed;
            return result;
        }

        int count = Math.Min(destination.Length, PendingBytes.Length);
        PendingBytes.AsSpan(0, count).CopyTo(destination);
        result.TransferredCount = count;
        return result;
    }

    public DiameterConnectionMetadata ReadDiameterConnectionMetadata()
    {
        DiameterConnectionMetadata metadata = new DiameterConnectionMetadata();
        metadata.ConnectionId = "scripted";
        metadata.Open = !Closed;
        return metadata;
    }

    public DiameterTransportOperationResult CloseDiameterConnection()
    {
        Closed = true;
        return new DiameterTransportOperationResult();
    }
}
