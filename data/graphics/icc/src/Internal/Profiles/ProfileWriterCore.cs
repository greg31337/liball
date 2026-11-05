namespace icc;

internal class ProfileWriteItem
{
    internal uint Signature;
    internal byte[] Payload;
    internal int Offset;
    internal int OriginalIndex;
    internal int SourceIndex;
    internal int SharedDataId;
    internal byte[] OriginalPaddingBytes;
    internal byte[] PaddingBytes;

    internal ProfileWriteItem(uint signature, byte[] payload, int originalIndex, int sourceIndex, int sharedDataId, byte[] originalPaddingBytes)
    {
        Signature = signature;
        Payload = payload;
        Offset = -1;
        OriginalIndex = originalIndex;
        SourceIndex = sourceIndex;
        SharedDataId = sharedDataId;
        OriginalPaddingBytes = originalPaddingBytes;
        PaddingBytes = new byte[0];
    }
}

internal class ProfileWritePlan
{
    internal byte[] HeaderBytes;
    internal ProfileWriteItem[] Items;
    internal int TotalSize;
    internal bool ComputeProfileId;

    internal ProfileWritePlan(byte[] headerBytes, ProfileWriteItem[] items, int totalSize, bool computeProfileId)
    {
        HeaderBytes = headerBytes;
        Items = items;
        TotalSize = totalSize;
        ComputeProfileId = computeProfileId;
    }
}

internal static class ProfileWriterCore
{
    internal static ProfileWritePlan BuildPlan(IccProfile profile, IccWriteOptions options)
    {
        ValidateInputs(profile, options);
        IccTag[] tags = profile.Tags;
        if (tags.Length > options.ResourceLimits.MaximumTagCount)
        {
            throw new IccProfileWriteException($"The profile contains {tags.Length} tags, exceeding the configured maximum of {options.ResourceLimits.MaximumTagCount}.");
        }

        long tableEnd = IccConstants.MinimumProfileSize + ((long)tags.Length * IccConstants.TagTableEntrySize);
        if (tableEnd > uint.MaxValue || tableEnd > options.ResourceLimits.MaximumProfileBytes || tableEnd > System.Array.MaxLength)
        {
            throw new IccProfileWriteException("The profile header and tag table exceed the configured or supported profile-size limit.");
        }

        ProfileWriteItem[] items = new ProfileWriteItem[tags.Length];
        int index = 0;
        System.Collections.Generic.Dictionary<uint, bool> signatures = new System.Collections.Generic.Dictionary<uint, bool>();
        while (index < tags.Length)
        {
            IccTag tag = tags[index];
            if (tag == null || tag.Data == null || tag.TableEntry == null || tag.PaddingBytes == null)
            {
                throw new IccProfileWriteException($"Profile tag {index}, its data, table entry, and padding bytes cannot be null.");
            }

            if (signatures.ContainsKey(tag.Signature))
            {
                throw new IccProfileWriteException($"Profile tag {index} duplicates signature 0x{tag.Signature:X8}.");
            }

            signatures.Add(tag.Signature, true);
            ProfileTagSerializer.ValidateWritableCodecs(tag, index, profile.Header.Version.Kind);
            byte[] payload = ProfileTagSerializer.SerializeTag(tag, profile.Header, options);
            if (payload == null || payload.Length < 8)
            {
                throw new IccProfileWriteException($"{DescribeTag(index, tag.Signature)} does not encode a complete eight-byte tag type header.");
            }

            if (payload.Length > options.ResourceLimits.MaximumTagBytes)
            {
                string childFailure = ProfileTagSerializer.FindOversizedChildDescription(tag, payload, options.ResourceLimits.MaximumTagBytes);
                if (childFailure.Length > 0)
                {
                    throw new IccProfileWriteException($"{DescribeTag(index, tag.Signature)} contains {childFailure}; its complete payload contains {payload.Length} bytes, exceeding the configured maximum of {options.ResourceLimits.MaximumTagBytes}.");
                }

                throw new IccProfileWriteException($"{DescribeTag(index, tag.Signature)} payload contains {payload.Length} bytes, exceeding the configured maximum of {options.ResourceLimits.MaximumTagBytes}.");
            }

            items[index] = new ProfileWriteItem(tag.Signature, payload, tag.TableEntry.OriginalIndex, index, tag.TableEntry.SharedDataId, CloneBytes(tag.PaddingBytes));
            index = index + 1;
        }

        if (options.Mode == IccWriteMode.Canonical)
        {
            SortItemsByUnsignedSignature(items);
        }
        else
        {
            SortItemsForPreservation(items);
        }

        bool internPayloads = options.Mode == IccWriteMode.Canonical && options.InternIdenticalTagPayloads;
        System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.List<ProfileWriteItem>> interned = new System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.List<ProfileWriteItem>>();
        System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<ProfileWriteItem>> preservedShared = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<ProfileWriteItem>>();
        long totalPayloadBytes = 0;
        long nextOffset = tableEnd;
        index = 0;
        while (index < tags.Length)
        {
            ProfileWriteItem item = items[index];
            byte[] payload = item.Payload;
            int sharedOffset = -1;
            ulong hash = 0;
            if (internPayloads)
            {
                hash = ComputePayloadHash(payload);
                sharedOffset = FindInternedOffset(interned, hash, payload);
            }
            else if (options.Mode == IccWriteMode.Preservation && item.SharedDataId >= 0)
            {
                sharedOffset = FindPreservedSharedOffset(preservedShared, item.SharedDataId, payload);
            }

            if (sharedOffset >= 0)
            {
                item.Offset = sharedOffset;
                index = index + 1;
                continue;
            }

            if (nextOffset > uint.MaxValue || nextOffset > int.MaxValue)
            {
                throw new IccProfileWriteException($"The stable payload offset for {DescribeTag(item.SourceIndex, item.Signature)} exceeds the ICC 32-bit profile representation.");
            }

            item.Offset = (int)nextOffset;
            if (internPayloads)
            {
                AddInternedPayload(interned, hash, item);
            }
            else if (options.Mode == IccWriteMode.Preservation && item.SharedDataId >= 0)
            {
                AddPreservedSharedPayload(preservedShared, item.SharedDataId, item);
            }

            totalPayloadBytes = totalPayloadBytes + payload.Length;
            if (totalPayloadBytes > options.ResourceLimits.MaximumTotalTagBytes)
            {
                throw new IccProfileWriteException($"Unique profile tag payloads total {totalPayloadBytes} bytes at {DescribeTag(item.SourceIndex, item.Signature)}, exceeding the configured maximum of {options.ResourceLimits.MaximumTotalTagBytes}.");
            }

            int paddingCount = (IccConstants.TagPayloadAlignment - (payload.Length & (IccConstants.TagPayloadAlignment - 1))) & (IccConstants.TagPayloadAlignment - 1);
            item.PaddingBytes = SelectPaddingBytes(item.OriginalPaddingBytes, paddingCount, options.Mode);
            nextOffset = nextOffset + payload.Length + item.PaddingBytes.Length;
            if (nextOffset > uint.MaxValue || nextOffset > options.ResourceLimits.MaximumProfileBytes || nextOffset > System.Array.MaxLength)
            {
                throw new IccProfileWriteException($"The final profile size after {DescribeTag(item.SourceIndex, item.Signature)} exceeds the ICC 32-bit, configured, or supported managed size limit.");
            }

            index = index + 1;
        }

        byte[] headerBytes = HeaderCodec.SerializeHeader(profile.Header, options, (int)nextOffset);
        bool computeProfileId = options.ComputeProfileId
            && profile.Header.ProfileClass != IccProfileClass.ColorEncodingSpace
            && (profile.Header.Version.Kind == IccVersionKind.Version4
                || profile.Header.Version.Kind == IccVersionKind.Version5);
        return new ProfileWritePlan(headerBytes, items, (int)nextOffset, computeProfileId);
    }

