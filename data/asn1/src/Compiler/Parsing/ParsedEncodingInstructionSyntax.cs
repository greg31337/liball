namespace asn1;

internal class ParsedEncodingInstructionSyntax
{
    internal AsnEncodingInstruction Instruction = new AsnEncodingInstruction();
    internal AsnToken[] ArgumentTokens = [];
    internal ParsedPerEncodingInstructionTargetSyntax[] PerTargets = [];
    internal string NamespaceUri = "";
    internal string NamespacePrefix = "";
    internal bool ControlNamespace;
    internal bool ModifiedEncodings;
}
