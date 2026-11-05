namespace barcode;

internal class DataMatrixStructuredAppendEntry
{
    internal BarcodeDecodeResult Segment = new BarcodeDecodeResult();
    internal StructuredAppendInfo Information = new StructuredAppendInfo();
    internal string FileIdentifier = "";
}

internal class DataMatrixStructuredAppendState
{
    internal global::System.Collections.Generic.List<DataMatrixStructuredAppendEntry> Entries =
        new global::System.Collections.Generic.List<DataMatrixStructuredAppendEntry>();
    internal global::System.Collections.Generic.List<string> Conflicts =
        new global::System.Collections.Generic.List<string>();
    internal global::System.Collections.Generic.List<BarcodeValidationIssue> Issues =
        new global::System.Collections.Generic.List<BarcodeValidationIssue>();
    internal BarcodeFormat Format;
    internal string? FileIdentifier;
    internal int Count;
    internal bool FatalConflict;
}

internal static class DataMatrixStructuredAppendAssembler
{
    internal static StructuredAppendResult AssembleDataMatrixSegments(
        StructuredAppendRequest request)
    {
        ValidateDataMatrixRequest(request);
        BarcodeDecodeResult[] source = RequireDataMatrixSegments(
            request.Segments);
        DataMatrixStructuredAppendState state =
            new DataMatrixStructuredAppendState();
        state.Format = request.Format;
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            DataMatrixStructuredAppendEntry entry = CreateDataMatrixEntry(
                source[sourceIndex],
                sourceIndex,
                request.Format);
            RecordDataMatrixIdentity(state, entry, sourceIndex);
            RecordDataMatrixCount(state, entry, sourceIndex);
            RecordDataMatrixSegment(
                state,
                entry,
                request.AllowIdenticalDuplicates);
            sourceIndex = sourceIndex + 1;
        }

        SortDataMatrixEntries(state.Entries);
        int[] missing = FindMissingDataMatrixIndices(
            state.Entries,
            state.Count);
        if (missing.Length > 0)
        {
            AddDataMatrixIssue(
                state,
                "DATAMATRIX_STRUCTURED_APPEND_SEGMENTS_MISSING",
                "One or more Data Matrix structured-append segments are missing.",
                "Index",
                "every index from zero through the final segment",
                JoinDataMatrixIndices(missing),
                BarcodeValidationSeverity.Warning);
        }

        if (request.RequireComplete && missing.Length > 0)
        {
            throw new BarcodeValidationException(
                "Data Matrix structured-append assembly requires every segment, but the sequence is incomplete.");
        }

