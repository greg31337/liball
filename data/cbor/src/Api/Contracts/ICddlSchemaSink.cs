namespace cbor;

/// <summary>Receives stable ordered events while a compiled CDDL schema is traversed.</summary>
public interface ICddlSchemaSink
{
    /// <summary>Begins a schema traversal.</summary>
    /// <param name="context">The initial emission context.</param>
    void BeginSchema(CddlEmissionContext context);

    /// <summary>Accepts one rule event.</summary>
    /// <param name="context">The rule emission context.</param>
    void AcceptRule(CddlEmissionContext context);

    /// <summary>Accepts one node event.</summary>
    /// <param name="context">The node emission context.</param>
    void AcceptNode(CddlEmissionContext context);

    /// <summary>Ends a schema traversal.</summary>
    /// <param name="context">The final emission context.</param>
    void EndSchema(CddlEmissionContext context);
}
