namespace barcode;

internal class Pdf417MetadataVoteState
{
    internal int[] Counts;
    internal int[] SeenKeys;
    internal int SeenKeyCount;
    internal int SelectedValue = -1;
    internal int SelectedCount;

    internal Pdf417MetadataVoteState(int valueCount, int maximumKeyCount)
    {
        Counts = new int[valueCount];
        SeenKeys = new int[maximumKeyCount];
    }
}

internal class Pdf417RowAssignment
{
    internal int ObservationIndex;
    internal int RowNumber;
    internal bool UsedLeftIndicator;
    internal bool UsedRightIndicator;
    internal Pdf417ScanlineRow Observation = new Pdf417ScanlineRow();
}

internal class Pdf417ReconciledRow
{
    internal int RowNumber;
    internal Pdf417ScanlineRow[] Observations = new Pdf417ScanlineRow[0];
}

internal class Pdf417ReconciledMetadata
{
    internal Pdf417Variant Variant;
    internal int RowCount;
    internal int ColumnCount;
    internal int ErrorCorrectionLevel;
    internal int RowCountVoteCount;
    internal int ColumnCountVoteCount;
    internal int ErrorCorrectionVoteCount;
    internal Pdf417RowAssignment[] Assignments = new Pdf417RowAssignment[0];
    internal Pdf417ReconciledRow[] Rows = new Pdf417ReconciledRow[0];
    internal int[] MissingRowNumbers = new int[0];
    internal int[] RejectedObservationIndices = new int[0];
}

internal static class Pdf417RowIndicatorReconciler
{
    internal const int MaximumIndicatorValue = 928;
    internal const int IndicatorRowGroupSize = 30;
    internal const int ErrorCorrectionRemainderValueCount = 27;

