namespace icc;

internal class StructureProjectionNumber
{
    internal bool IsValid;
    internal double Value;

    internal StructureProjectionNumber(bool isValid, double value)
    {
        IsValid = isValid;
        Value = value;
    }
}

internal static class StructuredTagProjectionCodec
{
    internal const uint BrdfType = 0x74797065;
    internal const uint BrdfFunction = 0x66756e63;
    internal const uint BrdfParametersPerChannel = 0x6e756d70;
    internal const uint BrdfTransform = 0x7866726d;
    internal const uint Name = 0x6e616d65;
    internal const uint LocalizedName = 0x6c636e6d;
    internal const uint PcsData = 0x70637320;
    internal const uint SpectralData = 0x73706563;
    internal const uint DeviceData = 0x64657620;
    internal const uint TintValues = 0x74696e74;
    internal const uint SpectralOverBlack = 0x73706362;
    internal const uint SpectralOverGray = 0x73706367;
    internal const uint BrdfColorimetric = 0x62636f6c;
    internal const uint BrdfColorimetricParameters = 0x62637072;
    internal const uint BrdfSpectral = 0x62737063;
    internal const uint BrdfSpectralParameters = 0x62737072;
    internal const uint NormalMap = 0x6e6d6170;
    internal const uint BluePrimary = 0x6258595a;
    internal const uint GreenPrimary = 0x6758595a;
    internal const uint RedPrimary = 0x7258595a;
    internal const uint TransferFunction = 0x66756e63;
    internal const uint InverseTransferFunction = 0x69666e63;
    internal const uint LumaChromaMatrix = 0x6c6d6174;
    internal const uint WhitePointLuminance = 0x776c756d;
    internal const uint WhitePointChromaticity = 0x7758595a;
    internal const uint EncodingRange = 0x65526e67;
    internal const uint BitDepth = 0x62697473;
    internal const uint ImageState = 0x696d7374;
    internal const uint ImageBackground = 0x69626b67;
    internal const uint ViewingSurround = 0x73726e64;
    internal const uint AmbientIlluminance = 0x61696c6d;
    internal const uint AmbientWhiteLuminance = 0x61776c6d;
    internal const uint AmbientWhiteChromaticity = 0x61777063;
    internal const uint ViewingFlare = 0x666c6172;
    internal const uint RelativeLuminanceRange = 0x6c726e67;
    internal const uint MediumWhiteLuminance = 0x6d77706c;
    internal const uint MediumWhiteChromaticity = 0x6d777063;
    internal const uint MediumBlackLuminance = 0x6d62706c;
    internal const uint MediumBlackChromaticity = 0x6d627063;
    internal const uint MeasurementBacking = 0x6d62616b;
    internal const uint MeasurementFlare = 0x6d666c72;
    internal const uint MeasurementGeometry = 0x6d67656f;
    internal const uint MeasurementIlluminant = 0x6d696c6c;
    internal const uint MeasurementIlluminantRange = 0x6d697772;
    internal const uint MeasurementMode = 0x6d6d6f64;
    internal const uint ProfileAttributes = 0x61747472;
    internal const uint ProfileDescription = 0x70647363;
    internal const uint ProfileId = 0x70696420;
    internal const uint ManufacturerDescription = 0x646d6e64;
    internal const uint ManufacturerSignature = 0x646d6e73;
    internal const uint ModelDescription = 0x646d6464;
    internal const uint ModelSignature = 0x6d6f6420;
    internal const uint RenderingTransform = 0x7274726e;
    internal const uint Technology = 0x74656368;
    internal const uint PccIlluminant = 0x6958595a;
    internal const uint PccMediaWhitePoint = 0x6d777074;
    internal const uint PccSpectralWhitePoint = 0x73777074;
    internal const uint PccViewingConditions = 0x7376636e;
    internal const uint PccCustomToStandard = 0x63327370;
    internal const uint PccStandardToCustom = 0x73326370;
    internal const uint SegmentedCurveType = 0x63757266;
    internal const uint SpectralRangeType = IccTypeSignatures.SpectralRange;
    internal const uint MonochromeBrdf = 0x6d6f6e6f;
    internal const uint ColorBrdf = 0x636f6c72;

    internal static string ProjectDecoded(TagDecodeState state, IccTagData data)
    {
        if (data.TagStructure == null)
        {
            return "A typed structure projection requires generic tag-structure data.";
        }

        return ProjectStructure(state, data.TagStructure, data);
    }

