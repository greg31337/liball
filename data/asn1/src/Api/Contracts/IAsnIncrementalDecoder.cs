namespace asn1;

/// <summary>
/// Consumes successive chunks using explicit non-throwing decode states.
/// </summary>
public interface IAsnIncrementalDecoder
{
    /// <summary>
    /// Consumes one input chunk.
    /// </summary>
    /// <param name="input">The incremental input.</param>
    /// <returns>The incremental result.</returns>
    public AsnIncrementalResult Consume(AsnIncrementalInput input);

    /// <summary>
    /// Completes the current value without supplying another chunk.
    /// </summary>
    /// <returns>The terminal incremental result.</returns>
    public AsnIncrementalResult Complete();

    /// <summary>
    /// Resets the decoder for a new independent value.
    /// </summary>
    public void Reset();
}
