using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicRandomizedRobustnessTests
{
    [Test]
    [CancelAfter(15000)]
    public void PublicDecodeOperationsReturnSuccessOrStructuredFailureForArbitraryBytes()
    {
        Random random = new Random(71027);
        int caseIndex = 0;
        while (caseIndex < 256)
        {
            byte[] data = CreateArbitraryBytes(random, caseIndex);

            AssertPublicOperationsAreStructured(data, caseIndex, "arbitrary");

            caseIndex = caseIndex + 1;
        }
    }

    [Test]
    [CancelAfter(15000)]
    public void PublicDecodeOperationsReturnSuccessOrStructuredFailureForMutatedGifBytes()
    {
        Random random = new Random(27031);
        byte[] baseline = GifFixtures.ComplexValidGif89a();
        int caseIndex = 0;
        while (caseIndex < 256)
        {
            byte[] data = CreateMutatedGifBytes(baseline, random, caseIndex);

            AssertPublicOperationsAreStructured(data, caseIndex, "mutated");

            caseIndex = caseIndex + 1;
        }
    }

    static void AssertPublicOperationsAreStructured(byte[] data, int caseIndex, string corpus)
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = CreateBoundedOptions();
        int frameIndex = (caseIndex % 4) - 1;

        AssertIdentifyIsStructured(decoder, data, options, caseIndex, corpus);
        AssertDecodeIsStructured(decoder, data, options, caseIndex, corpus);
        AssertAnimationDecodeIsStructured(decoder, data, options, caseIndex, corpus);
        AssertRawFrameDecodeIsStructured(decoder, data, frameIndex, options, caseIndex, corpus);
        AssertCompositedFrameDecodeIsStructured(decoder, data, frameIndex, options, caseIndex, corpus);
    }

    static void AssertIdentifyIsStructured(IGifDecoder decoder, byte[] data, GifDecodeOptions options, int caseIndex, string corpus)
    {
        GifInfo info;
        try
        {
            info = decoder.IdentifyBytes(data, options);
        }
        catch (Exception exception)
        {
            Assert.Fail(CreateFailureMessage(caseIndex, corpus, "IdentifyBytes", exception));
            return;
        }

        Assert.That(info, Is.Not.Null, CreateCaseMessage(caseIndex, corpus, "IdentifyBytes result"));
        Assert.That(info.Error, Is.Not.Null, CreateCaseMessage(caseIndex, corpus, "IdentifyBytes diagnostic"));
        if (info.Success)
        {
            Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.None), CreateCaseMessage(caseIndex, corpus, "IdentifyBytes success code"));
        }
        else
        {
            Assert.That(info.Error.Code, Is.Not.EqualTo(GifErrorCode.None), CreateCaseMessage(caseIndex, corpus, "IdentifyBytes failure code"));
            Assert.That(info.Error.Position.BlockName, Is.Not.Empty, CreateCaseMessage(caseIndex, corpus, "IdentifyBytes block name"));
        }
    }

    static void AssertDecodeIsStructured(IGifDecoder decoder, byte[] data, GifDecodeOptions options, int caseIndex, string corpus)
    {
        try
        {
            GifDocument document = decoder.DecodeBytes(data, options);
            Assert.That(document, Is.Not.Null, CreateCaseMessage(caseIndex, corpus, "DecodeBytes result"));
        }
        catch (GifException exception)
        {
            AssertStructuredException(exception, caseIndex, corpus, "DecodeBytes");
        }
        catch (Exception exception)
        {
            Assert.Fail(CreateFailureMessage(caseIndex, corpus, "DecodeBytes", exception));
        }
    }

    static void AssertAnimationDecodeIsStructured(IGifDecoder decoder, byte[] data, GifDecodeOptions options, int caseIndex, string corpus)
    {
        try
        {
            GifAnimation animation = decoder.DecodeAnimationBytes(data, options);
            Assert.That(animation, Is.Not.Null, CreateCaseMessage(caseIndex, corpus, "DecodeAnimationBytes result"));
        }
        catch (GifException exception)
        {
            AssertStructuredException(exception, caseIndex, corpus, "DecodeAnimationBytes");
        }
        catch (Exception exception)
        {
            Assert.Fail(CreateFailureMessage(caseIndex, corpus, "DecodeAnimationBytes", exception));
        }
    }

    static void AssertRawFrameDecodeIsStructured(IGifDecoder decoder, byte[] data, int frameIndex, GifDecodeOptions options, int caseIndex, string corpus)
    {
        try
        {
            GifDocument document = decoder.DecodeFrameBytes(data, frameIndex, options);
            Assert.That(document, Is.Not.Null, CreateCaseMessage(caseIndex, corpus, "DecodeFrameBytes result"));
        }
        catch (GifException exception)
        {
            AssertStructuredException(exception, caseIndex, corpus, "DecodeFrameBytes");
        }
        catch (Exception exception)
        {
            Assert.Fail(CreateFailureMessage(caseIndex, corpus, "DecodeFrameBytes", exception));
        }
    }

    static void AssertCompositedFrameDecodeIsStructured(IGifDecoder decoder, byte[] data, int frameIndex, GifDecodeOptions options, int caseIndex, string corpus)
    {
        try
        {
            GifAnimation animation = decoder.DecodeCompositedFrameBytes(data, frameIndex, options);
            Assert.That(animation, Is.Not.Null, CreateCaseMessage(caseIndex, corpus, "DecodeCompositedFrameBytes result"));
        }
        catch (GifException exception)
        {
            AssertStructuredException(exception, caseIndex, corpus, "DecodeCompositedFrameBytes");
        }
        catch (Exception exception)
        {
            Assert.Fail(CreateFailureMessage(caseIndex, corpus, "DecodeCompositedFrameBytes", exception));
        }
    }

    static void AssertStructuredException(GifException exception, int caseIndex, string corpus, string operation)
    {
        Assert.That(exception.ErrorCode, Is.Not.EqualTo(GifErrorCode.None), CreateCaseMessage(caseIndex, corpus, operation + " error code"));
        Assert.That(exception.BlockName, Is.Not.Empty, CreateCaseMessage(caseIndex, corpus, operation + " block name"));
        GifErrorDiagnostic diagnostic = exception.ToDiagnostic();
        Assert.That(diagnostic.Code, Is.EqualTo(exception.ErrorCode), CreateCaseMessage(caseIndex, corpus, operation + " diagnostic code"));
        Assert.That(diagnostic.Position.BlockName, Is.EqualTo(exception.BlockName), CreateCaseMessage(caseIndex, corpus, operation + " diagnostic block"));
    }

    static GifDecodeOptions CreateBoundedOptions()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxWidth = 64;
        options.MaxHeight = 64;
        options.MaxFrames = 8;
        options.MaxDecodedPixels = 4096;
        options.MaxBytes = 512;
        options.MaxCompressedBytes = 512;
        options.MaxSubBlocks = 64;
        options.MaxSubBlockBytes = 512;
        options.MaxMemoryBytes = 262144;
        return options;
    }

    static byte[] CreateArbitraryBytes(Random random, int caseIndex)
    {
        int length = (caseIndex * 73) % 513;
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)random.Next(0, 256);
            index = index + 1;
        }

        return data;
    }

    static byte[] CreateMutatedGifBytes(byte[] baseline, Random random, int caseIndex)
    {
        byte[] data = CopyBytes(baseline);
        int mutationCount = 1 + (caseIndex % 12);
        int mutationIndex = 0;
        while (mutationIndex < mutationCount)
        {
            int offset = random.Next(6, data.Length);
            data[offset] = (byte)random.Next(0, 256);
            mutationIndex = mutationIndex + 1;
        }

        if (caseIndex % 3 == 0)
        {
            data = Truncate(data, random.Next(0, data.Length + 1));
        }

        return data;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static byte[] Truncate(byte[] source, int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    static string CreateCaseMessage(int caseIndex, string corpus, string subject)
    {
        return "Expected structured public result for " + corpus + " case " + caseIndex + " (" + subject + ").";
    }

    static string CreateFailureMessage(int caseIndex, string corpus, string operation, Exception exception)
    {
        return operation + " leaked " + exception.GetType().Name + " for " + corpus + " case " + caseIndex + ": " + exception.Message;
    }
}
