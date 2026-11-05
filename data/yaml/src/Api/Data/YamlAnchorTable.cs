using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Tracks the anchors declared in one YAML document and resolves alias names to nodes.
/// </summary>
public class YamlAnchorTable
{
    /// <summary>
    /// The anchor names in declaration order.
    /// </summary>
    public List<string> Names;
    /// <summary>
    /// The nodes corresponding to <see cref="Names"/>.
    /// </summary>
    public List<YamlNode> Nodes;
    /// <summary>
    /// The source spans corresponding to <see cref="Names"/>.
    /// </summary>
    public List<YamlSourceSpan> Spans;

    /// <summary>
    /// Initializes a new YAML anchor table with empty or default state.
    /// </summary>
    public YamlAnchorTable()
    {
        Names = new List<string>();
        Nodes = new List<YamlNode>();
        Spans = new List<YamlSourceSpan>();
    }

    /// <summary>
    /// Registers an anchor name for a node, replacing an earlier binding with the same name.
    /// </summary>
    /// <param name="name">The directive, anchor, field, or type name.</param>
    /// <param name="node">The YAML node to process.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>True when the anchor was registered; false when the name or node is invalid.</returns>
    internal bool Register(string? name, YamlNode? node, YamlSourceSpan? span)
    {
        if (name == null || name == "" || node == null)
        {
            return false;
        }

        int index = 0;
        while (index < Names.Count)
        {
            if (Names[index] == name)
            {
                Nodes[index] = node;
                Spans[index] = span == null ? YamlSourceSpan.Empty() : span;
                return true;
            }

            index = index + 1;
        }

        Names.Add(name);
        Nodes.Add(node);
        Spans.Add(span == null ? YamlSourceSpan.Empty() : span);
        return true;
    }

    /// <summary>
    /// Determines whether a matching value is present.
    /// </summary>
    /// <param name="name">The directive, anchor, field, or type name.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool Contains(string name)
    {
        int index = 0;
        while (index < Names.Count)
        {
            if (Names[index] == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Finds the first matching value, or returns null when no match exists.
    /// </summary>
    /// <param name="name">The directive, anchor, field, or type name.</param>
    /// <returns>The resulting YAML node.</returns>
    internal YamlNode? Find(string name)
    {
        int index = 0;
        while (index < Names.Count)
        {
            if (Names[index] == name && index < Nodes.Count)
            {
                return Nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Returns the number of stored items.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int Count()
    {
        return Names.Count;
    }

    /// <summary>
    /// Removes all stored entries and returns this instance to an empty state.
    /// </summary>
    internal void Clear()
    {
        Names.Clear();
        Nodes.Clear();
        Spans.Clear();
    }
}
