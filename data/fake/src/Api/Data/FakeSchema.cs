using System;

namespace fake;

/// <summary>Wraps the root of a caller-defined fake-data schema tree.</summary>
public class FakeSchema
{
    /// <summary>The root node evaluated by schema generation operations.</summary>
    public SchemaNode Root;

    /// <summary>Initializes a schema from its root node.</summary>
    /// <param name="root">The non-null root node.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>root</c> is null.</exception>
    public FakeSchema(SchemaNode root)
    {
        Guard.NotNull(root, "root");
        Root = root;
    }
}
