using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using fake;

namespace fake.Tests;

internal class BundledLocaleResource
{
    internal string Name;
    internal string Json;
    internal LocalePack Pack;
}

[TestFixture]
public class LocaleDataTests
{
    static Regex FormatToken = new Regex(
        @"\{\{(?<double>[a-z][a-z0-9-]*(?:\.[a-z][a-z0-9-]*)+)\}\}|\{(?<single>[a-z][a-z0-9-]*(?:\.[a-z][a-z0-9-]*)+)\}",
        RegexOptions.CultureInvariant);

    [Test]
    public void BundledPacksHaveRequiredKeysFallbackDeclarationsAndProvenance()
    {
        BundledLocaleResource[] resources = Resources();
        Dictionary<string, string> fallbackDeclarations = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> provenanceDeclarations = new Dictionary<string, string>(StringComparer.Ordinal);

        Assert.That(resources, Is.Not.Empty);
        foreach (BundledLocaleResource resource in resources)
        {
            using (JsonDocument document = JsonDocument.Parse(resource.Json))
            {
                Assert.That(
                    document.RootElement.EnumerateObject().Select(property => property.Name),
                    Is.EquivalentTo(new string[]
                    {
                        "schemaVersion",
                        "dataVersion",
                        "locale",
                        "fallbacks",
                        "reviewStatus",
                        "provenance",
                        "fields"
                    }),
                    resource.Name);
            }

            string localeKey = resource.Pack.DataVersion + "\0" + resource.Pack.Locale;
            string fallbackSignature = string.Join("\0", resource.Pack.Fallbacks);
            if (fallbackDeclarations.ContainsKey(localeKey))
            {
                Assert.That(fallbackSignature, Is.EqualTo(fallbackDeclarations[localeKey]), resource.Name);
            }
            fallbackDeclarations[localeKey] = fallbackSignature;

            if (resource.Pack.Locale == LocaleFieldValue.InvariantLocale)
            {
                Assert.That(resource.Pack.Fallbacks, Is.Empty, resource.Name);
            }
            else
            {
                Assert.That(resource.Pack.Fallbacks, Is.Not.Empty, resource.Name);
                Assert.That(resource.Pack.Fallbacks[^1], Is.EqualTo(LocaleFieldValue.InvariantLocale), resource.Name);
                Assert.That(resource.Pack.ReviewStatus, Does.Contain("native").IgnoreCase, resource.Name);
            }

            HashSet<string> usedProvenance = new HashSet<string>(
                resource.Pack.Fields.Select(field => field.Provenance),
                StringComparer.Ordinal);
            Assert.That(
                usedProvenance,
                Is.EquivalentTo(resource.Pack.Provenance.Select(provenance => provenance.Id)),
                resource.Name + " must not contain unused provenance records");

            foreach (LocalePackProvenance provenance in resource.Pack.Provenance)
            {
                string provenanceKey = localeKey + "\0" + provenance.Id;
                string provenanceSignature = string.Join(
                    "\0",
                    provenance.Source,
                    provenance.SourceVersion,
                    provenance.License,
                    provenance.Reviewer);
                if (provenanceDeclarations.ContainsKey(provenanceKey))
                {
                    Assert.That(
                        provenanceSignature,
                        Is.EqualTo(provenanceDeclarations[provenanceKey]),
                        resource.Name + " redeclares provenance differently");
                }
                provenanceDeclarations[provenanceKey] = provenanceSignature;
            }
        }
    }

    [Test]
    public void BundledFieldsAndVocabularyArraysContainNoDuplicates()
    {
        BundledLocaleResource[] resources = Resources();
        string[] names = resources.Select(resource => resource.Name).ToArray();
        string[] contents = resources.Select(resource => resource.Json).ToArray();
        LocaleResourceStore store = new LocaleResourceStore(
            LocaleResourceSource.FromContent(names, contents),
            10000000,
            100000);

        foreach (string version in resources.Select(resource => resource.Pack.DataVersion).Distinct(StringComparer.Ordinal))
        {
            Assert.DoesNotThrow(() => store.Values(version), "duplicate locale fields in data version " + version);
        }

        foreach (BundledLocaleResource resource in resources)
        {
            foreach (LocalePackField field in resource.Pack.Fields)
            {
                using (JsonDocument document = JsonDocument.Parse(field.ValueJson))
                {
                    ValidateStringArrayDuplicates(
                        document.RootElement,
                        resource.Name + " $.fields[" + field.Name + "].value");
                }
            }
        }
    }

