using System.IO;
using System.Net.Security;
using System.Threading;

namespace ftp;

internal class FtpTlsServerStreamRequest
{
    internal Stream? TransportStream;
    internal FtpTlsOptions? Options;
    internal int HandshakeTimeoutMilliseconds;
    internal CancellationToken CancellationToken;
    internal bool LeaveTransportOpen;
    internal bool AllowTlsResume;
    internal RemoteCertificateValidationCallback? CertificateValidation;
    internal ServerCertificateSelectionCallback? CertificateSelection;
    internal IFtpCertificateProvider? CertificateProvider;
    internal string HostName;
    internal FtpEndpoint? LocalEndpoint;
    internal FtpEndpoint? RemoteEndpoint;

    internal FtpTlsServerStreamRequest()
    {
        TransportStream = null;
        Options = new FtpTlsOptions();
        HandshakeTimeoutMilliseconds = FtpDefaults.TlsHandshakeTimeoutMilliseconds;
        CancellationToken = default(CancellationToken);
        LeaveTransportOpen = false;
        AllowTlsResume = true;
        CertificateValidation = null;
        CertificateSelection = null;
        CertificateProvider = null;
        HostName = "";
        LocalEndpoint = null;
        RemoteEndpoint = null;
    }
}
