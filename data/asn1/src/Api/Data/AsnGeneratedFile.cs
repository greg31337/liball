namespace asn1;

/// <summary>
/// Contains one generated output and its source relationships.
/// </summary>
public class AsnGeneratedFile
{
    /// <summary>
    /// Contains the stable relative output path.
    /// </summary>
    public string RelativePath = "";

    /// <summary>
    /// Contains the generated text.
    /// </summary>
    public string Content = "";

    /// <summary>
    /// Contains source spans that contributed to the generated output.
    /// </summary>
    public AsnSourceSpan[] SourceSpans = [];

    /// <summary>
    /// Contains non-fatal diagnostics associated with this generated output.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];
}
