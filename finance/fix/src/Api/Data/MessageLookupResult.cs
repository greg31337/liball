using System.Collections.Generic;

namespace fix;

/// <summary>Contains deterministic dictionary message name or category lookup results.</summary>
public class MessageLookupResult
{
    /// <summary>Whether at least one unambiguous definition matched the lookup.</summary>
    public bool Success;
    /// <summary>The matching message definitions in deterministic order.</summary>
    public List<FixMessageDefinition> Definitions = [];
    /// <summary>The selected dictionary identity and scenario.</summary>
    public FixDictionaryIdentity Identity = new FixDictionaryIdentity();
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
}
