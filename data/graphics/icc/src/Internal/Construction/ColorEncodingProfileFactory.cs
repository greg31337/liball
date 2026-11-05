namespace icc;

internal class ColorEncodingProfileFactory
{
    internal ProfileConstructionCore Core;

    internal ColorEncodingProfileFactory(ProfileConstructionCore core)
    {
        Core = core;
    }

    internal IccProfile CreateColorEncodingProfile(IccProfileBuildOptions options)
    {
        IccColorEncodingProfileDefinition definition = ValidateOptions(options);
        bool abbreviated = options.ProfileClass
            == IccProfileClass.ColorEncodingSpace;
        IccProfile profile = new IccProfile();
        profile.Header = abbreviated ? CreateAbbreviatedHeader(options)
            : Core.CreateSkeletonHeader(options);
        ApplySpectralHeader(profile.Header, definition);
        System.Collections.Generic.List<IccTag> tags = abbreviated
            ? new System.Collections.Generic.List<IccTag>()
            : new System.Collections.Generic.List<IccTag>(
                Core.CreateSkeletonTags(options));
        AddOptionalDescriptions(tags, options, abbreviated);
        AddEncodingTags(tags, definition);
        if (!abbreviated)
        {
            AddConnectionConditionTags(tags, definition);
            AddFallbackTags(tags, definition);
        }

        profile.Tags = tags.ToArray();
        profile.TagTableEntries = Core.BuildTableEntries(profile.Tags);
        IccProfile result = Core.SerializeProfile(profile, false);
        ValidateExecutableTransforms(result);
        return Core.ValidateConformingProfile(result);
    }

    internal IccColorEncodingProfileDefinition ValidateOptions(
        IccProfileBuildOptions options)
    {
        if (options == null || options.Version == null
            || options.Descriptions == null || options.Copyright == null
            || options.CreationDateTime == null)
        {
            throw new IccProfileValidationException("Colour-encoding profile options and required metadata fields cannot be null.");
        }
        IccColorEncodingProfileDefinition? candidate =
            options.ColorEncodingDefinition;
        if (candidate == null || candidate.ReferenceName == null
            || candidate.ColorSpaceName == null
            || candidate.SpectralPcsRange == null
            || candidate.BiSpectralPcsRange == null)
        {
            throw new IccProfileValidationException("Colour-encoding construction requires a complete definition with non-null names and spectral ranges.");
        }
        if (candidate.ReferenceName.Length == 0)
        {
            throw new IccProfileValidationException("A colour-encoding profile requires a registered reference name.");
        }
        if (options.Version.Kind != IccVersionKind.Version5)
        {
            throw new IccProfileValidationException("Colour-encoding profile construction requires ICC version 5.");
        }
        ValidateVersionFive(options.Version);
        bool abbreviated = options.ProfileClass
            == IccProfileClass.ColorEncodingSpace;
        if (!abbreviated && options.ProfileClass != IccProfileClass.ColorSpace)
        {
            throw new IccProfileValidationException("Colour-encoding construction requires the ColorEncodingSpace or ColorSpace profile class.");
        }
        if (options.DataColorSpaceSignature != IccColorSpaceSignatures.Rgb
            && options.DataColorSpaceSignature != IccColorSpaceSignatures.YCbCr)
        {
            throw new IccProfileValidationException("A colour-encoding profile requires RGB or YCbCr data.");
        }
        RejectOtherDefinitions(options);
        Core.ValidateDescriptions(options.Descriptions,
            IccVersionKind.Version5);
        if (options.Copyright.Text == null)
        {
            throw new IccProfileValidationException("Profile copyright text cannot be null.");
        }
        if (!abbreviated)
        {
            ValidateFullProfileOptions(options);
        }

        ValidateParameters(candidate.Parameters);
        string colorSpaceName = ResolveColorSpaceName(candidate);
        if (candidate.ReferenceName == "ISO 22028-1"
            && (candidate.Parameters == null || colorSpaceName.Length == 0))
        {
            throw new IccProfileValidationException("The ISO 22028-1 reference requires colour-encoding parameters and an associated colour-space name.");
        }
        ValidateSpectralDefinition(candidate);
        if (abbreviated)
        {
            if (candidate.ProfileConnectionConditions != null
                || candidate.ForwardFallbackTransform != null
                || candidate.BackwardFallbackTransform != null
                || candidate.SpectralPcsSignature != 0)
            {
                throw new IccProfileValidationException("An abbreviated ColorEncodingSpace profile cannot contain PCC, spectral PCS, or fallback transform data.");
            }
        }
        else
        {
            ValidateConnectionConditions(candidate);
            ValidateFallbackTransforms(candidate, 3);
        }
        return candidate;
    }

