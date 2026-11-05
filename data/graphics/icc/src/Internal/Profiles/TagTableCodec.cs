namespace icc;

internal class TagPayloadRegion
{
    internal int RelativeOffset;
    internal int AlignedEnd;
    internal int FirstEntryIndex;
    internal int SharedDataId;
}

internal static class TagTableCodec
{
    internal static void ReadTable(System.ReadOnlyMemory<byte> source, int profileOffset, long absoluteProfileOffset, int declaredSize, IccReadOptions options, IccProfile profile, ReadDiagnosticCollector diagnostics, int currentNestingDepth)
    {
        ValidateTableLimits(options.ResourceLimits);
        int tableSourceOffset = profileOffset + IccConstants.HeaderSize;
        long absoluteTableOffset = CheckedSize.AddOffsets(absoluteProfileOffset, IccConstants.HeaderSize, long.MaxValue);
        BigEndianReader reader = new BigEndianReader(source.Slice(tableSourceOffset, declaredSize - IccConstants.HeaderSize), absoluteTableOffset);
        uint encodedCount = reader.ReadUInt32();
        int tagCount = ConvertTagCount(encodedCount, options.ResourceLimits.MaximumTagCount, absoluteTableOffset);
        int entryBytes = CheckedSize.MultiplyCount(tagCount, IccConstants.TagTableEntrySize, int.MaxValue);
        int tableSize = CheckedSize.AccumulateBytes(IccConstants.TagCountSize, entryBytes, int.MaxValue);
        int tableEnd = CheckedSize.AccumulateBytes(IccConstants.HeaderSize, tableSize, int.MaxValue);
        if (tableEnd > declaredSize)
        {
            throw new IccProfileFormatException($"The tag table at byte offset {absoluteTableOffset} requires {tableSize} bytes for {tagCount} entries, extending beyond the {declaredSize}-byte profile.");
        }

        IccTagTableEntry[] entries = new IccTagTableEntry[tagCount];
        IccTag[] tags = new IccTag[tagCount];
        TagPayloadRegion[] regions = new TagPayloadRegion[tagCount];
        System.Collections.Generic.Dictionary<uint, int> signatureIndices = new System.Collections.Generic.Dictionary<uint, int>();
        System.Collections.Generic.Dictionary<ulong, int> regionIndices = new System.Collections.Generic.Dictionary<ulong, int>();
        int regionCount = 0;
        int nextSharedDataId = 0;
        int totalPayloadBytes = 0;
        int index = 0;
        while (index < tagCount)
        {
            uint signature = reader.ReadUInt32();
            uint encodedOffset = reader.ReadUInt32();
            uint encodedSize = reader.ReadUInt32();
            if (signatureIndices.ContainsKey(signature))
            {
                int firstIndex = signatureIndices[signature];
                IccValidationIssue duplicateIssue = CreateDuplicateSignatureIssue(signature, firstIndex, index, absoluteProfileOffset);
                ParserPolicy.ReportIssue(options.Mode, diagnostics, duplicateIssue);
            }
            else
            {
                signatureIndices.Add(signature, index);
            }

            int relativeOffset = ConvertPayloadOffset(encodedOffset, declaredSize, index);
            int payloadSize = ConvertPayloadSize(encodedSize, options.ResourceLimits.MaximumTagBytes, index);
            if ((relativeOffset & (IccConstants.TagPayloadAlignment - 1)) != 0)
            {
                IccValidationIssue alignmentIssue = CreateAlignmentIssue(relativeOffset, index, absoluteProfileOffset);
                ParserPolicy.ReportIssue(options.Mode, diagnostics, alignmentIssue);
            }

            if (relativeOffset < tableEnd)
            {
                IccValidationIssue boundsIssue = CreateTableIntersectionIssue(relativeOffset, tableEnd, index, absoluteProfileOffset);
                ParserPolicy.ReportIssue(options.Mode, diagnostics, boundsIssue);
            }

            if (payloadSize > declaredSize - relativeOffset)
            {
                throw new IccProfileFormatException($"Tag-table entry {index} spans profile offsets {relativeOffset} through {((long)relativeOffset) + payloadSize}, outside the declared profile size {declaredSize}.");
            }

            int payloadEnd = relativeOffset + payloadSize;
            int alignedEnd = CheckedSize.AlignSize(payloadEnd, IccConstants.TagPayloadAlignment, declaredSize);
            if (payloadSize < 8)
            {
                IccValidationIssue sizeIssue = CreateTagSizeIssue(relativeOffset, payloadSize, index, absoluteProfileOffset);
                ParserPolicy.ReportIssue(options.Mode, diagnostics, sizeIssue);
            }

            IccTagTableEntry entry = new IccTagTableEntry();
            entry.Signature = signature;
            entry.Offset = CheckedSize.AddOffsets(absoluteProfileOffset, relativeOffset, long.MaxValue);
            entry.Size = payloadSize;
            entry.OriginalIndex = index;
            ulong regionKey = ((ulong)encodedOffset << 32) | encodedSize;
            bool isNewRegion = false;
            if (regionIndices.ContainsKey(regionKey))
            {
                int regionIndex = regionIndices[regionKey];
                TagPayloadRegion sharedRegion = regions[regionIndex];
                if (sharedRegion.SharedDataId < 0)
                {
                    sharedRegion.SharedDataId = nextSharedDataId;
                    entries[sharedRegion.FirstEntryIndex].SharedDataId = nextSharedDataId;
                    nextSharedDataId = nextSharedDataId + 1;
                }

                entry.SharedDataId = sharedRegion.SharedDataId;
            }
            else
            {
                isNewRegion = true;
                totalPayloadBytes = AccumulatePayloadBytes(totalPayloadBytes, payloadSize, options.ResourceLimits.MaximumTotalTagBytes, index);
                TagPayloadRegion region = new TagPayloadRegion();
                region.RelativeOffset = relativeOffset;
                region.AlignedEnd = alignedEnd;
                region.FirstEntryIndex = index;
                region.SharedDataId = -1;
                regions[regionCount] = region;
                regionIndices.Add(regionKey, regionCount);
                regionCount = regionCount + 1;
            }

            IccTag tag = new IccTag();
            tag.Signature = signature;
            tag.TableEntry = entry;
            int paddingCount = alignedEnd - payloadEnd;
            if (paddingCount != 0)
            {
                int paddingOffset = profileOffset + payloadEnd;
                tag.PaddingBytes = source.Slice(paddingOffset, paddingCount).ToArray();
                if (isNewRegion && ContainsNonZeroPadding(tag.PaddingBytes))
                {
                    IccValidationIssue paddingIssue = CreatePaddingIssue(payloadEnd, paddingCount, index, absoluteProfileOffset);
                    ParserPolicy.ReportIssue(options.Mode, diagnostics, paddingIssue);
                }
            }

            entries[index] = entry;
            tags[index] = tag;
            index = index + 1;
        }

        SortRegionsByOffset(regions, regionCount);
        ValidatePayloadTopology(regions, regionCount, tableEnd, declaredSize, absoluteProfileOffset, options.Mode, diagnostics);
        AttachDecodeStates(source, profileOffset, declaredSize, absoluteProfileOffset, profile.Header, options, entries, tags, nextSharedDataId, currentNestingDepth);
        profile.TagTableEntries = entries;
        profile.Tags = tags;
    }

