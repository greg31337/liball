using System.Net.Security;
using System.Security.Authentication;

namespace ftp;

internal class FtpTlsStreamResult
{
    internal SslStream? Stream;
    internal bool Authenticated;
    internal bool Server;
    internal bool ImplicitTls;
    internal bool ExplicitUpgrade;
    internal bool DataChannel;
    internal bool TlsResumeAllowed;
    internal FtpTlsSessionReuseEvidence? SessionReuseEvidence;
    internal bool Encrypted;
    internal bool Signed;
    internal bool MutuallyAuthenticated;
    internal SslProtocols Protocol;
    internal FtpTlsDiagnostics Diagnostics;

    internal FtpTlsStreamResult()
    {
        Stream = null;
        Authenticated = false;
        Server = false;
        ImplicitTls = false;
        ExplicitUpgrade = false;
        DataChannel = false;
        TlsResumeAllowed = false;
        SessionReuseEvidence = null;
        Encrypted = false;
        Signed = false;
        MutuallyAuthenticated = false;
        Protocol = SslProtocols.None;
        Diagnostics = new FtpTlsDiagnostics();
    }
}
