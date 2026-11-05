using System.Collections.Generic;

namespace fix;

/// <summary>Defines a named set of legal FIX field values.</summary>
public class FixCodeSetDefinition
{
    /// <summary>The stable source identifier.</summary>
    public int Id;
    /// <summary>The code-set name.</summary>
    public string Name = "";
    /// <summary>The lexical type shared by the code values.</summary>
    public FixFieldType FieldType;
    /// <summary>The code values in deterministic source order.</summary>
    public List<FixCodeDefinition> Codes = [];
    /// <summary>A human-readable description.</summary>
    public string Description = "";
    /// <summary>The source repository or overlay that supplied the definition.</summary>
    public string Source = "";
    /// <summary>Whether this overlay definition removes the matching base definition.</summary>
    public bool IsRemoved;
}
