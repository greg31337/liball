namespace icc;

internal static class SpectralViewingConditionsTagCodec
{
    internal const int MinimumPayloadSize = 60;

    internal static void DecodeSpectralViewingConditions(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < MinimumPayloadSize)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A spectralViewingConditionsType payload requires at least {MinimumPayloadSize} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload) || !HasZeroBytesAt(payload, 18, 2))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The spectralViewingConditionsType reserved bytes shall be zero.");
            return;
        }

        uint observerValue = ReadUInt32At(payload, 8);
        int observerSteps = ReadUInt16At(payload, 16);
        if (observerValue > 2)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The spectralViewingConditionsType observer encoding {observerValue} is not defined.");
            return;
        }

        long illuminantHeaderOffsetValue = 20L + ((long)observerSteps * 12L);
        if (illuminantHeaderOffsetValue > payload.Length - 40)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The spectralViewingConditionsType observer sample count exceeds its payload bounds.");
            return;
        }

        int illuminantHeaderOffset = (int)illuminantHeaderOffsetValue;
        uint illuminantValue = ReadUInt32At(payload, illuminantHeaderOffset);
        int illuminantSteps = ReadUInt16At(payload, illuminantHeaderOffset + 12);
        if (illuminantValue > 22)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The spectralViewingConditionsType illuminant encoding {illuminantValue} is not defined.");
            return;
        }

        long expectedSizeValue = MinimumPayloadSize + ((long)observerSteps * 12L) + ((long)illuminantSteps * 4L);
        if (expectedSizeValue != payload.Length)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The spectralViewingConditionsType sample counts require {expectedSizeValue} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!HasZeroBytesAt(payload, illuminantHeaderOffset + 14, 2))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The spectralViewingConditionsType reserved bytes shall be zero.");
            return;
        }

        int illuminantValuesOffset = illuminantHeaderOffset + 16;
        if (!Icc2ArrayTagCodec.ContainsOnlyFiniteFloatingPointValues(payload.Slice(12, 4), IccTypeSignatures.Float16Array)
            || !Icc2ArrayTagCodec.ContainsOnlyFiniteFloatingPointValues(payload.Slice(20, observerSteps * 12), IccTypeSignatures.Float32Array)
            || !Icc2ArrayTagCodec.ContainsOnlyFiniteFloatingPointValues(payload.Slice(illuminantHeaderOffset + 4, 4), IccTypeSignatures.Float32Array)
            || !Icc2ArrayTagCodec.ContainsOnlyFiniteFloatingPointValues(payload.Slice(illuminantHeaderOffset + 8, 4), IccTypeSignatures.Float16Array)
            || !Icc2ArrayTagCodec.ContainsOnlyFiniteFloatingPointValues(payload.Slice(illuminantValuesOffset, (illuminantSteps * 4) + 24), IccTypeSignatures.Float32Array))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The spectralViewingConditionsType shall contain only finite wavelength, sample, temperature, and XYZ values.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(8), state.AbsolutePayloadOffset + 8);
        reader.ReadUInt32();
        IccSpectralRangeNumber observerRange = IccNumberCodec.ReadSpectralRange(reader);
        reader.ReadUInt16();
        if (!IsValidRange(observerRange) || ((int)observerValue == 0 && observerRange.Steps == 0))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The spectralViewingConditionsType observer range is invalid or omits custom observer functions.");
            return;
        }

        IccSpectralData? observerFunctions = ReadSpectrum(reader, observerRange, 3);
        reader.ReadUInt32();
        double colorTemperature = IccNumberCodec.ReadFloat32(reader);
        IccSpectralRangeNumber illuminantRange = IccNumberCodec.ReadSpectralRange(reader);
        reader.ReadUInt16();
        if (!IsValidRange(illuminantRange) || !IsValidColorTemperature((int)illuminantValue, colorTemperature))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The spectralViewingConditionsType illuminant range or correlated colour temperature is invalid.");
            return;
        }

        IccSpectralData? illuminantSpectrum = ReadSpectrum(reader, illuminantRange, 1);
        IccXyzColor illuminantColor = ReadFloatXyz(reader);
        IccXyzColor surroundColor = ReadFloatXyz(reader);

        IccSpectralViewingConditions conditions = new IccSpectralViewingConditions();
        conditions.Observer = (IccMeasurementObserver)observerValue;
        conditions.ObserverFunctions = observerFunctions;
        conditions.Illuminant = (IccStandardIlluminant)illuminantValue;
        conditions.IlluminantSpectrum = illuminantSpectrum;
        conditions.ColorTemperature = colorTemperature;
        conditions.IlluminantColor = illuminantColor;
        conditions.SurroundColor = surroundColor;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.SpectralViewingConditions);
        data.SpectralViewingConditions = conditions;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded spectral viewing conditions with {observerSteps} observer samples and {illuminantSteps} illuminant samples.");
    }

    internal static byte[] SerializeSpectralViewingConditions(IccSpectralViewingConditions conditions)
    {
        if (conditions == null || conditions.IlluminantColor == null || conditions.SurroundColor == null)
        {
            throw new IccProfileWriteException("Spectral viewing conditions and both XYZ colours cannot be null.");
        }

        int observerValue = (int)conditions.Observer;
        int illuminantValue = (int)conditions.Illuminant;
        if (observerValue < 0 || observerValue > 2)
        {
            throw new IccProfileWriteException("Spectral viewing conditions shall use a defined observer encoding.");
        }

        if (illuminantValue < 0 || illuminantValue > 22)
        {
            throw new IccProfileWriteException("Spectral viewing conditions shall use a defined illuminant encoding.");
        }

        int observerSteps = ValidateObserverFunctions(conditions.ObserverFunctions, observerValue);
        int illuminantSteps = ValidateIlluminantSpectrum(conditions.IlluminantSpectrum);
        if (!IsValidColorTemperature(illuminantValue, conditions.ColorTemperature))
        {
            throw new IccProfileWriteException("Correlated colour temperature shall be non-negative and positive for temperature-defined illuminants.");
        }

        ValidateFloatXyz(conditions.IlluminantColor, "spectral-viewing illuminant XYZ");
        ValidateFloatXyz(conditions.SurroundColor, "spectral-viewing surround XYZ");
        long payloadSize = MinimumPayloadSize + ((long)observerSteps * 12L) + ((long)illuminantSteps * 4L);
        if (payloadSize > System.Array.MaxLength)
        {
            throw new IccProfileWriteException("The spectralViewingConditionsType payload exceeds the supported managed array size.");
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.SpectralViewingConditions);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)observerValue);
        WriteSpectralRange(writer, conditions.ObserverFunctions);
        writer.WriteZeroBytes(2);
        WriteSpectrumValues(writer, conditions.ObserverFunctions);
        writer.WriteUInt32((uint)illuminantValue);
        WriteFloat32(writer, conditions.ColorTemperature);
        WriteSpectralRange(writer, conditions.IlluminantSpectrum);
        writer.WriteZeroBytes(2);
        WriteSpectrumValues(writer, conditions.IlluminantSpectrum);
        WriteFloatXyz(writer, conditions.IlluminantColor);
        WriteFloatXyz(writer, conditions.SurroundColor);
        return payload;
    }

    internal static IccSpectralData? ReadSpectrum(BigEndianReader reader, IccSpectralRangeNumber range, int spectrumCount)
    {
        if (range.Steps == 0)
        {
            return null;
        }

        IccSpectralData spectrum = new IccSpectralData();
        spectrum.Range.StartWavelength = range.StartWavelength;
        spectrum.Range.EndWavelength = range.EndWavelength;
        spectrum.Range.Steps = range.Steps;
        spectrum.SpectrumCount = spectrumCount;
        spectrum.Values = new double[range.Steps * spectrumCount];
        int index = 0;
        while (index < spectrum.Values.Length)
        {
            spectrum.Values[index] = IccNumberCodec.ReadFloat32(reader);
            index = index + 1;
        }

        return spectrum;
    }

    internal static IccXyzColor ReadFloatXyz(BigEndianReader reader)
    {
        IccXyzColor color = new IccXyzColor();
        color.X = IccNumberCodec.ReadFloat32(reader);
        color.Y = IccNumberCodec.ReadFloat32(reader);
        color.Z = IccNumberCodec.ReadFloat32(reader);
        return color;
    }

    internal static int ValidateObserverFunctions(IccSpectralData? spectrum, int observerValue)
    {
        if (spectrum == null)
        {
            if (observerValue == 0)
            {
                throw new IccProfileWriteException("A custom observer shall provide three colour-matching functions.");
            }

            return 0;
        }

        return ValidateSpectrum(spectrum, 3, "observer functions");
    }

    internal static int ValidateIlluminantSpectrum(IccSpectralData? spectrum)
    {
        if (spectrum == null)
        {
            return 0;
        }

        return ValidateSpectrum(spectrum, 1, "illuminant spectrum");
    }

    internal static int ValidateSpectrum(IccSpectralData spectrum, int expectedSpectrumCount, string name)
    {
        if (spectrum.Range == null || spectrum.Values == null || spectrum.SparseValues != null
            || spectrum.SpectrumCount != expectedSpectrumCount)
        {
            throw new IccProfileWriteException($"The spectral-viewing {name} shall contain {expectedSpectrumCount} dense spectra and a range.");
        }

        int steps = spectrum.Range.Steps;
        if (steps < 1 || steps > ushort.MaxValue || spectrum.Values.Length != steps * expectedSpectrumCount
            || !IsValidRangeValues(spectrum.Range.StartWavelength, spectrum.Range.EndWavelength, steps))
        {
            throw new IccProfileWriteException($"The spectral-viewing {name} range and sample count are inconsistent.");
        }

        float encodedStart = IccNumberCodec.ConvertDoubleToFloat16Source(spectrum.Range.StartWavelength);
        float encodedEnd = IccNumberCodec.ConvertDoubleToFloat16Source(spectrum.Range.EndWavelength);
        if (!IsValidRangeValues(encodedStart, encodedEnd, steps))
        {
            throw new IccProfileWriteException($"The spectral-viewing {name} range is not ordered at float16 precision.");
        }

        int index = 0;
        while (index < spectrum.Values.Length)
        {
            ValidateFloat32(spectrum.Values[index], $"Spectral-viewing {name} sample {index}");
            index = index + 1;
        }

        return steps;
    }

    internal static bool IsValidRange(IccSpectralRangeNumber range)
    {
        return IsValidRangeValues(range.StartWavelength, range.EndWavelength, range.Steps);
    }

    internal static bool IsValidRangeValues(double start, double end, int steps)
    {
        if (double.IsNaN(start) || double.IsInfinity(start) || double.IsNaN(end) || double.IsInfinity(end))
        {
            return false;
        }

        if (steps == 0)
        {
            return start == 0.0 && end == 0.0;
        }

        if (steps == 1)
        {
            return start == end;
        }

        return start < end;
    }

    internal static bool IsValidColorTemperature(int illuminantValue, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return false;
        }

        if (illuminantValue == 9 || illuminantValue == 10)
        {
            return value > 0.0;
        }

        return value >= 0.0;
    }

    internal static void ValidateFloatXyz(IccXyzColor color, string name)
    {
        ValidateFloat32(color.X, $"{name} X");
        ValidateFloat32(color.Y, $"{name} Y");
        ValidateFloat32(color.Z, $"{name} Z");
    }

    internal static void ValidateFloat32(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new IccProfileWriteException($"{name} shall be finite.");
        }

        IccNumberCodec.ConvertDoubleToFloat32(value);
    }

    internal static void WriteSpectralRange(BigEndianWriter writer, IccSpectralData? spectrum)
    {
        if (spectrum == null)
        {
            IccNumberCodec.WriteSpectralRange(writer, new IccSpectralRangeNumber(0.0f, 0.0f, 0));
            return;
        }

        float start = IccNumberCodec.ConvertDoubleToFloat16Source(spectrum.Range.StartWavelength);
        float end = IccNumberCodec.ConvertDoubleToFloat16Source(spectrum.Range.EndWavelength);
        IccNumberCodec.WriteSpectralRange(writer, new IccSpectralRangeNumber(start, end, (ushort)spectrum.Range.Steps));
    }

    internal static void WriteSpectrumValues(BigEndianWriter writer, IccSpectralData? spectrum)
    {
        if (spectrum == null)
        {
            return;
        }

        int index = 0;
        while (index < spectrum.Values.Length)
        {
            WriteFloat32(writer, spectrum.Values[index]);
            index = index + 1;
        }
    }

    internal static void WriteFloatXyz(BigEndianWriter writer, IccXyzColor color)
    {
        WriteFloat32(writer, color.X);
        WriteFloat32(writer, color.Y);
        WriteFloat32(writer, color.Z);
    }

    internal static void WriteFloat32(BigEndianWriter writer, double value)
    {
        IccNumberCodec.WriteFloat32(writer, IccNumberCodec.ConvertDoubleToFloat32(value));
    }

    internal static bool HasZeroBytesAt(System.ReadOnlyMemory<byte> payload, int offset, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (payload.Span[offset + index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int ReadUInt16At(System.ReadOnlyMemory<byte> payload, int offset)
    {
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, 2), offset);
        return reader.ReadUInt16();
    }

    internal static uint ReadUInt32At(System.ReadOnlyMemory<byte> payload, int offset)
    {
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, 4), offset);
        return reader.ReadUInt32();
    }
}
