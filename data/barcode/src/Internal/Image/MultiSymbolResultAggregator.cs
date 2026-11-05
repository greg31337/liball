namespace barcode;

internal class MultiSymbolAggregationResult
{
    internal BarcodeDecodeResult[] Results = new BarcodeDecodeResult[0];
    internal int[] OriginalIndexes = new int[0];
    internal BarcodeDiagnostic[] Diagnostics = new BarcodeDiagnostic[0];
    internal int UniqueCount;
    internal int DuplicateCount;
    internal int ConflictCount;
    internal int DroppedDiagnosticCount;
    internal bool MaximumReached;
}

internal class ImageCandidateAggregationResult
{
    internal ImagePipelineCandidate[] Candidates = new ImagePipelineCandidate[0];
    internal int UniqueCount;
    internal int DuplicateCount;
    internal int ConflictCount;
    internal bool MaximumReached;
}

internal class MultiSymbolResultEntry
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal int OriginalIndex;
}

internal class ImageCandidateAggregationEntry
{
    internal ImagePipelineCandidate Candidate = new ImagePipelineCandidate();
    internal int OriginalIndex;
}

internal static class MultiSymbolGeometry
{
    internal const double MinimumContainedOverlap = 0.55;
    internal const double MaximumRelativeCenterOffset = 0.25;

    internal static double CalculateSimilarity(
        BarcodeQuadrilateral? first,
        BarcodeQuadrilateral? second)
    {
        return CalculateSimilarityWithScale(first, second, false);
    }

    internal static double CalculateResultSimilarity(
        BarcodeQuadrilateral? first,
        BarcodeQuadrilateral? second)
    {
        return CalculateSimilarityWithScale(first, second, true);
    }

    static double CalculateSimilarityWithScale(
        BarcodeQuadrilateral? first,
        BarcodeQuadrilateral? second,
        bool useLargerScale)
    {
        if (first == null || second == null)
        {
            return 0.0;
        }

        BarcodeRectangle firstBounds = GeometryMath.CalculateQuadrilateralBounds(first);
        BarcodeRectangle secondBounds = GeometryMath.CalculateQuadrilateralBounds(second);
        double firstCenterX = firstBounds.X + (firstBounds.Width * 0.5);
        double firstCenterY = firstBounds.Y + (firstBounds.Height * 0.5);
        double secondCenterX = secondBounds.X + (secondBounds.Width * 0.5);
        double secondCenterY = secondBounds.Y + (secondBounds.Height * 0.5);
        double widthScale = useLargerScale
            ? global::System.Math.Max(firstBounds.Width, secondBounds.Width)
            : global::System.Math.Min(firstBounds.Width, secondBounds.Width);
        double heightScale = useLargerScale
            ? global::System.Math.Max(firstBounds.Height, secondBounds.Height)
            : global::System.Math.Min(firstBounds.Height, secondBounds.Height);
        widthScale = global::System.Math.Max(GeometryMath.Epsilon, widthScale);
        heightScale = global::System.Math.Max(GeometryMath.Epsilon, heightScale);
        double offsetX = global::System.Math.Abs(firstCenterX - secondCenterX) / widthScale;
        double offsetY = global::System.Math.Abs(firstCenterY - secondCenterY) / heightScale;
        if (useLargerScale &&
            (firstBounds.Height <= 1.0 || secondBounds.Height <= 1.0) &&
            offsetX <= MaximumRelativeCenterOffset && offsetY <= 1.0)
        {
            return 1.0;
        }

        if (useLargerScale &&
            (firstBounds.Width <= 1.0 || secondBounds.Width <= 1.0) &&
            offsetY <= MaximumRelativeCenterOffset && offsetX <= 1.0)
        {
            return 1.0;
        }

        if (offsetX > MaximumRelativeCenterOffset ||
            offsetY > MaximumRelativeCenterOffset)
        {
            return 0.0;
        }

        BarcodeRectangle? intersection = GeometryMath.IntersectRectangles(
            firstBounds,
            secondBounds);
        if (intersection == null)
        {
            return 0.0;
        }

        double firstArea = firstBounds.Width * firstBounds.Height;
        double secondArea = secondBounds.Width * secondBounds.Height;
        double smallerArea = global::System.Math.Min(firstArea, secondArea);
        if (smallerArea <= GeometryMath.Epsilon)
        {
            return offsetX <= GeometryMath.Epsilon &&
                offsetY <= GeometryMath.Epsilon
                ? 1.0
                : 0.0;
        }

        double intersectionArea = intersection.Width * intersection.Height;
        double overlap = intersectionArea / smallerArea;
        return overlap >= MinimumContainedOverlap ? overlap : 0.0;
    }

