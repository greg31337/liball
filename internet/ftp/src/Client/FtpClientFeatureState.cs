namespace ftp;

internal class FtpClientFeatureState
{
    internal FtpFeatureSet Features;
    internal FtpOptionProcessor? OptionProcessor;
    internal bool Utf8Advertised;
    internal bool Utf8Enabled;
    internal bool MlstAdvertised;
    internal bool MachineListing;
    internal bool FileSize;
    internal bool ModificationTimeQuery;
    internal string[] MlstFacts;
    internal bool RestStream;
    internal bool ExtendedPassive;
    internal bool ExtendedActive;
    internal bool AuthTls;
    internal bool Host;
    internal bool Pret;
    internal bool ModeZ;
    internal bool ClientName;
    internal bool ModificationTime;
    internal bool SiteChmod;
    internal bool SiteUmask;
    internal bool Hash;

    internal FtpClientFeatureState()
    {
        Features = new FtpFeatureSet();
        OptionProcessor = null;
        Utf8Advertised = false;
        Utf8Enabled = false;
        MlstAdvertised = false;
        MachineListing = false;
        FileSize = false;
        ModificationTimeQuery = false;
        MlstFacts = new string[0];
        RestStream = false;
        ExtendedPassive = false;
        ExtendedActive = false;
        AuthTls = false;
        Host = false;
        Pret = false;
        ModeZ = false;
        ClientName = false;
        ModificationTime = false;
        SiteChmod = false;
        SiteUmask = false;
        Hash = false;
    }
}
