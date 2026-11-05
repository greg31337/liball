using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class PublicEnumTests
{
    [Test]
    public void SymbolKindsHaveStableValues()
    {
        AssertEnum<QrSymbolKind>(
            new string[] { "Model2", "Micro", "rMQR", "LegacyModel1" },
            new int[] { 0, 1, 2, 3 });
    }

    [Test]
    public void EncodingModesMatchModel2Indicators()
    {
        AssertEnum<QrEncodingMode>(
            new string[]
            {
                "Terminator", "Numeric", "Alphanumeric", "Byte", "Kanji", "Hanzi",
                "Eci", "Fnc1First", "Fnc1Second", "StructuredAppend"
            },
            new int[] { 0x0, 0x1, 0x2, 0x4, 0x8, 0xD, 0x7, 0x5, 0x9, 0x3 });
    }

    [Test]
    public void ErrorCorrectionLevelsHaveStableValues()
    {
        AssertEnum<QrErrorCorrectionLevel>(
            new string[] { "None", "Low", "Medium", "Quartile", "High" },
            new int[] { -1, 0, 1, 2, 3 });
    }

    [Test]
    public void MaskPatternsCoverAutomaticAndAllEightMasks()
    {
        AssertEnum<QrMaskPattern>(
            new string[] { "Automatic", "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven" },
            new int[] { -1, 0, 1, 2, 3, 4, 5, 6, 7 });
    }

    [Test]
    public void PixelFormatsHaveStableValues()
    {
        AssertEnum<QrPixelFormat>(
            new string[] { "Gray8", "Rgb24", "Rgba32", "Bgra32" },
            new int[] { 0, 1, 2, 3 });
    }

    [Test]
    public void BinarizationModesHaveStableValues()
    {
        AssertEnum<QrBinarizationMode>(
            new string[] { "Automatic", "GlobalHistogram", "HybridLocal", "SuppliedBinary" },
            new int[] { 0, 1, 2, 3 });
    }

    [Test]
    public void PolaritiesHaveStableValues()
    {
        AssertEnum<QrPolarity>(
            new string[] { "Automatic", "DarkOnLight", "LightOnDark" },
            new int[] { 0, 1, 2 });
    }

    [Test]
    public void ModuleValuesHaveStableValues()
    {
        AssertEnum<QrModuleValue>(
            new string[] { "Unknown", "Light", "Dark" },
            new int[] { 0, 1, 2 });
    }

    [Test]
    public void ModuleRolesHaveStableValues()
    {
        AssertEnum<QrModuleRole>(
            new string[]
            {
                "Unknown", "QuietZone", "Finder", "Separator", "Timing", "Alignment",
                "Format", "Version", "DarkModule", "Data", "ErrorCorrection", "Remainder"
            },
            new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 });
    }

    [Test]
    public void FailureReasonsHaveStableValues()
    {
        AssertEnum<QrFailureReason>(
            new string[]
            {
                "None", "NotFound", "InvalidInput", "InvalidGeometry", "InvalidFormat",
                "InvalidVersion", "InvalidMode", "InvalidData", "UnsupportedTextEncoding",
                "ChecksumFailure", "CapacityExceeded", "IncompleteSequence",
                "ConflictingSequence", "ResourceLimit"
            },
            new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 });
    }

    [Test]
    public void QualityGradesPreserveNumericBandsAndAnUngradedSentinel()
    {
        AssertEnum<QrQualityGrade>(
            new string[] { "NotGraded", "Grade0", "Grade1", "Grade2", "Grade3", "Grade4" },
            new int[] { -1, 0, 1, 2, 3, 4 });
    }

    private static void AssertEnum<TEnum>(string[] expectedNames, int[] expectedValues)
        where TEnum : struct, Enum
    {
        Assert.That(expectedValues, Has.Length.EqualTo(expectedNames.Length));
        Assert.That(Enum.GetNames<TEnum>(), Is.EquivalentTo(expectedNames));
        for (int index = 0; index < expectedNames.Length; index++)
        {
            TEnum value = Enum.Parse<TEnum>(expectedNames[index]);
            Assert.That(Convert.ToInt32(value), Is.EqualTo(expectedValues[index]), expectedNames[index]);
        }
    }
}
