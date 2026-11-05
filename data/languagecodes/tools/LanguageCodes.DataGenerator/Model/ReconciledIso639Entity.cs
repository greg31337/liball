namespace LanguageCodes.DataGenerator.Model;

internal sealed class ReconciledIso639Entity
{
    internal ReconciledIso639Entity(
        string key,
        string? alpha2Code,
        string? set2BibliographicCode,
        string? set2TerminologyCode,
        string? set3Code,
        string? set5Code,
        Iso639Set2Record? set2Record,
        Iso639Set3Record? set3Record,
        Iso639Set5Record? set5Record)
    {
        Key = key;
        Alpha2Code = alpha2Code;
        Set2BibliographicCode = set2BibliographicCode;
        Set2TerminologyCode = set2TerminologyCode;
        Set3Code = set3Code;
        Set5Code = set5Code;
        Set2Record = set2Record;
        Set3Record = set3Record;
        Set5Record = set5Record;
    }

    internal string Key { get; }

    internal string? Alpha2Code { get; }

    internal string? Set2BibliographicCode { get; }

    internal string? Set2TerminologyCode { get; }

    internal string? Set3Code { get; }

    internal string? Set5Code { get; }

    internal Iso639Set2Record? Set2Record { get; }

    internal Iso639Set3Record? Set3Record { get; }

    internal Iso639Set5Record? Set5Record { get; }

    internal bool InSet1 => Alpha2Code != null;

    internal bool InSet2 => Set2Record != null;

    internal bool InSet3 => Set3Record != null;

    internal bool InSet5 => Set5Record != null;
}
