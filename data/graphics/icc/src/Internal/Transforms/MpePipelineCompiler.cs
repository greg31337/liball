namespace icc;

internal class MpePipelinePlan
{
    internal uint TagSignature;
    internal IccTransformDirection Direction;
    internal int SourceChannels;
    internal int DestinationChannels;
    internal IccEncodingRange[] SourceRanges;
    internal IccEncodingRange[] DestinationRanges;
    internal PipelineStage[] Stages;

    internal MpePipelinePlan()
    {
        TagSignature = 0;
        Direction = IccTransformDirection.DeviceToPcs;
        SourceChannels = 0;
        DestinationChannels = 0;
        SourceRanges = new IccEncodingRange[0];
        DestinationRanges = new IccEncodingRange[0];
        Stages = new PipelineStage[0];
    }
}

internal static class MpePipelineCompiler
{
    internal static MpePipelinePlan Compile(IccProfile profile, IccTag tag, IccTransformDirection direction, IccInterpolationKind interpolation)
    {
        ValidateRequest(profile, tag, direction, interpolation);
        IccMultiProcessElements? pipeline = tag.Data.MultiProcessElements;
        if (pipeline == null)
        {
            throw new IccTransformException("The selected tag has no decoded multi-process-elements pipeline.");
        }

        ValidatePipeline(profile, pipeline, tag.Signature, direction, interpolation);
        PipelineStage[] stages = new PipelineStage[pipeline.Elements.Length];
        int index = 0;
        while (index < pipeline.Elements.Length)
        {
            stages[index] = CompileElement(profile, pipeline.Elements[index], index, interpolation);
            index = index + 1;
        }

        PipelineExecutor.GetRequiredValueScratchLength(stages);
        MpePipelinePlan plan = new MpePipelinePlan();
        plan.TagSignature = tag.Signature;
        plan.Direction = direction;
        plan.SourceChannels = pipeline.InputChannels;
        plan.DestinationChannels = pipeline.OutputChannels;
        plan.SourceRanges = CloneRanges(pipeline.InputRanges, "pipeline input ranges");
        plan.DestinationRanges = CloneRanges(pipeline.OutputRanges, "pipeline output ranges");
        plan.Stages = stages;
        return plan;
    }

    internal static MpePipelinePlan CompileEmbedded(IccProfile profile, IccMultiProcessElements pipeline, IccInterpolationKind interpolation)
    {
        if (profile == null || profile.Header == null || profile.Header.Version.Kind != IccVersionKind.Version5 || pipeline == null)
        {
            throw new IccTransformException("An embedded BRDF pipeline requires an ICC.2 profile and decoded multi-process-elements data.");
        }
        if (interpolation != IccInterpolationKind.Linear && interpolation != IccInterpolationKind.Multilinear && interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccTransformException("The requested embedded-pipeline interpolation algorithm is invalid.");
        }

        ValidatePipelineContents(profile, pipeline, interpolation);
        PipelineStage[] stages = new PipelineStage[pipeline.Elements.Length];
        int index = 0;
        while (index < pipeline.Elements.Length)
        {
            stages[index] = CompileElement(profile, pipeline.Elements[index], index, interpolation);
            index = index + 1;
        }

        PipelineExecutor.GetRequiredValueScratchLength(stages);
        MpePipelinePlan plan = new MpePipelinePlan();
        plan.SourceChannels = pipeline.InputChannels;
        plan.DestinationChannels = pipeline.OutputChannels;
        plan.SourceRanges = CloneRanges(pipeline.InputRanges, "embedded pipeline input ranges");
        plan.DestinationRanges = CloneRanges(pipeline.OutputRanges, "embedded pipeline output ranges");
        plan.Stages = stages;
        return plan;
    }

