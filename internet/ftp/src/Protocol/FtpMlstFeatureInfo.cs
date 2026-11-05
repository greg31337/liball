namespace ftp;

internal class FtpMlstFeatureInfo
{
    internal bool Advertised;
    internal string[] Facts;
    internal string[] DefaultFacts;

    internal FtpMlstFeatureInfo()
    {
        Advertised = false;
        Facts = new string[0];
        DefaultFacts = new string[0];
    }
}