    internal static void ValidateForWrite(IccTagStructure structure)
    {
        int index = 0;
        while (index < structure.Members.Length)
        {
            if (structure.Members[index] == null || structure.Members[index].Data == null)
            {
                throw new IccProfileWriteException($"Tag-structure member {index} and its data cannot be null.");
            }

            index = index + 1;
        }

        TagDecodeState state = new TagDecodeState(new TagDecodeSource(System.ReadOnlyMemory<byte>.Empty), 0, 0, 0, IccVersionKind.Version5, false);
        IccTagData data = new IccTagData();
        data.TagStructure = structure;
        string error = ProjectStructure(state, structure, data);
        if (error.Length != 0)
        {
            throw new IccProfileWriteException(error);
        }
    }

    internal static string ProjectStructure(TagDecodeState state, IccTagStructure structure, IccTagData data)
    {
        switch (structure.StructureSignature)
        {
            case IccStructureSignatures.BrdfTransform:
                return ProjectBrdfStructure(state, structure, data);
            case IccStructureSignatures.ColorantInformation:
                return ProjectColorantStructure(state, structure, data);
            case IccStructureSignatures.ColorEncodingParameters:
                return ProjectColorEncodingStructure(structure, data);
            case IccStructureSignatures.MeasurementInformation:
                return ProjectMeasurementStructure(structure, data);
            case IccStructureSignatures.NamedColor:
                return ProjectNamedColorStructure(state, structure, data);
            case IccStructureSignatures.ProfileConnectionConditions:
                return ProjectPccStructure(state, structure, data);
            case IccStructureSignatures.ProfileInformation:
                return ProjectProfileInfoStructure(structure, data);
            case IccStructureSignatures.TintZero:
                return ProjectTintZeroStructure(state, structure, data);
            default:
                return string.Empty;
        }
    }

    internal static string ProjectBrdfStructure(TagDecodeState state, IccTagStructure structure, IccTagData data)
    {
        IccStructureMember? type = FindMember(structure, BrdfType);
        IccStructureMember? function = FindMember(structure, BrdfFunction);
        IccStructureMember? count = FindMember(structure, BrdfParametersPerChannel);
        IccStructureMember? transform = FindMember(structure, BrdfTransform);
        if (type == null || function == null || count == null || transform == null)
        {
            return "A brdfTransformStructure shall contain the type, function, parameters-per-channel, and transform members.";
        }

        if (!HasType(type.Data, IccTypeSignatures.Signature) || !HasType(function.Data, IccTypeSignatures.Signature)
            || !HasType(count.Data, IccTypeSignatures.UInt16Array) || !HasType(transform.Data, IccTypeSignatures.MultiProcessElements))
        {
            return "A brdfTransformStructure member has a tag type that is not permitted for its signature.";
        }

        if (type.Data.Signature != MonochromeBrdf && type.Data.Signature != ColorBrdf)
        {
            return "The BRDF type member shall contain either the 'mono' or 'colr' signature.";
        }

        StructureProjectionNumber number = GetSingleNumber(count.Data);
        if (!number.IsValid || number.Value < 1.0 || number.Value > ushort.MaxValue)
        {
            return "The BRDF parameters-per-channel member shall contain one positive unsigned 16-bit value.";
        }

        int registeredCount = BrdfEvaluator.GetParameterCount(function.Data.Signature);
        if (registeredCount != 0 && number.Value != registeredCount)
        {
            return $"BRDF model '{FourCcCodec.Format(function.Data.Signature)}' requires {registeredCount} parameters per channel.";
        }
        if (registeredCount != 0 && ((BrdfEvaluator.IsMonochromeModel(function.Data.Signature) && type.Data.Signature != MonochromeBrdf)
            || (!BrdfEvaluator.IsMonochromeModel(function.Data.Signature) && type.Data.Signature != ColorBrdf)))
        {
            return $"BRDF model '{FourCcCodec.Format(function.Data.Signature)}' does not match the declared BRDF parameterization type.";
        }

        IccBrdf projection = new IccBrdf();
        projection.TypeSignature = type.Data.Signature;
        projection.ModelSignature = function.Data.Signature;
        projection.Parameters = new double[] { number.Value };
        if (IsSpectralBrdfParameterTag(state.TagSignature))
        {
            projection.SpectralTransform = transform.Data.MultiProcessElements;
        }
        else
        {
            projection.ColorimetricTransform = transform.Data.MultiProcessElements;
        }
        data.Brdf = projection;
        return string.Empty;
    }

