using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ftp;

internal class FtpPassiveListener
{
    internal IPAddress ControlPeerAddress;
    internal IPAddress ControlLocalAddress;
    internal IPAddress BindAddress;
    internal IPAddress AdvertisedAddress;
    internal FtpAddressFamily ControlFamily;
    internal FtpPassivePortPool PortPool;
    internal bool DualStack;
    internal bool RequireDataPeerMatch;
    internal FtpSocketOptions SocketOptions;
    internal bool ExtendedPassiveOnly;
    internal Socket? Listener;
    internal int ReservedPort;
    internal bool Extended;
    internal bool Open;
    internal FtpEndpoint BindEndpoint;
    internal FtpEndpoint AdvertisedEndpoint;

    internal FtpPassiveListener(
        IPAddress? controlPeerAddress,
        IPAddress? controlLocalAddress,
        FtpListenerOptions? listenerOptions,
        FtpDataConnectionOptions? dataOptions,
        FtpPassivePortPool? portPool)
    {
        ValidateConfiguration(
            controlPeerAddress, controlLocalAddress, listenerOptions, dataOptions, portPool);
        if (controlPeerAddress == null || controlLocalAddress == null ||
            listenerOptions == null || dataOptions == null || portPool == null)
        {
            throw new FtpConfigurationException(
                "Complete server passive-listener configuration is required.", null);
        }

        ControlPeerAddress = FtpPeerPolicy.NormalizeAddress(controlPeerAddress);
        ControlLocalAddress = FtpPeerPolicy.NormalizeAddress(controlLocalAddress);
        ControlFamily = FtpPeerPolicy.GetAddressFamily(controlLocalAddress);
        BindAddress = ControlLocalAddress;
        AdvertisedAddress = ResolveAdvertisedAddress(
            ControlLocalAddress, listenerOptions.PassiveAdvertisedAddress, ControlFamily);
        PortPool = portPool;
        DualStack = listenerOptions.DualStack;
        RequireDataPeerMatch = dataOptions.RequireDataPeerMatch;
        SocketOptions = FtpSocketSettings.CreateSnapshot(dataOptions.Socket);
        ExtendedPassiveOnly = false;
        Listener = null;
        ReservedPort = 0;
        Extended = false;
        Open = false;
        BindEndpoint = new FtpEndpoint();
        AdvertisedEndpoint = new FtpEndpoint();
    }

    internal FtpReply OpenPassiveListener(
        bool extended,
        FtpAddressFamily requestedFamily)
    {
        CloseListener();
        if (!extended && ExtendedPassiveOnly)
        {
            throw new FtpProtocolException("EPSV ALL prohibits PASV listener setup.", null);
        }

        if (!extended)
        {
            if (ControlFamily != FtpAddressFamily.IPv4 ||
                (requestedFamily != FtpAddressFamily.Unspecified &&
                    requestedFamily != FtpAddressFamily.IPv4))
            {
                throw new FtpProtocolException("PASV requires an IPv4 control connection.", null);
            }
        }
        else
        {
            if (requestedFamily != FtpAddressFamily.Unspecified &&
                requestedFamily != FtpAddressFamily.IPv4 &&
                requestedFamily != FtpAddressFamily.IPv6)
            {
                throw new FtpProtocolException("The EPSV network protocol is invalid.", null);
            }

            if (requestedFamily != FtpAddressFamily.Unspecified &&
                requestedFamily != ControlFamily)
            {
                int supportedProtocol = FtpEndpointCodec.FormatNetworkProtocol(
                    ControlFamily, "EPSV");
                return FtpEndpointCodec.CreateNetworkProtocolReply(
                    new int[] { supportedProtocol });
            }
        }

        int attempts = 0;
        while (attempts < PortPool.Capacity)
        {
            int port = PortPool.ReserveNextPort();
            if (port == 0)
            {
                throw new FtpConnectionException(
                    "The passive listener port pool is exhausted.", null);
            }

            if (TryOpenReservedPort(port, extended))
            {
                if (extended)
                {
                    return FtpEndpointCodec.CreateExtendedPassiveReply(port, '|');
                }

                return FtpEndpointCodec.CreatePassiveReply(AdvertisedEndpoint);
            }

            attempts++;
        }

        throw new FtpConnectionException(
            "No configured passive listener port could be opened.", null);
    }

    internal bool TryOpenReservedPort(int port, bool extended)
    {
        FtpPassiveBindState state = new FtpPassiveBindState();
        FtpFaultIsolationResult bindResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            Socket socket = new Socket(
                BindAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            state.Listener = socket;
            socket.ExclusiveAddressUse = true;
            if (socket.AddressFamily == AddressFamily.InterNetworkV6)
            {
                socket.DualMode = DualStack;
            }

            socket.Bind(new IPEndPoint(BindAddress, port));
            socket.Listen(1);
            return null;
        });
        FtpResourceLifetime lifetime = new FtpResourceLifetime(state.Listener);
        if (!bindResult.Succeeded)
        {
            FtpFaultIsolationResult cleanup = lifetime.DisposeResource();
            PortPool.ReleasePort(port);
            if (!cleanup.Succeeded)
            {
                throw new FtpConnectionException(
                    "A failed passive listener could not be closed.", cleanup.Failure);
            }

            return false;
        }

