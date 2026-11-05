using System;

namespace markdown;

/// <summary>
/// Provides invariant-preserving inspection and editing operations for one Markdown AST node.
/// </summary>
public class MarkdownNodeOperations : IMarkdownNodeOperations
{
    internal MarkdownNode Node;

    /// <summary>
    /// Initializes operations for an existing node.
    /// </summary>
    /// <param name="node">The node to inspect or edit.</param>
    public MarkdownNodeOperations(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        Node = node;
    }

    /// <summary>Returns the node operated on by this instance.</summary>
    /// <returns>The active node.</returns>
    public MarkdownNode GetNode() { return Node; }
    /// <summary>Returns the node literal.</summary>
    /// <returns>The current literal text.</returns>
    public string GetLiteral() { return Node.Literal; }
    /// <summary>Sets the node literal and invalidates affected captured trivia.</summary>
    /// <param name="literal">The replacement literal.</param>
    public void SetLiteral(string literal)
    {
        Node.SetLiteral(literal);
        InvalidateTrivia(Node);
    }
    /// <summary>Returns the number of direct children.</summary>
    /// <returns>The child count.</returns>
    public int GetChildCount() { return Node.GetChildCount(); }
    /// <summary>Reports whether the node has direct children.</summary>
    /// <returns>The child state.</returns>
    public bool HasChildren() { return Node.HasChildren(); }
    /// <summary>Returns a direct child by index.</summary>
    /// <param name="index">The zero-based child index.</param>
    /// <returns>The child node.</returns>
    public MarkdownNode GetChildAt(int index) { return Node.GetChild(index); }
    /// <summary>Returns a detached array containing the direct child references.</summary>
    /// <returns>The child references.</returns>
    public MarkdownNode[] GetChildren() { return Node.GetChildren(); }
    /// <summary>Appends a child, reparenting it when necessary.</summary>
    /// <param name="child">The child to append.</param>
    public void AppendChild(MarkdownNode child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }
        MarkdownNode? previousParent = child.Parent;
        Node.AppendChild(child);
        InvalidateTrivia(Node);
        InvalidateTrivia(previousParent);
    }
    /// <summary>Prepends a child, reparenting it when necessary.</summary>
    /// <param name="child">The child to prepend.</param>
    public void PrependChild(MarkdownNode child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }
        MarkdownNode? previousParent = child.Parent;
        Node.PrependChild(child);
        InvalidateTrivia(Node);
        InvalidateTrivia(previousParent);
    }
    /// <summary>Inserts a child at an index, reparenting it when necessary.</summary>
    /// <param name="index">The insertion index.</param>
    /// <param name="child">The child to insert.</param>
    public void InsertChildAt(int index, MarkdownNode child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }
        MarkdownNode? previousParent = child.Parent;
        Node.InsertChild(index, child);
        InvalidateTrivia(Node);
        InvalidateTrivia(previousParent);
    }
    /// <summary>Inserts a child before an attached child.</summary>
    /// <param name="existingChild">The attached child.</param>
    /// <param name="newChild">The child to insert.</param>
    public void InsertChildBefore(MarkdownNode existingChild, MarkdownNode newChild)
    {
        if (newChild == null)
        {
            throw new ArgumentNullException("newChild");
        }
        MarkdownNode? previousParent = newChild.Parent;
        Node.InsertBefore(existingChild, newChild);
        InvalidateTrivia(Node);
        InvalidateTrivia(previousParent);
    }
    /// <summary>Inserts a child after an attached child.</summary>
    /// <param name="existingChild">The attached child.</param>
    /// <param name="newChild">The child to insert.</param>
    public void InsertChildAfter(MarkdownNode existingChild, MarkdownNode newChild)
    {
        if (newChild == null)
        {
            throw new ArgumentNullException("newChild");
        }
        MarkdownNode? previousParent = newChild.Parent;
        Node.InsertAfter(existingChild, newChild);
        InvalidateTrivia(Node);
        InvalidateTrivia(previousParent);
    }
    /// <summary>Replaces an attached child.</summary>
    /// <param name="existingChild">The attached child to replace.</param>
    /// <param name="newChild">The replacement child.</param>
    /// <returns>The detached replaced child.</returns>
    public MarkdownNode ReplaceChild(MarkdownNode existingChild, MarkdownNode newChild)
    {
        if (newChild == null)
        {
            throw new ArgumentNullException("newChild");
        }
        MarkdownNode? previousParent = newChild.Parent;
        MarkdownNode replaced = Node.ReplaceChild(existingChild, newChild);
        InvalidateTrivia(Node);
        InvalidateTrivia(previousParent);
        return replaced;
    }
    /// <summary>Removes a child when it is attached to this node.</summary>
    /// <param name="child">The child to remove.</param>
    /// <returns>Whether the child was removed.</returns>
    public bool RemoveChild(MarkdownNode child)
    {
        bool removed = Node.RemoveChild(child);
        if (removed)
        {
            InvalidateTrivia(Node);
        }

        return removed;
    }
    /// <summary>Removes and returns the child at an index.</summary>
    /// <param name="index">The zero-based child index.</param>
    /// <returns>The detached child.</returns>
    public MarkdownNode RemoveChildAt(int index)
    {
        MarkdownNode removed = Node.RemoveChildAt(index);
        InvalidateTrivia(Node);
        return removed;
    }
    /// <summary>Detaches every direct child.</summary>
    public void ClearChildren()
    {
        if (Node.HasChildren())
        {
            Node.ClearChildren();
            InvalidateTrivia(Node);
        }
    }
    /// <summary>Detaches the active node from its parent.</summary>
    /// <returns>Whether the node was detached.</returns>
    public bool Detach()
    {
        MarkdownNode? previousParent = Node.Parent;
        bool detached = Node.Detach();
        if (detached)
        {
            InvalidateTrivia(previousParent);
        }

        return detached;
    }
    /// <summary>Reports whether the active node has a parent.</summary>
    /// <returns>The parent state.</returns>
    public bool HasParent() { return Node.HasParent(); }
    /// <summary>Returns the parent, or <c>null</c> while detached.</summary>
    /// <returns>The parent node or <c>null</c>.</returns>
    public MarkdownNode? GetParent() { return Node.Parent; }
    /// <summary>Returns the first child, or <c>null</c>.</summary>
    /// <returns>The first child or <c>null</c>.</returns>
    public MarkdownNode? GetFirstChild() { return Node.GetFirstChild(); }
    /// <summary>Returns the last child, or <c>null</c>.</summary>
    /// <returns>The last child or <c>null</c>.</returns>
    public MarkdownNode? GetLastChild() { return Node.GetLastChild(); }
    /// <summary>Returns the previous sibling, or <c>null</c>.</summary>
    /// <returns>The previous sibling or <c>null</c>.</returns>
    public MarkdownNode? GetPreviousSibling() { return Node.GetPreviousSibling(); }
    /// <summary>Returns the next sibling, or <c>null</c>.</summary>
    /// <returns>The next sibling or <c>null</c>.</returns>
    public MarkdownNode? GetNextSibling() { return Node.GetNextSibling(); }
    /// <summary>Returns the node index in its parent, or minus one.</summary>
    /// <returns>The zero-based index, or minus one.</returns>
    public int GetIndexInParent() { return Node.GetIndexInParent(); }
    /// <summary>Returns the number of attributes.</summary>
    /// <returns>The attribute count.</returns>
    public int GetAttributeCount() { return Node.GetAttributeCount(); }
    /// <summary>Reports whether the node has attributes.</summary>
    /// <returns>The attribute state.</returns>
    public bool HasAttributes() { return Node.HasAttributes(); }
    /// <summary>Reports whether a named attribute exists.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>Whether the attribute exists.</returns>
    public bool ContainsAttribute(string name) { return Node.HasAttribute(name); }
    /// <summary>Returns a detached attribute copy, or <c>null</c>.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The attribute copy or <c>null</c>.</returns>
    public MarkdownAttribute? GetAttribute(string name) { return Node.GetAttribute(name); }
    /// <summary>Returns an attribute value, or an empty string when absent.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The attribute value.</returns>
    public string GetAttributeValue(string name) { return Node.GetAttributeValue(name); }
    /// <summary>Returns detached copies of all attributes.</summary>
    /// <returns>The attribute copies.</returns>
    public MarkdownAttribute[] GetAttributes() { return Node.GetAttributes(); }
    /// <summary>Adds or replaces an attribute.</summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    public void SetAttribute(string name, string value)
    {
        Node.SetAttribute(name, value);
        InvalidateTrivia(Node);
    }
    /// <summary>Removes a named attribute.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>Whether an attribute was removed.</returns>
    public bool RemoveAttribute(string name)
    {
        bool removed = Node.RemoveAttribute(name);
        if (removed)
        {
            InvalidateTrivia(Node);
        }

        return removed;
    }
    /// <summary>Removes every attribute.</summary>
    public void ClearAttributes()
    {
        if (Node.HasAttributes())
        {
            Node.ClearAttributes();
            InvalidateTrivia(Node);
        }
    }
    /// <summary>Reports whether captured source trivia is attached.</summary>
    /// <returns>The trivia state.</returns>
    public bool HasTrivia() { return Node.HasTrivia(); }
    /// <summary>Returns a detached trivia copy, or <c>null</c>.</summary>
    /// <returns>The trivia copy or <c>null</c>.</returns>
    public MarkdownTrivia? GetTrivia() { return Node.GetTrivia(); }
    /// <summary>Returns captured source text, or an empty string.</summary>
    /// <returns>The captured source text.</returns>
    public string GetTriviaText() { return Node.GetTriviaText(); }
    /// <summary>Returns the captured original line-ending sequence, or an empty string.</summary>
    /// <returns>The original line ending.</returns>
    public string GetTriviaLineEnding()
    {
        return Node.Trivia == null ? "" : Node.Trivia.LineEnding;
    }
    /// <summary>Returns the captured opening delimiter, or an empty string.</summary>
    /// <returns>The opening delimiter.</returns>
    public string GetOpeningDelimiter()
    {
        return Node.Trivia == null ? "" : Node.Trivia.OpeningDelimiter;
    }
    /// <summary>Returns the captured closing delimiter, or an empty string.</summary>
    /// <returns>The closing delimiter.</returns>
    public string GetClosingDelimiter()
    {
        return Node.Trivia == null ? "" : Node.Trivia.ClosingDelimiter;
    }
    /// <summary>Attaches a detached trivia copy and invalidates ancestor trivia.</summary>
    /// <param name="trivia">The trivia to attach.</param>
    public void SetTrivia(MarkdownTrivia trivia)
    {
        Node.SetTrivia(trivia);
        InvalidateTrivia(Node.Parent);
    }
    /// <summary>Clears node trivia and invalidates ancestor trivia.</summary>
    public void ClearTrivia()
    {
        Node.ClearTrivia();
        InvalidateTrivia(Node.Parent);
    }
    /// <summary>Returns the number of node-local diagnostics.</summary>
    /// <returns>The diagnostic count.</returns>
    public int GetDiagnosticCount() { return Node.Diagnostics.GetCount(); }
    /// <summary>Reports whether node-local diagnostics exist.</summary>
    /// <returns>The diagnostic state.</returns>
    public bool HasDiagnostics() { return Node.Diagnostics.HasDiagnostics(); }
    /// <summary>Returns detached copies of node-local diagnostics.</summary>
    /// <returns>The diagnostic copies.</returns>
    public MarkdownDiagnostic[] GetDiagnostics() { return Node.Diagnostics.GetDiagnostics(); }
    /// <summary>Returns a detached node-local diagnostic by index.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic copy.</returns>
    public MarkdownDiagnostic GetDiagnosticAt(int index) { return Node.Diagnostics.Get(index); }
    /// <summary>Finds node-local diagnostics whose half-open spans contain an offset.</summary>
    /// <param name="offset">The zero-based source offset.</param>
    /// <returns>The matching diagnostic copies.</returns>
    public MarkdownDiagnostic[] FindDiagnosticsContainingOffset(int offset)
    {
        return MarkdownDiagnosticQueries.FindContaining(Node.Diagnostics, offset);
    }
    /// <summary>Finds node-local diagnostics whose spans intersect a query span.</summary>
    /// <param name="span">The half-open query span.</param>
    /// <returns>The matching diagnostic copies.</returns>
    public MarkdownDiagnostic[] FindDiagnosticsIntersectingSpan(MarkdownSourceSpan span)
    {
        return MarkdownDiagnosticQueries.FindIntersecting(Node.Diagnostics, span);
    }
    /// <summary>Adds a detached copy of a node-local diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    public void AddDiagnostic(MarkdownDiagnostic diagnostic) { Node.Diagnostics.Add(diagnostic); }
    /// <summary>Creates and adds a node-local diagnostic.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="span">The diagnostic source span.</param>
    public void AddDiagnosticValues(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span) { Node.Diagnostics.AddDiagnostic(severity, code, message, span); }
    /// <summary>Removes every node-local diagnostic.</summary>
    public void ClearDiagnostics() { Node.Diagnostics.Clear(); }

    internal static void InvalidateTrivia(MarkdownNode? node)
    {
        MarkdownNode? current = node;
        while (current != null)
        {
            if (current.Trivia != null)
            {
                current.Trivia.IsModified = true;
            }

            current = current.Parent;
        }
    }
}