        BarcodeDecodeResult[] normalized = CopyDataMatrixSegments(
            state.Entries);
        BarcodePayload payload = AssembleDataMatrixPayload(
            state.Entries,
            state.Format);
        StructuredAppendResult result = new StructuredAppendResult();
        result.Complete = missing.Length == 0
            && state.Entries.Count == state.Count
            && !state.FatalConflict;
        result.Payload = payload;
        result.Segments = normalized;
        result.MissingIndices = missing;
        result.Conflicts = state.Conflicts.ToArray();
        result.FileIdentifier = state.FileIdentifier;
        result.Issues = state.Issues.ToArray();
        return result;
    }

    static void ValidateDataMatrixRequest(StructuredAppendRequest request)
    {
        if (request == null)
        {
            throw new BarcodeValidationException(
                "Data Matrix structured-append request must not be null.");
        }

        if (request.Format != BarcodeFormat.DataMatrix &&
            request.Format != BarcodeFormat.Gs1DataMatrix)
        {
            throw new BarcodeValidationException(
                "Data Matrix structured-append assembly requires a Data Matrix format.");
        }
    }

    static BarcodeDecodeResult[] RequireDataMatrixSegments(
        BarcodeDecodeResult[]? segments)
    {
        if (segments == null || segments.Length == 0)
        {
            throw new BarcodeValidationException(
                "Data Matrix structured-append assembly requires at least one decoded segment.");
        }

        if (segments.Length >
            DataMatrixControlCodewordEncoder.MaximumStructuredAppendCount)
        {
            throw new BarcodeValidationException(
                "Data Matrix structured-append assembly contains too many supplied segments.");
        }

        return segments;
    }

    static DataMatrixStructuredAppendEntry CreateDataMatrixEntry(
        BarcodeDecodeResult segment,
        int sourceIndex,
        BarcodeFormat format)
    {
        if (segment == null)
        {
            throw new BarcodeValidationException(
                $"Data Matrix segment at input position {sourceIndex} is null.");
        }

        if (segment.Format != format)
        {
            throw new BarcodeValidationException(
                $"Data Matrix segment at input position {sourceIndex} does not match the requested format.");
        }

        BarcodePayload? payload = segment.Payload;
        if (payload == null ||
            (payload.RawBytes == null && payload.Text == null))
        {
            throw new BarcodeValidationException(
                $"Data Matrix segment at input position {sourceIndex} has no payload content.");
        }

        if (payload.IsGs1 != (format == BarcodeFormat.Gs1DataMatrix))
        {
            throw new BarcodeValidationException(
                $"Data Matrix segment at input position {sourceIndex} has inconsistent GS1 state.");
        }

        StructuredAppendInfo? information = payload.StructuredAppend;
        if (information == null)
        {
            throw new BarcodeValidationException(
                $"Data Matrix segment at input position {sourceIndex} has no structured-append metadata.");
        }

        if (information.Format != format)
        {
            throw new BarcodeValidationException(
                $"Data Matrix structured-append metadata at input position {sourceIndex} does not match the requested format.");
        }

        if (information.Count < 2 ||
            information.Count >
                DataMatrixControlCodewordEncoder.MaximumStructuredAppendCount ||
            information.Index < 0 ||
            information.Index >= information.Count)
        {
            throw new BarcodeValidationException(
                $"Data Matrix structured-append sequence metadata at input position {sourceIndex} is invalid.");
        }

        if (information.IsLastSegment !=
            (information.Index == information.Count - 1))
        {
            throw new BarcodeValidationException(
                $"Data Matrix last-segment state at input position {sourceIndex} conflicts with its index and count.");
        }

        string identifier = NormalizeDataMatrixFileIdentifier(
            information.FileIdentifier,
            sourceIndex);
        DataMatrixStructuredAppendEntry entry =
            new DataMatrixStructuredAppendEntry();
        entry.Segment = segment;
        entry.Information = information;
        entry.FileIdentifier = identifier;
        return entry;
    }

    static string NormalizeDataMatrixFileIdentifier(
        string? identifier,
        int sourceIndex)
    {
        if (identifier == null || identifier.Length == 0)
        {
            return "001001";
        }

        if (identifier.Length > 6)
        {
            throw new BarcodeValidationException(
                $"Data Matrix file identifier at input position {sourceIndex} is invalid.");
        }

        int value = ParseDataMatrixIdentifier(identifier, sourceIndex);
        int first;
        int second;
        if (identifier.Length == 6)
        {
            first = ParseDataMatrixIdentifierPart(
                identifier,
                0,
                sourceIndex);
            second = ParseDataMatrixIdentifierPart(
                identifier,
                3,
                sourceIndex);
            if (first < 1 || first > 254 || second < 1 || second > 254)
            {
                throw new BarcodeValidationException(
                    $"Data Matrix file identifier at input position {sourceIndex} is outside the supported range.");
            }
        }
        else
        {
            if (value < 1 || value >
                DataMatrixControlCodewordEncoder.MaximumStructuredAppendFileId)
            {
                throw new BarcodeValidationException(
                    $"Data Matrix file identifier at input position {sourceIndex} is outside the supported range.");
            }

            first = 1 + ((value - 1) / 254);
            second = 1 + ((value - 1) % 254);
        }

        return FormatDataMatrixIdentifierPart(first) +
            FormatDataMatrixIdentifierPart(second);
    }

    static int ParseDataMatrixIdentifier(
        string identifier,
        int sourceIndex)
    {
        int result = 0;
        int index = 0;
        while (index < identifier.Length)
        {
            char character = identifier[index];
            if (character < '0' || character > '9')
            {
                throw new BarcodeValidationException(
                    $"Data Matrix file identifier at input position {sourceIndex} must contain decimal digits only.");
            }

            result = (result * 10) + (character - '0');
            index = index + 1;
        }

        return result;
    }

    static int ParseDataMatrixIdentifierPart(
        string identifier,
        int start,
        int sourceIndex)
    {
        string part = identifier.Substring(start, 3);
        return ParseDataMatrixIdentifier(part, sourceIndex);
    }

    static string FormatDataMatrixIdentifierPart(int value)
    {
        if (value < 10)
        {
            return "00" + value.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        }

        if (value < 100)
        {
            return "0" + value.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        }

        return value.ToString(
            global::System.Globalization.CultureInfo.InvariantCulture);
    }

    static void RecordDataMatrixIdentity(
        DataMatrixStructuredAppendState state,
        DataMatrixStructuredAppendEntry entry,
        int sourceIndex)
    {
        if (state.FileIdentifier == null)
        {
            state.FileIdentifier = entry.FileIdentifier;
            return;
        }

        if (state.FileIdentifier != entry.FileIdentifier)
        {
            AddDataMatrixConflict(
                state,
                "DATAMATRIX_STRUCTURED_APPEND_FILE_IDENTIFIER_CONFLICT",
                $"Data Matrix segment at input position {sourceIndex} has a conflicting file identifier.",
                "FileIdentifier",
                state.FileIdentifier,
                entry.FileIdentifier,
                BarcodeValidationSeverity.Error,
                true);
        }
    }

    static void RecordDataMatrixCount(
        DataMatrixStructuredAppendState state,
        DataMatrixStructuredAppendEntry entry,
        int sourceIndex)
    {
        if (state.Count == 0)
        {
            state.Count = entry.Information.Count;
            return;
        }

        if (state.Count != entry.Information.Count)
        {
            AddDataMatrixConflict(
                state,
                "DATAMATRIX_STRUCTURED_APPEND_COUNT_CONFLICT",
                $"Data Matrix segment at input position {sourceIndex} has a conflicting segment count.",
                "Count",
                state.Count.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                entry.Information.Count.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                BarcodeValidationSeverity.Error,
                true);
        }
    }

    static void RecordDataMatrixSegment(
        DataMatrixStructuredAppendState state,
        DataMatrixStructuredAppendEntry entry,
        bool allowIdenticalDuplicates)
    {
        DataMatrixStructuredAppendEntry? existing = FindDataMatrixEntry(
            state.Entries,
            entry.Information.Index);
        if (existing == null)
        {
            state.Entries.Add(entry);
            return;
        }

        bool identical = AreDataMatrixSegmentsIdentical(existing, entry);
        if (identical && allowIdenticalDuplicates)
        {
            AddDataMatrixConflict(
                state,
                "DATAMATRIX_STRUCTURED_APPEND_IDENTICAL_DUPLICATE",
                $"Identical Data Matrix segment index {entry.Information.Index} was ignored.",
                "Index",
                "one segment",
                "identical duplicate",
                BarcodeValidationSeverity.Warning,
                false);
            return;
        }

        string code = identical
            ? "DATAMATRIX_STRUCTURED_APPEND_DUPLICATE_NOT_ALLOWED"
            : "DATAMATRIX_STRUCTURED_APPEND_DUPLICATE_CONFLICT";
        string observed = identical
            ? "identical duplicate"
            : "different payload or metadata";
        AddDataMatrixConflict(
            state,
            code,
            $"Data Matrix segment index {entry.Information.Index} is duplicated and cannot be assembled.",
            "Index",
            "one segment",
            observed,
            BarcodeValidationSeverity.Error,
            true);
    }

    static DataMatrixStructuredAppendEntry? FindDataMatrixEntry(
        global::System.Collections.Generic.List<DataMatrixStructuredAppendEntry> entries,
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

    static void SortDataMatrixEntries(
        global::System.Collections.Generic.List<DataMatrixStructuredAppendEntry> entries)
    {
        int index = 1;
        while (index < entries.Count)
        {
            DataMatrixStructuredAppendEntry value = entries[index];
            int destination = index - 1;
            while (destination >= 0 &&
                entries[destination].Information.Index >
                    value.Information.Index)
            {
                entries[destination + 1] = entries[destination];
                destination = destination - 1;
            }

            entries[destination + 1] = value;
            index = index + 1;
        }
    }

    static int[] FindMissingDataMatrixIndices(
        global::System.Collections.Generic.List<DataMatrixStructuredAppendEntry> entries,
        int expectedCount)
    {
        int[] buffer = new int[expectedCount];
        int count = 0;
        int expectedIndex = 0;
        while (expectedIndex < expectedCount)
        {
            if (FindDataMatrixEntry(entries, expectedIndex) == null)
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

    static BarcodeDecodeResult[] CopyDataMatrixSegments(
        global::System.Collections.Generic.List<DataMatrixStructuredAppendEntry> entries)
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

    static BarcodePayload AssembleDataMatrixPayload(
        global::System.Collections.Generic.List<DataMatrixStructuredAppendEntry> entries,
        BarcodeFormat format)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.IsGs1 = format == BarcodeFormat.Gs1DataMatrix;
        bool hasAllBytes = true;
        bool hasAllText = true;
        bool encodingConsistent = true;
        string? characterEncoding = null;
        long byteCount = 0;
        global::System.Text.StringBuilder text =
            new global::System.Text.StringBuilder();
        int index = 0;
        while (index < entries.Count)
        {
            BarcodePayload segmentPayload = RequireDataMatrixPayload(
                entries[index].Segment.Payload);
            if (segmentPayload.RawBytes == null)
            {
                hasAllBytes = false;
            }
            else
            {
                byteCount = byteCount + segmentPayload.RawBytes.Length;
                if (byteCount > DecodingContext.DefaultMaximumBytes)
                {
                    throw new BarcodeValidationException(
                        "Assembled Data Matrix payload exceeds the supported byte length.");
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
                encodingConsistent = false;
            }

            index = index + 1;
        }

        if (hasAllBytes)
        {
            payload.RawBytes = ConcatenateDataMatrixBytes(
                entries,
                (int)byteCount);
            payload.EciSegments = AssembleDataMatrixEciSegments(entries);
        }

        if (hasAllText)
        {
            payload.Text = text.ToString();
        }

        if (encodingConsistent)
        {
            payload.CharacterEncoding = characterEncoding;
        }

        return payload;
    }

    static byte[] ConcatenateDataMatrixBytes(
        global::System.Collections.Generic.List<DataMatrixStructuredAppendEntry> entries,
        int totalCount)
    {
        byte[] bytes = new byte[totalCount];
        int destination = 0;
        int entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            byte[] segmentBytes = RequireDataMatrixBytes(
                RequireDataMatrixPayload(
                    entries[entryIndex].Segment.Payload).RawBytes);
            int byteIndex = 0;
            while (byteIndex < segmentBytes.Length)
            {
                bytes[destination] = segmentBytes[byteIndex];
                destination = destination + 1;
                byteIndex = byteIndex + 1;
            }

            entryIndex = entryIndex + 1;
        }

        return bytes;
    }

    static EciSegment[]? AssembleDataMatrixEciSegments(
        global::System.Collections.Generic.List<DataMatrixStructuredAppendEntry> entries)
    {
        int eciCount = 0;
        int entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            BarcodePayload payload = RequireDataMatrixPayload(
                entries[entryIndex].Segment.Payload);
            if (payload.EciSegments != null)
            {
                eciCount = CheckedMath.Add(
                    eciCount,
                    payload.EciSegments.Length);
            }

            entryIndex = entryIndex + 1;
        }

        if (eciCount == 0)
        {
            return null;
        }

        EciSegment[] eciSegments = new EciSegment[eciCount];
        int destination = 0;
        int byteOffset = 0;
        entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            BarcodePayload payload = RequireDataMatrixPayload(
                entries[entryIndex].Segment.Payload);
            if (payload.EciSegments != null)
            {
                int eciIndex = 0;
                while (eciIndex < payload.EciSegments.Length)
                {
                    EciSegment source = payload.EciSegments[eciIndex];
                    EciSegment copy = ResultCopier.CopyEciSegment(source);
                    copy.ByteOffset = CheckedMath.Add(
                        byteOffset,
                        source.ByteOffset);
                    eciSegments[destination] = copy;
                    destination = destination + 1;
                    eciIndex = eciIndex + 1;
                }
            }

            byte[] segmentBytes = RequireDataMatrixBytes(payload.RawBytes);
            byteOffset = CheckedMath.Add(byteOffset, segmentBytes.Length);
            entryIndex = entryIndex + 1;
        }

        return eciSegments;
    }

    static bool AreDataMatrixSegmentsIdentical(
        DataMatrixStructuredAppendEntry first,
        DataMatrixStructuredAppendEntry second)
    {
        BarcodePayload firstPayload = RequireDataMatrixPayload(
            first.Segment.Payload);
        BarcodePayload secondPayload = RequireDataMatrixPayload(
            second.Segment.Payload);
        return AreDataMatrixBytesEqual(
                firstPayload.RawBytes,
                secondPayload.RawBytes)
            && firstPayload.Text == secondPayload.Text
            && firstPayload.CharacterEncoding ==
                secondPayload.CharacterEncoding
            && firstPayload.IsGs1 == secondPayload.IsGs1
            && AreDataMatrixEciArraysEqual(
                firstPayload.EciSegments,
                secondPayload.EciSegments)
            && first.Information.Index == second.Information.Index
            && first.Information.Count == second.Information.Count
            && first.FileIdentifier == second.FileIdentifier
            && first.Information.IsLastSegment ==
                second.Information.IsLastSegment;
    }

    static bool AreDataMatrixBytesEqual(byte[]? first, byte[]? second)
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

    static bool AreDataMatrixEciArraysEqual(
        EciSegment[]? first,
        EciSegment[]? second)
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
            if (first[index].AssignmentNumber != second[index].AssignmentNumber ||
                first[index].ByteOffset != second[index].ByteOffset ||
                first[index].ByteCount != second[index].ByteCount)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static BarcodePayload RequireDataMatrixPayload(BarcodePayload? payload)
    {
        if (payload == null)
        {
            throw new BarcodeValidationException(
                "Data Matrix payload is unexpectedly missing.");
        }

        return payload;
    }

    static byte[] RequireDataMatrixBytes(byte[]? bytes)
    {
        if (bytes == null)
        {
            throw new BarcodeValidationException(
                "Data Matrix raw payload bytes are unexpectedly missing.");
        }

        return bytes;
    }

    static void AddDataMatrixConflict(
        DataMatrixStructuredAppendState state,
        string code,
        string message,
        string field,
        string? expected,
        string? observed,
        BarcodeValidationSeverity severity,
        bool fatal)
    {
        state.Conflicts.Add(message);
        AddDataMatrixIssue(
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

    static void AddDataMatrixIssue(
        DataMatrixStructuredAppendState state,
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
            BarcodeStandards.DataMatrix,
            field,
            -1,
            expected,
            observed);
        state.Issues.Add(issue);
    }

    static string JoinDataMatrixIndices(int[] indices)
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
