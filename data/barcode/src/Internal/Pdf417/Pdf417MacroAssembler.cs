namespace barcode;

internal class Pdf417MacroAssemblyEntry
{
    internal BarcodeDecodeResult Segment = new BarcodeDecodeResult();
    internal StructuredAppendInfo Information = new StructuredAppendInfo();
}

internal class Pdf417MacroAssemblyState
{
    internal global::System.Collections.Generic.List<Pdf417MacroAssemblyEntry> Entries =
        new global::System.Collections.Generic.List<Pdf417MacroAssemblyEntry>();
    internal global::System.Collections.Generic.List<string> Conflicts =
        new global::System.Collections.Generic.List<string>();
    internal global::System.Collections.Generic.List<BarcodeValidationIssue> Issues =
        new global::System.Collections.Generic.List<BarcodeValidationIssue>();
    internal string? FileIdentifier;
    internal int DeclaredCount;
    internal int CountBearingSegmentCount;
    internal int LastIndex = -1;
    internal bool HasChecksum;
    internal int Checksum;
    internal bool FatalConflict;
}

internal static class Pdf417MacroAssembler
{
    internal const int CcittPolynomial = 0x1021;
    internal const int MaximumChecksum = 65535;

    internal static StructuredAppendResult AssembleMacroSegments(
        StructuredAppendRequest request)
    {
        ValidateMacroRequest(request);
        BarcodeDecodeResult[] sourceSegments = RequireMacroSegments(
            request.Segments);
        Pdf417MacroAssemblyState state = new Pdf417MacroAssemblyState();
        int sourceIndex = 0;
        while (sourceIndex < sourceSegments.Length)
        {
            BarcodeDecodeResult segment = sourceSegments[sourceIndex];
            StructuredAppendInfo information = ValidateMacroSegment(
                segment,
                sourceIndex);
            RecordMacroIdentity(state, information, sourceIndex);
            RecordMacroCount(state, information, sourceIndex);
            RecordMacroLastSegment(state, information, sourceIndex);
            RecordMacroChecksum(state, segment, sourceIndex);
            RecordMacroSegment(
                state,
                segment,
                information,
                request.AllowIdenticalDuplicates);
            sourceIndex = sourceIndex + 1;
        }

        if (state.DeclaredCount > 0
            && state.CountBearingSegmentCount != sourceSegments.Length)
        {
            AddMacroConflict(
                state,
                "MACRO_PDF417_SEGMENT_COUNT_MISSING",
                "The Macro PDF417 segment count field is not present in every supplied segment.",
                "Count",
                state.DeclaredCount.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                "missing",
                BarcodeValidationSeverity.Error,
                true);
        }

        int expectedCount = DetermineMacroExpectedCount(state);
        SortMacroEntries(state.Entries);
        int[] missing = FindMissingMacroIndices(
            state,
            expectedCount);
        BarcodeDecodeResult[] normalized = CopyNormalizedMacroSegments(
            state.Entries);
        BarcodePayload payload = AssembleMacroPayload(state.Entries);
        bool hasKnownCompletion = expectedCount > 0;
        bool structurallyComplete = hasKnownCompletion
            && missing.Length == 0
            && state.Entries.Count == expectedCount
            && !state.FatalConflict;
        bool checksumValid = VerifyMacroChecksum(
            state,
            payload,
            structurallyComplete);
        bool complete = structurallyComplete && checksumValid;

        if (!hasKnownCompletion)
        {
            AddMacroIssue(
                state,
                "MACRO_PDF417_COMPLETION_UNKNOWN",
                "The Macro PDF417 sequence has neither a segment count nor a last-segment marker.",
                "Count",
                "a segment count or last-segment marker",
                "neither",
                BarcodeValidationSeverity.Warning);
        }

        if (missing.Length > 0)
        {
            AddMacroIssue(
                state,
                "MACRO_PDF417_SEGMENTS_MISSING",
                "One or more Macro PDF417 segments are missing.",
                "Index",
                "every index from zero through the final segment",
                JoinMacroIndices(missing),
                BarcodeValidationSeverity.Warning);
        }

        if (request.RequireComplete
            && (!hasKnownCompletion || missing.Length > 0))
        {
            throw new BarcodeValidationException(
                "Macro PDF417 assembly requires every segment, but the sequence is incomplete.");
        }

        StructuredAppendResult result = new StructuredAppendResult();
        result.Complete = complete;
        result.Payload = payload;
        result.Segments = normalized;
        result.MissingIndices = missing;
        result.Conflicts = state.Conflicts.ToArray();
        result.FileIdentifier = state.FileIdentifier;
        result.Issues = state.Issues.ToArray();
        return result;
    }

