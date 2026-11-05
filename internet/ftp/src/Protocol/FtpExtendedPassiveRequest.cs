namespace ftp;

internal class FtpExtendedPassiveRequest
{
    internal bool All;
    internal bool HasProtocol;
    internal int Protocol;
    internal FtpAddressFamily AddressFamily;
    internal bool Supported;

    internal FtpExtendedPassiveRequest()
    {
        All = false;
        HasProtocol = false;
        Protocol = 0;
        AddressFamily = FtpAddressFamily.Unspecified;
        Supported = true;
    }
}
