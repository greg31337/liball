namespace qrcode;

internal class StructuredAppendAssembly
{
    internal static StructuredAppendAssemblyResult Assemble(QrDecodeResult[] parts, int maximumResults, int maximumAllocationBytes)
    {
        StructuredAppendGroupingResult grouping = StructuredAppendGrouper.Group(parts, maximumResults, maximumAllocationBytes);
        if (parts.Length == 0) { throw new QrInputException("At least one sequence part is required.", "parts"); }
        StructuredAppendAssemblyResult result = new StructuredAppendAssemblyResult();
        // Preserve known identity diagnostics even when metadata conflicts stop
        // assembly before payload ordering (for example incompatible FNC1 states).
        if (grouping.Groups.Length == 1)
        {
            StructuredAppendGroup identified = grouping.Groups[0];
            result.ExpectedPartCount = identified.Total;
            result.Parity = identified.Parity;
            bool[] present = new bool[identified.Total];
            foreach (QrDecodeResult part in identified.Parts)
            {
                QrStructuredAppendInfo? info = part.StructuredAppend;
                if (info != null && !present[info.Index])
                {
                    present[info.Index] = true;
                    result.ActualPartCount++;
                }
            }
        }
        if (grouping.FailureReason != QrFailureReason.None || grouping.Groups.Length != 1 || grouping.OrdinaryResults.Length != 0)
        {
            result.FailureReason = QrFailureReason.ConflictingSequence;
            return result;
        }
        StructuredAppendGroup group = grouping.Groups[0];
        if (!ValidatePayloads(parts, maximumAllocationBytes))
        {
            result.FailureReason = QrFailureReason.ConflictingSequence;
            return result;
        }
        QrDecodeResult?[] slots = new QrDecodeResult[group.Total];
        bool[] conflicts = new bool[group.Total];
        foreach (QrDecodeResult part in group.Parts)
        {
            QrStructuredAppendInfo? info = part.StructuredAppend;
            if (info == null) { continue; }
            QrDecodeResult? previous = slots[info.Index];
            if (previous == null) { slots[info.Index] = part; }
            else
            {
                result.DuplicateCount++;
                if (!Equivalent(previous, part)) { conflicts[info.Index] = true; }
                // Prefer the observation requiring fewer repairs; ties retain
                // arrival order. Diagnostics from duplicates are not summed.
                else if ((long)part.CorrectedErrorCount + part.CorrectedErasureCount
                    < (long)previous.CorrectedErrorCount + previous.CorrectedErasureCount) { slots[info.Index] = part; }
            }
        }
        global::System.Collections.Generic.List<QrDecodeResult> ordered = new global::System.Collections.Generic.List<QrDecodeResult>();
        global::System.Collections.Generic.List<int> missing = new global::System.Collections.Generic.List<int>();
        global::System.Collections.Generic.List<int> conflicting = new global::System.Collections.Generic.List<int>();
        for (int index = 0; index < slots.Length; index++)
        {
            QrDecodeResult? part = slots[index];
            if (part == null) { missing.Add(index); }
            else { ordered.Add(part); }
            if (conflicts[index]) { conflicting.Add(index); }
        }
        result.OrderedParts = ordered.ToArray();
        result.ActualPartCount = ordered.Count;
        result.MissingIndices = missing.ToArray();
        result.ConflictingIndices = conflicting.ToArray();
        if (conflicting.Count > 0 || missing.Count > 0)
        {
            result.FailureReason = conflicting.Count > 0 ? QrFailureReason.ConflictingSequence : QrFailureReason.IncompleteSequence;
            return result;
        }
        int parity = 0;
        foreach (QrDecodeResult part in result.OrderedParts)
        {
            foreach (byte value in part.RawBytes) { parity ^= value; }
            // The encoder computes parity over the original FNC1 payload;
            // alphanumeric Data retains the percent-escaped representation.
            if (part.UsesFnc1First || part.Fnc1SecondApplicationIndicator >= 0)
            {
                foreach (QrSegment segment in part.Segments)
                {
                    if (segment.Mode == QrEncodingMode.Alphanumeric && segment.Text != null)
                    {
                        foreach (byte value in segment.Data) { parity ^= value; }
                        foreach (char value in segment.Text) { parity ^= value; }
                    }
                }
            }
        }
        if (parity != group.Parity)
        {
            result.FailureReason = QrFailureReason.ConflictingSequence;
            return result;
        }
        result.Result = Combine(result, group);
        result.Success = true;
        return result;
    }

