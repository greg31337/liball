namespace avif.Tests;

public class MetadataAuthoringCompletionTests
{
    [Test]
    public void WriterAuthorsMimeUriAndSharedContentDescriptionRelationships()
    {
        AvifImage firstImage = CreateImage(2, 2, 31);
        AvifImage secondImage = CreateImage(2, 2, 79);
        AvifWriter writer = AvifWriter.Create();
        uint firstId = writer.AddImage(firstImage);
        uint secondId = writer.AddImage(secondImage);
        firstImage.Close();
        secondImage.Close();
        AvifMetadata mime = new AvifMetadata(
            "mime",
            "application/example+json",
            new byte[] { 1, 2, 3 });
        AvifResultCode encoding = mime.SetContentEncoding("gzip");
        uint mimeId = writer.AddMetadataToItems(
            new uint[] { firstId, secondId },
            mime);
        AvifMetadata uri = new AvifMetadata(
            "uri ",
            string.Empty,
            new byte[] { 5, 8, 13 });
        AvifResultCode uriType = uri.SetUriType(
            "https://example.test/metadata/schema");
        uint uriId = writer.AddMetadata(firstId, uri);
        AvifResultCode association = writer.AddMetadataAssociation(
            uriId,
            secondId);
        AvifEncodeResult encoded = writer.Finish();
        AvifReader reader = AvifReader.Create(encoded.GetData());
        reader.Parse();
        AvifDocument document = reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifImageItem parsedMime = document.FindItem(mimeId)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifImageItem parsedUri = document.FindItem(uriId)
            ?? throw new InvalidOperationException("Invalid internal state.");

        Assert.Multiple(() =>
        {
            Assert.That(encoding, Is.EqualTo(AvifResultCode.Success));
            Assert.That(uriType, Is.EqualTo(AvifResultCode.Success));
            Assert.That(association, Is.EqualTo(AvifResultCode.Success));
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsedMime.GetContentType(), Is.EqualTo("application/example+json"));
            Assert.That(parsedMime.GetContentEncoding(), Is.EqualTo("gzip"));
            Assert.That(parsedMime.GetOutgoingReferenceCount(), Is.EqualTo(1));
            Assert.That(
                (parsedMime.GetOutgoingReference(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetTargetCount(),
                Is.EqualTo(2));
            Assert.That(parsedUri.GetItemType(), Is.EqualTo("uri "));
            Assert.That(
                parsedUri.GetUriType(),
                Is.EqualTo("https://example.test/metadata/schema"));
            Assert.That(
                (parsedUri.GetOutgoingReference(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetTargetCount(),
                Is.EqualTo(2));
            Assert.That(reader.GetEncodedItem(mimeId), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(reader.GetEncodedItem(uriId), Is.EqualTo(new byte[] { 5, 8, 13 }));
        });

        reader.Close();
        writer.Close();
    }

    [Test]
    public void RewriterAddsSharedMetadataAssociationAndOpaqueItemObu()
    {
        AvifImage firstImage = CreateImage(2, 2, 17);
        AvifImage secondImage = CreateImage(2, 2, 113);
        AvifWriter writer = AvifWriter.Create();
        uint firstId = writer.AddImage(firstImage);
        uint secondId = writer.AddImage(secondImage);
        firstImage.Close();
        secondImage.Close();
        uint existingMetadataId = writer.AddMetadata(
            firstId,
            new AvifMetadata(
                "cust",
                string.Empty,
                new byte[] { 9, 7, 5 }));
        AvifEncodeResult sourceResult = writer.Finish();
        byte[] source = sourceResult.GetData();
        AvifReader sourceReader = AvifReader.Create(source);
        sourceReader.Parse();
        byte[] originalPayload = sourceReader.GetEncodedItem(firstId);
        sourceReader.Close();

        AvifRewriter rewriter = AvifRewriter.Create();
        rewriter.Open(source);
        AvifResultCode associated = rewriter.AddMetadataAssociation(
            existingMetadataId,
            secondId);
        AvifMetadata mime = new AvifMetadata(
            "mime",
            "application/cbor",
            new byte[] { 4, 2 });
        mime.SetContentEncoding("br");
        uint addedMetadataId = rewriter.AddMetadataToItems(
            new uint[] { firstId, secondId },
            mime);
        AvifMetadataObu metadataObu = new AvifMetadataObu(
            91,
            new byte[] { 6, 2, 6, 4 });
        AvifResultCode obuAdded = rewriter.AddItemMetadataObu(
            firstId,
            metadataObu);
        AvifEncodeResult rewritten = rewriter.Finish();
        AvifReader reader = AvifReader.Create(rewritten.GetData());
        reader.Parse();
        AvifImageItem existing = (reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(existingMetadataId)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifImageItem added = (reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(addedMetadataId)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifPayloadExtraction extractedObu = reader.ExtractItemMetadataObu(
            firstId,
            0,
            65536);
        byte[] rewrittenPayload = reader.GetEncodedItem(firstId);

        Assert.Multiple(() =>
        {
            Assert.That(associated, Is.EqualTo(AvifResultCode.Success));
            Assert.That(obuAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(rewritten.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(
                (existing.GetOutgoingReference(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetTargetCount(),
                Is.EqualTo(2));
            Assert.That(added.GetContentEncoding(), Is.EqualTo("br"));
            Assert.That(
                (added.GetOutgoingReference(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetTargetCount(),
                Is.EqualTo(2));
            Assert.That(extractedObu.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(extractedObu.GetMetadataType(), Is.EqualTo(91));
            Assert.That(extractedObu.IsMetadataTypeKnown(), Is.False);
            Assert.That(rewrittenPayload.Length, Is.GreaterThan(originalPayload.Length));
            Assert.That(
                rewrittenPayload.AsSpan(0, originalPayload.Length).ToArray(),
                Is.EqualTo(originalPayload));
        });

        reader.Close();
        rewriter.Close();
        writer.Close();
    }

    [Test]
    public void SequenceMetadataObuIsAuthoredSignaledAndPreservedByRemux()
    {
        AvifImage image = CreateImage(2, 2, 47);
        AvifFrame frame = new AvifFrame(image, null, 250, true);
        AvifMetadataObu metadata = new AvifMetadataObu(
            77,
            new byte[] { 10, 20, 30 });
        AvifResultCode attached = frame.AddMetadataObu(metadata);
        AvifMetadataObu? frameMetadata = frame.GetMetadataObu(0);
        AvifSequence sequence = new AvifSequence(1000);
        sequence.AddFrame(frame);
        AvifWriter writer = AvifWriter.Create();
        uint trackId = writer.AddSequence(sequence);
        image.Close();
        AvifEncodeResult encoded = writer.Finish();
        AvifReader authoredReader = AvifReader.Create(encoded.GetData());
        authoredReader.Parse();
        AvifPayloadExtraction authored = authoredReader.ExtractTrackMetadataObu(
            trackId,
            0,
            0,
            65536);
        AvifTrack track = ((authoredReader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetMovie()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .FindTrack(trackId)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifSample sample = track.GetSample(0)
            ?? throw new InvalidOperationException("Invalid internal state.");

        AvifRewriter rewriter = AvifRewriter.Create();
        rewriter.Open(encoded.GetData());
        uint fallbackId = ((authoredReader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetFileInfo()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetPrimaryItemId();
        AvifPayloadExtraction fallbackMetadata =
            authoredReader.ExtractItemMetadataObu(
                fallbackId,
                0,
                65536);
        rewriter.AddMetadata(
            fallbackId,
            new AvifMetadata(
                "cust",
                string.Empty,
                new byte[] { 1 }));
        AvifEncodeResult rewritten = rewriter.Finish();
        AvifReader rewrittenReader = AvifReader.Create(rewritten.GetData());
        rewrittenReader.Parse();
        AvifPayloadExtraction preserved =
            rewrittenReader.ExtractTrackMetadataObu(
                trackId,
                0,
                0,
                65536);

        Assert.Multiple(() =>
        {
            Assert.That(attached, Is.EqualTo(AvifResultCode.Success));
            Assert.That(frame.GetMetadataGroupCount(), Is.EqualTo(1));
            Assert.That(frame.GetMetadataGroupParameter(0),
                Is.EqualTo(0x4d000000));
            Assert.That(frame.GetMetadataObuCount(), Is.EqualTo(1));
            Assert.That(
                (frameMetadata ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetMetadataType(),
                Is.EqualTo(77));
            Assert.That(frameMetadata.GetData(),
                Is.EqualTo(new byte[] { 10, 20, 30 }));
            Assert.That(frame.GetMetadataObu(1), Is.Null);
            Assert.That(authored.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(authored.GetMetadataType(), Is.EqualTo(77));
            Assert.That(
                fallbackMetadata.GetCode(),
                Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(sample.GetMetadataGroupCount(), Is.EqualTo(1));
            Assert.That(
                (sample.GetMetadataGroup(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetMetadataType(),
                Is.EqualTo(77));
            Assert.That(rewritten.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(preserved.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(preserved.GetMetadataType(), Is.EqualTo(77));
            Assert.That(preserved.GetData(), Is.EqualTo(authored.GetData()));
        });

        rewrittenReader.Close();
        rewriter.Close();
        authoredReader.Close();
        writer.Close();
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
