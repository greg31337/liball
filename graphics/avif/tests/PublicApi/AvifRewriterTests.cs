using System.IO;

namespace avif.Tests;

public class AvifRewriterTests
{
    [Test]
    public void OpenAppliesSourceLimitBeforeRetainingInput()
    {
        AvifRewriteOptions options = new AvifRewriteOptions();
        options.Limits.MaxSourceBytes = 1;
        AvifRewriter rewriter = AvifRewriter.Create(options);

        Assert.Multiple(() =>
        {
            Assert.That(rewriter.Open(new byte[2]),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(rewriter.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
        });

        rewriter.Close();
    }

    [Test]
    public void NoOpRewritePreservesEverySourceByteAndWritesWithoutClosing()
    {
        byte[] source = CreateSingleImageSource(false, false);
        byte[] expected = (byte[])source.Clone();
        AvifRewriter rewriter = AvifRewriter.Create();
        AvifResultCode opened = rewriter.Open(source);
        source[0] = 0;

        AvifEncodeResult finished = rewriter.Finish();
        MemoryStream destination = new MemoryStream();
        AvifResultCode written = rewriter.Write(destination);

        Assert.Multiple(() =>
        {
            Assert.That(opened, Is.EqualTo(AvifResultCode.Success));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(finished.GetData(), Is.EqualTo(expected));
            Assert.That(written, Is.EqualTo(AvifResultCode.Success));
            Assert.That(destination.CanWrite, Is.True);
            Assert.That(destination.ToArray(), Is.EqualTo(expected));
            Assert.That(rewriter.IsOpen(), Is.True);
            Assert.That(rewriter.IsFinished(), Is.True);
        });

        rewriter.Close();
        Assert.That(rewriter.GetCode(), Is.EqualTo(AvifResultCode.Closed));
    }

    [Test]
    public void EditsPrimaryMetadataTransformAndBrandsWithoutGenerationLoss()
    {
        AvifImage first = CreateImage(3, 2, 21);
        AvifImage second = CreateImage(3, 2, 187);
        AvifWriter writer = AvifWriter.Create();
        uint firstId = writer.AddImage(first);
        uint secondId = writer.AddImage(second);
        uint oldMetadataId = writer.AddMetadata(
            firstId,
            AvifMetadata.Create(AvifMetadataKind.Xmp, new byte[] { 1, 2, 3 }));
        byte[] source = writer.Finish().GetData();
        AvifReader sourceReader = AvifReader.Create(source);
        sourceReader.Parse();
        byte[] firstPayload = sourceReader.GetEncodedItem(firstId);
        byte[] secondPayload = sourceReader.GetEncodedItem(secondId);
        AvifTransform transform = new AvifTransform();
        transform.SetRotation(1);
        transform.SetMirror(0);
        AvifRewriter rewriter = AvifRewriter.Create();

        AvifResultCode opened = rewriter.Open(source);
        AvifResultCode primary = rewriter.SetPrimaryItem(secondId);
        AvifResultCode removed = rewriter.RemoveMetadata(
            firstId,
            AvifMetadataKind.Xmp);
        uint newMetadataId = rewriter.AddMetadata(
            secondId,
            AvifMetadata.Create(
                "mime",
                "application/example",
                new byte[] { 9, 8, 7 }));
        AvifResultCode transformed = rewriter.SetTransform(secondId, transform);
        AvifResultCode branded = rewriter.AddCompatibleBrand("test");
        AvifResultCode normalized = rewriter.Normalize();
        AvifEncodeResult finished = rewriter.Finish();
        Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifDocument? document = read.GetDocument();
        AvifImageItem? metadata = (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(newMetadataId);
        AvifTransform parsedTransform = (document.FindItem(secondId) ?? throw new InvalidOperationException("Invalid internal state.")).GetTransform();
        AvifReader rewrittenReader = AvifReader.Create(finished.GetData());
        rewrittenReader.Parse();

        Assert.Multiple(() =>
        {
            Assert.That(opened, Is.EqualTo(AvifResultCode.Success));
            Assert.That(primary, Is.EqualTo(AvifResultCode.Success));
            Assert.That(removed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(transformed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(branded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(normalized, Is.EqualTo(AvifResultCode.Success));
            Assert.That(newMetadataId, Is.EqualTo(oldMetadataId + 1));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId(), Is.EqualTo(secondId));
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("test"), Is.True);
            Assert.That(document.FindItem(oldMetadataId), Is.Null);
            Assert.That(metadata, Is.Not.Null);
            Assert.That((metadata ?? throw new InvalidOperationException("Invalid internal state.")).GetContentType(), Is.EqualTo("application/example"));
            Assert.That((metadata.GetOutgoingReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(), Is.EqualTo("cdsc"));
            Assert.That((metadata.GetOutgoingReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetTargetItemId(0),
                Is.EqualTo(secondId));
            Assert.That(rewrittenReader.GetEncodedItem(newMetadataId),
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(rewrittenReader.GetEncodedItem(firstId), Is.EqualTo(firstPayload));
            Assert.That(rewrittenReader.GetEncodedItem(secondId), Is.EqualTo(secondPayload));
            Assert.That(parsedTransform.GetRotationQuarterTurnsCounterClockwise(),
                Is.EqualTo(1));
            Assert.That(parsedTransform.GetMirrorAxis(), Is.Zero);
        });

        AvifRewriter brandRemover = AvifRewriter.Create();
        brandRemover.Open(finished.GetData());
        brandRemover.RemoveCompatibleBrand("test");
        AvifEncodeResult brandRemoved = brandRemover.Finish();
        AvifReadResult withoutBrand = Avif.Read(brandRemoved.GetData());
        Assert.That(((withoutBrand.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("test"), Is.False);

        first.Close();
        second.Close();
        writer.Close();
        sourceReader.Close();
        rewrittenReader.Close();
        rewriter.Close();
        brandRemover.Close();
    }

    [Test]
    public void PreservationControlsDropOnlyRequestedOpaqueData()
    {
        byte[] source = CreateSingleImageSource(true, true);
        source = AppendBox(source, "junk", new byte[] { 4, 5, 6 });
        AvifReader sourceReader = AvifReader.Create(source);
        sourceReader.Parse();
        byte[] encodedPayload = sourceReader.GetEncodedItem(1);
        AvifRewriteOptions options = new AvifRewriteOptions();
        options.HeaderFormat = AvifHeaderFormat.Full;
        options.Canonicalize = true;
        options.PreserveUnknownBoxes = false;
        options.PreserveUnknownProperties = false;
        options.PreserveUnknownMetadata = false;
        AvifRewriter rewriter = AvifRewriter.Create(options);

        AvifResultCode opened = rewriter.Open(source);
        AvifEncodeResult finished = rewriter.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifDocument? document = read.GetDocument();
        AvifImageItem? primary = (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1);
        AvifReader outputReader = AvifReader.Create(finished.GetData());
        outputReader.Parse();

        Assert.Multiple(() =>
        {
            Assert.That(opened, Is.EqualTo(AvifResultCode.Success));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(FindProperty(primary, "zzzz"), Is.Null);
            Assert.That(document.FindItem(2), Is.Null);
            Assert.That(FindTopLevelBox(document, "junk"), Is.Null);
            Assert.That(outputReader.GetEncodedItem(1), Is.EqualTo(encodedPayload));
        });

        sourceReader.Close();
        outputReader.Close();
        rewriter.Close();
    }

    [Test]
    public void TimedRewriteRelocatesSamplesAndPreservesMoviePayloads()
    {
        AvifImage first = CreateImage(2, 2, 17);
        AvifImage second = CreateImage(2, 2, 203);
        AvifSequence sequence = AvifSequence.Create(1000);
        sequence.AddFrame(AvifFrame.Create(first, 100));
        sequence.AddFrame(AvifFrame.Create(second, 200));
        AvifWriter writer = AvifWriter.Create();
        writer.AddSequence(sequence);
        byte[] source = writer.Finish().GetData();
        AvifReadResult sourceRead = Avif.Read(source);
        AvifTrack? sourceTrack = ((sourceRead.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetMovie() ?? throw new InvalidOperationException("Invalid internal state.")).FindTrack(1);
        byte[] firstSample = GetSampleBytes(source, (sourceTrack ?? throw new InvalidOperationException("Invalid internal state.")).GetSample(0));
        byte[] secondSample = GetSampleBytes(source, sourceTrack.GetSample(1));
        AvifTransform transform = new AvifTransform();
        transform.SetRotation(1);
        AvifRewriter rewriter = AvifRewriter.Create();

        AvifResultCode opened = rewriter.Open(source);
        AvifResultCode transformed = rewriter.SetTransform(1, transform);
        AvifResultCode normalized = rewriter.Normalize();
        AvifEncodeResult finished = rewriter.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifTrack? track = ((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetMovie() ?? throw new InvalidOperationException("Invalid internal state.")).FindTrack(1);
        AvifReader reader = AvifReader.Create(finished.GetData());
        reader.Parse();
        AvifDecodeResult decodedSecond = reader.DecodeFrame(1);

        Assert.Multiple(() =>
        {
            Assert.That(opened, Is.EqualTo(AvifResultCode.Success));
            Assert.That(transformed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(normalized, Is.EqualTo(AvifResultCode.Success));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("avis"), Is.True);
            Assert.That(((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("msf1"), Is.True);
            Assert.That((track ?? throw new InvalidOperationException("Invalid internal state.")).GetSampleCount(), Is.EqualTo(2));
            Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(100));
            Assert.That((track.GetSample(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(200));
            Assert.That(GetSampleBytes(finished.GetData(), track.GetSample(0)),
                Is.EqualTo(firstSample));
            Assert.That(GetSampleBytes(finished.GetData(), track.GetSample(1)),
                Is.EqualTo(secondSample));
            Assert.That(((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetTransform()
                .GetRotationQuarterTurnsCounterClockwise(), Is.EqualTo(1));
            Assert.That(decodedSecond.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(((decodedSecond.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetRowSpan(0)[0], Is.EqualTo(203));
        });

        (decodedSecond.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        first.Close();
        second.Close();
        writer.Close();
        reader.Close();
        rewriter.Close();
    }

    [Test]
    public void WriterRoundTripPreservesOpaqueDataMetadataAndItemRanges()
    {
        byte[] opaquePropertyPayload = new byte[] { 7, 6, 5, 4 };
        byte[] xmpPayload = new byte[] { 60, 120, 109, 112, 62 };
        byte[] customMetadataPayload = new byte[] { 3, 2, 1, 0 };
        byte[] unknownBoxPayload = new byte[] { 9, 8, 7, 6, 5 };
        AvifImage image = CreateImage(3, 2, 73);
        AvifWriter writer = AvifWriter.Create();
        uint imageId = writer.AddImage(image);
        AvifResultCode propertyAdded = writer.AddOpaqueProperty(
            imageId,
            AvifOpaqueBox.Create("zzzz", opaquePropertyPayload, false));
        uint xmpId = writer.AddMetadata(
            imageId,
            AvifMetadata.Create(AvifMetadataKind.Xmp, xmpPayload));
        uint customId = writer.AddMetadata(
            imageId,
            AvifMetadata.Create("cust", string.Empty, customMetadataPayload));
        AvifEncodeResult authored = writer.Finish();
        byte[] source = AppendBox(
            authored.GetData(),
            "junk",
            unknownBoxPayload);
        AvifReadResult sourceRead = Avif.Read(source);
        AvifDocument? sourceDocument = sourceRead.GetDocument();
        AvifReader sourceReader = AvifReader.Create(source);
        AvifResultCode sourceParsed = sourceReader.Parse();
        byte[] imagePayload = sourceReader.GetEncodedItem(imageId);
        byte[] sourceOpaqueProperty = (FindProperty(
            (sourceDocument ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(imageId),
            "zzzz") ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData();
        byte[] sourceUnknownBox = GetBoxBytes(
            source,
            FindTopLevelBox(sourceDocument, "junk"));
        AvifRewriteOptions options = new AvifRewriteOptions();
        options.HeaderFormat = AvifHeaderFormat.Full;
        options.Canonicalize = true;
        AvifRewriter rewriter = AvifRewriter.Create(options);

        AvifResultCode opened = rewriter.Open(source);
        AvifResultCode branded = rewriter.AddCompatibleBrand("test");
        AvifEncodeResult rewritten = rewriter.Finish();
        AvifReadResult outputRead = Avif.Read(rewritten.GetData());
        AvifDocument? outputDocument = outputRead.GetDocument();
        AvifReader outputReader = AvifReader.Create(rewritten.GetData());
        AvifResultCode outputParsed = outputReader.Parse();
        AvifImageItem? outputImage = (outputDocument ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(imageId);
        AvifImageItem? outputXmp = outputDocument.FindItem(xmpId);
        AvifImageItem? outputCustom = outputDocument.FindItem(customId);
        AvifItemProperty? outputOpaqueProperty = FindProperty(
            outputImage,
            "zzzz");
        AvifBox? outputUnknownBox = FindTopLevelBox(outputDocument, "junk");
        AvifRewriter noOp = AvifRewriter.Create();
        AvifResultCode noOpOpened = noOp.Open(rewritten.GetData());
        AvifEncodeResult noOpResult = noOp.Finish();

        Assert.Multiple(() =>
        {
            Assert.That(propertyAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(authored.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(sourceRead.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(sourceParsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(opened, Is.EqualTo(AvifResultCode.Success));
            Assert.That(branded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(rewritten.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(outputRead.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(outputParsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That((outputDocument.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("test"), Is.True);
            Assert.That(outputReader.GetEncodedItem(imageId), Is.EqualTo(imagePayload));
            Assert.That(outputReader.GetEncodedItem(xmpId), Is.EqualTo(xmpPayload));
            Assert.That(outputReader.GetEncodedItem(customId),
                Is.EqualTo(customMetadataPayload));
            Assert.That(GetItemExtentBytes(rewritten.GetData(), outputImage),
                Is.EqualTo(imagePayload));
            Assert.That(GetItemExtentBytes(rewritten.GetData(), outputXmp),
                Is.EqualTo(xmpPayload));
            Assert.That(GetItemExtentBytes(rewritten.GetData(), outputCustom),
                Is.EqualTo(customMetadataPayload));
            Assert.That(outputOpaqueProperty, Is.Not.Null);
            Assert.That((outputOpaqueProperty ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData(),
                Is.EqualTo(sourceOpaqueProperty));
            Assert.That(outputUnknownBox, Is.Not.Null);
            Assert.That(GetBoxBytes(rewritten.GetData(), outputUnknownBox),
                Is.EqualTo(sourceUnknownBox));
            Assert.That((outputXmp ?? throw new InvalidOperationException("Invalid internal state.")).GetContentType(),
                Is.EqualTo("application/rdf+xml"));
            Assert.That(ReferencesItem(outputXmp, "cdsc", imageId), Is.True);
            Assert.That((outputCustom ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(), Is.EqualTo("cust"));
            Assert.That(ReferencesItem(outputCustom, "cdsc", imageId), Is.True);
            Assert.That(noOpOpened, Is.EqualTo(AvifResultCode.Success));
            Assert.That(noOpResult.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(noOpResult.GetData(), Is.EqualTo(rewritten.GetData()));
        });

        image.Close();
        writer.Close();
        sourceReader.Close();
        outputReader.Close();
        rewriter.Close();
        noOp.Close();
    }

    [Test]
    public void TimedWriterRewriteKeepsPublicFrameRangesOnPreservedBytes()
    {
        AvifImage first = CreateImage(2, 2, 17);
        AvifImage second = CreateImage(2, 2, 91);
        AvifImage third = CreateImage(2, 2, 203);
        AvifSequence sequence = AvifSequence.Create(1000);
        sequence.AddFrame(AvifFrame.Create(first, 100, true));
        sequence.AddFrame(AvifFrame.Create(second, 200, false));
        sequence.AddFrame(AvifFrame.Create(third, 300, false));
        AvifWriter writer = AvifWriter.Create();
        uint sequenceItemId = writer.AddSequence(sequence);
        AvifEncodeResult authored = writer.Finish();
        byte[] source = authored.GetData();
        AvifReadResult sourceRead = Avif.Read(source);
        AvifImageSource? sourceImage = (sourceRead.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).SelectSource(
            AvifSourceSelection.ImageSequence);
        byte[][] encodedFrames = new byte[(sourceImage ?? throw new InvalidOperationException("Invalid internal state.")).GetFrameCount()][];
        int frameIndex = 0;
        while (frameIndex < encodedFrames.Length)
        {
            encodedFrames[frameIndex] = GetRangeBytes(
                source,
                sourceImage.GetFrameEncodedRange(frameIndex));
            frameIndex++;
        }
        byte[] decodeRange = GetRangeBytes(
            source,
            sourceImage.GetFrameDecodeRange(2));
        AvifRewriter rewriter = AvifRewriter.Create();

        AvifResultCode opened = rewriter.Open(source);
        AvifResultCode branded = rewriter.AddCompatibleBrand("test");
        AvifResultCode normalized = rewriter.Normalize();
        AvifEncodeResult rewritten = rewriter.Finish();
        AvifReadResult outputRead = Avif.Read(rewritten.GetData());
        AvifImageSource? outputImage = (outputRead.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).SelectSource(
            AvifSourceSelection.ImageSequence);

        Assert.Multiple(() =>
        {
            Assert.That(sequenceItemId, Is.Not.Zero);
            Assert.That(authored.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(sourceRead.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(sourceImage, Is.Not.Null);
            Assert.That(sourceImage.GetFrameCount(), Is.EqualTo(3));
            Assert.That(opened, Is.EqualTo(AvifResultCode.Success));
            Assert.That(branded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(normalized, Is.EqualTo(AvifResultCode.Success));
            Assert.That(rewritten.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(outputRead.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(outputImage, Is.Not.Null);
            Assert.That((outputImage ?? throw new InvalidOperationException("Invalid internal state.")).GetFrameCount(), Is.EqualTo(3));
            Assert.That(outputImage.GetNearestKeyFrameIndex(2), Is.Zero);
            Assert.That((outputImage.GetFrameTiming(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(100));
            Assert.That((outputImage.GetFrameTiming(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(200));
            Assert.That((outputImage.GetFrameTiming(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(300));
            Assert.That(GetRangeBytes(
                rewritten.GetData(),
                outputImage.GetFrameEncodedRange(0)), Is.EqualTo(encodedFrames[0]));
            Assert.That(GetRangeBytes(
                rewritten.GetData(),
                outputImage.GetFrameEncodedRange(1)), Is.EqualTo(encodedFrames[1]));
            Assert.That(GetRangeBytes(
                rewritten.GetData(),
                outputImage.GetFrameEncodedRange(2)), Is.EqualTo(encodedFrames[2]));
            Assert.That(GetRangeBytes(
                rewritten.GetData(),
                outputImage.GetFrameDecodeRange(2)), Is.EqualTo(decodeRange));
        });

        first.Close();
        second.Close();
        third.Close();
        writer.Close();
        rewriter.Close();
    }

    static byte[] CreateSingleImageSource(
        bool opaqueProperty,
        bool opaqueMetadata)
    {
        AvifImage image = CreateImage(2, 2, 43);
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddImage(image);
        if (opaqueProperty)
        {
            writer.AddOpaqueProperty(
                itemId,
                AvifOpaqueBox.Create("zzzz", new byte[] { 7, 6, 5 }, false));
        }
        if (opaqueMetadata)
        {
            writer.AddMetadata(
                itemId,
                AvifMetadata.Create("cust", string.Empty, new byte[] { 3, 2, 1 }));
        }
        byte[] data = writer.Finish().GetData();
        image.Close();
        writer.Close();
        return data;
    }

    static AvifImage CreateImage(int width, int height, byte value)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane? plane = image.GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    static AvifItemProperty? FindProperty(AvifImageItem? item, string type)
    {
        int index = 0;
        while (index < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount())
        {
            if ((item.GetProperty(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName() == type)
            {
                return item.GetProperty(index);
            }
            index++;
        }
        return null;
    }

    static byte[] GetSampleBytes(byte[] source, AvifSample? sample)
    {
        return source.AsSpan(
            (int)(sample ?? throw new InvalidOperationException("Invalid internal state.")).GetOffset(),
            (int)sample.GetSize()).ToArray();
    }

    static byte[] GetRangeBytes(byte[] source, AvifByteRange? range)
    {
        return source.AsSpan(
            (int)(range ?? throw new InvalidOperationException("Invalid internal state.")).GetOffset(),
            (int)range.GetLength()).ToArray();
    }

    static byte[] GetItemExtentBytes(byte[] source, AvifImageItem? item)
    {
        Assert.That((item ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentCount(), Is.EqualTo(1));
        AvifItemExtent? extent = item.GetExtent(0);
        Assert.That((extent ?? throw new InvalidOperationException("Invalid internal state.")).IsResolved(), Is.True);
        return source.AsSpan(
            (int)extent.GetResolvedOffset(),
            (int)extent.GetResolvedLength()).ToArray();
    }

    static byte[] GetBoxBytes(byte[] source, AvifBox? box)
    {
        return source.AsSpan(
            (int)(box ?? throw new InvalidOperationException("Invalid internal state.")).GetOffset(),
            (int)box.GetSize()).ToArray();
    }

    static bool ReferencesItem(
        AvifImageItem item,
        string type,
        uint targetItemId)
    {
        int referenceIndex = 0;
        while (referenceIndex < item.GetOutgoingReferenceCount())
        {
            AvifItemReference? reference = item.GetOutgoingReference(referenceIndex);
            int targetIndex = 0;
            while ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName() == type
                && targetIndex < reference.GetTargetCount())
            {
                if (reference.GetTargetItemId(targetIndex) == targetItemId)
                {
                    return true;
                }
                targetIndex++;
            }
            referenceIndex++;
        }
        return false;
    }

    static AvifBox? FindTopLevelBox(AvifDocument document, string type)
    {
        int index = 0;
        while (index < document.GetTopLevelBoxCount())
        {
            if ((document.GetTopLevelBox(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName() == type)
            {
                return document.GetTopLevelBox(index);
            }
            index++;
        }
        return null;
    }

    static byte[] AppendBox(byte[] source, string type, byte[] payload)
    {
        byte[] box = new byte[8 + payload.Length];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(
            box.AsSpan(0, 4),
            (uint)box.Length);
        box[4] = (byte)type[0];
        box[5] = (byte)type[1];
        box[6] = (byte)type[2];
        box[7] = (byte)type[3];
        payload.CopyTo(box, 8);
        byte[] result = new byte[source.Length + box.Length];
        source.CopyTo(result, 0);
        box.CopyTo(result, source.Length);
        return result;
    }
}
