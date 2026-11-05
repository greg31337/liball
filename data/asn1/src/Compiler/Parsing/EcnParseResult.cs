namespace asn1;

internal class EcnParseResult
{
    internal bool Success;
    internal EcnModuleSyntax? Module;
    internal EcnAssignmentSyntax[] Assignments = [];
    internal string ErrorMessage = "";
    internal AsnToken? ErrorToken;
}
