using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Performs static template analysis for parse, evaluation, composition, security, flow, reuse, deprecation, and compatibility issues.
/// </summary>
public class TemplateLinter : ITemplateLinter
{
    internal TemplateOptions Options;
    internal TemplateRegistry Registry;
    internal TemplateProfileRegistry ProfileRegistry;
    internal ITemplateDiagnosticSink? DiagnosticSink;
    internal List<string> MacroNames;
    internal List<string> CalledNames;
    internal TemplateExpressionEvaluator KnownCallables;

    /// <summary>
    /// Initializes a linter with native options and an empty runtime registry.
    /// </summary>
    public TemplateLinter()
    {
        Options = new TemplateOptions();
        Registry = new TemplateRegistry();
        ProfileRegistry = new TemplateProfileRegistry();
        DiagnosticSink = null;
        MacroNames = new List<string>();
        CalledNames = new List<string>();
        KnownCallables = CreateKnownCallables();
    }

    /// <summary>
    /// Configures lint options and registered runtime components.
    /// </summary>
    /// <param name="options">The options that control linting.</param>
    /// <param name="registry">The registry that supplies known callables and dependencies.</param>
    public void Configure(TemplateOptions options, TemplateRegistry registry)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        Options = options.Clone();
        Registry = registry;
        KnownCallables = CreateKnownCallables();
    }

    /// <summary>
    /// Sets the syntax-profile registry used for parsing.
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
    /// Sets the sink that receives lint diagnostics.
    /// </summary>
    /// <param name="diagnosticSink">The sink that receives diagnostics.</param>
    public void SetDiagnosticSink(ITemplateDiagnosticSink diagnosticSink)
    {
        if (diagnosticSink == null)
        {
            throw new ArgumentNullException("diagnosticSink");
        }

        DiagnosticSink = diagnosticSink;
    }

    /// <summary>
    /// Lints template text using the configured syntax profile.
    /// </summary>
    /// <param name="templateText">The template source text to lint.</param>
    /// <returns>The ordered diagnostics.</returns>
    public List<TemplateDiagnostic> Lint(string templateText)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        TemplateSource source = TemplateSource.FromString(templateText);
        TemplateParseResult parseResult = ProfileRegistry.ParseSource(source, Options.SyntaxProfile);
        return LintDocument(parseResult.Document);
    }

    /// <summary>
    /// Lints an already parsed document.
    /// </summary>
    /// <param name="document">The parsed template document to lint.</param>
    /// <returns>The ordered diagnostics.</returns>
    public List<TemplateDiagnostic> LintDocument(TemplateDocumentSyntax document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<TemplateDiagnostic> diagnostics = new List<TemplateDiagnostic>();
        AddDiagnostics(diagnostics, document.Diagnostics);
        TemplateSecurityAuditor auditor = new TemplateSecurityAuditor();
        AddDiagnostics(diagnostics, auditor.Audit(document, Options));
        MacroNames = new List<string>();
        CalledNames = new List<string>();
        CollectReusableNames(document.Children);
        LintNodes(diagnostics, document.Children);
        AddUnusedMacroDiagnostics(diagnostics, document.Children);
        ReportDiagnostics(diagnostics);
        return diagnostics;
    }

    internal TemplateExpressionEvaluator CreateKnownCallables()
    {
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        TemplateScopeStack scope = TemplateScopeStack.Create(TemplateValue.FromMap(values));
        return new TemplateExpressionEvaluator(scope, Options, new TemplateMemberResolver(), Registry);
    }

    internal void CollectReusableNames(List<TemplateSyntaxNode> nodes)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            TemplateSyntaxNode node = nodes[index];
            if (node.Block != null)
            {
                if (node.Block.Name == "macro")
                {
                    string macroName = GetDeclaredName(node.Block.StartStatement);
                    if (macroName != "" && !MacroNames.Contains(macroName))
                    {
                        MacroNames.Add(macroName);
                    }
                }

                CollectReusableNames(node.Block.Children);
                CollectReusableNames(node.Block.ElseChildren);
            }

            if (node.Output != null)
            {
                TemplateExpressionSyntax? expression = node.Output.Expression;
                if (expression != null)
                {
                    CollectCalledNames(expression);
                }
            }

            if (node.Expression != null)
            {
                CollectCalledNames(node.Expression);
            }

            if (node.Statement != null)
            {
                CollectStatementCalls(node.Statement);
            }

            index = index + 1;
        }
    }

    internal void CollectStatementCalls(TemplateStatementSyntax statement)
    {
        int index = 0;
        while (index < statement.Arguments.Count)
        {
            CollectCalledNames(statement.Arguments[index]);
            index = index + 1;
        }
    }

    internal void CollectCalledNames(TemplateExpressionSyntax expression)
    {
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Call && !CalledNames.Contains(expression.Name))
        {
            CalledNames.Add(expression.Name);
        }

        int index = 0;
        while (index < expression.Children.Count)
        {
            CollectCalledNames(expression.Children[index]);
            index = index + 1;
        }

        index = 0;
        while (index < expression.Arguments.Count)
        {
            CollectCalledNames(expression.Arguments[index]);
            index = index + 1;
        }

        index = 0;
        while (index < expression.NamedArguments.Count)
        {
            CollectCalledNames(expression.NamedArguments[index].Value);
            index = index + 1;
        }

        index = 0;
        while (index < expression.HashArguments.Count)
        {
            CollectCalledNames(expression.HashArguments[index].Value);
            index = index + 1;
        }

        index = 0;
        while (index < expression.PathSegments.Count)
        {
            TemplateExpressionSyntax? pathExpression = expression.PathSegments[index].Expression;
            if (pathExpression != null)
            {
                CollectCalledNames(pathExpression);
            }

            index = index + 1;
        }
    }

    internal void LintNodes(List<TemplateDiagnostic> diagnostics, List<TemplateSyntaxNode> nodes)
    {
        bool unreachable = false;
        int index = 0;
        while (index < nodes.Count)
        {
            TemplateSyntaxNode node = nodes[index];
            if (unreachable)
            {
                AddDiagnostic(diagnostics, "FLOW002", TemplateDiagnosticSeverity.Warning, "Template content is unreachable after flow control leaves this block.", node.Span, "Remove the content or move it before the return, break, or continue statement.");
            }

            LintNode(diagnostics, node);
            if (node.Statement != null && (node.Statement.Name == "return" || node.Statement.Name == "break" || node.Statement.Name == "continue"))
            {
                unreachable = true;
            }

            index = index + 1;
        }
    }

    internal void LintNode(List<TemplateDiagnostic> diagnostics, TemplateSyntaxNode node)
    {
        if (node.Output != null)
        {
            LintExpression(diagnostics, node.Output.Expression);
        }
        else if (node.Statement != null)
        {
            LintStatement(diagnostics, node.Statement);
        }
        else if (node.Block != null)
        {
            LintBlock(diagnostics, node.Block);
        }
        else if (node.Expression != null)
        {
            LintExpression(diagnostics, node.Expression);
        }
    }

    internal void LintStatement(List<TemplateDiagnostic> diagnostics, TemplateStatementSyntax statement)
    {
        int index = 0;
        while (index < statement.Arguments.Count)
        {
            LintExpression(diagnostics, statement.Arguments[index]);
            index = index + 1;
        }

        if (statement.Name == "include" || statement.Name == "render" || statement.Name == "partial" || statement.Name == "import" || statement.Name == "extends")
        {
            LintComposition(diagnostics, statement);
        }

        if (statement.Name == "include" && Options.SyntaxProfile == "liquid")
        {
            AddDiagnostic(diagnostics, "DEPRECATION001", TemplateDiagnosticSeverity.Warning, "Liquid include is deprecated in favor of isolated render.", statement.Span, "Replace include with render and pass required values explicitly.");
        }

        if (statement.Name == "include" && Options.CompatibilityMode == TemplateCompatibilityMode.Pragmatic)
        {
            AddDiagnostic(diagnostics, "COMPAT009", TemplateDiagnosticSeverity.Warning, "Include context behavior is ambiguous across template profiles.", statement.Span, "Choose a strict profile or use render with explicit arguments.");
        }
    }

    internal void LintComposition(List<TemplateDiagnostic> diagnostics, TemplateStatementSyntax statement)
    {
        if (statement.Arguments.Count == 0)
        {
            return;
        }

        string name = GetLiteralName(statement.Arguments[0]);
        if (name == "")
        {
            AddDiagnostic(diagnostics, "COMPOSE002", TemplateDiagnosticSeverity.Information, "Dynamic composition name cannot be validated statically.", statement.Arguments[0].Span, "Use a string literal when static dependency validation is required.");
            return;
        }

        bool missing = false;
        if (statement.Name == "partial")
        {
            missing = !Registry.Partials.ContainsKey(Registry.NormalizeTemplateName(name));
        }
        else if (!HasPotentialTemplate(name))
        {
            missing = true;
        }

        if (missing)
        {
            AddDiagnostic(diagnostics, "COMPOSE001", TemplateDiagnosticSeverity.Warning, "Template dependency '" + name + "' was not found.", statement.Span, "Register the dependency or correct its name.");
        }
    }

    internal bool HasPotentialTemplate(string name)
    {
        if (Registry.MemoryLoader != null && Registry.MemoryLoader.ContainsTemplate(Registry.NormalizeTemplateName(name)))
        {
            return true;
        }

        int knownLoaderCount = 0;
        int index = 0;
        while (index < Registry.TemplateLoaders.Count)
        {
            if (Registry.TemplateLoaders[index] == Registry.MemoryLoader)
            {
                knownLoaderCount = knownLoaderCount + 1;
            }

            index = index + 1;
        }

        return Registry.TemplateLoaders.Count > knownLoaderCount;
    }

    internal void LintBlock(List<TemplateDiagnostic> diagnostics, TemplateBlockSyntax block)
    {
        LintStatement(diagnostics, block.StartStatement);
        if ((block.Name == "if" || block.Name == "unless") && block.StartStatement.Arguments.Count > 0)
        {
            TemplateExpressionSyntax condition = block.StartStatement.Arguments[0];
            if (condition.ExpressionKind == TemplateExpressionSyntaxKind.Literal && condition.LiteralKind == TemplateLiteralSyntaxKind.Boolean)
            {
                bool conditionValue = condition.Text == "true";
                if (block.Name == "unless")
                {
                    conditionValue = !conditionValue;
                }

                if (conditionValue && block.ElseChildren.Count > 0)
                {
                    AddDiagnostic(diagnostics, "FLOW001", TemplateDiagnosticSeverity.Warning, "Alternate branch is unreachable because the condition is always true.", block.ElseChildren[0].Span, "Remove the branch or use a non-constant condition.");
                }
                else if (!conditionValue && block.Children.Count > 0)
                {
                    AddDiagnostic(diagnostics, "FLOW001", TemplateDiagnosticSeverity.Warning, "Block body is unreachable because the condition is always false.", block.Children[0].Span, "Remove the body or use a non-constant condition.");
                }
            }
        }

        LintNodes(diagnostics, block.Children);
        LintNodes(diagnostics, block.ElseChildren);
    }

    internal void LintExpression(List<TemplateDiagnostic> diagnostics, TemplateExpressionSyntax expression)
    {
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Call && !IsKnownCallable(expression))
        {
            AddDiagnostic(diagnostics, "EVAL001", TemplateDiagnosticSeverity.Warning, "Unknown template callable '" + expression.Name + "'.", expression.Span, "Register the callable or correct its name.");
        }

        int index = 0;
        while (index < expression.Children.Count)
        {
            LintExpression(diagnostics, expression.Children[index]);
            index = index + 1;
        }

        index = 0;
        while (index < expression.Arguments.Count)
        {
            LintExpression(diagnostics, expression.Arguments[index]);
            index = index + 1;
        }

        index = 0;
        while (index < expression.NamedArguments.Count)
        {
            LintExpression(diagnostics, expression.NamedArguments[index].Value);
            index = index + 1;
        }

        index = 0;
        while (index < expression.HashArguments.Count)
        {
            LintExpression(diagnostics, expression.HashArguments[index].Value);
            index = index + 1;
        }

        index = 0;
        while (index < expression.PathSegments.Count)
        {
            TemplateExpressionSyntax? pathExpression = expression.PathSegments[index].Expression;
            if (pathExpression != null)
            {
                LintExpression(diagnostics, pathExpression);
            }

            index = index + 1;
        }
    }

    internal bool IsKnownCallable(TemplateExpressionSyntax expression)
    {
        string name = expression.Name;
        if (name.Length > 4 && name.Substring(0, 4) == "not ")
        {
            name = name.Substring(4);
        }

        if (MacroNames.Contains(name))
        {
            return true;
        }

        if (expression.CallKind == TemplateExpressionCallKind.Filter)
        {
            return KnownCallables.Filters.ContainsKey(name) || KnownCallables.AsyncFilters.ContainsKey(name);
        }

        if (expression.CallKind == TemplateExpressionCallKind.Test)
        {
            return KnownCallables.Tests.ContainsKey(name) || KnownCallables.AsyncTests.ContainsKey(name);
        }

        return KnownCallables.Functions.ContainsKey(name) || KnownCallables.AsyncFunctions.ContainsKey(name);
    }

    internal void AddUnusedMacroDiagnostics(List<TemplateDiagnostic> diagnostics, List<TemplateSyntaxNode> nodes)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            TemplateSyntaxNode node = nodes[index];
            if (node.Block != null)
            {
                if (node.Block.Name == "macro")
                {
                    string name = GetDeclaredName(node.Block.StartStatement);
                    if (name != "" && !CalledNames.Contains(name))
                    {
                        AddDiagnostic(diagnostics, "UNUSED001", TemplateDiagnosticSeverity.Warning, "Macro block '" + name + "' is never used.", node.Block.StartStatement.Span, "Remove the macro or call it from the template.");
                    }
                }

                AddUnusedMacroDiagnostics(diagnostics, node.Block.Children);
                AddUnusedMacroDiagnostics(diagnostics, node.Block.ElseChildren);
            }

            index = index + 1;
        }
    }

    internal string GetDeclaredName(TemplateStatementSyntax statement)
    {
        if (statement.Arguments.Count == 0)
        {
            return "";
        }

        return statement.Arguments[0].Text;
    }

    internal string GetLiteralName(TemplateExpressionSyntax expression)
    {
        if (expression.ExpressionKind != TemplateExpressionSyntaxKind.Literal || expression.LiteralKind != TemplateLiteralSyntaxKind.String)
        {
            return "";
        }

        string text = expression.Text;
        if (text.Length >= 2)
        {
            char first = text[0];
            char last = text[text.Length - 1];
            if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
            {
                return text.Substring(1, text.Length - 2);
            }
        }

        return text;
    }

    internal void AddDiagnostic(List<TemplateDiagnostic> diagnostics, string code, TemplateDiagnosticSeverity severity, string message, TemplateSourceSpan? span, string hint)
    {
        TemplateDiagnostic diagnostic = new TemplateDiagnostic(code, severity, message, span, hint);
        diagnostics.Add(diagnostic);
    }

    internal void AddDiagnostics(List<TemplateDiagnostic> diagnostics, List<TemplateDiagnostic> additions)
    {
        int index = 0;
        while (index < additions.Count)
        {
            diagnostics.Add(additions[index]);
            index = index + 1;
        }
    }

    internal void ReportDiagnostics(List<TemplateDiagnostic> diagnostics)
    {
        if (DiagnosticSink == null)
        {
            return;
        }

        int index = 0;
        while (index < diagnostics.Count)
        {
            DiagnosticSink.Report(diagnostics[index]);
            index = index + 1;
        }
    }
}
