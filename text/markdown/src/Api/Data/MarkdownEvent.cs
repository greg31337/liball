using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Represents an enter or leave record in a depth-first Markdown event stream.
/// </summary>
/// <remarks>
/// Enter events carry a node snapshot and are followed by their child events and a matching leave event. Attribute and trivia methods copy their inputs so an event can be transformed independently of its source node.
/// </remarks>
public class MarkdownEvent
{
    /// <summary>
    /// Stores the event kind value used by this instance.
    /// </summary>
    public MarkdownEventKind EventKind;
    /// <summary>
    /// Stores the node kind value used by this instance.
    /// </summary>
    public MarkdownNodeKind NodeKind;
    /// <summary>
    /// The source node, or <c>null</c> when the event was not created from a node.
    /// </summary>
    public MarkdownNode? Node;
    /// <summary>
    /// Stores the span value used by this instance.
    /// </summary>
    public MarkdownSourceSpan Span;
    /// <summary>
    /// Stores the literal value used by this instance.
    /// </summary>
    public string Literal;
    /// <summary>
    /// Stores the event attributes.
    /// </summary>
    public List<MarkdownAttribute> Attributes;
    /// <summary>
    /// Stores captured source trivia for the event.
    /// </summary>
    public MarkdownTrivia? Trivia;
    /// <summary>
    /// Stores the depth value used by this instance.
    /// </summary>
    public int Depth;
    /// <summary>
    /// Stores the child index value used by this instance.
    /// </summary>
    public int ChildIndex;

    /// <summary>
    /// Initializes a new <see cref="MarkdownEvent"/> instance.
    /// </summary>
    public MarkdownEvent()
    {
        EventKind = MarkdownEventKind.None;
        NodeKind = MarkdownNodeKind.Unknown;
        Node = null;
        Span = new MarkdownSourceSpan();
        Literal = "";
        Attributes = new List<MarkdownAttribute>();
        Trivia = null;
        Depth = 0;
        ChildIndex = -1;
    }

    /// <summary>
    /// Creates an event for a node kind.
    /// </summary>
    /// <param name="eventKind">Whether the event enters or leaves a node.</param>
    /// <param name="nodeKind">The syntax kind represented by the event.</param>
    internal static MarkdownEvent Create(MarkdownEventKind eventKind, MarkdownNodeKind nodeKind)
    {
        MarkdownEvent item = new MarkdownEvent();
        item.EventKind = eventKind;
        item.NodeKind = nodeKind;
        return item;
    }

    /// <summary>
    /// Creates an event snapshot from an AST node.
    /// </summary>
    /// <param name="eventKind">Whether the event enters or leaves a node.</param>
    /// <param name="node">The AST node to process.</param>
    /// <param name="depth">The zero-based depth in the syntax tree.</param>
    /// <param name="childIndex">The zero-based index within the parent node.</param>
    internal static MarkdownEvent Create(MarkdownEventKind eventKind, MarkdownNode node, int depth, int childIndex)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException("depth");
        }

        if (childIndex < -1)
        {
            throw new ArgumentOutOfRangeException("childIndex");
        }

        MarkdownEvent item = new MarkdownEvent();
        item.EventKind = eventKind;
        item.NodeKind = node.Kind;
        item.Node = node;
        item.Span = node.Span.Copy();
        item.Literal = node.Literal;
        item.CopyAttributesFrom(node);
        item.Trivia = node.GetTrivia();
        item.Depth = depth;
        item.ChildIndex = childIndex;
        return item;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownEvent"/> value.</returns>
    internal MarkdownEvent Copy()
    {
        MarkdownEvent copy = Create(EventKind, NodeKind);
        copy.Node = Node;
        copy.Span = Span.Copy();
        copy.Literal = Literal;
        copy.Depth = Depth;
        copy.ChildIndex = ChildIndex;
        if (Trivia != null)
        {
            copy.Trivia = Trivia.Copy();
        }

        int index = 0;
        while (index < Attributes.Count)
        {
            copy.Attributes.Add(Attributes[index].Copy());
            index = index + 1;
        }

        return copy;
    }

    /// <summary>
    /// Reports whether this event opens a node.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEnter()
    {
        return EventKind == MarkdownEventKind.EnterNode;
    }

    /// <summary>
    /// Reports whether this event closes a node.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsLeave()
    {
        return EventKind == MarkdownEventKind.LeaveNode;
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
        return true;
    }

    /// <summary>
    /// Clears attributes.
    /// </summary>
    internal void ClearAttributes()
    {
        Attributes.Clear();
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

    internal void CopyAttributesFrom(MarkdownNode node)
    {
        MarkdownAttribute[] attributes = node.GetAttributes();
        int index = 0;
        while (index < attributes.Length)
        {
            Attributes.Add(attributes[index].Copy());
            index = index + 1;
        }
    }
}
