using raw;

namespace raw.Tests;

public class PublicThumbnailWorkflowTests
{
    [Test]
    public void PublicDocumentOperationsSelectThumbnailsAndCopyAnyMappedPayload()
    {
        IRawCodec codec = new RawCodecService();
        NefFixture fixture = NefParserTests.BuildNef();
        RawDocument document = codec.OpenBytes(fixture.Bytes, null, new RawOpenOptions());
        RawThumbnail preview = codec.GetDocumentThumbnail(document, 0);
        RawPayloadBlock previewBlock = preview.Payload ?? throw new InvalidOperationException();
        RawImageInfo rawInfo = (document.Images ?? throw new InvalidOperationException()).Single(image => image.Kind == RawImageKind.Raw);
        RawPayloadBlock rawBlock = rawInfo.Payload ?? throw new InvalidOperationException();
        RawPayloadBlock metadataBlock = document.PayloadMap.Blocks.Single(block => block.Path.EndsWith("/NikonCaptureData", StringComparison.Ordinal));

        byte[] previewCopy = codec.ExtractDocumentPayload(document, previewBlock, new RawLimits(), CancellationToken.None);
        byte[] rawCopy = codec.ExtractDocumentPayload(document, rawBlock, new RawLimits(), CancellationToken.None);
        byte[] metadataCopy = codec.ExtractDocumentPayload(document, metadataBlock, new RawLimits(), CancellationToken.None);
        previewCopy[0] ^= 0xFF;
        byte[] secondPreviewCopy = codec.ExtractDocumentPayload(document, previewBlock, new RawLimits(), CancellationToken.None);
        RawPayloadBlock forged = new RawPayloadBlock(previewBlock.Kind, previewBlock.Path, previewBlock.Offset, previewBlock.Length, previewBlock.ImageIndex, previewBlock.PlaneIndex, previewBlock.BlockIndex, previewBlock.Compression, previewBlock.Decodable, previewBlock.PreserveByteExact);

        Assert.Multiple(() =>
        {
            Assert.That(preview, Is.SameAs(document.Thumbnails[0]));
            Assert.That(secondPreviewCopy, Is.EqualTo(fixture.Preview));
            Assert.That(rawCopy, Is.EqualTo(fixture.Raw));
            Assert.That(metadataCopy, Is.EqualTo(fixture.Capture));
            Assert.Throws<RawFormatException>(() => codec.GetDocumentThumbnail(document, -1));
            Assert.Throws<RawFormatException>(() => codec.GetDocumentThumbnail(document, document.Thumbnails.Length));
            Assert.Throws<RawFormatException>(() => codec.ExtractDocumentPayload(document, forged, new RawLimits(), CancellationToken.None));
        });
    }

