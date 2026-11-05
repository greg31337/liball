using System.Collections.Generic;

namespace fix;

/// <summary>Defines one FIX message type and scenario.</summary>
public class FixMessageDefinition
{
    /// <summary>The stable message identifier.</summary>
    public int Id;
    /// <summary>The canonical message name.</summary>
    public string Name = "";
    /// <summary>The exact MsgType value.</summary>
    public string MsgType = "";
    /// <summary>The business or session category.</summary>
    public string Category = "";
    /// <summary>The scenario name.</summary>
    public string Scenario = "";
    /// <summary>The ordered message members.</summary>
    public List<FixMemberDefinition> Members = [];
    /// <summary>A human-readable description.</summary>
    public string Description = "";
    /// <summary>The source repository or overlay that supplied the definition.</summary>
    public string Source = "";
    /// <summary>Whether this overlay definition removes the matching base definition.</summary>
    public bool IsRemoved;
}