    internal static Pdf417ReconciledMetadata ReconcileRowMetadata(
        Pdf417ScanlineRow[] observations)
    {
        if (observations == null || observations.Length == 0)
        {
            throw new BarcodeDecodingException("PDF417 row metadata reconciliation requires at least one scanline observation.");
        }

        if (observations[0] == null)
        {
            throw new BarcodeValidationException("PDF417 scanline observations must not contain null entries.");
        }

        Pdf417Variant variant = observations[0].Variant;
        int maximumKeys = CheckedMath.Multiply(observations.Length, 3);
        Pdf417MetadataVoteState upperRowVotes = new Pdf417MetadataVoteState(
            30,
            maximumKeys);
        Pdf417MetadataVoteState columnVotes = new Pdf417MetadataVoteState(
            30,
            maximumKeys);
        Pdf417MetadataVoteState errorRemainderVotes = new Pdf417MetadataVoteState(
            ErrorCorrectionRemainderValueCount,
            maximumKeys);

        int observationIndex = 0;
        while (observationIndex < observations.Length)
        {
            Pdf417ScanlineRow observation = observations[observationIndex];
            ValidateObservation(observation, variant);
            AddVote(
                columnVotes,
                observation.DataCodewords.Length - 1,
                100000 + observationIndex);
            AddIndicatorMetadataVotes(
                observation.LeftIndicator,
                observation.ClusterIndex,
                false,
                upperRowVotes,
                columnVotes,
                errorRemainderVotes);
            if (variant == Pdf417Variant.Standard)
            {
                AddIndicatorMetadataVotes(
                    observation.RightIndicator,
                    observation.ClusterIndex,
                    true,
                    upperRowVotes,
                    columnVotes,
                    errorRemainderVotes);
            }

            observationIndex = observationIndex + 1;
        }

        int upperRows = SelectVote(
            upperRowVotes,
            "PDF417 row indicators do not resolve the upper row-count component.");
        int columnsMinusOne = SelectVote(
            columnVotes,
            "PDF417 row indicators do not resolve a column count.");
        int errorRemainder = SelectVote(
            errorRemainderVotes,
            "PDF417 row indicators do not resolve error correction and the row-count remainder.");
        int rowCount = CheckedMath.Add(
            CheckedMath.Multiply(upperRows, 3),
            (errorRemainder % 3) + 1);
        int columnCount = columnsMinusOne + 1;
        int errorCorrectionLevel = errorRemainder / 3;
        ValidateResolvedMetadata(rowCount, columnCount, errorCorrectionLevel);

        Pdf417RowAssignment[] assignmentBuffer = new Pdf417RowAssignment[observations.Length];
        int[] rejectedBuffer = new int[observations.Length];
        int assignmentCount = 0;
        int rejectedCount = 0;
        observationIndex = 0;
        while (observationIndex < observations.Length)
        {
            Pdf417ScanlineRow observation = observations[observationIndex];
            int leftRow = CalculateRowNumber(
                observation.LeftIndicator,
                observation.ClusterIndex);
            bool leftValid = IsExpectedLeftIndicator(
                observation,
                leftRow,
                rowCount,
                columnCount,
                errorCorrectionLevel);
            int rightRow = -1;
            bool rightValid = false;
            if (variant == Pdf417Variant.Standard)
            {
                rightRow = CalculateRowNumber(
                    observation.RightIndicator,
                    observation.ClusterIndex);
                rightValid = IsExpectedRightIndicator(
                    observation,
                    rightRow,
                    rowCount,
                    columnCount,
                    errorCorrectionLevel);
            }

            bool conflictingRows = leftValid
                && rightValid
                && leftRow != rightRow;
            if (observation.DataCodewords.Length != columnCount
                || conflictingRows
                || (!leftValid && !rightValid))
            {
                rejectedBuffer[rejectedCount] = observationIndex;
                rejectedCount = rejectedCount + 1;
            }
            else
            {
                Pdf417RowAssignment assignment = new Pdf417RowAssignment();
                assignment.ObservationIndex = observationIndex;
                assignment.RowNumber = leftValid ? leftRow : rightRow;
                assignment.UsedLeftIndicator = leftValid;
                assignment.UsedRightIndicator = rightValid;
                assignment.Observation = observation;
                assignmentBuffer[assignmentCount] = assignment;
                assignmentCount = assignmentCount + 1;
            }

            observationIndex = observationIndex + 1;
        }

        if (assignmentCount == 0)
        {
            throw new BarcodeDecodingException("No PDF417 scanline observation agrees with the reconciled row metadata.");
        }

        Pdf417RowAssignment[] assignments = CopyAssignmentPrefix(
            assignmentBuffer,
            assignmentCount);
        Pdf417ReconciledRow[] rows = CreateReconciledRows(
            assignments,
            rowCount);
        int[] missingRows = CreateMissingRows(rows, rowCount);

        Pdf417ReconciledMetadata metadata = new Pdf417ReconciledMetadata();
        metadata.Variant = variant;
        metadata.RowCount = rowCount;
        metadata.ColumnCount = columnCount;
        metadata.ErrorCorrectionLevel = errorCorrectionLevel;
        metadata.RowCountVoteCount = upperRowVotes.SelectedCount;
        metadata.ColumnCountVoteCount = columnVotes.SelectedCount;
        metadata.ErrorCorrectionVoteCount = errorRemainderVotes.SelectedCount;
        metadata.Assignments = assignments;
        metadata.Rows = rows;
        metadata.MissingRowNumbers = missingRows;
        metadata.RejectedObservationIndices = CopyIntegerPrefix(
            rejectedBuffer,
            rejectedCount);
        return metadata;
    }

    internal static int CalculateRowNumber(int indicator, int clusterIndex)
    {
        if (indicator < 0 || indicator > MaximumIndicatorValue)
        {
            throw new BarcodeValidationException("PDF417 row indicator must be between zero and 928.");
        }

        if (clusterIndex < 0
            || clusterIndex >= Pdf417CodewordTable.ClusterCount)
        {
            throw new BarcodeValidationException("PDF417 row-indicator cluster must be from zero through two.");
        }

        return ((indicator / IndicatorRowGroupSize) * 3) + clusterIndex;
    }

    static void AddIndicatorMetadataVotes(
        int indicator,
        int clusterIndex,
        bool isRightIndicator,
        Pdf417MetadataVoteState upperRowVotes,
        Pdf417MetadataVoteState columnVotes,
        Pdf417MetadataVoteState errorRemainderVotes)
    {
        int rowNumber = CalculateRowNumber(indicator, clusterIndex);
        if (rowNumber >= Pdf417SymbolPlanner.MaximumRows)
        {
            return;
        }

        int component = indicator % IndicatorRowGroupSize;
        int key = (((isRightIndicator ? 1 : 0) * 3) + clusterIndex)
            * (MaximumIndicatorValue + 1)
            + indicator;
        if ((!isRightIndicator && clusterIndex == 0)
            || (isRightIndicator && clusterIndex == 1))
        {
            AddVote(upperRowVotes, component, key);
        }
        else if ((!isRightIndicator && clusterIndex == 2)
            || (isRightIndicator && clusterIndex == 0))
        {
            AddVote(columnVotes, component, key);
        }
        else if (component < ErrorCorrectionRemainderValueCount)
        {
            AddVote(errorRemainderVotes, component, key);
        }
    }

