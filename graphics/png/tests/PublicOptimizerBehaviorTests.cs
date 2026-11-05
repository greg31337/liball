using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PublicOptimizerBehaviorTests
{
    [Test]
    public void TrialMethods_PropagateEveryEncoderFacingOption()
    {
        PngImage image = PngValidFixtures.CreateRgba8Image(2, 1);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxWidth = 17;
        PngUnknownChunkReplacement replacement = new PngUnknownChunkReplacement();
        replacement.Type = "vpAg";
        replacement.Data = new byte[] { 7, 8 };
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.Limits = limits;
        options.MetadataPolicy = PngMetadataPolicy.RemoveSelected;
        options.TextCompressionMode = PngTextCompressionMode.Automatic;
        options.TextCompressionThreshold = 23;
        options.UnknownChunkDisposition = PngChunkDisposition.Replace;
        options.MetadataChunkTypesToKeep = new string[] { PngConstants.ChunkGamma };
        options.MetadataChunkTypesToRemove = new string[] { PngConstants.ChunkPhysicalPixels };
        options.UnknownChunkReplacements = new PngUnknownChunkReplacement[] { replacement };
        options.PixelDataChanged = false;
        options.FixedFilterType = PngFilterType.Paeth;
        options.RowFilterStrategy = PngRowFilterStrategy.Fast;
        options.MinimumCompressionLevel = 4;
        options.MaximumCompressionLevel = 7;
        options.TryFilters = false;
        options.TryCompressionLevels = false;
        options.UseFixedFilter = true;
        options.AllowColorTypeReduction = false;
        options.AllowBitDepthReduction = false;
        options.AllowPaletteReduction = false;
        options.UseEarlyBailout = true;
        options.BestKnownByteCount = 100;
        ScriptedEncoder encoder = new ScriptedEncoder(new int[] { 50, 50 });
        IPngOptimizer optimizer = new PngOptimizationService();

        PngCompressionTrialResult compression = optimizer.RunCompressionTrialsWithEncoder(image, options, encoder);
        PngFilterStrategyTrialResult filters = optimizer.RunFilterTrialsWithEncoder(image, options, encoder);

        Assert.That(compression.Trials, Has.Length.EqualTo(1));
        Assert.That(filters.Trials, Has.Length.EqualTo(1));
        Assert.That(compression.Trials[0].CompressionLevel, Is.EqualTo(4));
        Assert.That(filters.Trials[0].CompressionLevel, Is.EqualTo(4));
        Assert.That(filters.Trials[0].FilterType, Is.EqualTo(PngFilterType.Paeth));
        Assert.That(encoder.Options, Has.Count.EqualTo(2));
        AssertMappedOptions(encoder.Options[0], options, PngRowFilterStrategy.Fast);
        AssertMappedOptions(encoder.Options[1], options, PngRowFilterStrategy.Fixed);
    }

    [Test]
    public void CompressionTrials_SelectFirstSmallestResultWhenSizesTie()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 2;
        options.MaximumCompressionLevel = 5;
        options.UseEarlyBailout = false;
        ScriptedEncoder encoder = new ScriptedEncoder(new int[] { 40, 20, 20, 30 });
        IPngOptimizer optimizer = new PngOptimizationService();

        PngCompressionTrialResult result = optimizer.RunCompressionTrialsWithEncoder(PngValidFixtures.CreateRgba8Image(1, 1), options, encoder);

        Assert.That(result.Trials, Has.Length.EqualTo(4));
        Assert.That(result.TriedCompressionLevels, Is.True);
        Assert.That(result.BestTrial, Is.SameAs(result.Trials[1]));
        Assert.That(result.BestTrial.CompressionLevel, Is.EqualTo(3));
        Assert.That(result.BestTrial.ByteCount, Is.EqualTo(20));
        Assert.That(result.Trials[1].IsBest, Is.True);
        Assert.That(result.Trials[2].IsBest, Is.False);
    }

    [Test]
    public void FilterTrials_SelectFirstSmallestResultWhenSizesTie()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 6;
        options.RowFilterStrategy = PngRowFilterStrategy.Exhaustive;
        options.UseEarlyBailout = false;
        ScriptedEncoder encoder = new ScriptedEncoder(new int[] { 40, 35, 20, 20, 30, 32, 34 });
        IPngOptimizer optimizer = new PngOptimizationService();

        PngFilterStrategyTrialResult result = optimizer.RunFilterTrialsWithEncoder(PngValidFixtures.CreateRgba8Image(1, 1), options, encoder);

        Assert.That(result.Trials, Has.Length.EqualTo(7));
        Assert.That(result.TriedFilterStrategies, Is.True);
        Assert.That(result.BestTrial, Is.SameAs(result.Trials[2]));
        Assert.That(result.BestTrial.RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Fixed));
        Assert.That(result.BestTrial.FilterType, Is.EqualTo(PngFilterType.None));
        Assert.That(result.BestTrial.UseFixedFilter, Is.True);
        Assert.That(result.Trials[2].IsBest, Is.True);
        Assert.That(result.Trials[3].IsBest, Is.False);
    }

    [Test]
    public void InjectedEncoder_ReceivesCurrentWinnerAsEarlyTerminationThreshold()
    {
        PngOptimizerOptions compressionOptions = new PngOptimizerOptions();
        compressionOptions.MinimumCompressionLevel = 0;
        compressionOptions.MaximumCompressionLevel = 2;
        ScriptedEncoder compressionEncoder = new ScriptedEncoder(new int[] { 10, 10, 10 });
        compressionEncoder.BailOutWhenThresholdSet = true;
        IPngOptimizer optimizer = new PngOptimizationService();

        PngCompressionTrialResult compression = optimizer.RunCompressionTrialsWithEncoder(PngValidFixtures.CreateRgba8Image(1, 1), compressionOptions, compressionEncoder);

        Assert.That(compression.BestTrial, Is.SameAs(compression.Trials[0]));
        Assert.That(compression.BailedOutTrialCount, Is.EqualTo(2));
        Assert.That(compressionEncoder.Options[0].EarlyBailoutByteCount, Is.EqualTo(0));
        Assert.That(compressionEncoder.Options[1].EarlyBailoutByteCount, Is.EqualTo(10));
        Assert.That(compressionEncoder.Options[2].EarlyBailoutByteCount, Is.EqualTo(10));
        AssertBailedOut(compression.Trials[1], 11);
        AssertBailedOut(compression.Trials[2], 11);

        PngOptimizerOptions filterOptions = new PngOptimizerOptions();
        filterOptions.MinimumCompressionLevel = 5;
        ScriptedEncoder filterEncoder = new ScriptedEncoder(new int[] { 12, 12, 12, 12, 12, 12, 12 });
        filterEncoder.BailOutWhenThresholdSet = true;

        PngFilterStrategyTrialResult filters = optimizer.RunFilterTrialsWithEncoder(PngValidFixtures.CreateRgba8Image(1, 1), filterOptions, filterEncoder);

        Assert.That(filters.BestTrial, Is.SameAs(filters.Trials[0]));
        Assert.That(filters.BailedOutTrialCount, Is.EqualTo(6));
        Assert.That(filterEncoder.Options[0].EarlyBailoutByteCount, Is.EqualTo(0));
        Assert.That(filterEncoder.Options[1].EarlyBailoutByteCount, Is.EqualTo(12));
        Assert.That(filterEncoder.Options[6].EarlyBailoutByteCount, Is.EqualTo(12));
        Assert.That(filters.Trials[6].WasBailedOut, Is.True);
        Assert.That(filters.Trials[6].ByteCount, Is.EqualTo(13));
    }

    [Test]
    public void Reports_ChooseExactWinnerKeepCompressionOnTieAndCopyPayload()
    {
        IPngOptimizer optimizer = new PngOptimizationService();
        PngOptimizerOptions compressionOptions = new PngOptimizerOptions();
        compressionOptions.MinimumCompressionLevel = 2;
        compressionOptions.MaximumCompressionLevel = 3;
        compressionOptions.UseEarlyBailout = false;
        PngCompressionTrialResult compression = optimizer.RunCompressionTrialsWithEncoder(
            PngValidFixtures.CreateRgba8Image(1, 1), compressionOptions, new ScriptedEncoder(new int[] { 30, 20 }));
        PngOptimizerOptions filterOptions = new PngOptimizerOptions();
        filterOptions.MinimumCompressionLevel = 3;
        filterOptions.UseEarlyBailout = false;
        PngFilterStrategyTrialResult tiedFilters = optimizer.RunFilterTrialsWithEncoder(
            PngValidFixtures.CreateRgba8Image(1, 1), filterOptions, new ScriptedEncoder(new int[] { 20, 25, 26, 27, 28, 29, 30 }));

        PngOptimizerReport tied = optimizer.CreateReportFromByteCount(100, compression, tiedFilters);
        byte retainedByte = tied.BestEncodedBytes[0];
        compression.BestTrial.EncodedBytes[0] = 99;

        Assert.That(tied.FoundBestResult, Is.True);
        Assert.That(tied.BestFromCompressionTrial, Is.True);
        Assert.That(tied.BestFromFilterStrategyTrial, Is.False);
        Assert.That(tied.BestCompressionLevel, Is.EqualTo(3));
        Assert.That(tied.BestByteCount, Is.EqualTo(20));
        Assert.That(tied.ByteSavings, Is.EqualTo(80));
        Assert.That(tied.CompressionTrialCount, Is.EqualTo(2));
        Assert.That(tied.FilterStrategyTrialCount, Is.EqualTo(7));
        Assert.That(tied.BestEncodedBytes[0], Is.EqualTo(retainedByte));

        PngFilterStrategyTrialResult smallerFilters = optimizer.RunFilterTrialsWithEncoder(
            PngValidFixtures.CreateRgba8Image(1, 1), filterOptions, new ScriptedEncoder(new int[] { 30, 19, 25, 26, 27, 28, 29 }));
        PngOptimizerReport filterWinner = optimizer.CreateReportFromBytes(new byte[100], compression, smallerFilters);

        Assert.That(filterWinner.BestFromCompressionTrial, Is.False);
        Assert.That(filterWinner.BestFromFilterStrategyTrial, Is.True);
        Assert.That(filterWinner.BestCompressionLevel, Is.EqualTo(3));
        Assert.That(filterWinner.BestRowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Fast));
        Assert.That(filterWinner.BestFilterType, Is.EqualTo(PngFilterType.None));
        Assert.That(filterWinner.BestUsesFixedFilter, Is.False);
        Assert.That(filterWinner.BestByteCount, Is.EqualTo(19));
        Assert.That(filterWinner.ByteSavings, Is.EqualTo(81));
    }

    [Test]
    public void MetadataOptions_ControlSelectedTextAndUnknownChunksInTrialOutput()
    {
        PngImage image = PngValidFixtures.CreateRgba8Image(1, 1);
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 84, 105, 116, 108, 101, 0, 86, 97, 108, 117, 101 }),
            CreateChunk("vpAg", new byte[] { 1 })
        };
        PngUnknownChunkReplacement replacement = new PngUnknownChunkReplacement();
        replacement.Type = "vpAg";
        replacement.Data = new byte[] { 9, 8 };
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 9;
        options.MaximumCompressionLevel = 9;
        options.TryCompressionLevels = false;
        options.TryFilters = false;
        options.UseEarlyBailout = false;
        options.MetadataPolicy = PngMetadataPolicy.RemoveSelected;
        options.MetadataChunkTypesToRemove = new string[] { PngConstants.ChunkPhysicalPixels };
        options.TextCompressionMode = PngTextCompressionMode.Compressed;
        options.UnknownChunkDisposition = PngChunkDisposition.Replace;
        options.UnknownChunkReplacements = new PngUnknownChunkReplacement[] { replacement };
        IPngOptimizer optimizer = new PngOptimizationService();

        PngCompressionTrialResult result = optimizer.RunCompressionTrialsWithOptions(image, options);
        PngChunk[] chunks = Png.ReadChunks(result.BestTrial.EncodedBytes).Chunks;

        Assert.That(Png.ValidateBytes(result.BestTrial.EncodedBytes).IsValid, Is.True);
        Assert.That(HasChunk(chunks, PngConstants.ChunkGamma), Is.True);
        Assert.That(HasChunk(chunks, PngConstants.ChunkPhysicalPixels), Is.False);
        Assert.That(HasChunk(chunks, PngConstants.ChunkText), Is.False);
        Assert.That(HasChunk(chunks, PngConstants.ChunkCompressedText), Is.True);
        Assert.That(FindChunk(chunks, "vpAg")!.Data, Is.EqualTo(new byte[] { 9, 8 }));
        Assert.That(Png.DecodeBytes(result.BestTrial.EncodedBytes).Pixels.Data, Is.EqualTo(image.Pixels.Data));
    }

    [Test]
    public void PublicOptimizerMethods_RejectInvalidArguments()
    {
        IPngOptimizer optimizer = new PngOptimizationService();
        PngImage image = PngValidFixtures.CreateRgba8Image(1, 1);
        PngOptimizerOptions options = new PngOptimizerOptions();
        ScriptedEncoder encoder = new ScriptedEncoder(new int[] { 10 });
        PngCompressionTrialResult compression = new PngCompressionTrialResult();
        PngFilterStrategyTrialResult filters = new PngFilterStrategyTrialResult();

        Assert.Throws<ArgumentNullException>(() => optimizer.RunCompressionTrialsWithEncoder(null!, options, encoder));
        Assert.Throws<ArgumentNullException>(() => optimizer.RunCompressionTrialsWithEncoder(image, null!, encoder));
        Assert.Throws<ArgumentNullException>(() => optimizer.RunCompressionTrialsWithEncoder(image, options, null!));
        Assert.Throws<ArgumentNullException>(() => optimizer.RunFilterTrialsWithEncoder(null!, options, encoder));
        Assert.Throws<ArgumentNullException>(() => optimizer.RunFilterTrialsWithEncoder(image, null!, encoder));
        Assert.Throws<ArgumentNullException>(() => optimizer.RunFilterTrialsWithEncoder(image, options, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => optimizer.CreateReportFromByteCount(-1, compression, filters));
        Assert.Throws<ArgumentNullException>(() => optimizer.CreateReportFromByteCount(0, null!, filters));
        Assert.Throws<ArgumentNullException>(() => optimizer.CreateReportFromByteCount(0, compression, null!));
        Assert.Throws<ArgumentNullException>(() => optimizer.CreateReportFromBytes(null!, compression, filters));
    }

    static void AssertMappedOptions(PngEncoderOptions actual, PngOptimizerOptions expected, PngRowFilterStrategy expectedRowFilterStrategy)
    {
        Assert.That(actual.Limits, Is.SameAs(expected.Limits));
        Assert.That(actual.MetadataPolicy, Is.EqualTo(expected.MetadataPolicy));
        Assert.That(actual.TextCompressionMode, Is.EqualTo(expected.TextCompressionMode));
        Assert.That(actual.TextCompressionThreshold, Is.EqualTo(expected.TextCompressionThreshold));
        Assert.That(actual.UnknownChunkDisposition, Is.EqualTo(expected.UnknownChunkDisposition));
        Assert.That(actual.MetadataChunkTypesToKeep, Is.SameAs(expected.MetadataChunkTypesToKeep));
        Assert.That(actual.MetadataChunkTypesToRemove, Is.SameAs(expected.MetadataChunkTypesToRemove));
        Assert.That(actual.UnknownChunkReplacements, Is.SameAs(expected.UnknownChunkReplacements));
        Assert.That(actual.PixelDataChanged, Is.EqualTo(expected.PixelDataChanged));
        Assert.That(actual.FixedFilterType, Is.EqualTo(expected.FixedFilterType));
        Assert.That(actual.RowFilterStrategy, Is.EqualTo(expectedRowFilterStrategy));
        Assert.That(actual.UseFixedFilter, Is.True);
        Assert.That(actual.CompressionLevel, Is.EqualTo(expected.MinimumCompressionLevel));
        Assert.That(actual.UseAutomaticColorType, Is.EqualTo(expected.AllowColorTypeReduction));
        Assert.That(actual.UseAutomaticBitDepth, Is.EqualTo(expected.AllowBitDepthReduction));
        Assert.That(actual.AllowPaletteReduction, Is.EqualTo(expected.AllowPaletteReduction));
        Assert.That(actual.EarlyBailoutByteCount, Is.EqualTo(expected.BestKnownByteCount));
    }

    static void AssertBailedOut(PngCompressionTrial trial, long byteCount)
    {
        Assert.That(trial.WasBailedOut, Is.True);
        Assert.That(trial.EncodedBytes, Is.Empty);
        Assert.That(trial.ByteCount, Is.EqualTo(byteCount));
        Assert.That(trial.IsBest, Is.False);
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        return chunk;
    }

    static bool HasChunk(PngChunk[] chunks, string type)
    {
        return FindChunk(chunks, type) != null;
    }

    static PngChunk? FindChunk(PngChunk[] chunks, string type)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                return chunks[index];
            }

            index += 1;
        }

        return null;
    }

    class ScriptedEncoder : IPngEncoder
    {
        readonly int[] lengths;

        public List<PngEncoderOptions> Options = new List<PngEncoderOptions>();
        public bool BailOutWhenThresholdSet;

        public ScriptedEncoder(int[] lengths)
        {
            this.lengths = lengths;
        }

        public byte[] EncodeToBytes(PngImage image, PngEncoderOptions options)
        {
            int callIndex = Options.Count;
            Options.Add(options);
            if (BailOutWhenThresholdSet && options.EarlyBailoutByteCount > 0)
            {
                options.WasEarlyBailedOut = true;
                options.ProducedByteCount = options.EarlyBailoutByteCount + 1;
                return new byte[0];
            }

            byte[] result = new byte[lengths[callIndex]];
            int index = 0;
            while (index < result.Length)
            {
                result[index] = (byte)(callIndex + 1);
                index += 1;
            }

            return result;
        }

        public void EncodeToStream(Stream stream, PngImage image, PngEncoderOptions options)
        {
            byte[] data = EncodeToBytes(image, options);
            stream.Write(data, 0, data.Length);
        }
    }
}
