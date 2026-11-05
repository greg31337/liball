namespace icc;

internal static class VendorTagCodec
{
    internal static bool TryDecodeRegisteredVendor(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (state.HasProfileContext && state.TagSignature == IccTagSignatures.VideoCardGammaTable)
        {
            DecodeVideoCardGamma(state, payload, typeSignature);
            return true;
        }

        if (state.HasProfileContext && state.TagSignature == IccTagSignatures.MicrosoftHdrCalibration)
        {
            DecodeMicrosoftHardwareCalibration(state, payload, typeSignature);
            return true;
        }

        if (typeSignature == IccTypeSignatures.VideoCardGammaTable)
        {
            DecodeVideoCardGamma(state, payload, typeSignature);
            return true;
        }

        if (typeSignature == IccTypeSignatures.MicrosoftHardwareCalibration)
        {
            DecodeMicrosoftHardwareCalibration(state, payload, typeSignature);
            return true;
        }

        if (state.HasProfileContext && IsMicrosoftPrivateSignature(state.TagSignature))
        {
            DecodeMicrosoftPrivateRaw(state, payload, typeSignature);
            return true;
        }

        return false;
    }

    internal static bool IsMicrosoftPrivateSignature(uint tagSignature)
    {
        uint prefix = tagSignature >> 16;
        uint firstDigit = (tagSignature >> 8) & 255;
        uint secondDigit = tagSignature & 255;
        return prefix == 0x4d53 && firstDigit >= 0x30 && firstDigit <= 0x39 && secondDigit >= 0x30 && secondDigit <= 0x39;
    }

    internal static void DecodeVideoCardGamma(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (typeSignature != IccTypeSignatures.VideoCardGammaTable)
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Apple vcgt tag requires a vcgt payload type signature.");
            return;
        }