    static void AddVote(
        Pdf417MetadataVoteState votes,
        int value,
        int key)
    {
        if (value < 0 || value >= votes.Counts.Length)
        {
            return;
        }

        int index = 0;
        while (index < votes.SeenKeyCount)
        {
            if (votes.SeenKeys[index] == key)
            {
                return;
            }

            index = index + 1;
        }

        if (votes.SeenKeyCount >= votes.SeenKeys.Length)
        {
            throw new BarcodeValidationException("PDF417 metadata vote capacity was exceeded.");
        }

        votes.SeenKeys[votes.SeenKeyCount] = key;
        votes.SeenKeyCount = votes.SeenKeyCount + 1;
        votes.Counts[value] = votes.Counts[value] + 1;
    }

    static int SelectVote(
        Pdf417MetadataVoteState votes,
        string failureMessage)
    {
        int selectedValue = -1;
        int selectedCount = 0;
        bool tied = false;
        int value = 0;
        while (value < votes.Counts.Length)
        {
            int count = votes.Counts[value];
            if (count > selectedCount)
            {
                selectedValue = value;
                selectedCount = count;
                tied = false;
            }
            else if (count > 0 && count == selectedCount)
            {
                tied = true;
            }

            value = value + 1;
        }

        if (selectedValue < 0 || tied)
        {
            throw new BarcodeDecodingException(failureMessage);
        }

        votes.SelectedValue = selectedValue;
        votes.SelectedCount = selectedCount;
        return selectedValue;
    }

    static bool IsExpectedLeftIndicator(
        Pdf417ScanlineRow observation,
        int rowNumber,
        int rowCount,
        int columnCount,
        int errorCorrectionLevel)
    {
        if (rowNumber < 0 || rowNumber >= rowCount)
        {
            return false;
        }

        return observation.LeftIndicator
            == Pdf417Layout.CalculateLeftRowIndicator(
                rowNumber,
                rowCount,
                columnCount,
                errorCorrectionLevel);
    }

    static bool IsExpectedRightIndicator(
        Pdf417ScanlineRow observation,
        int rowNumber,
        int rowCount,
        int columnCount,
        int errorCorrectionLevel)
    {
        if (rowNumber < 0 || rowNumber >= rowCount)
        {
            return false;
        }

        return observation.RightIndicator
            == Pdf417Layout.CalculateRightRowIndicator(
                rowNumber,
                rowCount,
                columnCount,
                errorCorrectionLevel);
    }

    static Pdf417ReconciledRow[] CreateReconciledRows(
        Pdf417RowAssignment[] assignments,
        int rowCount)
    {
        int[] observationCounts = new int[rowCount];
        int assignmentIndex = 0;
        while (assignmentIndex < assignments.Length)
        {
            int rowNumber = assignments[assignmentIndex].RowNumber;
            observationCounts[rowNumber] = observationCounts[rowNumber] + 1;
            assignmentIndex = assignmentIndex + 1;
        }

        int observedRowCount = 0;
        int rowNumberIndex = 0;
        while (rowNumberIndex < rowCount)
        {
            if (observationCounts[rowNumberIndex] > 0)
            {
                observedRowCount = observedRowCount + 1;
            }

            rowNumberIndex = rowNumberIndex + 1;
        }

        Pdf417ReconciledRow[] rows = new Pdf417ReconciledRow[observedRowCount];
        int outputIndex = 0;
        rowNumberIndex = 0;
        while (rowNumberIndex < rowCount)
        {
            if (observationCounts[rowNumberIndex] > 0)
            {
                Pdf417ReconciledRow row = new Pdf417ReconciledRow();
                row.RowNumber = rowNumberIndex;
                row.Observations = new Pdf417ScanlineRow[observationCounts[rowNumberIndex]];
                int observationIndex = 0;
                assignmentIndex = 0;
                while (assignmentIndex < assignments.Length)
                {
                    if (assignments[assignmentIndex].RowNumber == rowNumberIndex)
                    {
                        row.Observations[observationIndex] = assignments[assignmentIndex].Observation;
                        observationIndex = observationIndex + 1;
                    }

                    assignmentIndex = assignmentIndex + 1;
                }

                rows[outputIndex] = row;
                outputIndex = outputIndex + 1;
            }

            rowNumberIndex = rowNumberIndex + 1;
        }

        return rows;
    }

