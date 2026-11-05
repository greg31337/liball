namespace odf;

/// <summary>Identifies a reusable presentation declaration.</summary>
public enum OdfPresentationDeclarationKind
{
    /// <summary>The declaration kind is unrecognized.</summary>
    Unknown = 0,
    /// <summary>A presentation header declaration.</summary>
    Header = 1,
    /// <summary>A presentation footer declaration.</summary>
    Footer = 2,
    /// <summary>A presentation date-and-time declaration.</summary>
    DateTime = 3
}
