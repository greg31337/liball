namespace icc;

internal class HierarchicalPosition
{
    internal uint MemberSignature;
    internal int Offset;
    internal int Size;
    internal int OriginalIndex;

    internal HierarchicalPosition(uint memberSignature, int offset, int size, int originalIndex)
    {
        MemberSignature = memberSignature;
        Offset = offset;
        Size = size;
        OriginalIndex = originalIndex;
    }
}

internal class NestedDecodeOutcome
{
    internal IccTagData Data;
    internal IccOperationStatus Status;
    internal string Message;

    internal NestedDecodeOutcome(IccTagData data, IccOperationStatus status, string message)
    {
        Data = data;
        Status = status;
        Message = message;
    }
}

internal class HierarchicalWriteContext
{
    internal int MaximumNestingDepth;
    internal IccWriteMode Mode;
    internal bool PreserveUnknownData;
    internal System.Collections.Generic.Dictionary<IccTagData, bool> Active;
    internal System.Collections.Generic.Dictionary<IccTagData, byte[]> Encoded;

    internal HierarchicalWriteContext(int maximumNestingDepth)
    {
        MaximumNestingDepth = maximumNestingDepth;
        Mode = IccWriteMode.Preservation;
        PreserveUnknownData = true;
        Active = new System.Collections.Generic.Dictionary<IccTagData, bool>();
        Encoded = new System.Collections.Generic.Dictionary<IccTagData, byte[]>();
    }
}

internal static class HierarchicalTagCodec
{
    internal static void DecodeTagArray(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (!ValidateContainerHeader(state, payload, typeSignature, "tagArrayType"))
        {
            return;
        }

        uint arraySignature = ReadUInt32At(payload, 8);
        uint encodedCount = ReadUInt32At(payload, 12);
        if (encodedCount > (uint)((payload.Length - 16) / 8))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The tagArrayType position count exceeds the number of records that fit in the payload.");
            return;
        }

        int count = (int)encodedCount;
        int tableEnd = 16 + (count * 8);
        HierarchicalPosition[] positions = new HierarchicalPosition[count];
        int index = 0;
        while (index < count)
        {
            int recordOffset = 16 + (index * 8);
            uint encodedOffset = ReadUInt32At(payload, recordOffset);
            uint encodedSize = ReadUInt32At(payload, recordOffset + 4);
            HierarchicalPosition? position = CreatePosition(payload.Length, tableEnd, 0, encodedOffset, encodedSize, index);
            if (position == null)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Tag-array element {index} has an invalid, unaligned, or out-of-bounds position.");
                return;
            }

