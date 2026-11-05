namespace icc;

internal enum PipelineStageKind
{
    Identity = 0,
    Curves = 1,
    Matrix = 2,
    Clut = 3,
    PcsNormalize = 4,
    PcsDenormalize = 5,
    ChannelMap = 6,
    Clip = 7,
    Unsupported = 8,
    ToneMap = 9,
    PcsLabToXyz = 10,
    PcsXyzToLab = 11
}

internal class PipelineStage
{
    internal PipelineStageKind Kind;
    internal int InputChannels;
    internal int OutputChannels;
    internal IccCurve[] Curves;
    internal bool InvertCurves;
    internal IccMatrix? Matrix;
    internal IccClut? Clut;
    internal IccToneMap? ToneMap;
    internal IccInterpolationKind Interpolation;
    internal IccPcsKind PcsKind;
    internal IccPcsEncodingKind PcsEncoding;
    internal int[] ChannelOrder;
    internal double[] ChannelDefaults;
    internal IccEncodingRange[] Ranges;
    internal string Description;
    internal uint TagSignature;

    internal PipelineStage()
    {
        Kind = PipelineStageKind.Identity;
        InputChannels = 0;
        OutputChannels = 0;
        Curves = new IccCurve[0];
        InvertCurves = false;
        Matrix = null;
        Clut = null;
        ToneMap = null;
        Interpolation = IccInterpolationKind.Multilinear;
        PcsKind = IccPcsKind.None;
        PcsEncoding = IccPcsEncodingKind.Normalized;
        ChannelOrder = new int[0];
        ChannelDefaults = new double[0];
        Ranges = new IccEncodingRange[0];
        Description = string.Empty;
        TagSignature = 0;
    }
}

internal static class PipelineExecutor
{
    internal static int GetRequiredValueScratchLength(PipelineStage[] stages)
    {
        if (stages == null)
        {
            throw new IccTransformException("A transform pipeline cannot be null.");
        }

        int maximum = 0;
        int previousChannels = 0;
        int index = 0;
        while (index < stages.Length)
        {
            PipelineStage stage = stages[index];
            if (stage == null)
            {
                throw new IccTransformException($"Transform stage {index} cannot be null.");
            }
            if (stage.InputChannels < 1 || stage.OutputChannels < 1)
            {
                throw new IccTransformException($"Transform stage {index} must declare positive input and output channel counts.");
            }
            if (index > 0 && stage.InputChannels != previousChannels)
            {
                throw new IccTransformException($"Transform stage {index} expects {stage.InputChannels} channels after a stage that produces {previousChannels}.");
            }

            ValidateStage(stage, index);
            if (stage.InputChannels > maximum)
            {
                maximum = stage.InputChannels;
            }
            if (stage.OutputChannels > maximum)
            {
                maximum = stage.OutputChannels;
            }

            previousChannels = stage.OutputChannels;
            index = index + 1;
        }

        return maximum;
    }

    internal static int GetRequiredClutIndexScratchLength(PipelineStage[] stages)
    {
        int maximum = GetRequiredClutInputChannels(stages);
        if (maximum == 0)
        {
            return 0;
        }

        return ClutInterpolator.GetMultilinearIndexScratchLength(maximum);
    }

    internal static int GetRequiredClutWeightScratchLength(PipelineStage[] stages)
    {
        int maximum = GetRequiredClutInputChannels(stages);
        if (maximum == 0)
        {
            return 0;
        }

        return ClutInterpolator.GetMultilinearWeightScratchLength(maximum);
    }