    internal static MpePipelinePlan CompileDeviceLink(IccProfile profile, IccTag tag, IccInterpolationKind interpolation)
    {
        if (profile == null || profile.Header == null || tag == null || tag.Data == null
            || profile.Header.Version.Kind != IccVersionKind.Version4 || profile.Header.ProfileClass != IccProfileClass.DeviceLink)
        {
            throw new IccTransformException("A direct DToB0 pipeline requires an ICC version 4 DeviceLink profile and decoded tag.");
        }
        if (tag.Signature != IccTagSignatures.DToB0 || tag.Data.MultiProcessElements == null)
        {
            throw new IccTransformException("A direct DeviceLink MPE pipeline requires decoded DToB0 multi-process elements.");
        }
        if (tag.Data.RawData != null && tag.Data.RawData.HasTypeSignature
            && tag.Data.RawData.TypeSignature != IccTypeSignatures.MultiProcessElements)
        {
            throw new IccTransformException("The DeviceLink DToB0 tag is not a multiProcessElementsType payload.");
        }
        if (interpolation != IccInterpolationKind.Linear && interpolation != IccInterpolationKind.Multilinear
            && interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccTransformException("The requested DeviceLink MPE interpolation algorithm is invalid.");
        }

        IccMultiProcessElements pipeline = tag.Data.MultiProcessElements;
        ValidatePipelineContents(profile, pipeline, interpolation);
        int expectedInput = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        int expectedOutput = ColorSpaceChannelCount.Decode(profile.Header.ProfileConnectionSpaceSignature);
        if (expectedInput < 1 || expectedOutput < 1 || pipeline.InputChannels != expectedInput
            || pipeline.OutputChannels != expectedOutput)
        {
            throw new IccTransformException($"The DeviceLink DToB0 pipeline declares {pipeline.InputChannels}-to-{pipeline.OutputChannels} channels, but its header colour spaces require {expectedInput}-to-{expectedOutput}.");
        }

        PipelineStage[] stages = new PipelineStage[pipeline.Elements.Length];
        int index = 0;
        while (index < pipeline.Elements.Length)
        {
            stages[index] = CompileElement(profile, pipeline.Elements[index], index, interpolation);
            index = index + 1;
        }
        PipelineExecutor.GetRequiredValueScratchLength(stages);
        MpePipelinePlan plan = new MpePipelinePlan();
        plan.TagSignature = tag.Signature;
        plan.Direction = IccTransformDirection.DeviceToDevice;
        plan.SourceChannels = pipeline.InputChannels;
        plan.DestinationChannels = pipeline.OutputChannels;
        plan.SourceRanges = CloneRanges(pipeline.InputRanges, "DeviceLink input ranges");
        plan.DestinationRanges = CloneRanges(pipeline.OutputRanges, "DeviceLink output ranges");
        plan.Stages = stages;
        return plan;
    }

    internal static void ValidatePipeline(IccProfile profile, IccMultiProcessElements pipeline, uint tagSignature, IccTransformDirection direction, IccInterpolationKind interpolation)
    {
        ValidatePipelineContents(profile, pipeline, interpolation);
        ValidateProfileChannels(profile, pipeline, tagSignature, direction);
    }

