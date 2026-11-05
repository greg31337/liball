namespace languagecodes;

internal class LanguageRow
{
    internal string Key;
    internal string? Alpha2Code;
    internal string? Set2TerminologyCode;
    internal string? Set2BibliographicCode;
    internal string? Set3Code;
    internal string? Set5Code;
    internal LanguageScope Scope;
    internal LanguageType LanguageType;
    internal LanguageCodeStatus Status;
    internal ReconciledGroupKind GroupKind;
    internal string? Comment;

    internal LanguageRow(
        string key,
        string? alpha2Code,
        string? set2TerminologyCode,
        string? set2BibliographicCode,
        string? set3Code,
        string? set5Code,
        LanguageScope scope,
        LanguageType languageType,
        LanguageCodeStatus status,
        ReconciledGroupKind groupKind,
        string? comment)
    {
        Key = key;
        Alpha2Code = alpha2Code;
        Set2TerminologyCode = set2TerminologyCode;
        Set2BibliographicCode = set2BibliographicCode;
        Set3Code = set3Code;
        Set5Code = set5Code;
        Scope = scope;
        LanguageType = languageType;
        Status = status;
        GroupKind = groupKind;
        Comment = comment;
    }
}
