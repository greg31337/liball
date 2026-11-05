namespace jpeg.Tests;

internal class BaselineSof0EncoderTests
{
    [Test]
    public void Encoder_EncodesGrayscalePlanarSamplesThatDecodeBack()
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarSource(frame);
        FillBlock(source.Components[0], 0, 0, 138);
        FillBlock(source.Components[0], 8, 0, 143);
        global::jpeg.JpegBaselineSof0Encoder encoder = new global::jpeg.JpegBaselineSof0Encoder();
        global::jpeg.JpegBaselineSof0FullImageDecoder decoder = new global::jpeg.JpegBaselineSof0FullImageDecoder();

        byte[] entropyData = encoder.Encode(frame, scan, tables, source);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.Decode(frame, scan, tables, entropyData);

        Assert.That(entropyData.Length, Is.GreaterThan(0));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[7], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(143));
        Assert.That(decoded.Components[0].Samples[15], Is.EqualTo(143));
    }

    [Test]
    public void Encoder_ClampsPartialRightEdgeBlocks()
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(9, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarSource(frame);
        FillRectangle(source.Components[0], 0, 0, 8, 8, 138);
        FillRectangle(source.Components[0], 8, 0, 1, 8, 143);
        global::jpeg.JpegBaselineSof0Encoder encoder = new global::jpeg.JpegBaselineSof0Encoder();
        global::jpeg.JpegBaselineSof0FullImageDecoder decoder = new global::jpeg.JpegBaselineSof0FullImageDecoder();

        byte[] entropyData = encoder.Encode(frame, scan, tables, source);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.Decode(frame, scan, tables, entropyData);

        Assert.That(decoded.Components[0].Width, Is.EqualTo(9));
        Assert.That(decoded.Components[0].PaddedWidth, Is.EqualTo(16));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(143));
        Assert.That(decoded.Components[0].Samples[15], Is.EqualTo(143));
    }

    [Test]
    public void Encoder_EncodesFourTwoZeroComponentBlockOrder()
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(16, 16);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 2, 2, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(2, 1, 1, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(3, 1, 1, 0));
        frame.ComponentCount = 3;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarSource(frame);
        FillBlock(source.Components[0], 0, 0, 138);
        FillBlock(source.Components[0], 8, 0, 143);
        FillBlock(source.Components[0], 0, 8, 133);
        FillBlock(source.Components[0], 8, 8, 128);
        FillBlock(source.Components[1], 0, 0, 133);
        FillBlock(source.Components[2], 0, 0, 138);
        global::jpeg.JpegBaselineSof0Encoder encoder = new global::jpeg.JpegBaselineSof0Encoder();
        global::jpeg.JpegBaselineSof0FullImageDecoder decoder = new global::jpeg.JpegBaselineSof0FullImageDecoder();

        byte[] entropyData = encoder.Encode(frame, scan, tables, source);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.Decode(frame, scan, tables, entropyData);

        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(143));
        Assert.That(decoded.Components[0].Samples[8 * decoded.Components[0].Stride], Is.EqualTo(133));
        Assert.That(decoded.Components[0].Samples[8 * decoded.Components[0].Stride + 8], Is.EqualTo(128));
        Assert.That(decoded.Components[1].Samples[0], Is.EqualTo(133));
        Assert.That(decoded.Components[2].Samples[0], Is.EqualTo(138));
    }

    [Test]
    public void Encoder_WritesRestartMarkersBetweenMcus()
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarSource(frame);
        FillBlock(source.Components[0], 0, 0, 138);
        FillBlock(source.Components[0], 8, 0, 138);
        global::jpeg.JpegBaselineSof0Encoder encoder = new global::jpeg.JpegBaselineSof0Encoder();
        global::jpeg.JpegBaselineSof0FullImageDecoder decoder = new global::jpeg.JpegBaselineSof0FullImageDecoder();

        byte[] entropyData = encoder.EncodeWithRestartInterval(frame, scan, tables, source, 1);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.DecodeWithRestartInterval(frame, scan, tables, entropyData, 1);

        Assert.That(ContainsRestart0(entropyData), Is.True);
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(138));
    }

    [Test]
    public void Encoder_RejectsInvalidInputs()
    {
        global::jpeg.JpegBaselineSof0Encoder encoder = new global::jpeg.JpegBaselineSof0Encoder();
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarSource(frame);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                encoder.Encode(frame, scan, tables, null!);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegPlanarImageBuffer badSize = CreatePlanarSource(frame);
                badSize.Width = 9;
                encoder.Encode(frame, scan, tables, badSize);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.JpegTableStore missingQuantization = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
                missingQuantization.QuantizationTables.Clear();
                encoder.Encode(frame, scan, missingQuantization, source);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                encoder.EncodeWithRestartInterval(frame, scan, tables, source, -1);
            });
    }

    public static global::jpeg.JpegPlanarImageBuffer CreatePlanarSource(global::jpeg.JpegFrameHeader frame)
    {
        global::jpeg.JpegMcuLayout layout = new global::jpeg.JpegMcuLayoutCalculator().Calculate(frame);
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = frame.Width;
        source.Height = frame.Height;
        source.ComponentCount = layout.ComponentCount;
        int index = 0;
        while (index < layout.ComponentCount)
        {
            source.Components.Add(CreateComponent(layout.Components[index]));
            index = index + 1;
        }

        return source;
    }

    public static global::jpeg.JpegPlanarComponentBuffer CreateComponent(global::jpeg.JpegMcuComponentLayout layout)
    {
        global::jpeg.JpegPlanarComponentBuffer component = new global::jpeg.JpegPlanarComponentBuffer();
        component.ComponentIndex = layout.ComponentIndex;
        component.Identifier = layout.Identifier;
        component.Width = layout.SampleWidth;
        component.Height = layout.SampleHeight;
        component.PaddedWidth = layout.SampleWidth;
        component.PaddedHeight = layout.SampleHeight;
        component.Stride = layout.SampleWidth;
        component.Samples = new byte[layout.SampleWidth * layout.SampleHeight];
        return component;
    }

    public static void FillBlock(global::jpeg.JpegPlanarComponentBuffer component, int x, int y, int value)
    {
        FillRectangle(component, x, y, 8, 8, value);
    }

    public static void FillRectangle(global::jpeg.JpegPlanarComponentBuffer component, int x, int y, int width, int height, int value)
    {
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                component.Samples[(y + row) * component.Stride + x + column] = (byte)value;
                column = column + 1;
            }

            row = row + 1;
        }
    }

    public static bool ContainsRestart0(byte[] data)
    {
        int index = 0;
        while (index + 1 < data.Length)
        {
            if (data[index] == 0xFF && data[index + 1] == 0xD0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
