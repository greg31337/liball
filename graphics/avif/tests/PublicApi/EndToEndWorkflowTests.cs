using System.Buffers.Binary;
using System.Text;

namespace avif.Tests;

public class EndToEndWorkflowTests
{
    [Test]
    public void OneCallStillPackedAndTranscodeWorkflowPreservesPixels()
    {
        IAvifCodec codec = new AvifCodec();
        AvifPixelBuffer source = new AvifPixelBuffer(
            3,
            2,
            AvifPixelFormat.Gray,
            8,
            false,
            1,
            new AvifSecurityLimits());
        new byte[] { 0, 64, 255 }.AsSpan().CopyTo(source.GetRowSpan(0));
        new byte[] { 13, 127, 240 }.AsSpan().CopyTo(source.GetRowSpan(1));
        AvifPackedEncodeOptions packedOptions =
            new AvifPackedEncodeOptions();
        packedOptions.ChromaFormat = AvifChromaFormat.Monochrome;
        packedOptions.AlphaMode = AvifAlphaMode.None;
        packedOptions.EncoderOptions = CreateLosslessOptions();

        AvifEncodeResult encoded = codec.EncodePacked(source, packedOptions);
        byte[] encodedBytes = encoded.GetData();
        AvifProbeResult probe = codec.ProbeBytes(encodedBytes);
        AvifValidationResult validation = codec.ValidateBytes(
            encodedBytes,
            new AvifValidationOptions());
        AvifReadResult inspection = codec.InspectBytes(
            encodedBytes,
            new AvifValidationOptions());
        AvifPackedDecodeOptions decodeOptions =
            new AvifPackedDecodeOptions();
        decodeOptions.PixelFormat = AvifPixelFormat.Gray;
        AvifPixelBuffer decoded = codec.DecodePackedBytes(
            encodedBytes,
            decodeOptions);
        AvifEncodeResult transcoded = codec.TranscodeBytes(
            encodedBytes,
            new AvifDecoderOptions(),
            CreateLosslessOptions());
        AvifPixelBuffer transcodedPixels = codec.DecodePackedBytes(
            transcoded.GetData(),
            decodeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(probe.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(probe.IsAvif(), Is.True);
            Assert.That(validation.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(inspection.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(Encoding.ASCII.GetString(encodedBytes, 4, 4),
                Is.EqualTo("ftyp"));
            Assert.That(Encoding.ASCII.GetString(encodedBytes, 8, 4),
                Is.EqualTo("avif"));
            Assert.That(decoded.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(transcoded.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(transcodedPixels.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.GetRowSpan(0).ToArray(),
                Is.EqualTo(source.GetRowSpan(0).ToArray()));
            Assert.That(decoded.GetRowSpan(1).ToArray(),
                Is.EqualTo(source.GetRowSpan(1).ToArray()));
            Assert.That(transcodedPixels.GetRowSpan(0).ToArray(),
                Is.EqualTo(source.GetRowSpan(0).ToArray()));
            Assert.That(transcodedPixels.GetRowSpan(1).ToArray(),
                Is.EqualTo(source.GetRowSpan(1).ToArray()));
        });

        transcodedPixels.Close();
        decoded.Close();
        source.Close();
    }

    [Test]
    public void StatefulCollectionGridAndLayerWorkflowsExposeSelections()
    {
        AvifImage first = CreateImage(2, 1, 17);
        AvifImage second = CreateImage(2, 1, 91);
        AvifWriter collectionWriter = new AvifWriter(
            CreateLosslessOptions());
        uint firstId = collectionWriter.AddImage(first);
        uint secondId = collectionWriter.AddImage(second);
        byte[] metadataBytes = Encoding.UTF8.GetBytes("{\"name\":\"second\"}");
        uint metadataId = collectionWriter.AddMetadata(
            secondId,
            new AvifMetadata(
                "mime",
                "application/json",
                metadataBytes));
        AvifEncodeResult collection = collectionWriter.Finish();
        first.Close();
        second.Close();
        AvifReader collectionReader = new AvifReader(
            collection.GetData(),
            new AvifDecoderOptions());
        AvifResultCode collectionParse = collectionReader.Parse();
        AvifDecodeResult selected = collectionReader.DecodeItem(secondId);
        AvifMetadata? selectedMetadata =
            collectionReader.GetAssociatedMetadataItem(secondId, 0);

        AvifImage left = CreateImage(2, 1, 11);
        AvifImage right = CreateImage(2, 1, 29);
        AvifGrid grid = new AvifGrid(
            1,
            2,
            3,
            1,
            new AvifSecurityLimits());
        AvifResultCode leftSet = grid.SetCell(0, left);
        AvifResultCode rightSet = grid.SetCell(1, right);
        AvifWriter gridWriter = new AvifWriter(CreateLosslessOptions());
        uint gridId = gridWriter.AddGrid(grid);
        AvifEncodeResult gridFile = gridWriter.Finish();
        left.Close();
        right.Close();
        AvifReader gridReader = new AvifReader(
            gridFile.GetData(),
            new AvifDecoderOptions());
        AvifResultCode gridParse = gridReader.Parse();
        AvifDecodeResult renderedGrid = gridReader.DecodePrimaryImage();
        AvifDecodeResult selectedTile = gridReader.DecodeTile(gridId, 0, 1);

        AvifImage preview = CreateImage(2, 1, 37);
        AvifImage finalImage = CreateImage(2, 1, 89);
        AvifLayeredImage layers = new AvifLayeredImage(0, true);
        AvifResultCode previewAdded = layers.AddLayer(preview);
        AvifResultCode finalAdded = layers.AddLayer(finalImage);
        AvifWriter layerWriter = new AvifWriter(CreateLosslessOptions());
        uint layeredId = layerWriter.AddLayeredImage(layers);
        AvifEncodeResult layeredFile = layerWriter.Finish();
        preview.Close();
        finalImage.Close();
        AvifReader layerReader = new AvifReader(
            layeredFile.GetData(),
            new AvifDecoderOptions());
        AvifResultCode layerParse = layerReader.Parse();
        AvifDecodeResult decodedPreview = layerReader.DecodeItemLayer(
            layeredId,
            0);
        AvifDecodeResult decodedFinal = layerReader.DecodePrimaryImage();

        Assert.Multiple(() =>
        {
            Assert.That(collection.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(collectionParse,
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(firstId, Is.EqualTo(1));
            Assert.That(secondId, Is.EqualTo(2));
            Assert.That(metadataId, Is.EqualTo(3));
            Assert.That(selected.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That((selectedMetadata
                ?? throw new InvalidOperationException("Missing metadata."))
                .GetData(), Is.EqualTo(metadataBytes));
            Assert.That(leftSet, Is.EqualTo(AvifResultCode.Success));
            Assert.That(rightSet, Is.EqualTo(AvifResultCode.Success));
            Assert.That(gridFile.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(gridParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(renderedGrid.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(selectedTile.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(previewAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(finalAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(layeredFile.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(layerParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decodedPreview.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(decodedFinal.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
        });
        AssertPlaneValue(selected.GetImage(), 91);
        AssertRow(renderedGrid.GetImage(), new byte[] { 11, 11, 29 });
        AssertPlaneValue(selectedTile.GetImage(), 29);
        AssertPlaneValue(decodedPreview.GetImage(), 37);
        AssertPlaneValue(decodedFinal.GetImage(), 89);

        CloseResult(decodedFinal);
        CloseResult(decodedPreview);
        layerReader.Close();
        layerWriter.Close();
        CloseResult(selectedTile);
        CloseResult(renderedGrid);
        gridReader.Close();
        gridWriter.Close();
        CloseResult(selected);
        collectionReader.Close();
        collectionWriter.Close();
    }

    [Test]
    public void GainMapAndSampleTransformWorkflowsReconstructDerivedPixels()
    {
        AvifImage baseImage = CreateImage(2, 1, 41);
        AvifImage gainImage = CreateImage(1, 1, 173);
        AvifGainMap gainMap = new AvifGainMap(
            gainImage,
            CreateGainMapMetadata(),
            null);
        AvifWriter gainWriter = new AvifWriter(CreateLosslessOptions());
        uint baseId = gainWriter.AddImage(baseImage);
        uint toneMapId = gainWriter.AddGainMap(baseId, gainMap);
        AvifEncodeResult gainFile = gainWriter.Finish();
        baseImage.Close();
        gainImage.Close();
        AvifReader gainReader = new AvifReader(
            gainFile.GetData(),
            new AvifDecoderOptions());
        AvifResultCode gainParse = gainReader.Parse();
        AvifDecodeResult decodedToneMap =
            gainReader.DecodeToneMap(toneMapId);

        AvifImage high = CreateImage(2, 1, 17);
        AvifImage low = CreateImage(2, 1, 23);
        byte[] expression = new byte[] { 2, 3, 1, 2, 128 };
        AvifSampleTransform authoredTransform = new AvifSampleTransform(
            expression,
            new AvifImage[] { high, low },
            12);
        AvifWriter transformWriter = new AvifWriter(
            CreateLosslessOptions());
        uint transformId = transformWriter.AddSampleTransform(
            authoredTransform);
        AvifEncodeResult transformFile = transformWriter.Finish();
        high.Close();
        low.Close();
        AvifReader transformReader = new AvifReader(
            transformFile.GetData(),
            new AvifDecoderOptions());
        AvifResultCode transformParse = transformReader.Parse();
        AvifDocument transformDocument = transformReader.GetDocument()
            ?? throw new InvalidOperationException("Missing document.");
        AvifImageItem transformItem = transformDocument.FindItem(transformId)
            ?? throw new InvalidOperationException("Missing transform item.");
        AvifItemReference dependency = transformItem.GetOutgoingReference(0)
            ?? throw new InvalidOperationException("Missing dependencies.");
        AvifDecodeResult decodedHigh = transformReader.DecodeItem(
            dependency.GetTargetItemId(0));
        AvifDecodeResult decodedLow = transformReader.DecodeItem(
            dependency.GetTargetItemId(1));
        AvifImage decodedHighImage = decodedHigh.GetImage()
            ?? throw new InvalidOperationException("Missing high image.");
        AvifImage decodedLowImage = decodedLow.GetImage()
            ?? throw new InvalidOperationException("Missing low image.");
        AvifSampleTransform reparsedTransform = new AvifSampleTransform(
            transformReader.GetEncodedItem(transformId),
            new AvifImage[] { decodedHighImage, decodedLowImage },
            transformItem.GetPixelBitDepth());
        AvifImage reconstructed = reparsedTransform.Render(
            new AvifImage[] { decodedHighImage, decodedLowImage },
            transformItem.GetPixelBitDepth());

        Assert.Multiple(() =>
        {
            Assert.That(gainFile.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(gainParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decodedToneMap.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(decodedToneMap.GetToneMap(), Is.Not.Null);
            Assert.That(decodedToneMap.GetGainMap(), Is.Not.Null);
            Assert.That(transformFile.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(transformParse,
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(transformItem.GetItemType(), Is.EqualTo("sato"));
            Assert.That(dependency.GetKind(),
                Is.EqualTo(AvifItemReferenceKind.DerivedImage));
            Assert.That(reparsedTransform.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(reconstructed.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(reconstructed, 0), Is.EqualTo(40));
            Assert.That(ReadSample(reconstructed, 1), Is.EqualTo(40));
        });
        AssertPlaneValue(decodedToneMap.GetImage(), 41);
        AssertPlaneValue(
            (decodedToneMap.GetGainMap()
                ?? throw new InvalidOperationException("Missing gain map."))
                .GetImage(),
            173);

        reconstructed.Close();
        CloseResult(decodedLow);
        CloseResult(decodedHigh);
        transformReader.Close();
        transformWriter.Close();
        CloseToneMapResult(decodedToneMap);
        gainReader.Close();
        gainWriter.Close();
    }

    [Test]
    public void TimedSequenceCompletesAfterExactRangeFulfillment()
    {
        AvifImage first = CreateImage(2, 1, 31);
        AvifImage second = CreateImage(2, 1, 201);
        AvifSequence sequence = new AvifSequence(1000);
        AvifResultCode firstAdded = sequence.AddFrame(
            new AvifFrame(first, null, 40, true));
        AvifResultCode secondAdded = sequence.AddFrame(
            new AvifFrame(second, null, 60, true));
        AvifWriter writer = new AvifWriter(CreateLosslessOptions());
        uint trackId = writer.AddSequence(sequence);
        AvifEncodeResult encoded = writer.Finish();
        first.Close();
        second.Close();
        EndToEndRangeSource source = new EndToEndRangeSource(
            encoded.GetData(),
            12,
            9);
        IAvifCodec codec = new AvifCodec();
        AvifReader reader = codec.CreateRangeReader(
            source,
            new AvifDecoderOptions());

        AvifResultCode waiting = reader.Parse();
        AvifByteRange[] missing = reader.GetMissingRanges();
        source.Fulfill();
        AvifResultCode parsed = reader.Parse();
        AvifDecodeResult frame = reader.DecodeFrame(1);
        AvifFrameTiming? timing = reader.GetFrameTiming(1);

        Assert.Multiple(() =>
        {
            Assert.That(firstAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(trackId, Is.EqualTo(1));
            Assert.That(encoded.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(waiting, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(missing, Has.Length.EqualTo(1));
            Assert.That(missing[0].GetOffset(), Is.EqualTo(12));
            Assert.That(missing[0].GetLength(), Is.EqualTo(9));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.GetMissingRanges(), Is.Empty);
            Assert.That(frame.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(frame.GetSelectedTrackId(), Is.EqualTo(trackId));
            Assert.That(frame.GetSelectedFrameIndex(), Is.EqualTo(1));
            Assert.That((timing
                ?? throw new InvalidOperationException("Missing timing."))
                .GetPresentationTime(), Is.EqualTo(40));
            Assert.That(timing.GetDuration(), Is.EqualTo(60));
            Assert.That(timing.GetTimescale(), Is.EqualTo(1000));
        });
        AssertPlaneValue(frame.GetImage(), 201);

        CloseResult(frame);
        reader.Close();
        writer.Close();
    }

    [Test]
    public void MetadataRemuxPreservesOriginalAv1PayloadExactly()
    {
        AvifImage sourceImage = CreateImage(3, 2, 67);
        AvifWriter writer = new AvifWriter(CreateLosslessOptions());
        uint imageId = writer.AddImage(sourceImage);
        AvifEncodeResult sourceFile = writer.Finish();
        sourceImage.Close();
        AvifReader sourceReader = new AvifReader(
            sourceFile.GetData(),
            new AvifDecoderOptions());
        AvifResultCode sourceParse = sourceReader.Parse();
        byte[] originalPayload = sourceReader.GetEncodedItem(imageId);
        sourceReader.Close();

        byte[] metadataBytes = Encoding.UTF8.GetBytes("lossless-remux");
        AvifRewriter rewriter = new AvifRewriter(
            new AvifRewriteOptions());
        AvifResultCode opened = rewriter.Open(sourceFile.GetData());
        uint metadataId = rewriter.AddMetadata(
            imageId,
            new AvifMetadata(
                "mime",
                "text/plain",
                metadataBytes));
        AvifEncodeResult remuxed = rewriter.Finish();
        AvifReader remuxedReader = new AvifReader(
            remuxed.GetData(),
            new AvifDecoderOptions());
        AvifResultCode remuxedParse = remuxedReader.Parse();
        AvifDecodeResult decoded = remuxedReader.DecodePrimaryImage();
        AvifMetadata? metadata =
            remuxedReader.GetAssociatedMetadataItem(imageId, 0);

        Assert.Multiple(() =>
        {
            Assert.That(sourceFile.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(sourceParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(opened, Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadataId, Is.Not.Zero);
            Assert.That(remuxed.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(remuxedParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(remuxedReader.GetEncodedItem(imageId),
                Is.EqualTo(originalPayload));
            Assert.That((metadata
                ?? throw new InvalidOperationException("Missing metadata."))
                .GetData(), Is.EqualTo(metadataBytes));
            Assert.That(decoded.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
        });
        AssertPlaneValue(decoded.GetImage(), 67);

        CloseResult(decoded);
        remuxedReader.Close();
        rewriter.Close();
        writer.Close();
    }

    static AvifEncoderOptions CreateLosslessOptions()
    {
        AvifEncoderOptions options = new AvifEncoderOptions();
        options.Mode = AvifEncoderMode.Lossless;
        options.Preset = AvifEncoderPreset.Fastest;
        return options;
    }

    static AvifImage CreateImage(int width, int height, byte value)
    {
        AvifImage image = new AvifImage(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
        AvifPlane plane = image.GetPlane(AvifPlaneKind.Y)
            ?? throw new InvalidOperationException("Missing luma plane.");
        int row = 0;
        while (row < plane.GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    static AvifGainMapMetadata CreateGainMapMetadata()
    {
        byte[] payload = new byte[62];
        payload[5] = 64;
        int offset = 6;
        WriteUnsigned(payload, offset, 0, 1);
        offset += 8;
        WriteUnsigned(payload, offset, 1, 1);
        offset += 8;
        WriteSigned(payload, offset, 0, 1);
        offset += 8;
        WriteSigned(payload, offset, 1, 1);
        offset += 8;
        WriteUnsigned(payload, offset, 1, 1);
        offset += 8;
        WriteSigned(payload, offset, 0, 1);
        offset += 8;
        WriteSigned(payload, offset, 0, 1);
        return new AvifGainMapMetadata(payload);
    }

    static void WriteSigned(
        byte[] payload,
        int offset,
        int numerator,
        uint denominator)
    {
        BinaryPrimitives.WriteInt32BigEndian(
            payload.AsSpan(offset, 4),
            numerator);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset + 4, 4),
            denominator);
    }

    static void WriteUnsigned(
        byte[] payload,
        int offset,
        uint numerator,
        uint denominator)
    {
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset, 4),
            numerator);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset + 4, 4),
            denominator);
    }

    static int ReadSample(AvifImage image, int column)
    {
        AvifPlane plane = image.GetPlane(AvifPlaneKind.Y)
            ?? throw new InvalidOperationException("Missing luma plane.");
        return BinaryPrimitives.ReadUInt16LittleEndian(
            plane.GetRowSpan(0).Slice(column * 2, 2));
    }

    static void AssertPlaneValue(AvifImage? image, byte expected)
    {
        AvifPlane plane = (image
            ?? throw new InvalidOperationException("Missing image."))
            .GetPlane(AvifPlaneKind.Y)
            ?? throw new InvalidOperationException("Missing luma plane.");
        int row = 0;
        while (row < plane.GetHeight())
        {
            int column = 0;
            while (column < plane.GetWidth())
            {
                Assert.That(plane.GetRowSpan(row)[column],
                    Is.EqualTo(expected));
                column++;
            }
            row++;
        }
    }

    static void AssertRow(AvifImage? image, byte[] expected)
    {
        AvifPlane plane = (image
            ?? throw new InvalidOperationException("Missing image."))
            .GetPlane(AvifPlaneKind.Y)
            ?? throw new InvalidOperationException("Missing luma plane.");
        Assert.That(plane.GetRowSpan(0).ToArray(), Is.EqualTo(expected));
    }

    static void CloseResult(AvifDecodeResult result)
    {
        AvifImage? image = result.GetImage();
        if (image != null)
        {
            image.Close();
        }
    }

    static void CloseToneMapResult(AvifDecodeResult result)
    {
        AvifImage? image = result.GetImage();
        AvifImage? gainImage = result.GetGainMap() == null
            ? null
            : (result.GetGainMap()
                ?? throw new InvalidOperationException("Missing gain map."))
                .GetImage();
        if (image != null)
        {
            image.Close();
        }
        if (gainImage != null && gainImage != image)
        {
            gainImage.Close();
        }
    }
}

internal class EndToEndRangeSource : IAvifRandomAccessSource
{
    byte[] data;
    long missingOffset;
    long missingLength;
    bool missing;

    internal EndToEndRangeSource(
        byte[] data,
        long missingOffset,
        long missingLength)
    {
        this.data = data;
        this.missingOffset = missingOffset;
        this.missingLength = missingLength;
        missing = true;
    }

    public long GetLength()
    {
        return data.LongLength;
    }

    public AvifRandomAccessReadResult? ReadRange(
        long offset,
        Span<byte> destination)
    {
        long end = offset + destination.Length;
        long missingEnd = missingOffset + missingLength;
        if (missing
            && missingOffset < end
            && missingEnd > offset)
        {
            long overlapOffset = missingOffset > offset
                ? missingOffset
                : offset;
            long overlapEnd = missingEnd < end ? missingEnd : end;
            return new AvifRandomAccessReadResult(
                AvifResultCode.NeedMoreData,
                0,
                overlapOffset,
                overlapEnd - overlapOffset);
        }
        data.AsSpan((int)offset, destination.Length).CopyTo(destination);
        return new AvifRandomAccessReadResult(
            AvifResultCode.Success,
            destination.Length,
            0,
            0);
    }

    internal void Fulfill()
    {
        missing = false;
    }
}
