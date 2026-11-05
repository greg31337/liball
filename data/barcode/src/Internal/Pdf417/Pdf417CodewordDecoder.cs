namespace barcode;

internal class Pdf417CodewordCandidateSet
{
    internal int CodewordIndex;
    internal int[] Values = new int[0];
    internal int[] VoteCounts = new int[0];
    internal int Count;
}

internal class Pdf417CandidateCollection
{
    internal Pdf417CodewordCandidateSet[] CandidateSets =
        new Pdf417CodewordCandidateSet[0];
    internal int[] ErasureIndices = new int[0];
    internal int[] AmbiguousIndices = new int[0];
}

internal class Pdf417ResolvedCodewords
{
    internal int[] ReceivedCodewords = new int[0];
    internal int[] CorrectedCodewords = new int[0];
    internal int[] SelectedCandidateIndices = new int[0];
    internal Pdf417CorrectionResult Correction = new Pdf417CorrectionResult(
        false,
        new int[0],
        0,
        0,
        new int[0],
        null,
        null);
}

internal class Pdf417CodewordDecodeResult
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal Pdf417ReconciledMetadata RowMetadata =
        new Pdf417ReconciledMetadata();
    internal Pdf417CorrectionResult Correction = new Pdf417CorrectionResult(
        false,
        new int[0],
        0,
        0,
        new int[0],
        null,
        null);
    internal Pdf417HighLevelDecodeResult HighLevel =
        new Pdf417HighLevelDecodeResult();
    internal Pdf417MacroBlock? MacroBlock;
    internal int[] ReceivedCodewords = new int[0];
    internal int[] CorrectedCodewords = new int[0];
    internal int[] DataCodewords = new int[0];
    internal int[] HighLevelCodewords = new int[0];
    internal int[] PayloadCodewords = new int[0];
    internal int[] ErasureIndices = new int[0];
    internal int[] AmbiguousIndices = new int[0];
    internal int[] SelectedCandidateIndices = new int[0];
}

internal static class Pdf417CodewordDecoder
{
    internal const int MaximumAmbiguityAttempts = 100000;

    internal static Pdf417CodewordDecodeResult DecodeRows(
        Pdf417ScanlineRow[] observations)
    {
        Pdf417ReconciledMetadata metadata =
            Pdf417RowIndicatorReconciler.ReconcileRowMetadata(observations);
        return DecodeReconciledRows(metadata);
    }

    internal static Pdf417CodewordDecodeResult DecodeReconciledRows(
        Pdf417ReconciledMetadata metadata)
    {
        ValidateReconciledInput(metadata);
        Pdf417CandidateCollection candidates = CollectCodewordCandidates(
            metadata);
        Pdf417ResolvedCodewords resolved = ResolveCodewordCandidates(
            candidates,
            metadata.ErrorCorrectionLevel);
        return CreateCodewordDecodeResult(metadata, candidates, resolved);
    }

