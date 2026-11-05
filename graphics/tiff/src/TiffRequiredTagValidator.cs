namespace tiff;

internal class TiffRequiredTagValidator
{
    internal static TiffRequiredTagValidationResult ValidateEntries(TiffIfdEntry[] entries)
    {
        return ValidateEntriesForImage(entries, TiffDiagnostic.NoImageIndex);
    }

    internal static TiffRequiredTagValidationResult ValidateEntriesForImage(TiffIfdEntry[] entries, int imageIndex)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        int[] missing = new int[0];
        missing = AddMissingEntryCommonTags(entries, missing, diagnostics, imageIndex);

        if (HasCompleteEntryStripStorage(entries) || HasCompleteEntryTileStorage(entries))
        {
            return TiffRequiredTagValidationResult.Create(missing, diagnostics);
        }

        if (HasAnyEntryTileStorageTag(entries) && !HasAnyEntryStripStorageTag(entries))
        {
            missing = AddMissingEntryTags(entries, GetTileStorageRequiredTagIds(), missing, diagnostics, imageIndex);
        }
        else
        {
            missing = AddMissingEntryTags(entries, GetStripStorageRequiredTagIds(), missing, diagnostics, imageIndex);
        }

        return TiffRequiredTagValidationResult.Create(missing, diagnostics);
    }

    internal static TiffRequiredTagValidationResult ValidateValues(TiffTagValue[] values)
    {
        return ValidateValuesForImage(values, TiffDiagnostic.NoImageIndex);
    }

    internal static TiffRequiredTagValidationResult ValidateValuesForImage(TiffTagValue[] values, int imageIndex)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        int[] missing = new int[0];
        missing = AddMissingValueCommonTags(values, missing, diagnostics, imageIndex);

        if (HasCompleteValueStripStorage(values) || HasCompleteValueTileStorage(values))
        {
            return TiffRequiredTagValidationResult.Create(missing, diagnostics);
        }

        if (HasAnyValueTileStorageTag(values) && !HasAnyValueStripStorageTag(values))
        {
            missing = AddMissingValueTags(values, GetTileStorageRequiredTagIds(), missing, diagnostics, imageIndex);
        }
        else
        {
            missing = AddMissingValueTags(values, GetStripStorageRequiredTagIds(), missing, diagnostics, imageIndex);
        }

        return TiffRequiredTagValidationResult.Create(missing, diagnostics);
    }

    internal static int[] GetCommonRequiredTagIds()
    {
        int[] tagIds = new int[4];
        tagIds[0] = TiffTagId.ImageWidth;
        tagIds[1] = TiffTagId.ImageLength;
        tagIds[2] = TiffTagId.BitsPerSample;
        tagIds[3] = TiffTagId.PhotometricInterpretation;
        return tagIds;
    }

    internal static int[] GetStripStorageRequiredTagIds()
    {
        int[] tagIds = new int[3];
        tagIds[0] = TiffTagId.StripOffsets;
        tagIds[1] = TiffTagId.RowsPerStrip;
        tagIds[2] = TiffTagId.StripByteCounts;
        return tagIds;
    }

    internal static int[] GetTileStorageRequiredTagIds()
    {
        int[] tagIds = new int[4];
        tagIds[0] = TiffTagId.TileWidth;
        tagIds[1] = TiffTagId.TileLength;
        tagIds[2] = TiffTagId.TileOffsets;
        tagIds[3] = TiffTagId.TileByteCounts;
        return tagIds;
    }

    internal static bool HasEntryTag(TiffIfdEntry[] entries, int tagId)
    {
        if (entries == null)
        {
            return false;
        }

        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index] != null && entries[index].TagId == tagId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasValueTag(TiffTagValue[] values, int tagId)
    {
        if (values == null)
        {
            return false;
        }

        int index = 0;
        while (index < values.Length)
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

    internal static bool HasCompleteEntryStripStorage(TiffIfdEntry[] entries)
    {
        return HasAllEntryTags(entries, GetStripStorageRequiredTagIds());
    }

    internal static bool HasCompleteEntryTileStorage(TiffIfdEntry[] entries)
    {
        return HasAllEntryTags(entries, GetTileStorageRequiredTagIds());
    }

    internal static bool HasCompleteValueStripStorage(TiffTagValue[] values)
    {
        return HasAllValueTags(values, GetStripStorageRequiredTagIds());
    }

    internal static bool HasCompleteValueTileStorage(TiffTagValue[] values)
    {
        return HasAllValueTags(values, GetTileStorageRequiredTagIds());
    }

    internal static bool HasAnyEntryStripStorageTag(TiffIfdEntry[] entries)
    {
        return HasAnyEntryTag(entries, GetStripStorageRequiredTagIds());
    }

    internal static bool HasAnyEntryTileStorageTag(TiffIfdEntry[] entries)
    {
        return HasAnyEntryTag(entries, GetTileStorageRequiredTagIds());
    }

    internal static bool HasAnyValueStripStorageTag(TiffTagValue[] values)
    {
        return HasAnyValueTag(values, GetStripStorageRequiredTagIds());
    }

    internal static bool HasAnyValueTileStorageTag(TiffTagValue[] values)
    {
        return HasAnyValueTag(values, GetTileStorageRequiredTagIds());
    }

    internal static bool HasAllEntryTags(TiffIfdEntry[] entries, int[] tagIds)
    {
        int index = 0;
        while (index < tagIds.Length)
        {
            if (!HasEntryTag(entries, tagIds[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasAllValueTags(TiffTagValue[] values, int[] tagIds)
    {
        int index = 0;
        while (index < tagIds.Length)
        {
            if (!HasValueTag(values, tagIds[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasAnyEntryTag(TiffIfdEntry[] entries, int[] tagIds)
    {
        int index = 0;
        while (index < tagIds.Length)
        {
            if (HasEntryTag(entries, tagIds[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasAnyValueTag(TiffTagValue[] values, int[] tagIds)
    {
        int index = 0;
        while (index < tagIds.Length)
        {
            if (HasValueTag(values, tagIds[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int[] AddMissingEntryCommonTags(TiffIfdEntry[] entries, int[] missing, TiffDiagnostics diagnostics, int imageIndex)
    {
        return AddMissingEntryTags(entries, GetCommonRequiredTagIds(), missing, diagnostics, imageIndex);
    }

    internal static int[] AddMissingValueCommonTags(TiffTagValue[] values, int[] missing, TiffDiagnostics diagnostics, int imageIndex)
    {
        return AddMissingValueTags(values, GetCommonRequiredTagIds(), missing, diagnostics, imageIndex);
    }

    internal static int[] AddMissingEntryTags(TiffIfdEntry[] entries, int[] required, int[] missing, TiffDiagnostics diagnostics, int imageIndex)
    {
        int index = 0;
        while (index < required.Length)
        {
            if (!HasEntryTag(entries, required[index]) && !ContainsTagId(missing, required[index]))
            {
                missing = AppendTagId(missing, required[index]);
                AddMissingTagDiagnostic(diagnostics, imageIndex, required[index]);
            }

            index = index + 1;
        }

        return missing;
    }

    internal static int[] AddMissingValueTags(TiffTagValue[] values, int[] required, int[] missing, TiffDiagnostics diagnostics, int imageIndex)
    {
        int index = 0;
        while (index < required.Length)
        {
            if (!HasValueTag(values, required[index]) && !ContainsTagId(missing, required[index]))
            {
                missing = AppendTagId(missing, required[index]);
                AddMissingTagDiagnostic(diagnostics, imageIndex, required[index]);
            }

            index = index + 1;
        }

        return missing;
    }

    internal static void AddMissingTagDiagnostic(TiffDiagnostics diagnostics, int imageIndex, int tagId)
    {
        if (diagnostics == null)
        {
            return;
        }

        diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Image directory is missing a required tag.", imageIndex, tagId);
    }
}
