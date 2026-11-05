namespace fix;

/// <summary>Controls deterministic Orchestra XML writing.</summary>
public class OrchestraWriteOptions
{
    /// <summary>The target Orchestra schema version.</summary>
    public OrchestraVersion Version = OrchestraVersion.Version11;
    /// <summary>Whether definitions are written in canonical order.</summary>
    public bool CanonicalOrder = true;
    /// <summary>Whether behavior content is written.</summary>
    public bool IncludeBehavior = true;
    /// <summary>Whether interface content is written.</summary>
    public bool IncludeInterfaces = true;
    /// <summary>Whether output is indented.</summary>
    public bool Indent = true;
    /// <summary>The output encoding registered name.</summary>
    public string EncodingName = "utf-8";
}
