using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ftp;

internal class FtpControlConnector
{
    internal FtpClientOptions Options;

    internal FtpControlConnector(FtpClientOptions options)
    {
        Options = options;
    }

    internal FtpControlConnection OpenConnection()
    {
        FtpControlConnection connection = OpenTransportConnection();
        ReadGreeting(connection);
        return connection;
    }

    internal FtpControlConnection OpenTransportConnection()
    {
        FtpDeadline connectionDeadline = FtpTimeout.StartDeadline(
            Options.Timeouts.ConnectTimeoutMilliseconds);
        IPAddress[] addresses = ResolveAddresses(connectionDeadline);
        IPAddress? configuredBindAddress = ResolveConfiguredBindAddress();
        Socket socket = OpenAddressConnection(
            addresses, configuredBindAddress, connectionDeadline);
        return CreateConnection(socket);
    }

    internal void ReadGreeting(FtpControlConnection connection)
    {
        FtpDeadline greetingDeadline = FtpTimeout.StartDeadline(
            Options.Timeouts.ControlReadTimeoutMilliseconds);
        FtpFaultIsolationResult greetingResult =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return ReadGreetingSequence(connection, greetingDeadline);
            });
        if (!greetingResult.Succeeded)
        {
            FtpFaultIsolationResult cleanup = connection.Close();
            ThrowGreetingFailure(greetingResult, cleanup, greetingDeadline);
        }

        FtpGreetingSequence? sequence = greetingResult.Value as FtpGreetingSequence;
        if (sequence == null)
        {
            connection.Close();
            throw new FtpProtocolException(
                "The FTP server greeting completed without a ready sequence.", null);
        }

        connection.InitialGreeting = sequence.InitialReply;
        connection.Greeting = sequence.ReadyReply;
        connection.DelayedGreeting = sequence.Delayed;
    }

    internal IPAddress[] ResolveAddresses(FtpDeadline deadline)
    {
        CancellationToken cancellationToken = Options.Timeouts.CancellationToken;
        TransferPump.CheckOperationState(cancellationToken, deadline);
        FtpFaultIsolationResult resolution = FtpFaultIsolationRunner.RunOperation(() =>
        {
            using (CancellationTokenSource linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                int remainingMilliseconds =
                    FtpTimeout.GetRemainingWaitMilliseconds(deadline);
                linkedCancellation.CancelAfter(remainingMilliseconds);
                Task<IPAddress[]> resolveTask = Dns.GetHostAddressesAsync(
                    Options.Endpoint.Host, linkedCancellation.Token);
                return resolveTask.GetAwaiter().GetResult();
            }
        });
        if (!resolution.Succeeded)
        {
            ThrowResolutionFailure(resolution, deadline);
        }

        IPAddress[]? addresses = resolution.Value as IPAddress[];
        if (addresses == null || addresses.Length == 0)
        {
            throw new FtpConnectionException(
                "DNS resolution returned no addresses for the FTP endpoint.", null);
        }

        return addresses;
    }

    internal IPAddress? ResolveConfiguredBindAddress()
    {
        FtpEndpoint? bindEndpoint = Options.LocalBindEndpoint;
        if (bindEndpoint == null || string.IsNullOrEmpty(bindEndpoint.Host))
        {
            return null;
        }

        FtpFaultIsolationResult parseResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return IPAddress.Parse(bindEndpoint.Host);
        });
        IPAddress? address = parseResult.Value as IPAddress;
        if (!parseResult.Succeeded || address == null)
        {
            throw new FtpConfigurationException(
                "A control-channel local bind host must be a numeric IP address.",
                parseResult.Failure);
        }

        if (!MatchesConfiguredFamily(address, bindEndpoint.AddressFamily))
        {
            throw new FtpConfigurationException(
                "The control-channel local bind address does not match its configured family.",
                null);
        }

        return address;
    }

    internal Socket OpenAddressConnection(
        IPAddress[] addresses,
        IPAddress? configuredBindAddress,
        FtpDeadline deadline)
    {
        CancellationToken cancellationToken = Options.Timeouts.CancellationToken;
        System.Exception? lastFailure = null;
        bool attempted = false;
        int index = 0;
        while (index < addresses.Length)
        {
            IPAddress address = addresses[index];
            if (CanAttemptAddress(address, configuredBindAddress))
            {
                attempted = true;
                TransferPump.CheckOperationState(cancellationToken, deadline);
                IPAddress? bindAddress = GetAttemptBindAddress(
                    address, configuredBindAddress);
                int bindPort = Options.LocalBindEndpoint == null ?
                    0 : Options.LocalBindEndpoint.Port;
                int remainingMilliseconds =
                    FtpTimeout.GetRemainingWaitMilliseconds(deadline);
                FtpFaultIsolationResult attempt =
                    FtpFaultIsolationRunner.RunOperation(() =>
                    {
                        return FtpSocketOperation.ConnectBoundConfigured(
                            address,
                            Options.Endpoint.Port,
                            bindAddress,
                            bindPort,
                            remainingMilliseconds,
                            cancellationToken,
                            Options.ControlSocket);
                    });
                if (attempt.Succeeded)
                {
                    Socket? socket = attempt.Value as Socket;
                    if (socket != null)
                    {
                        return socket;
                    }
                }

                lastFailure = attempt.Failure;
                if (FtpCancellation.IsCancellationRequested(cancellationToken))
                {
                    throw new OperationCanceledException(
                        "The FTP control connection was canceled.",
                        lastFailure,
                        cancellationToken);
                }

                if (FtpTimeout.HasDeadlineExpired(deadline))
                {
                    throw new TimeoutException(
                        "The FTP control connection timed out.", lastFailure);
                }
            }

            index++;
        }

        if (!attempted)
        {
            throw new FtpConnectionException(
                "No resolved FTP endpoint address matches the requested address family or local bind endpoint.",
                null);
        }

        throw new FtpConnectionException(
            "Every resolved FTP endpoint address refused or failed the control connection.",
            lastFailure);
    }

    internal bool CanAttemptAddress(
        IPAddress address,
        IPAddress? configuredBindAddress)
    {
        if (!MatchesConfiguredFamily(address, Options.Endpoint.AddressFamily))
        {
            return false;
        }

        FtpEndpoint? bindEndpoint = Options.LocalBindEndpoint;
        if (bindEndpoint != null &&
            !MatchesConfiguredFamily(address, bindEndpoint.AddressFamily))
        {
            return false;
        }

        return configuredBindAddress == null ||
            configuredBindAddress.AddressFamily == address.AddressFamily;
    }

    internal IPAddress? GetAttemptBindAddress(
        IPAddress remoteAddress,
        IPAddress? configuredBindAddress)
    {
        if (Options.LocalBindEndpoint == null)
        {
            return null;
        }
        if (configuredBindAddress != null)
        {
            return configuredBindAddress;
        }
        if (remoteAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            return IPAddress.Any;
        }

        return IPAddress.IPv6Any;
    }

    internal bool MatchesConfiguredFamily(
        IPAddress address,
        FtpAddressFamily addressFamily)
    {
        if (addressFamily == FtpAddressFamily.Unspecified)
        {
            return address.AddressFamily == AddressFamily.InterNetwork ||
                address.AddressFamily == AddressFamily.InterNetworkV6;
        }
        if (addressFamily == FtpAddressFamily.IPv4)
        {
            return address.AddressFamily == AddressFamily.InterNetwork;
        }

        return address.AddressFamily == AddressFamily.InterNetworkV6;
    }

    internal FtpControlConnection CreateConnection(Socket socket)
    {
        FtpControlConnection connection = new FtpControlConnection();
        connection.Socket = socket;
        FtpFaultIsolationResult streamResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return new NetworkStream(socket, true);
        });
        NetworkStream? stream = streamResult.Value as NetworkStream;
        if (!streamResult.Succeeded || stream == null)
        {
            connection.Close();
            throw new FtpConnectionException(
                "The connected FTP socket could not create a control stream.",
                streamResult.Failure);
        }

        connection.Stream = stream;
        connection.TransportStream = stream;
        connection.LocalEndpoint = CopySocketEndpoint(socket.LocalEndPoint);
        connection.RemoteEndpoint = CopySocketEndpoint(socket.RemoteEndPoint);
        return connection;
    }

    internal FtpEndpoint CopySocketEndpoint(EndPoint? endpoint)
    {
        IPEndPoint? ipEndpoint = endpoint as IPEndPoint;
        if (ipEndpoint == null)
        {
            throw new FtpConnectionException(
                "The FTP control socket did not expose an IP endpoint.", null);
        }

        FtpEndpoint result = new FtpEndpoint();
        result.Host = ipEndpoint.Address.ToString();
        result.Port = ipEndpoint.Port;
        result.AddressFamily = ipEndpoint.AddressFamily == AddressFamily.InterNetwork ?
            FtpAddressFamily.IPv4 : FtpAddressFamily.IPv6;
        return result;
    }

    internal FtpGreetingSequence ReadGreetingSequence(
        FtpControlConnection connection,
        FtpDeadline deadline)
    {
        CancellationToken cancellationToken = Options.Timeouts.CancellationToken;
        FtpReply first = FtpControlReplyReader.ReadReply(
            connection.Stream,
            connection.Socket,
            deadline,
            cancellationToken,
            Options.StrictReplyParsing);
        ValidateGreetingCode(first, true);
        FtpGreetingSequence sequence = new FtpGreetingSequence();
        sequence.InitialReply = first;
        if (first.Code == FtpReplyCodes.ServiceReady)
        {
            sequence.ReadyReply = first;
            return sequence;
        }

        sequence.Delayed = true;
        FtpReply ready = FtpControlReplyReader.ReadReply(
            connection.Stream,
            connection.Socket,
            deadline,
            cancellationToken,
            Options.StrictReplyParsing);
        ValidateGreetingCode(ready, false);
        sequence.ReadyReply = ready;
        return sequence;
    }

    internal void ValidateGreetingCode(FtpReply reply, bool allowDelay)
    {
        if (reply.Code == FtpReplyCodes.ServiceReady)
        {
            return;
        }
        if (allowDelay && reply.Code == FtpReplyCodes.ServiceReadySoon)
        {
            return;
        }
        if (reply.Classification.TransientFailure ||
            reply.Classification.PermanentFailure)
        {
            throw new FtpConnectionException(
                $"The FTP server rejected the control connection with reply {reply.Code}: {reply.Text}",
                null);
        }

        throw new FtpProtocolException(
            "An FTP server greeting must contain 220, optionally preceded by one 120 reply.",
            null);
    }

    internal void ThrowResolutionFailure(
        FtpFaultIsolationResult resolution,
        FtpDeadline deadline)
    {
        CancellationToken cancellationToken = Options.Timeouts.CancellationToken;
        if (FtpCancellation.IsCancellationRequested(cancellationToken))
        {
            throw new OperationCanceledException(
                "FTP endpoint resolution was canceled.",
                resolution.Failure,
                cancellationToken);
        }

        OperationCanceledException? canceledFailure =
            resolution.Failure as OperationCanceledException;
        if (FtpTimeout.HasDeadlineExpired(deadline) || canceledFailure != null)
        {
            throw new TimeoutException(
                "FTP endpoint resolution timed out.", resolution.Failure);
        }

        throw new FtpConnectionException(
            "The FTP endpoint host could not be resolved.", resolution.Failure);
    }

    internal void ThrowGreetingFailure(
        FtpFaultIsolationResult greetingResult,
        FtpFaultIsolationResult cleanup,
        FtpDeadline deadline)
    {
        CancellationToken cancellationToken = Options.Timeouts.CancellationToken;
        if (FtpCancellation.IsCancellationRequested(cancellationToken))
        {
            throw new OperationCanceledException(
                "The FTP server greeting was canceled.",
                greetingResult.Failure,
                cancellationToken);
        }
        if (FtpTimeout.HasDeadlineExpired(deadline))
        {
            throw new TimeoutException(
                "The FTP server greeting timed out.", greetingResult.Failure);
        }
        if (!greetingResult.Succeeded && greetingResult.Failure != null)
        {
            throw greetingResult.Failure;
        }
        if (!cleanup.Succeeded)
        {
            throw new FtpConnectionException(
                "The failed FTP control connection could not be closed.",
                cleanup.Failure);
        }

        throw new FtpConnectionException(
            "The FTP server greeting did not reach a ready state.", null);
    }
}
