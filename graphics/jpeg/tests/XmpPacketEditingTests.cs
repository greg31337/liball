namespace jpeg.Tests;

internal class XmpPacketEditingTests
{
    [Test]
    public void Identifier_ExposesStandardAndExtendedXmpPacketModels()
    {
        byte[] standardPacket = System.Text.Encoding.UTF8.GetBytes("<standard />");
        byte[] extendedPacket = System.Text.Encoding.UTF8.GetBytes("<extended />");
        byte[] first = new byte[5];
        byte[] second = new byte[extendedPacket.Length - first.Length];
        System.Array.Copy(extendedPacket, 0, first, 0, first.Length);
        System.Array.Copy(extendedPacket, first.Length, second, 0, second.Length);
        byte[][] payloads = new byte[][]
        {
            XmpMetadataParserTests.CreateXmpPayload(standardPacket),
            ExtendedXmpTests.CreateExtendedXmpPayload(ExtendedXmpTests.Guid, extendedPacket.Length, first.Length, second),
            ExtendedXmpTests.CreateExtendedXmpPayload(ExtendedXmpTests.Guid, extendedPacket.Length, 0, first)
        };

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(CreateIdentifySource(payloads));

        Assert.That(info.Metadata.XmpMetadata.PacketData, Is.EqualTo(standardPacket));
        Assert.That(info.Metadata.XmpMetadata.Packet, Is.EqualTo("<standard />"));
        Assert.That(info.Metadata.ExtendedXmpChunks.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.ExtendedXmpChunks[0].RawPayload, Is.EqualTo(payloads[1]));
        Assert.That(info.Metadata.ExtendedXmpMetadata.IsPresent, Is.True);
        Assert.That(info.Metadata.ExtendedXmpMetadata.IsComplete, Is.True);
        Assert.That(info.Metadata.ExtendedXmpMetadata.Guid, Is.EqualTo(ExtendedXmpTests.Guid));
        Assert.That(info.Metadata.ExtendedXmpMetadata.FullLength, Is.EqualTo(extendedPacket.Length));
        Assert.That(info.Metadata.ExtendedXmpMetadata.PacketData, Is.EqualTo(extendedPacket));
        Assert.That(info.Metadata.ExtendedXmpMetadata.Packet, Is.EqualTo("<extended />"));
        Assert.That(info.Metadata.ExtendedXmpMetadata.Chunks.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.ExtendedXmpMetadata.Diagnostics, Is.Empty);
    }

