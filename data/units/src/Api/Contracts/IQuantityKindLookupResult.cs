namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityKindLookupResult"/>.
/// </summary>
public interface IQuantityKindLookupResult
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
    /// Reports whether lookup produced more than one candidate.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsAmbiguous();
    /// <summary>
    /// Gets the quantity kind.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition GetQuantityKind();
    /// <summary>
    /// Returns a copy of the quantity kinds.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public QuantityKindDefinition[] GetQuantityKinds();
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
