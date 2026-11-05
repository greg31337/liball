namespace jpeg.Tests;

internal class CoreParsingIntegrationTests
{
    [Test]
    public void MarkerSegmentReader_ReportsOffsetsAcrossHeaderSegmentsAndEntropyMarkers()
    {
        byte[] data = new byte[]
        {
            0xFF, 0xD8,
            0xFF, 0xE1, 0x00, 0x04, 0x12, 0x34,
            0xFF, 0xDA, 0x00, 0x02,
            0x11, 0xFF, 0x00, 0x22,
            0xFF, 0xD0
        };
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(data);

        global::jpeg.JpegMarkerSegment soi = reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment app1 = reader.ReadNextSegment();
        global::jpeg.JpegMarkerSegment sos = reader.ReadNextSegment();
        global::jpeg.JpegMarkerSegment restart = reader.ReadEntropyMarkerSegment();

        Assert.That(soi.Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfImage));
        Assert.That(soi.Order, Is.EqualTo(0));
        Assert.That(soi.MarkerOffset, Is.EqualTo(0));
        Assert.That(soi.EndOffset, Is.EqualTo(2));

        Assert.That(app1.Marker, Is.EqualTo(global::jpeg.JpegMarker.App1));
        Assert.That(app1.Order, Is.EqualTo(1));
        Assert.That(app1.HasLength, Is.True);
        Assert.That(app1.MarkerOffset, Is.EqualTo(2));
        Assert.That(app1.LengthOffset, Is.EqualTo(4));
        Assert.That(app1.PayloadOffset, Is.EqualTo(6));
        Assert.That(app1.EndOffset, Is.EqualTo(8));
        Assert.That(app1.Payload, Is.EqualTo(new byte[] { 0x12, 0x34 }));

