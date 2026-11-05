namespace csv;

/// <summary>
/// Detects and handles spreadsheet formula-injection text.
/// </summary>
public interface ICsvInjectionGuard
{
    /// <summary>Applies a spreadsheet formula-injection policy.</summary>
    /// <param name="value">The field text.</param>
    /// <param name="mode">The policy to apply.</param>
    /// <returns>The guarded text.</returns>
    string Apply(string value, CsvInjectionMode mode);

    /// <summary>Determines whether text can begin a spreadsheet formula.</summary>
    /// <param name="value">The field text.</param>
    /// <returns><c>true</c> for formula-like text; otherwise <c>false</c>.</returns>
    bool IsDangerous(string value);
}
