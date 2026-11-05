namespace tiff;

internal class TiffMetadataSourceSnapshot
{
    internal TiffDirectory Directory;
    internal byte[] Bytes;
    internal TiffDiagnostics Diagnostics;

    internal TiffMetadataSourceSnapshot()
    {
        Directory = new TiffDirectory();
        Bytes = new byte[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffMetadataSourceSnapshot Create(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, int[] tagIds)
    {
        TiffMetadataSourceSnapshot snapshot = new TiffMetadataSourceSnapshot();
        if (directory == null || source == null)
        {
            snapshot.Diagnostics.AddError(TiffErrorCode.UnknownError, "A directory and source are required to read metadata.");
            return snapshot;
        }

        snapshot.Directory = directory.Copy();
        snapshot.Bytes = CreateHeaderBytes(byteOrder);
        int index = 0;
        while (index < snapshot.Directory.Entries.Length)
        {
            TiffIfdEntry entry = snapshot.Directory.Entries[index];
            if (ContainsTagId(tagIds, entry.TagId) && entry.HasExternalValue())
            {
                TiffTagValueLoadResult load = TiffTagValueLoader.LoadSourceBytes(entry, source, limits);
                snapshot.Diagnostics.Merge(load.Diagnostics);
                if (load.Success)
                {
                    entry.ValueOffset = 0;
                    entry.ValueOffsetBytes = TiffIfdEntry.CopyBytes(load.Bytes);
                    entry.ValueSlotByteCount = load.Bytes.Length;
                }
            }

            index = index + 1;
        }

        return snapshot;
    }

    internal static bool ContainsTagId(int[] tagIds, int tagId)
    {
        int index = 0;
        while (tagIds != null && index < tagIds.Length)
        {
            if (tagIds[index] == tagId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static byte[] CreateHeaderBytes(TiffByteOrder byteOrder)
    {
        byte[] bytes = new byte[2];
        if (byteOrder == TiffByteOrder.BigEndian)
        {
            bytes[0] = 0x4d;
            bytes[1] = 0x4d;
        }
        else if (byteOrder == TiffByteOrder.LittleEndian)
        {
            bytes[0] = 0x49;
            bytes[1] = 0x49;
        }

        return bytes;
    }
}
