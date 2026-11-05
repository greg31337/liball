namespace sql;

internal class SqlNormalizationWorkResult
{
    internal SqlScript Script;
    internal string Text;
    internal SqlNormalizationMarker[] Markers;
    internal SqlDiagnosticBag Diagnostics;

    internal SqlNormalizationWorkResult(SqlScript script, string text, SqlNormalizationMarker[] markers, SqlDiagnosticBag diagnostics)
    {
        Script = script;
        Text = text;
        Markers = markers;
        Diagnostics = diagnostics;
    }
}
