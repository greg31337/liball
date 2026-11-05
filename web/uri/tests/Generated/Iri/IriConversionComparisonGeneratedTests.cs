using System.Globalization;
using System.Text;

namespace uri.Tests;

public class IriConversionComparisonGeneratedTests
{
    private static readonly int[][] UcsCharacterRanges = new int[][]
    {
        new int[] { 0x00A0, 0xD7FF },
        new int[] { 0xF900, 0xFDCF },
        new int[] { 0xFDF0, 0xFFEF },
        new int[] { 0x10000, 0x1FFFD },
        new int[] { 0x20000, 0x2FFFD },
        new int[] { 0x30000, 0x3FFFD },
        new int[] { 0x40000, 0x4FFFD },
        new int[] { 0x50000, 0x5FFFD },
        new int[] { 0x60000, 0x6FFFD },
        new int[] { 0x70000, 0x7FFFD },
        new int[] { 0x80000, 0x8FFFD },
        new int[] { 0x90000, 0x9FFFD },
        new int[] { 0xA0000, 0xAFFFD },
        new int[] { 0xB0000, 0xBFFFD },
        new int[] { 0xC0000, 0xCFFFD },
        new int[] { 0xD0000, 0xDFFFD },
        new int[] { 0xE1000, 0xEFFFD }
    };

    private static readonly int[][] PrivateCharacterRanges = new int[][]
    {
        new int[] { 0xE000, 0xF8FF },
        new int[] { 0xF0000, 0xFFFFD },
        new int[] { 0x100000, 0x10FFFD }
    };

    private static readonly UriComponent[] InternationalizedComponents =
        new UriComponent[]
        {
            UriComponent.UserInfo,
            UriComponent.Host,
            UriComponent.Path,
            UriComponent.Query,
            UriComponent.Fragment
        };

    public static IEnumerable<TestCaseData> UnicodeComponentCases()
    {
        foreach (UriComponent component in InternationalizedComponents)
        {
            foreach (int[] range in UcsCharacterRanges)
            {
                foreach (int scalarValue in GetRangeSamples(range[0], range[1]))
                {
                    yield return CreateCase(component, scalarValue, "ucschar");
                }
            }
        }

        foreach (int[] range in PrivateCharacterRanges)
        {
            foreach (int scalarValue in GetRangeSamples(range[0], range[1]))
            {
                yield return CreateCase(UriComponent.Query, scalarValue, "iprivate");
            }
        }
    }

