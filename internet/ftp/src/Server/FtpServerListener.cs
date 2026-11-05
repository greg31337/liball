using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ftp;

internal class FtpServerListener
{
    internal FtpListenerOptions Settings;
    internal Socket? Listener;
    internal FtpEndpoint LocalEndpoint;
    internal Task<object?>? AcceptLoop;
    internal ManualResetEventSlim StopSignal;
    internal bool Stopping;

    internal FtpServerListener(FtpListenerOptions settings)
    {
        Settings = settings;
        Listener = null;
        LocalEndpoint = new FtpEndpoint();
        AcceptLoop = null;
        StopSignal = new ManualResetEventSlim(false);
        Stopping = false;
    }

    internal void StartListener()
    {
        IPAddress address = ResolveBindAddress();
        FtpFaultIsolationResult startResult =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                Socket socket = new Socket(
                    address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Listener = socket;
                socket.ExclusiveAddressUse = true;
                if (socket.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    socket.DualMode = Settings.DualStack;
                }

                socket.Bind(new IPEndPoint(address, Settings.Endpoint.Port));
                socket.Listen(Settings.Backlog);
                return null;
            });
        if (!startResult.Succeeded)
        {
            FtpFaultIsolationResult cleanupResult = CloseListener();
            System.Exception? failure = startResult.Failure == null ?
                cleanupResult.Failure : startResult.Failure;
            throw new FtpConnectionException(
                "The FTP control listener could not bind and start.", failure);
        }

        Socket? listener = Listener;
        if (listener == null)
        {
            throw new FtpConnectionException(
                "The FTP control listener started without a socket.", null);
        }

        IPEndPoint? localEndpoint = listener.LocalEndPoint as IPEndPoint;
        if (localEndpoint == null)
        {
            FtpFaultIsolationResult cleanupResult = CloseListener();
            throw new FtpConnectionException(
                "The FTP control listener did not expose a local IP endpoint.",
                cleanupResult.Failure);
        }

        LocalEndpoint.Host = localEndpoint.Address.ToString();
        LocalEndpoint.Port = localEndpoint.Port;
        LocalEndpoint.AddressFamily =
            localEndpoint.AddressFamily == AddressFamily.InterNetwork ?
                FtpAddressFamily.IPv4 : FtpAddressFamily.IPv6;
    }

    internal FtpFaultIsolationResult CloseListener()
    {
        Stopping = true;
        StopSignal.Set();
        Socket? listener = Listener;
        Listener = null;
        FtpFaultIsolationResult cleanupResult =
            new FtpResourceLifetime(listener).DisposeResource();
        Task<object?>? acceptLoop = AcceptLoop;
        AcceptLoop = null;
        if (acceptLoop == null)
        {
            return cleanupResult;
        }

        FtpFaultIsolationResult acceptResult =
            FtpFaultIsolationRunner.ObserveTask(acceptLoop);
        if (!cleanupResult.Succeeded)
        {
            return cleanupResult;
        }
        return acceptResult;
    }

    internal void StartAcceptLoop(FtpServerSessionDispatcher dispatcher)
    {
        Stopping = false;
        StopSignal.Reset();
        System.Func<object?> operation = () =>
        {
            RunAcceptLoop(dispatcher);
            return null;
        };
        AcceptLoop = Task.Run(operation);
    }

    internal void RunAcceptLoop(FtpServerSessionDispatcher dispatcher)
    {
        while (!Stopping)
        {
            if (!dispatcher.WaitForCapacity(this))
            {
                return;
            }

            Socket? listener = Listener;
            if (listener == null)
            {
                dispatcher.ReleaseCapacity();
                return;
            }

            FtpFaultIsolationResult acceptResult =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    return listener.Accept();
                });
            Socket? connection = acceptResult.Value as Socket;
            if (!acceptResult.Succeeded || connection == null)
            {
                dispatcher.ReleaseCapacity();
                if (Stopping)
                {
                    return;
                }
                return;
            }

            dispatcher.DispatchSession(connection, Settings);
        }
    }

    internal IPAddress ResolveBindAddress()
    {
        string host = Settings.Endpoint.Host;
        if (string.IsNullOrEmpty(host))
        {
            return ResolveWildcardAddress();
        }

        FtpFaultIsolationResult parseResult =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return IPAddress.Parse(host);
            });
        IPAddress? parsedAddress = parseResult.Value as IPAddress;
        if (parseResult.Succeeded && parsedAddress != null)
        {
            if (!MatchesConfiguredFamily(parsedAddress))
            {
                throw new FtpConfigurationException(
                    "The FTP listener address does not match its configured family.",
                    null);
            }
            return parsedAddress;
        }

        FtpFaultIsolationResult resolutionResult =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return Dns.GetHostAddresses(host);
            });
        IPAddress[]? addresses = resolutionResult.Value as IPAddress[];
        if (!resolutionResult.Succeeded || addresses == null)
        {
            throw new FtpConnectionException(
                "The FTP listener host could not be resolved.",
                resolutionResult.Failure);
        }

        int index = 0;
        while (index < addresses.Length)
        {
            IPAddress address = addresses[index];
            if (MatchesConfiguredFamily(address))
            {
                return address;
            }
            index++;
        }

        throw new FtpConfigurationException(
            "No FTP listener host address matches the configured family.", null);
    }

    internal IPAddress ResolveWildcardAddress()
    {
        if (Settings.Endpoint.AddressFamily == FtpAddressFamily.IPv4)
        {
            return IPAddress.Any;
        }
        if (Settings.Endpoint.AddressFamily == FtpAddressFamily.IPv6)
        {
            return IPAddress.IPv6Any;
        }
        if (Settings.DualStack && Socket.OSSupportsIPv6)
        {
            return IPAddress.IPv6Any;
        }
        return IPAddress.Any;
    }

    internal bool MatchesConfiguredFamily(IPAddress address)
    {
        if (Settings.Endpoint.AddressFamily == FtpAddressFamily.Unspecified)
        {
            return address.AddressFamily == AddressFamily.InterNetwork ||
                address.AddressFamily == AddressFamily.InterNetworkV6;
        }
        if (Settings.Endpoint.AddressFamily == FtpAddressFamily.IPv4)
        {
            return address.AddressFamily == AddressFamily.InterNetwork;
        }
        return address.AddressFamily == AddressFamily.InterNetworkV6;
    }
}
