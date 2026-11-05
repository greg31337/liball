using System.Buffers.Binary;

namespace avif.Tests;

public class AvifAssociatedDecodeOutputTests
{
    [Test]
    public void ItemDecodeReturnsAlphaDepthArbitraryAuxiliariesAndMetadata()
    {
        AvifImage master = CreateImage(2, 2, 31);
        AvifImage alpha = CreateImage(2, 2, 211);
        AvifImage depth = CreateImage(2, 2, 97);
        AvifImage arbitrary = CreateImage(2, 2, 149);
        AvifWriter writer = AvifWriter.Create();
        uint masterId = writer.AddImage(master);
        uint alphaId = writer.AddAuxiliary(
            masterId,
            new AvifAuxiliaryImage(
                alpha,
                AvifAuxiliaryKind.Alpha,
                true));
        writer.AddAuxiliary(
            masterId,
            new AvifAuxiliaryImage(
                depth,
                AvifAuxiliaryKind.Depth,
                false));
        writer.AddAuxiliary(
            masterId,
            AvifAuxiliaryImage.Create(
                arbitrary,
                "urn:example:auxiliary:mask"));
        writer.AddMetadata(
            masterId,
            new AvifMetadata(
                "mime",
                "application/octet-stream",
                new byte[] { 7, 8, 9 }));
        master.Close();
        alpha.Close();
        depth.Close();
        arbitrary.Close();
        AvifEncodeResult encoded = writer.Finish();
        AvifReader reader = new AvifReader(
            encoded.GetData(),
            new AvifDecoderOptions());
        reader.Parse();

        AvifDecodeResult decoded = reader.DecodePrimaryImage();
        AvifDecodeResult selectedAlpha = reader.DecodeAlpha(masterId);
        AvifDecodeResult selectedDepth = reader.DecodeDepth(masterId);
        AvifDecodeResult selectedOther = reader.DecodeAuxiliaryItem(
            masterId,
            2);
        AvifMetadata? metadata = reader.GetAssociatedMetadataItem(
            masterId,
            0);
        AvifDocument? document = reader.GetDocument();
        AvifImageItem? masterItem = (document
            ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(masterId);
        AvifImageItem? alphaItem = document.FindItem(alphaId);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.GetDocument(), Is.Not.Null);
            Assert.That(decoded.GetSelectedItemId(), Is.EqualTo(masterId));
            Assert.That(decoded.GetDecodedAuxiliaryCount(), Is.EqualTo(3));
            Assert.That(
                (decoded.GetDecodedAuxiliary(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .MasterItemId,
                Is.EqualTo(masterId));
            Assert.That(decoded.GetAlphaImage(), Is.Not.Null);
            Assert.That(decoded.GetDepthImage(), Is.Not.Null);
            Assert.That(
                (decoded.GetDecodedAuxiliary(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .Premultiplied,
                Is.True);
            Assert.That(
                (decoded.GetDecodedAuxiliary(2)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .Kind,
                Is.EqualTo(AvifAuxiliaryKind.Other));
            Assert.That(
                (decoded.GetDecodedAuxiliary(2)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .TypeName,
                Is.EqualTo("urn:example:auxiliary:mask"));
            Assert.That(decoded.GetAssociatedMetadataCount(), Is.EqualTo(1));
            Assert.That(
                (masterItem ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetAssociatedAuxiliaryItemId(0),
                Is.EqualTo(alphaId));
            Assert.That(masterItem.GetAssociatedAuxiliaryItemId(3), Is.Zero);
            Assert.That(
                (alphaItem ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetAuxiliaryTypeName(),
                Is.EqualTo("urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"));
            Assert.That(
                (decoded.GetAssociatedMetadata(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetData(),
                Is.EqualTo(new byte[] { 7, 8, 9 }));
            Assert.That(selectedAlpha.GetAlphaImage(), Is.SameAs(selectedAlpha.GetImage()));
            Assert.That(selectedDepth.GetDepthImage(), Is.SameAs(selectedDepth.GetImage()));
            Assert.That(
                (selectedOther.GetDecodedAuxiliary(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .Kind,
                Is.EqualTo(AvifAuxiliaryKind.Other));
            Assert.That(
                (metadata ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetData(),
                Is.EqualTo(new byte[] { 7, 8, 9 }));
        });
        AssertPlaneValue(decoded.GetImage(), 31);
        AssertPlaneValue(decoded.GetAlphaImage(), 211);
        AssertPlaneValue(decoded.GetDepthImage(), 97);
        AssertPlaneValue(
            (decoded.GetDecodedAuxiliary(2)
                ?? throw new InvalidOperationException("Invalid internal state."))
                .Image,
            149);

        CloseResultImages(decoded);
        (selectedAlpha.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (selectedDepth.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (selectedOther.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
    }

    [Test]
    public void DecodeReturnsGainMapAndToneMapAlternatives()
    {
        AvifImage baseImage = CreateImage(3, 2, 41);
        AvifImage gainImage = CreateImage(2, 1, 173);
        AvifGainMap gainMap = new AvifGainMap(
            gainImage,
            CreateGainMapMetadata(),
            null);
        AvifWriter writer = AvifWriter.Create();
        uint baseId = writer.AddImage(baseImage);
        uint toneMapId = writer.AddGainMap(baseId, gainMap);
        baseImage.Close();
        gainImage.Close();
        AvifEncodeResult encoded = writer.Finish();
        AvifReader reader = new AvifReader(
            encoded.GetData(),
            new AvifDecoderOptions());
        reader.Parse();

        AvifDecodeResult primary = reader.DecodePrimaryImage();
        AvifDecodeResult selectedGain = reader.DecodeGainMap(toneMapId);
        AvifDecodeResult selectedToneMap = reader.DecodeToneMap(toneMapId);
        AvifDecodeResult oneCall = Avif.Decode(encoded.GetData());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(primary.GetSelectedItemId(), Is.EqualTo(baseId));
            Assert.That(primary.GetGainMap(), Is.Not.Null);
            Assert.That(primary.GetToneMap(), Is.Not.Null);
            Assert.That(primary.GetDocument(), Is.Not.Null);
            Assert.That(selectedGain.GetSelectedItemId(), Is.EqualTo(toneMapId));
            Assert.That(selectedGain.GetGainMap(), Is.Not.Null);
            Assert.That(selectedGain.GetToneMap(), Is.Null);
            Assert.That(selectedToneMap.GetGainMap(), Is.Not.Null);
            Assert.That(selectedToneMap.GetToneMap(), Is.Not.Null);
            Assert.That(selectedToneMap.GetDocument(), Is.Not.Null);
            Assert.That(oneCall.GetGainMap(), Is.Not.Null);
            Assert.That(oneCall.GetToneMap(), Is.Not.Null);
            Assert.That(oneCall.GetDocument(), Is.Not.Null);
        });
        AssertPlaneValue(primary.GetImage(), 41);
        AssertPlaneValue(
            (primary.GetGainMap()
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetImage(),
            173);
        AssertPlaneValue(selectedGain.GetImage(), 173);

        CloseToneMapResult(primary);
        (selectedGain.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        CloseToneMapResult(selectedToneMap);
        CloseToneMapResult(oneCall);
        reader.Close();
        writer.Close();
    }

    [Test]
    public void ProgressiveDecodeReturnsEveryLayerAndSupportsItemSelection()
    {
        AvifImage preview = CreateImage(2, 2, 23);
        AvifImage middle = CreateImage(3, 3, 79);
        AvifImage finalImage = CreateImage(4, 4, 157);
        AvifLayeredImage layered = new AvifLayeredImage(0, true);
        layered.AddLayer(preview);
        layered.AddLayer(middle);
        layered.AddLayer(finalImage);
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddLayeredImage(layered);
        preview.Close();
        middle.Close();
        finalImage.Close();
        AvifEncodeResult encoded = writer.Finish();
        AvifReader reader = new AvifReader(
            encoded.GetData(),
            new AvifDecoderOptions());
        reader.Parse();

        AvifDecodeResult decoded = reader.DecodePrimaryImage();
        AvifDecodeResult middleLayer = reader.DecodeItemLayer(itemId, 1);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.GetSelectedItemId(), Is.EqualTo(itemId));
            Assert.That(decoded.GetProgressiveLayerCount(), Is.EqualTo(3));
            Assert.That(
                (decoded.GetProgressiveLayer(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetWidth(),
                Is.EqualTo(2));
            Assert.That(
                (decoded.GetProgressiveLayer(1)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetWidth(),
                Is.EqualTo(3));
            Assert.That(
                (decoded.GetProgressiveLayer(2)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetWidth(),
                Is.EqualTo(4));
            Assert.That(decoded.GetProgressiveLayer(3), Is.Null);
            Assert.That(middleLayer.GetSelectedItemId(), Is.EqualTo(itemId));
            Assert.That(middleLayer.GetProgressiveLayerCount(), Is.EqualTo(1));
            Assert.That(
                (middleLayer.GetImage()
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetWidth(),
                Is.EqualTo(3));
        });

        CloseResultImages(decoded);
        (middleLayer.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
    }

    [Test]
    public void SequenceDecodeReturnsTimingSampleAndAlphaTrackOutput()
    {
        AvifImage color = CreateImage(2, 2, 47);
        AvifImage alpha = CreateImage(2, 2, 199);
        AvifSequence sequence = new AvifSequence(1000);
        sequence.AddFrame(new AvifFrame(color, alpha, 250, true));
        AvifWriter writer = AvifWriter.Create();
        writer.AddSequence(sequence);
        color.Close();
        alpha.Close();
        AvifEncodeResult encoded = writer.Finish();
        AvifDecoderOptions options = new AvifDecoderOptions();
        options.SourceSelection = AvifSourceSelection.ImageSequence;
        AvifReader reader = new AvifReader(encoded.GetData(), options);
        reader.Parse();

        AvifDecodeResult frame = reader.DecodeSelectedImage();
        AvifDecodeResult selectedAlpha = reader.DecodeSequenceAuxiliary(0, 0);
        AvifDecodeResult oneCall = Avif.Decode(encoded.GetData(), options);

        Assert.Multiple(() =>
        {
            Assert.That(frame.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(frame.GetSelectedTrackId(), Is.Not.Zero);
            Assert.That(frame.GetSelectedFrameIndex(), Is.Zero);
            Assert.That(frame.GetSelectedFrameTiming(), Is.Not.Null);
            Assert.That(
                (frame.GetSelectedFrameTiming()
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetDuration(),
                Is.EqualTo(250));
            Assert.That(frame.GetSelectedSequenceSample(), Is.Not.Null);
            Assert.That(frame.GetAlphaImage(), Is.Not.Null);
            Assert.That(frame.GetDecodedAuxiliaryCount(), Is.EqualTo(1));
            Assert.That(
                (frame.GetDecodedAuxiliary(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .TrackId,
                Is.Not.Zero);
            Assert.That(
                (frame.GetDecodedAuxiliary(0)
                    ?? throw new InvalidOperationException("Invalid internal state."))
                    .MasterTrackId,
                Is.EqualTo(frame.GetSelectedTrackId()));
            Assert.That(selectedAlpha.GetAlphaImage(), Is.SameAs(selectedAlpha.GetImage()));
            Assert.That(selectedAlpha.GetSelectedFrameIndex(), Is.Zero);
            Assert.That(oneCall.GetAlphaImage(), Is.Not.Null);
            Assert.That(oneCall.GetSelectedSequenceSample(), Is.Not.Null);
            Assert.That(oneCall.GetDocument(), Is.Not.Null);
        });
        AssertPlaneValue(frame.GetImage(), 47);
        AssertPlaneValue(frame.GetAlphaImage(), 199);

        CloseResultImages(frame);
        (selectedAlpha.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        CloseResultImages(oneCall);
        reader.Close();
        writer.Close();
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
        AvifPlane? plane = image.GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    static void AssertPlaneValue(AvifImage? image, byte expected)
    {
        AvifPlane? plane = (image
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetPlane(AvifPlaneKind.Y);
        Assert.That(
            (plane ?? throw new InvalidOperationException("Invalid internal state."))
                .GetRowSpan(0)[0],
            Is.EqualTo(expected));
    }

    static void CloseResultImages(AvifDecodeResult result)
    {
        AvifImage? image = result.GetImage();
        if (image != null)
        {
            image.Close();
        }
        int auxiliaryIndex = 0;
        while (auxiliaryIndex < result.GetDecodedAuxiliaryCount())
        {
            AvifDecodedAuxiliary? auxiliary = result.GetDecodedAuxiliary(
                auxiliaryIndex);
            if (auxiliary != null && auxiliary.Image != image)
            {
                auxiliary.Image.Close();
            }
            auxiliaryIndex++;
        }
        int layerIndex = 0;
        while (layerIndex < result.GetProgressiveLayerCount())
        {
            AvifImage? layer = result.GetProgressiveLayer(layerIndex);
            if (layer != null && layer != image)
            {
                layer.Close();
            }
            layerIndex++;
        }
    }

    static void CloseToneMapResult(AvifDecodeResult result)
    {
        AvifImage? image = result.GetImage();
        if (image != null)
        {
            image.Close();
        }
        AvifImage? gainImage = result.GetGainMap() == null
            ? null : (result.GetGainMap()
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetImage();
        if (gainImage != null && gainImage != image)
        {
            gainImage.Close();
        }
    }
}
