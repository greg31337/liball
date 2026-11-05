using System;

namespace raw;

internal enum TiffPatchStorageKind
{
    Inline = 0,
    OutOfLine = 1
}

internal class TiffPatchEdit
{
    internal string IfdPath;
    internal ushort Tag;
    internal TiffFieldType FieldType;
    internal ulong Count;
    internal byte[] ValueBytes;

    internal TiffPatchEdit(string ifdPath, ushort tag, TiffFieldType fieldType, ulong count, byte[] valueBytes)
    {
        IfdPath = ifdPath;
        Tag = tag;
        FieldType = fieldType;
        Count = count;
        ValueBytes = valueBytes;
    }
}

internal class TiffPatchRegion
{
    internal long Offset;
    internal byte[] Bytes;
    internal string IfdPath;
    internal ushort Tag;
    internal TiffPatchStorageKind StorageKind;

    internal TiffPatchRegion(long offset, byte[] bytes, string ifdPath, ushort tag, TiffPatchStorageKind storageKind)
    {
        Offset = offset;
        Bytes = bytes;
        IfdPath = ifdPath;
        Tag = tag;
        StorageKind = storageKind;
    }
}

internal class TiffPatchPlan
{
    internal long ContainerStart;
    internal long OriginalLength;
    internal TiffPatchRegion[] Regions;

    internal TiffPatchPlan(long containerStart, long originalLength, TiffPatchRegion[] regions)
    {
        ContainerStart = containerStart;
        OriginalLength = originalLength;
        Regions = regions;
    }
}

