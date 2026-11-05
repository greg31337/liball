namespace ttf;

/// <summary>Controls handling of a PDF version below the selected font strategy.</summary>
public enum PdfVersionCompatibilityPolicy
{
    /// <summary>Reject a resource unsupported by the target PDF version.</summary>
    Reject = 0,

    /// <summary>Build the neutral artifact and report the version mismatch.</summary>
    Report = 1
}