    internal static bool ComesBefore(
        BarcodeQuadrilateral? first,
        int firstIndex,
        BarcodeQuadrilateral? second,
        int secondIndex)
    {
        if (first == null)
        {
            return second == null && firstIndex < secondIndex;
        }

        if (second == null)
        {
            return true;
        }

        BarcodeRectangle firstBounds = GeometryMath.CalculateQuadrilateralBounds(first);
        BarcodeRectangle secondBounds = GeometryMath.CalculateQuadrilateralBounds(second);
        double firstCenterY = firstBounds.Y + (firstBounds.Height * 0.5);
        double secondCenterY = secondBounds.Y + (secondBounds.Height * 0.5);
        double rowTolerance = global::System.Math.Min(
            firstBounds.Height,
            secondBounds.Height) * 0.5;
        if (global::System.Math.Abs(firstCenterY - secondCenterY) <= rowTolerance)
        {
            if (firstBounds.X < secondBounds.X)
            {
                return true;
            }

            if (firstBounds.X > secondBounds.X)
            {
                return false;
            }
        }

        if (firstCenterY < secondCenterY)
        {
            return true;
        }

        if (firstCenterY > secondCenterY)
        {
            return false;
        }

        return firstIndex < secondIndex;
    }
}

internal static class MultiSymbolResultAggregator
{
    internal const int DefaultMaximumResults = 64;

    internal static MultiSymbolAggregationResult Combine(
        BarcodeDecodeResult[] results,
        int maximumResults,
        int maximumDiagnostics)
    {
        if (results == null)
        {
            throw new BarcodeValidationException("Aggregation results must not be null.");
        }

        if (maximumResults < 0)
        {
            throw new BarcodeValidationException("Aggregation maximum results must not be negative.");
        }

        int limit = maximumResults == 0
            ? DefaultMaximumResults
            : maximumResults;
        BoundedDiagnosticCollector diagnostics =
            new BoundedDiagnosticCollector(maximumDiagnostics);
        global::System.Collections.Generic.List<MultiSymbolResultEntry> entries =
            new global::System.Collections.Generic.List<MultiSymbolResultEntry>();
        int duplicateCount = 0;
        int conflictCount = 0;
        int index = 0;
        while (index < results.Length)
        {
            BarcodeDecodeResult incoming = results[index];
            if (incoming == null)
            {
                throw new BarcodeValidationException("Aggregation result entries must not be null.");
            }

            int match = FindGeometryMatch(entries, incoming);
            if (match < 0)
            {
                MultiSymbolResultEntry entry = new MultiSymbolResultEntry();
                entry.Result = incoming;
                entry.OriginalIndex = index;
                entries.Add(entry);
            }
            else
            {
                MultiSymbolResultEntry existing = entries[match];
                bool duplicate = ResultsMatch(existing.Result, incoming);
                if (duplicate)
                {
                    duplicateCount = duplicateCount + 1;
                }
                else
                {
                    conflictCount = conflictCount + 1;
                }

                if (IsIncomingBetter(incoming, existing.Result))
                {
                    existing.Result = incoming;
                    existing.OriginalIndex = index;
                }
            }

            index = index + 1;
        }

        SortEntries(entries);
        int uniqueCount = entries.Count;
        int outputCount = global::System.Math.Min(uniqueCount, limit);
        BarcodeDecodeResult[] output = new BarcodeDecodeResult[outputCount];
        int[] originalIndexes = new int[outputCount];
        index = 0;
        while (index < outputCount)
        {
            output[index] = ResultCopier.CopyDecodeResult(entries[index].Result);
            originalIndexes[index] = entries[index].OriginalIndex;
            diagnostics.RecordDecodeResult(output[index]);
            index = index + 1;
        }

        MultiSymbolAggregationResult aggregation = new MultiSymbolAggregationResult();
        aggregation.Results = output;
        aggregation.OriginalIndexes = originalIndexes;
        aggregation.Diagnostics = diagnostics.CopyDiagnostics();
        aggregation.UniqueCount = uniqueCount;
        aggregation.DuplicateCount = duplicateCount;
        aggregation.ConflictCount = conflictCount;
        aggregation.DroppedDiagnosticCount = diagnostics.DroppedCount;
        aggregation.MaximumReached = uniqueCount >= limit;
        return aggregation;
    }

