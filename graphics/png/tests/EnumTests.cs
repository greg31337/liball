using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class EnumTests
{
    [Test]
    public void PngWireEnums_UseSpecificationValues()
    {
        Assert.That((int)PngColorType.Grayscale, Is.EqualTo(0));
        Assert.That((int)PngColorType.Truecolor, Is.EqualTo(2));
        Assert.That((int)PngColorType.IndexedColor, Is.EqualTo(3));
        Assert.That((int)PngColorType.GrayscaleAlpha, Is.EqualTo(4));
        Assert.That((int)PngColorType.TruecolorAlpha, Is.EqualTo(6));

        Assert.That((int)PngBitDepth.One, Is.EqualTo(1));
        Assert.That((int)PngBitDepth.Two, Is.EqualTo(2));
        Assert.That((int)PngBitDepth.Four, Is.EqualTo(4));
        Assert.That((int)PngBitDepth.Eight, Is.EqualTo(8));
        Assert.That((int)PngBitDepth.Sixteen, Is.EqualTo(16));

        Assert.That((int)PngCompressionMethod.Deflate, Is.EqualTo(0));
        Assert.That((int)PngFilterMethod.Adaptive, Is.EqualTo(0));

        Assert.That((int)PngInterlaceMethod.None, Is.EqualTo(0));
        Assert.That((int)PngInterlaceMethod.Adam7, Is.EqualTo(1));

        Assert.That((int)PngFilterType.None, Is.EqualTo(0));
        Assert.That((int)PngFilterType.Sub, Is.EqualTo(1));
        Assert.That((int)PngFilterType.Up, Is.EqualTo(2));
        Assert.That((int)PngFilterType.Average, Is.EqualTo(3));
        Assert.That((int)PngFilterType.Paeth, Is.EqualTo(4));
    }

    [Test]
    public void ApngWireEnums_UseSpecificationValues()
    {
        Assert.That((int)PngApngBlendOperation.Source, Is.EqualTo(0));
        Assert.That((int)PngApngBlendOperation.Over, Is.EqualTo(1));

        Assert.That((int)PngApngDisposeOperation.None, Is.EqualTo(0));
        Assert.That((int)PngApngDisposeOperation.Background, Is.EqualTo(1));
        Assert.That((int)PngApngDisposeOperation.Previous, Is.EqualTo(2));
    }

    [Test]
    public void PolicyEnums_ExposeBaselineChoices()
    {
        Assert.That(Enum.GetName(typeof(PngRowFilterStrategy), PngRowFilterStrategy.Fast), Is.EqualTo("Fast"));
        Assert.That(Enum.GetName(typeof(PngRowFilterStrategy), PngRowFilterStrategy.Adaptive), Is.EqualTo("Adaptive"));
        Assert.That(Enum.GetName(typeof(PngRowFilterStrategy), PngRowFilterStrategy.Fixed), Is.EqualTo("Fixed"));
        Assert.That(Enum.GetName(typeof(PngRowFilterStrategy), PngRowFilterStrategy.Exhaustive), Is.EqualTo("Exhaustive"));

        Assert.That(Enum.GetName(typeof(PngChunkDisposition), PngChunkDisposition.Preserve), Is.EqualTo("Preserve"));
        Assert.That(Enum.GetName(typeof(PngChunkDisposition), PngChunkDisposition.Drop), Is.EqualTo("Drop"));
        Assert.That(Enum.GetName(typeof(PngChunkDisposition), PngChunkDisposition.Replace), Is.EqualTo("Replace"));

        Assert.That(Enum.GetName(typeof(PngChunkPlacement), PngChunkPlacement.Unspecified), Is.EqualTo("Unspecified"));
        Assert.That(Enum.GetName(typeof(PngChunkPlacement), PngChunkPlacement.BeforePalette), Is.EqualTo("BeforePalette"));
        Assert.That(Enum.GetName(typeof(PngChunkPlacement), PngChunkPlacement.BeforeImageData), Is.EqualTo("BeforeImageData"));
        Assert.That(Enum.GetName(typeof(PngChunkPlacement), PngChunkPlacement.ImageData), Is.EqualTo("ImageData"));
        Assert.That(Enum.GetName(typeof(PngChunkPlacement), PngChunkPlacement.AfterImageData), Is.EqualTo("AfterImageData"));
        Assert.That(Enum.GetName(typeof(PngChunkPreservationState), PngChunkPreservationState.Created), Is.EqualTo("Created"));
        Assert.That(Enum.GetName(typeof(PngChunkPreservationState), PngChunkPreservationState.Preserved), Is.EqualTo("Preserved"));
        Assert.That(Enum.GetName(typeof(PngChunkPlacementPolicy), PngChunkPlacementPolicy.Preserve), Is.EqualTo("Preserve"));
        Assert.That(Enum.GetName(typeof(PngChunkPlacementPolicy), PngChunkPlacementPolicy.Canonicalize), Is.EqualTo("Canonicalize"));

        Assert.That(Enum.GetName(typeof(PngMetadataPolicy), PngMetadataPolicy.Preserve), Is.EqualTo("Preserve"));
        Assert.That(Enum.GetName(typeof(PngMetadataPolicy), PngMetadataPolicy.Strip), Is.EqualTo("Strip"));
        Assert.That(Enum.GetName(typeof(PngMetadataPolicy), PngMetadataPolicy.KeepSelected), Is.EqualTo("KeepSelected"));
        Assert.That(Enum.GetName(typeof(PngMetadataPolicy), PngMetadataPolicy.RemoveSelected), Is.EqualTo("RemoveSelected"));
        Assert.That(Enum.GetName(typeof(PngTextCompressionMode), PngTextCompressionMode.Preserve), Is.EqualTo("Preserve"));
        Assert.That(Enum.GetName(typeof(PngTextCompressionMode), PngTextCompressionMode.Uncompressed), Is.EqualTo("Uncompressed"));
        Assert.That(Enum.GetName(typeof(PngTextCompressionMode), PngTextCompressionMode.Compressed), Is.EqualTo("Compressed"));
        Assert.That(Enum.GetName(typeof(PngTextCompressionMode), PngTextCompressionMode.Automatic), Is.EqualTo("Automatic"));

        Assert.That(Enum.GetName(typeof(PngErrorPolicy), PngErrorPolicy.Strict), Is.EqualTo("Strict"));
        Assert.That(Enum.GetName(typeof(PngErrorPolicy), PngErrorPolicy.TolerantMetadata), Is.EqualTo("TolerantMetadata"));
        Assert.That(Enum.GetName(typeof(PngErrorPolicy), PngErrorPolicy.TolerantRepair), Is.EqualTo("TolerantRepair"));
        Assert.That(Enum.GetName(typeof(PngErrorPolicy), PngErrorPolicy.BestEffortInspection), Is.EqualTo("BestEffortInspection"));
    }

    [Test]
    public void PixelFormatEnum_CoversPlannedNativeFormats()
    {
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Unknown), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Grayscale1), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Grayscale2), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Grayscale4), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Grayscale8), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Grayscale16), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Indexed1), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Indexed2), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Indexed4), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Indexed8), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Rgb8), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Rgb16), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.GrayscaleAlpha8), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.GrayscaleAlpha16), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Rgba8), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Rgba16), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Bgr8), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Bgr16), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Bgra8), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Bgra16), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Argb8), Is.True);
        Assert.That(Enum.IsDefined(typeof(PngPixelFormat), PngPixelFormat.Argb16), Is.True);
    }
}
