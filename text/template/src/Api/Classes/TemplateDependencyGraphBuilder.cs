using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Builds a static dependency graph by following literal includes, partials, imports, and parent templates.
/// </summary>
public class TemplateDependencyGraphBuilder : ITemplateDependencyGraphBuilder
{
    /// <summary>
    /// Specifies the registry for this dependency graph builder.
    /// </summary>
    internal TemplateRegistry Registry;
    /// <summary>
    /// Specifies the options for this dependency graph builder.
    /// </summary>
    internal TemplateOptions Options;
    internal TemplateProfileRegistry ProfileRegistry;

    /// <summary>
    /// Initializes a new template dependency graph builder and validates any supplied values.
    /// </summary>
    public TemplateDependencyGraphBuilder()
    {
        Registry = new TemplateRegistry();
        Options = new TemplateOptions();
        ProfileRegistry = new TemplateProfileRegistry();
    }

    /// <summary>
    /// Sets the syntax-profile registry used to parse roots and recursively loaded dependencies.
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
    /// Configures the options and registry used to build dependency graphs.
    /// </summary>
    /// <param name="options">The options that control parsing and compatibility conversion.</param>
    /// <param name="registry">The registry that supplies named templates.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
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

        Options = options;
        Registry = registry;
    }

    /// <summary>
    /// Creates a dependency graph builder that uses the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies callables and named templates.</param>
    /// <returns>The configured dependency graph builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateDependencyGraphBuilder Create(TemplateRegistry registry)
    {
        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        TemplateDependencyGraphBuilder builder = new TemplateDependencyGraphBuilder();
        builder.Registry = registry;
        return builder;
    }

    /// <summary>
    /// Creates a dependency graph builder that uses the supplied options and registry.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="registry">The registry that supplies callables and named templates.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The configured dependency graph builder.</returns>
    internal static TemplateDependencyGraphBuilder Create(TemplateOptions options, TemplateRegistry registry)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        TemplateDependencyGraphBuilder builder = new TemplateDependencyGraphBuilder();
        builder.Options = options;
        builder.Registry = registry;
        return builder;
    }

    /// <summary>
    /// Parses template text with the selected registered profile and follows its literal dependencies to build a graph.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateDependencyGraph Build(string templateText)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        TemplateSource source = TemplateSource.FromString(templateText);
        TemplateParseResult parseResult = ProfileRegistry.ParseSource(source, Options.SyntaxProfile);
        return BuildDocument(parseResult.Document);
    }

    /// <summary>
    /// Follows literal dependencies from a parsed document to build a graph.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateDependencyGraph BuildDocument(TemplateDocumentSyntax document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        TemplateDependencyGraph graph = new TemplateDependencyGraph();
        string rootName = document.Source.GetDisplayName();
        TemplateDependencyGraphNode root = graph.AddNode(TemplateDependencyGraphNodeKind.Root, rootName, document.Source);
        AddParseDiagnostics(graph, document.Diagnostics);
        AddDocumentDependencies(graph, root.Id, document);
        return graph;
    }

    internal void AddDocumentDependencies(TemplateDependencyGraph graph, string fromId, TemplateDocumentSyntax document)
    {
        AddNodesDependencies(graph, fromId, document.Children);
    }

    internal void AddNodesDependencies(TemplateDependencyGraph graph, string fromId, List<TemplateSyntaxNode> nodes)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            AddNodeDependencies(graph, fromId, nodes[index]);
            index = index + 1;
        }
    }

    internal void AddNodeDependencies(TemplateDependencyGraph graph, string fromId, TemplateSyntaxNode node)
    {
        if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null)
        {
            AddStatementDependency(graph, fromId, node.Statement);
        }
        else if (node.Kind == TemplateSyntaxKind.Block && node.Block != null)
        {
            AddBlockDependencies(graph, fromId, node.Block);
        }
    }

    internal void AddBlockDependencies(TemplateDependencyGraph graph, string fromId, TemplateBlockSyntax block)
    {
        if (block.Name == "handlebarsPartialBlock")
        {
            AddDependencyFromArguments(graph, fromId, TemplateDependencyKind.Partial, block.StartStatement.Arguments);
        }

        AddNodesDependencies(graph, fromId, block.Children);
        AddNodesDependencies(graph, fromId, block.ElseChildren);
    }

    internal void AddStatementDependency(TemplateDependencyGraph graph, string fromId, TemplateStatementSyntax statement)
    {
        if (statement.Name == "include" || statement.Name == "render" || statement.Name == "import" || statement.Name == "extends")
        {
            AddDependencyFromArguments(graph, fromId, TemplateDependencyKind.Template, statement.Arguments);
            return;
        }

        if (statement.Name == "partial")
        {
            AddDependencyFromArguments(graph, fromId, TemplateDependencyKind.Partial, statement.Arguments);
        }
    }

    internal void AddDependencyFromArguments(TemplateDependencyGraph graph, string fromId, TemplateDependencyKind kind, List<TemplateExpressionSyntax> arguments)
    {
        if (arguments.Count == 0)
        {
            return;
        }

        TemplateExpressionSyntax expression = arguments[0];
        string name = GetLiteralString(expression);
        if (name == "")
        {
            graph.Diagnostics.Add(new TemplateDiagnostic("GRAPH001", TemplateDiagnosticSeverity.Warning, "Dynamic dependency name cannot be resolved statically.", expression.Span, "Use a string literal to make this dependency visible to static graph tooling."));
            return;
        }

        AddDependency(graph, fromId, kind, name, expression.Span);
    }

    internal void AddDependency(TemplateDependencyGraph graph, string fromId, TemplateDependencyKind kind, string name, TemplateSourceSpan span)
    {
        TemplateSource? source = LoadSource(kind, name);
        TemplateDependencyGraphNodeKind nodeKind = ConvertKind(kind);
        string toId = TemplateDependencyGraph.CreateNodeId(nodeKind, name);
        bool resolved = source != null;
        graph.AddEdge(fromId, toId, kind, name, resolved, span);
        if (source == null)
        {
            graph.Diagnostics.Add(new TemplateDiagnostic("GRAPH002", TemplateDiagnosticSeverity.Warning, "Dependency '" + name + "' was not found.", span, "Register the template or partial before building a complete dependency graph."));
            return;
        }

        TemplateDependencyGraphNode? existing = graph.FindNode(toId);
        graph.AddNode(nodeKind, name, source);
        if (existing != null)
        {
            return;
        }

        TemplateParseResult parseResult = ProfileRegistry.ParseSource(source, Options.SyntaxProfile);
        AddParseDiagnostics(graph, parseResult.Diagnostics);
        AddDocumentDependencies(graph, toId, parseResult.Document);
    }

    internal TemplateSource? LoadSource(TemplateDependencyKind kind, string name)
    {
        if (kind == TemplateDependencyKind.Partial)
        {
            return Registry.LoadPartial(name);
        }

        return Registry.LoadTemplate(name);
    }

    internal TemplateDependencyGraphNodeKind ConvertKind(TemplateDependencyKind kind)
    {
        if (kind == TemplateDependencyKind.Partial)
        {
            return TemplateDependencyGraphNodeKind.Partial;
        }

        return TemplateDependencyGraphNodeKind.Template;
    }

    internal TemplateSource ConvertSourceForOptions(TemplateSource source)
    {
        return ProfileRegistry.ConvertSourceToNative(source, Options.SyntaxProfile);
    }

    internal string GetLiteralString(TemplateExpressionSyntax expression)
    {
        if (expression.ExpressionKind != TemplateExpressionSyntaxKind.Literal || expression.LiteralKind != TemplateLiteralSyntaxKind.String)
        {
            return "";
        }

        string text = expression.Text;
        if (text.Length < 2)
        {
            return "";
        }

        char quote = text[0];
        if ((quote != '"' && quote != '\'') || text[text.Length - 1] != quote)
        {
            return "";
        }

        return UnescapeString(text.Substring(1, text.Length - 2));
    }

    internal string UnescapeString(string text)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (value == '\\' && index + 1 < text.Length)
            {
                index = index + 1;
                value = text[index];
                if (value == 'n')
                {
                    builder.Append('\n');
                }
                else if (value == 'r')
                {
                    builder.Append('\r');
                }
                else if (value == 't')
                {
                    builder.Append('\t');
                }
                else
                {
                    builder.Append(value);
                }
            }
            else
            {
                builder.Append(value);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal void AddParseDiagnostics(TemplateDependencyGraph graph, List<TemplateDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            graph.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }
    }
}
