namespace avif.Tests;

public class CollectionAuthoringTests
{
    [Test]
    public void AuthorsCollectionAndLinkedThumbnailWithIndependentPayloads()
    {
        AvifWriter writer = AvifWriter.Create();
        AvifImage first = CreateImage(19);
        AvifImage second = CreateImage(47);
        AvifImage thumbnail = CreateImage(83);

        uint firstId = writer.AddImage(first);
        uint secondId = writer.AddImage(second);
        uint thumbnailId = writer.AddThumbnail(firstId, thumbnail);
        first.Close();
        second.Close();
        thumbnail.Close();

        AvifEncodeResult finished = writer.Finish();
        AvifReader reader = AvifReader.Create(finished.GetData());
        AvifResultCode parsed = reader.Parse();
        AvifDocument? document = reader.GetDocument();
        AvifImageItem? thumbnailItem = (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(thumbnailId);
        AvifDecodeResult decodedSecond = reader.DecodeItem(secondId);
        AvifDecodeResult decodedThumbnail = reader.DecodeItem(thumbnailId);

        Assert.Multiple(() =>
        {
            Assert.That(firstId, Is.EqualTo(1));
            Assert.That(secondId, Is.EqualTo(2));
            Assert.That(thumbnailId, Is.EqualTo(3));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(document.GetItemCount(), Is.EqualTo(3));
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId(), Is.EqualTo(firstId));
            Assert.That((thumbnailItem ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That(thumbnailItem.IsThumbnail(), Is.True);
            Assert.That(thumbnailItem.GetOutgoingReferenceCount(), Is.EqualTo(1));
            Assert.That((thumbnailItem.GetOutgoingReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(AvifItemReferenceKind.Thumbnail));
            Assert.That((thumbnailItem.GetOutgoingReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetTargetItemId(0),
                Is.EqualTo(firstId));
            Assert.That(decodedSecond.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decodedThumbnail.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        AssertPlaneValue(decodedSecond.GetImage(), 47);
        AssertPlaneValue(decodedThumbnail.GetImage(), 83);

        (decodedSecond.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (decodedThumbnail.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
    }

    [Test]
    public void AuthorsPremultipliedAlphaAuxiliaryAssociation()
    {
        AvifWriter writer = AvifWriter.Create();
        AvifImage master = CreateImage(31);
        AvifImage alpha = CreateImage(223);
        uint masterId = writer.AddImage(master);
        AvifAuxiliaryImage descriptor = AvifAuxiliaryImage.Create(
            alpha,
            AvifAuxiliaryKind.Alpha,
            true);
        uint alphaId = writer.AddAuxiliary(masterId, descriptor);
        alpha.Close();

        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifDocument? document = read.GetDocument();
        AvifAuxiliaryImage? association = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetAuxiliaryImage(masterId, 0);
        AvifImageItem? alphaItem = document.FindItem(alphaId);
        AvifImageItem? masterItem = document.FindItem(masterId);
        AvifReader reader = AvifReader.Create(finished.GetData());
        reader.Parse();
        AvifDecodeResult decoded = reader.DecodeItem(alphaId);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(alphaId, Is.EqualTo(2));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(document.GetAuxiliaryImageCount(masterId), Is.EqualTo(1));
            Assert.That((association ?? throw new InvalidOperationException("Invalid internal state.")).GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(association.GetKind(), Is.EqualTo(AvifAuxiliaryKind.Alpha));
            Assert.That(association.GetItemId(), Is.EqualTo(alphaId));
            Assert.That(association.GetMasterItemId(), Is.EqualTo(masterId));
            Assert.That(association.IsPremultiplied(), Is.True);
            Assert.That(association.RequiresResize(), Is.False);
            Assert.That((alphaItem ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That(alphaItem.IsAuxiliaryImage(), Is.True);
            Assert.That((masterItem ?? throw new InvalidOperationException("Invalid internal state.")).GetOutgoingReferenceCount(), Is.EqualTo(1));
            Assert.That((masterItem.GetOutgoingReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(AvifItemReferenceKind.Premultiplied));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        AssertPlaneValue(decoded.GetImage(), 223);

        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        master.Close();
        writer.Close();
    }

    [Test]
    public void AuthorsDepthAndCallerDefinedAuxiliaryImages()
    {
        AvifWriter writer = AvifWriter.Create();
        AvifImage master = CreateImage(29);
        AvifImage depth = AvifImage.Create(
            2,
            1,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifImage confidence = AvifImage.Create(
            3,
            2,
            8,
            AvifChromaFormat.Yuv444,
            false);
        uint masterId = writer.AddImage(master);
        uint depthId = writer.AddAuxiliary(
            masterId,
            AvifAuxiliaryImage.Create(depth, AvifAuxiliaryKind.Depth));
        uint confidenceId = writer.AddAuxiliary(
            masterId,
            AvifAuxiliaryImage.Create(confidence, "urn:example:confidence"));

        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifDocument? document = read.GetDocument();
        AvifAuxiliaryImage? depthAssociation = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetAuxiliaryImage(masterId, 0);
        AvifAuxiliaryImage? confidenceAssociation = document.GetAuxiliaryImage(masterId, 1);

        Assert.Multiple(() =>
        {
            Assert.That(depthId, Is.EqualTo(2));
            Assert.That(confidenceId, Is.EqualTo(3));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(document.GetAuxiliaryImageCount(masterId), Is.EqualTo(2));
            Assert.That((depthAssociation ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(AvifAuxiliaryKind.Depth));
            Assert.That(depthAssociation.GetTypeName(),
                Is.EqualTo("urn:mpeg:mpegB:cicp:systems:auxiliary:depth"));
            Assert.That(depthAssociation.IsPremultiplied(), Is.False);
            Assert.That(depthAssociation.RequiresResize(), Is.True);
            Assert.That((confidenceAssociation ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(AvifAuxiliaryKind.Other));
            Assert.That(confidenceAssociation.GetTypeName(),
                Is.EqualTo("urn:example:confidence"));
            Assert.That((document.FindItem(depthId) ?? throw new InvalidOperationException("Invalid internal state.")).IsAuxiliaryImage(), Is.True);
            Assert.That((document.FindItem(confidenceId) ?? throw new InvalidOperationException("Invalid internal state.")).IsAuxiliaryImage(), Is.True);
        });

        master.Close();
        depth.Close();
        confidence.Close();
        writer.Close();
    }

    [Test]
    public void AuthorsAlternateAndStereoGroupsAndRejectsInvalidMembership()
    {
        AvifWriter writer = AvifWriter.Create();
        AvifImage image = CreateImage(53);
        uint leftId = writer.AddImage(image);
        uint rightId = writer.AddImage(image);
        uint fallbackId = writer.AddImage(image);
        uint alternateId = writer.AddAlternateGroup(
            new uint[] { leftId, rightId, fallbackId });
        uint duplicateAlternate = writer.AddAlternateGroup(
            new uint[] { leftId, fallbackId });
        uint invalidStereo = writer.AddStereoPair(leftId, leftId);
        uint stereoId = writer.AddStereoPair(leftId, rightId);

        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifEntityGroup? alternate = FindGroup(
            read.GetDocument(),
            AvifEntityGroupKind.Alternate);
        AvifEntityGroup? stereo = FindGroup(
            read.GetDocument(),
            AvifEntityGroupKind.Stereo);

        Assert.Multiple(() =>
        {
            Assert.That(alternateId, Is.EqualTo(uint.MaxValue));
            Assert.That(duplicateAlternate, Is.Zero);
            Assert.That(invalidStereo, Is.Zero);
            Assert.That(stereoId, Is.EqualTo(uint.MaxValue - 1));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupCount(), Is.EqualTo(2));
            Assert.That((alternate ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupId(), Is.EqualTo(alternateId));
            Assert.That(alternate.GetEntityCount(), Is.EqualTo(3));
            Assert.That(alternate.GetEntityId(0), Is.EqualTo(leftId));
            Assert.That(alternate.GetEntityId(1), Is.EqualTo(rightId));
            Assert.That(alternate.GetEntityId(2), Is.EqualTo(fallbackId));
            Assert.That((stereo ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupId(), Is.EqualTo(stereoId));
            Assert.That(stereo.GetEntityCount(), Is.EqualTo(2));
            Assert.That(stereo.GetEntityId(0), Is.EqualTo(leftId));
            Assert.That(stereo.GetEntityId(1), Is.EqualTo(rightId));
        });

        image.Close();
        writer.Close();
    }

    [Test]
    public void RejectsInvalidAuxiliaryDescriptorsWithoutConsumingAnItemId()
    {
        AvifWriter writer = AvifWriter.Create();
        AvifImage master = CreateImage(13);
        AvifImage colorAuxiliary = AvifImage.Create(
            3,
            2,
            8,
            AvifChromaFormat.Yuv444,
            false);
        AvifImage wrongBitDepth = AvifImage.Create(
            3,
            2,
            10,
            AvifChromaFormat.Monochrome,
            false);
        uint masterId = writer.AddImage(master);
        AvifAuxiliaryImage invalidKind = AvifAuxiliaryImage.Create(
            master,
            AvifAuxiliaryKind.None);
        AvifAuxiliaryImage invalidCustom = AvifAuxiliaryImage.Create(master, string.Empty);
        AvifAuxiliaryImage invalidColor = AvifAuxiliaryImage.Create(
            colorAuxiliary,
            AvifAuxiliaryKind.Depth);
        uint invalidId = writer.AddAuxiliary(masterId, invalidKind);
        uint invalidBitDepthId = writer.AddAuxiliary(
            masterId,
            AvifAuxiliaryImage.Create(wrongBitDepth, AvifAuxiliaryKind.Alpha));
        uint nextId = writer.AddImage(master);

        Assert.Multiple(() =>
        {
            Assert.That(invalidKind.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidCustom.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidColor.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidId, Is.Zero);
            Assert.That(invalidBitDepthId, Is.Zero);
            Assert.That(nextId, Is.EqualTo(2));
        });

        master.Close();
        colorAuxiliary.Close();
        wrongBitDepth.Close();
        writer.Close();
    }

    static AvifEntityGroup? FindGroup(
        AvifDocument? document,
        AvifEntityGroupKind kind)
    {
        int index = 0;
        while (index < (document ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupCount())
        {
            AvifEntityGroup? group = document.GetGroup(index);
            if ((group ?? throw new InvalidOperationException("Invalid internal state.")).GetKind() == kind)
            {
                return group;
            }
            index++;
        }
        return null;
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
            int column = 0;
            while (column < plane.GetWidth())
            {
                Assert.That(plane.GetRowSpan(row)[column], Is.EqualTo(value));
                column++;
            }
            row++;
        }
    }
}