    internal void ValidateVersionFive(IccVersion version)
    {
        if ((version.Major != 0
                && version.Major != IccConstants.Version5Major)
            || version.Minor != 0 || version.BugFix < 0
            || version.BugFix > 9 || version.SubclassMajor != 0
            || version.SubclassMinor != 0)
        {
            throw new IccProfileValidationException("Colour-encoding construction requires a supported ICC version 5.0 definition without a profile subclass.");
        }
    }

    internal void ValidateFullProfileOptions(IccProfileBuildOptions options)
    {
        if (options.MediaWhitePoint == null)
        {
            throw new IccProfileValidationException("A full colour-encoding profile requires a media white point.");
        }
        if (!Core.IsValidRenderingIntent(options.RenderingIntent))
        {
            throw new IccProfileValidationException("The profile rendering intent is invalid.");
        }
        if (options.ProfileConnectionSpaceSignature
                != IccColorSpaceSignatures.Xyz
            && options.ProfileConnectionSpaceSignature
                != IccColorSpaceSignatures.Lab)
        {
            throw new IccProfileValidationException("A full colour-encoding profile requires PCSXYZ or PCSLAB.");
        }
        Core.ValidateDateTime(options.CreationDateTime);
        Core.ValidateMediaWhite(options.MediaWhitePoint);
        if (options.ChromaticAdaptationMatrix != null)
        {
            Core.ValidateChromaticAdaptationMatrix(
                options.ChromaticAdaptationMatrix);
        }
    }

    internal void RejectOtherDefinitions(IccProfileBuildOptions options)
    {
        if (options.RgbDefinition != null || options.GrayDefinition != null
            || options.AbstractDefinition != null
            || options.AdaptiveGainCurve != null
            || options.NamedColorDefinition != null
            || options.OutputDefinition != null)
        {
            throw new IccProfileValidationException("Colour-encoding construction cannot be combined with another profile definition.");
        }
    }

    internal string ResolveColorSpaceName(
        IccColorEncodingProfileDefinition definition)
    {
        if (definition.ColorSpaceName.Length != 0)
        {
            return definition.ColorSpaceName;
        }
        return definition.Parameters == null ? string.Empty
            : definition.Parameters.Name;
    }

    internal void ValidateParameters(IccColorEncodingParameters? parameters)
    {
        if (parameters == null)
        {
            return;
        }
        if (parameters.Name == null || parameters.ChannelRanges == null
            || parameters.ReferenceWhite == null || parameters.Curves == null
            || parameters.Metadata == null)
        {
            throw new IccProfileValidationException("Colour-encoding parameters and their collections cannot be null.");
        }
        if (parameters.ChannelRanges.Length > 1
            || parameters.Curves.Length > 1)
        {
            throw new IccProfileValidationException("A colorEncodingParamsStructure can contain one shared encoding range and one shared transfer function.");
        }
        if (parameters.ChannelRanges.Length == 1)
        {
            IccEncodingRange range = parameters.ChannelRanges[0];
            if (range == null || !System.Double.IsFinite(range.Minimum)
                || !System.Double.IsFinite(range.Maximum)
                || range.Minimum > range.Maximum)
            {
                throw new IccProfileValidationException("The colour-encoding range must be finite and ordered.");
            }
        }
        if (parameters.Curves.Length == 1)
        {
            CreateTransferCurve(parameters.Curves[0]);
        }
        IccXyzColor white = parameters.ReferenceWhite;
        double whiteSum = white.X + white.Y + white.Z;
        if (!System.Double.IsFinite(white.X) || !System.Double.IsFinite(white.Y)
            || !System.Double.IsFinite(white.Z) || white.X < 0.0
            || white.Y < 0.0 || white.Z < 0.0 || whiteSum < 0.0)
        {
            throw new IccProfileValidationException("The colour-encoding reference white must contain finite non-negative values.");
        }
        if (parameters.Matrix != null)
        {
            IccMatrix matrix = parameters.Matrix;
            if (matrix.Rows != 3 || matrix.Columns != 3
                || matrix.Values == null || matrix.Values.Length != 9)
            {
                throw new IccProfileValidationException("The colour-encoding conversion matrix must contain three rows and three columns.");
            }
            int matrixIndex = 0;
            while (matrixIndex < matrix.Values.Length)
            {
                if (!System.Double.IsFinite(matrix.Values[matrixIndex]))
                {
                    throw new IccProfileValidationException("The colour-encoding conversion matrix must contain only finite values.");
                }
                matrixIndex = matrixIndex + 1;
            }
        }
        DictionaryTagCodec.SerializeDictionary(parameters.Metadata);
    }

