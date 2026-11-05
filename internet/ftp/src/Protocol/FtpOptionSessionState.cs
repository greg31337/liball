namespace ftp;

internal class FtpOptionSessionState
{
    internal string[] MlstFacts;
    internal bool MlstFactsConfigured;
    internal bool Utf8Enabled;
    internal bool Utf8Configured;
    internal FtpOptionRequest[] ExtensionOptions;
    internal string HashAlgorithm;
    internal int UnixUmask;

    internal FtpOptionSessionState()
    {
        MlstFacts = new string[0];
        MlstFactsConfigured = false;
        Utf8Enabled = false;
        Utf8Configured = false;
        ExtensionOptions = new FtpOptionRequest[0];
        HashAlgorithm = "SHA-256";
        UnixUmask = 18;
    }
}