    internal static bool IsSpectralBrdfParameterTag(uint signature)
    {
        return MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.BrdfSpectralParameter0)
            || MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.BrdfMToS0);
    }

    internal static string ProjectColorantStructure(TagDecodeState state, IccTagStructure structure, IccTagData data)
    {
        IccStructureMember? name = FindMember(structure, Name);
        if (name == null || !HasType(name.Data, IccTypeSignatures.Utf8) || name.Data.Text == null || name.Data.Text.Text.Length == 0)
        {
            return "A colorantInfoStructure shall contain a non-empty UTF-8 name member.";
        }

        int index = 0;
        while (index < structure.Members.Length)
        {
            IccStructureMember member = structure.Members[index];
            if ((member.Signature == LocalizedName && !HasType(member.Data, IccTypeSignatures.MultiLocalizedUnicode))
                || (member.Signature == PcsData && !IsColorSampleArray(member.Data, false))
                || (member.Signature == SpectralData && !IsColorSampleArray(member.Data, true)))
            {
                return WrongMemberType("colorantInfoStructure", member.Signature);
            }

            index = index + 1;
        }

        IccColorant colorant = new IccColorant();
        colorant.Name = name.Data.Text.Text;
        IccStructureMember? pcs = FindMember(structure, PcsData);
        if (pcs != null)
        {
            colorant.PcsColor = CreateColorVector(state.ProfileConnectionSpace == IccPcsKind.Xyz ? IccColorSpaceKind.Xyz : IccColorSpaceKind.Lab, state.ProfileConnectionSpaceSignature, GetNormalizedSamples(pcs.Data));
        }

        IccColorantSet set = new IccColorantSet();
        set.ColorSpaceSignature = state.ProfileConnectionSpaceSignature;
        set.Colorants = new IccColorant[] { colorant };
        data.Colorants = set;
        return string.Empty;
    }

    internal static string ProjectColorEncodingStructure(IccTagStructure structure, IccTagData data)
    {
        IccColorEncodingParameters projection = new IccColorEncodingParameters();
        int index = 0;
        while (index < structure.Members.Length)
        {
            IccStructureMember member = structure.Members[index];
            uint signature = member.Signature;
            if (IsChromaticityMember(signature))
            {
                double[]? values = GetFloat32Values(member.Data);
                if (values == null || !IsChromaticity(values))
                {
                    return $"Colour-encoding member '{FourCcCodec.Format(signature)}' shall contain two or three normalized float32 chromaticity values.";
                }

                if (signature == WhitePointChromaticity)
                {
                    projection.ReferenceWhite = CreateChromaticity(values);
                }
            }
            else if (signature == LumaChromaMatrix)
            {
                double[]? values = GetFloat32Values(member.Data);
                if (values == null || values.Length != 9)
                {
                    return "The colour-encoding luma-chroma matrix shall contain nine float32 values.";
                }

                IccMatrix matrix = new IccMatrix();
                matrix.Rows = 3;
                matrix.Columns = 3;
                matrix.Values = CloneDoubles(values);
                projection.Matrix = matrix;
            }
            else if (signature == EncodingRange || signature == RelativeLuminanceRange)
            {
                double[]? values = GetFloat32Values(member.Data);
                if (values == null || values.Length != 2 || values[0] > values[1])
                {
                    return $"Colour-encoding range member '{FourCcCodec.Format(signature)}' shall contain an ordered pair of float32 values.";
                }

                if (signature == EncodingRange)
                {
                    IccEncodingRange range = new IccEncodingRange();
                    range.Minimum = values[0];
                    range.Maximum = values[1];
                    projection.ChannelRanges = new IccEncodingRange[] { range };
                }
            }
            else if (signature == TransferFunction || signature == InverseTransferFunction)
            {
                if (!HasType(member.Data, SegmentedCurveType) && member.Data.Kind != IccTagDataKind.Curve)
                {
                    return WrongMemberType("colorEncodingParamsStructure", signature);
                }

                if (member.Data.Curve != null)
                {
                    projection.Curves = AppendCurve(projection.Curves, member.Data.Curve);
                }
            }
            else if (signature == BitDepth && (!HasType(member.Data, IccTypeSignatures.UInt8Array) || member.Data.IntegerValues.Length == 0))
            {
                return "The colour-encoding bit-depth member shall contain at least one unsigned 8-bit value.";
            }
            else if (signature == ImageState && !HasType(member.Data, IccTypeSignatures.Signature))
            {
                return WrongMemberType("colorEncodingParamsStructure", signature);
            }
            else if (IsColorEncodingScalar(signature))
            {
                double[]? values = GetFloat32Values(member.Data);
                if (values == null || values.Length != 1 || (signature == ViewingFlare && (values[0] < 0.0 || values[0] > 1.0)))
                {
                    return $"Colour-encoding scalar member '{FourCcCodec.Format(signature)}' has an invalid float32 value.";
                }
            }

            index = index + 1;
        }

        data.ColorEncodingParameters = projection;
        return string.Empty;
    }

    internal static string ProjectMeasurementStructure(IccTagStructure structure, IccTagData data)
    {
        IccMeasurement measurement = new IccMeasurement();
        IccStructureMember? illuminant = FindMember(structure, MeasurementIlluminant);
        IccStructureMember? range = FindMember(structure, MeasurementIlluminantRange);
        if ((illuminant == null) != (range == null))
        {
            return "Measurement illuminant samples and their spectral range shall either both be present or both be absent.";
        }

        int index = 0;
        while (index < structure.Members.Length)
        {
            IccStructureMember member = structure.Members[index];
            if (member.Signature == MeasurementBacking || member.Signature == MeasurementGeometry || member.Signature == MeasurementMode)
            {
                StructureProjectionNumber number = GetSingleNumber(member.Data);
                double maximum = member.Signature == MeasurementBacking ? 3.0 : member.Signature == MeasurementMode ? 4.0 : 2.0;
                if (!HasType(member.Data, IccTypeSignatures.UInt32Array) || !number.IsValid || number.Value < 0.0 || number.Value > maximum)
                {
                    return $"Measurement member '{FourCcCodec.Format(member.Signature)}' contains an invalid unsigned scalar value.";
                }

                if (member.Signature == MeasurementGeometry)
                {
                    measurement.Geometry = (IccMeasurementGeometry)(int)number.Value;
                }
            }
            else if (member.Signature == MeasurementFlare)
            {
                double[]? values = GetFloat32Values(member.Data);
                if (values == null || values.Length != 1 || values[0] < 0.0 || values[0] > 1.0)
                {
                    return "The measurement flare member shall contain one normalized float32 value.";
                }

                measurement.Flare = values[0];
            }
            else if (member.Signature == MeasurementIlluminant && !IsFloatSampleArray(member.Data))
            {
                return WrongMemberType("measurementInfoStructure", member.Signature);
            }
            else if (member.Signature == MeasurementIlluminantRange && !HasType(member.Data, SpectralRangeType))
            {
                return WrongMemberType("measurementInfoStructure", member.Signature);
            }

            index = index + 1;
        }

        data.Measurement = measurement;
        return string.Empty;
    }

    internal static string ProjectNamedColorStructure(TagDecodeState state, IccTagStructure structure, IccTagData data)
    {
        IccStructureMember? name = FindMember(structure, Name);
        if (name == null || !HasType(name.Data, IccTypeSignatures.Utf8) || name.Data.Text == null || name.Data.Text.Text.Length == 0)
        {
            return "A namedColorStructure shall contain a non-empty UTF-8 name member.";
        }

        IccNamedColor color = new IccNamedColor();
        color.Name = name.Data.Text.Text;
        color.Description.Name = color.Name;
        string error = ProjectNamedColorMembers(state, structure, color, false);
        if (error.Length != 0)
        {
            return error;
        }

        IccNamedColorSet set = new IccNamedColorSet();
        set.DeviceChannelCount = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        set.Colors = new IccNamedColor[] { color };
        data.NamedColors = set;
        return string.Empty;
    }

    internal static string ProjectTintZeroStructure(TagDecodeState state, IccTagStructure structure, IccTagData data)
    {
        IccNamedColor color = new IccNamedColor();
        string error = ProjectNamedColorMembers(state, structure, color, true);
        if (error.Length != 0)
        {
            return error;
        }

        IccNamedColorSet set = new IccNamedColorSet();
        set.DeviceChannelCount = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        set.Colors = new IccNamedColor[] { color };
        data.NamedColors = set;
        return string.Empty;
    }

    internal static string ProjectNamedColorMembers(TagDecodeState state, IccTagStructure structure, IccNamedColor color, bool tintZero)
    {
        int expectedTintCount = 0;
        int index = 0;
        while (index < structure.Members.Length)
        {
            IccStructureMember member = structure.Members[index];
            uint signature = member.Signature;
            if (signature == Name || signature == LocalizedName)
            {
                uint expected = signature == Name ? IccTypeSignatures.Utf8 : IccTypeSignatures.MultiLocalizedUnicode;
                if (tintZero || !HasType(member.Data, expected))
                {
                    return WrongMemberType(tintZero ? "tintZeroStructure" : "namedColorStructure", signature);
                }
            }
            else if (IsNamedColorSampleMember(signature))
            {
                bool spectral = signature == SpectralData || signature == SpectralOverBlack || signature == SpectralOverGray;
                if (!IsColorSampleArray(member.Data, spectral))
                {
                    return WrongMemberType(tintZero ? "tintZeroStructure" : "namedColorStructure", signature);
                }

                double[] values = GetNormalizedSamples(member.Data);
                int channels = GetMemberChannelCount(state, signature, values.Length);
                if (channels > 0 && values.Length % channels != 0)
                {
                    return $"Named-colour member '{FourCcCodec.Format(signature)}' does not contain a whole number of channel vectors.";
                }

                int vectorCount = channels == 0 ? 0 : values.Length / channels;
                if (tintZero && vectorCount > 1)
                {
                    return $"Tint-zero member '{FourCcCodec.Format(signature)}' shall contain exactly one channel vector.";
                }

                if (!tintZero && vectorCount > 0)
                {
                    if (expectedTintCount == 0)
                    {
                        expectedTintCount = vectorCount;
                    }
                    else if (expectedTintCount != vectorCount)
                    {
                        return "All named-colour sample members shall contain the same number of tint vectors.";
                    }
                }

                if (signature == DeviceData)
                {
                    color.DeviceColor = CreateColorVector(state.DataColorSpace, state.DataColorSpaceSignature, values);
                }
                else if (signature == PcsData)
                {
                    color.PcsColor = CreateColorVector(state.ProfileConnectionSpace == IccPcsKind.Xyz ? IccColorSpaceKind.Xyz : IccColorSpaceKind.Lab, state.ProfileConnectionSpaceSignature, values);
                }
            }
            else if (signature == TintValues)
            {
                if (tintZero || !IsTintArray(member.Data))
                {
                    return WrongMemberType(tintZero ? "tintZeroStructure" : "namedColorStructure", signature);
                }

                color.Tints = GetNormalizedSamples(member.Data);
                if (!IsIncreasingNormalizedTint(color.Tints))
                {
                    return "Named-colour tint values shall increase strictly from above zero through at most one.";
                }
            }
            else if (!tintZero && (signature == BrdfColorimetric || signature == BrdfSpectral) && !HasType(member.Data, IccTypeSignatures.MultiProcessElements))
            {
                return WrongMemberType("namedColorStructure", signature);
            }
            else if (!tintZero && (signature == BrdfColorimetricParameters || signature == BrdfSpectralParameters)
                && (!HasType(member.Data, IccTypeSignatures.TagStructure) || member.Data.TagStructure == null
                    || member.Data.TagStructure.StructureSignature != IccStructureSignatures.BrdfTransform))
            {
                return WrongMemberType("namedColorStructure", signature);
            }
            else if (!tintZero && signature == NormalMap && !HasType(member.Data, IccTypeSignatures.EmbeddedNormalImage))
            {
                return WrongMemberType("namedColorStructure", signature);
            }

            index = index + 1;
        }

        if (!tintZero && color.Tints.Length != 0 && expectedTintCount != 0 && color.Tints.Length != expectedTintCount)
        {
            return "The named-colour tint count shall match every sample member's vector count.";
        }

        return string.Empty;
    }

    internal static string ProjectPccStructure(TagDecodeState state, IccTagStructure structure, IccTagData data)
    {
        IccStructureMember? illuminant = FindMember(structure, PccIlluminant);
        IccStructureMember? mediaWhite = FindMember(structure, PccMediaWhitePoint);
        IccStructureMember? spectralWhite = FindMember(structure, PccSpectralWhitePoint);
        IccStructureMember? viewingConditions = FindMember(structure, PccViewingConditions);
        IccStructureMember? toStandard = FindMember(structure, PccCustomToStandard);
        IccStructureMember? fromStandard = FindMember(structure, PccStandardToCustom);
        bool isDevicePcc = state.HasProfileContext && state.TagSignature == IccTagSignatures.DevicePcc;
        bool usesColorimetricConnection = state.DataColorSpace == IccColorSpaceKind.Xyz || state.DataColorSpace == IccColorSpaceKind.Lab;
        bool usesSpectralConnection = state.DataColorSpace == IccColorSpaceKind.Spectral || state.DataColorSpace == IccColorSpaceKind.BiSpectral;
        if (isDevicePcc && (state.ProfileClass != IccProfileClass.Abstract || (!usesColorimetricConnection && !usesSpectralConnection)))
        {
            return "The devicePccTag is defined only for an abstract profile whose device colour space is a colorimetric or spectral PCS signature.";
        }

        if (illuminant == null || !IsSingleXyz(illuminant.Data))
        {
            return "A pccStructure shall contain one XYZ illuminant member.";
        }

        if ((!isDevicePcc || usesColorimetricConnection) && (mediaWhite == null || !IsSingleXyz(mediaWhite.Data)))
        {
            return "A pccStructure shall contain one XYZ illuminant member and one XYZ media-white-point member.";
        }

        if (isDevicePcc && usesSpectralConnection && (spectralWhite == null || !IsColorSampleArray(spectralWhite.Data, true)))
        {
            return "A spectral devicePccTag shall contain a spectral white-point member.";
        }

        if (isDevicePcc && usesSpectralConnection && spectralWhite != null
            && GetNormalizedSamples(spectralWhite.Data).Length != ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature))
        {
            return "The devicePccTag spectral white-point sample count shall match its spectral device colour-space channel count.";
        }

        if (isDevicePcc && (viewingConditions == null || toStandard == null || fromStandard == null))
        {
            return "A devicePccTag shall contain spectral viewing conditions and both PCC transform members.";
        }

        IccProfileConnectionConditions projection = new IccProfileConnectionConditions();
        projection.ViewingConditions.IlluminantColor = CreateXyzColor(illuminant.Data.XyzValues[0]);
        if (mediaWhite != null)
        {
            projection.MediaWhitePoint = CreateXyzColor(mediaWhite.Data.XyzValues[0]);
        }
        bool hasToStandard = false;
        bool hasFromStandard = false;
        int index = 0;
        while (index < structure.Members.Length)
        {
            IccStructureMember member = structure.Members[index];
            IccSpectralViewingConditions? spectralViewingConditions = member.Data.SpectralViewingConditions;
            if ((member.Signature == PccIlluminant || member.Signature == PccMediaWhitePoint) && !IsSingleXyz(member.Data))
            {
                return WrongMemberType("pccStructure", member.Signature);
            }
            else if (member.Signature == PccSpectralWhitePoint && !IsColorSampleArray(member.Data, true))
            {
                return WrongMemberType("pccStructure", member.Signature);
            }
            else if (member.Signature == PccSpectralWhitePoint)
            {
                projection.SpectralWhitePoint = GetNormalizedSamples(member.Data);
            }
            else if (member.Signature == PccViewingConditions)
            {
                if (!HasType(member.Data, IccTypeSignatures.SpectralViewingConditions) || spectralViewingConditions == null)
                {
                    return WrongMemberType("pccStructure", member.Signature);
                }

                projection.ViewingConditions = spectralViewingConditions;
            }
            else if (member.Signature == PccCustomToStandard || member.Signature == PccStandardToCustom)
            {
                if (!HasType(member.Data, IccTypeSignatures.MultiProcessElements))
                {
                    return WrongMemberType("pccStructure", member.Signature);
                }

                if (member.Signature == PccCustomToStandard)
                {
                    hasToStandard = true;
                    projection.ToStandardPcs = member.Data.MultiProcessElements;
                }
                else
                {
                    hasFromStandard = true;
                    projection.FromStandardPcs = member.Data.MultiProcessElements;
                }
            }

            index = index + 1;
        }

        if (hasToStandard != hasFromStandard)
        {
            return "The custom-to-standard and standard-to-custom PCC transforms shall either both be present or both be absent.";
        }

        data.ProfileConnectionConditions = projection;
        return string.Empty;
    }

    internal static string ProjectProfileInfoStructure(IccTagStructure structure, IccTagData data)
    {
        IccProfileSequenceEntry projection = new IccProfileSequenceEntry();
        int index = 0;
        while (index < structure.Members.Length)
        {
            IccStructureMember member = structure.Members[index];
            if (member.Signature == ProfileAttributes)
            {
                if (!HasType(member.Data, IccTypeSignatures.UInt64Array) || member.Data.IntegerValues.Length != 1 || member.Data.IntegerValues[0] < 0)
                {
                    return WrongMemberType("profileInfoStructure", member.Signature);
                }

                projection.DeviceAttributes = (ulong)member.Data.IntegerValues[0];
            }
            else if (member.Signature == ProfileDescription || member.Signature == ManufacturerDescription || member.Signature == ModelDescription)
            {
                if (!HasType(member.Data, IccTypeSignatures.MultiLocalizedUnicode))
                {
                    return WrongMemberType("profileInfoStructure", member.Signature);
                }

                if (member.Signature == ManufacturerDescription)
                {
                    projection.ManufacturerLocalizedDescriptions = member.Data.LocalizedStrings;
                    projection.ManufacturerDescription = CreateTextFromLocalized(member.Data.LocalizedStrings);
                }
                else if (member.Signature == ModelDescription)
                {
                    projection.ModelLocalizedDescriptions = member.Data.LocalizedStrings;
                    projection.ModelDescription = CreateTextFromLocalized(member.Data.LocalizedStrings);
                }
            }
            else if (member.Signature == ProfileId && (!HasType(member.Data, IccTypeSignatures.UInt8Array) || member.Data.IntegerValues.Length != 16))
            {
                return "The profile-info Profile ID member shall contain exactly 16 unsigned bytes.";
            }
            else if (member.Signature == ManufacturerSignature || member.Signature == ModelSignature || member.Signature == Technology)
            {
                if (!HasType(member.Data, IccTypeSignatures.Signature))
                {
                    return WrongMemberType("profileInfoStructure", member.Signature);
                }

                if (member.Signature == ManufacturerSignature)
                {
                    projection.DeviceManufacturerSignature = member.Data.Signature;
                }
                else if (member.Signature == ModelSignature)
                {
                    projection.DeviceModelSignature = member.Data.Signature;
                }
                else
                {
                    projection.TechnologySignature = member.Data.Signature;
                }
            }
            else if (member.Signature == RenderingTransform && (!HasType(member.Data, IccTypeSignatures.UInt32Array) || member.Data.IntegerValues.Length != 1))
            {
                return WrongMemberType("profileInfoStructure", member.Signature);
            }

            index = index + 1;
        }

        data.ProfileSequence = new IccProfileSequenceEntry[] { projection };
        return string.Empty;
    }

    internal static IccStructureMember? FindMember(IccTagStructure structure, uint signature)
    {
        int index = 0;
        while (index < structure.Members.Length)
        {
            if (structure.Members[index].Signature == signature)
            {
                return structure.Members[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool HasType(IccTagData data, uint typeSignature)
    {
        if (data.RawData != null && data.RawData.HasTypeSignature)
        {
            return data.RawData.TypeSignature == typeSignature;
        }

        if (data.Kind == IccTagDataKind.Text && data.Text != null)
        {
            return data.Text.TypeSignature == typeSignature;
        }

        switch (data.Kind)
        {
            case IccTagDataKind.Signature:
                return typeSignature == IccTypeSignatures.Signature;
            case IccTagDataKind.LocalizedText:
                return typeSignature == IccTypeSignatures.MultiLocalizedUnicode;
            case IccTagDataKind.XyzArray:
                return typeSignature == IccTypeSignatures.Xyz;
            case IccTagDataKind.MultiProcessElements:
                return typeSignature == IccTypeSignatures.MultiProcessElements;
            case IccTagDataKind.SpectralViewingConditions:
                return typeSignature == IccTypeSignatures.SpectralViewingConditions;
            case IccTagDataKind.TagStructure:
                return typeSignature == IccTypeSignatures.TagStructure;
            default:
                return false;
        }
    }

    internal static StructureProjectionNumber GetSingleNumber(IccTagData data)
    {
        if (data.Kind == IccTagDataKind.IntegerArray && data.IntegerValues != null && data.IntegerValues.Length == 1)
        {
            return new StructureProjectionNumber(true, data.IntegerValues[0]);
        }

        if (data.Kind == IccTagDataKind.FloatingPointArray && data.FloatingPointValues != null && data.FloatingPointValues.Length == 1)
        {
            return new StructureProjectionNumber(true, data.FloatingPointValues[0]);
        }

        return new StructureProjectionNumber(false, 0.0);
    }

    internal static bool IsColorSampleArray(IccTagData data, bool allowSparse)
    {
        uint typeSignature = GetKnownType(data);
        return typeSignature == IccTypeSignatures.UInt8Array || typeSignature == IccTypeSignatures.UInt16Array
            || typeSignature == IccTypeSignatures.Float16Array || typeSignature == IccTypeSignatures.Float32Array
            || (allowSparse && typeSignature == IccTypeSignatures.SparseMatrixArray);
    }

    internal static bool IsFloatSampleArray(IccTagData data)
    {
        uint typeSignature = GetKnownType(data);
        return typeSignature == IccTypeSignatures.Float16Array || typeSignature == IccTypeSignatures.Float32Array;
    }

    internal static bool IsTintArray(IccTagData data)
    {
        uint typeSignature = GetKnownType(data);
        return typeSignature == IccTypeSignatures.UInt8Array || typeSignature == IccTypeSignatures.UInt16Array || typeSignature == IccTypeSignatures.Float32Array;
    }

    internal static uint GetKnownType(IccTagData data)
    {
        return data.RawData != null && data.RawData.HasTypeSignature ? data.RawData.TypeSignature : 0;
    }

    internal static double[] GetNormalizedSamples(IccTagData data)
    {
        uint typeSignature = GetKnownType(data);
        if (data.Kind == IccTagDataKind.IntegerArray)
        {
            double divisor = typeSignature == IccTypeSignatures.UInt8Array ? 255.0 : 65535.0;
            double[] values = new double[data.IntegerValues.Length];
            int index = 0;
            while (index < values.Length)
            {
                values[index] = data.IntegerValues[index] / divisor;
                index = index + 1;
            }

            return values;
        }

        return data.Kind == IccTagDataKind.FloatingPointArray ? CloneDoubles(data.FloatingPointValues) : new double[0];
    }

    internal static double[]? GetFloat32Values(IccTagData data)
    {
        return HasType(data, IccTypeSignatures.Float32Array) && data.Kind == IccTagDataKind.FloatingPointArray ? data.FloatingPointValues : null;
    }

    internal static bool IsChromaticity(double[]? values)
    {
        if (values == null || (values.Length != 2 && values.Length != 3))
        {
            return false;
        }

        double sum = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] < 0.0 || values[index] > 1.0)
            {
                return false;
            }

            sum = sum + values[index];
            index = index + 1;
        }

        return values.Length == 2 ? sum <= 1.0 : System.Math.Abs(sum - 1.0) <= 0.00001;
    }

    internal static bool IsChromaticityMember(uint signature)
    {
        return signature == BluePrimary || signature == GreenPrimary || signature == RedPrimary || signature == WhitePointChromaticity
            || signature == AmbientWhiteChromaticity || signature == MediumWhiteChromaticity || signature == MediumBlackChromaticity;
    }

    internal static IccXyzColor CreateChromaticity(double[] values)
    {
        IccXyzColor color = new IccXyzColor();
        color.X = values[0];
        color.Y = values[1];
        color.Z = values.Length == 2 ? 1.0 - values[0] - values[1] : values[2];
        return color;
    }

    internal static bool IsColorEncodingScalar(uint signature)
    {
        return signature == WhitePointLuminance || signature == ImageBackground || signature == ViewingSurround
            || signature == AmbientIlluminance || signature == AmbientWhiteLuminance || signature == ViewingFlare
            || signature == MediumWhiteLuminance || signature == MediumBlackLuminance;
    }

    internal static bool IsNamedColorSampleMember(uint signature)
    {
        return signature == DeviceData || signature == PcsData || signature == SpectralData || signature == SpectralOverBlack || signature == SpectralOverGray;
    }

    internal static IccCurve[] AppendCurve(IccCurve[] values, IccCurve value)
    {
        IccCurve[] result = new IccCurve[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index = index + 1;
        }

        result[values.Length] = value;
        return result;
    }

    internal static int GetMemberChannelCount(TagDecodeState state, uint signature, int fallback)
    {
        if (signature == DeviceData)
        {
            return ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        }

        if (signature == PcsData)
        {
            return ColorSpaceChannelCount.Decode(state.ProfileConnectionSpaceSignature);
        }

        return fallback;
    }

    internal static bool IsIncreasingNormalizedTint(double[] values)
    {
        if (values.Length == 0)
        {
            return false;
        }

        double previous = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] <= previous || values[index] > 1.0)
            {
                return false;
            }

            previous = values[index];
            index = index + 1;
        }

        return true;
    }

    internal static bool IsSingleXyz(IccTagData data)
    {
        return HasType(data, IccTypeSignatures.Xyz) && data.XyzValues != null && data.XyzValues.Length == 1;
    }

    internal static IccXyzColor CreateXyzColor(IccXyzNumber number)
    {
        IccXyzColor color = new IccXyzColor();
        color.X = number.X;
        color.Y = number.Y;
        color.Z = number.Z;
        return color;
    }

    internal static IccColorVector CreateColorVector(IccColorSpaceKind kind, uint signature, double[] values)
    {
        IccColorVector vector = new IccColorVector();
        vector.ColorSpace = kind;
        vector.ColorSpaceSignature = signature;
        vector.Components = values;
        return vector;
    }

    internal static IccTextData CreateTextFromLocalized(IccLocalizedString[] values)
    {
        IccTextData text = new IccTextData();
        text.TypeSignature = IccTypeSignatures.MultiLocalizedUnicode;
        if (values != null && values.Length != 0)
        {
            text.Text = values[0].Text;
        }

        return text;
    }

    internal static double[] CloneDoubles(double[] values)
    {
        double[] clone = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            clone[index] = values[index];
            index = index + 1;
        }

        return clone;
    }

    internal static string WrongMemberType(string structureName, uint memberSignature)
    {
        return $"The {structureName} member '{FourCcCodec.Format(memberSignature)}' has a tag type that is not permitted for that member.";
    }
}
