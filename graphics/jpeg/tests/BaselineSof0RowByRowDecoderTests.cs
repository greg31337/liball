namespace jpeg.Tests;

internal class BaselineSof0RowByRowDecoderTests
{
    [Test]
    public void RowDecoder_DecodesOneMcuRowAtATime()
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(16, 16);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = BaselineSof0FullImageDecoderTests.CreateEncoder();
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(3), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(1), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(0), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.FlushToByteBoundary();
        global::jpeg.JpegBaselineSof0RowByRowDecoder decoder = new global::jpeg.JpegBaselineSof0RowByRowDecoder();
        global::jpeg.JpegBaselineSof0RowDecodeState state = decoder.CreateState(frame, scan, tables, encoder.ToArray());

        bool decodedFirstRow = decoder.DecodeNextRow(state);

        Assert.That(decodedFirstRow, Is.True);
        Assert.That(state.McuRow, Is.EqualTo(1));
        Assert.That(state.DecodedMcuCount, Is.EqualTo(2));
        Assert.That(state.Completed, Is.False);
        Assert.That(decoder.GetDecodedSampleRows(state, 1), Is.EqualTo(8));
        Assert.That(state.Destination.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(state.Destination.Components[0].Samples[8], Is.EqualTo(143));
        Assert.That(state.Destination.Components[0].Samples[8 * state.Destination.Components[0].Stride], Is.EqualTo(0));

        bool decodedSecondRow = decoder.DecodeNextRow(state);
        bool decodedAfterEnd = decoder.DecodeNextRow(state);

        Assert.That(decodedSecondRow, Is.True);
        Assert.That(decodedAfterEnd, Is.False);
        Assert.That(state.Completed, Is.True);
        Assert.That(decoder.GetDecodedSampleRows(state, 1), Is.EqualTo(16));
        Assert.That(state.Destination.Components[0].Samples[8 * state.Destination.Components[0].Stride], Is.EqualTo(133));
        Assert.That(state.Destination.Components[0].Samples[8 * state.Destination.Components[0].Stride + 8], Is.EqualTo(128));
    }

    [Test]
    public void RowDecoder_DecodeRowsMatchesFullImageDecoder()
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(16, 16);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 2, 2, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(2, 1, 1, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(3, 1, 1, 0));
        frame.ComponentCount = 3;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = BaselineSof0FullImageDecoderTests.CreateEncoder();
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(3), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(1), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(0), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(1), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 1);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 2);
        encoder.FlushToByteBoundary();
        byte[] entropyData = encoder.ToArray();
        global::jpeg.JpegBaselineSof0FullImageDecoder fullDecoder = new global::jpeg.JpegBaselineSof0FullImageDecoder();
        global::jpeg.JpegBaselineSof0RowByRowDecoder rowDecoder = new global::jpeg.JpegBaselineSof0RowByRowDecoder();

        global::jpeg.JpegPlanarImageBuffer fullImage = fullDecoder.Decode(frame, scan, tables, entropyData);
        global::jpeg.JpegPlanarImageBuffer rowImage = rowDecoder.DecodeRows(frame, scan, tables, entropyData);

        Assert.That(rowImage.Components.Count, Is.EqualTo(fullImage.Components.Count));
        Assert.That(rowImage.Components[0].Samples, Is.EqualTo(fullImage.Components[0].Samples));
        Assert.That(rowImage.Components[1].Samples, Is.EqualTo(fullImage.Components[1].Samples));
        Assert.That(rowImage.Components[2].Samples, Is.EqualTo(fullImage.Components[2].Samples));
    }

    [Test]
    public void RowDecoder_ConsumesRestartMarkersAcrossRows()
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(8, 16);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = BaselineSof0FullImageDecoderTests.CreateEncoder();
        encoder.ConfigureRestartInterval(1);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.RecordMcuEncoded();
        encoder.WriteRestartMarker();
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.FlushToByteBoundary();
        global::jpeg.JpegBaselineSof0RowByRowDecoder decoder = new global::jpeg.JpegBaselineSof0RowByRowDecoder();
        global::jpeg.JpegBaselineSof0RowDecodeState state = decoder.CreateStateWithRestartInterval(frame, scan, tables, encoder.ToArray(), 1);

        Assert.That(decoder.DecodeNextRow(state), Is.True);
        Assert.That(state.EntropyDecoder.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(decoder.DecodeNextRow(state), Is.True);

        Assert.That(state.Completed, Is.True);
        Assert.That(state.Destination.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(state.Destination.Components[0].Samples[8 * state.Destination.Components[0].Stride], Is.EqualTo(138));
    }

    [Test]
    public void RowDecoder_RejectsInvalidStateAndDestination()
    {
        global::jpeg.JpegBaselineSof0RowByRowDecoder decoder = new global::jpeg.JpegBaselineSof0RowByRowDecoder();
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.DecodeNextRow(null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.CreateStateInto(frame, scan, tables, new byte[0], null!);
            });

        global::jpeg.JpegBaselineSof0RowDecodeState badState = new global::jpeg.JpegBaselineSof0RowDecodeState();
        badState.Layout.McuRows = 1;
        badState.McuRow = 2;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeNextRow(badState);
            });
    }
}