    internal static void ExecutePipeline(PipelineStage[] stages, System.ReadOnlySpan<double> inputs, System.Span<double> outputs, System.Span<double> firstValueScratch, System.Span<double> secondValueScratch, System.Span<int> clutIndexScratch, System.Span<double> clutWeightScratch)
    {
        int requiredValueScratchLength = GetRequiredValueScratchLength(stages);
        if (stages.Length == 0)
        {
            if (outputs.Length < inputs.Length)
            {
                throw new IccTransformException("An empty identity pipeline requires an output span at least as long as its input span.");
            }

            ValidateFiniteInputs(inputs);
            inputs.CopyTo(outputs);
            return;
        }

        if (inputs.Length != stages[0].InputChannels)
        {
            throw new IccTransformException($"The transform requires {stages[0].InputChannels} source channels, but it received {inputs.Length}.");
        }
        if (outputs.Length < stages[stages.Length - 1].OutputChannels)
        {
            throw new IccTransformException("The transform output span is too short for the destination channels.");
        }
        if (firstValueScratch.Length < requiredValueScratchLength || secondValueScratch.Length < requiredValueScratchLength)
        {
            throw new IccTransformException($"Transform execution requires two value scratch spans of at least {requiredValueScratchLength} values each.");
        }

        int requiredIndexScratchLength = GetRequiredClutIndexScratchLength(stages);
        int requiredWeightScratchLength = GetRequiredClutWeightScratchLength(stages);
        if (clutIndexScratch.Length < requiredIndexScratchLength || clutWeightScratch.Length < requiredWeightScratchLength)
        {
            throw new IccTransformException("The transform CLUT scratch spans are too short.");
        }

        ValidateFiniteInputs(inputs);
        inputs.CopyTo(firstValueScratch);
        System.ReadOnlySpan<double> current = firstValueScratch.Slice(0, inputs.Length);
        bool writeSecond = true;
        int stageIndex = 0;
        while (stageIndex < stages.Length)
        {
            PipelineStage stage = stages[stageIndex];
            System.Span<double> target;
            if (writeSecond)
            {
                target = secondValueScratch.Slice(0, stage.OutputChannels);
            }
            else
            {
                target = firstValueScratch.Slice(0, stage.OutputChannels);
            }

            ExecuteStage(stage, current, target, clutIndexScratch, clutWeightScratch);
            current = target;
            writeSecond = !writeSecond;
            stageIndex = stageIndex + 1;
        }

        current.CopyTo(outputs);
    }

    internal static void ExecuteStage(PipelineStage stage, System.ReadOnlySpan<double> inputs, System.Span<double> outputs, System.Span<int> clutIndexScratch, System.Span<double> clutWeightScratch)
    {
        switch (stage.Kind)
        {
            case PipelineStageKind.Identity:
                ApplyIdentityStage(inputs, outputs);
                return;
            case PipelineStageKind.Curves:
                ApplyCurveStage(stage, inputs, outputs);
                return;
            case PipelineStageKind.Matrix:
                ApplyMatrixStage(stage, inputs, outputs);
                return;
            case PipelineStageKind.Clut:
                ApplyClutStage(stage, inputs, outputs, clutIndexScratch, clutWeightScratch);
                return;
            case PipelineStageKind.ToneMap:
                Icc2PipelineStages.ApplyToneMap(stage, inputs, outputs);
                return;
            case PipelineStageKind.PcsNormalize:
                ApplyPcsNormalizeStage(stage, inputs, outputs);
                return;
            case PipelineStageKind.PcsDenormalize:
                ApplyPcsDenormalizeStage(stage, inputs, outputs);
                return;
            case PipelineStageKind.PcsLabToXyz:
                ApplyPcsLabToXyzStage(inputs, outputs);
                return;
            case PipelineStageKind.PcsXyzToLab:
                ApplyPcsXyzToLabStage(inputs, outputs);
                return;
            case PipelineStageKind.ChannelMap:
                ApplyChannelMapStage(stage, inputs, outputs);
                return;
            case PipelineStageKind.Clip:
                ApplyClipStage(stage, inputs, outputs);
                return;
            default:
                throw new IccTransformException("The transform stage is unsupported.");
        }
    }

    internal static void ApplyIdentityStage(System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        inputs.CopyTo(outputs);
    }

    internal static void ApplyCurveStage(PipelineStage stage, System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        int channel = 0;
        while (channel < stage.OutputChannels)
        {
            if (stage.InvertCurves)
            {
                IccCurveInversionResult inversion = CurveInverter.InvertCurve(stage.Curves[channel], inputs[channel]);
                if (!inversion.HasInput)
                {
                    throw new IccTransformException($"Curve stage channel {channel} cannot invert its input: {inversion.Result.Message}");
                }

                outputs[channel] = inversion.Input;
            }
            else
            {
                outputs[channel] = CurveMath.EvaluateCurve(stage.Curves[channel], inputs[channel]);
            }

            channel = channel + 1;
        }
    }

