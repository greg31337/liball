namespace tiff;

internal class TiffRequiredTagValidationResult
{
    internal bool Success;
    internal bool HasMissingRequiredTags;
    internal int[] MissingTagIds;
    internal TiffDiagnostics Diagnostics;

    internal TiffRequiredTagValidationResult()
    {
        Success = true;
        HasMissingRequiredTags = false;
        MissingTagIds = new int[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffRequiredTagValidationResult Create(int[] missingTagIds, TiffDiagnostics diagnostics)
    {
        TiffRequiredTagValidationResult value = new TiffRequiredTagValidationResult();
        value.Initialize(missingTagIds, diagnostics);
        return value;
    }

    internal void Initialize(int[] missingTagIds, TiffDiagnostics diagnostics)
    {
        MissingTagIds = CopyTagIds(missingTagIds);
        Diagnostics = diagnostics;
        HasMissingRequiredTags = MissingTagIds.Length > 0;
        Success = !HasMissingRequiredTags && !Diagnostics.HasErrors();
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
