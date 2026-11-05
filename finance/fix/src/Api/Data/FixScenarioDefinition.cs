using System.Collections.Generic;

namespace fix;

/// <summary>Defines a named scenario and its specialized members and rules.</summary>
public class FixScenarioDefinition
{
    /// <summary>The scenario name.</summary>
    public string Name = "";
    /// <summary>The inherited scenario name, or an empty string when none exists.</summary>
    public string ParentName = "";
    /// <summary>The specialized ordered members.</summary>
    public List<FixMemberDefinition> Members = [];
    /// <summary>The conditional or assignment expressions in source order.</summary>
    public List<string> Rules = [];
    /// <summary>A human-readable description.</summary>
    public string Description = "";
    /// <summary>The source repository or overlay that supplied the definition.</summary>
    public string Source = "";
    /// <summary>Whether this overlay definition removes the matching base definition.</summary>
    public bool IsRemoved;
}
