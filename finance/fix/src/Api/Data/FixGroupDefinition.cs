using System.Collections.Generic;

namespace fix;

/// <summary>Defines one structured FIX repeating group.</summary>
public class FixGroupDefinition
{
    /// <summary>The stable group identifier.</summary>
    public int Id;
    /// <summary>The group name.</summary>
    public string Name = "";
    /// <summary>The tag that carries the entry count.</summary>
    public int CountTag;
    /// <summary>The first field tag required in each non-empty entry.</summary>
    public int DelimiterTag;
    /// <summary>The ordered entry members.</summary>
    public List<FixMemberDefinition> Members = [];
    /// <summary>A human-readable description.</summary>
    public string Description = "";
    /// <summary>The source repository or overlay that supplied the definition.</summary>
    public string Source = "";
    /// <summary>Whether this overlay definition removes the matching base definition.</summary>
    public bool IsRemoved;
}
