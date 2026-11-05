using sql;

// Formatting parses the local query; it does not open a database connection.
string input = File.ReadAllText(SampleFiles.InputPath("sales-report.sql"));
var parser = new SqlParserService();
var parsed = parser.ParseScript(input, new SqlParseOptions());
if (parsed.Diagnostics.Diagnostics.Any(d => d.Severity == SqlDiagnosticSeverity.Error))
{
    throw new InvalidOperationException("SQL parsing failed.");
}

var formatter = new SqlFormatterService(parser);
string output = formatter.FormatScript(parsed.Script, new SqlFormatOptions
{
    KeywordCase = SqlKeywordCase.Upper,
    TargetLineWidth = 72
});

Console.WriteLine("Formatted sales query:");
Console.WriteLine(output);
var reparsed = parser.ParseScript(output, new SqlParseOptions());
Console.WriteLine($"\nDiagnostics after formatting: {reparsed.Diagnostics.Diagnostics.Count}");
if (reparsed.Diagnostics.Diagnostics.Any(d => d.Severity == SqlDiagnosticSeverity.Error))
{
    throw new InvalidOperationException("Formatted SQL is invalid.");
}

Console.WriteLine("The query is parsed and formatted; no SQL is executed.");
