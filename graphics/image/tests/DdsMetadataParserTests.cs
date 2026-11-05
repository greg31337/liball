using System.Collections.Generic;
using System.IO;
using System.Linq;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class DdsMetadataParserTests
{
    [Test]
    public void Parse_ExtractsLegacyRgbaHeaderAndFrame()
    {
        byte[] bytes = CreateLegacy(256, 128, 4111, 1024, 0, 0, 65, "", 32, 16711680, 65280, 255, 4278190080, 4096, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Dds));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Dds.Header.Size").Value.UnsignedIntegerValue, Is.EqualTo(124));
        Assert.That(Find(document, "Dds.Header.Width").Value.UnsignedIntegerValue, Is.EqualTo(256));
        Assert.That(Find(document, "Dds.Header.Height").Value.UnsignedIntegerValue, Is.EqualTo(128));
        Assert.That(Find(document, "Dds.Header.PitchOrLinearSize").Value.UnsignedIntegerValue, Is.EqualTo(1024));
        Assert.That(Find(document, "Dds.Header.PitchOrLinearSize").DisplayValue, Is.EqualTo("row pitch in bytes"));
        Assert.That(Find(document, "Dds.PixelFormat.Name").Value.Text, Is.EqualTo("RGBA"));
        Assert.That(Find(document, "Dds.PixelFormat.RedMask").DisplayValue, Is.EqualTo("0x00FF0000"));
        Assert.That(Find(document, "Dds.PixelFormat.AlphaMask").DisplayValue, Is.EqualTo("0xFF000000"));
        Assert.That(Find(document, "Dds.Header.DataOffset").Value.IntegerValue, Is.EqualTo(128));
        Assert.That(document.Directories.Select(item => item.Path), Is.EqualTo(new[] { "DDS/Header", "DDS/PixelFormat" }));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(256));
        Assert.That(document.Frames[0].Height, Is.EqualTo(128));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
        Assert.That(document.Frames[0].Orientation, Is.EqualTo(ImageOrientation.Normal));
        Assert.That(document.Summary.Width, Is.EqualTo(256));
        Assert.That(document.Summary.Height, Is.EqualTo(128));
        Assert.That(document.RawBlocks.Single().Offset, Is.EqualTo(0));
        Assert.That(document.RawBlocks.Single().Length, Is.EqualTo(128));
    }

    [Test]
    public void Parse_ExtractsDxtCompressionMipmapsAndLinearSize()
    {
        byte[] bytes = CreateLegacy(512, 256, 528391, 131072, 0, 10, 4, "DXT5", 0, 0, 0, 0, 0, 4198408, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Dds.PixelFormat.FourCC").Value.Text, Is.EqualTo("DXT5"));
        Assert.That(Find(document, "Dds.PixelFormat.FourCC").DisplayValue, Is.EqualTo("BC3 / DXT5"));
        Assert.That(Find(document, "Dds.PixelFormat.IsBlockCompressed").Value.BooleanValue, Is.True);
        Assert.That(Find(document, "Dds.Header.MipMapCount").Value.UnsignedIntegerValue, Is.EqualTo(10));
        Assert.That(Find(document, "Dds.Header.PitchOrLinearSize").DisplayValue, Is.EqualTo("top-level byte length"));
        Assert.That(Find(document, "Dds.Header.Caps").DisplayValue, Does.Contain("MipMap"));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Unknown));
    }

    [Test]
    public void Parse_ExtractsLuminanceAlphaFormat()
    {
        byte[] bytes = CreateLegacy(64, 32, 4111, 128, 0, 0, 131073, "", 16, 255, 0, 0, 65280, 4096, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Dds.PixelFormat.Name").Value.Text, Is.EqualTo("Luminance with alpha"));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(2));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Gray));
    }

    [Test]
    public void Parse_ExtractsLegacyYuvMasks()
    {
        byte[] bytes = CreateLegacy(64, 32, 4111, 192, 0, 0, 512, "", 24, 255, 65280, 16711680, 0, 4096, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Dds.PixelFormat.Name").Value.Text, Is.EqualTo("YUV"));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.YCbCr));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
    }

    [Test]
    public void Parse_ExtractsLegacyAlphaOnlyDepth()
    {
        byte[] bytes = CreateLegacy(64, 32, 4111, 64, 0, 0, 2, "", 8, 0, 0, 0, 255, 4096, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Dds.PixelFormat.Name").Value.Text, Is.EqualTo("Alpha"));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(1));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Unknown));
    }

    [Test]
    public void Parse_ExtractsCubeMapFacesAndCapabilities()
    {
        long allFaces = 64512;
        byte[] bytes = CreateLegacy(128, 128, 4103, 0, 0, 1, 4, "DXT1", 0, 0, 0, 0, 0, 4104, 512 + allFaces);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Dds.Header.IsCubeMap").Value.BooleanValue, Is.True);
        Assert.That(Find(document, "Dds.Header.CubeFaceCount").Value.IntegerValue, Is.EqualTo(6));
        Assert.That(Find(document, "Dds.Header.Caps2").DisplayValue, Does.Contain("+X"));
        Assert.That(Find(document, "Dds.Header.Caps2").DisplayValue, Does.Contain("-Z"));
    }

    [Test]
    public void Parse_ExtractsVolumeTextureDepth()
    {
        byte[] bytes = CreateLegacy(32, 16, 8392719, 128, 8, 1, 65, "", 32, 16711680, 65280, 255, 4278190080, 4104, 2097152);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Dds.Header.Depth").Value.UnsignedIntegerValue, Is.EqualTo(8));
        Assert.That(Find(document, "Dds.Header.IsVolume").Value.BooleanValue, Is.True);
        Assert.That(Find(document, "Dds.Header.Caps2").DisplayValue, Does.Contain("Volume"));
    }

    [Test]
    public void Parse_ExtractsDx10CubeArrayAndBc7Format()
    {
        byte[] bytes = CreateDx10(1024, 1024, 98, 3, 4, 2, 1);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories.Select(item => item.Path), Is.EqualTo(new[] { "DDS/Header", "DDS/PixelFormat", "DDS/DX10" }));
        Assert.That(Find(document, "Dds.PixelFormat.FourCC").Value.Text, Is.EqualTo("DX10"));
        Assert.That(Find(document, "Dds.Header.DataOffset").Value.IntegerValue, Is.EqualTo(148));
        Assert.That(Find(document, "Dds.DX10.DxgiFormat").Value.UnsignedIntegerValue, Is.EqualTo(98));
        Assert.That(Find(document, "Dds.DX10.DxgiFormat").DisplayValue, Is.EqualTo("DXGI_FORMAT_BC7_UNORM"));
        Assert.That(Find(document, "Dds.DX10.ResourceDimension").DisplayValue, Is.EqualTo("Texture 2D"));
        Assert.That(Find(document, "Dds.DX10.ArraySize").Value.UnsignedIntegerValue, Is.EqualTo(2));
        Assert.That(Find(document, "Dds.DX10.AlphaMode").DisplayValue, Is.EqualTo("Straight"));
        Assert.That(Find(document, "Dds.DX10.IsCubeMap").Value.BooleanValue, Is.True);
        Assert.That(Find(document, "Dds.DX10.SurfaceCount").Value.UnsignedIntegerValue, Is.EqualTo(12));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Unknown));
        Assert.That(document.RawBlocks.Select(item => item.BlockType), Is.EqualTo(new[] { "Header", "DX10Header" }));
    }

    [Test]
    public void Parse_Dx10OpaqueModeSuppressesFormatAlpha()
    {
        byte[] bytes = CreateDx10(80, 40, 28, 3, 0, 1, 3);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Dds.DX10.AlphaMode").DisplayValue, Is.EqualTo("Opaque"));
        Assert.That(document.Frames[0].HasAlpha, Is.False);
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
    }

    [Test]
    public void Parse_Dx10SingleChannelFormatIsGrayscale()
    {
        byte[] bytes = CreateDx10(80, 40, 61, 3, 0, 1, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Dds.DX10.DxgiFormat").DisplayValue, Is.EqualTo("DXGI_FORMAT_R8_UNORM"));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(1));
        Assert.That(document.Frames[0].HasAlpha, Is.False);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Gray));
    }

    [Test]
    public void Parse_NumericLegacyFloatingPointFormatIsNamed()
    {
        byte[] bytes = CreateLegacy(16, 16, 4103, 0, 0, 1, 4, "", 0, 0, 0, 0, 0, 4096, 0);
        WriteUnsigned32(bytes, 84, 116);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Dds.PixelFormat.FourCC").Value.Text, Is.Empty);
        Assert.That(Find(document, "Dds.PixelFormat.FourCC").DisplayValue, Is.EqualTo("A32B32G32R32F"));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(32));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
    }

    [Test]
    public void Parse_TruncatedBaseHeaderReportsDiagnostic()
    {
        byte[] bytes = new byte[] { 68, 68, 83, 32, 124, 0, 0, 0 };

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "dds.header.truncated"), Is.True);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_InvalidMagicReportsDiagnostic()
    {
        byte[] bytes = CreateLegacy(16, 16, 4111, 64, 0, 0, 65, "", 32, 16711680, 65280, 255, 4278190080, 4096, 0);
        bytes[0] = 0;

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "dds.magic.invalid"), Is.True);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_InvalidStructureSizesAndDimensionsDoNotCreateFrame()
    {
        byte[] bytes = CreateLegacy(0, 32, 4111, 0, 0, 0, 65, "", 32, 16711680, 65280, 255, 4278190080, 4096, 0);
        WriteUnsigned32(bytes, 4, 120);
        WriteUnsigned32(bytes, 76, 28);

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "dds.header.size.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "dds.pixel-format.size.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "dds.dimensions.invalid"), Is.True);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_TruncatedDx10HeaderPreservesBaseFrame()
    {
        byte[] bytes = CreateLegacy(48, 24, 4103, 0, 0, 1, 4, "DX10", 0, 0, 0, 0, 0, 4096, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "dds.dx10.truncated"), Is.True);
        Assert.That(document.Directories.Select(item => item.Path), Is.EqualTo(new[] { "DDS/Header", "DDS/PixelFormat" }));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(48));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(0));
    }

    [Test]
    public void Parse_InvalidDx10SemanticsAreDiagnosedIndependently()
    {
        byte[] bytes = CreateDx10(64, 32, 28, 4, 4, 2, 13);
        WriteUnsigned32(bytes, 24, 0);
        WriteUnsigned32(bytes, 112, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "dds.dx10.texture3d.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "dds.dx10.cubemap.dimension"), Is.True);
        Assert.That(HasDiagnostic(document, "dds.dx10.alpha-mode.unknown"), Is.True);
        Assert.That(HasDiagnostic(document, "dds.dx10.misc-flags2.reserved"), Is.True);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_HeaderConflictsAndMissingCubeFacesWarn()
    {
        byte[] bytes = CreateLegacy(32, 32, 528399, 2048, 0, 0, 65, "", 32, 16711680, 65280, 255, 4278190080, 4096, 512);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "dds.pitch-linear-size.conflict"), Is.True);
        Assert.That(HasDiagnostic(document, "dds.cubemap.faces.missing"), Is.True);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_RawHeaderCaptureHonorsOptionsAndLimit()
    {
        byte[] bytes = CreateDx10(32, 32, 28, 3, 0, 1, 1);
        ImageMetadataReaderOptions captured = new ImageMetadataReaderOptions();
        captured.CaptureRawBlockData = true;

        ImageMetadataDocument capturedDocument = ParseDirect(bytes, captured);

        Assert.That(capturedDocument.RawBlocks.Single(item => item.BlockType == "Header").Data, Is.EqualTo(bytes.Take(128).ToArray()));
        Assert.That(capturedDocument.RawBlocks.Single(item => item.BlockType == "DX10Header").Data, Is.EqualTo(bytes.Skip(128).Take(20).ToArray()));

        ImageMetadataReaderOptions limited = new ImageMetadataReaderOptions();
        limited.CaptureRawBlockData = true;
        limited.MaxRawBlockBytes = 64;
        ImageMetadataDocument limitedDocument = ParseDirect(bytes, limited);
        Assert.That(limitedDocument.RawBlocks.Single(item => item.BlockType == "Header").Data, Is.Empty);
        Assert.That(limitedDocument.RawBlocks.Single(item => item.BlockType == "DX10Header").Data.Length, Is.EqualTo(20));
        Assert.That(HasDiagnostic(limitedDocument, "dds.raw.block.limit"), Is.True);

        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.CaptureRawBlocks = false;
        Assert.That(ParseDirect(bytes, disabled).RawBlocks, Is.Empty);
    }

    public static byte[] CreateLegacy(int width, int height, long flags, long pitch, int depth, int mipMaps, long pixelFlags, string fourCc, int rgbBits, long redMask, long greenMask, long blueMask, long alphaMask, long caps, long caps2)
    {
        byte[] bytes = new byte[128];
        bytes[0] = 68;
        bytes[1] = 68;
        bytes[2] = 83;
        bytes[3] = 32;
        WriteUnsigned32(bytes, 4, 124);
        WriteUnsigned32(bytes, 8, flags);
        WriteUnsigned32(bytes, 12, height);
        WriteUnsigned32(bytes, 16, width);
        WriteUnsigned32(bytes, 20, pitch);
        WriteUnsigned32(bytes, 24, depth);
        WriteUnsigned32(bytes, 28, mipMaps);
        WriteUnsigned32(bytes, 76, 32);
        WriteUnsigned32(bytes, 80, pixelFlags);
        WriteFourCc(bytes, 84, fourCc);
        WriteUnsigned32(bytes, 88, rgbBits);
        WriteUnsigned32(bytes, 92, redMask);
        WriteUnsigned32(bytes, 96, greenMask);
        WriteUnsigned32(bytes, 100, blueMask);
        WriteUnsigned32(bytes, 104, alphaMask);
        WriteUnsigned32(bytes, 108, caps);
        WriteUnsigned32(bytes, 112, caps2);
        return bytes;
    }

    public static byte[] CreateDx10(int width, int height, int dxgiFormat, int resourceDimension, int miscFlag, int arraySize, int miscFlags2)
    {
        byte[] baseBytes = CreateLegacy(width, height, 528391, 0, resourceDimension == 4 ? 8 : 0, 1, 4, "DX10", 0, 0, 0, 0, 0, 4096, resourceDimension == 4 ? 2097152 : 0);
        byte[] bytes = new byte[148];
        for (int index = 0; index < baseBytes.Length; index++)
        {
            bytes[index] = baseBytes[index];
        }
        WriteUnsigned32(bytes, 128, dxgiFormat);
        WriteUnsigned32(bytes, 132, resourceDimension);
        WriteUnsigned32(bytes, 136, miscFlag);
        WriteUnsigned32(bytes, 140, arraySize);
        WriteUnsigned32(bytes, 144, miscFlags2);
        return bytes;
    }

    public static void WriteFourCc(byte[] bytes, int offset, string value)
    {
        int count = value.Length < 4 ? value.Length : 4;
        for (int index = 0; index < count; index++)
        {
            bytes[offset + index] = (byte)value[index];
        }
    }

    public static void WriteUnsigned32(byte[] bytes, int offset, long value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    public static ImageMetadataDocument ParseDirect(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return DdsMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Dds), options);
    }

    public static MetadataTag Find(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key);
    }

    public static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        return document.Diagnostics.Any(item => item.Code == code);
    }
}
