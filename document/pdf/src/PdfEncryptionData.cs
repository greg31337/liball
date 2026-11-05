namespace pdf;

internal class PdfEncryptionData
{
    internal byte[] FileKey;
    internal byte[] OwnerKey;
    internal byte[] UserKey;
    internal int Permissions;

    internal PdfEncryptionData()
    {
        FileKey = new byte[0];
        OwnerKey = new byte[0];
        UserKey = new byte[0];
        Permissions = 0;
    }
}
