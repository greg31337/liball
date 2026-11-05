namespace cbor;

/// <summary>Traverses compiled CDDL schemas for downstream emitters.</summary>
public interface ICddlSchemaEmitter
{
    /// <summary>Emits a schema through the supplied traversal sink.</summary>
    /// <param name="schema">The compiled schema.</param>
    /// <param name="sink">The traversal event sink.</param>
    /// <param name="options">The traversal filtering and naming policies.</param>
    void EmitSchema(
        CddlSchema schema,
        ICddlSchemaSink sink,
        CddlEmissionOptions options);
}
