using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Represents one mutable node in a Markdown abstract syntax tree.
/// </summary>
/// <remarks>
/// Use the child insertion and removal methods to edit the tree; they keep parent links consistent and reject cycles. Collection-returning methods return arrays, while attribute and trivia setters copy caller-owned values.
/// </remarks>
public class MarkdownNode
{
    /// <summary>
    /// The syntax role of this node.
    /// </summary>
    public MarkdownNodeKind Kind;
    /// <summary>
    /// The half-open source range that produced this node.
    /// </summary>
    public MarkdownSourceSpan Span;
    /// <summary>
    /// The node's literal text, or an empty string when the node stores content in children.
    /// </summary>
    public string Literal;
    /// <summary>
    /// The containing node, or <c>null</c> while this node is detached.
    /// </summary>
    public MarkdownNode? Parent;
    /// <summary>
    /// Stores the ordered child nodes.
    /// </summary>
    public List<MarkdownNode> Children;
    /// <summary>
    /// Stores the node attributes.
    /// </summary>
    public List<MarkdownAttribute> Attributes;
    /// <summary>
    /// Stores captured source trivia when trivia tracking is enabled.
    /// </summary>
    public MarkdownTrivia? Trivia;
    /// <summary>
    /// Typed, feature-specific metadata for this node kind.
    /// </summary>
    public MarkdownNodeData Data;
    /// <summary>
    /// Diagnostics attached specifically to this node.
    /// </summary>
    public MarkdownDiagnosticCollection Diagnostics;

    /// <summary>
    /// Initializes a new <see cref="MarkdownNode"/> instance.
    /// </summary>
    public MarkdownNode()
    {
        Kind = MarkdownNodeKind.Unknown;
        Span = new MarkdownSourceSpan();
        Literal = "";
        Parent = null;
        Children = new List<MarkdownNode>();
        Attributes = new List<MarkdownAttribute>();
        Trivia = null;
        Data = new MarkdownNodeData();
        Diagnostics = new MarkdownDiagnosticCollection();
    }

    /// <summary>
    /// Creates a node with the specified kind.
    /// </summary>
    /// <param name="kind">The node kind to match.</param>
    internal static MarkdownNode Create(MarkdownNodeKind kind)
    {
        MarkdownNode node = new MarkdownNode();
        node.SetKind(kind);
        return node;
    }

    /// <summary>
    /// Creates a node with the specified kind and source span.
    /// </summary>
    /// <param name="kind">The node kind to match.</param>
    /// <param name="span">The source span associated with the value.</param>
    internal static MarkdownNode Create(MarkdownNodeKind kind, MarkdownSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        MarkdownNode node = new MarkdownNode();
        node.SetKind(kind);
        node.Span = span.Copy();
        return node;
    }

    /// <summary>
    /// Returns child count.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetChildCount()
    {
        return Children.Count;
    }

    /// <summary>
    /// Reports whether the node contains at least one child.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasChildren()
    {
        return Children.Count > 0;
    }

    /// <summary>
    /// Reports whether the node is attached beneath another node.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasParent()
    {
        return Parent != null;
    }

