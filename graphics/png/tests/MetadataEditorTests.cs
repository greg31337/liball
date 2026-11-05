using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class MetadataEditorTests
{
    [Test]
    public void SetIccProfile_ReplacesExistingIccProfileChunks()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngCodingIndependentCodePointsWriter.WriteChunk(CreateCodePoints(9, 16, 9, 1)),
            PngIccProfileWriter.WriteChunk(CreateIccProfile("Old", new byte[] { 1, 2, 3 })),
            PngTextWriter.WriteChunk(CreateText("Title", "Keep")),
            PngIccProfileWriter.WriteChunk(CreateIccProfile("Older", new byte[] { 4, 5, 6 })),
            CreateChunk("vpAg", new byte[] { 7, 8 }, false)
        }, new PngChunk[]
        {
            PngIccProfileWriter.WriteChunk(CreateIccProfile("Unknown", new byte[] { 9, 10 })),
            CreateChunk("vpAh", new byte[] { 11, 12 }, false)
        });

        PngMetadata result = PngMetadataEditor.SetIccProfile(metadata, CreateIccProfile("Display", CreateProfileBytes()));

        AssertChunkTypes(result.Chunks, PngConstants.ChunkCodingIndependentCodePoints, PngConstants.ChunkIccProfile, PngConstants.ChunkText, "vpAg");
        AssertChunkTypes(result.UnknownChunks, "vpAh");
        AssertIccProfile(PngIccProfileParser.Parse(result.Chunks[1]), "Display", CreateProfileBytes());
        AssertCodePoints(PngCodingIndependentCodePointsParser.Parse(result.Chunks[0]), 9, 16, 9, 1);
        Assert.That(result.MetadataBytes, Is.EqualTo(CountMetadataBytes(result.Chunks)));
        Assert.That(result.TextBytes, Is.EqualTo(CountTextBytes(result.Chunks)));
        Assert.That(result.TextEntryCount, Is.EqualTo(1));
    }

    [Test]
    public void SetIccProfile_AppendsIccProfileChunkWhenAbsent()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Title", "Keep"))
        }, new PngChunk[0]);

        PngMetadata result = PngMetadataEditor.SetIccProfile(metadata, CreateIccProfile("Display", CreateProfileBytes()));

        AssertChunkTypes(result.Chunks, PngConstants.ChunkText, PngConstants.ChunkIccProfile);
        AssertIccProfile(PngIccProfileParser.Parse(result.Chunks[1]), "Display", CreateProfileBytes());
    }

    [Test]
    public void SetCodingIndependentCodePoints_ReplacesExistingCicpChunks()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngIccProfileWriter.WriteChunk(CreateIccProfile("Display", CreateProfileBytes())),
            PngCodingIndependentCodePointsWriter.WriteChunk(CreateCodePoints(1, 2, 3, 0)),
            PngTextWriter.WriteChunk(CreateText("Title", "Keep")),
            PngCodingIndependentCodePointsWriter.WriteChunk(CreateCodePoints(4, 5, 6, 1)),
            CreateChunk("vpAg", new byte[] { 7, 8 }, false)
        }, new PngChunk[]
        {
            PngCodingIndependentCodePointsWriter.WriteChunk(CreateCodePoints(7, 8, 9, 0)),
            CreateChunk("vpAh", new byte[] { 10, 11 }, false)
        });

        PngMetadata result = PngMetadataEditor.SetCodingIndependentCodePoints(metadata, CreateCodePoints(9, 16, 9, 1));

        AssertChunkTypes(result.Chunks, PngConstants.ChunkIccProfile, PngConstants.ChunkCodingIndependentCodePoints, PngConstants.ChunkText, "vpAg");
        AssertChunkTypes(result.UnknownChunks, "vpAh");
        AssertCodePoints(PngCodingIndependentCodePointsParser.Parse(result.Chunks[1]), 9, 16, 9, 1);
        AssertIccProfile(PngIccProfileParser.Parse(result.Chunks[0]), "Display", CreateProfileBytes());
        Assert.That(result.MetadataBytes, Is.EqualTo(CountMetadataBytes(result.Chunks)));
        Assert.That(result.TextEntryCount, Is.EqualTo(1));
    }

    [Test]
    public void SetCodingIndependentCodePoints_AppendsCicpChunkWhenAbsent()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngIccProfileWriter.WriteChunk(CreateIccProfile("Display", CreateProfileBytes()))
        }, new PngChunk[0]);

        PngMetadata result = PngMetadataEditor.SetCodingIndependentCodePoints(metadata, 9, 16, 9, 1);

        AssertChunkTypes(result.Chunks, PngConstants.ChunkIccProfile, PngConstants.ChunkCodingIndependentCodePoints);
        AssertCodePoints(PngCodingIndependentCodePointsParser.Parse(result.Chunks[1]), 9, 16, 9, 1);
    }

    [Test]
    public void SetIccProfileAndCicp_ReturnIndependentMetadataCopies()
    {
        byte[] profileBytes = CreateProfileBytes();
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Title", "Keep"))
        }, new PngChunk[]
        {
            CreateChunk("vpAg", new byte[] { 5, 6 }, false)
        });
        metadata.Palette = CreatePalette(1);

        PngMetadata result = PngMetadataEditor.SetIccProfile(metadata, CreateIccProfile("Display", profileBytes));
        profileBytes[0] = 99;
        metadata.Chunks[0].Data[0] = 88;
        metadata.UnknownChunks[0].Data[0] = 77;
        metadata.Palette.Entries[0].Red = 66;

        Assert.That(PngTextParser.Parse(result.Chunks[0]).Keyword, Is.EqualTo("Title"));
        AssertIccProfile(PngIccProfileParser.Parse(result.Chunks[1]), "Display", CreateProfileBytes());
        Assert.That(result.UnknownChunks[0].Data[0], Is.EqualTo(5));
        AssertEntry(result.Palette.Entries[0], 1, 2, 3);
    }

    [Test]
    public void SetIccProfileAndCicp_RejectInvalidInputs()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[0], new PngChunk[0]);
        PngIccProfile invalidProfileName = CreateIccProfile("", CreateProfileBytes());
        PngIccProfile nullProfileData = CreateIccProfile("Display", CreateProfileBytes());
        PngCodingIndependentCodePoints invalidFlag = CreateCodePoints(9, 16, 9, 2);
        nullProfileData.Profile = null!;

        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetIccProfile(null!, CreateIccProfile("Display", CreateProfileBytes())));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetIccProfile(metadata, null!));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetIccProfile(metadata, nullProfileData));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMetadataEditor.SetIccProfile(metadata, invalidProfileName));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetCodingIndependentCodePoints(null!, CreateCodePoints(9, 16, 9, 1)));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetCodingIndependentCodePoints(metadata, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMetadataEditor.SetCodingIndependentCodePoints(metadata, invalidFlag));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMetadataEditor.SetCodingIndependentCodePoints(metadata, 9, 16, 9, 2));
    }

    [Test]
    public void SetGamma_ReplacesExistingGammaChunks()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngStandardRgbWriter.WriteChunk(CreateStandardRgb(2)),
            PngGammaWriter.WriteChunk(CreateGamma(100000)),
            PngTextWriter.WriteChunk(CreateText("Title", "Keep")),
            PngGammaWriter.WriteChunk(CreateGamma(200000)),
            CreateChunk("vpAg", new byte[] { 5, 6 }, false)
        }, new PngChunk[]
        {
            PngGammaWriter.WriteChunk(CreateGamma(300000)),
            CreateChunk("vpAh", new byte[] { 7, 8 }, false)
        });

        PngMetadata result = PngMetadataEditor.SetGamma(metadata, CreateGamma(45455));

        AssertChunkTypes(result.Chunks, PngConstants.ChunkStandardRgb, PngConstants.ChunkGamma, PngConstants.ChunkText, "vpAg");
        AssertChunkTypes(result.UnknownChunks, "vpAh");
        Assert.That(PngGammaParser.Parse(result.Chunks[1]).Value, Is.EqualTo(45455));
        Assert.That(PngStandardRgbParser.Parse(result.Chunks[0]).RenderingIntent, Is.EqualTo(2));
        Assert.That(result.MetadataBytes, Is.EqualTo(CountMetadataBytes(result.Chunks)));
        Assert.That(result.TextBytes, Is.EqualTo(CountTextBytes(result.Chunks)));
        Assert.That(result.TextEntryCount, Is.EqualTo(1));
    }

    [Test]
    public void SetGamma_AppendsGammaChunkWhenAbsent()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Title", "Keep"))
        }, new PngChunk[0]);

        PngMetadata result = PngMetadataEditor.SetGamma(metadata, 45455u);

        AssertChunkTypes(result.Chunks, PngConstants.ChunkText, PngConstants.ChunkGamma);
        Assert.That(PngGammaParser.Parse(result.Chunks[1]).Value, Is.EqualTo(45455));
    }

    [Test]
    public void SetStandardRgb_ReplacesExistingStandardRgbChunks()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngGammaWriter.WriteChunk(CreateGamma(45455)),
            PngStandardRgbWriter.WriteChunk(CreateStandardRgb(0)),
            PngTextWriter.WriteChunk(CreateText("Title", "Keep")),
            PngStandardRgbWriter.WriteChunk(CreateStandardRgb(1)),
            CreateChunk("vpAg", new byte[] { 5, 6 }, false)
        }, new PngChunk[]
        {
            PngStandardRgbWriter.WriteChunk(CreateStandardRgb(2)),
            CreateChunk("vpAh", new byte[] { 7, 8 }, false)
        });

        PngMetadata result = PngMetadataEditor.SetStandardRgb(metadata, CreateStandardRgb(3));

        AssertChunkTypes(result.Chunks, PngConstants.ChunkGamma, PngConstants.ChunkStandardRgb, PngConstants.ChunkText, "vpAg");
        AssertChunkTypes(result.UnknownChunks, "vpAh");
        Assert.That(PngStandardRgbParser.Parse(result.Chunks[1]).RenderingIntent, Is.EqualTo(3));
        Assert.That(PngGammaParser.Parse(result.Chunks[0]).Value, Is.EqualTo(45455));
        Assert.That(result.MetadataBytes, Is.EqualTo(CountMetadataBytes(result.Chunks)));
        Assert.That(result.TextEntryCount, Is.EqualTo(1));
    }

    [Test]
    public void SetStandardRgb_AppendsStandardRgbChunkWhenAbsent()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngGammaWriter.WriteChunk(CreateGamma(45455))
        }, new PngChunk[0]);

        PngMetadata result = PngMetadataEditor.SetStandardRgb(metadata, 1);

        AssertChunkTypes(result.Chunks, PngConstants.ChunkGamma, PngConstants.ChunkStandardRgb);
        Assert.That(PngStandardRgbParser.Parse(result.Chunks[1]).RenderingIntent, Is.EqualTo(1));
    }

    [Test]
    public void SetGammaAndStandardRgb_ReturnIndependentMetadataCopies()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Title", "Keep"))
        }, new PngChunk[]
        {
            CreateChunk("vpAg", new byte[] { 5, 6 }, false)
        });
        metadata.Palette = CreatePalette(1);

        PngMetadata result = PngMetadataEditor.SetStandardRgb(metadata, 2);
        metadata.Chunks[0].Data[0] = 99;
        metadata.UnknownChunks[0].Data[0] = 88;
        metadata.Palette.Entries[0].Red = 77;

        Assert.That(PngTextParser.Parse(result.Chunks[0]).Keyword, Is.EqualTo("Title"));
        Assert.That(result.UnknownChunks[0].Data[0], Is.EqualTo(5));
        AssertEntry(result.Palette.Entries[0], 1, 2, 3);
        Assert.That(PngStandardRgbParser.Parse(result.Chunks[1]).RenderingIntent, Is.EqualTo(2));
    }

    [Test]
    public void SetGammaAndStandardRgb_RejectInvalidInputs()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[0], new PngChunk[0]);
        PngStandardRgb invalidIntent = CreateStandardRgb(4);

        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetGamma(null!, CreateGamma(45455)));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetGamma(metadata, null!));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetStandardRgb(null!, CreateStandardRgb(1)));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetStandardRgb(metadata, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMetadataEditor.SetStandardRgb(metadata, invalidIntent));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMetadataEditor.SetStandardRgb(metadata, 4));
    }

    [Test]
    public void SetPhysicalDimensions_ReplacesExistingPhysicalChunks()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 1, 2, 3, 4 }, true),
            PngPhysicalPixelsWriter.WriteChunk(CreatePhysicalPixels(1, 2, PngPhysicalPixelsParser.UnitUnknown)),
            PngTextWriter.WriteChunk(CreateText("Title", "Keep")),
            PngPhysicalPixelsWriter.WriteChunk(CreatePhysicalPixels(3, 4, PngPhysicalPixelsParser.UnitMetre)),
            CreateChunk("vpAg", new byte[] { 5, 6 }, false)
        }, new PngChunk[]
        {
            PngPhysicalPixelsWriter.WriteChunk(CreatePhysicalPixels(7, 8, PngPhysicalPixelsParser.UnitUnknown)),
            CreateChunk("vpAh", new byte[] { 9, 10 }, false)
        });

        PngMetadata result = PngMetadataEditor.SetPhysicalDimensions(metadata, CreatePhysicalPixels(2835, 11811, PngPhysicalPixelsParser.UnitMetre));

        AssertChunkTypes(result.Chunks, PngConstants.ChunkGamma, PngConstants.ChunkPhysicalPixels, PngConstants.ChunkText, "vpAg");
        AssertChunkTypes(result.UnknownChunks, "vpAh");
        AssertPhysicalPixels(PngPhysicalPixelsParser.Parse(result.Chunks[1]), 2835, 11811, PngPhysicalPixelsParser.UnitMetre);
        Assert.That(result.MetadataBytes, Is.EqualTo(CountMetadataBytes(result.Chunks)));
        Assert.That(result.TextBytes, Is.EqualTo(CountTextBytes(result.Chunks)));
        Assert.That(result.TextEntryCount, Is.EqualTo(1));
    }

    [Test]
    public void SetPhysicalDimensions_AppendsPhysicalChunkWhenAbsent()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 1, 2, 3, 4 }, true),
            PngTextWriter.WriteChunk(CreateText("Title", "Keep"))
        }, new PngChunk[0]);

        PngMetadata result = PngMetadataEditor.SetPhysicalDimensions(metadata, CreatePhysicalPixels(100, 200, PngPhysicalPixelsParser.UnitUnknown));

        AssertChunkTypes(result.Chunks, PngConstants.ChunkGamma, PngConstants.ChunkText, PngConstants.ChunkPhysicalPixels);
        AssertPhysicalPixels(PngPhysicalPixelsParser.Parse(result.Chunks[2]), 100, 200, PngPhysicalPixelsParser.UnitUnknown);
    }

    [Test]
    public void SetPhysicalDimensions_ValueOverloadWritesPhysicalChunk()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[0], new PngChunk[0]);

        PngMetadata result = PngMetadataEditor.SetPhysicalDimensions(metadata, 300u, 400u, PngPhysicalPixelsParser.UnitMetre);

        AssertChunkTypes(result.Chunks, PngConstants.ChunkPhysicalPixels);
        AssertPhysicalPixels(PngPhysicalPixelsParser.Parse(result.Chunks[0]), 300, 400, PngPhysicalPixelsParser.UnitMetre);
        Assert.That(result.MetadataBytes, Is.EqualTo(9));
    }

    [Test]
    public void SetPhysicalDimensions_ReturnsIndependentMetadataCopies()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 1, 2, 3, 4 }, true)
        }, new PngChunk[]
        {
            CreateChunk("vpAg", new byte[] { 5, 6 }, false)
        });
        metadata.Palette = CreatePalette(1);

        PngMetadata result = PngMetadataEditor.SetPhysicalDimensions(metadata, CreatePhysicalPixels(100, 200, PngPhysicalPixelsParser.UnitMetre));
        metadata.Chunks[0].Data[0] = 99;
        metadata.UnknownChunks[0].Data[0] = 88;
        metadata.Palette.Entries[0].Red = 77;

        Assert.That(result.Chunks[0].Data[0], Is.EqualTo(1));
        Assert.That(result.UnknownChunks[0].Data[0], Is.EqualTo(5));
        AssertEntry(result.Palette.Entries[0], 1, 2, 3);
        Assert.That(result.Chunks[1].Data, Is.Not.SameAs(PngPhysicalPixelsWriter.WriteChunk(CreatePhysicalPixels(100, 200, PngPhysicalPixelsParser.UnitMetre)).Data));
    }

    [Test]
    public void SetPhysicalDimensions_RejectsInvalidInputs()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[0], new PngChunk[0]);
        PngPhysicalPixels invalidUnit = CreatePhysicalPixels(100, 200, 2);

        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetPhysicalDimensions(null!, CreatePhysicalPixels(100, 200, PngPhysicalPixelsParser.UnitMetre)));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.SetPhysicalDimensions(metadata, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMetadataEditor.SetPhysicalDimensions(metadata, invalidUnit));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMetadataEditor.SetPhysicalDimensions(metadata, 100u, 200u, 2));
    }

    [Test]
    public void ReplaceTextChunks_ReplacesExistingTextChunksAtFirstTextPosition()
    {
        PngChunk replacementText = PngTextWriter.WriteChunk(CreateText("Title", "Updated"));
        PngChunk replacementCompressed = PngCompressedTextWriter.WriteChunk(CreateCompressedText("Comment", "Compressed"));
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 1, 2, 3, 4 }, true),
            PngTextWriter.WriteChunk(CreateText("Title", "Old")),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 5, 6, 7 }, true),
            PngCompressedTextWriter.WriteChunk(CreateCompressedText("Comment", "Old compressed")),
            PngInternationalTextWriter.WriteChunk(CreateInternationalText("Description", "Old international")),
            CreateChunk("vpAg", new byte[] { 8, 9 }, false)
        }, new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Debug", "Remove")),
            CreateChunk("vpAh", new byte[] { 10, 11 }, false)
        });

        PngMetadata result = PngMetadataEditor.ReplaceTextChunks(metadata, new PngChunk[]
        {
            replacementText,
            replacementCompressed
        });

        AssertChunkTypes(result.Chunks, PngConstants.ChunkGamma, PngConstants.ChunkText, PngConstants.ChunkCompressedText, PngConstants.ChunkPhysicalPixels, "vpAg");
        AssertChunkTypes(result.UnknownChunks, "vpAh");
        Assert.That(PngTextParser.Parse(result.Chunks[1]).Text, Is.EqualTo("Updated"));
        Assert.That(PngCompressedTextParser.Parse(result.Chunks[2]).Text, Is.EqualTo("Compressed"));
        Assert.That(result.MetadataBytes, Is.EqualTo(CountMetadataBytes(result.Chunks)));
        Assert.That(result.TextBytes, Is.EqualTo(CountTextBytes(result.Chunks)));
        Assert.That(result.TextEntryCount, Is.EqualTo(2));
    }

    [Test]
    public void ReplaceTextChunks_AppendsTextChunksWhenNoExistingTextChunks()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 1, 2, 3, 4 }, true),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 5, 6, 7 }, true)
        }, new PngChunk[0]);

        PngMetadata result = PngMetadataEditor.ReplaceTextChunks(metadata, new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Title", "Added"))
        });

        AssertChunkTypes(result.Chunks, PngConstants.ChunkGamma, PngConstants.ChunkPhysicalPixels, PngConstants.ChunkText);
        Assert.That(PngTextParser.Parse(result.Chunks[2]).Text, Is.EqualTo("Added"));
        Assert.That(result.TextEntryCount, Is.EqualTo(1));
    }

    [Test]
    public void ReplaceTextChunks_TextModelOverloadWritesTextChunks()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Title", "Old"))
        }, new PngChunk[0]);

        PngMetadata result = PngMetadataEditor.ReplaceTextChunks(metadata, new PngText[]
        {
            CreateText("Title", "One"),
            CreateText("Author", "Two")
        });

        AssertChunkTypes(result.Chunks, PngConstants.ChunkText, PngConstants.ChunkText);
        Assert.That(PngTextParser.Parse(result.Chunks[0]).Text, Is.EqualTo("One"));
        Assert.That(PngTextParser.Parse(result.Chunks[1]).Keyword, Is.EqualTo("Author"));
        Assert.That(result.TextEntryCount, Is.EqualTo(2));
    }

    [Test]
    public void ReplaceTextChunks_RemovesExistingTextChunksWhenReplacementListIsEmpty()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Title", "Old")),
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 1, 2, 3, 4 }, true),
            PngCompressedTextWriter.WriteChunk(CreateCompressedText("Comment", "Old"))
        }, new PngChunk[0]);

        PngMetadata result = PngMetadataEditor.ReplaceTextChunks(metadata, new PngChunk[0]);

        AssertChunkTypes(result.Chunks, PngConstants.ChunkGamma);
        Assert.That(result.MetadataBytes, Is.EqualTo(4));
        Assert.That(result.TextBytes, Is.EqualTo(0));
        Assert.That(result.TextEntryCount, Is.EqualTo(0));
    }

    [Test]
    public void ReplaceTextChunks_ReturnsIndependentReplacementCopies()
    {
        PngChunk replacement = PngTextWriter.WriteChunk(CreateText("Title", "Stable"));
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 1, 2, 3, 4 }, true)
        }, new PngChunk[0]);
        metadata.Palette = CreatePalette(1);

        PngMetadata result = PngMetadataEditor.ReplaceTextChunks(metadata, new PngChunk[] { replacement });
        replacement.Data[0] = 90;
        metadata.Palette.Entries[0].Red = 99;

        Assert.That(result.Chunks[1], Is.Not.SameAs(replacement));
        Assert.That(result.Chunks[1].Data, Is.Not.SameAs(replacement.Data));
        Assert.That(PngTextParser.Parse(result.Chunks[1]).Keyword, Is.EqualTo("Title"));
        AssertEntry(result.Palette.Entries[0], 1, 2, 3);
    }

    [Test]
    public void ReplaceTextChunks_RejectsInvalidInputs()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Title", "Old"))
        }, new PngChunk[0]);
        PngChunk nullType = CreateChunk(null!, new byte[] { 1 }, true);
        PngChunk nullData = CreateChunk(PngConstants.ChunkText, null!, true);
        PngChunk nonText = CreateChunk(PngConstants.ChunkGamma, new byte[] { 1, 2, 3, 4 }, true);

        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.ReplaceTextChunks(null!, new PngChunk[0]));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.ReplaceTextChunks(metadata, (PngChunk[])null!));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.ReplaceTextChunks(metadata, (PngText[])null!));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.ReplaceTextChunks(metadata, new PngChunk[] { null! }));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.ReplaceTextChunks(metadata, new PngChunk[] { nullType }));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.ReplaceTextChunks(metadata, new PngChunk[] { nullData }));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.ReplaceTextChunks(metadata, new PngText[] { null! }));
        Assert.Throws<ArgumentException>(() => PngMetadataEditor.ReplaceTextChunks(metadata, new PngChunk[] { nonText }));
    }

    [Test]
    public void RemoveChunks_RemovesSelectedTypesFromChunksAndUnknownChunks()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 1, 2, 3, 4 }, true),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 5, 6, 7, 8, 9 }, true),
            CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 }, true),
            CreateChunk("vpAg", new byte[] { 10, 11 }, false)
        }, new PngChunk[]
        {
            CreateChunk("vpAg", new byte[] { 10, 11 }, false),
            CreateChunk("vpAh", new byte[] { 12, 13, 14 }, false)
        });

        PngMetadata result = PngMetadataEditor.RemoveChunks(metadata, new string[]
        {
            PngConstants.ChunkGamma,
            "vpAg"
        });

        AssertChunkTypes(result.Chunks, PngConstants.ChunkPhysicalPixels, PngConstants.ChunkText);
        AssertChunkTypes(result.UnknownChunks, "vpAh");
        Assert.That(result.MetadataBytes, Is.EqualTo(8));
        Assert.That(result.TextBytes, Is.EqualTo(1));
        Assert.That(result.TextEntryCount, Is.EqualTo(1));
    }

    [Test]
    public void RemoveChunks_RemovesSingleChunkType()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 }, true),
            CreateChunk(PngConstants.ChunkCompressedText, new byte[] { 75, 0, 0, 86 }, true),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 1, 2, 3 }, true)
        }, new PngChunk[0]);

        PngMetadata result = PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkText);

        AssertChunkTypes(result.Chunks, PngConstants.ChunkCompressedText, PngConstants.ChunkPhysicalPixels);
        Assert.That(result.MetadataBytes, Is.EqualTo(7));
        Assert.That(result.TextBytes, Is.EqualTo(0));
        Assert.That(result.TextEntryCount, Is.EqualTo(1));
    }

    [Test]
    public void RemoveChunks_ReturnsIndependentMetadataCopies()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 }, true)
        }, new PngChunk[]
        {
            CreateChunk("vpAg", new byte[] { 1, 2 }, false)
        });
        metadata.Palette = CreatePalette(1);

        PngMetadata result = PngMetadataEditor.RemoveChunks(metadata, new string[0]);
        metadata.Chunks[0].Data[0] = 90;
        metadata.UnknownChunks[0].Data[0] = 99;
        metadata.Palette.Entries[0].Red = 77;

        Assert.That(result.Chunks, Is.Not.SameAs(metadata.Chunks));
        Assert.That(result.UnknownChunks, Is.Not.SameAs(metadata.UnknownChunks));
        Assert.That(result.Palette, Is.Not.SameAs(metadata.Palette));
        Assert.That(result.Palette.Entries, Is.Not.SameAs(metadata.Palette.Entries));
        Assert.That(result.Chunks[0], Is.Not.SameAs(metadata.Chunks[0]));
        Assert.That(result.Chunks[0].Data, Is.Not.SameAs(metadata.Chunks[0].Data));
        Assert.That(result.UnknownChunks[0].Data, Is.Not.SameAs(metadata.UnknownChunks[0].Data));
        Assert.That(result.Palette.Entries[0], Is.Not.SameAs(metadata.Palette.Entries[0]));
        Assert.That(result.Chunks[0].Data[0], Is.EqualTo(75));
        Assert.That(result.UnknownChunks[0].Data[0], Is.EqualTo(1));
        AssertEntry(result.Palette.Entries[0], 1, 2, 3);
    }

    [Test]
    public void RemoveChunks_RecalculatesSummaryForNoRemainingMetadata()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 }, true),
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 1, 2, 3, 4 }, true)
        }, new PngChunk[0]);
        metadata.MetadataBytes = 100;
        metadata.TextBytes = 100;
        metadata.TextEntryCount = 100;

        PngMetadata result = PngMetadataEditor.RemoveChunks(metadata, new string[]
        {
            PngConstants.ChunkText,
            PngConstants.ChunkGamma
        });

        Assert.That(result.Chunks, Is.Empty);
        Assert.That(result.MetadataBytes, Is.EqualTo(0));
        Assert.That(result.TextBytes, Is.EqualTo(0));
        Assert.That(result.TextEntryCount, Is.EqualTo(0));
    }

    [Test]
    public void RemoveChunks_RejectsInvalidInputs()
    {
        PngMetadata metadata = CreateMetadata(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 }, true)
        }, new PngChunk[0]);
        PngMetadata nullChunks = CreateMetadata(new PngChunk[0], new PngChunk[0]);
        PngMetadata nullUnknownChunks = CreateMetadata(new PngChunk[0], new PngChunk[0]);
        PngMetadata nullPalette = CreateMetadata(new PngChunk[0], new PngChunk[0]);
        PngMetadata nullPaletteEntries = CreateMetadata(new PngChunk[0], new PngChunk[0]);
        PngMetadata nullPaletteEntry = CreateMetadata(new PngChunk[0], new PngChunk[0]);
        PngMetadata nullChunk = CreateMetadata(new PngChunk[] { null! }, new PngChunk[0]);
        PngMetadata nullChunkType = CreateMetadata(new PngChunk[] { CreateChunk(null!, new byte[] { 1 }, true) }, new PngChunk[0]);
        PngMetadata nullChunkData = CreateMetadata(new PngChunk[] { CreateChunk(PngConstants.ChunkText, null!, true) }, new PngChunk[0]);
        nullChunks.Chunks = null!;
        nullUnknownChunks.UnknownChunks = null!;
        nullPalette.Palette = null!;
        nullPaletteEntries.Palette.Entries = null!;
        nullPaletteEntry.Palette.Entries = new PngPaletteEntry[] { null! };

        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(null!, PngConstants.ChunkText));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(metadata, (string)null!));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(metadata, (string[])null!));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(metadata, new string[] { null! }));
        Assert.Throws<ArgumentException>(() => PngMetadataEditor.RemoveChunks(metadata, "abc"));
        Assert.Throws<ArgumentException>(() => PngMetadataEditor.RemoveChunks(metadata, "abca"));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(nullChunks, new string[0]));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(nullUnknownChunks, new string[0]));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(nullPalette, new string[0]));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(nullPaletteEntries, new string[0]));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(nullPaletteEntry, new string[0]));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(nullChunk, new string[0]));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(nullChunkType, new string[0]));
        Assert.Throws<ArgumentNullException>(() => PngMetadataEditor.RemoveChunks(nullChunkData, new string[0]));
    }

    static PngMetadata CreateMetadata(PngChunk[] chunks, PngChunk[] unknownChunks)
    {
        PngMetadata metadata = new PngMetadata();
        metadata.Chunks = chunks;
        metadata.UnknownChunks = unknownChunks;
        metadata.Palette = CreatePalette(0);
        return metadata;
    }

    static PngText CreateText(string keyword, string textValue)
    {
        PngText text = new PngText();
        text.Keyword = keyword;
        text.Text = textValue;
        return text;
    }

    static PngCompressedText CreateCompressedText(string keyword, string textValue)
    {
        PngCompressedText text = new PngCompressedText();
        text.Keyword = keyword;
        text.CompressionMethod = PngCompressedTextParser.CompressionMethodDeflate;
        text.Text = textValue;
        return text;
    }

    static PngInternationalText CreateInternationalText(string keyword, string textValue)
    {
        PngInternationalText text = new PngInternationalText();
        text.Keyword = keyword;
        text.CompressionFlag = PngInternationalTextParser.CompressionFlagUncompressed;
        text.CompressionMethod = PngInternationalTextParser.CompressionMethodDeflate;
        text.LanguageTag = "en";
        text.TranslatedKeyword = keyword;
        text.Text = textValue;
        return text;
    }

    static PngGamma CreateGamma(uint value)
    {
        PngGamma gamma = new PngGamma();
        gamma.Value = value;
        return gamma;
    }

    static PngStandardRgb CreateStandardRgb(byte renderingIntent)
    {
        PngStandardRgb standardRgb = new PngStandardRgb();
        standardRgb.RenderingIntent = renderingIntent;
        return standardRgb;
    }

    static PngIccProfile CreateIccProfile(string name, byte[] profileBytes)
    {
        PngIccProfile profile = new PngIccProfile();
        profile.Name = name;
        profile.CompressionMethod = PngIccProfileParser.CompressionMethodDeflate;
        profile.Profile = profileBytes;
        return profile;
    }

    static byte[] CreateProfileBytes()
    {
        return new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
    }

    static PngCodingIndependentCodePoints CreateCodePoints(byte colorPrimaries, byte transferCharacteristics, byte matrixCoefficients, byte videoFullRangeFlag)
    {
        PngCodingIndependentCodePoints codePoints = new PngCodingIndependentCodePoints();
        codePoints.ColorPrimaries = colorPrimaries;
        codePoints.TransferCharacteristics = transferCharacteristics;
        codePoints.MatrixCoefficients = matrixCoefficients;
        codePoints.VideoFullRangeFlag = videoFullRangeFlag;
        return codePoints;
    }

    static PngPhysicalPixels CreatePhysicalPixels(uint pixelsPerUnitX, uint pixelsPerUnitY, byte unitSpecifier)
    {
        PngPhysicalPixels physicalPixels = new PngPhysicalPixels();
        physicalPixels.PixelsPerUnitX = pixelsPerUnitX;
        physicalPixels.PixelsPerUnitY = pixelsPerUnitY;
        physicalPixels.UnitSpecifier = unitSpecifier;
        return physicalPixels;
    }

    static PngChunk CreateChunk(string type, byte[] data, bool isKnown)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        if (data != null)
        {
            chunk.Length = (uint)data.Length;
        }

        chunk.Crc = 1234;
        chunk.Offset = 56;
        chunk.IsKnown = isKnown;
        if (type != null)
        {
            chunk.IsCritical = !PngChunkTypeFlags.IsAncillary(type);
            chunk.IsPublic = PngChunkTypeFlags.IsPublic(type);
            chunk.IsReservedBitValid = PngChunkTypeFlags.IsReservedBitValid(type);
            chunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(type);
        }

        return chunk;
    }

    static PngPalette CreatePalette(int entryCount)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[entryCount];

        int index = 0;
        while (index < entryCount)
        {
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = (byte)(index + 1);
            entry.Green = (byte)(index + 2);
            entry.Blue = (byte)(index + 3);
            palette.Entries[index] = entry;
            index += 1;
        }

        return palette;
    }

    static void AssertChunkTypes(PngChunk[] chunks, params string[] types)
    {
        Assert.That(chunks, Has.Length.EqualTo(types.Length));

        int index = 0;
        while (index < types.Length)
        {
            Assert.That(chunks[index].Type, Is.EqualTo(types[index]));
            index += 1;
        }
    }

    static void AssertEntry(PngPaletteEntry entry, int red, int green, int blue)
    {
        Assert.That(entry.Red, Is.EqualTo(red));
        Assert.That(entry.Green, Is.EqualTo(green));
        Assert.That(entry.Blue, Is.EqualTo(blue));
    }

    static void AssertPhysicalPixels(PngPhysicalPixels physicalPixels, uint pixelsPerUnitX, uint pixelsPerUnitY, byte unitSpecifier)
    {
        Assert.That(physicalPixels.PixelsPerUnitX, Is.EqualTo(pixelsPerUnitX));
        Assert.That(physicalPixels.PixelsPerUnitY, Is.EqualTo(pixelsPerUnitY));
        Assert.That(physicalPixels.UnitSpecifier, Is.EqualTo(unitSpecifier));
    }

    static void AssertIccProfile(PngIccProfile profile, string name, byte[] profileBytes)
    {
        Assert.That(profile.Name, Is.EqualTo(name));
        Assert.That(profile.CompressionMethod, Is.EqualTo(PngIccProfileParser.CompressionMethodDeflate));
        Assert.That(profile.Profile, Is.EqualTo(profileBytes));
        Assert.That(profile.CompressedProfile, Is.Not.Empty);
    }

    static void AssertCodePoints(PngCodingIndependentCodePoints codePoints, byte colorPrimaries, byte transferCharacteristics, byte matrixCoefficients, byte videoFullRangeFlag)
    {
        Assert.That(codePoints.ColorPrimaries, Is.EqualTo(colorPrimaries));
        Assert.That(codePoints.TransferCharacteristics, Is.EqualTo(transferCharacteristics));
        Assert.That(codePoints.MatrixCoefficients, Is.EqualTo(matrixCoefficients));
        Assert.That(codePoints.VideoFullRangeFlag, Is.EqualTo(videoFullRangeFlag));
    }

    static long CountMetadataBytes(PngChunk[] chunks)
    {
        long count = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (PngChunkTypeFlags.IsAncillary(chunks[index].Type))
            {
                count += chunks[index].Length;
            }

            index += 1;
        }

        return count;
    }

    static long CountTextBytes(PngChunk[] chunks)
    {
        long count = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == PngConstants.ChunkText
                || chunks[index].Type == PngConstants.ChunkCompressedText
                || chunks[index].Type == PngConstants.ChunkInternationalText)
            {
                count += PngMetadataEditor.GetTextLength(chunks[index]);
            }

            index += 1;
        }

        return count;
    }
}
