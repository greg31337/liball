using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ftp;

internal class FtpActiveConnector
{
    internal IPAddress ControlPeerAddress;
    internal IPAddress ControlLocalAddress;
    internal IPAddress BindAddress;
    internal IPAddress AdvertisedAddress;
    internal FtpAddressFamily AddressFamily;
    internal bool UseExtendedActive;
    internal bool AllowActiveFallback;
    internal bool RequireDataPeerMatch;
    internal FtpSocketOptions SocketOptions;
    internal int FixedPort;
    internal int PortStart;
    internal int PortEnd;

    internal FtpActiveConnector(
        IPAddress? controlPeerAddress,
        IPAddress? controlLocalAddress,
        FtpDataConnectionOptions? options)
    {
        ValidateConfiguration(controlPeerAddress, controlLocalAddress, options);
        if (controlPeerAddress == null || controlLocalAddress == null || options == null)
        {
            throw new FtpConfigurationException(
                "Control peer, local address, and active options are required.", null);
        }

        ControlPeerAddress = FtpPeerPolicy.NormalizeAddress(controlPeerAddress);
        ControlLocalAddress = FtpPeerPolicy.NormalizeAddress(controlLocalAddress);
        AddressFamily = FtpPeerPolicy.GetAddressFamily(controlLocalAddress);
        BindAddress = ResolveBindAddress(ControlLocalAddress, options.ActiveBindEndpoint);
        AdvertisedAddress = ResolveAdvertisedAddress(
            ControlLocalAddress, BindAddress, options.ActiveAdvertisedAddress, AddressFamily);
        UseExtendedActive = options.UseExtendedActive;
        AllowActiveFallback = options.AllowActiveFallback;
        RequireDataPeerMatch = options.RequireDataPeerMatch;
        SocketOptions = FtpSocketSettings.CreateSnapshot(options.Socket);
        FixedPort = options.ActiveBindEndpoint == null ? 0 : options.ActiveBindEndpoint.Port;
        PortStart = options.ActivePortStart;
        PortEnd = options.ActivePortEnd;
        if (PortStart != 0 && FixedPort != 0)
        {
            throw new FtpConfigurationException(
                "An active bind endpoint port cannot be combined with an active port range.", null);
        }
    }

    internal FtpActiveListener OpenListener()
    {
        if (PortStart == 0)
        {
            FtpActiveListener? listener = TryOpenListener(FixedPort);
            if (listener != null)
            {
                return listener;
            }

            throw new FtpConnectionException("The active data listener could not be opened.", null);
        }

        int port = PortStart;
        while (port <= PortEnd)
        {
            FtpActiveListener? listener = TryOpenListener(port);
            if (listener != null)
            {
                return listener;
            }

            port++;
        }

        throw new FtpConnectionException(
            "No port in the configured active listener range could be opened.", null);
    }

    internal FtpActiveListener? TryOpenListener(int port)
    {
        Socket socket = new Socket(BindAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        FtpResourceLifetime lifetime = new FtpResourceLifetime(socket);
        FtpFaultIsolationResult bindResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            socket.ExclusiveAddressUse = true;
            socket.Bind(new IPEndPoint(BindAddress, port));
            socket.Listen(1);
            return null;
        });
        if (!bindResult.Succeeded)
        {
            lifetime.DisposeResource();
            return null;
        }

        EndPoint? localEndpointValue = socket.LocalEndPoint;
        if (localEndpointValue == null)
        {
            lifetime.DisposeResource();
            return null;
        }