            positions[index] = position;
            index = index + 1;
        }

        string topologyError = ValidateTopology(payload, positions, tableEnd);
        if (topologyError.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, topologyError);
            return;
        }

        System.Collections.Generic.Dictionary<ulong, NestedDecodeOutcome> cache = new System.Collections.Generic.Dictionary<ulong, NestedDecodeOutcome>();
        IccTagData[] values = new IccTagData[count];
        uint elementType = 0;
        uint structureType = 0;
        bool hasStructureType = false;
        int unknownCount = 0;
        index = 0;
        while (index < count)
        {
            HierarchicalPosition position = positions[index];
            uint nestedType = ReadUInt32At(payload, position.Offset);
            if (index == 0)
            {
                elementType = nestedType;
            }
            else if (nestedType != elementType)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "All tagArrayType elements shall have the same tag type signature.");
                return;
            }

            NestedDecodeOutcome outcome = DecodeNestedWithCache(state, position, cache, 0);
            if (outcome.Status != IccOperationStatus.Success && outcome.Status != IccOperationStatus.UnsupportedFeature)
            {
                CompleteNestedFailure(state, payload, typeSignature, outcome, $"Tag-array element {index}");
                return;
            }

            if (outcome.Status == IccOperationStatus.UnsupportedFeature)
            {
                unknownCount = unknownCount + 1;
            }

            values[index] = outcome.Data;
            if (nestedType == IccTypeSignatures.TagStructure)
            {
                if (outcome.Data.TagStructure == null)
                {
                    TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Tag-array element {index} did not produce a tag structure.");
                    return;
                }

                uint nestedStructureType = outcome.Data.TagStructure.StructureSignature;
                if (!hasStructureType)
                {
                    structureType = nestedStructureType;
                    hasStructureType = true;
                }
                else if (nestedStructureType != structureType && arraySignature != IccArraySignatures.NamedColor)
                {
                    TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "All tag structures in a tagArrayType shall have the same structure type identifier.");
                    return;
                }
            }

            index = index + 1;
        }

        IccTagArray array = new IccTagArray();
        array.ArraySignature = arraySignature;
        array.Values = values;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.TagArray);
        data.TagArray = array;
        string projectionError = RegisteredArrayProjectionCodec.ProjectDecoded(state, data);
        if (projectionError.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, projectionError);
            return;
        }

        string suffix = unknownCount == 0 ? string.Empty : $"; retained {unknownCount} unknown element payloads";
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {count} tag-array elements{suffix}.");
    }

    internal static void DecodeTagStructure(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (!ValidateContainerHeader(state, payload, typeSignature, "tagStructType"))
        {
            return;
        }

        uint structureSignature = ReadUInt32At(payload, 8);
        uint encodedCount = ReadUInt32At(payload, 12);
        if (encodedCount > (uint)((payload.Length - 16) / 12))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The tagStructType member count exceeds the number of records that fit in the payload.");
            return;
        }

        int count = (int)encodedCount;
        int tableEnd = 16 + (count * 12);
        HierarchicalPosition[] positions = new HierarchicalPosition[count];
        System.Collections.Generic.Dictionary<uint, bool> memberSignatures = new System.Collections.Generic.Dictionary<uint, bool>();
        int index = 0;
        while (index < count)
        {
            int recordOffset = 16 + (index * 12);
            uint memberSignature = ReadUInt32At(payload, recordOffset);
            if (memberSignatures.ContainsKey(memberSignature))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Tag-structure member signature '{FourCcCodec.Format(memberSignature)}' is duplicated.");
                return;
            }

            memberSignatures.Add(memberSignature, true);
            uint encodedOffset = ReadUInt32At(payload, recordOffset + 4);
            uint encodedSize = ReadUInt32At(payload, recordOffset + 8);
            HierarchicalPosition? position = CreatePosition(payload.Length, tableEnd, memberSignature, encodedOffset, encodedSize, index);
            if (position == null)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Tag-structure member {index} has an invalid, unaligned, or out-of-bounds position.");
                return;
            }

            positions[index] = position;
            index = index + 1;
        }

        string topologyError = ValidateTopology(payload, positions, tableEnd);
        if (topologyError.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, topologyError);
            return;
        }

        System.Collections.Generic.Dictionary<ulong, NestedDecodeOutcome> cache = new System.Collections.Generic.Dictionary<ulong, NestedDecodeOutcome>();
        IccStructureMember[] members = new IccStructureMember[count];
        int unknownCount = 0;
        index = 0;
        while (index < count)
        {
            HierarchicalPosition position = positions[index];
            NestedDecodeOutcome outcome = DecodeNestedWithCache(state, position, cache, position.MemberSignature);
            if (outcome.Status != IccOperationStatus.Success && outcome.Status != IccOperationStatus.UnsupportedFeature)
            {
                CompleteNestedFailure(state, payload, typeSignature, outcome, $"Tag-structure member {index}");
                return;
            }

            if (outcome.Status == IccOperationStatus.UnsupportedFeature)
            {
                unknownCount = unknownCount + 1;
            }

            IccStructureMember member = new IccStructureMember();
            member.Signature = position.MemberSignature;
            member.Data = outcome.Data;
            members[index] = member;
            index = index + 1;
        }

        IccTagStructure structure = new IccTagStructure();
        structure.StructureSignature = structureSignature;
        structure.Members = members;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.TagStructure);
        data.TagStructure = structure;
        string projectionError = StructuredTagProjectionCodec.ProjectDecoded(state, data);
        if (projectionError.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, projectionError);
            return;
        }

        string suffix = unknownCount == 0 ? string.Empty : $"; retained {unknownCount} unknown member payloads";
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {count} tag-structure members{suffix}.");
    }

    internal static byte[] SerializeTagArray(IccTagArray value)
    {
        return SerializeTagArrayWithLimit(value, 32);
    }

    internal static byte[] SerializeTagArrayWithLimit(IccTagArray value, int maximumNestingDepth)
    {
        ValidateWriteLimit(maximumNestingDepth);
        HierarchicalWriteContext context = new HierarchicalWriteContext(maximumNestingDepth);
        return SerializeArrayContainer(value, 1, context);
    }

    internal static byte[] SerializeTagStructure(IccTagStructure value)
    {
        return SerializeTagStructureWithLimit(value, 32);
    }

    internal static byte[] SerializeTagStructureWithLimit(IccTagStructure value, int maximumNestingDepth)
    {
        ValidateWriteLimit(maximumNestingDepth);
        HierarchicalWriteContext context = new HierarchicalWriteContext(maximumNestingDepth);
        return SerializeStructureContainer(value, 1, context);
    }

    internal static byte[] SerializeTagArrayForMode(IccTagArray value, int maximumNestingDepth, IccWriteMode mode)
    {
        ValidateWriteLimit(maximumNestingDepth);
        HierarchicalWriteContext context = new HierarchicalWriteContext(maximumNestingDepth);
        context.Mode = mode;
        return SerializeArrayContainer(value, 1, context);
    }

    internal static byte[] SerializeTagStructureForMode(IccTagStructure value, int maximumNestingDepth, IccWriteMode mode)
    {
        ValidateWriteLimit(maximumNestingDepth);
        HierarchicalWriteContext context = new HierarchicalWriteContext(maximumNestingDepth);
        context.Mode = mode;
        return SerializeStructureContainer(value, 1, context);
    }

    internal static byte[] SerializeTagArrayForProfile(IccTagArray value, int maximumNestingDepth, IccWriteMode mode, bool preserveUnknownData)
    {
        ValidateWriteLimit(maximumNestingDepth);
        HierarchicalWriteContext context = new HierarchicalWriteContext(maximumNestingDepth);
        context.Mode = mode;
        context.PreserveUnknownData = preserveUnknownData;
        return SerializeArrayContainer(value, 1, context);
    }

    internal static byte[] SerializeTagStructureForProfile(IccTagStructure value, int maximumNestingDepth, IccWriteMode mode, bool preserveUnknownData)
    {
        ValidateWriteLimit(maximumNestingDepth);
        HierarchicalWriteContext context = new HierarchicalWriteContext(maximumNestingDepth);
        context.Mode = mode;
        context.PreserveUnknownData = preserveUnknownData;
        return SerializeStructureContainer(value, 1, context);
    }

    internal static byte[] SerializeArrayContainer(IccTagArray value, int depth, HierarchicalWriteContext context)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("A tag array cannot be null.");
        }

        if (value.Values == null)
        {
            throw new IccProfileWriteException("Tag-array values cannot be null.");
        }

        ValidateWriteDepth(depth, context);
        RegisteredArrayProjectionCodec.ValidateForWrite(value);
        byte[][] encoded = new byte[value.Values.Length][];
        int[] offsets = new int[value.Values.Length];
        int[] sizes = new int[value.Values.Length];
        System.Collections.Generic.Dictionary<IccTagData, int> sharedOffsets = new System.Collections.Generic.Dictionary<IccTagData, int>();
        uint elementType = 0;
        uint structureType = 0;
        bool hasStructureType = false;
        long end = 16L + ((long)value.Values.Length * 8L);
        int index = 0;
        while (index < value.Values.Length)
        {
            IccTagData item = value.Values[index];
            if (item == null)
            {
                throw new IccProfileWriteException($"Tag-array element {index} cannot be null.");
            }

            byte[] bytes = NestedTagDataSerializer.Serialize(item, depth, context);
            uint typeSignature = ReadUInt32FromArray(bytes, 0);
            if (index == 0)
            {
                elementType = typeSignature;
            }
            else if (typeSignature != elementType)
            {
                throw new IccProfileWriteException("All tag-array elements shall have the same tag type signature.");
            }

            if (typeSignature == IccTypeSignatures.TagStructure)
            {
                uint nestedStructureType = ReadUInt32FromArray(bytes, 8);
                if (!hasStructureType)
                {
                    structureType = nestedStructureType;
                    hasStructureType = true;
                }
                else if (nestedStructureType != structureType && value.ArraySignature != IccArraySignatures.NamedColor)
                {
                    throw new IccProfileWriteException("All tag structures in a tag array shall have the same structure type identifier.");
                }
            }

            encoded[index] = bytes;
            sizes[index] = bytes.Length;
            if (sharedOffsets.ContainsKey(item))
            {
                offsets[index] = sharedOffsets[item];
            }
            else
            {
                end = AlignFour(end);
                if (end > System.Array.MaxLength || bytes.Length > System.Array.MaxLength - end)
                {
                    throw new IccProfileWriteException("The tag-array payload exceeds the supported managed array size.");
                }

                offsets[index] = (int)end;
                sharedOffsets.Add(item, offsets[index]);
                end = end + bytes.Length;
            }

            index = index + 1;
        }

        byte[] payload = new byte[(int)end];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.TagArray);
        writer.WriteUInt32(0);
        writer.WriteUInt32(value.ArraySignature);
        writer.WriteUInt32((uint)value.Values.Length);
        index = 0;
        while (index < value.Values.Length)
        {
            writer.WriteUInt32((uint)offsets[index]);
            writer.WriteUInt32((uint)sizes[index]);
            index = index + 1;
        }

        CopyUniquePayloads(payload, encoded, offsets);
        return payload;
    }

    internal static byte[] SerializeStructureContainer(IccTagStructure value, int depth, HierarchicalWriteContext context)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("A tag structure cannot be null.");
        }

        if (value.Members == null)
        {
            throw new IccProfileWriteException("Tag-structure members cannot be null.");
        }

        ValidateWriteDepth(depth, context);
        StructuredTagProjectionCodec.ValidateForWrite(value);
        IccStructureMember[] members = CreateMemberOrder(value.Members, context.Mode);
        byte[][] encoded = new byte[members.Length][];
        int[] offsets = new int[members.Length];
        int[] sizes = new int[members.Length];
        System.Collections.Generic.Dictionary<IccTagData, int> sharedOffsets = new System.Collections.Generic.Dictionary<IccTagData, int>();
        System.Collections.Generic.Dictionary<uint, bool> signatures = new System.Collections.Generic.Dictionary<uint, bool>();
        long end = 16L + ((long)members.Length * 12L);
        int index = 0;
        while (index < members.Length)
        {
            IccStructureMember member = members[index];
            if (member == null || member.Data == null)
            {
                throw new IccProfileWriteException($"Tag-structure member {index} and its data cannot be null.");
            }

            if (signatures.ContainsKey(member.Signature))
            {
                throw new IccProfileWriteException($"Tag-structure member signature '{FourCcCodec.Format(member.Signature)}' is duplicated.");
            }

            signatures.Add(member.Signature, true);
            byte[] bytes = NestedTagDataSerializer.Serialize(member.Data, depth, context);
            encoded[index] = bytes;
            sizes[index] = bytes.Length;
            if (sharedOffsets.ContainsKey(member.Data))
            {
                offsets[index] = sharedOffsets[member.Data];
            }
            else
            {
                end = AlignFour(end);
                if (end > System.Array.MaxLength || bytes.Length > System.Array.MaxLength - end)
                {
                    throw new IccProfileWriteException("The tag-structure payload exceeds the supported managed array size.");
                }

                offsets[index] = (int)end;
                sharedOffsets.Add(member.Data, offsets[index]);
                end = end + bytes.Length;
            }

            index = index + 1;
        }

        byte[] payload = new byte[(int)end];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.TagStructure);
        writer.WriteUInt32(0);
        writer.WriteUInt32(value.StructureSignature);
        writer.WriteUInt32((uint)members.Length);
        index = 0;
        while (index < members.Length)
        {
            writer.WriteUInt32(members[index].Signature);
            writer.WriteUInt32((uint)offsets[index]);
            writer.WriteUInt32((uint)sizes[index]);
            index = index + 1;
        }

        CopyUniquePayloads(payload, encoded, offsets);
        return payload;
    }

    internal static IccStructureMember[] CreateMemberOrder(IccStructureMember[] members, IccWriteMode mode)
    {
        IccStructureMember[] ordered = new IccStructureMember[members.Length];
        int index = 0;
        while (index < members.Length)
        {
            IccStructureMember member = members[index];
            ordered[index] = member;
            if (mode == IccWriteMode.Canonical && member != null)
            {
                int insertion = index;
                while (insertion > 0 && ordered[insertion - 1] != null && ordered[insertion - 1].Signature > member.Signature)
                {
                    ordered[insertion] = ordered[insertion - 1];
                    insertion = insertion - 1;
                }

                ordered[insertion] = member;
            }

            index = index + 1;
        }

        return ordered;
    }

    internal static bool ValidateContainerHeader(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, string description)
    {
        if (payload.Length < 16)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A {description} payload requires a 16-byte header, but this payload contains {payload.Length} bytes.");
            return false;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The {description} reserved bytes shall be zero.");
            return false;
        }

        if (state.MaximumNestingDepth < 0 || state.CurrentNestingDepth >= state.MaximumNestingDepth)
        {
            CompleteLimitFailure(state, payload, typeSignature, $"Decoding {description} would exceed the configured nesting depth of {state.MaximumNestingDepth}.");
            return false;
        }

        return true;
    }

    internal static HierarchicalPosition? CreatePosition(int payloadLength, int tableEnd, uint memberSignature, uint encodedOffset, uint encodedSize, int index)
    {
        if (encodedOffset > int.MaxValue || encodedSize > int.MaxValue)
        {
            return null;
        }

        int offset = (int)encodedOffset;
        int size = (int)encodedSize;
        if (offset < tableEnd || (offset & 3) != 0 || size < 8 || offset > payloadLength || size > payloadLength - offset)
        {
            return null;
        }

        return new HierarchicalPosition(memberSignature, offset, size, index);
    }

    internal static string ValidateTopology(System.ReadOnlyMemory<byte> payload, HierarchicalPosition[] positions, int tableEnd)
    {
        HierarchicalPosition[] sorted = new HierarchicalPosition[positions.Length];
        int index = 0;
        while (index < positions.Length)
        {
            sorted[index] = positions[index];
            int insertion = index;
            while (insertion > 0 && ComparePositions(sorted[insertion], sorted[insertion - 1]) < 0)
            {
                HierarchicalPosition swap = sorted[insertion - 1];
                sorted[insertion - 1] = sorted[insertion];
                sorted[insertion] = swap;
                insertion = insertion - 1;
            }

            index = index + 1;
        }

        int cursor = tableEnd;
        int previousOffset = -1;
        int previousSize = -1;
        index = 0;
        while (index < sorted.Length)
        {
            HierarchicalPosition position = sorted[index];
            if (position.Offset == previousOffset && position.Size == previousSize)
            {
                index = index + 1;
                continue;
            }

            if (position.Offset < cursor)
            {
                return "Hierarchical tag element positions partially overlap; only exact shared positions are permitted.";
            }

            if (!HasZeroRange(payload, cursor, position.Offset - cursor))
            {
                return "Hierarchical tag element alignment gaps shall contain only zero padding bytes.";
            }

            cursor = position.Offset + position.Size;
            previousOffset = position.Offset;
            previousSize = position.Size;
            index = index + 1;
        }

        if (!HasZeroRange(payload, cursor, payload.Length - cursor))
        {
            return "Bytes after the final hierarchical tag element shall contain only zero padding.";
        }

        return string.Empty;
    }

    internal static NestedDecodeOutcome DecodeNestedWithCache(TagDecodeState parent, HierarchicalPosition position, System.Collections.Generic.Dictionary<ulong, NestedDecodeOutcome> cache, uint nestedContextSignature)
    {
        ulong key = ((ulong)(uint)position.Offset << 32) | (uint)position.Size;
        bool isContextual = MultiProcessElementsTagCodec.IsContextualTransformTag(nestedContextSignature);
        if (!isContextual && cache.ContainsKey(key))
        {
            return cache[key];
        }

        int nestedOffset = parent.PayloadOffset + position.Offset;
        long absoluteOffset = CheckedSize.AddOffsets(parent.AbsolutePayloadOffset, position.Offset, long.MaxValue);
        TagDecodeState nested = new TagDecodeState(parent.Source, nestedOffset, position.Size, absoluteOffset, parent.ProfileVersion, parent.PreserveOriginalBytes);
        CopyNestedContext(parent, nested);
        if (isContextual)
        {
            nested.TagSignature = nestedContextSignature;
        }

        TagCodecDispatcher.DecodeTagPayload(nested);
        NestedDecodeOutcome outcome = new NestedDecodeOutcome(nested.Data, nested.Result.Status, nested.Result.Message);
        if (!isContextual)
        {
            cache.Add(key, outcome);
        }

        return outcome;
    }

    internal static void CopyNestedContext(TagDecodeState parent, TagDecodeState nested)
    {
        nested.MaximumDecompressedBytes = parent.MaximumDecompressedBytes;
        nested.MaximumProfileBytes = parent.MaximumProfileBytes;
        nested.MaximumTagCount = parent.MaximumTagCount;
        nested.MaximumTagBytes = parent.MaximumTagBytes;
        nested.MaximumTotalTagBytes = parent.MaximumTotalTagBytes;
        nested.MaximumDiagnosticCount = parent.MaximumDiagnosticCount;
        nested.MaximumNestingDepth = parent.MaximumNestingDepth;
        nested.CurrentNestingDepth = parent.CurrentNestingDepth + 1;
        nested.MaximumCalculatorInstructions = parent.MaximumCalculatorInstructions;
        nested.MaximumCalculatorStackDepth = parent.MaximumCalculatorStackDepth;
        nested.MaximumCalculatorStorageValues = parent.MaximumCalculatorStorageValues;
        nested.HasProfileContext = parent.HasProfileContext;
        nested.ProfileClass = parent.ProfileClass;
        nested.ProfileClassSignature = parent.ProfileClassSignature;
        nested.DataColorSpace = parent.DataColorSpace;
        nested.DataColorSpaceSignature = parent.DataColorSpaceSignature;
        nested.ProfileConnectionSpace = parent.ProfileConnectionSpace;
        nested.ProfileConnectionSpaceSignature = parent.ProfileConnectionSpaceSignature;
        nested.SpectralPcsSignature = parent.SpectralPcsSignature;
        nested.MaterialConnectionSpace = parent.MaterialConnectionSpace;
        nested.MaterialConnectionSpaceSignature = parent.MaterialConnectionSpaceSignature;
        nested.HasMatrixPrimaries = parent.HasMatrixPrimaries;
        nested.IsEmbedded = parent.IsEmbedded;
        nested.RequiresEmbeddedData = parent.RequiresEmbeddedData;
    }

    internal static void CompleteNestedFailure(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, NestedDecodeOutcome outcome, string description)
    {
        if (outcome.Status == IccOperationStatus.ResourceLimitExceeded)
        {
            CompleteLimitFailure(state, payload, typeSignature, $"{description} could not be decoded: {outcome.Message}");
            return;
        }

        TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"{description} could not be decoded: {outcome.Message}");
    }

    internal static void CompleteLimitFailure(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, string message)
    {
        IccTagData data = new IccTagData();
        if (state.PreserveOriginalBytes)
        {
            UnknownTagCodec.PopulateRawFallback(data, payload, typeSignature, true);
        }

        IccOperationResult result = UnknownTagCodec.CreateDecodeResult(state, IccOperationStatus.ResourceLimitExceeded);
        result.Message = message;
        UnknownTagCodec.CompleteDecodeState(state, data, result);
    }

    internal static void CopyUniquePayloads(byte[] destination, byte[][] encoded, int[] offsets)
    {
        System.Collections.Generic.Dictionary<int, bool> copied = new System.Collections.Generic.Dictionary<int, bool>();
        int index = 0;
        while (index < encoded.Length)
        {
            int offset = offsets[index];
            if (!copied.ContainsKey(offset))
            {
                System.ReadOnlySpan<byte> source = encoded[index];
                source.CopyTo(new System.Span<byte>(destination, offset, source.Length));
                copied.Add(offset, true);
            }

            index = index + 1;
        }
    }

    internal static int ComparePositions(HierarchicalPosition first, HierarchicalPosition second)
    {
        if (first.Offset != second.Offset)
        {
            return first.Offset < second.Offset ? -1 : 1;
        }

        if (first.Size != second.Size)
        {
            return first.Size < second.Size ? -1 : 1;
        }

        return first.OriginalIndex < second.OriginalIndex ? -1 : first.OriginalIndex == second.OriginalIndex ? 0 : 1;
    }

    internal static bool HasZeroRange(System.ReadOnlyMemory<byte> bytes, int offset, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (bytes.Span[offset + index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static long AlignFour(long value)
    {
        return (value + 3L) & -4L;
    }

    internal static void ValidateWriteDepth(int depth, HierarchicalWriteContext context)
    {
        if (depth > context.MaximumNestingDepth)
        {
            throw new IccProfileWriteException($"The hierarchical tag exceeds the configured nesting depth of {context.MaximumNestingDepth}.");
        }
    }

    internal static void ValidateWriteLimit(int maximumNestingDepth)
    {
        if (maximumNestingDepth < 0)
        {
            throw new IccProfileWriteException("The maximum hierarchical tag nesting depth cannot be negative.");
        }
    }

    internal static uint ReadUInt32At(System.ReadOnlyMemory<byte> bytes, int offset)
    {
        return ((uint)bytes.Span[offset] << 24)
            | ((uint)bytes.Span[offset + 1] << 16)
            | ((uint)bytes.Span[offset + 2] << 8)
            | bytes.Span[offset + 3];
    }

    internal static uint ReadUInt32FromArray(byte[] bytes, int offset)
    {
        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }
}
