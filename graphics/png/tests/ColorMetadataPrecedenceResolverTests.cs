using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ColorMetadataPrecedenceResolverTests
{
    [Test]
    public void Resolve_ReturnsEmptyMetadataWhenNoColorChunksExist()
    {
        PngColorMetadata metadata = PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        });

        Assert.That(metadata.HasColorMetadata, Is.False);
        Assert.That(metadata.SourceChunkType, Is.EqualTo(""));
        Assert.That(metadata.Precedence, Is.EqualTo(PngColorMetadataPrecedenceResolver.NoColorMetadataPrecedence));
        Assert.That(metadata.HasCodingIndependentCodePoints, Is.False);
        Assert.That(metadata.HasIccProfile, Is.False);
        Assert.That(metadata.HasStandardRgb, Is.False);
        Assert.That(metadata.HasChromaticities, Is.False);
        Assert.That(metadata.HasGamma, Is.False);
    }

    [Test]
    public void Resolve_FromMetadataUsesContainedChunks()
    {
        PngMetadata input = new PngMetadata();
        input.Chunks = new PngChunk[]
        {
            CreateGammaChunk()
        };

        PngColorMetadata metadata = PngColorMetadataPrecedenceResolver.Resolve(input);

        Assert.That(metadata.SourceChunkType, Is.EqualTo(PngConstants.ChunkGamma));
        Assert.That(metadata.HasGamma, Is.True);
        Assert.That(metadata.Gamma.Value, Is.EqualTo(45455));
    }

    [Test]
    public void Resolve_FromMetadataWithLimitsUsesHighestPriorityContainedChunk()
    {
        PngMetadata input = new PngMetadata();
        input.Chunks = new PngChunk[]
        {
            CreateIccProfileChunk(),
            CreateSrgbChunk(),
            CreateCicpChunk()
        };
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 16;

        PngColorMetadata metadata = PngColorMetadataPrecedenceResolver.Resolve(input, limits);

        Assert.That(metadata.SourceChunkType, Is.EqualTo(PngConstants.ChunkCodingIndependentCodePoints));
        Assert.That(metadata.Precedence, Is.EqualTo(PngColorMetadataPrecedenceResolver.CodingIndependentCodePointsPrecedence));
        Assert.That(metadata.HasCodingIndependentCodePoints, Is.True);
        Assert.That(metadata.HasIccProfile, Is.False);
        Assert.That(metadata.HasStandardRgb, Is.False);
    }

    [Test]
    public void Resolve_SelectsCicpOverLowerPriorityChunks()
    {
        PngColorMetadata metadata = PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIccProfile, new byte[] { 0 }),
            CreateSrgbChunk(),
            CreateGammaChunk(),
            CreateCicpChunk()
        });

        Assert.That(metadata.HasColorMetadata, Is.True);
        Assert.That(metadata.SourceChunkType, Is.EqualTo(PngConstants.ChunkCodingIndependentCodePoints));
        Assert.That(metadata.Precedence, Is.EqualTo(PngColorMetadataPrecedenceResolver.CodingIndependentCodePointsPrecedence));
        Assert.That(metadata.HasCodingIndependentCodePoints, Is.True);
        Assert.That(metadata.CodingIndependentCodePoints.ColorPrimaries, Is.EqualTo(9));
        Assert.That(metadata.CodingIndependentCodePoints.TransferCharacteristics, Is.EqualTo(16));
        Assert.That(metadata.CodingIndependentCodePoints.MatrixCoefficients, Is.EqualTo(0));
        Assert.That(metadata.CodingIndependentCodePoints.VideoFullRangeFlag, Is.EqualTo(1));
        Assert.That(metadata.HasIccProfile, Is.False);
        Assert.That(metadata.HasStandardRgb, Is.False);
        Assert.That(metadata.HasGamma, Is.False);
    }

    [Test]
    public void Resolve_UsesFirstChunkWithinSelectedPrecedenceTier()
    {
        PngColorMetadata cicp = PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[]
        {
            CreateCicpChunk(9),
            CreateCicpChunk(1),
            CreateIccProfileChunk()
        });
        PngColorMetadata srgb = PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[]
        {
            CreateSrgbChunk(1),
            CreateSrgbChunk(3),
            CreateGammaChunk()
        });

        Assert.That(cicp.SourceChunkType, Is.EqualTo(PngConstants.ChunkCodingIndependentCodePoints));
        Assert.That(cicp.CodingIndependentCodePoints.ColorPrimaries, Is.EqualTo(9));
        Assert.That(srgb.SourceChunkType, Is.EqualTo(PngConstants.ChunkStandardRgb));
        Assert.That(srgb.StandardRgb.RenderingIntent, Is.EqualTo(1));
    }

    [Test]
    public void Resolve_UsesFirstChromaticitiesAndFirstGammaInSharedTier()
    {
        PngColorMetadata metadata = PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[]
        {
            CreateGammaChunk(45455),
            CreateChromaticitiesChunk(31270),
            CreateGammaChunk(100000),
            CreateChromaticitiesChunk(1000)
        });

        Assert.That(metadata.SourceChunkType, Is.EqualTo(PngColorMetadataPrecedenceResolver.ChromaticitiesGammaSource));
        Assert.That(metadata.HasChromaticities, Is.True);
        Assert.That(metadata.Chromaticities.WhitePointX, Is.EqualTo(31270));
        Assert.That(metadata.HasGamma, Is.True);
        Assert.That(metadata.Gamma.Value, Is.EqualTo(45455));
    }

    [Test]
    public void Resolve_SelectsIccpOverSrgbAndChromaticitiesGamma()
    {
        PngColorMetadata metadata = PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[]
        {
            CreateSrgbChunk(),
            CreateChromaticitiesChunk(),
            CreateGammaChunk(),
            CreateIccProfileChunk()
        });

        Assert.That(metadata.SourceChunkType, Is.EqualTo(PngConstants.ChunkIccProfile));
        Assert.That(metadata.Precedence, Is.EqualTo(PngColorMetadataPrecedenceResolver.IccProfilePrecedence));
        Assert.That(metadata.HasIccProfile, Is.True);
        Assert.That(metadata.IccProfile.Name, Is.EqualTo("Test"));
        Assert.That(metadata.IccProfile.Profile, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(metadata.HasStandardRgb, Is.False);
        Assert.That(metadata.HasChromaticities, Is.False);
        Assert.That(metadata.HasGamma, Is.False);
    }

    [Test]
    public void Resolve_WithChunkArrayLimitsAppliesLimitToSelectedIccProfile()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateSrgbChunk(),
            CreateIccProfileChunk()
        };
        PngSecurityLimits exactLimit = new PngSecurityLimits();
        exactLimit.MaxDecompressedBytes = 4;
        PngSecurityLimits tooSmallLimit = new PngSecurityLimits();
        tooSmallLimit.MaxDecompressedBytes = 3;

        PngColorMetadata metadata = PngColorMetadataPrecedenceResolver.Resolve(chunks, exactLimit);

        Assert.That(metadata.SourceChunkType, Is.EqualTo(PngConstants.ChunkIccProfile));
        Assert.That(metadata.HasIccProfile, Is.True);
        Assert.That(metadata.IccProfile.Profile, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.Throws<OverflowException>(() => PngColorMetadataPrecedenceResolver.Resolve(chunks, tooSmallLimit));
    }

    [Test]
    public void Resolve_SelectsSrgbOverChromaticitiesGamma()
    {
        PngColorMetadata metadata = PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[]
        {
            CreateChromaticitiesChunk(),
            CreateGammaChunk(),
            CreateSrgbChunk()
        });

        Assert.That(metadata.SourceChunkType, Is.EqualTo(PngConstants.ChunkStandardRgb));
        Assert.That(metadata.Precedence, Is.EqualTo(PngColorMetadataPrecedenceResolver.StandardRgbPrecedence));
        Assert.That(metadata.HasStandardRgb, Is.True);
        Assert.That(metadata.StandardRgb.RenderingIntent, Is.EqualTo(2));
        Assert.That(metadata.HasChromaticities, Is.False);
        Assert.That(metadata.HasGamma, Is.False);
    }

    [Test]
    public void Resolve_UsesChromaticitiesAndGammaAsSharedLowestTier()
    {
        PngColorMetadata metadata = PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[]
        {
            CreateGammaChunk(),
            CreateChromaticitiesChunk()
        });

        Assert.That(metadata.SourceChunkType, Is.EqualTo(PngColorMetadataPrecedenceResolver.ChromaticitiesGammaSource));
        Assert.That(metadata.Precedence, Is.EqualTo(PngColorMetadataPrecedenceResolver.ChromaticitiesGammaPrecedence));
        Assert.That(metadata.HasChromaticities, Is.True);
        Assert.That(metadata.Chromaticities.WhitePointX, Is.EqualTo(31270));
        Assert.That(metadata.HasGamma, Is.True);
        Assert.That(metadata.Gamma.Value, Is.EqualTo(45455));
    }

    [Test]
    public void Resolve_UsesSingleChromaticitiesGammaTierChunkWhenOnlyOneExists()
    {
        PngColorMetadata chromaticities = PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[]
        {
            CreateChromaticitiesChunk()
        });
        PngColorMetadata gamma = PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[]
        {
            CreateGammaChunk()
        });

        Assert.That(chromaticities.SourceChunkType, Is.EqualTo(PngConstants.ChunkChromaticities));
        Assert.That(chromaticities.HasChromaticities, Is.True);
        Assert.That(chromaticities.HasGamma, Is.False);
        Assert.That(gamma.SourceChunkType, Is.EqualTo(PngConstants.ChunkGamma));
        Assert.That(gamma.HasChromaticities, Is.False);
        Assert.That(gamma.HasGamma, Is.True);
    }

    [Test]
    public void UtilityMethods_ClassifyColorMetadataChunks()
    {
        Assert.That(PngColorMetadataPrecedenceResolver.IsColorMetadataChunk(PngConstants.ChunkCodingIndependentCodePoints), Is.True);
        Assert.That(PngColorMetadataPrecedenceResolver.IsColorMetadataChunk(PngConstants.ChunkIccProfile), Is.True);
        Assert.That(PngColorMetadataPrecedenceResolver.IsColorMetadataChunk(PngConstants.ChunkStandardRgb), Is.True);
        Assert.That(PngColorMetadataPrecedenceResolver.IsColorMetadataChunk(PngConstants.ChunkChromaticities), Is.True);
        Assert.That(PngColorMetadataPrecedenceResolver.IsColorMetadataChunk(PngConstants.ChunkGamma), Is.True);
        Assert.That(PngColorMetadataPrecedenceResolver.IsColorMetadataChunk(PngConstants.ChunkContentLightLevelInformation), Is.False);
        Assert.That(PngColorMetadataPrecedenceResolver.GetPrecedence(PngConstants.ChunkCodingIndependentCodePoints), Is.EqualTo(1));
        Assert.That(PngColorMetadataPrecedenceResolver.GetPrecedence(PngConstants.ChunkIccProfile), Is.EqualTo(2));
        Assert.That(PngColorMetadataPrecedenceResolver.GetPrecedence(PngConstants.ChunkStandardRgb), Is.EqualTo(3));
        Assert.That(PngColorMetadataPrecedenceResolver.GetPrecedence(PngConstants.ChunkChromaticities), Is.EqualTo(4));
        Assert.That(PngColorMetadataPrecedenceResolver.GetPrecedence(PngConstants.ChunkGamma), Is.EqualTo(4));
        Assert.That(PngColorMetadataPrecedenceResolver.GetPrecedence(PngConstants.ChunkIEND), Is.EqualTo(0));
    }

    [Test]
    public void UtilityMethods_RepresentMissingChunksWithNullableReferences()
    {
        PngChunk gamma = CreateGammaChunk();
        PngChunk? missing = PngColorMetadataPrecedenceResolver.FindFirstChunk(
            new PngChunk[] { gamma },
            PngConstants.ChunkChromaticities);

        Assert.That(missing, Is.Null);
        Assert.That(
            PngColorMetadataPrecedenceResolver.GetChromaticitiesGammaSource(null, gamma),
            Is.EqualTo(PngConstants.ChunkGamma));
        Assert.Throws<ArgumentException>(() =>
            PngColorMetadataPrecedenceResolver.GetChromaticitiesGammaSource(null, null));
    }

    [Test]
    public void Resolve_RejectsInvalidInputs()
    {
        PngMetadata nullChunks = new PngMetadata();
        nullChunks.Chunks = null!;
        PngChunk nullType = new PngChunk();
        nullType.Type = null!;
        nullType.Data = new byte[0];

        Assert.Throws<ArgumentNullException>(() => PngColorMetadataPrecedenceResolver.Resolve((PngChunk[])null!));
        Assert.Throws<ArgumentNullException>(() => PngColorMetadataPrecedenceResolver.Resolve((PngMetadata)null!));
        Assert.Throws<ArgumentNullException>(() => PngColorMetadataPrecedenceResolver.Resolve(nullChunks));
        Assert.Throws<ArgumentNullException>(() => PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[0], null!));
        Assert.Throws<ArgumentNullException>(() => PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[] { null! }));
        Assert.Throws<ArgumentNullException>(() => PngColorMetadataPrecedenceResolver.Resolve(new PngChunk[] { nullType }));
        Assert.Throws<ArgumentNullException>(() => PngColorMetadataPrecedenceResolver.IsColorMetadataChunk(null!));
        Assert.Throws<ArgumentNullException>(() => PngColorMetadataPrecedenceResolver.GetPrecedence(null!));
    }

    static PngChunk CreateCicpChunk()
    {
        return CreateCicpChunk(9);
    }

    static PngChunk CreateCicpChunk(byte colorPrimaries)
    {
        PngCodingIndependentCodePoints codePoints = new PngCodingIndependentCodePoints();
        codePoints.ColorPrimaries = colorPrimaries;
        codePoints.TransferCharacteristics = 16;
        codePoints.MatrixCoefficients = 0;
        codePoints.VideoFullRangeFlag = 1;
        return PngCodingIndependentCodePointsWriter.WriteChunk(codePoints);
    }

    static PngChunk CreateIccProfileChunk()
    {
        PngIccProfile profile = new PngIccProfile();
        profile.Name = "Test";
        profile.CompressionMethod = PngIccProfileParser.CompressionMethodDeflate;
        profile.Profile = new byte[] { 1, 2, 3, 4 };
        return PngIccProfileWriter.WriteChunk(profile);
    }

    static PngChunk CreateSrgbChunk()
    {
        return CreateSrgbChunk(2);
    }

    static PngChunk CreateSrgbChunk(byte renderingIntent)
    {
        PngStandardRgb standardRgb = new PngStandardRgb();
        standardRgb.RenderingIntent = renderingIntent;
        return PngStandardRgbWriter.WriteChunk(standardRgb);
    }

    static PngChunk CreateGammaChunk()
    {
        return CreateGammaChunk(45455);
    }

    static PngChunk CreateGammaChunk(uint gammaValue)
    {
        PngGamma gamma = new PngGamma();
        gamma.Value = gammaValue;
        return PngGammaWriter.WriteChunk(gamma);
    }

    static PngChunk CreateChromaticitiesChunk()
    {
        return CreateChromaticitiesChunk(31270);
    }

    static PngChunk CreateChromaticitiesChunk(uint whitePointX)
    {
        PngChromaticities chromaticities = new PngChromaticities();
        chromaticities.WhitePointX = whitePointX;
        chromaticities.WhitePointY = 32900;
        chromaticities.RedX = 64000;
        chromaticities.RedY = 33000;
        chromaticities.GreenX = 30000;
        chromaticities.GreenY = 60000;
        chromaticities.BlueX = 15000;
        chromaticities.BlueY = 6000;
        return PngChromaticitiesWriter.WriteChunk(chromaticities);
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        chunk.IsKnown = true;
        return chunk;
    }
}