    [Test]
    public void DeterministicGrayJpegEncodingHonorsStrideAndRetainedOrientation()
    {
        IRawCodec codec = new RawCodecService();
        int width = 9;
        int height = 8;
        int stride = 12;
        byte[] samples = Enumerable.Repeat((byte)0xEE, stride * height).ToArray();
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                samples[y * stride + x] = (byte)(30 + x * 8 + y * 4);
                x++;
            }
            y++;
        }
        RawRgbImage image = new RawRgbImage(width, height, 1, stride, samples, null, null, GrayChannels(), RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Rotate90Clockwise, false, false, null);

        RawThumbnail first = codec.EncodeJpegThumbnail(image, 3, 100, new RawLimits(), CancellationToken.None);
        RawThumbnail second = codec.EncodeJpegThumbnail(image, 3, 100, new RawLimits(), CancellationToken.None);
        RawRgbImage decoded = codec.DecodeThumbnail(first, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(RawThumbnailKind.Jpeg));
            Assert.That(first.Index, Is.EqualTo(3));
            Assert.That(first.MimeType, Is.EqualTo("image/jpeg"));
            Assert.That(first.Width, Is.EqualTo(width));
            Assert.That(first.Height, Is.EqualTo(height));
            Assert.That(first.Orientation, Is.EqualTo(RawOrientation.Rotate90Clockwise));
            Assert.That(first.Payload, Is.Null);
            Assert.That(first.EncodedBytes, Is.EqualTo(second.EncodedBytes));
            Assert.That(first.EncodedBytes.Take(2), Is.EqualTo(new byte[] { 0xFF, 0xD8 }));
            Assert.That(decoded.ChannelCount, Is.EqualTo(1));
            Assert.That(decoded.Orientation, Is.EqualTo(RawOrientation.Rotate90Clockwise));
            Assert.That(decoded.OrientationApplied, Is.False);
            Assert.That(ActiveGray(decoded.ByteSamples, width, height).Zip(ActiveGray(samples, width, height, stride), Difference), Is.All.LessThanOrEqualTo(2));
        });
    }

    [Test]
    public void RgbJpegEncodingUsesDeclaredChannelOrderAndNormalizesAppliedOrientation()
    {
        IRawCodec codec = new RawCodecService();
        int width = 8;
        int height = 8;
        int stride = 26;
        byte[] bgr = Enumerable.Repeat((byte)0xDD, stride * height).ToArray();
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int offset = y * stride + x * 3;
                bgr[offset] = 50;
                bgr[offset + 1] = 100;
                bgr[offset + 2] = 200;
                x++;
            }
            y++;
        }
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("B", RawChannelKind.Blue),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("R", RawChannelKind.Red)
        };
        RawRgbImage image = new RawRgbImage(width, height, 3, stride, bgr, null, null, channels, RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Rotate180, true, true, null);

        RawThumbnail thumbnail = codec.EncodeJpegThumbnail(image, 0, 100, new RawLimits(), CancellationToken.None);
        RawRgbImage decoded = codec.DecodeThumbnail(thumbnail, new RawLimits(), CancellationToken.None);
        RawCr2WriteInput cr2Input = new RawCr2WriteInput();
        cr2Input.PrimaryPreview = thumbnail;
        RawNefWriteInput nefInput = new RawNefWriteInput();
        nefInput.Preview = thumbnail;

        Assert.Multiple(() =>
        {
            Assert.That(thumbnail.Orientation, Is.EqualTo(RawOrientation.Normal));
            Assert.That(decoded.ChannelCount, Is.EqualTo(3));
            Assert.That(decoded.Channels.Select(channel => channel.Kind), Is.EqualTo(new RawChannelKind[] { RawChannelKind.Red, RawChannelKind.Green, RawChannelKind.Blue }));
            Assert.That(decoded.ByteSamples.Where((value, index) => index % 3 == 0), Is.All.InRange(198, 202));
            Assert.That(decoded.ByteSamples.Where((value, index) => index % 3 == 1), Is.All.InRange(98, 102));
            Assert.That(decoded.ByteSamples.Where((value, index) => index % 3 == 2), Is.All.InRange(48, 52));
            Assert.That(cr2Input.PrimaryPreview, Is.SameAs(thumbnail));
            Assert.That(nefInput.Preview, Is.SameAs(thumbnail));
        });
    }

    [Test]
    public void ThumbnailOperationsRejectUnsupportedInputsAndHonorLimitsAndCancellation()
    {
        IRawCodec codec = new RawCodecService();
        RawRgbImage gray = new RawRgbImage(8, 8, 1, 8, new byte[64], null, null, GrayChannels(), RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Normal, false, true, null);
        RawThumbnail valid = codec.EncodeJpegThumbnail(gray, 0, 90, new RawLimits(), CancellationToken.None);
        RawThumbnail opaque = new RawThumbnail(0, RawThumbnailKind.Opaque, 1, 1, RawOrientation.Normal, "application/octet-stream", new byte[] { 1 }, null, null);
        RawRgbImage words = new RawRgbImage(8, 8, 1, 8, null, new ushort[64], null, GrayChannels(), RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Normal, false, true, null);
        RawRgbImage wrongColor = new RawRgbImage(8, 8, 1, 8, new byte[64], null, null, GrayChannels(), RawColorSpace.Rec2020, RawTransferFunction.Srgb, RawOrientation.Normal, false, true, null);
        RawLimits previewLimit = new RawLimits();
        previewLimit.MaxPreviewBytes = valid.EncodedBytes.Length - 1;
        CancellationToken cancelled = new CancellationToken(true);
        NefFixture fixture = NefParserTests.BuildNef();
        RawDocument document = codec.OpenBytes(fixture.Bytes, null, new RawOpenOptions());
        RawPayloadBlock previewBlock = codec.GetDocumentThumbnail(document, 0).Payload ?? throw new InvalidOperationException();
        RawLimits copyLimit = new RawLimits();
        copyLimit.MaxAllocationBytes = previewBlock.Length - 1;
        copyLimit.MaxDecodedBytes = previewBlock.Length - 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => codec.DecodeThumbnail(opaque, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawUnsupportedException>(() => codec.EncodeJpegThumbnail(words, 0, 90, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawUnsupportedException>(() => codec.EncodeJpegThumbnail(wrongColor, 0, 90, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawWriteException>(() => codec.EncodeJpegThumbnail(gray, 0, 0, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawLimitException>(() => codec.DecodeThumbnail(valid, previewLimit, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => codec.EncodeJpegThumbnail(gray, 0, 90, previewLimit, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => codec.ExtractDocumentPayload(document, previewBlock, copyLimit, CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => codec.DecodeThumbnail(valid, new RawLimits(), cancelled));
            Assert.Throws<OperationCanceledException>(() => codec.EncodeJpegThumbnail(gray, 0, 90, new RawLimits(), cancelled));
            Assert.Throws<OperationCanceledException>(() => codec.ExtractDocumentPayload(document, previewBlock, new RawLimits(), cancelled));
        });
    }

    private static RawChannelDescription[] GrayChannels()
    {
        return new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
    }

    private static byte[] ActiveGray(byte[] samples, int width, int height)
    {
        return ActiveGray(samples, width, height, width);
    }

    private static byte[] ActiveGray(byte[] samples, int width, int height, int stride)
    {
        byte[] result = new byte[width * height];
        int y = 0;
        while (y < height)
        {
            samples.AsSpan(y * stride, width).CopyTo(result.AsSpan(y * width, width));
            y++;
        }
        return result;
    }

    private static int Difference(byte left, byte right)
    {
        return Math.Abs(left - right);
    }
}
