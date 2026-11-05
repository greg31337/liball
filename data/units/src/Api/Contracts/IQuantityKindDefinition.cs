namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityKindDefinition"/>.
/// </summary>
public interface IQuantityKindDefinition
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
    /// Gets the dimension.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector GetDimension();
    /// <summary>
    /// Gets the default unit id.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetDefaultUnitId();
    /// <summary>
    /// Returns a copy of the aliases.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetAliases();
    /// <summary>
    /// Returns a copy of the accepted unit ids.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetAcceptedUnitIds();
    /// <summary>
    /// Reports whether the supplied text matches the definition's identity or aliases.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool MatchesText(string? text);
    /// <summary>
    /// Reports whether the supplied unit identifier is accepted for this quantity kind.
    /// </summary>
    /// <param name="unitId">The stable unit identifier.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool AcceptsUnitId(string? unitId);
    /// <summary>
    /// Reports whether the supplied unit has this quantity kind's dimension and is accepted by identifier.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool AcceptsUnit(UnitDefinition? unit);
}
