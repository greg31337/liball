using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Stores one ordered YAML mapping key and value together with comments and source metadata.
/// </summary>
public class YamlPair
{
    /// <summary>
    /// The mapping key node.
    /// </summary>
    public YamlNode? Key;
    /// <summary>
    /// The mapping value node, which may be an empty node when the value was omitted.
    /// </summary>
    public YamlNode? Value;
    /// <summary>
    /// Whether the key requires explicit complex-key YAML syntax.
    /// </summary>
    public bool ComplexKey;
    /// <summary>
    /// Comments attached before this item, without changing their order.
    /// </summary>
    public List<string> LeadingComments;
    /// <summary>
    /// Comments attached after or inline with this item, in source order.
    /// </summary>
    public List<string> TrailingComments;
    /// <summary>
    /// Blank-line markers preserved before this item.
    /// </summary>
    public List<string> LeadingBlankLines;
    /// <summary>
    /// Blank-line markers preserved after this item.
    /// </summary>
    public List<string> TrailingBlankLines;
    /// <summary>
    /// Ordered comment and blank-line metadata attached to this mapping pair.
    /// </summary>
    public List<YamlTrivia> Trivia;
    /// <summary>
    /// The source range occupied by this item.
    /// </summary>
    public YamlSourceSpan Span;

    /// <summary>
    /// Initializes a mapping pair with null key and value nodes and empty presentation metadata.
    /// </summary>
    public YamlPair()
    {
        Key = null;
        Value = null;
        ComplexKey = false;
        LeadingComments = new List<string>();
        TrailingComments = new List<string>();
        LeadingBlankLines = new List<string>();
        TrailingBlankLines = new List<string>();
        Trivia = new List<YamlTrivia>();
        Span = YamlSourceSpan.Empty();
    }

    /// <summary>
    /// Creates a mapping pair and derives its complex-key flag and source range from the supplied nodes.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML pair.</returns>
    internal static YamlPair Create(YamlNode? key, YamlNode? value, YamlSourceSpan? span)
    {
        YamlPair pair = new YamlPair();
        pair.Key = key;
        pair.Value = value;
        pair.ComplexKey = KeyIsComplex(key);
        pair.Span = span == null ? YamlSourceSpan.Empty() : span;
        return pair;
    }

    /// <summary>
    /// Replaces this instance's stored values with the supplied values.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <param name="value">The value to process.</param>
    internal void Set(YamlNode? key, YamlNode? value)
    {
        Key = key;
        Value = value;
        ComplexKey = KeyIsComplex(key);
    }

    /// <summary>
    /// Creates a deep copy of the key, value, comments, and source range.
    /// </summary>
    /// <returns>The resulting YAML pair.</returns>
    internal YamlPair Clone()
    {
        return CloneGraph(new Dictionary<YamlNode, YamlNode>());
    }

    internal YamlPair CloneGraph(Dictionary<YamlNode, YamlNode> visited)
    {
        if (visited == null)
        {
            visited = new Dictionary<YamlNode, YamlNode>();
        }

        YamlPair clone = new YamlPair();
        if (Key != null)
        {
            clone.Key = Key.CloneGraph(visited);
        }

        if (Value != null)
        {
            clone.Value = Value.CloneGraph(visited);
        }

        clone.ComplexKey = ComplexKey;
        clone.LeadingComments = YamlNode.CopyStringList(LeadingComments);
        clone.TrailingComments = YamlNode.CopyStringList(TrailingComments);
        clone.LeadingBlankLines = YamlNode.CopyStringList(LeadingBlankLines);
        clone.TrailingBlankLines = YamlNode.CopyStringList(TrailingBlankLines);
        clone.Trivia = YamlNode.CopyTriviaList(Trivia);
        clone.Span = Span == null ? YamlSourceSpan.Empty() : Span.Copy();
        clone.RefreshComplexKey();
        return clone;
    }

    /// <summary>
    /// Determines whether the pair has a key node.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasKey()
    {
        return Key != null;
    }

    /// <summary>
    /// Determines whether the pair has a value node.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasValue()
    {
        return Value != null;
    }

    /// <summary>
    /// Returns the stored decision about whether the key requires explicit complex-key syntax.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsComplexKey()
    {
        return ComplexKey;
    }

