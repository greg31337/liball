using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class MetadataPopulationIntegrationTests
{
    [Test]
    public void DecodeAndIdentify_PopulateEquivalentTypedMetadataTotalsAndColorSelection()
    {
        byte[] png = CreateMetadataPng();

        PngImage image = Png.DecodeBytes(png);
        PngIdentifyResult identify = Png.IdentifyBytes(png);

        AssertMetadata(image.Metadata, 11);
        AssertMetadata(identify.Metadata, 11);
        Assert.That(identify.Metadata.MetadataBytes, Is.EqualTo(image.Metadata.MetadataBytes));
        Assert.That(identify.Metadata.TextBytes, Is.EqualTo(image.Metadata.TextBytes));
        Assert.That(identify.Metadata.TextEntryCount, Is.EqualTo(image.Metadata.TextEntryCount));
        Assert.That(identify.Metadata.TextEntries[2].OriginalChunkIndex, Is.EqualTo(image.Metadata.TextEntries[2].OriginalChunkIndex));
        Assert.That(HasChunk(image.Metadata.Chunks, PngConstants.ChunkIDAT), Is.True);
        Assert.That(HasChunk(identify.Metadata.Chunks, PngConstants.ChunkIDAT), Is.False);
        Assert.That(image.Warnings, Is.Empty);
        Assert.That(image.Errors, Is.Empty);
        Assert.That(identify.Warnings, Is.Empty);
        Assert.That(identify.Errors, Is.Empty);
    }

    [Test]
    public void DecodeAndIdentify_ReportTheSameToleratedAncillaryCrcDiagnostic()
    {
        byte[] png = CorruptChunkCrc(CreateMetadataPng(), "vpAg");
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.TolerantMetadata;

        PngImage image = Png.DecodeBytes(png, options);
        PngIdentifyResult identify = Png.IdentifyBytes(png, options);

        Assert.That(image.Warnings, Has.Length.EqualTo(1));
        Assert.That(identify.Warnings, Has.Length.EqualTo(1));
        Assert.That(image.Warnings[0].Code, Is.EqualTo("PNG_CHUNK_CRC_MISMATCH"));
        Assert.That(identify.Warnings[0].Code, Is.EqualTo(image.Warnings[0].Code));
        Assert.That(identify.Warnings[0].ChunkType, Is.EqualTo(image.Warnings[0].ChunkType));
        Assert.That(image.Errors, Is.Empty);
        Assert.That(identify.Errors, Is.Empty);
    }

    [Test]
    public void Identify_PopulatesTypedMetadataWithoutInflatingInvalidIdat()
    {
        byte[] png = ReplaceIdat(CreateMetadataPng(), new byte[] { 1, 2, 3, 4, 5 });

        PngIdentifyResult identify = Png.IdentifyBytes(png);

        Assert.That(identify.IsValid, Is.True);
        Assert.That(identify.IdatBytes, Is.EqualTo(5));
        Assert.That(identify.Metadata.PhysicalPixels, Is.Not.Null);
        Assert.That(identify.Metadata.TextEntries, Has.Length.EqualTo(3));
        Assert.That(identify.Metadata.ColorMetadata.SourceChunkType, Is.EqualTo(PngConstants.ChunkCodingIndependentCodePoints));
        Assert.Throws<InvalidDataException>(() => Png.DecodeBytes(png));
    }

    static void AssertMetadata(PngMetadata metadata, long expectedTextBytes)
    {
        Assert.That(metadata.Chromaticities, Is.Not.Null);
        Assert.That(metadata.Gamma, Is.Not.Null);
        Assert.That(metadata.Gamma!.Value, Is.EqualTo(45455));
        Assert.That(metadata.IccProfile, Is.Not.Null);
        Assert.That(metadata.IccProfile!.Name, Is.EqualTo("Test"));
        Assert.That(metadata.IccProfile.Profile, Is.EqualTo(new byte[] { 1, 3, 5, 7 }));
        Assert.That(metadata.SignificantBits, Is.Not.Null);
        Assert.That(metadata.StandardRgb, Is.Not.Null);
        Assert.That(metadata.CodingIndependentCodePoints, Is.Not.Null);
        Assert.That(metadata.MasteringDisplayColorVolume, Is.Not.Null);
        Assert.That(metadata.ContentLightLevelInformation, Is.Not.Null);
        Assert.That(metadata.BackgroundColor, Is.Not.Null);
        Assert.That(metadata.PhysicalPixels, Is.Not.Null);
        Assert.That(metadata.PhysicalPixels!.PixelsPerUnitX, Is.EqualTo(3780));
        Assert.That(metadata.SuggestedPalettes, Has.Length.EqualTo(1));
        Assert.That(metadata.ExifProfile, Is.Not.Null);
        Assert.That(metadata.Time, Is.Not.Null);
        Assert.That(metadata.TextEntries, Has.Length.EqualTo(3));
        Assert.That(metadata.Latin1TextEntries, Has.Length.EqualTo(1));
        Assert.That(metadata.CompressedTextEntries, Has.Length.EqualTo(1));
        Assert.That(metadata.InternationalTextEntries, Has.Length.EqualTo(1));
        Assert.That(metadata.TextBytes, Is.EqualTo(expectedTextBytes));
        Assert.That(metadata.TextEntryCount, Is.EqualTo(3));
        Assert.That(metadata.UnknownChunks, Has.Length.EqualTo(1));
        Assert.That(metadata.UnknownChunks[0].Type, Is.EqualTo("vpAg"));
        Assert.That(metadata.ColorMetadata.HasColorMetadata, Is.True);
        Assert.That(metadata.ColorMetadata.SourceChunkType, Is.EqualTo(PngConstants.ChunkCodingIndependentCodePoints));
        Assert.That(metadata.ColorMetadata.Precedence, Is.EqualTo(PngColorMetadataPrecedenceResolver.CodingIndependentCodePointsPrecedence));
        Assert.That(metadata.ColorMetadata.HasCodingIndependentCodePoints, Is.True);
    }

    static byte[] CreateMetadataPng()
    {
        PngChunk[] baseChunks = ReadChunks(PngValidFixtures.CreateRgba8Png(1, 1));
        List<PngChunk> chunks = new List<PngChunk>();
        chunks.Add(baseChunks[0]);
        chunks.Add(CreateChunk(PngConstants.ChunkChromaticities, new byte[32]));

        PngGamma gamma = new PngGamma();
        gamma.Value = 45455;
        chunks.Add(PngGammaWriter.WriteChunk(gamma));

        PngIccProfile profile = new PngIccProfile();
        profile.Name = "Test";
        profile.CompressionMethod = PngIccProfileParser.CompressionMethodDeflate;
        profile.Profile = new byte[] { 1, 3, 5, 7 };
        chunks.Add(PngIccProfileWriter.WriteChunk(profile));
        chunks.Add(CreateChunk(PngConstants.ChunkSignificantBits, new byte[] { 8, 8, 8, 8 }));

        PngStandardRgb standardRgb = new PngStandardRgb();
        standardRgb.RenderingIntent = 1;
        chunks.Add(PngStandardRgbWriter.WriteChunk(standardRgb));

        PngCodingIndependentCodePoints codePoints = new PngCodingIndependentCodePoints();
        codePoints.ColorPrimaries = 1;
        codePoints.TransferCharacteristics = 13;
        codePoints.MatrixCoefficients = 0;
        codePoints.VideoFullRangeFlag = 1;
        chunks.Add(PngCodingIndependentCodePointsWriter.WriteChunk(codePoints));
        chunks.Add(PngMasteringDisplayColorVolumeWriter.WriteChunk(new PngMasteringDisplayColorVolume()));
        chunks.Add(PngContentLightLevelInformationWriter.WriteChunk(new PngContentLightLevelInformation()));
        chunks.Add(CreateChunk(PngConstants.ChunkBackgroundColor, new byte[] { 0, 1, 0, 2, 0, 3 }));

        PngPhysicalPixels physicalPixels = new PngPhysicalPixels();
        physicalPixels.PixelsPerUnitX = 3780;
        physicalPixels.PixelsPerUnitY = 3780;
        physicalPixels.UnitSpecifier = PngPhysicalPixelsParser.UnitMetre;
        chunks.Add(PngPhysicalPixelsWriter.WriteChunk(physicalPixels));
        chunks.Add(CreateChunk(PngConstants.ChunkSuggestedPalette, new byte[] { 65, 108, 116, 0, 8, 1, 2, 3, 255, 0, 1 }));

        PngText latin1 = new PngText();
        latin1.Keyword = "Title";
        latin1.Text = "A";
        chunks.Add(PngTextWriter.WriteChunk(latin1));

        PngExifProfile exif = new PngExifProfile();
        exif.Profile = new byte[] { 0x49, 0x49, 0x2A, 0x00 };
        chunks.Add(PngExifProfileWriter.WriteChunk(exif));
        chunks.Add(baseChunks[1]);

        PngCompressedText compressed = new PngCompressedText();
        compressed.Keyword = "Comment";
        compressed.CompressionMethod = PngCompressedTextParser.CompressionMethodDeflate;
        compressed.Text = "Zip";
        chunks.Add(PngCompressedTextWriter.WriteChunk(compressed));

        PngInternationalText international = new PngInternationalText();
        international.Keyword = "Description";
        international.CompressionFlag = PngInternationalTextParser.CompressionFlagCompressed;
        international.CompressionMethod = PngInternationalTextParser.CompressionMethodDeflate;
        international.LanguageTag = "de";
        international.TranslatedKeyword = "Beschreibung";
        international.Text = "Grüße";
        chunks.Add(PngInternationalTextWriter.WriteChunk(international));

        PngTime time = new PngTime();
        time.Year = 2026;
        time.Month = 8;
        time.Day = 26;
        time.Hour = 12;
        chunks.Add(PngTimeWriter.WriteChunk(time));
        chunks.Add(CreateChunk("vpAg", new byte[] { 9, 8, 7 }));
        chunks.Add(baseChunks[2]);
        return WritePng(chunks.ToArray());
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        return chunk;
    }

    static PngChunk[] ReadChunks(byte[] png)
    {
        return PngChunkListReader.Read(png, PngConstants.SignatureLength, new PngSecurityLimits()).Chunks;
    }

    static byte[] WritePng(PngChunk[] chunks)
    {
        MemoryStream stream = new MemoryStream();
        byte[] signature = new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            PngConstants.SignatureByte7
        };
        stream.Write(signature, 0, signature.Length);
        int index = 0;
        while (index < chunks.Length)
        {
            PngRawChunkWriter.Write(stream, chunks[index]);
            index += 1;
        }

        return stream.ToArray();
    }

    static byte[] ReplaceIdat(byte[] png, byte[] data)
    {
        PngChunk[] chunks = ReadChunks(png);
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == PngConstants.ChunkIDAT)
            {
                chunks[index].Data = data;
                chunks[index].Length = (uint)data.Length;
                break;
            }

            index += 1;
        }

        return WritePng(chunks);
    }

    static byte[] CorruptChunkCrc(byte[] png, string chunkType)
    {
        byte[] result = (byte[])png.Clone();
        int offset = PngConstants.SignatureLength;
        while (offset < result.Length)
        {
            int length = (int)PngBinaryReader.ReadUInt32(result, offset);
            string type = new string(new char[]
            {
                (char)result[offset + 4],
                (char)result[offset + 5],
                (char)result[offset + 6],
                (char)result[offset + 7]
            });
            if (type == chunkType)
            {
                result[offset + 8 + length] ^= 1;
                return result;
            }

            offset += 12 + length;
        }

        throw new AssertionException("Chunk was not found.");
    }

    static bool HasChunk(PngChunk[] chunks, string type)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
