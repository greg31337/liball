using System.Collections.Generic;

namespace cbor;

/// <summary>Represents one ordered CDDL rule definition or extension.</summary>
public class CddlRule
{
    /// <summary>The rule name without a socket prefix.</summary>
    public string Name = "";
    /// <summary>The definition or extension kind.</summary>
    public CddlRuleKind Kind = CddlRuleKind.None;
    /// <summary>The rule body.</summary>
    public CddlNode? Value;
    /// <summary>The generic parameter names in source order.</summary>
    public List<string> GenericParameters = new List<string>();
    /// <summary>Indicates whether the rule came from the standard prelude.</summary>
    public bool IsPrelude;
    /// <summary>The zero-based order of the rule in its source set.</summary>
    public int SourceOrder;
    /// <summary>The exact source range occupied by the rule.</summary>
    public CddlSourceSpan Span = new CddlSourceSpan();
}