    internal static void ApplyMatrixStage(PipelineStage stage, System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        IccMatrix? matrix = stage.Matrix;
        if (matrix == null)
        {
            throw new IccTransformException("A matrix execution stage has no matrix data.");
        }

        int row = 0;
        while (row < stage.OutputChannels)
        {
            int rowOffset = row * matrix.Columns;
            double sum = matrix.Columns == stage.InputChannels + 1 ? matrix.Values[rowOffset + stage.InputChannels] : 0.0;
            double compensation = 0.0;
            int column = 0;
            while (column < stage.InputChannels)
            {
                double product = matrix.Values[rowOffset + column] * inputs[column];
                if (!System.Double.IsFinite(product))
                {
                    throw new IccTransformException($"Matrix stage output {row} is outside the finite double range.");
                }

                double adjusted = product - compensation;
                double next = sum + adjusted;
                if (!System.Double.IsFinite(next))
                {
                    throw new IccTransformException($"Matrix stage output {row} is outside the finite double range.");
                }

                compensation = (next - sum) - adjusted;
                sum = next;
                column = column + 1;
            }

            outputs[row] = sum;
            row = row + 1;
        }
    }

    internal static void ApplyClutStage(PipelineStage stage, System.ReadOnlySpan<double> inputs, System.Span<double> outputs, System.Span<int> clutIndexScratch, System.Span<double> clutWeightScratch)
    {
        IccClut? clut = stage.Clut;
        if (clut == null)
        {
            throw new IccTransformException("A CLUT execution stage has no CLUT data.");
        }

        ClutInterpolator.InterpolateClut(clut, inputs, outputs, clutIndexScratch, clutWeightScratch, stage.Interpolation);
    }

    internal static void ApplyPcsNormalizeStage(PipelineStage stage, System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        if (stage.PcsEncoding == IccPcsEncodingKind.Normalized || stage.PcsEncoding == IccPcsEncodingKind.Version5)
        {
            inputs.CopyTo(outputs);
            return;
        }

        PcsConverter.NormalizePcsComponents(inputs, outputs, stage.PcsKind, stage.PcsEncoding);
    }

    internal static void ApplyPcsDenormalizeStage(PipelineStage stage, System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        if (stage.PcsEncoding == IccPcsEncodingKind.Normalized || stage.PcsEncoding == IccPcsEncodingKind.Version5)
        {
            inputs.CopyTo(outputs);
            return;
        }

        PcsConverter.DenormalizePcsComponents(inputs, outputs, stage.PcsKind, stage.PcsEncoding);
    }

    internal static void ApplyPcsLabToXyzStage(System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        double y = (inputs[0] + 16.0) / 116.0;
        double x = y + (inputs[1] / 500.0);
        double z = y - (inputs[2] / 200.0);
        outputs[0] = IccStandardColorimetry.D50X * ColorMathCore.EvaluateInverseLabFunction(x);
        outputs[1] = IccStandardColorimetry.D50Y * ColorMathCore.EvaluateInverseLabFunction(y);
        outputs[2] = IccStandardColorimetry.D50Z * ColorMathCore.EvaluateInverseLabFunction(z);
        ValidateFiniteInputs(outputs.Slice(0, 3));
    }

    internal static void ApplyPcsXyzToLabStage(System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        double x = ColorMathCore.EvaluateLabFunction(inputs[0] / IccStandardColorimetry.D50X);
        double y = ColorMathCore.EvaluateLabFunction(inputs[1] / IccStandardColorimetry.D50Y);
        double z = ColorMathCore.EvaluateLabFunction(inputs[2] / IccStandardColorimetry.D50Z);
        outputs[0] = (116.0 * y) - 16.0;
        outputs[1] = 500.0 * (x - y);
        outputs[2] = 200.0 * (y - z);
        ValidateFiniteInputs(outputs.Slice(0, 3));
    }

    internal static void ApplyChannelMapStage(PipelineStage stage, System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        int output = 0;
        while (output < stage.OutputChannels)
        {
            int source = stage.ChannelOrder[output];
            if (source < 0)
            {
                outputs[output] = stage.ChannelDefaults[output];
            }
            else
            {
                outputs[output] = inputs[source];
            }

            output = output + 1;
        }
    }

