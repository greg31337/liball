namespace fix;

/// <summary>Describes one deterministic change between Orchestra repositories.</summary>
public class OrchestraChange
{
    /// <summary>The change kind.</summary>
    public string Kind = "";
    /// <summary>The stable changed element path.</summary>
    public string Path = "";
    /// <summary>The canonical value before the change.</summary>
    public string Before = "";
    /// <summary>The canonical value after the change.</summary>
    public string After = "";
    /// <summary>Whether behavior or wire compatibility may be broken.</summary>
    public bool IsBreaking;
}
