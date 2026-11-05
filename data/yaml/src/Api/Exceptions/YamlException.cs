using System;

namespace yaml;

/// <summary>
/// Represents a failure reported by a public YAML processing operation.
/// </summary>
public class YamlException : System.Exception
{
    /// <summary>
    /// Initializes a YAML exception with a failure message and optional cause.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    /// <param name="innerException">The exception that caused this failure, or null.</param>
    public YamlException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    internal static YamlException Create(string? message)
    {
        string effective = message == null || message == "" ? "YAML processing failed." : message;
        return new YamlException(effective, null);
    }
}
