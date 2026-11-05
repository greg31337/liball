namespace fake;

/// <summary>Selects the top-level shape of generated JSON text.</summary>
public enum JsonTextKind
{
    /// <summary>Generates a JSON scalar value.</summary>
    Scalar = 0,

    /// <summary>Generates a JSON object value.</summary>
    Object = 1,

    /// <summary>Generates a JSON array value.</summary>
    Array = 2
}
