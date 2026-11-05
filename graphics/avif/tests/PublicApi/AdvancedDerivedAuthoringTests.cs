using System.Buffers.Binary;

namespace avif.Tests;

public class AdvancedDerivedAuthoringTests
{
    [Test]
    public void AuthorsToneMapWithHiddenGainInputAndPreferredFallbackGroup()
    {
        AvifImage baseImage = CreateImage(3, 2, 8, 37);
        AvifImage gainImage = CreateImage(2, 1, 8, 191);
        AvifGainMapMetadata metadata = CreateGainMapMetadata();
        AvifColorInfo alternateColor = AvifColorInfo.Create(
            AvifColorPrimaries.Bt2020,
            AvifTransferCharacteristics.Smpte2084,
            AvifMatrixCoefficients.Bt2020NonConstantLuminance,
            AvifRange.Full);
        AvifGainMap gainMap = AvifGainMap.Create(
            gainImage,
            metadata,
            alternateColor);
        AvifWriter writer = AvifWriter.Create();
        uint baseItemId = writer.AddImage(baseImage);
        uint toneMapItemId = writer.AddGainMap(baseItemId, gainMap);
        baseImage.Close();
        gainImage.Close();

        AvifEncodeResult finished = writer.Finish();
        AvifReader reader = AvifReader.Create(finished.GetData());
        AvifResultCode parsed = reader.Parse();
        AvifDocument? document = reader.GetDocument();
        AvifImageItem? gainItem = (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(2);
        AvifImageItem? toneMapItem = document.FindItem(toneMapItemId);
        AvifItemReference? dependency = (toneMapItem ?? throw new InvalidOperationException("Invalid internal state.")).GetOutgoingReference(0);
        AvifEntityGroup? alternatives = document.GetGroup(0);
        byte[] payload = reader.GetEncodedItem(toneMapItemId);

        Assert.Multiple(() =>
        {
            Assert.That(baseItemId, Is.EqualTo(1));
            Assert.That(toneMapItemId, Is.EqualTo(3));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(document.GetItemCount(), Is.EqualTo(3));
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId(),
                Is.EqualTo(baseItemId));
            Assert.That((gainItem ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(), Is.EqualTo("av01"));
            Assert.That(gainItem.IsHidden(), Is.True);
            Assert.That(toneMapItem.GetItemType(), Is.EqualTo("tmap"));
            Assert.That(toneMapItem.IsDerivedImage(), Is.True);
            Assert.That(toneMapItem.IsHidden(), Is.False);
            Assert.That(toneMapItem.GetPixelWidth(), Is.EqualTo(3));
            Assert.That(toneMapItem.GetPixelHeight(), Is.EqualTo(2));
            Assert.That(toneMapItem.GetPixelBitDepth(), Is.EqualTo(8));
            Assert.That((dependency ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(), Is.EqualTo("dimg"));
            Assert.That(dependency.GetTargetCount(), Is.EqualTo(2));
            Assert.That(dependency.GetTargetItemId(0), Is.EqualTo(baseItemId));
            Assert.That(dependency.GetTargetItemId(1), Is.EqualTo(2));
            Assert.That((alternatives ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(AvifEntityGroupKind.Alternate));
            Assert.That(alternatives.GetEntityId(0), Is.EqualTo(toneMapItemId));
            Assert.That(alternatives.GetEntityId(1), Is.EqualTo(baseItemId));
            Assert.That(payload, Is.EqualTo(metadata.GetData()));
            Assert.That(FindProperty(toneMapItem, "av1C"), Is.Null);
            Assert.That(FindProperty(toneMapItem, "colr"), Is.Not.Null);
        });

        reader.Close();
        writer.Close();
    }

    [Test]
    public void AuthorsStandardSuffixRecipeWithVisibleFallbackAndHiddenSuffix()
    {
        AvifImage fallback = CreateImage(2, 2, 8, 17);
        AvifImage suffix = CreateImage(2, 2, 8, 233);
        AvifSampleTransform transform =
            AvifSampleTransform.CreateSuffixBitDepthExtension(
                fallback,
                suffix);
        AvifWriter writer = AvifWriter.Create();
        uint sampleTransformItemId = writer.AddSampleTransform(transform);
        fallback.Close();
        suffix.Close();

        AvifEncodeResult finished = writer.Finish();
        AvifReader reader = AvifReader.Create(finished.GetData());
        AvifResultCode parsed = reader.Parse();
        AvifDocument? document = reader.GetDocument();
        AvifImageItem? fallbackItem = (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1);
        AvifImageItem? suffixItem = document.FindItem(2);
        AvifImageItem? sampleTransformItem = document.FindItem(3);
        AvifItemReference? dependency = (sampleTransformItem ?? throw new InvalidOperationException("Invalid internal state.")).GetOutgoingReference(0);
        AvifEntityGroup? alternatives = document.GetGroup(0);
        byte[] payload = reader.GetEncodedItem(sampleTransformItemId);
        AvifSampleTransform reparsed = AvifSampleTransform.Create(payload, 2);
        long[] evaluated = new long[1];
        AvifResultCode evaluation = reparsed.Evaluate(
            new long[] { 17, 233 },
            evaluated);
        AvifSampleTransform residualRecipe =
            AvifSampleTransform.CreateResidualBitDepthExtension();
        AvifSampleTransform reparsedResidual = AvifSampleTransform.Create(
            residualRecipe.GetData(),
            2);
        long[] residualValue = new long[1];
        AvifResultCode residualEvaluation = reparsedResidual.Evaluate(
            new long[] { 100, 20 },
            residualValue);

        Assert.Multiple(() =>
        {
            Assert.That(transform.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(transform.GetOutputBitDepth(), Is.EqualTo(16));
            Assert.That(sampleTransformItemId, Is.EqualTo(3));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId(), Is.EqualTo(1));
            Assert.That((fallbackItem ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.False);
            Assert.That((suffixItem ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That(sampleTransformItem.GetItemType(), Is.EqualTo("sato"));
            Assert.That(sampleTransformItem.IsDerivedImage(), Is.True);
            Assert.That(sampleTransformItem.GetPixelBitDepth(), Is.EqualTo(16));
            Assert.That(FindProperty(sampleTransformItem, "av1C"), Is.Null);
            Assert.That((FindProperty(sampleTransformItem, "pixi") ?? throw new InvalidOperationException("Invalid internal state."))
                .GetPixelChannelBitDepth(0), Is.EqualTo(16));
            Assert.That((dependency ?? throw new InvalidOperationException("Invalid internal state.")).GetTargetItemId(0), Is.EqualTo(1));
            Assert.That(dependency.GetTargetItemId(1), Is.EqualTo(2));
            Assert.That((alternatives ?? throw new InvalidOperationException("Invalid internal state.")).GetEntityId(0),
                Is.EqualTo(sampleTransformItemId));
            Assert.That(alternatives.GetEntityId(1), Is.EqualTo(1));
            Assert.That(reparsed.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(evaluation, Is.EqualTo(AvifResultCode.Success));
            Assert.That(evaluated[0], Is.EqualTo(4585));
            Assert.That(reparsedResidual.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(residualEvaluation, Is.EqualTo(AvifResultCode.Success));
            Assert.That(residualValue[0], Is.EqualTo(1492));
        });

        reader.Close();
        writer.Close();
    }

    [Test]
    public void AuthorsArbitraryExpressionPayloadAndRejectsUnboundOrMismatchedInputs()
    {
        byte[] payload = new byte[] { 2, 3, 1, 2, 128 };
        AvifImage first = CreateImage(2, 2, 8, 9);
        AvifImage second = CreateImage(2, 2, 8, 12);
        AvifSampleTransform arbitrary = AvifSampleTransform.Create(
            payload,
            new AvifImage[] { first, second },
            12);
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddSampleTransform(arbitrary);
        payload[0] = 0;
        AvifEncodeResult finished = writer.Finish();
        AvifReader reader = AvifReader.Create(finished.GetData());
        reader.Parse();
        byte[] extracted = reader.GetEncodedItem(itemId);

        AvifWriter invalidWriter = AvifWriter.Create();
        AvifSampleTransform unbound = AvifSampleTransform.Create(
            new byte[] { 2, 1, 1 },
            1);
        uint unboundId = invalidWriter.AddSampleTransform(unbound);
        AvifImage wrongSize = CreateImage(3, 2, 8, 5);
        AvifSampleTransform mismatched = AvifSampleTransform.Create(
            new byte[] { 2, 3, 1, 2, 128 },
            new AvifImage[] { first, wrongSize },
            12);
        uint mismatchedId = invalidWriter.AddSampleTransform(mismatched);
        uint nextId = invalidWriter.AddImage(first);

        Assert.Multiple(() =>
        {
            Assert.That(itemId, Is.EqualTo(3));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(extracted, Is.EqualTo(new byte[] { 2, 3, 1, 2, 128 }));
            Assert.That(((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId) ?? throw new InvalidOperationException("Invalid internal state.")).GetPixelBitDepth(),
                Is.EqualTo(12));
            Assert.That(unboundId, Is.Zero);
            Assert.That(mismatchedId, Is.Zero);
            Assert.That(nextId, Is.EqualTo(1));
        });

        first.Close();
        second.Close();
        wrongSize.Close();
        reader.Close();
        writer.Close();
        invalidWriter.Close();
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
            payload.AsSpan(offset, 4), numerator);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset + 4, 4), denominator);
    }

    static void WriteUnsigned(
        byte[] payload,
        int offset,
        uint numerator,
        uint denominator)
    {
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset, 4), numerator);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset + 4, 4), denominator);
    }

    static AvifItemProperty? FindProperty(AvifImageItem item, string type)
    {
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            AvifItemProperty? property = item.GetProperty(index);
            if ((property ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName() == type)
            {
                return property;
            }
            index++;
        }
        return null;
    }

    static AvifImage CreateImage(
        int width,
        int height,
        int bitDepth,
        byte value)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            bitDepth,
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
}
