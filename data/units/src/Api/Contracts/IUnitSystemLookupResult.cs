namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="UnitSystemLookupResult"/>.
/// </summary>
public interface IUnitSystemLookupResult
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
    /// Gets the unit system.
    /// </summary>
    /// <returns>The resulting unit-system definition.</returns>
    public UnitSystemDefinition GetUnitSystem();
    /// <summary>
    /// Returns a copy of the unit systems.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public UnitSystemDefinition[] GetUnitSystems();
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