    internal static void AttachDecodeStates(System.ReadOnlyMemory<byte> source, int profileOffset, int declaredSize, long absoluteProfileOffset, IccProfileHeader header, IccReadOptions options, IccTagTableEntry[] entries, IccTag[] tags, int sharedDataCount, int currentNestingDepth)
    {
        if (tags.Length == 0)
        {
            return;
        }

        System.ReadOnlyMemory<byte> profileBytes = source.Slice(profileOffset, declaredSize);
        if (options.CopyInputBytes)
        {
            profileBytes = profileBytes.ToArray();
        }

        TagDecodeSource decodeSource = new TagDecodeSource(profileBytes);
        TagDecodeState?[] sharedStates = new TagDecodeState?[sharedDataCount];
        bool hasMatrixPrimaries = HasMatrixPrimaries(entries);
        int index = 0;
        while (index < tags.Length)
        {
            IccTagTableEntry entry = entries[index];
            int relativeOffset = (int)(entry.Offset - absoluteProfileOffset);
            TagDecodeState? decodeState = null;
            if (entry.SharedDataId >= 0)
            {
                decodeState = sharedStates[entry.SharedDataId];
                if (decodeState != null && (UsesContextualColorantSemantics(entry.Signature)
                    || UsesContextualColorantSemantics(decodeState.TagSignature)))
                {
                    decodeState = null;
                }
            }

            if (decodeState == null)
            {
                decodeState = new TagDecodeState(decodeSource, relativeOffset, entry.Size, entry.Offset, header.Version.Kind, options.PreserveOriginalBytes);
                decodeState.MaximumDecompressedBytes = options.ResourceLimits.MaximumDecompressedBytes;
                decodeState.MaximumProfileBytes = options.ResourceLimits.MaximumProfileBytes;
                decodeState.MaximumTagCount = options.ResourceLimits.MaximumTagCount;
                decodeState.MaximumTagBytes = options.ResourceLimits.MaximumTagBytes;
                decodeState.MaximumTotalTagBytes = options.ResourceLimits.MaximumTotalTagBytes;
                decodeState.MaximumDiagnosticCount = options.ResourceLimits.MaximumDiagnosticCount;
                decodeState.MaximumNestingDepth = options.ResourceLimits.MaximumNestingDepth;
                decodeState.CurrentNestingDepth = currentNestingDepth;
                decodeState.MaximumCalculatorInstructions = options.ResourceLimits.MaximumCalculatorInstructions;
                decodeState.MaximumCalculatorStackDepth = options.ResourceLimits.MaximumCalculatorStackDepth;
                decodeState.MaximumCalculatorStorageValues = options.ResourceLimits.MaximumCalculatorStorageValues;
                decodeState.HasProfileContext = true;
                decodeState.TagSignature = entry.Signature;
                decodeState.ProfileClass = header.ProfileClass;
                decodeState.ProfileClassSignature = header.ProfileClassSignature;
                decodeState.DataColorSpace = header.DataColorSpace;
                decodeState.DataColorSpaceSignature = header.DataColorSpaceSignature;
                decodeState.ProfileConnectionSpace = header.ProfileConnectionSpace;
                decodeState.ProfileConnectionSpaceSignature = header.ProfileConnectionSpaceSignature;
                decodeState.SpectralPcsSignature = header.SpectralPcsSignature;
                decodeState.MaterialConnectionSpace = header.MaterialConnectionSpace;
                decodeState.MaterialConnectionSpaceSignature = header.MaterialConnectionSpaceSignature;
                decodeState.HasMatrixPrimaries = hasMatrixPrimaries;
                decodeState.IsEmbedded = header.IsEmbedded;
                decodeState.RequiresEmbeddedData = header.RequiresEmbeddedData;
                if (entry.SharedDataId >= 0 && sharedStates[entry.SharedDataId] == null)
                {
                    sharedStates[entry.SharedDataId] = decodeState;
                }
            }

            tags[index].DecodeState = decodeState;
            if (!options.DecodeTagsLazily)
            {
                TagDecoder.GetDecodedData(tags[index]);
                tags[index].DecodeState = null;
            }

            index = index + 1;
        }
    }

