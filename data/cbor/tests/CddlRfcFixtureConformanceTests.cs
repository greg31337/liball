using System.Text.Json;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlRfcFixtureConformanceTests
{
    [TestCaseSource(nameof(RfcFixtures))]
    public void EveryRfcFixtureAcceptsExamplesAndRejectsTargetedMutations(
        CddlRfcFixture fixture)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            fixture.Cddl,
            new CddlParseOptions
            {
                SourceName = $"{fixture.Rfc}-{fixture.Name}.cddl"
            });
        Assert.Multiple(() =>
        {
            Assert.That(parse.IsSuccess, Is.True, fixture.Context);
            Assert.That(parse.Diagnostics, Is.Empty, fixture.Context);
            Assert.That(parse.Document, Is.Not.Null, fixture.Context);
        });

        CddlCompileResult compile = new CddlCompiler().TryCompileSchema(
            parse.Document!,
            new CddlCompileOptions { RootRuleName = fixture.Rule });
        Assert.Multiple(() =>
        {
            Assert.That(compile.IsSuccess, Is.True, fixture.Context);
            Assert.That(compile.Diagnostics, Is.Empty, fixture.Context);
            Assert.That(compile.Schema, Is.Not.Null, fixture.Context);
        });

        CddlSchema schema = compile.Schema!;
        foreach (string diagnostic in fixture.Valid)
        {
            CborValue value = ParseDiagnostic(diagnostic, fixture.Context);
            CddlValidationResult direct = Validate(schema, value, fixture);
            CddlValidationResult encoded = ValidateEncoded(schema, value, fixture);

            Assert.Multiple(() =>
            {
                Assert.That(
                    direct.IsValid,
                    Is.True,
                    $"{fixture.Context} positive {diagnostic}: {FirstIssue(direct)}");
                Assert.That(direct.Issues, Is.Empty, fixture.Context);
                Assert.That(
                    encoded.IsValid,
                    Is.True,
                    $"{fixture.Context} encoded positive {diagnostic}: {FirstIssue(encoded)}");
                Assert.That(encoded.Issues, Is.Empty, fixture.Context);
            });
        }

        foreach (string diagnostic in fixture.Mutations)
        {
            CborValue value = ParseDiagnostic(diagnostic, fixture.Context);
            CddlValidationResult direct = Validate(schema, value, fixture);
            CddlValidationResult encoded = ValidateEncoded(schema, value, fixture);

            Assert.Multiple(() =>
            {
                Assert.That(
                    direct.IsValid,
                    Is.False,
                    $"{fixture.Context} mutation unexpectedly matched: {diagnostic}");
                Assert.That(direct.Issues, Is.Not.Empty, fixture.Context);
                Assert.That(
                    encoded.IsValid,
                    Is.False,
                    $"{fixture.Context} encoded mutation unexpectedly matched: {diagnostic}");
                Assert.That(encoded.Issues, Is.Not.Empty, fixture.Context);
            });
        }
    }

    [Test]
    public void FixtureInventoryCoversEveryRequiredRfcAndBothOutcomeClasses()
    {
        List<CddlRfcFixture> fixtures = LoadFixtures();

        Assert.Multiple(() =>
        {
            Assert.That(fixtures.Select(item => item.Rfc).Distinct(),
                Is.EquivalentTo(new[]
                {
                    "RFC 8610",
                    "RFC 9165",
                    "RFC 9682",
                    "RFC 9741"
                }));
            Assert.That(fixtures, Has.Count.EqualTo(17));
            Assert.That(fixtures.Sum(item => item.Valid.Count), Is.EqualTo(26));
            Assert.That(fixtures.Sum(item => item.Mutations.Count), Is.EqualTo(43));
            Assert.That(fixtures.All(item => item.Valid.Count > 0), Is.True);
            Assert.That(fixtures.All(item => item.Mutations.Count > 0), Is.True);
        });
    }

    private static IEnumerable<TestCaseData> RfcFixtures()
    {
        foreach (CddlRfcFixture fixture in LoadFixtures())
        {
            yield return new TestCaseData(fixture).SetName(
                $"{fixture.Rfc.Replace(" ", "")}_{fixture.Name}");
        }
    }

    private static List<CddlRfcFixture> LoadFixtures()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "..",
            "specs",
            "test-vectors",
            "cddl-rfc-conformance.json"));
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        List<CddlRfcFixture> fixtures = new List<CddlRfcFixture>();
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            CddlRfcFixture fixture = new CddlRfcFixture();
            fixture.Rfc = element.GetProperty("rfc").GetString()!;
            fixture.Name = element.GetProperty("name").GetString()!;
            fixture.Rule = element.GetProperty("rule").GetString()!;
            fixture.Cddl = element.GetProperty("cddl").GetString()!;
            fixture.Context = $"{fixture.Rfc} {fixture.Name}";
            JsonElement features;
            if (element.TryGetProperty("features", out features))
            {
                fixture.Features = features
                    .EnumerateArray()
                    .Select(item => item.GetString()!)
                    .ToArray();
            }
            fixture.Valid = element.GetProperty("valid")
                .EnumerateArray()
                .Select(item => item.GetString()!)
                .ToList();
            fixture.Mutations = element.GetProperty("mutations")
                .EnumerateArray()
                .Select(item => item.GetString()!)
                .ToList();
            fixtures.Add(fixture);
        }
        return fixtures;
    }

    private static CborValue ParseDiagnostic(string diagnostic, string context)
    {
        CborReadResult result = new CborDiagnosticConverter().ParseDiagnostic(
            diagnostic,
            new CborDiagnosticOptions());
        Assert.That(
            result.Status,
            Is.EqualTo(CborReadStatus.Done),
            $"{context} diagnostic {diagnostic}: {result.ErrorMessage}");
        Assert.That(result.Value, Is.Not.Null, context);
        return result.Value!;
    }

    private static CddlValidationResult Validate(
        CddlSchema schema,
        CborValue value,
        CddlRfcFixture fixture)
    {
        return new CddlValidator().ValidateCbor(
            schema,
            value,
            ValidationOptions(fixture));
    }

    private static CddlValidationResult ValidateEncoded(
        CddlSchema schema,
        CborValue value,
        CddlRfcFixture fixture)
    {
        byte[] encoded = new CborEncoder().Encode(
            value,
            new CborEncodeOptions());
        return new CddlValidator().ValidateEncodedCbor(
            schema,
            encoded,
            new CborDecodeOptions
            {
                DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve,
                TagPolicy = CborTagPolicy.Preserve,
                SharedValuePolicy = CborSharedValuePolicy.Preserve
            },
            ValidationOptions(fixture));
    }

    private static CddlValidationOptions ValidationOptions(CddlRfcFixture fixture)
    {
        return new CddlValidationOptions
        {
            RootRuleName = fixture.Rule,
            EnabledFeatures = fixture.Features.ToArray()
        };
    }

    private static string FirstIssue(CddlValidationResult result)
    {
        if (result.Issues.Length == 0)
        {
            return "no issue";
        }
        return result.Issues[0].Message;
    }

    public class CddlRfcFixture
    {
        public string Rfc = "";
        public string Name = "";
        public string Rule = "";
        public string Cddl = "";
        public string[] Features = [];
        public List<string> Valid = new List<string>();
        public List<string> Mutations = new List<string>();
        public string Context = "";

        public override string ToString()
        {
            return $"{Rfc} {Name}";
        }
    }
}
