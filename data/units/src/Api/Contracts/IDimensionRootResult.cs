namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="DimensionRootResult"/>.
/// </summary>
public interface IDimensionRootResult
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
    /// Gets the dimension.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    /// <exception cref="System.InvalidOperationException">The result represents a failure and therefore has no successful value.</exception>
    public DimensionVector GetDimension();
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
