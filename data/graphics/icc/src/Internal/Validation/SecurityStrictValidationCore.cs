namespace icc;

internal class SecurityStrictValidationResult
{
    internal IccValidationIssue[] Issues;
    internal bool MayContinue;

    internal SecurityStrictValidationResult()
    {
        Issues = new IccValidationIssue[0];
        MayContinue = true;
    }
}

internal class SecurityStrictValidationState
{
    internal IccValidationPolicy Policy;
    internal System.Collections.Generic.List<IccValidationIssue> Issues;
    internal System.Collections.Generic.Dictionary<IccProfile, bool> ActiveProfiles;
    internal System.Collections.Generic.Dictionary<IccTagData, bool> ActiveData;
    internal System.Collections.Generic.Dictionary<IccProcessingElement, bool> ActiveElements;
    internal System.Collections.Generic.Dictionary<IccCalculatorProgram, bool> ActiveCalculators;
    internal long EncodedBytes;
    internal long DecodedBytes;
    internal long CalculatorInstructions;
    internal bool EncodedLimitReported;
    internal bool DecodedLimitReported;
    internal bool CalculatorLimitReported;
    internal bool MayContinue;

    internal SecurityStrictValidationState(IccValidationPolicy policy)
    {
        Policy = policy;
        Issues = new System.Collections.Generic.List<IccValidationIssue>();
        ActiveProfiles = new System.Collections.Generic.Dictionary<IccProfile, bool>();
        ActiveData = new System.Collections.Generic.Dictionary<IccTagData, bool>();
        ActiveElements = new System.Collections.Generic.Dictionary<IccProcessingElement, bool>();
        ActiveCalculators = new System.Collections.Generic.Dictionary<IccCalculatorProgram, bool>();
        EncodedBytes = 0;
        DecodedBytes = 0;
        CalculatorInstructions = 0;
        EncodedLimitReported = false;
        DecodedLimitReported = false;
        CalculatorLimitReported = false;
        MayContinue = true;
    }
}

internal static class SecurityStrictValidationCore
{
    internal static SecurityStrictValidationResult Validate(IccProfile profile,
        IccValidationPolicy policy)
    {
        SecurityStrictValidationResult result = new SecurityStrictValidationResult();
        if (policy.Mode != IccValidationMode.SecurityStrict)
        {
            return result;
        }

        SecurityStrictValidationState state = new SecurityStrictValidationState(policy);
        ValidateProfile(state, profile, "Profile", 0);
        result.Issues = state.Issues.ToArray();
        result.MayContinue = state.MayContinue;
        return result;
    }

    internal static void ValidateProfile(SecurityStrictValidationState state, IccProfile profile,
        string path, int depth)
    {
        if (depth > state.Policy.ResourceLimits.MaximumNestingDepth)
        {
            AddDepthIssue(state, path, "The embedded-profile hierarchy exceeds the configured nesting depth.");
            return;
        }
        if (state.ActiveProfiles.ContainsKey(profile))
        {
            AddDepthIssue(state, path, "The embedded-profile hierarchy contains a recursive reference cycle.");
            return;
        }
        state.ActiveProfiles.Add(profile, true);

        if (profile.ValidationIssues != null && profile.ValidationIssues.Length
            > state.Policy.ResourceLimits.MaximumDiagnosticCount)
        {
            AddResourceIssue(state, path + ".ValidationIssues",
                $"The profile retains {profile.ValidationIssues.Length} diagnostics, exceeding the configured maximum of {state.Policy.ResourceLimits.MaximumDiagnosticCount}.", null);
        }
        if (profile.Header != null && profile.Header.ValidationIssues != null
            && profile.Header.ValidationIssues.Length
                > state.Policy.ResourceLimits.MaximumDiagnosticCount)
        {
            AddResourceIssue(state, path + ".Header.ValidationIssues",
                $"The header retains {profile.Header.ValidationIssues.Length} diagnostics, exceeding the configured maximum of {state.Policy.ResourceLimits.MaximumDiagnosticCount}.", null);
        }

        if (profile.OriginalBytes == null)
        {
            AddIssue(state, IccValidationRuleIds.SecurityResourceLimit,
                IccValidationSeverity.Fatal, "The retained profile byte collection is null.",
                path + ".OriginalBytes", "Provide a bounded non-null retained byte collection.", null);
            state.MayContinue = false;
        }
        else if (profile.OriginalBytes.Length > state.Policy.ResourceLimits.MaximumProfileBytes)
        {
            AddResourceIssue(state, path + ".OriginalBytes",
                $"The retained profile contains {profile.OriginalBytes.Length} bytes, exceeding the configured maximum of {state.Policy.ResourceLimits.MaximumProfileBytes} bytes.", null);
        }

        if (profile.Tags == null)
        {
            AddResourceIssue(state, path + ".Tags", "The profile tag collection is null.", null);
        }
        else
        {
            if (profile.Tags.Length > state.Policy.ResourceLimits.MaximumTagCount)
            {
                AddResourceIssue(state, path + ".Tags",
                    $"The profile contains {profile.Tags.Length} tag models, exceeding the configured maximum of {state.Policy.ResourceLimits.MaximumTagCount}.", null);
            }
            int limit = System.Math.Min(profile.Tags.Length,
                state.Policy.ResourceLimits.MaximumTagCount);
            int index = 0;
            while (index < limit)
            {
                IccTag tag = profile.Tags[index];
                if (tag != null && tag.Data != null)
                {
                    ValidateTagData(state, tag.Data, tag, index,
                        $"{path}.Tags[{index}].Data", depth);
                }
                index = index + 1;
            }
        }

        if (profile.TagTableEntries != null
            && profile.TagTableEntries.Length > state.Policy.ResourceLimits.MaximumTagCount)
        {
            AddResourceIssue(state, path + ".TagTableEntries",
                $"The profile contains {profile.TagTableEntries.Length} tag-table entries, exceeding the configured maximum of {state.Policy.ResourceLimits.MaximumTagCount}.", null);
        }
        if (depth == 0)
        {
            ValidateAliasesAndHiddenData(state, profile);
        }
        state.ActiveProfiles.Remove(profile);
    }