    [Test]
    public void BundledStringsContainWellFormedNormalizedUnicode()
    {
        foreach (BundledLocaleResource resource in Resources())
        {
            LocaleJsonSyntax.Validate(resource.Json, resource.Name);
            using (JsonDocument document = JsonDocument.Parse(resource.Json))
            {
                ValidateUnicode(document.RootElement, resource.Name + " $");
            }
        }
    }

    [Test]
    public void BundledWeightedCollectionsAreAlignedFiniteAndUsable()
    {
        foreach (BundledLocaleResource resource in Resources())
        {
            foreach (LocalePackField field in resource.Pack.Fields)
            {
                using (JsonDocument document = JsonDocument.Parse(field.ValueJson))
                {
                    ValidateWeights(
                        document.RootElement,
                        resource.Name + " $.fields[" + field.Name + "].value");
                }
            }
        }
    }

    [Test]
    public void BundledFormatTokensReferenceFieldsInTheSameDataVersion()
    {
        BundledLocaleResource[] resources = Resources();
        Dictionary<string, HashSet<string>> fieldsByVersion = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (BundledLocaleResource resource in resources)
        {
            if (!fieldsByVersion.ContainsKey(resource.Pack.DataVersion))
            {
                fieldsByVersion[resource.Pack.DataVersion] = new HashSet<string>(StringComparer.Ordinal);
            }
            foreach (LocalePackField field in resource.Pack.Fields)
            {
                fieldsByVersion[resource.Pack.DataVersion].Add(field.Name);
            }
        }

        foreach (BundledLocaleResource resource in resources)
        {
            foreach (LocalePackField field in resource.Pack.Fields)
            {
                if (!IsFormatField(field.Name))
                {
                    continue;
                }
                using (JsonDocument document = JsonDocument.Parse(field.ValueJson))
                {
                    ValidateFormatTokens(
                        document.RootElement,
                        fieldsByVersion[resource.Pack.DataVersion],
                        resource.Name + " $.fields[" + field.Name + "].value");
                }
            }
        }
    }

