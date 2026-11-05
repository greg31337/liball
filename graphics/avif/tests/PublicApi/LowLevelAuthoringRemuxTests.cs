using System.Text;

namespace avif.Tests;

public class LowLevelAuthoringRemuxTests
{
    [Test]
    public void WriterAuthorsExactAv1ArbitraryCollectionAndOpaqueBoxes()
    {
        Av1PayloadSource av1 = CreateAv1Payload(47);
        AvifWriter writer = new AvifWriter(new AvifEncoderOptions());
        uint imageId = writer.AddAv1Item(
            av1.Payload,
            av1.Configuration,
            3,
            2,
            false);
        uint noteId = writer.AddItem(
            "mime",
            "note",
            "text/plain",
            string.Empty,
            string.Empty,
            Encoding.UTF8.GetBytes("opaque note"),
            false);
        AvifResultCode reference = writer.AddItemReference(
            "zref",
            noteId,
            new uint[] { imageId });
        uint groupId = writer.AddEntityGroup(
            "zgrp",
            new uint[] { imageId, noteId });
        AvifOpaqueBox property = CreateOpaqueBox(
            "zprp",
            new byte[] { 1, 3, 5 },
            false);
        AvifResultCode propertyCode = writer.AddOpaqueProperty(
            noteId,
            property);
        AvifOpaqueBox topLevel = CreateOpaqueBox(
            "ztop",
            new byte[] { 2, 4, 6, 8 },
            false);
        AvifResultCode topLevelCode = writer.AddOpaqueTopLevelBox(topLevel);
        AvifEncodeResult encoded = writer.Finish();
        AvifReader reader = AvifReader.Create(encoded.GetData());
        AvifResultCode parse = reader.Parse();
        AvifDocument document = reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifImageItem note = document.FindItem(noteId)
            ?? throw new InvalidOperationException("Invalid internal state.");

        Assert.Multiple(() =>
        {
            Assert.That(imageId, Is.EqualTo(1));
            Assert.That(noteId, Is.EqualTo(2));
            Assert.That(reference, Is.EqualTo(AvifResultCode.Success));
            Assert.That(groupId, Is.Not.Zero);
            Assert.That(propertyCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(topLevelCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.GetEncodedItem(imageId), Is.EqualTo(av1.Payload));
            Assert.That(reader.GetEncodedItem(noteId),
                Is.EqualTo(Encoding.UTF8.GetBytes("opaque note")));
            Assert.That(HasProperty(note, "zprp"), Is.True);
            Assert.That(HasReference(document, "zref", noteId, imageId),
                Is.True);
            Assert.That(HasGroup(document, "zgrp", groupId), Is.True);
            Assert.That(HasTopLevelBox(document, "ztop"), Is.True);
        });

        reader.Close();
        writer.Close();
    }

    [Test]
    public void RewriterAddsItemsAndPreservesExistingAv1BytesExactly()
    {
        AvifImage sourceImage = CreateImage(3, 2, 19);
        AvifEncodeResult sourceEncoded = Avif.Encode(
            sourceImage,
            new AvifEncoderOptions());
        sourceImage.Close();
        AvifReader sourceReader = AvifReader.Create(sourceEncoded.GetData());
        Assert.That(sourceReader.Parse(), Is.EqualTo(AvifResultCode.Success));
        AvifDocument sourceDocument = sourceReader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state.");
        uint originalId = (sourceDocument.GetFileInfo()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetPrimaryItemId();
        byte[] originalPayload = sourceReader.GetEncodedItem(originalId);
        sourceReader.Close();

        Av1PayloadSource av1 = CreateAv1Payload(83);
        AvifRewriter rewriter = new AvifRewriter(
            new AvifRewriteOptions());
        AvifResultCode opened = rewriter.Open(sourceEncoded.GetData());
        uint newImageId = rewriter.AddAv1Item(
            av1.Payload,
            av1.Configuration,
            3,
            2,
            false);
        uint dataId = rewriter.AddItem(
            "mime",
            "sidecar",
            "application/octet-stream",
            string.Empty,
            string.Empty,
            new byte[] { 9, 8, 7 },
            true);
        AvifResultCode primary = rewriter.SetPrimaryItem(newImageId);
        AvifResultCode reference = rewriter.AddItemReference(
            "zlnk",
            dataId,
            new uint[] { originalId, newImageId });
        uint groupId = rewriter.AddEntityGroup(
            "zset",
            new uint[] { originalId, newImageId, dataId });
        AvifResultCode property = rewriter.AddOpaqueProperty(
            originalId,
            CreateOpaqueBox("zold", new byte[] { 7, 7 }, false));
        AvifResultCode topLevel = rewriter.AddOpaqueTopLevelBox(
            CreateOpaqueBox("zrwx", new byte[] { 4, 2 }, false));
        AvifEncodeResult rewritten = rewriter.Finish();
        AvifReader reader = AvifReader.Create(rewritten.GetData());
        AvifResultCode parse = reader.Parse();
        AvifDocument document = reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifImageItem original = document.FindItem(originalId)
            ?? throw new InvalidOperationException("Invalid internal state.");

        Assert.Multiple(() =>
        {
            Assert.That(opened, Is.EqualTo(AvifResultCode.Success));
            Assert.That(newImageId, Is.Not.Zero);
            Assert.That(dataId, Is.Not.Zero);
            Assert.That(primary, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reference, Is.EqualTo(AvifResultCode.Success));
            Assert.That(groupId, Is.Not.Zero);
            Assert.That(property, Is.EqualTo(AvifResultCode.Success));
            Assert.That(topLevel, Is.EqualTo(AvifResultCode.Success));
            Assert.That(rewritten.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parse, Is.EqualTo(AvifResultCode.Success));
            Assert.That((document.GetFileInfo()
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetPrimaryItemId(), Is.EqualTo(newImageId));
            Assert.That(reader.GetEncodedItem(originalId),
                Is.EqualTo(originalPayload));
            Assert.That(reader.GetEncodedItem(newImageId),
                Is.EqualTo(av1.Payload));
            Assert.That(reader.GetEncodedItem(dataId),
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(HasProperty(original, "zold"), Is.True);
            Assert.That(HasReference(document, "zlnk", dataId, originalId),
                Is.True);
            Assert.That(HasReference(document, "zlnk", dataId, newImageId),
                Is.True);
            Assert.That(HasGroup(document, "zset", groupId), Is.True);
            Assert.That(HasTopLevelBox(document, "zrwx"), Is.True);
        });

        reader.Close();
        rewriter.Close();
    }

    [Test]
    public void LowLevelAuthoringRejectsMismatchedAv1AndUnsafeOpaqueData()
    {
        Av1PayloadSource av1 = CreateAv1Payload(31);
        AvifWriter writer = new AvifWriter(new AvifEncoderOptions());
        Av1CodecConfiguration mismatch = CopyConfiguration(
            av1.Configuration);
        mismatch.Profile = mismatch.Profile == 0 ? 1 : 0;
        uint mismatched = writer.AddAv1Item(
            av1.Payload,
            mismatch,
            3,
            2,
            false);
        uint imageId = writer.AddAv1Item(
            av1.Payload,
            av1.Configuration,
            3,
            2,
            false);
        AvifResultCode essential = writer.AddOpaqueProperty(
            imageId,
            CreateOpaqueBox("zess", new byte[] { 1 }, true));
        AvifResultCode structural = writer.AddOpaqueTopLevelBox(
            CreateOpaqueBox("ftyp", new byte[] { 0, 0, 0, 0 }, false));

        Assert.Multiple(() =>
        {
            Assert.That(mismatched, Is.Zero);
            Assert.That(imageId, Is.Not.Zero);
            Assert.That(essential,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(structural,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        writer.Close();
    }

    static Av1PayloadSource CreateAv1Payload(byte value)
    {
        AvifImage image = CreateImage(3, 2, value);
        Av1Encoder encoder = new Av1Encoder();
        Av1EncoderOptions options = new Av1EncoderOptions();
        options.SequencePolicy = AvifSequenceEncodingPolicy.StillPicture;
        Assert.That(encoder.Configure(options),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(encoder.AddFrame(image, 1),
            Is.EqualTo(AvifResultCode.Success));
        AvifEncodeResult encoded = encoder.Finish();
        image.Close();
        encoder.Close();
        byte[] payload = encoded.GetData();
        Av1CodecConfigurationCodec codec =
            new Av1CodecConfigurationCodec();
        Av1CodecParameters parameters =
            codec.ParseSequenceHeaderParameters(payload);
        Av1CodecConfiguration configuration =
            new Av1CodecConfiguration();
        configuration.Profile = parameters.Profile;
        configuration.Level = parameters.Level;
        configuration.Tier = parameters.Tier;
        configuration.BitDepth = parameters.BitDepth;
        configuration.Monochrome = parameters.Monochrome;
        configuration.ChromaSubsamplingX = parameters.ChromaSubsamplingX;
        configuration.ChromaSubsamplingY = parameters.ChromaSubsamplingY;
        configuration.ChromaSamplePosition =
            parameters.ChromaSamplePosition;
        return new Av1PayloadSource(payload, configuration);
    }

    static Av1CodecConfiguration CopyConfiguration(
        Av1CodecConfiguration source)
    {
        Av1CodecConfiguration result = new Av1CodecConfiguration();
        result.Code = source.Code;
        result.Profile = source.Profile;
        result.Level = source.Level;
        result.Tier = source.Tier;
        result.BitDepth = source.BitDepth;
        result.Monochrome = source.Monochrome;
        result.ChromaSubsamplingX = source.ChromaSubsamplingX;
        result.ChromaSubsamplingY = source.ChromaSubsamplingY;
        result.ChromaSamplePosition = source.ChromaSamplePosition;
        result.InitialPresentationDelayPresent =
            source.InitialPresentationDelayPresent;
        result.InitialPresentationDelayMinusOne =
            source.InitialPresentationDelayMinusOne;
        result.ConfigObus = (byte[])source.ConfigObus.Clone();
        return result;
    }

    static AvifOpaqueBox CreateOpaqueBox(
        string type,
        byte[] payload,
        bool essential)
    {
        AvifBoxWriter writer = new AvifBoxWriter(1024);
        Assert.That(writer.BeginBox(type),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(writer.Write(payload),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(writer.EndBox(),
            Is.EqualTo(AvifResultCode.Success));
        byte[] raw = writer.Finish().GetData();
        writer.Close();
        return new AvifOpaqueBox(raw, essential);
    }

    static bool HasProperty(AvifImageItem item, string type)
    {
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            AvifItemProperty property = item.GetProperty(index)
                ?? throw new InvalidOperationException("Invalid internal state.");
            if (property.GetTypeName() == type)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static bool HasReference(
        AvifDocument document,
        string type,
        uint fromItemId,
        uint targetItemId)
    {
        int index = 0;
        while (index < document.GetReferenceCount())
        {
            AvifItemReference reference = document.GetReference(index)
                ?? throw new InvalidOperationException("Invalid internal state.");
            if (reference.GetTypeName() == type
                && reference.GetFromItemId() == fromItemId)
            {
                int target = 0;
                while (target < reference.GetTargetCount())
                {
                    if (reference.GetTargetItemId(target) == targetItemId)
                    {
                        return true;
                    }
                    target++;
                }
            }
            index++;
        }
        return false;
    }

    static bool HasGroup(
        AvifDocument document,
        string type,
        uint groupId)
    {
        int index = 0;
        while (index < document.GetGroupCount())
        {
            AvifEntityGroup group = document.GetGroup(index)
                ?? throw new InvalidOperationException("Invalid internal state.");
            if (group.GetTypeName() == type
                && group.GetGroupId() == groupId)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static bool HasTopLevelBox(AvifDocument document, string type)
    {
        int index = 0;
        while (index < document.GetTopLevelBoxCount())
        {
            AvifBox box = document.GetTopLevelBox(index)
                ?? throw new InvalidOperationException("Invalid internal state.");
            if (box.GetTypeName() == type)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static AvifImage CreateImage(int width, int height, byte value)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            false);
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

internal class Av1PayloadSource
{
    internal byte[] Payload;
    internal Av1CodecConfiguration Configuration;

    internal Av1PayloadSource(
        byte[] payload,
        Av1CodecConfiguration configuration)
    {
        Payload = payload;
        Configuration = configuration;
    }
}