    internal static void ValidateTagData(SecurityStrictValidationState state, IccTagData data,
        IccTag tag, int tagIndex, string path, int depth)
    {
        if (depth > state.Policy.ResourceLimits.MaximumNestingDepth)
        {
            AddTagDepthIssue(state, tag, tagIndex, path,
                "The decoded tag hierarchy exceeds the configured nesting depth.");
            return;
        }
        if (state.ActiveData.ContainsKey(data))
        {
            AddTagDepthIssue(state, tag, tagIndex, path,
                "The decoded tag hierarchy contains a recursive reference cycle.");
            return;
        }
        state.ActiveData.Add(data, true);

        if (data.RawData != null && data.RawData.Bytes != null)
        {
            AddEncodedAllocation(state, data.RawData.Bytes.Length, path + ".RawData.Bytes", tag);
        }
        ValidateTagCollections(state, data, path, tag);
        ValidateDoubleArray(state, data.FixedPointValues, path + ".FixedPointValues", tag);
        ValidateDoubleArray(state, data.FloatingPointValues, path + ".FloatingPointValues", tag);
        ValidateLongArray(state, data.IntegerValues, path + ".IntegerValues", tag);
        ValidateText(state, data.Text, path + ".Text", tag);
        ValidateCurve(state, data.Curve, path + ".Curve", tag);
        ValidateLut(state, data.Lut, path + ".Lut", tag);
        ValidateMultiStageLut(state, data.MultiStageLut, path + ".MultiStageLut", tag);
        ValidatePipeline(state, data.MultiProcessElements, tag, tagIndex,
            path + ".MultiProcessElements", depth + 1);
        ValidateSparseArray(state, data.SparseMatrixArray, path + ".SparseMatrixArray", tag);
        ValidateImage(state, data.EmbeddedImage, path + ".EmbeddedImage", tag, false);
        ValidateSurfaceMap(state, data.SurfaceMap, path + ".SurfaceMap", tag);
        if (data.Calculator != null)
        {
            state.CalculatorInstructions = 0;
            state.CalculatorLimitReported = false;
            ValidateCalculator(state, data.Calculator, null, tag, tagIndex,
                path + ".Calculator", depth + 1);
        }
        ValidateDoubleArray(state, data.Brdf == null ? null : data.Brdf.Parameters,
            path + ".Brdf.Parameters", tag);
        if (data.Brdf != null)
        {
            ValidatePipeline(state, data.Brdf.ColorimetricTransform, tag, tagIndex,
                path + ".Brdf.ColorimetricTransform", depth + 1);
            ValidatePipeline(state, data.Brdf.SpectralTransform, tag, tagIndex,
                path + ".Brdf.SpectralTransform", depth + 1);
        }
        if (data.ProfileConnectionConditions != null)
        {
            ValidateDoubleArray(state, data.ProfileConnectionConditions.SpectralWhitePoint,
                path + ".ProfileConnectionConditions.SpectralWhitePoint", tag);
            ValidatePipeline(state, data.ProfileConnectionConditions.ToStandardPcs, tag,
                tagIndex, path + ".ProfileConnectionConditions.ToStandardPcs", depth + 1);
            ValidatePipeline(state, data.ProfileConnectionConditions.FromStandardPcs, tag,
                tagIndex, path + ".ProfileConnectionConditions.FromStandardPcs", depth + 1);
        }
        if (data.MultiplexChannels != null)
        {
            ValidateDoubleArray(state, data.MultiplexChannels.DefaultValues,
                path + ".MultiplexChannels.DefaultValues", tag);
        }

        ValidateNestedData(state, data, tag, tagIndex, path, depth);
        if (data.EmbeddedProfile != null)
        {
            ValidateProfile(state, data.EmbeddedProfile, path + ".EmbeddedProfile", depth + 1);
        }
        state.ActiveData.Remove(data);
    }

