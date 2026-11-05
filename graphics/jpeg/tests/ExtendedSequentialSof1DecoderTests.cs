namespace jpeg.Tests;

internal class ExtendedSequentialSof1DecoderTests
{
    [Test]
    public void Decoder_DecodesTwelveBitGrayscaleSamples()
    {
        global::jpeg.JpegFrameHeader frame = CreateSof1Frame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = BaselineSof0FullImageDecoderTests.CreateEncoder();
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(2), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(3), BaselineHuffmanEntropyDecoderTests.CreateDcTable(), BaselineHuffmanEntropyDecoderTests.CreateAcTable(), 0);
        encoder.FlushToByteBoundary();
        global::jpeg.JpegExtendedSequentialSof1Decoder decoder = new global::jpeg.JpegExtendedSequentialSof1Decoder();

        global::jpeg.JpegPlanarIntImageBuffer image = decoder.Decode(frame, scan, tables, encoder.ToArray());

        Assert.That(image.Width, Is.EqualTo(16));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.Precision, Is.EqualTo(12));
        Assert.That(image.Components[0].Samples[0], Is.EqualTo(2058));
        Assert.That(image.Components[0].Samples[7], Is.EqualTo(2058));
        Assert.That(image.Components[0].Samples[8], Is.EqualTo(2063));
        Assert.That(image.Components[0].Samples[15], Is.EqualTo(2063));
    }

    [Test]
    public void Decoder_PlacesFourTwoZeroTwelveBitBlocks()
    {
        global::jpeg.JpegFrameHeader frame = CreateSof1Frame(16, 16);
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
        global::jpeg.JpegExtendedSequentialSof1Decoder decoder = new global::jpeg.JpegExtendedSequentialSof1Decoder();

        global::jpeg.JpegPlanarIntImageBuffer image = decoder.Decode(frame, scan, tables, encoder.ToArray());

        Assert.That(image.Components[0].Samples[0], Is.EqualTo(2058));
        Assert.That(image.Components[0].Samples[8], Is.EqualTo(2063));
        Assert.That(image.Components[0].Samples[8 * image.Components[0].Stride], Is.EqualTo(2053));
        Assert.That(image.Components[0].Samples[8 * image.Components[0].Stride + 8], Is.EqualTo(2048));
        Assert.That(image.Components[1].Samples[0], Is.EqualTo(2053));
        Assert.That(image.Components[2].Samples[0], Is.EqualTo(2058));
    }

    [Test]
    public void Decoder_ConsumesRestartMarkersForTwelveBitScan()
    {
        global::jpeg.JpegFrameHeader frame = CreateSof1Frame(16, 8);
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
        global::jpeg.JpegExtendedSequentialSof1Decoder decoder = new global::jpeg.JpegExtendedSequentialSof1Decoder();

        global::jpeg.JpegPlanarIntImageBuffer image = decoder.DecodeWithRestartInterval(frame, scan, tables, encoder.ToArray(), 1);

        Assert.That(image.Components[0].Samples[0], Is.EqualTo(2058));
        Assert.That(image.Components[0].Samples[8], Is.EqualTo(2058));
    }

    [Test]
    public void SequentialEntropyDecoder_AllowsExtendedMagnitudeCategories()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(0, 1);
        writer.WriteBits(2048, 12);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundary();
        global::jpeg.JpegSequentialHuffmanEntropyDecoder decoder = new global::jpeg.JpegSequentialHuffmanEntropyDecoder(writer.ToArray());

        int[] block = decoder.DecodeBlock(CreateSingleSymbolDcTable(12), CreateSingleSymbolAcTable(0), 0);

        Assert.That(block[0], Is.EqualTo(2048));
        Assert.That(block[1], Is.EqualTo(0));
    }

    [Test]
    public void Decoder_ClampsTwelveBitSamplesAndRejectsInvalidInputs()
    {
        global::jpeg.JpegExtendedSequentialSof1Decoder decoder = new global::jpeg.JpegExtendedSequentialSof1Decoder();
        global::jpeg.JpegFrameHeader frame = CreateSof1Frame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);

        Assert.That(decoder.ToSampleInt(0, 12), Is.EqualTo(2048));
        Assert.That(decoder.ToSampleInt(3000, 12), Is.EqualTo(4095));
        Assert.That(decoder.ToSampleInt(-3000, 12), Is.EqualTo(0));

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegFrameHeader baseline = BaselineSof0FullImageDecoderTests.CreateFrame(8, 8);
                baseline.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
                baseline.ComponentCount = 1;
                decoder.Decode(baseline, scan, tables, new byte[0]);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegFrameHeader wrongPrecision = CreateSof1Frame(8, 8);
                wrongPrecision.Precision = 8;
                wrongPrecision.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
                wrongPrecision.ComponentCount = 1;
                decoder.Decode(wrongPrecision, scan, tables, new byte[0]);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegScanHeader partial = BaselineSof0FullImageDecoderTests.CreateScan(frame);
                partial.SpectralEnd = 10;
                decoder.Decode(frame, partial, tables, new byte[0]);
            });
    }

    public static global::jpeg.JpegFrameHeader CreateSof1Frame(int width, int height)
    {
        global::jpeg.JpegFrameHeader frame = new global::jpeg.JpegFrameHeader();
        frame.Marker = global::jpeg.JpegMarker.StartOfFrame1;
        frame.Value = global::jpeg.JpegConstants.MarkerStartOfFrame1;
        frame.Process = global::jpeg.JpegProcess.ExtendedSequentialDct;
        frame.Precision = 12;
        frame.Width = width;
        frame.Height = height;
        return frame;
    }

    public static global::jpeg.JpegHuffmanTable CreateSingleSymbolDcTable(int symbol)
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 0;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.SymbolCount = 1;
        table.Symbols = new int[] { symbol };
        table.Codes = new int[1];
        table.CodeLengths = new int[1];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }

    public static global::jpeg.JpegHuffmanTable CreateSingleSymbolAcTable(int symbol)
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 1;
        table.Identifier = 0;
        table.CodeCounts[0] = 1;
        table.SymbolCount = 1;
        table.Symbols = new int[] { symbol };
        table.Codes = new int[1];
        table.CodeLengths = new int[1];
        new global::jpeg.JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }
}
