namespace icc;

internal static class ProofingTransformExecutor
{
    internal static void ApplySingle(PipelineStage[] gamutStages, double[] alarmColor,
        IccEncodingRange[] destinationRanges, bool clipOutput, System.ReadOnlySpan<float> sourceColor,
        SingleColorTransformResult execution)
    {
        double gamutValue = EvaluateSingleGamut(gamutStages, sourceColor);
        bool outside = gamutValue > 0.0;
        execution.GamutStatus = outside ? IccGamutStatus.Outside : IccGamutStatus.Inside;
        if (!outside)
        {
            return;
        }

        execution.Values = CreateAlarmFloats(alarmColor, destinationRanges, clipOutput);
        if (execution.Trace != null)
        {
            execution.Trace.Destination = SingleColorTransformExecutor.CreateVector(ConvertFloatsToDoubles(execution.Values));
        }
    }

    internal static IccGamutStatus ApplyInterleaved(PipelineStage[] gamutStages, double[] alarmColor,
        IccEncodingRange[] destinationRanges, bool clipOutput, float[] sourceColors, int sourceStride,
        System.Span<float> destinationColors, int destinationStride, int colorCount, IccTransformTrace? trace)
    {
        if (colorCount == 0)
        {
            return IccGamutStatus.Unknown;
        }

        int scratchLength = PipelineExecutor.GetRequiredValueScratchLength(gamutStages);
        double[] inputs = new double[gamutStages[0].InputChannels];
        double[] output = new double[1];
        double[] firstScratch = new double[scratchLength];
        double[] secondScratch = new double[scratchLength];
        int[] indexScratch = new int[PipelineExecutor.GetRequiredClutIndexScratchLength(gamutStages)];
        double[] weightScratch = new double[PipelineExecutor.GetRequiredClutWeightScratchLength(gamutStages)];
        float[] alarm = CreateAlarmFloats(alarmColor, destinationRanges, clipOutput);
        IccGamutStatus aggregate = IccGamutStatus.Inside;
        bool lastOutside = false;
        int color = 0;
        while (color < colorCount)
        {
            int sourceOffset = color * sourceStride;
            int channel = 0;
            while (channel < inputs.Length)
            {
                inputs[channel] = sourceColors[sourceOffset + channel];
                channel = channel + 1;
            }
            PipelineExecutor.ExecutePipeline(gamutStages, inputs, output, firstScratch, secondScratch,
                indexScratch, weightScratch);
            bool outside = output[0] > 0.0;
            if (outside)
            {
                aggregate = IccGamutStatus.Outside;
                WriteAlarm(alarm, destinationColors, color * destinationStride);
            }
            lastOutside = outside;
            color = color + 1;
        }
        if (lastOutside && trace != null)
        {
            trace.Destination = SingleColorTransformExecutor.CreateVector(ConvertFloatsToDoubles(alarm));
        }
        return aggregate;
    }

    internal static double EvaluateSingleGamut(PipelineStage[] gamutStages, System.ReadOnlySpan<float> sourceColor)
    {
        int scratchLength = PipelineExecutor.GetRequiredValueScratchLength(gamutStages);
        double[] input = new double[sourceColor.Length];
        int channel = 0;
        while (channel < input.Length)
        {
            input[channel] = sourceColor[channel];
            channel = channel + 1;
        }
        double[] output = new double[1];
        double[] firstScratch = new double[scratchLength];
        double[] secondScratch = new double[scratchLength];
        int[] indexScratch = new int[PipelineExecutor.GetRequiredClutIndexScratchLength(gamutStages)];
        double[] weightScratch = new double[PipelineExecutor.GetRequiredClutWeightScratchLength(gamutStages)];
        PipelineExecutor.ExecutePipeline(gamutStages, input, output, firstScratch, secondScratch,
            indexScratch, weightScratch);
        return output[0];
    }

    internal static float[] CreateAlarmFloats(double[] alarmColor, IccEncodingRange[] destinationRanges, bool clipOutput)
    {
        float[] values = new float[alarmColor.Length];
        int channel = 0;
        while (channel < values.Length)
        {
            double value = alarmColor[channel];
            if (clipOutput && destinationRanges.Length == alarmColor.Length)
            {
                IccEncodingRange range = destinationRanges[channel];
                if (value < range.Minimum)
                {
                    value = range.Minimum;
                }
                else if (value > range.Maximum)
                {
                    value = range.Maximum;
                }
            }
            if (!System.Double.IsFinite(value) || value < -System.Single.MaxValue || value > System.Single.MaxValue)
            {
                throw new IccTransformException($"Gamut alarm channel {channel} cannot be represented as a finite float value.");
            }
            values[channel] = (float)value;
            channel = channel + 1;
        }
        return values;
    }

    internal static void WriteAlarm(float[] alarm, System.Span<float> destination, int offset)
    {
        int channel = 0;
        while (channel < alarm.Length)
        {
            destination[offset + channel] = alarm[channel];
            channel = channel + 1;
        }
    }

    internal static double[] ConvertFloatsToDoubles(float[] source)
    {
        double[] result = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }
}
