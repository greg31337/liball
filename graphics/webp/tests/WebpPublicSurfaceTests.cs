using System.Reflection;
using webp;

namespace webp.Tests;

[TestFixture]
public class WebpPublicSurfaceTests
{
    [Test]
    public void EveryPublicModelFieldRemainsCallerMutable()
    {
        Type[] modelTypes = new Type[]
        {
            typeof(WebpDiagnostic),
            typeof(WebpValidationResult),
            typeof(WebpNormalizationResult),
            typeof(WebpChunkInfo),
            typeof(WebpUnknownChunk),
            typeof(WebpMetadata),
            typeof(WebpPixelBuffer),
            typeof(WebpPremultipliedPixelBuffer),
            typeof(WebpFrame),
            typeof(WebpFrameEncodeOptions),
            typeof(WebpImage),
            typeof(WebpDecodeTransform),
            typeof(WebpInfo),
            typeof(WebpSecurityLimits),
            typeof(WebpDecodeOptions),
            typeof(WebpEncodeOptions),
            typeof(WebpDecodeResult)
        };

        int typeIndex = 0;
        while (typeIndex < modelTypes.Length)
        {
            FieldInfo[] fields = modelTypes[typeIndex].GetFields(BindingFlags.Instance | BindingFlags.Public);
            Assert.That(fields, Is.Not.Empty, modelTypes[typeIndex].Name);
            int fieldIndex = 0;
            while (fieldIndex < fields.Length)
            {
                Assert.That(fields[fieldIndex].IsInitOnly, Is.False, modelTypes[typeIndex].Name + "." + fields[fieldIndex].Name);
                Assert.That(fields[fieldIndex].IsLiteral, Is.False, modelTypes[typeIndex].Name + "." + fields[fieldIndex].Name);
                fieldIndex = fieldIndex + 1;
            }

            Assert.That(modelTypes[typeIndex].GetConstructors(), Has.Length.EqualTo(1), modelTypes[typeIndex].Name);
            typeIndex = typeIndex + 1;
        }
    }

    [Test]
    public void PublicEnumsExposeTheirDefinedNamesAndIntegerValues()
    {
        AssertEnum(typeof(WebpDiagnosticSeverity), new string[] { "Info", "Warning", "Error" }, new int[] { 0, 1, 2 });
        AssertEnum(typeof(WebpStrictness), new string[] { "Permissive", "Strict" }, new int[] { 0, 1 });
        AssertEnum(typeof(WebpContainerKind), new string[] { "Unknown", "SimpleLossy", "SimpleLossless", "ExtendedStill", "ExtendedAnimation" }, new int[] { 0, 1, 2, 3, 4 });
        AssertEnum(typeof(WebpVp8XFeatureFlags), new string[] { "None", "Animation", "Xmp", "Exif", "Alpha", "IccProfile" }, new int[] { 0, 2, 4, 8, 16, 32 });
        AssertEnum(typeof(WebpBitstreamKind), new string[] { "Unknown", "Lossy", "Lossless" }, new int[] { 0, 1, 2 });
        AssertEnum(typeof(WebpEncodeMode), new string[] { "Automatic", "Lossy", "Lossless", "NearLossless" }, new int[] { 0, 1, 2, 3 });
        AssertEnum(typeof(WebpLossyPreset), new string[] { "Default", "Photo", "Picture", "Drawing", "Icon", "Text" }, new int[] { 0, 1, 2, 3, 4, 5 });
        AssertEnum(typeof(WebpLossyPreprocessing), new string[] { "None", "SegmentSmooth", "Dithering", "SegmentSmoothAndDithering" }, new int[] { 0, 1, 2, 3 });
        AssertEnum(typeof(WebpAlphaCompressionMethod), new string[] { "None", "Lossless" }, new int[] { 0, 1 });
        AssertEnum(typeof(WebpAlphaFilterMode), new string[] { "None", "Fast", "Best" }, new int[] { 0, 1, 2 });
        AssertEnum(typeof(WebpLossyAlphaMode), new string[] { "Preserve", "Discard", "BlendWithBackground" }, new int[] { 0, 1, 2 });
        AssertEnum(typeof(WebpMetadataCopyPolicy), new string[] { "None", "All", "Icc", "Exif", "Xmp", "Selected" }, new int[] { 0, 1, 2, 3, 4, 5 });
        AssertEnum(typeof(WebpUnknownChunkPolicy), new string[] { "Preserve", "Strip" }, new int[] { 0, 1 });
        AssertEnum(typeof(WebpUnknownChunkPlacement), new string[] { "BeforeRecognizedChunks", "AfterRecognizedChunks" }, new int[] { 0, 1 });
        AssertEnum(typeof(WebpFrameBlendMode), new string[] { "AlphaBlend", "Replace" }, new int[] { 0, 1 });
        AssertEnum(typeof(WebpFrameDisposeMode), new string[] { "DoNotDispose", "DisposeToBackground" }, new int[] { 0, 1 });
        AssertEnum(typeof(WebpAnimationDecodeMode), new string[] { "IndependentFrames", "CompositedFrames" }, new int[] { 0, 1 });
        AssertEnum(typeof(WebpAspectRatioMode), new string[] { "Stretch", "Preserve" }, new int[] { 0, 1 });
    }

    [Test]
    public void PublicContractsReportMalformedContentButRejectInvalidArguments()
    {
        WebpCodec codec = new WebpCodec();
        IWebpDecoder decoder = codec;
        IWebpInfoReader infoReader = codec;
        IWebpValidator validator = codec;
        IWebpPixelUtilities pixelUtilities = codec;
        IWebpMux mux = codec;
        byte[] malformed = new byte[] { 1, 2, 3 };
        WebpDecodeOptions options = new WebpDecodeOptions();

        Assert.That(decoder.Decode(malformed, options).IsValid, Is.False);
        Assert.That(infoReader.Read(malformed, options).Diagnostics, Is.Not.Empty);
        Assert.That(validator.Validate(malformed, options).IsValid, Is.False);
        Assert.Throws<ArgumentException>(() => mux.GetMetadata(malformed));
        Assert.Throws<ArgumentNullException>(() => decoder.Decode(null!, options));
        Assert.Throws<ArgumentNullException>(() => decoder.Decode(malformed, null!));
        Assert.Throws<ArgumentNullException>(() => infoReader.Read(null!, options));
        Assert.Throws<ArgumentNullException>(() => validator.Validate(null!, options));
        Assert.Throws<ArgumentNullException>(() => pixelUtilities.CopyRgbaToBgra(null!));
        Assert.Throws<ArgumentNullException>(() => pixelUtilities.FlattenAnimationFrames(null!));
        Assert.Throws<ArgumentNullException>(() => mux.GetMetadata(null!));
        Assert.Throws<ArgumentNullException>(() => mux.ReplaceMetadata(malformed, null!));
        Assert.Throws<ArgumentNullException>(() => mux.ReplaceUnknownChunks(malformed, null!, WebpUnknownChunkPlacement.AfterRecognizedChunks));
    }

    static void AssertEnum(Type enumType, string[] expectedNames, int[] expectedValues)
    {
        string[] names = Enum.GetNames(enumType);
        Array values = Enum.GetValues(enumType);
        int[] integerValues = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            object? value = values.GetValue(index);
            integerValues[index] = Convert.ToInt32(value);
            index = index + 1;
        }

        Assert.That(names, Is.EqualTo(expectedNames), enumType.Name);
        Assert.That(integerValues, Is.EqualTo(expectedValues), enumType.Name);
    }
}
