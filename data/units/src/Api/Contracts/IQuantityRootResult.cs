namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityRootResult"/>.
/// </summary>
public interface IQuantityRootResult
{
    /// <summary>
    /// Reports whether this result represents a successful operation.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsSuccess();
    /// <summary>
    /// Reports whether this result represents a failed operation.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsFailure();
    /// <summary>
    /// Gets the quantity.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure and therefore has no successful value.</exception>
    public QuantityValue GetQuantity();
    /// <summary>
    /// Gets the error code.
    /// </summary>
    /// <returns>The resulting unit error code.</returns>
    public UnitErrorCode GetErrorCode();
    /// <summary>
    /// Gets the developer-readable result message, which is empty for success.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetMessage();
}
