namespace ttf;

/// <summary>Describes the effect of a font diagnostic.</summary>
public enum FontDiagnosticSeverity
{
    /// <summary>The diagnostic records valid metadata or a harmless repair.</summary>
    Information = 0,

    /// <summary>The input was accepted, but a caller may need to review it.</summary>
    Warning = 1,

    /// <summary>The affected semantic operation cannot be completed.</summary>
    Error = 2,

    /// <summary>The font or container cannot be loaded safely.</summary>
    Fatal = 3
}
