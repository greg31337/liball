using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifCodecInterfaceContractTests
{
    [Test]
    public void DecoderInterface_InvokesEveryMethodSuccessfullyWithNullOptions()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.MinimalGif89a();
        ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(source);

        GifDocument bytesDocument = decoder.DecodeBytes(source, null);
        GifDocument memoryDocument = decoder.DecodeMemory(memory, null);
        GifDocument streamDocument = decoder.DecodeStream(new MemoryStream(source), null);
        GifDocument byteFrame = decoder.DecodeFrameBytes(source, 0, null);
        GifDocument memoryFrame = decoder.DecodeFrameMemory(memory, 0, null);
        GifDocument streamFrame = decoder.DecodeFrameStream(new MemoryStream(source), 0, null);

        byte[] bytePixels = new byte[1];
        byte[] memoryPixels = new byte[1];
        byte[] streamPixels = new byte[1];
        int bytePixelCount = decoder.DecodeFrameBytesToSpan(source, 0, bytePixels, null);
        int memoryPixelCount = decoder.DecodeFrameMemoryToSpan(memory, 0, memoryPixels, null);
        int streamPixelCount = decoder.DecodeFrameStreamToSpan(new MemoryStream(source), 0, streamPixels, null);

        GifAnimation byteAnimation = decoder.DecodeAnimationBytes(source, null);
        GifAnimation memoryAnimation = decoder.DecodeAnimationMemory(memory, null);
        GifAnimation streamAnimation = decoder.DecodeAnimationStream(new MemoryStream(source), null);
        GifAnimation byteComposite = decoder.DecodeCompositedFrameBytes(source, 0, null);
        GifAnimation memoryComposite = decoder.DecodeCompositedFrameMemory(memory, 0, null);
        GifAnimation streamComposite = decoder.DecodeCompositedFrameStream(new MemoryStream(source), 0, null);

        byte[] byteRgba = new byte[4];
        byte[] memoryRgba = new byte[4];
        byte[] streamRgba = new byte[4];
        int byteRgbaCount = decoder.DecodeCompositedFrameBytesToSpan(source, 0, byteRgba, null);
        int memoryRgbaCount = decoder.DecodeCompositedFrameMemoryToSpan(memory, 0, memoryRgba, null);
        int streamRgbaCount = decoder.DecodeCompositedFrameStreamToSpan(new MemoryStream(source), 0, streamRgba, null);

        GifAnimation coalesced = decoder.Coalesce(bytesDocument, null);
        GifInfo byteInfo = decoder.IdentifyBytes(source, null);
        GifInfo memoryInfo = decoder.IdentifyMemory(memory, null);
        GifInfo streamInfo = decoder.IdentifyStream(new MemoryStream(source), null);

        Assert.That(bytesDocument.Frames, Has.Length.EqualTo(1));
        Assert.That(memoryDocument.Frames, Has.Length.EqualTo(1));
        Assert.That(streamDocument.Frames, Has.Length.EqualTo(1));
        Assert.That(byteFrame.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 0 }));
        Assert.That(memoryFrame.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 0 }));
        Assert.That(streamFrame.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 0 }));
        Assert.That(new int[] { bytePixelCount, memoryPixelCount, streamPixelCount }, Is.EqualTo(new int[] { 1, 1, 1 }));
        Assert.That(bytePixels, Is.EqualTo(new byte[] { 0 }));
        Assert.That(memoryPixels, Is.EqualTo(bytePixels));
        Assert.That(streamPixels, Is.EqualTo(bytePixels));
        Assert.That(byteAnimation.Frames, Has.Length.EqualTo(1));
        Assert.That(memoryAnimation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(byteAnimation.Frames[0].Bitmap.RgbaPixels));
        Assert.That(streamAnimation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(byteAnimation.Frames[0].Bitmap.RgbaPixels));
        Assert.That(byteComposite.Frames, Has.Length.EqualTo(1));
        Assert.That(memoryComposite.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(byteComposite.Frames[0].Bitmap.RgbaPixels));
        Assert.That(streamComposite.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(byteComposite.Frames[0].Bitmap.RgbaPixels));
        Assert.That(new int[] { byteRgbaCount, memoryRgbaCount, streamRgbaCount }, Is.EqualTo(new int[] { 4, 4, 4 }));
        Assert.That(byteRgba, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
        Assert.That(memoryRgba, Is.EqualTo(byteRgba));
        Assert.That(streamRgba, Is.EqualTo(byteRgba));
        Assert.That(coalesced.Frames, Has.Length.EqualTo(1));
        Assert.That(byteInfo.Success, Is.True);
        Assert.That(memoryInfo.FrameCount, Is.EqualTo(byteInfo.FrameCount));
        Assert.That(streamInfo.FrameCount, Is.EqualTo(byteInfo.FrameCount));
    }

    [Test]
    public void EncoderInterface_InvokesEveryMethodSuccessfullyWithNullOptions()
    {
        IGifEncoder encoder = new GifCodec();
        GifBitmap indexed = CreateIndexedBitmap(1);
        GifBitmap rgba = CreateRgbaBitmap(10, 20, 30);
        GifAnimation indexedAnimation = CreateAnimation(CreateIndexedBitmap(0), CreateIndexedBitmap(1));
        GifAnimation rgbaAnimation = CreateAnimation(CreateRgbaBitmap(10, 20, 30), CreateRgbaBitmap(40, 50, 60));
        GifDocument document = new GifCodec().CreateStill(indexed, null);

        byte[] encodedDocument = encoder.Encode(document, null);
        MemoryStream destination = new MemoryStream();
        encoder.Write(destination, document, null);
        GifEncodeResult indexedStill = encoder.EncodeIndexedStill(indexed, null, false, -1);
        GifEncodeResult indexedAnimationResult = encoder.EncodeIndexedAnimation(indexedAnimation, null);
        GifEncodeResult indexedFrames = encoder.EncodeIndexedFrames(new GifBitmap[] { CreateIndexedBitmap(0), CreateIndexedBitmap(1) }, null);
        GifEncodeResult rgbaStill = encoder.EncodeRgbaStill(rgba, null, null);
        GifEncodeResult rgbaAnimationResult = encoder.EncodeRgbaAnimation(rgbaAnimation, null, null);
        GifEncodeResult rgbaFrames = encoder.EncodeRgbaFrames(new GifBitmap[] { CreateRgbaBitmap(10, 20, 30), CreateRgbaBitmap(40, 50, 60) }, null, null);

        Assert.That(encodedDocument.Length, Is.GreaterThan(0));
        Assert.That(destination.ToArray(), Is.EqualTo(encodedDocument));
        AssertSuccessfulEncode(indexedStill);
        AssertSuccessfulEncode(indexedAnimationResult);
        AssertSuccessfulEncode(indexedFrames);
        AssertSuccessfulEncode(rgbaStill);
        AssertSuccessfulEncode(rgbaAnimationResult);
        AssertSuccessfulEncode(rgbaFrames);
    }

    [Test]
    public void FactoryInterface_InvokesEveryMethodSuccessfullyWithNullOptions()
    {
        IGifFactory factory = new GifCodec();

        GifBitmap bitmap = factory.CreateIndexedBitmap(1, 1, new byte[] { 1 }, CreatePalette());
        GifDocument still = factory.CreateStill(bitmap, null);
        GifDocument animation = factory.CreateAnimation(new GifBitmap[] { CreateIndexedBitmap(0), CreateIndexedBitmap(1) }, null, null);

        Assert.That(bitmap.UsesIndexedPixels, Is.True);
        Assert.That(bitmap.IndexedPixels, Is.EqualTo(new byte[] { 1 }));
        Assert.That(still.Frames, Has.Length.EqualTo(1));
        Assert.That(animation.Frames, Has.Length.EqualTo(2));
    }

    [Test]
    public void TransformerInterface_InvokesEveryMethodSuccessfullyWithNullOptions()
    {
        IGifTransformer transformer = new GifCodec();
        GifAnimation deconstructed = transformer.Deconstruct(CreateAnimation(CreateRgbaBitmap(1, 2, 3), CreateRgbaBitmap(4, 5, 6)));
        GifErrorDiagnostic documentValidation = transformer.ValidateDocument(CreateDocument(), null);
        GifErrorDiagnostic bitmapValidation = transformer.ValidateBitmap(CreateIndexedBitmap(0), null);
        GifErrorDiagnostic animationValidation = transformer.ValidateAnimation(CreateAnimation(CreateIndexedBitmap(0), CreateIndexedBitmap(1)), null);
        int milliseconds = transformer.ConvertDelayHundredthsToMilliseconds(7);
        int hundredths = transformer.ConvertDelayMillisecondsToHundredths(74, GifDelayRoundingMode.Nearest);

        GifAnimation delays = CreateAnimation(CreateRgbaBitmap(1, 2, 3), CreateRgbaBitmap(4, 5, 6));
        transformer.SetDelay(delays, 20);
        transformer.SetDelays(delays, new int[] { 30, 40 });
        transformer.SetDisposal(delays, GifDisposalMethod.RestoreToBackground);
        transformer.SetDisposals(delays, new GifDisposalMethod[] { GifDisposalMethod.DoNotDispose, GifDisposalMethod.RestoreToPrevious });
        transformer.SetAnimationLoopCount(delays, 3);

        GifDocument metadata = CreateAnimationDocument();
        transformer.SetDocumentLoopCount(metadata, 4);
        transformer.SetCommentsFromText(metadata, new string[] { "first" });
        transformer.SetComments(metadata, new GifCommentExtension[] { new GifCommentExtension("second") });

        GifDocument globalPaletteDocument = CreateAnimationDocument();
        transformer.SetPalette(globalPaletteDocument, CreateAlternatePalette());
        GifDocument localPaletteDocument = CreateAnimationDocument();
        transformer.SetPalettes(localPaletteDocument, new GifPalette[] { CreatePalette(), CreateAlternatePalette() });
        GifAnimation optimized = transformer.Optimize(CreateAnimation(CreateRgbaBitmap(1, 2, 3), CreateRgbaBitmap(4, 5, 6)), null);

        Assert.That(deconstructed.Frames, Has.Length.EqualTo(2));
        Assert.That(documentValidation.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(bitmapValidation.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(animationValidation.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(milliseconds, Is.EqualTo(70));
        Assert.That(hundredths, Is.EqualTo(7));
        Assert.That(delays.Frames[0].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(delays.Frames[1].DelayMilliseconds, Is.EqualTo(40));
        Assert.That(delays.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(delays.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(delays.LoopCount, Is.EqualTo(3));
        Assert.That(metadata.LoopCount, Is.EqualTo(4));
        Assert.That(metadata.Comments[0].Text, Is.EqualTo("second"));
        Assert.That(globalPaletteDocument.GlobalColorTable.Colors[0].Red, Is.EqualTo(255));
        Assert.That(localPaletteDocument.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(localPaletteDocument.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(optimized.Frames, Has.Length.EqualTo(2));
    }

    [Test]
    public void Interfaces_ReportInvalidInputsThroughTheirPublicContracts()
    {
        IGifDecoder decoder = new GifCodec();
        IGifEncoder encoder = new GifCodec();
        IGifFactory factory = new GifCodec();
        IGifTransformer transformer = new GifCodec();

        GifException decodeException = Assert.Throws<GifException>(() => decoder.DecodeBytes(new byte[] { 1, 2, 3, 4, 5, 6 }, null));
        GifException encodeException = Assert.Throws<GifException>(() => encoder.Encode(null, null));
        GifDocument missingStill = factory.CreateStill(null, null);
        GifDocument missingAnimation = factory.CreateAnimation(null, null, null);
        GifBitmap invalidBitmap = factory.CreateIndexedBitmap(0, 1, null, null);
        GifErrorDiagnostic invalidDocument = transformer.ValidateDocument(null, null);
        GifException transformException = Assert.Throws<GifException>(() => transformer.SetDelay(null, -1));

        Assert.That(decodeException.ErrorCode, Is.EqualTo(GifErrorCode.InvalidSignature));
        Assert.That(encodeException.ErrorCode, Is.EqualTo(GifErrorCode.MissingLogicalScreenDescriptor));
        Assert.That(missingStill.Frames, Is.Empty);
        Assert.That(missingAnimation.Frames, Is.Empty);
        Assert.That(invalidBitmap.Width, Is.EqualTo(0));
        Assert.That(invalidBitmap.IndexedPixels, Is.Empty);
        Assert.That(invalidDocument.Code, Is.EqualTo(GifErrorCode.MissingLogicalScreenDescriptor));
        Assert.That(transformException.ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
    }

    [Test]
    public void InterfaceCallsMatchConcreteCodecWithExplicitDefaultOptions()
    {
        GifCodec codec = new GifCodec();
        IGifDecoder decoder = codec;
        IGifEncoder encoder = codec;
        IGifFactory factory = codec;
        IGifTransformer transformer = codec;
        byte[] source = GifFixtures.MinimalGif89a();

        GifDocument interfaceDecode = decoder.DecodeBytes(source, null);
        GifDocument concreteDecode = codec.DecodeBytes(source, new GifDecodeOptions());
        GifEncodeResult interfaceEncode = encoder.EncodeRgbaStill(CreateRgbaBitmap(7, 8, 9), null, null);
        GifEncodeResult concreteEncode = codec.EncodeRgbaStill(CreateRgbaBitmap(7, 8, 9), new GifQuantizeOptions(), new GifEncodeOptions());
        GifDocument interfaceStill = factory.CreateStill(CreateIndexedBitmap(1), null);
        GifDocument concreteStill = codec.CreateStill(CreateIndexedBitmap(1), new GifEncodeOptions());
        GifAnimation interfaceOptimize = transformer.Optimize(CreateAnimation(CreateRgbaBitmap(1, 2, 3), CreateRgbaBitmap(4, 5, 6)), null);
        GifAnimation concreteOptimize = codec.Optimize(CreateAnimation(CreateRgbaBitmap(1, 2, 3), CreateRgbaBitmap(4, 5, 6)), new GifEncodeOptions());

        Assert.That(interfaceDecode.Version, Is.EqualTo(concreteDecode.Version));
        Assert.That(interfaceDecode.Frames[0].CompressedImageData, Is.EqualTo(concreteDecode.Frames[0].CompressedImageData));
        Assert.That(interfaceEncode.Data, Is.EqualTo(concreteEncode.Data));
        Assert.That(codec.Encode(interfaceStill, null), Is.EqualTo(codec.Encode(concreteStill, null)));
        Assert.That(interfaceOptimize.Frames.Length, Is.EqualTo(concreteOptimize.Frames.Length));
        Assert.That(interfaceOptimize.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(concreteOptimize.Frames[1].Bitmap.RgbaPixels));
        Assert.That(transformer.ConvertDelayHundredthsToMilliseconds(9), Is.EqualTo(codec.ConvertDelayHundredthsToMilliseconds(9)));
    }

    static void AssertSuccessfulEncode(GifEncodeResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(result.Data.Length, Is.GreaterThan(0));
    }

    static GifDocument CreateDocument()
    {
        return new GifCodec().CreateStill(CreateIndexedBitmap(0), null);
    }

    static GifDocument CreateAnimationDocument()
    {
        return new GifCodec().CreateAnimation(new GifBitmap[] { CreateIndexedBitmap(0), CreateIndexedBitmap(1) }, null, null);
    }

    static GifBitmap CreateIndexedBitmap(int index)
    {
        return GifBitmap.CreateIndexed(1, 1, new byte[] { (byte)index }, CreatePalette());
    }

    static GifBitmap CreateRgbaBitmap(byte red, byte green, byte blue)
    {
        return new GifBitmap(1, 1, new byte[] { red, green, blue, 255 });
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static GifPalette CreateAlternatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(255, 0, 0),
            new GifColor(0, 0, 255)
        });
    }

    static GifAnimation CreateAnimation(GifBitmap firstBitmap, GifBitmap secondBitmap)
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(firstBitmap, 10, 0),
            CreateFrame(secondBitmap, 20, 1)
        };
        animation.TotalDurationMilliseconds = 30;
        return animation;
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap, int delayMilliseconds, int frameIndex)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.DelayMilliseconds = delayMilliseconds;
        frame.DisposalMethod = GifDisposalMethod.DoNotDispose;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        frame.FrameIndex = frameIndex;
        return frame;
    }
}