    internal static void ValidatePipelineContents(IccProfile profile, IccMultiProcessElements pipeline, IccInterpolationKind interpolation)
    {
        if (pipeline.Elements == null || pipeline.InputRanges == null || pipeline.OutputRanges == null || pipeline.Elements.Length == 0)
        {
            throw new IccTransformException("A v4 MPE pipeline requires elements and channel-range arrays.");
        }
        if (pipeline.InputChannels < 1 || pipeline.OutputChannels < 1 || pipeline.InputRanges.Length != pipeline.InputChannels || pipeline.OutputRanges.Length != pipeline.OutputChannels)
        {
            throw new IccTransformException("The v4 MPE pipeline channel counts and range counts are inconsistent.");
        }

        ValidateRanges(pipeline.InputRanges, "pipeline input ranges");
        ValidateRanges(pipeline.OutputRanges, "pipeline output ranges");
        int previousOutputs = pipeline.InputChannels;
        int index = 0;
        while (index < pipeline.Elements.Length)
        {
            IccProcessingElement? element = pipeline.Elements[index];
            if (element == null)
            {
                throw new IccTransformException($"MPE element {index} cannot be null.");
            }
            if (element.InputChannels != previousOutputs)
            {
                throw new IccTransformException($"MPE element {index} expects {element.InputChannels} channels after {previousOutputs} channels.");
            }

            ValidateElement(element, index, interpolation);
            if (profile.Header.Version.Kind == IccVersionKind.Version4 && !IsVersionFourElement(element.Kind))
            {
                throw new IccTransformException($"MPE element {index} has no executable ICC version 4 semantics.");
            }
            previousOutputs = element.OutputChannels;
            index = index + 1;
        }
        if (previousOutputs != pipeline.OutputChannels)
        {
            throw new IccTransformException($"The last MPE element produces {previousOutputs} channels instead of the declared {pipeline.OutputChannels}.");
        }
    }

    internal static void ValidateElement(IccProcessingElement element, int index, IccInterpolationKind interpolation)
    {
        if (element.InputChannels < 1 || element.OutputChannels < 1)
        {
            throw new IccTransformException($"MPE element {index} must declare positive channel counts.");
        }

        switch (element.Kind)
        {
            case IccProcessingElementKind.CurveSet:
                ValidateCurveSet(element, index);
                return;
            case IccProcessingElementKind.Matrix:
                ValidateMatrix(element, index);
                return;
            case IccProcessingElementKind.Clut:
                ValidateClut(element, index, interpolation);
                return;
            case IccProcessingElementKind.ExtendedClut:
            case IccProcessingElementKind.SparseMatrix:
            case IccProcessingElementKind.TintArray:
            case IccProcessingElementKind.ToneMap:
            case IccProcessingElementKind.EmissionClut:
            case IccProcessingElementKind.EmissionMatrix:
            case IccProcessingElementKind.EmissionObserver:
            case IccProcessingElementKind.InverseEmissionMatrix:
            case IccProcessingElementKind.ReflectanceClut:
            case IccProcessingElementKind.ReflectanceObserver:
                return;
            case IccProcessingElementKind.BeginAcs:
                ValidateAcs(element, index, true);
                return;
            case IccProcessingElementKind.EndAcs:
                ValidateAcs(element, index, false);
                return;
            default:
                throw new IccTransformException($"MPE element {index} with signature 0x{element.Signature:X8} has no executable ICC version 4 semantics.");
        }
    }

    internal static void ValidateCurveSet(IccProcessingElement element, int index)
    {
        if (element.Signature != IccProcessingElementSignatures.CurveSet || element.InputChannels != element.OutputChannels || element.Curves == null || element.Curves.Length != element.InputChannels)
        {
            throw new IccTransformException($"MPE curve-set element {index} has inconsistent signatures, channels, or curves.");
        }

        int curveIndex = 0;
        while (curveIndex < element.Curves.Length)
        {
            IccCurve? curve = element.Curves[curveIndex];
            if (curve == null || curve.Kind != IccCurveKind.Segmented)
            {
                throw new IccTransformException($"MPE curve-set element {index} curve {curveIndex} must be a decoded segmented curve.");
            }

            MatrixTrcCompiler.ValidateCurve(curve, $"MPE element {index} curve {curveIndex}", false);
            curveIndex = curveIndex + 1;
        }
    }

    internal static void ValidateMatrix(IccProcessingElement element, int index)
    {
        IccMatrix? matrix = element.Matrix;
        if (element.Signature != IccProcessingElementSignatures.Matrix || matrix == null || matrix.Values == null || matrix.Rows != element.OutputChannels || matrix.Columns != element.InputChannels + 1)
        {
            throw new IccTransformException($"MPE matrix element {index} must contain an output-by-(input-plus-one) affine matrix.");
        }
        if (matrix.Rows > System.Int32.MaxValue / matrix.Columns || matrix.Values.Length != matrix.Rows * matrix.Columns)
        {
            throw new IccTransformException($"MPE matrix element {index} has an invalid or overflowing value count.");
        }

        ValidateFiniteValues(matrix.Values, $"MPE matrix element {index}");
    }

