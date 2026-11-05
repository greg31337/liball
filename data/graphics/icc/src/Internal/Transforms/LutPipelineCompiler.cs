namespace icc;

internal class LutPipelinePlan
{
    internal uint TagSignature;
    internal uint TypeSignature;
    internal IccTransformDirection Direction;
    internal IccPcsKind PcsKind;
    internal IccPcsEncodingKind PcsEncoding;
    internal int SourceChannels;
    internal int DestinationChannels;
    internal IccEncodingRange[] SourceRanges;
    internal IccEncodingRange[] DestinationRanges;
    internal PipelineStage[] Stages;

    internal LutPipelinePlan()
    {
        TagSignature = 0;
        TypeSignature = 0;
        Direction = IccTransformDirection.DeviceToPcs;
        PcsKind = IccPcsKind.None;
        PcsEncoding = IccPcsEncodingKind.Normalized;
        SourceChannels = 0;
        DestinationChannels = 0;
        SourceRanges = new IccEncodingRange[0];
        DestinationRanges = new IccEncodingRange[0];
        Stages = new PipelineStage[0];
    }
}

internal static class LutPipelineCompiler
{
    internal static LutPipelinePlan Compile(IccProfile profile, IccTag tag, IccTransformDirection direction, IccInterpolationKind interpolation)
    {
        ValidateRequest(profile, tag, direction, interpolation);
        ValidateTagDirection(tag.Signature, direction);
        uint typeSignature = ResolveTypeSignature(tag, direction);
        LutPipelinePlan plan;
        if (typeSignature == IccTypeSignatures.Lut8 || typeSignature == IccTypeSignatures.Lut16)
        {
            if (tag.Data.Lut == null || tag.Data.MultiStageLut != null)
            {
                throw new IccTransformException("A classic LUT tag must contain one decoded LUT8 or LUT16 representation.");
            }

            plan = CompileClassic(profile, tag, tag.Data.Lut, direction, interpolation, typeSignature);
        }
        else
        {
            if (tag.Data.MultiStageLut == null || tag.Data.Lut != null)
            {
                throw new IccTransformException("A multi-stage LUT tag must contain one decoded lutAToB or lutBToA representation.");
            }

            plan = CompileMultiStage(profile, tag, tag.Data.MultiStageLut, direction, interpolation, typeSignature);
        }

        PipelineExecutor.GetRequiredValueScratchLength(plan.Stages);
        return plan;
    }

