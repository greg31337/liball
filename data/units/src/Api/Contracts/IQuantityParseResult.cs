namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityParseResult"/>.
/// </summary>
public interface IQuantityParseResult
{
    /// <summary>
    /// Reports whether parsing succeeded.
    /// </summary>
    /// <returns>True when parsing succeeded; otherwise, false.</returns>
    public bool IsSuccess();
    /// <summary>
    /// Reports whether parsing failed.
    /// </summary>
    /// <returns>True when parsing failed; otherwise, false.</returns>
    public bool IsFailure();
    /// <summary>
    /// Gets the parsed quantity.
    /// </summary>
    /// <returns>The parsed quantity value.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure.</exception>
    public QuantityValue GetQuantity();
    /// <summary>
    /// Gets the stable error code.
    /// </summary>
    /// <returns>The resulting unit error code.</returns>
    public UnitErrorCode GetErrorCode();
    /// <summary>
    /// Gets the developer-readable result message, which is empty for success.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetMessage();
    /// <summary>
    /// Gets the zero-based source position of the failure, or -1 for success.
    /// </summary>
    /// <returns>The source position.</returns>
    public int GetPosition();
    /// <summary>
    /// Returns a copy of unit ambiguity candidates.
    /// </summary>
    /// <returns>A copy of the candidate unit definitions.</returns>
    public UnitDefinition[] GetCandidates();
}