    static QrDecodeResult Combine(StructuredAppendAssemblyResult assembly, StructuredAppendGroup group)
    {
        QrDecodeResult result = new QrDecodeResult
        {
            StructuredAppend = new QrStructuredAppendInfo { Total = group.Total, Parity = group.Parity,
                ApplicationIdentifier = group.ApplicationIdentifier, IsComplete = true },
            UsesFnc1First = group.UsesFnc1First, Fnc1SecondApplicationIndicator = group.Fnc1SecondApplicationIndicator,
            IsConformant = true
        };
        global::System.Collections.Generic.List<byte> bytes = new global::System.Collections.Generic.List<byte>();
        global::System.Collections.Generic.List<QrSegment> segments = new global::System.Collections.Generic.List<QrSegment>();
        global::System.Collections.Generic.List<int> assignments = new global::System.Collections.Generic.List<int>();
        global::System.Collections.Generic.List<string> warnings = new global::System.Collections.Generic.List<string>();
        bool hadMissingText = false;
        for (int index = 0; index < assembly.OrderedParts.Length; index++)
        {
            QrDecodeResult part = assembly.OrderedParts[index];
            bytes.AddRange(part.RawBytes);
            // Keep all per-part control boundaries and independent segment data.
            foreach (QrSegment segment in part.Segments) { segments.Add(DecodedPayloadBuilder.CopySegment(segment)); }
            assignments.AddRange(part.EciAssignments);
            foreach (string warning in part.Warnings) { warnings.Add($"Part {index}: {warning}"); }
            result.CorrectedErrorCount = CheckedDimensions.CalculateSum(result.CorrectedErrorCount, part.CorrectedErrorCount, int.MaxValue, "CorrectedErrorCount");
            result.CorrectedErasureCount = CheckedDimensions.CalculateSum(result.CorrectedErasureCount, part.CorrectedErasureCount, int.MaxValue, "CorrectedErasureCount");
            result.IsConformant = result.IsConformant && part.IsConformant;
            result.UsesHanziCompatibility = result.UsesHanziCompatibility || part.UsesHanziCompatibility;
            hadMissingText = hadMissingText || part.Text == null;
        }
        result.RawBytes = bytes.ToArray();
        result.Segments = segments.ToArray();
        result.EciAssignments = assignments.ToArray();
        result.Text = StructuredAppendTextBuilder.Build(assembly.OrderedParts);
        if (assembly.DuplicateCount > 0) { warnings.Add($"Ignored {assembly.DuplicateCount} equivalent duplicate observations."); }
        if (hadMissingText && result.Text != null) { warnings.Add("Recovered text across compatible byte-segment boundaries."); }
        if (result.Text == null) { warnings.Add("Complete text is unavailable; all sequence payload bytes were retained."); }
        result.Warnings = warnings.ToArray();
        result.SymbologyIdentifier = SymbologyIdentifierSelector.GetIdentifier(QrSymbolKind.Model2, assignments.Count > 0,
            group.UsesFnc1First, group.Fnc1SecondApplicationIndicator);
        return result;
    }

    static bool Equivalent(QrDecodeResult a, QrDecodeResult b)
    {
        if (a.Text != b.Text || a.UsesHanziCompatibility != b.UsesHanziCompatibility || !SameBytes(a.RawBytes, b.RawBytes)
            || a.Segments.Length != b.Segments.Length || a.EciAssignments.Length != b.EciAssignments.Length) { return false; }
        for (int index = 0; index < a.EciAssignments.Length; index++) { if (a.EciAssignments[index] != b.EciAssignments[index]) { return false; } }
        for (int index = 0; index < a.Segments.Length; index++)
        {
            QrSegment x = a.Segments[index];
            QrSegment y = b.Segments[index];
            if (x.Mode != y.Mode || x.CharacterCount != y.CharacterCount || x.Text != y.Text || x.EciAssignment != y.EciAssignment
                || x.Fnc1ApplicationIndicator != y.Fnc1ApplicationIndicator || x.IsHanziCompatibilityMode != y.IsHanziCompatibilityMode
                || !SameBytes(x.Data, y.Data)) { return false; }
        }
        return true;
    }

    static bool SameBytes(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) { return false; }
        for (int index = 0; index < a.Length; index++) { if (a[index] != b[index]) { return false; } }
        return true;
    }

    static bool ValidatePayloads(QrDecodeResult[] parts, int maximumAllocationBytes)
    {
        long allocation = (long)parts.Length * 1024;
        bool consistent = true;
        foreach (QrDecodeResult part in parts)
        {
            if (part.RawBytes == null || part.EciAssignments == null || part.Warnings == null
                || part.CorrectedErrorCount < 0 || part.CorrectedErasureCount < 0)
            {
                throw new QrInputException("Payload buffers and nonnegative correction counts are required.", "parts");
            }
            allocation += (long)part.RawBytes.Length * 8 + (long)part.EciAssignments.Length * 8;
            allocation += part.Text == null ? 0 : (long)part.Text.Length * 4;
            foreach (int assignment in part.EciAssignments)
            {
                if (assignment < 0 || assignment > QrLimits.EciMaximumQrAssignment) { throw new QrInputException("An ECI assignment is invalid.", "parts"); }
            }
            int offset = 0;
            foreach (QrSegment segment in part.Segments)
            {
                if (segment.Data == null || segment.CharacterCount < 0 || segment.EciAssignment < -1 || segment.EciAssignment > QrLimits.EciMaximumQrAssignment)
                {
                    throw new QrInputException("Retained segment data and metadata are invalid.", "parts");
                }
                allocation += 512 + (long)segment.Data.Length * 8 + (segment.Text == null ? 0 : (long)segment.Text.Length * 4);
                if (!DecodedPayloadBuilder.IsPayload(segment.Mode) && segment.Mode != QrEncodingMode.StructuredAppend
                    && segment.Mode != QrEncodingMode.Eci && segment.Mode != QrEncodingMode.Fnc1First && segment.Mode != QrEncodingMode.Fnc1Second) { consistent = false; }
                if (DecodedPayloadBuilder.IsPayload(segment.Mode))
                {
                    foreach (byte value in segment.Data)
                    {
                        if (offset >= part.RawBytes.Length || part.RawBytes[offset] != value) { consistent = false; }
                        if (offset < part.RawBytes.Length) { offset++; }
                    }
                }
            }
            if (part.Segments.Length > 0 && offset != part.RawBytes.Length) { consistent = false; }
            foreach (string warning in part.Warnings)
            {
                if (warning == null) { throw new QrInputException("Warning entries cannot be null.", "parts"); }
                allocation += 64 + (long)warning.Length * 4;
            }
            if (allocation > maximumAllocationBytes)
            {
                throw new QrResourceLimitException("The assembled payload exceeds the allocation limit.", "MaximumAllocationBytes",
                    allocation > int.MaxValue ? int.MaxValue : (int)allocation, maximumAllocationBytes);
            }
        }
        return consistent;
    }
}