    internal static void SortItemsByUnsignedSignature(ProfileWriteItem[] items)
    {
        int index = 1;
        while (index < items.Length)
        {
            ProfileWriteItem item = items[index];
            int destination = index;
            while (destination > 0 && items[destination - 1].Signature > item.Signature)
            {
                items[destination] = items[destination - 1];
                destination = destination - 1;
            }

            items[destination] = item;
            index = index + 1;
        }
    }

    internal static void SortItemsForPreservation(ProfileWriteItem[] items)
    {
        int index = 1;
        while (index < items.Length)
        {
            ProfileWriteItem item = items[index];
            int destination = index;
            while (destination > 0 && ComparePreservationOrder(items[destination - 1], item) > 0)
            {
                items[destination] = items[destination - 1];
                destination = destination - 1;
            }

            items[destination] = item;
            index = index + 1;
        }
    }

    internal static int ComparePreservationOrder(ProfileWriteItem first, ProfileWriteItem second)
    {
        bool firstOriginal = first.OriginalIndex >= 0;
        bool secondOriginal = second.OriginalIndex >= 0;
        if (firstOriginal && !secondOriginal)
        {
            return -1;
        }

        if (!firstOriginal && secondOriginal)
        {
            return 1;
        }

        if (firstOriginal && secondOriginal && first.OriginalIndex != second.OriginalIndex)
        {
            return first.OriginalIndex < second.OriginalIndex ? -1 : 1;
        }

        if (first.SourceIndex == second.SourceIndex)
        {
            return 0;
        }

        return first.SourceIndex < second.SourceIndex ? -1 : 1;
    }

    internal static ulong ComputePayloadHash(byte[] payload)
    {
        ulong hash = (ulong)payload.Length;
        int index = 0;
        while (index < payload.Length)
        {
            hash = (hash << 5) | (hash >> 59);
            hash = hash ^ payload[index];
            index = index + 1;
        }

        return hash;
    }

