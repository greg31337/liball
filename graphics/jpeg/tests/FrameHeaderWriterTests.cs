namespace jpeg.Tests;

internal class FrameHeaderWriterTests
{
    [Test]
    public void Writer_WritesFramePayloadThatParsesBack()
    {
        global::jpeg.JpegFrameHeader frame = CreateBaselineFrame();
        global::jpeg.JpegFrameHeaderWriter writer = new global::jpeg.JpegFrameHeaderWriter();

        byte[] payload = writer.WritePayload(frame);
        global::jpeg.JpegFrameHeader parsed = new global::jpeg.JpegFrameHeaderParser().Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, payload);

        Assert.That(payload, Is.EqualTo(IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3)));
        Assert.That(parsed.Width, Is.EqualTo(32));
        Assert.That(parsed.Height, Is.EqualTo(16));
        Assert.That(parsed.Components.Count, Is.EqualTo(3));
        Assert.That(parsed.Components[0].HorizontalSamplingFactor, Is.EqualTo(2));
        Assert.That(parsed.Components[1].QuantizationTableIdentifier, Is.EqualTo(1));
    }

    [Test]
    public void Writer_WritesMarkerSegmentUsingProcessWhenMarkerIsUnset()
    {
        global::jpeg.JpegFrameHeader frame = CreateBaselineFrame();
        frame.Marker = global::jpeg.JpegMarker.Unknown;
        frame.Value = 0;
        frame.Process = global::jpeg.JpegProcess.ProgressiveDctArithmetic;
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegFrameHeaderWriter writer = new global::jpeg.JpegFrameHeaderWriter();

        writer.WriteMarkerSegment(segmentWriter, frame);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        global::jpeg.JpegFrameHeader parsed = new global::jpeg.JpegFrameHeaderParser().Parse(segment.Value, segment.Payload);
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfFrame10));
        Assert.That(parsed.Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDctArithmetic));
        Assert.That(parsed.ArithmeticCoding, Is.True);
        Assert.That(parsed.Progressive, Is.True);
    }

    [Test]
    public void Writer_AllowsZeroHeightForDnlCompatibleFrames()
    {
        global::jpeg.JpegFrameHeader frame = CreateBaselineFrame();
        frame.Height = 0;
        global::jpeg.JpegFrameHeaderWriter writer = new global::jpeg.JpegFrameHeaderWriter();

        byte[] payload = writer.WritePayload(frame);
        global::jpeg.JpegFrameHeader parsed = new global::jpeg.JpegFrameHeaderParser().Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, payload);

        Assert.That(parsed.Height, Is.EqualTo(0));
        Assert.That(payload[1], Is.EqualTo(0));
        Assert.That(payload[2], Is.EqualTo(0));
    }

    [Test]
    public void Writer_RejectsInvalidFrameHeaders()
    {
        global::jpeg.JpegFrameHeaderWriter writer = new global::jpeg.JpegFrameHeaderWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        global::jpeg.JpegFrameHeader missingMarker = CreateBaselineFrame();
        missingMarker.Marker = global::jpeg.JpegMarker.Unknown;
        missingMarker.Value = 0;
        missingMarker.Process = global::jpeg.JpegProcess.Unknown;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(missingMarker);
            });

        global::jpeg.JpegFrameHeader invalidMarker = CreateBaselineFrame();
        invalidMarker.Value = global::jpeg.JpegConstants.MarkerDefineHuffmanTable;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(invalidMarker);
            });

        global::jpeg.JpegFrameHeader mismatch = CreateBaselineFrame();
        mismatch.Value = global::jpeg.JpegConstants.MarkerStartOfFrame2;
        mismatch.Marker = global::jpeg.JpegMarker.StartOfFrame0;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(mismatch);
            });

        global::jpeg.JpegFrameHeader badPrecision = CreateBaselineFrame();
        badPrecision.Precision = 17;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badPrecision);
            });

        global::jpeg.JpegFrameHeader badWidth = CreateBaselineFrame();
        badWidth.Width = 0;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badWidth);
            });

        global::jpeg.JpegFrameHeader componentMismatch = CreateBaselineFrame();
        componentMismatch.ComponentCount = 2;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(componentMismatch);
            });

        global::jpeg.JpegFrameHeader badSampling = CreateBaselineFrame();
        badSampling.Components[0].HorizontalSamplingFactor = 5;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badSampling);
            });

        global::jpeg.JpegFrameHeader badQuantization = CreateBaselineFrame();
        badQuantization.Components[0].QuantizationTableIdentifier = 4;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badQuantization);
            });

        global::jpeg.JpegFrameHeader duplicate = CreateBaselineFrame();
        duplicate.Components[1].Identifier = duplicate.Components[0].Identifier;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(duplicate);
            });
    }

    public static global::jpeg.JpegFrameHeader CreateBaselineFrame()
    {
        global::jpeg.JpegFrameHeader frame = new global::jpeg.JpegFrameHeader();
        frame.Marker = global::jpeg.JpegMarker.StartOfFrame0;
        frame.Value = global::jpeg.JpegConstants.MarkerStartOfFrame0;
        frame.Process = global::jpeg.JpegProcess.BaselineDct;
        frame.Precision = 8;
        frame.Height = 16;
        frame.Width = 32;
        frame.ComponentCount = 3;
        frame.Components.Add(CreateComponent(1, 2, 2, 0));
        frame.Components.Add(CreateComponent(2, 1, 1, 1));
        frame.Components.Add(CreateComponent(3, 1, 1, 1));
        return frame;
    }

    public static global::jpeg.JpegFrameComponent CreateComponent(int identifier, int horizontal, int vertical, int quantizationTable)
    {
        global::jpeg.JpegFrameComponent component = new global::jpeg.JpegFrameComponent();
        component.Identifier = identifier;
        component.HorizontalSamplingFactor = horizontal;
        component.VerticalSamplingFactor = vertical;
        component.QuantizationTableIdentifier = quantizationTable;
        return component;
    }
}
