namespace asn1;

internal class EncodingControlSectionSyntax
{
    internal string EncodingReference = "";
    internal AsnToken[] InstructionTokens = [];
    internal ParsedEncodingInstructionSyntax[] Instructions = [];
    internal EcnAssignmentSyntax[] EcnAssignments = [];
    internal ParsedRecoverySyntax[] Recoveries = [];
    internal AsnSourceSpan? Span;
}
