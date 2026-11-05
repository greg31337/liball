namespace asn1;

/// <summary>
/// Exposes XML Schema importing through a composed importer implementation.
/// </summary>
public class XsdSchemaImporter : IXsdSchemaImporter
{
    internal IXsdSchemaImporter Implementation;

    /// <summary>
    /// Initializes an XML Schema importer facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that imports schemas.</param>
    public XsdSchemaImporter(IXsdSchemaImporter implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Imports XML Schema inputs into ASN.1 source units.
    /// </summary>
    /// <param name="inputs">The XML Schema inputs.</param>
    /// <param name="options">The import options.</param>
    /// <returns>The import result.</returns>
    public AsnXsdImportResult ImportSchemas(System.ReadOnlySpan<AsnXsdInput> inputs, AsnXsdImportOptions options)
    {
        return this.Implementation.ImportSchemas(inputs, options);
    }
}
