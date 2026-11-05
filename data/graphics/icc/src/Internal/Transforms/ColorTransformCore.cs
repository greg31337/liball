namespace icc;

internal class SingleColorTransformResult
{
    internal float[] Values;
    internal IccGamutStatus GamutStatus;
    internal IccTransformTrace? Trace;
    internal IccOperationResult Result;

    internal SingleColorTransformResult()
    {
        Values = new float[0];
        GamutStatus = IccGamutStatus.Unknown;
        Trace = null;
        Result = new IccOperationResult();
    }
}

internal class ColorTransformCore
{
    internal bool IsCompiled;
    internal int SourceChannels;
    internal int DestinationChannels;
    internal PipelineStage[] Stages;
    internal IccEncodingRange[] SourceRanges;
    internal IccEncodingRange[] DestinationRanges;
    internal IccTransformOptions Options;
    internal PipelineStage[] GamutStages;
    internal IccProofingOptions? ProofingOptions;
    internal IccTransformPlan Plan;
    internal IccGamutStatus LastGamutStatus;

    internal ColorTransformCore()
    {
        IsCompiled = false;
        SourceChannels = 0;
        DestinationChannels = 0;
        Stages = new PipelineStage[0];
        SourceRanges = new IccEncodingRange[0];
        DestinationRanges = new IccEncodingRange[0];
        Options = new IccTransformOptions();
        GamutStages = new PipelineStage[0];
        ProofingOptions = null;
        Plan = new IccTransformPlan();
        LastGamutStatus = IccGamutStatus.Unknown;
    }

    internal void Configure(PipelineStage[] stages, int sourceChannels, int destinationChannels, IccEncodingRange[] sourceRanges,
        IccEncodingRange[] destinationRanges, IccTransformOptions options, uint tagSignature)
    {
        if (stages == null || sourceRanges == null || destinationRanges == null || options == null || sourceChannels < 1 || destinationChannels < 1)
        {
            throw new IccTransformException("A compiled colour transform requires stages, options, ranges, and positive endpoint channel counts.");
        }
        if ((sourceRanges.Length != 0 && sourceRanges.Length != sourceChannels) || (destinationRanges.Length != 0 && destinationRanges.Length != destinationChannels))
        {
            throw new IccTransformException("A compiled colour transform range array shall be empty or match its endpoint channel count.");
        }
        if (stages.Length == 0 && sourceChannels != destinationChannels)
        {
            throw new IccTransformException("An empty identity transform shall preserve its endpoint channel count.");
        }
        if (stages.Length > 0 && (stages[0].InputChannels != sourceChannels || stages[stages.Length - 1].OutputChannels != destinationChannels))
        {
            throw new IccTransformException("The transform stages do not match the declared source and destination channel counts.");
        }

        PipelineExecutor.GetRequiredValueScratchLength(stages);
        ValidateRanges(sourceRanges, "source");
        ValidateRanges(destinationRanges, "destination");
        Stages = CopyStages(stages);
        SourceChannels = sourceChannels;
        DestinationChannels = destinationChannels;
        SourceRanges = CopyRanges(sourceRanges);
        DestinationRanges = CopyRanges(destinationRanges);
        Options = CopyOptions(options);
        GamutStages = new PipelineStage[0];
        ProofingOptions = null;
        Plan = CreatePublicPlan(Stages, SourceChannels, DestinationChannels, Options, tagSignature);
        LastGamutStatus = IccGamutStatus.Unknown;
        IsCompiled = true;
    }

    internal void ConfigureProofing(PipelineStage[] stages, PipelineStage[] gamutStages, int sourceChannels,
        int destinationChannels, IccEncodingRange[] sourceRanges, IccEncodingRange[] destinationRanges,
        IccTransformOptions transformOptions, IccProofingOptions proofingOptions)
    {
        if (gamutStages == null || proofingOptions == null)
        {
            throw new IccTransformException("A proofing transform requires gamut stages and proofing options.");
        }
        Configure(stages, sourceChannels, destinationChannels, sourceRanges, destinationRanges, transformOptions, 0);
        if (proofingOptions.EnableGamutCheck)
        {
            PipelineExecutor.GetRequiredValueScratchLength(gamutStages);
            if (gamutStages.Length == 0 || gamutStages[0].InputChannels != sourceChannels
                || gamutStages[gamutStages.Length - 1].OutputChannels != 1)
            {
                throw new IccTransformException("A proofing gamut-check pipeline must connect the source channels to one gamut channel.");
            }
        }
        GamutStages = CopyStages(gamutStages);
        ProofingOptions = ProofingTransformCompiler.CopyProofingOptions(proofingOptions);
        Plan.ProofingOptions = ProofingTransformCompiler.CopyProofingOptions(proofingOptions);
        Plan.GamutStages = CreatePublicStages(GamutStages);
    }

