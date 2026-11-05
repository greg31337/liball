namespace asn1;

/// <summary>
/// Contains ASN.1 source units and diagnostics produced by XML Schema mapping.
/// </summary>
public class AsnXsdImportResult
{
    /// <summary>
    /// Indicates whether mapping completed without error diagnostics.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains generated ASN.1 source units.
    /// </summary>
    public AsnSourceUnit[] SourceUnits = [];

    /// <summary>
    /// Contains XML Schema mapping diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];
}