internal static class TiffPatchAnalyzer
{
    internal static TiffPatchPlan Plan(TiffIfdGraph? graph, TiffPatchEdit[]? edits, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateInputs(graph, edits, limits);
        cancellationToken.ThrowIfCancellationRequested();
        RawValidationReport validation = TiffValidator.Validate(graph, false);
        if (!validation.IsValid())
        {
            throw Reject("A fixed-size TIFF patch requires a structurally valid parsed container.", (graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).ContainerStart);
        }

        System.Collections.Generic.List<TiffPatchRegion> regions = new System.Collections.Generic.List<TiffPatchRegion>();
        long replacementBytes = 0;
        int index = 0;
        while (index < (edits ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TiffPatchEdit edit = edits[index];
            ValidateEdit(edit, limits);
            RequireUniqueEdit(edits, index, edit);
            TiffIfd? ifd = (graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindIfd(edit.IfdPath);
            if (ifd == null)
            {
                throw Reject("A fixed-size TIFF patch cannot add a directory or target an unknown directory.", -1);
            }
            TiffEntry? entry = ifd.FindEntry(edit.Tag);
            if (entry == null)
            {
                throw Reject("A fixed-size TIFF patch cannot add a TIFF tag.", ifd.AbsoluteOffset);
            }
            if (edit.FieldType != entry.FieldType || edit.Count != entry.Count)
            {
                throw Reject("A fixed-size TIFF patch cannot change a tag type or count.", entry.EntryOffset);
            }
            if (edit.ValueBytes.Length != entry.RawBytes.Length)
            {
                throw Reject("A fixed-size TIFF patch cannot grow, shrink, or move tag storage.", entry.EntryOffset);
            }
            replacementBytes = CheckedMath.Add(replacementBytes, edit.ValueBytes.Length);
            if (replacementBytes > limits.MaxMetadataBytes)
            {
                throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Fixed-size TIFF patch bytes exceed the configured metadata limit.", entry.EntryOffset);
            }
            if (!BytesEqual(edit.ValueBytes, entry.RawBytes))
            {
                if (ReferencesStorage(graph, ifd, entry) || DefinesImageStorage(entry.Tag))
                {
                    throw Reject("A fixed-size TIFF patch cannot change offsets or fields that define referenced storage.", entry.EntryOffset);
                }
                TiffPatchRegion region = CreateRegion(graph, ifd, entry, edit.ValueBytes);
                RequireNoOverlap(regions, region);
                regions.Add(region);
            }
            index++;
        }

        TiffPatchRegion[] result = regions.ToArray();
        System.Array.Sort(result, (first, second) => first.Offset.CompareTo(second.Offset));
        return new TiffPatchPlan((graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).ContainerStart, graph.ContainerLength, result);
    }

    internal static void ValidateInputs(TiffIfdGraph? graph, TiffPatchEdit[]? edits, RawLimits limits)
    {
        if (graph == null || graph.Header == null || graph.Ifds == null || graph.Links == null || graph.Images == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A complete parsed TIFF graph is required for patch analysis.", -1);
        }
        if (edits == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "TIFF patch edits are required.", -1);
        }
        CheckedMath.RequireLimits(limits);
        CheckedMath.RequirePositiveLimit(limits.MaxTagCount, "The configured tag-count limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxTagValueBytes, "The configured tag-value limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxMetadataBytes, "The configured metadata limit must be greater than zero.");
        CheckedMath.FileSize(graph.ContainerLength, limits);
        if (graph.ContainerStart < 0 || graph.ContainerLength <= 0 || graph.Header.BaseOffset != graph.ContainerStart || edits.Length > limits.MaxTagCount)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidDescriptor, "The TIFF graph or patch edit count is not eligible for a fixed-size patch.", graph.ContainerStart);
        }
    }

    internal static void ValidateEdit(TiffPatchEdit edit, RawLimits limits)
    {
        if (edit == null || edit.IfdPath == null || edit.IfdPath.Length == 0 || edit.ValueBytes == null || !DefinedFieldType(edit.FieldType))
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A TIFF patch edit is incomplete.", -1);
        }
        long count = CheckedMath.ToInt64(edit.Count);
        long expected = CheckedMath.Multiply(count, TiffFieldTypes.GetElementSize(edit.FieldType));
        if (expected != edit.ValueBytes.Length)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "A TIFF patch value does not match its field type and count.", -1);
        }
        if (expected > limits.MaxTagValueBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "A TIFF patch value exceeds the configured tag-value limit.", -1);
        }
        CheckedMath.AllocationSize(expected, 1, limits);
    }

    internal static void RequireUniqueEdit(TiffPatchEdit[] edits, int index, TiffPatchEdit edit)
    {
        int previous = 0;
        while (previous < index)
        {
            TiffPatchEdit other = edits[previous];
            if (other != null && other.IfdPath == edit.IfdPath && other.Tag == edit.Tag)
            {
                throw Reject("A TIFF tag cannot have more than one fixed-size patch edit.", -1);
            }
            previous++;
        }
    }

    internal static TiffPatchRegion CreateRegion(TiffIfdGraph graph, TiffIfd ifd, TiffEntry entry, byte[] valueBytes)
    {
        long offset;
        TiffPatchStorageKind storageKind;
        if (entry.Inline)
        {
            int slotOffset = graph.Header.BigTiff ? 12 : 8;
            offset = CheckedMath.Add(entry.EntryOffset, slotOffset);
            storageKind = TiffPatchStorageKind.Inline;
        }
        else
        {
            offset = CheckedMath.Add(graph.Header.BaseOffset, CheckedMath.ToInt64(entry.ValueOffset));
            storageKind = TiffPatchStorageKind.OutOfLine;
        }
        long containerEnd = CheckedMath.Add(graph.ContainerStart, graph.ContainerLength);
        if (offset < graph.ContainerStart || offset > containerEnd || valueBytes.Length > containerEnd - offset)
        {
            throw Reject("A fixed-size TIFF patch value lies outside the bounded container.", offset);
        }
        byte[] bytes = new byte[valueBytes.Length];
        valueBytes.AsSpan().CopyTo(bytes);
        return new TiffPatchRegion(offset, bytes, ifd.Path, entry.Tag, storageKind);
    }

    internal static void RequireNoOverlap(System.Collections.Generic.List<TiffPatchRegion> regions, TiffPatchRegion candidate)
    {
        long candidateEnd = CheckedMath.Add(candidate.Offset, candidate.Bytes.Length);
        int index = 0;
        while (index < regions.Count)
        {
            TiffPatchRegion existing = regions[index];
            long existingEnd = CheckedMath.Add(existing.Offset, existing.Bytes.Length);
            if (candidate.Offset < existingEnd && existing.Offset < candidateEnd)
            {
                throw Reject("Fixed-size TIFF patch regions cannot overlap.", candidate.Offset);
            }
            index++;
        }
    }

    internal static bool ReferencesStorage(TiffIfdGraph graph, TiffIfd ifd, TiffEntry entry)
    {
        if (entry.FieldType == TiffFieldType.Ifd || entry.FieldType == TiffFieldType.Ifd8)
        {
            return true;
        }
        if (entry.Tag == 37500)
        {
            return true;
        }
        if (entry.Tag == 273 || entry.Tag == 288 || entry.Tag == 324 || entry.Tag == 330 || entry.Tag == 513 || entry.Tag == 519 || entry.Tag == 520 || entry.Tag == 521 || entry.Tag == 34665 || entry.Tag == 34853 || entry.Tag == 40965)
        {
            return true;
        }
        int index = 0;
        while (index < graph.Links.Length)
        {
            TiffIfdLink link = graph.Links[index];
            if (link != null && link.SourcePath == ifd.Path && link.Tag == entry.Tag)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool DefinesImageStorage(ushort tag)
    {
        return tag == 256 || tag == 257 || tag == 258 || tag == 259 || tag == 266 || tag == 273 || tag == 277 || tag == 278 || tag == 279 || tag == 284 || tag == 288 || tag == 289 || tag == 317 || tag == 322 || tag == 323 || tag == 324 || tag == 325 || tag == 338 || tag == 339 || tag == 513 || tag == 514 || tag == 519 || tag == 520 || tag == 521;
    }

    internal static bool DefinedFieldType(TiffFieldType fieldType)
    {
        int code = (int)fieldType;
        return code >= 1 && code <= 13 || code >= 16 && code <= 18 || code == 129;
    }

    internal static bool BytesEqual(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static RawWriteException Reject(string message, long offset)
    {
        return new RawWriteException(RawDiagnosticCode.WritePlanningFailed, message, offset);
    }
}