        if (payload.Length < 12)
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Apple vcgt payload is shorter than its 12-byte header.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Apple vcgt payload reserved bytes must be zero.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(8), state.AbsolutePayloadOffset + 8);
        uint representation = reader.ReadUInt32();
        if (representation == 0)
        {
            DecodeVideoCardGammaTable(state, payload, typeSignature);
            return;
        }

        if (representation == 1)
        {
            DecodeVideoCardGammaFormula(state, payload, typeSignature);
            return;
        }

        CompleteInvalidVendor(state, payload, typeSignature, $"The Apple vcgt representation value {representation} is not the registered table or formula form.");
    }

    internal static void DecodeMicrosoftHardwareCalibration(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (typeSignature != IccTypeSignatures.MicrosoftHardwareCalibration)
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Microsoft MHC2 tag requires an MHC2 payload type signature.");
            return;
        }

        if (payload.Length < 36)
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Microsoft MHC2 payload is shorter than its 36-byte header.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Microsoft MHC2 payload reserved bytes must be zero.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(8, 28), state.AbsolutePayloadOffset + 8);
        uint encodedLutCount = reader.ReadUInt32();
        double minimumLuminance = IccNumberCodec.ReadS15Fixed16(reader);
        double peakLuminance = IccNumberCodec.ReadS15Fixed16(reader);
        uint matrixOffset = reader.ReadUInt32();
        uint redOffset = reader.ReadUInt32();
        uint greenOffset = reader.ReadUInt32();
        uint blueOffset = reader.ReadUInt32();

        if (encodedLutCount > 4096)
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Microsoft MHC2 one-dimensional LUT count exceeds 4096 entries.");
            return;
        }

        if (minimumLuminance < 0.0 || peakLuminance <= 0.0 || minimumLuminance > peakLuminance)
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Microsoft MHC2 minimum and peak luminance values are invalid.");
            return;
        }

        int lutCount = (int)encodedLutCount;
        int lutSize = 8 + (lutCount * 4);
        if (lutCount == 0)
        {
            if (redOffset != 0 || greenOffset != 0 || blueOffset != 0)
            {
                CompleteInvalidVendor(state, payload, typeSignature, "An identity Microsoft MHC2 one-dimensional LUT must use zero RGB offsets.");
                return;
            }
        }
        else
        {
            if (!IsVendorBlockValid(redOffset, lutSize, payload.Length) || !IsVendorBlockValid(greenOffset, lutSize, payload.Length) || !IsVendorBlockValid(blueOffset, lutSize, payload.Length))
            {
                CompleteInvalidVendor(state, payload, typeSignature, "A Microsoft MHC2 RGB LUT offset is unaligned, inside the header, or outside the payload.");
                return;
            }

            if (!HasMhcLutHeader(payload, redOffset) || !HasMhcLutHeader(payload, greenOffset) || !HasMhcLutHeader(payload, blueOffset))
            {
                CompleteInvalidVendor(state, payload, typeSignature, "Every Microsoft MHC2 RGB LUT must have an sf32 signature and zero reserved bytes.");
                return;
            }
        }

        if (matrixOffset != 0 && !IsVendorBlockValid(matrixOffset, 48, payload.Length))
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Microsoft MHC2 matrix offset is unaligned, inside the header, or outside the payload.");
            return;
        }

        if (matrixOffset != 0 && lutCount > 0)
        {
            if (VendorBlocksOverlap(matrixOffset, 48, redOffset, lutSize) || VendorBlocksOverlap(matrixOffset, 48, greenOffset, lutSize) || VendorBlocksOverlap(matrixOffset, 48, blueOffset, lutSize))
            {
                CompleteInvalidVendor(state, payload, typeSignature, "The Microsoft MHC2 matrix overlaps an RGB LUT.");
                return;
            }
        }

        if (lutCount > 0)
        {
            if (DistinctVendorBlocksOverlap(redOffset, greenOffset, lutSize) || DistinctVendorBlocksOverlap(redOffset, blueOffset, lutSize) || DistinctVendorBlocksOverlap(greenOffset, blueOffset, lutSize))
            {
                CompleteInvalidVendor(state, payload, typeSignature, "Distinct Microsoft MHC2 RGB LUT blocks overlap.");
                return;
            }
        }

        IccMicrosoftHardwareCalibration calibration = new IccMicrosoftHardwareCalibration();
        calibration.LutEntryCount = lutCount;
        calibration.MinimumLuminance = minimumLuminance;
        calibration.PeakLuminance = peakLuminance;
        if (matrixOffset != 0)
        {
            calibration.Matrix = ReadFixedValues(payload, (int)matrixOffset, 12, state.AbsolutePayloadOffset);
        }

        if (lutCount > 0)
        {
            calibration.RedLut = ReadFixedValues(payload, (int)redOffset + 8, lutCount, state.AbsolutePayloadOffset);
            calibration.GreenLut = ReadFixedValues(payload, (int)greenOffset + 8, lutCount, state.AbsolutePayloadOffset);
            calibration.BlueLut = ReadFixedValues(payload, (int)blueOffset + 8, lutCount, state.AbsolutePayloadOffset);
            if (!AreMhcLutValuesValid(calibration.RedLut) || !AreMhcLutValuesValid(calibration.GreenLut) || !AreMhcLutValuesValid(calibration.BlueLut))
            {
                CompleteInvalidVendor(state, payload, typeSignature, "Microsoft MHC2 LUT values must be normalized to the inclusive range from zero to one.");
                return;
            }
        }

        IccVendorTagData vendor = CreateVendorDescription("Microsoft", "Microsoft Hardware Calibration 2", state.TagSignature, typeSignature);
        vendor.MicrosoftHardwareCalibration = calibration;
        CompleteRecognizedVendor(state, payload, typeSignature, vendor, "Recognized and decoded Microsoft MHC2 hardware-calibration metadata; the private payload was retained losslessly as raw data.");
    }

    internal static byte[] SerializeVideoCardGammaRaw(IccRawData rawData)
    {
        return SerializeValidatedVendorRaw(rawData, IccTagSignatures.VideoCardGammaTable, IccTypeSignatures.VideoCardGammaTable, "Apple vcgt");
    }

    internal static byte[] SerializeMicrosoftHardwareCalibrationRaw(IccRawData rawData)
    {
        return SerializeValidatedVendorRaw(rawData, IccTagSignatures.MicrosoftHdrCalibration, IccTypeSignatures.MicrosoftHardwareCalibration, "Microsoft MHC2");
    }

    internal static byte[] SerializeRegisteredVendorRaw(IccRawData rawData, uint tagSignature)
    {
        if (!IsMicrosoftPrivateSignature(tagSignature))
        {
            throw new IccProfileWriteException("The requested tag signature is not in the registered Microsoft MS00 through MS99 private range.");
        }

        return SerializeValidatedVendorRaw(rawData, tagSignature, 0, "registered Microsoft private");
    }

    internal static void DecodeVideoCardGammaTable(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 18)
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Apple vcgt table payload is shorter than its 18-byte header.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(12, 6), state.AbsolutePayloadOffset + 12);
        int channels = reader.ReadUInt16();
        int entryCount = reader.ReadUInt16();
        int entrySize = reader.ReadUInt16();
        if ((channels != 1 && channels != 3) || entryCount == 0 || (entrySize != 1 && entrySize != 2))
        {
            CompleteInvalidVendor(state, payload, typeSignature, "An Apple vcgt table requires one or three channels, at least one entry, and one- or two-byte entries.");
            return;
        }

        long dataLength = (long)channels * entryCount * entrySize;
        if (dataLength > int.MaxValue || dataLength != payload.Length - 18)
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Apple vcgt table dimensions do not exactly match the encoded payload size.");
            return;
        }

        IccVideoCardGamma gamma = new IccVideoCardGamma();
        gamma.ChannelCount = channels;
        gamma.EntryCount = entryCount;
        gamma.EntrySize = entrySize;
        gamma.TableData = payload.Slice(18, (int)dataLength).ToArray();
        IccVendorTagData vendor = CreateVendorDescription("Apple", "Video Card Gamma Table", state.TagSignature, typeSignature);
        vendor.VideoCardGamma = gamma;
        CompleteRecognizedVendor(state, payload, typeSignature, vendor, "Recognized and decoded an Apple vcgt table; the private payload was retained losslessly as raw data.");
    }

    internal static void DecodeVideoCardGammaFormula(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length != 48)
        {
            CompleteInvalidVendor(state, payload, typeSignature, "The Apple vcgt formula representation requires exactly 48 bytes.");
            return;
        }

        double[] values = ReadFixedValues(payload, 12, 9, state.AbsolutePayloadOffset);
        if (!IsVcgtFormulaChannelValid(values, 0) || !IsVcgtFormulaChannelValid(values, 3) || !IsVcgtFormulaChannelValid(values, 6))
        {
            CompleteInvalidVendor(state, payload, typeSignature, "Every Apple vcgt formula channel requires positive gamma and ordered minimum and maximum values between zero and one.");
            return;
        }

        IccVideoCardGamma gamma = new IccVideoCardGamma();
        gamma.IsFormula = true;
        gamma.RedFormula = CopyThreeValues(values, 0);
        gamma.GreenFormula = CopyThreeValues(values, 3);
        gamma.BlueFormula = CopyThreeValues(values, 6);
        IccVendorTagData vendor = CreateVendorDescription("Apple", "Video Card Gamma Formula", state.TagSignature, typeSignature);
        vendor.VideoCardGamma = gamma;
        CompleteRecognizedVendor(state, payload, typeSignature, vendor, "Recognized and decoded an Apple vcgt formula; the private payload was retained losslessly as raw data.");
    }

    internal static void DecodeMicrosoftPrivateRaw(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        string formatName = "Registered Microsoft Private Payload";
        if (state.TagSignature == IccTagSignatures.MicrosoftGammaRamp)
        {
            formatName = "Windows Gamma Ramp";
        }

        IccVendorTagData vendor = CreateVendorDescription("Microsoft", formatName, state.TagSignature, typeSignature);
        CompleteRecognizedVendor(state, payload, typeSignature, vendor, "Recognized a tag in the registered Microsoft MS00 through MS99 private range; its payload was retained losslessly as raw data.");
    }

    internal static IccVendorTagData CreateVendorDescription(string vendorName, string formatName, uint tagSignature, uint typeSignature)
    {
        IccVendorTagData vendor = new IccVendorTagData();
        vendor.VendorName = vendorName;
        vendor.FormatName = formatName;
        vendor.TagSignature = tagSignature;
        vendor.TypeSignature = typeSignature;
        return vendor;
    }

    internal static void CompleteRecognizedVendor(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, IccVendorTagData vendor, string message)
    {
        IccTagData data = new IccTagData();
        UnknownTagCodec.PopulateRawFallback(data, payload, typeSignature, true);
        data.VendorData = vendor;
        IccOperationResult result = UnknownTagCodec.CreateDecodeResult(state, IccOperationStatus.Success);
        result.Message = message;
        UnknownTagCodec.CompleteDecodeState(state, data, result);
    }

    internal static void CompleteInvalidVendor(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, string message)
    {
        IccTagData data = new IccTagData();
        UnknownTagCodec.PopulateRawFallback(data, payload, typeSignature, true);
        IccOperationResult result = UnknownTagCodec.CreateDecodeResult(state, IccOperationStatus.InvalidData);
        result.Message = message;
        UnknownTagCodec.CompleteDecodeState(state, data, result);
    }

    internal static bool IsVendorBlockValid(uint offset, int size, int payloadLength)
    {
        if (offset < 36 || offset > int.MaxValue || (offset & 3) != 0)
        {
            return false;
        }

        int position = (int)offset;
        return size >= 0 && position <= payloadLength && size <= payloadLength - position;
    }

    internal static bool HasMhcLutHeader(System.ReadOnlyMemory<byte> payload, uint offset)
    {
        int position = (int)offset;
        BigEndianReader reader = new BigEndianReader(payload.Slice(position, 8), position);
        return reader.ReadUInt32() == IccTypeSignatures.S15Fixed16Array && reader.ReadUInt32() == 0;
    }

    internal static bool VendorBlocksOverlap(uint firstOffset, int firstSize, uint secondOffset, int secondSize)
    {
        long firstEnd = firstOffset + firstSize;
        long secondEnd = secondOffset + secondSize;
        return firstOffset < secondEnd && secondOffset < firstEnd;
    }

    internal static bool DistinctVendorBlocksOverlap(uint firstOffset, uint secondOffset, int size)
    {
        return firstOffset != secondOffset && VendorBlocksOverlap(firstOffset, size, secondOffset, size);
    }

    internal static double[] ReadFixedValues(System.ReadOnlyMemory<byte> payload, int offset, int count, long absolutePayloadOffset)
    {
        double[] values = new double[count];
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, count * 4), absolutePayloadOffset + offset);
        int index = 0;
        while (index < values.Length)
        {
            values[index] = IccNumberCodec.ReadS15Fixed16(reader);
            index = index + 1;
        }

        return values;
    }

    internal static bool AreMhcLutValuesValid(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] < 0.0 || values[index] > 1.0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsVcgtFormulaChannelValid(double[] values, int offset)
    {
        double gamma = values[offset];
        double minimum = values[offset + 1];
        double maximum = values[offset + 2];
        return gamma > 0.0 && minimum >= 0.0 && maximum <= 1.0 && minimum <= maximum;
    }

    internal static double[] CopyThreeValues(double[] source, int offset)
    {
        double[] values = new double[3];
        values[0] = source[offset];
        values[1] = source[offset + 1];
        values[2] = source[offset + 2];
        return values;
    }

    internal static byte[] SerializeValidatedVendorRaw(IccRawData rawData, uint tagSignature, uint expectedTypeSignature, string description)
    {
        if (rawData == null || rawData.Bytes == null || rawData.Bytes.Length < 8)
        {
            throw new IccProfileWriteException($"Raw {description} data must contain a complete payload header.");
        }

        System.ReadOnlyMemory<byte> payload = new System.ReadOnlyMemory<byte>(rawData.Bytes);
        BigEndianReader reader = new BigEndianReader(payload, 0);
        uint encodedTypeSignature = reader.ReadUInt32();
        if (rawData.HasTypeSignature && rawData.TypeSignature != encodedTypeSignature)
        {
            throw new IccProfileWriteException($"Raw {description} metadata does not match its encoded type signature.");
        }

        if (expectedTypeSignature != 0 && encodedTypeSignature != expectedTypeSignature)
        {
            throw new IccProfileWriteException($"Raw {description} data has the wrong encoded type signature.");
        }

        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, IccVersionKind.Version4, true);
        state.HasProfileContext = true;
        state.TagSignature = tagSignature;
        if (!TryDecodeRegisteredVendor(state, payload, encodedTypeSignature) || state.Result.Status != IccOperationStatus.Success)
        {
            string detail = state.Result.Message;
            if (detail.Length == 0)
            {
                detail = $"Raw {description} data is not a recognized vendor payload.";
            }

            throw new IccProfileWriteException(detail);
        }

        byte[] clone = new byte[rawData.Bytes.Length];
        System.Array.Copy(rawData.Bytes, clone, clone.Length);
        return clone;
    }
}
