namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="PrefixDefinition"/>.
/// </summary>
public interface IPrefixDefinition
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
    /// Gets the factor.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetFactor();
}
