using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PublicInteroperabilityCorpusTests
{
    [TestCaseSource(nameof(PngSuiteFiles))]
    public void PngSuiteImage_IdentifiesValidatesDecodesAndLosslesslyReencodes(string relativePath)
    {
        string path = Path.Combine(FindRepositoryRoot(), "specs", "pngsuite", relativePath);
        byte[] sourceBytes = File.ReadAllBytes(path);
        IPngIdentifier identifier = new PngIdentifierService();
        IPngValidator validator = new PngValidationService();
        IPngDecoder decoder = new PngCodec();
        IPngEncoder encoder = new PngCodec();

        PngIdentifyResult identified = identifier.IdentifyBytes(sourceBytes);
        PngValidationResult validation = validator.ValidateBytes(sourceBytes);
        PngImage decoded = decoder.DecodeBytes(sourceBytes, CreateNativeDecodeOptions());
        byte[] reencodedBytes = encoder.EncodeToBytes(decoded, CreateLosslessEncoderOptions(identified.Info));
        PngIdentifyResult reidentified = identifier.IdentifyBytes(reencodedBytes);
        PngValidationResult revalidation = validator.ValidateBytes(reencodedBytes);
        PngImage sourceRgba16 = decoder.DecodeBytes(sourceBytes, CreateRgba16DecodeOptions());
        PngImage reencodedRgba16 = decoder.DecodeBytes(reencodedBytes, CreateRgba16DecodeOptions());

        string fileName = Path.GetFileNameWithoutExtension(relativePath);
        Assert.Multiple(() =>
        {
            Assert.That(identified.IsValid, Is.True, DescribeErrors(identified.Errors));
            Assert.That(validation.IsValid, Is.True, DescribeErrors(validation.Errors));
            Assert.That(decoded.Errors, Is.Empty);
            Assert.That(identified.Info.Width, Is.EqualTo(32));
            Assert.That(identified.Info.Height, Is.EqualTo(32));
            Assert.That(identified.Info.ColorType, Is.EqualTo(ExpectedColorType(fileName)));
            Assert.That(identified.Info.BitDepth, Is.EqualTo(ExpectedBitDepth(fileName)));
            Assert.That(identified.Info.InterlaceMethod, Is.EqualTo(ExpectedInterlaceMethod(fileName)));
            Assert.That(decoded.Info.ColorType, Is.EqualTo(identified.Info.ColorType));
            Assert.That(decoded.Info.BitDepth, Is.EqualTo(identified.Info.BitDepth));
            Assert.That(decoded.Info.InterlaceMethod, Is.EqualTo(identified.Info.InterlaceMethod));
            Assert.That(revalidation.IsValid, Is.True, DescribeErrors(revalidation.Errors));
            Assert.That(reidentified.Info.ColorType, Is.EqualTo(identified.Info.ColorType));
            Assert.That(reidentified.Info.BitDepth, Is.EqualTo(identified.Info.BitDepth));
            Assert.That(reidentified.Info.InterlaceMethod, Is.EqualTo(identified.Info.InterlaceMethod));
            Assert.That(reencodedRgba16.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgba16));
            Assert.That(reencodedRgba16.Pixels.Data, Is.EqualTo(sourceRgba16.Pixels.Data));
            Assert.That(GetMetadataChunkTypes(reidentified.Metadata), Is.EqualTo(GetMetadataChunkTypes(identified.Metadata)));
        });
    }

    [Test]
    public void SupplementalApngFixture_IsDeterministicAndInteroperable()
    {
        PngImage source = CreateApngFixture();
        PngCodec codec = new PngCodec();
        PngEncoderOptions encoderOptions = CreateApngEncoderOptions();
        PngDecoderOptions decoderOptions = new PngDecoderOptions();
        decoderOptions.DecodeAnimation = true;
        decoderOptions.PreferredPixelFormat = PngPixelFormat.Rgba8;

        byte[] firstEncoding = codec.EncodeToBytes(source, encoderOptions);
        byte[] secondEncoding = codec.EncodeToBytes(source, CreateApngEncoderOptions());
        PngIdentifyResult identified = new PngIdentifierService().IdentifyBytes(firstEncoding);
        PngValidationResult validation = new PngValidationService().ValidateBytes(firstEncoding);
        PngImage decoded = codec.DecodeBytes(firstEncoding, decoderOptions);
        byte[] reencoded = codec.EncodeToBytes(decoded, CreateApngEncoderOptions());
        PngImage decodedAgain = codec.DecodeBytes(reencoded, decoderOptions);

        Assert.Multiple(() =>
        {
            Assert.That(secondEncoding, Is.EqualTo(firstEncoding));
            Assert.That(validation.IsValid, Is.True, DescribeErrors(validation.Errors));
            Assert.That(identified.IsValid, Is.True, DescribeErrors(identified.Errors));
            Assert.That(identified.Info.IsAnimated, Is.True);
            Assert.That(identified.Info.FrameCount, Is.EqualTo(2));
            Assert.That(identified.Info.PlayCount, Is.EqualTo(4));
            Assert.That(decoded.Animation.Frames, Has.Length.EqualTo(2));
            Assert.That(decoded.Animation.ComposedFrames, Has.Length.EqualTo(2));
            Assert.That(decodedAgain.Animation.Frames, Has.Length.EqualTo(2));
            Assert.That(decodedAgain.Animation.ComposedFrames[0].Data, Is.EqualTo(decoded.Animation.ComposedFrames[0].Data));
            Assert.That(decodedAgain.Animation.ComposedFrames[1].Data, Is.EqualTo(decoded.Animation.ComposedFrames[1].Data));
            Assert.That(new PngValidationService().ValidateBytes(reencoded).IsValid, Is.True);
        });
    }

    [Test]
    public void SupplementalPng3Fixture_IsDeterministicAndPreservesModernColorChunks()
    {
        PngImage source = PngValidFixtures.CreateRgba8Image(3, 2);
        PngMetadataService metadata = new PngMetadataService();
        PngCodingIndependentCodePoints codePoints = new PngCodingIndependentCodePoints();
        codePoints.ColorPrimaries = 9;
        codePoints.TransferCharacteristics = 16;
        codePoints.MatrixCoefficients = 9;
        codePoints.VideoFullRangeFlag = 1;
        PngMasteringDisplayColorVolume display = new PngMasteringDisplayColorVolume();
        display.RedX = 34000;
        display.RedY = 16000;
        display.GreenX = 13250;
        display.GreenY = 34500;
        display.BlueX = 7500;
        display.BlueY = 3000;
        display.WhitePointX = 15635;
        display.WhitePointY = 16450;
        display.MaximumLuminance = 10000000;
        display.MinimumLuminance = 50;
        PngContentLightLevelInformation light = new PngContentLightLevelInformation();
        light.MaximumContentLightLevel = 1000;
        light.MaximumFrameAverageLightLevel = 400;
        source.Metadata = metadata.SetCodingIndependentCodePoints(source.Metadata, codePoints);
        source.Metadata = metadata.SetMasteringDisplayColorVolume(source.Metadata, display);
        source.Metadata = metadata.SetContentLightLevelInformation(source.Metadata, light);
        PngCodec codec = new PngCodec();
        PngEncoderOptions options = CreateRgba8EncoderOptions();

        byte[] firstEncoding = codec.EncodeToBytes(source, options);
        byte[] secondEncoding = codec.EncodeToBytes(source, CreateRgba8EncoderOptions());
        PngIdentifyResult identified = new PngIdentifierService().IdentifyBytes(firstEncoding);
        PngValidationResult validation = new PngValidationService().ValidateBytes(firstEncoding);
        PngImage decoded = codec.DecodeBytes(firstEncoding, CreateNativeDecodeOptions());
        byte[] reencoded = codec.EncodeToBytes(decoded, CreateRgba8EncoderOptions());
        PngIdentifyResult reidentified = new PngIdentifierService().IdentifyBytes(reencoded);

        Assert.Multiple(() =>
        {
            Assert.That(secondEncoding, Is.EqualTo(firstEncoding));
            Assert.That(validation.IsValid, Is.True, DescribeErrors(validation.Errors));
            Assert.That(identified.IsValid, Is.True, DescribeErrors(identified.Errors));
            Assert.That(identified.Metadata.CodingIndependentCodePoints!.ColorPrimaries, Is.EqualTo(9));
            Assert.That(identified.Metadata.CodingIndependentCodePoints.TransferCharacteristics, Is.EqualTo(16));
            Assert.That(identified.Metadata.CodingIndependentCodePoints.MatrixCoefficients, Is.EqualTo(9));
            Assert.That(identified.Metadata.CodingIndependentCodePoints.VideoFullRangeFlag, Is.EqualTo(1));
            Assert.That(identified.Metadata.MasteringDisplayColorVolume!.MaximumLuminance, Is.EqualTo(10000000));
            Assert.That(identified.Metadata.MasteringDisplayColorVolume.MinimumLuminance, Is.EqualTo(50));
            Assert.That(identified.Metadata.ContentLightLevelInformation!.MaximumContentLightLevel, Is.EqualTo(1000));
            Assert.That(identified.Metadata.ContentLightLevelInformation.MaximumFrameAverageLightLevel, Is.EqualTo(400));
            Assert.That(HasChunk(firstEncoding, PngConstants.ChunkCodingIndependentCodePoints), Is.True);
            Assert.That(HasChunk(firstEncoding, PngConstants.ChunkMasteringDisplayColorVolume), Is.True);
            Assert.That(HasChunk(firstEncoding, PngConstants.ChunkContentLightLevelInformation), Is.True);
            Assert.That(reidentified.Metadata.CodingIndependentCodePoints!.ColorPrimaries, Is.EqualTo(9));
            Assert.That(reidentified.Metadata.MasteringDisplayColorVolume!.MaximumLuminance, Is.EqualTo(10000000));
            Assert.That(reidentified.Metadata.ContentLightLevelInformation!.MaximumContentLightLevel, Is.EqualTo(1000));
            Assert.That(codec.DecodeBytes(reencoded, CreateNativeDecodeOptions()).Pixels.Data, Is.EqualTo(decoded.Pixels.Data));
            Assert.That(new PngValidationService().ValidateBytes(reencoded).IsValid, Is.True);
        });
    }

    static IEnumerable<TestCaseData> PngSuiteFiles()
    {
        string corpusDirectory = Path.Combine(FindRepositoryRoot(), "specs", "pngsuite");
        string[] paths = Directory.GetFiles(corpusDirectory, "*.png", SearchOption.AllDirectories);
        Array.Sort(paths, StringComparer.Ordinal);
        int index = 0;
        while (index < paths.Length)
        {
            string relativePath = Path.GetRelativePath(corpusDirectory, paths[index]);
            yield return new TestCaseData(relativePath).SetName("PngSuite_" + relativePath.Replace(Path.DirectorySeparatorChar, '_').Replace('.', '_'));
            index += 1;
        }
    }

    static PngDecoderOptions CreateNativeDecodeOptions()
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Unknown;
        return options;
    }

    static PngDecoderOptions CreateRgba16DecodeOptions()
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Rgba16;
        return options;
    }

    static PngEncoderOptions CreateLosslessEncoderOptions(PngImageInfo info)
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.ColorType = info.ColorType;
        options.BitDepth = info.BitDepth;
        options.InterlaceMethod = info.InterlaceMethod;
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.AllowPaletteReduction = false;
        options.MetadataPolicy = PngMetadataPolicy.Preserve;
        options.UnknownChunkDisposition = PngChunkDisposition.Preserve;
        options.PixelDataChanged = false;
        return options;
    }

    static PngEncoderOptions CreateRgba8EncoderOptions()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Eight;
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.PixelDataChanged = false;
        return options;
    }

    static PngImage CreateApngFixture()
    {
        PngImageFactory factory = new PngImageFactory();
        byte[] canvas = new byte[]
        {
            255, 0, 0, 255, 0, 255, 0, 255,
            0, 0, 255, 255, 255, 255, 255, 255
        };
        PngImage image = factory.CreateImageWithData(2, 2, PngPixelFormat.Rgba8, canvas);
        PngFrame[] frames = new PngFrame[]
        {
            factory.CreateFrameWithData(2, 2, 0, 0, PngPixelFormat.Rgba8, 8, canvas, 1, 10, PngApngDisposeOperation.None, PngApngBlendOperation.Source),
            factory.CreateFrameWithData(1, 1, 1, 0, PngPixelFormat.Rgba8, 4, new byte[] { 255, 255, 0, 128 }, 2, 25, PngApngDisposeOperation.Background, PngApngBlendOperation.Over)
        };
        return factory.CreateAnimationImage(image, 4, true, frames);
    }

    static PngEncoderOptions CreateApngEncoderOptions()
    {
        PngEncoderOptions options = CreateRgba8EncoderOptions();
        options.EncodeAnimation = true;
        options.UseFixedFilter = true;
        options.RowFilterStrategy = PngRowFilterStrategy.Fixed;
        options.FixedFilterType = PngFilterType.None;
        return options;
    }

    static PngColorType ExpectedColorType(string fileName)
    {
        switch (fileName[fileName.Length - 4])
        {
            case '0':
                return PngColorType.Grayscale;
            case '2':
                return PngColorType.Truecolor;
            case '3':
                return PngColorType.IndexedColor;
            case '4':
                return PngColorType.GrayscaleAlpha;
            case '6':
                return PngColorType.TruecolorAlpha;
            default:
                throw new InvalidDataException("Unknown PNGSuite color type in " + fileName + ".");
        }
    }

    static PngBitDepth ExpectedBitDepth(string fileName)
    {
        int depth = int.Parse(fileName.Substring(fileName.Length - 2, 2));
        return (PngBitDepth)depth;
    }

    static PngInterlaceMethod ExpectedInterlaceMethod(string fileName)
    {
        if (fileName == "iftbbn2c16" || fileName == "iftbgn2c16" || fileName == "iftbrn2c08")
        {
            return PngInterlaceMethod.None;
        }

        if (fileName[0] == 'i')
        {
            return PngInterlaceMethod.Adam7;
        }

        return PngInterlaceMethod.None;
    }

    static string[] GetMetadataChunkTypes(PngMetadata metadata)
    {
        string[] result = new string[metadata.Chunks.Length];
        int index = 0;
        while (index < metadata.Chunks.Length)
        {
            result[index] = metadata.Chunks[index].Type;
            index += 1;
        }

        return result;
    }

    static bool HasChunk(byte[] data, string type)
    {
        PngChunk[] chunks = Png.ReadChunks(data).Chunks;
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

    static string DescribeErrors(PngError[] errors)
    {
        List<string> descriptions = new List<string>();
        int index = 0;
        while (index < errors.Length)
        {
            descriptions.Add(errors[index].Code + ":" + errors[index].ChunkType + ":" + errors[index].Message);
            index += 1;
        }

        return string.Join(", ", descriptions);
    }

    static string FindRepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (Directory.Exists(Path.Combine(directory, "specs", "pngsuite")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the pinned specs/pngsuite corpus.");
    }
}