    static BundledLocaleResource[] Resources()
    {
        LocaleResourceSource source = LocaleResourceSource.FromAssembly(typeof(Fake).Assembly);
        string[] names = source.Names()
            .Where(IsLocaleResource)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        BundledLocaleResource[] resources = new BundledLocaleResource[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            using (Stream stream = source.Open(names[index]))
            {
                Assert.That(stream, Is.Not.Null, names[index]);
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, false))
                {
                    string json = reader.ReadToEnd();
                    resources[index] = new BundledLocaleResource();
                    resources[index].Name = names[index];
                    resources[index].Json = json;
                    resources[index].Pack = LocalePack.Parse(json, 10000000, 100000, names[index]);
                }
            }
            index = index + 1;
        }
        return resources;
    }

    static bool IsLocaleResource(string name)
    {
        bool locale = name.StartsWith("fake.Data.locales.", StringComparison.Ordinal);
        bool invariant = name.StartsWith("fake.Data.invariant.", StringComparison.Ordinal);
        return (locale || invariant) && name.EndsWith(".json", StringComparison.Ordinal);
    }

    static void ValidateStringArrayDuplicates(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            HashSet<string> strings = new HashSet<string>(StringComparer.Ordinal);
            int index = 0;
            foreach (JsonElement item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string text = item.GetString();
                    Assert.That(strings.Add(text), Is.True, path + " duplicates string at index " + index);
                }
                ValidateStringArrayDuplicates(item, path + "[" + index + "]");
                index = index + 1;
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in value.EnumerateObject())
            {
                ValidateStringArrayDuplicates(property.Value, path + "." + property.Name);
            }
        }
    }

    static void ValidateUnicode(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            ValidateUnicodeString(value.GetString(), path);
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in value.EnumerateArray())
            {
                ValidateUnicode(item, path + "[" + index + "]");
                index = index + 1;
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in value.EnumerateObject())
            {
                ValidateUnicodeString(property.Name, path + " member name");
                ValidateUnicode(property.Value, path + "." + property.Name);
            }
        }
    }

    static void ValidateUnicodeString(string value, string path)
    {
        Assert.That(value.IsNormalized(NormalizationForm.FormC), Is.True, path + " is not NFC-normalized");
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                Assert.That(index + 1, Is.LessThan(value.Length), path + " ends with an unpaired high surrogate");
                Assert.That(char.IsLowSurrogate(value[index + 1]), Is.True, path + " contains an unpaired high surrogate");
                index = index + 2;
            }
            else
            {
                Assert.That(char.IsLowSurrogate(character), Is.False, path + " contains an unpaired low surrogate");
                index = index + 1;
            }
        }
    }

    static void ValidateWeights(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in value.EnumerateArray())
            {
                ValidateWeights(item, path + "[" + index + "]");
                index = index + 1;
            }
            return;
        }
        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (value.TryGetProperty("weights", out JsonElement weights))
        {
            Assert.That(weights.ValueKind, Is.EqualTo(JsonValueKind.Array), path + ".weights must be an array");
            Assert.That(value.TryGetProperty("values", out JsonElement values), Is.True, path + " requires values beside weights");
            Assert.That(values.ValueKind, Is.EqualTo(JsonValueKind.Array), path + ".values must be an array");
            Assert.That(weights.GetArrayLength(), Is.GreaterThan(0), path + ".weights must not be empty");
            Assert.That(weights.GetArrayLength(), Is.EqualTo(values.GetArrayLength()), path + " weights must align with values");
            bool positive = false;
            foreach (JsonElement weight in weights.EnumerateArray())
            {
                Assert.That(weight.TryGetDouble(out double number), Is.True, path + ".weights must contain numbers");
                Assert.That(double.IsNaN(number) || double.IsInfinity(number), Is.False, path + ".weights must be finite");
                Assert.That(number, Is.GreaterThanOrEqualTo(0.0), path + ".weights must be nonnegative");
                positive = positive || number > 0.0;
            }
            Assert.That(positive, Is.True, path + ".weights must contain a positive weight");
            HashSet<string> choices = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonElement choice in values.EnumerateArray())
            {
                Assert.That(choices.Add(choice.GetRawText()), Is.True, path + ".values contains a duplicate weighted choice");
            }
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            ValidateWeights(property.Value, path + "." + property.Name);
        }
    }

    static bool IsFormatField(string field)
    {
        return field.StartsWith("formats.", StringComparison.Ordinal)
            || field.EndsWith(".format", StringComparison.Ordinal)
            || field.Contains(".formats.", StringComparison.Ordinal);
    }

    static void ValidateFormatTokens(JsonElement value, HashSet<string> fields, string path)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            string format = value.GetString();
            MatchCollection matches = FormatToken.Matches(format);
            string withoutTokens = FormatToken.Replace(format, "");
            Assert.That(withoutTokens.Contains('{') || withoutTokens.Contains('}'), Is.False, path + " contains an invalid format token");
            foreach (Match match in matches)
            {
                string reference = match.Groups["double"].Success
                    ? match.Groups["double"].Value
                    : match.Groups["single"].Value;
                Assert.That(fields.Contains(reference), Is.True, path + " references missing field '" + reference + "'");
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in value.EnumerateArray())
            {
                ValidateFormatTokens(item, fields, path + "[" + index + "]");
                index = index + 1;
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in value.EnumerateObject())
            {
                ValidateFormatTokens(property.Value, fields, path + "." + property.Name);
            }
        }
    }
}