    internal void ValidateSpectralDefinition(
        IccColorEncodingProfileDefinition definition)
    {
        IccSpectralRanges ranges = new IccSpectralRanges();
        ranges.SpectralRange = definition.SpectralPcsRange;
        ranges.BiSpectralRange = definition.BiSpectralPcsRange;
        if (definition.SpectralPcsSignature == 0)
        {
            if (!IsZeroRange(definition.SpectralPcsRange)
                || !IsZeroRange(definition.BiSpectralPcsRange))
            {
                throw new IccProfileValidationException("Spectral PCS ranges require a spectral PCS signature.");
            }
            return;
        }

        SpectralRangeTagCodec.SerializeSpectralRanges(ranges);
        IccColorSpaceKind kind = HeaderCodec.DecodeSpectralPcs(
            definition.SpectralPcsSignature);
        int channels = ColorSpaceChannelCount.Decode(
            definition.SpectralPcsSignature);
        bool invalid = kind == IccColorSpaceKind.Spectral
            ? channels != definition.SpectralPcsRange.Steps
                || !IsZeroRange(definition.BiSpectralPcsRange)
            : kind == IccColorSpaceKind.BiSpectral
                ? channels != (long)definition.SpectralPcsRange.Steps
                    * (long)definition.BiSpectralPcsRange.Steps
                : true;
        if (invalid)
        {
            throw new IccProfileValidationException("The spectral PCS signature and wavelength ranges have inconsistent sample counts.");
        }
    }

    internal bool IsZeroRange(IccSpectralRange range)
    {
        return range.StartWavelength == 0.0 && range.EndWavelength == 0.0
            && range.Steps == 0;
    }

    internal void ValidateConnectionConditions(
        IccColorEncodingProfileDefinition definition)
    {
        IccProfileConnectionConditions? conditions =
            definition.ProfileConnectionConditions;
        if (conditions == null)
        {
            return;
        }
        if (conditions.ViewingConditions == null
            || conditions.MediaWhitePoint == null
            || conditions.SpectralWhitePoint == null)
        {
            throw new IccProfileValidationException("PCC viewing conditions, media white, and spectral white data cannot be null.");
        }
        Core.ValidateMediaWhite(conditions.MediaWhitePoint);
        SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(
            conditions.ViewingConditions);
        int spectralChannels = ColorSpaceChannelCount.Decode(
            definition.SpectralPcsSignature);
        if (conditions.SpectralWhitePoint.Length != 0
            && conditions.SpectralWhitePoint.Length != spectralChannels)
        {
            throw new IccProfileValidationException("The PCC spectral white point must match the spectral PCS channel count.");
        }
        int index = 0;
        while (index < conditions.SpectralWhitePoint.Length)
        {
            if (!System.Double.IsFinite(conditions.SpectralWhitePoint[index]))
            {
                throw new IccProfileValidationException("The PCC spectral white point must contain finite values.");
            }
            index = index + 1;
        }
        bool hasToStandard = conditions.ToStandardPcs != null;
        bool hasFromStandard = conditions.FromStandardPcs != null;
        if (hasToStandard != hasFromStandard)
        {
            throw new IccProfileValidationException("Custom and standard PCC transforms must be supplied as a pair.");
        }
        if (hasToStandard)
        {
            ValidateMpe(conditions.ToStandardPcs, 3, 3,
                "custom-to-standard PCC");
            ValidateMpe(conditions.FromStandardPcs, 3, 3,
                "standard-to-custom PCC");
        }
    }

