using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents malformed or inconsistent JMAP protocol data.
/// </summary>
public class JmapProtocolException : System.Exception
{
    /// <summary>The protocol path associated with the failure, or null when no path is available.</summary>
    public string? Path;

    /// <summary>The structured protocol data associated with the failure.</summary>
    public JsonElement StructuredData;

    /// <summary>Creates a protocol exception with its structured failure context.</summary>
    /// <param name="message">The diagnostic failure message.</param>
    /// <param name="path">The protocol path associated with the failure, or null.</param>
    /// <param name="data">The structured protocol data associated with the failure.</param>
    public JmapProtocolException(string message, string? path, JsonElement data) : base(message)
    {
        Path = path;
        StructuredData = data;
    }
}
