using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace template;

/// <summary>
/// Contains a source, its parsed document, and the diagnostics captured when it was compiled.
/// </summary>
/// <remarks>
/// Compilation currently means parsing and retaining the syntax tree; rendering evaluates that tree for each model.
/// </remarks>
public class TemplateCompiledTemplate
{
    /// <summary>
    /// Stores the template source associated with this value.
    /// </summary>
    public TemplateSource Source;
    /// <summary>
    /// Specifies the document for this compiled template.
    /// </summary>
    public TemplateDocumentSyntax Document;
    /// <summary>
    /// Stores the ordered diagnostics.
    /// </summary>
    public List<TemplateDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new template compiled template and validates any supplied values.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <param name="parseResult">The parse result used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateCompiledTemplate(TemplateSource source, TemplateParseResult parseResult)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (parseResult == null)
        {
            throw new ArgumentNullException("parseResult");
        }

        Source = source;
        Document = parseResult.Document;
        Diagnostics = new List<TemplateDiagnostic>();
        int index = 0;
        while (index < parseResult.Diagnostics.Count)
        {
            Diagnostics.Add(parseResult.Diagnostics[index]);
            index = index + 1;
        }
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

