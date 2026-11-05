namespace fix;

/// <summary>Controls secure, bounded Orchestra XML reading.</summary>
public class OrchestraReadOptions
{
    /// <summary>Whether supported schemas are validated.</summary>
    public bool ValidateSchema = true;
    /// <summary>Whether behavior content is loaded.</summary>
    public bool IncludeBehavior = true;
    /// <summary>Whether interface content is loaded.</summary>
    public bool IncludeInterfaces = true;
    /// <summary>Whether XInclude references are resolved.</summary>
    public bool ResolveXInclude;
    /// <summary>Whether external entities are permitted.</summary>
    public bool AllowExternalEntities;
    /// <summary>The maximum XML element depth.</summary>
    public int MaxXmlDepth = FixDefaults.MaxXmlDepth;
    /// <summary>The maximum source size in bytes.</summary>
    public int MaxSourceSize = FixDefaults.MaxMessageSize;
    /// <summary>The base URI used for explicitly enabled includes.</summary>
    public string BaseUri = "";
}
