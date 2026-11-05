namespace fix;

/// <summary>Provides the enum-defined capabilities of a FIX implementation.</summary>
public interface IFixApiCapabilitiesProvider
{
    /// <summary>Gets a snapshot of the implementation's supported capabilities.</summary>
    /// <returns>The supported capability values.</returns>
    FixApiCapabilities GetFixApiCapabilities();
}
