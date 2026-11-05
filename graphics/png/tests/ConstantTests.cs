using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ConstantTests
{
    [Test]
    public void PngConstants_ExposeOnlyInternalConstants()
    {
        FieldInfo[] fields = typeof(PngConstants).GetFields(BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(typeof(PngConstants).IsNotPublic, Is.True);
        Assert.That(fields, Is.Not.Empty);
        foreach (FieldInfo field in fields)
        {
            Assert.That(field.IsLiteral, Is.True, field.Name);
            Assert.That(field.IsAssembly, Is.True, field.Name);
        }
    }

    [Test]
    public void SignatureConstants_MatchPngSignature()
    {
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

        Assert.That(PngConstants.SignatureLength, Is.EqualTo(8));
        Assert.That(signature, Is.EqualTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
    }

    [Test]
    public void ChunkNameConstants_MatchCriticalAndKnownAncillaryNames()
    {
        Assert.That(PngConstants.ChunkIHDR, Is.EqualTo("IHDR"));
        Assert.That(PngConstants.ChunkPLTE, Is.EqualTo("PLTE"));
        Assert.That(PngConstants.ChunkIDAT, Is.EqualTo("IDAT"));
        Assert.That(PngConstants.ChunkIEND, Is.EqualTo("IEND"));

        Assert.That(PngConstants.ChunkTransparency, Is.EqualTo("tRNS"));
        Assert.That(PngConstants.ChunkChromaticities, Is.EqualTo("cHRM"));
        Assert.That(PngConstants.ChunkGamma, Is.EqualTo("gAMA"));
        Assert.That(PngConstants.ChunkIccProfile, Is.EqualTo("iCCP"));
        Assert.That(PngConstants.ChunkSignificantBits, Is.EqualTo("sBIT"));
        Assert.That(PngConstants.ChunkStandardRgb, Is.EqualTo("sRGB"));
        Assert.That(PngConstants.ChunkCodingIndependentCodePoints, Is.EqualTo("cICP"));
        Assert.That(PngConstants.ChunkMasteringDisplayColorVolume, Is.EqualTo("mDCV"));
        Assert.That(PngConstants.ChunkContentLightLevelInformation, Is.EqualTo("cLLI"));
        Assert.That(PngConstants.ChunkText, Is.EqualTo("tEXt"));
        Assert.That(PngConstants.ChunkCompressedText, Is.EqualTo("zTXt"));
        Assert.That(PngConstants.ChunkInternationalText, Is.EqualTo("iTXt"));
        Assert.That(PngConstants.ChunkBackgroundColor, Is.EqualTo("bKGD"));
        Assert.That(PngConstants.ChunkPaletteHistogram, Is.EqualTo("hIST"));
        Assert.That(PngConstants.ChunkPhysicalPixels, Is.EqualTo("pHYs"));
        Assert.That(PngConstants.ChunkSuggestedPalette, Is.EqualTo("sPLT"));
        Assert.That(PngConstants.ChunkExif, Is.EqualTo("eXIf"));
        Assert.That(PngConstants.ChunkTime, Is.EqualTo("tIME"));
        Assert.That(PngConstants.ChunkAnimationControl, Is.EqualTo("acTL"));
        Assert.That(PngConstants.ChunkFrameControl, Is.EqualTo("fcTL"));
        Assert.That(PngConstants.ChunkFrameData, Is.EqualTo("fdAT"));
    }

    [Test]
    public void NumericConstants_MatchPublicEnums()
    {
        Assert.That(PngConstants.ColorTypeGrayscale, Is.EqualTo((int)PngColorType.Grayscale));
        Assert.That(PngConstants.ColorTypeTruecolor, Is.EqualTo((int)PngColorType.Truecolor));
        Assert.That(PngConstants.ColorTypeIndexedColor, Is.EqualTo((int)PngColorType.IndexedColor));
        Assert.That(PngConstants.ColorTypeGrayscaleAlpha, Is.EqualTo((int)PngColorType.GrayscaleAlpha));
        Assert.That(PngConstants.ColorTypeTruecolorAlpha, Is.EqualTo((int)PngColorType.TruecolorAlpha));

        Assert.That(PngConstants.BitDepth1, Is.EqualTo((int)PngBitDepth.One));
        Assert.That(PngConstants.BitDepth2, Is.EqualTo((int)PngBitDepth.Two));
        Assert.That(PngConstants.BitDepth4, Is.EqualTo((int)PngBitDepth.Four));
        Assert.That(PngConstants.BitDepth8, Is.EqualTo((int)PngBitDepth.Eight));
        Assert.That(PngConstants.BitDepth16, Is.EqualTo((int)PngBitDepth.Sixteen));

        Assert.That(PngConstants.CompressionMethodDeflate, Is.EqualTo((int)PngCompressionMethod.Deflate));
        Assert.That(PngConstants.FilterMethodAdaptive, Is.EqualTo((int)PngFilterMethod.Adaptive));
        Assert.That(PngConstants.InterlaceMethodNone, Is.EqualTo((int)PngInterlaceMethod.None));
        Assert.That(PngConstants.InterlaceMethodAdam7, Is.EqualTo((int)PngInterlaceMethod.Adam7));

        Assert.That(PngConstants.FilterTypeNone, Is.EqualTo((int)PngFilterType.None));
        Assert.That(PngConstants.FilterTypeSub, Is.EqualTo((int)PngFilterType.Sub));
        Assert.That(PngConstants.FilterTypeUp, Is.EqualTo((int)PngFilterType.Up));
        Assert.That(PngConstants.FilterTypeAverage, Is.EqualTo((int)PngFilterType.Average));
        Assert.That(PngConstants.FilterTypePaeth, Is.EqualTo((int)PngFilterType.Paeth));

        Assert.That(PngConstants.ApngBlendOperationSource, Is.EqualTo((int)PngApngBlendOperation.Source));
        Assert.That(PngConstants.ApngBlendOperationOver, Is.EqualTo((int)PngApngBlendOperation.Over));
        Assert.That(PngConstants.ApngDisposeOperationNone, Is.EqualTo((int)PngApngDisposeOperation.None));
        Assert.That(PngConstants.ApngDisposeOperationBackground, Is.EqualTo((int)PngApngDisposeOperation.Background));
        Assert.That(PngConstants.ApngDisposeOperationPrevious, Is.EqualTo((int)PngApngDisposeOperation.Previous));
    }
}