        Assert.That(sos.Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfScan));
        Assert.That(sos.Order, Is.EqualTo(2));
        Assert.That(sos.PayloadLength, Is.EqualTo(0));
        Assert.That(sos.EndOffset, Is.EqualTo(12));

        Assert.That(restart.Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart0));
        Assert.That(restart.Order, Is.EqualTo(3));
        Assert.That(restart.HasLength, Is.False);
        Assert.That(restart.FoundAfterEntropyData, Is.True);
        Assert.That(restart.EntropyBytesSkipped, Is.EqualTo(4));
        Assert.That(restart.MarkerOffset, Is.EqualTo(16));
        Assert.That(restart.EndOffset, Is.EqualTo(18));
    }

    [Test]
    public void Identifier_ParsesMixedMetadataSegmentsAndPreservesRawMarkers()
    {
        byte[] data = MetadataPolicyTests.CreateMetadataHeavyJpeg();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(data);

        Assert.That(info.Metadata.HasJfif, Is.True);
        Assert.That(info.Metadata.JfifMajorVersion, Is.EqualTo(1));
        Assert.That(info.Metadata.JfifMinorVersion, Is.EqualTo(2));
        Assert.That(info.Metadata.DensityUnit, Is.EqualTo(1));
        Assert.That(info.Metadata.XDensity, Is.EqualTo(72));
        Assert.That(info.Metadata.YDensity, Is.EqualTo(96));
        Assert.That(info.Metadata.HasExif, Is.True);
        Assert.That(info.Metadata.ExifMetadata.IsPresent, Is.True);
        Assert.That(info.Metadata.HasXmp, Is.True);
        Assert.That(System.Text.Encoding.UTF8.GetString(info.Metadata.Xmp), Does.Contain("<x />"));
        Assert.That(info.Metadata.HasMpf, Is.True);
        Assert.That(info.Metadata.HasPhotoshop, Is.True);
        Assert.That(info.Metadata.HasIptc, Is.True);
        Assert.That(info.Metadata.HasIccProfileChunks, Is.True);
        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(info.Metadata.IccProfile, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(info.Metadata.HasAdobe, Is.True);
        Assert.That(info.Metadata.AdobeTransform, Is.EqualTo(global::jpeg.JpegConstants.AdobeTransformYCbCr));
        Assert.That(info.Metadata.Comments, Is.EqualTo(new string[] { "note" }));
        Assert.That(MetadataPolicyTests.ContainsRawMarker(info.Metadata, 0xFFF0), Is.True);
        Assert.That(info.AppSegmentCount, Is.EqualTo(7));
        Assert.That(info.CommentCount, Is.EqualTo(1));
    }

    [Test]
    public void TableParsers_ReadMultipleTablesAndRestartIntervalPayloads()
    {
        global::jpeg.JpegQuantizationTableParser quantizationParser = new global::jpeg.JpegQuantizationTableParser();
        System.Collections.Generic.List<global::jpeg.JpegQuantizationTable> quantizationTables =
            quantizationParser.Parse(CreateTwoQuantizationTablePayload());

        Assert.That(quantizationTables.Count, Is.EqualTo(2));
        Assert.That(quantizationTables[0].Identifier, Is.EqualTo(0));
        Assert.That(quantizationTables[0].Precision, Is.EqualTo(8));
        Assert.That(quantizationTables[0].ZigZagValues[0], Is.EqualTo(1));
        Assert.That(quantizationTables[0].Values[global::jpeg.JpegConstants.ZigZagOrder[5]], Is.EqualTo(6));
        Assert.That(quantizationTables[1].Identifier, Is.EqualTo(1));
        Assert.That(quantizationTables[1].Is16Bit, Is.True);
        Assert.That(quantizationTables[1].Precision, Is.EqualTo(16));
        Assert.That(quantizationTables[1].ZigZagValues[0], Is.EqualTo(300));
        Assert.That(quantizationTables[1].Values[global::jpeg.JpegConstants.ZigZagOrder[63]], Is.EqualTo(363));

        global::jpeg.JpegHuffmanTableParser huffmanParser = new global::jpeg.JpegHuffmanTableParser();
        System.Collections.Generic.List<global::jpeg.JpegHuffmanTable> huffmanTables =
            huffmanParser.Parse(CreateTwoHuffmanTablePayload());

        Assert.That(huffmanTables.Count, Is.EqualTo(2));
        Assert.That(huffmanTables[0].TableClass, Is.EqualTo(0));
        Assert.That(huffmanTables[0].Identifier, Is.EqualTo(0));
        Assert.That(huffmanTables[0].SymbolCount, Is.EqualTo(2));
        Assert.That(huffmanTables[0].Codes, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(huffmanTables[0].CodeLengths, Is.EqualTo(new int[] { 2, 2 }));
        Assert.That(huffmanParser.DecodeSymbol(huffmanTables[0], 1, 2), Is.EqualTo(5));
        Assert.That(huffmanTables[1].TableClass, Is.EqualTo(1));
        Assert.That(huffmanTables[1].Identifier, Is.EqualTo(1));
        Assert.That(huffmanParser.DecodeSymbol(huffmanTables[1], 0, 1), Is.EqualTo(0xF0));

        global::jpeg.JpegRestartIntervalParser restartParser = new global::jpeg.JpegRestartIntervalParser();
        Assert.That(restartParser.Parse(new byte[] { 0x01, 0x2C }), Is.EqualTo(300));

        global::jpeg.JpegArithmeticConditioningTableParser arithmeticParser =
            new global::jpeg.JpegArithmeticConditioningTableParser();
        System.Collections.Generic.List<global::jpeg.JpegArithmeticConditioningTable> arithmeticTables =
            arithmeticParser.Parse(new byte[] { 0x00, 0x95, 0x11, 0x3F });

        Assert.That(arithmeticTables.Count, Is.EqualTo(2));
        Assert.That(arithmeticTables[0].TableClass, Is.EqualTo(0));
        Assert.That(arithmeticTables[0].Identifier, Is.EqualTo(0));
        Assert.That(arithmeticTables[0].Lower, Is.EqualTo(5));
        Assert.That(arithmeticTables[0].Upper, Is.EqualTo(9));
        Assert.That(arithmeticTables[1].TableClass, Is.EqualTo(1));
        Assert.That(arithmeticTables[1].Identifier, Is.EqualTo(1));
        Assert.That(arithmeticTables[1].Kx, Is.EqualTo(63));
    }

    [Test]
    public void BitIo_RoundTripsStuffedBytesAndLeavesStandaloneMarkerLookahead()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(0xFF, 8);
        writer.WriteBits(0xA, 4);
        writer.WriteMarkerAfterByteAlign(global::jpeg.JpegConstants.MarkerEndOfImage);

        byte[] encoded = writer.ToArray();
        Assert.That(encoded, Is.EqualTo(new byte[] { 0xFF, 0x00, 0xAF, 0xFF, 0xD9 }));
        Assert.That(writer.StuffedByteCount, Is.EqualTo(1));

        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(encoded);
        Assert.That(reader.ReadBits(8), Is.EqualTo(0xFF));
        Assert.That(reader.StuffedByteCount, Is.EqualTo(1));
        Assert.That(reader.ReadBits(4), Is.EqualTo(0xA));
        reader.ByteAlign();

        Assert.That(reader.EnsureBits(1), Is.False);
        global::jpeg.JpegScannedMarker marker = reader.GetMarkerLookahead();
        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.EndOfImage));
        Assert.That(marker.Offset, Is.EqualTo(3));
        Assert.That(marker.EntropyBytesSkipped, Is.EqualTo(3));
    }

    public static byte[] CreateTwoQuantizationTablePayload()
    {
        System.Collections.Generic.List<byte> payload = new System.Collections.Generic.List<byte>();
        payload.Add(0x00);
        int index = 0;
        while (index < 64)
        {
            payload.Add((byte)(index + 1));
            index = index + 1;
        }

        payload.Add(0x11);
        index = 0;
        while (index < 64)
        {
            int value = 300 + index;
            payload.Add((byte)(value >> 8));
            payload.Add((byte)(value & 255));
            index = index + 1;
        }

        return payload.ToArray();
    }

    public static byte[] CreateTwoHuffmanTablePayload()
    {
        System.Collections.Generic.List<byte> payload = new System.Collections.Generic.List<byte>();
        payload.Add(0x00);
        int index = 0;
        while (index < 16)
        {
            if (index == 1)
            {
                payload.Add(2);
            }
            else
            {
                payload.Add(0);
            }

            index = index + 1;
        }

        payload.Add(3);
        payload.Add(5);
        payload.Add(0x11);
        index = 0;
        while (index < 16)
        {
            if (index == 0)
            {
                payload.Add(1);
            }
            else
            {
                payload.Add(0);
            }

            index = index + 1;
        }

        payload.Add(0xF0);
        return payload.ToArray();
    }
}
