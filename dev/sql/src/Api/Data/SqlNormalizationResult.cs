using System;

namespace sql;

/// <summary>Contains a normalized AST, its stable text, diagnostics, and explicit unresolved-change markers.</summary>
public class SqlNormalizationResult
{
    /// <summary>The unchanged input script.</summary>
    public SqlScript InputScript;
    /// <summary>The fresh syntax tree parsed from normalized text.</summary>
    public SqlScript Script;
    /// <summary>The normalized SQL used to construct the result syntax tree.</summary>
    public string NormalizedText;
    /// <summary>The source regions that could not be normalized losslessly.</summary>
    public SqlNormalizationMarker[] Markers;
    /// <summary>The diagnostics produced while reparsing normalized SQL.</summary>
    public SqlDiagnosticBag Diagnostics;

    /// <summary>Initializes a normalization result.</summary>
    /// <param name="inputScript">The unchanged input script.</param>
    /// <param name="script">The normalized syntax tree.</param>
    /// <param name="normalizedText">The normalized SQL text.</param>
    /// <param name="markers">The unresolved-change markers.</param>
    /// <param name="diagnostics">The reparse diagnostics.</param>
    /// <exception cref="ArgumentNullException">inputScript, script, normalizedText, markers, diagnostics, or a marker is <c>null</c>.</exception>
    public SqlNormalizationResult(SqlScript inputScript, SqlScript script, string normalizedText, SqlNormalizationMarker[] markers, SqlDiagnosticBag diagnostics)
    {
        InputScript = inputScript ?? throw new ArgumentNullException("inputScript");
        Script = script ?? throw new ArgumentNullException("script");
        NormalizedText = normalizedText ?? throw new ArgumentNullException("normalizedText");
        ValidateMarkers(markers);
        Markers = markers;
        Diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
    }

    internal static void ValidateMarkers(SqlNormalizationMarker[] markers)
    {
        if (markers == null)
        {
            throw new ArgumentNullException("markers");
        }

        int index = 0;
        while (index < markers.Length)
        {
            if (markers[index] == null)
            {
                throw new ArgumentNullException("markers");
            }

            index = index + 1;
        }
    }
}