    internal static int CalculateMacroChecksum(byte[] data)
    {
        if (data == null)
        {
            throw new BarcodeValidationException(
                "Macro PDF417 checksum data must not be null.");
        }

        int checksum = 0;
        int index = 0;
        while (index < data.Length)
        {
            checksum = checksum ^ (data[index] << 8);
            int bit = 0;
            while (bit < 8)
            {
                if ((checksum & 0x8000) != 0)
                {
                    checksum = ((checksum << 1) ^ CcittPolynomial)
                        & MaximumChecksum;
                }
                else
                {
                    checksum = (checksum << 1) & MaximumChecksum;
                }

                bit = bit + 1;
            }

            index = index + 1;
        }

        return checksum;
    }

    static void ValidateMacroRequest(StructuredAppendRequest request)
    {
        if (request == null)
        {
            throw new BarcodeValidationException(
                "Macro PDF417 assembly request must not be null.");
        }

        if (request.Format != BarcodeFormat.MacroPdf417)
        {
            throw new BarcodeValidationException(
                "Macro PDF417 assembly requires the Macro PDF417 format.");
        }
    }

    static BarcodeDecodeResult[] RequireMacroSegments(
        BarcodeDecodeResult[]? segments)
    {
        if (segments == null || segments.Length == 0)
        {
            throw new BarcodeValidationException(
                "Macro PDF417 assembly requires at least one decoded segment.");
        }

        if (segments.Length > Pdf417MacroCodec.MaximumSegmentCount)
        {
            throw new BarcodeValidationException(
                "Macro PDF417 assembly contains too many supplied segments.");
        }

        return segments;
    }

    static StructuredAppendInfo ValidateMacroSegment(
        BarcodeDecodeResult segment,
        int sourceIndex)
    {
        if (segment == null)
        {
            throw new BarcodeValidationException(
                $"Macro PDF417 segment at input position {sourceIndex} is null.");
        }

        if (segment.Format != BarcodeFormat.MacroPdf417)
        {
            throw new BarcodeValidationException(
                $"Segment at input position {sourceIndex} is not Macro PDF417.");
        }

        BarcodePayload? payload = segment.Payload;
        if (payload == null)
        {
            throw new BarcodeValidationException(
                $"Macro PDF417 segment at input position {sourceIndex} has no payload.");
        }

        if (payload.RawBytes == null && payload.Text == null)
        {
            throw new BarcodeValidationException(
                $"Macro PDF417 segment at input position {sourceIndex} has no payload content.");
        }

        StructuredAppendInfo? information = payload.StructuredAppend;
        if (information == null)
        {
            throw new BarcodeValidationException(
                $"Macro PDF417 segment at input position {sourceIndex} has no structured-append metadata.");
        }

        if (information.Format != BarcodeFormat.MacroPdf417)
        {
            throw new BarcodeValidationException(
                $"Structured-append metadata at input position {sourceIndex} is not Macro PDF417.");
        }

        if (information.Index < 0
            || information.Index > Pdf417MacroCodec.MaximumSegmentIndex)
        {
            throw new BarcodeValidationException(
                $"Macro PDF417 segment index at input position {sourceIndex} is outside the supported range.");
        }

        if (information.Count < 0
            || information.Count > Pdf417MacroCodec.MaximumSegmentCount
            || (information.Count > 0
                && information.Index >= information.Count))
        {
            throw new BarcodeValidationException(
                $"Macro PDF417 segment count at input position {sourceIndex} is invalid.");
        }

        if (information.FileIdentifier == null
            || information.FileIdentifier.Length == 0)
        {
            throw new BarcodeValidationException(
                $"Macro PDF417 file identifier at input position {sourceIndex} is missing.");
        }

        return information;
    }