    internal static void ValidateTagCollections(SecurityStrictValidationState state,
        IccTagData data, string path, IccTag tag)
    {
        if (data.BinaryData != null)
        {
            AddDecodedAllocation(state, data.BinaryData.Length,
                path + ".BinaryData", tag);
        }
        if (data.XyzValues != null
            && AddDecodedAllocation(state, MultiplyBytes(data.XyzValues.Length, 24),
                path + ".XyzValues", tag))
        {
            int index = 0;
            while (index < data.XyzValues.Length)
            {
                IccXyzNumber value = data.XyzValues[index];
                if (value != null && (!CurveTagCodec.IsFinite(value.X)
                    || !CurveTagCodec.IsFinite(value.Y)
                    || !CurveTagCodec.IsFinite(value.Z)))
                {
                    AddNonFiniteIssue(state, $"{path}.XyzValues[{index}]", tag);
                    break;
                }
                index = index + 1;
            }
        }
        ValidateCollectionSize(state, data.LocalizedStrings == null ? 0
            : data.LocalizedStrings.Length, path + ".LocalizedStrings", tag);
        ValidateCollectionSize(state, data.Metadata == null ? 0
            : data.Metadata.Length, path + ".Metadata", tag);
        ValidateCollectionSize(state, data.ResponseCurves == null ? 0
            : data.ResponseCurves.Length, path + ".ResponseCurves", tag);
        ValidateCollectionSize(state, data.ProfileSequence == null ? 0
            : data.ProfileSequence.Length, path + ".ProfileSequence", tag);
        ValidateCollectionSize(state, data.ProfileSequenceIdentifiers == null ? 0
            : data.ProfileSequenceIdentifiers.Length,
            path + ".ProfileSequenceIdentifiers", tag);
        if (data.NamedColors != null)
        {
            ValidateCollectionSize(state, data.NamedColors.Colors == null ? 0
                : data.NamedColors.Colors.Length, path + ".NamedColors.Colors", tag);
        }
        if (data.Colorants != null)
        {
            ValidateCollectionSize(state, data.Colorants.Colorants == null ? 0
                : data.Colorants.Colorants.Length, path + ".Colorants.Colorants", tag);
        }
        if (data.GamutBoundary != null)
        {
            ValidateCollectionSize(state, data.GamutBoundary.Vertices == null ? 0
                : data.GamutBoundary.Vertices.Length,
                path + ".GamutBoundary.Vertices", tag);
            ValidateCollectionSize(state, data.GamutBoundary.Triangles == null ? 0
                : data.GamutBoundary.Triangles.Length,
                path + ".GamutBoundary.Triangles", tag);
        }
        if (data.SpectralData != null)
        {
            ValidateDoubleArray(state, data.SpectralData.Values,
                path + ".SpectralData.Values", tag);
        }
    }

    internal static void ValidateCollectionSize(SecurityStrictValidationState state,
        int count, string path, IccTag tag)
    {
        ValidateReferenceAllocation(state, count, path, tag);
    }

    internal static void ValidateNestedData(SecurityStrictValidationState state, IccTagData data,
        IccTag tag, int tagIndex, string path, int depth)
    {
        if (data.TagArray != null && data.TagArray.Values != null)
        {
            int count = data.TagArray.Values.Length;
            if (!ValidateReferenceAllocation(state, count, path + ".TagArray.Values", tag))
            {
                return;
            }
            int index = 0;
            while (index < count)
            {
                IccTagData child = data.TagArray.Values[index];
                if (child != null)
                {
                    ValidateTagData(state, child, tag, tagIndex,
                        $"{path}.TagArray.Values[{index}]", depth + 1);
                }
                index = index + 1;
            }
        }
        if (data.TagStructure != null && data.TagStructure.Members != null)
        {
            int count = data.TagStructure.Members.Length;
            if (!ValidateReferenceAllocation(state, count,
                path + ".TagStructure.Members", tag))
            {
                return;
            }
            int index = 0;
            while (index < count)
            {
                IccStructureMember member = data.TagStructure.Members[index];
                if (member != null && member.Data != null)
                {
                    ValidateTagData(state, member.Data, tag, tagIndex,
                        $"{path}.TagStructure.Members[{index}].Data", depth + 1);
                }
                index = index + 1;
            }
        }
    }

    internal static void ValidatePipeline(SecurityStrictValidationState state,
        IccMultiProcessElements? pipeline, IccTag tag, int tagIndex, string path, int depth)
    {
        if (pipeline == null || pipeline.Elements == null)
        {
            return;
        }
        state.CalculatorInstructions = 0;
        state.CalculatorLimitReported = false;
        if (!ValidateReferenceAllocation(state, pipeline.Elements.Length,
            path + ".Elements", tag))
        {
            return;
        }
        ValidateRanges(state, pipeline.InputRanges, path + ".InputRanges", tag);
        ValidateRanges(state, pipeline.OutputRanges, path + ".OutputRanges", tag);
        int index = 0;
        while (index < pipeline.Elements.Length)
        {
            IccProcessingElement element = pipeline.Elements[index];
            if (element != null)
            {
                string elementPath = $"{path}.Elements[{index}]";
                if (element.Kind == IccProcessingElementKind.Unknown && pipeline.IsExecutable)
                {
                    AddTagIssue(state, IccValidationRuleIds.CalculatorSafety,
                        IccValidationSeverity.Fatal,
                        "A private or unknown processing element is marked executable.",
                        tag, tagIndex, elementPath,
                        "Treat private elements as opaque data unless a trusted implementation explicitly validates them.");
                    state.MayContinue = false;
                }
                ValidateElement(state, element, tag, tagIndex, elementPath, depth);
            }
            index = index + 1;
        }
    }

