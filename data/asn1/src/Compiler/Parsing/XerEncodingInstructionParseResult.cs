namespace asn1;

internal class XerEncodingInstructionParseResult
{
    internal ParsedEncodingInstructionSyntax[] Instructions = [];
    internal bool Success = true;
    internal string ErrorMessage = "";
    internal AsnToken? ErrorToken;
}
