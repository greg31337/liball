namespace asn1;

internal class PerEncodingInstructionParseResult
{
    internal ParsedEncodingInstructionSyntax[] Instructions = [];
    internal bool Success = true;
    internal string ErrorMessage = "";
    internal AsnToken? ErrorToken;
}
