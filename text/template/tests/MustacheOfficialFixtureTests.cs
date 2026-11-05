using System.Text.Json;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class MustacheOfficialFixtureTests
{
    [TestCase("comments.json")]
    [TestCase("delimiters.json")]
    [TestCase("interpolation.json")]
    [TestCase("inverted.json")]
    [TestCase("partials.json")]
    [TestCase("sections.json")]
    [TestCase("~dynamic-names.json")]
    [TestCase("~inheritance.json")]
    public void RenderMustacheToString_ConformsToOfficialDataFixtures(string fixtureName)
    {
        string fixturePath = FindFixture(fixtureName);
        using JsonDocument fixture = JsonDocument.Parse(File.ReadAllText(fixturePath));
        List<string> failures = new List<string>();

        foreach (JsonElement test in fixture.RootElement.GetProperty("tests").EnumerateArray())
        {
            TemplateRegistry registry = new TemplateRegistry();
            if (test.TryGetProperty("partials", out JsonElement partials))
            {
                foreach (JsonProperty partial in partials.EnumerateObject())
                {
                    registry.RegisterPartial(partial.Name, partial.Value.GetString()!);
                    registry.RegisterTemplate(partial.Name, partial.Value.GetString()!);
                }
            }

            TemplateRenderer renderer = TemplateRenderer.Create(registry);
            string actual;
            try
            {
                actual = renderer.RenderMustacheToString(
                    test.GetProperty("template").GetString()!,
                    ConvertValue(test.GetProperty("data")));
            }
            catch (Exception exception)
            {
                actual = "<" + exception.GetType().Name + ": " + exception.Message + ">";
            }

            string expected = test.GetProperty("expected").GetString()!;
            if (actual != expected)
            {
                failures.Add(test.GetProperty("name").GetString() + "\nexpected: " + JsonSerializer.Serialize(expected) + "\nactual:   " + JsonSerializer.Serialize(actual));
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n\n", failures));
    }

    static string FindFixture(string fixtureName)
    {
        string directory = TestContext.CurrentContext.TestDirectory;
        while (Directory.GetParent(directory) != null)
        {
            string candidate = Path.Combine(directory, "specs", "mustache", "specs", fixtureName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = Directory.GetParent(directory)!.FullName;
        }

        throw new FileNotFoundException("Mustache fixture was not found.", fixtureName);
    }

    static TemplateValue ConvertValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return new TemplateValueFactory().CreateNull();
        }

        if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
        {
            return new TemplateValueFactory().CreateBoolean(value.GetBoolean());
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return new TemplateValueFactory().CreateNumber(value.GetDecimal());
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return new TemplateValueFactory().CreateString(value.GetString()!);
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            List<TemplateValue> items = new List<TemplateValue>();
            foreach (JsonElement item in value.EnumerateArray())
            {
                items.Add(ConvertValue(item));
            }

            return new TemplateValueFactory().CreateList(items);
        }

        Dictionary<string, TemplateValue> fields = new Dictionary<string, TemplateValue>();
        foreach (JsonProperty field in value.EnumerateObject())
        {
            fields[field.Name] = ConvertValue(field.Value);
        }

        return new TemplateValueFactory().CreateMap(fields);
    }
}