    internal static void ValidateElement(SecurityStrictValidationState state,
        IccProcessingElement element, IccTag tag, int tagIndex, string path, int depth)
    {
        if (depth > state.Policy.ResourceLimits.MaximumNestingDepth
            || state.ActiveElements.ContainsKey(element))
        {
            AddTagDepthIssue(state, tag, tagIndex, path,
                "The processing-element graph exceeds the nesting limit or contains a recursive reference cycle.");
            return;
        }
        state.ActiveElements.Add(element, true);
        ValidateRanges(state, element.InputRanges, path + ".InputRanges", tag);
        ValidateRanges(state, element.OutputRanges, path + ".OutputRanges", tag);
        ValidateMatrix(state, element.Matrix, path + ".Matrix", tag);
        ValidateClut(state, element.Clut, path + ".Clut", tag);
        ValidateSparseArray(state, element.SparseMatrixLut, path + ".SparseMatrixLut", tag);
        if (element.Curves != null
            && ValidateReferenceAllocation(state, element.Curves.Length, path + ".Curves", tag))
        {
            int curveIndex = 0;
            while (curveIndex < element.Curves.Length)
            {
                ValidateCurve(state, element.Curves[curveIndex],
                    $"{path}.Curves[{curveIndex}]", tag);
                curveIndex = curveIndex + 1;
            }
        }
        if (element.ToneMap != null)
        {
            ValidateCurve(state, element.ToneMap.LuminanceCurve,
                path + ".ToneMap.LuminanceCurve", tag);
            if (element.ToneMap.Functions != null
                && ValidateReferenceAllocation(state, element.ToneMap.Functions.Length,
                    path + ".ToneMap.Functions", tag))
            {
                int functionIndex = 0;
                while (functionIndex < element.ToneMap.Functions.Length)
                {
                    IccToneMapFunction function = element.ToneMap.Functions[functionIndex];
                    if (function != null)
                    {
                        ValidateDoubleArray(state, function.Parameters,
                            $"{path}.ToneMap.Functions[{functionIndex}].Parameters", tag);
                    }
                    functionIndex = functionIndex + 1;
                }
            }
        }
        if (element.Calculator != null)
        {
            ValidateCalculator(state, element.Calculator, element, tag, tagIndex,
                path + ".Calculator", depth + 1);
        }
        state.ActiveElements.Remove(element);
    }

    internal static void ValidateCalculator(SecurityStrictValidationState state,
        IccCalculatorProgram program, IccProcessingElement? owner, IccTag tag, int tagIndex,
        string path, int depth)
    {
        if (depth > state.Policy.ResourceLimits.MaximumNestingDepth
            || state.ActiveCalculators.ContainsKey(program))
        {
            AddTagDepthIssue(state, tag, tagIndex, path,
                "The calculator graph exceeds the recursion limit or contains a recursive sub-element cycle.");
            return;
        }
        state.ActiveCalculators.Add(program, true);
        int instructionLength = program.Instructions == null ? 0 : program.Instructions.Length;
        state.CalculatorInstructions = AddSaturated(state.CalculatorInstructions,
            instructionLength);
        if (instructionLength > state.Policy.ResourceLimits.MaximumCalculatorInstructions
            || state.CalculatorInstructions > state.Policy.ResourceLimits.MaximumCalculatorInstructions)
        {
            if (!state.CalculatorLimitReported)
            {
                AddTagIssue(state, IccValidationRuleIds.CalculatorSafety,
                    IccValidationSeverity.Fatal,
                    $"Calculator execution can require more than the configured {state.Policy.ResourceLimits.MaximumCalculatorInstructions} instructions.",
                    tag, tagIndex, path + ".Instructions",
                    "Reduce calculator instructions and recursively referenced calculator work.");
                state.CalculatorLimitReported = true;
            }
            state.MayContinue = false;
        }
        if (program.InstructionCount != instructionLength
            || program.RequiredStackDepth < 0
            || program.RequiredStackDepth > state.Policy.ResourceLimits.MaximumCalculatorStackDepth
            || program.RequiredStorageValues < 0
            || program.RequiredStorageValues > state.Policy.ResourceLimits.MaximumCalculatorStorageValues)
        {
            AddTagIssue(state, IccValidationRuleIds.CalculatorSafety,
                IccValidationSeverity.Fatal,
                "Calculator counts, stack depth, or storage requirements are inconsistent or exceed configured limits.",
                tag, tagIndex, path,
                "Recompute the program requirements and keep them within the configured execution limits.");
            state.MayContinue = false;
        }
        bool hasUnknownOperation = program.UnknownOperationCount != 0;
        if (program.Instructions != null)
        {
            int unknownIndex = 0;
            while (unknownIndex < program.Instructions.Length && !hasUnknownOperation)
            {
                IccCalculatorInstruction unknownInstruction = program.Instructions[unknownIndex];
                hasUnknownOperation = unknownInstruction != null
                    && !CalculatorElementCodec.IsKnownOperation(unknownInstruction.Signature);
                unknownIndex = unknownIndex + 1;
            }
        }
        if (program.IsExecutable && hasUnknownOperation)
        {
            AddTagIssue(state, IccValidationRuleIds.CalculatorSafety,
                IccValidationSeverity.Fatal,
                "A calculator containing private or unknown operations is marked executable.",
                tag, tagIndex, path + ".IsExecutable",
                "Disable execution until every private operation is implemented and explicitly trusted.");
            state.MayContinue = false;
        }
        if (program.Instructions != null)
        {
            int limit = System.Math.Min(program.Instructions.Length,
                state.Policy.ResourceLimits.MaximumCalculatorInstructions);
            int index = 0;
            while (index < limit)
            {
                IccCalculatorInstruction instruction = program.Instructions[index];
                if (instruction != null
                    && !CurveTagCodec.IsFinite(instruction.FloatingPointValue))
                {
                    AddTagIssue(state, IccValidationRuleIds.CalculatorSafety,
                        IccValidationSeverity.Error,
                        "A calculator instruction contains a non-finite floating-point operand.",
                        tag, tagIndex, $"{path}.Instructions[{index}]",
                        "Replace NaN and infinity operands with finite values.");
                }
                index = index + 1;
            }
        }
        if (program.SubElements != null
            && ValidateReferenceAllocation(state, program.SubElements.Length,
                path + ".SubElements", tag))
        {
            int index = 0;
            while (index < program.SubElements.Length)
            {
                IccProcessingElement child = program.SubElements[index];
                if (child != null)
                {
                    ValidateElement(state, child, tag, tagIndex,
                        $"{path}.SubElements[{index}]", depth + 1);
                }
                index = index + 1;
            }
        }
        state.ActiveCalculators.Remove(program);
    }

