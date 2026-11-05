namespace args;

/// <summary>
/// Resolves parser, validation, invocation, help, and completion messages by stable key.
/// </summary>
public interface IArgMessageProvider
{
    /// <summary>
    /// Returns the message associated with a key and positional values, or null when the provider does not define the key.
    /// </summary>
    string? GetMessage(string key, string[] values);
}
