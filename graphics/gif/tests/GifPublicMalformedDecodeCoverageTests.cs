using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicMalformedDecodeCoverageTests
{
    [TestCase(GifStrictness.Strict)]
    [TestCase(GifStrictness.Normal)]
    [TestCase(GifStrictness.Tolerant)]
    public void MalformedSignature_IsFatalInEveryMode(GifStrictness strictness)
    {
        GifException exception = DecodeFailure(new byte[] { 0, 0, 0, 0, 0, 0 }, strictness, false);

        AssertFailure(exception, GifErrorCode.InvalidSignature, -1, "Header");
        Assert.That(exception.ByteOffset, Is.EqualTo(6));
    }

    [TestCase(GifStrictness.Strict)]
    [TestCase(GifStrictness.Normal)]
    [TestCase(GifStrictness.Tolerant)]
    public void UnsupportedVersion_IsFatalInEveryMode(GifStrictness strictness)
    {
        GifException exception = DecodeFailure(new byte[] { 71, 73, 70, 48, 48, 97 }, strictness, false);

        AssertFailure(exception, GifErrorCode.UnsupportedVersion, -1, "Header");
        Assert.That(exception.ByteOffset, Is.EqualTo(6));
    }

    [TestCase(GifStrictness.Strict, false)]
    [TestCase(GifStrictness.Normal, true)]
    [TestCase(GifStrictness.Tolerant, true)]
    public void PackedTableMetadataWithoutTable_IsStrictOnlyFailure(GifStrictness strictness, bool succeeds)
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = CreateOptions(strictness);
        byte[] data = CreateTrailerOnlyGif(1);

        if (succeeds)
        {
            GifDocument document = decoder.DecodeBytes(data, options);
            Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
            Assert.That(document.Frames, Is.Empty);
            return;
        }

        GifException exception = Assert.Throws<GifException>(() => decoder.DecodeBytes(data, options));
        AssertFailure(exception, GifErrorCode.MalformedPackedField, -1, "LogicalScreenDescriptor");
    }

    [TestCase(GifStrictness.Strict)]
    [TestCase(GifStrictness.Normal)]
    [TestCase(GifStrictness.Tolerant)]
    public void InvalidPaletteBackgroundIndex_IsFatalInEveryMode(GifStrictness strictness)
    {
        GifException exception = DecodeFailure(CreateOnePixelGif(1, 1, 2, 0, new byte[] { 0x44, 0x01 }), strictness, true);

        AssertFailure(exception, GifErrorCode.InvalidBackgroundColorIndex, -1, "Compositor");
    }

    [TestCase(GifStrictness.Strict)]
    [TestCase(GifStrictness.Normal)]
    [TestCase(GifStrictness.Tolerant)]
    public void TruncatedGlobalPalette_IsFatalInEveryMode(GifStrictness strictness)
    {
        GifException exception = DecodeFailure(CreateTruncatedGlobalPaletteGif(), strictness, false);

        AssertFailure(exception, GifErrorCode.UnexpectedEndOfStream, -1, "ColorTable");
    }

    [TestCase(GifStrictness.Strict)]
    [TestCase(GifStrictness.Normal)]
    [TestCase(GifStrictness.Tolerant)]
    public void FrameOutsideLogicalScreen_IsFatalInEveryMode(GifStrictness strictness)
    {
        GifException exception = DecodeFailure(CreateOnePixelGif(1, 1, 0, 1, new byte[] { 0x44, 0x01 }), strictness, false);

        AssertFailure(exception, GifErrorCode.InvalidImageRectangle, 0, "ImageDescriptor");
    }

    [TestCase(GifStrictness.Strict, 0xF9, "GraphicControlExtension")]
    [TestCase(GifStrictness.Strict, 0x01, "PlainTextExtension")]
    [TestCase(GifStrictness.Strict, 0xFF, "ApplicationExtension")]
    [TestCase(GifStrictness.Normal, 0xF9, "GraphicControlExtension")]
    [TestCase(GifStrictness.Normal, 0x01, "PlainTextExtension")]
    [TestCase(GifStrictness.Normal, 0xFF, "ApplicationExtension")]
    [TestCase(GifStrictness.Tolerant, 0xF9, "GraphicControlExtension")]
    [TestCase(GifStrictness.Tolerant, 0x01, "PlainTextExtension")]
    [TestCase(GifStrictness.Tolerant, 0xFF, "ApplicationExtension")]
    public void WrongFixedExtensionSize_IsFatalInEveryMode(GifStrictness strictness, byte label, string blockName)
    {
        GifException exception = DecodeFailure(CreateWrongFixedExtensionSizeGif(label), strictness, false);

        AssertFailure(exception, GifErrorCode.MalformedExtension, -1, blockName);
    }

    [TestCase(GifStrictness.Strict)]
    [TestCase(GifStrictness.Normal)]
    [TestCase(GifStrictness.Tolerant)]
    public void UnterminatedCommentSubBlocks_AreFatalInEveryMode(GifStrictness strictness)
    {
        GifException exception = DecodeFailure(CreateUnterminatedCommentGif(), strictness, false);

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.UnterminatedSubBlocks));
        Assert.That(exception.BlockName, Is.Not.Empty);
    }

    [TestCase(GifStrictness.Strict, false)]
    [TestCase(GifStrictness.Normal, false)]
    [TestCase(GifStrictness.Tolerant, true)]
    public void LzwStreamWithoutEndCode_RecoversOnlyInTolerantMode(GifStrictness strictness, bool succeeds)
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = CreateOptions(strictness);
        byte[] data = CreateOnePixelGif(1, 1, 0, 0, new byte[] { 0x04 });

        if (succeeds)
        {
            GifAnimation animation = decoder.DecodeAnimationBytes(data, options);
            Assert.That(animation.Frames, Has.Length.EqualTo(1));
            Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
            Assert.That(HasWarning(animation.Warnings, GifWarningCode.ToleratedMalformedBlock), Is.True);
            return;
        }

        GifException exception = Assert.Throws<GifException>(() => decoder.DecodeAnimationBytes(data, options));
        AssertFailure(exception, GifErrorCode.InvalidLzwCodeStream, 0, "LzwBitReader");
    }

    [TestCase(GifStrictness.Strict, false)]
    [TestCase(GifStrictness.Normal, false)]
    [TestCase(GifStrictness.Tolerant, true)]
    public void ShortDecodedPixelCount_RecoversOnlyInTolerantMode(GifStrictness strictness, bool succeeds)
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = CreateOptions(strictness);
        byte[] data = CreateOnePixelGif(2, 1, 0, 0, new byte[] { 0x44, 0x01 });

        if (succeeds)
        {
            GifAnimation animation = decoder.DecodeAnimationBytes(data, options);
            Assert.That(animation.Frames, Has.Length.EqualTo(1));
            Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 }));
            Assert.That(HasWarning(animation.Warnings, GifWarningCode.ToleratedMalformedBlock), Is.True);
            return;
        }

        GifException exception = Assert.Throws<GifException>(() => decoder.DecodeAnimationBytes(data, options));
        AssertFailure(exception, GifErrorCode.DecodedPixelCountMismatch, 0, "LzwOutput");
    }

    [TestCase(GifStrictness.Strict, GifErrorCode.MissingTrailer)]
    [TestCase(GifStrictness.Normal, GifErrorCode.UnexpectedEndOfStream)]
    [TestCase(GifStrictness.Tolerant, GifErrorCode.UnexpectedEndOfStream)]
    public void MissingTrailer_IsFatalWithModeSpecificDiagnosticAndWarning(GifStrictness strictness, GifErrorCode expectedCode)
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = CreateOptions(strictness);
        byte[] data = CreateMissingTrailerGif();

        GifException exception = Assert.Throws<GifException>(() => decoder.DecodeBytes(data, options));
        GifInfo info = decoder.IdentifyBytes(data, options);

        AssertFailure(exception, expectedCode, -1, "Trailer");
        Assert.That(info.Success, Is.False);
        Assert.That(info.Error.Code, Is.EqualTo(expectedCode));
        Assert.That(info.Warnings, Has.Length.EqualTo(1));
        Assert.That(info.Warnings[0].Code, Is.EqualTo(GifWarningCode.MissingTrailer));
        Assert.That(info.Warnings[0].Position.BlockName, Is.EqualTo("Trailer"));
    }

    [TestCase(GifStrictness.Strict)]
    [TestCase(GifStrictness.Normal)]
    [TestCase(GifStrictness.Tolerant)]
    public void ExtraDataAfterTrailer_IsSuccessfulWarningInEveryMode(GifStrictness strictness)
    {
        IGifDecoder decoder = new GifCodec();
        GifDocument document = decoder.DecodeBytes(CreateGifWithExtraTrailerData(), CreateOptions(strictness));

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.Warnings, Has.Length.EqualTo(1));
        Assert.That(document.Warnings[0].Code, Is.EqualTo(GifWarningCode.ExtraDataAfterTrailer));
        Assert.That(document.Warnings[0].Position.ByteOffset, Is.EqualTo(14));
        Assert.That(document.Warnings[0].Position.BlockName, Is.EqualTo("Trailer"));
    }

    [TestCase(GifStrictness.Strict)]
    [TestCase(GifStrictness.Normal)]
    [TestCase(GifStrictness.Tolerant)]
    public void UnknownTopLevelBlock_IsFatalInEveryMode(GifStrictness strictness)
    {
        GifException exception = DecodeFailure(CreateUnknownBlockGif(), strictness, false);

        AssertFailure(exception, GifErrorCode.UnknownBlockType, 0, "BlockReader");
        Assert.That(exception.ByteOffset, Is.EqualTo(13));
    }

    static GifException DecodeFailure(byte[] data, GifStrictness strictness, bool animation)
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = CreateOptions(strictness);
        if (animation)
        {
            return Assert.Throws<GifException>(() => decoder.DecodeAnimationBytes(data, options));
        }

        return Assert.Throws<GifException>(() => decoder.DecodeBytes(data, options));
    }

    static GifDecodeOptions CreateOptions(GifStrictness strictness)
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.Strictness = strictness;
        return options;
    }

    static void AssertFailure(GifException exception, GifErrorCode code, int frameIndex, string blockName)
    {
        Assert.That(exception.ErrorCode, Is.EqualTo(code));
        Assert.That(exception.FrameIndex, Is.EqualTo(frameIndex));
        Assert.That(exception.BlockName, Is.EqualTo(blockName));
    }

    static bool HasWarning(GifWarningDiagnostic[] warnings, GifWarningCode code)
    {
        int index = 0;
        while (index < warnings.Length)
        {
            if (warnings[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static byte[] CreateTrailerOnlyGif(byte packedFields)
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, packedFields, 0, 0,
            0x3B
        };
    }

    static byte[] CreateOnePixelGif(int width, int height, byte backgroundIndex, int left, byte[] compressedData)
    {
        byte[] data = new byte[33 + compressedData.Length];
        byte[] prefix = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            (byte)width, 0, (byte)height, 0, 0x80, backgroundIndex, 0,
            0, 0, 0,
            255, 255, 255,
            0x2C, (byte)left, 0, 0, 0, (byte)width, 0, (byte)height, 0, 0,
            2, (byte)compressedData.Length
        };
        Array.Copy(prefix, 0, data, 0, prefix.Length);
        Array.Copy(compressedData, 0, data, prefix.Length, compressedData.Length);
        data[data.Length - 2] = 0;
        data[data.Length - 1] = 0x3B;
        return data;
    }

    static byte[] CreateTruncatedGlobalPaletteGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0
        };
    }

    static byte[] CreateWrongFixedExtensionSizeGif(byte label)
    {
        if (label == 0x01)
        {
            return new byte[]
            {
                71, 73, 70, 56, 57, 97,
                1, 0, 1, 0, 0, 0, 0,
                0x21, 0x01, 11,
                0, 0, 0, 0, 1, 0, 1, 0, 1, 1, 0, 0,
                0,
                0x3B
            };
        }

        if (label == 0xFF)
        {
            return new byte[]
            {
                71, 73, 70, 56, 57, 97,
                1, 0, 1, 0, 0, 0, 0,
                0x21, 0xFF, 10,
                65, 80, 80, 76, 73, 67, 65, 84, 73, 79, 78,
                0,
                0x3B
            };
        }

        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x21, 0xF9, 3, 0, 0, 0, 0, 0,
            0x3B
        };
    }

    static byte[] CreateUnterminatedCommentGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x21, 0xFE, 1, 65
        };
    }

    static byte[] CreateMissingTrailerGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0
        };
    }

    static byte[] CreateGifWithExtraTrailerData()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x3B, 0xAA, 0xBB
        };
    }

    static byte[] CreateUnknownBlockGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0
        };
    }
}
