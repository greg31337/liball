using System.Buffers.Binary;

namespace avif.Tests;

public class AvifPayloadExtractionTests
{
    [Test]
    public void ItemAndOpaqueMetadataExtractionsAreBoundedOwnedAndMapped()
    {
        AvifImage image = CreateImage(3, 2, 37);
        AvifWriter writer = AvifWriter.Create();
        uint imageId = writer.AddImage(image);
        uint metadataId = writer.AddMetadata(
            imageId,
            new AvifMetadata(
                "mime",
                "application/example",
                new byte[] { 3, 1, 4, 1, 5 }));
        image.Close();
        AvifEncodeResult encoded = writer.Finish();
        byte[] source = encoded.GetData();
        AvifReader reader = AvifReader.Create(source);
        reader.Parse();

        AvifPayloadExtraction item = reader.ExtractItemPayload(
            imageId,
            65536);
        AvifPayloadExtraction segment = reader.ExtractItemPayloadSegment(
            imageId,
            0,
            65536);
        AvifPayloadExtraction opaque = reader.ExtractUnknownMetadata(
            metadataId,
            5);
        AvifPayloadExtraction limited = reader.ExtractUnknownMetadata(
            metadataId,
            4);
        byte[] firstCopy = item.GetData();
        byte original = firstCopy[0];
        firstCopy[0] ^= 255;

        Assert.Multiple(() =>
        {
            Assert.That(item.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(item.GetItemId(), Is.EqualTo(imageId));
            Assert.That(item.GetSourceSegmentCount(), Is.GreaterThan(0));
            Assert.That(item.GetData()[0], Is.EqualTo(original));
            Assert.That(segment.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(segment.GetSourceSegmentCount(), Is.EqualTo(1));
            Assert.That(opaque.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(opaque.GetData(), Is.EqualTo(new byte[] { 3, 1, 4, 1, 5 }));
            Assert.That(limited.GetCode(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(limited.GetData(), Is.Empty);
        });
        AssertSourceMappings(source, item);
        AssertSourceMappings(source, segment);
        AssertSourceMappings(source, opaque);

        reader.Close();
        writer.Close();
    }

    [Test]
    public void TrackConfigurationObuExtractionCoversLifecycleBoundsAndMissingSelections()
    {
        AvifImage image = CreateImage(3, 2, 61);
        AvifSequence sequence = new AvifSequence(1000);
        sequence.AddFrame(new AvifFrame(image, null, 100, true));
        AvifWriter writer = AvifWriter.Create();
        uint trackId = writer.AddSequence(sequence);
        image.Close();
        AvifEncodeResult encoded = writer.Finish();
        byte[] source = encoded.GetData();
        AvifReader reader = AvifReader.Create(source);

        AvifPayloadExtraction invalidState =
            reader.ExtractTrackConfigurationObu(trackId, 1, 0, 65536);
        Assert.That(invalidState.GetCode(), Is.EqualTo(AvifResultCode.InvalidState));
        Assert.That(reader.Parse(), Is.EqualTo(AvifResultCode.Success));

        AvifPayloadExtraction extracted =
            reader.ExtractTrackConfigurationObu(trackId, 1, 0, 65536);
        AvifPayloadExtraction limited = reader.ExtractTrackConfigurationObu(
            trackId,
            1,
            0,
            extracted.GetDataLength() - 1);
        AvifPayloadExtraction invalidTrack =
            reader.ExtractTrackConfigurationObu(0, 1, 0, 65536);
        AvifPayloadExtraction invalidDescription =
            reader.ExtractTrackConfigurationObu(trackId, 0, 0, 65536);
        AvifPayloadExtraction invalidObu =
            reader.ExtractTrackConfigurationObu(trackId, 1, -1, 65536);
        AvifPayloadExtraction missingDescription =
            reader.ExtractTrackConfigurationObu(trackId, 2, 0, 65536);
        AvifPayloadExtraction missingObu =
            reader.ExtractTrackConfigurationObu(trackId, 1, 1, 65536);

        Assert.Multiple(() =>
        {
            Assert.That(extracted.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(extracted.GetTrackId(), Is.EqualTo(trackId));
            Assert.That(extracted.GetItemId(), Is.Zero);
            Assert.That(
                extracted.GetObuType(),
                Is.EqualTo((int)Av1ObuType.SequenceHeader));
            Assert.That(extracted.GetSourceSegmentCount(), Is.EqualTo(1));
            Assert.That(limited.GetCode(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(invalidTrack.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidDescription.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidObu.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(missingDescription.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(missingObu.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
        });
        AssertSourceMappings(source, extracted);

        Assert.That(reader.Close(), Is.EqualTo(AvifResultCode.Success));
        AvifPayloadExtraction closed =
            reader.ExtractTrackConfigurationObu(trackId, 1, 0, 65536);
        Assert.That(closed.GetCode(), Is.EqualTo(AvifResultCode.Closed));
        writer.Close();
    }

    [Test]
    public void SampleAndDependencyRangeExtractionsRetainSampleBoundaries()
    {
        AvifImage firstImage = CreateImage(2, 2, 19);
        AvifImage secondImage = CreateImage(2, 2, 83);
        AvifSequence sequence = new AvifSequence(1000);
        sequence.AddFrame(new AvifFrame(firstImage, null, 100, true));
        sequence.AddFrame(new AvifFrame(secondImage, null, 100, false));
        AvifWriter writer = AvifWriter.Create();
        uint trackId = writer.AddSequence(sequence);
        firstImage.Close();
        secondImage.Close();
        AvifEncodeResult encoded = writer.Finish();
        byte[] source = encoded.GetData();
        AvifReader reader = AvifReader.Create(source);
        reader.Parse();
        AvifTrack track = ((reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetMovie()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .FindTrack(trackId)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifSample firstSample = track.GetSample(0)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifSample secondSample = track.GetSample(1)
            ?? throw new InvalidOperationException("Invalid internal state.");

        AvifPayloadExtraction sample = reader.ExtractTrackSamplePayload(
            trackId,
            1,
            65536);
        AvifPayloadExtraction dependency =
            reader.ExtractSampleDependencyRange(trackId, 1, 65536);
        AvifPayloadExtraction limited =
            reader.ExtractSampleDependencyRange(
                trackId,
                1,
                firstSample.GetSize() + secondSample.GetSize() - 1);

        Assert.Multiple(() =>
        {
            Assert.That(sample.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(sample.GetTrackId(), Is.EqualTo(trackId));
            Assert.That(sample.GetFirstSampleIndex(), Is.EqualTo(1));
            Assert.That(sample.GetLastSampleIndex(), Is.EqualTo(1));
            Assert.That(sample.GetDataLength(), Is.EqualTo(secondSample.GetSize()));
            Assert.That(dependency.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(dependency.GetFirstSampleIndex(), Is.Zero);
            Assert.That(dependency.GetLastSampleIndex(), Is.EqualTo(1));
            Assert.That(dependency.GetSourceSegmentCount(), Is.EqualTo(2));
            Assert.That(
                dependency.GetDataLength(),
                Is.EqualTo(firstSample.GetSize() + secondSample.GetSize()));
            Assert.That(limited.GetCode(), Is.EqualTo(AvifResultCode.LimitExceeded));
        });
        AssertSourceMappings(source, sample);
        AssertSourceMappings(source, dependency);

        reader.Close();
        writer.Close();
    }

    [Test]
    public void ConfigurationAndUnknownMetadataObusRetainExactOffsets()
    {
        byte[] source = CreateObuExtractionSource();
        AvifReader reader = AvifReader.Create(source);
        Assert.That(reader.Parse(), Is.EqualTo(AvifResultCode.Success));
        uint itemId = ((reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetFileInfo()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetPrimaryItemId();

        AvifPayloadExtraction configuration =
            reader.ExtractItemConfigurationObu(itemId, 0, 65536);
        AvifPayloadExtraction metadata =
            reader.ExtractItemMetadataObu(itemId, 0, 65536);
        AvifPayloadExtraction missing =
            reader.ExtractItemMetadataObu(itemId, 1, 65536);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(configuration.GetItemId(), Is.EqualTo(itemId));
            Assert.That(
                configuration.GetObuType(),
                Is.EqualTo((int)Av1ObuType.SequenceHeader));
            Assert.That(metadata.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadata.GetObuType(), Is.EqualTo((int)Av1ObuType.Metadata));
            Assert.That(metadata.GetMetadataType(), Is.EqualTo(99));
            Assert.That(metadata.IsMetadataTypeKnown(), Is.False);
            Assert.That(missing.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
        });
        AssertSourceMappings(source, configuration);
        AssertSourceMappings(source, metadata);

        reader.Close();
    }

    [Test]
    public void LayerExtractionsPartitionTheCompleteLayeredPayload()
    {
        AvifImage preview = CreateImage(2, 2, 29);
        AvifImage finalImage = CreateImage(4, 4, 167);
        AvifLayeredImage layered = new AvifLayeredImage(0, true);
        layered.AddLayer(preview);
        layered.AddLayer(finalImage);
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddLayeredImage(layered);
        preview.Close();
        finalImage.Close();
        AvifEncodeResult encoded = writer.Finish();
        byte[] source = encoded.GetData();
        AvifReader reader = AvifReader.Create(source);
        reader.Parse();

        AvifPayloadExtraction complete = reader.ExtractItemPayload(
            itemId,
            65536);
        AvifPayloadExtraction first = reader.ExtractLayerPayload(
            itemId,
            0,
            65536);
        AvifPayloadExtraction second = reader.ExtractLayerPayload(
            itemId,
            1,
            65536);
        AvifPayloadExtraction missing = reader.ExtractLayerPayload(
            itemId,
            2,
            65536);
        byte[] combined = new byte[first.GetDataLength() + second.GetDataLength()];
        first.GetData().CopyTo(combined, 0);
        second.GetData().CopyTo(combined, first.GetDataLength());

        Assert.Multiple(() =>
        {
            Assert.That(first.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(second.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(combined, Is.EqualTo(complete.GetData()));
            Assert.That(missing.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
        });
        AssertSourceMappings(source, first);
        AssertSourceMappings(source, second);

        reader.Close();
        writer.Close();
    }

    static byte[] CreateObuExtractionSource()
    {
        AvifImage image = CreateImage(2, 2, 53);
        AvifEncodeResult encoded = Avif.Encode(image);
        image.Close();
        AvifReader originalReader = AvifReader.Create(encoded.GetData());
        Assert.That(originalReader.Parse(), Is.EqualTo(AvifResultCode.Success));
        AvifImageItem originalItem = (originalReader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetItem(0)
            ?? throw new InvalidOperationException("Invalid internal state.");
        CanonicalItemPayload item = CanonicalItemPayload.Create(
            1,
            "av01",
            string.Empty,
            false,
            originalReader.GetEncodedItem(originalItem.GetItemId()));
        int propertyIndex = 0;
        while (propertyIndex < originalItem.GetPropertyCount())
        {
            AvifItemProperty property = originalItem.GetProperty(propertyIndex)
                ?? throw new InvalidOperationException("Invalid internal state.");
            item.Properties.Add(new CanonicalItemPropertyAssociation(
                property.GetRawData(),
                property.IsEssential()));
            propertyIndex++;
        }
        originalReader.Close();

        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            item.Data,
            new AvifSecurityLimits(),
            0);
        Av1Obu sequence = parsed.Obus[0];
        byte[] sequenceObu = item.Data.AsSpan(
            sequence.Offset,
            sequence.TotalSize).ToArray();
        Av1WriteResult metadata = Av1ObuWriter.WriteObu(
            Av1ObuType.Metadata,
            new byte[] { 99, 9, 8, 7, 128 },
            false,
            0,
            0);
        byte[] payload = new byte[item.Data.Length + metadata.Data.Length];
        item.Data.AsSpan(0, sequence.TotalSize).CopyTo(payload);
        metadata.Data.CopyTo(payload, sequence.TotalSize);
        item.Data.AsSpan(sequence.TotalSize).CopyTo(
            payload.AsSpan(sequence.TotalSize + metadata.Data.Length));
        item.Data = payload;
        item.Length = payload.Length;

        int index = 0;
        while (index < item.Properties.Count)
        {
            CanonicalPropertyProbe probe =
                CanonicalItemContainerWriter.ProbeProperty(
                    item.Properties[index].RawProperty);
            if (probe.Code == AvifResultCode.Success
                && probe.Type == "av1C")
            {
                byte[] raw = item.Properties[index].RawProperty;
                byte[] expanded = new byte[raw.Length + sequenceObu.Length];
                raw.CopyTo(expanded, 0);
                sequenceObu.CopyTo(expanded, raw.Length);
                BinaryPrimitives.WriteUInt32BigEndian(
                    expanded.AsSpan(0, 4),
                    (uint)expanded.Length);
                item.Properties[index] =
                    new CanonicalItemPropertyAssociation(expanded, true);
            }
            index++;
        }

        CanonicalItemContainerOptions options =
            new CanonicalItemContainerOptions();
        options.MaximumOutputBytes = 65536;
        CanonicalItemContainerRequest request =
            CanonicalItemContainerRequest.Create(
                "avif",
                0,
                new List<string> { "mif1", "miaf" },
                1,
                new List<CanonicalItemPayload> { item },
                options);
        CanonicalItemContainerWriteResult written =
            CanonicalItemContainerWriter.Write(request);
        Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
        return written.Data;
    }

    static void AssertSourceMappings(
        byte[] source,
        AvifPayloadExtraction extraction)
    {
        byte[] data = extraction.GetData();
        int index = 0;
        while (index < extraction.GetSourceSegmentCount())
        {
            AvifPayloadSegment segment = extraction.GetSourceSegment(index)
                ?? throw new InvalidOperationException("Invalid internal state.");
            Assert.That(
                source.AsSpan(
                    (int)segment.GetSourceOffset(),
                    (int)segment.GetLength()).ToArray(),
                Is.EqualTo(data.AsSpan(
                    (int)segment.GetPayloadOffset(),
                    (int)segment.GetLength()).ToArray()));
            index++;
        }
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
            ?? throw new InvalidOperationException("Invalid internal state.");
        int row = 0;
        while (row < plane.GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }
}
