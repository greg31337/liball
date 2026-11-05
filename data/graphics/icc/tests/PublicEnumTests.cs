using icc;

namespace icc.Tests;

public class PublicEnumTests
{
    [Test]
    public void StableEnumSnapshotIncludesEveryPublicEnum()
    {
        string[] actual = typeof(IccVersionKind).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "icc" && type.IsEnum)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal).ToArray();
        string[] expected = new string[]
        {
            "IccColorDifferenceFormula", "IccColorSpaceKind", "IccCurveKind",
            "IccCurveSegmentKind", "IccEmbeddedFormat",
            "IccEmbeddedOperation", "IccGamutStatus", "IccInspectionDetail",
            "IccIntentFallback", "IccInterpolationKind",
            "IccMeasurementGeometry", "IccMeasurementObserver",
            "IccOperationStatus", "IccPcsEncodingKind", "IccPcsKind",
            "IccProcessingElementKind", "IccProfileClass", "IccReadMode",
            "IccRenderingIntent", "IccScreeningSpotShape",
            "IccStandardIlluminant", "IccTagDataKind",
            "IccTransformDirection", "IccValidationMode",
            "IccValidationSeverity", "IccVersionKind", "IccWriteMode"
        };

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void PublicEnumsHaveStableNamesAndValues()
    {
        Assert.Multiple(() =>
        {
            AssertEnum<IccVersionKind>(
                new[] { "Unknown", "Version2", "Version4", "Version5" },
                new[] { 0, 2, 4, 5 });
            AssertEnum<IccProfileClass>(
                new[] { "Unknown", "Input", "Display", "Output", "DeviceLink", "ColorSpace", "Abstract", "NamedColor", "ColorEncodingSpace", "MultiplexIdentification", "MultiplexLink", "MultiplexVisualization" },
                Sequence(0, 11));
            AssertEnum<IccColorSpaceKind>(
                new[] { "Unknown", "Xyz", "Lab", "Luv", "YCbCr", "Yxy", "Rgb", "Gray", "Hsv", "Hls", "Cmyk", "Cmy", "NamedColor", "DeviceChannels", "ExtendedDevice", "Spectral", "BiSpectral", "Multiplex" },
                Sequence(0, 17));
            AssertEnum<IccPcsKind>(
                new[] { "None", "Xyz", "Lab", "Spectral", "BiSpectral", "Material" },
                Sequence(0, 5));
            AssertEnum<IccRenderingIntent>(
                new[] { "Perceptual", "MediaRelativeColorimetric", "Saturation", "IccAbsoluteColorimetric" },
                Sequence(0, 3));
            AssertEnum<IccTagDataKind>(
                new[] { "Unknown", "Raw", "IntegerArray", "FixedPointArray", "XyzArray", "Signature", "DateTime", "Binary", "Text", "LocalizedText", "Dictionary", "Curve", "Lut", "MultiProcessElements", "Measurement", "ViewingConditions", "Chromaticity", "Cicp", "NamedColors", "Colorants", "ResponseCurves", "ProfileSequence", "AdaptiveGainCurve", "Spectral", "TagArray", "TagStructure", "GamutBoundary", "EmbeddedImage", "Calculator", "CrdInfo", "Screening", "UcrBg", "FloatingPointArray", "SparseMatrixArray", "SpectralViewingConditions", "SpectralRange", "EmbeddedProfile" },
                Sequence(0, 36));
            AssertEnum<IccScreeningSpotShape>(
                new[] { "Unknown", "PrinterDefault", "Round", "Diamond", "Ellipse", "Line", "Square", "Cross" },
                Sequence(0, 7));
            AssertEnum<IccCurveKind>(
                new[] { "Identity", "Gamma", "Sampled", "Parametric", "Segmented", "SingleSampled" },
                Sequence(0, 5));
            AssertEnum<IccCurveSegmentKind>(
                new[] { "Formula", "Sampled" },
                Sequence(0, 1));
            AssertEnum<IccProcessingElementKind>(
                new[] { "Unknown", "CurveSet", "Matrix", "Clut", "BeginAcs", "EndAcs", "Calculator", "ExtendedClut", "SparseMatrix", "TintArray", "ToneMap", "XyzToJab", "JabToXyz", "EmissionClut", "EmissionMatrix", "EmissionObserver", "InverseEmissionMatrix", "ReflectanceClut", "ReflectanceObserver" },
                Sequence(0, 18));
            AssertEnum<IccInterpolationKind>(
                new[] { "Linear", "Multilinear", "Tetrahedral" },
                Sequence(0, 2));
            AssertEnum<IccValidationSeverity>(
                new[] { "Information", "Warning", "Error", "Fatal" },
                Sequence(0, 3));
            AssertEnum<IccValidationMode>(
                new[] { "Tolerant", "Conforming", "SecurityStrict" },
                Sequence(0, 2));
            AssertEnum<IccWriteMode>(
                new[] { "Canonical", "Preservation" },
                Sequence(0, 1));
            AssertEnum<IccReadMode>(
                new[] { "Strict", "Conforming", "Recovery" },
                Sequence(0, 2));
            AssertEnum<IccTransformDirection>(
                new[] { "DeviceToPcs", "PcsToDevice", "DeviceToDevice" },
                Sequence(0, 2));
            AssertEnum<IccIntentFallback>(
                new[] { "None", "AllowColorimetric", "AllowAny" },
                Sequence(0, 2));
            AssertEnum<IccPcsEncodingKind>(
                new[] { "Normalized", "Version2", "Version4", "Version5" },
                Sequence(0, 3));
            AssertEnum<IccColorDifferenceFormula>(
                new[] { "Cie76", "Cie94GraphicArts", "Cie94Textiles", "Ciede2000" },
                Sequence(0, 3));
            AssertEnum<IccInspectionDetail>(
                new[] { "Summary", "Normal", "Detailed", "Raw" },
                Sequence(0, 3));
            AssertEnum<IccGamutStatus>(
                new[] { "Unknown", "Inside", "Outside", "Boundary", "Unsupported" },
                Sequence(0, 4));
            AssertEnum<IccEmbeddedFormat>(
                new[] { "Unknown", "RawIcc", "Jpeg", "Png", "Tiff", "BigTiff", "Dng", "WebP", "Jpeg2000", "IsoBmff", "Avif", "Heif", "Unsupported" },
                Sequence(0, 12));
            AssertEnum<IccEmbeddedOperation>(
                new[] { "Detect", "Enumerate", "Extract", "Insert", "Replace", "Remove" },
                Sequence(0, 5));
            AssertEnum<IccMeasurementObserver>(
                new[] { "Unknown", "Cie1931TwoDegree", "Cie1964TenDegree" },
                Sequence(0, 2));
            AssertEnum<IccMeasurementGeometry>(
                new[] { "Unknown", "Degree0Or45", "Degree0OrDiffuse" },
                Sequence(0, 2));
            AssertEnum<IccStandardIlluminant>(
                new[] { "Unknown", "D50", "D65", "D93", "F2", "D55", "A", "EqualEnergyE", "F8", "BlackBody", "Daylight", "B", "C", "F1", "F3", "F4", "F5", "F6", "F7", "F9", "F10", "F11", "F12" },
                Sequence(0, 22));
            AssertEnum<IccOperationStatus>(
                new[] { "Unspecified", "Success", "InvalidData", "ResourceLimitExceeded", "UnsupportedFeature", "NotFound", "Conflict", "Failed" },
                Sequence(0, 7));
        });
    }

    private static void AssertEnum<T>(string[] expectedNames, int[] expectedValues) where T : struct, Enum
    {
        T[] values = Enum.GetValues<T>();
        int[] actualValues = Array.ConvertAll(values, value => Convert.ToInt32(value));

        Assert.That(Enum.GetNames<T>(), Is.EqualTo(expectedNames), typeof(T).Name + " names");
        Assert.That(actualValues, Is.EqualTo(expectedValues), typeof(T).Name + " values");
    }

    private static int[] Sequence(int first, int last)
    {
        int[] values = new int[last - first + 1];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = first + index;
        }

        return values;
    }
}