    internal void ConfigureDeviceLink(PipelineStage[] stages, int sourceChannels, int destinationChannels,
        IccEncodingRange[] sourceRanges, IccEncodingRange[] destinationRanges, IccTransformOptions options,
        uint sourceColorSpaceSignature, uint destinationColorSpaceSignature,
        IccProfileSequenceEntry[] profileSequence, IccProfileSequenceIdentifier[] profileSequenceIdentifiers)
    {
        if (profileSequence == null || profileSequenceIdentifiers == null)
        {
            throw new IccTransformException("DeviceLink sequence metadata arrays cannot be null.");
        }
        Configure(stages, sourceChannels, destinationChannels, sourceRanges, destinationRanges, options, 0);
        ProfileCloneContext context = new ProfileCloneContext();
        Plan.SourceColorSpaceSignature = sourceColorSpaceSignature;
        Plan.DestinationColorSpaceSignature = destinationColorSpaceSignature;
        Plan.ProfileSequence = ExtendedTagDataClone.CloneProfileSequenceEntryArray(profileSequence,
            "compiled DeviceLink profile sequence", context);
        Plan.ProfileSequenceIdentifiers = ExtendedTagDataClone.CloneProfileSequenceIdentifierArray(
            profileSequenceIdentifiers, "compiled DeviceLink profile sequence identifiers", context);
    }

    internal int GetSourceChannelCount()
    {
        return IsCompiled ? SourceChannels : 0;
    }

    internal int GetDestinationChannelCount()
    {
        return IsCompiled ? DestinationChannels : 0;
    }

    internal SingleColorTransformResult TransformSingle(System.ReadOnlySpan<float> sourceColor)
    {
        if (!IsCompiled)
        {
            throw CreateUnsupportedException();
        }

        IccOperationResult probe = ProbeSingle(sourceColor);
        if (probe.Status != IccOperationStatus.Success)
        {
            throw new IccTransformException(probe.Message);
        }

        SingleColorTransformResult execution = SingleColorTransformExecutor.Execute(Stages, SourceChannels, DestinationChannels,
            DestinationRanges, Options.ClipOutput, Options.EnableTrace, sourceColor);
        IccProofingOptions? proofing = ProofingOptions;
        if (proofing != null && proofing.EnableGamutCheck)
        {
            ProofingTransformExecutor.ApplySingle(GamutStages, proofing.AlarmColor, DestinationRanges,
                Options.ClipOutput, sourceColor, execution);
        }
        LastGamutStatus = execution.GamutStatus;
        Plan.Trace = execution.Trace;
        return execution;
    }

    internal void TransformInterleaved(System.ReadOnlySpan<float> sourceColors, int sourceStride, System.Span<float> destinationColors,
        int destinationStride, int colorCount)
    {
        if (!IsCompiled)
        {
            throw CreateUnsupportedException();
        }

        float[]? proofingSource = null;
        IccProofingOptions? proofing = ProofingOptions;
        if (proofing != null && proofing.EnableGamutCheck)
        {
            proofingSource = sourceColors.ToArray();
        }
        InterleavedColorTransformResult execution = InterleavedColorTransformExecutor.Execute(Stages, SourceChannels, DestinationChannels,
            DestinationRanges, Options.ClipOutput, Options.EnableTrace, sourceColors, sourceStride, destinationColors, destinationStride, colorCount);
        if (proofing != null && proofing.EnableGamutCheck && proofingSource != null)
        {
            execution.GamutStatus = ProofingTransformExecutor.ApplyInterleaved(GamutStages, proofing.AlarmColor,
                DestinationRanges, Options.ClipOutput, proofingSource, sourceStride, destinationColors,
                destinationStride, colorCount, execution.Trace);
        }
        LastGamutStatus = execution.GamutStatus;
        Plan.Trace = execution.Trace;
    }

