namespace qrcode;

internal class StructuredAppendGrouper
{
    // Exact, ordinal identity: missing application identity is not a wildcard.
    // Groups and parts retain arrival order. References to decoded parts are
    // borrowed; this stage never edits, deduplicates, or concatenates payloads.
    internal static StructuredAppendGroupingResult Group(QrDecodeResult[] parts, int maximumResults, int maximumAllocationBytes)
    {
        ValidateCollection(parts, maximumResults, maximumAllocationBytes);
        global::System.Collections.Generic.Dictionary<string, StructuredAppendGroup> byIdentity =
            new global::System.Collections.Generic.Dictionary<string, StructuredAppendGroup>(global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.List<StructuredAppendGroup> groups = new global::System.Collections.Generic.List<StructuredAppendGroup>();
        global::System.Collections.Generic.List<QrDecodeResult> ordinary = new global::System.Collections.Generic.List<QrDecodeResult>();
        global::System.Collections.Generic.List<QrDecodeResult> rejected = new global::System.Collections.Generic.List<QrDecodeResult>();
        StructuredAppendGroupingResult result = new StructuredAppendGroupingResult();
        foreach (QrDecodeResult part in parts)
        {
            if (!HasConsistentMetadata(part))
            {
                rejected.Add(part);
                result.FailureReason = QrFailureReason.ConflictingSequence;
                continue;
            }
            QrStructuredAppendInfo? info = part.StructuredAppend;
            if (info == null)
            {
                ordinary.Add(part);
                continue;
            }

            string application = info.ApplicationIdentifier == null ? "N" : "S" + info.ApplicationIdentifier;
            string key = info.Parity.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + ":"
                + info.Total.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + ":" + application;
            StructuredAppendGroup group;
            if (byIdentity.ContainsKey(key))
            {
                group = byIdentity[key];
            }
            else
            {
                group = new StructuredAppendGroup
                {
                    Parity = info.Parity, Total = info.Total, ApplicationIdentifier = info.ApplicationIdentifier,
                    UsesFnc1First = part.UsesFnc1First, Fnc1SecondApplicationIndicator = part.Fnc1SecondApplicationIndicator
                };
                byIdentity.Add(key, group);
                groups.Add(group);
            }
            if (group.UsesFnc1First != part.UsesFnc1First || group.Fnc1SecondApplicationIndicator != part.Fnc1SecondApplicationIndicator)
            {
                group.FailureReason = QrFailureReason.ConflictingSequence;
                result.FailureReason = QrFailureReason.ConflictingSequence;
            }
            group.PendingParts.Add(part);
        }
        foreach (StructuredAppendGroup group in groups)
        {
            group.Parts = group.PendingParts.ToArray();
            group.PendingParts.Clear();
        }
        result.Groups = groups.ToArray();
        result.OrdinaryResults = ordinary.ToArray();
        result.RejectedResults = rejected.ToArray();
        return result;
    }

    // A sequence assembler must not silently select one identity from a mixed
    // collection. Completeness and duplicate equivalence are later stages.
    internal static StructuredAppendGroup RequireSingleGroup(QrDecodeResult[] parts, int maximumResults, int maximumAllocationBytes)
    {
        StructuredAppendGroupingResult result = Group(parts, maximumResults, maximumAllocationBytes);
        if (parts.Length == 0) { throw new QrInputException("At least one sequence part is required.", "parts"); }
        if (result.FailureReason == QrFailureReason.None && result.Groups.Length == 1 && result.OrdinaryResults.Length == 0)
        {
            return result.Groups[0];
        }
        int parity = -1;
        int total = 0;
        int indices = 0;
        QrStructuredAppendInfo? first = parts[0].StructuredAppend;
        if (first != null && first.Parity >= 0 && first.Parity <= 255 && first.Total >= 2 && first.Total <= 16)
        {
            parity = first.Parity;
            total = first.Total;
        }
        foreach (QrDecodeResult part in parts)
        {
            QrStructuredAppendInfo? info = part.StructuredAppend;
            if (info == null || info.Parity != parity) { parity = -1; }
            if (info != null && info.Total == total && info.Index >= 0 && info.Index < total && HasConsistentMetadata(part))
            {
                indices |= 1 << info.Index;
            }
        }
        int distinct = 0;
        for (int index = 0; index < 16; index++) { if ((indices & (1 << index)) != 0) { distinct++; } }
        throw new QrStructuredAppendException("The parts do not identify one compatible structured-append sequence.",
            QrFailureReason.ConflictingSequence, total, distinct, parity);
    }

    static bool HasConsistentMetadata(QrDecodeResult part)
    {
        QrStructuredAppendInfo? info = part.StructuredAppend;
        if (info != null)
        {
            if (info.Total < QrLimits.StructuredAppendMinimumSymbols || info.Total > QrLimits.StructuredAppendMaximumSymbols
                || info.Index < 0 || info.Index >= info.Total || info.Parity < 0 || info.Parity > QrLimits.StructuredAppendMaximumParity
                || part.Symbol != null && part.Symbol.SymbolKind != QrSymbolKind.Model2
                || part.Fnc1SecondApplicationIndicator < -1
                || part.Fnc1SecondApplicationIndicator >= 0 && (!Fnc1Encoder.IsValidSecondPositionIndicator(part.Fnc1SecondApplicationIndicator) || part.UsesFnc1First))
            {
                return false;
            }
        }
        bool header = false;
        bool fnc1First = false;
        int fnc1Second = -1;
        for (int index = 0; index < part.Segments.Length; index++)
        {
            QrSegment segment = part.Segments[index];
            if (segment.Mode == QrEncodingMode.StructuredAppend)
            {
                QrStructuredAppendInfo? copy = segment.StructuredAppend;
                if (index != 0 || info == null || copy == null || copy.Index != info.Index || copy.Total != info.Total || copy.Parity != info.Parity) { return false; }
                header = true;
            }
            if (info != null && (segment.Mode == QrEncodingMode.Fnc1First || segment.Mode == QrEncodingMode.Fnc1Second))
            {
                if (index != 1 || fnc1First || fnc1Second >= 0
                    || segment.Mode == QrEncodingMode.Fnc1Second && !Fnc1Encoder.IsValidSecondPositionIndicator(segment.Fnc1ApplicationIndicator)) { return false; }
                fnc1First = segment.Mode == QrEncodingMode.Fnc1First;
                if (!fnc1First) { fnc1Second = segment.Fnc1ApplicationIndicator; }
            }
        }
        // Metadata-only results are permitted when callers did not retain segments.
        // ApplicationIdentifier is supplied out of band, not encoded in the header.
        return info == null || part.Segments.Length == 0
            || header && fnc1First == part.UsesFnc1First && fnc1Second == part.Fnc1SecondApplicationIndicator;
    }

    static void ValidateCollection(QrDecodeResult[] parts, int maximumResults, int maximumAllocationBytes)
    {
        if (parts == null || maximumResults <= 0 || maximumAllocationBytes <= 0)
        {
            throw new QrInputException("Parts and positive grouping limits are required.", "parts");
        }
        if (parts.Length > maximumResults)
        {
            throw new QrResourceLimitException("Too many decoded results were supplied for grouping.", "MaximumResults", parts.Length, maximumResults);
        }
        int allocation = CheckedDimensions.CalculateAllocationBytes(parts.Length, 512, maximumAllocationBytes);
        foreach (QrDecodeResult part in parts)
        {
            if (part == null || part.Segments == null) { throw new QrInputException("Each result requires a segment array.", "parts"); }
            int segmentWork = CheckedDimensions.CalculateAllocationBytes(part.Segments.Length, 16, maximumAllocationBytes);
            allocation = CheckedDimensions.CalculateSum(allocation, segmentWork, maximumAllocationBytes, "MaximumAllocationBytes");
            foreach (QrSegment segment in part.Segments)
            {
                if (segment == null) { throw new QrInputException("A retained segment cannot be null.", "parts"); }
            }
            string? application = part.StructuredAppend?.ApplicationIdentifier;
            if (application != null)
            {
                int bytes = CheckedDimensions.CalculateAllocationBytes(application.Length, 4, maximumAllocationBytes);
                allocation = CheckedDimensions.CalculateSum(allocation, bytes, maximumAllocationBytes, "MaximumAllocationBytes");
            }
        }
    }
}