    internal static bool UsesContextualColorantSemantics(uint tagSignature)
    {
        Icc1TagDefinition? definition = Icc1TagRegistry.FindDefinition(tagSignature);
        bool hasIcc1Context = definition != null
            && (definition.AssociatedChannels != Icc1ChannelSource.None || definition.RequiredDataChannels != 0);
        Icc2TagDefinition? icc2Definition = Icc2TagRegistry.FindDefinition(tagSignature);
        bool hasIcc2Context = icc2Definition != null
            && (icc2Definition.InputChannels != Icc2ChannelSource.None
                || icc2Definition.OutputChannels != Icc2ChannelSource.None
                || icc2Definition.AssociatedChannels != Icc2ChannelSource.None);
        return hasIcc1Context
            || hasIcc2Context
            || tagSignature == IccTagSignatures.ColorantOrderOut
            || tagSignature == IccTagSignatures.MultiplexDefaultValues
            || tagSignature == IccTagSignatures.MultiplexTypeArray
            || tagSignature == IccTagSignatures.ReferenceName
            || tagSignature == IccTagSignatures.ColorSpaceName
            || GamutBoundaryTagCodec.IsGamutBoundaryTag(tagSignature)
            || tagSignature == IccTagSignatures.SurfaceMap
            || tagSignature == IccTagSignatures.VideoCardGammaTable
            || tagSignature == IccTagSignatures.MicrosoftHdrCalibration
            || VendorTagCodec.IsMicrosoftPrivateSignature(tagSignature)
            || MultiProcessElementsTagCodec.IsContextualTransformTag(tagSignature)
            || LegacyStructuredTagCodec.HasLegacyTagMapping(tagSignature);
    }

