using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents validation failures detected before a JMAP operation is performed.
/// </summary>
public class JmapValidationException : System.Exception
{
    /// <summary>The invalid property paths associated with the failure.</summary>
    public List<string> InvalidProperties;

    /// <summary>The structured input data associated with the failure.</summary>
    public JsonElement StructuredData;

    /// <summary>Creates a validation exception with invalid paths and structured input data.</summary>
    /// <param name="message">The diagnostic failure message.</param>
    /// <param name="invalidProperties">The invalid property paths associated with the failure.</param>
    /// <param name="data">The structured input data associated with the failure.</param>
    public JmapValidationException(string message, List<string> invalidProperties, JsonElement data) : base(message)
    {
        InvalidProperties = invalidProperties;
        StructuredData = data;
    }
}
