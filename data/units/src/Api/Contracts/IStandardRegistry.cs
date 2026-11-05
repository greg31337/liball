namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="StandardRegistry"/>.
/// </summary>
public interface IStandardRegistry
{
    /// <summary>
    /// Creates the complete standard registry and freezes it before returning.
    /// </summary>
    /// <returns>The resulting unit registry.</returns>
    public UnitRegistry Create();
}
