namespace asn1;

internal class ModuleSyntax
{
    internal string Name = "";
    internal DefinitiveOidComponentSyntax[] DefinitiveOidComponents = [];
    internal bool HasDefinitiveOid;
    internal string DefinitiveIri = "";
    internal bool HasDefinitiveIri;
    internal bool HasVersion;
    internal string VersionModule = "";
    internal string Version = "";
    internal string EncodingReference = "TAG";
    internal bool EncodingReferenceSpecified;
    internal AsnTaggingMode TagDefault = AsnTaggingMode.Explicit;
    internal bool TagDefaultSpecified;
    internal bool ExtensibilityImplied;
    internal bool ExportsSpecified;
    internal bool ExportsAll = true;
    internal ImportExportSymbolSyntax[] ExportedSymbols = [];
    internal bool ImportsSpecified;
    internal ImportedModuleSyntax[] ImportedModules = [];
    internal AsnToken[] BodyTokens = [];
    internal AssignmentHeadSyntax[] AssignmentHeads = [];
    internal SymbolTable? SymbolTable;
    internal EncodingControlSectionSyntax[] EncodingControlSections = [];
    internal bool XerModifiedEncodings;
    internal string XerControlNamespaceUri = "urn:oid:2.1.5.2.0.1";
    internal string XerControlNamespacePrefix = "asn1";
    internal XsdSourceMappingSyntax[] XsdSourceMappings = [];
    internal ParsedRecoverySyntax[] Recoveries = [];
    internal AsnSourceSpan? Span;
}