    internal void ValidateFallbackTransforms(
        IccColorEncodingProfileDefinition definition, int pcsChannels)
    {
        bool hasForward = definition.ForwardFallbackTransform != null;
        bool hasBackward = definition.BackwardFallbackTransform != null;
        if (hasForward != hasBackward)
        {
            throw new IccProfileValidationException("Forward and backward colour-encoding fallback transforms must be supplied as a pair.");
        }
        if (hasForward)
        {
            int dataChannels = ColorSpaceChannelCount.Decode(
                IccColorSpaceSignatures.Rgb);
            ValidateMpe(definition.ForwardFallbackTransform, dataChannels,
                pcsChannels, "forward fallback");
            ValidateMpe(definition.BackwardFallbackTransform, pcsChannels,
                dataChannels, "backward fallback");
        }
    }

    internal void ValidateMpe(IccMultiProcessElements? pipeline,
        int inputChannels, int outputChannels, string description)
    {
        if (pipeline == null || pipeline.InputChannels != inputChannels
            || pipeline.OutputChannels != outputChannels)
        {
            throw new IccProfileValidationException($"The {description} transform has incorrect endpoint channel counts.");
        }
        MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(
            pipeline);
    }

    internal IccProfileHeader CreateAbbreviatedHeader(
        IccProfileBuildOptions options)
    {
        IccProfileHeader header = new IccProfileHeader();
        header.Version.Kind = IccVersionKind.Version5;
        header.Version.Major = IccConstants.Version5Major;
        header.Version.Minor = options.Version.Minor;
        header.Version.BugFix = options.Version.BugFix;
        header.Version.IsSupported = true;
        header.Version.PackedValue = ((uint)IccConstants.Version5Major << 24)
            | ((uint)options.Version.BugFix << 16);
        header.ProfileClass = IccProfileClass.ColorEncodingSpace;
        header.ProfileClassSignature =
            IccProfileClassSignatures.ColorEncodingSpace;
        header.DataColorSpaceSignature = options.DataColorSpaceSignature;
        header.DataColorSpace = HeaderCodec.DecodeDataColorSpace(
            options.DataColorSpaceSignature, IccVersionKind.Version5);
        header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        return header;
    }

    internal void ApplySpectralHeader(IccProfileHeader header,
        IccColorEncodingProfileDefinition definition)
    {
        header.SpectralPcsSignature = definition.SpectralPcsSignature;
        header.SpectralPcs = HeaderCodec.DecodeSpectralPcs(
            definition.SpectralPcsSignature);
        header.SpectralPcsRangeStart =
            definition.SpectralPcsRange.StartWavelength;
        header.SpectralPcsRangeEnd =
            definition.SpectralPcsRange.EndWavelength;
        header.SpectralPcsRangeSteps = definition.SpectralPcsRange.Steps;
        header.BiSpectralPcsRangeStart =
            definition.BiSpectralPcsRange.StartWavelength;
        header.BiSpectralPcsRangeEnd =
            definition.BiSpectralPcsRange.EndWavelength;
        header.BiSpectralPcsRangeSteps =
            definition.BiSpectralPcsRange.Steps;
    }

    internal void AddOptionalDescriptions(
        System.Collections.Generic.List<IccTag> tags,
        IccProfileBuildOptions options, bool abbreviated)
    {
        if (!abbreviated)
        {
            return;
        }
        if (options.Descriptions.Length != 0)
        {
            tags.Add(Core.CreateLocalizedTag(
                IccTagSignatures.ProfileDescription,
                Core.CloneLocalizedStrings(options.Descriptions)));
        }
        if (options.Copyright.Text.Length != 0)
        {
            tags.Add(Core.CreateCopyrightTag(options));
        }
    }

    internal void AddEncodingTags(
        System.Collections.Generic.List<IccTag> tags,
        IccColorEncodingProfileDefinition definition)
    {
        tags.Add(CreateUtf8Tag(IccTagSignatures.ReferenceName,
            definition.ReferenceName));
        string colorSpaceName = ResolveColorSpaceName(definition);
        if (colorSpaceName.Length != 0)
        {
            tags.Add(CreateUtf8Tag(IccTagSignatures.ColorSpaceName,
                colorSpaceName));
        }
        if (definition.Parameters != null)
        {
            tags.Add(CreateParametersTag(definition.Parameters));
            if (definition.Parameters.Metadata.Length != 0)
            {
                tags.Add(CreateMetadataTag(definition.Parameters.Metadata));
            }
        }
    }

