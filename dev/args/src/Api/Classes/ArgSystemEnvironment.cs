namespace args;

/// <summary>
/// Reads environment-variable values from the current process environment.
/// </summary>
public class ArgSystemEnvironment : IArgEnvironment
{
    /// <summary>
    /// Creates an environment reader backed by <see cref="System.Environment"/>.
    /// </summary>
    public ArgSystemEnvironment()
    {
    }

    /// <summary>
    /// Returns the value of an environment variable from the current process environment.
    /// </summary>
    /// <param name="name">The environment-variable name.</param>
    /// <returns>The variable value, or null when no value is defined.</returns>
    public string? GetEnvironmentVariable(string name)
    {
        return System.Environment.GetEnvironmentVariable(name);
    }
}
