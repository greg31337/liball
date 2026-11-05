using System;

namespace fake;

/// <summary>Associates a deterministic object-field name with a schema node.</summary>
public class SchemaField
{
    /// <summary>The field name placed in the generated object.</summary>
    public string Name = string.Empty;

    /// <summary>The node evaluated to obtain the field value.</summary>
    public SchemaNode Node;

    /// <summary>Initializes a named schema field.</summary>
    /// <param name="name">The nonempty field name.</param>
    /// <param name="node">The node that supplies the field value.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public SchemaField(string name, SchemaNode node)
    {
        Guard.NotNullOrWhiteSpace(name, "name");
        Guard.NotNull(node, "node");
        Name = name;
        Node = node;
    }
}
