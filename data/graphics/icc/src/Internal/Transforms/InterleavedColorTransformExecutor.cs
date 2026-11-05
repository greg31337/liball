namespace icc;

internal class InterleavedColorTransformResult
{
    internal IccGamutStatus GamutStatus;
    internal IccTransformTrace? Trace;

    internal InterleavedColorTransformResult()
    {
        GamutStatus = IccGamutStatus.Unknown;
        Trace = null;
    }
}

internal static class InterleavedColorTransformExecutor
{
    internal static InterleavedColorTransformResult Execute(PipelineStage[] stages, int sourceChannels, int destinationChannels,
        IccEncodingRange[] destinationRanges, bool clipOutput, bool enableTrace, System.ReadOnlySpan<float> sourceColors,
        int sourceStride, System.Span<float> destinationColors, int destinationStride, int colorCount)
    {
        ValidateArguments(stages, sourceChannels, destinationChannels, destinationRanges, sourceColors.Length, sourceStride,
            destinationColors.Length, destinationStride, colorCount);
        ValidateInputs(sourceColors, sourceStride, sourceChannels, colorCount);

        InterleavedColorTransformResult result = new InterleavedColorTransformResult();
        if (colorCount == 0)
        {
            return result;
        }

        float[]? sourceSnapshot = null;
        System.ReadOnlySpan<float> readableSource = sourceColors;
        if (System.MemoryExtensions.Overlaps<float>(sourceColors, destinationColors))
        {
            sourceSnapshot = sourceColors.ToArray();
            readableSource = sourceSnapshot;
        }

        int valueScratchLength = PipelineExecutor.GetRequiredValueScratchLength(stages);
        double[] inputs = new double[sourceChannels];
        double[] outputs = new double[destinationChannels];
        double[] firstValueScratch = new double[valueScratchLength];
        double[] secondValueScratch = new double[valueScratchLength];
        int[] clutIndexScratch = new int[PipelineExecutor.GetRequiredClutIndexScratchLength(stages)];
        double[] clutWeightScratch = new double[PipelineExecutor.GetRequiredClutWeightScratchLength(stages)];
        IccGamutStatus aggregateStatus = destinationRanges.Length == destinationChannels
            ? IccGamutStatus.Inside
            : IccGamutStatus.Unknown;
        int color = 0;
        while (color < colorCount)
        {
            int sourceOffset = color * sourceStride;
            int channel = 0;
            while (channel < sourceChannels)
            {
                inputs[channel] = readableSource[sourceOffset + channel];
                channel = channel + 1;
            }

            IccTransformTrace? trace = null;
            if (enableTrace && color == colorCount - 1)
            {
                trace = SingleColorTransformExecutor.ExecuteWithTrace(stages, inputs, outputs, firstValueScratch, secondValueScratch,
                    clutIndexScratch, clutWeightScratch);
            }
            else
            {
                PipelineExecutor.ExecutePipeline(stages, inputs, outputs, firstValueScratch, secondValueScratch, clutIndexScratch, clutWeightScratch);
            }

            IccGamutStatus sampleStatus = SingleColorTransformExecutor.GetGamutStatus(outputs, destinationRanges);
            aggregateStatus = CombineGamutStatus(aggregateStatus, sampleStatus);
            if (clipOutput && destinationRanges.Length == destinationChannels)
            {
                SingleColorTransformExecutor.ClipOutputs(outputs, destinationRanges);
            }
            if (trace != null)
            {
                trace.Destination = SingleColorTransformExecutor.CreateVector(outputs);
                result.Trace = trace;
            }

            WriteOutputs(outputs, destinationColors, color * destinationStride);
            color = color + 1;
        }

        result.GamutStatus = aggregateStatus;
        return result;
    }