    internal static void ValidateText(SecurityStrictValidationState state, IccTextData? text,
        string path, IccTag tag)
    {
        if (text == null)
        {
            return;
        }
        if (text.Bytes != null)
        {
            AddDecodedAllocation(state, text.Bytes.Length, path + ".Bytes", tag);
            if (text.IsCompressed
                && text.Bytes.Length > state.Policy.ResourceLimits.MaximumDecompressedBytes)
            {
                AddIssue(state, IccValidationRuleIds.CompressionLimit,
                    IccValidationSeverity.Fatal,
                    $"Compressed text expands to {text.Bytes.Length} bytes, exceeding the configured maximum of {state.Policy.ResourceLimits.MaximumDecompressedBytes} bytes.",
                    path + ".Bytes", "Reduce the expanded text or raise the limit only for trusted content.", tag);
                state.MayContinue = false;
            }
        }
    }

    internal static void ValidateImage(SecurityStrictValidationState state, IccEmbeddedImage? image,
        string path, IccTag tag, bool heightMap)
    {
        if (image == null)
        {
            return;
        }
        if (image.Bytes == null)
        {
            AddIssue(state, IccValidationRuleIds.TagSize, IccValidationSeverity.Fatal,
                "An embedded image has a null encoded byte collection.", path + ".Bytes",
                "Provide complete bounded PNG or TIFF bytes.", tag);
            state.MayContinue = false;
            return;
        }
        AddEncodedAllocation(state, image.Bytes.Length, path + ".Bytes", tag);
        long samples = MultiplyDimensions(state, image.Width, image.Height,
            image.Components, path, tag);
        if (samples > state.Policy.ResourceLimits.MaximumDecompressedBytes)
        {
            AddIssue(state, IccValidationRuleIds.CompressionLimit,
                IccValidationSeverity.Fatal,
                $"The embedded image expands to at least {samples} component samples, exceeding the configured maximum of {state.Policy.ResourceLimits.MaximumDecompressedBytes} bytes.",
                path, "Reduce the image dimensions or raise the limit only for trusted content.", tag);
            state.MayContinue = false;
            return;
        }
        EmbeddedImageInspection inspection = EmbeddedImageTagCodec.InspectImage(image.Bytes,
            image.EncodingSignature, heightMap,
            state.Policy.ResourceLimits.MaximumDecompressedBytes);
        if (!inspection.Success)
        {
            string rule = inspection.LimitExceeded ? IccValidationRuleIds.CompressionLimit
                : IccValidationRuleIds.TagSize;
            AddIssue(state, rule, inspection.LimitExceeded
                    ? IccValidationSeverity.Fatal : IccValidationSeverity.Error,
                inspection.Error.Length == 0 ? "The embedded image is malformed."
                    : inspection.Error,
                path + ".Bytes", "Provide a well-formed bounded PNG or TIFF image.", tag);
            if (inspection.LimitExceeded)
            {
                state.MayContinue = false;
            }
        }
        else if (inspection.Width != image.Width || inspection.Height != image.Height
            || inspection.Components != image.Components
            || inspection.IsCompressed != image.IsCompressed)
        {
            AddIssue(state, IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "Embedded-image metadata does not match the encoded image.", path,
                "Set dimensions, components, and compression metadata from the inspected image.", tag);
        }
        if (!CurveTagCodec.IsFinite(image.MinimumHeightMeters)
            || !CurveTagCodec.IsFinite(image.MaximumHeightMeters))
        {
            AddNonFiniteIssue(state, path, tag);
        }
    }

    internal static void ValidateSurfaceMap(SecurityStrictValidationState state,
        IccSurfaceMap? surface, string path, IccTag tag)
    {
        if (surface == null)
        {
            return;
        }
        MultiplyDimensions(state, surface.Width, surface.Height, 1, path, tag);
        ValidateDoubleArray(state, surface.Scale, path + ".Scale", tag);
        ValidateImage(state, surface.HeightImage, path + ".HeightImage", tag, true);
        ValidateImage(state, surface.NormalImage, path + ".NormalImage", tag, false);
    }

