namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="PhysicalConstantDefinition"/>.
/// </summary>
public interface IPhysicalConstantDefinition
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
    /// Gets the symbol.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetSymbol();
    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetDescription();
    /// <summary>
    /// Gets the exactness label.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetExactnessLabel();
    /// <summary>
    /// Reports whether the constant is labeled as exact.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsExact();
    /// <summary>
    /// Gets the source label for the constant metadata.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetSourceLabel();
    /// <summary>
    /// Gets the source adjustment or edition label.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetAdjustmentOrEdition();
    /// <summary>
    /// Gets the absolute standard uncertainty expressed in the quantity's stored unit.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public double GetStandardUncertainty();
    /// <summary>
    /// Gets the quantity.
    /// </summary>
    /// <returns>The resulting quantity value.</returns>
    public QuantityValue GetQuantity();
    /// <summary>
    /// Returns a copy of the aliases.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetAliases();
    /// <summary>
    /// Reports whether the supplied text matches the definition's identity or aliases.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool MatchesText(string? text);
}
