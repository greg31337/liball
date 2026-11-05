namespace iso20022;

/// <summary>Identifies an explicit repair applied during lenient namespace processing.</summary>
public enum NamespaceRepairKind
{
    /// <summary>Surrounding XML whitespace was removed.</summary>
    TrimXmlWhitespace = 0,
    /// <summary>A trailing slash on an ISO 20022 message namespace was removed.</summary>
    RemoveTrailingSlash = 1,
    /// <summary>The ISO 20022 technical namespace prefix was restored to its registered casing.</summary>
    NormalizeTechnicalPrefixCase = 2,
    /// <summary>The four-letter business-area component was converted to lowercase ASCII.</summary>
    NormalizeBusinessAreaCase = 3
}
