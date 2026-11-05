using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

/// <summary>
/// Creates unencrypted or externally IPsec-protected Diameter TCP listeners and connections.
/// </summary>
public class DiameterTcpTransportFactory : IDiameterTransportFactory
{
    internal const int ListenerResolutionTimeoutMilliseconds = 10000;

    /// <summary>Initializes a TCP transport factory.</summary>
    public DiameterTcpTransportFactory()
    {
    }

    /// <summary>Probes whether the requested TCP and security combination is supported.</summary>
    /// <param name="transport">The TCP transport configuration.</param>
    /// <param name="security">The unsecured or IPsec security configuration.</param>
    /// <returns>The support outcome and TCP capability metadata.</returns>
    public DiameterTransportSupportResult ProbeDiameterTransportSupport(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterTransportSupportResult result = new DiameterTransportSupportResult();
        result.Metadata.Kind = DiameterTransportKind.Tcp;
        result.Metadata.SupportsActiveConnections = true;
        result.Metadata.SupportsListeners = true;
        result.Metadata.SupportsIpv4 = Socket.OSSupportsIPv4;
        result.Metadata.SupportsIpv6 = Socket.OSSupportsIPv6;
        if (transport == null || security == null)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "configuration-required";
            result.Message = "TCP transport and security configurations are required.";
            return result;
        }