        if (state.Listener == null)
        {
            PortPool.ReleasePort(port);
            throw new FtpConnectionException(
                "The passive bind completed without a listener socket.", null);
        }

        Socket socket = state.Listener;
        EndPoint? localEndpointValue = socket.LocalEndPoint;
        if (localEndpointValue == null)
        {
            FtpFaultIsolationResult cleanup = lifetime.DisposeResource();
            PortPool.ReleasePort(port);
            if (!cleanup.Succeeded)
            {
                throw new FtpConnectionException(
                    "An incomplete passive listener could not be closed.", cleanup.Failure);
            }

            return false;
        }

        IPEndPoint localEndpoint = (IPEndPoint)localEndpointValue;
        lifetime.ReleaseResource();
        Listener = socket;
        ReservedPort = port;
        Extended = extended;
        Open = true;
        BindEndpoint.Host = localEndpoint.Address.ToString();
        BindEndpoint.Port = localEndpoint.Port;
        BindEndpoint.AddressFamily = ControlFamily;
        AdvertisedEndpoint.Host = AdvertisedAddress.ToString();
        AdvertisedEndpoint.Port = localEndpoint.Port;
        AdvertisedEndpoint.AddressFamily = ControlFamily;
        return true;
    }

    internal void EnableExtendedPassiveOnly()
    {
        CloseListener();
        ExtendedPassiveOnly = true;
    }

    internal void ResetSession()
    {
        CloseListener();
        ExtendedPassiveOnly = false;
    }

    internal void CloseListener()
    {
        Socket? socket = Listener;
        int port = ReservedPort;
        Listener = null;
        ReservedPort = 0;
        Extended = false;
        Open = false;
        BindEndpoint = new FtpEndpoint();
        AdvertisedEndpoint = new FtpEndpoint();

        FtpResourceLifetime lifetime = new FtpResourceLifetime(socket);
        FtpFaultIsolationResult cleanup = lifetime.DisposeResource();
        if (port != 0)
        {
            PortPool.ReleasePort(port);
        }

        if (!cleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "The passive data listener could not be closed.", cleanup.Failure);
        }
    }

    internal Socket AcceptConnection(
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        ValidateAcceptRequest(timeoutMilliseconds);
        if (Listener == null)
        {
            throw new FtpConfigurationException("An open passive listener is required.", null);
        }

        Socket listenerSocket = Listener;
        int port = ReservedPort;
        Listener = null;
        ReservedPort = 0;
        Extended = false;
        Open = false;
        BindEndpoint = new FtpEndpoint();
        AdvertisedEndpoint = new FtpEndpoint();
        FtpResourceLifetime listenerLifetime = new FtpResourceLifetime(listenerSocket);
        FtpDeadline deadline = FtpTimeout.StartDeadline(timeoutMilliseconds);
        FtpPassiveAcceptState state = new FtpPassiveAcceptState();
        FtpFaultIsolationResult acceptResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            TransferPump.CheckOperationState(cancellationToken, deadline);
            using (CancellationTokenSource linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                int remainingMilliseconds = FtpTimeout.GetRemainingWaitMilliseconds(deadline);
                if (remainingMilliseconds != Timeout.Infinite)
                {
                    linkedCancellation.CancelAfter(remainingMilliseconds);
                }

                ValueTask<Socket> acceptOperation = listenerSocket.AcceptAsync(
                    linkedCancellation.Token);
                Task<Socket> acceptTask = acceptOperation.AsTask();
                state.Connection = acceptTask.GetAwaiter().GetResult();
            }

            if (state.Connection == null)
            {
                throw new FtpConnectionException(
                    "The passive listener completed without a data connection.", null);
            }

            EndPoint? remoteEndpointValue = state.Connection.RemoteEndPoint;
            if (remoteEndpointValue == null)
            {
                throw new FtpConnectionException(
                    "The passive data connection has no remote endpoint.", null);
            }

            IPEndPoint remoteEndpoint = (IPEndPoint)remoteEndpointValue;
            if (RequireDataPeerMatch &&
                !FtpPeerPolicy.IsSameAddress(remoteEndpoint.Address, ControlPeerAddress))
            {
                throw new FtpConnectionException(
                    "The passive data peer does not match the control peer.", null);
            }

            FtpSocketSettings.ApplyConnectedSocket(state.Connection, SocketOptions);

            TransferPump.CheckOperationState(cancellationToken, deadline);
            return null;
        });

        FtpFaultIsolationResult listenerCleanup = listenerLifetime.DisposeResource();
        PortPool.ReleasePort(port);
        FtpResourceLifetime connectionLifetime = new FtpResourceLifetime(state.Connection);
        bool canceled = FtpCancellation.IsCancellationRequested(cancellationToken);
        bool timedOut = FtpTimeout.HasDeadlineExpired(deadline);
        if (acceptResult.Succeeded && listenerCleanup.Succeeded && !canceled && !timedOut &&
            state.Connection != null)
        {
            connectionLifetime.ReleaseResource();
            return state.Connection;
        }

        FtpFaultIsolationResult connectionCleanup = connectionLifetime.DisposeResource();
        ThrowAcceptFailure(
            acceptResult,
            listenerCleanup,
            connectionCleanup,
            cancellationToken,
            deadline);
        throw new FtpConnectionException("The passive data accept did not reach a terminal result.", null);
    }

    internal void ValidateAcceptRequest(int timeoutMilliseconds)
    {
        if (!Open || Listener == null || ReservedPort == 0)
        {
            throw new FtpConfigurationException("An open passive listener is required.", null);
        }

        if (timeoutMilliseconds < Timeout.Infinite)
        {
            throw new FtpConfigurationException(
                "A passive accept timeout must be non-negative or Timeout.Infinite.", null);
        }
    }

    internal void ThrowAcceptFailure(
        FtpFaultIsolationResult acceptResult,
        FtpFaultIsolationResult listenerCleanup,
        FtpFaultIsolationResult connectionCleanup,
        CancellationToken cancellationToken,
        FtpDeadline deadline)
    {
        if (FtpCancellation.IsCancellationRequested(cancellationToken))
        {
            throw new OperationCanceledException(
                "The passive data accept was canceled.", acceptResult.Failure, cancellationToken);
        }

        if (FtpTimeout.HasDeadlineExpired(deadline))
        {
            throw new TimeoutException("The passive data accept timed out.", acceptResult.Failure);
        }

        if (!acceptResult.Succeeded)
        {
            throw new FtpConnectionException("The passive data accept failed.", acceptResult.Failure);
        }

        if (!listenerCleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "The passive data listener could not be closed.", listenerCleanup.Failure);
        }

        if (!connectionCleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "The rejected passive data connection could not be closed.",
                connectionCleanup.Failure);
        }
    }

    internal static IPAddress ResolveAdvertisedAddress(
        IPAddress controlLocalAddress,
        string? configuredAddress,
        FtpAddressFamily family)
    {
        IPAddress address = configuredAddress == null ? controlLocalAddress :
            FtpActiveConnector.ParseNumericAddress(
                configuredAddress, family, "passive advertised");
        if (FtpPeerPolicy.IsProhibitedAdvertisedAddress(address))
        {
            throw new FtpConfigurationException(
                "The passive advertised address cannot be unspecified or multicast.", null);
        }

        return address;
    }

    internal static void ValidateConfiguration(
        IPAddress? controlPeerAddress,
        IPAddress? controlLocalAddress,
        FtpListenerOptions? listenerOptions,
        FtpDataConnectionOptions? dataOptions,
        FtpPassivePortPool? portPool)
    {
        if (controlPeerAddress == null || controlLocalAddress == null ||
            listenerOptions == null || dataOptions == null || portPool == null)
        {
            throw new FtpConfigurationException(
                "Complete server passive-listener configuration is required.", null);
        }

        FtpConfigurationValidator.ValidateListenerOptions(listenerOptions);
        FtpConfigurationValidator.ValidateDataConnectionOptions(dataOptions);
        FtpAddressFamily localFamily = FtpPeerPolicy.GetAddressFamily(
            controlLocalAddress);
        if (!FtpPeerPolicy.IsAddressFamilyMatch(
            controlPeerAddress, controlLocalAddress))
        {
            throw new FtpConfigurationException(
                "Control peer and local addresses must use the same network protocol.", null);
        }

        if (listenerOptions.Endpoint.AddressFamily != FtpAddressFamily.Unspecified &&
            listenerOptions.Endpoint.AddressFamily != localFamily)
        {
            throw new FtpConfigurationException(
                "The server listener and accepted control connection families must match.", null);
        }

        if (FtpPeerPolicy.IsProhibitedAdvertisedAddress(controlPeerAddress) ||
            FtpPeerPolicy.IsProhibitedAdvertisedAddress(controlLocalAddress))
        {
            throw new FtpConfigurationException(
                "Control connection addresses cannot be unspecified or multicast.", null);
        }

        if (dataOptions.Mode == FtpDataConnectionMode.Active)
        {
            throw new FtpConfigurationException(
                "A passive listener cannot be used with active-only data mode.", null);
        }

        if (portPool.StartPort != dataOptions.PassivePortStart ||
            portPool.EndPort != dataOptions.PassivePortEnd)
        {
            throw new FtpConfigurationException(
                "The passive port pool must match the configured passive range.", null);
        }
    }
}