    internal static bool HasMatrixPrimaries(IccTagTableEntry[] entries)
    {
        bool hasRed = false;
        bool hasGreen = false;
        bool hasBlue = false;
        int index = 0;
        while (index < entries.Length)
        {
            uint signature = entries[index].Signature;
            if (signature == IccTagSignatures.RedMatrixColumn)
            {
                hasRed = true;
            }
            else if (signature == IccTagSignatures.GreenMatrixColumn)
            {
                hasGreen = true;
            }
            else if (signature == IccTagSignatures.BlueMatrixColumn)
            {
                hasBlue = true;
            }

            index = index + 1;
        }

        return hasRed && hasGreen && hasBlue;
    }

    internal static void ValidateTableLimits(IccResourceLimits limits)
    {
        if (limits.MaximumTagCount < 0 || limits.MaximumTagBytes < 0 || limits.MaximumTotalTagBytes < 0 || limits.MaximumDecompressedBytes < 0 || limits.MaximumNestingDepth < 0 || limits.MaximumCalculatorInstructions < 0 || limits.MaximumCalculatorStackDepth < 0 || limits.MaximumCalculatorStorageValues < 0)
        {
            throw new IccProfileValidationException("Tag count, payload, decompression, nesting, and calculator resource limits cannot be negative.");
        }
    }

    internal static int ConvertTagCount(uint encodedCount, int maximumTagCount, long absoluteTableOffset)
    {
        if (encodedCount > int.MaxValue)
        {
            throw new IccProfileFormatException($"The tag count at byte offset {absoluteTableOffset} is {encodedCount} and cannot be represented by the managed profile model.");
        }

        int tagCount = (int)encodedCount;
        if (tagCount > maximumTagCount)
        {
            throw new IccProfileFormatException($"The tag count {tagCount} at byte offset {absoluteTableOffset} exceeds the configured maximum of {maximumTagCount}.");
        }

        return tagCount;
    }

    internal static int ConvertPayloadOffset(uint encodedOffset, int declaredSize, int entryIndex)
    {
        if (encodedOffset > int.MaxValue || encodedOffset > declaredSize)
        {
            throw new IccProfileFormatException($"Tag-table entry {entryIndex} has payload offset {encodedOffset}, outside the declared profile size {declaredSize}.");
        }

        return (int)encodedOffset;
    }

    internal static int ConvertPayloadSize(uint encodedSize, int maximumTagBytes, int entryIndex)
    {
        if (encodedSize > int.MaxValue)
        {
            throw new IccProfileFormatException($"Tag-table entry {entryIndex} has payload size {encodedSize}, which cannot be represented by the managed profile model.");
        }

        int payloadSize = (int)encodedSize;
        if (payloadSize > maximumTagBytes)
        {
            throw new IccProfileFormatException($"Tag-table entry {entryIndex} has payload size {payloadSize}, exceeding the configured maximum of {maximumTagBytes} bytes.");
        }

        return payloadSize;
    }

    internal static int AccumulatePayloadBytes(int currentTotal, int payloadSize, int maximumTotal, int entryIndex)
    {
        if (payloadSize > int.MaxValue - currentTotal)
        {
            throw new IccProfileFormatException($"Accumulating the unique payload for tag-table entry {entryIndex} would overflow the managed profile model.");
        }

        int total = currentTotal + payloadSize;
        if (total > maximumTotal)
        {
            throw new IccProfileFormatException($"Unique tag payloads total {total} bytes at tag-table entry {entryIndex}, exceeding the configured maximum of {maximumTotal} bytes.");
        }

        return total;
    }