    static void RecordMacroIdentity(
        Pdf417MacroAssemblyState state,
        StructuredAppendInfo information,
        int sourceIndex)
    {
        if (state.FileIdentifier == null)
        {
            state.FileIdentifier = information.FileIdentifier;
            return;
        }

        if (state.FileIdentifier != information.FileIdentifier)
        {
            AddMacroConflict(
                state,
                "MACRO_PDF417_FILE_IDENTIFIER_CONFLICT",
                $"Macro PDF417 segment at input position {sourceIndex} has a conflicting file identifier.",
                "FileIdentifier",
                state.FileIdentifier,
                information.FileIdentifier,
                BarcodeValidationSeverity.Error,
                true);
        }
    }

    static void RecordMacroCount(
        Pdf417MacroAssemblyState state,
        StructuredAppendInfo information,
        int sourceIndex)
    {
        if (information.Count == 0)
        {
            return;
        }

        state.CountBearingSegmentCount = state.CountBearingSegmentCount + 1;
        if (state.DeclaredCount == 0)
        {
            state.DeclaredCount = information.Count;
            return;
        }

        if (state.DeclaredCount != information.Count)
        {
            AddMacroConflict(
                state,
                "MACRO_PDF417_SEGMENT_COUNT_CONFLICT",
                $"Macro PDF417 segment at input position {sourceIndex} has a conflicting segment count.",
                "Count",
                state.DeclaredCount.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                information.Count.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                BarcodeValidationSeverity.Error,
                true);
        }
    }

    static void RecordMacroLastSegment(
        Pdf417MacroAssemblyState state,
        StructuredAppendInfo information,
        int sourceIndex)
    {
        if (!information.IsLastSegment)
        {
            return;
        }

        if (state.LastIndex < 0)
        {
            state.LastIndex = information.Index;
            return;
        }

        if (state.LastIndex != information.Index)
        {
            AddMacroConflict(
                state,
                "MACRO_PDF417_LAST_SEGMENT_CONFLICT",
                $"Macro PDF417 segment at input position {sourceIndex} conflicts with the final segment index.",
                "IsLastSegment",
                state.LastIndex.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                information.Index.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                BarcodeValidationSeverity.Error,
                true);
        }
    }

    static void RecordMacroChecksum(
        Pdf417MacroAssemblyState state,
        BarcodeDecodeResult segment,
        int sourceIndex)
    {
        BarcodeDecodeMetadata? metadata = segment.Metadata;
        if (metadata == null || !metadata.HasChecksum)
        {
            return;
        }

        if (metadata.ChecksumValue < 0
            || metadata.ChecksumValue > MaximumChecksum)
        {
            throw new BarcodeValidationException(
                $"Macro PDF417 checksum at input position {sourceIndex} is outside the supported range.");
        }

        if (!state.HasChecksum)
        {
            state.HasChecksum = true;
            state.Checksum = metadata.ChecksumValue;
            return;
        }

        if (state.Checksum != metadata.ChecksumValue)
        {
            AddMacroConflict(
                state,
                "MACRO_PDF417_CHECKSUM_CONFLICT",
                $"Macro PDF417 segment at input position {sourceIndex} has a conflicting file checksum.",
                "ChecksumValue",
                state.Checksum.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                metadata.ChecksumValue.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                BarcodeValidationSeverity.Error,
                true);
        }
    }

    static void RecordMacroSegment(
        Pdf417MacroAssemblyState state,
        BarcodeDecodeResult segment,
        StructuredAppendInfo information,
        bool allowIdenticalDuplicates)
    {
        Pdf417MacroAssemblyEntry? existing = FindMacroEntry(
            state.Entries,
            information.Index);
        if (existing == null)
        {
            Pdf417MacroAssemblyEntry entry = new Pdf417MacroAssemblyEntry();
            entry.Segment = segment;
            entry.Information = information;
            state.Entries.Add(entry);
            return;
        }

        bool identical = AreMacroSegmentsIdentical(
            existing.Segment,
            segment);
        if (identical && allowIdenticalDuplicates)
        {
            AddMacroConflict(
                state,
                "MACRO_PDF417_IDENTICAL_DUPLICATE",
                $"Identical Macro PDF417 segment index {information.Index} was ignored.",
                "Index",
                "one segment",
                "identical duplicate",
                BarcodeValidationSeverity.Warning,
                false);
            return;
        }

        string code = identical
            ? "MACRO_PDF417_DUPLICATE_NOT_ALLOWED"
            : "MACRO_PDF417_DUPLICATE_CONFLICT";
        string observed = identical
            ? "identical duplicate"
            : "different payload or metadata";
        AddMacroConflict(
            state,
            code,
            $"Macro PDF417 segment index {information.Index} is duplicated and cannot be assembled.",
            "Index",
            "one segment",
            observed,
            BarcodeValidationSeverity.Error,
            true);
    }

