using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class MetadataPolicySelectionTests
{
    [Test]
    public void KeepSelected_PreservesOnlyExplicitMetadataTypes()
    {
        PngImage image = CreateImageWithMetadata();
        PngEncoderOptions options = CreateOptions();
        options.MetadataPolicy = PngMetadataPolicy.KeepSelected;
        options.MetadataChunkTypesToKeep = new string[]
        {
            PngConstants.ChunkGamma,
            PngConstants.ChunkText
        };

        PngChunk[] chunks = Png.ReadChunks(Png.EncodeToBytes(image, options)).Chunks;

        Assert.That(HasChunk(chunks, PngConstants.ChunkGamma), Is.True);
        Assert.That(HasChunk(chunks, PngConstants.ChunkText), Is.True);
        Assert.That(HasChunk(chunks, PngConstants.ChunkPhysicalPixels), Is.False);
        Assert.That(HasChunk(chunks, "vpAg"), Is.False);
    }

    [Test]
    public void RemoveSelected_PreservesEverythingExceptExplicitMetadataTypes()
    {
        PngImage image = CreateImageWithMetadata();
        PngEncoderOptions options = CreateOptions();
        options.MetadataPolicy = PngMetadataPolicy.RemoveSelected;
        options.MetadataChunkTypesToRemove = new string[]
        {
            PngConstants.ChunkPhysicalPixels,
            PngConstants.ChunkText
        };

        PngChunk[] chunks = Png.ReadChunks(Png.EncodeToBytes(image, options)).Chunks;

        Assert.That(HasChunk(chunks, PngConstants.ChunkGamma), Is.True);
        Assert.That(HasChunk(chunks, PngConstants.ChunkPhysicalPixels), Is.False);
        Assert.That(HasChunk(chunks, PngConstants.ChunkText), Is.False);
        Assert.That(HasChunk(chunks, "vpAg"), Is.True);
    }

    [Test]
    public void PreserveUnknownChunks_EnforcesSafeToCopyAfterPixelChanges()
    {
        PngImage image = PngValidFixtures.CreateRgba8Image(1, 1);
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk("vpAg", new byte[] { 1 }),
            CreateChunk("vpAG", new byte[] { 2 })
        };
        PngEncoderOptions changedOptions = CreateOptions();
        PngEncoderOptions unchangedOptions = CreateOptions();
        unchangedOptions.PixelDataChanged = false;

        PngChunk[] changedChunks = Png.ReadChunks(Png.EncodeToBytes(image, changedOptions)).Chunks;
        PngChunk[] unchangedChunks = Png.ReadChunks(Png.EncodeToBytes(image, unchangedOptions)).Chunks;

        Assert.That(HasChunk(changedChunks, "vpAg"), Is.True);
        Assert.That(HasChunk(changedChunks, "vpAG"), Is.False);
        Assert.That(HasChunk(unchangedChunks, "vpAg"), Is.True);
        Assert.That(HasChunk(unchangedChunks, "vpAG"), Is.True);
    }

    [Test]
    public void ReplaceUnknownChunks_UsesConfiguredPayloadsAndDropsUnconfiguredTypes()
    {
        PngImage image = PngValidFixtures.CreateRgba8Image(1, 1);
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk("vpAg", new byte[] { 1 }),
            CreateChunk("vpAG", new byte[] { 2 }),
            CreateChunk("vpBg", new byte[] { 3 })
        };
        PngUnknownChunkReplacement safeReplacement = CreateReplacement("vpAg", new byte[] { 9, 8 });
        PngUnknownChunkReplacement unsafeReplacement = CreateReplacement("vpAG", new byte[] { 7, 6 });
        PngEncoderOptions options = CreateOptions();
        options.UnknownChunkDisposition = PngChunkDisposition.Replace;
        options.UnknownChunkReplacements = new PngUnknownChunkReplacement[]
        {
            safeReplacement,
            unsafeReplacement
        };

        PngChunk[] chunks = Png.ReadChunks(Png.EncodeToBytes(image, options)).Chunks;

        Assert.That(FindChunk(chunks, "vpAg")!.Data, Is.EqualTo(new byte[] { 9, 8 }));
        Assert.That(FindChunk(chunks, "vpAG")!.Data, Is.EqualTo(new byte[] { 7, 6 }));
        Assert.That(HasChunk(chunks, "vpBg"), Is.False);
        safeReplacement.Data[0] = 0;
        Assert.That(FindChunk(chunks, "vpAg")!.Data[0], Is.EqualTo(9));
    }

    [Test]
    public void MetadataPolicyOptions_RejectInvalidSelectionsAndReplacements()
    {
        PngImage image = PngValidFixtures.CreateRgba8Image(1, 1);
        PngEncoderOptions nullKeep = CreateOptions();
        nullKeep.MetadataChunkTypesToKeep = null!;
        PngEncoderOptions invalidSelection = CreateOptions();
        invalidSelection.MetadataChunkTypesToKeep = new string[] { "abc" };
        PngEncoderOptions criticalSelection = CreateOptions();
        criticalSelection.MetadataChunkTypesToKeep = new string[] { PngConstants.ChunkIDAT };
        PngEncoderOptions nullReplacements = CreateOptions();
        nullReplacements.UnknownChunkReplacements = null!;
        PngEncoderOptions knownReplacement = CreateOptions();
        knownReplacement.UnknownChunkReplacements = new PngUnknownChunkReplacement[]
        {
            CreateReplacement(PngConstants.ChunkGamma, new byte[] { 1 })
        };
        PngEncoderOptions duplicateReplacement = CreateOptions();
        duplicateReplacement.UnknownChunkReplacements = new PngUnknownChunkReplacement[]
        {
            CreateReplacement("vpAg", new byte[] { 1 }),
            CreateReplacement("vpAg", new byte[] { 2 })
        };
        PngEncoderOptions invalidPolicy = CreateOptions();
        invalidPolicy.MetadataPolicy = (PngMetadataPolicy)99;
        PngEncoderOptions invalidTextMode = CreateOptions();
        invalidTextMode.TextCompressionMode = (PngTextCompressionMode)99;
        PngEncoderOptions invalidTextThreshold = CreateOptions();
        invalidTextThreshold.TextCompressionThreshold = -1;

        Assert.Throws<ArgumentNullException>(() => Png.EncodeToBytes(image, nullKeep));
        Assert.Throws<ArgumentException>(() => Png.EncodeToBytes(image, invalidSelection));
        Assert.Throws<ArgumentException>(() => Png.EncodeToBytes(image, criticalSelection));
        Assert.Throws<ArgumentNullException>(() => Png.EncodeToBytes(image, nullReplacements));
        Assert.Throws<ArgumentException>(() => Png.EncodeToBytes(image, knownReplacement));
        Assert.Throws<ArgumentException>(() => Png.EncodeToBytes(image, duplicateReplacement));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.EncodeToBytes(image, invalidPolicy));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.EncodeToBytes(image, invalidTextMode));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.EncodeToBytes(image, invalidTextThreshold));
    }

    [Test]
    public void OptimizerOptionMapping_PropagatesMetadataSelectionsAndReplacements()
    {
        PngOptimizerOptions optimizerOptions = new PngOptimizerOptions();
        optimizerOptions.MetadataPolicy = PngMetadataPolicy.KeepSelected;
        optimizerOptions.TextCompressionMode = PngTextCompressionMode.Automatic;
        optimizerOptions.TextCompressionThreshold = 24;
        optimizerOptions.UnknownChunkDisposition = PngChunkDisposition.Replace;
        optimizerOptions.MetadataChunkTypesToKeep = new string[] { PngConstants.ChunkGamma };
        optimizerOptions.MetadataChunkTypesToRemove = new string[] { PngConstants.ChunkText };
        optimizerOptions.UnknownChunkReplacements = new PngUnknownChunkReplacement[]
        {
            CreateReplacement("vpAg", new byte[] { 4 })
        };
        optimizerOptions.PixelDataChanged = false;

        PngEncoderOptions compressionOptions = PngCompressionLevelTrialRunner.CreateEncoderOptions(optimizerOptions, 5);
        PngEncoderOptions filterOptions = PngFilterStrategyTrialRunner.CreateEncoderOptions(optimizerOptions);

        AssertMappedOptions(compressionOptions);
        AssertMappedOptions(filterOptions);
    }

    [Test]
    public void UnknownChunkReplacement_DefaultsToEmptyFields()
    {
        PngUnknownChunkReplacement replacement = new PngUnknownChunkReplacement();

        Assert.That(replacement.Type, Is.EqualTo(""));
        Assert.That(replacement.Data, Is.Empty);
    }

    static void AssertMappedOptions(PngEncoderOptions options)
    {
        Assert.That(options.MetadataPolicy, Is.EqualTo(PngMetadataPolicy.KeepSelected));
        Assert.That(options.TextCompressionMode, Is.EqualTo(PngTextCompressionMode.Automatic));
        Assert.That(options.TextCompressionThreshold, Is.EqualTo(24));
        Assert.That(options.UnknownChunkDisposition, Is.EqualTo(PngChunkDisposition.Replace));
        Assert.That(options.MetadataChunkTypesToKeep, Is.EqualTo(new string[] { PngConstants.ChunkGamma }));
        Assert.That(options.MetadataChunkTypesToRemove, Is.EqualTo(new string[] { PngConstants.ChunkText }));
        Assert.That(options.UnknownChunkReplacements, Has.Length.EqualTo(1));
        Assert.That(options.UnknownChunkReplacements[0].Type, Is.EqualTo("vpAg"));
        Assert.That(options.PixelDataChanged, Is.False);
    }

    static PngImage CreateImageWithMetadata()
    {
        PngImage image = PngValidFixtures.CreateRgba8Image(1, 1);
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 14, 196, 0, 0, 14, 196, 1 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 }),
            CreateChunk("vpAg", new byte[] { 5 })
        };
        return image;
    }

    static PngEncoderOptions CreateOptions()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Eight;
        return options;
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        chunk.IsKnown = PngKnownChunkTypes.IsKnown(type);
        chunk.IsCritical = !PngChunkTypeFlags.IsAncillary(type);
        chunk.IsPublic = PngChunkTypeFlags.IsPublic(type);
        chunk.IsReservedBitValid = PngChunkTypeFlags.IsReservedBitValid(type);
        chunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(type);
        return chunk;
    }

    static PngUnknownChunkReplacement CreateReplacement(string type, byte[] data)
    {
        PngUnknownChunkReplacement replacement = new PngUnknownChunkReplacement();
        replacement.Type = type;
        replacement.Data = data;
        return replacement;
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
}
