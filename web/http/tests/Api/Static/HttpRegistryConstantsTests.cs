using System.Reflection;

namespace http.Tests;

public class HttpRegistryConstantsTests
{
    [Test]
    public void EveryPublicConstantBelongsToAnExhaustivelyCheckedStaticClass()
    {
        Type[] expectedTypes =
        [
            typeof(Http2Constants),
            typeof(Http2SettingIdentifiers),
            typeof(HttpContentCodings),
            typeof(HttpHeaderNames),
            typeof(HttpHeaderValues),
            typeof(HttpMethods),
            typeof(HttpProtocolNames),
            typeof(HttpRangeUnits),
            typeof(HttpReasonPhrases),
            typeof(HttpStatusCodes),
            typeof(HttpTransferCodings)
        ];
        Type[] actualTypes = typeof(HttpMethods).Assembly.GetTypes()
            .Where(type => type.IsPublic)
            .Where(type => !type.IsEnum)
            .Where(type => type.GetFields(
                BindingFlags.Public | BindingFlags.Static)
                .Any(field => field.IsLiteral))
            .ToArray();

        Assert.That(actualTypes, Is.EquivalentTo(expectedTypes));
    }

    [Test]
    public void MethodConstantsMatchRegistrySnapshot()
    {
        string[] expected = ReadFirstColumn("http-methods.csv");
        string[] actual = ReadStringConstants(typeof(HttpMethods));

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void StatusConstantsMatchNamedRegistryEntries()
    {
        string[] lines = ReadRegistryLines("http-status-codes.csv");
        int[] expected = lines
            .Where(line => !FirstColumn(line).Contains('-'))
            .Where(line => !line.Contains(",Unassigned,"))
            .Select(line => int.Parse(FirstColumn(line)))
            .ToArray();
        int[] actual = ReadIntConstants(typeof(HttpStatusCodes));

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void HeaderNameConstantsMatchRegistrySnapshot()
    {
        string[] expected = ReadFirstColumn("http-field-names.csv");
        string[] actual = ReadStringConstants(typeof(HttpHeaderNames));

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void ContentCodingConstantsMatchRegistrySnapshot()
    {
        string[] expected = ReadFirstColumn("http-content-codings.csv");
        string[] actual = ReadStringConstants(typeof(HttpContentCodings));

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void TransferCodingConstantsMatchRegistrySnapshot()
    {
        string[] expected = ReadFirstColumn("http-transfer-codings.csv");
        string[] actual = ReadStringConstants(typeof(HttpTransferCodings));

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void RangeUnitConstantsMatchRegistrySnapshot()
    {
        string[] expected = ReadFirstColumn("http-range-units.csv");
        string[] actual = ReadStringConstants(typeof(HttpRangeUnits));

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void Http2SettingConstantsMatchNamedRegistryEntries()
    {
        string[] lines = ReadRegistryLines("http2-settings.csv");
        int[] expected = lines
            .Where(line => !FirstColumn(line).Contains('-'))
            .Where(line => !line.Contains(",Unassigned,"))
            .Select(line => Convert.ToInt32(FirstColumn(line).Substring(2), 16))
            .ToArray();
        int[] actual = ReadIntConstants(typeof(Http2SettingIdentifiers));

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void CoreRegistryConstantsExposeCanonicalNames()
    {
        Assert.That(HttpMethods.Get, Is.EqualTo("GET"));
        Assert.That(HttpStatusCodes.Ok, Is.EqualTo(200));
        Assert.That(HttpHeaderNames.ContentLength, Is.EqualTo("Content-Length"));
        Assert.That(HttpHeaderNames.ETag, Is.EqualTo("ETag"));
        Assert.That(HttpContentCodings.Gzip, Is.EqualTo("gzip"));
        Assert.That(HttpTransferCodings.Chunked, Is.EqualTo("chunked"));
        Assert.That(HttpRangeUnits.Bytes, Is.EqualTo("bytes"));
        Assert.That(Http2SettingIdentifiers.InitialWindowSize, Is.EqualTo(0x04));
    }

    private static string[] ReadFirstColumn(string fileName)
    {
        return ReadRegistryLines(fileName)
            .Select(FirstColumn)
            .ToArray();
    }

    private static string FirstColumn(string line)
    {
        int comma = line.IndexOf(',');
        return line.Substring(0, comma);
    }

    private static string[] ReadRegistryLines(string fileName)
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "specs", "iana", fileName);
        return File.ReadLines(Path.GetFullPath(path))
            .Skip(1)
            .Where(line => line.Length > 0)
            .Where(line => !char.IsWhiteSpace(line[0]))
            .Where(line => line.Contains(','))
            .ToArray();
    }

    private static string[] ReadStringConstants(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();
    }

    private static int[] ReadIntConstants(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (int)field.GetRawConstantValue()!)
            .ToArray();
    }
}
