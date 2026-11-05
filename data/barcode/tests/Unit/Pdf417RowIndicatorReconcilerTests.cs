namespace barcode.Tests;

public class Pdf417RowIndicatorReconcilerTests
{
    [Test]
    public void StandardRowsReconcileOutOfOrderMissingAndDuplicateScanlines()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Standard);
        int[] rowNumbers = new int[] { 4, 0, 1, 4, 8 };
        Pdf417ScanlineRow[] observations = DecodeRows(encoded, rowNumbers);

        Pdf417ReconciledMetadata metadata =
            Pdf417RowIndicatorReconciler.ReconcileRowMetadata(observations);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Variant, Is.EqualTo(Pdf417Variant.Standard));
            Assert.That(metadata.RowCount, Is.EqualTo(9));
            Assert.That(metadata.ColumnCount, Is.EqualTo(4));
            Assert.That(metadata.ErrorCorrectionLevel, Is.EqualTo(4));
            Assert.That(metadata.Assignments.Select(item => item.RowNumber),
                Is.EqualTo(rowNumbers));
            Assert.That(metadata.Rows.Select(item => item.RowNumber),
                Is.EqualTo(new int[] { 0, 1, 4, 8 }));
            Assert.That(metadata.Rows.Single(item => item.RowNumber == 4).Observations,
                Has.Length.EqualTo(2));
            Assert.That(metadata.MissingRowNumbers,
                Is.EqualTo(new int[] { 2, 3, 5, 6, 7 }));
            Assert.That(metadata.RejectedObservationIndices, Is.Empty);
            Assert.That(metadata.RowCountVoteCount, Is.GreaterThan(0));
            Assert.That(metadata.ColumnCountVoteCount, Is.GreaterThan(0));
            Assert.That(metadata.ErrorCorrectionVoteCount, Is.GreaterThan(0));
        });
    }

    [Test]
    public void CompactRowsReconcileWithOneObservedRowFromEachCluster()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Compact);
        int[] rowNumbers = new int[] { 8, 0, 4 };
        Pdf417ScanlineRow[] observations = DecodeRows(encoded, rowNumbers);

        Pdf417ReconciledMetadata metadata =
            Pdf417RowIndicatorReconciler.ReconcileRowMetadata(observations);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Variant, Is.EqualTo(Pdf417Variant.Compact));
            Assert.That(metadata.RowCount, Is.EqualTo(9));
            Assert.That(metadata.ColumnCount, Is.EqualTo(4));
            Assert.That(metadata.ErrorCorrectionLevel, Is.EqualTo(4));
            Assert.That(metadata.Assignments.Select(item => item.RowNumber),
                Is.EqualTo(rowNumbers));
            Assert.That(metadata.MissingRowNumbers,
                Is.EqualTo(new int[] { 1, 2, 3, 5, 6, 7 }));
            Assert.That(metadata.Assignments, Has.All.Matches<Pdf417RowAssignment>(
                item => item.UsedLeftIndicator && !item.UsedRightIndicator));
        });
    }

    [Test]
    public void ACorruptStandardIndicatorDoesNotDiscardAValidOppositeIndicator()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Standard);
        Pdf417ScanlineRow[] observations = DecodeRows(
            encoded,
            new int[] { 0, 1, 2, 4 });
        observations[1] = CloneRow(observations[1]);
        observations[1].LeftIndicator = observations[1].LeftIndicator - 3;
        observations[1].Codewords[0] = observations[1].LeftIndicator;

        Pdf417ReconciledMetadata metadata =
            Pdf417RowIndicatorReconciler.ReconcileRowMetadata(observations);
        Pdf417RowAssignment assignment = metadata.Assignments.Single(
            item => item.ObservationIndex == 1);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.ErrorCorrectionLevel, Is.EqualTo(4));
            Assert.That(assignment.RowNumber, Is.EqualTo(1));
            Assert.That(assignment.UsedLeftIndicator, Is.False);
            Assert.That(assignment.UsedRightIndicator, Is.True);
            Assert.That(metadata.RejectedObservationIndices, Is.Empty);
        });
    }

    [Test]
    public void ConflictingValidSidesRejectOnlyTheirObservation()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Standard);
        Pdf417ScanlineRow[] observations = DecodeRows(
            encoded,
            new int[] { 0, 1, 2 });
        observations[1] = CloneRow(observations[1]);
        observations[1].LeftIndicator = observations[1].LeftIndicator + 30;
        observations[1].Codewords[0] = observations[1].LeftIndicator;

        Pdf417ReconciledMetadata metadata =
            Pdf417RowIndicatorReconciler.ReconcileRowMetadata(observations);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.RowCount, Is.EqualTo(9));
            Assert.That(metadata.Assignments.Select(item => item.RowNumber),
                Is.EqualTo(new int[] { 0, 2 }));
            Assert.That(metadata.RejectedObservationIndices,
                Is.EqualTo(new int[] { 1 }));
            Assert.That(metadata.MissingRowNumbers,
                Is.EqualTo(new int[] { 1, 3, 4, 5, 6, 7, 8 }));
        });
    }

    [Test]
    public void TiedMetadataEvidenceIsRejectedInsteadOfGuessed()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Standard);
        Pdf417ScanlineRow rowZero = DecodeRow(encoded, 0);
        Pdf417ScanlineRow rowOne = DecodeRow(encoded, 1);
        Pdf417ScanlineRow conflictingRowOne = CloneRow(rowOne);
        conflictingRowOne.LeftIndicator = conflictingRowOne.LeftIndicator - 3;
        conflictingRowOne.Codewords[0] = conflictingRowOne.LeftIndicator;

        Assert.That(
            () => Pdf417RowIndicatorReconciler.ReconcileRowMetadata(
                new Pdf417ScanlineRow[] { rowZero, rowOne, conflictingRowOne }),
            Throws.TypeOf<BarcodeDecodingException>());
    }

    [Test]
    public void MissingRequiredCompactMetadataIsRejected()
    {
        Pdf417EncodingResult encoded = CreateEncoded(Pdf417Variant.Compact);
        Pdf417ScanlineRow rowZero = DecodeRow(encoded, 0);

        Assert.That(
            () => Pdf417RowIndicatorReconciler.ReconcileRowMetadata(
                new Pdf417ScanlineRow[] { rowZero }),
            Throws.TypeOf<BarcodeDecodingException>());
    }

    [Test]
    public void IndicatorRowNumbersRoundTripForBothStandardSides()
    {
        int rowNumber = 0;
        while (rowNumber < 9)
        {
            int cluster = Pdf417Layout.GetClusterIndex(rowNumber);
            int left = Pdf417Layout.CalculateLeftRowIndicator(
                rowNumber,
                9,
                4,
                4);
            int right = Pdf417Layout.CalculateRightRowIndicator(
                rowNumber,
                9,
                4,
                4);

            Assert.Multiple(() =>
            {
                Assert.That(
                    Pdf417RowIndicatorReconciler.CalculateRowNumber(left, cluster),
                    Is.EqualTo(rowNumber));
                Assert.That(
                    Pdf417RowIndicatorReconciler.CalculateRowNumber(right, cluster),
                    Is.EqualTo(rowNumber));
            });
            rowNumber++;
        }
    }

    [Test]
    public void InvalidObservationShapesAreRejected()
    {
        Pdf417EncodingResult standard = CreateEncoded(Pdf417Variant.Standard);
        Pdf417EncodingResult compact = CreateEncoded(Pdf417Variant.Compact);
        Pdf417ScanlineRow standardRow = DecodeRow(standard, 0);
        Pdf417ScanlineRow compactRow = DecodeRow(compact, 1);
        Pdf417ScanlineRow invalidCluster = CloneRow(standardRow);
        invalidCluster.ClusterIndex = 3;
        Pdf417ScanlineRow invalidBuckets = CloneRow(standardRow);
        invalidBuckets.Buckets[1] = 3;
        Pdf417ScanlineRow inconsistentCodewords = CloneRow(standardRow);
        inconsistentCodewords.Codewords[0] = inconsistentCodewords.Codewords[0] + 1;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417RowIndicatorReconciler.ReconcileRowMetadata(null!),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417RowIndicatorReconciler.ReconcileRowMetadata(
                    new Pdf417ScanlineRow[0]),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417RowIndicatorReconciler.ReconcileRowMetadata(
                    new Pdf417ScanlineRow[] { null! }),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417RowIndicatorReconciler.ReconcileRowMetadata(
                    new Pdf417ScanlineRow[] { standardRow, compactRow }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417RowIndicatorReconciler.ReconcileRowMetadata(
                    new Pdf417ScanlineRow[] { invalidCluster }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417RowIndicatorReconciler.ReconcileRowMetadata(
                    new Pdf417ScanlineRow[] { invalidBuckets }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417RowIndicatorReconciler.ReconcileRowMetadata(
                    new Pdf417ScanlineRow[] { inconsistentCodewords }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417RowIndicatorReconciler.CalculateRowNumber(-1, 0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417RowIndicatorReconciler.CalculateRowNumber(0, 3),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static Pdf417EncodingResult CreateEncoded(Pdf417Variant variant)
    {
        byte[] data = global::System.Text.Encoding.Latin1.GetBytes("ABC");
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "ABC";
        payload.RawBytes = (byte[])data.Clone();
        Pdf417Options options = new Pdf417Options();
        options.Columns = 4;
        options.Rows = 9;
        options.Variant = variant;
        options.CompactionMode = Pdf417CompactionMode.Text;
        options.ErrorCorrectionLevel = 4;
        return Pdf417Encoder.Encode(data, payload, options, null);
    }

    static Pdf417ScanlineRow[] DecodeRows(
        Pdf417EncodingResult encoded,
        int[] rowNumbers)
    {
        Pdf417ScanlineRow[] rows = new Pdf417ScanlineRow[rowNumbers.Length];
        int index = 0;
        while (index < rows.Length)
        {
            rows[index] = DecodeRow(encoded, rowNumbers[index]);
            index++;
        }

        return rows;
    }

    static Pdf417ScanlineRow DecodeRow(
        Pdf417EncodingResult encoded,
        int rowNumber)
    {
        int scale = 2 + (rowNumber % 2);
        int quietModules = 2 + (rowNumber % 3);
        int length = (quietModules + encoded.Symbol.WidthModules + quietModules)
            * scale;
        BitRow row = new BitRow(length);
        int sourceRow = rowNumber * Pdf417Layout.DefaultRowHeightModules;
        int column = 0;
        while (column < encoded.Symbol.WidthModules)
        {
            if (encoded.Symbol.Modules![(sourceRow * encoded.Symbol.WidthModules) + column]
                != 0)
            {
                int repeat = 0;
                while (repeat < scale)
                {
                    row.SetBit(((quietModules + column) * scale) + repeat, true);
                    repeat++;
                }
            }

            column++;
        }

        return Pdf417ScanningDecoder.DecodeRow(row);
    }

    static Pdf417ScanlineRow CloneRow(Pdf417ScanlineRow source)
    {
        Pdf417ScanlineRow clone = new Pdf417ScanlineRow();
        clone.Variant = source.Variant;
        clone.LeftIndicator = source.LeftIndicator;
        clone.RightIndicator = source.RightIndicator;
        clone.DataCodewords = (int[])source.DataCodewords.Clone();
        clone.Codewords = (int[])source.Codewords.Clone();
        clone.Patterns = (int[])source.Patterns.Clone();
        clone.Buckets = (int[])source.Buckets.Clone();
        clone.ClusterIndex = source.ClusterIndex;
        clone.IsReversed = source.IsReversed;
        clone.StartPosition = source.StartPosition;
        clone.EndPosition = source.EndPosition;
        clone.ModuleSize = source.ModuleSize;
        clone.AverageVariance = source.AverageVariance;
        return clone;
    }
}
