using System.Buffers.Binary;

namespace avif.Tests;

public class AvifIncrementalReaderTests
{
    [Test]
    public void ChunkedParseReportsExactMissingRangesThenDecodesCompleteImage()
    {
        AvifImage image = CreateImage(4, 3, 37);
        byte[] encoded = Avif.Encode(image).GetData();
        AvifIncrementalReader reader = AvifIncrementalReader.Create();

        AvifResultCode emptyParse = reader.ParseAvailable();
        AvifByteRange emptyRange = reader.GetMissingRanges()[0];
        reader.Append(encoded.AsSpan(0, 4));
        AvifResultCode headerParse = reader.ParseAvailable();
        AvifByteRange headerRange = reader.GetMissingRanges()[0];
        reader.Append(encoded.AsSpan(4, encoded.Length - 9));
        AvifResultCode payloadParse = reader.ParseAvailable();
        AvifByteRange payloadRange = reader.GetMissingRanges()[0];
        AvifDecodeResult waitingDecode = reader.DecodeAvailable();
        reader.Append(encoded.AsSpan(encoded.Length - 5));
        AvifResultCode completeParse = reader.ParseAvailable();
        AvifDecodeResult decoded = reader.DecodeAvailable();

        Assert.Multiple(() =>
        {
            Assert.That(emptyParse, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(emptyRange.GetOffset(), Is.Zero);
            Assert.That(emptyRange.GetLength(), Is.EqualTo(8));
            Assert.That(headerParse, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(headerRange.GetOffset(), Is.EqualTo(4));
            Assert.That(headerRange.GetLength(), Is.EqualTo(4));
            Assert.That(payloadParse, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(payloadRange.GetOffset(), Is.EqualTo((ulong)encoded.Length - 5));
            Assert.That(payloadRange.GetLength(), Is.EqualTo(5));
            Assert.That(waitingDecode.GetCode(), Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(reader.GetMissingRanges(), Is.Empty);
            Assert.That(completeParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.GetDocument(), Is.Not.Null);
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.GetDecodedRowCount(), Is.EqualTo(3));
            Assert.That(reader.GetReceivedByteCount(), Is.EqualTo(encoded.Length));
        });
        AssertImage(decoded.GetImage(), 37);

        image.Close();
        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
    }

    [Test]
    public void AppendingAfterSuccessfulDecodeInvalidatesProgressUntilReparsed()
    {
        AvifImage image = CreateImage(2, 2, 51);
        byte[] encoded = Avif.Encode(image).GetData();
        AvifIncrementalReader reader = AvifIncrementalReader.Create();
        reader.Append(encoded);
        AvifDecodeResult first = reader.DecodeAvailable();
        byte[] freeBox = new byte[]
        {
            0, 0, 0, 8,
            (byte)'f', (byte)'r', (byte)'e', (byte)'e'
        };

        AvifResultCode append = reader.Append(freeBox);
        AvifDocument? invalidatedDocument = reader.GetDocument();
        int invalidatedRows = reader.GetDecodedRowCount();
        AvifResultCode reparse = reader.ParseAvailable();
        AvifResultCode complete = reader.Complete();
        AvifResultCode appendAfterComplete = reader.Append(new byte[] { 0 });

        Assert.Multiple(() =>
        {
            Assert.That(first.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(append, Is.EqualTo(AvifResultCode.Success));
            Assert.That(invalidatedDocument, Is.Null);
            Assert.That(invalidatedRows, Is.Zero);
            Assert.That(reparse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(complete, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.IsComplete(), Is.True);
            Assert.That(appendAfterComplete, Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That((first.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetCode(), Is.EqualTo(AvifResultCode.Success));
        });

        image.Close();
        (first.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
    }

    [Test]
    public void CompletionTurnsWaitingIntoFinalTruncation()
    {
        AvifImage image = CreateImage(2, 2, 19);
        byte[] encoded = Avif.Encode(image).GetData();
        AvifIncrementalReader reader = AvifIncrementalReader.Create();
        reader.Append(encoded.AsSpan(0, encoded.Length - 2));

        AvifResultCode waiting = reader.ParseAvailable();
        AvifResultCode complete = reader.Complete();
        AvifResultCode truncated = reader.ParseAvailable();
        AvifDecodeResult decode = reader.DecodeAvailable();

        Assert.Multiple(() =>
        {
            Assert.That(waiting, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(complete, Is.EqualTo(AvifResultCode.Success));
            Assert.That(truncated, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(decode.GetCode(), Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(reader.GetMissingRanges(), Is.Empty);
            Assert.That(reader.GetDecodedRowCount(), Is.Zero);
        });

        image.Close();
        reader.Close();
    }

    [Test]
    public void InvalidBrandsLimitsCancellationAndCleanupHaveStableResults()
    {
        AvifIncrementalReader nonAvif = AvifIncrementalReader.Create();
        nonAvif.Append(BuildFileType("heic", "mif1"));
        AvifResultCode nonAvifCode = nonAvif.ParseAvailable();

        AvifDecoderOptions limitedOptions = new AvifDecoderOptions();
        limitedOptions.Limits.MaxSourceBytes = 16;
        AvifIncrementalReader limited = AvifIncrementalReader.Create(limitedOptions);
        AvifResultCode limitCode = limited.Append(new byte[17]);

        AvifImage image = CreateImage(1, 1, 73);
        byte[] encoded = Avif.Encode(image).GetData();
        AvifDecoderOptions cancelledOptions = new AvifDecoderOptions();
        AvifIncrementalReader cancelled = AvifIncrementalReader.Create(cancelledOptions);
        cancelled.Append(encoded);
        cancelled.ParseAvailable();
        cancelledOptions.Cancellation.Cancel();
        AvifDecodeResult cancelledDecode = cancelled.DecodeAvailable();

        AvifDecoderOptions parseCancelledOptions = new AvifDecoderOptions();
        parseCancelledOptions.Cancellation.Cancel();
        AvifIncrementalReader parseCancelled = AvifIncrementalReader.Create(
            parseCancelledOptions);
        AvifResultCode cancelledAppend = parseCancelled.Append(encoded);
        AvifResultCode cancelledParse = parseCancelled.ParseAvailable();

        AvifIncrementalReader invalid = AvifIncrementalReader.Create(null);
        AvifResultCode invalidAppend = invalid.Append(new byte[] { 1 });
        AvifIncrementalReader closed = AvifIncrementalReader.Create();
        closed.Append(new byte[] { 1, 2, 3 });
        AvifResultCode firstClose = closed.Close();

        Assert.Multiple(() =>
        {
            Assert.That(nonAvifCode, Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(limitCode, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(limited.GetReceivedByteCount(), Is.Zero);
            Assert.That(cancelledDecode.GetCode(), Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(cancelledAppend, Is.EqualTo(AvifResultCode.Success));
            Assert.That(cancelledParse, Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(parseCancelled.GetDocument(), Is.Null);
            Assert.That(invalidAppend, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(firstClose, Is.EqualTo(AvifResultCode.Success));
            Assert.That(closed.GetCode(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(closed.GetReceivedByteCount(), Is.Zero);
            Assert.That(closed.GetMissingRanges(), Is.Empty);
            Assert.That(closed.Append(new byte[] { 4 }), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(closed.ParseAvailable(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(closed.Complete(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(closed.Close(), Is.EqualTo(AvifResultCode.Success));
        });

        image.Close();
        nonAvif.Close();
        limited.Close();
        cancelled.Close();
        parseCancelled.Close();
        invalid.Close();
    }

    [Test]
    public void OutOfOrderRangesReportCoverageGapsAndMergeWithAppends()
    {
        AvifImage image = CreateImage(4, 3, 61);
        byte[] encoded = Avif.Encode(image).GetData();
        int firstSplit = encoded.Length / 3;
        int secondSplit = encoded.Length * 2 / 3;
        AvifIncrementalReader reader = AvifIncrementalReader.Create();

        AvifResultCode tail = reader.AddRange(
            secondSplit,
            encoded.AsSpan(secondSplit));
        AvifResultCode tailParse = reader.ParseAvailable();
        AvifByteRange tailGap = reader.GetMissingRanges()[0];
        AvifResultCode middle = reader.AddRange(
            firstSplit,
            encoded.AsSpan(firstSplit, secondSplit - firstSplit));
        AvifResultCode middleParse = reader.ParseAvailable();
        AvifByteRange middleGap = reader.GetMissingRanges()[0];
        AvifResultCode prefix = reader.Append(
            encoded.AsSpan(0, firstSplit));
        int receivedBeforeOverlap = reader.GetReceivedByteCount();
        AvifResultCode identicalOverlap = reader.AddRange(
            0,
            encoded.AsSpan(0, 8));
        AvifResultCode parsed = reader.ParseAvailable();
        AvifDecodeResult decoded = reader.DecodeAvailable();

        Assert.Multiple(() =>
        {
            Assert.That(tail, Is.EqualTo(AvifResultCode.Success));
            Assert.That(tailParse, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(tailGap.GetOffset(), Is.Zero);
            Assert.That(tailGap.GetLength(), Is.EqualTo(secondSplit));
            Assert.That(middle, Is.EqualTo(AvifResultCode.Success));
            Assert.That(middleParse, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(middleGap.GetOffset(), Is.Zero);
            Assert.That(middleGap.GetLength(), Is.EqualTo(firstSplit));
            Assert.That(prefix, Is.EqualTo(AvifResultCode.Success));
            Assert.That(identicalOverlap, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.GetReceivedByteCount(),
                Is.EqualTo(receivedBeforeOverlap));
            Assert.That(reader.GetReceivedByteCount(),
                Is.EqualTo(encoded.Length));
            Assert.That(reader.GetContiguousByteCount(),
                Is.EqualTo(encoded.Length));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.IsParseAvailable(), Is.True);
            Assert.That(reader.IsItemAvailable(1), Is.True);
            Assert.That(reader.IsItemAvailable(99), Is.False);
            Assert.That(reader.IsFrameAvailable(0), Is.False);
            Assert.That(reader.IsTileAvailable(1, 0, 0), Is.False);
            Assert.That(reader.IsLayerAvailable(0), Is.False);
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.GetDecodedRowCount(), Is.EqualTo(3));
        });
        AssertImage(decoded.GetImage(), 61);

        image.Close();
        (decoded.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Close();
        reader.Close();
    }

    [Test]
    public void OutOfOrderRangesRejectConflictsAndSourceLimitViolations()
    {
        AvifIncrementalReader conflicting =
            AvifIncrementalReader.Create();
        conflicting.AddRange(4, new byte[] { 1, 2, 3, 4 });
        AvifResultCode conflict = conflicting.AddRange(
            6,
            new byte[] { 9, 4 });
        AvifDecoderOptions limitedOptions = new AvifDecoderOptions();
        limitedOptions.Limits.MaxSourceBytes = 16;
        AvifIncrementalReader limited =
            AvifIncrementalReader.Create(limitedOptions);
        AvifResultCode limit = limited.AddRange(
            16,
            new byte[] { 1 });
        AvifIncrementalReader completed =
            AvifIncrementalReader.Create();
        completed.AddRange(8, new byte[] { 1, 2, 3, 4 });
        completed.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(conflict, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(conflicting.GetReceivedByteCount(), Is.EqualTo(4));
            Assert.That(conflicting.AddRange(0, new byte[] { 0 }),
                Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(limit, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(limited.GetReceivedByteCount(), Is.Zero);
            Assert.That(completed.ParseAvailable(),
                Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(completed.AddRange(0, new byte[8]),
                Is.EqualTo(AvifResultCode.InvalidState));
        });

        conflicting.Close();
        limited.Close();
        completed.Close();
    }

    [Test]
    public void AvailabilityQueriesDistinguishFramesTilesAndLayers()
    {
        AvifImage first = CreateImage(2, 2, 17);
        AvifImage second = CreateImage(2, 2, 29);
        AvifSequence sequence = AvifSequence.Create(1000);
        sequence.AddFrame(AvifFrame.Create(first, 100));
        sequence.AddFrame(AvifFrame.Create(second, 100));
        AvifWriter sequenceWriter = AvifWriter.Create();
        sequenceWriter.AddSequence(sequence);
        AvifIncrementalReader sequenceReader =
            AvifIncrementalReader.Create();
        sequenceReader.Append(sequenceWriter.Finish().GetData());
        sequenceReader.ParseAvailable();

        AvifImage topLeft = CreateImage(2, 2, 41);
        AvifImage topRight = CreateImage(2, 2, 53);
        AvifGrid grid = AvifGrid.Create(1, 2, 4, 2);
        grid.SetCell(0, topLeft);
        grid.SetCell(1, topRight);
        AvifWriter gridWriter = AvifWriter.Create();
        uint gridId = gridWriter.AddGrid(grid);
        AvifIncrementalReader gridReader = AvifIncrementalReader.Create();
        gridReader.Append(gridWriter.Finish().GetData());
        gridReader.ParseAvailable();

        AvifImage preview = CreateImage(2, 2, 67);
        AvifImage final = CreateImage(3, 3, 79);
        AvifLayeredImage layered = AvifLayeredImage.Create(0, true);
        layered.AddLayer(preview);
        layered.AddLayer(final);
        AvifWriter layerWriter = AvifWriter.Create();
        layerWriter.AddLayeredImage(layered);
        AvifIncrementalReader layerReader = AvifIncrementalReader.Create();
        layerReader.Append(layerWriter.Finish().GetData());
        layerReader.ParseAvailable();

        Assert.Multiple(() =>
        {
            Assert.That(sequenceReader.IsParseAvailable(), Is.True);
            Assert.That(sequenceReader.IsFrameAvailable(0), Is.True);
            Assert.That(sequenceReader.IsFrameAvailable(1), Is.True);
            Assert.That(sequenceReader.IsFrameAvailable(2), Is.False);
            Assert.That(gridReader.IsParseAvailable(), Is.True);
            Assert.That(gridReader.IsTileAvailable(gridId, 0, 0), Is.True);
            Assert.That(gridReader.IsTileAvailable(gridId, 0, 1), Is.True);
            Assert.That(gridReader.IsTileAvailable(gridId, 1, 0), Is.False);
            Assert.That(layerReader.IsParseAvailable(), Is.True);
            Assert.That(layerReader.IsLayerAvailable(0), Is.True);
            Assert.That(layerReader.IsLayerAvailable(1), Is.True);
            Assert.That(layerReader.IsLayerAvailable(2), Is.False);
            Assert.That(sequenceReader.GetDecodedRowCount(), Is.Zero);
            Assert.That(gridReader.GetDecodedRowCount(), Is.Zero);
            Assert.That(layerReader.GetDecodedRowCount(), Is.Zero);
        });

        sequenceReader.Close();
        gridReader.Close();
        layerReader.Close();
        sequenceWriter.Close();
        gridWriter.Close();
        layerWriter.Close();
        first.Close();
        second.Close();
        topLeft.Close();
        topRight.Close();
        preview.Close();
        final.Close();
    }

    static AvifImage CreateImage(int width, int height, byte value)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane? plane = image.GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < height)
        {
            (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    static void AssertImage(AvifImage? image, byte value)
    {
        AvifPlane? plane = (image ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            Assert.That(
                plane.GetRowSpan(row).ToArray(),
                Is.EqualTo(Enumerable.Repeat(value, plane.GetWidth()).ToArray()));
            row++;
        }
    }

    static byte[] BuildFileType(string majorBrand, string compatibleBrand)
    {
        byte[] data = new byte[20];
        BinaryPrimitives.WriteUInt32BigEndian(data, 20);
        WriteFourCc(data, 4, "ftyp");
        WriteFourCc(data, 8, majorBrand);
        WriteFourCc(data, 16, compatibleBrand);
        return data;
    }

    static void WriteFourCc(byte[] data, int offset, string value)
    {
        data[offset] = (byte)value[0];
        data[offset + 1] = (byte)value[1];
        data[offset + 2] = (byte)value[2];
        data[offset + 3] = (byte)value[3];
    }
}