    [Test]
    public void Identifier_ReportsExtendedXmpGapsAndOverlaps()
    {
        byte[][] gapPayloads = new byte[][]
        {
            ExtendedXmpTests.CreateExtendedXmpPayload(ExtendedXmpTests.Guid, 5, 0, new byte[] { 1, 2 })
        };
        byte[][] overlapPayloads = new byte[][]
        {
            ExtendedXmpTests.CreateExtendedXmpPayload(ExtendedXmpTests.Guid, 4, 0, new byte[] { 1, 2, 3 }),
            ExtendedXmpTests.CreateExtendedXmpPayload(ExtendedXmpTests.Guid, 4, 2, new byte[] { 3, 4 })
        };
        byte[][] mismatchPayloads = new byte[][]
        {
            ExtendedXmpTests.CreateExtendedXmpPayload(ExtendedXmpTests.Guid, 4, 0, new byte[] { 1, 2 }),
            ExtendedXmpTests.CreateExtendedXmpPayload("FEDCBA9876543210FEDCBA9876543210", 5, 2, new byte[] { 3, 4, 5 })
        };

        global::jpeg.JpegInfo gap = global::jpeg.Jpeg.Identify(CreateIdentifySource(gapPayloads));
        global::jpeg.JpegInfo overlap = global::jpeg.Jpeg.Identify(CreateIdentifySource(overlapPayloads));
        global::jpeg.JpegInfo mismatch = global::jpeg.Jpeg.Identify(CreateIdentifySource(mismatchPayloads));

        Assert.That(gap.Metadata.HasExtendedXmpChunks, Is.True);
        Assert.That(gap.Metadata.HasExtendedXmp, Is.False);
        Assert.That(gap.Metadata.ExtendedXmpMetadata.IsComplete, Is.False);
        Assert.That(gap.Metadata.ExtendedXmpMetadata.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegXmpReassemblyDiagnosticCode.Gap));
        Assert.That(gap.Metadata.ExtendedXmpMetadata.Diagnostics[0].Offset, Is.EqualTo(2));
        Assert.That(gap.Metadata.ExtendedXmpMetadata.Diagnostics[0].Length, Is.EqualTo(3));
        Assert.That(overlap.Metadata.HasExtendedXmp, Is.False);
        Assert.That(overlap.Metadata.ExtendedXmpMetadata.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegXmpReassemblyDiagnosticCode.Overlap));
        Assert.That(overlap.Metadata.ExtendedXmpMetadata.Diagnostics[0].ChunkIndex, Is.EqualTo(1));
        Assert.That(mismatch.Metadata.HasExtendedXmp, Is.False);
        Assert.That(mismatch.Metadata.ExtendedXmpMetadata.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegXmpReassemblyDiagnosticCode.GuidMismatch));
        Assert.That(mismatch.Metadata.ExtendedXmpMetadata.Diagnostics[1].Code, Is.EqualTo(global::jpeg.JpegXmpReassemblyDiagnosticCode.FullLengthMismatch));
    }

    [Test]
    public void ExtendedXmp_ValidatesGuidAndChunkOffsets()
    {
        global::jpeg.JpegExtendedXmpChunkParser parser = new global::jpeg.JpegExtendedXmpChunkParser();
        byte[] invalidGuid = ExtendedXmpTests.CreateExtendedXmpPayload("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG", 1, 0, new byte[] { 1 });
        byte[] invalidOffset = ExtendedXmpTests.CreateExtendedXmpPayload(ExtendedXmpTests.Guid, 4, 4, new byte[] { 1 });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(invalidGuid);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(invalidOffset);
            });

        global::jpeg.JpegExtendedXmpMetadata replacement = new global::jpeg.JpegExtendedXmpMetadata();
        replacement.Guid = "GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG";
        replacement.PacketData = new byte[] { 1 };
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                new global::jpeg.JpegMetadataEditor().ReplaceExtendedXmp(CreateEditingSource(), replacement);
            });
    }

    [Test]
    public void MetadataEditor_ReplacesAndRemovesStandardAndExtendedXmpIndependently()
    {
        byte[] source = CreateEditingSource();
        global::jpeg.JpegMetadataEditor editor = new global::jpeg.JpegMetadataEditor();
        global::jpeg.JpegXmpMetadata standard = new global::jpeg.JpegXmpMetadata();
        standard.PacketData = System.Text.Encoding.UTF8.GetBytes("<new-standard />");
        global::jpeg.JpegExtendedXmpMetadata extended = new global::jpeg.JpegExtendedXmpMetadata();
        extended.Guid = "FEDCBA9876543210FEDCBA9876543210";
        extended.PacketData = System.Text.Encoding.UTF8.GetBytes("<new-extended />");

        byte[] replacedStandard = editor.ReplaceXmp(source, standard);
        global::jpeg.JpegInfo standardInfo = global::jpeg.Jpeg.Identify(replacedStandard);
        Assert.That(standardInfo.Metadata.XmpMetadata.PacketData, Is.EqualTo(standard.PacketData));
        Assert.That(standardInfo.Metadata.ExtendedXmpMetadata.Packet, Is.EqualTo("<old-extended />"));

        byte[] replacedBoth = editor.ReplaceExtendedXmp(replacedStandard, extended);
        global::jpeg.JpegInfo replacedInfo = global::jpeg.Jpeg.Identify(replacedBoth);
        Assert.That(replacedInfo.Metadata.XmpMetadata.Packet, Is.EqualTo("<new-standard />"));
        Assert.That(replacedInfo.Metadata.ExtendedXmpMetadata.Guid, Is.EqualTo(extended.Guid));
        Assert.That(replacedInfo.Metadata.ExtendedXmpMetadata.PacketData, Is.EqualTo(extended.PacketData));
        global::jpeg.JpegCoefficientImage coefficients = global::jpeg.JpegInspector.ReadCoefficients(replacedBoth);
        Assert.That(JpegMetadataUpdaterTests.GetNaturalCoefficient(coefficients.Components[0], 0, 0), Is.EqualTo(1));

        global::jpeg.JpegInfo withoutStandard = global::jpeg.Jpeg.Identify(editor.RemoveXmp(replacedBoth));
        Assert.That(withoutStandard.Metadata.HasXmp, Is.False);
        Assert.That(withoutStandard.Metadata.HasExtendedXmp, Is.True);
        Assert.That(withoutStandard.Metadata.ExtendedXmp, Is.EqualTo(extended.PacketData));

        global::jpeg.JpegInfo withoutExtended = global::jpeg.Jpeg.Identify(editor.RemoveExtendedXmp(replacedBoth));
        Assert.That(withoutExtended.Metadata.HasXmp, Is.True);
        Assert.That(withoutExtended.Metadata.XmpMetadata.PacketData, Is.EqualTo(standard.PacketData));
        Assert.That(withoutExtended.Metadata.HasExtendedXmpChunks, Is.False);
        Assert.That(withoutExtended.Metadata.HasExtendedXmp, Is.False);
    }

    public static byte[] CreateIdentifySource(byte[][] app1Payloads)
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        foreach (byte[] payload in app1Payloads)
        {
            writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, payload);
        }

        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateEditingSource()
    {
        global::jpeg.JpegCoefficientImage image = JpegLosslessTransformerTests.CreateFourBlockCoefficientImage();
        byte[] standard = XmpMetadataParserTests.CreateXmpPayload(System.Text.Encoding.UTF8.GetBytes("<old-standard />"));
        image.Metadata.RawMarkers.Add(JpegMetadataUpdaterTests.CreateRawMarker(global::jpeg.JpegMarker.App1, global::jpeg.JpegConstants.MarkerApp1, standard));
        global::jpeg.JpegExtendedXmpChunkWriter writer = new global::jpeg.JpegExtendedXmpChunkWriter();
        System.Collections.Generic.List<global::jpeg.JpegExtendedXmpChunk> chunks = writer.CreateChunks(System.Text.Encoding.UTF8.GetBytes("<old-extended />"), ExtendedXmpTests.Guid);
        foreach (global::jpeg.JpegExtendedXmpChunk chunk in chunks)
        {
            image.Metadata.RawMarkers.Add(JpegMetadataUpdaterTests.CreateRawMarker(global::jpeg.JpegMarker.App1, global::jpeg.JpegConstants.MarkerApp1, writer.WritePayload(chunk)));
        }

        return global::jpeg.JpegInspector.WriteCoefficients(image);
    }
}