    static Pdf417CandidateCollection CollectCodewordCandidates(
        Pdf417ReconciledMetadata metadata)
    {
        int totalCodewordCount = CheckedMath.Multiply(
            metadata.RowCount,
            metadata.ColumnCount);
        Pdf417CodewordCandidateSet[] sets =
            new Pdf417CodewordCandidateSet[totalCodewordCount];
        int index = 0;
        while (index < sets.Length)
        {
            Pdf417CodewordCandidateSet set = new Pdf417CodewordCandidateSet();
            set.CodewordIndex = index;
            set.Values = new int[metadata.Assignments.Length];
            set.VoteCounts = new int[metadata.Assignments.Length];
            sets[index] = set;
            index = index + 1;
        }

        int rowIndex = 0;
        while (rowIndex < metadata.Rows.Length)
        {
            Pdf417ReconciledRow row = metadata.Rows[rowIndex];
            int observationIndex = 0;
            while (observationIndex < row.Observations.Length)
            {
                Pdf417ScanlineRow observation = row.Observations[observationIndex];
                int column = 0;
                while (column < metadata.ColumnCount)
                {
                    int value = observation.DataCodewords[column];
                    if (value >= 0
                        && value < Pdf417CodewordTable.CodewordCount)
                    {
                        int codewordIndex = (row.RowNumber * metadata.ColumnCount)
                            + column;
                        AddCodewordCandidate(sets[codewordIndex], value);
                    }

                    column = column + 1;
                }

                observationIndex = observationIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }

        int[] erasureBuffer = new int[totalCodewordCount];
        int[] ambiguityBuffer = new int[totalCodewordCount];
        int erasureCount = 0;
        int ambiguityCount = 0;
        index = 0;
        while (index < sets.Length)
        {
            SortCodewordCandidates(sets[index]);
            if (sets[index].Count == 0)
            {
                erasureBuffer[erasureCount] = index;
                erasureCount = erasureCount + 1;
            }
            else if (sets[index].Count > 1)
            {
                ambiguityBuffer[ambiguityCount] = index;
                ambiguityCount = ambiguityCount + 1;
            }

            index = index + 1;
        }

        Pdf417CandidateCollection collection = new Pdf417CandidateCollection();
        collection.CandidateSets = sets;
        collection.ErasureIndices = CopyCandidateIntegerPrefix(
            erasureBuffer,
            erasureCount);
        collection.AmbiguousIndices = CopyCandidateIntegerPrefix(
            ambiguityBuffer,
            ambiguityCount);
        return collection;
    }

    static void AddCodewordCandidate(
        Pdf417CodewordCandidateSet set,
        int value)
    {
        int index = 0;
        while (index < set.Count)
        {
            if (set.Values[index] == value)
            {
                set.VoteCounts[index] = set.VoteCounts[index] + 1;
                return;
            }

            index = index + 1;
        }

        if (set.Count >= set.Values.Length)
        {
            throw new BarcodeDecodingException("PDF417 codeword candidate capacity was exceeded.");
        }

        set.Values[set.Count] = value;
        set.VoteCounts[set.Count] = 1;
        set.Count = set.Count + 1;
    }

    static void SortCodewordCandidates(Pdf417CodewordCandidateSet set)
    {
        int index = 1;
        while (index < set.Count)
        {
            int value = set.Values[index];
            int votes = set.VoteCounts[index];
            int destination = index;
            while (destination > 0
                && (set.VoteCounts[destination - 1] < votes
                    || (set.VoteCounts[destination - 1] == votes
                        && set.Values[destination - 1] > value)))
            {
                set.Values[destination] = set.Values[destination - 1];
                set.VoteCounts[destination] = set.VoteCounts[destination - 1];
                destination = destination - 1;
            }

            set.Values[destination] = value;
            set.VoteCounts[destination] = votes;
            index = index + 1;
        }
    }

    static Pdf417ResolvedCodewords ResolveCodewordCandidates(
        Pdf417CandidateCollection candidates,
        int errorCorrectionLevel)
    {
        int[] choices = new int[candidates.AmbiguousIndices.Length];
        int attempt = 0;
        bool hasMore = true;
        Pdf417CorrectionResult? lastCorrection = null;
        while (hasMore && attempt < MaximumAmbiguityAttempts)
        {
            int[] received = ApplyCandidateChoices(candidates, choices);
            Pdf417CorrectionResult correction =
                Pdf417ReedSolomon.CorrectCodewords(
                    received,
                    errorCorrectionLevel,
                    candidates.ErasureIndices);
            lastCorrection = correction;
            if (correction.Success
                && HasValidCodewordEnvelope(
                    correction.CorrectedCodewords,
                    errorCorrectionLevel))
            {
                Pdf417ResolvedCodewords resolved =
                    new Pdf417ResolvedCodewords();
                resolved.ReceivedCodewords = received;
                resolved.CorrectedCodewords = CopyResolvedIntegers(
                    correction.CorrectedCodewords);
                resolved.SelectedCandidateIndices = CopyResolvedIntegers(
                    choices);
                resolved.Correction = correction;
                return resolved;
            }

            hasMore = AdvanceCandidateChoices(candidates, choices);
            attempt = attempt + 1;
        }

        string message = lastCorrection == null
            || lastCorrection.FailureMessage == null
            ? "PDF417 ambiguity resolution did not find a valid error-corrected codeword block."
            : lastCorrection.FailureMessage;
        throw new BarcodeDecodingException(message);
    }

    static int[] ApplyCandidateChoices(
        Pdf417CandidateCollection candidates,
        int[] choices)
    {
        int[] received = new int[candidates.CandidateSets.Length];
        int index = 0;
        while (index < candidates.CandidateSets.Length)
        {
            Pdf417CodewordCandidateSet set = candidates.CandidateSets[index];
            if (set.Count > 0)
            {
                received[index] = set.Values[0];
            }

            index = index + 1;
        }

        index = 0;
        while (index < candidates.AmbiguousIndices.Length)
        {
            int codewordIndex = candidates.AmbiguousIndices[index];
            Pdf417CodewordCandidateSet set =
                candidates.CandidateSets[codewordIndex];
            received[codewordIndex] = set.Values[choices[index]];
            index = index + 1;
        }

        return received;
    }

    static bool AdvanceCandidateChoices(
        Pdf417CandidateCollection candidates,
        int[] choices)
    {
        int index = choices.Length - 1;
        while (index >= 0)
        {
            int codewordIndex = candidates.AmbiguousIndices[index];
            choices[index] = choices[index] + 1;
            if (choices[index]
                < candidates.CandidateSets[codewordIndex].Count)
            {
                return true;
            }

            choices[index] = 0;
            index = index - 1;
        }

        return false;
    }

    static bool HasValidCodewordEnvelope(
        int[] correctedCodewords,
        int errorCorrectionLevel)
    {
        int errorCorrectionCount =
            Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(
                errorCorrectionLevel);
        int dataCapacity = correctedCodewords.Length - errorCorrectionCount;
        int lengthDescriptor = correctedCodewords[0];
        if (lengthDescriptor != dataCapacity)
        {
            return false;
        }

        return true;
    }

    static Pdf417CodewordDecodeResult CreateCodewordDecodeResult(
        Pdf417ReconciledMetadata rowMetadata,
        Pdf417CandidateCollection candidates,
        Pdf417ResolvedCodewords resolved)
    {
        int errorCorrectionCount =
            Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(
                rowMetadata.ErrorCorrectionLevel);
        int dataCapacity = resolved.CorrectedCodewords.Length
            - errorCorrectionCount;
        int lengthDescriptor = resolved.CorrectedCodewords[0];
        int[] dataCodewords = CopyResolvedRange(
            resolved.CorrectedCodewords,
            0,
            dataCapacity);
        int highLevelCount = lengthDescriptor - 1;
        while (highLevelCount > 0
            && resolved.CorrectedCodewords[highLevelCount]
                == Pdf417SymbolPlanner.PaddingCodeword)
        {
            highLevelCount = highLevelCount - 1;
        }

        if (highLevelCount == 0)
        {
            throw new BarcodeDecodingException("PDF417 payload contains no compaction data.");
        }

        int[] highLevelCodewords = CopyResolvedRange(
            resolved.CorrectedCodewords,
            1,
            highLevelCount);
        int macroStart = FindMacroCodewordStart(highLevelCodewords);
        int payloadCount = macroStart < 0
            ? highLevelCodewords.Length
            : macroStart;
        if (payloadCount == 0)
        {
            throw new BarcodeDecodingException("PDF417 payload contains no compaction data.");
        }

        int[] payloadCodewords = CopyResolvedRange(
            highLevelCodewords,
            0,
            payloadCount);
        Pdf417HighLevelDecodeResult highLevel =
            Pdf417HighLevelDecoder.DecodeHighLevelCodewords(
                payloadCodewords);
        Pdf417MacroBlock? macroBlock = null;
        if (macroStart >= 0)
        {
            int[] macroCodewords = CopyResolvedRange(
                highLevelCodewords,
                macroStart,
                highLevelCodewords.Length - macroStart);
            macroBlock = Pdf417MacroCodec.DecodeControlBlock(macroCodewords);
        }

        BarcodeFormat format = macroBlock != null
            ? BarcodeFormat.MacroPdf417
            : rowMetadata.Variant == Pdf417Variant.Compact
                ? BarcodeFormat.CompactPdf417
                : BarcodeFormat.Pdf417;
        BarcodePayload payload = new BarcodePayload();
        payload.RawBytes = CopyResolvedBytes(highLevel.RawBytes);
        payload.Text = highLevel.Text;
        payload.CharacterEncoding = highLevel.CharacterEncoding;
        payload.EciSegments = CopyResolvedEciSegments(highLevel.EciSegments);
        if (macroBlock != null)
        {
            payload.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(
                macroBlock.Macro);
        }

        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        if (macroBlock != null && macroBlock.HasFileChecksum)
        {
            metadata.HasChecksum = true;
            metadata.ChecksumValue = macroBlock.FileChecksum;
        }

        metadata.ErrorCorrectionApplied = resolved.Correction.CorrectedErrorCount > 0
            || resolved.Correction.ErasureCount > 0
            || HasAlternateCandidateChoice(
                resolved.SelectedCandidateIndices);
        metadata.CorrectedErrorCount = resolved.Correction.CorrectedErrorCount;
        metadata.ErasureCount = resolved.Correction.ErasureCount;
        metadata.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Pdf417;
        metadata.Codewords = CopyResolvedIntegers(
            resolved.CorrectedCodewords);
        metadata.Confidence = CalculateDecodeConfidence(
            resolved.Correction,
            errorCorrectionCount);
        metadata.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(
            payload.StructuredAppend);

        BarcodeDecodeResult publicResult = new BarcodeDecodeResult();
        publicResult.Payload = payload;
        publicResult.Format = format;
        publicResult.Dimension = BarcodeDimension.StackedLinear;
        publicResult.Rotation = BarcodeRotation.None;
        publicResult.Polarity = BarcodePolarity.DarkOnLight;
        publicResult.MirrorMode = BarcodeMirrorMode.Normal;
        publicResult.Metadata = metadata;
        publicResult.Diagnostics = new BarcodeDiagnostic[0];

        Pdf417CodewordDecodeResult result =
            new Pdf417CodewordDecodeResult();
        result.Result = publicResult;
        result.RowMetadata = rowMetadata;
        result.Correction = resolved.Correction;
        result.HighLevel = highLevel;
        result.MacroBlock = macroBlock;
        result.ReceivedCodewords = CopyResolvedIntegers(
            resolved.ReceivedCodewords);
        result.CorrectedCodewords = CopyResolvedIntegers(
            resolved.CorrectedCodewords);
        result.DataCodewords = dataCodewords;
        result.HighLevelCodewords = highLevelCodewords;
        result.PayloadCodewords = payloadCodewords;
        result.ErasureIndices = CopyResolvedIntegers(
            candidates.ErasureIndices);
        result.AmbiguousIndices = CopyResolvedIntegers(
            candidates.AmbiguousIndices);
        result.SelectedCandidateIndices = CopyResolvedIntegers(
            resolved.SelectedCandidateIndices);
        return result;
    }

    static int FindMacroCodewordStart(int[] codewords)
    {
        int index = 0;
        while (index < codewords.Length)
        {
            if (codewords[index] == Pdf417MacroCodec.MacroMarkerCodeword)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool HasAlternateCandidateChoice(int[] choices)
    {
        int index = 0;
        while (index < choices.Length)
        {
            if (choices[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static double CalculateDecodeConfidence(
        Pdf417CorrectionResult correction,
        int errorCorrectionCount)
    {
        int unknownErrorCount = correction.CorrectedErrorCount
            - correction.ErasureCount;
        if (unknownErrorCount < 0)
        {
            unknownErrorCount = 0;
        }

        int burden = (unknownErrorCount * 2) + correction.ErasureCount;
        double confidence = 1.0
            - ((double)burden / (errorCorrectionCount + 1));
        if (confidence < 0.0)
        {
            return 0.0;
        }

        return confidence;
    }

    static int[] CopyCandidateIntegerPrefix(int[] source, int count)
    {
        int[] copy = new int[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static int[] CopyResolvedIntegers(int[] source)
    {
        int[] copy = new int[source.Length];
        int index = 0;
        while (index < copy.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static int[] CopyResolvedRange(
        int[] source,
        int startIndex,
        int count)
    {
        int[] copy = new int[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[startIndex + index];
            index = index + 1;
        }

        return copy;
    }

    static byte[] CopyResolvedBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < copy.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static EciSegment[] CopyResolvedEciSegments(EciSegment[] source)
    {
        EciSegment[] copy = new EciSegment[source.Length];
        int index = 0;
        while (index < copy.Length)
        {
            EciSegment segment = new EciSegment();
            segment.AssignmentNumber = source[index].AssignmentNumber;
            segment.ByteOffset = source[index].ByteOffset;
            segment.ByteCount = source[index].ByteCount;
            copy[index] = segment;
            index = index + 1;
        }

        return copy;
    }

    static void ValidateReconciledInput(Pdf417ReconciledMetadata metadata)
    {
        if (metadata == null)
        {
            throw new BarcodeValidationException("PDF417 reconciled row metadata must not be null.");
        }

        if (metadata.RowCount < Pdf417SymbolPlanner.MinimumRows
            || metadata.RowCount > Pdf417SymbolPlanner.MaximumRows
            || metadata.ColumnCount < Pdf417SymbolPlanner.MinimumColumns
            || metadata.ColumnCount > Pdf417SymbolPlanner.MaximumColumns
            || metadata.ErrorCorrectionLevel
                < Pdf417ReedSolomon.MinimumErrorCorrectionLevel
            || metadata.ErrorCorrectionLevel
                > Pdf417ReedSolomon.MaximumErrorCorrectionLevel
            || metadata.Rows == null
            || metadata.Assignments == null)
        {
            throw new BarcodeValidationException("PDF417 reconciled row metadata is incomplete or outside the standard range.");
        }

        int totalCodewordCount = CheckedMath.Multiply(
            metadata.RowCount,
            metadata.ColumnCount);
        int errorCorrectionCount =
            Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(
                metadata.ErrorCorrectionLevel);
        if (totalCodewordCount > Pdf417SymbolPlanner.MaximumTotalCodewordCount
            || totalCodewordCount <= errorCorrectionCount)
        {
            throw new BarcodeValidationException("PDF417 reconciled dimensions cannot contain the reported error-correction level.");
        }

        bool[] seenRows = new bool[metadata.RowCount];
        int rowIndex = 0;
        while (rowIndex < metadata.Rows.Length)
        {
            Pdf417ReconciledRow row = metadata.Rows[rowIndex];
            if (row == null
                || row.RowNumber < 0
                || row.RowNumber >= metadata.RowCount
                || seenRows[row.RowNumber]
                || row.Observations == null
                || row.Observations.Length == 0)
            {
                throw new BarcodeValidationException("PDF417 reconciled rows contain invalid or duplicate entries.");
            }

            seenRows[row.RowNumber] = true;
            int observationIndex = 0;
            while (observationIndex < row.Observations.Length)
            {
                Pdf417ScanlineRow observation = row.Observations[observationIndex];
                if (observation == null
                    || observation.DataCodewords == null
                    || observation.DataCodewords.Length
                        != metadata.ColumnCount)
                {
                    throw new BarcodeValidationException("PDF417 reconciled observations do not match the resolved column count.");
                }

                observationIndex = observationIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }
    }
}
