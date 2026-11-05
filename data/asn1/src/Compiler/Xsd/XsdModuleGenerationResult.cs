namespace asn1;

internal class XsdModuleGenerationResult
{
    internal bool Success;
    internal XsdGeneratedModule[] Modules = [];
    internal AsnSourceUnit[] SourceUnits = [];
    internal XsdGeneratedSourceMapEntry[] SourceMap = [];
    internal AsnDiagnostic[] Diagnostics = [];
}
