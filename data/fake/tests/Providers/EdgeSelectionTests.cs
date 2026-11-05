using fake;

namespace fake.Tests;

[TestFixture]
public class EdgeSelectionTests
{
    [TestCase(GenerationMode.ValidUnusual)]
    [TestCase(GenerationMode.Invalid)]
    [TestCase(GenerationMode.Mixed)]
    [TestCase(GenerationMode.Adversarial)]
    public void CapturedModeDispatchMatchesItsExplicitSelection(GenerationMode mode)
    {
        FakeOptions options = FakeOptions.Create(148L);
        options.GenerationMode = mode;
        EdgeProvider captured = Fake.Create(options).Edge();
        EdgeProvider explicitProvider = Fake.Create(options).Edge();

        EdgeSelectionResult actual = captured.Select();
        EdgeSelectionResult expected = Explicit(explicitProvider, mode);

        Assert.That(Signature(actual), Is.EqualTo(Signature(expected)));
    }

    [Test]
    public void TypicalAndBoundaryModesDoNotImplicitlyChooseAnEdgePolicy()
    {
        FakeOptions typicalOptions = FakeOptions.Create(148L);
        FakeOptions boundaryOptions = FakeOptions.Create(148L);
        boundaryOptions.GenerationMode = GenerationMode.Boundary;

        InvalidOperationException typical = Assert.Throws<InvalidOperationException>(() => Fake.Create(typicalOptions).Edge().Select());
        InvalidOperationException boundary = Assert.Throws<InvalidOperationException>(() => Fake.Create(boundaryOptions).Edge().Select());

        Assert.Multiple(() =>
        {
            Assert.That(typical.Message, Does.Contain("ValidUnusual, Invalid, Mixed, or Adversarial"));
            Assert.That(boundary.Message, Is.EqualTo(typical.Message));
        });
    }

    [TestCase(GenerationMode.ValidUnusual)]
    [TestCase(GenerationMode.Invalid)]
    [TestCase(GenerationMode.Mixed)]
    [TestCase(GenerationMode.Adversarial)]
    public void EqualSeedsProduceEqualSelectionSequences(GenerationMode mode)
    {
        EdgeProvider first = Fake.Create(8148L).Edge();
        EdgeProvider second = Fake.Create(8148L).Edge();

        Assert.That(Take(first, mode, 100), Is.EqualTo(Take(second, mode, 100)));
    }

    [Test]
    public void DifferentSeedsProduceDifferentSelectionSequences()
    {
        EdgeProvider first = Fake.Create(148L).Edge();
        EdgeProvider second = Fake.Create(149L).Edge();

        Assert.That(
            Take(first, GenerationMode.Mixed, 100),
            Is.Not.EqualTo(Take(second, GenerationMode.Mixed, 100)));
    }

    [Test]
    public void ValidUnusualSelectionsHaveValidMetadataAndNoViolatedRule()
    {
        EdgeProvider edge = Fake.Create(148L).Edge();

        int index = 0;
        while (index < 250)
        {
            EdgeSelectionResult result = edge.ValidUnusual();

            Assert.Multiple(() =>
            {
                Assert.That(result.GenerationMode, Is.EqualTo(GenerationMode.ValidUnusual));
                Assert.That(result.Validity, Is.EqualTo(ValidityMode.Valid));
                Assert.That(result.ViolatedRule, Is.Empty);
                Assert.That(result.Category, Is.Not.EqualTo(EdgeCaseCategory.Injection));
                Assert.That(result.SelectedEdge, Is.EqualTo(result.Edge.Name));
            });
            index++;
        }
    }

