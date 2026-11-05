using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Configures structural or semantic comparison of SDP descriptions.
/// </summary>
public class SdpComparisonOptions
{
    /// <summary>Indicates whether effective semantic values are compared instead of exact structure.</summary>
    public bool CompareSemanticValues = false;

    /// <summary>Indicates whether the origin session version is excluded from comparison.</summary>
    public bool IgnoreOriginSessionVersion = false;

    /// <summary>Indicates whether ordering is ignored where the selected semantics permit it.</summary>
    public bool IgnoreInsignificantOrdering = false;

    /// <summary>Indicates whether retained raw formatting is excluded from comparison.</summary>
    public bool IgnoreRawFormatting = false;

    /// <summary>Contains attribute names that are explicitly excluded from comparison.</summary>
    public List<string> IgnoredAttributeNames = new List<string>();
}
