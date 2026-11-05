namespace odf;

internal class PackageValidationResult
{
    internal bool IsValid;
    internal OdfDiagnostic[] Diagnostics = [];
    internal OdfSignatureContainer[] SignatureContainers = [];
    internal EmbeddedPackageNode[] EmbeddedPackages = [];
}
