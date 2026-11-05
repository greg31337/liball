namespace args;

/// <summary>
/// Provides environment-variable values used as option and argument fallbacks.
/// </summary>
/// <remarks>
/// Implement this interface to supply deterministic values without changing process-wide environment state.
/// </remarks>
public interface IArgEnvironment
{
    /// <summary>
    /// Returns the value of an environment variable, or null when the variable is not defined.
    /// </summary>
    /// <param name="name">The environment-variable name.</param>
    /// <returns>The variable value, or null when no value is defined.</returns>
    string? GetEnvironmentVariable(string name);
}