    [TestCaseSource(nameof(UnicodeComponentCases))]
    public void GeneratedConversionAndComparisonCyclesAreIdempotent(
        UriComponent component,
        int scalarValue)
    {
        IIriConverter converter = new IriConverter();
        IIriComparer iriComparer = new IriComparer();
        IUriComparer uriComparer = new UriComparer();
        IriConversionOptions conversionOptions = new IriConversionOptions();
        UriComparisonOptions comparisonOptions = new UriComparisonOptions();
        string source = CreateIri(component, scalarValue);
        string normalizedSource = source.Normalize(NormalizationForm.FormC);
        string message = component + " U+" +
            scalarValue.ToString("X", CultureInfo.InvariantCulture);

        string mappedOnce = converter.ConvertIriToUri(source, conversionOptions);
        string mappedTwice = converter.ConvertIriToUri(mappedOnce, conversionOptions);
        string restoredOnce = converter.ConvertUriToIri(mappedOnce, conversionOptions);
        string remapped = converter.ConvertIriToUri(restoredOnce, conversionOptions);
        string restoredTwice = converter.ConvertUriToIri(remapped, conversionOptions);

        string lowerCaseTriplets = LowerCasePercentTriplets(mappedOnce);
        string lowerCaseMappedTwice = converter.ConvertIriToUri(
            lowerCaseTriplets,
            conversionOptions);
        string lowerCaseRestored = converter.ConvertUriToIri(
            lowerCaseTriplets,
            conversionOptions);

        UriComparisonResult uriCycle = uriComparer.CompareSyntax(
            mappedOnce,
            remapped,
            comparisonOptions);
        UriComparisonResult uriTripletCase = uriComparer.CompareSyntax(
            mappedOnce,
            lowerCaseTriplets,
            comparisonOptions);
        UriComparisonResult iriCycle = iriComparer.CompareIriAsUri(
            source,
            restoredOnce,
            comparisonOptions);
        UriComparisonResult iriReverseCycle = iriComparer.CompareIriAsUri(
            restoredTwice,
            source,
            comparisonOptions);
        UriComparisonResult exactReflexive = iriComparer.CompareIriExact(
            source,
            source,
            comparisonOptions);
        UriComparisonResult unicodeEquivalent = iriComparer.CompareIriUnicode(
            source,
            normalizedSource,
            comparisonOptions);

        Assert.Multiple(() =>
        {
            Assert.That(IsAscii(mappedOnce), Is.True, message);
            Assert.That(mappedTwice, Is.EqualTo(mappedOnce), message);
            Assert.That(remapped, Is.EqualTo(mappedOnce), message);
            Assert.That(restoredTwice, Is.EqualTo(restoredOnce), message);
            Assert.That(lowerCaseMappedTwice, Is.EqualTo(lowerCaseTriplets), message);
            Assert.That(lowerCaseRestored, Is.EqualTo(restoredOnce), message);

            Assert.That(uriCycle.Success, Is.True, message);
            Assert.That(uriCycle.Equal, Is.True, message);
            Assert.That(uriCycle.DifferentComponent, Is.EqualTo(UriComponent.None), message);
            Assert.That(uriTripletCase.Success, Is.True, message);
            Assert.That(uriTripletCase.Equal, Is.True, message);
            Assert.That(uriTripletCase.DifferentComponent, Is.EqualTo(UriComponent.None), message);

            Assert.That(iriCycle.Success, Is.True, message);
            Assert.That(iriCycle.Equal, Is.True, message);
            Assert.That(iriCycle.DifferentComponent, Is.EqualTo(UriComponent.None), message);
            Assert.That(iriReverseCycle.Success, Is.True, message);
            Assert.That(iriReverseCycle.Equal, Is.True, message);
            Assert.That(iriReverseCycle.DifferentComponent, Is.EqualTo(UriComponent.None), message);

            Assert.That(exactReflexive.Success, Is.True, message);
            Assert.That(exactReflexive.Equal, Is.True, message);
            Assert.That(exactReflexive.DifferentComponent, Is.EqualTo(UriComponent.None), message);
            Assert.That(unicodeEquivalent.Success, Is.True, message);
            Assert.That(unicodeEquivalent.Equal, Is.True, message);
            Assert.That(unicodeEquivalent.DifferentComponent, Is.EqualTo(UriComponent.None), message);
        });
    }

    private static TestCaseData CreateCase(
        UriComponent component,
        int scalarValue,
        string rangeName)
    {
        string name = "GeneratedConversionAndComparisonCyclesAreIdempotent_" +
            rangeName + "_" + component + "_U" +
            scalarValue.ToString("X", CultureInfo.InvariantCulture);
        return new TestCaseData(component, scalarValue).SetName(name);
    }

    private static int[] GetRangeSamples(int first, int last)
    {
        int quarter = first + ((last - first) / 4);
        int middle = first + ((last - first) / 2);
        int threeQuarters = first + (((last - first) * 3) / 4);
        return new int[] { first, quarter, middle, threeQuarters, last };
    }

    private static string CreateIri(UriComponent component, int scalarValue)
    {
        string value = char.ConvertFromUtf32(scalarValue);
        if (component == UriComponent.UserInfo)
        {
            return "x://u" + value + "v@host.test/path?query#fragment";
        }

        if (component == UriComponent.Host)
        {
            return "x://h" + value + "i.test/path?query#fragment";
        }

        if (component == UriComponent.Path)
        {
            return "x://host.test/a" + value + "b?query#fragment";
        }

        if (component == UriComponent.Query)
        {
            return "x://host.test/path?q" + value + "r#fragment";
        }

        return "x://host.test/path?query#f" + value + "g";
    }

    private static string LowerCasePercentTriplets(string value)
    {
        char[] characters = value.ToCharArray();
        int offset = 0;
        while (offset + 2 < characters.Length)
        {
            if (characters[offset] == '%')
            {
                characters[offset + 1] = char.ToLowerInvariant(characters[offset + 1]);
                characters[offset + 2] = char.ToLowerInvariant(characters[offset + 2]);
                offset = offset + 3;
            }
            else
            {
                offset = offset + 1;
            }
        }

        return new string(characters);
    }

    private static bool IsAscii(string value)
    {
        int offset = 0;
        while (offset < value.Length)
        {
            if (value[offset] > 127)
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }
}
