using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace template;

/// <summary>
/// Converts a source dialect to native syntax and reports behaviors that can change in a target dialect.
/// </summary>
public class TemplateCompatibilityAnalyzer : ITemplateCompatibilityAnalyzer
{
    internal TemplateProfileRegistry ProfileRegistry;

    /// <summary>
    /// Initializes a compatibility analyzer with the built-in dialect profiles.
    /// </summary>
    public TemplateCompatibilityAnalyzer()
    {
        ProfileRegistry = new TemplateProfileRegistry();
    }

    /// <summary>
    /// Sets the syntax-profile registry used to resolve source and target profiles.
    /// </summary>
    /// <param name="profileRegistry">The profile registry to use.</param>
    public void SetProfileRegistry(TemplateProfileRegistry profileRegistry)
    {
        if (profileRegistry == null)
        {
            throw new ArgumentNullException("profileRegistry");
        }

        ProfileRegistry = profileRegistry;
    }

    /// <summary>
    /// Analyzes the supplied template for compatibility changes and returns converted text with diagnostics.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <param name="sourceProfileName">The dialect used by the input text.</param>
    /// <param name="targetProfileName">The dialect whose behavior should be checked.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateCompatibilityAnalysis Analyze(string templateText, string sourceProfileName, string targetProfileName)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        TemplateProfile sourceProfile = ProfileRegistry.GetProfile(sourceProfileName);
        TemplateProfile targetProfile = ProfileRegistry.GetProfile(targetProfileName);
        string nativeText = ProfileRegistry.ConvertTextToNative(templateText, sourceProfile.Name);
        TemplateCompatibilityAnalysis analysis = new TemplateCompatibilityAnalysis(sourceProfile.Name, targetProfile.Name, nativeText);
        AddProfileDiagnostics(analysis, templateText, sourceProfile, targetProfile);
        AddDocumentedSourceDiagnostics(analysis, templateText, sourceProfile, nativeText);
        TemplateParseResult parseResult = ProfileRegistry.Parse(templateText, sourceProfile.Name);
        AddParseDiagnostics(analysis, parseResult.Diagnostics);
        AnalyzeDocument(analysis, parseResult.Document, targetProfile);
        return analysis;
    }

    /// <summary>
    /// Analyzes the supplied template for compatibility changes and returns converted text with diagnostics.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="targetProfileName">The dialect whose behavior should be checked.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateCompatibilityAnalysis AnalyzeNative(TemplateDocumentSyntax document, string targetProfileName)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        TemplateProfile targetProfile = ProfileRegistry.GetProfile(targetProfileName);
        TemplateCompatibilityAnalysis analysis = new TemplateCompatibilityAnalysis("native", targetProfile.Name, document.Source.Text);
        AddParseDiagnostics(analysis, document.Diagnostics);
        AnalyzeDocument(analysis, document, targetProfile);
        return analysis;
    }

    internal void AddProfileDiagnostics(TemplateCompatibilityAnalysis analysis, string templateText, TemplateProfile sourceProfile, TemplateProfile targetProfile)
    {
        if (sourceProfile.AutoEscape != targetProfile.AutoEscape)
        {
            analysis.Diagnostics.Add(CreateWarning("COMPAT002", "Target profile changes automatic escaping behavior.", null, "Review raw, safe, and escaped output before migrating."));
        }

        if (sourceProfile.DefaultEscapingMode != targetProfile.DefaultEscapingMode)
        {
            analysis.Diagnostics.Add(CreateWarning("COMPAT003", "Target profile changes the default escaping mode.", null, "Verify rendered output in contexts that contain HTML, JSON, URLs, or CSV."));
        }

        if (UsesTrimMarkers(templateText, sourceProfile))
        {
            if (!targetProfile.SupportsWhitespaceTrim)
            {
                analysis.Diagnostics.Add(CreateWarning("COMPAT004", "Target profile does not support whitespace trim markers.", null, "Remove trim markers or choose a target profile that supports them."));
            }
            else if (sourceProfile.LeftTrimMarker != targetProfile.LeftTrimMarker || sourceProfile.RightTrimMarker != targetProfile.RightTrimMarker)
            {
                analysis.Diagnostics.Add(CreateWarning("COMPAT005", "Target profile uses different whitespace trim markers.", null, "Rewrite trim markers for the target profile."));
            }
        }
    }

    internal void AddDocumentedSourceDiagnostics(TemplateCompatibilityAnalysis analysis, string templateText, TemplateProfile sourceProfile, string nativeText)
    {
        string name = sourceProfile.Name;
        if (name != "scriban" && name != "nunjucks" && name != "go-text-template" && name != "razor-inspired")
        {
            return;
        }

        if (nativeText != templateText || name == "nunjucks")
        {
            analysis.Diagnostics.Add(new TemplateDiagnostic("COMPAT009", TemplateDiagnosticSeverity.Information, "Documented " + name + " constructs have a supported native translation.", null, "Review NativeTemplateText to inspect the translated form."));
        }

        if (HasDocumentedSemanticDifference(templateText, name))
        {
            analysis.Diagnostics.Add(CreateWarning("COMPAT010", GetDocumentedSemanticDifference(name), null, "Verify the translated output with representative model values."));
        }

        string unsupported = FindUnsupportedDocumentedConstruct(templateText, name);
        if (unsupported != "")
        {
            analysis.Diagnostics.Add(CreateWarning("COMPAT011", "Construct '" + unsupported + "' from profile '" + name + "' requires manual rewriting.", null, "Replace the construct with native expressions, registered functions, or supported control blocks."));
        }
    }

    internal bool HasDocumentedSemanticDifference(string templateText, string profileName)
    {
        if (profileName == "scriban")
        {
            return templateText.IndexOf(" | ", StringComparison.Ordinal) >= 0 || templateText.IndexOf("empty?", StringComparison.Ordinal) >= 0;
        }

        if (profileName == "nunjucks")
        {
            return templateText.IndexOf("asyncEach", StringComparison.Ordinal) >= 0 || templateText.IndexOf("asyncAll", StringComparison.Ordinal) >= 0 || templateText.IndexOf("r/", StringComparison.Ordinal) >= 0;
        }

        if (profileName == "go-text-template")
        {
            return templateText.IndexOf('|') >= 0 || templateText.IndexOf("{{range", StringComparison.Ordinal) >= 0 || templateText.IndexOf("{{ range", StringComparison.Ordinal) >= 0;
        }

        return profileName == "razor-inspired" && templateText.IndexOf('@') >= 0;
    }

    internal string GetDocumentedSemanticDifference(string profileName)
    {
        if (profileName == "scriban")
        {
            return "Scriban pipeline binding and special empty members can differ from native filter and truthiness behavior.";
        }

        if (profileName == "nunjucks")
        {
            return "Nunjucks asynchronous tags and JavaScript-oriented values do not have identical native execution semantics.";
        }

        if (profileName == "go-text-template")
        {
            return "Go pipelines pass their input as the last command argument and map ranges use Go ordering rules.";
        }

        return "Razor expressions use C# binding and formatting; the translated subset uses template expression semantics.";
    }

    internal string FindUnsupportedDocumentedConstruct(string templateText, string profileName)
    {
        if (profileName == "scriban")
        {
            return FindFirstContained(templateText, new string[] { "{{ func ", "{{ wrap ", "{{ readonly ", "include_join", "??", "?!" });
        }

        if (profileName == "nunjucks")
        {
            return FindFirstContained(templateText, new string[] { "{% asyncEach", "{% asyncAll", "r/" });
        }

        if (profileName == "go-text-template")
        {
            return FindFirstContained(templateText, new string[] { "{{define ", "{{ define ", "{{block ", "{{ block ", "{{printf ", "{{ printf ", " | printf ", "{{print ", "{{ print " });
        }

        return FindFirstContained(templateText, new string[] { "@await ", "@switch", "@for", "@while", "@do", "@try", "@using", "@functions", "@section", "@inject", "@model", "@layout" });
    }

    internal string FindFirstContained(string text, string[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (text.IndexOf(values[index], StringComparison.Ordinal) >= 0)
            {
                return values[index].Trim();
            }

            index = index + 1;
        }

        return "";
    }

    internal void AnalyzeDocument(TemplateCompatibilityAnalysis analysis, TemplateDocumentSyntax document, TemplateProfile targetProfile)
    {
        AnalyzeNodes(analysis, document.Children, targetProfile);
    }

    internal void AnalyzeNodes(TemplateCompatibilityAnalysis analysis, List<TemplateSyntaxNode> nodes, TemplateProfile targetProfile)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            AnalyzeNode(analysis, nodes[index], targetProfile);
            index = index + 1;
        }
    }

    internal void AnalyzeNode(TemplateCompatibilityAnalysis analysis, TemplateSyntaxNode node, TemplateProfile targetProfile)
    {
        if (node.Kind == TemplateSyntaxKind.Output && node.Output != null)
        {
            AnalyzeExpression(analysis, node.Output.Expression, targetProfile);
        }
        else if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null)
        {
            AnalyzeStatement(analysis, node.Statement, targetProfile);
        }
        else if (node.Kind == TemplateSyntaxKind.Block && node.Block != null)
        {
            AnalyzeBlock(analysis, node.Block, targetProfile);
        }
    }

    internal void AnalyzeStatement(TemplateCompatibilityAnalysis analysis, TemplateStatementSyntax statement, TemplateProfile targetProfile)
    {
        if (!SupportsStatement(targetProfile, statement.Name))
        {
            analysis.Diagnostics.Add(CreateWarning("COMPAT007", "Statement '" + statement.Name + "' may not be supported by target profile '" + targetProfile.Name + "'.", statement.Span, "Rewrite this statement using syntax supported by the target profile."));
        }

        int index = 0;
        while (index < statement.Arguments.Count)
        {
            AnalyzeExpression(analysis, statement.Arguments[index], targetProfile);
            index = index + 1;
        }
    }

    internal void AnalyzeBlock(TemplateCompatibilityAnalysis analysis, TemplateBlockSyntax block, TemplateProfile targetProfile)
    {
        if (!SupportsBlock(targetProfile, block.Name))
        {
            analysis.Diagnostics.Add(CreateWarning("COMPAT008", "Block '" + block.Name + "' may not be supported by target profile '" + targetProfile.Name + "'.", block.Span, "Rewrite this block before migrating."));
        }

        AnalyzeStatement(analysis, block.StartStatement, targetProfile);
        AnalyzeNodes(analysis, block.Children, targetProfile);
        AnalyzeNodes(analysis, block.ElseChildren, targetProfile);
        if (block.EndStatement != null)
        {
            AnalyzeStatement(analysis, block.EndStatement, targetProfile);
        }
    }

    internal void AnalyzeExpression(TemplateCompatibilityAnalysis analysis, TemplateExpressionSyntax expression, TemplateProfile targetProfile)
    {
        if (!targetProfile.AllowsComplexExpressions && IsComplexExpression(expression))
        {
            analysis.Diagnostics.Add(CreateWarning("COMPAT006", "Expression may be too complex for target profile '" + targetProfile.Name + "'.", expression.Span, "Reduce the expression to a simple variable, path, or literal before migrating."));
            return;
        }

        int index = 0;
        while (index < expression.Children.Count)
        {
            AnalyzeExpression(analysis, expression.Children[index], targetProfile);
            index = index + 1;
        }

        index = 0;
        while (index < expression.Arguments.Count)
        {
            AnalyzeExpression(analysis, expression.Arguments[index], targetProfile);
            index = index + 1;
        }

        index = 0;
        while (index < expression.PathSegments.Count)
        {
            TemplatePathSegmentSyntax segment = expression.PathSegments[index];
            if (segment.Expression != null)
            {
                AnalyzeExpression(analysis, segment.Expression, targetProfile);
            }

            index = index + 1;
        }
    }

    internal bool IsComplexExpression(TemplateExpressionSyntax expression)
    {
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Call || expression.ExpressionKind == TemplateExpressionSyntaxKind.Operator || expression.ExpressionKind == TemplateExpressionSyntaxKind.Group)
        {
            return true;
        }

        if (expression.LiteralKind == TemplateLiteralSyntaxKind.Array || expression.LiteralKind == TemplateLiteralSyntaxKind.Object || expression.LiteralKind == TemplateLiteralSyntaxKind.Range)
        {
            return true;
        }

        int index = 0;
        while (index < expression.PathSegments.Count)
        {
            if (expression.PathSegments[index].SegmentKind == TemplatePathSegmentSyntaxKind.Indexer)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool SupportsStatement(TemplateProfile profile, string statementName)
    {
        if (statementName == "else" || statementName == "elseif" || statementName == "when" || statementName == "empty")
        {
            return true;
        }

        if (statementName.Length > 3 && statementName.Substring(0, 3) == "end")
        {
            return true;
        }

        string profileName = profile.Name;
        if (profileName == "native" || !ProfileRegistry.IsBuiltInName(profileName))
        {
            return true;
        }

        if (profileName == "mustache")
        {
            return statementName == "include" || statementName == "extends";
        }

        if (profileName == "handlebars")
        {
            return statementName == "partial" || statementName == "handlebarsPartialBlockBody";
        }

        if (profileName == "liquid")
        {
            return statementName == "assign" || statementName == "set" || statementName == "include" || statementName == "render" || statementName == "break" || statementName == "continue";
        }

        if (profileName == "jinja" || profileName == "django" || profileName == "nunjucks")
        {
            return statementName == "include" || statementName == "import" || statementName == "extends" || statementName == "set" || statementName == "assign" || statementName == "break" || statementName == "continue";
        }

        if (profileName == "scriban")
        {
            return statementName == "set" || statementName == "include" || statementName == "import" || statementName == "return" || statementName == "break" || statementName == "continue";
        }

        if (profileName == "go-text-template")
        {
            return statementName == "set" || statementName == "include" || statementName == "break" || statementName == "continue";
        }

        if (profileName == "razor-inspired")
        {
            return statementName == "set" || statementName == "break" || statementName == "continue";
        }

        return false;
    }

    internal bool SupportsBlock(TemplateProfile profile, string blockName)
    {
        string profileName = profile.Name;
        if (profileName == "native" || !ProfileRegistry.IsBuiltInName(profileName))
        {
            return true;
        }

        if (profileName == "mustache")
        {
            return blockName == "mustacheSection" || blockName == "mustacheInverted" || blockName == "block";
        }

        if (profileName == "handlebars")
        {
            return blockName == "handlebarsEach" || blockName == "handlebarsBlock" || blockName == "handlebarsPartialBlock" || blockName == "handlebarsInlinePartial";
        }

        if (profileName == "liquid")
        {
            return blockName == "if" || blockName == "unless" || blockName == "case" || blockName == "for" || blockName == "capture";
        }

        if (profileName == "jinja" || profileName == "django" || profileName == "nunjucks")
        {
            return blockName == "if" || blockName == "for" || blockName == "with" || blockName == "capture" || blockName == "block" || blockName == "macro" || blockName == "filter" || blockName == "call" || blockName == "jinjaAutoescape";
        }

        if (profileName == "scriban")
        {
            return blockName == "if" || blockName == "for" || blockName == "while" || blockName == "case" || blockName == "capture" || blockName == "with";
        }

        if (profileName == "go-text-template")
        {
            return blockName == "if" || blockName == "for" || blockName == "with";
        }

        if (profileName == "razor-inspired")
        {
            return blockName == "if" || blockName == "for" || blockName == "while" || blockName == "case";
        }

        return false;
    }

    internal void AddParseDiagnostics(TemplateCompatibilityAnalysis analysis, List<TemplateDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            TemplateDiagnostic diagnostic = diagnostics[index];
            analysis.Diagnostics.Add(new TemplateDiagnostic("COMPAT001", diagnostic.Severity, "Converted template has parse diagnostic " + diagnostic.Code + ": " + diagnostic.Message, diagnostic.Span, diagnostic.Hint));
            index = index + 1;
        }
    }

    internal bool UsesTrimMarkers(string templateText, TemplateProfile sourceProfile)
    {
        if (!sourceProfile.SupportsWhitespaceTrim)
        {
            return false;
        }

        if (ContainsTrimMarker(templateText, sourceProfile.OutputStartDelimiter, sourceProfile.LeftTrimMarker))
        {
            return true;
        }

        if (ContainsTrimMarker(templateText, sourceProfile.StatementStartDelimiter, sourceProfile.LeftTrimMarker))
        {
            return true;
        }

        if (ContainsRightTrimMarker(templateText, sourceProfile.OutputEndDelimiter, sourceProfile.RightTrimMarker))
        {
            return true;
        }

        return ContainsRightTrimMarker(templateText, sourceProfile.StatementEndDelimiter, sourceProfile.RightTrimMarker);
    }

    internal bool ContainsTrimMarker(string text, string delimiter, char marker)
    {
        string value = delimiter + marker.ToString();
        return text.IndexOf(value, StringComparison.Ordinal) >= 0;
    }

    internal bool ContainsRightTrimMarker(string text, string delimiter, char marker)
    {
        string value = marker.ToString() + delimiter;
        return text.IndexOf(value, StringComparison.Ordinal) >= 0;
    }

    internal string ConvertToNative(string templateText, string profileName)
    {
        return ProfileRegistry.ConvertTextToNative(templateText, profileName);
    }

    internal TemplateProfile CreateProfile(string name)
    {
        return ProfileRegistry.GetProfile(name);
    }

    internal TemplateDiagnostic CreateWarning(string code, string message, TemplateSourceSpan? span, string hint)
    {
        return new TemplateDiagnostic(code, TemplateDiagnosticSeverity.Warning, message, span, hint);
    }
}
