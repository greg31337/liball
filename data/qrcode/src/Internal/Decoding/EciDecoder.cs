namespace qrcode;

internal class EciDecoder
{
    internal static int DecodeAssignment(BitReader reader)
    {
        int value = TryDecodeAssignment(reader);
        if (value == -1) { throw new QrDecodeException("The encoded value is invalid or truncated.", QrFailureReason.InvalidData, -1, 0); }
        return value;
    }

    internal static int TryDecodeAssignment(BitReader reader)
    {
        if (reader == null)
        {
            throw new QrInputException("The bit reader cannot be null.", "reader");
        }

        int startPosition = reader.GetPosition();
        if (reader.GetRemainingBitCount() < 8)
        {
            return -1;
        }

        int firstByte = (int)reader.ReadBits(8);
        if ((firstByte & 0x80) == 0)
        {
            return firstByte & 0x7F;
        }

        if ((firstByte & 0x40) == 0)
        {
            if (reader.GetRemainingBitCount() < 8)
            {
                reader.SeekBitPosition(startPosition);
                return -1;
            }

            int assignment = (firstByte & 0x3F) << 8 | (int)reader.ReadBits(8);
            if (assignment <= QrLimits.EciOneByteMaximumAssignment)
            {
                reader.SeekBitPosition(startPosition);
                return -1;
            }

            return assignment;
        }

        if ((firstByte & 0x20) == 0)
        {
            if (reader.GetRemainingBitCount() < 16)
            {
                reader.SeekBitPosition(startPosition);
                return -1;
            }

            int assignment = (firstByte & 0x1F) << 16 | (int)reader.ReadBits(16);
            if (assignment <= QrLimits.EciTwoByteMaximumAssignment
                || assignment > QrLimits.EciMaximumQrAssignment)
            {
                reader.SeekBitPosition(startPosition);
                return -1;
            }

            return assignment;
        }

        reader.SeekBitPosition(startPosition);
        return -1;
    }

    internal static QrSegment DecodeControlSegment(BitReader reader)
    {
        int assignment = DecodeAssignment(reader);
        return new QrSegment
        {
            Mode = QrEncodingMode.Eci,
            Data = EciEncoder.GetDesignatorBytes(assignment),
            EciAssignment = assignment
        };
    }
}
