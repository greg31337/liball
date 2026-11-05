namespace asn1;

/// <summary>
/// Exposes incremental decoding through a composed decoder implementation.
/// </summary>
public class AsnIncrementalDecoder : IAsnIncrementalDecoder
{
    internal IAsnIncrementalDecoder Implementation;

    /// <summary>
    /// Initializes an incremental-decoder facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that maintains incremental decode state.</param>
    public AsnIncrementalDecoder(IAsnIncrementalDecoder implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Consumes one input chunk.
    /// </summary>
    /// <param name="input">The incremental input.</param>
    /// <returns>The incremental result.</returns>
    public AsnIncrementalResult Consume(AsnIncrementalInput input)
    {
        return this.Implementation.Consume(input);
    }

    /// <summary>
    /// Completes the current value without another chunk.
    /// </summary>
    /// <returns>The terminal incremental result.</returns>
    public AsnIncrementalResult Complete()
    {
        return this.Implementation.Complete();
    }

    /// <summary>
    /// Resets the decoder for a new independent value.
    /// </summary>
    public void Reset()
    {
        this.Implementation.Reset();
    }
}