    internal static void SortRegionsByOffset(TagPayloadRegion[] regions, int regionCount)
    {
        int index = 1;
        while (index < regionCount)
        {
            TagPayloadRegion current = regions[index];
            int destination = index;
            while (destination > 0 && regions[destination - 1].RelativeOffset > current.RelativeOffset)
            {
                regions[destination] = regions[destination - 1];
                destination = destination - 1;
            }

            regions[destination] = current;
            index = index + 1;
        }
    }

    internal static void ValidatePayloadTopology(TagPayloadRegion[] regions, int regionCount, int tableEnd, int declaredSize, long absoluteProfileOffset, IccReadMode readMode, ReadDiagnosticCollector diagnostics)
    {
        int expectedOffset = tableEnd;
        int index = 0;
        while (index < regionCount)
        {
            TagPayloadRegion region = regions[index];
            if (region.RelativeOffset > expectedOffset)
            {
                int gapSize = region.RelativeOffset - expectedOffset;
                long absoluteGapOffset = CheckedSize.AddOffsets(absoluteProfileOffset, expectedOffset, long.MaxValue);
                IccValidationIssue gapIssue = CreateGapIssue(absoluteGapOffset, gapSize, region.FirstEntryIndex, false);
                ParserPolicy.ReportIssue(readMode, diagnostics, gapIssue);
            }

            if (region.RelativeOffset < expectedOffset)
            {
                long absoluteOverlapOffset = CheckedSize.AddOffsets(absoluteProfileOffset, region.RelativeOffset, long.MaxValue);
                int overlapSize = expectedOffset - region.RelativeOffset;
                if (overlapSize > region.AlignedEnd - region.RelativeOffset)
                {
                    overlapSize = region.AlignedEnd - region.RelativeOffset;
                }

                IccValidationIssue overlapIssue = CreateOverlapIssue(absoluteOverlapOffset, overlapSize, region.FirstEntryIndex);
                ParserPolicy.ReportIssue(readMode, diagnostics, overlapIssue);
            }

            if (region.AlignedEnd > expectedOffset)
            {
                expectedOffset = region.AlignedEnd;
            }

            index = index + 1;
        }

        if (expectedOffset < declaredSize)
        {
            int gapSize = declaredSize - expectedOffset;
            long absoluteGapOffset = CheckedSize.AddOffsets(absoluteProfileOffset, expectedOffset, long.MaxValue);
            IccValidationIssue gapIssue = CreateGapIssue(absoluteGapOffset, gapSize, -1, true);
            ParserPolicy.ReportIssue(readMode, diagnostics, gapIssue);
        }
    }

    internal static bool ContainsNonZeroPadding(byte[] paddingBytes)
    {
        int index = 0;
        while (index < paddingBytes.Length)
        {
            if (paddingBytes[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static IccValidationIssue CreateDuplicateSignatureIssue(uint signature, int firstIndex, int duplicateIndex, long absoluteProfileOffset)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.TagTableDuplicateSignature;
        issue.Severity = IccValidationSeverity.Error;
        issue.Message = $"Tag-table entries {firstIndex} and {duplicateIndex} contain the duplicate signature 0x{signature:X8}.";
        issue.Path = $"TagTableEntries[{duplicateIndex}].Signature";
        issue.Location.Offset = GetTableEntryFieldOffset(absoluteProfileOffset, duplicateIndex, 0);
        issue.Location.Length = 4;
        issue.Remediation = "Assign every tag-table entry a unique tag signature.";
        return issue;
    }

    internal static IccValidationIssue CreateAlignmentIssue(int relativeOffset, int entryIndex, long absoluteProfileOffset)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.TagTableAlignment;
        issue.Severity = IccValidationSeverity.Error;
        issue.Message = $"Tag-table entry {entryIndex} has unaligned payload offset {relativeOffset}; tag payloads shall begin on a four-byte boundary.";
        issue.Path = $"TagTableEntries[{entryIndex}].Offset";
        issue.Location.Offset = GetTableEntryFieldOffset(absoluteProfileOffset, entryIndex, 4);
        issue.Location.Length = 4;
        issue.Remediation = "Move the payload to a four-byte-aligned profile offset and update its table entry.";
        return issue;
    }

    internal static IccValidationIssue CreateTableIntersectionIssue(int relativeOffset, int tableEnd, int entryIndex, long absoluteProfileOffset)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.TagTableBounds;
        issue.Severity = IccValidationSeverity.Error;
        issue.Message = $"Tag-table entry {entryIndex} starts at profile offset {relativeOffset}, inside the tag table ending at offset {tableEnd}.";
        issue.Path = $"TagTableEntries[{entryIndex}].Offset";
        issue.Location.Offset = GetTableEntryFieldOffset(absoluteProfileOffset, entryIndex, 4);
        issue.Location.Length = 4;
        issue.Remediation = "Move the payload after the complete tag table and update its table entry.";
        return issue;
    }

    internal static IccValidationIssue CreateTagSizeIssue(int relativeOffset, int payloadSize, int entryIndex, long absoluteProfileOffset)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.TagSize;
        issue.Severity = IccValidationSeverity.Error;
        issue.Message = $"The payload referenced by tag-table entry {entryIndex} is {payloadSize} bytes; a complete tag type header requires at least 8 bytes.";
        issue.Path = $"Tags[{entryIndex}]";
        issue.Location.Offset = CheckedSize.AddOffsets(absoluteProfileOffset, relativeOffset, long.MaxValue);
        issue.Location.Length = payloadSize;
        issue.Remediation = "Provide the complete type signature, reserved field, and type data within this tag boundary.";
        return issue;
    }

