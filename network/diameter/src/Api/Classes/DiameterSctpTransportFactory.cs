using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

/// <summary>Creates capability-gated Diameter SCTP listeners and connections.</summary>
public class DiameterSctpTransportFactory : IDiameterTransportFactory
{
    internal const int AddressResolutionTimeoutMilliseconds = 10000;
    internal DiameterTcpTransportFactory AddressResolver =
        new DiameterTcpTransportFactory();
    internal ISctpSocketCapabilityProbe CapabilityProbe =
        new SctpSocketCapabilityProbe();

    /// <summary>Initializes an SCTP transport factory.</summary>
    public DiameterSctpTransportFactory()
    {
    }

    /// <summary>Probes managed socket, address-family, stream, and payload-policy support.</summary>
    /// <param name="transport">The SCTP transport configuration.</param>
    /// <param name="security">The unsecured or IPsec security configuration.</param>
    /// <returns>The SCTP support outcome and capability metadata.</returns>
    public DiameterTransportSupportResult ProbeDiameterTransportSupport(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterTransportSupportResult result = CreateSctpSupportResult();
        if (transport == null || security == null)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "configuration-required";
            result.Message = "SCTP transport and security configurations are required.";
            return result;
        }

        result.Metadata.MaximumInboundStreams = transport.SctpStreamCount;
        result.Metadata.MaximumOutboundStreams = transport.SctpStreamCount;
        if (transport.Kind != DiameterTransportKind.Sctp)
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "transport-unsupported";
            result.Message = "The SCTP factory only supports SCTP configuration.";
            return result;
        }

        if (security.Mode != DiameterSecurityMode.None &&
            security.Mode != DiameterSecurityMode.Ipsec)
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "security-unsupported";
            result.Message = "Built-in SCTP supports unsecured or external IPsec security.";
            return result;
        }

        if ((security.Mode == DiameterSecurityMode.None &&
            !security.AllowUnsecuredTransport) ||
            (security.Mode == DiameterSecurityMode.Ipsec &&
            security.AllowUnsecuredTransport))
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "security-configuration-invalid";
            result.Message = "The SCTP security configuration is inconsistent.";
            return result;
        }

        if (!string.IsNullOrWhiteSpace(security.LocalCertificatePath) ||
            !string.IsNullOrWhiteSpace(security.LocalCertificatePassword) ||
            !string.IsNullOrWhiteSpace(security.TrustAnchorPath) ||
            !string.IsNullOrWhiteSpace(security.ExpectedPeerName) ||
            security.RequirePeerCertificate || security.CheckCertificateRevocation ||
            security.CertificatePeerPolicy != null)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "certificate-configuration-invalid";
            result.Message = "Certificate settings require a DTLS-capable external transport.";
            return result;
        }

        if (transport.Port <= 0 || transport.Port > 65535 ||
            (!transport.EnableIpv4 && !transport.EnableIpv6) ||
            transport.ReceiveBufferSize < DiameterProtocolConstants.MessageHeaderSize ||
            transport.SendBufferSize < DiameterProtocolConstants.MessageHeaderSize ||
            transport.ListenBacklog <= 0 || transport.SctpStreamCount <= 0 ||
            transport.SctpStreamCount > ushort.MaxValue)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "transport-configuration-invalid";
            result.Message = "The SCTP transport configuration is invalid.";
            return result;
        }

        SctpProbeResult ipv4 = new SctpProbeResult();
        if (transport.EnableIpv4 && Socket.OSSupportsIPv4)
        {
            ipv4 = CapabilityProbe.ProbeSctpAddressFamily(
                AddressFamily.InterNetwork,
                transport.SctpStreamCount);
        }

        SctpProbeResult ipv6 = new SctpProbeResult();
        if (transport.EnableIpv6 && Socket.OSSupportsIPv6)
        {
            ipv6 = CapabilityProbe.ProbeSctpAddressFamily(
                AddressFamily.InterNetworkV6,
                transport.SctpStreamCount);
        }

        result.Metadata.SupportsIpv4 = ipv4.Supported;
        result.Metadata.SupportsIpv6 = ipv6.Supported;
        if (!ipv4.Supported && !ipv6.Supported)
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "sctp-platform-unsupported";
            result.Message = ReadSctpProbeFailure(ipv4, ipv6);
        }

        return result;
    }

    /// <summary>Creates a passive SCTP listener when the configured host supports it.</summary>
    /// <param name="transport">The SCTP listener configuration.</param>
    /// <param name="security">The unsecured or IPsec security configuration.</param>
    /// <returns>The listener creation outcome.</returns>
    public DiameterListenerResult CreateDiameterListener(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterTransportSupportResult support = ProbeDiameterTransportSupport(
            transport,
            security);
        if (support.Status != DiameterTransportStatus.Success)
        {
            return CreateSctpListenerFailure(support.Status, support.Code, support.Message);
        }

        if (transport == null || security == null || !transport.EnableListener)
        {
            return CreateSctpListenerFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "listener-configuration-invalid",
                "An enabled SCTP listener configuration is required.");
        }

        long deadline = Environment.TickCount64 + AddressResolutionTimeoutMilliseconds;
        TcpAddressResolution local = AddressResolver.ResolveTcpListenerAddresses(
            transport,
            deadline);
        if (local.Status != DiameterTransportStatus.Success)
        {
            return CreateSctpListenerFailure(local.Status, local.Code, local.Message);
        }

        List<Socket> sockets = new List<Socket>();
        int index = 0;
        while (index < local.Addresses.Count)
        {
            IPAddress address = local.Addresses[index];
            bool familySupported =
                (address.AddressFamily == AddressFamily.InterNetwork &&
                support.Metadata.SupportsIpv4) ||
                (address.AddressFamily == AddressFamily.InterNetworkV6 &&
                support.Metadata.SupportsIpv6);
            if (familySupported)
            {
                Socket socket = SctpSocketSupport.CreateConfiguredSctpSocket(
                    address.AddressFamily,
                    transport.SctpStreamCount,
                    transport.NoDelay,
                    transport.ReceiveBufferSize,
                    transport.SendBufferSize);
                socket.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true);
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    socket.DualMode = address.Equals(IPAddress.IPv6Any) &&
                        support.Metadata.SupportsIpv4;
                }

                socket.Bind(new IPEndPoint(address, transport.Port));
                socket.Listen(transport.ListenBacklog);
                sockets.Add(socket);
            }

            index = index + 1;
        }

        if (sockets.Count == 0)
        {
            return CreateSctpListenerFailure(
                DiameterTransportStatus.Unsupported,
                "address-family-unsupported",
                "No resolved listener address has SCTP support.");
        }

        DiameterListenerMetadata metadata = CreateSctpListenerMetadata(
            sockets[0],
            transport,
            security,
            support.Metadata);
        SctpSocketDiameterListener listener = new SctpSocketDiameterListener(
            sockets,
            transport,
            security,
            metadata);
        DiameterListenerResult result = new DiameterListenerResult();
        result.Listener = listener;
        result.Metadata = TcpSocketDiameterListener.CloneListenerMetadata(metadata);
        return result;
    }

    /// <summary>Creates an active ordered SCTP connection when supported.</summary>
    /// <param name="endpoint">The resolved or named remote SCTP endpoint.</param>
    /// <param name="transport">The local SCTP configuration.</param>
    /// <param name="security">The unsecured or IPsec security configuration.</param>
    /// <param name="timeoutMilliseconds">The positive total connection timeout.</param>
    /// <returns>The active connection outcome.</returns>
    public DiameterConnectionResult ConnectDiameterEndpoint(
        DiameterEndpoint? endpoint,
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security,
        int timeoutMilliseconds)
    {
        DiameterTransportSupportResult support = ProbeDiameterTransportSupport(
            transport,
            security);
        if (support.Status != DiameterTransportStatus.Success)
        {
            return CreateSctpConnectionFailure(support.Status, support.Code, support.Message);
        }

        if (endpoint == null || transport == null || security == null ||
            timeoutMilliseconds <= 0 || !transport.EnableActiveConnections ||
            endpoint.Port <= 0 || endpoint.Port > 65535 ||
            endpoint.Transport != DiameterTransportKind.Sctp)
        {
            return CreateSctpConnectionFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "connection-input-invalid",
                "A valid SCTP endpoint and positive connection timeout are required.");
        }

        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        TcpAddressResolution remote = AddressResolver.ResolveTcpAddresses(
            endpoint.Address,
            endpoint.Host,
            transport,
            deadline);
        if (remote.Status != DiameterTransportStatus.Success)
        {
            return CreateSctpConnectionFailure(remote.Status, remote.Code, remote.Message);
        }

        TcpAddressResolution local = AddressResolver.ResolveLocalTcpAddresses(
            transport,
            deadline);
        if (local.Status != DiameterTransportStatus.Success)
        {
            return CreateSctpConnectionFailure(local.Status, local.Code, local.Message);
        }

        DiameterConnectionResult lastFailure = CreateSctpConnectionFailure(
            DiameterTransportStatus.Failed,
            "connect-failed",
            "No remote SCTP address accepted the connection.");
        int index = 0;
        while (index < remote.Addresses.Count)
        {
            IPAddress remoteAddress = remote.Addresses[index];
            bool familySupported =
                (remoteAddress.AddressFamily == AddressFamily.InterNetwork &&
                support.Metadata.SupportsIpv4) ||
                (remoteAddress.AddressFamily == AddressFamily.InterNetworkV6 &&
                support.Metadata.SupportsIpv6);
            if (!familySupported)
            {
                index = index + 1;
                continue;
            }

            int remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
            if (remaining <= 0)
            {
                return CreateSctpConnectionFailure(
                    DiameterTransportStatus.TimedOut,
                    "connect-timeout",
                    "The SCTP connection did not complete before its timeout.");
            }

            Socket socket = SctpSocketSupport.CreateConfiguredSctpSocket(
                remoteAddress.AddressFamily,
                transport.SctpStreamCount,
                transport.NoDelay,
                transport.ReceiveBufferSize,
                transport.SendBufferSize);
            IPAddress? localAddress = AddressResolver.FindTcpAddressFamily(
                local.Addresses,
                remoteAddress.AddressFamily);
            if (local.Addresses.Count > 0 && localAddress == null)
            {
                socket.Close();
                index = index + 1;
                continue;
            }

            if (localAddress != null)
            {
                socket.Bind(new IPEndPoint(localAddress, 0));
            }

            Task connectTask = socket.ConnectAsync(
                new IPEndPoint(remoteAddress, endpoint.Port));
            bool completed = SpinWait.SpinUntil(() => connectTask.IsCompleted, remaining);
            if (!completed)
            {
                socket.Close();
                return CreateSctpConnectionFailure(
                    DiameterTransportStatus.TimedOut,
                    "connect-timeout",
                    "The SCTP connection did not complete before its timeout.");
            }

            if (connectTask.IsCompletedSuccessfully && socket.Connected)
            {
                DiameterConnectionMetadata metadata = CreateSctpConnectionMetadata(
                    socket,
                    endpoint,
                    remoteAddress,
                    security,
                    support.Metadata,
                    transport.SctpStreamCount);
                SctpSocketDiameterConnection connection =
                    new SctpSocketDiameterConnection(
                        socket,
                        metadata,
                        transport.SctpStreamCount);
                DiameterConnectionResult result = new DiameterConnectionResult();
                result.Connection = connection;
                result.Metadata = QueuedDiameterConnection.CloneConnectionMetadata(metadata);
                return result;
            }

            lastFailure = CreateSctpConnectionFailure(
                DiameterTransportStatus.Failed,
                "connect-failed",
                SctpSocketDiameterConnection.ReadStaticSctpTaskFailure(
                    connectTask,
                    "The SCTP connection attempt failed."));
            socket.Close();
            index = index + 1;
        }

        return lastFailure;
    }

    internal DiameterTransportSupportResult CreateSctpSupportResult()
    {
        DiameterTransportSupportResult result = new DiameterTransportSupportResult();
        result.Metadata.Kind = DiameterTransportKind.Sctp;
        result.Metadata.SupportsActiveConnections = true;
        result.Metadata.SupportsListeners = true;
        result.Metadata.MessageOriented = true;
        result.Metadata.PayloadProtocolIdentifier =
            DiameterProtocolConstants.SctpPayloadProtocolIdentifier;
        return result;
    }

    internal string ReadSctpProbeFailure(
        SctpProbeResult ipv4,
        SctpProbeResult ipv6)
    {
        if (!string.IsNullOrWhiteSpace(ipv4.Message))
        {
            return ipv4.Message;
        }

        if (!string.IsNullOrWhiteSpace(ipv6.Message))
        {
            return ipv6.Message;
        }

        return "No enabled address family exposes usable SCTP sockets.";
    }

    internal DiameterConnectionMetadata CreateSctpConnectionMetadata(
        Socket socket,
        DiameterEndpoint requestedEndpoint,
        IPAddress remoteAddress,
        DiameterSecurityConfiguration security,
        DiameterTransportMetadata support,
        int streamCount)
    {
        EndPoint? localValue = socket.LocalEndPoint;
        EndPoint? remoteValue = socket.RemoteEndPoint;
        if (localValue == null || remoteValue == null)
        {
            throw new DiameterTransportException(
                "A connected SCTP socket requires local and remote endpoints.");
        }

        IPEndPoint local = (IPEndPoint)localValue;
        IPEndPoint remote = (IPEndPoint)remoteValue;
        DiameterConnectionMetadata metadata = new DiameterConnectionMetadata();
        metadata.ConnectionId = $"{local.Address}:{local.Port}-{remote.Address}:{remote.Port}";
        metadata.LocalEndpoint.Address = local.Address;
        metadata.LocalEndpoint.Host = local.Address.ToString();
        metadata.LocalEndpoint.Port = local.Port;
        metadata.LocalEndpoint.Transport = DiameterTransportKind.Sctp;
        metadata.LocalEndpoint.Security = security.Mode;
        metadata.RemoteEndpoint.Identity.Value = requestedEndpoint.Identity.Value;
        metadata.RemoteEndpoint.Host = requestedEndpoint.Host;
        metadata.RemoteEndpoint.Address = remoteAddress;
        metadata.RemoteEndpoint.Port = requestedEndpoint.Port;
        metadata.RemoteEndpoint.Transport = DiameterTransportKind.Sctp;
        metadata.RemoteEndpoint.Security = security.Mode;
        metadata.Direction = DiameterConnectionDirection.Outbound;
        metadata.Transport = QueuedDiameterConnection.CloneTransportMetadata(support);
        metadata.Transport.MaximumInboundStreams = streamCount;
        metadata.Transport.MaximumOutboundStreams = streamCount;
        PopulateSctpConnectionSecurity(metadata.Security, security.Mode);
        metadata.EstablishedAt = DateTimeOffset.UtcNow;
        metadata.Open = true;
        return metadata;
    }

    internal DiameterListenerMetadata CreateSctpListenerMetadata(
        Socket socket,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security,
        DiameterTransportMetadata support)
    {
        EndPoint? endpointValue = socket.LocalEndPoint;
        if (endpointValue == null)
        {
            throw new DiameterTransportException(
                "A bound SCTP listener requires a local endpoint.");
        }

        IPEndPoint endpoint = (IPEndPoint)endpointValue;
        DiameterListenerMetadata metadata = new DiameterListenerMetadata();
        metadata.LocalEndpoint.Host = transport.Host;
        metadata.LocalEndpoint.Address = endpoint.Address;
        metadata.LocalEndpoint.Port = endpoint.Port;
        metadata.LocalEndpoint.Transport = DiameterTransportKind.Sctp;
        metadata.LocalEndpoint.Security = security.Mode;
        metadata.Transport = QueuedDiameterConnection.CloneTransportMetadata(support);
        metadata.SecurityMode = security.Mode;
        metadata.Open = true;
        return metadata;
    }

    internal void PopulateSctpConnectionSecurity(
        DiameterSecurityMetadata metadata,
        DiameterSecurityMode securityMode)
    {
        metadata.Mode = securityMode;
        if (securityMode == DiameterSecurityMode.Ipsec)
        {
            metadata.Authenticated = true;
            metadata.Encrypted = true;
            metadata.Protocol = "IPsec";
        }
    }

    internal DiameterListenerResult CreateSctpListenerFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterListenerResult result = new DiameterListenerResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Metadata.Transport.Kind = DiameterTransportKind.Sctp;
        result.Metadata.Transport.MessageOriented = true;
        result.Metadata.Transport.PayloadProtocolIdentifier =
            DiameterProtocolConstants.SctpPayloadProtocolIdentifier;
        return result;
    }

    internal DiameterConnectionResult CreateSctpConnectionFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Metadata.Transport.Kind = DiameterTransportKind.Sctp;
        result.Metadata.Transport.MessageOriented = true;
        result.Metadata.Transport.PayloadProtocolIdentifier =
            DiameterProtocolConstants.SctpPayloadProtocolIdentifier;
        return result;
    }
}
