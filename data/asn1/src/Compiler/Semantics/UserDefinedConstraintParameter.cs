namespace asn1;

internal class UserDefinedConstraintParameter
{
    internal int Ordinal;
    internal AsnToken[] SourceTokens = [];
    internal AsnToken[] GovernorTokens = [];
    internal AsnToken[] ActualTokens = [];
    internal string Text = "";
    internal string GovernorText = "";
    internal string ActualText = "";
    internal string FormalParameterName = "";
    internal bool HasGovernor;
    internal bool ReferencesFormalParameter;
}
