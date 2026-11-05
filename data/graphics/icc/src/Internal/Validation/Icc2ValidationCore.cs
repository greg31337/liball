namespace icc;

internal static class Icc2ValidationCore
{
    internal static IccValidationIssue[] Validate(IccProfile profile, IccValidationPolicy policy)
    {
        System.Collections.Generic.List<IccValidationIssue> issues =
            new System.Collections.Generic.List<IccValidationIssue>();
        if (profile == null || profile.Header == null || profile.Header.Version == null
            || profile.Tags == null || profile.Header.Version.Kind != IccVersionKind.Version5)
        {
            return issues.ToArray();
        }

        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Data != null)
            {
                ValidateSharedTypeModel(profile, tag, index, issues);
                ValidateTagRegistration(profile, tag, index, issues);
                ValidateTagData(profile, tag, tag.Data, index, "Data", 0, policy, issues);
                ValidateTagProjection(profile, tag, index, policy, issues);
            }
            index = index + 1;
        }

        ValidateSpectralProfile(profile, issues);
        ValidateMultiplexProfile(profile, issues);
        ValidateAbbreviatedProfile(profile, issues);
        ValidateSubclassAndExtendedSpace(profile, issues);
        return issues.ToArray();
    }

    internal static void ValidateTagRegistration(IccProfile profile, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        Icc2TagDefinition? definition = Icc2TagRegistry.FindDefinition(tag.Signature);
        if (definition == null)
        {
            return;
        }
        uint typeSignature = Icc1TagValidationCore.ResolveTypeSignature(tag);
        if (typeSignature != 0
            && !Icc2TagRegistry.ContainsType(definition.PermittedTypes, typeSignature))
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.TagTypeCompatibility,
                $"ICC.2 tag '{FourCcCodec.Format(tag.Signature)}' contains type '{FourCcCodec.Format(typeSignature)}', but permits {Icc2TagRegistry.FormatPermittedTypes(definition.PermittedTypes)}.",
                tag, index, "Data", "Use one of the registered ICC.2 types for this tag signature."));
        }
        if (!Icc2TagRegistry.IsProfileClassAllowed(definition, profile.Header.ProfileClass))
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ForbiddenTag,
                $"ICC.2 tag '{FourCcCodec.Format(tag.Signature)}' is not permitted in the {profile.Header.ProfileClass} profile class.",
                tag, index, "Data", "Remove the tag or use a profile class in which it is defined."));
        }
        if (definition.ContainerSignature != 0)
        {
            uint actual = 0;
            if (typeSignature == IccTypeSignatures.TagArray && tag.Data.TagArray != null)
            {
                actual = tag.Data.TagArray.ArraySignature;
            }
            else if (typeSignature == IccTypeSignatures.TagStructure
                && tag.Data.TagStructure != null)
            {
                actual = tag.Data.TagStructure.StructureSignature;
            }
            if (actual != 0 && actual != definition.ContainerSignature)
            {
                issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxHierarchy,
                    $"ICC.2 tag '{FourCcCodec.Format(tag.Signature)}' requires container '{FourCcCodec.Format(definition.ContainerSignature)}', not '{FourCcCodec.Format(actual)}'.",
                    tag, index, "Data", "Use the registered array or structure signature required by the enclosing tag."));
            }
        }

        ValidateRegisteredChannels(profile, tag, index, definition, issues);
        if (definition.Role == Icc2TagRole.HdrToSdrTransform
            && !profile.Header.UsesExtendedPcsRange)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                $"HDR-to-SDR tag '{FourCcCodec.Format(tag.Signature)}' requires the ICC.2 extended PCS range flag.",
                tag, index, "Data",
                "Set the extended PCS range flag or remove the HDR-to-SDR transform."));
        }
    }

    internal static void ValidateSharedTypeModel(IccProfile profile, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccTagDataKind kind = tag.Data.Kind;
        bool shared = kind == IccTagDataKind.IntegerArray
            || kind == IccTagDataKind.FixedPointArray
            || kind == IccTagDataKind.XyzArray
            || kind == IccTagDataKind.DateTime
            || kind == IccTagDataKind.Binary
            || kind == IccTagDataKind.Text
            || kind == IccTagDataKind.LocalizedText
            || kind == IccTagDataKind.Dictionary
            || kind == IccTagDataKind.Curve
            || kind == IccTagDataKind.Lut
            || kind == IccTagDataKind.Measurement
            || kind == IccTagDataKind.ViewingConditions
            || kind == IccTagDataKind.Chromaticity
            || kind == IccTagDataKind.Cicp
            || kind == IccTagDataKind.NamedColors
            || kind == IccTagDataKind.Colorants
            || kind == IccTagDataKind.ResponseCurves
            || kind == IccTagDataKind.ProfileSequence;
        if (shared)
        {
            Icc1TagValidationCore.ValidateDecodedTag(profile, tag, index,
                IccVersionKind.Version5, issues);
        }
    }

    internal static void ValidateRegisteredChannels(IccProfile profile, IccTag tag, int index,
        Icc2TagDefinition definition, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int input = 0;
        int output = 0;
        bool hasPipeline = false;
        if (tag.Data.MultiProcessElements != null)
        {
            input = tag.Data.MultiProcessElements.InputChannels;
            output = tag.Data.MultiProcessElements.OutputChannels;
            hasPipeline = true;
        }
        else if (tag.Data.Lut != null)
        {
            input = tag.Data.Lut.InputChannels;
            output = tag.Data.Lut.OutputChannels;
            hasPipeline = true;
        }
        else if (tag.Data.MultiStageLut != null)
        {
            input = tag.Data.MultiStageLut.InputChannels;
            output = tag.Data.MultiStageLut.OutputChannels;
            hasPipeline = true;
        }
        int expectedInput = ResolveHeaderChannels(definition.InputChannels, profile.Header);
        int expectedOutput = ResolveHeaderChannels(definition.OutputChannels, profile.Header);
        if (hasPipeline && ((expectedInput != 0 && input != expectedInput)
            || (expectedOutput != 0 && output != expectedOutput)))
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ChannelCount,
                $"ICC.2 {definition.Role} tag '{FourCcCodec.Format(tag.Signature)}' declares {input}-to-{output} channels, but its header-defined endpoints require {expectedInput}-to-{expectedOutput}.",
                tag, index, "Data", "Match the transform direction, rendering intent, geometry channels, and header colour spaces."));
        }
    }

    internal static void ValidateTagData(IccProfile profile, IccTag tag, IccTagData data,
        int tagIndex, string path, int depth, IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (depth > policy.ResourceLimits.MaximumNestingDepth)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxHierarchy,
                "The decoded ICC.2 hierarchy exceeds the configured nesting depth.",
                tag, tagIndex, path, "Reduce nested tag-array and tag-structure depth."));
            return;
        }
        switch (data.Kind)
        {
            case IccTagDataKind.FloatingPointArray:
                if (!AreFinite(data.FloatingPointValues))
                {
                    issues.Add(CreateTagIssue(IccValidationRuleIds.TagSize,
                        "An ICC.2 floating-point array contains a non-finite value.",
                        tag, tagIndex, path + ".FloatingPointValues",
                        "Replace NaN and infinity with finite representable values."));
                }
                break;
            case IccTagDataKind.SparseMatrixArray:
                ValidateSparseArray(data.SparseMatrixArray, tag, tagIndex,
                    path + ".SparseMatrixArray", issues);
                break;
            case IccTagDataKind.TagArray:
                ValidateArrayData(profile, tag, data.TagArray, tagIndex, path,
                    depth, policy, issues);
                break;
            case IccTagDataKind.TagStructure:
                ValidateStructureData(profile, tag, data.TagStructure, tagIndex,
                    path, depth, policy, issues);
                break;
            case IccTagDataKind.MultiProcessElements:
                ValidateMpe(data.MultiProcessElements, tag, tagIndex,
                    path + ".MultiProcessElements", issues);
                break;
            case IccTagDataKind.SpectralViewingConditions:
                ValidateSpectralViewing(data.SpectralViewingConditions, tag, tagIndex,
                    path + ".SpectralViewingConditions", issues);
                break;
            case IccTagDataKind.SpectralRange:
                ValidateSpectralRanges(data.SpectralRanges, tag, tagIndex,
                    path + ".SpectralRanges", issues);
                break;
            case IccTagDataKind.GamutBoundary:
                ValidateGamut(profile, data.GamutBoundary, tag, tagIndex,
                    path + ".GamutBoundary", issues);
                break;
            case IccTagDataKind.EmbeddedImage:
                ValidateEmbeddedImage(data.EmbeddedImage, data.SurfaceMap,
                    ResolveNestedType(data) == IccTypeSignatures.EmbeddedHeightImage,
                    tag, tagIndex, path + ".EmbeddedImage", policy, issues);
                break;
            default:
                break;
        }
    }

    internal static void ValidateArrayData(IccProfile profile, IccTag tag, IccTagArray? array,
        int tagIndex, string path, int depth, IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (array == null || array.Values == null)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxHierarchy,
                "An ICC.2 tag array or its value collection is missing.", tag, tagIndex,
                path + ".TagArray", "Provide a complete tag-array model."));
            return;
        }
        string error = RegisteredArrayProjectionCodec.ValidateArray(array);
        if (error.Length != 0)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxHierarchy,
                error, tag, tagIndex, path + ".TagArray",
                "Use the registered element structure and ordering for this ICC.2 array."));
        }
        uint elementType = 0;
        int index = 0;
        while (index < array.Values.Length)
        {
            IccTagData item = array.Values[index];
            if (item == null)
            {
                issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxHierarchy,
                    $"ICC.2 tag-array element {index} is null.", tag, tagIndex,
                    $"{path}.TagArray.Values[{index}]", "Replace the null nested tag value."));
            }
            else
            {
                uint itemType = ResolveNestedType(item);
                if (index == 0)
                {
                    elementType = itemType;
                }
                else if (itemType != 0 && elementType != 0 && itemType != elementType)
                {
                    issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxHierarchy,
                        "Every ICC.2 tag-array element shall have the same tag type.", tag,
                        tagIndex, $"{path}.TagArray.Values[{index}]",
                        "Use a homogeneous nested tag type throughout the array."));
                }
                ValidateTagData(profile, tag, item, tagIndex,
                    $"{path}.TagArray.Values[{index}]", depth + 1, policy, issues);
            }
            index = index + 1;
        }
    }

    internal static void ValidateStructureData(IccProfile profile, IccTag tag,
        IccTagStructure? structure, int tagIndex, string path, int depth,
        IccValidationPolicy policy, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (structure == null || structure.Members == null)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxHierarchy,
                "An ICC.2 tag structure or its member collection is missing.", tag,
                tagIndex, path + ".TagStructure", "Provide a complete tag-structure model."));
            return;
        }
        int index = 0;
        while (index < structure.Members.Length)
        {
            IccStructureMember member = structure.Members[index];
            if (member == null || member.Data == null)
            {
                issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxHierarchy,
                    $"ICC.2 tag-structure member {index} is incomplete.", tag, tagIndex,
                    $"{path}.TagStructure.Members[{index}]",
                    "Provide both a member signature and nested value."));
            }
            else
            {
                int prior = 0;
                while (prior < index)
                {
                    IccStructureMember previous = structure.Members[prior];
                    if (previous != null && previous.Signature == member.Signature)
                    {
                        issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxHierarchy,
                            $"ICC.2 structure member '{FourCcCodec.Format(member.Signature)}' is duplicated.",
                            tag, tagIndex, $"{path}.TagStructure.Members[{index}]",
                            "Keep each structure-member signature unique."));
                    }
                    prior = prior + 1;
                }
                ValidateTagData(profile, tag, member.Data, tagIndex,
                    $"{path}.TagStructure.Members[{index}].Data", depth + 1,
                    policy, issues);
            }
            index = index + 1;
        }
    }

    internal static void ValidateTagProjection(IccProfile profile, IccTag tag, int index,
        IccValidationPolicy policy, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccTagData projected = tag.Data;
        if (tag.Data.TagStructure != null && StructureIsComplete(tag.Data.TagStructure))
        {
            projected = new IccTagData();
            projected.Kind = tag.Data.Kind;
            projected.RawData = tag.Data.RawData;
            projected.TagStructure = tag.Data.TagStructure;
            TagDecodeState state = CreateContext(profile, tag, policy);
            string error = StructuredTagProjectionCodec.ProjectDecoded(state, projected);
            if (error.Length != 0)
            {
                string rule = projected.TagStructure != null
                    && projected.TagStructure.StructureSignature
                        == IccStructureSignatures.ProfileConnectionConditions
                    ? IccValidationRuleIds.IccMaxPcc : IccValidationRuleIds.IccMaxHierarchy;
                issues.Add(CreateTagIssue(rule, error, tag, index, "Data.TagStructure",
                    "Provide all registered members with their required ICC.2 types and relationships."));
            }
        }
        else if (tag.Data.TagArray != null)
        {
            projected = new IccTagData();
            projected.Kind = tag.Data.Kind;
            projected.RawData = tag.Data.RawData;
            projected.TagArray = tag.Data.TagArray;
            TagDecodeState state = CreateContext(profile, tag, policy);
            string error = RegisteredArrayProjectionCodec.ProjectDecoded(state, projected);
            if (error.Length != 0)
            {
                string rule = tag.Signature == IccTagSignatures.MultiplexTypeArray
                    ? IccValidationRuleIds.IccMaxMcs
                    : IccValidationRuleIds.IccMaxHierarchy;
                issues.Add(CreateTagIssue(rule, error, tag, index, "Data.TagArray",
                    "Provide the registered array elements and header-defined channel count."));
            }
        }
        else if (tag.Signature == IccTagSignatures.MultiplexDefaultValues)
        {
            projected = new IccTagData();
            projected.Kind = tag.Data.Kind;
            projected.RawData = tag.Data.RawData;
            projected.IntegerValues = tag.Data.IntegerValues;
            projected.FloatingPointValues = tag.Data.FloatingPointValues;
            TagDecodeState state = CreateContext(profile, tag, policy);
            string error = RegisteredArrayProjectionCodec.ProjectMultiplexDefaults(
                state, projected);
            if (error.Length != 0)
            {
                issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxMcs, error, tag,
                    index, "Data", "Provide one valid default value per MCS channel."));
            }
        }
        if (projected.ProfileConnectionConditions != null)
        {
            ValidatePcc(profile, projected.ProfileConnectionConditions, tag, index,
                "Data.ProfileConnectionConditions", issues);
        }
        if (projected.MultiplexChannels != null)
        {
            ValidateMultiplexData(profile, projected.MultiplexChannels, tag, index,
                "Data.MultiplexChannels", issues);
        }
        if (projected.Brdf != null)
        {
            ValidateBrdf(profile, projected.Brdf, tag, index, "Data.Brdf", issues);
        }
        if (projected.ColorEncodingParameters != null)
        {
            ValidateColorEncoding(profile, projected.ColorEncodingParameters, tag,
                index, "Data.ColorEncodingParameters", issues);
        }
        if (projected.SurfaceMap != null
            && projected.Kind != IccTagDataKind.EmbeddedImage)
        {
            ValidateSurfaceMap(projected.SurfaceMap, tag, index, "Data.SurfaceMap",
                policy, issues);
        }
    }

    internal static void ValidateMpe(IccMultiProcessElements? mpe, IccTag tag, int tagIndex,
        string path, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool invalid = mpe == null || mpe.Elements == null || mpe.InputChannels < 1
            || mpe.OutputChannels < 1 || mpe.InputChannels > ushort.MaxValue
            || mpe.OutputChannels > ushort.MaxValue || mpe.Elements.Length == 0
            || !RangesAreValid(mpe.InputRanges, mpe.InputChannels)
            || !RangesAreValid(mpe.OutputRanges, mpe.OutputChannels);
        if (invalid || mpe == null || mpe.Elements == null)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.PipelineChannelContinuity,
                "The ICC.2 multi-process pipeline has invalid endpoints, ranges, or no processing elements.",
                tag, tagIndex, path, "Provide non-zero channels, one range per endpoint, and at least one element."));
            return;
        }
        string channelError = MultiProcessElementsTagCodec.ValidateChannelContinuity(
            mpe.Elements, mpe.InputChannels, mpe.OutputChannels);
        if (channelError.Length != 0)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.PipelineChannelContinuity,
                channelError, tag, tagIndex, path,
                "Connect every element output to the next input and the container endpoints."));
        }
        int index = 0;
        while (index < mpe.Elements.Length)
        {
            ValidateMpeElement(mpe.Elements[index], tag, tagIndex,
                $"{path}.Elements[{index}]", issues);
            index = index + 1;
        }
    }

    internal static void ValidateMpeElement(IccProcessingElement element, IccTag tag,
        int tagIndex, string path, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (element == null)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.PipelineChannelContinuity,
                "An ICC.2 processing element is missing.", tag, tagIndex, path,
                "Replace the null processing element."));
            return;
        }
        bool invalid = element.InputChannels < 1 || element.OutputChannels < 1;
        if (!invalid)
        {
            IccProcessingElementKind expected =
                MultiProcessElementsTagCodec.IdentifyVersion5Kind(element.Signature);
            invalid = element.Kind != expected;
        }
        if (!invalid && (!RangesAreValid(element.InputRanges, element.InputChannels)
            || !RangesAreValid(element.OutputRanges, element.OutputChannels)))
        {
            invalid = true;
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.PipelineChannelContinuity,
                "An ICC.2 processing element has inconsistent kind, signature, channels, or ranges.",
                tag, tagIndex, path,
                "Match the element signature to its model and provide one ordered finite range per channel."));
            return;
        }
        if (element.Kind == IccProcessingElementKind.CurveSet
            && (element.InputChannels != element.OutputChannels || element.Curves == null
                || element.Curves.Length != element.InputChannels))
        {
            AddElementIssue(tag, tagIndex, path, "The ICC.2 curve-set element does not contain one curve per channel.", issues);
        }
        else if (element.Kind == IccProcessingElementKind.CurveSet)
        {
            int curveIndex = 0;
            while (curveIndex < element.Curves.Length)
            {
                Icc1TagValidationCore.ValidateCurveValue(element.Curves[curveIndex], tag,
                    tagIndex, $"{path}.Curves[{curveIndex}]", issues);
                curveIndex = curveIndex + 1;
            }
        }
        else if (element.Kind == IccProcessingElementKind.Matrix
            && !MatrixIsValid(element.Matrix, element.OutputChannels,
                element.InputChannels + 1))
        {
            AddElementIssue(tag, tagIndex, path, "The ICC.2 matrix element dimensions do not match its channels and offsets.", issues);
        }
        else if (element.Kind == IccProcessingElementKind.Clut
            || element.Kind == IccProcessingElementKind.ExtendedClut
            || element.Kind == IccProcessingElementKind.TintArray)
        {
            bool invalidClut = !ClutIsValid(element.Clut, element.InputChannels,
                element.OutputChannels);
            if (!invalidClut && element.Clut != null
                && ((element.Kind == IccProcessingElementKind.ExtendedClut
                        && (element.Clut.Encoding < 0 || element.Clut.Encoding > 3
                            || element.Clut.InterpolationHint < 0
                            || element.Clut.InterpolationHint > 2))
                    || (element.Kind == IccProcessingElementKind.TintArray
                        && (element.InputChannels != 1 || element.Clut.Encoding < 0
                            || element.Clut.Encoding > 3))))
            {
                invalidClut = true;
            }
            if (invalidClut)
            {
                AddElementIssue(tag, tagIndex, path, "The ICC.2 CLUT or tint-array dimensions are inconsistent.", issues);
            }
        }
        else if (element.Kind == IccProcessingElementKind.SparseMatrix)
        {
            if (!SparseLutIsValid(element.SparseMatrixLut, element.InputChannels,
                element.OutputChannels))
            {
                AddElementIssue(tag, tagIndex, path, "The ICC.2 sparse-matrix element dimensions are inconsistent.", issues);
            }
        }
        else if (element.Kind == IccProcessingElementKind.ToneMap)
        {
            if (!ToneMapIsValid(element))
            {
                AddElementIssue(tag, tagIndex, path, "The ICC.2 tone-map functions or channel relationship are invalid.", issues);
            }
        }
        else if (element.Kind == IccProcessingElementKind.Calculator)
        {
            if (element.Calculator == null || element.Calculator.Instructions == null
                || element.Calculator.SubElements == null)
            {
                AddElementIssue(tag, tagIndex, path,
                    "The ICC.2 calculator element is missing its program data.", issues);
            }
        }
        else if (element.Kind == IccProcessingElementKind.XyzToJab
            || element.Kind == IccProcessingElementKind.JabToXyz)
        {
            if (!CamIsValid(element))
            {
                AddElementIssue(tag, tagIndex, path, "The ICC.2 appearance-conversion element is invalid.", issues);
            }
        }
        else if (element.Kind == IccProcessingElementKind.EmissionClut
            || element.Kind == IccProcessingElementKind.ReflectanceClut
            || element.Kind == IccProcessingElementKind.EmissionMatrix
            || element.Kind == IccProcessingElementKind.InverseEmissionMatrix
            || element.Kind == IccProcessingElementKind.EmissionObserver
            || element.Kind == IccProcessingElementKind.ReflectanceObserver)
        {
            if (!SpectralElementIsValid(element))
            {
                issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxSpectral,
                    "The ICC.2 spectral processing element has inconsistent spectral white, channels, flags, or body dimensions.",
                    tag, tagIndex, path,
                    "Use a valid wavelength range and one finite reference-white sample per spectral step."));
            }
        }
    }

    internal static void ValidateSparseArray(IccSparseMatrixArray? array, IccTag tag,
        int tagIndex, string path, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (!SparseArrayIsValid(array))
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxSpectral,
                "The ICC.2 sparse-matrix array has invalid encoding, dimensions, row topology, or values.",
                tag, tagIndex, path,
                "Use one matrix shape and encoding with valid compressed-row offsets and finite entries."));
        }
    }

    internal static void ValidateSpectralViewing(IccSpectralViewingConditions? conditions,
        IccTag tag, int tagIndex, string path,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool invalid = conditions == null;
        if (!invalid && conditions != null)
        {
            int observer = (int)conditions.Observer;
            int illuminant = (int)conditions.Illuminant;
            invalid = observer < 0 || observer > 2 || illuminant < 0 || illuminant > 22
                || !SpectralViewingConditionsTagCodec.IsValidColorTemperature(
                    illuminant, conditions.ColorTemperature)
                || !XyzIsFinite(conditions.IlluminantColor)
                || !XyzIsFinite(conditions.SurroundColor)
                || (observer == 0 && !SpectrumIsValid(conditions.ObserverFunctions, 3, 1))
                || (conditions.ObserverFunctions != null
                    && !SpectrumIsValid(conditions.ObserverFunctions, 3, 1))
                || (conditions.IlluminantSpectrum != null
                    && !SpectrumIsValid(conditions.IlluminantSpectrum, 1, 1));
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxSpectral,
                "The ICC.2 spectral viewing conditions have inconsistent selectors, ranges, spectra, temperature, or XYZ values.",
                tag, tagIndex, path,
                "Provide defined viewing selectors and matching finite observer and illuminant spectra."));
        }
    }

    internal static void ValidateSpectralRanges(IccSpectralRanges? ranges, IccTag tag,
        int tagIndex, string path, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool invalid = ranges == null || !RangeIsValid(ranges.SpectralRange, false)
            || !RangeIsValid(ranges.BiSpectralRange, true);
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxSpectral,
                "The ICC.2 spectral-range data is missing or contains an invalid primary or secondary range.",
                tag, tagIndex, path,
                "Use an ordered primary wavelength range and either zero or an ordered secondary range."));
        }
    }

    internal static void ValidateGamut(IccProfile profile, IccGamutBoundary? boundary,
        IccTag tag, int tagIndex, string path,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (boundary == null)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                "The ICC.2 gamut boundary is missing.", tag, tagIndex, path,
                "Provide a closed valid gamut-boundary mesh."));
            return;
        }
        string error = GamutBoundaryTagCodec.ValidateBoundaryForWrite(boundary);
        int colorimetric = ColorSpaceChannelCount.Decode(
            profile.Header.ProfileConnectionSpaceSignature);
        int spectral = ColorSpaceChannelCount.Decode(profile.Header.SpectralPcsSignature);
        int device = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        bool wrongContext = (boundary.PcsChannels != colorimetric
            && boundary.PcsChannels != spectral)
            || (boundary.DeviceChannels != 0 && boundary.DeviceChannels != device);
        bool wrongClass = profile.Header.ProfileClass != IccProfileClass.Input
            && profile.Header.ProfileClass != IccProfileClass.Display
            && profile.Header.ProfileClass != IccProfileClass.Output
            && profile.Header.ProfileClass != IccProfileClass.ColorSpace;
        if (error.Length != 0 || wrongContext || wrongClass)
        {
            string message = error.Length == 0
                ? "The gamut boundary channels or profile class do not match the ICC.2 header."
                : error;
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                message, tag, tagIndex, path,
                "Use a closed valid mesh whose optional device and PCS coordinates match an allowed profile context."));
        }
    }

    internal static void ValidateEmbeddedImage(IccEmbeddedImage? image, IccSurfaceMap? surface,
        bool height, IccTag tag, int tagIndex, string path, IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool invalid = !ImageIsValid(image, height, policy);
        if (!invalid && surface != null && image != null)
        {
            invalid = surface.Width != image.Width || surface.Height != image.Height
                || (height && surface.HeightImage != image)
                || (!height && surface.NormalImage != image);
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                "The ICC.2 surface image metadata, encoded image, or projected dimensions are inconsistent.",
                tag, tagIndex, path,
                "Use a valid PNG or TIFF height/normal image and keep modeled dimensions and surface projection synchronized."));
        }
    }

    internal static void ValidatePcc(IccProfile profile, IccProfileConnectionConditions pcc,
        IccTag tag, int tagIndex, string path,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool invalid = pcc.ViewingConditions == null || pcc.MediaWhitePoint == null
            || !XyzIsFinite(pcc.MediaWhitePoint)
            || (pcc.ToStandardPcs == null) != (pcc.FromStandardPcs == null);
        if (!invalid && pcc.ToStandardPcs != null && pcc.FromStandardPcs != null)
        {
            invalid = pcc.ToStandardPcs.InputChannels != 3
                || pcc.ToStandardPcs.OutputChannels != 3
                || pcc.FromStandardPcs.InputChannels != 3
                || pcc.FromStandardPcs.OutputChannels != 3;
        }
        if (!invalid && tag.Signature == IccTagSignatures.DevicePcc)
        {
            int channels = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
            bool spectral = profile.Header.DataColorSpace == IccColorSpaceKind.Spectral
                || profile.Header.DataColorSpace == IccColorSpaceKind.BiSpectral;
            invalid = profile.Header.ProfileClass != IccProfileClass.Abstract
                || pcc.ToStandardPcs == null || pcc.FromStandardPcs == null
                || (spectral && (pcc.SpectralWhitePoint == null
                    || pcc.SpectralWhitePoint.Length != channels));
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxPcc,
                "The ICC.2 profile connection conditions have invalid white data, class context, spectral count, or an unpaired transform.",
                tag, tagIndex, path,
                "Provide valid connection-condition white data and paired three-channel custom/standard transforms."));
        }
        if (pcc.ToStandardPcs != null)
        {
            ValidateMpe(pcc.ToStandardPcs, tag, tagIndex,
                path + ".ToStandardPcs", issues);
        }
        if (pcc.FromStandardPcs != null)
        {
            ValidateMpe(pcc.FromStandardPcs, tag, tagIndex,
                path + ".FromStandardPcs", issues);
        }
    }

    internal static void ValidateMultiplexData(IccProfile profile, IccMultiplexChannels value,
        IccTag tag, int tagIndex, string path,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int expected = DecodeMaterialChannels(
            profile.Header.MaterialConnectionSpaceSignature);
        string[]? names = value.Names;
        double[]? defaults = value.DefaultValues;
        int[]? order = value.Order;
        if (names == null || defaults == null || order == null)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxMcs,
                "The multiplex channel projection has a missing name, default, or order collection.",
                tag, tagIndex, path,
                "Provide complete name, default, and order collections."));
            return;
        }
        bool invalid = false;
        if (tag.Signature == IccTagSignatures.MultiplexTypeArray)
        {
            invalid = names.Length != expected || order.Length != expected
                || !NamesAreUnique(names) || !IsPermutation(order, expected);
        }
        if (!invalid && tag.Signature == IccTagSignatures.MultiplexDefaultValues)
        {
            invalid = defaults.Length != expected
                || order.Length != expected || !AreFinite(defaults)
                || !IsPermutation(order, expected);
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxMcs,
                $"The multiplex channel projection does not match the {expected}-channel Material Connection Space.",
                tag, tagIndex, path,
                "Provide unique channel names or finite defaults and one permutation entry per MCS channel."));
        }
    }

    internal static void ValidateBrdf(IccProfile profile, IccBrdf brdf, IccTag tag,
        int tagIndex, string path,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int parameters = BrdfEvaluator.GetParameterCount(brdf.ModelSignature);
        bool monochrome = BrdfEvaluator.IsMonochromeModel(brdf.ModelSignature);
        uint expectedType = monochrome ? StructuredTagProjectionCodec.MonochromeBrdf
            : StructuredTagProjectionCodec.ColorBrdf;
        bool spectral = StructuredTagProjectionCodec.IsSpectralBrdfParameterTag(
            tag.Signature);
        IccMultiProcessElements? transform = spectral ? brdf.SpectralTransform
            : brdf.ColorimetricTransform;
        Icc2TagDefinition? definition = Icc2TagRegistry.FindDefinition(tag.Signature);
        int inputChannels = definition == null ? 0
            : ResolveHeaderChannels(definition.InputChannels, profile.Header);
        int valueChannels = definition == null ? 0
            : ResolveHeaderChannels(definition.AssociatedChannels, profile.Header);
        bool invalid = parameters == 0 || brdf.TypeSignature != expectedType
            || brdf.Parameters == null || brdf.Parameters.Length != 1
            || brdf.Parameters[0] != parameters
            || transform == null || (spectral && brdf.ColorimetricTransform != null)
            || (!spectral && brdf.SpectralTransform != null);
        if (!invalid && transform != null)
        {
            long expectedOutput = monochrome ? parameters
                : (long)parameters * valueChannels;
            invalid = inputChannels == 0 || valueChannels == 0
                || expectedOutput > ushort.MaxValue
                || transform.InputChannels != inputChannels
                || transform.OutputChannels != expectedOutput;
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                "The ICC.2 BRDF model, parameterization type, parameter count, or transform is inconsistent.",
                tag, tagIndex, path,
                "Use a registered BRDF model with its matching mono/colour type and parameter transform."));
        }
        if (transform != null)
        {
            ValidateMpe(transform, tag, tagIndex, path + ".Transform", issues);
        }
    }

    internal static void ValidateColorEncoding(IccProfile profile,
        IccColorEncodingParameters value, IccTag tag, int tagIndex, string path,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int channels = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        IccEncodingRange[]? ranges = value.ChannelRanges;
        if (ranges == null)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxHierarchy,
                "The ICC.2 colour-encoding channel ranges are missing.", tag, tagIndex,
                path, "Provide zero, one, or one range per device channel."));
            return;
        }
        bool invalid = value.Name == null || value.Curves == null
            || value.Metadata == null || value.ReferenceWhite == null
            || !XyzIsFinite(value.ReferenceWhite)
            || (ranges.Length != 0 && ranges.Length != 1 && ranges.Length != channels)
            || (value.Curves.Length != 0 && value.Curves.Length != 1
                && value.Curves.Length != channels);
        int index = 0;
        while (!invalid && index < ranges.Length)
        {
            IccEncodingRange range = ranges[index];
            invalid = range == null || !IsFinite(range.Minimum)
                || !IsFinite(range.Maximum) || range.Minimum > range.Maximum;
            index = index + 1;
        }
        if (!invalid && value.Matrix != null)
        {
            invalid = !MatrixIsValid(value.Matrix, 3, 3);
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                "The abbreviated ICC.2 colour-encoding parameters do not match the profile channels or contain invalid ranges, curves, white point, or matrix.",
                tag, tagIndex, path,
                "Use one or one-per-channel range/curve and finite ordered encoding parameters."));
        }
    }

    internal static void ValidateSurfaceMap(IccSurfaceMap surface, IccTag tag, int tagIndex,
        string path, IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool hasHeight = surface.HeightImage != null;
        bool hasNormal = surface.NormalImage != null;
        bool invalid = hasHeight == hasNormal || surface.Width < 1 || surface.Height < 1
            || surface.Scale == null || (hasHeight && surface.Scale.Length != 2)
            || (hasNormal && surface.Scale.Length != 0);
        IccEmbeddedImage? image = hasHeight ? surface.HeightImage : surface.NormalImage;
        if (!invalid && image != null)
        {
            invalid = surface.Width != image.Width || surface.Height != image.Height
                || !ImageIsValid(image, hasHeight, policy);
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                "The ICC.2 surface map shall contain exactly one valid height or normal image with matching dimensions and scale.",
                tag, tagIndex, path,
                "Keep one surface image, synchronize dimensions, and provide two height limits only for a height map."));
        }
    }

    internal static void ValidateSpectralProfile(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccTag? white = FindTag(profile, IccTagSignatures.SpectralWhitePoint);
        if (white != null && white.Data != null)
        {
            int expected = ColorSpaceChannelCount.Decode(profile.Header.SpectralPcsSignature);
            int actual = white.Data.FloatingPointValues == null
                ? 0 : white.Data.FloatingPointValues.Length;
            if (white.Data.Kind == IccTagDataKind.IntegerArray)
            {
                actual = white.Data.IntegerValues == null ? 0 : white.Data.IntegerValues.Length;
            }
            if (expected == 0 || actual != expected)
            {
                int index = FindTagIndex(profile, IccTagSignatures.SpectralWhitePoint);
                issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxSpectral,
                    $"The spectral white point contains {actual} samples, but the header spectral PCS defines {expected} channels.",
                    white, index, "Data", "Provide one spectral-white sample per spectral PCS channel."));
            }
        }
        IccTag? rangeTag = FindTag(profile, IccTagSignatures.DeviceSpectralRange);
        if (rangeTag != null && rangeTag.Data != null && rangeTag.Data.SpectralRanges != null)
        {
            IccSpectralRanges ranges = rangeTag.Data.SpectralRanges;
            int channels = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
            bool bi = profile.Header.DataColorSpace == IccColorSpaceKind.BiSpectral;
            long samples = ranges.SpectralRange == null ? 0
                : ranges.SpectralRange.Steps;
            if (bi && ranges.BiSpectralRange != null)
            {
                samples = samples * ranges.BiSpectralRange.Steps;
            }
            bool invalid = profile.Header.DataColorSpace != IccColorSpaceKind.Spectral
                && profile.Header.DataColorSpace != IccColorSpaceKind.BiSpectral;
            invalid = invalid || samples != channels;
            if (invalid)
            {
                int index = FindTagIndex(profile, IccTagSignatures.DeviceSpectralRange);
                issues.Add(CreateTagIssue(IccValidationRuleIds.IccMaxSpectral,
                    "The device spectral-range tag does not match the spectral device colour-space signature and channel count.",
                    rangeTag, index, "Data.SpectralRanges",
                    "Use a spectral device space and matching primary or bi-spectral range products."));
            }
        }
    }

    internal static void ValidateMultiplexProfile(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int channels = DecodeMaterialChannels(
            profile.Header.MaterialConnectionSpaceSignature);
        if (channels == 0)
        {
            return;
        }
        IccTag? names = FindTag(profile, IccTagSignatures.MultiplexTypeArray);
        IccTag? defaults = FindTag(profile, IccTagSignatures.MultiplexDefaultValues);
        if (names != null && names.Data != null && names.Data.MultiplexChannels != null
            && defaults != null && defaults.Data != null
            && defaults.Data.MultiplexChannels != null)
        {
            IccMultiplexChannels nameData = names.Data.MultiplexChannels;
            IccMultiplexChannels defaultData = defaults.Data.MultiplexChannels;
            string[]? channelNames = nameData.Names;
            int[]? nameOrder = nameData.Order;
            double[]? defaultValues = defaultData.DefaultValues;
            int[]? defaultOrder = defaultData.Order;
            if (channelNames == null || nameOrder == null || defaultValues == null
                || defaultOrder == null)
            {
                issues.Add(CreateProfileIssue(IccValidationRuleIds.IccMaxMcs,
                    "The multiplex name or default-value projection is incomplete.",
                    "Tags['mcta','mdv ']",
                    "Provide complete ordered name and default-value projections."));
                return;
            }
            if (channelNames.Length != defaultValues.Length
                || channelNames.Length != channels
                || !OrdersMatch(nameOrder, defaultOrder))
            {
                issues.Add(CreateProfileIssue(IccValidationRuleIds.IccMaxMcs,
                    "The multiplex name and default-value tags do not describe the same Material Connection Space channels.",
                    "Tags['mcta','mdv ']",
                    "Provide one ordered unique name and one default value for every MCS channel."));
            }
        }
    }

    internal static void ValidateAbbreviatedProfile(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (profile.Header.ProfileClass != IccProfileClass.ColorEncodingSpace)
        {
            return;
        }
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature != IccTagSignatures.ColorEncodingParameters
                && tag.Signature != IccTagSignatures.ColorSpaceName
                && tag.Signature != IccTagSignatures.ReferenceName
                && tag.Signature != IccTagSignatures.Copyright
                && tag.Signature != IccTagSignatures.ProfileDescription
                && tag.Signature != IccTagSignatures.Metadata)
            {
                issues.Add(CreateTagIssue(IccValidationRuleIds.ForbiddenTag,
                    $"Tag '{FourCcCodec.Format(tag.Signature)}' is not part of an abbreviated ColorEncodingSpace profile.",
                    tag, index, "Data",
                    "Keep only abbreviated colour-encoding identification, parameters, descriptions, copyright, and metadata."));
            }
            index = index + 1;
        }
    }

    internal static void ValidateSubclassAndExtendedSpace(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccProfileHeader header = profile.Header;
        bool hasSubclass = header.ProfileSubclassSignature != 0;
        bool hasVersion = header.Version.SubclassMajor != 0
            || header.Version.SubclassMinor != 0;
        if (hasSubclass != hasVersion)
        {
            issues.Add(CreateProfileIssue(IccValidationRuleIds.HeaderProfileSubclass,
                "The ICC.2 subclass signature and subclass ICS version are not both present.",
                "Header.ProfileSubclass",
                "Set both subclass fields or clear both fields."));
        }
        if (header.DataColorSpace == IccColorSpaceKind.ExtendedDevice)
        {
            int channels = ColorSpaceChannelCount.Decode(header.DataColorSpaceSignature);
            uint prefix = header.DataColorSpaceSignature & 0xffff0000u;
            if (prefix != IccColorSpaceSignatures.ExtendedDevicePrefix || channels < 1
                || channels > ushort.MaxValue)
            {
                issues.Add(CreateProfileIssue(IccValidationRuleIds.HeaderColorSpace,
                    "The ICC.2 extended device colour-space signature has an invalid prefix or channel count.",
                    "Header.DataColorSpace",
                    "Use the ncXXXX signature form with a non-zero unsigned 16-bit channel count."));
            }
        }
    }

    internal static int ResolveHeaderChannels(Icc2ChannelSource source, IccProfileHeader header)
    {
        int count = 0;
        if (source == Icc2ChannelSource.DataColorSpace
            || source == Icc2ChannelSource.DataColorSpaceWithGeometry)
        {
            count = ColorSpaceChannelCount.Decode(header.DataColorSpaceSignature);
        }
        else if (source == Icc2ChannelSource.ColorimetricPcs
            || source == Icc2ChannelSource.ColorimetricPcsWithGeometry)
        {
            count = ColorSpaceChannelCount.Decode(header.ProfileConnectionSpaceSignature);
        }
        else if (source == Icc2ChannelSource.SpectralPcs
            || source == Icc2ChannelSource.SpectralPcsWithGeometry)
        {
            count = ColorSpaceChannelCount.Decode(header.SpectralPcsSignature);
        }
        else if (source == Icc2ChannelSource.MaterialConnectionSpace
            || source == Icc2ChannelSource.MaterialConnectionSpaceWithGeometry)
        {
            count = DecodeMaterialChannels(header.MaterialConnectionSpaceSignature);
        }
        else if (source == Icc2ChannelSource.Three)
        {
            count = 3;
        }
        if (count != 0 && (source == Icc2ChannelSource.DataColorSpaceWithGeometry
            || source == Icc2ChannelSource.ColorimetricPcsWithGeometry
            || source == Icc2ChannelSource.SpectralPcsWithGeometry
            || source == Icc2ChannelSource.MaterialConnectionSpaceWithGeometry))
        {
            count = count + 4;
        }
        return count;
    }

    internal static int DecodeMaterialChannels(uint signature)
    {
        uint prefix = signature & 0xffff0000u;
        int count = (int)(signature & 0x0000ffffu);
        return prefix == IccColorSpaceSignatures.MultiplexPrefix && count > 0
            ? count : 0;
    }

    internal static bool SparseArrayIsValid(IccSparseMatrixArray? array)
    {
        if (array == null || array.Matrices == null || array.Matrices.Length == 0
            || array.Encoding < 1 || array.Encoding > 4
            || array.EquivalentOutputChannels < 1)
        {
            return false;
        }
        int rows = array.Matrices[0] == null ? 0 : array.Matrices[0].Rows;
        int columns = array.Matrices[0] == null ? 0 : array.Matrices[0].Columns;
        if ((long)rows * columns != array.EquivalentOutputChannels)
        {
            return false;
        }
        int index = 0;
        while (index < array.Matrices.Length)
        {
            IccSparseMatrix matrix = array.Matrices[index];
            if (!SparseMatrixIsValid(matrix, rows, columns, array.Encoding))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool SparseMatrixIsValid(IccSparseMatrix? matrix, int rows,
        int columns, int encoding)
    {
        if (matrix == null || matrix.Rows != rows || matrix.Columns != columns
            || matrix.Encoding != encoding || rows < 1 || columns < 1
            || matrix.RowStartOffsets == null || matrix.ColumnIndices == null
            || matrix.Values == null || matrix.RowStartOffsets.Length != rows + 1
            || matrix.ColumnIndices.Length != matrix.Values.Length
            || !AreFinite(matrix.Values) || matrix.RowStartOffsets[0] != 0
            || matrix.RowStartOffsets[rows] != matrix.Values.Length)
        {
            return false;
        }
        int row = 0;
        while (row < rows)
        {
            int start = matrix.RowStartOffsets[row];
            int end = matrix.RowStartOffsets[row + 1];
            if (start < 0 || end < start || end > matrix.Values.Length)
            {
                return false;
            }
            int index = start;
            while (index < end)
            {
                if (matrix.ColumnIndices[index] < 0
                    || matrix.ColumnIndices[index] >= columns
                    || (index > start && matrix.ColumnIndices[index]
                        <= matrix.ColumnIndices[index - 1]))
                {
                    return false;
                }
                index = index + 1;
            }
            row = row + 1;
        }
        return true;
    }

    internal static bool SparseLutIsValid(IccSparseMatrixArray? array, int input, int output)
    {
        if (!SparseArrayIsValid(array) || array == null || array.GridPoints == null
            || array.GridPoints.Length != input
            || array.EquivalentOutputChannels != output || input > 16)
        {
            return false;
        }
        int count = 1;
        int index = 0;
        while (index < array.GridPoints.Length)
        {
            int grid = array.GridPoints[index];
            if (grid < 2 || grid > 255 || count > int.MaxValue / grid)
            {
                return false;
            }
            count = count * grid;
            index = index + 1;
        }
        return array.Matrices.Length == count;
    }

    internal static bool ClutIsValid(IccClut? clut, int input, int output)
    {
        if (clut == null || clut.GridPoints == null || clut.Values == null
            || clut.InputChannels != input || clut.OutputChannels != output
            || clut.GridPoints.Length != input || input < 1 || input > 16)
        {
            return false;
        }
        int count = output;
        int index = 0;
        while (index < input)
        {
            int grid = clut.GridPoints[index];
            if (grid < 2 || grid > 255 || count > int.MaxValue / grid)
            {
                return false;
            }
            count = count * grid;
            index = index + 1;
        }
        return clut.Values.Length == count && AreFinite(clut.Values);
    }

    internal static bool MatrixIsValid(IccMatrix? matrix, int rows, int columns)
    {
        return matrix != null && matrix.Values != null && matrix.Rows == rows
            && matrix.Columns == columns && matrix.Values.Length == rows * columns
            && AreFinite(matrix.Values);
    }

    internal static bool ToneMapIsValid(IccProcessingElement element)
    {
        IccToneMap? toneMap = element.ToneMap;
        if (toneMap == null || toneMap.LuminanceCurve == null
            || toneMap.Functions == null || element.InputChannels != element.OutputChannels + 1
            || element.OutputChannels > 16
            || toneMap.Functions.Length != element.OutputChannels)
        {
            return false;
        }
        System.Collections.Generic.List<IccValidationIssue> curveIssues =
            new System.Collections.Generic.List<IccValidationIssue>();
        Icc1TagValidationCore.ValidateCurveValue(toneMap.LuminanceCurve,
            new IccTag(), 0, "ToneMap.LuminanceCurve", curveIssues);
        if (curveIssues.Count != 0)
        {
            return false;
        }
        int index = 0;
        while (index < toneMap.Functions.Length)
        {
            IccToneMapFunction function = toneMap.Functions[index];
            if (function == null || function.FunctionType != 0
                || function.Parameters == null || function.Parameters.Length != 3
                || !AreFinite(function.Parameters))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool CamIsValid(IccProcessingElement element)
    {
        IccCamViewingConditions? value = element.CamViewingConditions;
        return element.InputChannels == 3 && element.OutputChannels == 3
            && value != null && value.WhitePoint != null && XyzIsFinite(value.WhitePoint)
            && IsFinite(value.Luminance) && IsFinite(value.BackgroundLuminance)
            && IsFinite(value.ImpactOfSurround) && value.ImpactOfSurround >= 0.0
            && value.ImpactOfSurround <= 1.0
            && IsFinite(value.ChromaticInductionFactor)
            && IsFinite(value.AdaptationFactor);
    }

    internal static bool SpectralElementIsValid(IccProcessingElement element)
    {
        IccSpectralProcessingData? spectral = element.SpectralProcessingData;
        if (spectral == null || !SpectrumIsValid(spectral.White, 1, 2)
            || spectral.Flags > 1 || element.OutputChannels != 3)
        {
            return false;
        }
        int steps = spectral.White.Range.Steps;
        if (element.Kind == IccProcessingElementKind.EmissionClut
            || element.Kind == IccProcessingElementKind.ReflectanceClut)
        {
            return element.Clut != null && element.Clut.Encoding >= 0
                && element.Clut.Encoding <= 3 && element.Clut.InterpolationHint >= 0
                && element.Clut.InterpolationHint <= 2
                && (element.Kind != IccProcessingElementKind.EmissionClut
                    || spectral.Flags == 0)
                && ClutIsValid(element.Clut, element.InputChannels, steps);
        }
        if (element.Kind == IccProcessingElementKind.EmissionMatrix
            || element.Kind == IccProcessingElementKind.InverseEmissionMatrix)
        {
            return MatrixIsValid(element.Matrix, 4, steps)
                && element.InputChannels == 3
                && (element.Kind != IccProcessingElementKind.InverseEmissionMatrix
                    || spectral.Flags == 0)
                && element.OutputChannels == 3;
        }
        return element.InputChannels == steps;
    }

    internal static bool SpectrumIsValid(IccSpectralData? data, int spectra, int minimumSteps)
    {
        return data != null && data.Range != null && data.Values != null
            && data.SparseValues == null && data.SpectrumCount == spectra
            && data.Range.Steps >= minimumSteps
            && data.Range.Steps <= ushort.MaxValue
            && SpectralViewingConditionsTagCodec.IsValidRangeValues(
                data.Range.StartWavelength, data.Range.EndWavelength, data.Range.Steps)
            && data.Values.Length == data.Range.Steps * spectra
            && AreFinite(data.Values);
    }

    internal static bool RangeIsValid(IccSpectralRange? range, bool allowZero)
    {
        if (range == null)
        {
            return false;
        }
        if (allowZero && range.Steps == 0 && range.StartWavelength == 0.0
            && range.EndWavelength == 0.0)
        {
            return true;
        }
        return range.Steps > 0 && range.Steps <= ushort.MaxValue
            && SpectralViewingConditionsTagCodec.IsValidRangeValues(
                range.StartWavelength, range.EndWavelength, range.Steps);
    }

    internal static bool ImageIsValid(IccEmbeddedImage? image, bool height,
        IccValidationPolicy policy)
    {
        if (image == null || image.Bytes == null || image.Width < 1 || image.Height < 1
            || (image.EncodingSignature != EmbeddedImageTagCodec.PngEncoding
                && image.EncodingSignature != EmbeddedImageTagCodec.TiffEncoding)
            || (height && (!IsFinite(image.MinimumHeightMeters)
                || !IsFinite(image.MaximumHeightMeters)
                || image.MinimumHeightMeters > image.MaximumHeightMeters)))
        {
            return false;
        }
        EmbeddedImageInspection inspection = EmbeddedImageTagCodec.InspectImage(image.Bytes,
            image.EncodingSignature, height, policy.ResourceLimits.MaximumDecompressedBytes);
        return inspection.Success && inspection.Width == image.Width
            && inspection.Height == image.Height && inspection.Components == image.Components
            && inspection.IsCompressed == image.IsCompressed;
    }

    internal static bool RangesAreValid(IccEncodingRange[]? ranges, int channels)
    {
        if (ranges == null || (ranges.Length != 0 && ranges.Length != channels))
        {
            return false;
        }
        int index = 0;
        while (index < ranges.Length)
        {
            IccEncodingRange range = ranges[index];
            if (range == null || !IsFinite(range.Minimum) || !IsFinite(range.Maximum)
                || range.Minimum > range.Maximum)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool NamesAreUnique(string[] names)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == null || names[index].Length == 0)
            {
                return false;
            }
            int prior = 0;
            while (prior < index)
            {
                if (names[prior] == names[index])
                {
                    return false;
                }
                prior = prior + 1;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsPermutation(int[] values, int count)
    {
        if (values == null || values.Length != count)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] < 0 || values[index] >= count)
            {
                return false;
            }
            int prior = 0;
            while (prior < index)
            {
                if (values[prior] == values[index])
                {
                    return false;
                }
                prior = prior + 1;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool OrdersMatch(int[] first, int[] second)
    {
        if (first == null || second == null || first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool XyzIsFinite(IccXyzColor? value)
    {
        return value != null && IsFinite(value.X) && IsFinite(value.Y)
            && IsFinite(value.Z);
    }

    internal static bool AreFinite(double[]? values)
    {
        if (values == null)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!IsFinite(values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    internal static uint ResolveNestedType(IccTagData data)
    {
        if (data.RawData != null && data.RawData.HasTypeSignature)
        {
            return data.RawData.TypeSignature;
        }
        if (data.Text != null && data.Text.TypeSignature != 0)
        {
            return data.Text.TypeSignature;
        }
        switch (data.Kind)
        {
            case IccTagDataKind.TagArray:
                return IccTypeSignatures.TagArray;
            case IccTagDataKind.TagStructure:
                return IccTypeSignatures.TagStructure;
            case IccTagDataKind.MultiProcessElements:
                return IccTypeSignatures.MultiProcessElements;
            case IccTagDataKind.SpectralViewingConditions:
                return IccTypeSignatures.SpectralViewingConditions;
            case IccTagDataKind.SpectralRange:
                return IccTypeSignatures.SpectralRange;
            case IccTagDataKind.GamutBoundary:
                return IccTypeSignatures.GamutBoundaryDescription;
            default:
                return 0;
        }
    }

    internal static bool StructureIsComplete(IccTagStructure structure)
    {
        if (structure.Members == null)
        {
            return false;
        }
        int index = 0;
        while (index < structure.Members.Length)
        {
            if (structure.Members[index] == null || structure.Members[index].Data == null)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static TagDecodeState CreateContext(IccProfile profile, IccTag tag,
        IccValidationPolicy policy)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(
            System.ReadOnlyMemory<byte>.Empty), 0, 0, 0,
            IccVersionKind.Version5, false);
        Icc1TagValidationCore.PopulateDecodeContext(state, profile, tag, policy);
        return state;
    }

    internal static IccTag? FindTag(IccProfile profile, uint signature)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == signature)
            {
                return tag;
            }
            index = index + 1;
        }
        return null;
    }

    internal static int FindTagIndex(IccProfile profile, uint signature)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == signature)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static void AddElementIssue(IccTag tag, int tagIndex, string path,
        string message, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        issues.Add(CreateTagIssue(IccValidationRuleIds.PipelineChannelContinuity,
            message, tag, tagIndex, path,
            "Correct the element's channels and kind-specific data."));
    }

    internal static IccValidationIssue CreateTagIssue(string ruleId, string message,
        IccTag tag, int index, string suffix, string remediation)
    {
        IccValidationIssue issue = CreateProfileIssue(ruleId, message,
            $"Tags[{index}].{suffix}", remediation);
        if (tag.TableEntry != null && tag.TableEntry.Offset >= 0)
        {
            issue.Location.Offset = tag.TableEntry.Offset;
            issue.Location.Length = tag.TableEntry.Size < 0 ? 0 : tag.TableEntry.Size;
        }
        return issue;
    }

    internal static IccValidationIssue CreateProfileIssue(string ruleId, string message,
        string path, string remediation)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = ruleId;
        issue.Severity = IccValidationSeverity.Error;
        issue.Message = message;
        issue.Path = path;
        issue.Remediation = remediation;
        return issue;
    }
}
