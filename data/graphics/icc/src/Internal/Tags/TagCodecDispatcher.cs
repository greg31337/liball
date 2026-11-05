namespace icc;

internal static class TagCodecDispatcher
{
    internal static void DecodeTagPayload(TagDecodeState state)
    {
        System.ReadOnlyMemory<byte> payload = state.Source.ProfileBytes.Slice(state.PayloadOffset, state.PayloadSize);
        if (state.PayloadSize < 8)
        {
            UnknownTagCodec.DecodeMalformedPayload(state, payload);
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload, state.AbsolutePayloadOffset);
        uint typeSignature = reader.ReadUInt32();
        if (VendorTagCodec.TryDecodeRegisteredVendor(state, payload, typeSignature))
        {
            return;
        }

        bool isSupported = IsTypeSupportedByGeneration(typeSignature, state.ProfileVersion);
        string generationName = DescribeProfileGeneration(state.ProfileVersion);
        if (!isSupported)
        {
            UnknownTagCodec.DecodeFallbackPayload(state, payload, typeSignature, false, generationName);
            return;
        }

        if ((state.TagSignature == IccTagSignatures.DevicePcc || state.TagSignature == IccTagSignatures.DeviceSpectralRange)
            && !Icc2TagRegistry.ValidateTagContext(state, payload, typeSignature))
        {
            return;
        }

        if (state.TagSignature == IccTagSignatures.EmbeddedIccMaxProfile
            && !Icc1TagRegistry.ValidateTagContext(state, payload, typeSignature))
        {
            return;
        }

        if (!LegacyStructuredTagCodec.ValidateV2TagTypeMapping(state, payload, typeSignature))
        {
            return;
        }

        switch (typeSignature)
        {
            case IccTypeSignatures.UInt8Array:
            case IccTypeSignatures.UInt16Array:
            case IccTypeSignatures.UInt32Array:
            case IccTypeSignatures.UInt64Array:
                ArrayTagCodec.DecodeIntegerArray(state, payload, typeSignature);
                return;
            case IccTypeSignatures.S15Fixed16Array:
            case IccTypeSignatures.U16Fixed16Array:
                ArrayTagCodec.DecodeFixedPointArray(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Float16Array:
            case IccTypeSignatures.Float32Array:
            case IccTypeSignatures.Float64Array:
                Icc2ArrayTagCodec.DecodeFloatingPointArray(state, payload, typeSignature);
                return;
            case IccTypeSignatures.SparseMatrixArray:
                Icc2ArrayTagCodec.DecodeSparseMatrixArray(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Xyz:
                XyzTagCodec.DecodeXyzArray(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Signature:
                SignatureTagCodec.DecodeSignature(state, payload, typeSignature);
                return;
            case IccTypeSignatures.DateTime:
                DateTimeTagCodec.DecodeDateTime(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Data:
                TextTagCodec.DecodeData(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Text:
                TextTagCodec.DecodeText(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Utf8:
            case IccTypeSignatures.Utf16:
            case IccTypeSignatures.ZippedUtf8:
            case IccTypeSignatures.ZippedXml:
            case IccTypeSignatures.ZippedXmlUppercase:
                Icc2TextTagCodec.DecodeText(state, payload, typeSignature);
                return;
            case IccTypeSignatures.TextDescription:
                LegacyTagCodec.DecodeTextDescription(state, payload, typeSignature);
                return;
            case IccTypeSignatures.CrdInfo:
                LegacyStructuredTagCodec.DecodeCrdInfo(state, payload, typeSignature);
                return;
            case IccTypeSignatures.MultiLocalizedUnicode:
                LocalizedUnicodeTagCodec.DecodeMultiLocalizedUnicode(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Dictionary:
                DictionaryTagCodec.DecodeDictionary(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Curve:
                CurveTagCodec.DecodeCurve(state, payload, typeSignature);
                return;
            case IccTypeSignatures.ParametricCurve:
                CurveTagCodec.DecodeParametricCurve(state, payload, typeSignature);
                return;
            case StructuredTagProjectionCodec.SegmentedCurveType:
                Icc2ProcessingElementTagCodec.DecodeStandaloneSegmentedCurve(
                    state, payload, typeSignature);
                return;
            case IccTypeSignatures.Chromaticity:
                ChromaticityTagCodec.DecodeChromaticity(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Measurement:
                MeasurementTagCodec.DecodeMeasurement(state, payload, typeSignature);
                return;
            case IccTypeSignatures.ViewingConditions:
                ViewingConditionsTagCodec.DecodeViewingConditions(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Cicp:
                CicpTagCodec.DecodeCicp(state, payload, typeSignature);
                return;
            case IccTypeSignatures.ColorantOrder:
                ColorantOrderTagCodec.DecodeColorantOrder(state, payload, typeSignature);
                return;
            case IccTypeSignatures.ColorantTable:
                ColorantTableTagCodec.DecodeColorantTable(state, payload, typeSignature);
                return;
            case IccTypeSignatures.NamedColor:
                NamedColorTagCodec.DecodeNamedColor(state, payload, typeSignature);
                return;
            case IccTypeSignatures.NamedColor2:
                NamedColorTagCodec.DecodeNamedColor2(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Screening:
                LegacyStructuredTagCodec.DecodeScreening(state, payload, typeSignature);
                return;
            case IccTypeSignatures.UcrBg:
                LegacyStructuredTagCodec.DecodeUcrBg(state, payload, typeSignature);
                return;
            case IccTypeSignatures.DeviceSettings:
                LegacyStructuredTagCodec.DecodeDeviceSettingsRaw(state, payload, typeSignature);
                return;
            case IccTypeSignatures.EmbeddedProfile:
                EmbeddedProfileTagCodec.DecodeEmbeddedProfile(state, payload, typeSignature);
                return;
            case IccTypeSignatures.ProfileSequenceDescription:
                ProfileSequenceTagCodec.DecodeProfileSequenceDescription(state, payload, typeSignature);
                return;
            case IccTypeSignatures.ProfileSequenceIdentifier:
                ProfileSequenceTagCodec.DecodeProfileSequenceIdentifier(state, payload, typeSignature);
                return;
            case IccTypeSignatures.ResponseCurveSet16:
                ResponseCurveTagCodec.DecodeResponseCurveSet16(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Lut8:
                LutTagCodec.DecodeLut8(state, payload, typeSignature);
                return;
            case IccTypeSignatures.Lut16:
                LutTagCodec.DecodeLut16(state, payload, typeSignature);
                return;
            case IccTypeSignatures.LutAToB:
                MultiStageLutTagCodec.DecodeLutAToB(state, payload, typeSignature);
                return;
            case IccTypeSignatures.LutBToA:
                MultiStageLutTagCodec.DecodeLutBToA(state, payload, typeSignature);
                return;
            case IccTypeSignatures.MultiProcessElements:
                MultiProcessElementsTagCodec.DecodeMultiProcessElements(state, payload, typeSignature);
                return;
            case IccTypeSignatures.AdaptiveGainCurve:
                AdaptiveGainCurveTagCodec.DecodeAdaptiveGainCurve(state, payload, typeSignature);
                return;
            case IccTypeSignatures.SpectralViewingConditions:
                SpectralViewingConditionsTagCodec.DecodeSpectralViewingConditions(state, payload, typeSignature);
                return;
            case IccTypeSignatures.SpectralRange:
                SpectralRangeTagCodec.DecodeSpectralRange(state, payload, typeSignature);
                return;
            case IccTypeSignatures.GamutBoundaryDescription:
                GamutBoundaryTagCodec.DecodeGamutBoundary(state, payload, typeSignature);
                return;
            case IccTypeSignatures.EmbeddedHeightImage:
            case IccTypeSignatures.EmbeddedNormalImage:
                EmbeddedImageTagCodec.DecodeEmbeddedImage(state, payload, typeSignature);
                return;
            case IccTypeSignatures.TagArray:
                HierarchicalTagCodec.DecodeTagArray(state, payload, typeSignature);
                return;
            case IccTypeSignatures.TagStructure:
                HierarchicalTagCodec.DecodeTagStructure(state, payload, typeSignature);
                return;
            default:
                break;
        }

        UnknownTagCodec.DecodeFallbackPayload(state, payload, typeSignature, isSupported, generationName);
    }

    internal static bool IsTypeSupportedByGeneration(uint typeSignature, IccVersionKind profileVersion)
    {
        switch (profileVersion)
        {
            case IccVersionKind.Version2:
                return IsVersion2Type(typeSignature);
            case IccVersionKind.Version4:
                return IsVersion4Type(typeSignature);
            case IccVersionKind.Version5:
                return IsVersion5Type(typeSignature);
            default:
                return false;
        }
    }

    internal static bool IsVersion2Type(uint typeSignature)
    {
        switch (typeSignature)
        {
            case IccTypeSignatures.Chromaticity:
            case IccTypeSignatures.ColorantOrder:
            case IccTypeSignatures.ColorantTable:
            case IccTypeSignatures.Curve:
            case IccTypeSignatures.Data:
            case IccTypeSignatures.DateTime:
            case IccTypeSignatures.Dictionary:
            case IccTypeSignatures.Lut8:
            case IccTypeSignatures.Lut16:
            case IccTypeSignatures.Measurement:
            case IccTypeSignatures.NamedColor2:
            case IccTypeSignatures.ProfileSequenceDescription:
            case IccTypeSignatures.ResponseCurveSet16:
            case IccTypeSignatures.S15Fixed16Array:
            case IccTypeSignatures.Signature:
            case IccTypeSignatures.Text:
            case IccTypeSignatures.U16Fixed16Array:
            case IccTypeSignatures.UInt8Array:
            case IccTypeSignatures.UInt16Array:
            case IccTypeSignatures.UInt32Array:
            case IccTypeSignatures.UInt64Array:
            case IccTypeSignatures.ViewingConditions:
            case IccTypeSignatures.Xyz:
            case IccTypeSignatures.TextDescription:
            case IccTypeSignatures.CrdInfo:
            case IccTypeSignatures.NamedColor:
            case IccTypeSignatures.Screening:
            case IccTypeSignatures.UcrBg:
            case IccTypeSignatures.DeviceSettings:
            case IccTypeSignatures.EmbeddedProfile:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsVersion4Type(uint typeSignature)
    {
        switch (typeSignature)
        {
            case IccTypeSignatures.Chromaticity:
            case IccTypeSignatures.Cicp:
            case IccTypeSignatures.ColorantOrder:
            case IccTypeSignatures.ColorantTable:
            case IccTypeSignatures.Curve:
            case IccTypeSignatures.Data:
            case IccTypeSignatures.DateTime:
            case IccTypeSignatures.Dictionary:
            case IccTypeSignatures.Lut8:
            case IccTypeSignatures.Lut16:
            case IccTypeSignatures.LutAToB:
            case IccTypeSignatures.LutBToA:
            case IccTypeSignatures.Measurement:
            case IccTypeSignatures.MultiLocalizedUnicode:
            case IccTypeSignatures.MultiProcessElements:
            case IccTypeSignatures.NamedColor2:
            case IccTypeSignatures.ParametricCurve:
            case IccTypeSignatures.ProfileSequenceDescription:
            case IccTypeSignatures.ProfileSequenceIdentifier:
            case IccTypeSignatures.ResponseCurveSet16:
            case IccTypeSignatures.S15Fixed16Array:
            case IccTypeSignatures.Signature:
            case IccTypeSignatures.Text:
            case IccTypeSignatures.U16Fixed16Array:
            case IccTypeSignatures.UInt8Array:
            case IccTypeSignatures.UInt16Array:
            case IccTypeSignatures.UInt32Array:
            case IccTypeSignatures.UInt64Array:
            case IccTypeSignatures.ViewingConditions:
            case IccTypeSignatures.Xyz:
            case IccTypeSignatures.EmbeddedProfile:
            case IccTypeSignatures.AdaptiveGainCurve:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsVersion5Type(uint typeSignature)
    {
        switch (typeSignature)
        {
            case IccTypeSignatures.Cicp:
            case IccTypeSignatures.ColorantOrder:
            case IccTypeSignatures.Curve:
            case IccTypeSignatures.Data:
            case IccTypeSignatures.DateTime:
            case IccTypeSignatures.Dictionary:
            case IccTypeSignatures.LutAToB:
            case IccTypeSignatures.LutBToA:
            case IccTypeSignatures.Measurement:
            case IccTypeSignatures.MultiLocalizedUnicode:
            case IccTypeSignatures.MultiProcessElements:
            case IccTypeSignatures.ParametricCurve:
            case IccTypeSignatures.S15Fixed16Array:
            case IccTypeSignatures.Signature:
            case IccTypeSignatures.Text:
            case IccTypeSignatures.U16Fixed16Array:
            case IccTypeSignatures.UInt8Array:
            case IccTypeSignatures.UInt16Array:
            case IccTypeSignatures.UInt32Array:
            case IccTypeSignatures.UInt64Array:
            case IccTypeSignatures.Xyz:
            case IccTypeSignatures.Float16Array:
            case IccTypeSignatures.Float32Array:
            case IccTypeSignatures.Float64Array:
            case IccTypeSignatures.GamutBoundaryDescription:
            case IccTypeSignatures.SparseMatrixArray:
            case IccTypeSignatures.SpectralViewingConditions:
            case IccTypeSignatures.SpectralRange:
            case IccTypeSignatures.TagArray:
            case IccTypeSignatures.TagStructure:
            case IccTypeSignatures.Utf8:
            case IccTypeSignatures.Utf16:
            case IccTypeSignatures.ZippedUtf8:
            case IccTypeSignatures.ZippedXml:
            case IccTypeSignatures.ZippedXmlUppercase:
            case IccTypeSignatures.EmbeddedHeightImage:
            case IccTypeSignatures.EmbeddedNormalImage:
            case StructuredTagProjectionCodec.SegmentedCurveType:
                return true;
            default:
                return false;
        }
    }

    internal static string DescribeProfileGeneration(IccVersionKind profileVersion)
    {
        switch (profileVersion)
        {
            case IccVersionKind.Version2:
                return "ICC.1 version 2";
            case IccVersionKind.Version4:
                return "ICC.1 version 4";
            case IccVersionKind.Version5:
                return "ICC.2 version 5";
            default:
                return "an unknown profile generation";
        }
    }
}
