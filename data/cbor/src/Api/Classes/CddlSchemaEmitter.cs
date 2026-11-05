namespace cbor;

/// <summary>Traverses compiled CDDL schemas for downstream emitters.</summary>
public class CddlSchemaEmitter : ICddlSchemaEmitter
{
    internal object EmissionLock = new object();

    /// <summary>Creates a CDDL schema emitter.</summary>
    public CddlSchemaEmitter()
    {
    }

    /// <summary>Emits a schema through the supplied traversal sink.</summary>
    /// <param name="schema">The compiled schema.</param>
    /// <param name="sink">The traversal event sink.</param>
    /// <param name="options">The traversal filtering and naming policies.</param>
    public void EmitSchema(
        CddlSchema schema,
        ICddlSchemaSink sink,
        CddlEmissionOptions options)
    {
        if (schema == null)
        {
            throw new System.ArgumentNullException("schema");
        }
        if (sink == null)
        {
            throw new System.ArgumentNullException("sink");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (System.Threading.Monitor.IsEntered(EmissionLock))
        {
            throw new System.InvalidOperationException(
                "A CDDL schema emitter cannot be reentered during an active traversal."
            );
        }
        lock (EmissionLock)
        {
            CddlSchemaEmissionState state = new CddlSchemaEmissionState(
                schema,
                sink,
                options
            );
            state.EmitTraversal();
        }
    }
}