    internal static void ValidateLut(SecurityStrictValidationState state, IccLut? lut,
        string path, IccTag tag)
    {
        if (lut == null)
        {
            return;
        }
        ValidateMatrix(state, lut.Matrix, path + ".Matrix", tag);
        ValidateClut(state, lut.Clut, path + ".Clut", tag);
        ValidateJaggedArray(state, lut.InputTables, path + ".InputTables", tag);
        ValidateJaggedArray(state, lut.OutputTables, path + ".OutputTables", tag);
    }

    internal static void ValidateMultiStageLut(SecurityStrictValidationState state,
        IccMultiStageLut? lut, string path, IccTag tag)
    {
        if (lut == null)
        {
            return;
        }
        ValidateMatrix(state, lut.Matrix, path + ".Matrix", tag);
        ValidateClut(state, lut.Clut, path + ".Clut", tag);
        ValidateCurveArray(state, lut.ACurves, path + ".ACurves", tag);
        ValidateCurveArray(state, lut.BCurves, path + ".BCurves", tag);
        ValidateCurveArray(state, lut.MCurves, path + ".MCurves", tag);
    }

    internal static void ValidateCurveArray(SecurityStrictValidationState state,
        IccCurve[]? curves, string path, IccTag tag)
    {
        if (curves == null || !ValidateReferenceAllocation(state, curves.Length, path, tag))
        {
            return;
        }
        int index = 0;
        while (index < curves.Length)
        {
            ValidateCurve(state, curves[index], $"{path}[{index}]", tag);
            index = index + 1;
        }
    }

    internal static void ValidateCurve(SecurityStrictValidationState state, IccCurve? curve,
        string path, IccTag tag)
    {
        if (curve == null)
        {
            return;
        }
        if (!CurveTagCodec.IsFinite(curve.Gamma)
            || !CurveTagCodec.IsFinite(curve.InputMinimum)
            || !CurveTagCodec.IsFinite(curve.InputMaximum))
        {
            AddNonFiniteIssue(state, path, tag);
        }
        ValidateDoubleArray(state, curve.Samples, path + ".Samples", tag);
        if (curve.Parametric != null)
        {
            ValidateDoubleArray(state, curve.Parametric.Parameters,
                path + ".Parametric.Parameters", tag);
        }
        if (curve.Segments != null
            && ValidateReferenceAllocation(state, curve.Segments.Length,
                path + ".Segments", tag))
        {
            int index = 0;
            while (index < curve.Segments.Length)
            {
                IccCurveSegment segment = curve.Segments[index];
                if (segment != null)
                {
                    if (!CurveTagCodec.IsFinite(segment.BreakPoint))
                    {
                        AddNonFiniteIssue(state, $"{path}.Segments[{index}].BreakPoint", tag);
                    }
                    ValidateDoubleArray(state, segment.Parameters,
                        $"{path}.Segments[{index}].Parameters", tag);
                    ValidateDoubleArray(state, segment.Samples,
                        $"{path}.Segments[{index}].Samples", tag);
                }
                index = index + 1;
            }
        }
    }

    internal static void ValidateMatrix(SecurityStrictValidationState state, IccMatrix? matrix,
        string path, IccTag tag)
    {
        if (matrix == null)
        {
            return;
        }
        long expected = MultiplyDimensions(state, matrix.Rows, matrix.Columns, 1, path, tag);
        ValidateDoubleArray(state, matrix.Values, path + ".Values", tag);
        if (expected >= 0 && matrix.Values != null && expected != matrix.Values.Length)
        {
            AddIssue(state, IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "Matrix dimensions do not match its value count.", path,
                "Provide exactly rows multiplied by columns finite values.", tag);
        }
    }

    internal static void ValidateClut(SecurityStrictValidationState state, IccClut? clut,
        string path, IccTag tag)
    {
        if (clut == null)
        {
            return;
        }
        long expected = clut.OutputChannels;
        if (clut.GridPoints == null || clut.GridPoints.Length != clut.InputChannels)
        {
            AddIssue(state, IccValidationRuleIds.ClutDimensions,
                IccValidationSeverity.Error, "CLUT grid dimensions are incomplete.", path,
                "Provide one positive grid-point count per input channel.", tag);
            return;
        }
        int index = 0;
        while (index < clut.GridPoints.Length && expected >= 0)
        {
            expected = MultiplyCount(state, expected, clut.GridPoints[index], path, tag);
            index = index + 1;
        }
        ValidateDoubleArray(state, clut.Values, path + ".Values", tag);
        if (expected >= 0 && clut.Values != null && expected != clut.Values.Length)
        {
            AddIssue(state, IccValidationRuleIds.ClutDimensions,
                IccValidationSeverity.Error,
                "CLUT dimension multiplication does not match its value count.", path,
                "Provide the checked product of all grid dimensions and output channels.", tag);
        }
    }

