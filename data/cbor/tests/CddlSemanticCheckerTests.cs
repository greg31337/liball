using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlSemanticCheckerTests
{
    private sealed class TestControlHandler : ICddlControlHandler
    {
        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            return new CddlControlResult
            {
                Status = CddlControlStatus.Applied
            };
        }

        public CddlControlResult GenerateControl(
            CddlNode target,
            CddlNode controller,
            CddlGenerationContext context)
        {
            return new CddlControlResult
            {
                Status = CddlControlStatus.Applied
            };
        }
    }

    [Test]
    public void AcceptsValidSemanticConstructsAcrossAllContexts()
    {
        CddlSemanticResult result = Check(
            "root = {\n"
            + "  ? sized: bstr .size 4,\n"
            + "  flags: uint .bits (0..7),\n"
            + "  pattern: tstr .regexp \"[a-z]+\",\n"
            + "  payload: bstr .cbor uint,\n"
            + "  ? count: (uint .gt 0) .default 1,\n"
            + "  nested: [~array-base],\n"
            + "  color: &colors,\n"
            + "  tagged: #6.<tag-range>(tstr),\n"
            + "  simple: #7.<simple-range>,\n"
            + "  tstr ^ => any\n"
            + "}\n"
            + "array-base = [uint]\n"
            + "colors = (red: 1, blue: 2,)\n"
            + "tag-range = 100..200\n"
            + "simple-range = 0..255\n"
            + "empty-range = 10..0",
            new CddlCompileOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ValidatesNumericRangeEndpointKindsAndSingletons()
    {
        CddlSemanticResult result = Check(
            "root = good\n"
            + "good = low .. high\n"
            + "low = 0\n"
            + "high = 10\n"
            + "mixed = 0..1.0\n"
            + "nonnumeric = tstr .. 10",
            new CddlCompileOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_RANGE_KIND_MISMATCH",
                    "CDDL_COMPILE_RANGE_ENDPOINT_INVALID"
                }));
            Assert.That(result.Diagnostics.Select(item => item.RuleName),
                Is.EqualTo(new string[] { "mixed", "nonnumeric" }));
        });
    }

    [Test]
    public void ValidatesControlRegistrationOperandsAndDefaultContext()
    {
        CddlSemanticResult result = Check(
            "root = bad-size / bad-regexp / bad-rel / bad-unknown / bad-default\n"
            + "bad-size = tstr .size \"four\"\n"
            + "bad-regexp = uint .regexp \"[0-9]+\"\n"
            + "bad-rel = uint .lt (0 / 1)\n"
            + "bad-unknown = tstr .mystery 1\n"
            + "bad-default = uint .default 0",
            new CddlCompileOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID",
                    "CDDL_COMPILE_CONTROL_TARGET_INVALID",
                    "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID",
                    "CDDL_COMPILE_CONTROL_UNKNOWN",
                    "CDDL_COMPILE_DEFAULT_CONTEXT_INVALID"
                }));
            Assert.That(result.Diagnostics.Select(item => item.RuleName),
                Is.EqualTo(new string[]
                {
                    "bad-size", "bad-regexp", "bad-rel", "bad-unknown",
                    "bad-default"
                }));
        });
    }

    [Test]
    public void RejectsNegativeSizeAndBitDomains()
    {
        CddlSemanticResult result = Check(
            "root = signed-size / negative-bits\n"
            + "signed-size = nint .size 1\n"
            + "negative-bits = uint .bits (-1 .. 2)",
            new CddlCompileOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_CONTROL_TARGET_INVALID",
                    "CDDL_COMPILE_CONTROL_CONTROLLER_INVALID"
                }));
            Assert.That(result.Diagnostics.Select(item => item.RuleName),
                Is.EqualTo(new string[] { "signed-size", "negative-bits" }));
        });
    }

    [Test]
    public void AcceptsAnEnabledCustomControlRegistration()
    {
        CddlCompileOptions options = new CddlCompileOptions();
        options.ControlRegistrations.Add(new CddlControlRegistration
        {
            Name = ".application",
            Handler = new TestControlHandler()
        });
        CddlSemanticResult result = Check(
            "root = uint .application \"controller\"",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ValidatesOccurrencesMapLabelsAndCutPlacement()
    {
        CddlSemanticResult result = Check(
            "root = [3*2 int, tstr ^ => int]\n"
            + "bad-map = {int}\n"
            + "good-map = {tstr ^ => int}",
            new CddlCompileOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_OCCURRENCE_INVALID",
                    "CDDL_COMPILE_CUT_CONTEXT_INVALID",
                    "CDDL_COMPILE_MAP_LABEL_MISSING"
                }));
            Assert.That(result.Diagnostics.Select(item => item.RuleName),
                Is.EqualTo(new string[] { "root", "root", "bad-map" }));
        });
    }

    [Test]
    public void ValidatesUnwrapTargetsAndExposedContext()
    {
        CddlSemanticResult result = Check(
            "root = tagged-content / [~array-base] / bad-scalar / bad-context\n"
            + "tagged-content = ~tagged\n"
            + "tagged = #6.1(uint)\n"
            + "array-base = [uint]\n"
            + "scalar = 1\n"
            + "bad-scalar = ~scalar\n"
            + "bad-context = ~array-base",
            new CddlCompileOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_UNWRAP_TARGET_INVALID",
                    "CDDL_COMPILE_CONTEXT_KIND_MISMATCH"
                }));
            Assert.That(result.Diagnostics.Select(item => item.RuleName),
                Is.EqualTo(new string[] { "bad-scalar", "bad-context" }));
        });
    }

    [Test]
    public void ValidatesLiteralAndTypeValuedHeadArguments()
    {
        CddlSemanticResult result = Check(
            "root = bad-ai / bad-simple / bad-simple-high / bad-tag / bad-typed-tag\n"
            + "bad-ai = #0.31\n"
            + "bad-simple = #7.28\n"
            + "bad-simple-high = #7.256\n"
            + "bad-tag = #6.18446744073709551616(any)\n"
            + "bad-typed-tag = #6.<-1..2>(any)",
            new CddlCompileOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_REPRESENTATION_ARGUMENT_INVALID",
                    "CDDL_COMPILE_REPRESENTATION_ARGUMENT_INVALID",
                    "CDDL_COMPILE_REPRESENTATION_ARGUMENT_INVALID",
                    "CDDL_COMPILE_TAG_ARGUMENT_INVALID",
                    "CDDL_COMPILE_TAG_ARGUMENT_INVALID"
                }));
        });
    }

    [Test]
    public void RejectsANonGroupEnumerationTarget()
    {
        CddlNormalizationResult normalization = Normalize(
            "root = &fields\nfields = (red: 1,)",
            NoPrelude());
        CddlNormalizedNode enumeration = normalization.RootRule!.Value!;
        enumeration.ReferenceKind = CddlRuleKind.Type;
        CddlSemanticResult result = new CddlSemanticChecker(
            normalization,
            NoPrelude()
        ).CheckDocument();

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_GROUP_ENUM_TARGET_INVALID"));
            Assert.That(result.Diagnostics[0].RuleName, Is.EqualTo("root"));
        });
    }

    private static CddlSemanticResult Check(
        string source,
        CddlCompileOptions options)
    {
        CddlNormalizationResult normalization = Normalize(source, options);
        return new CddlSemanticChecker(
            normalization,
            options
        ).CheckDocument();
    }

    private static CddlNormalizationResult Normalize(
        string source,
        CddlCompileOptions options)
    {
        CddlParseResult parse = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "semantic.cddl" });
        Assert.That(parse.Diagnostics, Is.Empty);
        CddlDocument document = parse.Document!;
        CddlRule[] loaded = new CddlPreludeLoader().LoadRules(document, options);
        CddlRuleTable table = new CddlRuleTable(loaded, options);
        Assert.That(table.Diagnostics, Is.Empty);
        CddlResolutionResult resolution =
            new CddlReferenceResolver(table, document, options).ResolveDocument();
        Assert.That(resolution.Diagnostics, Is.Empty);
        CddlGenericExpansionResult expansion = new CddlGenericExpander(
            table,
            resolution,
            options
        ).ExpandDocument();
        Assert.That(expansion.Diagnostics, Is.Empty);
        CddlNormalizationResult normalization = new CddlSchemaNormalizer(
            table,
            resolution,
            expansion,
            options
        ).NormalizeDocument();
        Assert.That(normalization.Diagnostics, Is.Empty);
        return normalization;
    }

    private static CddlCompileOptions NoPrelude()
    {
        return new CddlCompileOptions
        {
            IncludeStandardPrelude = false
        };
    }
}
