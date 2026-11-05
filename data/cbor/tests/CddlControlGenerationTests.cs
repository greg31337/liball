using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlControlGenerationTests
{
    [Test]
    public void GeneratesComputedLiteralControls()
    {
        AssertText("root = \"foo\" .cat \"bar\"", "foobar");
        AssertText(
            "root = \"  a\\n    b\" .det \"  c\"",
            "a\n  bc"
        );
        CddlSchema schema = Compile("root = 10 .plus -3");
        CddlGenerationResult result = Generate(schema, Options());

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(schema, result);
            Assert.That(result.Value!.IntegerValue,
                Is.EqualTo(new BigInteger(7)));
        });
    }

    [Test]
    public void GeneratesSizedStringsAndIntegers()
    {
        CddlSchema bytesSchema = Compile("root = bstr .size 4");
        CddlSchema textSchema = Compile("root = tstr .size 3");
        CddlSchema integerSchema = Compile("root = uint .size 1");
        CddlGenerationResult bytes = Generate(bytesSchema, Options());
        CddlGenerationResult text = Generate(textSchema, Options());
        CddlGenerationResult integer = Generate(integerSchema, Options());

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(bytesSchema, bytes);
            Assert.That(bytes.Value!.ByteStringValue, Has.Length.EqualTo(4));
            AssertSuccessfulAndValid(textSchema, text);
            Assert.That(text.Value!.TextStringValue, Has.Length.EqualTo(3));
            AssertSuccessfulAndValid(integerSchema, integer);
        });
    }

    [Test]
    public void GeneratesBitsIntersectionsComparisonsAndEquality()
    {
        AssertGeneratedAndValid("root = uint .bits (0 / 3 / 7)");
        CddlSchema boundaryBits = Compile("root = uint .bits (0 / 3 / 7)");
        CddlGenerationOptions boundaryBitsOptions = Options();
        boundaryBitsOptions.Strategy = CddlGenerationStrategy.Boundary;
        AssertSuccessfulAndValid(
            boundaryBits,
            Generate(boundaryBits, boundaryBitsOptions)
        );
        AssertGeneratedAndValid("root = uint .and (2..4)");
        AssertGeneratedAndValid("root = uint .within (2..4)");
        AssertGeneratedAndValid("root = uint .lt 3");
        AssertGeneratedAndValid("root = uint .gt 3");
        AssertGeneratedAndValid("root = (uint / float64) .eq 7");
        AssertGeneratedAndValid("root = uint .ne 0");
        CddlSchema defaultSchema = Compile(
            "root = {? value: (uint .gt 0) .default 1}"
        );
        CddlGenerationOptions defaultOptions = Options();
        defaultOptions.Strategy = CddlGenerationStrategy.Boundary;
        CddlGenerationResult defaultResult = Generate(
            defaultSchema,
            defaultOptions
        );
        AssertSuccessfulAndValid(defaultSchema, defaultResult);
    }

    [Test]
    public void GeneratesEmbeddedCborAndSequences()
    {
        CddlSchema itemSchema = Compile("root = bstr .cbor [1, \"x\"]");
        CddlSchema sequenceSchema = Compile(
            "root = bstr .cborseq [1, \"x\"]"
        );
        CddlGenerationResult item = Generate(itemSchema, Options());
        CddlGenerationResult sequence = Generate(
            sequenceSchema,
            Options()
        );

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(itemSchema, item);
            Assert.That(item.Value!.ByteStringValue, Is.Not.Empty);
            AssertSuccessfulAndValid(sequenceSchema, sequence);
            Assert.That(sequence.Value!.ByteStringValue, Is.Not.Empty);
        });
    }

    [Test]
    public void SelectsOnlyEnabledFeatureAlternatives()
    {
        CddlSchema schema = Compile(
            "root = (1 .feature \"alpha\") / "
            + "(2 .feature \"beta\")"
        );
        CddlGenerationOptions options = Options();
        options.EnabledFeatures = new string[] { "beta" };
        CddlGenerationResult result = Generate(schema, options);

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(schema, result);
            Assert.That(result.Value!.IntegerValue,
                Is.EqualTo(new BigInteger(2)));
        });
    }

    [TestCase(".b64u", "-_8")]
    [TestCase(".b64u-sloppy", "-_8")]
    [TestCase(".b64c", "+/8=")]
    [TestCase(".b64c-sloppy", "+/8=")]
    [TestCase(".b32", "7P7Q")]
    [TestCase(".h32", "VFV G")]
    [TestCase(".hex", "fbff")]
    [TestCase(".hexlc", "fbff")]
    [TestCase(".hexuc", "FBFF")]
    public void GeneratesStandardByteTextEncodings(
        string control,
        string expected)
    {
        CddlSchema schema = Compile(
            "root = tstr " + control + " h'fbff'"
        );
        CddlGenerationResult result = Generate(schema, Options());

        AssertSuccessfulAndValid(schema, result);
        Assert.That(result.Value!.TextStringValue,
            Is.EqualTo(expected.Replace(" ", "")));
    }

    [Test]
    public void GeneratesBase45Base10PrintfJsonAndJoinText()
    {
        AssertGeneratedAndValid("root = tstr .b45 h'48656c6c6f'");
        AssertText("root = tstr .base10 -42", "-42");
        AssertText(
            "root = tstr .printf ([\"id=%04X\", 42])",
            "id=002A"
        );
        AssertGeneratedAndValid(
            "root = tstr .json {name: \"Ada\", count: 2}"
        );
        AssertText(
            "root = tstr .join [\"a\", h'62', \"c\"]",
            "abc"
        );
    }

    [Test]
    public void UsesGenerationHookForPatternControls()
    {
        SupplyingGenerationHook hook = new SupplyingGenerationHook();
        hook.Value = Text("abc");
        CddlSchema schema = Compile(
            "root = tstr .regexp \"[a-z]+\""
        );
        CddlGenerationOptions options = Options();
        options.Hook = hook;
        CddlGenerationResult result = Generate(schema, options);

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(schema, result);
            Assert.That(hook.GenerateCalls, Is.EqualTo(1));
            Assert.That(hook.LastContext!.Node!.ControlName,
                Is.EqualTo(CddlControlNames.RegularExpression));
        });
    }

    [Test]
    public void UsesRegisteredHandlerForCustomControls()
    {
        SupplyingControlHandler handler = new SupplyingControlHandler();
        CddlControlRegistration registration = new CddlControlRegistration();
        registration.Name = ".answer";
        registration.Handler = handler;
        CddlCompileOptions compileOptions = new CddlCompileOptions();
        compileOptions.ControlRegistrations.Add(registration);
        CddlSchema schema = Compile(
            "root = uint .answer 1",
            compileOptions
        );
        CddlGenerationResult result = Generate(schema, Options());

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(schema, result);
            Assert.That(result.Value!.IntegerValue,
                Is.EqualTo(new BigInteger(42)));
            Assert.That(handler.GenerateCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReturnsStructuredFailureWhenAControlNeedsAHook()
    {
        CddlGenerationResult result = Generate(
            Compile("root = tstr .regexp \"[a-z]+\""),
            Options()
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.ErrorCode,
                Is.EqualTo("CDDL_GENERATION_CONTROL_REQUIRES_HOOK"));
            Assert.That(result.Span.SourceName,
                Is.EqualTo("control-generation.cddl"));
        });
    }

    private static void AssertText(string source, string expected)
    {
        CddlSchema schema = Compile(source);
        CddlGenerationResult result = Generate(schema, Options());
        AssertSuccessfulAndValid(schema, result);
        Assert.That(result.Value!.TextStringValue, Is.EqualTo(expected));
    }

    private static void AssertGeneratedAndValid(string source)
    {
        CddlSchema schema = Compile(source);
        CddlGenerationResult result = Generate(schema, Options());
        AssertSuccessfulAndValid(schema, result);
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value,
            LengthKind = CborLengthKind.Definite
        };
    }

    private static void AssertSuccessfulAndValid(
        CddlSchema schema,
        CddlGenerationResult result)
    {
        Assert.That(
            result.IsSuccess,
            Is.True,
            result.ErrorMessage ?? "generation failed"
        );
        Assert.That(result.Value, Is.Not.Null);
        CddlValidationResult validation = new CddlValidator().ValidateCbor(
            schema,
            result.Value!,
            new CddlValidationOptions
            {
                RootRuleName = result.RuleName,
                EnabledFeatures = result.Options.EnabledFeatures
            }
        );
        Assert.That(
            validation.IsValid,
            Is.True,
            validation.Issues.Length == 0
                ? "generated value did not validate"
                : validation.Issues[0].Message
        );
    }

    private static CddlGenerationResult Generate(
        CddlSchema schema,
        CddlGenerationOptions options)
    {
        return new CddlValueGenerator().GenerateValue(schema, options);
    }

    private static CddlGenerationOptions Options()
    {
        return new CddlGenerationOptions
        {
            Strategy = CddlGenerationStrategy.Minimal,
            Seed = 0
        };
    }

    private static CddlSchema Compile(string source)
    {
        return Compile(source, new CddlCompileOptions());
    }

    private static CddlSchema Compile(
        string source,
        CddlCompileOptions options)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions
            {
                SourceName = "control-generation.cddl"
            }
        );
        return new CddlCompiler().CompileSchema(document, options);
    }

    private class SupplyingGenerationHook : ICddlGenerationHook
    {
        public CborValue? Value;
        public int GenerateCalls;
        public CddlGenerationContext? LastContext;

        public void BeginGeneration(CddlGenerationContext context)
        {
        }

        public CddlControlResult GenerateNode(CddlGenerationContext context)
        {
            GenerateCalls = GenerateCalls + 1;
            LastContext = context;
            return new CddlControlResult
            {
                Status = CddlControlStatus.Applied,
                Value = Value
            };
        }

        public void EndGeneration(
            CddlGenerationContext context,
            CddlGenerationResult result)
        {
        }
    }

    private class SupplyingControlHandler : ICddlControlHandler
    {
        public int GenerateCalls;

        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            return new CddlControlResult
            {
                Status = value.IntegerValue == new BigInteger(42)
                    ? CddlControlStatus.Applied
                    : CddlControlStatus.Mismatch
            };
        }

        public CddlControlResult GenerateControl(
            CddlNode target,
            CddlNode controller,
            CddlGenerationContext context)
        {
            GenerateCalls = GenerateCalls + 1;
            return new CddlControlResult
            {
                Status = CddlControlStatus.Applied,
                Value = new CborValue
                {
                    Kind = CborValueKind.UnsignedInteger,
                    IntegerValue = new BigInteger(42)
                }
            };
        }
    }
}
