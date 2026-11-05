using System.IO.Compression;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class CompressionLevelTrialRunnerTests
{
    [Test]
    public void TrialModels_DefaultToEmptyResults()
    {
        PngCompressionTrial trial = new PngCompressionTrial();
        PngCompressionTrialResult result = new PngCompressionTrialResult();

        Assert.That(trial.CompressionLevel, Is.EqualTo(0));
        Assert.That(trial.EncodedBytes, Is.Not.Null);
        Assert.That(trial.EncodedBytes, Is.Empty);
        Assert.That(trial.ByteCount, Is.EqualTo(0));
        Assert.That(trial.IsBest, Is.False);
        Assert.That(trial.WasBailedOut, Is.False);
        Assert.That(result.Trials, Is.Not.Null);
        Assert.That(result.Trials, Is.Empty);
        Assert.That(result.BestTrial, Is.Not.Null);
        Assert.That(result.TriedCompressionLevels, Is.False);
        Assert.That(result.BailedOutTrialCount, Is.EqualTo(0));
    }

    [Test]
    public void CompressionLevelMapper_MapsPngLevelsToRuntimeCompressionLevels()
    {
        Assert.That(PngCompressionLevelMapper.Map(0), Is.EqualTo(CompressionLevel.NoCompression));
        Assert.That(PngCompressionLevelMapper.Map(1), Is.EqualTo(CompressionLevel.Fastest));
        Assert.That(PngCompressionLevelMapper.Map(3), Is.EqualTo(CompressionLevel.Fastest));
        Assert.That(PngCompressionLevelMapper.Map(4), Is.EqualTo(CompressionLevel.Optimal));
        Assert.That(PngCompressionLevelMapper.Map(8), Is.EqualTo(CompressionLevel.Optimal));
        Assert.That(PngCompressionLevelMapper.Map(9), Is.EqualTo(CompressionLevel.SmallestSize));
    }

    [Test]
    public void Run_TriesConfiguredCompressionLevelsAndChoosesSmallestOutput()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 0;
        options.MaximumCompressionLevel = 9;
        options.UseEarlyBailout = false;

        PngCompressionTrialResult result = PngCompressionLevelTrialRunner.Run(CreateImage(), options);

        Assert.That(result.TriedCompressionLevels, Is.True);
        Assert.That(result.Trials, Has.Length.EqualTo(10));
        AssertTrialLevels(result.Trials, 0, 9);
        Assert.That(result.BestTrial.ByteCount, Is.EqualTo(FindSmallestByteCount(result.Trials)));
        Assert.That(result.BestTrial.IsBest, Is.True);
        Assert.That(CountBestTrials(result.Trials), Is.EqualTo(1));
        Assert.That(result.Trials[0].EncodedBytes.Length, Is.EqualTo(result.Trials[0].ByteCount));
        Assert.That(result.Trials[0].EncodedBytes[0], Is.EqualTo(PngConstants.SignatureByte0));
    }

    [Test]
    public void Run_BailsOutCompressionTrialsThatExceedCurrentBest()
    {
        PngOptimizerOptions baselineOptions = new PngOptimizerOptions();
        baselineOptions.MinimumCompressionLevel = 0;
        baselineOptions.MaximumCompressionLevel = 9;
        baselineOptions.UseEarlyBailout = false;
        PngCompressionTrialResult baseline = PngCompressionLevelTrialRunner.Run(CreateImage(), baselineOptions);
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 0;
        options.MaximumCompressionLevel = 9;
        options.BestKnownByteCount = baseline.BestTrial.ByteCount;

        PngCompressionTrialResult result = PngCompressionLevelTrialRunner.Run(CreateImage(), options);

        Assert.That(result.BailedOutTrialCount, Is.GreaterThan(0));
        Assert.That(result.BestTrial.ByteCount, Is.EqualTo(baseline.BestTrial.ByteCount));
        AssertBailedOutTrials(result.Trials, baseline.BestTrial.ByteCount);
    }

    [Test]
    public void Run_KeepsPayloadsWhenEarlyBailoutIsDisabled()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 0;
        options.MaximumCompressionLevel = 2;
        options.BestKnownByteCount = 1;
        options.UseEarlyBailout = false;

        PngCompressionTrialResult result = PngCompressionLevelTrialRunner.Run(CreateImage(), options);

        Assert.That(result.BailedOutTrialCount, Is.EqualTo(0));
        Assert.That(CountEmptyPayloads(result.Trials), Is.EqualTo(0));
    }

    [Test]
    public void Run_UsesSingleMinimumLevelWhenCompressionTrialsAreDisabled()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 2;
        options.MaximumCompressionLevel = 9;
        options.TryCompressionLevels = false;

        PngCompressionTrialResult result = PngCompressionLevelTrialRunner.Run(CreateImage(), options);

        Assert.That(result.TriedCompressionLevels, Is.False);
        Assert.That(result.Trials, Has.Length.EqualTo(1));
        Assert.That(result.Trials[0].CompressionLevel, Is.EqualTo(2));
        Assert.That(result.BestTrial, Is.SameAs(result.Trials[0]));
        Assert.That(result.BestTrial.IsBest, Is.True);
    }

    [Test]
    public void Run_PropagatesMetadataPreservationOptionsToEncoder()
    {
        PngImage image = CreateImage();
        image.Metadata.Chunks = new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Title", "Metadata"))
        };
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 9;
        options.MaximumCompressionLevel = 9;
        options.MetadataPolicy = PngMetadataPolicy.Strip;

        PngCompressionTrialResult result = PngCompressionLevelTrialRunner.Run(image, options);
        PngChunkListResult chunkList = PngChunkListReader.Read(result.BestTrial.EncodedBytes, PngConstants.SignatureLength);

        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkText), Is.False);
    }

    [Test]
    public void Run_RejectsInvalidInputs()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        PngOptimizerOptions nullLimits = new PngOptimizerOptions();
        PngOptimizerOptions invalidMinimum = new PngOptimizerOptions();
        PngOptimizerOptions invalidMaximum = new PngOptimizerOptions();
        PngOptimizerOptions reversedRange = new PngOptimizerOptions();
        PngEncoderOptions invalidEncoderOptions = new PngEncoderOptions();
        nullLimits.Limits = null!;
        invalidMinimum.MinimumCompressionLevel = -1;
        invalidMaximum.MaximumCompressionLevel = 10;
        reversedRange.MinimumCompressionLevel = 7;
        reversedRange.MaximumCompressionLevel = 3;
        invalidEncoderOptions.CompressionLevel = 10;

        Assert.Throws<ArgumentNullException>(() => PngCompressionLevelTrialRunner.Run(null!, options));
        Assert.Throws<ArgumentNullException>(() => PngCompressionLevelTrialRunner.Run(CreateImage(), null!));
        Assert.Throws<ArgumentNullException>(() => PngCompressionLevelTrialRunner.Run(CreateImage(), nullLimits));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressionLevelTrialRunner.Run(CreateImage(), invalidMinimum));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressionLevelTrialRunner.Run(CreateImage(), invalidMaximum));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressionLevelTrialRunner.Run(CreateImage(), reversedRange));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressionLevelMapper.Map(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCompressionLevelMapper.Map(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngEncoder.EncodeToBytes(CreateImage(), invalidEncoderOptions));
    }

    static PngImage CreateImage()
    {
        PngImage image = new PngImage();
        image.Pixels = CreateBuffer(PngPixelFormat.Rgba8, 4, 3, 16, new byte[]
        {
            10, 20, 30, 255, 10, 20, 30, 255, 10, 20, 30, 255, 10, 20, 30, 255,
            10, 20, 30, 255, 40, 50, 60, 255, 40, 50, 60, 255, 10, 20, 30, 255,
            10, 20, 30, 255, 10, 20, 30, 255, 10, 20, 30, 255, 10, 20, 30, 255
        });
        return image;
    }

    static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }

    static PngText CreateText(string keyword, string textValue)
    {
        PngText text = new PngText();
        text.Keyword = keyword;
        text.Text = textValue;
        return text;
    }

    static void AssertTrialLevels(PngCompressionTrial[] trials, int firstLevel, int lastLevel)
    {
        Assert.That(trials[0].CompressionLevel, Is.EqualTo(firstLevel));
        Assert.That(trials[trials.Length - 1].CompressionLevel, Is.EqualTo(lastLevel));

        int index = 1;
        while (index < trials.Length)
        {
            Assert.That(trials[index].CompressionLevel, Is.EqualTo(trials[index - 1].CompressionLevel + 1));
            index += 1;
        }
    }

    static long FindSmallestByteCount(PngCompressionTrial[] trials)
    {
        long smallest = trials[0].ByteCount;
        int index = 1;
        while (index < trials.Length)
        {
            if (trials[index].ByteCount < smallest)
            {
                smallest = trials[index].ByteCount;
            }

            index += 1;
        }

        return smallest;
    }

    static int CountBestTrials(PngCompressionTrial[] trials)
    {
        int count = 0;
        int index = 0;
        while (index < trials.Length)
        {
            if (trials[index].IsBest)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    static void AssertBailedOutTrials(PngCompressionTrial[] trials, long bestByteCount)
    {
        int index = 0;
        while (index < trials.Length)
        {
            if (trials[index].WasBailedOut)
            {
                Assert.That(trials[index].EncodedBytes, Is.Empty);
                Assert.That(trials[index].ByteCount, Is.GreaterThan(bestByteCount));
                Assert.That(trials[index].IsBest, Is.False);
            }
            else
            {
                Assert.That(trials[index].EncodedBytes, Is.Not.Empty);
                Assert.That(trials[index].ByteCount, Is.LessThanOrEqualTo(bestByteCount));
            }

            index += 1;
        }
    }

    static int CountEmptyPayloads(PngCompressionTrial[] trials)
    {
        int count = 0;
        int index = 0;
        while (index < trials.Length)
        {
            if (trials[index].EncodedBytes.Length == 0)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    static bool HasChunk(PngChunk[] chunks, string chunkType)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
