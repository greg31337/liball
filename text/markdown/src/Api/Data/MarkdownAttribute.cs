using System;

namespace markdown;

/// <summary>
/// Represents a name and value attached to an AST node or event.
/// </summary>
public class MarkdownAttribute
{
    /// <summary>
    /// The non-empty attribute name.
    /// </summary>
    public string Name;
    /// <summary>
    /// The attribute value; an empty string is allowed.
    /// </summary>
    public string Value;

    /// <summary>
    /// Initializes a new <see cref="MarkdownAttribute"/> instance.
    /// </summary>
    public MarkdownAttribute()
    {
        Name = "";
        Value = "";
    }

    /// <summary>
    /// Creates an attribute with a name and value.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="value">The value to apply.</param>
    internal static MarkdownAttribute Create(string name, string value)
    {
        ValidateName(name);

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        MarkdownAttribute attribute = new MarkdownAttribute();
        attribute.Name = name;
        attribute.Value = value;
        return attribute;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownAttribute"/> value.</returns>
    internal MarkdownAttribute Copy()
    {
        return Create(Name, Value);
    }

    /// <summary>
    /// Sets value.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    internal void SetValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        Value = value;
    }

    internal static void ValidateName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Attribute name must not be empty.");
        }
    }
}
