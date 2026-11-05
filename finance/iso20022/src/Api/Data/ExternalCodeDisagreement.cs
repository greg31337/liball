namespace iso20022;

/// <summary>Describes one deterministic disagreement between external-code publication formats.</summary>
public class ExternalCodeDisagreement
{
    /// <summary>The structural disagreement category.</summary>
    public ExternalCodeDisagreementKind Kind;
    /// <summary>A stable machine-readable disagreement code.</summary>
    public string Code = string.Empty;
    /// <summary>The affected code-set identifier, or an empty value for catalogue-level differences.</summary>
    public string CodeSetIdentifier = string.Empty;
    /// <summary>The affected code value, or an empty value for code-set-level differences.</summary>
    public string CodeValue = string.Empty;
    /// <summary>The affected normalized field name.</summary>
    public string FieldName = string.Empty;
    /// <summary>Whether the compared JSON format contains the affected item.</summary>
    public bool JsonPresent;
    /// <summary>The normalized JSON value or occurrence count.</summary>
    public string JsonValue = string.Empty;
    /// <summary>Whether the compared XSD format contains the affected item.</summary>
    public bool XsdPresent;
    /// <summary>The normalized XSD value or occurrence count.</summary>
    public string XsdValue = string.Empty;
    /// <summary>Whether the compared XLSX format contains the affected item.</summary>
    public bool XlsxPresent;
    /// <summary>The normalized XLSX value or occurrence count.</summary>
    public string XlsxValue = string.Empty;
}