    static int FindGeometryMatch(
        global::System.Collections.Generic.List<MultiSymbolResultEntry> entries,
        BarcodeDecodeResult incoming)
    {
        int bestIndex = -1;
        double bestSimilarity = 0.0;
        int index = 0;
        while (index < entries.Count)
        {
            BarcodeDecodeResult existing = entries[index].Result;
            double similarity = MultiSymbolGeometry.CalculateResultSimilarity(
                existing.Location,
                incoming.Location);
            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestIndex = index;
            }
            else if (similarity == 0.0 && bestIndex < 0 &&
                existing.Location == null && incoming.Location == null &&
                ResultsMatch(existing, incoming))
            {
                bestIndex = index;
            }

            index = index + 1;
        }

        return bestIndex;
    }

    static bool ResultsMatch(
        BarcodeDecodeResult first,
        BarcodeDecodeResult second)
    {
        return first.Format == second.Format &&
            first.Dimension == second.Dimension &&
            PayloadsMatch(first.Payload, second.Payload);
    }

    static bool PayloadsMatch(BarcodePayload? first, BarcodePayload? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        return first.Text == second.Text &&
            ByteArraysMatch(first.RawBytes, second.RawBytes) &&
            first.CharacterEncoding == second.CharacterEncoding &&
            first.IsGs1 == second.IsGs1 &&
            EciSegmentsMatch(first.EciSegments, second.EciSegments) &&
            StructuredAppendMatches(first.StructuredAppend, second.StructuredAppend);
    }

    static bool ByteArraysMatch(byte[]? first, byte[]? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
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

    static bool EciSegmentsMatch(EciSegment[]? first, EciSegment[]? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
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

    static bool StructuredAppendMatches(
        StructuredAppendInfo? first,
        StructuredAppendInfo? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        return first.Format == second.Format &&
            first.Index == second.Index &&
            first.Count == second.Count &&
            first.FileIdentifier == second.FileIdentifier &&
            first.IsLastSegment == second.IsLastSegment;
    }

    static bool IsIncomingBetter(
        BarcodeDecodeResult incoming,
        BarcodeDecodeResult existing)
    {
        int incomingIntegrity = CalculateIntegrityRank(incoming.Metadata);
        int existingIntegrity = CalculateIntegrityRank(existing.Metadata);
        if (incomingIntegrity != existingIntegrity)
        {
            return incomingIntegrity > existingIntegrity;
        }

        double incomingConfidence = ReadConfidence(incoming.Metadata);
        double existingConfidence = ReadConfidence(existing.Metadata);
        if (incomingConfidence != existingConfidence)
        {
            return incomingConfidence > existingConfidence;
        }

        int incomingCorrections = ReadCorrectionCount(incoming.Metadata);
        int existingCorrections = ReadCorrectionCount(existing.Metadata);
        return incomingCorrections < existingCorrections;
    }

    static int CalculateIntegrityRank(BarcodeDecodeMetadata? metadata)
    {
        if (metadata == null || !metadata.HasChecksum)
        {
            return 1;
        }

        return metadata.ChecksumValid ? 2 : 0;
    }

    static double ReadConfidence(BarcodeDecodeMetadata? metadata)
    {
        if (metadata == null || !GeometryMath.IsFiniteValue(metadata.Confidence))
        {
            return 0.0;
        }

        if (metadata.Confidence < 0.0)
        {
            return 0.0;
        }

        return global::System.Math.Min(1.0, metadata.Confidence);
    }

    static int ReadCorrectionCount(BarcodeDecodeMetadata? metadata)
    {
        if (metadata == null)
        {
            return int.MaxValue;
        }

        if (metadata.CorrectedErrorCount < 0 || metadata.ErasureCount < 0 ||
            metadata.CorrectedErrorCount > int.MaxValue - metadata.ErasureCount)
        {
            return int.MaxValue;
        }

        return metadata.CorrectedErrorCount + metadata.ErasureCount;
    }

    static void SortEntries(
        global::System.Collections.Generic.List<MultiSymbolResultEntry> entries)
    {
        int index = 1;
        while (index < entries.Count)
        {
            MultiSymbolResultEntry current = entries[index];
            int insertion = index;
            while (insertion > 0 && MultiSymbolGeometry.ComesBefore(
                current.Result.Location,
                current.OriginalIndex,
                entries[insertion - 1].Result.Location,
                entries[insertion - 1].OriginalIndex))
            {
                entries[insertion] = entries[insertion - 1];
                insertion = insertion - 1;
            }

            entries[insertion] = current;
            index = index + 1;
        }
    }
}

internal static class ImageCandidateAggregator
{
    internal static ImageCandidateAggregationResult Combine(
        ImagePipelineCandidate[] candidates,
        int maximumCandidates)
    {
        if (candidates == null)
        {
            throw new BarcodeValidationException("Candidate aggregation input must not be null.");
        }

        if (maximumCandidates <= 0)
        {
            throw new BarcodeValidationException("Candidate aggregation maximum must be positive.");
        }

        global::System.Collections.Generic.List<ImageCandidateAggregationEntry> entries =
            new global::System.Collections.Generic.List<ImageCandidateAggregationEntry>();
        int duplicateCount = 0;
        int conflictCount = 0;
        int index = 0;
        while (index < candidates.Length)
        {
            ImagePipelineCandidate incoming = candidates[index];
            if (incoming == null)
            {
                throw new BarcodeValidationException("Candidate aggregation entries must not be null.");
            }

            int match = FindCandidateMatch(entries, incoming);
            if (match < 0)
            {
                ImageCandidateAggregationEntry entry = new ImageCandidateAggregationEntry();
                entry.Candidate = incoming;
                entry.OriginalIndex = index;
                entries.Add(entry);
            }
            else
            {
                ImageCandidateAggregationEntry existing = entries[match];
                if (CandidatesMatch(existing.Candidate, incoming))
                {
                    duplicateCount = duplicateCount + 1;
                }
                else
                {
                    conflictCount = conflictCount + 1;
                }

                if (incoming.Score.Total > existing.Candidate.Score.Total)
                {
                    existing.Candidate = incoming;
                    existing.OriginalIndex = index;
                }
            }

            index = index + 1;
        }

        SortCandidateEntries(entries);
        int uniqueCount = entries.Count;
        int outputCount = global::System.Math.Min(uniqueCount, maximumCandidates);
        ImagePipelineCandidate[] output = new ImagePipelineCandidate[outputCount];
        index = 0;
        while (index < outputCount)
        {
            output[index] = entries[index].Candidate;
            index = index + 1;
        }

        ImageCandidateAggregationResult aggregation =
            new ImageCandidateAggregationResult();
        aggregation.Candidates = output;
        aggregation.UniqueCount = uniqueCount;
        aggregation.DuplicateCount = duplicateCount;
        aggregation.ConflictCount = conflictCount;
        aggregation.MaximumReached = uniqueCount >= maximumCandidates;
        return aggregation;
    }

    static int FindCandidateMatch(
        global::System.Collections.Generic.List<ImageCandidateAggregationEntry> entries,
        ImagePipelineCandidate incoming)
    {
        int bestIndex = -1;
        double bestSimilarity = 0.0;
        int index = 0;
        while (index < entries.Count)
        {
            double similarity = MultiSymbolGeometry.CalculateSimilarity(
                entries[index].Candidate.Localization.Candidate.Location,
                incoming.Localization.Candidate.Location);
            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestIndex = index;
            }

            index = index + 1;
        }

        return bestIndex;
    }

    static bool CandidatesMatch(
        ImagePipelineCandidate first,
        ImagePipelineCandidate second)
    {
        return first.Localization.Candidate.Format ==
                second.Localization.Candidate.Format &&
            first.Localization.Kind == second.Localization.Kind;
    }

    static void SortCandidateEntries(
        global::System.Collections.Generic.List<ImageCandidateAggregationEntry> entries)
    {
        int index = 1;
        while (index < entries.Count)
        {
            ImageCandidateAggregationEntry current = entries[index];
            int insertion = index;
            while (insertion > 0 && MultiSymbolGeometry.ComesBefore(
                current.Candidate.Localization.Candidate.Location,
                current.OriginalIndex,
                entries[insertion - 1].Candidate.Localization.Candidate.Location,
                entries[insertion - 1].OriginalIndex))
            {
                entries[insertion] = entries[insertion - 1];
                insertion = insertion - 1;
            }

            entries[insertion] = current;
            index = index + 1;
        }
    }
}
