namespace sip;

internal class SipOwnedBuffer
{
    byte[] bytes;

    internal SipOwnedBuffer(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        this.bytes = new byte[bytes.Length];
        System.Array.Copy(bytes, this.bytes, bytes.Length);
    }

    internal int GetLength()
    {
        return bytes.Length;
    }

    internal byte[] CopyBytes()
    {
        byte[] result = new byte[bytes.Length];
        System.Array.Copy(bytes, result, bytes.Length);
        return result;
    }

    internal byte[] CopySegment(int offset, int count)
    {
        if (offset < 0)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        if (offset > bytes.Length - count)
        {
            throw new System.ArgumentException("The requested segment is outside the owned buffer.");
        }

        byte[] result = new byte[count];
        System.Array.Copy(bytes, offset, result, 0, count);
        return result;
    }

    internal void Clear()
    {
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
    }
}