        IPEndPoint localEndpoint = (IPEndPoint)localEndpointValue;
        lifetime.ReleaseResource();
        FtpActiveListener listener = new FtpActiveListener();
        listener.Listener = socket;
        listener.Open = true;
        listener.BindEndpoint.Host = localEndpoint.Address.ToString();
        listener.BindEndpoint.Port = localEndpoint.Port;
        listener.BindEndpoint.AddressFamily = AddressFamily;
        listener.AdvertisedEndpoint.Host = AdvertisedAddress.ToString();
        listener.AdvertisedEndpoint.Port = localEndpoint.Port;
        listener.AdvertisedEndpoint.AddressFamily = AddressFamily;
        return listener;
    }

    internal FtpCommand CreateExtendedActiveCommand(FtpActiveListener? listener)
    {
        ValidateOpenListener(listener);
        if (!UseExtendedActive)
        {
            throw new FtpConfigurationException(
                "Extended active mode is disabled for this connector.", null);
        }

        if (listener == null)
        {
            throw new FtpConfigurationException("An open active listener is required.", null);
        }

        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Eprt;
        command.Verb = FtpCommandNames.Eprt;
        command.Argument = FtpEndpointCodec.FormatExtendedPortDefault(
            listener.AdvertisedEndpoint);
        return command;
    }

    internal FtpCommand CreateActiveCommand(FtpActiveListener? listener)
    {
        ValidateOpenListener(listener);
        if (UseExtendedActive)
        {
            throw new FtpConfigurationException(
                "Direct PORT setup requires extended active mode to be disabled.", null);
        }

        return CreatePortCommand(listener);
    }

    internal FtpCommand CreateActiveFallbackCommand(
        FtpActiveListener? listener,
        FtpReply? extendedActiveReply)
    {
        ValidateOpenListener(listener);
        if (!UseExtendedActive || !AllowActiveFallback)
        {
            throw new FtpProtocolException("PORT fallback is not enabled for this connector.", null);
        }

        if (extendedActiveReply == null)
        {
            throw new FtpProtocolException("An EPRT failure reply is required before PORT fallback.", null);
        }

        int code = extendedActiveReply.Code;
        bool unsupported = code == FtpReplyCodes.SyntaxError ||
            code == FtpReplyCodes.SyntaxErrorInParameters ||
            code == FtpReplyCodes.CommandNotImplemented ||
            code == FtpReplyCodes.CommandParameterNotImplemented ||
            code == FtpReplyCodes.NetworkProtocolNotSupported;
        if (!unsupported)
        {
            throw new FtpProtocolException(
                "PORT fallback requires a standards-defined EPRT unsupported reply.", null);
        }

        return CreatePortCommand(listener);
    }

    internal FtpCommand CreatePortCommand(FtpActiveListener? listener)
    {
        if (AddressFamily != FtpAddressFamily.IPv4)
        {
            throw new FtpProtocolException("PORT is available only for an IPv4 active listener.", null);
        }

        if (listener == null)
        {
            throw new FtpConfigurationException("An open active listener is required.", null);
        }

        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Port;
        command.Verb = FtpCommandNames.Port;
        command.Argument = FtpEndpointCodec.FormatPortArgument(listener.AdvertisedEndpoint);
        return command;
    }

    internal Socket AcceptConnection(
        FtpActiveListener? listener,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        ValidateAcceptRequest(listener, timeoutMilliseconds);
        if (listener == null || listener.Listener == null)
        {
            throw new FtpConfigurationException("An open active listener is required.", null);
        }

        Socket listenerSocket = listener.Listener;
        listener.Listener = null;
        listener.Open = false;
        FtpResourceLifetime listenerLifetime = new FtpResourceLifetime(listenerSocket);
        FtpDeadline deadline = FtpTimeout.StartDeadline(timeoutMilliseconds);
        FtpActiveAcceptState state = new FtpActiveAcceptState();
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
                    "The active listener completed without a data connection.", null);
            }

            EndPoint? remoteEndpointValue = state.Connection.RemoteEndPoint;
            if (remoteEndpointValue == null)
            {
                throw new FtpConnectionException(
                    "The active data connection has no remote endpoint.", null);
            }

            IPEndPoint remoteEndpoint = (IPEndPoint)remoteEndpointValue;
            if (RequireDataPeerMatch &&
                !FtpPeerPolicy.IsSameAddress(remoteEndpoint.Address, ControlPeerAddress))
            {
                throw new FtpConnectionException(
                    "The active data peer does not match the control peer.", null);
            }

            FtpSocketSettings.ApplyConnectedSocket(state.Connection, SocketOptions);

            TransferPump.CheckOperationState(cancellationToken, deadline);
            return null;
        });

        FtpFaultIsolationResult listenerCleanup = listenerLifetime.DisposeResource();
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
        throw new FtpConnectionException("The active data accept did not reach a terminal result.", null);
    }

    internal void ValidateOpenListener(FtpActiveListener? listener)
    {
        if (listener == null || !listener.Open || listener.Listener == null ||
            listener.AdvertisedEndpoint.Port < 1 || listener.AdvertisedEndpoint.Port > 65535 ||
            listener.AdvertisedEndpoint.AddressFamily != AddressFamily)
        {
            throw new FtpConfigurationException("An open matching active listener is required.", null);
        }
    }

    internal void ValidateAcceptRequest(
        FtpActiveListener? listener,
        int timeoutMilliseconds)
    {
        ValidateOpenListener(listener);
        if (timeoutMilliseconds < Timeout.Infinite)
        {
            throw new FtpConfigurationException(
                "An active accept timeout must be non-negative or Timeout.Infinite.", null);
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
                "The active data accept was canceled.", acceptResult.Failure, cancellationToken);
        }

        if (FtpTimeout.HasDeadlineExpired(deadline))
        {
            throw new TimeoutException("The active data accept timed out.", acceptResult.Failure);
        }

        if (!acceptResult.Succeeded)
        {
            throw new FtpConnectionException("The active data accept failed.", acceptResult.Failure);
        }

        if (!listenerCleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "The active data listener could not be closed.", listenerCleanup.Failure);
        }

        if (!connectionCleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "The rejected active data connection could not be closed.",
                connectionCleanup.Failure);
        }
    }

    internal static IPAddress ResolveBindAddress(
        IPAddress controlLocalAddress,
        FtpEndpoint? endpoint)
    {
        if (endpoint == null)
        {
            return controlLocalAddress;
        }

        FtpAddressFamily controlFamily = FtpPeerPolicy.GetAddressFamily(
            controlLocalAddress);
        if (endpoint.AddressFamily != FtpAddressFamily.Unspecified &&
            endpoint.AddressFamily != controlFamily)
        {
            throw new FtpConfigurationException(
                "The active bind endpoint must match the control connection address family.", null);
        }

        if (string.IsNullOrEmpty(endpoint.Host))
        {
            return controlFamily == FtpAddressFamily.IPv4 ? IPAddress.Any : IPAddress.IPv6Any;
        }

        IPAddress bindAddress = ParseNumericAddress(endpoint.Host, controlFamily, "active bind");
        if (FtpPeerPolicy.IsProhibitedAdvertisedAddress(bindAddress) &&
            !FtpPeerPolicy.IsWildcardAddress(bindAddress))
        {
            throw new FtpConfigurationException(
                "The active bind address cannot be multicast or broadcast.", null);
        }

        return bindAddress;
    }

    internal static IPAddress ResolveAdvertisedAddress(
        IPAddress controlLocalAddress,
        IPAddress bindAddress,
        string? configuredAddress,
        FtpAddressFamily family)
    {
        IPAddress address;
        if (configuredAddress != null)
        {
            address = ParseNumericAddress(configuredAddress, family, "active advertised");
        }
        else if (FtpPeerPolicy.IsWildcardAddress(bindAddress))
        {
            address = controlLocalAddress;
        }
        else
        {
            address = bindAddress;
        }

        if (FtpPeerPolicy.IsProhibitedAdvertisedAddress(address))
        {
            throw new FtpConfigurationException(
                "The active advertised address cannot be unspecified or multicast.", null);
        }

        return address;
    }

    internal static IPAddress ParseNumericAddress(
        string text,
        FtpAddressFamily family,
        string valueName)
    {
        UriHostNameType expectedHostType = family == FtpAddressFamily.IPv4 ?
            UriHostNameType.IPv4 : UriHostNameType.IPv6;
        if (Uri.CheckHostName(text) != expectedHostType)
        {
            throw new FtpConfigurationException(
                $"The {valueName} address must be numeric and match the control protocol.", null);
        }

        FtpEndpointCodec.ValidateNumericAddress(text, family, valueName);
        if (family == FtpAddressFamily.IPv4)
        {
            int[] values = FtpEndpointCodec.ParseIpv4Address(text, valueName);
            return new IPAddress(new byte[]
            {
                (byte)values[0],
                (byte)values[1],
                (byte)values[2],
                (byte)values[3]
            });
        }

        IPAddress address = FtpPeerPolicy.NormalizeAddress(IPAddress.Parse(text));
        if (FtpPeerPolicy.GetAddressFamily(address) != family)
        {
            throw new FtpConfigurationException(
                $"The {valueName} address must match the selected network protocol.", null);
        }

        return address;
    }

    internal static void ValidateConfiguration(
        IPAddress? controlPeerAddress,
        IPAddress? controlLocalAddress,
        FtpDataConnectionOptions? options)
    {
        if (controlPeerAddress == null || controlLocalAddress == null || options == null)
        {
            throw new FtpConfigurationException(
                "Control peer, local address, and active options are required.", null);
        }

        FtpConfigurationValidator.ValidateDataConnectionOptions(options);
        FtpAddressFamily localFamily = FtpPeerPolicy.GetAddressFamily(
            controlLocalAddress);
        if (!FtpPeerPolicy.IsAddressFamilyMatch(
            controlPeerAddress, controlLocalAddress))
        {
            throw new FtpConfigurationException(
                "Control peer and local addresses must use the same network protocol.", null);
        }

        if (FtpPeerPolicy.IsProhibitedAdvertisedAddress(controlPeerAddress) ||
            FtpPeerPolicy.IsProhibitedAdvertisedAddress(controlLocalAddress))
        {
            throw new FtpConfigurationException(
                "Control connection addresses cannot be unspecified or multicast.", null);
        }

        if (options.Mode == FtpDataConnectionMode.Passive)
        {
            throw new FtpConfigurationException(
                "An active connector cannot be used with passive-only data mode.", null);
        }

        if (localFamily == FtpAddressFamily.IPv6 && !options.UseExtendedActive)
        {
            throw new FtpConfigurationException("An IPv6 active connection requires EPRT.", null);
        }
    }
}