    static int[] CreateMissingRows(
        Pdf417ReconciledRow[] rows,
        int rowCount)
    {
        int[] buffer = new int[rowCount];
        int count = 0;
        int observedIndex = 0;
        int rowNumber = 0;
        while (rowNumber < rowCount)
        {
            if (observedIndex < rows.Length
                && rows[observedIndex].RowNumber == rowNumber)
            {
                observedIndex = observedIndex + 1;
            }
            else
            {
                buffer[count] = rowNumber;
                count = count + 1;
            }

            rowNumber = rowNumber + 1;
        }

        return CopyIntegerPrefix(buffer, count);
    }

    static void ValidateObservation(
        Pdf417ScanlineRow observation,
        Pdf417Variant expectedVariant)
    {
        if (observation == null)
        {
            throw new BarcodeValidationException("PDF417 scanline observations must not contain null entries.");
        }

        if (observation.Variant != expectedVariant
            || (observation.Variant != Pdf417Variant.Standard
                && observation.Variant != Pdf417Variant.Compact))
        {
            throw new BarcodeDecodingException("PDF417 scanline observations must use one valid row-layout variant.");
        }

        if (observation.ClusterIndex < 0
            || observation.ClusterIndex >= Pdf417CodewordTable.ClusterCount)
        {
            throw new BarcodeDecodingException("PDF417 scanline observation has an invalid cluster.");
        }

        if (observation.LeftIndicator < 0
            || observation.LeftIndicator > MaximumIndicatorValue
            || (observation.Variant == Pdf417Variant.Standard
                && (observation.RightIndicator < 0
                    || observation.RightIndicator > MaximumIndicatorValue))
            || (observation.Variant == Pdf417Variant.Compact
                && observation.RightIndicator != -1))
        {
            throw new BarcodeDecodingException("PDF417 scanline observation has an invalid row indicator.");
        }

        if (observation.DataCodewords == null
            || observation.DataCodewords.Length < Pdf417SymbolPlanner.MinimumColumns
            || observation.DataCodewords.Length > Pdf417SymbolPlanner.MaximumColumns)
        {
            throw new BarcodeDecodingException("PDF417 scanline observation has an invalid data-column count.");
        }

        int expectedCodewordCount = observation.DataCodewords.Length
            + (observation.Variant == Pdf417Variant.Standard ? 2 : 1);
        if (observation.Codewords != null && observation.Codewords.Length > 0)
        {
            if (observation.Codewords.Length != expectedCodewordCount
                || observation.Codewords[0] != observation.LeftIndicator
                || (observation.Variant == Pdf417Variant.Standard
                    && observation.Codewords[observation.Codewords.Length - 1]
                        != observation.RightIndicator))
            {
                throw new BarcodeDecodingException("PDF417 scanline codewords and row indicators are inconsistent.");
            }
        }

        if (observation.Buckets != null && observation.Buckets.Length > 0)
        {
            if (observation.Buckets.Length != expectedCodewordCount)
            {
                throw new BarcodeDecodingException("PDF417 scanline bucket count is inconsistent.");
            }

            int bucketIndex = 0;
            while (bucketIndex < observation.Buckets.Length)
            {
                if (observation.Buckets[bucketIndex]
                    != observation.ClusterIndex * 3)
                {
                    throw new BarcodeDecodingException("PDF417 scanline buckets disagree with the row cluster.");
                }

                bucketIndex = bucketIndex + 1;
            }
        }
    }

    static void ValidateResolvedMetadata(
        int rowCount,
        int columnCount,
        int errorCorrectionLevel)
    {
        if (rowCount < Pdf417SymbolPlanner.MinimumRows
            || rowCount > Pdf417SymbolPlanner.MaximumRows
            || columnCount < Pdf417SymbolPlanner.MinimumColumns
            || columnCount > Pdf417SymbolPlanner.MaximumColumns
            || errorCorrectionLevel < Pdf417ReedSolomon.MinimumErrorCorrectionLevel
            || errorCorrectionLevel > Pdf417ReedSolomon.MaximumErrorCorrectionLevel)
        {
            throw new BarcodeDecodingException("Reconciled PDF417 row metadata is outside the standard range.");
        }

        if (CheckedMath.Multiply(rowCount, columnCount)
            > Pdf417SymbolPlanner.MaximumTotalCodewordCount)
        {
            throw new BarcodeDecodingException("Reconciled PDF417 rows and columns exceed symbol capacity.");
        }
    }

    static Pdf417RowAssignment[] CopyAssignmentPrefix(
        Pdf417RowAssignment[] source,
        int count)
    {
        Pdf417RowAssignment[] copy = new Pdf417RowAssignment[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static int[] CopyIntegerPrefix(int[] source, int count)
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
}