    internal static void ValidateClut(IccProcessingElement element, int index, IccInterpolationKind interpolation)
    {
        IccClut? clut = element.Clut;
        if (element.Signature != IccProcessingElementSignatures.Clut || clut == null || clut.GridPoints == null || clut.Values == null || clut.InputChannels != element.InputChannels || clut.OutputChannels != element.OutputChannels || clut.GridPoints.Length != element.InputChannels)
        {
            throw new IccTransformException($"MPE CLUT element {index} has inconsistent signatures, channels, or table data.");
        }
        if (element.InputChannels > 15)
        {
            throw new IccTransformException($"MPE CLUT element {index} exceeds the executable 15-channel interpolation limit.");
        }
        if (interpolation == IccInterpolationKind.Linear && element.InputChannels != 1)
        {
            throw new IccTransformException($"MPE CLUT element {index} can use linear interpolation only with one input channel.");
        }

        int valueCount = element.OutputChannels;
        int dimension = 0;
        while (dimension < element.InputChannels)
        {
            int grid = clut.GridPoints[dimension];
            if (grid < 2 || grid > 255 || valueCount > System.Int32.MaxValue / grid)
            {
                throw new IccTransformException($"MPE CLUT element {index} grid dimension {dimension} is invalid or overflows its value count.");
            }

            valueCount = valueCount * grid;
            dimension = dimension + 1;
        }
        if (clut.Values.Length != valueCount)
        {
            throw new IccTransformException($"MPE CLUT element {index} requires exactly {valueCount} table values.");
        }

        ValidateFiniteValues(clut.Values, $"MPE CLUT element {index}");
    }

    internal static void ValidateAcs(IccProcessingElement element, int index, bool begin)
    {
        uint expected = begin ? IccProcessingElementSignatures.BeginAcs : IccProcessingElementSignatures.EndAcs;
        if (element.Signature != expected || element.InputChannels != element.OutputChannels || element.AcsData == null)
        {
            throw new IccTransformException($"MPE ACS element {index} has inconsistent kind, signature, channels, or vendor data.");
        }
    }

    internal static PipelineStage CompileElement(IccProfile profile, IccProcessingElement element, int index, IccInterpolationKind interpolation)
    {
        switch (element.Kind)
        {
            case IccProcessingElementKind.CurveSet:
                return CreateCurveStage(element, index);
            case IccProcessingElementKind.Matrix:
                return CreateMatrixStage(element, index);
            case IccProcessingElementKind.Clut:
                return CreateClutStage(element, index, interpolation);
            case IccProcessingElementKind.ExtendedClut:
                return Icc2PipelineStages.CreateExtendedClutStage(element, index, interpolation);
            case IccProcessingElementKind.SparseMatrix:
                return Icc2PipelineStages.CreateSparseMatrixStage(element, index, interpolation);
            case IccProcessingElementKind.TintArray:
                return Icc2PipelineStages.CreateTintArrayStage(element, index);
            case IccProcessingElementKind.ToneMap:
                return Icc2PipelineStages.CreateToneMapStage(element, index);
            case IccProcessingElementKind.EmissionClut:
            case IccProcessingElementKind.EmissionMatrix:
            case IccProcessingElementKind.EmissionObserver:
            case IccProcessingElementKind.InverseEmissionMatrix:
            case IccProcessingElementKind.ReflectanceClut:
            case IccProcessingElementKind.ReflectanceObserver:
                return Icc2PipelineStages.CreateSpectralStage(profile, element, index, interpolation);
            case IccProcessingElementKind.BeginAcs:
            case IccProcessingElementKind.EndAcs:
                return CreateAcsStage(element, index);
            default:
                throw new IccTransformException($"MPE element {index} is unsupported.");
        }
    }