    internal static int FindInternedOffset(System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.List<ProfileWriteItem>> interned, ulong hash, byte[] payload)
    {
        if (!interned.ContainsKey(hash))
        {
            return -1;
        }

        System.Collections.Generic.List<ProfileWriteItem> candidates = interned[hash];
        int index = 0;
        while (index < candidates.Count)
        {
            ProfileWriteItem candidate = candidates[index];
            if (PayloadsEqual(candidate.Payload, payload))
            {
                return candidate.Offset;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static void AddInternedPayload(System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.List<ProfileWriteItem>> interned, ulong hash, ProfileWriteItem item)
    {
        System.Collections.Generic.List<ProfileWriteItem> bucket;
        if (interned.ContainsKey(hash))
        {
            bucket = interned[hash];
        }
        else
        {
            bucket = new System.Collections.Generic.List<ProfileWriteItem>();
            interned.Add(hash, bucket);
        }

        bucket.Add(item);
    }

    internal static int FindPreservedSharedOffset(System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<ProfileWriteItem>> preservedShared, int sharedDataId, byte[] payload)
    {
        if (!preservedShared.ContainsKey(sharedDataId))
        {
            return -1;
        }

        System.Collections.Generic.List<ProfileWriteItem> candidates = preservedShared[sharedDataId];
        int index = 0;
        while (index < candidates.Count)
        {
            ProfileWriteItem candidate = candidates[index];
            if (PayloadsEqual(candidate.Payload, payload))
            {
                return candidate.Offset;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static void AddPreservedSharedPayload(System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<ProfileWriteItem>> preservedShared, int sharedDataId, ProfileWriteItem item)
    {
        System.Collections.Generic.List<ProfileWriteItem> bucket;
        if (preservedShared.ContainsKey(sharedDataId))
        {
            bucket = preservedShared[sharedDataId];
        }
        else
        {
            bucket = new System.Collections.Generic.List<ProfileWriteItem>();
            preservedShared.Add(sharedDataId, bucket);
        }

        bucket.Add(item);
    }

    internal static byte[] SelectPaddingBytes(byte[] originalPaddingBytes, int paddingCount, IccWriteMode mode)
    {
        if (mode == IccWriteMode.Preservation && originalPaddingBytes.Length == paddingCount)
        {
            return CloneBytes(originalPaddingBytes);
        }

        return new byte[paddingCount];
    }

    internal static bool PayloadsEqual(byte[] first, byte[] second)
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

            index = index + 1;
        }

        return true;
    }

    internal static byte[] EncodePlan(ProfileWritePlan plan)
    {
        byte[] output = new byte[plan.TotalSize];
        BigEndianWriter writer = new BigEndianWriter(output, 0);
        writer.WriteExactBytes(plan.HeaderBytes);
        writer.WriteUInt32((uint)plan.Items.Length);
        int index = 0;
        while (index < plan.Items.Length)
        {
            ProfileWriteItem item = plan.Items[index];
            writer.WriteUInt32(item.Signature);
            writer.WriteUInt32((uint)item.Offset);
            writer.WriteUInt32((uint)item.Payload.Length);
            index = index + 1;
        }

        index = 0;
        while (index < plan.Items.Length)
        {
            ProfileWriteItem item = plan.Items[index];
            int gapSize = item.Offset - writer.GetWritePosition();
            if (gapSize < 0)
            {
                index = index + 1;
                continue;
            }

            writer.WriteZeroBytes(gapSize);
            writer.WriteExactBytes(item.Payload);
            writer.WriteExactBytes(item.PaddingBytes);
            index = index + 1;
        }

        if (writer.GetWritePosition() != plan.TotalSize)
        {
            throw new IccProfileWriteException("The profile write plan did not produce its preflighted byte count.");
        }

        if (plan.ComputeProfileId)
        {
            IccProfileId computed = ProfileIdCore.Compute(output);
            System.Array.Copy(computed.Bytes, 0, output, ProfileIdCore.ProfileIdOffset, IccConstants.ProfileIdSize);
        }

        return output;
    }

    internal static IccOperationResult CreateBufferResult(IccOperationStatus status, string message, int requiredSize)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        result.Location.Offset = 0;
        result.Location.Length = requiredSize;
        return result;
    }

    internal static void ValidateInputs(IccProfile profile, IccWriteOptions options)
    {
        if (profile == null || profile.Header == null || profile.Tags == null)
        {
            throw new IccProfileWriteException("A profile, header, and tag collection are required for writing.");
        }

        if (options == null || options.ResourceLimits == null)
        {
            throw new IccProfileWriteException("Profile write options and resource limits are required.");
        }

        if (options.Mode != IccWriteMode.Canonical && options.Mode != IccWriteMode.Preservation)
        {
            throw new IccProfileWriteException($"Profile write mode value {(int)options.Mode} is not supported.");
        }

        IccResourceLimits limits = options.ResourceLimits;
        if (limits.MaximumProfileBytes < IccConstants.MinimumProfileSize || limits.MaximumTagCount < 0
            || limits.MaximumTagBytes < 0 || limits.MaximumTotalTagBytes < 0 || limits.MaximumNestingDepth < 0
            || limits.MaximumDecompressedBytes < 0 || limits.MaximumCalculatorInstructions < 0
            || limits.MaximumCalculatorStackDepth < 0 || limits.MaximumCalculatorStorageValues < 0
            || limits.MaximumDiagnosticCount < 0)
        {
            throw new IccProfileWriteException("Profile, tag, payload, nesting, and decompression write limits shall be non-negative and permit a complete ICC profile.");
        }

        if (options.RejectInvalidProfile && HasKnownInvalidData(profile))
        {
            throw new IccProfileWriteException("The profile contains retained validation or tag-decoding errors and RejectInvalidProfile is enabled.");
        }
    }

    internal static bool HasKnownInvalidData(IccProfile profile)
    {
        if (ContainsErrorIssue(profile.ValidationIssues) || ContainsErrorIssue(profile.Header.ValidationIssues))
        {
            return true;
        }

        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.IsDecodeAttempted && tag.DecodeResult != null
                && (tag.DecodeResult.Status == IccOperationStatus.InvalidData
                    || tag.DecodeResult.Status == IccOperationStatus.ResourceLimitExceeded
                    || tag.DecodeResult.Status == IccOperationStatus.Conflict
                    || tag.DecodeResult.Status == IccOperationStatus.Failed))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsErrorIssue(IccValidationIssue[] issues)
    {
        if (issues == null)
        {
            return true;
        }

        int index = 0;
        while (index < issues.Length)
        {
            IccValidationIssue issue = issues[index];
            if (issue == null || issue.Severity == IccValidationSeverity.Error || issue.Severity == IccValidationSeverity.Fatal)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static byte[] CloneBytes(byte[] source)
    {
        byte[] clone = new byte[source.Length];
        System.Array.Copy(source, clone, clone.Length);
        return clone;
    }

    internal static string DescribeTag(int index, uint signature)
    {
        return $"profile tag {index} (signature '{FourCcCodec.Format(signature)}', 0x{signature:X8})";
    }

}

internal static class ProfileTagSerializer
{
    internal static void ValidateWritableCodecs(IccTag tag, int tagIndex, IccVersionKind version)
    {
        string tagDescription = ProfileWriterCore.DescribeTag(tagIndex, tag.Signature);
        if (tag.Data.Kind != IccTagDataKind.Unknown || tag.DecodeState == null)
        {
            if (!HasTopLevelCodec(tag.Data.Kind) && !HasCompleteRetainedPayload(tag.Data))
            {
                throw new IccProfileWriteException($"{tagDescription} data kind {tag.Data.Kind} has no writable codec and no complete retained raw payload.");
            }
        }

        if (tag.Data.Kind == IccTagDataKind.MultiProcessElements && tag.Data.MultiProcessElements != null)
        {
            ValidateProcessingElementCodecs(tag.Data.MultiProcessElements, version, tagDescription);
        }

        System.Collections.Generic.Dictionary<IccTagData, bool> active = new System.Collections.Generic.Dictionary<IccTagData, bool>();
        if (tag.Data.Kind == IccTagDataKind.TagArray && tag.Data.TagArray != null && tag.Data.TagArray.Values != null)
        {
            ValidateNestedArrayCodecs(tag.Data.TagArray, tagDescription, active);
        }
        else if (tag.Data.Kind == IccTagDataKind.TagStructure && tag.Data.TagStructure != null && tag.Data.TagStructure.Members != null)
        {
            ValidateNestedStructureCodecs(tag.Data.TagStructure, tagDescription, active);
        }
    }

    internal static bool HasTopLevelCodec(IccTagDataKind kind)
    {
        switch (kind)
        {
            case IccTagDataKind.Raw:
            case IccTagDataKind.IntegerArray:
            case IccTagDataKind.FixedPointArray:
            case IccTagDataKind.FloatingPointArray:
            case IccTagDataKind.XyzArray:
            case IccTagDataKind.Signature:
            case IccTagDataKind.DateTime:
            case IccTagDataKind.Binary:
            case IccTagDataKind.Text:
            case IccTagDataKind.LocalizedText:
            case IccTagDataKind.Dictionary:
            case IccTagDataKind.Curve:
            case IccTagDataKind.Lut:
            case IccTagDataKind.MultiProcessElements:
            case IccTagDataKind.Measurement:
            case IccTagDataKind.ViewingConditions:
            case IccTagDataKind.Chromaticity:
            case IccTagDataKind.Cicp:
            case IccTagDataKind.NamedColors:
            case IccTagDataKind.Colorants:
            case IccTagDataKind.ResponseCurves:
            case IccTagDataKind.ProfileSequence:
            case IccTagDataKind.AdaptiveGainCurve:
            case IccTagDataKind.TagArray:
            case IccTagDataKind.TagStructure:
            case IccTagDataKind.GamutBoundary:
            case IccTagDataKind.EmbeddedImage:
            case IccTagDataKind.CrdInfo:
            case IccTagDataKind.Screening:
            case IccTagDataKind.UcrBg:
            case IccTagDataKind.SparseMatrixArray:
            case IccTagDataKind.SpectralViewingConditions:
            case IccTagDataKind.SpectralRange:
            case IccTagDataKind.EmbeddedProfile:
                return true;
            default:
                return false;
        }
    }

    internal static bool HasNestedCodec(IccTagDataKind kind)
    {
        switch (kind)
        {
            case IccTagDataKind.Raw:
            case IccTagDataKind.IntegerArray:
            case IccTagDataKind.FixedPointArray:
            case IccTagDataKind.FloatingPointArray:
            case IccTagDataKind.XyzArray:
            case IccTagDataKind.Signature:
            case IccTagDataKind.DateTime:
            case IccTagDataKind.Binary:
            case IccTagDataKind.Text:
            case IccTagDataKind.LocalizedText:
            case IccTagDataKind.Dictionary:
            case IccTagDataKind.Curve:
            case IccTagDataKind.Lut:
            case IccTagDataKind.MultiProcessElements:
            case IccTagDataKind.Measurement:
            case IccTagDataKind.ViewingConditions:
            case IccTagDataKind.Chromaticity:
            case IccTagDataKind.SpectralViewingConditions:
            case IccTagDataKind.SpectralRange:
            case IccTagDataKind.EmbeddedProfile:
            case IccTagDataKind.SparseMatrixArray:
            case IccTagDataKind.EmbeddedImage:
            case IccTagDataKind.TagArray:
            case IccTagDataKind.TagStructure:
                return true;
            default:
                return false;
        }
    }

    internal static bool HasCompleteRetainedPayload(IccTagData data)
    {
        return data.RawData != null && data.RawData.HasTypeSignature && data.RawData.Bytes != null && data.RawData.Bytes.Length >= 8;
    }

    internal static void ValidateProcessingElementCodecs(IccMultiProcessElements pipeline, IccVersionKind version, string tagDescription)
    {
        if (pipeline.Elements == null)
        {
            return;
        }

        int index = 0;
        while (index < pipeline.Elements.Length)
        {
            IccProcessingElement element = pipeline.Elements[index];
            if (element != null && !HasProcessingElementCodec(element.Kind, version)
                && (element.RawData == null || element.RawData.Bytes == null || element.RawData.Bytes.Length < 12))
            {
                throw new IccProfileWriteException($"{tagDescription} processing element {index} (signature 0x{element.Signature:X8}, kind {element.Kind}) has no writable codec for ICC version {VersionDescription(version)} and no complete retained raw element.");
            }

            index = index + 1;
        }
    }

    internal static bool HasProcessingElementCodec(IccProcessingElementKind kind, IccVersionKind version)
    {
        if (version == IccVersionKind.Version5)
        {
            return Icc2ProcessingElementTagCodec.IsBodyCodecAvailable(kind);
        }

        if (version != IccVersionKind.Version4)
        {
            return false;
        }

        return kind == IccProcessingElementKind.CurveSet || kind == IccProcessingElementKind.Matrix
            || kind == IccProcessingElementKind.Clut || kind == IccProcessingElementKind.BeginAcs
            || kind == IccProcessingElementKind.EndAcs;
    }

    internal static string VersionDescription(IccVersionKind version)
    {
        if (version == IccVersionKind.Version4)
        {
            return "4";
        }

        if (version == IccVersionKind.Version5)
        {
            return "5";
        }

        return $"kind {(int)version}";
    }

    internal static void ValidateNestedArrayCodecs(IccTagArray array, string parentDescription, System.Collections.Generic.Dictionary<IccTagData, bool> active)
    {
        int index = 0;
        while (index < array.Values.Length)
        {
            IccTagData data = array.Values[index];
            if (data != null)
            {
                ValidateNestedCodec(data, $"{parentDescription} tag-array element {index}", active);
            }

            index = index + 1;
        }
    }

    internal static void ValidateNestedStructureCodecs(IccTagStructure structure, string parentDescription, System.Collections.Generic.Dictionary<IccTagData, bool> active)
    {
        int index = 0;
        while (index < structure.Members.Length)
        {
            IccStructureMember member = structure.Members[index];
            if (member != null && member.Data != null)
            {
                ValidateNestedCodec(member.Data, $"{parentDescription} tag-structure member {index} (signature 0x{member.Signature:X8})", active);
            }

            index = index + 1;
        }
    }

    internal static void ValidateNestedCodec(IccTagData data, string description, System.Collections.Generic.Dictionary<IccTagData, bool> active)
    {
        if (active.ContainsKey(data))
        {
            return;
        }

        if (!HasNestedCodec(data.Kind) && !HasCompleteRetainedPayload(data))
        {
            throw new IccProfileWriteException($"{description} data kind {data.Kind} has no writable nested codec and no complete retained raw payload.");
        }

        active.Add(data, true);
        if (data.Kind == IccTagDataKind.TagArray && data.TagArray != null && data.TagArray.Values != null)
        {
            ValidateNestedArrayCodecs(data.TagArray, description, active);
        }
        else if (data.Kind == IccTagDataKind.TagStructure && data.TagStructure != null && data.TagStructure.Members != null)
        {
            ValidateNestedStructureCodecs(data.TagStructure, description, active);
        }

        active.Remove(data);
    }

    internal static string FindOversizedChildDescription(IccTag tag, byte[] payload, int maximumBytes)
    {
        if (tag.Data.Kind == IccTagDataKind.MultiProcessElements)
        {
            return FindOversizedTableItem(payload, 12, 16, 8, 4, maximumBytes, "processing element");
        }

        if (tag.Data.Kind == IccTagDataKind.TagArray)
        {
            return FindOversizedTableItem(payload, 12, 16, 8, 4, maximumBytes, "tag-array element");
        }

        if (tag.Data.Kind == IccTagDataKind.TagStructure)
        {
            return FindOversizedTableItem(payload, 12, 16, 12, 8, maximumBytes, "tag-structure member");
        }

        return string.Empty;
    }

    internal static string FindOversizedTableItem(byte[] payload, int countOffset, int tableOffset, int recordSize, int sizeOffset, int maximumBytes, string description)
    {
        if (payload.Length < tableOffset || countOffset > payload.Length - 4)
        {
            return string.Empty;
        }

        uint encodedCount = ReadUInt32(payload, countOffset);
        if (encodedCount > int.MaxValue || encodedCount > (uint)((payload.Length - tableOffset) / recordSize))
        {
            return string.Empty;
        }

        int count = (int)encodedCount;
        int index = 0;
        while (index < count)
        {
            uint encodedSize = ReadUInt32(payload, tableOffset + (index * recordSize) + sizeOffset);
            if (encodedSize > (uint)maximumBytes)
            {
                return $"{description} {index}, which encodes {encodedSize} bytes and alone exceeds the per-tag limit";
            }

            index = index + 1;
        }

        return string.Empty;
    }

    internal static uint ReadUInt32(byte[] bytes, int offset)
    {
        return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    internal static byte[] SerializeTag(IccTag tag, IccProfileHeader header, IccWriteOptions options)
    {
        if (tag.Data.Kind == IccTagDataKind.Unknown && tag.DecodeState != null)
        {
            if (!options.PreserveUnknownData)
            {
                throw new IccProfileWriteException($"Unknown tag 0x{tag.Signature:X8} cannot be written when unknown-data preservation is disabled.");
            }

            return CopyLazyPayload(tag.DecodeState);
        }

        if (tag.Data.Kind == IccTagDataKind.Raw && !options.PreserveUnknownData)
        {
            throw new IccProfileWriteException($"Raw tag 0x{tag.Signature:X8} cannot be written when unknown-data preservation is disabled.");
        }

        byte[] encoded = SerializeTypedTag(tag, header, options);
        if (options.Mode == IccWriteMode.Preservation && CanReuseOriginalEncoding(tag, header, options, encoded))
        {
            return ProfileWriterCore.CloneBytes(tag.Data.RawData.Bytes);
        }

        return encoded;
    }

    internal static byte[] SerializeTypedTag(IccTag tag, IccProfileHeader header, IccWriteOptions options)
    {
        switch (tag.Data.Kind)
        {
            case IccTagDataKind.MultiProcessElements:
                if (tag.Data.MultiProcessElements == null)
                {
                    throw new IccProfileWriteException("Multi-process-elements tag data cannot be null.");
                }

                return MultiProcessElementsTagCodec.SerializeMultiProcessElementsForVersion(tag.Data.MultiProcessElements, header.Version.Kind);
            case IccTagDataKind.Chromaticity:
                if (tag.Data.Chromaticity == null)
                {
                    throw new IccProfileWriteException("Chromaticity tag data cannot be null.");
                }

                return ChromaticityTagCodec.SerializeChromaticity(tag.Data.Chromaticity, header.Version.Kind);
            case IccTagDataKind.Cicp:
                if (tag.Data.Cicp == null)
                {
                    throw new IccProfileWriteException("CICP tag data cannot be null.");
                }

                return CicpTagCodec.SerializeCicp(tag.Data.Cicp, header.ProfileClass, header.DataColorSpace);
            case IccTagDataKind.NamedColors:
                return SerializeNamedColors(tag, header);
            case IccTagDataKind.Colorants:
                if (tag.Data.Colorants == null)
                {
                    throw new IccProfileWriteException("Colorant-table tag data cannot be null.");
                }

                return ColorantTableTagCodec.SerializeColorantTable(tag.Data.Colorants, header.Version.Kind, header.ProfileConnectionSpace, tag.Signature == IccTagSignatures.ColorantTableOut);
            case IccTagDataKind.ResponseCurves:
                return ResponseCurveTagCodec.SerializeResponseCurveSet16(tag.Data.ResponseCurves, header.DataColorSpaceSignature);
            case IccTagDataKind.ProfileSequence:
                return SerializeProfileSequence(tag, header.Version.Kind, options.Mode);
            case IccTagDataKind.AdaptiveGainCurve:
                if (tag.Data.AdaptiveGainCurve == null)
                {
                    throw new IccProfileWriteException("Adaptive-gain-curve tag data cannot be null.");
                }

                return AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(tag.Data.AdaptiveGainCurve, header.ProfileClass, header.DataColorSpace, header.IsEmbedded, header.RequiresEmbeddedData);
            case IccTagDataKind.Text:
                if (tag.Data.Text == null)
                {
                    throw new IccProfileWriteException("Text tag data cannot be null.");
                }

                uint textTypeSignature =
                    NestedTagDataSerializer.ResolveTypeSignature(tag.Data);
                if (textTypeSignature == IccTypeSignatures.Data
                    && (header.Version.Kind == IccVersionKind.Version2
                        || header.Version.Kind == IccVersionKind.Version4))
                {
                    return NestedTagDataSerializer.SerializeAsciiData(
                        tag.Data.Text.Text);
                }

                break;
            case IccTagDataKind.GamutBoundary:
                if (tag.Data.GamutBoundary == null)
                {
                    throw new IccProfileWriteException("Gamut-boundary tag data cannot be null.");
                }

                return GamutBoundaryTagCodec.SerializeGamutBoundary(tag.Data.GamutBoundary);
            case IccTagDataKind.EmbeddedImage:
                if (tag.Data.EmbeddedImage == null)
                {
                    throw new IccProfileWriteException("Embedded-image tag data cannot be null.");
                }

                return EmbeddedImageTagCodec.SerializeEmbeddedImageWithLimit(tag.Data.EmbeddedImage, ResolveTypeSignature(tag), options.ResourceLimits.MaximumDecompressedBytes);
            case IccTagDataKind.EmbeddedProfile:
                if (tag.Data.EmbeddedProfile == null)
                {
                    throw new IccProfileWriteException("Embedded-profile tag data cannot be null.");
                }

                return EmbeddedProfileTagCodec.SerializeEmbeddedProfileForContext(
                    tag.Data.EmbeddedProfile, header.Version.Kind,
                    header.ProfileClassSignature, header.DataColorSpaceSignature,
                    options.ResourceLimits.MaximumProfileBytes);
            case IccTagDataKind.TagArray:
                if (tag.Data.TagArray == null)
                {
                    throw new IccProfileWriteException("Tag-array data cannot be null.");
                }

                return HierarchicalTagCodec.SerializeTagArrayForProfile(tag.Data.TagArray, options.ResourceLimits.MaximumNestingDepth, options.Mode, options.PreserveUnknownData);
            case IccTagDataKind.TagStructure:
                if (tag.Data.TagStructure == null)
                {
                    throw new IccProfileWriteException("Tag-structure data cannot be null.");
                }

                return HierarchicalTagCodec.SerializeTagStructureForProfile(tag.Data.TagStructure, options.ResourceLimits.MaximumNestingDepth, options.Mode, options.PreserveUnknownData);
            case IccTagDataKind.CrdInfo:
                if (tag.Data.CrdInfo == null)
                {
                    throw new IccProfileWriteException("CRD-information tag data cannot be null.");
                }

                return LegacyStructuredTagCodec.SerializeCrdInfo(tag.Data.CrdInfo);
            case IccTagDataKind.Screening:
                if (tag.Data.Screening == null)
                {
                    throw new IccProfileWriteException("Screening tag data cannot be null.");
                }

                return LegacyStructuredTagCodec.SerializeScreening(tag.Data.Screening);
            case IccTagDataKind.UcrBg:
                if (tag.Data.UcrBg == null)
                {
                    throw new IccProfileWriteException("UCR/BG tag data cannot be null.");
                }

                return LegacyStructuredTagCodec.SerializeUcrBg(tag.Data.UcrBg);
            default:
                break;
        }

        HierarchicalWriteContext context = new HierarchicalWriteContext(options.ResourceLimits.MaximumNestingDepth);
        context.Mode = options.Mode;
        context.PreserveUnknownData = options.PreserveUnknownData;
        return NestedTagDataSerializer.Serialize(tag.Data, 0, context);
    }

    internal static bool CanReuseOriginalEncoding(IccTag tag, IccProfileHeader header, IccWriteOptions options, byte[] currentEncoding)
    {
        if (!tag.IsDecodeAttempted || tag.DecodeResult == null || tag.DecodeResult.Status != IccOperationStatus.Success
            || tag.Data.RawData == null || tag.Data.RawData.Bytes == null || tag.Data.RawData.Bytes.Length < 8)
        {
            return false;
        }

        byte[] originalBytes = tag.Data.RawData.Bytes;
        TagDecodeState state = CreatePreservationDecodeState(tag, header, options, originalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        if (state.Result.Status != IccOperationStatus.Success)
        {
            return false;
        }

        IccTag originalTag = new IccTag();
        originalTag.Signature = tag.Signature;
        originalTag.TableEntry.Signature = tag.Signature;
        originalTag.Data = state.Data;
        byte[] originalCanonical = SerializeTypedTag(originalTag, header, options);
        return ProfileWriterCore.PayloadsEqual(originalCanonical, currentEncoding);
    }

    internal static TagDecodeState CreatePreservationDecodeState(IccTag tag, IccProfileHeader header, IccWriteOptions options, byte[] originalBytes)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(originalBytes), 0, originalBytes.Length, 0, header.Version.Kind, true);
        IccResourceLimits limits = options.ResourceLimits;
        state.MaximumDecompressedBytes = limits.MaximumDecompressedBytes;
        state.MaximumProfileBytes = limits.MaximumProfileBytes;
        state.MaximumTagCount = limits.MaximumTagCount;
        state.MaximumTagBytes = limits.MaximumTagBytes;
        state.MaximumTotalTagBytes = limits.MaximumTotalTagBytes;
        state.MaximumDiagnosticCount = limits.MaximumDiagnosticCount;
        state.MaximumNestingDepth = limits.MaximumNestingDepth;
        state.MaximumCalculatorInstructions = limits.MaximumCalculatorInstructions;
        state.MaximumCalculatorStackDepth = limits.MaximumCalculatorStackDepth;
        state.MaximumCalculatorStorageValues = limits.MaximumCalculatorStorageValues;
        state.HasProfileContext = true;
        state.TagSignature = tag.Signature;
        state.ProfileClass = header.ProfileClass;
        state.ProfileClassSignature = header.ProfileClassSignature;
        state.DataColorSpace = header.DataColorSpace;
        state.DataColorSpaceSignature = header.DataColorSpaceSignature;
        state.ProfileConnectionSpace = header.ProfileConnectionSpace;
        state.ProfileConnectionSpaceSignature = header.ProfileConnectionSpaceSignature;
        state.SpectralPcsSignature = header.SpectralPcsSignature;
        state.MaterialConnectionSpace = header.MaterialConnectionSpace;
        state.MaterialConnectionSpaceSignature = header.MaterialConnectionSpaceSignature;
        state.IsEmbedded = header.IsEmbedded;
        state.RequiresEmbeddedData = header.RequiresEmbeddedData;
        if (tag.DecodeState != null)
        {
            state.HasMatrixPrimaries = tag.DecodeState.HasMatrixPrimaries;
            state.CurrentNestingDepth = tag.DecodeState.CurrentNestingDepth;
        }

        return state;
    }

    internal static byte[] SerializeNamedColors(IccTag tag, IccProfileHeader header)
    {
        if (tag.Data.NamedColors == null)
        {
            throw new IccProfileWriteException("Named-colour tag data cannot be null.");
        }

        uint typeSignature = ResolveTypeSignature(tag);
        if (typeSignature == IccTypeSignatures.NamedColor)
        {
            return NamedColorTagCodec.SerializeNamedColor(tag.Data.NamedColors, header.DataColorSpaceSignature);
        }

        if (typeSignature == IccTypeSignatures.NamedColor2)
        {
            return NamedColorTagCodec.SerializeNamedColor2(tag.Data.NamedColors, header.Version.Kind, header.ProfileConnectionSpace, header.DataColorSpaceSignature);
        }

        throw new IccProfileWriteException("Named-colour data requires a namedColorType or namedColor2Type signature.");
    }

    internal static byte[] SerializeProfileSequence(IccTag tag, IccVersionKind version, IccWriteMode mode)
    {
        if (tag.Signature == IccTagSignatures.ProfileSequenceIdentifier)
        {
            return ProfileSequenceTagCodec.SerializeProfileSequenceIdentifierForMode(tag.Data.ProfileSequenceIdentifiers, mode);
        }

        return ProfileSequenceTagCodec.SerializeProfileSequenceDescriptionForMode(tag.Data.ProfileSequence, version, mode);
    }

    internal static uint ResolveTypeSignature(IccTag tag)
    {
        if (tag.Data.RawData != null && tag.Data.RawData.HasTypeSignature)
        {
            return tag.Data.RawData.TypeSignature;
        }

        if (tag.Signature == IccTagSignatures.NamedColor)
        {
            return IccTypeSignatures.NamedColor;
        }

        if (tag.Signature == IccTagSignatures.NamedColor2)
        {
            return IccTypeSignatures.NamedColor2;
        }

        throw new IccProfileWriteException($"Profile tag 0x{tag.Signature:X8} requires an explicit retained tag type signature.");
    }

    internal static byte[] CopyLazyPayload(TagDecodeState state)
    {
        if (state.PayloadOffset < 0 || state.PayloadSize < 8 || state.PayloadOffset > state.Source.ProfileBytes.Length - state.PayloadSize)
        {
            throw new IccProfileWriteException("A lazy profile tag does not retain a complete bounded payload.");
        }

        return state.Source.ProfileBytes.Slice(state.PayloadOffset, state.PayloadSize).ToArray();
    }
}
