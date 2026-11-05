using System.Text;

namespace avif.Tests;

public class AvifWriterTests
{
    [Test]
    public void AllocatesSequentialIdsAndAuthorsSelectedVisiblePrimary()
    {
        AvifImage hiddenImage = CreateImage(11);
        AvifImage firstImage = CreateImage(37);
        AvifImage primaryImage = CreateImage(83);
        AvifWriter writer = AvifWriter.Create();

        uint hiddenId = writer.AddHiddenImage(hiddenImage);
        uint firstId = writer.AddImage(firstImage);
        uint primaryId = writer.AddImage(primaryImage);
        AvifResultCode selected = writer.SetPrimaryItem(primaryId);
        hiddenImage.Close();
        firstImage.Close();
        primaryImage.Close();
        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifReader reader = AvifReader.Create(finished.GetData());
        AvifResultCode parsed = reader.Parse();
        AvifDecodeResult hidden = reader.DecodeItem(hiddenId);
        AvifDecodeResult first = reader.DecodeItem(firstId);
        AvifDecodeResult primary = reader.DecodePrimaryImage();

        Assert.Multiple(() =>
        {
            Assert.That(hiddenId, Is.EqualTo(1));
            Assert.That(firstId, Is.EqualTo(2));
            Assert.That(primaryId, Is.EqualTo(3));
            Assert.That(selected, Is.EqualTo(AvifResultCode.Success));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount(), Is.EqualTo(3));
            Assert.That(((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(hiddenId) ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That(((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(firstId) ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.False);
            Assert.That(((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId(),
                Is.EqualTo(primaryId));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(hidden.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(first.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(primary.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        AssertPlaneValue(hidden.GetImage(), 11);
        AssertPlaneValue(first.GetImage(), 37);
        AssertPlaneValue(primary.GetImage(), 83);

        (hidden.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (first.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (primary.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
    }

    [Test]
    public void MetadataIsCopiedAllocatedAndLinkedToItsTarget()
    {
        byte[] packet = Encoding.UTF8.GetBytes(
            "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"/>");
        byte[] expected = (byte[])packet.Clone();
        AvifMetadata metadata = AvifMetadata.Create(AvifMetadataKind.Xmp, packet);
        packet[0] = 0;
        byte[] returned = metadata.GetData();
        returned[1] = 0;
        AvifImage image = CreateImage(29);
        AvifWriter writer = AvifWriter.Create();
        uint imageId = writer.AddImage(image);
        uint metadataId = writer.AddMetadata(imageId, metadata);
        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifImageItem? metadataItem = (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(metadataId);
        AvifItemReference? reference = (metadataItem ?? throw new InvalidOperationException("Invalid internal state.")).GetOutgoingReference(0);
        AvifReader reader = AvifReader.Create(finished.GetData());
        reader.Parse();
        byte[] extracted = reader.GetEncodedItem(metadataId);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(imageId, Is.EqualTo(1));
            Assert.That(metadataId, Is.EqualTo(2));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadataItem.IsMetadataItem(), Is.True);
            Assert.That(metadataItem.IsHidden(), Is.True);
            Assert.That(metadataItem.GetItemType(), Is.EqualTo("mime"));
            Assert.That(metadataItem.GetContentType(),
                Is.EqualTo("application/rdf+xml"));
            Assert.That((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(AvifItemReferenceKind.ContentDescription));
            Assert.That(reference.GetTargetItemId(0), Is.EqualTo(imageId));
            Assert.That(extracted, Is.EqualTo(expected));
        });

        image.Close();
        reader.Close();
        writer.Close();
    }

    [Test]
    public void FinishWriteAndCloseEnforceTheConstructionLifecycle()
    {
        AvifWriter empty = AvifWriter.Create();
        AvifEncodeResult emptyFinish = empty.Finish();
        AvifWriter writer = AvifWriter.Create();
        MemoryStream destination = new MemoryStream();
        AvifResultCode earlyWrite = writer.Write(destination);
        AvifImage image = CreateImage(61);
        uint itemId = writer.AddImage(image);
        AvifEncodeResult first = writer.Finish();
        uint lateItem = writer.AddImage(image);
        AvifEncodeResult second = writer.Finish();
        AvifResultCode write = writer.Write(destination);
        AvifResultCode close = writer.Close();
        AvifResultCode secondClose = writer.Close();
        AvifEncodeResult afterClose = writer.Finish();

        Assert.Multiple(() =>
        {
            Assert.That(emptyFinish.GetCode(), Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(earlyWrite, Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(itemId, Is.EqualTo(1));
            Assert.That(first.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(lateItem, Is.Zero);
            Assert.That(second.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(second.GetData(), Is.EqualTo(first.GetData()));
            Assert.That(write, Is.EqualTo(AvifResultCode.Success));
            Assert.That(destination.ToArray(), Is.EqualTo(first.GetData()));
            Assert.That(destination.CanWrite, Is.True);
            Assert.That(close, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondClose, Is.EqualTo(AvifResultCode.Success));
            Assert.That(writer.GetCode(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(writer.GetItemCount(), Is.Zero);
            Assert.That(writer.GetPrimaryItemId(), Is.Zero);
            Assert.That(writer.IsFinished(), Is.False);
            Assert.That(afterClose.GetCode(), Is.EqualTo(AvifResultCode.Closed));
        });

        image.Close();
        empty.Close();
        destination.Close();
    }

    [Test]
    public void InvalidTargetsHiddenPrimariesOptionsAndLimitsReturnStableCodes()
    {
        AvifImage image = CreateImage(17);
        AvifMetadata metadata = AvifMetadata.Create(
            AvifMetadataKind.Exif,
            new byte[] { 0, 0, 0, 0 });
        AvifWriter writer = AvifWriter.Create();
        uint hiddenId = writer.AddHiddenImage(image);
        AvifResultCode hiddenPrimary = writer.SetPrimaryItem(hiddenId);
        uint missingTarget = writer.AddMetadata(999, metadata);
        uint visibleId = writer.AddImage(image);

        AvifEncoderOptions limitedOptions = new AvifEncoderOptions();
        (limitedOptions.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxMetadataBytes = 3;
        AvifWriter limited = AvifWriter.Create(limitedOptions);
        uint limitedImage = limited.AddImage(image);
        uint oversizedMetadata = limited.AddMetadata(limitedImage, metadata);

        AvifEncoderOptions missingLimits = new AvifEncoderOptions();
        missingLimits.Limits = null;
        AvifWriter invalidOptions = AvifWriter.Create(missingLimits);
        AvifMetadata invalidMetadata = AvifMetadata.Create(
            AvifMetadataKind.Custom,
            new byte[] { 1 });

        Assert.Multiple(() =>
        {
            Assert.That(hiddenId, Is.EqualTo(1));
            Assert.That(hiddenPrimary, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(missingTarget, Is.Zero);
            Assert.That(visibleId, Is.EqualTo(2));
            Assert.That(writer.GetPrimaryItemId(), Is.EqualTo(visibleId));
            Assert.That(writer.Finish().GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(limitedImage, Is.EqualTo(1));
            Assert.That(oversizedMetadata, Is.Zero);
            Assert.That(limited.GetCode(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(invalidOptions.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidOptions.AddImage(image), Is.Zero);
            Assert.That(invalidMetadata.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        image.Close();
        writer.Close();
        limited.Close();
        invalidOptions.Close();
    }

    static AvifImage CreateImage(byte value)
    {
        AvifImage image = AvifImage.Create(
            3,
            2,
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

    static void AssertPlaneValue(AvifImage? image, byte value)
    {
        AvifPlane? plane = (image ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            Assert.That(
                plane.GetRowSpan(row).ToArray(),
                Is.EqualTo(Enumerable.Repeat(value, plane.GetWidth()).ToArray()));
            row++;
        }
    }
}
