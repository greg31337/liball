namespace languagecodes;

/// <summary>Identifies how normalization treats macrolanguage relationships.</summary>
public enum MacrolanguageNormalizationPolicy
{
    /// <summary>Preserve the resolved individual or macrolanguage code.</summary>
    Preserve = 0,
    /// <summary>Replace an individual language with its macrolanguage when one exists.</summary>
    FoldToMacrolanguage = 1
}
