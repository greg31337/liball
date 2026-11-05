namespace icc;

internal static class ChromaticityTagCodec
{
    internal static void DecodeChromaticity(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A chromaticityType payload requires at least 12 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The chromaticityType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader headerReader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
        int channelCount = headerReader.ReadUInt16();
        int encoding = headerReader.ReadUInt16();
        long expectedSize = 12L + ((long)channelCount * 8L);
        if (channelCount == 0 || expectedSize != payload.Length)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The chromaticityType channel count {channelCount} requires exactly {expectedSize} bytes and at least one channel, but this payload contains {payload.Length} bytes.");
            return;
        }

        int maximumEncoding = state.ProfileVersion == IccVersionKind.Version2 ? 4 : 6;
        if (encoding < 0 || encoding > maximumEncoding)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The chromaticityType colorant encoding {encoding} is not defined for this profile generation.");
            return;
        }

        double[] xCoordinates = new double[channelCount];
        double[] yCoordinates = new double[channelCount];
        BigEndianReader coordinateReader = new BigEndianReader(payload.Slice(12), state.AbsolutePayloadOffset + 12);
        int index = 0;
        while (index < channelCount)
        {
            xCoordinates[index] = IccNumberCodec.ReadU16Fixed16(coordinateReader);
            yCoordinates[index] = IccNumberCodec.ReadU16Fixed16(coordinateReader);
            index = index + 1;
        }

        if (encoding != 0 && (channelCount != 3 || !MatchesPredefinedEncoding(encoding, xCoordinates, yCoordinates)))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Chromaticity colorant encoding {encoding} requires exactly the three standardized channel coordinates.");
            return;
        }

        IccChromaticity chromaticity = new IccChromaticity();
        chromaticity.ColorantEncoding = encoding;
        chromaticity.XCoordinates = xCoordinates;
        chromaticity.YCoordinates = yCoordinates;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Chromaticity);
        data.Chromaticity = chromaticity;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded chromaticityType data for {channelCount} device channels.");
    }

    internal static byte[] SerializeChromaticity(IccChromaticity chromaticity, IccVersionKind profileVersion)
    {
        if (chromaticity == null || chromaticity.XCoordinates == null || chromaticity.YCoordinates == null)
        {
            throw new IccProfileWriteException("Chromaticity data and both coordinate arrays cannot be null.");
        }

        if (profileVersion != IccVersionKind.Version2 && profileVersion != IccVersionKind.Version4)
        {
            throw new IccProfileWriteException("chromaticityType is supported only by ICC.1 version 2 and version 4 profiles.");
        }

        int channelCount = chromaticity.XCoordinates.Length;
        if (channelCount == 0 || channelCount > ushort.MaxValue || chromaticity.YCoordinates.Length != channelCount)
        {
            throw new IccProfileWriteException("Chromaticity coordinate arrays shall have the same length between one and 65535 channels.");
        }

        int maximumEncoding = profileVersion == IccVersionKind.Version2 ? 4 : 6;
        int encoding = chromaticity.ColorantEncoding;
        if (encoding < 0 || encoding > maximumEncoding)
        {
            throw new IccProfileWriteException($"Chromaticity colorant encoding {encoding} is not defined for this profile generation.");
        }

        if (encoding != 0 && (channelCount != 3 || !MatchesPredefinedEncoding(encoding, chromaticity.XCoordinates, chromaticity.YCoordinates)))
        {
            throw new IccProfileWriteException($"Chromaticity colorant encoding {encoding} requires exactly the three standardized channel coordinates.");
        }

        byte[] payload = new byte[12 + (channelCount * 8)];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Chromaticity);
        writer.WriteZeroBytes(4);
        writer.WriteUInt16((ushort)channelCount);
        writer.WriteUInt16((ushort)encoding);
        int index = 0;
        while (index < channelCount)
        {
            ValidateCoordinate(chromaticity.XCoordinates[index], index, "x");
            ValidateCoordinate(chromaticity.YCoordinates[index], index, "y");
            IccNumberCodec.WriteU16Fixed16(writer, chromaticity.XCoordinates[index]);
            IccNumberCodec.WriteU16Fixed16(writer, chromaticity.YCoordinates[index]);
            index = index + 1;
        }

        return payload;
    }

    internal static bool MatchesPredefinedEncoding(int encoding, double[] xCoordinates, double[] yCoordinates)
    {
        if (xCoordinates.Length != 3 || yCoordinates.Length != 3)
        {
            return false;
        }

        double[] expected = GetPredefinedCoordinates(encoding);
        int index = 0;
        while (index < 3)
        {
            if (IccNumberCodec.EncodeU16Fixed16(xCoordinates[index]) != IccNumberCodec.EncodeU16Fixed16(expected[index * 2])
                || IccNumberCodec.EncodeU16Fixed16(yCoordinates[index]) != IccNumberCodec.EncodeU16Fixed16(expected[(index * 2) + 1]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static double[] GetPredefinedCoordinates(int encoding)
    {
        switch (encoding)
        {
            case 1:
                return new double[] { 0.640, 0.330, 0.300, 0.600, 0.150, 0.060 };
            case 2:
                return new double[] { 0.630, 0.340, 0.310, 0.595, 0.155, 0.070 };
            case 3:
                return new double[] { 0.640, 0.330, 0.290, 0.600, 0.150, 0.060 };
            case 4:
                return new double[] { 0.625, 0.340, 0.280, 0.605, 0.155, 0.070 };
            case 5:
                return new double[] { 0.680, 0.320, 0.265, 0.690, 0.150, 0.060 };
            case 6:
                return new double[] { 0.708, 0.292, 0.170, 0.797, 0.131, 0.046 };
            default:
                return new double[0];
        }
    }

    internal static void ValidateCoordinate(double value, int channelIndex, string component)
    {
        if (System.Double.IsNaN(value) || System.Double.IsInfinity(value) || value < 0.0 || value > 1.0)
        {
            throw new IccProfileWriteException($"Chromaticity channel {channelIndex} {component} coordinate shall be finite and between zero and one.");
        }
    }
}
