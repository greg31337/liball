using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class FilterStrategyTrialRunnerTests
{
    [Test]
    public void TrialModels_DefaultToEmptyResults()
    {
        PngFilterStrategyTrial trial = new PngFilterStrategyTrial();
        PngFilterStrategyTrialResult result = new PngFilterStrategyTrialResult();

        Assert.That(trial.CompressionLevel, Is.EqualTo(0));
        Assert.That(trial.FilterType, Is.EqualTo(PngFilterType.None));
        Assert.That(trial.RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Adaptive));
        Assert.That(trial.UseFixedFilter, Is.False);
        Assert.That(trial.EncodedBytes, Is.Not.Null);
        Assert.That(trial.EncodedBytes, Is.Empty);
        Assert.That(trial.ByteCount, Is.EqualTo(0));
        Assert.That(trial.IsBest, Is.False);
        Assert.That(trial.WasBailedOut, Is.False);
        Assert.That(result.Trials, Is.Not.Null);
        Assert.That(result.Trials, Is.Empty);
        Assert.That(result.BestTrial, Is.Not.Null);
        Assert.That(result.TriedFilterStrategies, Is.False);
        Assert.That(result.BailedOutTrialCount, Is.EqualTo(0));
    }

    [Test]
    public void Run_TriesAdaptiveAndFixedFilterStrategiesAndChoosesSmallestOutput()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 9;
        options.UseEarlyBailout = false;

        PngFilterStrategyTrialResult result = PngFilterStrategyTrialRunner.Run(CreateImage(), options);

        Assert.That(result.TriedFilterStrategies, Is.True);
        Assert.That(result.Trials, Has.Length.EqualTo(7));
        int trialIndex = 0;
        while (trialIndex < result.Trials.Length)
        {
            Assert.That(result.Trials[trialIndex].CompressionLevel, Is.EqualTo(9));
            trialIndex += 1;
        }
        Assert.That(result.Trials[0].UseFixedFilter, Is.False);
        Assert.That(result.Trials[0].RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Adaptive));
        Assert.That(result.Trials[0].FilterType, Is.EqualTo(PngFilterType.None));
        Assert.That(result.Trials[1].RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Fast));
        Assert.That(result.Trials[1].UseFixedFilter, Is.False);
        AssertFixedTrial(result.Trials[2], PngFilterType.None);
        AssertFixedTrial(result.Trials[3], PngFilterType.Sub);
        AssertFixedTrial(result.Trials[4], PngFilterType.Up);
        AssertFixedTrial(result.Trials[5], PngFilterType.Average);
        AssertFixedTrial(result.Trials[6], PngFilterType.Paeth);
        Assert.That(result.BestTrial.ByteCount, Is.EqualTo(FindSmallestByteCount(result.Trials)));
        Assert.That(result.BestTrial.IsBest, Is.True);
        Assert.That(CountBestTrials(result.Trials), Is.EqualTo(1));
        Assert.That(result.Trials[0].EncodedBytes[0], Is.EqualTo(PngConstants.SignatureByte0));
    }

    [Test]
    public void Run_BailsOutFilterTrialsThatExceedCurrentBest()
    {
        PngOptimizerOptions baselineOptions = new PngOptimizerOptions();
        baselineOptions.MinimumCompressionLevel = 9;
        baselineOptions.UseEarlyBailout = false;
        PngFilterStrategyTrialResult baseline = PngFilterStrategyTrialRunner.Run(CreateImage(), baselineOptions);
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 9;
        options.BestKnownByteCount = baseline.BestTrial.ByteCount;

        PngFilterStrategyTrialResult result = PngFilterStrategyTrialRunner.Run(CreateImage(), options);

        Assert.That(result.BailedOutTrialCount, Is.GreaterThan(0));
        Assert.That(result.BestTrial.ByteCount, Is.EqualTo(baseline.BestTrial.ByteCount));
        AssertBailedOutTrials(result.Trials, baseline.BestTrial.ByteCount);
    }

    [Test]
    public void Run_KeepsPayloadsWhenEarlyBailoutIsDisabled()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 9;
        options.BestKnownByteCount = 1;
        options.UseEarlyBailout = false;

        PngFilterStrategyTrialResult result = PngFilterStrategyTrialRunner.Run(CreateImage(), options);

        Assert.That(result.BailedOutTrialCount, Is.EqualTo(0));
        Assert.That(CountEmptyPayloads(result.Trials), Is.EqualTo(0));
    }

    [Test]
    public void Run_UsesSingleConfiguredFilterWhenFixedFilterIsRequested()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.Paeth;
        options.MinimumCompressionLevel = 9;

        PngFilterStrategyTrialResult result = PngFilterStrategyTrialRunner.Run(CreateImage(), options);

        Assert.That(result.TriedFilterStrategies, Is.False);
        Assert.That(result.Trials, Has.Length.EqualTo(1));
        AssertFixedTrial(result.Trials[0], PngFilterType.Paeth);
        Assert.That(result.BestTrial, Is.SameAs(result.Trials[0]));
        Assert.That(result.BestTrial.IsBest, Is.True);
    }

    [Test]
    public void Run_UsesSingleExplicitFastAdaptiveOrFixedStrategy()
    {
        PngOptimizerOptions fast = new PngOptimizerOptions();
        fast.RowFilterStrategy = PngRowFilterStrategy.Fast;
        PngOptimizerOptions adaptive = new PngOptimizerOptions();
        adaptive.RowFilterStrategy = PngRowFilterStrategy.Adaptive;
        PngOptimizerOptions fixedOptions = new PngOptimizerOptions();
        fixedOptions.RowFilterStrategy = PngRowFilterStrategy.Fixed;
        fixedOptions.FixedFilterType = PngFilterType.Up;

        PngFilterStrategyTrialResult fastResult = PngFilterStrategyTrialRunner.Run(CreateImage(), fast);
        PngFilterStrategyTrialResult adaptiveResult = PngFilterStrategyTrialRunner.Run(CreateImage(), adaptive);
        PngFilterStrategyTrialResult fixedResult = PngFilterStrategyTrialRunner.Run(CreateImage(), fixedOptions);

        Assert.That(fastResult.Trials, Has.Length.EqualTo(1));
        Assert.That(fastResult.Trials[0].RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Fast));
        Assert.That(adaptiveResult.Trials, Has.Length.EqualTo(1));
        Assert.That(adaptiveResult.Trials[0].RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Adaptive));
        Assert.That(fixedResult.Trials, Has.Length.EqualTo(1));
        AssertFixedTrial(fixedResult.Trials[0], PngFilterType.Up);
    }

    [Test]
    public void Run_UsesSingleConfiguredFilterWhenFilterTrialsAreDisabled()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.TryFilters = false;
        options.FixedFilterType = PngFilterType.Sub;
        options.MinimumCompressionLevel = 9;

        PngFilterStrategyTrialResult result = PngFilterStrategyTrialRunner.Run(CreateImage(), options);

        Assert.That(result.TriedFilterStrategies, Is.False);
        Assert.That(result.Trials, Has.Length.EqualTo(1));
        AssertFixedTrial(result.Trials[0], PngFilterType.Sub);
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
        options.MetadataPolicy = PngMetadataPolicy.Strip;

        PngFilterStrategyTrialResult result = PngFilterStrategyTrialRunner.Run(image, options);
        PngChunkListResult chunkList = PngChunkListReader.Read(result.BestTrial.EncodedBytes, PngConstants.SignatureLength);

        Assert.That(HasChunk(chunkList.Chunks, PngConstants.ChunkText), Is.False);
    }

    [Test]
    public void Run_RejectsInvalidInputs()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        PngOptimizerOptions nullLimits = new PngOptimizerOptions();
        PngOptimizerOptions invalidCompression = new PngOptimizerOptions();
        PngOptimizerOptions invalidFixed = new PngOptimizerOptions();
        PngOptimizerOptions invalidDisabled = new PngOptimizerOptions();
        PngOptimizerOptions invalidStrategy = new PngOptimizerOptions();
        nullLimits.Limits = null!;
        invalidCompression.MinimumCompressionLevel = -1;
        invalidFixed.UseFixedFilter = true;
        invalidFixed.FixedFilterType = (PngFilterType)99;
        invalidDisabled.TryFilters = false;
        invalidDisabled.FixedFilterType = (PngFilterType)99;
        invalidStrategy.RowFilterStrategy = (PngRowFilterStrategy)99;

        Assert.Throws<ArgumentNullException>(() => PngFilterStrategyTrialRunner.Run(null!, options));
        Assert.Throws<ArgumentNullException>(() => PngFilterStrategyTrialRunner.Run(CreateImage(), null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterStrategyTrialRunner.Run(CreateImage(), nullLimits));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterStrategyTrialRunner.Run(CreateImage(), invalidCompression));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterStrategyTrialRunner.Run(CreateImage(), invalidFixed));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterStrategyTrialRunner.Run(CreateImage(), invalidDisabled));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterStrategyTrialRunner.Run(CreateImage(), invalidStrategy));
    }

    static PngImage CreateImage()
    {
        PngImage image = new PngImage();
        image.Pixels = CreateBuffer(PngPixelFormat.Rgba8, 4, 3, 16, new byte[]
        {
            10, 20, 30, 255, 12, 22, 32, 255, 14, 24, 34, 255, 16, 26, 36, 255,
            11, 21, 31, 255, 13, 23, 33, 255, 15, 25, 35, 255, 17, 27, 37, 255,
            12, 22, 32, 255, 14, 24, 34, 255, 16, 26, 36, 255, 18, 28, 38, 255
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

    static void AssertFixedTrial(PngFilterStrategyTrial trial, PngFilterType filterType)
    {
        Assert.That(trial.UseFixedFilter, Is.True);
        Assert.That(trial.RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Fixed));
        Assert.That(trial.FilterType, Is.EqualTo(filterType));
        Assert.That(trial.EncodedBytes, Is.Not.Empty);
        Assert.That(trial.ByteCount, Is.EqualTo(trial.EncodedBytes.Length));
    }

    static long FindSmallestByteCount(PngFilterStrategyTrial[] trials)
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

    static int CountBestTrials(PngFilterStrategyTrial[] trials)
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

    static void AssertBailedOutTrials(PngFilterStrategyTrial[] trials, long bestByteCount)
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

    static int CountEmptyPayloads(PngFilterStrategyTrial[] trials)
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
