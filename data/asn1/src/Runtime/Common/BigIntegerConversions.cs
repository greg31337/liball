namespace asn1;

internal static class BigIntegerConversions
{
    internal static System.Numerics.BigInteger FromUnsignedBigEndian(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        if (bytes.Length == 0)
        {
            return System.Numerics.BigInteger.Zero;
        }

        byte[] littleEndian = new byte[bytes.Length + 1];
        int index = 0;
        while (index < bytes.Length)
        {
            littleEndian[index] = bytes[bytes.Length - index - 1];
            index = index + 1;
        }

        return new System.Numerics.BigInteger(littleEndian);
    }

    internal static System.Numerics.BigInteger FromSignedBigEndian(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        if (bytes.Length == 0)
        {
            return System.Numerics.BigInteger.Zero;
        }

        byte[] littleEndian = new byte[bytes.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            littleEndian[index] = bytes[bytes.Length - index - 1];
            index = index + 1;
        }

        return new System.Numerics.BigInteger(littleEndian);
    }

    internal static byte[] ToUnsignedBigEndian(System.Numerics.BigInteger value)
    {
        if (value < System.Numerics.BigInteger.Zero)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        byte[] littleEndian = value.ToByteArray();
        int resultLength = littleEndian.Length;
        if (resultLength > 1 && littleEndian[resultLength - 1] == 0)
        {
            resultLength = resultLength - 1;
        }

        byte[] result = new byte[resultLength];
        int index = 0;
        while (index < resultLength)
        {
            result[index] = littleEndian[resultLength - index - 1];
            index = index + 1;
        }

        return result;
    }

    internal static byte[] ToSignedBigEndian(System.Numerics.BigInteger value)
    {
        byte[] littleEndian = value.ToByteArray();
        byte[] result = new byte[littleEndian.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = littleEndian[result.Length - index - 1];
            index = index + 1;
        }

        return result;
    }
}
