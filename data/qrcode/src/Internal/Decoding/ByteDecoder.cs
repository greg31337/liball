namespace qrcode;

internal class ByteDecoder
{
    internal static byte[] DecodeBytes(BitReader reader, int byteCount)
    {
        if (reader == null)
        {
            throw new QrInputException("The bit reader cannot be null.", "reader");
        }

        if (byteCount < 0)
        {
            throw new QrInputException("The byte count cannot be negative.", "byteCount");
        }

        if (byteCount > QrLimits.DefaultMaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The byte segment exceeds the configured input limit.",
                "MaximumInputBytes",
                byteCount,
                QrLimits.DefaultMaximumInputBytes);
        }

        CheckedDimensions.CalculateAllocationBytes(
            byteCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        int requiredBitCount = ByteEncoder.CalculateBitLength(byteCount);
        if (requiredBitCount > reader.GetRemainingBitCount())
        {
            throw new QrDecodeException(
                "The byte segment ends before all declared bytes can be read.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        byte[] bytes = new byte[byteCount];
        global::System.Span<byte> destination = bytes;
        reader.ReadBytes(byteCount, destination);
        return bytes;
    }
}