    internal IccTag CreateUtf8Tag(uint signature, string value)
    {
        return Core.CreateTextTag(signature, value, IccTypeSignatures.Utf8);
    }

    internal IccTag CreateParametersTag(
        IccColorEncodingParameters parameters)
    {
        System.Collections.Generic.List<IccStructureMember> members =
            new System.Collections.Generic.List<IccStructureMember>();
        double whiteSum = parameters.ReferenceWhite.X
            + parameters.ReferenceWhite.Y + parameters.ReferenceWhite.Z;
        if (whiteSum > 0.0)
        {
            members.Add(CreateFloatMember(
                StructuredTagProjectionCodec.WhitePointChromaticity,
                new double[]
                {
                    parameters.ReferenceWhite.X / whiteSum,
                    parameters.ReferenceWhite.Y / whiteSum,
                    parameters.ReferenceWhite.Z / whiteSum
                }));
        }
        if (parameters.ChannelRanges.Length == 1)
        {
            members.Add(CreateFloatMember(
                StructuredTagProjectionCodec.EncodingRange,
                new double[]
                {
                    parameters.ChannelRanges[0].Minimum,
                    parameters.ChannelRanges[0].Maximum
                }));
        }
        if (parameters.Curves.Length == 1)
        {
            IccTagData curveData = new IccTagData();
            curveData.Kind = IccTagDataKind.Curve;
            curveData.Curve = CreateTransferCurve(parameters.Curves[0]);
            curveData.RawData.HasTypeSignature = true;
            curveData.RawData.TypeSignature =
                StructuredTagProjectionCodec.SegmentedCurveType;
            IccStructureMember curveMember = new IccStructureMember();
            curveMember.Signature =
                StructuredTagProjectionCodec.TransferFunction;
            curveMember.Data = curveData;
            members.Add(curveMember);
        }
        if (parameters.Matrix != null)
        {
            members.Add(CreateFloatMember(
                StructuredTagProjectionCodec.LumaChromaMatrix,
                Core.CloneDoubles(parameters.Matrix.Values)));
        }

        IccTagStructure structure = new IccTagStructure();
        structure.StructureSignature =
            IccStructureSignatures.ColorEncodingParameters;
        structure.Members = members.ToArray();
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.TagStructure;
        data.TagStructure = structure;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.TagStructure;
        return Core.CreateTag(IccTagSignatures.ColorEncodingParameters,
            data);
    }

