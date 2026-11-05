namespace tiff;

internal class TiffDuplicateTagResult
{
    internal bool Success;
    internal bool HasDuplicates;
    internal int[] DuplicateTagIds;
    internal TiffDiagnostics Diagnostics;

    internal TiffDuplicateTagResult()
    {
        Success = true;
        HasDuplicates = false;
        DuplicateTagIds = new int[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffDuplicateTagResult Create(int[] duplicateTagIds, TiffDiagnostics diagnostics)
    {
        TiffDuplicateTagResult value = new TiffDuplicateTagResult();
        value.Initialize(duplicateTagIds, diagnostics);
        return value;
    }

    internal void Initialize(int[] duplicateTagIds, TiffDiagnostics diagnostics)
    {
        DuplicateTagIds = CopyTagIds(duplicateTagIds);
        Diagnostics = diagnostics;
        HasDuplicates = DuplicateTagIds.Length > 0;
        Success = !HasDuplicates && !Diagnostics.HasErrors();
    }

    internal static int[] CopyTagIds(int[] tagIds)
    {
        if (tagIds == null)
        {
            return new int[0];
        }

        int[] copy = new int[tagIds.Length];
        int index = 0;
        while (index < tagIds.Length)
        {
            copy[index] = tagIds[index];
            index = index + 1;
        }

        return copy;
    }
}