    static Pdf417MacroAssemblyEntry? FindMacroEntry(
        global::System.Collections.Generic.List<Pdf417MacroAssemblyEntry> entries,
        int index)
    {
        int entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            if (entries[entryIndex].Information.Index == index)
            {
                return entries[entryIndex];
            }

            entryIndex = entryIndex + 1;
        }

        return null;
    }

    static int DetermineMacroExpectedCount(
        Pdf417MacroAssemblyState state)
    {
        int lastCount = state.LastIndex < 0 ? 0 : state.LastIndex + 1;
        if (state.DeclaredCount > 0
            && lastCount > 0
            && state.DeclaredCount != lastCount)
        {
            AddMacroConflict(
                state,
                "MACRO_PDF417_TERMINATOR_COUNT_CONFLICT",
                "The Macro PDF417 last-segment marker conflicts with the declared segment count.",
                "Count",
                state.DeclaredCount.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                lastCount.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                BarcodeValidationSeverity.Error,
                true);
        }

        int expectedCount = state.DeclaredCount > 0
            ? state.DeclaredCount
            : lastCount;
        if (expectedCount > 0)
        {
            int index = 0;
            while (index < state.Entries.Count)
            {
                if (state.Entries[index].Information.Index >= expectedCount)
                {
                    AddMacroConflict(
                        state,
                        "MACRO_PDF417_INDEX_OUTSIDE_SEQUENCE",
                        $"Macro PDF417 segment index {state.Entries[index].Information.Index} is outside the completed sequence.",
                        "Index",
                        $"zero through {expectedCount - 1}",
                        state.Entries[index].Information.Index.ToString(
                            global::System.Globalization.CultureInfo.InvariantCulture),
                        BarcodeValidationSeverity.Error,
                        true);
                }

                index = index + 1;
            }
        }

        return expectedCount;
    }

    static int[] FindMissingMacroIndices(
        Pdf417MacroAssemblyState state,
        int expectedCount)
    {
        if (expectedCount == 0)
        {
            return new int[0];
        }

        int[] buffer = new int[expectedCount];
        int count = 0;
        int expectedIndex = 0;
        while (expectedIndex < expectedCount)
        {
            if (FindMacroEntry(state.Entries, expectedIndex) == null)
            {
                buffer[count] = expectedIndex;
                count = count + 1;
            }

            expectedIndex = expectedIndex + 1;
        }

        int[] missing = new int[count];
        int index = 0;
        while (index < count)
        {
            missing[index] = buffer[index];
            index = index + 1;
        }

        return missing;
    }

    static void SortMacroEntries(
        global::System.Collections.Generic.List<Pdf417MacroAssemblyEntry> entries)
    {
        int index = 1;
        while (index < entries.Count)
        {
            Pdf417MacroAssemblyEntry value = entries[index];
            int destination = index - 1;
            while (destination >= 0
                && entries[destination].Information.Index
                    > value.Information.Index)
            {
                entries[destination + 1] = entries[destination];
                destination = destination - 1;
            }

            entries[destination + 1] = value;
            index = index + 1;
        }
    }

    static BarcodeDecodeResult[] CopyNormalizedMacroSegments(
        global::System.Collections.Generic.List<Pdf417MacroAssemblyEntry> entries)
    {
        BarcodeDecodeResult[] segments =
            new BarcodeDecodeResult[entries.Count];
        int index = 0;
        while (index < entries.Count)
        {
            segments[index] = ResultCopier.CopyDecodeResult(
                entries[index].Segment);
            index = index + 1;
        }

        return segments;
    }

    static BarcodePayload AssembleMacroPayload(
        global::System.Collections.Generic.List<Pdf417MacroAssemblyEntry> entries)
    {
        BarcodePayload payload = new BarcodePayload();
        bool hasAllBytes = true;
        bool hasAllText = true;
        long totalByteCount = 0;
        global::System.Text.StringBuilder text =
            new global::System.Text.StringBuilder();
        string? characterEncoding = null;
        bool characterEncodingConsistent = true;
        int index = 0;
        while (index < entries.Count)
        {
            BarcodePayload segmentPayload = RequireMacroPayload(
                entries[index].Segment.Payload);
            if (segmentPayload.RawBytes == null)
            {
                hasAllBytes = false;
            }
            else
            {
                totalByteCount = totalByteCount
                    + segmentPayload.RawBytes.Length;
                if (totalByteCount > int.MaxValue)
                {
                    throw new BarcodeValidationException(
                        "Assembled Macro PDF417 payload exceeds the supported byte length.");
                }
            }

            if (segmentPayload.Text == null)
            {
                hasAllText = false;
            }
            else
            {
                text.Append(segmentPayload.Text);
            }

            if (index == 0)
            {
                characterEncoding = segmentPayload.CharacterEncoding;
            }
            else if (characterEncoding != segmentPayload.CharacterEncoding)
            {
                characterEncodingConsistent = false;
            }

            index = index + 1;
        }

        if (hasAllBytes)
        {
            byte[] bytes = new byte[(int)totalByteCount];
            int destination = 0;
            index = 0;
            while (index < entries.Count)
            {
                byte[] segmentBytes = RequireMacroBytes(
                    RequireMacroPayload(
                        entries[index].Segment.Payload).RawBytes);
                int byteIndex = 0;
                while (byteIndex < segmentBytes.Length)
                {
                    bytes[destination] = segmentBytes[byteIndex];
                    destination = destination + 1;
                    byteIndex = byteIndex + 1;
                }

                index = index + 1;
            }

            payload.RawBytes = bytes;
            payload.EciSegments = AssembleMacroEciSegments(entries);
        }

        if (hasAllText)
        {
            payload.Text = text.ToString();
        }

        if (characterEncodingConsistent)
        {
            payload.CharacterEncoding = characterEncoding;
        }

        return payload;
    }

    static EciSegment[]? AssembleMacroEciSegments(
        global::System.Collections.Generic.List<Pdf417MacroAssemblyEntry> entries)
    {
        int eciCount = 0;
        int entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            BarcodePayload segmentPayload = RequireMacroPayload(
                entries[entryIndex].Segment.Payload);
            if (segmentPayload.EciSegments != null)
            {
                eciCount = CheckedMath.Add(
                    eciCount,
                    segmentPayload.EciSegments.Length);
            }

            entryIndex = entryIndex + 1;
        }

        if (eciCount == 0)
        {
            return null;
        }

        EciSegment[] assembled = new EciSegment[eciCount];
        int destination = 0;
        int byteOffset = 0;
        entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            BarcodePayload segmentPayload = RequireMacroPayload(
                entries[entryIndex].Segment.Payload);
            if (segmentPayload.EciSegments != null)
            {
                int eciIndex = 0;
                while (eciIndex < segmentPayload.EciSegments.Length)
                {
                    EciSegment source =
                        segmentPayload.EciSegments[eciIndex];
                    EciSegment copy = new EciSegment();
                    copy.AssignmentNumber = source.AssignmentNumber;
                    copy.ByteOffset = CheckedMath.Add(
                        byteOffset,
                        source.ByteOffset);
                    copy.ByteCount = source.ByteCount;
                    assembled[destination] = copy;
                    destination = destination + 1;
                    eciIndex = eciIndex + 1;
                }
            }

            byte[] segmentBytes = RequireMacroBytes(
                segmentPayload.RawBytes);
            byteOffset = CheckedMath.Add(
                byteOffset,
                segmentBytes.Length);
            entryIndex = entryIndex + 1;
        }

        return assembled;
    }

    static bool VerifyMacroChecksum(
        Pdf417MacroAssemblyState state,
        BarcodePayload payload,
        bool structurallyComplete)
    {
        if (!state.HasChecksum)
        {
            return true;
        }

        if (!structurallyComplete || payload.RawBytes == null)
        {
            AddMacroIssue(
                state,
                "MACRO_PDF417_CHECKSUM_NOT_VERIFIED",
                "The Macro PDF417 file checksum could not be verified without a complete raw-byte payload.",
                "ChecksumValue",
                state.Checksum.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                "not verified",
                BarcodeValidationSeverity.Warning);
            return structurallyComplete;
        }

        int calculated = CalculateMacroChecksum(payload.RawBytes);
        if (calculated != state.Checksum)
        {
            AddMacroConflict(
                state,
                "MACRO_PDF417_CHECKSUM_MISMATCH",
                "The assembled Macro PDF417 payload does not match its CCITT-16 checksum.",
                "ChecksumValue",
                state.Checksum.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                calculated.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                BarcodeValidationSeverity.Error,
                true);
            return false;
        }

        AddMacroIssue(
            state,
            "MACRO_PDF417_CHECKSUM_VALID",
            "The assembled Macro PDF417 payload matches its CCITT-16 checksum.",
            "ChecksumValue",
            state.Checksum.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture),
            calculated.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture),
            BarcodeValidationSeverity.Information);
        return true;
    }

    static bool AreMacroSegmentsIdentical(
        BarcodeDecodeResult first,
        BarcodeDecodeResult second)
    {
        BarcodePayload firstPayload = RequireMacroPayload(first.Payload);
        BarcodePayload secondPayload = RequireMacroPayload(second.Payload);
        if (!AreMacroByteArraysEqual(
                firstPayload.RawBytes,
                secondPayload.RawBytes)
            || firstPayload.Text != secondPayload.Text
            || firstPayload.CharacterEncoding
                != secondPayload.CharacterEncoding
            || firstPayload.IsGs1 != secondPayload.IsGs1
            || !AreMacroInformationEqual(
                firstPayload.StructuredAppend,
                secondPayload.StructuredAppend))
        {
            return false;
        }

        BarcodeDecodeMetadata? firstMetadata = first.Metadata;
        BarcodeDecodeMetadata? secondMetadata = second.Metadata;
        bool firstHasChecksum = firstMetadata != null
            && firstMetadata.HasChecksum;
        bool secondHasChecksum = secondMetadata != null
            && secondMetadata.HasChecksum;
        if (firstHasChecksum != secondHasChecksum)
        {
            return false;
        }

        return !firstHasChecksum
            || (firstMetadata != null
                && secondMetadata != null
                && firstMetadata.ChecksumValue
                    == secondMetadata.ChecksumValue);
    }

    static bool AreMacroByteArraysEqual(byte[]? first, byte[]? second)
    {
        if (first == null || second == null)
        {
            return first == second;
        }

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

    static bool AreMacroInformationEqual(
        StructuredAppendInfo? first,
        StructuredAppendInfo? second)
    {
        if (first == null || second == null)
        {
            return first == second;
        }

        return first.Format == second.Format
            && first.Index == second.Index
            && first.Count == second.Count
            && first.FileIdentifier == second.FileIdentifier
            && first.IsLastSegment == second.IsLastSegment;
    }

    static BarcodePayload RequireMacroPayload(BarcodePayload? payload)
    {
        if (payload == null)
        {
            throw new BarcodeValidationException(
                "Macro PDF417 payload is unexpectedly missing.");
        }

        return payload;
    }

    static byte[] RequireMacroBytes(byte[]? bytes)
    {
        if (bytes == null)
        {
            throw new BarcodeValidationException(
                "Macro PDF417 raw payload bytes are unexpectedly missing.");
        }

        return bytes;
    }

    static void AddMacroConflict(
        Pdf417MacroAssemblyState state,
        string code,
        string message,
        string field,
        string? expected,
        string? observed,
        BarcodeValidationSeverity severity,
        bool fatal)
    {
        state.Conflicts.Add(message);
        AddMacroIssue(
            state,
            code,
            message,
            field,
            expected,
            observed,
            severity);
        if (fatal)
        {
            state.FatalConflict = true;
        }
    }

    static void AddMacroIssue(
        Pdf417MacroAssemblyState state,
        string code,
        string message,
        string field,
        string? expected,
        string? observed,
        BarcodeValidationSeverity severity)
    {
        BarcodeValidationIssue issue = ValidationIssueFactory.Create(
            severity,
            code,
            message,
            BarcodeStandards.Pdf417,
            field,
            -1,
            expected,
            observed);
        state.Issues.Add(issue);
    }

    static string JoinMacroIndices(int[] indices)
    {
        global::System.Text.StringBuilder builder =
            new global::System.Text.StringBuilder();
        int index = 0;
        while (index < indices.Length)
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            builder.Append(indices[index].ToString(
                global::System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }

        return builder.ToString();
    }
}
