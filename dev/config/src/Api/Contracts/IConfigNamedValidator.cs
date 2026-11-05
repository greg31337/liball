namespace config;

/// <summary>Defines an application validation check registered and selected by name.</summary>
/// <remarks>The validator receives an independent value copy. Return empty text when the value is valid or a diagnostic message when it is invalid.</remarks>
public interface IConfigNamedValidator
{
    /// <summary>Gets the stable, case-sensitive registration name.</summary>
    /// <returns>The nonempty validator name.</returns>
    string Name();
    /// <summary>Validates one present, non-null configuration value.</summary>
    /// <param name="value">An independent copy of the resolved value.</param>
    /// <returns>Empty text for success or a validation error message.</returns>
    string Validate(ConfigValue value);
}
