namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="MolarMassContext"/>.
/// </summary>
public interface IMolarMassContext
{
    /// <summary>
    /// Gets the positive scale-only mass-per-amount quantity supplied by the caller.
    /// </summary>
    /// <returns>The molar-mass quantity.</returns>
    public QuantityValue GetMolarMass();
}
