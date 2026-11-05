using fake;

namespace fake.Tests;

[TestFixture]
public class LocalePackTests
{
    [Test]
    public void ParseReturnsValidatedLocalePackModel()
    {
        LocalePack pack = Parse(ValidPack());

        Assert.Multiple(() =>
        {
            Assert.That(pack.SchemaVersion, Is.EqualTo(1));
            Assert.That(pack.DataVersion, Is.EqualTo("pack-2026.08"));
            Assert.That(pack.Locale, Is.EqualTo("fr-CA"));
            Assert.That(pack.Fallbacks, Is.EqualTo(new string[] { "fr", "en-US", "en", "invariant" }));
            Assert.That(pack.ReviewStatus, Is.EqualTo("native-reviewed"));
            Assert.That(pack.Provenance, Has.Length.EqualTo(2));
            Assert.That(pack.Provenance[0].Id, Is.EqualTo("cldr"));
            Assert.That(pack.Provenance[0].Source, Is.EqualTo("Unicode CLDR"));
            Assert.That(pack.Provenance[0].SourceVersion, Is.EqualTo("48"));
            Assert.That(pack.Provenance[0].License, Is.EqualTo("Unicode-3.0"));
            Assert.That(pack.Provenance[0].Reviewer, Is.EqualTo("data-review"));
            Assert.That(pack.Fields, Has.Length.EqualTo(3));
            Assert.That(pack.Fields[0].Name, Is.EqualTo("date.order"));
            Assert.That(pack.Fields[0].Provenance, Is.EqualTo("cldr"));
            Assert.That(pack.Fields[0].ValueJson, Is.EqualTo("\"dmy\""));
            Assert.That(pack.Fields[1].ValueJson, Is.EqualTo("[\"Aline\",\"Camille\"]"));
            Assert.That(pack.Fields[2].ValueJson, Is.EqualTo("{\"separator\":\" \",\"lines\":2}"));
        });
    }

    [Test]
    public void InvariantPackRequiresAnExplicitEmptyFallbackList()
    {
        string valid = MinimalPack("invariant", "[]", "\"value\"");
        string invalid = MinimalPack("invariant", "[\"en\"]", "\"value\"");

        Assert.That(Parse(valid).Fallbacks, Is.Empty);
        AssertViolation(invalid, "$.fallbacks", "must be empty for the invariant pack");
    }

    [Test]
    public void MalformedJsonUsesTheRuntimeJsonDiagnostic()
    {
        Exception exception = Assert.Catch<Exception>(() => Parse("{"));

        Assert.That(exception.GetType().FullName, Is.EqualTo("System.Text.Json.JsonReaderException"));
        Assert.That(exception.Message, Does.Contain("JSON object or array"));
    }

    [Test]
    public void NullEmptyAndOversizedJsonAreRejected()
    {
        Assert.That(
            Assert.Throws<ArgumentNullException>(() => LocalePack.Parse(null, 100, 10)).ParamName,
            Is.EqualTo("json"));
        Assert.That(
            Assert.Throws<ArgumentException>(() => LocalePack.Parse("", 100, 10)).ParamName,
            Is.EqualTo("json"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => LocalePack.Parse("{}", 1, 10)).ParamName,
            Is.EqualTo("json"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => LocalePack.Parse("{}", 10, 0)).ParamName,
            Is.EqualTo("maximumCount"));
    }

    [TestCase("[]", "$", "must be a JSON object")]
    [TestCase("{}", "$", "requires member 'schemaVersion'")]
    [TestCase("{\"schemaVersion\":1,\"schemaVersion\":1}", "$.schemaVersion", "duplicates member")]
    [TestCase("{\"unknown\":true}", "$.unknown", "is not allowed")]
    public void RootShapeRequiredAndDuplicateMembersAreRejected(
        string json,
        string path,
        string reason)
    {
        AssertViolation(json, path, reason);
    }

