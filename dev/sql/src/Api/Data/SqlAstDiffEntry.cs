using System;

namespace sql;

/// <summary>Describes one node comparison at a stable AST path.</summary>
public class SqlAstDiffEntry
{
    /// <summary>The comparison outcome.</summary>
    public SqlAstDiffKind Kind;
    /// <summary>The stable path of the node within the script.</summary>
    public string Path;
    /// <summary>The public AST model type represented by the entry.</summary>
    public string NodeType;
    /// <summary>The original node, or <c>null</c> for an added node.</summary>
    public object? OriginalNode;
    /// <summary>The modified node, or <c>null</c> for a removed node.</summary>
    public object? ModifiedNode;
    /// <summary>The original source span, or <c>null</c> for an added node.</summary>
    public SqlTextSpan? OriginalSpan;
    /// <summary>The modified source span, or <c>null</c> for a removed node.</summary>
    public SqlTextSpan? ModifiedSpan;

    /// <summary>Initializes an AST diff entry.</summary>
    /// <param name="kind">The comparison outcome.</param>
    /// <param name="path">The stable AST path.</param>
    /// <param name="nodeType">The AST model type name.</param>
    /// <param name="originalNode">The original node, when present.</param>
    /// <param name="modifiedNode">The modified node, when present.</param>
    /// <param name="originalSpan">The original span, when present.</param>
    /// <param name="modifiedSpan">The modified span, when present.</param>
    /// <exception cref="ArgumentNullException">path or nodeType is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">path or nodeType is empty, or both nodes are <c>null</c>.</exception>
    public SqlAstDiffEntry(SqlAstDiffKind kind, string path, string nodeType, object? originalNode, object? modifiedNode, SqlTextSpan? originalSpan, SqlTextSpan? modifiedSpan)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (nodeType == null)
        {
            throw new ArgumentNullException("nodeType");
        }

        if (path.Length == 0 || nodeType.Length == 0 || (originalNode == null && modifiedNode == null))
        {
            throw new ArgumentException("A path, node type, and at least one node are required.");
        }

        Kind = kind;
        Path = path;
        NodeType = nodeType;
        OriginalNode = originalNode;
        ModifiedNode = modifiedNode;
        OriginalSpan = originalSpan;
        ModifiedSpan = modifiedSpan;
    }
}
