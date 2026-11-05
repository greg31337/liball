namespace xml;

/// <summary>
/// Specifies an XML canonicalization algorithm.
/// </summary>
public enum XmlCanonicalizationMethod
{
    /// <summary>
    /// Uses Canonical XML 1.1.
    /// </summary>
    CanonicalXml11 = 0,
    /// <summary>
    /// Uses Exclusive XML Canonicalization 1.0.
    /// </summary>
    ExclusiveXml10 = 1,
    /// <summary>
    /// Uses the project experimental profile inspired by the Canonical XML 2.0 Working Group Note, without claiming Recommendation conformance.
    /// </summary>
    CanonicalXml20Experimental = 2
}