    [TestCase("0")]
    [TestCase("2")]
    [TestCase("1.0")]
    [TestCase("\"1\"")]
    public void OnlyCurrentIntegerSchemaVersionIsAccepted(string schemaVersion)
    {
        string json = ValidPack().Replace("\"schemaVersion\": 1", "\"schemaVersion\": " + schemaVersion);

        AssertViolation(json, "$.schemaVersion", "must be the integer 1");
    }

    [TestCase("\"dataVersion\": \"pack-2026.08\"", "\"dataVersion\": \" \"", "$.dataVersion")]
    [TestCase("\"reviewStatus\": \"native-reviewed\"", "\"reviewStatus\": null", "$.reviewStatus")]
    public void RequiredMetadataMustUseNonemptyStrings(string before, string after, string path)
    {
        AssertViolation(ValidPack().Replace(before, after), path, "must");
    }

    [TestCase("FR-ca", "fr-CA")]
    [TestCase("iw-IL", "he-IL")]
    public void PackLocaleMustAlreadyBeCanonical(string locale, string canonical)
    {
        string json = ValidPack().Replace("\"locale\": \"fr-CA\"", "\"locale\": \"" + locale + "\"");

        AssertViolation(json, "$.locale", "must use canonical tag '" + canonical + "'");
    }

