namespace fix;

/// <summary>Defines one value in a FIX code set.</summary>
public class FixCodeDefinition
{
    /// <summary>The symbolic code name.</summary>
    public string Name = "";
    /// <summary>The exact lexical value carried on the wire.</summary>
    public string Value = "";
    /// <summary>The stable source identifier when one is available.</summary>
    public int Id;
    /// <summary>The source-defined display order.</summary>
    public int SortOrder;
    /// <summary>A human-readable description.</summary>
    public string Description = "";
    /// <summary>The source repository or overlay that supplied the code.</summary>
    public string Source = "";
    /// <summary>Whether this overlay code removes the matching base code.</summary>
    public bool IsRemoved;
}
