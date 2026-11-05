using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents a complete Structured Field item, list, or dictionary.
/// </summary>
public class StructuredFieldValue
{
    /// <summary>
    /// Indicates that the value is a single item.
    /// </summary>
    public bool IsItem;

    /// <summary>
    /// Indicates that the value is a list.
    /// </summary>
    public bool IsList;

    /// <summary>
    /// Indicates that the value is a dictionary.
    /// </summary>
    public bool IsDictionary;

    /// <summary>
    /// The single item, or <c>null</c> for a list or dictionary.
    /// </summary>
    public StructuredFieldMember? Item;

    /// <summary>
    /// The ordered list or dictionary members.
    /// </summary>
    public List<StructuredFieldMember> Members = new List<StructuredFieldMember>();
}
