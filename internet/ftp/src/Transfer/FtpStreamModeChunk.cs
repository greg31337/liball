namespace ftp;

internal class FtpStreamModeChunk
{
    internal byte[] Data;
    internal int Count;
    internal bool CloseConnection;

    internal FtpStreamModeChunk(byte[] data, bool closeConnection)
    {
        Data = data;
        Count = data.Length;
        CloseConnection = closeConnection;
    }
}