    internal static PipelineStage CreateCurveStage(IccProcessingElement element, int index)
    {
        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Curves;
        stage.InputChannels = element.InputChannels;
        stage.OutputChannels = element.OutputChannels;
        stage.Curves = ExtendedTagDataClone.CloneCurveArray(element.Curves, $"MPE element {index} curves", new ProfileCloneContext());
        stage.Description = $"MPE curve-set element {index}";
        return stage;
    }

    internal static PipelineStage CreateMatrixStage(IccProcessingElement element, int index)
    {
        IccMatrix? source = element.Matrix;
        if (source == null)
        {
            throw new IccTransformException($"MPE matrix element {index} is missing its matrix.");
        }

        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Matrix;
        stage.InputChannels = element.InputChannels;
        stage.OutputChannels = element.OutputChannels;
        stage.Matrix = ExtendedTagDataClone.CloneMatrix(source);
        stage.Description = $"MPE matrix element {index}";
        return stage;
    }

    internal static PipelineStage CreateClutStage(IccProcessingElement element, int index, IccInterpolationKind interpolation)
    {
        IccClut? source = element.Clut;
        if (source == null)
        {
            throw new IccTransformException($"MPE CLUT element {index} is missing its table.");
        }

        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Clut;
        stage.InputChannels = element.InputChannels;
        stage.OutputChannels = element.OutputChannels;
        stage.Clut = ExtendedTagDataClone.CloneClut(source);
        stage.Interpolation = interpolation;
        stage.Description = $"MPE CLUT element {index}";
        return stage;
    }

    internal static PipelineStage CreateAcsStage(IccProcessingElement element, int index)
    {
        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Identity;
        stage.InputChannels = element.InputChannels;
        stage.OutputChannels = element.OutputChannels;
        stage.Description = element.Kind == IccProcessingElementKind.BeginAcs ? $"MPE begin ACS element {index}" : $"MPE end ACS element {index}";
        return stage;
    }

    internal static void ValidateProfileChannels(IccProfile profile, IccMultiProcessElements pipeline, uint tagSignature, IccTransformDirection direction)
    {
        int deviceChannels = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        int pcsChannels = ColorSpaceChannelCount.Decode(profile.Header.ProfileConnectionSpaceSignature);
        if (profile.Header.Version.Kind == IccVersionKind.Version5)
        {
            ValidateVersionFiveChannels(profile, pipeline, tagSignature);
            return;
        }
        if (deviceChannels < 1 || pcsChannels != 3)
        {
            throw new IccTransformException("A v4 MPE transform requires known device channels and a three-channel PCSXYZ or PCSLAB connection space.");
        }

        int expectedInput = direction == IccTransformDirection.DeviceToPcs ? deviceChannels : pcsChannels;
        int expectedOutput = direction == IccTransformDirection.DeviceToPcs ? pcsChannels : deviceChannels;
        if (pipeline.InputChannels != expectedInput || pipeline.OutputChannels != expectedOutput)
        {
            throw new IccTransformException($"The v4 MPE declares {pipeline.InputChannels}-to-{pipeline.OutputChannels} channels, but the profile direction requires {expectedInput}-to-{expectedOutput}.");
        }
    }

    internal static void ValidateRanges(IccEncodingRange[] ranges, string description)
    {
        int index = 0;
        while (index < ranges.Length)
        {
            IccEncodingRange? range = ranges[index];
            if (range == null || !System.Double.IsFinite(range.Minimum) || !System.Double.IsFinite(range.Maximum) || range.Minimum > range.Maximum)
            {
                throw new IccTransformException($"The {description} item {index} must have finite ordered endpoints.");
            }

            index = index + 1;
        }
    }