    internal static LutPipelinePlan CompileDeviceLink(IccProfile profile, IccTag tag, IccInterpolationKind interpolation)
    {
        if (profile == null || profile.Header == null || tag == null || tag.Data == null
            || profile.Header.ProfileClass != IccProfileClass.DeviceLink
            || (profile.Header.Version.Kind != IccVersionKind.Version2 && profile.Header.Version.Kind != IccVersionKind.Version4))
        {
            throw new IccTransformException("A direct AToB0 pipeline requires an ICC version 2 or version 4 DeviceLink profile and decoded tag.");
        }
        if (tag.Signature != IccTagSignatures.AToB0)
        {
            throw new IccTransformException("A direct DeviceLink LUT pipeline requires the AToB0 tag.");
        }
        if (interpolation != IccInterpolationKind.Linear && interpolation != IccInterpolationKind.Multilinear
            && interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccTransformException("The requested DeviceLink LUT interpolation algorithm is invalid.");
        }

        uint typeSignature = ResolveTypeSignature(tag, IccTransformDirection.DeviceToPcs);
        PipelineStage[] encodedStages = new PipelineStage[5];
        int count = 0;
        int inputChannels;
        int outputChannels;
        IccPcsEncodingKind encoding;
        if (typeSignature == IccTypeSignatures.Lut8 || typeSignature == IccTypeSignatures.Lut16)
        {
            IccLut? lut = tag.Data.Lut;
            if (lut == null || tag.Data.MultiStageLut != null)
            {
                throw new IccTransformException("A DeviceLink classic LUT tag must contain one decoded LUT8 or LUT16 representation.");
            }
            ValidateClassicLut(lut, typeSignature);
            inputChannels = lut.InputChannels;
            outputChannels = lut.OutputChannels;
            encoding = lut.PcsEncoding;
            if (profile.Header.DataColorSpaceSignature == IccColorSpaceSignatures.Xyz)
            {
                if (lut.InputChannels != 3)
                {
                    throw new IccTransformException("A DeviceLink classic LUT matrix can be applied only to three XYZ input channels.");
                }
                encodedStages[count] = CreateMatrixStage(lut.Matrix, "DeviceLink classic LUT input matrix");
                count = count + 1;
            }
            encodedStages[count] = CreateTableCurveStage(lut.InputTables, "DeviceLink classic LUT input tables");
            count = count + 1;
            encodedStages[count] = CreateClutStage(lut.Clut, interpolation, "DeviceLink classic LUT CLUT");
            count = count + 1;
            encodedStages[count] = CreateTableCurveStage(lut.OutputTables, "DeviceLink classic LUT output tables");
            count = count + 1;
        }
        else
        {
            IccMultiStageLut? lut = tag.Data.MultiStageLut;
            if (typeSignature != IccTypeSignatures.LutAToB || lut == null || tag.Data.Lut != null)
            {
                throw new IccTransformException("A DeviceLink multi-stage AToB0 tag must contain one decoded lutAToB representation.");
            }
            ValidateMultiStageLut(lut, true);
            inputChannels = lut.InputChannels;
            outputChannels = lut.OutputChannels;
            encoding = lut.PcsEncoding;
            if (lut.ACurves.Length > 0)
            {
                encodedStages[count] = CreateCurveStage(lut.ACurves, "DeviceLink lutAToB A curves");
                count = count + 1;
                encodedStages[count] = CreateClutStage(lut.Clut, interpolation, "DeviceLink lutAToB CLUT");
                count = count + 1;
            }
            if (lut.MCurves.Length > 0)
            {
                encodedStages[count] = CreateCurveStage(lut.MCurves, "DeviceLink lutAToB M curves");
                count = count + 1;
                encodedStages[count] = CreateMatrixStage(lut.Matrix, "DeviceLink lutAToB matrix and offsets");
                count = count + 1;
            }
            encodedStages[count] = CreateCurveStage(lut.BCurves, "DeviceLink lutAToB B curves");
            count = count + 1;
        }

        int expectedInput = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        int expectedOutput = ColorSpaceChannelCount.Decode(profile.Header.ProfileConnectionSpaceSignature);
        if (expectedInput < 1 || expectedOutput < 1 || inputChannels != expectedInput || outputChannels != expectedOutput)
        {
            throw new IccTransformException($"The DeviceLink AToB0 pipeline declares {inputChannels}-to-{outputChannels} channels, but its header colour spaces require {expectedInput}-to-{expectedOutput}.");
        }
        return CreateDirectDeviceLinkPlan(tag.Signature, typeSignature, encoding, encodedStages, count,
            inputChannels, outputChannels);
    }

    internal static LutPipelinePlan CreateDirectDeviceLinkPlan(uint tagSignature, uint typeSignature,
        IccPcsEncodingKind encoding, PipelineStage[] encodedStages, int encodedStageCount, int inputChannels,
        int outputChannels)
    {
        ValidatePcsEncoding(encoding);
        PipelineStage[] stages = new PipelineStage[encodedStageCount];
        int index = 0;
        while (index < encodedStageCount)
        {
            stages[index] = encodedStages[index];
            index = index + 1;
        }
        PipelineExecutor.GetRequiredValueScratchLength(stages);
        LutPipelinePlan plan = new LutPipelinePlan();
        plan.TagSignature = tagSignature;
        plan.TypeSignature = typeSignature;
        plan.Direction = IccTransformDirection.DeviceToDevice;
        plan.PcsKind = IccPcsKind.None;
        plan.PcsEncoding = encoding;
        plan.SourceChannels = inputChannels;
        plan.DestinationChannels = outputChannels;
        plan.SourceRanges = CreateNormalizedRanges(inputChannels);
        plan.DestinationRanges = CreateNormalizedRanges(outputChannels);
        plan.Stages = stages;
        return plan;
    }

