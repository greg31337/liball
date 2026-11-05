namespace icc;

/// <summary>Applies a compiled colour transform.</summary>
public class IccColorTransform : IIccColorTransform
{
    internal ColorTransformCore Core;

    /// <summary>Initializes an uncompiled colour transform.</summary>
    public IccColorTransform()
    {
        Core = new ColorTransformCore();
    }

    /// <summary>Gets the source channel count.</summary>
    /// <returns>Zero for an uncompiled transform.</returns>
    public int GetSourceChannelCount()
    {
        return Core.GetSourceChannelCount();
    }

    /// <summary>Gets the destination channel count.</summary>
    /// <returns>Zero for an uncompiled transform.</returns>
    public int GetDestinationChannelCount()
    {
        return Core.GetDestinationChannelCount();
    }

    /// <summary>Transforms one colour.</summary>
    /// <param name="sourceColor">The source values.</param>
    /// <returns>The destination values.</returns>
    public float[] TransformSingleColor(System.ReadOnlySpan<float> sourceColor)
    {
        return Core.TransformSingle(sourceColor).Values;
    }

    /// <summary>Transforms interleaved colours.</summary>
    /// <param name="sourceColors">The source values.</param>
    /// <param name="sourceStride">The source stride.</param>
    /// <param name="destinationColors">The destination values.</param>
    /// <param name="destinationStride">The destination stride.</param>
    /// <param name="colorCount">The colour count.</param>
    public void TransformInterleavedColors(System.ReadOnlySpan<float> sourceColors, int sourceStride, System.Span<float> destinationColors, int destinationStride, int colorCount)
    {
        Core.TransformInterleaved(sourceColors, sourceStride, destinationColors, destinationStride, colorCount);
    }

    /// <summary>Probes whether one colour can be transformed.</summary>
    /// <param name="sourceColor">The source values.</param>
    /// <returns>The probe result.</returns>
    public IccOperationResult ProbeTransformColor(System.ReadOnlySpan<float> sourceColor)
    {
        return Core.ProbeSingle(sourceColor);
    }

    internal void ConfigureCompiledPlan(PipelineStage[] stages, int sourceChannels, int destinationChannels, IccEncodingRange[] sourceRanges,
        IccEncodingRange[] destinationRanges, IccTransformOptions options, uint tagSignature)
    {
        Core.Configure(stages, sourceChannels, destinationChannels, sourceRanges, destinationRanges, options, tagSignature);
    }

    internal void ConfigureProofingPlan(PipelineStage[] stages, PipelineStage[] gamutStages, int sourceChannels,
        int destinationChannels, IccEncodingRange[] sourceRanges, IccEncodingRange[] destinationRanges,
        IccTransformOptions transformOptions, IccProofingOptions proofingOptions)
    {
        Core.ConfigureProofing(stages, gamutStages, sourceChannels, destinationChannels, sourceRanges,
            destinationRanges, transformOptions, proofingOptions);
    }

    internal void ConfigureDeviceLinkPlan(PipelineStage[] stages, int sourceChannels, int destinationChannels,
        IccEncodingRange[] sourceRanges, IccEncodingRange[] destinationRanges, IccTransformOptions options,
        uint sourceColorSpaceSignature, uint destinationColorSpaceSignature,
        IccProfileSequenceEntry[] profileSequence, IccProfileSequenceIdentifier[] profileSequenceIdentifiers)
    {
        Core.ConfigureDeviceLink(stages, sourceChannels, destinationChannels, sourceRanges, destinationRanges,
            options, sourceColorSpaceSignature, destinationColorSpaceSignature, profileSequence,
            profileSequenceIdentifiers);
    }
}