    internal IccOperationResult ProbeSingle(System.ReadOnlySpan<float> sourceColor)
    {
        if (!IsCompiled)
        {
            return CreateUnsupportedResult();
        }
        if (sourceColor.Length != SourceChannels)
        {
            return CreateResult(IccOperationStatus.InvalidData, $"The transform requires {SourceChannels} source channels, but it received {sourceColor.Length}.");
        }

        int channel = 0;
        while (channel < sourceColor.Length)
        {
            if (!System.Single.IsFinite(sourceColor[channel]))
            {
                return CreateResult(IccOperationStatus.InvalidData, $"Transform input channel {channel} must be finite.");
            }
            channel = channel + 1;
        }

        return CreateResult(IccOperationStatus.Success, "The source colour can be transformed.");
    }

    internal IccUnsupportedFeatureException CreateUnsupportedException()
    {
        return new IccUnsupportedFeatureException("Transform execution is not available for this input.");
    }

    internal IccOperationResult CreateUnsupportedResult()
    {
        return CreateResult(IccOperationStatus.UnsupportedFeature, "Transform execution is not available for this input.");
    }

    internal static IccTransformPlan CreatePublicPlan(PipelineStage[] stages, int sourceChannels, int destinationChannels, IccTransformOptions options, uint tagSignature)
    {
        IccTransformPlan plan = new IccTransformPlan();
        plan.Options = CopyOptions(options);
        plan.SourceChannels = sourceChannels;
        plan.DestinationChannels = destinationChannels;
        plan.Stages = CreatePublicStages(stages);
        int stageIndex = 0;
        while (stageIndex < plan.Stages.Length)
        {
            if (plan.Stages[stageIndex].TagSignature == 0)
            {
                plan.Stages[stageIndex].TagSignature = tagSignature;
            }
            stageIndex = stageIndex + 1;
        }
        plan.Result = CreateResult(IccOperationStatus.Success, "The transform plan is compiled and executable.");
        return plan;
    }

    internal static IccTransformStage[] CreatePublicStages(PipelineStage[] stages)
    {
        IccTransformStage[] result = new IccTransformStage[stages.Length];
        int index = 0;
        while (index < stages.Length)
        {
            PipelineStage stage = stages[index];
            IccTransformStage description = new IccTransformStage();
            description.Name = stage.Description;
            description.Kind = GetPublicStageKind(stage.Kind);
            description.TagSignature = stage.TagSignature;
            description.InputChannels = stage.InputChannels;
            description.OutputChannels = stage.OutputChannels;
            result[index] = description;
            index = index + 1;
        }
        return result;
    }

    internal static IccProcessingElementKind GetPublicStageKind(PipelineStageKind kind)
    {
        switch (kind)
        {
            case PipelineStageKind.Curves:
                return IccProcessingElementKind.CurveSet;
            case PipelineStageKind.Matrix:
                return IccProcessingElementKind.Matrix;
            case PipelineStageKind.Clut:
                return IccProcessingElementKind.Clut;
            case PipelineStageKind.ToneMap:
                return IccProcessingElementKind.ToneMap;
            default:
                return IccProcessingElementKind.Unknown;
        }
    }

    internal static PipelineStage[] CopyStages(PipelineStage[] stages)
    {
        PipelineStage[] copy = new PipelineStage[stages.Length];
        ProfileCloneContext context = new ProfileCloneContext();
        int index = 0;
        while (index < stages.Length)
        {
            PipelineStage source = stages[index];
            PipelineStage stage = new PipelineStage();
            stage.Kind = source.Kind;
            stage.InputChannels = source.InputChannels;
            stage.OutputChannels = source.OutputChannels;
            stage.Curves = ExtendedTagDataClone.CloneCurveArray(source.Curves, $"transform stage {index} curves", context);
            stage.InvertCurves = source.InvertCurves;
            stage.Matrix = source.Matrix == null ? null : ExtendedTagDataClone.CloneMatrix(source.Matrix);
            stage.Clut = source.Clut == null ? null : ExtendedTagDataClone.CloneClut(source.Clut);
            stage.ToneMap = source.ToneMap == null ? null : ExtendedTagDataClone.CloneToneMap(source.ToneMap, context);
            stage.Interpolation = source.Interpolation;
            stage.PcsKind = source.PcsKind;
            stage.PcsEncoding = source.PcsEncoding;
            stage.ChannelOrder = ExtendedTagDataClone.CloneIntArray(source.ChannelOrder, $"transform stage {index} channel order");
            stage.ChannelDefaults = ExtendedTagDataClone.CloneDoubleArray(source.ChannelDefaults, $"transform stage {index} channel defaults");
            stage.Ranges = ExtendedTagDataClone.CloneEncodingRangeArray(source.Ranges, $"transform stage {index} ranges");
            stage.Description = source.Description;
            stage.TagSignature = source.TagSignature;
            copy[index] = stage;
            index = index + 1;
        }

        return copy;
    }

