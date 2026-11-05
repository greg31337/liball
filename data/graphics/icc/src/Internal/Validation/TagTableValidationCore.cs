namespace icc;

internal class ValidatedTagRegion
{
    internal long RelativeOffset;
    internal long End;
    internal long AlignedEnd;
    internal int FirstEntryIndex;
    internal int SharedDataId;
    internal int ReferenceCount;
}

internal static class TagTableValidationCore
{
    internal static IccValidationIssue[] Validate(IccProfile profile)
    {
        System.Collections.Generic.List<IccValidationIssue> issues = new System.Collections.Generic.List<IccValidationIssue>();
        long diagnosticBase = GetDiagnosticBase(profile);
        long logicalBase = diagnosticBase < 0 ? 0 : diagnosticBase;
        if (profile.TagTableEntries == null)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagTableCount, IccValidationSeverity.Fatal,
                "The profile tag-table entry collection is missing.", "TagTableEntries",
                diagnosticBase, IccConstants.HeaderSize, 4,
                "Provide the complete ordered tag-table entry collection."));
            ValidateTrailingBytes(profile, diagnosticBase, issues);
            return issues.ToArray();
        }

        int entryCount = profile.TagTableEntries.Length;
        long tableEnd = IccConstants.MinimumProfileSize + ((long)entryCount * IccConstants.TagTableEntrySize);
        int declaredSize = profile.Header == null ? 0 : profile.Header.DeclaredSize;
        ValidateCollections(profile, entryCount, diagnosticBase, issues);
        ValidateEncodedCount(profile, entryCount, tableEnd, diagnosticBase, issues);
        if (declaredSize < IccConstants.MinimumProfileSize || tableEnd > declaredSize)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagTableBounds, IccValidationSeverity.Error,
                $"The {entryCount}-entry tag table ends at profile offset {tableEnd}, outside the declared profile size {declaredSize}.",
                "TagTableEntries", diagnosticBase, IccConstants.HeaderSize, 4,
                "Reduce the tag count or increase the declared size to contain the complete table."));
        }

        ValidatedTagRegion[] regions = new ValidatedTagRegion[entryCount];
        int regionCount = 0;
        int index = 0;
        while (index < entryCount)
        {
            IccTagTableEntry entry = profile.TagTableEntries[index];
            if (entry == null)
            {
                issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableBounds, IccValidationSeverity.Fatal,
                    "The tag-table entry is missing.", index, 0, diagnosticBase,
                    "Replace the null table entry with a complete entry or remove it."));
            }
            else
            {
                ValidateDuplicateSignature(profile.TagTableEntries, entry, index, diagnosticBase, issues);
                ValidateTagLink(profile, entry, index, diagnosticBase, issues);
                long relativeOffset = entry.Offset - logicalBase;
                if (entry.Offset < logicalBase || relativeOffset > declaredSize)
                {
                    issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableBounds, IccValidationSeverity.Error,
                        $"Tag-table entry {index} has a payload offset outside the declared profile.",
                        index, 4, diagnosticBase,
                        "Use a payload offset within the declared profile."));
                }
                else if (entry.Size < 0 || entry.Size > declaredSize - relativeOffset)
                {
                    issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableBounds, IccValidationSeverity.Error,
                        $"Tag-table entry {index} has a payload size extending outside the declared profile.",
                        index, 8, diagnosticBase,
                        "Use a non-negative payload size whose checked range remains inside the profile."));
                }
                else
                {
                    if ((relativeOffset & (IccConstants.TagPayloadAlignment - 1)) != 0)
                    {
                        issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableAlignment, IccValidationSeverity.Error,
                            $"Tag-table entry {index} has unaligned payload offset {relativeOffset}.",
                            index, 4, diagnosticBase,
                            "Move the payload to a four-byte-aligned profile offset."));
                    }
                    if (relativeOffset < tableEnd)
                    {
                        issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableBounds, IccValidationSeverity.Error,
                            $"Tag-table entry {index} starts at profile offset {relativeOffset}, inside the tag table ending at {tableEnd}.",
                            index, 4, diagnosticBase,
                            "Move the payload after the complete tag table."));
                    }
                    if (entry.Size < 8)
                    {
                        issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                            $"The payload referenced by tag-table entry {index} is too short for a tag type header.",
                            index, relativeOffset, entry.Size, diagnosticBase,
                            "Provide at least the type signature and reserved field."));
                    }

                    long end = relativeOffset + entry.Size;
                    long alignedEnd = AlignToFour(end);
                    if (alignedEnd > declaredSize)
                    {
                        issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableBounds, IccValidationSeverity.Error,
                            $"Tag-table entry {index} requires alignment padding beyond the declared profile.",
                            index, 4, diagnosticBase,
                            "Include all required alignment padding within the declared profile size."));
                    }
                    else
                    {
                        int regionIndex = FindRegion(regions, regionCount, relativeOffset, end);
                        if (regionIndex >= 0)
                        {
                            ValidateSharedReference(regions[regionIndex], entry, index, diagnosticBase, issues);
                        }
                        else
                        {
                            ValidateSharedIdentity(regions, regionCount, entry, index, diagnosticBase, issues);
                            ValidatedTagRegion region = new ValidatedTagRegion();
                            region.RelativeOffset = relativeOffset;
                            region.End = end;
                            region.AlignedEnd = alignedEnd;
                            region.FirstEntryIndex = index;
                            region.SharedDataId = entry.SharedDataId;
                            region.ReferenceCount = 1;
                            regions[regionCount] = region;
                            regionCount = regionCount + 1;
                        }
                        ValidatePadding(profile, index, end, alignedEnd, diagnosticBase, issues);
                    }
                }
            }
            index = index + 1;
        }

        ValidateUniqueSharedIds(regions, regionCount, diagnosticBase, issues);
        SortRegions(regions, regionCount);
        ValidateTopology(regions, regionCount, tableEnd, declaredSize, diagnosticBase, issues);
        ValidateTrailingBytes(profile, diagnosticBase, issues);
        return issues.ToArray();
    }

    internal static void ValidateCollections(IccProfile profile, int entryCount, long diagnosticBase,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (profile.Tags == null)
        {
            return;
        }
        if (profile.Tags.Length != entryCount)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagTableCount, IccValidationSeverity.Error,
                $"The profile contains {entryCount} tag-table entries but {profile.Tags.Length} tag models.",
                "TagTableEntries", diagnosticBase, IccConstants.HeaderSize, 4,
                "Keep the tag and tag-table entry collections in one-to-one correspondence."));
        }
    }

    internal static void ValidateEncodedCount(IccProfile profile, int entryCount, long tableEnd,
        long diagnosticBase, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (profile.OriginalBytes == null || profile.OriginalBytes.Length == 0)
        {
            return;
        }
        if (profile.OriginalBytes.Length < IccConstants.MinimumProfileSize)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagTableBounds, IccValidationSeverity.Fatal,
                "The retained profile bytes do not contain the complete tag count.", "TagTableEntries",
                diagnosticBase, IccConstants.HeaderSize,
                profile.OriginalBytes.Length > IccConstants.HeaderSize
                    ? profile.OriginalBytes.Length - IccConstants.HeaderSize : 0,
                "Provide at least the complete 128-byte header and four-byte tag count."));
            return;
        }
        uint encodedCount = ProfileIdCore.ReadUInt32(profile.OriginalBytes, IccConstants.HeaderSize);
        if (encodedCount != (uint)entryCount)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagTableCount, IccValidationSeverity.Error,
                $"The encoded tag count {encodedCount} does not match the {entryCount} decoded table entries.",
                "TagTableEntries", diagnosticBase, IccConstants.HeaderSize, 4,
                "Make the encoded tag count agree with the decoded tag-table entry collection."));
        }
        if (tableEnd > profile.OriginalBytes.Length)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagTableBounds, IccValidationSeverity.Fatal,
                "The complete decoded tag table does not fit in the retained profile bytes.",
                "TagTableEntries", diagnosticBase, IccConstants.HeaderSize, 4,
                "Provide retained bytes containing the complete tag table."));
        }
    }

    internal static void ValidateDuplicateSignature(IccTagTableEntry[] entries, IccTagTableEntry entry,
        int index, long diagnosticBase, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int previous = 0;
        while (previous < index)
        {
            if (entries[previous] != null && entries[previous].Signature == entry.Signature)
            {
                issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableDuplicateSignature,
                    IccValidationSeverity.Error,
                    $"Tag-table entries {previous} and {index} contain duplicate signature 0x{entry.Signature:X8}.",
                    index, 0, diagnosticBase,
                    "Assign every tag-table entry a unique tag signature."));
                return;
            }
            previous = previous + 1;
        }
    }

    internal static void ValidateTagLink(IccProfile profile, IccTagTableEntry entry, int index,
        long diagnosticBase, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (profile.Tags == null || index >= profile.Tags.Length)
        {
            return;
        }
        IccTag tag = profile.Tags[index];
        if (tag == null)
        {
            return;
        }
        if (tag.TableEntry == null || tag.TableEntry != entry || tag.Signature != entry.Signature)
        {
            issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableBounds, IccValidationSeverity.Error,
                $"Tag model {index} is not linked consistently to its tag-table entry.", index, 0,
                diagnosticBase,
                "Use the corresponding table entry and the same signature for each tag model."));
        }
    }

    internal static int FindRegion(ValidatedTagRegion[] regions, int regionCount,
        long relativeOffset, long end)
    {
        int index = 0;
        while (index < regionCount)
        {
            if (regions[index].RelativeOffset == relativeOffset && regions[index].End == end)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static void ValidateSharedReference(ValidatedTagRegion region, IccTagTableEntry entry,
        int index, long diagnosticBase, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        region.ReferenceCount = region.ReferenceCount + 1;
        if (region.SharedDataId < 0 || entry.SharedDataId < 0
            || region.SharedDataId != entry.SharedDataId)
        {
            issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableOverlap, IccValidationSeverity.Error,
                $"Tag-table entry {index} references an exactly shared payload without a consistent shared-data identity.",
                index, 4, diagnosticBase,
                "Assign the same non-negative shared-data identity to every exact offset-and-size alias."));
        }
    }

    internal static void ValidateSharedIdentity(ValidatedTagRegion[] regions, int regionCount,
        IccTagTableEntry entry, int index, long diagnosticBase,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (entry.SharedDataId < 0)
        {
            return;
        }
        int regionIndex = 0;
        while (regionIndex < regionCount)
        {
            if (regions[regionIndex].SharedDataId == entry.SharedDataId)
            {
                issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableOverlap, IccValidationSeverity.Error,
                    $"Tag-table entry {index} reuses shared-data identity {entry.SharedDataId} for a different payload range.",
                    index, 4, diagnosticBase,
                    "Use each shared-data identity for exactly one offset-and-size pair."));
                return;
            }
            regionIndex = regionIndex + 1;
        }
    }

    internal static void ValidateUniqueSharedIds(ValidatedTagRegion[] regions, int regionCount,
        long diagnosticBase, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int index = 0;
        while (index < regionCount)
        {
            if (regions[index].ReferenceCount == 1 && regions[index].SharedDataId >= 0)
            {
                issues.Add(CreateEntryIssue(IccValidationRuleIds.TagTableOverlap, IccValidationSeverity.Error,
                    $"Tag-table entry {regions[index].FirstEntryIndex} assigns a shared-data identity to an unshared payload.",
                    regions[index].FirstEntryIndex, 4, diagnosticBase,
                    "Use -1 for an unshared payload or add the matching exact alias."));
            }
            index = index + 1;
        }
    }

    internal static void ValidatePadding(IccProfile profile, int index, long end, long alignedEnd,
        long diagnosticBase, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (profile.Tags == null || index >= profile.Tags.Length || profile.Tags[index] == null)
        {
            return;
        }
        IccTag tag = profile.Tags[index];
        int expectedLength = (int)(alignedEnd - end);
        if (tag.PaddingBytes == null || tag.PaddingBytes.Length != expectedLength)
        {
            int reportedLength = tag.PaddingBytes == null
                ? expectedLength : System.Math.Max(expectedLength, tag.PaddingBytes.Length);
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagTablePadding, IccValidationSeverity.Error,
                $"Tag {index} retains {(tag.PaddingBytes == null ? -1 : tag.PaddingBytes.Length)} padding bytes, but {expectedLength} are required.",
                index, end, reportedLength, diagnosticBase,
                "Retain exactly zero through three bytes needed to reach the next four-byte boundary."));
        }
        else if (TagTableCodec.ContainsNonZeroPadding(tag.PaddingBytes))
        {
            issues.Add(CreateIssue(IccValidationRuleIds.TagTablePadding, IccValidationSeverity.Warning,
                $"The alignment padding after tag {index} contains non-zero data.",
                $"Tags[{index}].PaddingBytes", diagnosticBase, end, expectedLength,
                "Set every tag-alignment padding byte to zero."));
        }
    }

    internal static void SortRegions(ValidatedTagRegion[] regions, int regionCount)
    {
        int index = 1;
        while (index < regionCount)
        {
            ValidatedTagRegion current = regions[index];
            int destination = index;
            while (destination > 0
                && regions[destination - 1].RelativeOffset > current.RelativeOffset)
            {
                regions[destination] = regions[destination - 1];
                destination = destination - 1;
            }
            regions[destination] = current;
            index = index + 1;
        }
    }

    internal static void ValidateTopology(ValidatedTagRegion[] regions, int regionCount,
        long tableEnd, int declaredSize, long diagnosticBase,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        long expectedOffset = tableEnd;
        int index = 0;
        while (index < regionCount)
        {
            ValidatedTagRegion region = regions[index];
            if (region.RelativeOffset > expectedOffset)
            {
                long gapLength = region.RelativeOffset - expectedOffset;
                issues.Add(CreateIssue(IccValidationRuleIds.TagTableGap, IccValidationSeverity.Error,
                    $"The profile contains an unreferenced {gapLength}-byte gap before tag-table entry {region.FirstEntryIndex}.",
                    $"TagTableEntries[{region.FirstEntryIndex}].Offset", diagnosticBase,
                    expectedOffset, gapLength,
                    "Remove unreferenced bytes so tag data forms a contiguous sequence."));
            }
            if (region.RelativeOffset < expectedOffset)
            {
                long overlapLength = expectedOffset - region.RelativeOffset;
                long regionLength = region.AlignedEnd - region.RelativeOffset;
                if (overlapLength > regionLength)
                {
                    overlapLength = regionLength;
                }
                issues.Add(CreateIssue(IccValidationRuleIds.TagTableOverlap, IccValidationSeverity.Error,
                    $"The payload referenced by tag-table entry {region.FirstEntryIndex} partially overlaps another payload.",
                    $"TagTableEntries[{region.FirstEntryIndex}]", diagnosticBase,
                    region.RelativeOffset, overlapLength,
                    "Use an exact shared offset-and-size pair or move the payload into a disjoint region."));
            }
            if (region.AlignedEnd > expectedOffset)
            {
                expectedOffset = region.AlignedEnd;
            }
            index = index + 1;
        }
        if (expectedOffset < declaredSize)
        {
            long trailingLength = declaredSize - expectedOffset;
            issues.Add(CreateIssue(IccValidationRuleIds.ProfileExactEnd, IccValidationSeverity.Error,
                $"The declared profile contains {trailingLength} unreferenced bytes after the final tag payload.",
                "Tags", diagnosticBase, expectedOffset, trailingLength,
                "End the declared profile immediately after the final aligned tag payload."));
        }
    }

    internal static void ValidateTrailingBytes(IccProfile profile, long diagnosticBase,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (profile.TrailingBytes == null)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ProfileExactEnd, IccValidationSeverity.Fatal,
                "The retained trailing-byte collection is missing.", "Profile.TrailingBytes",
                diagnosticBase, profile.Header == null ? 0 : profile.Header.DeclaredSize, 0,
                "Provide a non-null trailing-byte collection."));
        }
        else if (profile.TrailingBytes.Length != 0)
        {
            issues.Add(CreateIssue(IccValidationRuleIds.ProfileExactEnd, IccValidationSeverity.Error,
                $"The input contains {profile.TrailingBytes.Length} hidden bytes after the declared profile.",
                "Profile.TrailingBytes", diagnosticBase,
                profile.Header == null ? 0 : profile.Header.DeclaredSize,
                profile.TrailingBytes.Length,
                "Remove all bytes following the exact declared profile end."));
        }
    }

    internal static long AlignToFour(long value)
    {
        if (value > long.MaxValue - 3)
        {
            return long.MaxValue;
        }
        return (value + 3) & ~3L;
    }

    internal static long GetDiagnosticBase(IccProfile profile)
    {
        if (profile.Location != null && profile.Location.Offset >= 0)
        {
            return profile.Location.Offset;
        }
        return -1;
    }

    internal static IccValidationIssue CreateEntryIssue(string ruleId, IccValidationSeverity severity,
        string message, int index, int fieldOffset, long diagnosticBase, string remediation)
    {
        long relativeOffset = IccConstants.MinimumProfileSize
            + ((long)index * IccConstants.TagTableEntrySize) + fieldOffset;
        string field = fieldOffset == 0 ? ".Signature"
            : fieldOffset == 4 ? ".Offset" : ".Size";
        return CreateIssue(ruleId, severity, message,
            $"TagTableEntries[{index}]" + field,
            diagnosticBase, relativeOffset, 4, remediation);
    }

    internal static IccValidationIssue CreatePayloadIssue(string ruleId, IccValidationSeverity severity,
        string message, int index, long relativeOffset, long length, long diagnosticBase,
        string remediation)
    {
        return CreateIssue(ruleId, severity, message, $"Tags[{index}]", diagnosticBase,
            relativeOffset, length, remediation);
    }

    internal static IccValidationIssue CreateIssue(string ruleId, IccValidationSeverity severity,
        string message, string path, long diagnosticBase, long relativeOffset, long length,
        string remediation)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = ruleId;
        issue.Severity = severity;
        issue.Message = message;
        issue.Path = path;
        issue.Location.Offset = diagnosticBase < 0 || relativeOffset < 0
            || diagnosticBase > long.MaxValue - relativeOffset
            ? -1 : diagnosticBase + relativeOffset;
        issue.Location.Length = length < 0 ? 0 : length;
        issue.Remediation = remediation;
        return issue;
    }
}
