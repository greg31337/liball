using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PublicApiStateInvariantTests
{
    [Test]
    public void PublicClasses_HaveOneUsableParameterlessConstructor()
    {
        Type[] types = GetPublicClasses();

        Assert.Multiple(() =>
        {
            foreach (Type type in types)
            {
                ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                Assert.That(constructors, Has.Length.EqualTo(1), type.FullName);
                Assert.That(constructors[0].GetParameters(), Is.Empty, type.FullName);
                Assert.That(Activator.CreateInstance(type), Is.Not.Null, type.FullName);
            }
        });
    }

    [Test]
    public void PublicDataFields_HaveCompleteDocumentedDefaults()
    {
        Dictionary<string, object?> overrides = CreateDefaultOverrides();
        HashSet<string> visitedOverrides = new HashSet<string>();
        Type[] types = GetPublicClasses();

        Assert.Multiple(() =>
        {
            foreach (Type type in types)
            {
                object instance = Activator.CreateInstance(type)!;
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    string key = type.Name + "." + field.Name;
                    object? actual = field.GetValue(instance);
                    if (overrides.TryGetValue(key, out object? expected))
                    {
                        visitedOverrides.Add(key);
                        Assert.That(actual, Is.EqualTo(expected), key);
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        Assert.That(actual, Is.EqualTo(""), key);
                    }
                    else if (field.FieldType.IsArray)
                    {
                        Assert.That(actual, Is.Not.Null, key);
                        Assert.That((Array)actual!, Is.Empty, key);
                    }
                    else if (field.FieldType.IsValueType)
                    {
                        Assert.That(actual, Is.EqualTo(Activator.CreateInstance(field.FieldType)), key);
                    }
                    else
                    {
                        Assert.That(actual, Is.Not.Null, key);
                    }
                }
            }

            Assert.That(visitedOverrides, Is.EquivalentTo(overrides.Keys));
        });
    }

    [Test]
    public void MutableDefaults_AreIndependentAcrossEveryPublicDataModel()
    {
        Type[] types = GetPublicClasses();

        Assert.Multiple(() =>
        {
            foreach (Type type in types)
            {
                object first = Activator.CreateInstance(type)!;
                object second = Activator.CreateInstance(type)!;
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType.IsValueType || field.FieldType == typeof(string))
                    {
                        continue;
                    }

                    object? firstValue = field.GetValue(first);
                    object? secondValue = field.GetValue(second);
                    if (firstValue != null || secondValue != null)
                    {
                        Assert.That(firstValue, Is.Not.SameAs(secondValue), type.Name + "." + field.Name);
                    }
                }
            }
        });
    }

    [Test]
    public void SecurityLimits_ExposeEveryConstantAsItsFieldDefault()
    {
        PngSecurityLimits limits = new PngSecurityLimits();

        Assert.Multiple(() =>
        {
            Assert.That(limits.MaxWidth, Is.EqualTo(PngSecurityLimits.DefaultMaxWidth));
            Assert.That(limits.MaxHeight, Is.EqualTo(PngSecurityLimits.DefaultMaxHeight));
            Assert.That(limits.MaxPixelCount, Is.EqualTo(PngSecurityLimits.DefaultMaxPixelCount));
            Assert.That(limits.MaxRowBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxRowBytes));
            Assert.That(limits.MaxChunkCount, Is.EqualTo(PngSecurityLimits.DefaultMaxChunkCount));
            Assert.That(limits.MaxMetadataBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxMetadataBytes));
            Assert.That(limits.MaxTextBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxTextBytes));
            Assert.That(limits.MaxTextEntries, Is.EqualTo(PngSecurityLimits.DefaultMaxTextEntries));
            Assert.That(limits.MaxIdatBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxIdatBytes));
            Assert.That(limits.MaxDecompressedBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxDecompressedBytes));
            Assert.That(limits.MaxAnimationFrames, Is.EqualTo(PngSecurityLimits.DefaultMaxAnimationFrames));
            Assert.That(limits.MaxFrameBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxFrameBytes));
            Assert.That(limits.MaxAnimationBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxAnimationBytes));
        });
    }

    [Test]
    public void PublicEnums_ExposeEverySupportedNameAndValue()
    {
        AssertEnum(typeof(PngColorType), new string[] { "Grayscale", "Truecolor", "IndexedColor", "GrayscaleAlpha", "TruecolorAlpha" }, new int[] { 0, 2, 3, 4, 6 });
        AssertEnum(typeof(PngBitDepth), new string[] { "One", "Two", "Four", "Eight", "Sixteen" }, new int[] { 1, 2, 4, 8, 16 });
        AssertEnum(typeof(PngCompressionMethod), new string[] { "Deflate" }, new int[] { 0 });
        AssertEnum(typeof(PngFilterMethod), new string[] { "Adaptive" }, new int[] { 0 });
        AssertEnum(typeof(PngInterlaceMethod), new string[] { "None", "Adam7" }, new int[] { 0, 1 });
        AssertEnum(typeof(PngFilterType), new string[] { "None", "Sub", "Up", "Average", "Paeth" }, new int[] { 0, 1, 2, 3, 4 });
        AssertEnum(typeof(PngRowFilterStrategy), new string[] { "Adaptive", "Fast", "Fixed", "Exhaustive" }, new int[] { 0, 1, 2, 3 });
        AssertEnum(typeof(PngChunkDisposition), new string[] { "Preserve", "Drop", "Replace" }, new int[] { 0, 1, 2 });
        AssertEnum(typeof(PngChunkPlacement), new string[] { "Unspecified", "BeforePalette", "BeforeImageData", "ImageData", "AfterImageData" }, new int[] { 0, 1, 2, 3, 4 });
        AssertEnum(typeof(PngChunkPreservationState), new string[] { "Created", "Preserved" }, new int[] { 0, 1 });
        AssertEnum(typeof(PngChunkPlacementPolicy), new string[] { "Preserve", "Canonicalize" }, new int[] { 0, 1 });
        AssertEnum(typeof(PngMetadataPolicy), new string[] { "Preserve", "Strip", "KeepSelected", "RemoveSelected" }, new int[] { 0, 1, 2, 3 });
        AssertEnum(typeof(PngTextCompressionMode), new string[] { "Preserve", "Uncompressed", "Compressed", "Automatic" }, new int[] { 0, 1, 2, 3 });
        AssertEnum(typeof(PngTextKind), new string[] { "Latin1", "CompressedLatin1", "InternationalUtf8" }, new int[] { 0, 1, 2 });
        AssertEnum(typeof(PngErrorPolicy), new string[] { "Strict", "TolerantMetadata", "TolerantRepair", "BestEffortInspection" }, new int[] { 0, 1, 2, 3 });
        AssertEnum(typeof(PngPixelFormat), new string[] { "Unknown", "Grayscale1", "Grayscale2", "Grayscale4", "Grayscale8", "Grayscale16", "Indexed1", "Indexed2", "Indexed4", "Indexed8", "Rgb8", "Rgb16", "GrayscaleAlpha8", "GrayscaleAlpha16", "Rgba8", "Rgba16", "Bgr8", "Bgr16", "Bgra8", "Bgra16", "Argb8", "Argb16" }, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 });
        AssertEnum(typeof(PngApngBlendOperation), new string[] { "Source", "Over" }, new int[] { 0, 1 });
        AssertEnum(typeof(PngApngDisposeOperation), new string[] { "None", "Background", "Previous" }, new int[] { 0, 1, 2 });
    }

    static Type[] GetPublicClasses()
    {
        return typeof(PngImage).Assembly.GetExportedTypes()
            .Where(type => type.IsClass && type.Namespace == "png")
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    static Dictionary<string, object?> CreateDefaultOverrides()
    {
        return new Dictionary<string, object?>
        {
            ["PngSecurityLimits.MaxWidth"] = PngSecurityLimits.DefaultMaxWidth,
            ["PngSecurityLimits.MaxHeight"] = PngSecurityLimits.DefaultMaxHeight,
            ["PngSecurityLimits.MaxPixelCount"] = PngSecurityLimits.DefaultMaxPixelCount,
            ["PngSecurityLimits.MaxRowBytes"] = PngSecurityLimits.DefaultMaxRowBytes,
            ["PngSecurityLimits.MaxChunkCount"] = PngSecurityLimits.DefaultMaxChunkCount,
            ["PngSecurityLimits.MaxMetadataBytes"] = PngSecurityLimits.DefaultMaxMetadataBytes,
            ["PngSecurityLimits.MaxTextBytes"] = PngSecurityLimits.DefaultMaxTextBytes,
            ["PngSecurityLimits.MaxTextEntries"] = PngSecurityLimits.DefaultMaxTextEntries,
            ["PngSecurityLimits.MaxIdatBytes"] = PngSecurityLimits.DefaultMaxIdatBytes,
            ["PngSecurityLimits.MaxDecompressedBytes"] = PngSecurityLimits.DefaultMaxDecompressedBytes,
            ["PngSecurityLimits.MaxAnimationFrames"] = PngSecurityLimits.DefaultMaxAnimationFrames,
            ["PngSecurityLimits.MaxFrameBytes"] = PngSecurityLimits.DefaultMaxFrameBytes,
            ["PngSecurityLimits.MaxAnimationBytes"] = PngSecurityLimits.DefaultMaxAnimationBytes,
            ["PngPixelConversionOptions.TargetFormat"] = PngPixelFormat.Rgba8,
            ["PngPixelConversionOptions.Palette"] = null,
            ["PngPixelConversionOptions.IndexedTransparency"] = null,
            ["PngPixelConversionOptions.GrayscaleTransparency"] = null,
            ["PngPixelConversionOptions.TruecolorTransparency"] = null,
            ["PngDecoderOptions.PreferredPixelFormat"] = PngPixelFormat.Rgba8,
            ["PngDecoderOptions.ValidateCrc"] = true,
            ["PngDecoderOptions.PreserveUnknownChunks"] = true,
            ["PngDecoderOptions.DecodeAnimation"] = true,
            ["PngEncoderOptions.ColorType"] = PngColorType.TruecolorAlpha,
            ["PngEncoderOptions.BitDepth"] = PngBitDepth.Eight,
            ["PngEncoderOptions.TextCompressionThreshold"] = 1024,
            ["PngEncoderOptions.PixelDataChanged"] = true,
            ["PngEncoderOptions.CompressionLevel"] = 9,
            ["PngEncoderOptions.UseAutomaticColorType"] = true,
            ["PngEncoderOptions.UseAutomaticBitDepth"] = true,
            ["PngEncoderOptions.AllowPaletteReduction"] = true,
            ["PngOptimizerOptions.TextCompressionThreshold"] = 1024,
            ["PngOptimizerOptions.PixelDataChanged"] = true,
            ["PngOptimizerOptions.RowFilterStrategy"] = PngRowFilterStrategy.Exhaustive,
            ["PngOptimizerOptions.MaximumCompressionLevel"] = 9,
            ["PngOptimizerOptions.TryFilters"] = true,
            ["PngOptimizerOptions.TryCompressionLevels"] = true,
            ["PngOptimizerOptions.AllowColorTypeReduction"] = true,
            ["PngOptimizerOptions.AllowBitDepthReduction"] = true,
            ["PngOptimizerOptions.AllowPaletteReduction"] = true,
            ["PngOptimizerOptions.UseEarlyBailout"] = true,
            ["PngColorNormalizationOptions.ApplySignificantBits"] = true,
            ["PngColorNormalizationOptions.Background"] = null,
            ["PngTextEntry.OriginalChunkIndex"] = -1,
            ["PngMetadata.IndexedTransparency"] = null,
            ["PngMetadata.GrayscaleTransparency"] = null,
            ["PngMetadata.TruecolorTransparency"] = null,
            ["PngMetadata.Chromaticities"] = null,
            ["PngMetadata.Gamma"] = null,
            ["PngMetadata.IccProfile"] = null,
            ["PngMetadata.SignificantBits"] = null,
            ["PngMetadata.StandardRgb"] = null,
            ["PngMetadata.CodingIndependentCodePoints"] = null,
            ["PngMetadata.MasteringDisplayColorVolume"] = null,
            ["PngMetadata.ContentLightLevelInformation"] = null,
            ["PngMetadata.BackgroundColor"] = null,
            ["PngMetadata.PaletteHistogram"] = null,
            ["PngMetadata.PhysicalPixels"] = null,
            ["PngMetadata.ExifProfile"] = null,
            ["PngMetadata.Time"] = null,
            ["PngMetadata.AnimationControl"] = null
        };
    }

    static void AssertEnum(Type type, string[] expectedNames, int[] expectedValues)
    {
        string[] actualNames = Enum.GetNames(type);
        Array values = Enum.GetValues(type);
        int[] actualValues = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            actualValues[index] = Convert.ToInt32(values.GetValue(index));
            index += 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(actualNames, Is.EqualTo(expectedNames), type.Name);
            Assert.That(actualValues, Is.EqualTo(expectedValues), type.Name);
        });
    }
}
