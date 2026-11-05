using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Represents one root, named template, or partial in a dependency graph.
/// </summary>
public class TemplateDependencyGraphNode
{
    /// <summary>
    /// Specifies the id for this dependency graph node.
    /// </summary>
    public string Id;
    /// <summary>
    /// Identifies the kind represented by this dependency graph node.
    /// </summary>
    public TemplateDependencyGraphNodeKind Kind;
    /// <summary>
    /// Stores the logical name used to identify this value.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the template source associated with this value.
    /// </summary>
    public TemplateSource? Source;

    /// <summary>
    /// Initializes a dependency graph node.
    /// </summary>
    /// <param name="id">The stable graph node identifier.</param>
    /// <param name="kind">The graph node kind.</param>
    /// <param name="name">The logical dependency name.</param>
    /// <param name="source">The resolved source, or <c>null</c> for an unresolved node.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>id</c> or <c>name</c> is <c>null</c>.</exception>
    public TemplateDependencyGraphNode(string id, TemplateDependencyGraphNodeKind kind, string name, TemplateSource? source)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        Id = id;
        Kind = kind;
        Name = name;
        Source = source;
    }
}

/// <summary>
/// Represents one dependency reference between two graph nodes.
/// </summary>
public class TemplateDependencyGraphEdge
{
    /// <summary>
    /// Specifies the from id for this dependency graph edge.
    /// </summary>
    public string FromId;
    /// <summary>
    /// Specifies the to id for this dependency graph edge.
    /// </summary>
    public string ToId;
    /// <summary>
    /// Identifies the kind represented by this dependency graph edge.
    /// </summary>
    public TemplateDependencyKind Kind;
    /// <summary>
    /// Stores the logical name used to identify this value.
    /// </summary>
    public string Name;
    /// <summary>
    /// Indicates whether the referenced template or partial was found while building the graph.
    /// </summary>
    public bool Resolved;
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan? Span;

    /// <summary>
    /// Initializes a dependency graph edge.
    /// </summary>
    /// <param name="fromId">The source node identifier.</param>
    /// <param name="toId">The target node identifier.</param>
    /// <param name="kind">The referenced dependency kind.</param>
    /// <param name="name">The logical dependency name.</param>
    /// <param name="resolved">Whether the dependency was resolved.</param>
    /// <param name="span">The source span containing the dependency reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when an identifier or <c>name</c> is <c>null</c>.</exception>
    public TemplateDependencyGraphEdge(string fromId, string toId, TemplateDependencyKind kind, string name, bool resolved, TemplateSourceSpan? span)
    {
        if (fromId == null)
        {
            throw new ArgumentNullException("fromId");
        }

        if (toId == null)
        {
            throw new ArgumentNullException("toId");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        FromId = fromId;
        ToId = toId;
        Kind = kind;
        Name = name;
        Resolved = resolved;
        Span = span;
    }
}

/// <summary>
/// Contains static template dependency nodes, edges, and diagnostics for unresolved references.
/// </summary>
public class TemplateDependencyGraph
{
    /// <summary>
    /// Stores the ordered nodes.
    /// </summary>
    public List<TemplateDependencyGraphNode> Nodes;
    /// <summary>
    /// Stores the ordered edges.
    /// </summary>
    public List<TemplateDependencyGraphEdge> Edges;
    /// <summary>
    /// Stores the ordered diagnostics.
    /// </summary>
    public List<TemplateDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new template dependency graph and validates any supplied values.
    /// </summary>
    public TemplateDependencyGraph()
    {
        Nodes = new List<TemplateDependencyGraphNode>();
        Edges = new List<TemplateDependencyGraphEdge>();
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

    internal TemplateDependencyGraphNode AddNode(TemplateDependencyGraphNodeKind kind, string name, TemplateSource source)
    {
        string id = CreateNodeId(kind, name);
        TemplateDependencyGraphNode? existing = FindNode(id);
        if (existing != null)
        {
            return existing;
        }

        TemplateDependencyGraphNode node = new TemplateDependencyGraphNode(id, kind, name, source);
        Nodes.Add(node);
        return node;
    }

    internal void AddEdge(string fromId, string toId, TemplateDependencyKind kind, string name, bool resolved, TemplateSourceSpan span)
    {
        if (HasEdge(fromId, toId, kind, name))
        {
            return;
        }

        Edges.Add(new TemplateDependencyGraphEdge(fromId, toId, kind, name, resolved, span));
    }

    /// <summary>
    /// Finds a node by its stable identifier using ordinal comparison.
    /// </summary>
    /// <param name="id">The id used by the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal TemplateDependencyGraphNode? FindNode(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        int index = 0;
        while (index < Nodes.Count)
        {
            if (Nodes[index].Id == id)
            {
                return Nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Formats this value into a stable, human-readable form.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string Format()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("DependencyGraph\n");
        int index = 0;
        while (index < Nodes.Count)
        {
            TemplateDependencyGraphNode node = Nodes[index];
            builder.Append("  Node ");
            builder.Append(node.Id);
            builder.Append(" kind=");
            builder.Append(node.Kind.ToString());
            builder.Append(" name=");
            WriteQuoted(builder, node.Name);
            builder.Append('\n');
            index = index + 1;
        }

        index = 0;
        while (index < Edges.Count)
        {
            TemplateDependencyGraphEdge edge = Edges[index];
            builder.Append("  Edge ");
            builder.Append(edge.FromId);
            builder.Append(" -> ");
            builder.Append(edge.ToId);
            builder.Append(" kind=");
            builder.Append(edge.Kind.ToString());
            builder.Append(" name=");
            WriteQuoted(builder, edge.Name);
            builder.Append(" resolved=");
            WriteBoolean(builder, edge.Resolved);
            builder.Append('\n');
            index = index + 1;
        }

        index = 0;
        while (index < Diagnostics.Count)
        {
            TemplateDiagnostic diagnostic = Diagnostics[index];
            builder.Append("  Diagnostic ");
            builder.Append(diagnostic.Code);
            builder.Append(" ");
            builder.Append(diagnostic.Severity.ToString());
            builder.Append(" message=");
            WriteQuoted(builder, diagnostic.Message);
            builder.Append('\n');
            index = index + 1;
        }

        return builder.ToString();
    }

    internal bool HasEdge(string fromId, string toId, TemplateDependencyKind kind, string name)
    {
        int index = 0;
        while (index < Edges.Count)
        {
            TemplateDependencyGraphEdge edge = Edges[index];
            if (edge.FromId == fromId && edge.ToId == toId && edge.Kind == kind && edge.Name == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string CreateNodeId(TemplateDependencyGraphNodeKind kind, string name)
    {
        if (kind == TemplateDependencyGraphNodeKind.Root)
        {
            return "root:" + name;
        }

        if (kind == TemplateDependencyGraphNodeKind.Partial)
        {
            return "partial:" + name;
        }

        return "template:" + name;
    }

    internal void WriteQuoted(StringBuilder builder, string text)
    {
        builder.Append('"');
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (value == '\\' || value == '"')
            {
                builder.Append('\\');
                builder.Append(value);
            }
            else if (value == '\n')
            {
                builder.Append("\\n");
            }
            else if (value == '\r')
            {
                builder.Append("\\r");
            }
            else if (value == '\t')
            {
                builder.Append("\\t");
            }
            else
            {
                builder.Append(value);
            }

            index = index + 1;
        }

        builder.Append('"');
    }

    internal void WriteBoolean(StringBuilder builder, bool value)
    {
        if (value)
        {
            builder.Append("true");
        }
        else
        {
            builder.Append("false");
        }
    }
}