    internal static IccEncodingRange[] CopyRanges(IccEncodingRange[] ranges)
    {
        IccEncodingRange[] copy = new IccEncodingRange[ranges.Length];
        int index = 0;
        while (index < ranges.Length)
        {
            IccEncodingRange range = new IccEncodingRange();
            range.Minimum = ranges[index].Minimum;
            range.Maximum = ranges[index].Maximum;
            copy[index] = range;
            index = index + 1;
        }

        return copy;
    }

    internal static IccTransformOptions CopyOptions(IccTransformOptions options)
    {
        IccTransformOptions copy = new IccTransformOptions();
        copy.Direction = options.Direction;
        copy.RenderingIntent = options.RenderingIntent;
        copy.IntentFallback = options.IntentFallback;
        copy.Interpolation = options.Interpolation;
        copy.PcsEncoding = options.PcsEncoding;
        copy.BlackPointCompensation = options.BlackPointCompensation;
        copy.ClipOutput = options.ClipOutput;
        copy.EnableTrace = options.EnableTrace;
        return copy;
    }

    internal static void ValidateRanges(IccEncodingRange[] ranges, string description)
    {
        int index = 0;
        while (index < ranges.Length)
        {
            IccEncodingRange? range = ranges[index];
            if (range == null || !System.Double.IsFinite(range.Minimum) || !System.Double.IsFinite(range.Maximum) || range.Minimum > range.Maximum)
            {
                throw new IccTransformException($"Transform {description} range {index} shall have finite ordered endpoints.");
            }
            index = index + 1;
        }
    }

    internal static IccOperationResult CreateResult(IccOperationStatus status, string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }
}

internal static class SingleColorTransformExecutor
{
    internal static SingleColorTransformResult Execute(PipelineStage[] stages, int sourceChannels, int destinationChannels,
        IccEncodingRange[] destinationRanges, bool clipOutput, bool enableTrace, System.ReadOnlySpan<float> sourceColor)
    {
        if (stages == null || destinationRanges == null || sourceColor.Length != sourceChannels || sourceChannels < 1 || destinationChannels < 1)
        {
            throw new IccTransformException("Single-colour execution received inconsistent stages or endpoint channel counts.");
        }
        if (destinationRanges.Length != 0 && destinationRanges.Length != destinationChannels)
        {
            throw new IccTransformException("Single-colour execution requires one destination range per output channel when ranges are provided.");
        }

        int scratchLength = PipelineExecutor.GetRequiredValueScratchLength(stages);
        if (stages.Length == 0 && sourceChannels != destinationChannels)
        {
            throw new IccTransformException("An empty single-colour pipeline shall preserve its channel count.");
        }
        if (stages.Length > 0 && (stages[0].InputChannels != sourceChannels || stages[stages.Length - 1].OutputChannels != destinationChannels))
        {
            throw new IccTransformException("The single-colour pipeline endpoints do not match its declared channel counts.");
        }

        double[] inputs = new double[sourceChannels];
        int channel = 0;
        while (channel < sourceChannels)
        {
            if (!System.Single.IsFinite(sourceColor[channel]))
            {
                throw new IccTransformException($"Transform input channel {channel} must be finite.");
            }
            inputs[channel] = sourceColor[channel];
            channel = channel + 1;
        }

        double[] outputs = new double[destinationChannels];
        int indexLength = PipelineExecutor.GetRequiredClutIndexScratchLength(stages);
        int weightLength = PipelineExecutor.GetRequiredClutWeightScratchLength(stages);
        double[] firstScratch = new double[scratchLength];
        double[] secondScratch = new double[scratchLength];
        int[] indexScratch = new int[indexLength];
        double[] weightScratch = new double[weightLength];
        IccTransformTrace? trace = enableTrace
            ? ExecuteWithTrace(stages, inputs, outputs, firstScratch, secondScratch, indexScratch, weightScratch)
            : null;
        if (!enableTrace)
        {
            PipelineExecutor.ExecutePipeline(stages, inputs, outputs, firstScratch, secondScratch, indexScratch, weightScratch);
        }

        IccGamutStatus gamutStatus = GetGamutStatus(outputs, destinationRanges);
        if (clipOutput && destinationRanges.Length == destinationChannels)
        {
            ClipOutputs(outputs, destinationRanges);
        }
        if (trace != null)
        {
            trace.Destination = CreateVector(outputs);
        }

        float[] values = ConvertToFloats(outputs);
        SingleColorTransformResult result = new SingleColorTransformResult();
        result.Values = values;
        result.GamutStatus = gamutStatus;
        result.Trace = trace;
        result.Result = ColorTransformCore.CreateResult(IccOperationStatus.Success, "The source colour was transformed successfully.");
        return result;
    }