    internal static IccValidationIssue CreatePaddingIssue(int relativePaddingOffset, int paddingCount, int entryIndex, long absoluteProfileOffset)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.TagTablePadding;
        issue.Severity = IccValidationSeverity.Warning;
        issue.Message = $"The {paddingCount}-byte padding after the payload referenced by tag-table entry {entryIndex} contains non-zero data.";
        issue.Path = $"Tags[{entryIndex}].PaddingBytes";
        issue.Location.Offset = CheckedSize.AddOffsets(absoluteProfileOffset, relativePaddingOffset, long.MaxValue);
        issue.Location.Length = paddingCount;
        issue.Remediation = "Set tag-alignment padding bytes to zero.";
        return issue;
    }

    internal static IccValidationIssue CreateGapIssue(long absoluteGapOffset, int gapSize, int followingEntryIndex, bool isTrailing)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.TagTableGap;
        issue.Severity = IccValidationSeverity.Error;
        if (isTrailing)
        {
            issue.RuleId = IccValidationRuleIds.ProfileExactEnd;
            issue.Message = $"The profile contains a {gapSize}-byte gap at byte offset {absoluteGapOffset} after the final tag payload.";
            issue.Path = "Tags";
        }
        else
        {
            issue.Message = $"The profile contains a {gapSize}-byte gap at byte offset {absoluteGapOffset} before the payload referenced by tag-table entry {followingEntryIndex}.";
            issue.Path = $"TagTableEntries[{followingEntryIndex}].Offset";
        }

        issue.Location.Offset = absoluteGapOffset;
        issue.Location.Length = gapSize;
        issue.Remediation = "Remove unreferenced bytes so the tag data elements form one contiguous sequence.";
        return issue;
    }

    internal static IccValidationIssue CreateOverlapIssue(long absoluteOverlapOffset, int overlapSize, int entryIndex)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.TagTableOverlap;
        issue.Severity = IccValidationSeverity.Error;
        issue.Message = $"The payload referenced by tag-table entry {entryIndex} overlaps another tag payload at byte offset {absoluteOverlapOffset}.";
        issue.Path = $"TagTableEntries[{entryIndex}]";
        issue.Location.Offset = absoluteOverlapOffset;
        issue.Location.Length = overlapSize;
        issue.Remediation = "Use identical offset and size values for shared payloads, or move the payloads into disjoint contiguous regions.";
        return issue;
    }

    internal static long GetTableEntryFieldOffset(long absoluteProfileOffset, int entryIndex, int fieldOffset)
    {
        int entryBytes = CheckedSize.MultiplyCount(entryIndex, IccConstants.TagTableEntrySize, int.MaxValue);
        int relativeOffset = CheckedSize.AccumulateBytes(IccConstants.MinimumProfileSize, entryBytes, int.MaxValue);
        relativeOffset = CheckedSize.AccumulateBytes(relativeOffset, fieldOffset, int.MaxValue);
        return CheckedSize.AddOffsets(absoluteProfileOffset, relativeOffset, long.MaxValue);
    }
}
