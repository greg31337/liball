namespace ttf;

/// <summary>Identifies the structural compatibility mode used while loading.</summary>
public enum FontLoadMode
{
    /// <summary>Reject every violated invariant required by supported specifications.</summary>
    Strict = 0,

    /// <summary>Accept only safe, deterministic compatibility repairs and report each one.</summary>
    Compatible = 1,

    /// <summary>Expose bounded raw and metadata views without trusting invalid semantics.</summary>
    InspectOnly = 2
}