    internal static IccEncodingRange[] CloneRanges(IccEncodingRange[] source, string description)
    {
        ValidateRanges(source, description);
        IccEncodingRange[] clone = new IccEncodingRange[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            IccEncodingRange range = new IccEncodingRange();
            range.Minimum = source[index].Minimum;
            range.Maximum = source[index].Maximum;
            clone[index] = range;
            index = index + 1;
        }

        return clone;
    }

    internal static void ValidateFiniteValues(double[] values, string description)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!System.Double.IsFinite(values[index]))
            {
                throw new IccTransformException($"The {description} value {index} must be finite.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateRequest(IccProfile profile, IccTag tag, IccTransformDirection direction, IccInterpolationKind interpolation)
    {
        if (profile == null || profile.Header == null || tag == null || tag.Data == null)
        {
            throw new IccTransformException("A profile, header, and decoded MPE tag are required.");
        }
        if (profile.Header.Version.Kind != IccVersionKind.Version4 && profile.Header.Version.Kind != IccVersionKind.Version5)
        {
            throw new IccTransformException("This compiler accepts ICC version 4 and ICC.2 multi-process-elements pipelines.");
        }
        if (tag.Data.RawData != null && tag.Data.RawData.HasTypeSignature && tag.Data.RawData.TypeSignature != IccTypeSignatures.MultiProcessElements)
        {
            throw new IccTransformException("The selected tag is not a multiProcessElementsType payload.");
        }
        if (direction != IccTransformDirection.DeviceToPcs && direction != IccTransformDirection.PcsToDevice && direction != IccTransformDirection.DeviceToDevice)
        {
            throw new IccTransformException("A v4 MPE plan supports only device-to-PCS and PCS-to-device directions.");
        }
        if (interpolation != IccInterpolationKind.Linear && interpolation != IccInterpolationKind.Multilinear && interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccTransformException("The requested MPE interpolation algorithm is invalid.");
        }

        bool forward = MultiProcessElementsTagCodec.IsIntentTag(tag.Signature, IccTagSignatures.DToB0);
        bool reverse = MultiProcessElementsTagCodec.IsIntentTag(tag.Signature, IccTagSignatures.BToD0);
        if (profile.Header.Version.Kind == IccVersionKind.Version4 && ((!forward && !reverse) || (forward && direction != IccTransformDirection.DeviceToPcs) || (reverse && direction != IccTransformDirection.PcsToDevice)))
        {
            throw new IccTransformException("The selected v4 MPE tag signature does not match the requested transform direction.");
        }
        if (profile.Header.Version.Kind == IccVersionKind.Version5)
        {
            ValidateVersionFiveRequest(tag.Signature, direction);
        }
    }

    internal static bool IsVersionFourElement(IccProcessingElementKind kind)
    {
        return kind == IccProcessingElementKind.CurveSet || kind == IccProcessingElementKind.Matrix || kind == IccProcessingElementKind.Clut
            || kind == IccProcessingElementKind.BeginAcs || kind == IccProcessingElementKind.EndAcs;
    }

    internal static void ValidateVersionFiveRequest(uint signature, IccTransformDirection direction)
    {
        bool forward = MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.AToB0)
            || MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.DToB0)
            || MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.MToB0)
            || MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.MToS0)
            || signature == IccTagSignatures.AToM0;
        bool reverse = MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.BToA0)
            || MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.BToD0)
            || signature == IccTagSignatures.MToA0;
        bool connection = MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.HdrToSdr0)
            || signature == IccTagSignatures.CustomToStandardPcc || signature == IccTagSignatures.StandardToCustomPcc;
        bool angularForward = MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfAToB0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfDToB0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfMToB0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfMToS0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalAToB0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalDToB0);
        bool angularReverse = MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfBToA0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfBToD0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalBToA0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalBToD0);
        if ((!forward && !reverse && !connection && !angularForward && !angularReverse) || (forward && direction != IccTransformDirection.DeviceToPcs)
            || (reverse && direction != IccTransformDirection.PcsToDevice) || (connection && direction != IccTransformDirection.DeviceToDevice))
        {
            throw new IccTransformException("The selected ICC.2 MPE tag signature does not match the requested transform direction.");
        }
        if ((angularForward && direction != IccTransformDirection.DeviceToPcs) || (angularReverse && direction != IccTransformDirection.PcsToDevice))
        {
            throw new IccTransformException("The selected ICC.2 angular tag signature does not match the requested transform direction.");
        }
    }

    internal static void ValidateVersionFiveChannels(IccProfile profile, IccMultiProcessElements pipeline, uint signature)
    {
        int dataChannels = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        int pcsChannels = ColorSpaceChannelCount.Decode(profile.Header.ProfileConnectionSpaceSignature);
        int spectralChannels = (int)(profile.Header.SpectralPcsSignature & 0x0000ffff);
        int materialChannels = (int)(profile.Header.MaterialConnectionSpaceSignature & 0x0000ffff);
        int expectedInput = 0;
        int expectedOutput = 0;
        if (MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.AToB0))
        {
            expectedInput = dataChannels;
            expectedOutput = pcsChannels;
        }
        else if (MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.BToA0))
        {
            expectedInput = pcsChannels;
            expectedOutput = dataChannels;
        }
        else if (MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.DToB0))
        {
            expectedInput = dataChannels;
            expectedOutput = spectralChannels;
        }
        else if (MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.BToD0))
        {
            expectedInput = spectralChannels;
            expectedOutput = dataChannels;
        }
        else if (signature == IccTagSignatures.AToM0)
        {
            expectedInput = dataChannels;
            expectedOutput = materialChannels;
        }
        else if (signature == IccTagSignatures.MToA0)
        {
            expectedInput = materialChannels;
            expectedOutput = dataChannels;
        }
        else if (MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.MToB0))
        {
            expectedInput = materialChannels;
            expectedOutput = pcsChannels;
        }
        else if (MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.MToS0))
        {
            expectedInput = materialChannels;
            expectedOutput = spectralChannels;
        }
        else if (MultiProcessElementsTagCodec.IsIntentTag(signature, IccTagSignatures.HdrToSdr0))
        {
            expectedInput = pcsChannels;
            expectedOutput = pcsChannels;
        }
        else if (signature == IccTagSignatures.CustomToStandardPcc || signature == IccTagSignatures.StandardToCustomPcc)
        {
            expectedInput = 3;
            expectedOutput = 3;
        }
        else if (MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfAToB0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalAToB0))
        {
            expectedInput = MultiProcessElementsTagCodec.AddGeometryChannels(dataChannels);
            expectedOutput = pcsChannels;
        }
        else if (MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfBToA0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalBToA0))
        {
            expectedInput = MultiProcessElementsTagCodec.AddGeometryChannels(pcsChannels);
            expectedOutput = dataChannels;
        }
        else if (MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfDToB0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalDToB0))
        {
            expectedInput = MultiProcessElementsTagCodec.AddGeometryChannels(dataChannels);
            expectedOutput = spectralChannels;
        }
        else if (MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfBToD0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalBToD0))
        {
            expectedInput = MultiProcessElementsTagCodec.AddGeometryChannels(spectralChannels);
            expectedOutput = dataChannels;
        }
        else if (MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfMToB0))
        {
            expectedInput = MultiProcessElementsTagCodec.AddGeometryChannels(materialChannels);
            expectedOutput = pcsChannels;
        }
        else if (MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfMToS0))
        {
            expectedInput = MultiProcessElementsTagCodec.AddGeometryChannels(materialChannels);
            expectedOutput = spectralChannels;
        }
        if (expectedInput < 1 || expectedOutput < 1 || pipeline.InputChannels != expectedInput || pipeline.OutputChannels != expectedOutput)
        {
            throw new IccTransformException($"The ICC.2 MPE declares {pipeline.InputChannels}-to-{pipeline.OutputChannels} channels, but its tag and profile context require {expectedInput}-to-{expectedOutput}.");
        }
    }

}