    internal static void ApplyClipStage(PipelineStage stage, System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        int channel = 0;
        while (channel < stage.OutputChannels)
        {
            IccEncodingRange range = stage.Ranges[channel];
            double value = inputs[channel];
            if (value < range.Minimum)
            {
                value = range.Minimum;
            }
            else if (value > range.Maximum)
            {
                value = range.Maximum;
            }

            outputs[channel] = value;
            channel = channel + 1;
        }
    }

    internal static int GetRequiredClutInputChannels(PipelineStage[] stages)
    {
        if (stages == null)
        {
            throw new IccTransformException("A transform pipeline cannot be null.");
        }

        int maximum = 0;
        int index = 0;
        while (index < stages.Length)
        {
            PipelineStage stage = stages[index];
            if (stage == null)
            {
                throw new IccTransformException($"Transform stage {index} cannot be null.");
            }
            if (stage.Kind == PipelineStageKind.Clut && stage.InputChannels > maximum)
            {
                maximum = stage.InputChannels;
            }

            index = index + 1;
        }

        return maximum;
    }

    internal static void ValidateStage(PipelineStage stage, int index)
    {
        switch (stage.Kind)
        {
            case PipelineStageKind.Identity:
                if (stage.InputChannels != stage.OutputChannels)
                {
                    throw new IccTransformException($"Identity stage {index} must preserve its channel count.");
                }

                return;
            case PipelineStageKind.Curves:
                ValidateCurveStage(stage, index);
                return;
            case PipelineStageKind.Matrix:
                ValidateMatrixStage(stage, index);
                return;
            case PipelineStageKind.Clut:
                ValidateClutStage(stage, index);
                return;
            case PipelineStageKind.ToneMap:
                Icc2PipelineStages.ValidateToneMapStage(stage, index);
                return;
            case PipelineStageKind.PcsNormalize:
            case PipelineStageKind.PcsDenormalize:
                ValidatePcsStage(stage, index);
                return;
            case PipelineStageKind.PcsLabToXyz:
            case PipelineStageKind.PcsXyzToLab:
                ValidatePcsBridgeStage(stage, index);
                return;
            case PipelineStageKind.ChannelMap:
                ValidateChannelMapStage(stage, index);
                return;
            case PipelineStageKind.Clip:
                ValidateClipStage(stage, index);
                return;
            case PipelineStageKind.Unsupported:
                throw new IccTransformException($"Transform stage {index} is unsupported: {stage.Description}");
            default:
                throw new IccTransformException($"Transform stage {index} has an unknown execution kind.");
        }
    }

    internal static void ValidateCurveStage(PipelineStage stage, int index)
    {
        if (stage.InputChannels != stage.OutputChannels || stage.Curves == null || stage.Curves.Length != stage.InputChannels)
        {
            throw new IccTransformException($"Curve stage {index} requires exactly one curve per preserved channel.");
        }

        int channel = 0;
        while (channel < stage.Curves.Length)
        {
            if (stage.Curves[channel] == null)
            {
                throw new IccTransformException($"Curve stage {index} channel {channel} cannot be null.");
            }

            channel = channel + 1;
        }
    }

    internal static void ValidateMatrixStage(PipelineStage stage, int index)
    {
        IccMatrix? matrix = stage.Matrix;
        if (matrix == null || matrix.Values == null || matrix.Rows != stage.OutputChannels || (matrix.Columns != stage.InputChannels && matrix.Columns != stage.InputChannels + 1))
        {
            throw new IccTransformException($"Matrix stage {index} dimensions must match its input and output channels, with at most one constant column.");
        }
        if (matrix.Rows > System.Int32.MaxValue / matrix.Columns || matrix.Values.Length != matrix.Rows * matrix.Columns)
        {
            throw new IccTransformException($"Matrix stage {index} has an invalid or overflowing value count.");
        }

        ValidateFiniteArray(matrix.Values, $"matrix stage {index}");
    }

