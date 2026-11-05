namespace qrcode;

internal class ByteEncoder
{
    internal static int CalculateBitLength(int byteCount)
    {
        if (byteCount < 0)
        {
            throw new QrInputException("The byte count cannot be negative.", "byteCount");
        }

        return CheckedDimensions.CalculateProduct(
            byteCount,
            8,
            int.MaxValue,
            "ByteBitLength");
    }

    internal static void EncodeBytes(byte[] bytes, BitBuffer destination)
    {
        if (destination == null)
        {
            throw new QrInputException("The destination bit buffer cannot be null.", "destination");
        }

        if (bytes == null)
        {
            throw new QrInputException("The byte segment cannot be null.", "bytes");
        }

        if (bytes.Length > QrLimits.DefaultMaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The byte segment exceeds the configured input limit.",
                "MaximumInputBytes",
                bytes.Length,
                QrLimits.DefaultMaximumInputBytes);
        }

        int encodedBitLength = CalculateBitLength(bytes.Length);
        int requiredBitLength = CheckedDimensions.CalculateSum(
            destination.GetBitLength(),
            encodedBitLength,
            int.MaxValue,
            "BitLength");
        destination.EnsureBitCapacity(requiredBitLength);

        global::System.ReadOnlySpan<byte> source = bytes;
        destination.AppendBytes(source);
    }
}
