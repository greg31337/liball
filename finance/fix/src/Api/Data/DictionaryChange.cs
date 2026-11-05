namespace fix;

/// <summary>Describes one deterministic change between FIX dictionaries.</summary>
public class DictionaryChange
{
    /// <summary>The change kind, such as <c>add</c>, <c>remove</c>, or <c>replace</c>.</summary>
    public string Kind = "";
    /// <summary>The stable path of the changed definition or member.</summary>
    public string Path = "";
    /// <summary>The canonical value before the change.</summary>
    public string Before = "";
    /// <summary>The canonical value after the change.</summary>
    public string After = "";
    /// <summary>Whether the change may reject messages accepted by the earlier dictionary.</summary>
    public bool IsBreaking;
}
