namespace barcode.Tests;

public class Pdf417CodewordDecoderTests
{
    [Test]
    public void CompleteRowsDecodeMixedCompactionAndReportMetadata()
    {
        byte[] prefix = Bytes("ab;:");
        byte[] suffix = Bytes("12345678901234567890XYZ");
        byte[] source = new byte[prefix.Length + suffix.Length + 1];
        global::System.Array.Copy(prefix, source, prefix.Length);
        source[prefix.Length] = 255;
        global::System.Array.Copy(
            suffix,
            0,
            source,
            prefix.Length + 1,
            suffix.Length);
        Pdf417EncodingResult encoded = CreateEncoded(source, null, null);

        Pdf417CodewordDecodeResult decoded = Pdf417CodewordDecoder.DecodeRows(
            CreateRows(encoded));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Result.Format, Is.EqualTo(BarcodeFormat.Pdf417));
            Assert.That(decoded.Result.Dimension,
                Is.EqualTo(BarcodeDimension.StackedLinear));
            Assert.That(decoded.Result.Payload?.RawBytes, Is.EqualTo(source));
            Assert.That(decoded.Result.Payload?.Text,
                Is.EqualTo(global::System.Text.Encoding.Latin1.GetString(source)));
            Assert.That(decoded.Result.Payload?.CharacterEncoding,
                Is.EqualTo("ISO-8859-1"));
            Assert.That(decoded.Result.Metadata?.Codewords,
                Is.EqualTo(encoded.SymbolCodewords));
            Assert.That(decoded.Result.Metadata?.CorrectedErrorCount, Is.Zero);
            Assert.That(decoded.Result.Metadata?.ErasureCount, Is.Zero);
            Assert.That(decoded.Result.Metadata?.ErrorCorrectionApplied, Is.False);
            Assert.That(decoded.CorrectedCodewords,
                Is.EqualTo(encoded.SymbolCodewords));
            Assert.That(decoded.HighLevel.SegmentModes,
                Does.Contain(Pdf417CompactionMode.Text));
            Assert.That(decoded.HighLevel.SegmentModes,
                Does.Contain(Pdf417CompactionMode.Byte));
            Assert.That(decoded.HighLevel.SegmentModes,
                Does.Contain(Pdf417CompactionMode.Numeric));
        });
    }

    [Test]
    public void MissingRowIsCollectedAsErasuresAndRecovered()
    {
        byte[] source = Bytes("PDF417");
        Pdf417EncodingResult encoded = CreateEncoded(source, null, null);
        Pdf417ScanlineRow[] complete = CreateRows(encoded);
        Pdf417ScanlineRow[] missing = complete
            .Where((item, index) => index != 3)
            .ToArray();

        Pdf417CodewordDecodeResult decoded = Pdf417CodewordDecoder.DecodeRows(
            missing);
        int firstMissingCodeword = 3 * encoded.SymbolPlan.Columns;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Result.Payload?.RawBytes, Is.EqualTo(source));
            Assert.That(decoded.ErasureIndices, Is.EqualTo(
                global::System.Linq.Enumerable.Range(
                    firstMissingCodeword,
                    encoded.SymbolPlan.Columns)));
            Assert.That(decoded.Correction.ErasureCount,
                Is.EqualTo(encoded.SymbolPlan.Columns));
            Assert.That(decoded.CorrectedCodewords,
                Is.EqualTo(encoded.SymbolCodewords));
            Assert.That(decoded.Result.Metadata?.ErrorCorrectionApplied, Is.True);
        });
    }

    [Test]
    public void UnknownCodewordErrorIsCorrectedAndReported()
    {
        byte[] source = Bytes("ERROR CORRECTION");
        Pdf417Options options = new Pdf417Options();
        options.ErrorCorrectionLevel = 2;
        Pdf417EncodingResult encoded = CreateEncoded(source, options, null);
        Pdf417ScanlineRow[] rows = CreateRows(encoded);
        rows[0] = CloneRow(rows[0]);
        rows[0].DataCodewords[1] =
            (rows[0].DataCodewords[1] + 17) % 929;

        Pdf417CodewordDecodeResult decoded = Pdf417CodewordDecoder.DecodeRows(
            rows);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Result.Payload?.RawBytes, Is.EqualTo(source));
            Assert.That(decoded.Correction.CorrectedErrorCount, Is.EqualTo(1));
            Assert.That(decoded.Correction.CorrectedIndices,
                Is.EqualTo(new int[] { 1 }));
            Assert.That(decoded.Result.Metadata?.CorrectedErrorCount,
                Is.EqualTo(1));
            Assert.That(decoded.Result.Metadata?.ErrorCorrectionApplied, Is.True);
            Assert.That(decoded.CorrectedCodewords,
                Is.EqualTo(encoded.SymbolCodewords));
        });
    }

    [Test]
    public void AmbiguousCellsTryAlternateCandidatesUntilEccSucceeds()
    {
        byte[] source = Bytes("ABC");
        Pdf417Options options = new Pdf417Options();
        options.Columns = 2;
        options.Rows = 3;
        options.ErrorCorrectionLevel = 0;
        Pdf417EncodingResult encoded = CreateEncoded(source, options, null);
        Pdf417ScanlineRow[] rows = CreateRows(encoded);
        Pdf417ScanlineRow firstWrong = CloneRow(rows[0]);
        Pdf417ScanlineRow secondWrong = CloneRow(rows[0]);
        int column = 0;
        while (column < 2)
        {
            int wrong = (rows[0].DataCodewords[column] + 100 + column) % 929;
            firstWrong.DataCodewords[column] = wrong;
            secondWrong.DataCodewords[column] = wrong;
            column++;
        }

        Pdf417ScanlineRow[] observations = new Pdf417ScanlineRow[]
        {
            rows[0],
            firstWrong,
            secondWrong,
            rows[1],
            rows[2]
        };

        Pdf417CodewordDecodeResult decoded = Pdf417CodewordDecoder.DecodeRows(
            observations);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Result.Payload?.RawBytes, Is.EqualTo(source));
            Assert.That(decoded.AmbiguousIndices,
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(decoded.SelectedCandidateIndices,
                Is.Not.EqualTo(new int[] { 0, 0 }));
            Assert.That(decoded.CorrectedCodewords,
                Is.EqualTo(encoded.SymbolCodewords));
            Assert.That(decoded.Result.Metadata?.ErrorCorrectionApplied, Is.True);
        });
    }

    [Test]
    public void CompactMacroEciPayloadReportsTextAndStructuredAppendMetadata()
    {
        byte[] source = global::System.Text.Encoding.UTF8.GetBytes("café");
        StructuredAppendInfo macro = new StructuredAppendInfo();
        macro.Format = BarcodeFormat.MacroPdf417;
        macro.Index = 1;
        macro.Count = 2;
        macro.FileIdentifier = "017053";
        macro.IsLastSegment = true;
        Pdf417Options options = new Pdf417Options();
        options.Variant = Pdf417Variant.Compact;
        options.CompactionMode = Pdf417CompactionMode.Byte;
        options.ErrorCorrectionLevel = 2;
        options.EciAssignmentNumber = 26;
        options.Macro = macro;
        options.Sender = "CEN";
        options.FileChecksum = 32123;
        Pdf417EncodingResult encoded = CreateEncoded(source, options, null);

        Pdf417CodewordDecodeResult decoded = Pdf417CodewordDecoder.DecodeRows(
            CreateRows(encoded));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Result.Format,
                Is.EqualTo(BarcodeFormat.MacroPdf417));
            Assert.That(decoded.RowMetadata.Variant,
                Is.EqualTo(Pdf417Variant.Compact));
            Assert.That(decoded.Result.Payload?.RawBytes, Is.EqualTo(source));
            Assert.That(decoded.Result.Payload?.Text, Is.EqualTo("café"));
            Assert.That(decoded.Result.Payload?.CharacterEncoding,
                Is.EqualTo("UTF-8"));
            Assert.That(decoded.Result.Payload?.EciSegments?[0].AssignmentNumber,
                Is.EqualTo(26));
            Assert.That(decoded.Result.Payload?.EciSegments?[0].ByteCount,
                Is.EqualTo(source.Length));
            Assert.That(decoded.Result.Payload?.StructuredAppend?.Index,
                Is.EqualTo(1));
            Assert.That(decoded.Result.Payload?.StructuredAppend?.Count,
                Is.EqualTo(2));
            Assert.That(decoded.Result.Payload?.StructuredAppend?.FileIdentifier,
                Is.EqualTo("017053"));
            Assert.That(decoded.MacroBlock?.Sender, Is.EqualTo("CEN"));
            Assert.That(decoded.Result.Metadata?.HasChecksum, Is.True);
            Assert.That(decoded.Result.Metadata?.ChecksumValue,
                Is.EqualTo(32123));
            Assert.That(decoded.Result.Metadata?.SymbologyIdentifier,
                Is.EqualTo("]L0"));
        });
    }

    [TestCase(26)]
    [TestCase(900)]
    [TestCase(810900)]
    public void EveryEciDesignatorWidthIsDecoded(int assignmentNumber)
    {
        int[] eci = Pdf417CompactionPlanner.EncodeEciDesignator(
            assignmentNumber);
        int[] codewords = eci.Concat(new int[] { 913, 65 }).ToArray();

        Pdf417HighLevelDecodeResult decoded =
            Pdf417HighLevelDecoder.DecodeHighLevelCodewords(codewords);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RawBytes, Is.EqualTo(new byte[] { 65 }));
            Assert.That(decoded.EciSegments, Has.Length.EqualTo(1));
            Assert.That(decoded.EciSegments[0].AssignmentNumber,
                Is.EqualTo(assignmentNumber));
            Assert.That(decoded.EciSegments[0].ByteOffset, Is.Zero);
            Assert.That(decoded.EciSegments[0].ByteCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExcessiveErasuresAndMalformedHighLevelDataAreRejected()
    {
        byte[] source = Bytes("PDF417");
        Pdf417EncodingResult encoded = CreateEncoded(source, null, null);
        Pdf417ScanlineRow[] rows = CreateRows(encoded);
        Pdf417ScanlineRow[] excessiveMissing = rows
            .Where((item, index) => index >= 5)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417CodewordDecoder.DecodeRows(excessiveMissing),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417CodewordDecoder.DecodeReconciledRows(null!),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417HighLevelDecoder.DecodeHighLevelCodewords(null!),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417HighLevelDecoder.DecodeHighLevelCodewords(
                    new int[] { 928 }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417HighLevelDecoder.DecodeHighLevelCodewords(
                    new int[] { 927 }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417HighLevelDecoder.DecodeHighLevelCodewords(
                    new int[] { 913, 256 }),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417HighLevelDecoder.DecodeHighLevelCodewords(
                    new int[] { 901 }),
                Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    static Pdf417EncodingResult CreateEncoded(
        byte[] data,
        Pdf417Options? options,
        BarcodeEncodeOptions? common)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.RawBytes = (byte[])data.Clone();
        return Pdf417Encoder.Encode(data, payload, options, common);
    }

    static Pdf417ScanlineRow[] CreateRows(Pdf417EncodingResult encoded)
    {
        Pdf417ScanlineRow[] rows =
            new Pdf417ScanlineRow[encoded.SymbolPlan.Rows];
        int rowNumber = 0;
        while (rowNumber < rows.Length)
        {
            Pdf417ScanlineRow row = new Pdf417ScanlineRow();
            row.Variant = encoded.SymbolPlan.Variant;
            row.ClusterIndex = rowNumber % 3;
            row.LeftIndicator = Pdf417Layout.CalculateLeftRowIndicator(
                rowNumber,
                encoded.SymbolPlan.Rows,
                encoded.SymbolPlan.Columns,
                encoded.SymbolPlan.ErrorCorrectionLevel);
            if (row.Variant == Pdf417Variant.Standard)
            {
                row.RightIndicator = Pdf417Layout.CalculateRightRowIndicator(
                    rowNumber,
                    encoded.SymbolPlan.Rows,
                    encoded.SymbolPlan.Columns,
                    encoded.SymbolPlan.ErrorCorrectionLevel);
            }

            row.DataCodewords = encoded.SymbolCodewords
                .Skip(rowNumber * encoded.SymbolPlan.Columns)
                .Take(encoded.SymbolPlan.Columns)
                .ToArray();
            rows[rowNumber] = row;
            rowNumber++;
        }

        return rows;
    }

    static Pdf417ScanlineRow CloneRow(Pdf417ScanlineRow source)
    {
        Pdf417ScanlineRow clone = new Pdf417ScanlineRow();
        clone.Variant = source.Variant;
        clone.LeftIndicator = source.LeftIndicator;
        clone.RightIndicator = source.RightIndicator;
        clone.DataCodewords = (int[])source.DataCodewords.Clone();
        clone.ClusterIndex = source.ClusterIndex;
        return clone;
    }

    static byte[] Bytes(string text)
    {
        return global::System.Text.Encoding.Latin1.GetBytes(text);
    }
}
