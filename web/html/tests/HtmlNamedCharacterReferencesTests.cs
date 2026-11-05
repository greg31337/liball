using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlNamedCharacterReferencesTests
{
    [Test]
    public void EveryPinnedReferenceHasAnExactTrieMatch()
    {
        using JsonDocument document = ReadPinnedEntities();
        Dictionary<string, int[]> expected = new(StringComparer.Ordinal);
        int legacyCount = 0;
        int twoCodePointCount = 0;
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            string name = property.Name[1..];
            int[] codePoints = property.Value.GetProperty("codepoints").EnumerateArray()
                .Select(value => value.GetInt32()).ToArray();
            expected.Add(name, codePoints);
            HtmlNamedCharacterReferenceMatch match = HtmlNamedCharacterReferences.FindLongest(name + "#");

            Assert.That(match.IsMatch(), Is.True, name);
            Assert.That(match.GetConsumedCharacters(), Is.EqualTo(name.Length), name);
            Assert.That(match.GetFirstCodePoint(), Is.EqualTo(codePoints[0]), name);
            Assert.That(match.HasSecondCodePoint(), Is.EqualTo(codePoints.Length == 2), name);
            Assert.That(match.GetSecondCodePoint(), Is.EqualTo(codePoints.Length == 2 ? codePoints[1] : -1), name);
            Assert.That(match.HasSemicolon(), Is.EqualTo(name.EndsWith(';')), name);
            if (!name.EndsWith(';'))
            {
                legacyCount++;
            }

            if (codePoints.Length == 2)
            {
                twoCodePointCount++;
            }
        }

        Assert.That(HtmlNamedCharacterReferences.GetCount(), Is.EqualTo(expected.Count));
        Assert.That(HtmlNamedCharacterReferences.GetLegacySemicolonlessCount(), Is.EqualTo(legacyCount));
        Assert.That(HtmlNamedCharacterReferences.GetTwoCodePointCount(), Is.EqualTo(twoCodePointCount));

        string previous = "";
        for (int index = 0; index < HtmlNamedCharacterReferences.GetCount(); index++)
        {
            string name = HtmlNamedCharacterReferences.GetName(index);
            Assert.That(expected.ContainsKey(name), Is.True, name);
            Assert.That(string.CompareOrdinal(previous, name), Is.LessThan(0), name);
            Assert.That(HtmlNamedCharacterReferences.GetFirstCodePoint(index), Is.EqualTo(expected[name][0]), name);
            Assert.That(HtmlNamedCharacterReferences.GetSecondCodePoint(index),
                Is.EqualTo(expected[name].Length == 2 ? expected[name][1] : -1), name);
            previous = name;
        }
    }

    [Test]
    public void LookupReturnsTheLongestOrdinalMatch()
    {
        HtmlNamedCharacterReferenceMatch complete = HtmlNamedCharacterReferences.FindLongest("notin;tail");
        Assert.That(complete.IsMatch(), Is.True);
        Assert.That(complete.GetConsumedCharacters(), Is.EqualTo(6));
        Assert.That(complete.GetFirstCodePoint(), Is.EqualTo(0x2209));
        Assert.That(complete.HasSemicolon(), Is.True);

        HtmlNamedCharacterReferenceMatch legacy = HtmlNamedCharacterReferences.FindLongest("notit;");
        Assert.That(legacy.IsMatch(), Is.True);
        Assert.That(legacy.GetConsumedCharacters(), Is.EqualTo(3));
        Assert.That(legacy.GetFirstCodePoint(), Is.EqualTo(0x00AC));
        Assert.That(legacy.HasSemicolon(), Is.False);

        HtmlNamedCharacterReferenceMatch pair = HtmlNamedCharacterReferences.FindLongest("NotEqualTilde;");
        Assert.That(pair.GetFirstCodePoint(), Is.EqualTo(0x2242));
        Assert.That(pair.GetSecondCodePoint(), Is.EqualTo(0x0338));
        Assert.That(pair.HasSecondCodePoint(), Is.True);

        Assert.That(HtmlNamedCharacterReferences.FindLongest("AELIG;").IsMatch(), Is.False);
        Assert.That(HtmlNamedCharacterReferences.FindLongest("").IsMatch(), Is.False);
        Assert.That(HtmlNamedCharacterReferences.FindLongest("unknown;").IsMatch(), Is.False);
    }

    [Test]
    public void LookupAndEnumerationValidateArguments()
    {
        Assert.Throws<ArgumentNullException>(() => HtmlNamedCharacterReferences.FindLongest((string)null));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlNamedCharacterReferences.GetName(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HtmlNamedCharacterReferences.GetFirstCodePoint(HtmlNamedCharacterReferences.GetCount()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HtmlNamedCharacterReferences.GetSecondCodePoint(HtmlNamedCharacterReferences.GetCount()));
    }

    static JsonDocument ReadPinnedEntities()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(root, "tools", "GenerateTables", "inputs", "entities.json.gz.b64");
        byte[] compressed = Convert.FromBase64String(File.ReadAllText(path, Encoding.ASCII));
        using MemoryStream memory = new MemoryStream(compressed, false);
        using GZipStream gzip = new GZipStream(memory, CompressionMode.Decompress, false);
        return JsonDocument.Parse(gzip);
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "html.csproj"))
                && File.Exists(Path.Combine(directory.FullName, "tools", "GenerateTables", "inputs", "manifest.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root from the test output directory.");
    }
}
