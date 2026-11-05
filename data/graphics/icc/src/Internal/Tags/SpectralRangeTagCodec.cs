namespace icc;

internal static class SpectralRangeTagCodec
{
    internal const int PayloadSize = 20;

    internal static void DecodeSpectralRange(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length != PayloadSize)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A spectralRangeType payload requires exactly {PayloadSize} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The spectralRangeType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(8), state.AbsolutePayloadOffset + 8);
        IccSpectralRangeNumber spectral = IccNumberCodec.ReadSpectralRange(reader);
        IccSpectralRangeNumber biSpectral = IccNumberCodec.ReadSpectralRange(reader);
        if (!IsDefinedRange(spectral) || (!IsZeroRange(biSpectral) && !IsDefinedRange(biSpectral)))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The spectralRangeType shall contain an ordered primary range and either an ordered or zero secondary range.");
            return;
        }

        string contextError = ValidateDeviceContext(state, spectral, biSpectral);
        if (contextError.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, contextError);
            return;
        }

        IccSpectralRanges ranges = new IccSpectralRanges();
        CopyRange(spectral, ranges.SpectralRange);
        CopyRange(biSpectral, ranges.BiSpectralRange);
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.SpectralRange);
        data.SpectralRanges = ranges;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded spectral ranges with {spectral.Steps} primary and {biSpectral.Steps} secondary steps.");
    }

    internal static byte[] SerializeSpectralRanges(IccSpectralRanges ranges)
    {
        if (ranges == null || ranges.SpectralRange == null || ranges.BiSpectralRange == null)
        {
            throw new IccProfileWriteException("Spectral-range data and both range values cannot be null.");
        }

        ValidateRangeForWrite(ranges.SpectralRange, false, "primary");
        ValidateRangeForWrite(ranges.BiSpectralRange, true, "secondary");
        byte[] payload = new byte[PayloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.SpectralRange);
        writer.WriteZeroBytes(4);
        WriteRange(writer, ranges.SpectralRange);
        WriteRange(writer, ranges.BiSpectralRange);
        return payload;
    }

    internal static string ValidateDeviceContext(TagDecodeState state, IccSpectralRangeNumber spectral, IccSpectralRangeNumber biSpectral)
    {
        if (!state.HasProfileContext || state.TagSignature != IccTagSignatures.DeviceSpectralRange)
        {
            return string.Empty;
        }

        if (state.DataColorSpace != IccColorSpaceKind.Spectral && state.DataColorSpace != IccColorSpaceKind.BiSpectral)
        {
            return "The deviceSpectralRangeTag requires a spectral device colour-space signature.";
        }

        uint prefix = state.DataColorSpaceSignature & 0xffff0000;
        int channels = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        if (state.DataColorSpace == IccColorSpaceKind.Spectral)
        {
            if (!IsZeroRange(biSpectral))
            {
                return "The deviceSpectralRangeTag bi-spectral range shall be zero for a non-bi-spectral device colour space.";
            }

            if (channels != spectral.Steps)
            {
                return $"The device spectral range defines {spectral.Steps} steps, but its colour-space signature defines {channels} channels.";
            }

            return string.Empty;
        }

        if (IsZeroRange(biSpectral))
        {
            return "The deviceSpectralRangeTag shall define a secondary range for a bi-spectral device colour space.";
        }

        if (prefix == IccColorSpaceSignatures.BiSpectralPrefix && (long)spectral.Steps * biSpectral.Steps != channels)
        {
            return $"The bi-spectral device ranges define {(long)spectral.Steps * biSpectral.Steps} total channels, but the colour-space signature defines {channels} channels.";
        }

        return string.Empty;
    }

    internal static bool IsDefinedRange(IccSpectralRangeNumber range)
    {
        return range.Steps > 0 && SpectralViewingConditionsTagCodec.IsValidRange(range);
    }

    internal static bool IsZeroRange(IccSpectralRangeNumber range)
    {
        return range.Steps == 0 && range.StartWavelength == 0.0 && range.EndWavelength == 0.0;
    }

    internal static void ValidateRangeForWrite(IccSpectralRange range, bool allowZero, string name)
    {
        if (range.Steps < 0 || range.Steps > ushort.MaxValue)
        {
            throw new IccProfileWriteException($"The spectralRangeType {name} step count shall fit in an unsigned 16-bit integer.");
        }

        bool isZero = range.Steps == 0 && range.StartWavelength == 0.0 && range.EndWavelength == 0.0;
        if (allowZero && isZero)
        {
            return;
        }

        if (range.Steps == 0 || !SpectralViewingConditionsTagCodec.IsValidRangeValues(range.StartWavelength, range.EndWavelength, range.Steps))
        {
            throw new IccProfileWriteException($"The spectralRangeType {name} range is not defined and ordered.");
        }

        float encodedStart = IccNumberCodec.ConvertDoubleToFloat16Source(range.StartWavelength);
        float encodedEnd = IccNumberCodec.ConvertDoubleToFloat16Source(range.EndWavelength);
        if (!SpectralViewingConditionsTagCodec.IsValidRangeValues(encodedStart, encodedEnd, range.Steps))
        {
            throw new IccProfileWriteException($"The spectralRangeType {name} range is not ordered at float16 precision.");
        }
    }

    internal static void WriteRange(BigEndianWriter writer, IccSpectralRange range)
    {
        IccNumberCodec.WriteSpectralRange(writer, new IccSpectralRangeNumber(
            IccNumberCodec.ConvertDoubleToFloat16Source(range.StartWavelength),
            IccNumberCodec.ConvertDoubleToFloat16Source(range.EndWavelength),
            (ushort)range.Steps));
    }

    internal static void CopyRange(IccSpectralRangeNumber source, IccSpectralRange destination)
    {
        destination.StartWavelength = source.StartWavelength;
        destination.EndWavelength = source.EndWavelength;
        destination.Steps = source.Steps;
    }
}
