using System;
using System.Collections.Generic;

namespace sql;

internal class SqlDialectTranspilation
{
    internal static SqlTranspileResult TranspileScript(SqlScript script, SqlTranspileOptions options)
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
        SqlFormatOptions formatOptions = CreateFormatOptions(options);
        SqlGenerateOptions generateOptions = CreateGenerateOptions(options, formatOptions);
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkScript(script);
        List<SqlTranspileMapping> mappings = CollectSafeMappings(script, walker, options.WriteDialect, generateOptions);

        ISqlDialectPolicy targetPolicy = new SqlDialectPolicy(options.WriteDialect);
        SqlCompatibilityResult compatibility = SqlCompatibilityAnalysis.Analyze(script, targetPolicy);
        List<SqlUnsupportedSyntax> unsupported = new List<SqlUnsupportedSyntax>();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        AddLossyIssues(script, compatibility, mappings, unsupported, diagnostics);

        string sql = SqlGenerator.GenerateScriptForTranspilation(script, generateOptions);
        SqlParseOptions parseOptions = new SqlParseOptions();
        parseOptions.Dialect = options.WriteDialect;
        parseOptions.PreserveTokens = true;
        parseOptions.PreserveTrivia = formatOptions.PreserveComments;
        SqlParseResult parsed = SqlStatementParser.ParseScript(sql, parseOptions);
        diagnostics.Merge(parsed.Diagnostics);
        PreserveUnsupportedSyntax(parsed.Script, unsupported);
        bool isLossless = unsupported.Count == 0 && !parsed.Diagnostics.HasErrors();
        return new SqlTranspileResult(script, parsed.Script, sql, options.ReadDialect, options.WriteDialect, mappings.ToArray(), unsupported.ToArray(), diagnostics, isLossless);
    }

    internal static SqlFormatOptions CreateFormatOptions(SqlTranspileOptions options)
    {
        SqlFormatOptions formatOptions = options.FormatOptions.Copy();
        formatOptions.KeywordDialect = options.WriteDialect;
        formatOptions.ReadDialect = options.ReadDialect;
        formatOptions.WriteDialect = options.WriteDialect;
        return formatOptions;
    }

    internal static SqlGenerateOptions CreateGenerateOptions(SqlTranspileOptions options, SqlFormatOptions formatOptions)
    {
        SqlGenerateOptions generateOptions = new SqlGenerateOptions();
        generateOptions.FormatOptions = formatOptions;
        generateOptions.WriteDialect = options.WriteDialect;
        generateOptions.IncludeSemicolons = options.IncludeSemicolons;
        generateOptions.PreserveSourceText = false;
        return generateOptions;
    }

    internal static List<SqlTranspileMapping> CollectSafeMappings(SqlScript script, SqlAstWalker walker, SqlDialectOptions writeDialect, SqlGenerateOptions generateOptions)
    {
        List<SqlTranspileMapping> mappings = new List<SqlTranspileMapping>();
        SqlGenerator generator = SqlGenerator.CreateForTranspilation(generateOptions);
        int nameIndex = 0;
        while (nameIndex < walker.Names.Count)
        {
            SqlName name = walker.Names[nameIndex];
            int partIndex = 0;
            while (partIndex < name.Parts.Length)
            {
                SqlNamePart part = name.Parts[partIndex];
                if (part.QuoteStyle != SqlNameQuoteStyle.None && !generator.IsNameQuoteSupported(part.QuoteStyle))
                {
                    string targetText = generator.GetFormattedNamePart(part);
                    if (IsSafeIdentifierMapping(part, targetText, generator, writeDialect))
                    {
                        AddMappingIfNew(mappings, new SqlTranspileMapping(SqlTranspileMappingKind.IdentifierQuoting, part.SourceText, targetText, part.Span, true));
                    }
                }

                partIndex = partIndex + 1;
            }

            nameIndex = nameIndex + 1;
        }

        int expressionIndex = 0;
        while (expressionIndex < walker.Expressions.Count)
        {
            SqlExpression expression = walker.Expressions[expressionIndex];
            if (expression.CastData != null && expression.CastData.SyntaxKind == SqlCastSyntaxKind.PostgreSql && !SupportsPostgreSqlCastSyntax(writeDialect.Kind))
            {
                string sourceText = GetSourceText(script, expression.Span);
                if (sourceText.Length == 0)
                {
                    sourceText = SqlGenerator.GenerateExpression(expression);
                }

                string targetText = SqlGenerator.GenerateExpressionForTranspilation(expression, generateOptions);
                AddMappingIfNew(mappings, new SqlTranspileMapping(SqlTranspileMappingKind.CastSyntax, sourceText, targetText, expression.Span, true));
            }

            expressionIndex = expressionIndex + 1;
        }

        return mappings;
    }

    internal static bool IsSafeIdentifierMapping(SqlNamePart part, string targetText, SqlGenerator generator, SqlDialectOptions writeDialect)
    {
        if (targetText == part.SourceText)
        {
            return false;
        }

        if (generator.GetPreferredNameQuoteStyle() != SqlNameQuoteStyle.None)
        {
            return true;
        }

        return generator.IsBareIdentifierText(targetText) && !writeDialect.IsKeyword(targetText);
    }

    internal static bool SupportsPostgreSqlCastSyntax(SqlDialectKind kind)
    {
        return kind == SqlDialectKind.Generic || kind == SqlDialectKind.PostgreSql || kind == SqlDialectKind.DuckDb;
    }

    internal static void AddMappingIfNew(List<SqlTranspileMapping> mappings, SqlTranspileMapping addition)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            SqlTranspileMapping existing = mappings[index];
            if (existing.Kind == addition.Kind && existing.SourceSpan.Start == addition.SourceSpan.Start && existing.SourceSpan.Length == addition.SourceSpan.Length && existing.TargetText == addition.TargetText)
            {
                return;
            }

            index = index + 1;
        }

        mappings.Add(addition);
    }

    internal static void AddLossyIssues(SqlScript script, SqlCompatibilityResult compatibility, List<SqlTranspileMapping> mappings, List<SqlUnsupportedSyntax> unsupported, SqlDiagnosticBag diagnostics)
    {
        int index = 0;
        while (index < compatibility.Issues.Length)
        {
            SqlCompatibilityIssue issue = compatibility.Issues[index];
            if (!IsSafelyMappedIssue(issue, mappings))
            {
                string sourceText = GetSourceText(script, issue.Span);
                SqlToken[] tokens = GetTokens(script, issue.Span);
                string description = issue.Feature + " has no lossless mapping to the target dialect.";
                unsupported.Add(new SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind.DialectFeature, description, sourceText, issue.Span, tokens));
                diagnostics.Add(issue.Diagnostic);
            }

            index = index + 1;
        }
    }

    internal static bool IsSafelyMappedIssue(SqlCompatibilityIssue issue, List<SqlTranspileMapping> mappings)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            SqlTranspileMapping mapping = mappings[index];
            bool sameSpan = mapping.SourceSpan.Start == issue.Span.Start && mapping.SourceSpan.Length == issue.Span.Length;
            if (sameSpan && mapping.Kind == SqlTranspileMappingKind.IdentifierQuoting && issue.Kind == SqlCompatibilityIssueKind.UnsupportedQuoting)
            {
                return true;
            }

            if (sameSpan && mapping.Kind == SqlTranspileMappingKind.CastSyntax && issue.Kind == SqlCompatibilityIssueKind.UnsupportedExpression && issue.Feature.Contains("cast", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string GetSourceText(SqlScript script, SqlTextSpan span)
    {
        if (span.Length == 0 || span.Start >= script.SourceText.Length || span.Start > script.SourceText.Length - span.Length)
        {
            return "";
        }

        return script.SourceText.Substring(span.Start, span.Length);
    }

    internal static SqlToken[] GetTokens(SqlScript script, SqlTextSpan span)
    {
        List<SqlToken> tokens = new List<SqlToken>();
        int index = 0;
        while (index < script.Tokens.Length)
        {
            SqlToken token = script.Tokens[index];
            if (token.Kind != SqlTokenKind.EndOfFile && (span.ContainsSpan(token.Span) || span.OverlapsWith(token.Span)))
            {
                tokens.Add(token);
            }

            index = index + 1;
        }

        return tokens.ToArray();
    }

    internal static void PreserveUnsupportedSyntax(SqlScript script, List<SqlUnsupportedSyntax> unsupported)
    {
        List<SqlUnsupportedSyntax> combined = new List<SqlUnsupportedSyntax>();
        SqlNormalizationTransform.AddUnsupportedSyntax(combined, script.UnsupportedSyntax);
        SqlNormalizationTransform.AddUnsupportedSyntax(combined, unsupported.ToArray());
        script.UnsupportedSyntax = combined.ToArray();
    }
}
