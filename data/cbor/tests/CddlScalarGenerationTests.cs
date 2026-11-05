using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlScalarGenerationTests
{
    [Test]
    public void GeneratesEveryScalarLiteralKindExactly()
    {
        Assert.Multiple(() =>
        {
            AssertInteger("root = 42", 42);
            AssertInteger("root = -17", -17);
            AssertFloat("root = 1.5", 1.5);
            AssertText("root = \"hello\"", "hello");
            AssertBytes("root = 'hello'", [104, 101, 108, 108, 111]);
            AssertBytes("root = h'00ff'", [0, 255]);
            AssertBytes("root = b64'AQID'", [1, 2, 3]);
        });
    }

    [TestCase("any")]
    [TestCase("uint")]
    [TestCase("nint")]
    [TestCase("int")]
    [TestCase("bstr")]
    [TestCase("bytes")]
    [TestCase("tstr")]
    [TestCase("text")]
    [TestCase("tdate")]
    [TestCase("time")]
    [TestCase("number")]
    [TestCase("biguint")]
    [TestCase("bignint")]
    [TestCase("bigint")]
    [TestCase("integer")]
    [TestCase("unsigned")]
    [TestCase("eb64url")]
    [TestCase("eb64legacy")]
    [TestCase("eb16")]
    [TestCase("encoded-cbor")]
    [TestCase("uri")]
    [TestCase("b64url")]
    [TestCase("b64legacy")]
    [TestCase("regexp")]
    [TestCase("mime-message")]
    [TestCase("cbor-any")]
    [TestCase("float16")]
    [TestCase("float32")]
    [TestCase("float64")]
    [TestCase("float16-32")]
    [TestCase("float32-64")]
    [TestCase("float")]
    [TestCase("false")]
    [TestCase("true")]
    [TestCase("bool")]
    [TestCase("nil")]
    [TestCase("null")]
    [TestCase("undefined")]
    public void GeneratesEveryScalarCapablePreludeRule(string prelude)
    {
        CddlSchema schema = Compile("root = " + prelude);
        CddlGenerationResult result = Generate(
            schema,
            Options(CddlGenerationStrategy.Minimal, 0));

        AssertSuccessfulAndValid(schema, result);
    }

    [Test]
    public void MinimalRangesChooseTheValueClosestToZero()
    {
        CddlGenerationResult positive = Generate(
            Compile("root = 10..20"),
            Options(CddlGenerationStrategy.Minimal, 0));
        CddlGenerationResult negative = Generate(
            Compile("root = -20..-10"),
            Options(CddlGenerationStrategy.Minimal, 0));
        CddlGenerationResult crossing = Generate(
            Compile("root = -10..20"),
            Options(CddlGenerationStrategy.Minimal, 0));
        CddlGenerationResult exclusive = Generate(
            Compile("root = 10...11"),
            Options(CddlGenerationStrategy.Minimal, 0));
        CddlGenerationResult floating = Generate(
            Compile("root = -2.0..2.0"),
            Options(CddlGenerationStrategy.Minimal, 0));

        Assert.Multiple(() =>
        {
            Assert.That(positive.Value!.IntegerValue,
                Is.EqualTo(new BigInteger(10)));
            Assert.That(negative.Value!.IntegerValue,
                Is.EqualTo(new BigInteger(-10)));
            Assert.That(crossing.Value!.IntegerValue,
                Is.EqualTo(BigInteger.Zero));
            Assert.That(exclusive.Value!.IntegerValue,
                Is.EqualTo(new BigInteger(10)));
            Assert.That(floating.Value!.FloatValue!.Value,
                Is.EqualTo(0.0));
        });
    }

    [Test]
    public void BoundaryRangesChooseSeededInclusiveOrExclusiveEndpoints()
    {
        CddlSchema inclusiveSchema = Compile("root = 10..20");
        CddlSchema exclusiveSchema = Compile("root = 10...20");
        CddlGenerationResult first = Generate(
            inclusiveSchema,
            Options(CddlGenerationStrategy.Boundary, 9));
        CddlGenerationResult repeated = Generate(
            inclusiveSchema,
            Options(CddlGenerationStrategy.Boundary, 9));
        CddlGenerationResult other = Generate(
            inclusiveSchema,
            Options(CddlGenerationStrategy.Boundary, 10));
        CddlGenerationResult exclusive = Generate(
            exclusiveSchema,
            Options(CddlGenerationStrategy.Boundary, 9));

        Assert.Multiple(() =>
        {
            Assert.That(first.Value!.IntegerValue,
                Is.AnyOf(new BigInteger(10), new BigInteger(20)));
            Assert.That(repeated.Value!.IntegerValue,
                Is.EqualTo(first.Value.IntegerValue));
            Assert.That(other.Value!.IntegerValue,
                Is.AnyOf(new BigInteger(10), new BigInteger(20)));
            Assert.That(exclusive.Value!.IntegerValue,
                Is.AnyOf(new BigInteger(10), new BigInteger(19)));
            AssertSuccessfulAndValid(inclusiveSchema, first);
            AssertSuccessfulAndValid(exclusiveSchema, exclusive);
        });
    }

    [Test]
    public void RepresentativeRangesAreSeededReproducibleAndBounded()
    {
        CddlSchema schema = Compile("root = 100..1000000");
        CddlGenerationResult first = Generate(
            schema,
            Options(CddlGenerationStrategy.Representative, 1234));
        CddlGenerationResult repeated = Generate(
            schema,
            Options(CddlGenerationStrategy.Representative, 1234));
        HashSet<BigInteger> values = new HashSet<BigInteger>();
        int seed = 0;
        while (seed < 8)
        {
            CddlGenerationResult sample = Generate(
                schema,
                Options(CddlGenerationStrategy.Representative, seed));
            values.Add(sample.Value!.IntegerValue);
            AssertSuccessfulAndValid(schema, sample);
            seed = seed + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(first.Value!.IntegerValue,
                Is.EqualTo(repeated.Value!.IntegerValue));
            Assert.That(values.Count, Is.GreaterThan(1));
        });
    }

    [TestCase("float16", CborFloatWidth.Half)]
    [TestCase("float32", CborFloatWidth.Single)]
    [TestCase("float64", CborFloatWidth.Double)]
    public void FloatPreludeGenerationRetainsTheRequiredWidth(
        string prelude,
        CborFloatWidth expectedWidth)
    {
        CddlSchema schema = Compile("root = " + prelude);
        CddlGenerationResult minimal = Generate(
            schema,
            Options(CddlGenerationStrategy.Minimal, 1));
        CddlGenerationResult boundary = Generate(
            schema,
            Options(CddlGenerationStrategy.Boundary, 2));
        CddlGenerationResult representative = Generate(
            schema,
            Options(CddlGenerationStrategy.Representative, 3));

        Assert.Multiple(() =>
        {
            AssertFloatWidth(minimal, expectedWidth);
            AssertFloatWidth(boundary, expectedWidth);
            AssertFloatWidth(representative, expectedWidth);
            AssertSuccessfulAndValid(schema, minimal);
            AssertSuccessfulAndValid(schema, boundary);
            AssertSuccessfulAndValid(schema, representative);
        });
    }

    [Test]
    public void GeneratesFixedAndTypeValuedSemanticTags()
    {
        CddlSchema fixedSchema = Compile("root = #6.42(\"inside\")");
        CddlSchema rangedSchema = Compile("root = #6.<40..45>(uint)");
        CddlGenerationResult fixedTag = Generate(
            fixedSchema,
            Options(CddlGenerationStrategy.Minimal, 0));
        CddlGenerationResult rangedTag = Generate(
            rangedSchema,
            Options(CddlGenerationStrategy.Boundary, 7));

        Assert.Multiple(() =>
        {
            Assert.That(fixedTag.Value!.Kind, Is.EqualTo(CborValueKind.Tag));
            Assert.That(fixedTag.Value.TagNumber, Is.EqualTo(42));
            Assert.That(fixedTag.Value.TaggedValue!.TextStringValue,
                Is.EqualTo("inside"));
            Assert.That(rangedTag.Value!.TagNumber, Is.AnyOf(40, 45));
            Assert.That(rangedTag.Value.TaggedValue!.Kind,
                Is.EqualTo(CborValueKind.UnsignedInteger));
            AssertSuccessfulAndValid(fixedSchema, fixedTag);
            AssertSuccessfulAndValid(rangedSchema, rangedTag);
        });
    }

    [Test]
    public void GeneratesExplicitIntegerStringAndSimpleRepresentations()
    {
        CddlSchema unsignedSchema = Compile("root = #0.24");
        CddlSchema negativeSchema = Compile("root = #1.25");
        CddlSchema bytesSchema = Compile("root = #2.3");
        CddlSchema textSchema = Compile("root = #3.24");
        CddlSchema simpleSchema = Compile("root = #7.42");
        CddlGenerationResult unsigned = Generate(
            unsignedSchema,
            Options(CddlGenerationStrategy.Boundary, 4));
        CddlGenerationResult negative = Generate(
            negativeSchema,
            Options(CddlGenerationStrategy.Boundary, 5));
        CddlGenerationResult bytes = Generate(
            bytesSchema,
            Options(CddlGenerationStrategy.Minimal, 0));
        CddlGenerationResult text = Generate(
            textSchema,
            Options(CddlGenerationStrategy.Minimal, 0));
        CddlGenerationResult simple = Generate(
            simpleSchema,
            Options(CddlGenerationStrategy.Minimal, 0));

        Assert.Multiple(() =>
        {
            Assert.That(unsigned.Value!.IntegerValue,
                Is.InRange(new BigInteger(24), new BigInteger(255)));
            Assert.That(negative.Value!.IntegerValue,
                Is.InRange(new BigInteger(-65536), new BigInteger(-257)));
            Assert.That(bytes.Value!.ByteStringValue, Has.Length.EqualTo(3));
            Assert.That(text.Value!.TextStringValue, Has.Length.EqualTo(24));
            Assert.That(simple.Value!.SimpleValue, Is.EqualTo(42));
            AssertSuccessfulAndValid(unsignedSchema, unsigned);
            AssertSuccessfulAndValid(negativeSchema, negative);
            AssertSuccessfulAndValid(bytesSchema, bytes);
            AssertSuccessfulAndValid(textSchema, text);
            AssertSuccessfulAndValid(simpleSchema, simple);
        });
    }

    [Test]
    public void SeededChoicesAreStableAndExploreAlternatives()
    {
        CddlSchema schema = Compile(
            "root = \"red\" / \"green\" / \"blue\"");
        CddlGenerationResult first = Generate(
            schema,
            Options(CddlGenerationStrategy.Representative, 101));
        CddlGenerationResult repeated = Generate(
            schema,
            Options(CddlGenerationStrategy.Representative, 101));
        HashSet<string> values = new HashSet<string>();
        int seed = 0;
        while (seed < 12)
        {
            CddlGenerationResult sample = Generate(
                schema,
                Options(CddlGenerationStrategy.Representative, seed));
            values.Add(sample.Value!.TextStringValue!);
            AssertSuccessfulAndValid(schema, sample);
            seed = seed + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(first.Value!.TextStringValue,
                Is.EqualTo(repeated.Value!.TextStringValue));
            Assert.That(values.Count, Is.GreaterThan(1));
        });
    }

    [Test]
    public void FollowsNonRecursiveRulesAndSelectsAnExplicitRoot()
    {
        CddlSchema schema = Compile(
            "root = false\n"
            + "answer = alias\n"
            + "alias = 42");
        CddlGenerationOptions options = Options(
            CddlGenerationStrategy.Minimal,
            0);
        options.RootRuleName = "answer";
        CddlGenerationResult result = Generate(schema, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.RuleName, Is.EqualTo("answer"));
            Assert.That(result.Options.RootRuleName, Is.EqualTo("answer"));
            Assert.That(result.Value!.IntegerValue,
                Is.EqualTo(new BigInteger(42)));
            Assert.That(new CddlValidator().ValidateCbor(
                schema,
                result.Value,
                new CddlValidationOptions { RootRuleName = "answer" })
                .IsValid, Is.True);
        });
    }

    [Test]
    public void SnapshotsGenerationOptionsForTheOperation()
    {
        CddlGenerationOptions options = Options(
            CddlGenerationStrategy.Representative,
            55);
        options.EnabledFeatures = new string[] { "alpha" };
        CddlGenerationResult result = Generate(
            Compile("root = uint"),
            options);
        options.Seed = 99;
        options.EnabledFeatures[0] = "mutated";
        options.MaxSteps = 1;

        Assert.Multiple(() =>
        {
            Assert.That(result.Options, Is.Not.SameAs(options));
            Assert.That(result.Options.Seed, Is.EqualTo(55));
            Assert.That(result.Options.EnabledFeatures,
                Is.EqualTo(new string[] { "alpha" }));
            Assert.That(result.Options.MaxSteps, Is.EqualTo(100000));
        });
    }

    [Test]
    public void ReturnsStructuredFailuresForInvalidInputsAndFutureConstructs()
    {
        ICddlValueGenerator generator = new CddlValueGenerator();
        CddlGenerationResult missingSchema = generator.GenerateValue(
            null!,
            new CddlGenerationOptions());
        CddlGenerationResult missingOptions = generator.GenerateValue(
            Compile("root = 1"),
            null!);
        CddlGenerationResult missingRule = Generate(
            Compile("root = 1"),
            new CddlGenerationOptions { RootRuleName = "missing" });
        CddlGenerationResult unsupported = Generate(
            Compile("root = ~wrapped\nwrapped = #6.1(#0)"),
            new CddlGenerationOptions());
        CddlGenerationResult steps = Generate(
            Compile("root = alias\nalias = 1"),
            new CddlGenerationOptions { MaxSteps = 1 });
        CddlGenerationResult attempts = Generate(
            Compile("root = 1 / 2"),
            new CddlGenerationOptions { MaxAttempts = 0 });

        Assert.Multiple(() =>
        {
            AssertFailure(missingSchema, "CDDL_GENERATION_SCHEMA_MISSING");
            AssertFailure(missingOptions, "CDDL_GENERATION_OPTIONS_MISSING");
            AssertFailure(missingRule, "CDDL_GENERATION_RULE_NOT_FOUND");
            AssertFailure(unsupported, "CDDL_GENERATION_NODE_UNSUPPORTED");
            AssertFailure(steps, "CDDL_GENERATION_STEP_LIMIT");
            AssertFailure(attempts, "CDDL_GENERATION_LIMIT_INVALID");
            Assert.That(unsupported.Span.SourceName,
                Is.EqualTo("scalar-generation.cddl"));
            Assert.That(unsupported.Span.Length, Is.GreaterThan(0));
        });
    }

    private static void AssertInteger(string source, int expected)
    {
        CddlSchema schema = Compile(source);
        CddlGenerationResult result = Generate(
            schema,
            Options(CddlGenerationStrategy.Minimal, 0));
        Assert.That(result.Value!.IntegerValue,
            Is.EqualTo(new BigInteger(expected)));
        AssertSuccessfulAndValid(schema, result);
    }

    private static void AssertFloat(string source, double expected)
    {
        CddlSchema schema = Compile(source);
        CddlGenerationResult result = Generate(
            schema,
            Options(CddlGenerationStrategy.Minimal, 0));
        Assert.That(result.Value!.FloatValue!.Value, Is.EqualTo(expected));
        AssertSuccessfulAndValid(schema, result);
    }

    private static void AssertText(string source, string expected)
    {
        CddlSchema schema = Compile(source);
        CddlGenerationResult result = Generate(
            schema,
            Options(CddlGenerationStrategy.Minimal, 0));
        Assert.That(result.Value!.TextStringValue, Is.EqualTo(expected));
        AssertSuccessfulAndValid(schema, result);
    }

    private static void AssertBytes(string source, byte[] expected)
    {
        CddlSchema schema = Compile(source);
        CddlGenerationResult result = Generate(
            schema,
            Options(CddlGenerationStrategy.Minimal, 0));
        Assert.That(result.Value!.ByteStringValue, Is.EqualTo(expected));
        AssertSuccessfulAndValid(schema, result);
    }

    private static void AssertFloatWidth(
        CddlGenerationResult result,
        CborFloatWidth expected)
    {
        Assert.That(result.Value!.Kind, Is.EqualTo(CborValueKind.Float));
        Assert.That(result.Value.FloatWidth, Is.EqualTo(expected));
        Assert.That(result.Value.FloatValue!.Width, Is.EqualTo(expected));
    }

    private static void AssertSuccessfulAndValid(
        CddlSchema schema,
        CddlGenerationResult result)
    {
        Assert.That(
            result.IsSuccess,
            Is.True,
            result.ErrorMessage ?? "generation failed");
        Assert.That(result.Value, Is.Not.Null);
        CddlValidationResult validation = new CddlValidator().ValidateCbor(
            schema,
            result.Value!,
            new CddlValidationOptions
            {
                RootRuleName = result.RuleName
            });
        Assert.That(
            validation.IsValid,
            Is.True,
            validation.Issues.Length == 0
                ? "generated value did not validate"
                : validation.Issues[0].Message);
    }

    private static void AssertFailure(
        CddlGenerationResult result,
        string expectedCode)
    {
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(expectedCode));
        Assert.That(result.ErrorMessage, Is.Not.Empty);
    }

    private static CddlGenerationResult Generate(
        CddlSchema schema,
        CddlGenerationOptions options)
    {
        return new CddlValueGenerator().GenerateValue(schema, options);
    }

    private static CddlGenerationOptions Options(
        CddlGenerationStrategy strategy,
        int seed)
    {
        return new CddlGenerationOptions
        {
            Strategy = strategy,
            Seed = seed
        };
    }

    private static CddlSchema Compile(string source)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions
            {
                SourceName = "scalar-generation.cddl"
            });
        return new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions());
    }
}