    internal static LutPipelinePlan CompileClassic(IccProfile profile, IccTag tag, IccLut lut, IccTransformDirection direction, IccInterpolationKind interpolation, uint typeSignature)
    {
        ValidateClassicLut(lut, typeSignature);
        ValidateChannelCounts(profile, direction, lut.InputChannels, lut.OutputChannels);
        PipelineStage[] encodedStages = new PipelineStage[4];
        int count = 0;
        if (ClassicMatrixApplies(profile, direction))
        {
            if (lut.InputChannels != 3)
            {
                throw new IccTransformException("A classic LUT matrix can be applied only to three input channels.");
            }

            encodedStages[count] = CreateMatrixStage(lut.Matrix, "classic LUT input matrix");
            count = count + 1;
        }

        encodedStages[count] = CreateTableCurveStage(lut.InputTables, "classic LUT input tables");
        count = count + 1;
        encodedStages[count] = CreateClutStage(lut.Clut, interpolation, "classic LUT CLUT");
        count = count + 1;
        encodedStages[count] = CreateTableCurveStage(lut.OutputTables, "classic LUT output tables");
        count = count + 1;
        return CreatePlan(profile, tag.Signature, typeSignature, direction, lut.PcsEncoding, encodedStages, count, lut.InputChannels, lut.OutputChannels);
    }

    internal static LutPipelinePlan CompileMultiStage(IccProfile profile, IccTag tag, IccMultiStageLut lut, IccTransformDirection direction, IccInterpolationKind interpolation, uint typeSignature)
    {
        bool aToB = typeSignature == IccTypeSignatures.LutAToB;
        if ((aToB && direction != IccTransformDirection.DeviceToPcs) || (!aToB && direction != IccTransformDirection.PcsToDevice))
        {
            throw new IccTransformException("The multi-stage LUT type does not match the requested transform direction.");
        }

        ValidateMultiStageLut(lut, aToB);
        ValidateChannelCounts(profile, direction, lut.InputChannels, lut.OutputChannels);
        PipelineStage[] encodedStages = new PipelineStage[5];
        int count = 0;
        if (aToB)
        {
            if (lut.ACurves.Length > 0)
            {
                encodedStages[count] = CreateCurveStage(lut.ACurves, "lutAToB A curves");
                count = count + 1;
                encodedStages[count] = CreateClutStage(lut.Clut, interpolation, "lutAToB CLUT");
                count = count + 1;
            }
            if (lut.MCurves.Length > 0)
            {
                encodedStages[count] = CreateCurveStage(lut.MCurves, "lutAToB M curves");
                count = count + 1;
                encodedStages[count] = CreateMatrixStage(lut.Matrix, "lutAToB matrix and offsets");
                count = count + 1;
            }

            encodedStages[count] = CreateCurveStage(lut.BCurves, "lutAToB B curves");
            count = count + 1;
        }
        else
        {
            encodedStages[count] = CreateCurveStage(lut.BCurves, "lutBToA B curves");
            count = count + 1;
            if (lut.MCurves.Length > 0)
            {
                encodedStages[count] = CreateMatrixStage(lut.Matrix, "lutBToA matrix and offsets");
                count = count + 1;
                encodedStages[count] = CreateCurveStage(lut.MCurves, "lutBToA M curves");
                count = count + 1;
            }
            if (lut.ACurves.Length > 0)
            {
                encodedStages[count] = CreateClutStage(lut.Clut, interpolation, "lutBToA CLUT");
                count = count + 1;
                encodedStages[count] = CreateCurveStage(lut.ACurves, "lutBToA A curves");
                count = count + 1;
            }
        }

        return CreatePlan(profile, tag.Signature, typeSignature, direction, lut.PcsEncoding, encodedStages, count, lut.InputChannels, lut.OutputChannels);
    }