    /// <summary>
    /// Returns child.
    /// </summary>
    /// <param name="index">The zero-based position in the collection.</param>
    /// <returns>The resulting <see cref="MarkdownNode"/> value.</returns>
    internal MarkdownNode GetChild(int index)
    {
        if (index < 0 || index >= Children.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return Children[index];
    }

    /// <summary>
    /// Returns children.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownNode[] GetChildren()
    {
        return Children.ToArray();
    }

    /// <summary>
    /// Copies this node's scalar data, attributes, and trivia without copying its children or parent.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownNode"/> value.</returns>
    internal MarkdownNode CloneShallow()
    {
        MarkdownNode clone = Create(Kind, Span);
        clone.SetLiteral(Literal);

        int index = 0;
        while (index < Attributes.Count)
        {
            clone.Attributes.Add(Attributes[index].Copy());
            index = index + 1;
        }

        if (Trivia != null)
        {
            clone.Trivia = Trivia.Copy();
        }

        clone.Data = Data.CopyData();

        MarkdownDiagnostic[] diagnostics = Diagnostics.GetDiagnostics();
        index = 0;
        while (index < diagnostics.Length)
        {
            clone.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }

        return clone;
    }

    /// <summary>
    /// Deeply copies this node and all descendants with new parent links.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownNode"/> value.</returns>
    internal MarkdownNode CloneNode()
    {
        MarkdownNode clone = CloneShallow();

        int index = 0;
        while (index < Children.Count)
        {
            clone.AppendChild(Children[index].CloneNode());
            index = index + 1;
        }

        return clone;
    }

    /// <summary>
    /// Returns attribute count.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetAttributeCount()
    {
        return Attributes.Count;
    }

    /// <summary>
    /// Reports whether the node has at least one attribute.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasAttributes()
    {
        return Attributes.Count > 0;
    }

    /// <summary>
    /// Reports whether original source trivia is available.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTrivia()
    {
        return Trivia != null;
    }

    /// <summary>
    /// Returns trivia.
    /// </summary>
    /// <returns>A detached trivia copy, or <c>null</c> when trivia is unavailable.</returns>
    internal MarkdownTrivia? GetTrivia()
    {
        if (Trivia == null)
        {
            return null;
        }

        return Trivia.Copy();
    }

    /// <summary>
    /// Returns trivia text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetTriviaText()
    {
        if (Trivia == null)
        {
            return "";
        }

        return Trivia.Text;
    }

    /// <summary>
    /// Sets trivia.
    /// </summary>
    /// <param name="trivia">The trivia value.</param>
    internal void SetTrivia(MarkdownTrivia trivia)
    {
        if (trivia == null)
        {
            throw new ArgumentNullException("trivia");
        }

        Trivia = trivia.Copy();
    }

    /// <summary>
    /// Clears trivia.
    /// </summary>
    internal void ClearTrivia()
    {
        Trivia = null;
    }

    /// <summary>
    /// Returns attributes.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownAttribute[] GetAttributes()
    {
        MarkdownAttribute[] attributes = new MarkdownAttribute[Attributes.Count];
        int index = 0;

        while (index < Attributes.Count)
        {
            attributes[index] = Attributes[index].Copy();
            index = index + 1;
        }

        return attributes;
    }

    /// <summary>
    /// Returns the index of attribute.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int IndexOfAttribute(string name)
    {
        MarkdownAttribute.ValidateName(name);

        int index = 0;
        while (index < Attributes.Count)
        {
            if (Attributes[index].Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Reports whether an attribute with the requested name exists.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasAttribute(string name)
    {
        return IndexOfAttribute(name) >= 0;
    }

    /// <summary>
    /// Returns attribute.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns>A detached attribute copy, or <c>null</c> when the name is absent.</returns>
    internal MarkdownAttribute? GetAttribute(string name)
    {
        int index = IndexOfAttribute(name);
        if (index < 0)
        {
            return null;
        }

        return Attributes[index].Copy();
    }

    /// <summary>
    /// Returns attribute value.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns>The resulting text.</returns>
    internal string GetAttributeValue(string name)
    {
        int index = IndexOfAttribute(name);
        if (index < 0)
        {
            return "";
        }

        return Attributes[index].Value;
    }

    /// <summary>
    /// Sets attribute.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="value">The value to apply.</param>
    internal void SetAttribute(string name, string value)
    {
        MarkdownAttribute attribute = MarkdownAttribute.Create(name, value);
        SetAttributeCopy(attribute);
    }

    /// <summary>
    /// Sets attribute.
    /// </summary>
    /// <param name="attribute">The attribute to copy onto the node or event.</param>
    internal void SetAttributeCopy(MarkdownAttribute attribute)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException("attribute");
        }

        MarkdownAttribute copy = attribute.Copy();
        int index = IndexOfAttribute(copy.Name);
        if (index < 0)
        {
            Attributes.Add(copy);
        }
        else
        {
            Attributes[index] = copy;
        }

        Data.ApplyAttribute(copy.Name, copy.Value);
    }

    /// <summary>
    /// Removes attribute.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool RemoveAttribute(string name)
    {
        int index = IndexOfAttribute(name);
        if (index < 0)
        {
            return false;
        }

        Attributes.RemoveAt(index);
        SetKind(Kind);
        return true;
    }

    /// <summary>
    /// Clears attributes.
    /// </summary>
    internal void ClearAttributes()
    {
        Attributes.Clear();
        SetKind(Kind);
    }

    /// <summary>
    /// Returns first child.
    /// </summary>
    /// <returns>The first child, or <c>null</c> when this node has no children.</returns>
    internal MarkdownNode? GetFirstChild()
    {
        if (Children.Count == 0)
        {
            return null;
        }

        return Children[0];
    }

    /// <summary>
    /// Returns last child.
    /// </summary>
    /// <returns>The last child, or <c>null</c> when this node has no children.</returns>
    internal MarkdownNode? GetLastChild()
    {
        if (Children.Count == 0)
        {
            return null;
        }

        return Children[Children.Count - 1];
    }

    /// <summary>
    /// Returns the index of child.
    /// </summary>
    /// <param name="child">The child node to attach or remove.</param>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int IndexOfChild(MarkdownNode child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        return Children.IndexOf(child);
    }

    /// <summary>
    /// Appends child.
    /// </summary>
    /// <param name="child">The child node to attach or remove.</param>
    internal void AppendChild(MarkdownNode child)
    {
        InsertChild(Children.Count, child);
    }

    /// <summary>
    /// Prepends child.
    /// </summary>
    /// <param name="child">The child node to attach or remove.</param>
    internal void PrependChild(MarkdownNode child)
    {
        InsertChild(0, child);
    }

    /// <summary>
    /// Inserts child.
    /// </summary>
    /// <param name="index">The zero-based position in the collection.</param>
    /// <param name="child">The child node to attach or remove.</param>
    internal void InsertChild(int index, MarkdownNode child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        if (index < 0 || index > Children.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (child == this)
        {
            throw new ArgumentException("A node cannot be inserted as its own child.");
        }

        if (child.IsAncestorOf(this))
        {
            throw new ArgumentException("A node cannot be inserted below one of its descendants.");
        }

        if (child.Parent == this)
        {
            int oldIndex = Children.IndexOf(child);
            if (oldIndex >= 0)
            {
                Children.RemoveAt(oldIndex);
                if (oldIndex < index)
                {
                    index = index - 1;
                }
            }
        }
        else if (child.Parent != null)
        {
            child.Parent.RemoveChild(child);
        }

        child.Parent = this;
        Children.Insert(index, child);
    }

    /// <summary>
    /// Inserts before.
    /// </summary>
    /// <param name="existingChild">The child already attached to this node.</param>
    /// <param name="newChild">The node to insert.</param>
    internal void InsertBefore(MarkdownNode existingChild, MarkdownNode newChild)
    {
        if (existingChild == null)
        {
            throw new ArgumentNullException("existingChild");
        }

        int index = Children.IndexOf(existingChild);
        if (index < 0)
        {
            throw new ArgumentException("Existing child is not a child of this node.");
        }

        InsertChild(index, newChild);
    }

    /// <summary>
    /// Inserts after.
    /// </summary>
    /// <param name="existingChild">The child already attached to this node.</param>
    /// <param name="newChild">The node to insert.</param>
    internal void InsertAfter(MarkdownNode existingChild, MarkdownNode newChild)
    {
        if (existingChild == null)
        {
            throw new ArgumentNullException("existingChild");
        }

        int index = Children.IndexOf(existingChild);
        if (index < 0)
        {
            throw new ArgumentException("Existing child is not a child of this node.");
        }

        InsertChild(index + 1, newChild);
    }

    /// <summary>
    /// Replaces an attached child while preserving parent links and rejecting cycles.
    /// </summary>
    /// <param name="existingChild">The child to replace.</param>
    /// <param name="newChild">The replacement node.</param>
    /// <returns>The detached child that was replaced.</returns>
    internal MarkdownNode ReplaceChild(MarkdownNode existingChild, MarkdownNode newChild)
    {
        if (existingChild == null)
        {
            throw new ArgumentNullException("existingChild");
        }

        if (newChild == null)
        {
            throw new ArgumentNullException("newChild");
        }

        int index = Children.IndexOf(existingChild);
        if (index < 0)
        {
            throw new ArgumentException("Existing child is not a child of this node.");
        }

        if (existingChild == newChild)
        {
            return existingChild;
        }

        if (newChild == this)
        {
            throw new ArgumentException("A node cannot be inserted as its own child.");
        }

        if (newChild.IsAncestorOf(this))
        {
            throw new ArgumentException("A node cannot be inserted below one of its descendants.");
        }

        if (newChild.Parent == this)
        {
            int newChildIndex = Children.IndexOf(newChild);
            if (newChildIndex >= 0)
            {
                Children.RemoveAt(newChildIndex);
                if (newChildIndex < index)
                {
                    index = index - 1;
                }
            }
        }
        else if (newChild.Parent != null)
        {
            newChild.Parent.RemoveChild(newChild);
        }

        Children[index] = newChild;
        if (existingChild.Parent == this)
        {
            existingChild.Parent = null;
        }
        newChild.Parent = this;
        return existingChild;
    }

    /// <summary>
    /// Removes child.
    /// </summary>
    /// <param name="child">The child node to attach or remove.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool RemoveChild(MarkdownNode child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        int index = Children.IndexOf(child);
        if (index < 0)
        {
            return false;
        }

        Children.RemoveAt(index);
        if (child.Parent == this)
        {
            child.Parent = null;
        }

        return true;
    }

    /// <summary>
    /// Removes child at.
    /// </summary>
    /// <param name="index">The zero-based position in the collection.</param>
    /// <returns>The removed child.</returns>
    internal MarkdownNode RemoveChildAt(int index)
    {
        if (index < 0 || index >= Children.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        MarkdownNode child = Children[index];
        Children.RemoveAt(index);
        if (child.Parent == this)
        {
            child.Parent = null;
        }

        return child;
    }

    /// <summary>
    /// Clears children.
    /// </summary>
    internal void ClearChildren()
    {
        int index = 0;
        while (index < Children.Count)
        {
            MarkdownNode child = Children[index];
            if (child.Parent == this)
            {
                child.Parent = null;
            }

            index = index + 1;
        }

        Children.Clear();
    }

    /// <summary>
    /// Removes this node from its parent, if it has one.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool Detach()
    {
        if (Parent == null)
        {
            return false;
        }

        return Parent.RemoveChild(this);
    }

    /// <summary>
    /// Returns previous sibling.
    /// </summary>
    /// <returns>The previous sibling, or <c>null</c> when no previous sibling exists.</returns>
    internal MarkdownNode? GetPreviousSibling()
    {
        if (Parent == null)
        {
            return null;
        }

        int index = Parent.Children.IndexOf(this);
        if (index <= 0)
        {
            return null;
        }

        return Parent.Children[index - 1];
    }

    /// <summary>
    /// Returns next sibling.
    /// </summary>
    /// <returns>The next sibling, or <c>null</c> when no next sibling exists.</returns>
    internal MarkdownNode? GetNextSibling()
    {
        if (Parent == null)
        {
            return null;
        }

        int index = Parent.Children.IndexOf(this);
        if (index < 0 || index + 1 >= Parent.Children.Count)
        {
            return null;
        }

        return Parent.Children[index + 1];
    }

    /// <summary>
    /// Returns this node's position in its parent, or minus one while detached.
    /// </summary>
    /// <returns>The zero-based child index, or minus one.</returns>
    internal int GetIndexInParent()
    {
        if (Parent == null)
        {
            return -1;
        }

        return Parent.Children.IndexOf(this);
    }

    /// <summary>
    /// Reports whether this node appears in the other node parent chain.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsAncestorOf(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        MarkdownNode? current = node.Parent;
        while (current != null)
        {
            if (current == this)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Sets literal.
    /// </summary>
    /// <param name="literal">The literal value.</param>
    internal void SetLiteral(string literal)
    {
        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        Literal = literal;
    }

    /// <summary>
    /// Sets the node kind and rebuilds its typed metadata from current attributes.
    /// </summary>
    /// <param name="kind">The node kind to apply.</param>
    internal void SetKind(MarkdownNodeKind kind)
    {
        Kind = kind;
        Data = MarkdownNodeData.CreateForKind(kind);
        int index = 0;
        while (index < Attributes.Count)
        {
            MarkdownAttribute attribute = Attributes[index];
            Data.ApplyAttribute(attribute.Name, attribute.Value);
            index = index + 1;
        }
    }

    /// <summary>
    /// Sets source span.
    /// </summary>
    /// <param name="span">The source span associated with the value.</param>
    internal void SetSourceSpan(MarkdownSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Span = span.Copy();
    }
}
