namespace ftp;

internal class FtpTransferHashResult
{
    internal string Algorithm;
    internal string Checksum;
    internal long BytesRead;

    internal FtpTransferHashResult()
    {
        Algorithm = "";
        Checksum = "";
        BytesRead = 0;
    }
}