    internal static void ValidateSparseArray(SecurityStrictValidationState state,
        IccSparseMatrixArray? array, string path, IccTag tag)
    {
        if (array == null || array.Matrices == null)
        {
            return;
        }
        IccSparseMatrix[] matrices = array.Matrices;
        if (!ValidateReferenceAllocation(state, matrices.Length,
            path + ".Matrices", tag))
        {
            return;
        }
        long gridProduct = 1;
        if (array.GridPoints != null)
        {
            int gridIndex = 0;
            while (gridIndex < array.GridPoints.Length && gridProduct >= 0)
            {
                gridProduct = MultiplyCount(state, gridProduct,
                    array.GridPoints[gridIndex], path + ".GridPoints", tag);
                gridIndex = gridIndex + 1;
            }
        }
        int index = 0;
        while (index < matrices.Length)
        {
            IccSparseMatrix matrix = matrices[index];
            if (matrix != null)
            {
                MultiplyDimensions(state, matrix.Rows, matrix.Columns, 1,
                    $"{path}.Matrices[{index}]", tag);
                ValidateDoubleArray(state, matrix.Values,
                    $"{path}.Matrices[{index}].Values", tag);
            }
            index = index + 1;
        }
    }

    internal static void ValidateRanges(SecurityStrictValidationState state,
        IccEncodingRange[]? ranges, string path, IccTag tag)
    {
        if (ranges == null || !ValidateReferenceAllocation(state, ranges.Length, path, tag))
        {
            return;
        }
        int index = 0;
        while (index < ranges.Length)
        {
            IccEncodingRange range = ranges[index];
            if (range != null && (!CurveTagCodec.IsFinite(range.Minimum)
                || !CurveTagCodec.IsFinite(range.Maximum)))
            {
                AddNonFiniteIssue(state, $"{path}[{index}]", tag);
            }
            index = index + 1;
        }
    }

    internal static void ValidateJaggedArray(SecurityStrictValidationState state,
        double[][]? values, string path, IccTag tag)
    {
        if (values == null || !ValidateReferenceAllocation(state, values.Length, path, tag))
        {
            return;
        }
        int index = 0;
        while (index < values.Length)
        {
            ValidateDoubleArray(state, values[index], $"{path}[{index}]", tag);
            index = index + 1;
        }
    }

