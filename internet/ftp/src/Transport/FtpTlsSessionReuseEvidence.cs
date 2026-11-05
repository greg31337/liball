namespace ftp;

internal class FtpTlsSessionReuseEvidence
{
    internal bool Available;
    internal bool Reused;
    internal bool MatchesControlSession;

    internal FtpTlsSessionReuseEvidence()
    {
        Available = false;
        Reused = false;
        MatchesControlSession = false;
    }
}
