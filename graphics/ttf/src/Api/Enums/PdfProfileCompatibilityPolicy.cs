namespace ttf;

/// <summary>Controls handling of requested PDF/A and PDF/UA findings.</summary>
public enum PdfProfileCompatibilityPolicy
{
    /// <summary>Return requested-profile findings in the resource report.</summary>
    Report = 0,

    /// <summary>Reject a resource with a requested-profile finding.</summary>
    Reject = 1
}
