using System.Collections.Generic;

namespace cbor;

/// <summary>Holds CDDL schema compilation policies and limits.</summary>
public class CddlCompileOptions
{
    /// <summary>Indicates whether the RFC standard prelude is included.</summary>
    public bool IncludeStandardPrelude = true;
    /// <summary>An explicit root rule name, or null to select the first base rule.</summary>
    public string? RootRuleName;
    /// <summary>An optional resolver for external rule definitions.</summary>
    public ICddlRuleResolver? RuleResolver;
    /// <summary>Custom control registrations available during compilation.</summary>
    public List<CddlControlRegistration> ControlRegistrations =
        new List<CddlControlRegistration>();
    /// <summary>Specifies how unregistered control operators are handled.</summary>
    public CddlUnknownControlPolicy UnknownControlPolicy =
        CddlUnknownControlPolicy.Reject;
    /// <summary>The maximum number of returned diagnostics.</summary>
    public int MaxDiagnostics = 100;
    /// <summary>The maximum number of normalized schema nodes.</summary>
    public int MaxNodes = 100000;
    /// <summary>The maximum number of generic instantiations.</summary>
    public int MaxGenericInstantiations = 10000;
    /// <summary>The maximum rule-reference depth examined during compilation.</summary>
    public int MaxReferenceDepth = 256;
}
