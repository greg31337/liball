namespace icc;

internal static class Icc1TagValidationCore
{
    internal static IccValidationIssue[] Validate(IccProfile profile, IccValidationPolicy policy)
    {
        System.Collections.Generic.List<IccValidationIssue> issues =
            new System.Collections.Generic.List<IccValidationIssue>();
        if (profile.Header == null || profile.Header.Version == null || profile.Tags == null)
        {
            return issues.ToArray();
        }

        IccVersionKind version = profile.Header.Version.Kind;
        if (version != IccVersionKind.Version2 && version != IccVersionKind.Version4)
        {
            return issues.ToArray();
        }

        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Data != null && tag.Data.Kind != IccTagDataKind.Unknown
                && tag.Data.Kind != IccTagDataKind.Raw)
            {
                ValidateDecodedTag(profile, tag, index, version, issues);
            }
            index = index + 1;
        }
        return issues.ToArray();
    }

    internal static IccOperationResult? DecodeRetainedPayload(IccProfile profile, IccTag tag,
        IccValidationPolicy policy)
    {
        if (profile.Header == null || profile.Header.Version == null)
        {
            return null;
        }
        IccVersionKind version = profile.Header.Version.Kind;
        if (version != IccVersionKind.Version2 && version != IccVersionKind.Version4
            && version != IccVersionKind.Version5)
        {
            return null;
        }

        System.ReadOnlyMemory<byte> payload = TagPayloadValidationCore.ResolvePayload(tag);
        if (payload.Length == 0)
        {
            return null;
        }

        long absoluteOffset = tag.TableEntry == null ? 0 : tag.TableEntry.Offset;
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0,
            payload.Length, absoluteOffset, version, true);
        PopulateDecodeContext(state, profile, tag, policy);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state.Result;
    }

    internal static void PopulateDecodeContext(TagDecodeState state, IccProfile profile,
        IccTag tag, IccValidationPolicy policy)
    {
        IccProfileHeader header = profile.Header;
        state.MaximumDecompressedBytes = policy.ResourceLimits.MaximumDecompressedBytes;
        state.MaximumProfileBytes = policy.ResourceLimits.MaximumProfileBytes;
        state.MaximumTagCount = policy.ResourceLimits.MaximumTagCount;
        state.MaximumTagBytes = policy.ResourceLimits.MaximumTagBytes;
        state.MaximumTotalTagBytes = policy.ResourceLimits.MaximumTotalTagBytes;
        state.MaximumDiagnosticCount = policy.ResourceLimits.MaximumDiagnosticCount;
        state.MaximumNestingDepth = policy.ResourceLimits.MaximumNestingDepth;
        state.MaximumCalculatorInstructions = policy.ResourceLimits.MaximumCalculatorInstructions;
        state.MaximumCalculatorStackDepth = policy.ResourceLimits.MaximumCalculatorStackDepth;
        state.MaximumCalculatorStorageValues = policy.ResourceLimits.MaximumCalculatorStorageValues;
        state.HasProfileContext = true;
        state.TagSignature = tag.Signature;
        state.ProfileClass = header.ProfileClass;
        state.ProfileClassSignature = header.ProfileClassSignature;
        state.DataColorSpace = header.DataColorSpace;
        state.DataColorSpaceSignature = header.DataColorSpaceSignature;
        state.ProfileConnectionSpace = header.ProfileConnectionSpace;
        state.ProfileConnectionSpaceSignature = header.ProfileConnectionSpaceSignature;
        state.SpectralPcsSignature = header.SpectralPcsSignature;
        state.MaterialConnectionSpace = header.MaterialConnectionSpace;
        state.MaterialConnectionSpaceSignature = header.MaterialConnectionSpaceSignature;
        state.HasMatrixPrimaries = profile.TagTableEntries != null
            && TagTableCodec.HasMatrixPrimaries(profile.TagTableEntries);
        state.IsEmbedded = header.IsEmbedded;
        state.RequiresEmbeddedData = header.RequiresEmbeddedData;
    }

    internal static void ValidateDecodedTag(IccProfile profile, IccTag tag, int index,
        IccVersionKind version,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccTagData data = tag.Data;
        uint typeSignature = ResolveTypeSignature(tag);
        ValidateRequiredRepresentation(data, typeSignature, tag, index, issues);
        switch (data.Kind)
        {
            case IccTagDataKind.IntegerArray:
                ValidateIntegerValues(data, typeSignature, tag, index, issues);
                break;
            case IccTagDataKind.FixedPointArray:
                ValidateFixedValues(data, typeSignature, tag, index, issues);
                break;
            case IccTagDataKind.XyzArray:
                ValidateXyzValues(data, tag, index, issues);
                break;
            case IccTagDataKind.DateTime:
                ValidateDateTimeValue(data, tag, index, issues);
                break;
            case IccTagDataKind.Text:
                ValidateTextValue(data, typeSignature, tag, index, issues);
                break;
            case IccTagDataKind.LocalizedText:
                ValidateLocalizedValues(data, tag, index, issues);
                break;
            case IccTagDataKind.Dictionary:
                ValidateDictionaryValues(data, tag, index, issues);
                break;
            case IccTagDataKind.Curve:
                ValidateCurveValue(data.Curve, tag, index, "Curve", issues);
                break;
            case IccTagDataKind.Lut:
                ValidateLutValue(data, typeSignature, tag, index, issues);
                break;
            case IccTagDataKind.MultiProcessElements:
                ValidateMpeValue(data.MultiProcessElements, tag, index, issues);
                break;
            case IccTagDataKind.Measurement:
                ValidateMeasurementValue(data.Measurement, tag, index, issues);
                break;
            case IccTagDataKind.ViewingConditions:
                ValidateViewingValue(data.ViewingConditions, tag, index, issues);
                break;
            case IccTagDataKind.Chromaticity:
                ValidateChromaticityValue(data.Chromaticity, version, tag, index, issues);
                break;
            case IccTagDataKind.Cicp:
                ValidateCicpValue(data.Cicp, tag, index, issues);
                break;
            case IccTagDataKind.NamedColors:
                ValidateNamedColorValue(data.NamedColors, typeSignature,
                    profile.Header.ProfileConnectionSpace, tag, index, issues);
                break;
            case IccTagDataKind.Colorants:
                ValidateColorantValue(data.Colorants,
                    profile.Header.ProfileConnectionSpace, tag, index, issues);
                break;
            case IccTagDataKind.ResponseCurves:
                ValidateResponseValue(data, tag, index, issues);
                break;
            case IccTagDataKind.ProfileSequence:
                ValidateProfileSequenceValue(data, typeSignature, version, tag, index, issues);
                break;
            case IccTagDataKind.AdaptiveGainCurve:
                ValidateAdaptiveValue(data.AdaptiveGainCurve, tag, index, issues);
                break;
            case IccTagDataKind.CrdInfo:
                ValidateCrdInfoValue(data.CrdInfo, tag, index, issues);
                break;
            case IccTagDataKind.Screening:
                ValidateScreeningValue(data.Screening, tag, index, issues);
                break;
            case IccTagDataKind.UcrBg:
                ValidateUcrBgValue(data.UcrBg, tag, index, issues);
                break;
            default:
                break;
        }
    }

    internal static void ValidateRequiredRepresentation(IccTagData data, uint typeSignature,
        IccTag tag, int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool missing = false;
        switch (data.Kind)
        {
            case IccTagDataKind.IntegerArray:
                missing = data.IntegerValues == null;
                break;
            case IccTagDataKind.FixedPointArray:
                missing = data.FixedPointValues == null;
                break;
            case IccTagDataKind.XyzArray:
                missing = data.XyzValues == null;
                break;
            case IccTagDataKind.DateTime:
                missing = data.DateTime == null;
                break;
            case IccTagDataKind.Binary:
                missing = data.BinaryData == null;
                break;
            case IccTagDataKind.Text:
                missing = data.Text == null;
                break;
            case IccTagDataKind.LocalizedText:
                missing = data.LocalizedStrings == null;
                break;
            case IccTagDataKind.Dictionary:
                missing = data.Metadata == null;
                break;
            case IccTagDataKind.Curve:
                missing = data.Curve == null;
                break;
            case IccTagDataKind.Lut:
                missing = typeSignature == IccTypeSignatures.LutAToB
                    || typeSignature == IccTypeSignatures.LutBToA
                    ? data.MultiStageLut == null : data.Lut == null;
                break;
            case IccTagDataKind.MultiProcessElements:
                missing = data.MultiProcessElements == null;
                break;
            case IccTagDataKind.Measurement:
                missing = data.Measurement == null;
                break;
            case IccTagDataKind.ViewingConditions:
                missing = data.ViewingConditions == null;
                break;
            case IccTagDataKind.Chromaticity:
                missing = data.Chromaticity == null;
                break;
            case IccTagDataKind.Cicp:
                missing = data.Cicp == null;
                break;
            case IccTagDataKind.NamedColors:
                missing = data.NamedColors == null;
                break;
            case IccTagDataKind.Colorants:
                missing = data.Colorants == null;
                break;
            case IccTagDataKind.ResponseCurves:
                missing = data.ResponseCurves == null;
                break;
            case IccTagDataKind.ProfileSequence:
                missing = typeSignature == IccTypeSignatures.ProfileSequenceIdentifier
                    ? data.ProfileSequenceIdentifiers == null : data.ProfileSequence == null;
                break;
            case IccTagDataKind.AdaptiveGainCurve:
                missing = data.AdaptiveGainCurve == null;
                break;
            case IccTagDataKind.CrdInfo:
                missing = data.CrdInfo == null;
                break;
            case IccTagDataKind.Screening:
                missing = data.Screening == null;
                break;
            case IccTagDataKind.UcrBg:
                missing = data.UcrBg == null;
                break;
            case IccTagDataKind.EmbeddedProfile:
                missing = data.EmbeddedProfile == null;
                break;
            default:
                break;
        }

        if (missing)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "The active ICC.1 tag-data representation is missing one or more required child values.",
                tag, index, "Data",
                "Populate every array and child object required by the active tag-data kind."));
        }
    }

    internal static uint ResolveTypeSignature(IccTag tag)
    {
        if (tag.Data.RawData != null && tag.Data.RawData.HasTypeSignature)
        {
            return tag.Data.RawData.TypeSignature;
        }
        System.ReadOnlyMemory<byte> payload = TagPayloadValidationCore.ResolvePayload(tag);
        if (payload.Length >= 4)
        {
            return TagPayloadValidationCore.ReadUInt32(payload, 0);
        }
        return 0;
    }

    internal static void ValidateIntegerValues(IccTagData data, uint typeSignature, IccTag tag,
        int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (data.IntegerValues == null)
        {
            return;
        }
        ulong maximum = typeSignature == IccTypeSignatures.UInt8Array ? byte.MaxValue
            : typeSignature == IccTypeSignatures.UInt16Array ? ushort.MaxValue
            : typeSignature == IccTypeSignatures.UInt32Array ? uint.MaxValue
            : ulong.MaxValue;
        bool invalid = false;
        int valueIndex = 0;
        while (valueIndex < data.IntegerValues.Length)
        {
            long value = data.IntegerValues[valueIndex];
            if (value < 0 || (maximum != ulong.MaxValue && (ulong)value > maximum))
            {
                invalid = true;
            }
            valueIndex = valueIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "An unsigned integer-array value is outside the range of its encoded element width.",
                tag, index, "Data.IntegerValues",
                "Keep every value within the unsigned range selected by the tag type."));
        }
    }

    internal static void ValidateFixedValues(IccTagData data, uint typeSignature, IccTag tag,
        int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (data.FixedPointValues == null)
        {
            return;
        }
        double minimum = typeSignature == IccTypeSignatures.U16Fixed16Array
            ? 0.0 : IccNumberCodec.S15Fixed16Minimum;
        double maximum = typeSignature == IccTypeSignatures.U16Fixed16Array
            ? IccNumberCodec.U16Fixed16Maximum : IccNumberCodec.S15Fixed16Maximum;
        if (!AreValuesInRange(data.FixedPointValues, minimum, maximum))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "A fixed-point-array value is non-finite or outside its encoded numeric range.",
                tag, index, "Data.FixedPointValues",
                "Keep every value within the finite range of the selected ICC fixed-point encoding."));
        }
    }

    internal static void ValidateXyzValues(IccTagData data, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (data.XyzValues == null)
        {
            return;
        }
        bool invalid = false;
        int valueIndex = 0;
        while (valueIndex < data.XyzValues.Length)
        {
            IccXyzNumber value = data.XyzValues[valueIndex];
            if (value == null || !IsFixedXyz(value.X, value.Y, value.Z))
            {
                invalid = true;
            }
            valueIndex = valueIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "An XYZNumber is missing, non-finite, or outside the signed 15.16 range.",
                tag, index, "Data.XyzValues",
                "Provide complete XYZ values representable as signed 15.16 numbers."));
        }
    }

    internal static void ValidateDateTimeValue(IccTagData data, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (data.DateTime != null && !DateTimeTagCodec.IsValidDateTime(data.DateTime))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "The dateTimeType value contains a component outside the ICC date/time ranges.",
                tag, index, "Data.DateTime",
                "Use a valid Gregorian date and time with hour, minute, and second in range."));
        }
    }

    internal static void ValidateTextValue(IccTagData data, uint typeSignature, IccTag tag,
        int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccTextData? text = data.Text;
        bool invalid = text != null && (text.Text == null || text.Bytes == null
            || !Icc2TextTagCodec.IsValidUnicodeString(text.Text));
        if (!invalid && text != null && typeSignature == IccTypeSignatures.Text)
        {
            invalid = !IsNonNullAscii(text.Text);
        }
        if (!invalid && text != null && typeSignature == IccTypeSignatures.TextDescription)
        {
            invalid = text.AsciiText == null || text.UnicodeText == null
                || text.ScriptCodeText == null || !IsNonNullAscii(text.AsciiText)
                || !Icc2TextTagCodec.IsValidUnicodeString(text.UnicodeText)
                || text.ScriptCode < 0 || text.ScriptCode > ushort.MaxValue
                || !IsLegacyScriptText(text.ScriptCodeText);
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TextEncoding, IccValidationSeverity.Error,
                "The decoded text representation has a missing field, invalid character, invalid legacy code, or unrepresentable text.",
                tag, index, "Data.Text",
                "Provide valid Unicode and the 7-bit ASCII or legacy ScriptCode fields required by the selected ICC.1 text type."));
        }
    }

    internal static void ValidateLocalizedValues(IccTagData data, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (data.LocalizedStrings == null)
        {
            return;
        }
        bool invalid = false;
        int valueIndex = 0;
        while (valueIndex < data.LocalizedStrings.Length)
        {
            IccLocalizedString value = data.LocalizedStrings[valueIndex];
            if (!IsValidLocalizedString(value))
            {
                invalid = true;
            }
            valueIndex = valueIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TextEncoding, IccValidationSeverity.Error,
                "A localized Unicode record has a missing field or invalid surrogate sequence.",
                tag, index, "Data.LocalizedStrings",
                "Provide complete locale records containing valid Unicode text."));
        }
    }

    internal static void ValidateDictionaryValues(IccTagData data, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (data.Metadata == null)
        {
            return;
        }
        bool invalid = false;
        int valueIndex = 0;
        while (valueIndex < data.Metadata.Length)
        {
            IccMetadataEntry value = data.Metadata[valueIndex];
            if (value == null || value.Name == null || value.Name.Length == 0
                || value.Value == null || value.DisplayNames == null || value.DisplayValues == null
                || !Icc2TextTagCodec.IsValidUnicodeString(value.Name)
                || !Icc2TextTagCodec.IsValidUnicodeString(value.Value)
                || !AreLocalizedStringsValid(value.DisplayNames)
                || !AreLocalizedStringsValid(value.DisplayValues))
            {
                invalid = true;
            }
            int priorIndex = 0;
            while (value != null && value.Name != null && priorIndex < valueIndex)
            {
                IccMetadataEntry prior = data.Metadata[priorIndex];
                if (prior != null && prior.Name == value.Name)
                {
                    invalid = true;
                }
                priorIndex = priorIndex + 1;
            }
            valueIndex = valueIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TextEncoding, IccValidationSeverity.Error,
                "A dictionary record has a missing name/value field or invalid Unicode text.",
                tag, index, "Data.Metadata",
                "Provide unique non-empty names and valid Unicode dictionary values."));
        }
    }

    internal static void ValidateCurveValue(IccCurve? curve, IccTag tag, int index,
        string path, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (curve == null)
        {
            return;
        }
        bool invalid = false;
        switch (curve.Kind)
        {
            case IccCurveKind.Identity:
                invalid = curve.Samples == null || curve.Samples.Length != 0;
                break;
            case IccCurveKind.Gamma:
                invalid = curve.Samples == null || !CurveTagCodec.IsFinite(curve.Gamma)
                    || curve.Gamma <= 0.0
                    || curve.Gamma > IccNumberCodec.U8Fixed8Maximum;
                break;
            case IccCurveKind.Sampled:
                invalid = curve.Samples == null || curve.Samples.Length < 2
                    || !AreValuesInRange(curve.Samples, 0.0, 1.0);
                break;
            case IccCurveKind.Parametric:
                invalid = !IsValidParametricCurve(curve.Parametric);
                break;
            case IccCurveKind.Segmented:
                invalid = !IsValidSegmentedCurve(curve.Segments);
                break;
            default:
                invalid = true;
                break;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.CurveValidity,
                IccValidationSeverity.Error,
                "An ICC.1 curve has an unsupported representation, invalid function parameters, or values outside its encoded range.",
                tag, index, path,
                "Use identity, positive gamma, normalized sampled, or ICC function 0 through 4 curve data with exact parameter counts."));
        }
    }

    internal static bool IsValidParametricCurve(IccParametricCurve? curve)
    {
        if (curve == null || curve.Parameters == null)
        {
            return false;
        }
        int count = CurveTagCodec.GetParameterCount(curve.FunctionType);
        if (count < 0 || curve.Parameters.Length != count
            || !AreValuesInRange(curve.Parameters, IccNumberCodec.S15Fixed16Minimum,
                IccNumberCodec.S15Fixed16Maximum))
        {
            return false;
        }
        if (curve.Parameters[0] <= 0.0)
        {
            return false;
        }
        return curve.FunctionType == 0 || curve.Parameters[1] != 0.0;
    }

    internal static bool IsValidSegmentedCurve(IccCurveSegment[] segments)
    {
        if (segments == null || segments.Length < 1 || segments.Length > ushort.MaxValue
            || segments[0] == null
            || !System.Double.IsNegativeInfinity(segments[0].BreakPoint))
        {
            return false;
        }
        double previousBreakPoint = double.NegativeInfinity;
        int index = 0;
        while (index < segments.Length)
        {
            IccCurveSegment segment = segments[index];
            if (segment == null || ((index == 0 || index == segments.Length - 1)
                && segment.Kind != IccCurveSegmentKind.Formula))
            {
                return false;
            }
            if (index > 0 && (!System.Double.IsFinite(segment.BreakPoint)
                || segment.BreakPoint < previousBreakPoint))
            {
                return false;
            }
            if (index > 0)
            {
                previousBreakPoint = segment.BreakPoint;
            }
            if (segment.Kind == IccCurveSegmentKind.Formula)
            {
                int parameterCount = MultiProcessElementTagCodec.GetFormulaParameterCount(
                    segment.FormulaType);
                if (segment.Parameters == null || segment.Parameters.Length != parameterCount
                    || parameterCount < 0 || !AreFiniteValues(segment.Parameters))
                {
                    return false;
                }
            }
            else if (segment.Kind == IccCurveSegmentKind.Sampled)
            {
                if (segment.Samples == null || segment.Samples.Length < 1
                    || !AreFiniteValues(segment.Samples))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static void ValidateLutValue(IccTagData data, uint typeSignature, IccTag tag,
        int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (typeSignature == IccTypeSignatures.LutAToB
            || typeSignature == IccTypeSignatures.LutBToA)
        {
            ValidateMultiStageLut(data.MultiStageLut, typeSignature, tag, index, issues);
            return;
        }
        ValidateClassicLut(data.Lut, typeSignature, tag, index, issues);
    }

    internal static void ValidateClassicLut(IccLut? lut, uint typeSignature, IccTag tag,
        int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (lut == null)
        {
            return;
        }
        bool channelsValid = IsIcc1ChannelCount(lut.InputChannels)
            && IsIcc1ChannelCount(lut.OutputChannels)
            && lut.InputTables != null && lut.OutputTables != null
            && lut.InputTables.Length == lut.InputChannels
            && lut.OutputTables.Length == lut.OutputChannels;
        if (!channelsValid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ChannelCount,
                IccValidationSeverity.Error,
                "The classic LUT channel counts and per-channel table counts are inconsistent.",
                tag, index, "Data.Lut",
                "Use 1 through 15 channels and exactly one input/output table per corresponding channel."));
        }

        ValidateMatrixValue(lut.Matrix, 3, 3, tag, index, "Data.Lut.Matrix", issues);
        ValidateClutValue(lut.Clut, lut.InputChannels, lut.OutputChannels, true,
            tag, index, "Data.Lut.Clut", issues);
        bool sixteenBit = typeSignature == IccTypeSignatures.Lut16;
        int expectedTableCount = sixteenBit ? -1 : 256;
        if (!AreLutTablesValid(lut.InputTables, expectedTableCount, sixteenBit)
            || !AreLutTablesValid(lut.OutputTables, expectedTableCount, sixteenBit))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ClutDimensions,
                IccValidationSeverity.Error,
                "The classic LUT input or output tables have illegal lengths or sample values.",
                tag, index, "Data.Lut",
                sixteenBit
                    ? "Use equal table lengths between 2 and 4096 with normalized finite values."
                    : "Use exactly 256 normalized finite entries in every input and output table."));
        }

        IccPcsEncodingKind expectedEncoding = sixteenBit
            ? IccPcsEncodingKind.Version2 : IccPcsEncodingKind.Normalized;
        if (lut.PcsEncoding != expectedEncoding)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagTypeCompatibility,
                IccValidationSeverity.Error,
                "The classic LUT PCS encoding does not match its ICC.1 type.",
                tag, index, "Data.Lut.PcsEncoding",
                "Use normalized PCS encoding for lut8Type and version-2 PCS encoding for lut16Type."));
        }
    }

    internal static void ValidateMultiStageLut(IccMultiStageLut? lut, uint typeSignature,
        IccTag tag, int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (lut == null)
        {
            return;
        }
        bool aToB = typeSignature == IccTypeSignatures.LutAToB;
        int bCount = aToB ? lut.OutputChannels : lut.InputChannels;
        int aCount = aToB ? lut.InputChannels : lut.OutputChannels;
        IccCurve[]? aCurves = lut.ACurves;
        IccCurve[]? bCurves = lut.BCurves;
        IccCurve[]? mCurves = lut.MCurves;
        bool channelsValid = IsIcc1ChannelCount(lut.InputChannels)
            && IsIcc1ChannelCount(lut.OutputChannels) && aCurves != null
            && bCurves != null && mCurves != null;
        bool hasMatrix = lut.Matrix != null;
        bool hasM = mCurves != null && mCurves.Length > 0;
        bool hasClut = lut.Clut != null;
        bool hasA = aCurves != null && aCurves.Length > 0;
        if (channelsValid && aCurves != null && bCurves != null && mCurves != null)
        {
            channelsValid = bCurves.Length == bCount && hasMatrix == hasM
                && hasClut == hasA && (!hasM || mCurves.Length == bCount)
                && (!hasA || aCurves.Length == aCount)
                && (hasClut || lut.InputChannels == lut.OutputChannels)
                && (!hasMatrix || (aToB ? lut.OutputChannels == 3 : lut.InputChannels == 3));
        }
        if (!channelsValid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ChannelCount,
                IccValidationSeverity.Error,
                "The multi-stage LUT channel counts do not match its required curve, matrix, and CLUT stages.",
                tag, index, "Data.MultiStageLut",
                "Match A, B, and M curve counts to the LUT direction and pair matrix/M and CLUT/A stages."));
        }
        if (lut.PcsEncoding != IccPcsEncodingKind.Version4)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagTypeCompatibility,
                IccValidationSeverity.Error,
                "The multi-stage LUT does not use ICC version-4 PCS encoding.",
                tag, index, "Data.MultiStageLut.PcsEncoding",
                "Use version-4 PCS encoding for lutAToBType and lutBToAType."));
        }
        if (hasMatrix)
        {
            ValidateMatrixValue(lut.Matrix, 3, 4, tag, index,
                "Data.MultiStageLut.Matrix", issues);
        }
        if (hasClut)
        {
            ValidateClutValue(lut.Clut, lut.InputChannels, lut.OutputChannels, false,
                tag, index, "Data.MultiStageLut.Clut", issues);
        }
        if (bCurves != null)
        {
            ValidateCurveArray(bCurves, tag, index, "Data.MultiStageLut.BCurves", issues);
        }
        if (mCurves != null)
        {
            ValidateCurveArray(mCurves, tag, index, "Data.MultiStageLut.MCurves", issues);
        }
        if (aCurves != null)
        {
            ValidateCurveArray(aCurves, tag, index, "Data.MultiStageLut.ACurves", issues);
        }
    }

    internal static void ValidateMatrixValue(IccMatrix? matrix, int rows, int columns,
        IccTag tag, int index, string path,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool invalid;
        if (matrix == null || matrix.Values == null || matrix.Rows != rows
            || matrix.Columns != columns || matrix.Values.Length != rows * columns)
        {
            invalid = true;
        }
        else
        {
            invalid = !AreValuesInRange(matrix.Values, IccNumberCodec.S15Fixed16Minimum,
                IccNumberCodec.S15Fixed16Maximum) || !IsInvertibleLinearMatrix(matrix);
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.MatrixInvertibility,
                IccValidationSeverity.Error,
                "The LUT matrix has invalid dimensions, non-representable values, or a singular linear component.",
                tag, index, path,
                "Provide a finite, representable matrix whose three-by-three linear component is invertible."));
        }
    }

    internal static bool IsInvertibleLinearMatrix(IccMatrix matrix)
    {
        double maximum = 0.0;
        int row = 0;
        while (row < 3)
        {
            int column = 0;
            while (column < 3)
            {
                double absolute = System.Math.Abs(matrix.Values[(row * matrix.Columns) + column]);
                if (absolute > maximum)
                {
                    maximum = absolute;
                }
                column = column + 1;
            }
            row = row + 1;
        }
        if (maximum == 0.0 || !System.Double.IsFinite(maximum))
        {
            return false;
        }
        double a = matrix.Values[0] / maximum;
        double b = matrix.Values[1] / maximum;
        double c = matrix.Values[2] / maximum;
        double d = matrix.Values[matrix.Columns] / maximum;
        double e = matrix.Values[matrix.Columns + 1] / maximum;
        double f = matrix.Values[matrix.Columns + 2] / maximum;
        double g = matrix.Values[matrix.Columns * 2] / maximum;
        double h = matrix.Values[(matrix.Columns * 2) + 1] / maximum;
        double i = matrix.Values[(matrix.Columns * 2) + 2] / maximum;
        double determinant = (a * ((e * i) - (f * h)))
            - (b * ((d * i) - (f * g))) + (c * ((d * h) - (e * g)));
        return System.Double.IsFinite(determinant) && System.Math.Abs(determinant) > 1.0e-12;
    }

    internal static void ValidateClutValue(IccClut? clut, int inputChannels,
        int outputChannels, bool uniformGrid, IccTag tag, int index, string path,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool invalid;
        if (clut == null || clut.GridPoints == null || clut.Values == null
            || clut.InputChannels != inputChannels || clut.OutputChannels != outputChannels
            || clut.GridPoints.Length != inputChannels || !IsIcc1ChannelCount(inputChannels)
            || !IsIcc1ChannelCount(outputChannels))
        {
            invalid = true;
        }
        else
        {
            invalid = false;
        }
        int valueCount = outputChannels;
        int firstGrid = -1;
        int dimension = 0;
        while (!invalid && clut != null && clut.GridPoints != null
            && dimension < inputChannels)
        {
            int grid = clut.GridPoints[dimension];
            if (grid < 2 || grid > 255 || (uniformGrid && firstGrid >= 0 && grid != firstGrid)
                || valueCount > int.MaxValue / grid)
            {
                invalid = true;
            }
            else
            {
                firstGrid = grid;
                valueCount = valueCount * grid;
            }
            dimension = dimension + 1;
        }
        if (!invalid && clut != null && clut.Values != null)
        {
            invalid = clut.Values.Length != valueCount
                || !AreValuesInRange(clut.Values, 0.0, 1.0);
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ClutDimensions,
                IccValidationSeverity.Error,
                "The CLUT channel dimensions, grid points, value count, or normalized samples are invalid.",
                tag, index, path,
                "Match CLUT channels to the pipeline and provide exactly the checked grid-product value count."));
        }
    }

    internal static bool AreLutTablesValid(double[][]? tables, int expectedCount,
        bool boundedCount)
    {
        if (tables == null || tables.Length == 0)
        {
            return false;
        }
        int commonCount = tables[0] == null ? 0 : tables[0].Length;
        if ((expectedCount >= 0 && commonCount != expectedCount)
            || (boundedCount && (commonCount < 2 || commonCount > 4096)))
        {
            return false;
        }
        int index = 0;
        while (index < tables.Length)
        {
            if (tables[index] == null || tables[index].Length != commonCount
                || !AreValuesInRange(tables[index], 0.0, 1.0))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static void ValidateCurveArray(IccCurve[] curves, IccTag tag, int index,
        string path, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int curveIndex = 0;
        while (curveIndex < curves.Length)
        {
            int issueCount = issues.Count;
            ValidateCurveValue(curves[curveIndex], tag, index,
                $"{path}[{curveIndex}]", issues);
            if (issues.Count != issueCount)
            {
                return;
            }
            curveIndex = curveIndex + 1;
        }
    }

    internal static void ValidateMpeValue(IccMultiProcessElements? pipeline, IccTag tag,
        int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (pipeline == null)
        {
            return;
        }
        IccProcessingElement[]? elements = pipeline.Elements;
        bool invalid = !IsIcc1ChannelCount(pipeline.InputChannels)
            || !IsIcc1ChannelCount(pipeline.OutputChannels) || elements == null
            || elements.Length == 0;
        int previousOutput = pipeline.InputChannels;
        int elementIndex = 0;
        while (!invalid && elements != null && elementIndex < elements.Length)
        {
            IccProcessingElement element = elements[elementIndex];
            if (element == null || !IsIcc1ChannelCount(element.InputChannels)
                || !IsIcc1ChannelCount(element.OutputChannels)
                || element.InputChannels != previousOutput)
            {
                invalid = true;
            }
            else
            {
                previousOutput = element.OutputChannels;
                ValidateMpeElement(element, tag, index, elementIndex, issues);
            }
            elementIndex = elementIndex + 1;
        }
        if (!invalid && elements != null && elements.Length > 0
            && previousOutput != pipeline.OutputChannels)
        {
            invalid = true;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.PipelineChannelContinuity,
                IccValidationSeverity.Error,
                "The ICC.1 multi-process pipeline has invalid channel counts or a discontinuity between elements.",
                tag, index, "Data.MultiProcessElements",
                "Use 1 through 15 channels and connect every element output count to the next input count."));
        }
    }

    internal static void ValidateMpeElement(IccProcessingElement element, IccTag tag,
        int tagIndex, int elementIndex,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        string path = $"Data.MultiProcessElements.Elements[{elementIndex}]";
        bool invalid = false;
        switch (element.Kind)
        {
            case IccProcessingElementKind.CurveSet:
                IccCurve[]? curves = element.Curves;
                invalid = element.Signature != IccProcessingElementSignatures.CurveSet
                    || element.InputChannels != element.OutputChannels
                    || curves == null || curves.Length != element.InputChannels;
                if (!invalid && curves != null)
                {
                    ValidateCurveArray(curves, tag, tagIndex,
                        $"{path}.Curves", issues);
                }
                break;
            case IccProcessingElementKind.Matrix:
                invalid = element.Signature != IccProcessingElementSignatures.Matrix
                    || element.Matrix == null || element.Matrix.Values == null
                    || element.Matrix.Rows != element.OutputChannels
                    || element.Matrix.Columns != element.InputChannels + 1
                    || element.Matrix.Values.Length != element.OutputChannels
                        * (element.InputChannels + 1)
                    || !AreFiniteValues(element.Matrix.Values);
                break;
            case IccProcessingElementKind.Clut:
                invalid = element.Signature != IccProcessingElementSignatures.Clut
                    || !IsValidMpeClut(element.Clut, element.InputChannels,
                        element.OutputChannels);
                break;
            case IccProcessingElementKind.BeginAcs:
                invalid = element.Signature != IccProcessingElementSignatures.BeginAcs
                    || element.InputChannels != element.OutputChannels
                    || element.AcsData == null;
                break;
            case IccProcessingElementKind.EndAcs:
                invalid = element.Signature != IccProcessingElementSignatures.EndAcs
                    || element.InputChannels != element.OutputChannels
                    || element.AcsData == null;
                break;
            case IccProcessingElementKind.Unknown:
                invalid = element.RawData == null || element.RawData.Bytes == null
                    || element.RawData.Bytes.Length < 12;
                break;
            default:
                invalid = true;
                break;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagSize,
                IccValidationSeverity.Error,
                "An ICC.1 processing element has inconsistent kind-specific data, dimensions, signature, or finite values.",
                tag, tagIndex, path,
                "Provide a structurally complete ICC.1 curve-set, matrix, CLUT, ACS, or preserved future element."));
        }
    }

    internal static bool IsValidMpeClut(IccClut? clut, int inputChannels,
        int outputChannels)
    {
        if (clut == null || clut.GridPoints == null || clut.Values == null
            || inputChannels < 1 || inputChannels > 16 || outputChannels < 1
            || clut.InputChannels != inputChannels || clut.OutputChannels != outputChannels
            || clut.GridPoints.Length != inputChannels)
        {
            return false;
        }
        int valueCount = outputChannels;
        int index = 0;
        while (index < inputChannels)
        {
            int grid = clut.GridPoints[index];
            if (grid < 2 || grid > 255 || valueCount > int.MaxValue / grid)
            {
                return false;
            }
            valueCount = valueCount * grid;
            index = index + 1;
        }
        return clut.Values.Length == valueCount && AreFiniteValues(clut.Values);
    }

    internal static void ValidateMeasurementValue(IccMeasurement? measurement, IccTag tag,
        int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (measurement == null)
        {
            return;
        }
        int observer = (int)measurement.Observer;
        int geometry = (int)measurement.Geometry;
        int illuminant = (int)measurement.Illuminant;
        bool invalid = observer < 0 || observer > 2 || geometry < 0 || geometry > 2
            || illuminant < 0 || illuminant > 8 || !System.Double.IsFinite(measurement.Flare)
            || measurement.Flare < 0.0 || measurement.Flare > 1.0
            || measurement.Backing == null || !IsFixedXyz(measurement.Backing.X,
                measurement.Backing.Y, measurement.Backing.Z);
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "The measurement conditions contain an undefined selector, invalid flare, or non-representable backing colour.",
                tag, index, "Data.Measurement",
                "Use defined ICC observer, geometry, and illuminant values with normalized flare and finite XYZ backing."));
        }
    }

    internal static void ValidateViewingValue(IccViewingConditions? viewing, IccTag tag,
        int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (viewing == null)
        {
            return;
        }
        int illuminant = (int)viewing.IlluminantKind;
        bool invalid = illuminant < 0 || illuminant > 8 || viewing.Illuminant == null
            || viewing.Surround == null || !IsFixedXyz(viewing.Illuminant.X,
                viewing.Illuminant.Y, viewing.Illuminant.Z)
            || !IsFixedXyz(viewing.Surround.X, viewing.Surround.Y, viewing.Surround.Z);
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "The viewing conditions contain an undefined illuminant or non-representable XYZ values.",
                tag, index, "Data.ViewingConditions",
                "Use a defined ICC.1 illuminant and finite signed 15.16 XYZ values."));
        }
    }

    internal static void ValidateChromaticityValue(IccChromaticity? chromaticity,
        IccVersionKind version, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (chromaticity == null)
        {
            return;
        }
        int maximumEncoding = version == IccVersionKind.Version2 ? 4 : 6;
        double[]? xCoordinates = chromaticity.XCoordinates;
        double[]? yCoordinates = chromaticity.YCoordinates;
        bool invalid;
        if (xCoordinates == null || yCoordinates == null)
        {
            invalid = true;
        }
        else
        {
            invalid = xCoordinates.Length == 0
                || xCoordinates.Length != yCoordinates.Length
                || xCoordinates.Length > ushort.MaxValue
                || chromaticity.ColorantEncoding < 0
                || chromaticity.ColorantEncoding > maximumEncoding
                || !AreValuesInRange(xCoordinates, 0.0,
                    IccNumberCodec.U16Fixed16Maximum)
                || !AreValuesInRange(yCoordinates, 0.0,
                    IccNumberCodec.U16Fixed16Maximum);
        }
        if (!invalid && xCoordinates != null && yCoordinates != null
            && chromaticity.ColorantEncoding != 0)
        {
            invalid = xCoordinates.Length != 3
                || !ChromaticityTagCodec.MatchesPredefinedEncoding(
                    chromaticity.ColorantEncoding, xCoordinates, yCoordinates);
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ChannelCount,
                IccValidationSeverity.Error,
                "The chromaticity channel arrays, coordinates, or predefined colorant encoding are invalid.",
                tag, index, "Data.Chromaticity",
                "Use equal non-empty coordinate arrays and exact standardized triples for predefined encodings."));
        }
    }

    internal static void ValidateCicpValue(IccCicp? cicp, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (cicp != null && (cicp.ColorPrimaries < 0 || cicp.ColorPrimaries > byte.MaxValue
            || cicp.TransferCharacteristics < 0
            || cicp.TransferCharacteristics > byte.MaxValue
            || cicp.MatrixCoefficients < 0 || cicp.MatrixCoefficients > byte.MaxValue))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "A CICP code point is outside its unsigned eight-bit field.",
                tag, index, "Data.Cicp",
                "Keep every CICP code point between zero and 255."));
        }
    }

    internal static void ValidateNamedColorValue(IccNamedColorSet? set, uint typeSignature,
        IccPcsKind pcsKind, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (set == null)
        {
            return;
        }
        bool legacy = typeSignature == IccTypeSignatures.NamedColor;
        bool invalidChannels = legacy
            ? !IsIcc1ChannelCount(set.DeviceChannelCount)
            : set.DeviceChannelCount < 0
                || set.DeviceChannelCount > IccConstants.MaximumIcc1ChannelCount;
        invalidChannels = invalidChannels || set.Colors == null;
        bool invalidPcs = false;
        bool invalidNames = !IsFixedAsciiName(set.Prefix) || !IsFixedAsciiName(set.Suffix);
        int colorIndex = 0;
        while (set.Colors != null && colorIndex < set.Colors.Length)
        {
            IccNamedColor color = set.Colors[colorIndex];
            if (color == null || color.Name == null || !IsFixedAsciiName(color.Name))
            {
                invalidNames = true;
            }
            if (legacy)
            {
                if (color == null || color.Tints == null
                    || color.Tints.Length != set.DeviceChannelCount
                    || !AreValuesInRange(color.Tints, 0.0, 1.0))
                {
                    invalidChannels = true;
                }
            }
            else
            {
                if (color == null || color.DeviceColor == null
                    || color.DeviceColor.Components == null
                    || color.DeviceColor.Components.Length != set.DeviceChannelCount
                    || !AreValuesInRange(color.DeviceColor.Components, 0.0, 1.0))
                {
                    invalidChannels = true;
                }
                if (color == null || color.PcsColor == null
                    || !ArePcsValuesValid(color.PcsColor.Components, pcsKind))
                {
                    invalidPcs = true;
                }
            }
            colorIndex = colorIndex + 1;
        }
        if (invalidChannels)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ChannelCount,
                IccValidationSeverity.Error,
                "Named-colour device coordinates do not match the declared channel count.",
                tag, index, "Data.NamedColors",
                "Use at most 15 device channels and exactly that many coordinates for every colour."));
        }
        if (invalidPcs)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagTypeCompatibility,
                IccValidationSeverity.Error,
                "A named colour does not contain normalized three-component PCS data.",
                tag, index, "Data.NamedColors",
                "Use three normalized PCS components and normalized device coordinates."));
        }
        if (invalidNames)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TextEncoding,
                IccValidationSeverity.Error,
                "A named-colour prefix, suffix, or root name cannot be represented as a required terminated 32-byte ASCII field.",
                tag, index, "Data.NamedColors",
                "Use at most 31 non-null 7-bit ASCII characters in every named-colour name field."));
        }
    }

    internal static void ValidateColorantValue(IccColorantSet? set, IccPcsKind pcsKind,
        IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (set == null)
        {
            return;
        }
        int expected = ColorSpaceChannelCount.Decode(set.ColorSpaceSignature);
        bool invalid = set.Colorants == null || expected < 1
            || expected > IccConstants.MaximumIcc1ChannelCount
            || set.Colorants.Length != expected;
        bool invalidNames = false;
        int colorantIndex = 0;
        while (set.Colorants != null && colorantIndex < set.Colorants.Length)
        {
            IccColorant colorant = set.Colorants[colorantIndex];
            if (colorant == null || colorant.Name == null || colorant.PcsColor == null
                || !ArePcsValuesValid(colorant.PcsColor.Components, pcsKind))
            {
                invalid = true;
            }
            if (colorant == null || !IsFixedAsciiName(colorant.Name))
            {
                invalidNames = true;
            }
            int priorIndex = 0;
            while (colorant != null && colorant.Name != null && priorIndex < colorantIndex)
            {
                IccColorant prior = set.Colorants[priorIndex];
                if (prior != null && prior.Name == colorant.Name)
                {
                    invalidNames = true;
                }
                priorIndex = priorIndex + 1;
            }
            colorantIndex = colorantIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ChannelCount,
                IccValidationSeverity.Error,
                "The colorant table count or PCS coordinates do not match its associated colour space.",
                tag, index, "Data.Colorants",
                "Provide one named colorant per associated channel with representable three-component PCS coordinates."));
        }
        if (invalidNames)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TextEncoding,
                IccValidationSeverity.Error,
                "Colorant names must be unique and fit their required terminated 32-byte ASCII fields.",
                tag, index, "Data.Colorants",
                "Use unique names containing at most 31 non-null 7-bit ASCII characters."));
        }
    }

    internal static void ValidateResponseValue(IccTagData data, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccResponseCurve[]? responseCurves = data.ResponseCurves;
        bool invalid = !IsIcc1ChannelCount(data.ResponseCurveChannelCount)
            || responseCurves == null;
        int curveIndex = 0;
        while (!invalid && responseCurves != null && curveIndex < responseCurves.Length)
        {
            IccResponseCurve curve = responseCurves[curveIndex];
            if (curve == null || curve.MaximumColorantXyz == null
                || curve.DeviceCodes == null || curve.Measurements == null
                || !ResponseCurveTagCodec.IsRegisteredMeasurementUnit(
                    curve.MeasurementUnitSignature)
                || curve.MaximumColorantXyz.Length != data.ResponseCurveChannelCount
                || curve.DeviceCodes.Length != data.ResponseCurveChannelCount
                || curve.Measurements.Length != data.ResponseCurveChannelCount)
            {
                invalid = true;
            }
            else
            {
                int channel = 0;
                while (!invalid && channel < data.ResponseCurveChannelCount)
                {
                    invalid = curve.DeviceCodes[channel] == null
                        || curve.Measurements[channel] == null
                        || curve.MaximumColorantXyz[channel] == null
                        || curve.DeviceCodes[channel].Length
                            != curve.Measurements[channel].Length
                        || !AreValuesInRange(curve.Measurements[channel],
                            IccNumberCodec.S15Fixed16Minimum,
                            IccNumberCodec.S15Fixed16Maximum);
                    if (!invalid)
                    {
                        IccXyzNumber maximum = curve.MaximumColorantXyz[channel];
                        invalid = !IsFixedXyz(maximum.X, maximum.Y, maximum.Z)
                            || !AreDeviceCodesIncreasing(curve.DeviceCodes[channel]);
                    }
                    channel = channel + 1;
                }
            }
            int priorIndex = 0;
            while (!invalid && curve != null && priorIndex < curveIndex)
            {
                IccResponseCurve prior = responseCurves[priorIndex];
                if (prior != null && prior.MeasurementUnitSignature
                    == curve.MeasurementUnitSignature)
                {
                    invalid = true;
                }
                priorIndex = priorIndex + 1;
            }
            curveIndex = curveIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ChannelCount,
                IccValidationSeverity.Error,
                "Response-curve records have invalid units, channel arrays, XYZ values, numeric ranges, or device-code ordering.",
                tag, index, "Data.ResponseCurves",
                "Use unique registered units, matching channel arrays, representable values, and strictly increasing device codes."));
        }
    }

    internal static bool AreDeviceCodesIncreasing(ushort[] values)
    {
        if (values == null)
        {
            return false;
        }
        int index = 1;
        while (index < values.Length)
        {
            if (values[index] <= values[index - 1])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static void ValidateProfileSequenceValue(IccTagData data, uint typeSignature,
        IccVersionKind version, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool invalid = false;
        if (typeSignature == IccTypeSignatures.ProfileSequenceIdentifier)
        {
            IccProfileSequenceIdentifier[]? identifiers = data.ProfileSequenceIdentifiers;
            invalid = identifiers == null;
            int identifierIndex = 0;
            while (!invalid && identifiers != null && identifierIndex < identifiers.Length)
            {
                IccProfileSequenceIdentifier identifier = identifiers[identifierIndex];
                invalid = identifier == null || identifier.ProfileId == null
                    || identifier.ProfileId.Bytes == null
                    || identifier.ProfileId.Bytes.Length != 16
                    || !AreLocalizedStringsValid(identifier.Descriptions);
                identifierIndex = identifierIndex + 1;
            }
        }
        else
        {
            IccProfileSequenceEntry[]? entries = data.ProfileSequence;
            invalid = entries == null;
            int entryIndex = 0;
            while (!invalid && entries != null && entryIndex < entries.Length)
            {
                IccProfileSequenceEntry entry = entries[entryIndex];
                if (entry == null)
                {
                    invalid = true;
                }
                else
                {
                    IccTextData? manufacturer = entry.ManufacturerDescription;
                    IccTextData? model = entry.ModelDescription;
                    IccLocalizedString[]? manufacturerLocalized =
                        entry.ManufacturerLocalizedDescriptions;
                    IccLocalizedString[]? modelLocalized = entry.ModelLocalizedDescriptions;
                    invalid = manufacturer == null || model == null
                        || manufacturerLocalized == null || modelLocalized == null;
                    if (!invalid && version == IccVersionKind.Version2)
                    {
                        invalid = !IsValidLegacyTextDescription(manufacturer)
                            || !IsValidLegacyTextDescription(model);
                    }
                    if (!invalid && version == IccVersionKind.Version4)
                    {
                        invalid = !AreLocalizedStringsValid(manufacturerLocalized)
                            || !AreLocalizedStringsValid(modelLocalized);
                    }
                }
                entryIndex = entryIndex + 1;
            }
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagNestedOffset,
                IccValidationSeverity.Error,
                "A profile-sequence record has a missing nested description, invalid locale, or incorrectly sized Profile ID.",
                tag, index, "Data.ProfileSequence",
                "Provide complete generation-appropriate nested descriptions and exactly 16 Profile ID bytes."));
        }
    }

    internal static void ValidateCrdInfoValue(IccCrdInfo? info, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        string[]? names = info == null ? null : info.RenderingIntentCrdNames;
        bool invalid = info != null && (info.ProductName == null || names == null
            || names.Length != 4 || !IsNonNullAscii(info.ProductName));
        int nameIndex = 0;
        while (!invalid && names != null && nameIndex < names.Length)
        {
            string name = names[nameIndex];
            invalid = name == null || !IsNonNullAscii(name);
            nameIndex = nameIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TextEncoding,
                IccValidationSeverity.Error,
                "Legacy CRD information requires five complete, terminated 7-bit ASCII fields.",
                tag, index, "Data.CrdInfo",
                "Provide a product name and exactly four rendering-intent names using non-null 7-bit ASCII."));
        }
    }

    internal static void ValidateScreeningValue(IccScreening? screening, IccTag tag,
        int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccScreeningChannel[]? channels = screening == null ? null : screening.Channels;
        bool invalid = screening != null && (channels == null
            || channels.Length > IccConstants.MaximumIcc1ChannelCount);
        int channelIndex = 0;
        while (!invalid && channels != null && channelIndex < channels.Length)
        {
            IccScreeningChannel channel = channels[channelIndex];
            invalid = channel == null || (int)channel.SpotShape < 0
                || channel.SpotShape > IccScreeningSpotShape.Cross
                || !IsFixedValue(channel.Frequency) || !IsFixedValue(channel.AngleDegrees);
            channelIndex = channelIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ChannelCount,
                IccValidationSeverity.Error,
                "Legacy screening data has too many channels, an undefined spot shape, or an unrepresentable numeric value.",
                tag, index, "Data.Screening",
                "Use at most 15 complete screening channels and signed 15.16 frequency and angle values."));
        }
    }

    internal static void ValidateUcrBgValue(IccUcrBg? value, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        ushort[]? ucr = value == null ? null : value.UnderColorRemovalValues;
        ushort[]? bg = value == null ? null : value.BlackGenerationValues;
        bool invalid = value != null && (ucr == null || bg == null || value.Description == null
            || !IsNonNullAscii(value.Description));
        if (!invalid && ucr != null && bg != null)
        {
            invalid = ucr.Length == 1 && ucr[0] > 100;
            invalid = invalid || (bg.Length == 1 && bg[0] > 100);
        }
        if (invalid)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.CurveValidity,
                IccValidationSeverity.Error,
                "Legacy UCR/BG data has a missing curve, invalid percentage, or non-ASCII description.",
                tag, index, "Data.UcrBg",
                "Provide both curves, keep single-entry percentages at or below 100, and use non-null 7-bit ASCII text."));
        }
    }

    internal static void ValidateAdaptiveValue(IccAdaptiveGainCurve? curve, IccTag tag,
        int index, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (curve == null)
        {
            return;
        }
        byte[]? identifier = curve.Identifier;
        byte[]? functionData = curve.FunctionData;
        double[]? compatibleHeadrooms = curve.BackwardCompatibleTargetHeadrooms;
        bool headerInvalid;
        if (identifier == null || identifier.Length != 16 || functionData == null
            || functionData.Length != 52 || compatibleHeadrooms == null
            || compatibleHeadrooms.Length != 3)
        {
            headerInvalid = true;
        }
        else
        {
            bool hasIdentifier = AdaptiveGainCurveTagCodec.HasNonZeroByte(identifier);
            headerInvalid = hasIdentifier != curve.IsImageSpecific
                || !AreFiniteValues(compatibleHeadrooms)
                || !System.Double.IsFinite(curve.TargetHeadroom)
                || !System.Double.IsFinite(curve.AlternateHeadroom)
                || !AdaptiveGainCurveTagCodec.HasValidFunctionDataFloat32Values(functionData);
        }
        string redError = AdaptiveGainCurveTagCodec.ValidateNodes(curve.RedNodes);
        string greenError = AdaptiveGainCurveTagCodec.ValidateNodes(curve.GreenNodes);
        string blueError = AdaptiveGainCurveTagCodec.ValidateNodes(curve.BlueNodes);
        if (headerInvalid || redError.Length > 0 || greenError.Length > 0
            || blueError.Length > 0)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.CurveValidity,
                IccValidationSeverity.Error,
                "The adaptive-gain header or one of its RGB node curves is invalid.",
                tag, index, "Data.AdaptiveGainCurve",
                "Use valid fixed header arrays, consistent image identity, finite values, and 2 through 32 strictly ordered nodes per channel."));
        }
    }

    internal static bool IsValidLocalizedString(IccLocalizedString? value)
    {
        return value != null && value.Language != null && value.Country != null
            && value.Text != null && value.RawBytes != null
            && value.Language.Length == 2 && value.Language[0] >= 'a'
            && value.Language[0] <= 'z' && value.Language[1] >= 'a'
            && value.Language[1] <= 'z' && value.Country.Length == 2
            && value.Country[0] >= 'A' && value.Country[0] <= 'Z'
            && value.Country[1] >= 'A' && value.Country[1] <= 'Z'
            && Icc2TextTagCodec.IsValidUnicodeString(value.Text);
    }

    internal static bool AreLocalizedStringsValid(IccLocalizedString[]? values)
    {
        if (values == null)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!IsValidLocalizedString(values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsValidLegacyTextDescription(IccTextData? text)
    {
        return text != null && text.Text != null && text.AsciiText != null
            && text.UnicodeText != null && text.ScriptCodeText != null
            && text.Bytes != null && IsNonNullAscii(text.AsciiText)
            && Icc2TextTagCodec.IsValidUnicodeString(text.UnicodeText)
            && text.ScriptCode >= 0 && text.ScriptCode <= ushort.MaxValue
            && IsLegacyScriptText(text.ScriptCodeText);
    }

    internal static bool IsLegacyScriptText(string? text)
    {
        if (text == null || text.Length > 66)
        {
            return false;
        }
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == 0 || text[index] > 255)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsNonNullAscii(string? text)
    {
        if (text == null)
        {
            return false;
        }
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == 0 || text[index] > 127)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsFixedAsciiName(string? text)
    {
        return text != null && text.Length <= 31 && IsNonNullAscii(text);
    }

    internal static bool ArePcsValuesValid(double[]? values, IccPcsKind pcsKind)
    {
        if (values == null || values.Length != 3)
        {
            return false;
        }
        if (pcsKind == IccPcsKind.Xyz)
        {
            return AreValuesInRange(values, 0.0, 65535.0 / 32768.0);
        }
        if (pcsKind == IccPcsKind.Lab)
        {
            return System.Double.IsFinite(values[0]) && values[0] >= 0.0
                && values[0] <= 100.0 && System.Double.IsFinite(values[1])
                && values[1] >= -128.0 && values[1] <= 127.99609375
                && System.Double.IsFinite(values[2]) && values[2] >= -128.0
                && values[2] <= 127.99609375;
        }
        return false;
    }

    internal static bool IsFixedValue(double value)
    {
        return System.Double.IsFinite(value) && value >= IccNumberCodec.S15Fixed16Minimum
            && value <= IccNumberCodec.S15Fixed16Maximum;
    }

    internal static bool AreValuesInRange(double[] values, double minimum, double maximum)
    {
        if (values == null)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!System.Double.IsFinite(values[index]) || values[index] < minimum
                || values[index] > maximum)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool AreFiniteValues(double[] values)
    {
        if (values == null)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!System.Double.IsFinite(values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsFixedXyz(double x, double y, double z)
    {
        return System.Double.IsFinite(x) && x >= IccNumberCodec.S15Fixed16Minimum
            && x <= IccNumberCodec.S15Fixed16Maximum && System.Double.IsFinite(y)
            && y >= IccNumberCodec.S15Fixed16Minimum
            && y <= IccNumberCodec.S15Fixed16Maximum && System.Double.IsFinite(z)
            && z >= IccNumberCodec.S15Fixed16Minimum
            && z <= IccNumberCodec.S15Fixed16Maximum;
    }

    internal static bool IsIcc1ChannelCount(int count)
    {
        return count >= 1 && count <= IccConstants.MaximumIcc1ChannelCount;
    }

    internal static IccValidationIssue CreateIssue(string ruleId,
        IccValidationSeverity severity, string message, IccTag tag, int index,
        string childPath, string remediation)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = ruleId;
        issue.Severity = severity;
        issue.Message = message;
        issue.Path = $"Tags[{index}].{childPath}";
        issue.Remediation = remediation;
        if (tag.TableEntry != null && tag.TableEntry.Offset >= 0)
        {
            issue.Location.Offset = tag.TableEntry.Offset;
            issue.Location.Length = tag.TableEntry.Size < 0 ? 0 : tag.TableEntry.Size;
        }
        return issue;
    }
}