    internal static LutPipelinePlan CreatePlan(IccProfile profile, uint tagSignature, uint typeSignature, IccTransformDirection direction, IccPcsEncodingKind encoding, PipelineStage[] encodedStages, int encodedStageCount, int inputChannels, int outputChannels)
    {
        ValidatePcsEncoding(encoding);
        bool startsWithPcs = direction == IccTransformDirection.PcsToDevice;
        bool endsWithPcs = direction == IccTransformDirection.DeviceToPcs;
        int boundaryCount = (startsWithPcs ? 1 : 0) + (endsWithPcs ? 1 : 0);
        PipelineStage[] stages = new PipelineStage[encodedStageCount + boundaryCount];
        int target = 0;
        if (startsWithPcs)
        {
            stages[target] = CreatePcsStage(PipelineStageKind.PcsNormalize, profile.Header.ProfileConnectionSpace, encoding, "normalize source PCS values");
            target = target + 1;
        }

        int source = 0;
        while (source < encodedStageCount)
        {
            stages[target] = encodedStages[source];
            target = target + 1;
            source = source + 1;
        }
        if (endsWithPcs)
        {
            stages[target] = CreatePcsStage(PipelineStageKind.PcsDenormalize, profile.Header.ProfileConnectionSpace, encoding, "denormalize destination PCS values");
        }

        LutPipelinePlan plan = new LutPipelinePlan();
        plan.TagSignature = tagSignature;
        plan.TypeSignature = typeSignature;
        plan.Direction = direction;
        plan.PcsKind = profile.Header.ProfileConnectionSpace;
        plan.PcsEncoding = encoding;
        plan.SourceChannels = inputChannels;
        plan.DestinationChannels = outputChannels;
        plan.SourceRanges = direction == IccTransformDirection.PcsToDevice ? CreatePcsRanges(plan.PcsKind, encoding) : CreateNormalizedRanges(inputChannels);
        plan.DestinationRanges = direction == IccTransformDirection.DeviceToPcs ? CreatePcsRanges(plan.PcsKind, encoding) : CreateNormalizedRanges(outputChannels);
        plan.Stages = stages;
        return plan;
    }

    internal static PipelineStage CreateTableCurveStage(double[][] tables, string description)
    {
        IccCurve[] curves = new IccCurve[tables.Length];
        int index = 0;
        while (index < tables.Length)
        {
            ValidateNormalizedValues(tables[index], 2, description);
            IccCurve curve = new IccCurve();
            curve.Kind = IccCurveKind.Sampled;
            curve.Samples = CloneDoubleArray(tables[index]);
            curves[index] = curve;
            index = index + 1;
        }

        return CreateCurveStage(curves, description);
    }

    internal static PipelineStage CreateCurveStage(IccCurve[] source, string description)
    {
        if (source == null || source.Length == 0)
        {
            throw new IccTransformException($"The {description} must contain at least one curve.");
        }

        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == null)
            {
                throw new IccTransformException($"The {description} curve {index} cannot be null.");
            }

