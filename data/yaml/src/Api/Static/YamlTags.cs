namespace yaml;

/// <summary>
/// Defines the expanded standard YAML tags used by schema resolution.
/// </summary>
public static class YamlTags
{
    /// <summary>The standard string tag.</summary>
    public const string String = "tag:yaml.org,2002:str";
    /// <summary>The standard null tag.</summary>
    public const string Null = "tag:yaml.org,2002:null";
    /// <summary>The standard Boolean tag.</summary>
    public const string Bool = "tag:yaml.org,2002:bool";
    /// <summary>The standard integer tag.</summary>
    public const string Int = "tag:yaml.org,2002:int";
    /// <summary>The standard floating-point tag.</summary>
    public const string Float = "tag:yaml.org,2002:float";
    /// <summary>The standard sequence tag.</summary>
    public const string Sequence = "tag:yaml.org,2002:seq";
    /// <summary>The standard mapping tag.</summary>
    public const string Mapping = "tag:yaml.org,2002:map";
    /// <summary>The standard timestamp tag.</summary>
    public const string Timestamp = "tag:yaml.org,2002:timestamp";
    /// <summary>The standard binary tag.</summary>
    public const string Binary = "tag:yaml.org,2002:binary";
    /// <summary>The standard set tag.</summary>
    public const string Set = "tag:yaml.org,2002:set";
    /// <summary>The standard ordered-map tag.</summary>
    public const string OrderedMap = "tag:yaml.org,2002:omap";
    /// <summary>The standard pairs tag.</summary>
    public const string Pairs = "tag:yaml.org,2002:pairs";
    /// <summary>The standard merge-key tag.</summary>
    public const string Merge = "tag:yaml.org,2002:merge";
}
