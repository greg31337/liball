namespace uri;

/// <summary>
/// Selects explicit Unicode normalization behavior for internationalized text.
/// </summary>
public enum UnicodeNormalizationMode
{
    /// <summary>Preserve the supplied Unicode scalar sequence unchanged.</summary>
    Preserve = 0,

    /// <summary>Normalize producer-side text to Unicode Normalization Form C.</summary>
    NormalizeNfc = 1,

    /// <summary>Diagnose text that is not in Form C without changing it.</summary>
    DiagnoseNfc = 2
}