    [Test]
    public void MalformedPackLocaleIsRejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Parse(
            ValidPack().Replace("\"locale\": \"fr-CA\"", "\"locale\": \"not_a_tag\"")));

        Assert.That(exception.ParamName, Is.EqualTo("json"));
    }

    [Test]
    public void FallbacksMustBeCanonicalUniqueAndDifferentFromPackLocale()
    {
        AssertViolation(
            ReplaceFallbacks("[\"FR\"]"),
            "$.fallbacks[0]",
            "must use canonical tag 'fr'");
        AssertViolation(
            ReplaceFallbacks("[\"fr\",\"fr\"]"),
            "$.fallbacks[1]",
            "duplicates fallback 'fr'");
        AssertViolation(
            ReplaceFallbacks("[\"fr-CA\"]"),
            "$.fallbacks[0]",
            "must not repeat the pack locale");
    }

    [Test]
    public void ProvenanceArrayAndEveryRequiredMemberAreValidated()
    {
        string empty = ReplaceSection(ValidPack(), "provenance", "[]", "fields");
        string missingReviewer = ValidPack().Replace(",\n      \"reviewer\": \"data-review\"", "");
        string unknown = ValidPack().Replace(
            "\"reviewer\": \"data-review\"",
            "\"reviewer\": \"data-review\", \"url\": \"https://example.test\"");

        AssertViolation(empty, "$.provenance", "must contain at least one provenance record");
        AssertViolation(missingReviewer, "$.provenance[0]", "requires member 'reviewer'");
        AssertViolation(unknown, "$.provenance[0].url", "is not allowed");
    }

    [Test]
    public void ProvenanceIdentifiersMustBeValidAndUnique()
    {
        string invalid = ValidPack().Replace("\"id\": \"cldr\"", "\"id\": \"CLDR_48\"");
        string duplicate = ValidPack().Replace("\"id\": \"authored-fr\"", "\"id\": \"cldr\"");

        AssertViolation(invalid, "$.provenance[0].id", "lowercase ASCII letters");
        AssertViolation(duplicate, "$.provenance[1].id", "duplicates provenance id 'cldr'");
    }

    [Test]
    public void FieldNamesMustBeUniqueDottedIdentifiers()
    {
        string invalid = ValidPack().Replace("\"name\": \"date.order\"", "\"name\": \"Date..order\"");
        string duplicate = ValidPack().Replace("\"name\": \"person.names\"", "\"name\": \"date.order\"");

        AssertViolation(invalid, "$.fields[0].name", "lowercase ASCII letters");
        AssertViolation(duplicate, "$.fields[1].name", "duplicates field 'date.order'");
    }

    [Test]
    public void FieldProvenanceMustReferenceADeclaredRecord()
    {
        string json = ValidPack().Replace(
            "\"name\": \"date.order\", \"provenance\": \"cldr\"",
            "\"name\": \"date.order\", \"provenance\": \"missing\"");

        AssertViolation(json, "$.fields[0].provenance", "references unknown provenance id 'missing'");
    }

    [Test]
    public void FieldValuesRejectNullsAndDuplicateObjectMembersAtAnyDepth()
    {
        AssertViolation(
            MinimalPack("en", "[]", "[1,null]"),
            "$.fields[0].value[1]",
            "must not be null");
        AssertViolation(
            MinimalPack("en", "[]", "{\"nested\":{\"x\":1,\"x\":2}}"),
            "$.fields[0].value.nested.x",
            "duplicates member 'x'");
    }

    [Test]
    public void CollectionLimitsApplyToSchemaAndNestedValueArrays()
    {
        AssertViolation(ValidPack(), "$.fallbacks", "exceeding the configured maximum", 3);
        AssertViolation(
            MinimalPack("en", "[]", "[1,2]"),
            "$.fields[0].value",
            "exceeding the configured maximum",
            1);
    }

    [Test]
    public void ConcurrentValidationHasNoSharedMutableState()
    {
        string json = ValidPack();
        string[] locales = new string[500];

        Parallel.For(0, locales.Length, index => locales[index] = Parse(json).Locale);

        Assert.That(locales, Has.All.EqualTo("fr-CA"));
    }

    static LocalePack Parse(string json)
    {
        return LocalePack.Parse(json, 100000, 1000);
    }

    static void AssertViolation(string json, string path, string reason)
    {
        AssertViolation(json, path, reason, 1000);
    }

    static void AssertViolation(string json, string path, string reason, int maximumCount)
    {
        FakeException exception = Assert.Throws<FakeException>(() => LocalePack.Parse(json, 100000, maximumCount));

        Assert.That(exception.Message, Does.Contain("Locale pack schema violation at '" + path + "'"));
        Assert.That(exception.Message, Does.Contain(reason));
    }

    static string ReplaceFallbacks(string fallbacks)
    {
        return ReplaceSection(ValidPack(), "fallbacks", fallbacks, "reviewStatus");
    }

    static string ReplaceSection(string json, string name, string replacement, string nextName)
    {
        int start = json.IndexOf("\"" + name + "\":", StringComparison.Ordinal);
        int end = json.IndexOf("\"" + nextName + "\":", start, StringComparison.Ordinal);
        return json.Substring(0, start) + "\"" + name + "\": " + replacement + ",\n  " + json.Substring(end);
    }

    static string MinimalPack(string locale, string fallbacks, string value)
    {
        return """
        {
          "schemaVersion": 1,
          "dataVersion": "one",
          "locale": "LOCALE",
          "fallbacks": FALLBACKS,
          "reviewStatus": "reviewed",
          "provenance": [
            {
              "id": "project",
              "source": "Project authors",
              "sourceVersion": "one",
              "license": "project",
              "reviewer": "reviewer"
            }
          ],
          "fields": [
            { "name": "test.value", "provenance": "project", "value": VALUE }
          ]
        }
        """
            .Replace("\"LOCALE\"", "\"" + locale + "\"")
            .Replace("FALLBACKS", fallbacks)
            .Replace("VALUE", value);
    }

    static string ValidPack()
    {
        return """
        {
          "schemaVersion": 1,
          "dataVersion": "pack-2026.08",
          "locale": "fr-CA",
          "fallbacks": ["fr", "en-US", "en", "invariant"],
          "reviewStatus": "native-reviewed",
          "provenance": [
            {
              "id": "cldr",
              "source": "Unicode CLDR",
              "sourceVersion": "48",
              "license": "Unicode-3.0",
              "reviewer": "data-review"
            },
            {
              "id": "authored-fr",
              "source": "Project contributors",
              "sourceVersion": "2026.08",
              "license": "project",
              "reviewer": "native-review"
            }
          ],
          "fields": [
            { "name": "date.order", "provenance": "cldr", "value": "dmy" },
            { "name": "person.names", "provenance": "authored-fr", "value": ["Aline","Camille"] },
            { "name": "formats.address-line", "provenance": "cldr", "value": {"separator":" ","lines":2} }
          ]
        }
        """;
    }
}
