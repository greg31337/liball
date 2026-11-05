using System;

namespace raw;

internal static class TiffLayoutPlanner
{
    internal static TiffLayoutPlan Plan(TiffLayoutRequest? request, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateRequest(request, limits);
        cancellationToken.ThrowIfCancellationRequested();
        int alignment = (request ?? throw new System.InvalidOperationException("A required value is unavailable.")).BigTiff ? 8 : 2;
        int baseHeaderLength = request.BigTiff ? 16 : 8;
        int headerLength = CheckedMath.ToInt32(CheckedMath.Add(baseHeaderLength, request.HeaderExtraBytes.Length));
        int entrySize = request.BigTiff ? 20 : 12;
        int countSize = request.BigTiff ? 8 : 2;
        int nextSize = request.BigTiff ? 8 : 4;
        int slotSize = request.BigTiff ? 8 : 4;

        TiffLayoutPlan plan = new TiffLayoutPlan();
        plan.BigTiff = request.BigTiff;
        plan.ByteOrder = request.ByteOrder;
        plan.Alignment = alignment;
        plan.HeaderLength = headerLength;
        plan.HeaderExtraBytes = new byte[request.HeaderExtraBytes.Length];
        request.HeaderExtraBytes.AsSpan().CopyTo(plan.HeaderExtraBytes);
        plan.Ifds = new TiffLayoutIfdPlan[request.Ifds.Length];
        plan.Blocks = new TiffLayoutBlockPlan[request.Blocks.Length];

        long position = headerLength;
        int ifdIndex = 0;
        long metadataBytes = headerLength;
        while (ifdIndex < request.Ifds.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TiffLayoutIfdInput input = request.Ifds[ifdIndex];
            long alignedDirectory = Align(position, alignment);
            metadataBytes = CheckedMath.Add(metadataBytes, alignedDirectory - position);
            position = alignedDirectory;
            long directoryLength = CheckedMath.Add(countSize, CheckedMath.Multiply(input.Entries.Length, entrySize));
            directoryLength = CheckedMath.Add(directoryLength, nextSize);
            TiffLayoutIfdPlan directory = new TiffLayoutIfdPlan();
            directory.Path = input.Path;
            directory.Offset = ToOffset(position, request.BigTiff);
            directory.Length = directoryLength;
            directory.NextIfdPath = input.NextIfdPath;
            directory.NextIfdOffset = 0;
            directory.Entries = CreateEntryPlans(input, position, countSize, entrySize, slotSize, request.BigTiff, limits);
            plan.Ifds[ifdIndex] = directory;
            position = CheckedMath.Add(position, directoryLength);
            metadataBytes = CheckedMath.Add(metadataBytes, directoryLength);
            ifdIndex++;
        }

        ifdIndex = 0;
        while (ifdIndex < plan.Ifds.Length)
        {
            TiffLayoutIfdPlan directory = plan.Ifds[ifdIndex];
            int entryIndex = 0;
            while (entryIndex < (directory.Entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
            {
                TiffLayoutEntryPlan entry = directory.Entries[entryIndex];
                if (!entry.Inline)
                {
                    long aligned = Align(position, alignment);
                    metadataBytes = CheckedMath.Add(metadataBytes, aligned - position);
                    position = aligned;
                    entry.ValueOffset = ToOffset(position, request.BigTiff);
                    position = CheckedMath.Add(position, entry.ValueLength);
                    metadataBytes = CheckedMath.Add(metadataBytes, entry.ValueLength);
                }
                entryIndex++;
            }
            ifdIndex++;
        }
        if (metadataBytes > limits.MaxMetadataBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The planned TIFF metadata exceeds the configured metadata limit.", -1);
        }

        int blockIndex = 0;
        while (blockIndex < request.Blocks.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TiffLayoutBlockInput input = request.Blocks[blockIndex];
            position = Align(position, alignment);
            TiffLayoutBlockPlan block = new TiffLayoutBlockPlan();
            block.Name = input.Name;
            block.Kind = input.Kind;
            block.Offset = ToOffset(position, request.BigTiff);
            block.Length = input.Length;
            block.Bytes = input.Bytes;
            plan.Blocks[blockIndex] = block;
            position = CheckedMath.Add(position, input.Length);
            blockIndex++;
        }

        CheckedMath.FileSize(position, limits);
        if (!request.BigTiff && position > uint.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A classic TIFF layout exceeds the 32-bit address space.", position);
        }
        plan.PredictedSize = position;
        TiffLayoutIfdPlan first = FindIfd(plan, request.FirstIfdPath);
        plan.FirstIfdOffset = first.Offset;
        ResolveDirectoryLinks(plan, request.BigTiff);
        ResolveEntryReferences(plan, request.BigTiff, request.ByteOrder);
        return plan;
    }

    internal static TiffLayoutEntryPlan[] CreateEntryPlans(TiffLayoutIfdInput input, long directoryOffset, int countSize, int entrySize, int slotSize, bool bigTiff, RawLimits limits)
    {
        TiffLayoutEntryInput[] sorted = new TiffLayoutEntryInput[input.Entries.Length];
        System.Array.Copy(input.Entries, sorted, input.Entries.Length);
        int validationIndex = 0;
        while (validationIndex < sorted.Length)
        {
            ValidateEntry(sorted[validationIndex], bigTiff, limits);
            validationIndex++;
        }
        System.Array.Sort(sorted, (first, second) => first.Tag.CompareTo(second.Tag));
        int duplicateIndex = 1;
        while (duplicateIndex < sorted.Length)
        {
            if (sorted[duplicateIndex - 1].Tag == sorted[duplicateIndex].Tag)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A planned TIFF IFD contains duplicate tag identifiers.", -1);
            }
            duplicateIndex++;
        }

        TiffLayoutEntryPlan[] plans = new TiffLayoutEntryPlan[sorted.Length];
        int index = 0;
        while (index < sorted.Length)
        {
            TiffLayoutEntryInput entry = sorted[index];
            TiffLayoutEntryPlan entryPlan = new TiffLayoutEntryPlan();
            entryPlan.Tag = entry.Tag;
            entryPlan.FieldType = entry.FieldType;
            entryPlan.Count = entry.Count;
            entryPlan.EntryOffset = CheckedMath.Add(directoryOffset, countSize + index * entrySize);
            entryPlan.ValueLength = entry.ValueBytes.Length;
            entryPlan.Inline = entry.ValueBytes.Length <= slotSize;
            entryPlan.ValueOffset = 0;
            entryPlan.ValueBytes = new byte[entry.ValueBytes.Length];
            entry.ValueBytes.AsSpan().CopyTo(entryPlan.ValueBytes);
            entryPlan.References = entry.References == null ? new TiffLayoutReference[0] : entry.References;
            plans[index] = entryPlan;
            index++;
        }
        return plans;
    }

    internal static void ValidateRequest(TiffLayoutRequest? request, RawLimits limits)
    {
        if (request == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A TIFF layout request is required.", -1);
        }
        CheckedMath.RequireLimits(limits);
        CheckedMath.RequirePositiveLimit(limits.MaxMetadataBytes, "The configured metadata limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxTagCount, "The configured tag-count limit must be greater than zero.");
        if (request.ByteOrder != RawByteOrder.LittleEndian && request.ByteOrder != RawByteOrder.BigEndian)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The TIFF layout byte order is not defined.", -1);
        }
        if (request.HeaderExtraBytes == null || request.Ifds == null || request.Ifds.Length == 0 || request.Blocks == null || request.FirstIfdPath == null || request.FirstIfdPath.Length == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A TIFF layout requires directories, blocks, and a first-directory path.", -1);
        }
        if (request.HeaderExtraBytes.LongLength > limits.MaxMetadataBytes || request.HeaderExtraBytes.LongLength > limits.MaxTagValueBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The TIFF header extension exceeds configured metadata limits.", -1);
        }
        long tagCount = 0;
        int index = 0;
        while (index < request.Ifds.Length)
        {
            TiffLayoutIfdInput directory = request.Ifds[index];
            if (directory == null || directory.Path == null || directory.Path.Length == 0 || directory.Entries == null)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A planned TIFF directory is incomplete.", -1);
            }
            if (!request.BigTiff && directory.Entries.Length > ushort.MaxValue)
            {
                throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A classic TIFF directory contains more than 65,535 entries.", -1);
            }
            int previous = 0;
            while (previous < index)
            {
                if (request.Ifds[previous].Path == directory.Path)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Planned TIFF directory paths must be unique.", -1);
                }
                previous++;
            }
            tagCount = CheckedMath.Add(tagCount, directory.Entries.Length);
            index++;
        }
        if (tagCount > limits.MaxTagCount)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The planned TIFF tag count exceeds the configured limit.", -1);
        }
        index = 0;
        while (index < request.Blocks.Length)
        {
            TiffLayoutBlockInput block = request.Blocks[index];
            if (block == null || block.Name == null || block.Name.Length == 0 || block.Length < 0 || block.Bytes != null && block.Bytes.Length != block.Length || block.Kind != TiffLayoutBlockKind.Strip && block.Kind != TiffLayoutBlockKind.Tile && block.Kind != TiffLayoutBlockKind.Opaque)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A planned TIFF data block is incomplete or has an inconsistent length.", -1);
            }
            int previous = 0;
            while (previous < index)
            {
                if (request.Blocks[previous].Name == block.Name)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Planned TIFF data block names must be unique.", -1);
                }
                previous++;
            }
            index++;
        }
    }

    internal static void ValidateEntry(TiffLayoutEntryInput entry, bool bigTiff, RawLimits limits)
    {
        if (entry == null || entry.ValueBytes == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A planned TIFF entry and its value bytes are required.", -1);
        }
        if (!bigTiff && (entry.FieldType == TiffFieldType.Long8 || entry.FieldType == TiffFieldType.SignedLong8 || entry.FieldType == TiffFieldType.Ifd8))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Classic TIFF cannot store 64-bit field types.", -1);
        }
        if (!bigTiff && entry.Count > uint.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A classic TIFF field count exceeds 32 bits.", -1);
        }
        long count = CheckedMath.ToInt64(entry.Count);
        long expectedLength = CheckedMath.Multiply(count, TiffFieldTypes.GetElementSize(entry.FieldType));
        if (entry.ValueBytes.Length != expectedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "A planned TIFF entry value does not match its type and count.", -1);
        }
        if (expectedLength > limits.MaxTagValueBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "A planned TIFF value exceeds the configured tag-value limit.", -1);
        }
        CheckedMath.AllocationSize(expectedLength, 1, limits);
    }

    internal static void ResolveDirectoryLinks(TiffLayoutPlan plan, bool bigTiff)
    {
        int index = 0;
        while (index < (plan.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            TiffLayoutIfdPlan directory = plan.Ifds[index];
            if (directory.NextIfdPath != null && directory.NextIfdPath.Length != 0)
            {
                directory.NextIfdOffset = FindIfd(plan, directory.NextIfdPath).Offset;
                RequireOffsetWidth(directory.NextIfdOffset, bigTiff);
            }
            index++;
        }
    }

    internal static void ResolveEntryReferences(TiffLayoutPlan plan, bool bigTiff, RawByteOrder byteOrder)
    {
        int ifdIndex = 0;
        while (ifdIndex < (plan.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            TiffLayoutEntryPlan[]? entries = plan.Ifds[ifdIndex].Entries;
            int entryIndex = 0;
            while (entryIndex < (entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
            {
                TiffLayoutEntryPlan entry = entries[entryIndex];
                int referenceIndex = 0;
                while (referenceIndex < (entry.References ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
                {
                    TiffLayoutReference reference = entry.References[referenceIndex];
                    if (reference == null || reference.Target == null || reference.Target.Length == 0 || reference.Index < 0 || (ulong)reference.Index >= entry.Count)
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A planned TIFF offset reference is invalid.", -1);
                    }
                    int earlierReference = 0;
                    while (earlierReference < referenceIndex)
                    {
                        if (entry.References[earlierReference] != null && entry.References[earlierReference].Index == reference.Index)
                        {
                            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A planned TIFF value index has more than one offset reference.", -1);
                        }
                        earlierReference++;
                    }
                    ulong offset;
                    if (reference.Kind == TiffLayoutReferenceKind.Ifd)
                    {
                        offset = FindIfd(plan, reference.Target).Offset;
                    }
                    else if (reference.Kind == TiffLayoutReferenceKind.Block)
                    {
                        offset = FindBlock(plan, reference.Target).Offset;
                    }
                    else
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A planned TIFF offset reference kind is invalid.", -1);
                    }
                    RequireOffsetWidth(offset, bigTiff);
                    WriteReference(entry, reference.Index, offset, byteOrder);
                    referenceIndex++;
                }
                entryIndex++;
            }
            ifdIndex++;
        }
    }

    internal static void WriteReference(TiffLayoutEntryPlan entry, int index, ulong value, RawByteOrder byteOrder)
    {
        int elementSize = TiffFieldTypes.GetElementSize(entry.FieldType);
        int offset = index * elementSize;
        if (entry.FieldType == TiffFieldType.Short)
        {
            if (value > ushort.MaxValue)
            {
                throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A TIFF SHORT reference cannot represent its planned offset.", -1);
            }
            EndianWriter.WriteUInt16(entry.ValueBytes.AsSpan(offset, 2), (ushort)value, byteOrder);
            return;
        }
        if (entry.FieldType == TiffFieldType.Long || entry.FieldType == TiffFieldType.Ifd)
        {
            if (value > uint.MaxValue)
            {
                throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A TIFF LONG reference cannot represent its planned offset.", -1);
            }
            EndianWriter.WriteUInt32(entry.ValueBytes.AsSpan(offset, 4), (uint)value, byteOrder);
            return;
        }
        if (entry.FieldType == TiffFieldType.Long8 || entry.FieldType == TiffFieldType.Ifd8)
        {
            EndianWriter.WriteUInt64(entry.ValueBytes.AsSpan(offset, 8), value, byteOrder);
            return;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A TIFF offset reference requires an unsigned integer or IFD field type.", -1);
    }

    internal static TiffLayoutIfdPlan FindIfd(TiffLayoutPlan? plan, string? path)
    {
        int index = 0;
        while (index < ((plan ?? throw new System.InvalidOperationException("A required value is unavailable.")).Ifds ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            if ((plan.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable."))[index].Path == path)
            {
                return plan.Ifds[index];
            }
            index++;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A planned TIFF reference targets an unknown directory.", -1);
    }

    internal static TiffLayoutBlockPlan FindBlock(TiffLayoutPlan? plan, string name)
    {
        int index = 0;
        while (index < ((plan ?? throw new System.InvalidOperationException("A required value is unavailable.")).Blocks ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            if ((plan.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable."))[index].Name == name)
            {
                return plan.Blocks[index];
            }
            index++;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A planned TIFF reference targets an unknown data block.", -1);
    }

    internal static ulong ToOffset(long value, bool bigTiff)
    {
        if (!bigTiff && value > uint.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A classic TIFF offset exceeds 32 bits.", value);
        }
        return (ulong)value;
    }

    internal static void RequireOffsetWidth(ulong value, bool bigTiff)
    {
        if (!bigTiff && value > uint.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A classic TIFF offset exceeds 32 bits.", -1);
        }
    }

    internal static long Align(long value, int alignment)
    {
        long remainder = value % alignment;
        if (remainder == 0)
        {
            return value;
        }
        return CheckedMath.Add(value, alignment - remainder);
    }
}
