namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set5HierarchyData
{
    internal Iso639Set5HierarchyData(
        Iso639Set5HierarchyEntry[] entries,
        Iso639Set5HierarchyRelationship[] relationships)
    {
        Entries = entries;
        Relationships = relationships;
    }

    internal Iso639Set5HierarchyEntry[] Entries { get; }

    internal Iso639Set5HierarchyRelationship[] Relationships { get; }
}