    internal static void ValidateDoubleArray(SecurityStrictValidationState state,
        double[]? values, string path, IccTag tag)
    {
        if (values == null)
        {
            return;
        }
        if (!AddDecodedAllocation(state, MultiplyBytes(values.Length, 8), path, tag))
        {
            return;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!CurveTagCodec.IsFinite(values[index]))
            {
                AddNonFiniteIssue(state, $"{path}[{index}]", tag);
                return;
            }
            index = index + 1;
        }
    }

    internal static void ValidateLongArray(SecurityStrictValidationState state,
        long[]? values, string path, IccTag tag)
    {
        if (values != null)
        {
            AddDecodedAllocation(state, MultiplyBytes(values.Length, 8), path, tag);
        }
    }

    internal static bool ValidateReferenceAllocation(SecurityStrictValidationState state,
        int count, string path, IccTag tag)
    {
        return AddDecodedAllocation(state, MultiplyBytes(count, 8), path, tag);
    }

    internal static long MultiplyDimensions(SecurityStrictValidationState state, int first,
        int second, int third, string path, IccTag tag)
    {
        long product = MultiplyCount(state, 1, first, path, tag);
        if (product >= 0)
        {
            product = MultiplyCount(state, product, second, path, tag);
        }
        if (product >= 0)
        {
            product = MultiplyCount(state, product, third, path, tag);
        }
        return product;
    }

    internal static long MultiplyCount(SecurityStrictValidationState state, long current,
        int factor, string path, IccTag tag)
    {
        if (current < 0 || factor < 0 || (factor != 0 && current > long.MaxValue / factor))
        {
            AddResourceIssue(state, path,
                "A decoded count multiplication is negative or overflows its checked range.", tag);
            return -1;
        }
        long product = current * factor;
        if (product > state.Policy.ResourceLimits.MaximumDecompressedBytes)
        {
            AddResourceIssue(state, path,
                $"A decoded count product of {product} exceeds the configured allocation maximum of {state.Policy.ResourceLimits.MaximumDecompressedBytes}.", tag);
            return -1;
        }
        return product;
    }

    internal static long MultiplyBytes(int count, int width)
    {
        if (count < 0 || width < 0 || (count != 0 && width > long.MaxValue / count))
        {
            return long.MaxValue;
        }
        return (long)count * width;
    }

    internal static void AddEncodedAllocation(SecurityStrictValidationState state, long bytes,
        string path, IccTag tag)
    {
        if (bytes > state.Policy.ResourceLimits.MaximumTagBytes)
        {
            AddResourceIssue(state, path,
                $"An encoded object contains {bytes} bytes, exceeding the configured per-tag maximum of {state.Policy.ResourceLimits.MaximumTagBytes}.", tag);
        }
        state.EncodedBytes = AddSaturated(state.EncodedBytes, bytes);
        if (state.EncodedBytes > state.Policy.ResourceLimits.MaximumTotalTagBytes
            && !state.EncodedLimitReported)
        {
            AddResourceIssue(state, path,
                $"Encoded tag data exceeds the configured aggregate maximum of {state.Policy.ResourceLimits.MaximumTotalTagBytes} bytes.", tag);
            state.EncodedLimitReported = true;
        }
    }

    internal static bool AddDecodedAllocation(SecurityStrictValidationState state, long bytes,
        string path, IccTag tag)
    {
        state.DecodedBytes = AddSaturated(state.DecodedBytes, bytes);
        if ((bytes > state.Policy.ResourceLimits.MaximumDecompressedBytes
            || state.DecodedBytes > state.Policy.ResourceLimits.MaximumDecompressedBytes)
            && !state.DecodedLimitReported)
        {
            AddResourceIssue(state, path,
                $"Decoded model allocation exceeds the configured maximum of {state.Policy.ResourceLimits.MaximumDecompressedBytes} bytes.", tag);
            state.DecodedLimitReported = true;
            return false;
        }
        return bytes <= state.Policy.ResourceLimits.MaximumDecompressedBytes;
    }

    internal static long AddSaturated(long first, long second)
    {
        if (second < 0 || first > long.MaxValue - second)
        {
            return long.MaxValue;
        }
        return first + second;
    }

    internal static void ValidateAliasesAndHiddenData(SecurityStrictValidationState state,
        IccProfile profile)
    {
        if (profile.TagTableEntries == null || profile.Tags == null
            || profile.TagTableEntries.Length > state.Policy.ResourceLimits.MaximumTagCount
            || profile.Tags.Length > state.Policy.ResourceLimits.MaximumTagCount)
        {
            return;
        }
        int index = 0;
        while (index < profile.TagTableEntries.Length)
        {
            IccTagTableEntry entry = profile.TagTableEntries[index];
            int prior = 0;
            while (entry != null && prior < index)
            {
                IccTagTableEntry previous = profile.TagTableEntries[prior];
                if (previous != null && entry.Offset == previous.Offset
                    && entry.Size == previous.Size && entry.Size > 0
                    && !AliasPayloadsMatch(profile, prior, index))
                {
                    AddIssue(state, IccValidationRuleIds.TagTableOverlap,
                        IccValidationSeverity.Fatal,
                        $"Exact payload aliases {prior} and {index} retain different bytes.",
                        $"TagTableEntries[{index}]",
                        "Retain one identical byte sequence for every exact shared payload alias.",
                        index < profile.Tags.Length ? profile.Tags[index] : null);
                    state.MayContinue = false;
                }
                prior = prior + 1;
            }
            index = index + 1;
        }

    }

    internal static bool AliasPayloadsMatch(IccProfile profile, int first, int second)
    {
        if (first >= profile.Tags.Length || second >= profile.Tags.Length
            || profile.Tags[first] == null || profile.Tags[second] == null
            || profile.Tags[first].Data == null || profile.Tags[second].Data == null
            || profile.Tags[first].Data.RawData == null
            || profile.Tags[second].Data.RawData == null
            || profile.Tags[first].Data.RawData.Bytes == null
            || profile.Tags[second].Data.RawData.Bytes == null)
        {
            return true;
        }
        byte[] firstBytes = profile.Tags[first].Data.RawData.Bytes;
        byte[] secondBytes = profile.Tags[second].Data.RawData.Bytes;
        if (firstBytes.Length == 0 || secondBytes.Length == 0)
        {
            return true;
        }
        if (firstBytes.Length != secondBytes.Length)
        {
            return false;
        }
        int index = 0;
        while (index < firstBytes.Length)
        {
            if (firstBytes[index] != secondBytes[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static void AddResourceIssue(SecurityStrictValidationState state, string path,
        string message, IccTag? tag)
    {
        AddIssue(state, IccValidationRuleIds.SecurityResourceLimit,
            IccValidationSeverity.Fatal, message, path,
            "Reduce the untrusted data size or explicitly raise the relevant trusted resource limit.", tag);
        state.MayContinue = false;
    }

    internal static void AddDepthIssue(SecurityStrictValidationState state, string path,
        string message)
    {
        AddIssue(state, IccValidationRuleIds.IccMaxHierarchy,
            IccValidationSeverity.Fatal, message, path,
            "Remove recursive references and reduce the nested hierarchy depth.", null);
        state.MayContinue = false;
    }

    internal static void AddTagDepthIssue(SecurityStrictValidationState state, IccTag tag,
        int tagIndex, string path, string message)
    {
        AddTagIssue(state, IccValidationRuleIds.IccMaxHierarchy,
            IccValidationSeverity.Fatal, message, tag, tagIndex, path,
            "Remove recursive references and reduce the nested hierarchy depth.");
        state.MayContinue = false;
    }

    internal static void AddNonFiniteIssue(SecurityStrictValidationState state, string path,
        IccTag tag)
    {
        AddIssue(state, IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
            "Decoded numeric data contains NaN or infinity.", path,
            "Replace every non-finite value with a finite representable value.", tag);
    }

    internal static void AddTagIssue(SecurityStrictValidationState state, string ruleId,
        IccValidationSeverity severity, string message, IccTag tag, int tagIndex,
        string path, string remediation)
    {
        string resolvedPath = path.Length == 0 ? $"Tags[{tagIndex}]" : path;
        AddIssue(state, ruleId, severity, message, resolvedPath, remediation, tag);
    }

    internal static void AddIssue(SecurityStrictValidationState state, string ruleId,
        IccValidationSeverity severity, string message, string path, string remediation,
        IccTag? tag)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = ruleId;
        issue.Severity = severity;
        issue.Message = message;
        issue.Path = path;
        issue.Remediation = remediation;
        if (tag != null && tag.TableEntry != null && tag.TableEntry.Offset >= 0)
        {
            issue.Location.Offset = tag.TableEntry.Offset;
            issue.Location.Length = tag.TableEntry.Size < 0 ? 0 : tag.TableEntry.Size;
        }
        state.Issues.Add(issue);
    }
}
