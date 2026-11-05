namespace asn1;

internal class CSharpGenerationResult
{
    internal CSharpGenerationPlan Plan = new CSharpGenerationPlan();
    internal CSharpGenerationCapabilities Capabilities =
        new CSharpGenerationCapabilities();
    internal CSharpGeneratedSource[] Sources = [];
    internal AsnDiagnostic[] Diagnostics = [];
}

internal class CSharpGenerationCapabilities
{
    internal bool Models;
    internal bool Descriptors;
    internal bool Validators;
    internal bool Codecs;
    internal bool Encode;
    internal bool Decode;
}

internal class CSharpGeneratedSource
{
    internal CSharpFilePartition Partition = new CSharpFilePartition();
    internal string Content = "";
    internal AsnSourceSpan[] SourceSpans = [];
    internal CSharpNameMapping[] NameMappings = [];
    internal AsnDiagnostic[] Diagnostics = [];
}
