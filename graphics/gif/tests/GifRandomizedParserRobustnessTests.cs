using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifRandomizedParserRobustnessTests
{
    [Test]
    public void Read_RandomByteStreamsReturnStructuredResult()
    {
        Random random = new Random(137);
        int seed = 0;
        while (seed < 96)
        {
            byte[] data = CreateRandomBytes(random, seed);

            AssertReaderResultIsStructured(data, seed, false);
            AssertReaderResultIsStructured(data, seed, true);

            seed = seed + 1;
        }
    }

    [Test]
    public void Read_MutatedValidStreamsReturnStructuredResult()
    {
        Random random = new Random(9137);
        byte[] baseline = GifFixtures.MinimalGif89a();
        int seed = 0;
        while (seed < 96)
        {
            byte[] data = MutateAndMaybeTruncate(baseline, random, seed);

            AssertReaderResultIsStructured(data, seed, false);
            AssertReaderResultIsStructured(data, seed, true);

            seed = seed + 1;
        }
    }

    [Test]
    public void Identify_RandomByteStreamsReturnStructuredResult()
    {
        Random random = new Random(27137);
        GifIdentifier identifier = new GifIdentifier();
        int seed = 0;
        while (seed < 64)
        {
            byte[] data = CreateRandomBytes(random, seed);

            GifInfo byteInfo = identifier.IdentifyBytesWithOptions(data, CreateRobustnessOptions());
            AssertInfoIsStructured(byteInfo, seed, false);

            MemoryStream stream = new MemoryStream(data);
            GifInfo streamInfo = identifier.IdentifyStreamWithOptions(stream, CreateRobustnessOptions());
            AssertInfoIsStructured(streamInfo, seed, true);

            seed = seed + 1;
        }
    }

    static void AssertReaderResultIsStructured(byte[] data, int seed, bool useStream)
    {
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult result;
        if (useStream)
        {
            result = reader.ReadStreamWithOptions(new MemoryStream(data), CreateRobustnessOptions());
        }
        else
        {
            result = reader.ReadBytesWithOptions(data, CreateRobustnessOptions());
        }

        Assert.That(result, Is.Not.Null, CreateCaseMessage(seed, useStream, "reader result"));
        Assert.That(result.Document, Is.Not.Null, CreateCaseMessage(seed, useStream, "document"));
        Assert.That(result.Error, Is.Not.Null, CreateCaseMessage(seed, useStream, "error"));
        Assert.That(result.BytesRead, Is.GreaterThanOrEqualTo(0), CreateCaseMessage(seed, useStream, "bytes read lower bound"));
        Assert.That(result.BytesRead, Is.LessThanOrEqualTo(data.Length), CreateCaseMessage(seed, useStream, "bytes read upper bound"));

        if (result.Success)
        {
            Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.None), CreateCaseMessage(seed, useStream, "success error code"));
        }
        else
        {
            Assert.That(result.Error.Code, Is.Not.EqualTo(GifErrorCode.None), CreateCaseMessage(seed, useStream, "failure error code"));
        }
    }

    static void AssertInfoIsStructured(GifInfo info, int seed, bool useStream)
    {
        Assert.That(info, Is.Not.Null, CreateCaseMessage(seed, useStream, "info"));
        Assert.That(info.Error, Is.Not.Null, CreateCaseMessage(seed, useStream, "info error"));
        Assert.That(info.Width, Is.GreaterThanOrEqualTo(0), CreateCaseMessage(seed, useStream, "width"));
        Assert.That(info.Height, Is.GreaterThanOrEqualTo(0), CreateCaseMessage(seed, useStream, "height"));
        Assert.That(info.FrameCount, Is.GreaterThanOrEqualTo(0), CreateCaseMessage(seed, useStream, "frame count"));

        if (info.Success)
        {
            Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.None), CreateCaseMessage(seed, useStream, "identify success error code"));
        }
        else
        {
            Assert.That(info.Error.Code, Is.Not.EqualTo(GifErrorCode.None), CreateCaseMessage(seed, useStream, "identify failure error code"));
        }
    }

    static GifDecodeOptions CreateRobustnessOptions()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MetadataOnly = true;
        options.MaxBytes = 512;
        options.MaxSubBlocks = 32;
        options.MaxSubBlockBytes = 512;
        options.MaxFrames = 16;
        options.MaxWidth = 4096;
        options.MaxHeight = 4096;
        options.MaxDecodedPixels = 4096 * 4096;
        return options;
    }

    static byte[] CreateRandomBytes(Random random, int seed)
    {
        int length = (seed * 31) % 257;
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)random.Next(0, 256);
            index = index + 1;
        }

        return data;
    }

    static byte[] MutateAndMaybeTruncate(byte[] baseline, Random random, int seed)
    {
        byte[] data = CopyBytes(baseline);
        int mutationCount = 1 + (seed % 7);
        int mutationIndex = 0;
        while (mutationIndex < mutationCount)
        {
            int offset = random.Next(0, data.Length);
            data[offset] = (byte)random.Next(0, 256);
            mutationIndex = mutationIndex + 1;
        }

        if (seed % 3 == 0)
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
        byte[] truncated = new byte[length];
        int index = 0;
        while (index < length)
        {
            truncated[index] = source[index];
            index = index + 1;
        }

        return truncated;
    }

    static string CreateCaseMessage(int seed, bool useStream, string subject)
    {
        if (useStream)
        {
            return "Expected structured stream parse result for randomized case " + seed + " (" + subject + ").";
        }

        return "Expected structured byte parse result for randomized case " + seed + " (" + subject + ").";
    }
}
