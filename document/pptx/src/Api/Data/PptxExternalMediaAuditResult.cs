using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Summarizes externally linked audio and video targets in a presentation.
/// </summary>
public class PptxExternalMediaAuditResult
{
    /// <summary>
    /// Contains the external links in slide and relationship order.
    /// </summary>
    public List<PptxExternalMediaLink> Links;
    /// <summary>
    /// Stores the number of external media relationships.
    /// </summary>
    public int ExternalCount;
    /// <summary>
    /// Stores the number of external targets that do not use HTTPS.
    /// </summary>
    public int InsecureCount;
    /// <summary>
    /// Stores the number of external relationships with an empty target.
    /// </summary>
    public int MissingTargetCount;

    /// <summary>
    /// Initializes an empty external-media audit result.
    /// </summary>
    public PptxExternalMediaAuditResult()
    {
        Links = new List<PptxExternalMediaLink>();
        ExternalCount = 0;
        InsecureCount = 0;
        MissingTargetCount = 0;
    }
}
