namespace csv;

/// <summary>
/// Provides instance-based spreadsheet formula-injection protection.
/// </summary>
public class CsvInjectionGuardService : ICsvInjectionGuard
{
    /// <summary>Initializes an injection-guard service.</summary>
    public CsvInjectionGuardService()
    {
    }

    /// <summary>Applies a spreadsheet formula-injection policy.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="mode">The policy to apply.</param>
    /// <returns>The guarded text.</returns>
    public string Apply(string value, CsvInjectionMode mode)
    {
        return CsvInjectionGuard.Apply(value, mode);
    }

    /// <summary>Determines whether text can begin a spreadsheet formula.</summary>
    /// <param name="value">The field text.</param>
    /// <returns><c>true</c> for formula-like text; otherwise <c>false</c>.</returns>
    public bool IsDangerous(string value)
    {
        return CsvInjectionGuard.IsDangerous(value);
    }
}