    internal static void ValidateClutStage(PipelineStage stage, int index)
    {
        IccClut? clut = stage.Clut;
        if (clut == null || clut.GridPoints == null || clut.Values == null || clut.InputChannels != stage.InputChannels || clut.OutputChannels != stage.OutputChannels || clut.GridPoints.Length != stage.InputChannels)
        {
            throw new IccTransformException($"CLUT stage {index} dimensions must match its input and output channels.");
        }
        if (stage.InputChannels > 16)
        {
            throw new IccTransformException($"CLUT stage {index} exceeds the supported 16 input channels.");
        }
        if (stage.Interpolation != IccInterpolationKind.Linear && stage.Interpolation != IccInterpolationKind.Multilinear && stage.Interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccTransformException($"CLUT stage {index} has an unsupported interpolation algorithm.");
        }
        if (stage.Interpolation == IccInterpolationKind.Linear && stage.InputChannels != 1)
        {
            throw new IccTransformException($"CLUT stage {index} can use linear interpolation only with one input channel.");
        }

        int valueCount = stage.OutputChannels;
        int dimension = 0;
        while (dimension < stage.InputChannels)
        {
            int grid = clut.GridPoints[dimension];
            if (grid < 2 || grid > 255 || valueCount > System.Int32.MaxValue / grid)
            {
                throw new IccTransformException($"CLUT stage {index} grid dimension {dimension} is invalid or overflows its value count.");
            }

            valueCount = valueCount * grid;
            dimension = dimension + 1;
        }
        if (clut.Values.Length != valueCount)
        {
            throw new IccTransformException($"CLUT stage {index} does not contain its required {valueCount} values.");
        }
    }

    internal static void ValidatePcsStage(PipelineStage stage, int index)
    {
        if (stage.InputChannels != 3 || stage.OutputChannels != 3 || (stage.PcsKind != IccPcsKind.Xyz && stage.PcsKind != IccPcsKind.Lab))
        {
            throw new IccTransformException($"PCS stage {index} requires three PCSXYZ or PCSLAB channels.");
        }
        if (stage.PcsEncoding != IccPcsEncodingKind.Normalized && stage.PcsEncoding != IccPcsEncodingKind.Version2 && stage.PcsEncoding != IccPcsEncodingKind.Version4 && stage.PcsEncoding != IccPcsEncodingKind.Version5)
        {
            throw new IccTransformException($"PCS stage {index} has an unsupported PCS encoding.");
        }
    }

    internal static void ValidatePcsBridgeStage(PipelineStage stage, int index)
    {
        if (stage.InputChannels != 3 || stage.OutputChannels != 3)
        {
            throw new IccTransformException($"PCS bridge stage {index} requires exactly three input and output channels.");
        }
    }

    internal static void ValidateChannelMapStage(PipelineStage stage, int index)
    {
        if (stage.ChannelOrder == null || stage.ChannelDefaults == null || stage.ChannelOrder.Length != stage.OutputChannels || stage.ChannelDefaults.Length != stage.OutputChannels)
        {
            throw new IccTransformException($"Channel-map stage {index} requires one source index and default value per output channel.");
        }

        int output = 0;
        while (output < stage.OutputChannels)
        {
            int source = stage.ChannelOrder[output];
            if (source < -1 || source >= stage.InputChannels)
            {
                throw new IccTransformException($"Channel-map stage {index} output {output} has an invalid source index.");
            }
            if (!System.Double.IsFinite(stage.ChannelDefaults[output]))
            {
                throw new IccTransformException($"Channel-map stage {index} default {output} must be finite.");
            }

            output = output + 1;
        }
    }

    internal static void ValidateClipStage(PipelineStage stage, int index)
    {
        if (stage.InputChannels != stage.OutputChannels || stage.Ranges == null || stage.Ranges.Length != stage.InputChannels)
        {
            throw new IccTransformException($"Clipping stage {index} requires one range per preserved channel.");
        }

        int channel = 0;
        while (channel < stage.Ranges.Length)
        {
            IccEncodingRange range = stage.Ranges[channel];
            if (range == null || !System.Double.IsFinite(range.Minimum) || !System.Double.IsFinite(range.Maximum) || range.Minimum > range.Maximum)
            {
                throw new IccTransformException($"Clipping stage {index} range {channel} must have finite, ordered endpoints.");
            }

            channel = channel + 1;
        }
    }

    internal static void ValidateFiniteInputs(System.ReadOnlySpan<double> inputs)
    {
        int channel = 0;
        while (channel < inputs.Length)
        {
            if (!System.Double.IsFinite(inputs[channel]))
            {
                throw new IccTransformException($"Transform input channel {channel} must be finite.");
            }

            channel = channel + 1;
        }
    }

    internal static void ValidateFiniteArray(double[] values, string description)
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
}
