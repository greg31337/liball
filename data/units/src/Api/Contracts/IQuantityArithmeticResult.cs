namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityArithmeticResult"/>.
/// </summary>
public interface IQuantityArithmeticResult
{
    /// <summary>
    /// Reports whether quantity arithmetic succeeded.
    /// </summary>
    /// <returns>True when arithmetic succeeded; otherwise, false.</returns>
    public bool IsSuccess();
    /// <summary>
    /// Reports whether quantity arithmetic failed.
    /// </summary>
    /// <returns>True when arithmetic failed; otherwise, false.</returns>
    public bool IsFailure();
    /// <summary>
    /// Gets the resulting quantity.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
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
}
