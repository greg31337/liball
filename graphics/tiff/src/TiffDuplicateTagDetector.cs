namespace tiff;

internal class TiffDuplicateTagDetector
{
    internal static TiffDuplicateTagResult DetectEntries(TiffIfdEntry[] entries)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        int[] duplicates = new int[0];
        if (entries == null)
        {
            return TiffDuplicateTagResult.Create(duplicates, diagnostics);
        }

        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index] != null && AppearsEarlier(entries, index, entries[index].TagId) && !ContainsTagId(duplicates, entries[index].TagId))
            {
                duplicates = AppendTagId(duplicates, entries[index].TagId);
                diagnostics.AddErrorForTag(TiffErrorCode.DuplicateTag, "IFD contains a duplicate tag.", TiffDiagnostic.NoImageIndex, entries[index].TagId);
            }

            index = index + 1;
        }

        return TiffDuplicateTagResult.Create(duplicates, diagnostics);
    }

    internal static TiffDuplicateTagResult DetectValues(TiffTagValue[] values)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        int[] duplicates = new int[0];
        if (values == null)
        {
            return TiffDuplicateTagResult.Create(duplicates, diagnostics);
        }

        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null && ValueAppearsEarlier(values, index, values[index].TagId) && !ContainsTagId(duplicates, values[index].TagId))
            {
                duplicates = AppendTagId(duplicates, values[index].TagId);
                diagnostics.AddErrorForTag(TiffErrorCode.DuplicateTag, "Tag value list contains a duplicate tag.", TiffDiagnostic.NoImageIndex, values[index].TagId);
            }

            index = index + 1;
        }

        return TiffDuplicateTagResult.Create(duplicates, diagnostics);
    }

    internal static bool AppearsEarlier(TiffIfdEntry[] entries, int currentIndex, int tagId)
    {
        int index = 0;
        while (index < currentIndex)
        {
            if (entries[index] != null && entries[index].TagId == tagId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ValueAppearsEarlier(TiffTagValue[] values, int currentIndex, int tagId)
    {
        int index = 0;
        while (index < currentIndex)
        {
            if (values[index] != null && values[index].TagId == tagId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsTagId(int[] tagIds, int tagId)
    {
        if (tagIds == null)
        {
            return false;
        }

        int index = 0;
        while (index < tagIds.Length)
        {
            if (tagIds[index] == tagId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int[] AppendTagId(int[] tagIds, int tagId)
    {
        if (tagIds == null)
        {
            int[] single = new int[1];
            single[0] = tagId;
            return single;
        }

        int[] copy = new int[tagIds.Length + 1];
        int index = 0;
        while (index < tagIds.Length)
        {
            copy[index] = tagIds[index];
            index = index + 1;
        }

        copy[copy.Length - 1] = tagId;
        return copy;
    }
}
