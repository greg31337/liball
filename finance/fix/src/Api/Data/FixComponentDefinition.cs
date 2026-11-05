using System.Collections.Generic;

namespace fix;

/// <summary>Defines a reusable ordered FIX component.</summary>
public class FixComponentDefinition
{
    /// <summary>The stable component identifier.</summary>
    public int Id;
    /// <summary>The component name.</summary>
    public string Name = "";
    /// <summary>The abbreviated component name.</summary>
    public string Abbreviation = "";
    /// <summary>The ordered members.</summary>
    public List<FixMemberDefinition> Members = [];
    /// <summary>A human-readable description.</summary>
    public string Description = "";
    /// <summary>The source repository or overlay that supplied the definition.</summary>
    public string Source = "";
    /// <summary>Whether this overlay definition removes the matching base definition.</summary>
    public bool IsRemoved;
}
