namespace iso20022;

internal class EcoreMetamodelDiscoveryResult
{
    internal bool Success;
    internal QualifiedNameData RootName = new QualifiedNameData();
    internal string RootIdentifier = string.Empty;
    internal string XmiVersion = string.Empty;
    internal string MetamodelNamespace = string.Empty;
    internal string MetamodelVersion = string.Empty;
    internal string AdapterIdentifier = string.Empty;
    internal Iso20022Ecore2013Adapter? Adapter;
    internal SourceLocation Location = new SourceLocation();
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