    /// <summary>
    /// Recomputes whether the current key requires explicit complex-key syntax.
    /// </summary>
    internal void RefreshComplexKey()
    {
        ComplexKey = KeyIsComplex(Key);
    }

    /// <summary>
    /// Expands the pair's source range to cover its current key and value nodes.
    /// </summary>
    internal void RefreshSpanFromNodes()
    {
        if (Key == null && Value == null)
        {
            Span = YamlSourceSpan.Empty();
            return;
        }

        if (Key == null)
        {
            Span = Value == null ? YamlSourceSpan.Empty() : Value.Span.Copy();
            return;
        }

        if (Value == null)
        {
            Span = Key.Span.Copy();
            return;
        }

        Span = YamlSourceSpan.Cover(Key.Span, Value.Span);
    }

    internal static bool KeyIsComplex(YamlNode? key)
    {
        if (key == null)
        {
            return false;
        }

        if (key.Kind == YamlNodeKind.Sequence || key.Kind == YamlNodeKind.Mapping)
        {
            return true;
        }

        if (key.Kind == YamlNodeKind.Alias && key.AliasTarget != null)
        {
            return key.AliasTarget.Kind == YamlNodeKind.Sequence
                || key.AliasTarget.Kind == YamlNodeKind.Mapping;
        }

        return false;
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="comment">The comment text, with or without a leading number sign.</param>
    internal void AddLeadingComment(string? comment)
    {
        AddTrivia(YamlTrivia.Create(YamlTriviaKind.Comment, YamlTriviaPlacement.Before, comment, YamlSourceSpan.Empty()));
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="comment">The comment text, with or without a leading number sign.</param>
    internal void AddTrailingComment(string? comment)
    {
        AddTrivia(YamlTrivia.Create(YamlTriviaKind.Comment, YamlTriviaPlacement.Inline, comment, YamlSourceSpan.Empty()));
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="blankLine">The preserved blank-line marker.</param>
    internal void AddLeadingBlankLine(string? blankLine)
    {
        AddTrivia(YamlTrivia.Create(YamlTriviaKind.BlankLine, YamlTriviaPlacement.Before, blankLine, YamlSourceSpan.Empty()));
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="blankLine">The preserved blank-line marker.</param>
    internal void AddTrailingBlankLine(string? blankLine)
    {
        AddTrivia(YamlTrivia.Create(YamlTriviaKind.BlankLine, YamlTriviaPlacement.Following, blankLine, YamlSourceSpan.Empty()));
    }

    internal void AddTrivia(YamlTrivia? trivia)
    {
        if (trivia == null)
        {
            return;
        }

        YamlTrivia copy = trivia.Copy();
        Trivia.Add(copy);
        if (copy.Kind == YamlTriviaKind.BlankLine)
        {
            if (copy.Placement == YamlTriviaPlacement.Before)
            {
                LeadingBlankLines.Add(copy.Text);
            }
            else
            {
                TrailingBlankLines.Add(copy.Text);
            }
        }
        else if (copy.Kind == YamlTriviaKind.Comment)
        {
            if (copy.Placement == YamlTriviaPlacement.Before)
            {
                LeadingComments.Add(copy.Text);
            }
            else
            {
                TrailingComments.Add(copy.Text);
            }
        }
    }

    internal bool RemoveTrivia(int index)
    {
        if (index < 0 || index >= Trivia.Count)
        {
            return false;
        }

        YamlTrivia trivia = Trivia[index];
        Trivia.RemoveAt(index);
        if (trivia.Kind == YamlTriviaKind.BlankLine)
        {
            if (trivia.Placement == YamlTriviaPlacement.Before)
            {
                YamlTrivia.RemoveFirstText(LeadingBlankLines, trivia.Text);
            }
            else
            {
                YamlTrivia.RemoveFirstText(TrailingBlankLines, trivia.Text);
            }
        }
        else if (trivia.Kind == YamlTriviaKind.Comment)
        {
            if (trivia.Placement == YamlTriviaPlacement.Before)
            {
                YamlTrivia.RemoveFirstText(LeadingComments, trivia.Text);
            }
            else
            {
                YamlTrivia.RemoveFirstText(TrailingComments, trivia.Text);
            }
        }

        return true;
    }
}