    internal IccCurve CreateTransferCurve(IccCurve source)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The colour-encoding transfer function cannot be null.");
        }
        if (source.Kind == IccCurveKind.Segmented)
        {
            IccCurve clone = ExtendedTagDataClone.CloneCurve(source,
                new ProfileCloneContext());
            Icc2ProcessingElementTagCodec.SerializeIcc2SegmentedCurve(clone,
                0);
            return clone;
        }
        if (source.Kind != IccCurveKind.Identity
            && source.Kind != IccCurveKind.Gamma)
        {
            throw new IccProfileValidationException("A colour-encoding transfer function must be identity, gamma, or an ICC.2 segmented curve.");
        }
        if (!System.Double.IsFinite(source.Gamma) || source.Gamma <= 0.0)
        {
            throw new IccProfileValidationException("A colour-encoding gamma transfer function requires a finite positive exponent.");
        }
        IccCurveSegment segment = new IccCurveSegment();
        segment.Kind = IccCurveSegmentKind.Formula;
        segment.FormulaType = 0;
        segment.BreakPoint = double.NegativeInfinity;
        segment.Parameters = new double[]
        {
            source.Kind == IccCurveKind.Identity ? 1.0 : source.Gamma,
            1.0,
            0.0,
            0.0
        };
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Segmented;
        curve.Segments = new IccCurveSegment[] { segment };
        return curve;
    }

    internal IccStructureMember CreateFloatMember(uint signature,
        double[] values)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.FloatingPointArray;
        data.FloatingPointValues = values;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.Float32Array;
        IccStructureMember member = new IccStructureMember();
        member.Signature = signature;
        member.Data = data;
        return member;
    }

    internal IccTag CreateMetadataTag(IccMetadataEntry[] entries)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Dictionary;
        data.Metadata = ExtendedTagDataClone.CloneMetadataEntryArray(entries,
            "colour-encoding metadata", new ProfileCloneContext());
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.Dictionary;
        return Core.CreateTag(IccTagSignatures.Metadata, data);
    }

    internal void AddConnectionConditionTags(
        System.Collections.Generic.List<IccTag> tags,
        IccColorEncodingProfileDefinition definition)
    {
        IccProfileConnectionConditions? conditions =
            definition.ProfileConnectionConditions;
        if (conditions == null)
        {
            return;
        }
        ReplaceTag(tags, Core.CreateMediaWhiteTag(
            conditions.MediaWhitePoint));
        IccTagData viewingData = new IccTagData();
        viewingData.Kind = IccTagDataKind.SpectralViewingConditions;
        viewingData.SpectralViewingConditions =
            ExtendedTagDataClone.CloneSpectralViewingConditions(
                conditions.ViewingConditions);
        viewingData.RawData.HasTypeSignature = true;
        viewingData.RawData.TypeSignature =
            IccTypeSignatures.SpectralViewingConditions;
        tags.Add(Core.CreateTag(IccTagSignatures.SpectralViewingConditions,
            viewingData));
        if (conditions.SpectralWhitePoint.Length != 0)
        {
            IccTagData whiteData = new IccTagData();
            whiteData.Kind = IccTagDataKind.FloatingPointArray;
            whiteData.FloatingPointValues = Core.CloneDoubles(
                conditions.SpectralWhitePoint);
            whiteData.RawData.HasTypeSignature = true;
            whiteData.RawData.TypeSignature = IccTypeSignatures.Float32Array;
            tags.Add(Core.CreateTag(IccTagSignatures.SpectralWhitePoint,
                whiteData));
        }
        if (conditions.ToStandardPcs != null)
        {
            tags.Add(CreateMpeTag(IccTagSignatures.CustomToStandardPcc,
                conditions.ToStandardPcs));
            tags.Add(CreateMpeTag(IccTagSignatures.StandardToCustomPcc,
                conditions.FromStandardPcs));
        }
    }

    internal void AddFallbackTags(
        System.Collections.Generic.List<IccTag> tags,
        IccColorEncodingProfileDefinition definition)
    {
        if (definition.ForwardFallbackTransform == null)
        {
            return;
        }
        tags.Add(CreateMpeTag(IccTagSignatures.AToB0,
            definition.ForwardFallbackTransform));
        tags.Add(CreateMpeTag(IccTagSignatures.BToA0,
            definition.BackwardFallbackTransform));
    }

    internal IccTag CreateMpeTag(uint signature,
        IccMultiProcessElements? pipeline)
    {
        if (pipeline == null)
        {
            throw new IccProfileValidationException("A paired ICC.2 transform is missing.");
        }
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.MultiProcessElements;
        data.MultiProcessElements = ExtendedTagDataClone.CloneMultiProcessElements(
            pipeline, new ProfileCloneContext());
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        return Core.CreateTag(signature, data);
    }

    internal void ReplaceTag(System.Collections.Generic.List<IccTag> tags,
        IccTag replacement)
    {
        int index = 0;
        while (index < tags.Count)
        {
            if (tags[index].Signature == replacement.Signature)
            {
                tags[index] = replacement;
                return;
            }
            index = index + 1;
        }
        tags.Add(replacement);
    }

    internal void ValidateExecutableTransforms(IccProfile profile)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            IccTransformDirection direction =
                IccTransformDirection.DeviceToDevice;
            bool compile = tag.Signature
                == IccTagSignatures.CustomToStandardPcc
                || tag.Signature == IccTagSignatures.StandardToCustomPcc;
            if (tag.Signature == IccTagSignatures.AToB0)
            {
                compile = true;
                direction = IccTransformDirection.DeviceToPcs;
            }
            else if (tag.Signature == IccTagSignatures.BToA0)
            {
                compile = true;
                direction = IccTransformDirection.PcsToDevice;
            }
            if (compile)
            {
                MpePipelineCompiler.Compile(profile, tag, direction,
                    IccInterpolationKind.Multilinear);
            }
            index = index + 1;
        }
    }
}
