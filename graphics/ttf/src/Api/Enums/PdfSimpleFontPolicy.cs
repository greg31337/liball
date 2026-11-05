namespace ttf;

/// <summary>Controls availability of the restricted simple TrueType path.</summary>
public enum PdfSimpleFontPolicy
{
    /// <summary>Allow a simple font only when explicitly selected.</summary>
    ExplicitOnly = 0,

    /// <summary>Disallow simple-font resource generation.</summary>
    Disallow = 1
}
