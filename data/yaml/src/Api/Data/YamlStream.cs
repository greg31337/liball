using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Represents an ordered YAML stream containing zero or more documents.
/// </summary>
public class YamlStream
{
    /// <summary>
    /// The documents in stream order.
    /// </summary>
    public List<YamlDocument> Documents;
    /// <summary>
    /// The diagnostics in the order they were reported.
    /// </summary>
    public List<YamlDiagnostic> Diagnostics;
    /// <summary>
    /// The source range occupied by this item.
    /// </summary>
    public YamlSourceSpan Span;

    /// <summary>
    /// Initializes a new YAML stream with empty or default state.
    /// </summary>
    public YamlStream()
    {
        Documents = new List<YamlDocument>();
        Diagnostics = new List<YamlDiagnostic>();
        Span = YamlSourceSpan.Empty();
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="document">The YAML document to process.</param>
    internal void AddDocument(YamlDocument? document)
    {
        if (document == null)
        {
            return;
        }

        Documents.Add(document);
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to append.</param>
    internal void AddDiagnostic(YamlDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return;
        }

        Diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Returns the number of document entries.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int DocumentCount()
    {
        return Documents.Count;
    }

    /// <summary>
    /// Determines whether any stream-level diagnostics have been recorded.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasDiagnostics()
    {
        return Diagnostics.Count > 0;
    }
}