            MatrixTrcCompiler.ValidateCurve(source[index], $"{description} curve {index}", false);
            index = index + 1;
        }

        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Curves;
        stage.InputChannels = source.Length;
        stage.OutputChannels = source.Length;
        stage.Curves = ExtendedTagDataClone.CloneCurveArray(source, description, new ProfileCloneContext());
        stage.Description = description;
        return stage;
    }

    internal static PipelineStage CreateMatrixStage(IccMatrix? source, string description)
    {
        if (source == null || source.Values == null || source.Rows != 3 || (source.Columns != 3 && source.Columns != 4) || source.Values.Length != source.Rows * source.Columns)
        {
            throw new IccTransformException($"The {description} must be a three-row matrix with three coefficients and an optional offset per row.");
        }

        ValidateFiniteValues(source.Values, description);
        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Matrix;
        stage.InputChannels = 3;
        stage.OutputChannels = 3;
        stage.Matrix = new IccMatrix();
        stage.Matrix.Rows = source.Rows;
        stage.Matrix.Columns = source.Columns;
        stage.Matrix.Values = CloneDoubleArray(source.Values);
        stage.Description = description;
        return stage;
    }

    internal static PipelineStage CreateClutStage(IccClut? source, IccInterpolationKind interpolation, string description)
    {
        if (source == null)
        {
            throw new IccTransformException($"The {description} is required.");
        }
        if (interpolation == IccInterpolationKind.Linear && source.InputChannels != 1)
        {
            throw new IccTransformException("Linear CLUT interpolation requires a one-dimensional LUT.");
        }

        ValidateClut(source, description);
        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Clut;
        stage.InputChannels = source.InputChannels;
        stage.OutputChannels = source.OutputChannels;
        stage.Clut = ExtendedTagDataClone.CloneClut(source);
        stage.Interpolation = interpolation;
        stage.Description = description;
        return stage;
    }

    internal static PipelineStage CreatePcsStage(PipelineStageKind kind, IccPcsKind pcs, IccPcsEncodingKind encoding, string description)
    {
        PipelineStage stage = new PipelineStage();
        stage.Kind = kind;
        stage.InputChannels = 3;
        stage.OutputChannels = 3;
        stage.PcsKind = pcs;
        stage.PcsEncoding = encoding;
        stage.Description = description;
        return stage;
    }

    internal static void ValidateClassicLut(IccLut lut, uint typeSignature)
    {
        if (lut == null || lut.Matrix == null || lut.InputTables == null || lut.OutputTables == null || lut.Clut == null)
        {
            throw new IccTransformException("A classic LUT and all of its required stages must be present.");
        }
        if (lut.InputChannels < 1 || lut.InputChannels > IccConstants.MaximumIcc1ChannelCount || lut.OutputChannels < 1 || lut.OutputChannels > IccConstants.MaximumIcc1ChannelCount)
        {
            throw new IccTransformException("A classic LUT channel count is outside the ICC.1 range.");
        }
        if (lut.InputTables.Length != lut.InputChannels || lut.OutputTables.Length != lut.OutputChannels)
        {
            throw new IccTransformException("A classic LUT requires one input and output table per corresponding channel.");
        }
        if (lut.Matrix.Values == null || lut.Matrix.Rows != 3 || lut.Matrix.Columns != 3 || lut.Matrix.Values.Length != 9)
        {
            throw new IccTransformException("A classic LUT requires a three-by-three matrix.");
        }
        if ((typeSignature == IccTypeSignatures.Lut8 && lut.PcsEncoding != IccPcsEncodingKind.Normalized)
            || (typeSignature == IccTypeSignatures.Lut16 && lut.PcsEncoding != IccPcsEncodingKind.Version2))
        {
            throw new IccTransformException("The classic LUT type and its decoded PCS encoding do not agree.");
        }

        ValidateFiniteValues(lut.Matrix.Values, "classic LUT matrix");
        ValidateClut(lut.Clut, "classic LUT CLUT");
        if (lut.Clut.InputChannels != lut.InputChannels || lut.Clut.OutputChannels != lut.OutputChannels)
        {
            throw new IccTransformException("The classic LUT CLUT channel counts must match the LUT channel counts.");
        }

        int inputEntryCount = lut.InputTables.Length == 0 || lut.InputTables[0] == null ? 0 : lut.InputTables[0].Length;
        int outputEntryCount = lut.OutputTables.Length == 0 || lut.OutputTables[0] == null ? 0 : lut.OutputTables[0].Length;
        if ((typeSignature == IccTypeSignatures.Lut8 && (inputEntryCount != 256 || outputEntryCount != 256))
            || (typeSignature == IccTypeSignatures.Lut16 && (inputEntryCount < 2 || inputEntryCount > 4096 || outputEntryCount < 2 || outputEntryCount > 4096)))
        {
            throw new IccTransformException("The classic LUT table lengths do not match the LUT8 or LUT16 type.");
        }

        int grid = lut.Clut.GridPoints[0];
        int gridIndex = 1;
        while (gridIndex < lut.Clut.GridPoints.Length)
        {
            if (lut.Clut.GridPoints[gridIndex] != grid)
            {
                throw new IccTransformException("A classic LUT requires the same grid-point count in every input dimension.");
            }

            gridIndex = gridIndex + 1;
        }

        int index = 0;
        while (index < lut.InputTables.Length)
        {
            ValidateNormalizedValues(lut.InputTables[index], 2, "classic LUT input table");
            if (lut.InputTables[index].Length != inputEntryCount)
            {
                throw new IccTransformException("All classic LUT input tables must have the same length.");
            }

            index = index + 1;
        }
        index = 0;
        while (index < lut.OutputTables.Length)
        {
            ValidateNormalizedValues(lut.OutputTables[index], 2, "classic LUT output table");
            if (lut.OutputTables[index].Length != outputEntryCount)
            {
                throw new IccTransformException("All classic LUT output tables must have the same length.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateMultiStageLut(IccMultiStageLut lut, bool aToB)
    {
        if (lut == null || lut.ACurves == null || lut.BCurves == null || lut.MCurves == null)
        {
            throw new IccTransformException("A multi-stage LUT and all curve arrays must be present.");
        }
        if (lut.PcsEncoding != IccPcsEncodingKind.Version4)
        {
            throw new IccTransformException("A lutAToB or lutBToA plan requires ICC version 4 PCS encoding.");
        }
        if (lut.InputChannels < 1 || lut.InputChannels > IccConstants.MaximumIcc1ChannelCount || lut.OutputChannels < 1 || lut.OutputChannels > IccConstants.MaximumIcc1ChannelCount)
        {
            throw new IccTransformException("A multi-stage LUT channel count is outside the ICC.1 range.");
        }

        int bCount = aToB ? lut.OutputChannels : lut.InputChannels;
        int mCount = bCount;
        int aCount = aToB ? lut.InputChannels : lut.OutputChannels;
        bool hasMatrix = lut.Matrix != null;
        bool hasM = lut.MCurves.Length > 0;
        bool hasClut = lut.Clut != null;
        bool hasA = lut.ACurves.Length > 0;
        if (lut.BCurves.Length != bCount || hasMatrix != hasM || hasClut != hasA || (hasM && lut.MCurves.Length != mCount) || (hasA && lut.ACurves.Length != aCount))
        {
            throw new IccTransformException("The multi-stage LUT curve counts and optional stage pairs are inconsistent.");
        }
        if (!hasClut && lut.InputChannels != lut.OutputChannels)
        {
            throw new IccTransformException("A multi-stage LUT requires a CLUT when its channel counts differ.");
        }
        if (hasMatrix && ((aToB && lut.OutputChannels != 3) || (!aToB && lut.InputChannels != 3)))
        {
            throw new IccTransformException("A multi-stage LUT matrix requires three channels at its transition.");
        }
        if (hasMatrix)
        {
            CreateMatrixStage(lut.Matrix, "multi-stage LUT matrix");
        }
        if (hasClut)
        {
            IccClut? clut = lut.Clut;
            if (clut == null)
            {
                throw new IccTransformException("The multi-stage LUT CLUT is missing.");
            }

            ValidateClut(clut, "multi-stage LUT CLUT");
            if (clut.InputChannels != lut.InputChannels || clut.OutputChannels != lut.OutputChannels)
            {
                throw new IccTransformException("The multi-stage LUT CLUT channel counts must match the LUT channel counts.");
            }
        }
    }

    internal static void ValidateClut(IccClut clut, string description)
    {
        if (clut.GridPoints == null || clut.Values == null || clut.InputChannels < 1 || clut.InputChannels > 15 || clut.OutputChannels < 1 || clut.GridPoints.Length != clut.InputChannels)
        {
            throw new IccTransformException($"The {description} dimensions are invalid.");
        }

        int count = clut.OutputChannels;
        int index = 0;
        while (index < clut.InputChannels)
        {
            int grid = clut.GridPoints[index];
            if (grid < 2 || grid > 255 || count > System.Int32.MaxValue / grid)
            {
                throw new IccTransformException($"The {description} grid is invalid or exceeds the supported size.");
            }

            count = count * grid;
            index = index + 1;
        }
        if (clut.Values.Length != count)
        {
            throw new IccTransformException($"The {description} does not contain its required values.");
        }

        ValidateNormalizedValues(clut.Values, count, description);
    }

    internal static void ValidateChannelCounts(IccProfile profile, IccTransformDirection direction, int inputChannels, int outputChannels)
    {
        int deviceChannels = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        if (deviceChannels < 1)
        {
            throw new IccTransformException("The profile data colour space has no supported channel count.");
        }

        int expectedInput = direction == IccTransformDirection.DeviceToPcs ? deviceChannels : 3;
        int expectedOutput = direction == IccTransformDirection.DeviceToPcs ? 3 : deviceChannels;
        if (inputChannels != expectedInput || outputChannels != expectedOutput)
        {
            throw new IccTransformException($"The LUT declares {inputChannels}-to-{outputChannels} channels, but the profile direction requires {expectedInput}-to-{expectedOutput}.");
        }
    }

    internal static bool ClassicMatrixApplies(IccProfile profile, IccTransformDirection direction)
    {
        if (direction == IccTransformDirection.PcsToDevice)
        {
            return profile.Header.ProfileConnectionSpace == IccPcsKind.Xyz;
        }

        return profile.Header.DataColorSpaceSignature == IccColorSpaceSignatures.Xyz;
    }

    internal static void ValidateTagDirection(uint signature, IccTransformDirection direction)
    {
        bool forward = signature == IccTagSignatures.AToB0 || signature == IccTagSignatures.AToB1 || signature == IccTagSignatures.AToB2;
        bool reverse = signature == IccTagSignatures.BToA0 || signature == IccTagSignatures.BToA1 || signature == IccTagSignatures.BToA2;
        if ((forward && direction != IccTransformDirection.DeviceToPcs) || (reverse && direction != IccTransformDirection.PcsToDevice))
        {
            throw new IccTransformException("The selected LUT tag signature does not match the requested transform direction.");
        }
    }

    internal static uint ResolveTypeSignature(IccTag tag, IccTransformDirection direction)
    {
        if (tag.Data.RawData != null && tag.Data.RawData.HasTypeSignature)
        {
            uint signature = tag.Data.RawData.TypeSignature;
            if (signature != IccTypeSignatures.Lut8 && signature != IccTypeSignatures.Lut16 && signature != IccTypeSignatures.LutAToB && signature != IccTypeSignatures.LutBToA)
            {
                throw new IccTransformException("The selected tag does not contain a supported ICC.1 LUT type.");
            }

            return signature;
        }
        if (tag.Data.Lut != null && tag.Data.MultiStageLut == null)
        {
            return tag.Data.Lut.PcsEncoding == IccPcsEncodingKind.Version2 ? IccTypeSignatures.Lut16 : IccTypeSignatures.Lut8;
        }
        if (tag.Data.MultiStageLut != null && tag.Data.Lut == null)
        {
            return direction == IccTransformDirection.DeviceToPcs ? IccTypeSignatures.LutAToB : IccTypeSignatures.LutBToA;
        }

        throw new IccTransformException("The selected tag does not have one unambiguous decoded LUT representation.");
    }

    internal static void ValidateRequest(IccProfile profile, IccTag tag, IccTransformDirection direction, IccInterpolationKind interpolation)
    {
        if (profile == null || profile.Header == null || tag == null || tag.Data == null)
        {
            throw new IccTransformException("A profile, header, and decoded LUT tag are required.");
        }
        if (direction != IccTransformDirection.DeviceToPcs && direction != IccTransformDirection.PcsToDevice)
        {
            throw new IccTransformException("An ICC.1 LUT plan supports only device-to-PCS and PCS-to-device directions.");
        }
        if (interpolation != IccInterpolationKind.Linear && interpolation != IccInterpolationKind.Multilinear && interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccTransformException("The requested LUT interpolation algorithm is invalid.");
        }

        IccPcsKind pcs = profile.Header.ProfileConnectionSpace;
        uint pcsSignature = profile.Header.ProfileConnectionSpaceSignature;
        bool validKind = pcs == IccPcsKind.Xyz || pcs == IccPcsKind.Lab;
        bool validSignature = pcsSignature == 0 || (pcs == IccPcsKind.Xyz && pcsSignature == IccColorSpaceSignatures.Xyz) || (pcs == IccPcsKind.Lab && pcsSignature == IccColorSpaceSignatures.Lab);
        if (!validKind || !validSignature)
        {
            throw new IccTransformException("An ICC.1 LUT plan requires a consistent PCSXYZ or PCSLAB profile connection space.");
        }
    }

    internal static void ValidatePcsEncoding(IccPcsEncodingKind encoding)
    {
        if (encoding != IccPcsEncodingKind.Normalized && encoding != IccPcsEncodingKind.Version2 && encoding != IccPcsEncodingKind.Version4)
        {
            throw new IccTransformException("The LUT declares an unsupported ICC.1 PCS encoding.");
        }
    }

    internal static void ValidateNormalizedValues(double[] values, int minimumCount, string description)
    {
        if (values == null || values.Length < minimumCount)
        {
            throw new IccTransformException($"The {description} contains too few values.");
        }

        int index = 0;
        while (index < values.Length)
        {
            double value = values[index];
            if (!System.Double.IsFinite(value) || value < 0.0 || value > 1.0)
            {
                throw new IccTransformException($"The {description} value {index} must be finite and normalized.");
            }

            index = index + 1;
        }
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

    internal static double[] CloneDoubleArray(double[] source)
    {
        double[] clone = new double[source.Length];
        source.CopyTo(clone, 0);
        return clone;
    }

    internal static IccEncodingRange[] CreateNormalizedRanges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = new IccEncodingRange();
            index = index + 1;
        }

        return ranges;
    }

    internal static IccEncodingRange[] CreatePcsRanges(IccPcsKind pcs, IccPcsEncodingKind encoding)
    {
        if (encoding == IccPcsEncodingKind.Normalized)
        {
            return CreateNormalizedRanges(3);
        }
        if (pcs == IccPcsKind.Xyz)
        {
            return new IccEncodingRange[]
            {
                CreateRange(0.0, PcsConverter.XyzMaximum),
                CreateRange(0.0, PcsConverter.XyzMaximum),
                CreateRange(0.0, PcsConverter.XyzMaximum)
            };
        }

        double chromaMaximum = encoding == IccPcsEncodingKind.Version2 ? PcsConverter.Version2LabMaximum : 127.0;
        return new IccEncodingRange[]
        {
            CreateRange(0.0, 100.0),
            CreateRange(-128.0, chromaMaximum),
            CreateRange(-128.0, chromaMaximum)
        };
    }

    internal static IccEncodingRange CreateRange(double minimum, double maximum)
    {
        IccEncodingRange range = new IccEncodingRange();
        range.Minimum = minimum;
        range.Maximum = maximum;
        return range;
    }
}
