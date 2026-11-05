using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Contains the tokens and diagnostics produced by lexical analysis.
/// </summary>
public class TemplateLexerResult
{
    /// <summary>
    /// Stores the ordered tokens.
    /// </summary>
    public List<TemplateToken> Tokens;
    /// <summary>
    /// Stores the ordered diagnostics.
    /// </summary>
    public List<TemplateDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new template lexer result and validates any supplied values.
    /// </summary>
    public TemplateLexerResult()
    {
        Tokens = new List<TemplateToken>();
        Diagnostics = new List<TemplateDiagnostic>();
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

