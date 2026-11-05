using System.IO;

namespace ftp;

internal class FtpLineWriter
{
    internal int MaximumLineBytes;

    internal FtpLineWriter(int maximumLineBytes)
    {
        if (maximumLineBytes < 2)
        {
            throw new FtpConfigurationException("A line limit must reserve at least two bytes for CRLF.", null);
        }

        MaximumLineBytes = maximumLineBytes;
    }

    internal void WriteLine(Stream destination, byte[] lineBytes)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (lineBytes == null)
        {
            throw new System.ArgumentNullException("lineBytes");
        }

        if (lineBytes.Length > MaximumLineBytes - 2)
        {
            throw new FtpProtocolException("The FTP line exceeded its configured byte limit.", null);
        }

        int index = 0;
        while (index < lineBytes.Length)
        {
            if (lineBytes[index] == 13 || lineBytes[index] == 10)
            {
                throw new FtpProtocolException("FTP line content cannot contain carriage-return or line-feed bytes.", null);
            }

            index++;
        }

        destination.Write(lineBytes, 0, lineBytes.Length);
        destination.WriteByte(13);
        destination.WriteByte(10);
    }
}
