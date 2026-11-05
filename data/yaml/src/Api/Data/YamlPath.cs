using System.Collections.Generic;
using System.Text;

namespace yaml;

/// <summary>
/// Represents one mapping-key or sequence-index step in a YAML path.
/// </summary>
public class YamlPathSegment
{
    /// <summary>
    /// The kind of token, event, node, or path segment represented by this instance.
    /// </summary>
    public YamlPathSegmentKind Kind;
    /// <summary>
    /// The mapping key node.
    /// </summary>
    public string Key;
    /// <summary>
    /// The zero-based source or sequence index represented by this value.
    /// </summary>
    public int Index;

    /// <summary>
    /// Initializes a new YAML path segment with empty or default state.
    /// </summary>
    public YamlPathSegment()
    {
        Kind = YamlPathSegmentKind.Unknown;
        Key = "";
        Index = -1;
    }

    /// <summary>
    /// Creates a mapping node with the requested style or initial pair.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <returns>The resulting YAML path segment.</returns>
    internal static YamlPathSegment MappingKey(string? key)
    {
        YamlPathSegment segment = new YamlPathSegment();
        segment.Kind = YamlPathSegmentKind.MappingKey;
        segment.Key = key == null ? "" : key;
        segment.Index = -1;
        return segment;
    }

    /// <summary>
    /// Creates a sequence node with the requested style or initial child.
    /// </summary>
    /// <param name="index">The zero-based source or sequence index.</param>
    /// <returns>The resulting YAML path segment.</returns>
    internal static YamlPathSegment SequenceIndex(int index)
    {
        YamlPathSegment segment = new YamlPathSegment();
        segment.Kind = YamlPathSegmentKind.SequenceIndex;
        segment.Key = "";
        segment.Index = index;
        return segment;
    }

    /// <summary>
    /// Determines whether this segment selects a mapping key.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsMappingKey()
    {
        return Kind == YamlPathSegmentKind.MappingKey;
    }

    /// <summary>
    /// Determines whether this segment selects a sequence index.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsSequenceIndex()
    {
        return Kind == YamlPathSegmentKind.SequenceIndex;
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML path segment.</returns>
    internal YamlPathSegment Copy()
    {
        YamlPathSegment copy = new YamlPathSegment();
        copy.Kind = Kind;
        copy.Key = Key;
        copy.Index = Index;
        return copy;
    }
}

/// <summary>
/// Represents a parsed path through string mapping keys and zero-based sequence indexes.
/// </summary>
public class YamlPath
{
    /// <summary>
    /// The navigation segments in path order.
    /// </summary>
    public List<YamlPathSegment> Segments;
    /// <summary>
    /// The original path text.
    /// </summary>
    public string Source;
    /// <summary>
    /// Whether the path text was parsed successfully.
    /// </summary>
    public bool Valid;
    /// <summary>
    /// A plain-English path parsing error, or an empty string for a valid path.
    /// </summary>
    public string Error;

    /// <summary>
    /// Initializes a new YAML path with empty or default state.
    /// </summary>
    public YamlPath()
    {
        Segments = new List<YamlPathSegment>();
        Source = "";
        Valid = true;
        Error = "";
    }

    /// <summary>
    /// Creates a valid path that selects the current root node.
    /// </summary>
    /// <returns>The resulting YAML path.</returns>
    internal static YamlPath Root()
    {
        return new YamlPath();
    }

    /// <summary>
    /// Returns the number of stored items.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int Count()
    {
        return Segments.Count;
    }

    /// <summary>
    /// Determines whether the path has no navigation segments and therefore selects the root.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsRoot()
    {
        return Segments.Count == 0;
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    internal void AddKey(string? key)
    {
        Segments.Add(YamlPathSegment.MappingKey(key));
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="index">The zero-based source or sequence index.</param>
    internal void AddIndex(int index)
    {
        Segments.Add(YamlPathSegment.SequenceIndex(index));
    }

    /// <summary>
    /// Returns the requested stored value, or null when it is unavailable.
    /// </summary>
    /// <param name="index">The zero-based source or sequence index.</param>
    /// <returns>The resulting YAML path segment.</returns>
    internal YamlPathSegment? GetSegment(int index)
    {
        if (index < 0 || index >= Segments.Count)
        {
            return null;
        }

        return Segments[index];
    }

    /// <summary>
    /// Returns the final segment, or null when this path selects the root.
    /// </summary>
    /// <returns>The resulting YAML path segment.</returns>
    internal YamlPathSegment? LastSegment()
    {
        if (Segments.Count == 0)
        {
            return null;
        }

        return Segments[Segments.Count - 1];
    }

    /// <summary>
    /// Marks the path invalid and stores a plain-English parsing error.
    /// </summary>
    /// <param name="message">The plain-English diagnostic or validation message.</param>
    internal void MarkInvalid(string? message)
    {
        Valid = false;
        Error = message == null ? "" : message;
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML path.</returns>
    internal YamlPath Copy()
    {
        YamlPath copy = new YamlPath();
        copy.Source = Source;
        copy.Valid = Valid;
        copy.Error = Error;
        int index = 0;
        while (index < Segments.Count)
        {
            copy.Segments.Add(Segments[index].Copy());
            index = index + 1;
        }

        return copy;
    }

    /// <summary>
    /// Reconstructs the normalized dotted and bracketed path notation.
    /// </summary>
    /// <returns>The emitted or formatted text.</returns>
    internal string ToText()
    {
        if (Segments.Count == 0)
        {
            return "$";
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < Segments.Count)
        {
            YamlPathSegment segment = Segments[index];
            if (segment.Kind == YamlPathSegmentKind.MappingKey)
            {
                if (builder.Length > 0)
                {
                    builder.Append(".");
                }

                builder.Append(segment.Key);
            }
            else if (segment.Kind == YamlPathSegmentKind.SequenceIndex)
            {
                builder.Append("[");
                builder.Append(segment.Index.ToString());
                builder.Append("]");
            }

            index = index + 1;
        }

        return builder.ToString();
    }
}
