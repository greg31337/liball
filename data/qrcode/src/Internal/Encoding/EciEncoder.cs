namespace qrcode;

internal class EciEncoder
{
    internal static void ValidateAssignment(int assignment)
    {
        if (assignment < 0 || assignment > QrLimits.EciMaximumQrAssignment)
        {
            throw new QrInputException(
                "The ECI assignment must fit in the QR six-digit assignment range.",
                "assignment");
        }
    }

    internal static int CalculateBitLength(int assignment)
    {
        ValidateAssignment(assignment);
        if (assignment <= QrLimits.EciOneByteMaximumAssignment)
        {
            return 8;
        }

        if (assignment <= QrLimits.EciTwoByteMaximumAssignment)
        {
            return 16;
        }

        return 24;
    }

    internal static byte[] GetDesignatorBytes(int assignment)
    {
        ValidateAssignment(assignment);
        if (assignment <= QrLimits.EciOneByteMaximumAssignment)
        {
            return new byte[] { (byte)assignment };
        }

        if (assignment <= QrLimits.EciTwoByteMaximumAssignment)
        {
            return new byte[]
            {
                (byte)(0x80 | assignment >> 8),
                (byte)(assignment & 0xFF)
            };
        }

        return new byte[]
        {
            (byte)(0xC0 | assignment >> 16),
            (byte)(assignment >> 8 & 0xFF),
            (byte)(assignment & 0xFF)
        };
    }

    internal static void EncodeAssignment(int assignment, BitBuffer destination)
    {
        if (destination == null)
        {
            throw new QrInputException("The destination bit buffer cannot be null.", "destination");
        }

        int encodedBitLength = CalculateBitLength(assignment);
        int requiredBitLength = CheckedDimensions.CalculateSum(
            destination.GetBitLength(),
            encodedBitLength,
            int.MaxValue,
            "BitLength");
        destination.EnsureBitCapacity(requiredBitLength);
        byte[] designator = GetDesignatorBytes(assignment);
        global::System.ReadOnlySpan<byte> source = designator;
        destination.AppendBytes(source);
    }

    internal static QrSegment CreateControlSegment(int assignment)
    {
        byte[] designator = GetDesignatorBytes(assignment);
        return new QrSegment
        {
            Mode = QrEncodingMode.Eci,
            Data = designator,
            EciAssignment = assignment
        };
    }
}
