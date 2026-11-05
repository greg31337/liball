namespace jpeg.Tests;

internal class FrameHeaderParserTests
{
    [Test]
    public void Parser_ReadsEveryJpegOneStartOfFrameVariant()
    {
        int[] markers = new int[]
        {
            global::jpeg.JpegConstants.MarkerStartOfFrame0,
            global::jpeg.JpegConstants.MarkerStartOfFrame1,
            global::jpeg.JpegConstants.MarkerStartOfFrame2,
            global::jpeg.JpegConstants.MarkerStartOfFrame3,
            global::jpeg.JpegConstants.MarkerStartOfFrame5,
            global::jpeg.JpegConstants.MarkerStartOfFrame6,
            global::jpeg.JpegConstants.MarkerStartOfFrame7,
            global::jpeg.JpegConstants.MarkerStartOfFrame9,
            global::jpeg.JpegConstants.MarkerStartOfFrame10,
            global::jpeg.JpegConstants.MarkerStartOfFrame11,
            global::jpeg.JpegConstants.MarkerStartOfFrame13,
            global::jpeg.JpegConstants.MarkerStartOfFrame14,
            global::jpeg.JpegConstants.MarkerStartOfFrame15
        };
        global::jpeg.JpegFrameHeaderParser parser = new global::jpeg.JpegFrameHeaderParser();

        int index = 0;
        while (index < markers.Length)
        {
            global::jpeg.JpegFrameHeader frame = parser.Parse(markers[index], CreateFramePayload(8, 16, 32, 1));

            Assert.That(frame.Value, Is.EqualTo(markers[index]));
            Assert.That(frame.Process, Is.EqualTo((global::jpeg.JpegProcess)markers[index]));
            Assert.That(frame.Width, Is.EqualTo(32));
            Assert.That(frame.Height, Is.EqualTo(16));
            Assert.That(frame.ComponentCount, Is.EqualTo(1));
            Assert.That(frame.Components.Count, Is.EqualTo(1));
            index = index + 1;
        }
    }

    [Test]
    public void Parser_PreservesComponentDescriptors()
    {
        global::jpeg.JpegFrameHeaderParser parser = new global::jpeg.JpegFrameHeaderParser();

        global::jpeg.JpegFrameHeader frame = parser.Parse(
            global::jpeg.JpegConstants.MarkerStartOfFrame2,
            IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame2, 3));

