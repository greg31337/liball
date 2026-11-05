namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityConstructionResult"/>.
/// </summary>
public interface IQuantityConstructionResult
{
    /// <summary>
    /// Reports whether quantity construction succeeded.
    /// </summary>
    /// <returns>True when construction succeeded; otherwise, false.</returns>
    public bool IsSuccess();
    /// <summary>
    /// Reports whether quantity construction failed.
    /// </summary>
    /// <returns>True when construction failed; otherwise, false.</returns>
    public bool IsFailure();
    /// <summary>
    /// Gets the constructed quantity.
    /// </summary>
    /// <returns>The constructed quantity value.</returns>
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
    /// Returns a copy of unit ambiguity candidates.
    /// </summary>
    /// <returns>A copy of the candidate unit definitions.</returns>
    public UnitDefinition[] GetCandidates();
}
