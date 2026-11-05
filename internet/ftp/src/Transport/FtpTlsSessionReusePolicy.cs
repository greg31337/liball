using System.Net.Security;

namespace ftp;

internal static class FtpTlsSessionReusePolicy
{
    internal static FtpTlsSessionReuseEvidence InspectRuntime(SslStream? stream)
    {
        if (stream == null || !stream.IsAuthenticated)
        {
            throw new FtpTlsException(
                "An authenticated TLS stream is required for session-reuse inspection.", null);
        }

        return new FtpTlsSessionReuseEvidence();
    }

    internal static void RequireSatisfied(
        bool required,
        bool tlsResumeAllowed,
        FtpTlsSessionReuseEvidence? evidence)
    {
        if (!required)
        {
            return;
        }
        if (!tlsResumeAllowed)
        {
            throw new FtpConfigurationException(
                "Required FTPS data-session reuse cannot be combined with disabled TLS resumption.",
                null);
        }
        if (evidence == null || !evidence.Available)
        {
            throw new FtpTlsException(
                "Required FTPS data-session reuse cannot be verified by this TLS runtime.", null);
        }
        if (!evidence.Reused)
        {
            throw new FtpTlsException(
                "The FTPS data connection did not reuse a TLS session.", null);
        }
        if (!evidence.MatchesControlSession)
        {
            throw new FtpTlsException(
                "The FTPS data connection reused a TLS session that does not match the control channel.",
                null);
        }
    }
}