        if (transport.Kind != DiameterTransportKind.Tcp)
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "transport-unsupported";
            result.Message = "The TCP factory only supports TCP transport configuration.";
            return result;
        }

        if (security.Mode != DiameterSecurityMode.None &&
            security.Mode != DiameterSecurityMode.Ipsec)
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "security-unsupported";
            result.Message = "The TCP factory supports unsecured or external IPsec security.";
            return result;
        }

        if ((security.Mode == DiameterSecurityMode.None &&
            !security.AllowUnsecuredTransport) ||
            (security.Mode == DiameterSecurityMode.Ipsec &&
            security.AllowUnsecuredTransport))
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "security-configuration-invalid";
            result.Message = "The TCP security configuration is inconsistent.";
            return result;
        }

        if (!string.IsNullOrEmpty(security.LocalCertificatePath) ||
            !string.IsNullOrEmpty(security.LocalCertificatePassword) ||
            !string.IsNullOrEmpty(security.TrustAnchorPath) ||
            !string.IsNullOrEmpty(security.ExpectedPeerName) ||
            security.RequirePeerCertificate || security.CheckCertificateRevocation)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "certificate-configuration-invalid";
            result.Message = "Certificate settings require a TLS transport factory.";
            return result;
        }

        if (transport.Port <= 0 || transport.Port > 65535 ||
            (!transport.EnableIpv4 && !transport.EnableIpv6) ||
            transport.ReceiveBufferSize < DiameterProtocolConstants.MessageHeaderSize ||
            transport.SendBufferSize < DiameterProtocolConstants.MessageHeaderSize ||
            transport.SctpStreamCount != 1)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "transport-configuration-invalid";
            result.Message = "The TCP transport configuration is invalid.";
            return result;
        }

        if ((!transport.EnableIpv4 || !Socket.OSSupportsIPv4) &&
            (!transport.EnableIpv6 || !Socket.OSSupportsIPv6))
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "address-family-unsupported";
            result.Message = "A configured IP address family is not supported by this host.";
        }

        return result;
    }

    /// <summary>Creates a passive IPv4, IPv6, or dual-stack TCP listener.</summary>
    /// <param name="transport">The listener transport configuration.</param>
    /// <param name="security">The listener security configuration.</param>
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
            return CreateListenerFailure(support.Status, support.Code, support.Message);
        }

        if (transport == null || security == null || !transport.EnableListener)
        {
            return CreateListenerFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "listener-configuration-invalid",
                "An enabled TCP listener configuration is required.");
        }

        long deadline = Environment.TickCount64 + ListenerResolutionTimeoutMilliseconds;
        TcpAddressResolution local = ResolveTcpListenerAddresses(transport, deadline);
        if (local.Status != DiameterTransportStatus.Success)
        {
            return CreateListenerFailure(local.Status, local.Code, local.Message);
        }

        List<Socket> sockets = new List<Socket>();
        int index = 0;
        while (index < local.Addresses.Count)
        {
            IPAddress address = local.Addresses[index];
            Socket socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                socket.DualMode = address.Equals(IPAddress.IPv6Any) &&
                    transport.EnableIpv4;
            }

            socket.ReceiveBufferSize = transport.ReceiveBufferSize;
            socket.SendBufferSize = transport.SendBufferSize;
            socket.Bind(new IPEndPoint(address, transport.Port));
            socket.Listen(transport.ListenBacklog);
            sockets.Add(socket);
            index = index + 1;
        }

        DiameterListenerMetadata metadata = CreateTcpListenerMetadata(
            sockets[0],
            transport,
            security);
        TcpSocketDiameterListener listener = new TcpSocketDiameterListener(
            sockets,
            transport,
            security,
            metadata);
        DiameterListenerResult result = new DiameterListenerResult();
        result.Listener = listener;
        result.Metadata = TcpSocketDiameterListener.CloneListenerMetadata(metadata);
        return result;
    }

    /// <summary>Creates an active Diameter TCP connection.</summary>
    /// <param name="endpoint">The resolved or named remote endpoint.</param>
    /// <param name="transport">The local TCP transport configuration.</param>
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
            return CreateConnectionFailure(support.Status, support.Code, support.Message);
        }

        if (endpoint == null || transport == null || security == null ||
            timeoutMilliseconds <= 0)
        {
            return CreateConnectionFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "connection-input-invalid",
                "A remote endpoint and positive connection timeout are required.");
        }

        if (!transport.EnableActiveConnections || endpoint.Port <= 0 ||
            endpoint.Port > 65535 || endpoint.Transport != DiameterTransportKind.Tcp)
        {
            return CreateConnectionFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "endpoint-configuration-invalid",
                "The active TCP endpoint configuration is invalid.");
        }

        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        TcpAddressResolution remote = ResolveTcpAddresses(
            endpoint.Address,
            endpoint.Host,
            transport,
            deadline);
        if (remote.Status != DiameterTransportStatus.Success)
        {
            return CreateConnectionFailure(remote.Status, remote.Code, remote.Message);
        }

        TcpAddressResolution local = ResolveLocalTcpAddresses(transport, deadline);
        if (local.Status != DiameterTransportStatus.Success)
        {
            return CreateConnectionFailure(local.Status, local.Code, local.Message);
        }

        DiameterConnectionResult lastFailure = CreateConnectionFailure(
            DiameterTransportStatus.Failed,
            "connect-failed",
            "No remote TCP address accepted the connection.");
        int index = 0;
        while (index < remote.Addresses.Count)
        {
            IPAddress remoteAddress = remote.Addresses[index];
            int remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
            if (remaining <= 0)
            {
                return CreateConnectionFailure(
                    DiameterTransportStatus.TimedOut,
                    "connect-timeout",
                    "The TCP connection did not complete before its timeout.");
            }

            Socket socket = new Socket(
                remoteAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);
            ConfigureTcpSocket(socket, transport);
            IPAddress? localAddress = FindTcpAddressFamily(
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
                return CreateConnectionFailure(
                    DiameterTransportStatus.TimedOut,
                    "connect-timeout",
                    "The TCP connection did not complete before its timeout.");
            }

            if (connectTask.IsCompletedSuccessfully && socket.Connected)
            {
                DiameterConnectionMetadata metadata = CreateTcpConnectionMetadata(
                    socket,
                    endpoint,
                    remoteAddress,
                    security);
                TcpSocketDiameterConnection connection =
                    new TcpSocketDiameterConnection(socket, metadata);
                DiameterConnectionResult result = new DiameterConnectionResult();
                result.Connection = connection;
                result.Metadata = QueuedDiameterConnection.CloneConnectionMetadata(metadata);
                return result;
            }

            string failureMessage = ReadTaskFailureMessage(
                connectTask,
                "The TCP connection attempt failed.");
            socket.Close();
            lastFailure = CreateConnectionFailure(
                DiameterTransportStatus.Failed,
                "connect-failed",
                failureMessage);
            index = index + 1;
        }

        return lastFailure;
    }

    internal TcpAddressResolution ResolveLocalTcpAddresses(
        DiameterTransportConfiguration transport,
        long deadline)
    {
        if (string.IsNullOrEmpty(transport.Host))
        {
            return new TcpAddressResolution();
        }

        return ResolveTcpAddresses(null, transport.Host, transport, deadline);
    }

    internal TcpAddressResolution ResolveTcpListenerAddresses(
        DiameterTransportConfiguration transport,
        long deadline)
    {
        if (!string.IsNullOrEmpty(transport.Host))
        {
            return ResolveTcpAddresses(null, transport.Host, transport, deadline);
        }

        TcpAddressResolution result = new TcpAddressResolution();
        if (transport.EnableIpv6 && Socket.OSSupportsIPv6)
        {
            result.Addresses.Add(IPAddress.IPv6Any);
            if (transport.EnableIpv4)
            {
                return result;
            }
        }

        if (transport.EnableIpv4 && Socket.OSSupportsIPv4)
        {
            result.Addresses.Add(IPAddress.Any);
        }

        if (result.Addresses.Count == 0)
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "address-family-unsupported";
            result.Message = "No enabled listener address family is supported by this host.";
        }

        return result;
    }

    internal TcpAddressResolution ResolveTcpAddresses(
        IPAddress? address,
        string host,
        DiameterTransportConfiguration transport,
        long deadline)
    {
        TcpAddressResolution result = new TcpAddressResolution();
        if (address != null)
        {
            AddSupportedTcpAddress(result.Addresses, address, transport);
        }
        else if (!string.IsNullOrWhiteSpace(host))
        {
            Task<IPAddress[]> resolution = Dns.GetHostAddressesAsync(host);
            int remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
            bool completed = remaining > 0 &&
                SpinWait.SpinUntil(() => resolution.IsCompleted, remaining);
            if (!completed)
            {
                result.Status = DiameterTransportStatus.TimedOut;
                result.Code = "address-resolution-timeout";
                result.Message = "Endpoint address resolution timed out.";
                return result;
            }

            if (!resolution.IsCompletedSuccessfully)
            {
                result.Status = DiameterTransportStatus.Failed;
                result.Code = "address-resolution-failed";
                result.Message = ReadTaskFailureMessage(
                    resolution,
                    "Endpoint address resolution failed.");
                return result;
            }

            IPAddress[] addresses = resolution.Result;
            int index = 0;
            while (index < addresses.Length)
            {
                AddSupportedTcpAddress(result.Addresses, addresses[index], transport);
                index = index + 1;
            }
        }

        if (result.Addresses.Count == 0)
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "address-family-unsupported";
            result.Message = "The endpoint has no enabled, supported IP address.";
        }

        return result;
    }

    internal void AddSupportedTcpAddress(
        List<IPAddress> addresses,
        IPAddress address,
        DiameterTransportConfiguration transport)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork &&
            transport.EnableIpv4 && Socket.OSSupportsIPv4)
        {
            addresses.Add(address);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6 &&
            transport.EnableIpv6 && Socket.OSSupportsIPv6)
        {
            addresses.Add(address);
        }
    }

    internal IPAddress? FindTcpAddressFamily(
        List<IPAddress> addresses,
        AddressFamily family)
    {
        int index = 0;
        while (index < addresses.Count)
        {
            if (addresses[index].AddressFamily == family)
            {
                return addresses[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void ConfigureTcpSocket(
        Socket socket,
        DiameterTransportConfiguration transport)
    {
        socket.NoDelay = transport.NoDelay;
        socket.ReceiveBufferSize = transport.ReceiveBufferSize;
        socket.SendBufferSize = transport.SendBufferSize;
        socket.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.KeepAlive,
            transport.KeepAlive);
    }

    internal DiameterConnectionMetadata CreateTcpConnectionMetadata(
        Socket socket,
        DiameterEndpoint requestedEndpoint,
        IPAddress remoteAddress,
        DiameterSecurityConfiguration security)
    {
        EndPoint? localValue = socket.LocalEndPoint;
        EndPoint? remoteValue = socket.RemoteEndPoint;
        if (localValue == null || remoteValue == null)
        {
            throw new DiameterTransportException(
                "A connected TCP socket requires local and remote endpoints.");
        }

        IPEndPoint local = (IPEndPoint)localValue;
        IPEndPoint remote = (IPEndPoint)remoteValue;
        DiameterConnectionMetadata metadata = new DiameterConnectionMetadata();
        metadata.ConnectionId = $"{local.Address}:{local.Port}-{remote.Address}:{remote.Port}";
        metadata.LocalEndpoint.Address = local.Address;
        metadata.LocalEndpoint.Host = local.Address.ToString();
        metadata.LocalEndpoint.Port = local.Port;
        metadata.LocalEndpoint.Transport = DiameterTransportKind.Tcp;
        metadata.LocalEndpoint.Security = security.Mode;
        metadata.RemoteEndpoint.Identity.Value = requestedEndpoint.Identity.Value;
        metadata.RemoteEndpoint.Host = requestedEndpoint.Host;
        metadata.RemoteEndpoint.Address = remoteAddress;
        metadata.RemoteEndpoint.Port = requestedEndpoint.Port;
        metadata.RemoteEndpoint.Transport = DiameterTransportKind.Tcp;
        metadata.RemoteEndpoint.Security = security.Mode;
        metadata.Direction = DiameterConnectionDirection.Outbound;
        metadata.Transport.Kind = DiameterTransportKind.Tcp;
        metadata.Transport.SupportsActiveConnections = true;
        metadata.Transport.SupportsIpv4 = Socket.OSSupportsIPv4;
        metadata.Transport.SupportsIpv6 = Socket.OSSupportsIPv6;
        metadata.Security.Mode = security.Mode;
        if (security.Mode == DiameterSecurityMode.Ipsec)
        {
            metadata.Security.Authenticated = true;
            metadata.Security.Encrypted = true;
            metadata.Security.Protocol = "IPsec";
        }

        metadata.EstablishedAt = DateTimeOffset.UtcNow;
        metadata.Open = true;
        return metadata;
    }

    internal DiameterConnectionResult CreateConnectionFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Metadata.Transport.Kind = DiameterTransportKind.Tcp;
        return result;
    }

    internal DiameterListenerMetadata CreateTcpListenerMetadata(
        Socket socket,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security)
    {
        EndPoint? endpointValue = socket.LocalEndPoint;
        if (endpointValue == null)
        {
            throw new DiameterTransportException(
                "A bound TCP listener requires a local endpoint.");
        }

        IPEndPoint endpoint = (IPEndPoint)endpointValue;
        DiameterListenerMetadata metadata = new DiameterListenerMetadata();
        metadata.LocalEndpoint.Host = transport.Host;
        metadata.LocalEndpoint.Address = endpoint.Address;
        metadata.LocalEndpoint.Port = endpoint.Port;
        metadata.LocalEndpoint.Transport = DiameterTransportKind.Tcp;
        metadata.LocalEndpoint.Security = security.Mode;
        metadata.Transport.Kind = DiameterTransportKind.Tcp;
        metadata.Transport.SupportsActiveConnections = true;
        metadata.Transport.SupportsListeners = true;
        metadata.Transport.SupportsIpv4 = Socket.OSSupportsIPv4;
        metadata.Transport.SupportsIpv6 = Socket.OSSupportsIPv6;
        metadata.SecurityMode = security.Mode;
        metadata.Open = true;
        return metadata;
    }

    internal DiameterListenerResult CreateListenerFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterListenerResult result = new DiameterListenerResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Metadata.Transport.Kind = DiameterTransportKind.Tcp;
        return result;
    }

    internal string ReadTaskFailureMessage(Task task, string fallback)
    {
        AggregateException? aggregate = task.Exception;
        if (aggregate == null)
        {
            return fallback;
        }

        Exception failure = aggregate.GetBaseException();
        if (string.IsNullOrEmpty(failure.Message))
        {
            return fallback;
        }

        return failure.Message;
    }
}
