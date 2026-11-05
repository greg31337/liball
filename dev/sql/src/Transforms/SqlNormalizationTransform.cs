using System;
using System.Collections.Generic;

namespace sql;

internal class SqlNormalizationTransform
{
    internal static SqlNormalizationResult NormalizeScript(SqlScript script, SqlNormalizationOptions options)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        SqlFormatOptions formatOptions = CreateFormatOptions(options.Dialect, options.KeywordCase, options.IdentifierCase, options.Mode, options.PreserveComments);
        SqlNormalizationWorkResult work = TransformScript(script, options.Dialect, formatOptions, options.IncludeSemicolons);
        return new SqlNormalizationResult(script, work.Script, work.Text, work.Markers, work.Diagnostics);
    }

    internal static SqlCanonicalizationResult CanonicalizeScript(SqlScript script, SqlCanonicalizationOptions options)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        SqlFormatOptions formatOptions = CreateFormatOptions(options.Dialect, options.KeywordCase, options.IdentifierCase, SqlFormatMode.Minimal, options.PreserveComments);
        SqlNormalizationWorkResult work = TransformScript(script, options.Dialect, formatOptions, options.IncludeSemicolons);
        return new SqlCanonicalizationResult(script, work.Script, work.Text, work.Markers, work.Diagnostics);
    }

    internal static SqlFormatOptions CreateFormatOptions(SqlDialectOptions dialect, SqlKeywordCase keywordCase, SqlIdentifierCase identifierCase, SqlFormatMode mode, bool preserveComments)
    {
        SqlFormatOptions formatOptions = new SqlFormatOptions();
        formatOptions.KeywordCase = keywordCase;
        formatOptions.IdentifierCase = identifierCase;
        formatOptions.Mode = mode;
        formatOptions.PreserveComments = preserveComments;
        formatOptions.KeywordDialect = dialect;
        formatOptions.ReadDialect = dialect;
        formatOptions.WriteDialect = dialect;
        return formatOptions;
    }

    internal static SqlNormalizationWorkResult TransformScript(SqlScript script, SqlDialectOptions dialect, SqlFormatOptions formatOptions, bool includeSemicolons)
    {
        List<SqlNormalizationMarker> markers = CollectInputMarkers(script);
        SqlGenerateOptions generateOptions = new SqlGenerateOptions();
        generateOptions.FormatOptions = formatOptions;
        generateOptions.WriteDialect = dialect;
        generateOptions.IncludeSemicolons = includeSemicolons;
        generateOptions.PreserveSourceText = false;

        string text = SqlGenerator.GenerateScript(script, generateOptions);
        SqlParseOptions parseOptions = new SqlParseOptions();
        parseOptions.Dialect = dialect;
        parseOptions.PreserveTokens = true;
        parseOptions.PreserveTrivia = formatOptions.PreserveComments;
        SqlParseResult parsed = SqlStatementParser.ParseScript(text, parseOptions);
        AddDiagnosticMarkers(markers, parsed.Diagnostics, text);
        PreserveUnsupportedSyntax(parsed.Script, markers);
        return new SqlNormalizationWorkResult(parsed.Script, text, markers.ToArray(), parsed.Diagnostics);
    }

    internal static List<SqlNormalizationMarker> CollectInputMarkers(SqlScript script)
    {
        List<SqlNormalizationMarker> markers = new List<SqlNormalizationMarker>();
        List<SqlUnsupportedSyntax> syntaxItems = new List<SqlUnsupportedSyntax>();
        AddUnsupportedSyntax(syntaxItems, script.UnsupportedSyntax);

        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkScript(script);
        AddUnsupportedSyntax(syntaxItems, walker.UnsupportedSyntax.ToArray());

        int index = 0;
        while (index < syntaxItems.Count)
        {
            SqlUnsupportedSyntax syntax = syntaxItems[index];
            markers.Add(new SqlNormalizationMarker(SqlNormalizationMarkerKind.UnsupportedSyntax, syntax.Description, syntax.SourceText, syntax.Span, syntax));
            index = index + 1;
        }

        index = 0;
        while (index < walker.Statements.Count)
        {
            SqlStatement statement = walker.Statements[index];
            if (statement.Kind == SqlStatementKind.Unknown && statement.UnsupportedSyntax.Length == 0)
            {
                markers.Add(new SqlNormalizationMarker(SqlNormalizationMarkerKind.UnknownStatement, "Unknown statement cannot be normalized losslessly.", statement.SourceText, statement.Span, null));
            }

            index = index + 1;
        }

        index = 0;
        while (index < walker.Expressions.Count)
        {
            SqlExpression expression = walker.Expressions[index];
            if (expression.Kind == SqlExpressionKind.Unknown && expression.Tokens.Length > 0)
            {
                markers.Add(new SqlNormalizationMarker(SqlNormalizationMarkerKind.UnknownExpression, "Unknown expression cannot be normalized losslessly.", GetExpressionSourceText(expression), expression.Span, null));
            }

            index = index + 1;
        }

        return markers;
    }

    internal static void AddUnsupportedSyntax(List<SqlUnsupportedSyntax> items, SqlUnsupportedSyntax[] additions)
    {
        int additionIndex = 0;
        while (additionIndex < additions.Length)
        {
            SqlUnsupportedSyntax addition = additions[additionIndex];
            bool exists = false;
            int itemIndex = 0;
            while (itemIndex < items.Count)
            {
                SqlUnsupportedSyntax item = items[itemIndex];
                if (item == addition || (item.Kind == addition.Kind && item.SourceText == addition.SourceText && item.Span.Start == addition.Span.Start && item.Span.Length == addition.Span.Length))
                {
                    exists = true;
                }

                itemIndex = itemIndex + 1;
            }

            if (!exists)
            {
                items.Add(addition);
            }

            additionIndex = additionIndex + 1;
        }
    }

    internal static string GetExpressionSourceText(SqlExpression expression)
    {
        if (expression.Tokens.Length == 0)
        {
            return "";
        }

        string text = "";
        int index = 0;
        while (index < expression.Tokens.Length)
        {
            text = text + expression.Tokens[index].Text;
            index = index + 1;
        }

        return text;
    }

    internal static void AddDiagnosticMarkers(List<SqlNormalizationMarker> markers, SqlDiagnosticBag diagnostics, string text)
    {
        int index = 0;
        while (index < diagnostics.Diagnostics.Count)
        {
            SqlDiagnostic diagnostic = diagnostics.Diagnostics[index];
            if (diagnostic.Severity == SqlDiagnosticSeverity.Error)
            {
                string sourceText = GetSpanText(text, diagnostic.Span);
                markers.Add(new SqlNormalizationMarker(SqlNormalizationMarkerKind.ReparseDiagnostic, diagnostic.Message, sourceText, diagnostic.Span, null));
            }

            index = index + 1;
        }
    }

    internal static string GetSpanText(string text, SqlTextSpan span)
    {
        if (span.Start < 0 || span.Start >= text.Length || span.Length == 0)
        {
            return "";
        }

        int length = span.Length;
        if (span.Start > text.Length - length)
        {
            length = text.Length - span.Start;
        }

        return text.Substring(span.Start, length);
    }

    internal static void PreserveUnsupportedSyntax(SqlScript script, List<SqlNormalizationMarker> markers)
    {
        List<SqlUnsupportedSyntax> preserved = new List<SqlUnsupportedSyntax>();
        AddUnsupportedSyntax(preserved, script.UnsupportedSyntax);
        int index = 0;
        while (index < markers.Count)
        {
            SqlUnsupportedSyntax? syntax = markers[index].UnsupportedSyntax;
            if (syntax != null)
            {
                AddUnsupportedSyntax(preserved, new SqlUnsupportedSyntax[] { syntax });
            }

            index = index + 1;
        }

        script.UnsupportedSyntax = preserved.ToArray();
    }
}
