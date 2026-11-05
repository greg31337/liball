using System.Globalization;
using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlEntityConformanceTests
{
    [Test]
    public void EveryPinnedNamedReferenceDecodesToItsFixtureScalars()
    {
        string path = Path.Combine(FindRepositoryRoot(), "tests", "data", "entities",
            "named-character-references.tsv");
        int count = 0;
        int legacyCount = 0;
        int pairCount = 0;
        foreach (string line in File.ReadLines(path))
        {
            if (line.StartsWith('#'))
            {
                continue;
            }

            string[] fields = line.Split('\t');
            Assert.That(fields, Has.Length.EqualTo(3), line);
            int first = int.Parse(fields[1], CultureInfo.InvariantCulture);
            int second = int.Parse(fields[2], CultureInfo.InvariantCulture);
            string expected = char.ConvertFromUtf32(first);
            if (second >= 0)
            {
                expected += char.ConvertFromUtf32(second);
                pairCount++;
            }

            Assert.That(HtmlEntities.Decode(fields[0], HtmlEntityContext.Text),
                Is.EqualTo(expected), fields[0]);
            Assert.That(HtmlEntities.Decode(fields[0], HtmlEntityContext.Attribute),
                Is.EqualTo(expected), fields[0]);
            if (!fields[0].EndsWith(';'))
            {
                legacyCount++;
            }

            count++;
        }

        Assert.That(count, Is.EqualTo(2231));
        Assert.That(legacyCount, Is.EqualTo(106));
        Assert.That(pairCount, Is.EqualTo(93));
    }

    [Test]
    public void MinimalAndNamedModesRoundTripEveryValidUnicodeScalar()
    {
        AssertEveryScalarRoundTrips(HtmlEntityContext.Text, HtmlEntityEncoding.Minimal, false);
        AssertEveryScalarRoundTrips(HtmlEntityContext.Attribute, HtmlEntityEncoding.Minimal, false);
        AssertEveryScalarRoundTrips(HtmlEntityContext.Text, HtmlEntityEncoding.Named, false);
        AssertEveryScalarRoundTrips(HtmlEntityContext.Attribute, HtmlEntityEncoding.Named, false);
    }

    [Test]
    public void NumericAndAsciiOnlyModesRoundTripEveryScalarWithoutHtmlControlRemapping()
    {
        AssertEveryScalarRoundTrips(HtmlEntityContext.Text, HtmlEntityEncoding.Numeric, true);
        AssertEveryScalarRoundTrips(HtmlEntityContext.Attribute, HtmlEntityEncoding.Numeric, true);
        AssertEveryScalarRoundTrips(HtmlEntityContext.Text, HtmlEntityEncoding.AsciiOnly, true);
        AssertEveryScalarRoundTrips(HtmlEntityContext.Attribute, HtmlEntityEncoding.AsciiOnly, true);
    }

    [Test]
    public void NumericModesApplyTheRequiredC1ControlRemappingOnReparse()
    {
        int[] replacements =
        [
            0x20AC, 0x81, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
            0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x8D, 0x017D, 0x8F,
            0x90, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
            0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x9D, 0x017E, 0x0178
        ];

        for (int scalar = 0x80; scalar <= 0x9F; scalar++)
        {
            string value = char.ConvertFromUtf32(scalar);
            foreach (HtmlEntityEncoding mode in new[]
            {
                HtmlEntityEncoding.Numeric,
                HtmlEntityEncoding.AsciiOnly
            })
            {
                string encoded = HtmlEntities.Encode(value, mode);
                Assert.That(encoded.All(character => character <= 127), Is.True,
                    $"{mode} U+{scalar:X4}");
                Assert.That(HtmlEntities.Decode(encoded),
                    Is.EqualTo(char.ConvertFromUtf32(replacements[scalar - 0x80])),
                    $"{mode} U+{scalar:X4}");
            }
        }
    }

    static void AssertEveryScalarRoundTrips(HtmlEntityContext context,
        HtmlEntityEncoding mode, bool skipC1Controls)
    {
        const int BlockSize = 4096;
        int start = 0;
        while (start <= UnicodeTools.MaximumScalar)
        {
            int end = start + BlockSize;
            if (end > UnicodeTools.MaximumScalar + 1)
            {
                end = UnicodeTools.MaximumScalar + 1;
            }

            string original = CreateScalarBlock(start, end, skipC1Controls);
            string encoded = HtmlEntities.Encode(original, context, mode);
            string reparsed = HtmlEntities.Decode(encoded, context);
            Assert.That(reparsed, Is.EqualTo(original),
                $"{context} {mode} U+{start:X6}..U+{end - 1:X6}");
            start = end;
        }
    }

    static string CreateScalarBlock(int start, int end, bool skipC1Controls)
    {
        StringBuilder builder = new StringBuilder();
        int scalar = start;
        while (scalar < end)
        {
            if (UnicodeTools.IsScalarValue(scalar)
                && (!skipC1Controls || scalar < 0x80 || scalar > 0x9F))
            {
                builder.Append(char.ConvertFromUtf32(scalar));
            }

            scalar++;
        }

        return builder.ToString();
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "tests", "data", "entities",
                "named-character-references.tsv")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root from the test output directory.");
    }
}
