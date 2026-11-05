namespace asn1;

/// <summary>
/// Maps deterministic XML Schema inputs into ASN.1 source units.
/// </summary>
public interface IXsdSchemaImporter
{
    /// <summary>
    /// Imports a read-only span of XML Schema inputs.
    /// </summary>
    /// <param name="inputs">The XML Schema inputs and resolver data.</param>
    /// <param name="options">The import options.</param>
    /// <returns>The XML Schema import result.</returns>
    public AsnXsdImportResult ImportSchemas(System.ReadOnlySpan<AsnXsdInput> inputs, AsnXsdImportOptions options);
}
