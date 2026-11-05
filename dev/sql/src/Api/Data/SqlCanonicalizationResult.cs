using System;

namespace sql;

/// <summary>Contains a canonical AST and stable comparison text with any unresolved-change markers.</summary>
public class SqlCanonicalizationResult
{
    /// <summary>The unchanged input script.</summary>
    public SqlScript InputScript;
    /// <summary>The fresh syntax tree parsed from canonical text.</summary>
    public SqlScript Script;
    /// <summary>The deterministic text key for AST comparison.</summary>
    public string CanonicalText;
    /// <summary>The source regions that could not be canonicalized losslessly.</summary>
    public SqlNormalizationMarker[] Markers;
    /// <summary>The diagnostics produced while reparsing canonical SQL.</summary>
    public SqlDiagnosticBag Diagnostics;

    /// <summary>Initializes a canonicalization result.</summary>
    /// <param name="inputScript">The unchanged input script.</param>
    /// <param name="script">The canonical syntax tree.</param>
    /// <param name="canonicalText">The deterministic comparison text.</param>
    /// <param name="markers">The unresolved-change markers.</param>
    /// <param name="diagnostics">The reparse diagnostics.</param>
    /// <exception cref="ArgumentNullException">inputScript, script, canonicalText, markers, diagnostics, or a marker is <c>null</c>.</exception>
    public SqlCanonicalizationResult(SqlScript inputScript, SqlScript script, string canonicalText, SqlNormalizationMarker[] markers, SqlDiagnosticBag diagnostics)
    {
        InputScript = inputScript ?? throw new ArgumentNullException("inputScript");
        Script = script ?? throw new ArgumentNullException("script");
        CanonicalText = canonicalText ?? throw new ArgumentNullException("canonicalText");
        SqlNormalizationResult.ValidateMarkers(markers);
        Markers = markers;
        Diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
    }
}
