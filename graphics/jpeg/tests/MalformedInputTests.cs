namespace jpeg.Tests;

internal class MalformedInputTests
{
    [Test]
    public void Malformed_TruncatedEntropyDataIsRejected()
    {
        byte[] data = global::jpeg.Jpeg.Write(JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138));
        byte[] truncated = CopyBytes(data, data.Length - 1);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.Jpeg.Read(truncated);
            });
    }

    [Test]
    public void Malformed_InvalidMarkerLengthsAreRejected()
    {
        byte[] data = new byte[]
        {
            0xFF, 0xD8,
            0xFF, 0xE0, 0x00, 0x01
        };

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.Jpeg.Identify(data);
            });
    }

    [Test]
    public void Malformed_MissingQuantizationAndHuffmanTablesAreRejected()
    {
        global::jpeg.JpegBaselineSof0FullImageDecoder decoder = new global::jpeg.JpegBaselineSof0FullImageDecoder();
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore missingQuantization = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        missingQuantization.QuantizationTables.Clear();
        global::jpeg.JpegTableStore missingHuffman = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        missingHuffman.HuffmanTables.Clear();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.Decode(frame, scan, missingQuantization, new byte[0]);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.Decode(frame, scan, missingHuffman, new byte[0]);
            });
    }

    [Test]
    public void Malformed_BadScanComponentSelectorIsRejected()
    {
        byte[] scanPayload = IdentifierTests.CreateSosPayload(3);
        scanPayload[1] = 4;
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, QuantizationTableParserTests.CreateEightBitTablePayload(0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, HuffmanTableParserTests.CreateCanonicalDcPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, scanPayload);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.Jpeg.Read(writer.ToArray());
            });
    }

    [Test]
    public void Malformed_BadRestartMarkerSequenceIsRejected()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.RestartInterval = 1;
        byte[] data = global::jpeg.Jpeg.Write(source, options);
        int restartOffset = FindMarkerOffset(data, global::jpeg.JpegConstants.MarkerRestart0);
        data[restartOffset + 1] = 0xD2;

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.Jpeg.Read(data);
            });
    }

    [Test]
    public void Malformed_MissingRestartMarkerDeclaredByDriIsRejected()
    {
        byte[] data = InsertSegmentBeforeMarker(
            global::jpeg.Jpeg.Write(JpegWriteApiTests.CreateTwoBlockGrayscaleImage()),
            global::jpeg.JpegConstants.MarkerStartOfFrame0,
            global::jpeg.JpegConstants.MarkerDefineRestartInterval,
            new byte[] { 0, 1 });

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);

        Assert.That(info.HasRestartInterval, Is.True);
        Assert.That(info.RestartInterval, Is.EqualTo(1));
        Assert.That(info.RestartMarkerCount, Is.EqualTo(0));
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.Jpeg.Read(data);
            });
    }

    [Test]
    public void Malformed_HugeDimensionsTripConfiguredResourceLimits()
    {
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxWidth = 64;
        options.MaxHeight = 64;
        options.MaxOutputPixels = 4096;
        byte[] data = CreateHugeHeaderJpeg();

        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.Jpeg.Identify(data, options);
            });
    }

    [Test]
    public void Malformed_CorruptMetadataCanBeSkippedWhenPixelsAreReadable()
    {
        byte[] data = InsertSegmentAfterSoi(
            global::jpeg.Jpeg.Write(JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138)),
            global::jpeg.JpegConstants.MarkerApp0,
            new byte[] { 0x4A, 0x46, 0x49, 0x46, 0 });
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.SkipMetadata = true;

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.Jpeg.Identify(data);
            });

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data, options);

        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.Pixels[0], Is.EqualTo(138));
        Assert.That(image.Metadata.RawMarkers.Count, Is.EqualTo(0));
        Assert.That(image.Metadata.HasJfif, Is.False);
    }

    public static byte[] CreateHugeHeaderJpeg()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateSofPayload(65535, 65535));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateSofPayload(int width, int height)
    {
        byte[] payload = new byte[]
        {
            8, 0, 0, 0, 0, 1,
            1, 0x11, 0
        };
        global::jpeg.JpegByteWriter.WriteUInt16(payload, 1, height);
        global::jpeg.JpegByteWriter.WriteUInt16(payload, 3, width);
        return payload;
    }

    public static byte[] CopyBytes(byte[] source, int length)
    {
        byte[] copy = new byte[length];
        System.Array.Copy(source, 0, copy, 0, length);
        return copy;
    }

    public static byte[] InsertSegmentAfterSoi(byte[] data, int markerValue, byte[] payload)
    {
        int segmentLength = payload.Length + 2;
        byte[] result = new byte[data.Length + payload.Length + 4];
        result[0] = data[0];
        result[1] = data[1];
        global::jpeg.JpegByteWriter.WriteUInt16(result, 2, markerValue);
        global::jpeg.JpegByteWriter.WriteUInt16(result, 4, segmentLength);
        System.Array.Copy(payload, 0, result, 6, payload.Length);
        System.Array.Copy(data, 2, result, 6 + payload.Length, data.Length - 2);
        return result;
    }

    public static byte[] InsertSegmentBeforeMarker(byte[] data, int beforeMarkerValue, int markerValue, byte[] payload)
    {
        int markerOffset = FindMarkerOffset(data, beforeMarkerValue);
        if (markerOffset < 0)
        {
            throw new System.InvalidOperationException("Marker was not found.");
        }

        int segmentLength = payload.Length + 2;
        byte[] result = new byte[data.Length + payload.Length + 4];
        System.Array.Copy(data, 0, result, 0, markerOffset);
        global::jpeg.JpegByteWriter.WriteUInt16(result, markerOffset, markerValue);
        global::jpeg.JpegByteWriter.WriteUInt16(result, markerOffset + 2, segmentLength);
        System.Array.Copy(payload, 0, result, markerOffset + 4, payload.Length);
        System.Array.Copy(data, markerOffset, result, markerOffset + 4 + payload.Length, data.Length - markerOffset);
        return result;
    }

    public static int FindMarkerOffset(byte[] data, int markerValue)
    {
        int index = 0;
        while (index + 1 < data.Length)
        {
            int value = (data[index] << 8) | data[index + 1];
            if (value == markerValue)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }
}
