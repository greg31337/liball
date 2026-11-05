namespace cbor;

/// <summary>Specifies how unregistered CDDL control operators are handled.</summary>
public enum CddlUnknownControlPolicy
{
    /// <summary>Rejects an unregistered or disabled control operator.</summary>
    Reject = 0,
    /// <summary>Ignores the operator after its target has matched.</summary>
    Ignore = 1
}
