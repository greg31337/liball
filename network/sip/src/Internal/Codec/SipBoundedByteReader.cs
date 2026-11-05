namespace sip;

internal class SipBoundedByteReader
{
    byte[] bytes;
    int position;

    internal SipBoundedByteReader(byte[] bytes, int maximumLength)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        if (maximumLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumLength");
        }

        if (bytes.Length > maximumLength)
        {
            throw new System.ArgumentException("The input exceeds the configured maximum length.", "bytes");
        }

        this.bytes = bytes;
        position = 0;
    }

    internal int GetPosition()
    {
        return position;
    }

    internal int GetRemainingCount()
    {
        return bytes.Length - position;
    }

    internal int PeekByte()
    {
        if (position == bytes.Length)
        {
            return -1;
        }

        return bytes[position];
    }

    internal int ReadByte()
    {
        int value = PeekByte();
        if (value >= 0)
        {
            position = position + 1;
        }

        return value;
    }

    internal byte[] ReadBytes(int count)
    {
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        if (count > bytes.Length - position)
        {
            throw new System.InvalidOperationException("The requested bytes extend beyond the bounded input.");
        }

        byte[] result = new byte[count];
        System.Array.Copy(bytes, position, result, 0, count);
        position = position + count;
        return result;
    }

    internal bool TrySkip(int count)
    {
        if (count < 0 || count > bytes.Length - position)
        {
            return false;
        }

        position = position + count;
        return true;
    }
}
