using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifEnumTests
{
    [Test]
    public void PublicEnums_ArePresentAndUseGifPrefix()
    {
        Type[] enumTypes = ExpectedEnumTypes();

        foreach (Type enumType in enumTypes)
        {
            Assert.That(enumType.IsEnum, Is.True, enumType.Name + " should be an enum.");
            Assert.That(enumType.IsPublic, Is.True, enumType.Name + " should be public.");
            Assert.That(enumType.Name.StartsWith("Gif", StringComparison.Ordinal), Is.True, enumType.Name + " should use the Gif prefix.");
        }
    }

    [Test]
    public void GifVersion_UsesStableSpecificationValues()
    {
        Assert.That((int)GifVersion.Unknown, Is.EqualTo(0));
        Assert.That((int)GifVersion.Gif87a, Is.EqualTo(87));
        Assert.That((int)GifVersion.Gif89a, Is.EqualTo(89));
    }

    [Test]
    public void GifDisposalMethod_PreservesEncodedValuesIncludingReservedValues()
    {
        Assert.That((int)GifDisposalMethod.Unspecified, Is.EqualTo(0));
        Assert.That((int)GifDisposalMethod.DoNotDispose, Is.EqualTo(1));
        Assert.That((int)GifDisposalMethod.RestoreToBackground, Is.EqualTo(2));
        Assert.That((int)GifDisposalMethod.RestoreToPrevious, Is.EqualTo(3));
        Assert.That((int)GifDisposalMethod.Reserved4, Is.EqualTo(4));
        Assert.That((int)GifDisposalMethod.Reserved5, Is.EqualTo(5));
        Assert.That((int)GifDisposalMethod.Reserved6, Is.EqualTo(6));
        Assert.That((int)GifDisposalMethod.Reserved7, Is.EqualTo(7));
    }

    [Test]
    public void ModeEnums_HaveConservativeZeroDefaults()
    {
        Assert.That((int)GifStrictness.Normal, Is.EqualTo(0));
        Assert.That((int)GifColorTableMode.Auto, Is.EqualTo(0));
        Assert.That((int)GifQuantizerMode.Exact, Is.EqualTo(0));
        Assert.That((int)GifDitherMode.None, Is.EqualTo(0));
        Assert.That((int)GifOptimizationMode.None, Is.EqualTo(0));
        Assert.That((int)GifBackgroundMode.LogicalScreen, Is.EqualTo(0));
        Assert.That((int)GifTransparentColorMode.None, Is.EqualTo(0));
        Assert.That((int)GifLzwClearPolicy.ClearOnFullDictionary, Is.EqualTo(0));
        Assert.That((int)GifErrorCode.None, Is.EqualTo(0));
        Assert.That((int)GifWarningCode.None, Is.EqualTo(0));
    }

    [Test]
    public void DiagnosticEnums_ExposeInitialErrorAndWarningCategories()
    {
        Assert.That(Enum.GetName(typeof(GifErrorCode), GifErrorCode.InvalidSignature), Is.EqualTo("InvalidSignature"));
        Assert.That(Enum.GetName(typeof(GifErrorCode), GifErrorCode.InvalidLzwCodeStream), Is.EqualTo("InvalidLzwCodeStream"));
        Assert.That(Enum.GetName(typeof(GifErrorCode), GifErrorCode.MemoryLimitExceeded), Is.EqualTo("MemoryLimitExceeded"));
        Assert.That(Enum.GetName(typeof(GifWarningCode), GifWarningCode.ReservedDisposalMethod), Is.EqualTo("ReservedDisposalMethod"));
        Assert.That(Enum.GetName(typeof(GifWarningCode), GifWarningCode.ToleratedMalformedBlock), Is.EqualTo("ToleratedMalformedBlock"));
    }

    [Test]
    public void EveryPublicEnumMember_UsesItsStableNumericValue()
    {
        Assert.That(NumericValues<GifVersion>(), Is.EqualTo(new int[] { 0, 87, 89 }));
        Assert.That(NumericValues<GifDisposalMethod>(), Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
        Assert.That(NumericValues<GifStrictness>(), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(NumericValues<GifPlainTextExtensionPolicy>(), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(NumericValues<GifDelayRoundingMode>(), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(NumericValues<GifColorTableMode>(), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(NumericValues<GifQuantizerMode>(), Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5 }));
        Assert.That(NumericValues<GifDitherMode>(), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(NumericValues<GifOptimizationMode>(), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        Assert.That(NumericValues<GifRawBlockKind>(), Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5, 6 }));
        Assert.That(NumericValues<GifRawBlockScope>(), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        Assert.That(NumericValues<GifBackgroundMode>(), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(NumericValues<GifTransparentColorMode>(), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        Assert.That(NumericValues<GifLzwClearPolicy>(), Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(NumericValues<GifErrorCode>(), Is.EqualTo(Sequence(30)));
        Assert.That(NumericValues<GifWarningCode>(), Is.EqualTo(Sequence(8)));
    }

    static Type[] ExpectedEnumTypes()
    {
        return new Type[]
        {
            typeof(GifVersion),
            typeof(GifDisposalMethod),
            typeof(GifStrictness),
            typeof(GifPlainTextExtensionPolicy),
            typeof(GifDelayRoundingMode),
            typeof(GifColorTableMode),
            typeof(GifQuantizerMode),
            typeof(GifDitherMode),
            typeof(GifOptimizationMode),
            typeof(GifRawBlockKind),
            typeof(GifRawBlockScope),
            typeof(GifBackgroundMode),
            typeof(GifTransparentColorMode),
            typeof(GifLzwClearPolicy),
            typeof(GifErrorCode),
            typeof(GifWarningCode)
        };
    }

    static int[] NumericValues<T>() where T : struct, Enum
    {
        T[] values = Enum.GetValues<T>();
        int[] numericValues = new int[values.Length];

        for (int index = 0; index < values.Length; index++)
        {
            numericValues[index] = Convert.ToInt32(values[index]);
        }

        return numericValues;
    }

    static int[] Sequence(int count)
    {
        int[] values = new int[count];
        for (int index = 0; index < count; index++)
        {
            values[index] = index;
        }

        return values;
    }
}
