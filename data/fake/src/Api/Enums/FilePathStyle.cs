namespace fake;

/// <summary>Selects the syntax used for generated directory and file paths.</summary>
public enum FilePathStyle
{
    /// <summary>Uses Unix roots and forward-slash separators.</summary>
    Unix = 0,

    /// <summary>Uses Windows drive roots and backslash separators.</summary>
    Windows = 1
}