        Assert.That(frame.Progressive, Is.True);
        Assert.That(frame.ArithmeticCoding, Is.False);
        Assert.That(frame.Components.Count, Is.EqualTo(3));
        Assert.That(frame.Components[0].Identifier, Is.EqualTo(1));
        Assert.That(frame.Components[0].HorizontalSamplingFactor, Is.EqualTo(2));
        Assert.That(frame.Components[0].VerticalSamplingFactor, Is.EqualTo(2));
        Assert.That(frame.Components[0].QuantizationTableIdentifier, Is.EqualTo(0));
        Assert.That(frame.Components[1].Identifier, Is.EqualTo(2));
        Assert.That(frame.Components[1].QuantizationTableIdentifier, Is.EqualTo(1));
    }

    [Test]
    public void Identifier_StoresParsedFrameHeaderAndComponents()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame14, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame14, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.DifferentialProgressiveDctArithmetic));
        Assert.That(info.Progressive, Is.True);
        Assert.That(info.ArithmeticCoding, Is.True);
        Assert.That(info.Frame.Process, Is.EqualTo(info.Process));
        Assert.That(info.Components.Count, Is.EqualTo(3));
        Assert.That(info.Components[0].HorizontalSamplingFactor, Is.EqualTo(2));
        Assert.That(info.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio420));
    }

    [Test]
    public void Identifier_ReportsUnsupportedDifferentialFrameProcesses()
    {
        int[] markers = new int[]
        {
            global::jpeg.JpegConstants.MarkerStartOfFrame5,
            global::jpeg.JpegConstants.MarkerStartOfFrame6,
            global::jpeg.JpegConstants.MarkerStartOfFrame7,
            global::jpeg.JpegConstants.MarkerStartOfFrame13,
            global::jpeg.JpegConstants.MarkerStartOfFrame14,
            global::jpeg.JpegConstants.MarkerStartOfFrame15
        };
        global::jpeg.JpegProcess[] processes = new global::jpeg.JpegProcess[]
        {
            global::jpeg.JpegProcess.DifferentialSequentialDct,
            global::jpeg.JpegProcess.DifferentialProgressiveDct,
            global::jpeg.JpegProcess.DifferentialLosslessSequential,
            global::jpeg.JpegProcess.DifferentialSequentialDctArithmetic,
            global::jpeg.JpegProcess.DifferentialProgressiveDctArithmetic,
            global::jpeg.JpegProcess.DifferentialLosslessSequentialArithmetic
        };

        int index = 0;
        while (index < markers.Length)
        {
            global::jpeg.JpegInfo info = IdentifyFrameWithMarker(markers[index]);

            Assert.That(info.Process, Is.EqualTo(processes[index]));
            Assert.That(info.Frame.Value, Is.EqualTo(markers[index]));
            Assert.That(info.DiagnosticCollection.WarningCount, Is.EqualTo(1));
            Assert.That(info.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(info.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegDiagnosticCode.UnsupportedProcess));
            Assert.That(info.Diagnostics[0].Severity, Is.EqualTo(global::jpeg.JpegDiagnosticSeverity.Warning));
            Assert.That(info.Diagnostics[0].Marker, Is.EqualTo((global::jpeg.JpegMarker)markers[index]));
            Assert.That(info.Diagnostics[0].Message, Does.Contain("Differential"));
            index = index + 1;
        }
    }

    [Test]
    public void Parser_RejectsMalformedFrameHeaders()
    {
        global::jpeg.JpegFrameHeaderParser parser = new global::jpeg.JpegFrameHeaderParser();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(global::jpeg.JpegConstants.MarkerDefineHuffmanTable, CreateFramePayload(8, 16, 32, 1));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, new byte[] { 8, 0, 1 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, new byte[] { 8, 0, 16, 0, 32, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, new byte[] { 8, 0, 16, 0, 32, 1, 1, 0x11 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, CreateFramePayload(0, 16, 32, 1));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, CreateFramePayload(8, 16, 0, 1));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, CreateFramePayloadWithSampling(0x01));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, CreateFramePayloadWithSampling(0x50));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] payload = CreateFramePayload(8, 16, 32, 1);
                payload[8] = 4;
                parser.Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, payload);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] payload = IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3);
                payload[9] = payload[6];
                parser.Parse(global::jpeg.JpegConstants.MarkerStartOfFrame0, payload);
            });
    }

    public static byte[] CreateFramePayload(int precision, int height, int width, int componentCount)
    {
        byte[] payload = new byte[6 + componentCount * 3];
        payload[0] = (byte)precision;
        global::jpeg.JpegByteWriter.WriteUInt16(payload, 1, height);
        global::jpeg.JpegByteWriter.WriteUInt16(payload, 3, width);
        payload[5] = (byte)componentCount;
        int offset = 6;
        int index = 0;
        while (index < componentCount)
        {
            payload[offset] = (byte)(index + 1);
            payload[offset + 1] = 0x11;
            payload[offset + 2] = 0;
            offset = offset + 3;
            index = index + 1;
        }

        return payload;
    }

    public static byte[] CreateFramePayloadWithSampling(int sampling)
    {
        byte[] payload = CreateFramePayload(8, 16, 32, 1);
        payload[7] = (byte)sampling;
        return payload;
    }

    public static global::jpeg.JpegInfo IdentifyFrameWithMarker(int markerValue)
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegmentValue(markerValue, IdentifierTests.CreateSofPayload(markerValue, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();
        return global::jpeg.JpegIdentifier.Identify(writer.ToArray());
    }
}