    internal static IccTransformTrace ExecuteWithTrace(PipelineStage[] stages, double[] inputs, double[] outputs,
        double[] firstScratch, double[] secondScratch, int[] indexScratch, double[] weightScratch)
    {
        IccTransformTrace trace = new IccTransformTrace();
        trace.Source = CreateVector(inputs);
        trace.Intermediates = new IccColorVector[stages.Length];
        trace.Messages = new string[stages.Length];
        if (stages.Length == 0)
        {
            new System.ReadOnlySpan<double>(inputs).CopyTo(outputs);
            trace.Destination = CreateVector(outputs);
            return trace;
        }

        new System.ReadOnlySpan<double>(inputs).CopyTo(firstScratch);
        System.ReadOnlySpan<double> current = new System.ReadOnlySpan<double>(firstScratch, 0, inputs.Length);
        bool writeSecond = true;
        int stageIndex = 0;
        while (stageIndex < stages.Length)
        {
            PipelineStage stage = stages[stageIndex];
            System.Span<double> target = writeSecond
                ? new System.Span<double>(secondScratch, 0, stage.OutputChannels)
                : new System.Span<double>(firstScratch, 0, stage.OutputChannels);
            PipelineExecutor.ExecuteStage(stage, current, target, indexScratch, weightScratch);
            trace.Intermediates[stageIndex] = CreateVector(target);
            trace.Messages[stageIndex] = stage.Description.Length == 0 ? $"Transform stage {stageIndex}." : stage.Description;
            current = target;
            writeSecond = !writeSecond;
            stageIndex = stageIndex + 1;
        }

        current.CopyTo(outputs);
        trace.Destination = CreateVector(outputs);
        return trace;
    }

    internal static IccGamutStatus GetGamutStatus(double[] outputs, IccEncodingRange[] ranges)
    {
        if (ranges.Length != outputs.Length)
        {
            return IccGamutStatus.Unknown;
        }

        bool boundary = false;
        int channel = 0;
        while (channel < outputs.Length)
        {
            double value = outputs[channel];
            IccEncodingRange range = ranges[channel];
            if (value < range.Minimum || value > range.Maximum)
            {
                return IccGamutStatus.Outside;
            }
            if (value == range.Minimum || value == range.Maximum)
            {
                boundary = true;
            }
            channel = channel + 1;
        }

        return boundary ? IccGamutStatus.Boundary : IccGamutStatus.Inside;
    }

    internal static void ClipOutputs(double[] outputs, IccEncodingRange[] ranges)
    {
        int channel = 0;
        while (channel < outputs.Length)
        {
            if (outputs[channel] < ranges[channel].Minimum)
            {
                outputs[channel] = ranges[channel].Minimum;
            }
            else if (outputs[channel] > ranges[channel].Maximum)
            {
                outputs[channel] = ranges[channel].Maximum;
            }
            channel = channel + 1;
        }
    }

    internal static float[] ConvertToFloats(double[] outputs)
    {
        float[] values = new float[outputs.Length];
        int channel = 0;
        while (channel < outputs.Length)
        {
            if (!System.Double.IsFinite(outputs[channel]) || outputs[channel] < -System.Single.MaxValue || outputs[channel] > System.Single.MaxValue)
            {
                throw new IccTransformException($"Transform output channel {channel} cannot be represented as a finite float value.");
            }
            values[channel] = (float)outputs[channel];
            channel = channel + 1;
        }

        return values;
    }

    internal static IccColorVector CreateVector(System.ReadOnlySpan<double> values)
    {
        IccColorVector vector = new IccColorVector();
        vector.Components = values.ToArray();
        return vector;
    }
}
