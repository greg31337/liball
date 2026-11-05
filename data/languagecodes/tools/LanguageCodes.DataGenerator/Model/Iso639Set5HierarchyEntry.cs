namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set5HierarchyEntry
{
    internal Iso639Set5HierarchyEntry(
        string code,
        Iso639Set5GroupKind groupKind,
        string[] hierarchyPath,
        string[] noteIds)
    {
        Code = code;
        GroupKind = groupKind;
        HierarchyPath = hierarchyPath;
        NoteIds = noteIds;
    }

    internal string Code { get; }

    internal Iso639Set5GroupKind GroupKind { get; }

    internal string[] HierarchyPath { get; }

    internal string[] NoteIds { get; }
}
