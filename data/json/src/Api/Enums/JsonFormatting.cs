namespace json;

/// <summary>Selects compact or indented JSON output.</summary>
public enum JsonFormatting
{
    /// <summary>Emits no insignificant whitespace.</summary>
    Compact = 0,

    /// <summary>Emits configured indentation and line breaks.</summary>
    Pretty = 1
}
