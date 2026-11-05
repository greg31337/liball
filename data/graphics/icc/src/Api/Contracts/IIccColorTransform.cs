namespace icc;

/// <summary>
/// Applies a compiled colour transform to individual colours and interleaved buffers.
/// </summary>
public interface IIccColorTransform
{
    /// <summary>
    /// Gets the required number of source channels.
    /// </summary>
    /// <returns>The source channel count.</returns>
    int GetSourceChannelCount();

    /// <summary>
    /// Gets the produced number of destination channels.
    /// </summary>
    /// <returns>The destination channel count.</returns>
    int GetDestinationChannelCount();

    /// <summary>
    /// Transforms one source colour.
    /// </summary>
    /// <param name="sourceColor">The source-channel values.</param>
    /// <returns>The destination-channel values.</returns>
    /// <exception cref="IccTransformException">The source channel count or transform execution is invalid.</exception>
    float[] TransformSingleColor(System.ReadOnlySpan<float> sourceColor);

    /// <summary>
    /// Transforms interleaved colours between caller-provided buffers.
    /// </summary>
    /// <param name="sourceColors">The interleaved source buffer.</param>
    /// <param name="sourceStride">The number of values between source colours.</param>
    /// <param name="destinationColors">The interleaved destination buffer.</param>
    /// <param name="destinationStride">The number of values between destination colours.</param>
    /// <param name="colorCount">The number of colours to transform.</param>
    /// <exception cref="IccTransformException">The buffers, strides, channel counts, or transform execution are invalid.</exception>
    void TransformInterleavedColors(System.ReadOnlySpan<float> sourceColors, int sourceStride, System.Span<float> destinationColors, int destinationStride, int colorCount);

    /// <summary>
    /// Checks one source colour without throwing for expected channel or capability failures.
    /// </summary>
    /// <param name="sourceColor">The source-channel values to check.</param>
    /// <returns>The transform capability outcome.</returns>
    IccOperationResult ProbeTransformColor(System.ReadOnlySpan<float> sourceColor);
}
