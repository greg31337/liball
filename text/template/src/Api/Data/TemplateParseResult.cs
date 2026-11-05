using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Contains a parsed document and all diagnostics reported while lexing or parsing it.
/// </summary>
public class TemplateParseResult
{
    /// <summary>
    /// Specifies the document for this parse result.
    /// </summary>
    public TemplateDocumentSyntax Document;
    /// <summary>
    /// Stores the ordered diagnostics.
    /// </summary>
    public List<TemplateDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new template parse result and validates any supplied values.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateParseResult(TemplateDocumentSyntax document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        Document = document;
        Diagnostics = new List<TemplateDiagnostic>();
    }

    internal void AddDiagnostic(TemplateDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new ArgumentNullException("diagnostic");
        }

        Diagnostics.Add(diagnostic);
        Document.AddDiagnostic(diagnostic);
    }

    /// <summary>
    /// Indicates whether this value contains diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasDiagnostics()
    {
        return Diagnostics.Count > 0;
    }
}