    [Test]
    public void InvalidSelectionExposesEverySelectedEdgesExactViolatedRule()
    {
        Dictionary<string, string> expected = InvalidRules();
        Dictionary<string, string> actual = new Dictionary<string, string>();
        EdgeProvider edge = Fake.Create(148L).Edge();

        int index = 0;
        while (index < 1000)
        {
            EdgeSelectionResult result = edge.Invalid();
            actual[result.SelectedEdge] = result.ViolatedRule;

            Assert.Multiple(() =>
            {
                Assert.That(result.GenerationMode, Is.EqualTo(GenerationMode.Invalid));
                Assert.That(result.Validity, Is.EqualTo(ValidityMode.Invalid));
                Assert.That(result.ViolatedRule, Is.EqualTo(expected[result.SelectedEdge]));
                Assert.That(result.Category, Is.Not.EqualTo(EdgeCaseCategory.Injection));
            });
            index++;
        }

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void AdversarialSelectionUsesOnlyInertInjectionRecordsAndNamesRules()
    {
        Dictionary<string, string> expected = AdversarialRules();
        Dictionary<string, string> actual = new Dictionary<string, string>();
        EdgeProvider edge = Fake.Create(148L).Edge();

        int index = 0;
        while (index < 400)
        {
            EdgeSelectionResult result = edge.Adversarial();
            actual[result.SelectedEdge] = result.ViolatedRule;

            Assert.Multiple(() =>
            {
                Assert.That(result.GenerationMode, Is.EqualTo(GenerationMode.Adversarial));
                Assert.That(result.Category, Is.EqualTo(EdgeCaseCategory.Injection));
                Assert.That(result.Validity, Is.EqualTo(ValidityMode.Invalid));
                Assert.That(result.Edge.Value, Is.TypeOf<string>());
                Assert.That(result.ViolatedRule, Is.EqualTo(expected[result.SelectedEdge]));
            });
            index++;
        }

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void MixedSelectionIncludesValidInvalidAndAdversarialOutcomes()
    {
        EdgeProvider edge = Fake.Create(148L).Edge();
        HashSet<ValidityMode> validities = new HashSet<ValidityMode>();
        bool sawAdversarial = false;
        bool sawNonAdversarial = false;

        int index = 0;
        while (index < 300)
        {
            EdgeSelectionResult result = edge.Mixed();
            validities.Add(result.Validity);
            sawAdversarial = sawAdversarial || result.Category == EdgeCaseCategory.Injection;
            sawNonAdversarial = sawNonAdversarial || result.Category != EdgeCaseCategory.Injection;

            Assert.That(result.GenerationMode, Is.EqualTo(GenerationMode.Mixed));
            if (result.Validity == ValidityMode.Valid)
                Assert.That(result.ViolatedRule, Is.Empty);
            else
                Assert.That(result.ViolatedRule, Is.Not.Empty);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(validities, Is.EquivalentTo(new ValidityMode[] { ValidityMode.Valid, ValidityMode.Invalid }));
            Assert.That(sawAdversarial, Is.True);
            Assert.That(sawNonAdversarial, Is.True);
        });
    }

    [Test]
    public void SelectionMetadataCarriesIndependentCapturedModes()
    {
        FakeOptions options = FakeOptions.Create(148L);
        options.SafetyMode = SafetyMode.UnrestrictedShape;
        options.StrictnessMode = StrictnessMode.Strict;
        options.NormalizationMode = NormalizationMode.Nfkc;
        EdgeProvider edge = Fake.Create(options).Edge();
        options.SafetyMode = SafetyMode.Reserved;
        options.StrictnessMode = StrictnessMode.AllowFallback;
        options.NormalizationMode = NormalizationMode.None;

        EdgeSelectionResult result = edge.Invalid();

        Assert.Multiple(() =>
        {
            Assert.That(result.SafetyMode, Is.EqualTo(SafetyMode.UnrestrictedShape));
            Assert.That(result.StrictnessMode, Is.EqualTo(StrictnessMode.Strict));
            Assert.That(result.NormalizationMode, Is.EqualTo(NormalizationMode.Nfkc));
        });
    }

    [Test]
    public void StableSelectionMetadataDoesNotChangeWhenRecordIsMutated()
    {
        EdgeSelectionResult result = Fake.Create(148L).Edge().Invalid();
        string selectedEdge = result.SelectedEdge;
        EdgeCaseCategory category = result.Category;
        result.Edge.Name = "changed";
        result.Edge.Category = EdgeCaseCategory.Injection;

        Assert.Multiple(() =>
        {
            Assert.That(result.SelectedEdge, Is.EqualTo(selectedEdge));
            Assert.That(result.Category, Is.EqualTo(category));
        });
    }

    static EdgeSelectionResult Explicit(EdgeProvider edge, GenerationMode mode)
    {
        if (mode == GenerationMode.ValidUnusual)
            return edge.ValidUnusual();
        if (mode == GenerationMode.Invalid)
            return edge.Invalid();
        if (mode == GenerationMode.Mixed)
            return edge.Mixed();
        return edge.Adversarial();
    }

    static string[] Take(EdgeProvider edge, GenerationMode mode, int count)
    {
        string[] result = new string[count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Signature(Explicit(edge, mode));
            index++;
        }
        return result;
    }

    static string Signature(EdgeSelectionResult result)
    {
        return result.GenerationMode + "|" +
            result.Category + "|" +
            result.SelectedEdge + "|" +
            result.Validity + "|" +
            result.ViolatedRule;
    }

    static Dictionary<string, string> InvalidRules()
    {
        return new Dictionary<string, string>
        {
            ["null-reference"] = "value must not be null",
            ["empty-string"] = "value must not be empty",
            ["ascii-space"] = "value must not be whitespace-only",
            ["mixed-whitespace"] = "value must not be whitespace-only",
            ["positive-infinity"] = "number must be finite",
            ["negative-infinity"] = "number must be finite",
            ["not-a-number"] = "number must be finite",
            ["unpaired-high-surrogate"] = "text must contain well-formed UTF-16",
            ["wrong-checksum"] = "identifier checksum must be valid",
            ["bad-length"] = "identifier length must be supported",
            ["invalid-character"] = "identifier characters must be permitted",
            ["forbidden-prefix"] = "identifier prefix must be permitted",
            ["bad-uuid-version"] = "UUID version must be supported",
            ["ulid-overflow"] = "ULID text must fit in 128 bits",
            ["invalid-iban-country"] = "IBAN country and length must be supported",
            ["malformed-idn"] = "IDN label must contain a complete encoding",
            ["ambiguous-phone"] = "phone extension formatting must be unambiguous",
            ["reversed-numeric-range"] = "minimum must not exceed maximum",
            ["negative-count"] = "count must not be negative",
            ["empty-exclusive-time-range"] = "exclusive interval must contain a representable value",
            ["conflicting-required-value"] = "one field cannot equal two distinct required values",
            ["required-and-forbidden"] = "one feature cannot be both required and forbidden"
        };
    }

    static Dictionary<string, string> AdversarialRules()
    {
        return new Dictionary<string, string>
        {
            ["sql"] = "SQL input must be treated as data",
            ["html"] = "HTML input must be escaped for its output context",
            ["spreadsheet"] = "spreadsheet formulas must be guarded",
            ["shell"] = "shell input must not be executed",
            ["path-traversal"] = "path input must remain within its allowed root",
            ["format-string"] = "format input must not be interpreted as directives",
            ["template"] = "template input must not be evaluated",
            ["header-splitting"] = "header values must not contain record separators"
        };
    }
}
