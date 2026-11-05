using System.IO;
using System.Net.Sockets;

namespace ftp;

internal class FtpControlConnection
{
    internal Socket? Socket;
    internal NetworkStream? TransportStream;
    internal Stream? Stream;
    internal FtpTlsClientStream? TlsClient;
    internal FtpTlsStreamResult? TlsResult;
    internal FtpEndpoint LocalEndpoint;
    internal FtpEndpoint RemoteEndpoint;
    internal FtpReply? InitialGreeting;
    internal FtpReply? Greeting;
    internal bool DelayedGreeting;
    internal bool ControlProtected;
    internal bool PlaintextFallback;
    internal bool Utf8Enabled;

    internal FtpControlConnection()
    {
        Socket = null;
        TransportStream = null;
        Stream = null;
        TlsClient = null;
        TlsResult = null;
        LocalEndpoint = new FtpEndpoint();
        RemoteEndpoint = new FtpEndpoint();
        InitialGreeting = null;
        Greeting = null;
        DelayedGreeting = false;
        ControlProtected = false;
        PlaintextFallback = false;
        Utf8Enabled = false;
    }

    internal FtpFaultIsolationResult Close()
    {
        FtpTlsClientStream? tlsClient = TlsClient;
        NetworkStream? transportStream = TransportStream;
        Stream? stream = Stream;
        Socket? socket = Socket;
        TlsClient = null;
        TlsResult = null;
        TransportStream = null;
        Stream = null;
        Socket = null;
        FtpFaultIsolationResult tlsCleanup = CreateSuccessfulResult();
        if (tlsClient != null)
        {
            tlsCleanup = FtpFaultIsolationRunner.RunOperation(() =>
            {
                tlsClient.CloseClientTls();
                return null;
            });
        }

        FtpFaultIsolationResult transportCleanup = CreateSuccessfulResult();
        if (transportStream != null)
        {
            transportCleanup = new FtpResourceLifetime(
                transportStream).DisposeResource();
        }
        else if (stream != null)
        {
            transportCleanup = new FtpResourceLifetime(stream).DisposeResource();
        }
        else if (socket != null)
        {
            transportCleanup = new FtpResourceLifetime(socket).DisposeResource();
        }

        if (!tlsCleanup.Succeeded)
        {
            return tlsCleanup;
        }
        return transportCleanup;
    }

    internal FtpFaultIsolationResult CreateSuccessfulResult()
    {
        FtpFaultIsolationResult result = new FtpFaultIsolationResult();
        result.Succeeded = true;
        return result;
    }
}
