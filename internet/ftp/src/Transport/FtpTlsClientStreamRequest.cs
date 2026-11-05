using System.IO;
using System.Net.Security;
using System.Threading;

namespace ftp;

internal class FtpTlsClientStreamRequest
{
    internal Stream? TransportStream;
    internal FtpTlsOptions? Options;
    internal string TargetHost;
    internal int HandshakeTimeoutMilliseconds;
    internal CancellationToken CancellationToken;
    internal bool LeaveTransportOpen;
    internal bool AllowTlsResume;
    internal RemoteCertificateValidationCallback? CertificateValidation;
    internal FtpEndpoint? RemoteEndpoint;
    internal IFtpCertificateValidator? CertificateValidator;

    internal FtpTlsClientStreamRequest()
    {
        TransportStream = null;
        Options = new FtpTlsOptions();
        TargetHost = "";
        HandshakeTimeoutMilliseconds = FtpDefaults.TlsHandshakeTimeoutMilliseconds;
        CancellationToken = default(CancellationToken);
        LeaveTransportOpen = false;
        AllowTlsResume = true;
        CertificateValidation = null;
        RemoteEndpoint = null;
        CertificateValidator = null;
    }
}
