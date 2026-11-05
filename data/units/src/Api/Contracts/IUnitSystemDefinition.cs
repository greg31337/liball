namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="UnitSystemDefinition"/>.
/// </summary>
public interface IUnitSystemDefinition
{
    /// <summary>
    /// Gets the stable identifier.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetId();
    /// <summary>
    /// Gets the display name.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetName();
    /// <summary>
    /// Returns a copy of the base unit ids.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetBaseUnitIds();
    /// <summary>
    /// Returns a copy of the preferred quantity kind ids.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetPreferredQuantityKindIds();
    /// <summary>
    /// Returns a copy of the preferred unit ids.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetPreferredUnitIds();
    /// <summary>
    /// Reports whether the supplied identifier is one of this system's base units.
    /// </summary>
    /// <param name="unitId">The stable unit identifier.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool UsesBaseUnitId(string? unitId);
    /// <summary>
    /// Reports whether this system defines a preferred unit for the quantity kind.
    /// </summary>
    /// <param name="quantityKindId">The quantity kind id to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasPreferredUnitFor(string? quantityKindId);
    /// <summary>
    /// Returns the preferred unit identifier for the quantity kind, or null when none is configured.
    /// </summary>
    /// <param name="quantityKindId">The quantity kind id to use.</param>
    /// <returns>The requested text.</returns>
    public string? FindPreferredUnitId(string? quantityKindId);
}