    internal static void ValidateArguments(PipelineStage[] stages, int sourceChannels, int destinationChannels,
        IccEncodingRange[] destinationRanges, int sourceLength, int sourceStride, int destinationLength, int destinationStride, int colorCount)
    {
        if (stages == null || destinationRanges == null || sourceChannels < 1 || destinationChannels < 1)
        {
            throw new IccTransformException("Interleaved execution received inconsistent stages or endpoint channel counts.");
        }
        if (destinationRanges.Length != 0 && destinationRanges.Length != destinationChannels)
        {
            throw new IccTransformException("Interleaved execution requires one destination range per output channel when ranges are provided.");
        }
        if (sourceStride < sourceChannels || destinationStride < destinationChannels)
        {
            throw new IccTransformException("Interleaved strides shall be at least their corresponding channel counts.");
        }
        if (colorCount < 0)
        {
            throw new IccTransformException("The interleaved colour count cannot be negative.");
        }

        int requiredSourceLength = GetRequiredLength(colorCount, sourceStride, sourceChannels, "source");
        int requiredDestinationLength = GetRequiredLength(colorCount, destinationStride, destinationChannels, "destination");
        if (sourceLength != requiredSourceLength)
        {
            throw new IccTransformException($"The interleaved source buffer requires exactly {requiredSourceLength} values, but it contains {sourceLength}.");
        }
        if (destinationLength != requiredDestinationLength)
        {
            throw new IccTransformException($"The interleaved destination buffer requires exactly {requiredDestinationLength} values, but it contains {destinationLength}.");
        }

        PipelineExecutor.GetRequiredValueScratchLength(stages);
        if (stages.Length == 0 && sourceChannels != destinationChannels)
        {
            throw new IccTransformException("An empty interleaved pipeline shall preserve its channel count.");
        }
        if (stages.Length > 0 && (stages[0].InputChannels != sourceChannels || stages[stages.Length - 1].OutputChannels != destinationChannels))
        {
            throw new IccTransformException("The interleaved pipeline endpoints do not match its declared channel counts.");
        }
    }

    internal static int GetRequiredLength(int colorCount, int stride, int channelCount, string description)
    {
        if (colorCount == 0)
        {
            return 0;
        }
        if (colorCount - 1 > (System.Int32.MaxValue - channelCount) / stride)
        {
            throw new IccTransformException($"The interleaved {description} buffer length exceeds the supported managed size.");
        }

        return ((colorCount - 1) * stride) + channelCount;
    }

    internal static void ValidateInputs(System.ReadOnlySpan<float> sourceColors, int sourceStride, int sourceChannels, int colorCount)
    {
        int color = 0;
        while (color < colorCount)
        {
            int offset = color * sourceStride;
            int channel = 0;
            while (channel < sourceChannels)
            {
                if (!System.Single.IsFinite(sourceColors[offset + channel]))
                {
                    throw new IccTransformException($"Transform input colour {color}, channel {channel} must be finite.");
                }
                channel = channel + 1;
            }
            color = color + 1;
        }
    }

    internal static IccGamutStatus CombineGamutStatus(IccGamutStatus aggregateStatus, IccGamutStatus sampleStatus)
    {
        if (aggregateStatus == IccGamutStatus.Unknown || sampleStatus == IccGamutStatus.Unknown)
        {
            return IccGamutStatus.Unknown;
        }
        if (aggregateStatus == IccGamutStatus.Outside || sampleStatus == IccGamutStatus.Outside)
        {
            return IccGamutStatus.Outside;
        }
        if (aggregateStatus == IccGamutStatus.Boundary || sampleStatus == IccGamutStatus.Boundary)
        {
            return IccGamutStatus.Boundary;
        }

        return IccGamutStatus.Inside;
    }

    internal static void WriteOutputs(double[] outputs, System.Span<float> destinationColors, int destinationOffset)
    {
        int channel = 0;
        while (channel < outputs.Length)
        {
            if (!System.Double.IsFinite(outputs[channel]) || outputs[channel] < -System.Single.MaxValue || outputs[channel] > System.Single.MaxValue)
            {
                throw new IccTransformException($"Transform output channel {channel} cannot be represented as a finite float value.");
            }
            channel = channel + 1;
        }

        channel = 0;
        while (channel < outputs.Length)
        {
            destinationColors[destinationOffset + channel] = (float)outputs[channel];
            channel = channel + 1;
        }
    }
}
