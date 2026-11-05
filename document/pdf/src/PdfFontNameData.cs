namespace pdf;

internal class PdfFontNameData
{
    internal string FamilyName;
    internal string SubfamilyName;
    internal string FullName;
    internal string PostScriptName;
    internal int FamilyScore;
    internal int SubfamilyScore;
    internal int FullScore;
    internal int PostScriptScore;

    internal PdfFontNameData()
    {
        FamilyName = string.Empty;
        SubfamilyName = string.Empty;
        FullName = string.Empty;
        PostScriptName = string.Empty;
        FamilyScore = -1;
        SubfamilyScore = -1;
        FullScore = -1;
        PostScriptScore = -1;
    }
}
