using System.IO;
using System.Text.Json;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class CommonMarkSpecFixtureLoaderTests
{
    [Test]
    public void Loader_ReadsAuthoritativeJsonFieldsWithoutChangingContent()
    {
        string fixture =
            "[{"
            + "\"markdown\":\"a\\r\\nb\\n\","
            + "\"html\":\"<p>a b</p>\\n\","
            + "\"example\":42,"
            + "\"start_line\":100,"
            + "\"end_line\":106,"
            + "\"section\":\"Soft line breaks\""
            + "}]";

        CommonMarkSpecExample[] examples = CommonMarkSpecFixtureLoader.LoadFromString(fixture);

        Assert.That(examples.Length, Is.EqualTo(1));
        Assert.That(examples[0].ExampleNumber, Is.EqualTo(42));
        Assert.That(examples[0].Section, Is.EqualTo("Soft line breaks"));
        Assert.That(examples[0].Markdown, Is.EqualTo("a\r\nb\n"));
        Assert.That(examples[0].Html, Is.EqualTo("<p>a b</p>\n"));
        Assert.That(examples[0].StartLine, Is.EqualTo(100));
        Assert.That(examples[0].EndLine, Is.EqualTo(106));
        Assert.That(examples[0].ToString(), Is.EqualTo("Example 42 (Soft line breaks)"));
    }

    [Test]
    public void Loader_LoadsFromFileAndPreservesFixtureOrderAndNumbers()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "commonmark-spec-loader-test.json");
        File.WriteAllText(
            path,
            "["
            + "{\"markdown\":\"one\\n\",\"html\":\"<p>one</p>\\n\",\"example\":7,\"start_line\":10,\"end_line\":15,\"section\":\"First\"},"
            + "{\"markdown\":\"two\\n\",\"html\":\"<p>two</p>\\n\",\"example\":9,\"start_line\":20,\"end_line\":25,\"section\":\"Second\"}"
            + "]");

        CommonMarkSpecExample[] examples = CommonMarkSpecFixtureLoader.LoadFromFile(path);

        Assert.That(examples.Length, Is.EqualTo(2));
        Assert.That(examples[0].ExampleNumber, Is.EqualTo(7));
        Assert.That(examples[1].ExampleNumber, Is.EqualTo(9));
        Assert.That(examples[1].Section, Is.EqualTo("Second"));
    }

    [Test]
    public void Loader_RejectsMalformedRootsEntriesAndRequiredFields()
    {
        string nonArray = "{}";
        string nonObject = "[1]";
        string missingField = "[{\"example\":1}]";
        string wrongFieldType =
            "[{\"markdown\":\"x\",\"html\":\"y\",\"example\":\"1\",\"start_line\":1,\"end_line\":2,\"section\":\"S\"}]";

        Assert.That(() => CommonMarkSpecFixtureLoader.LoadFromString("["), Throws.InstanceOf<JsonException>());
        Assert.That(() => CommonMarkSpecFixtureLoader.LoadFromString(nonArray), Throws.TypeOf<FormatException>());
        Assert.That(() => CommonMarkSpecFixtureLoader.LoadFromString(nonObject), Throws.TypeOf<FormatException>());
        Assert.That(() => CommonMarkSpecFixtureLoader.LoadFromString(missingField), Throws.TypeOf<FormatException>());
        Assert.That(() => CommonMarkSpecFixtureLoader.LoadFromString(wrongFieldType), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Loader_RejectsInvalidArgumentsAndRanges()
    {
        Assert.That(() => new CommonMarkSpecExample(0, "", "", "", 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new CommonMarkSpecExample(1, null!, "", "", 1, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new CommonMarkSpecExample(1, "", null!, "", 1, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new CommonMarkSpecExample(1, "", "", null!, 1, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new CommonMarkSpecExample(1, "", "", "", 0, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new CommonMarkSpecExample(1, "", "", "", 2, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkSpecFixtureLoader.LoadFromFile(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkSpecFixtureLoader.LoadFromString(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void ConformanceCaseNames_IncludeExampleAndSectionIdentifiers()
    {
        Assert.That(CommonMarkSpecConformanceTests.SanitizeTestName("Links / images"), Is.EqualTo("Links___images"));
    }
}
