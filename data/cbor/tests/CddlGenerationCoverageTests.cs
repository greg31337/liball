using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlGenerationCoverageTests
{
    [Test]
    public void GeneratesEveryNormalizedConstructWithEveryStrategy()
    {
        string[] sources =
        {
            "root = (1 / 2)",
            "root = [1, 2 // 3 //]",
            "root = {name: 1}",
            "root = alias\nalias = 1",
            "root = uint .le 5",
            "root = 1..3",
            "root = [~wrapped]\nwrapped = [1, 2]",
            "root = &(a: 1, b: 2)",
            "root = #6.42(\"x\")",
            "root = #2.2",
            "root = [(1, 2)]"
        };
        CddlGenerationStrategy[] strategies =
        {
            CddlGenerationStrategy.Minimal,
            CddlGenerationStrategy.Boundary,
            CddlGenerationStrategy.Representative
        };
        HashSet<CddlNodeKind> kinds = new HashSet<CddlNodeKind>();

        int sourceIndex = 0;
        while (sourceIndex < sources.Length)
        {
            CddlSchema schema = Compile(sources[sourceIndex]);
            int nodeIndex = 0;
            while (nodeIndex < schema.NormalizedNodes.Length)
            {
                kinds.Add(schema.NormalizedNodes[nodeIndex].Kind);
                nodeIndex = nodeIndex + 1;
            }
            int strategyIndex = 0;
            while (strategyIndex < strategies.Length)
            {
                CddlGenerationResult result = Generate(
                    schema,
                    Options(strategies[strategyIndex], 17)
                );
                AssertSuccessfulAndValid(schema, result);
                Assert.That(result.ValidationIssues, Is.Empty);
                strategyIndex = strategyIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
        CddlSchema[] manualSchemas =
        {
            CreateParenthesizedTypeSchema(),
            CreateParenthesizedGroupSchema()
        };
        int manualIndex = 0;
        while (manualIndex < manualSchemas.Length)
        {
            CddlSchema schema = manualSchemas[manualIndex];
            int nodeIndex = 0;
            while (nodeIndex < schema.NormalizedNodes.Length)
            {
                kinds.Add(schema.NormalizedNodes[nodeIndex].Kind);
                nodeIndex = nodeIndex + 1;
            }
            int strategyIndex = 0;
            while (strategyIndex < strategies.Length)
            {
                CddlGenerationResult result = Generate(
                    schema,
                    Options(strategies[strategyIndex], 17)
                );
                AssertSuccessfulAndValid(schema, result);
                strategyIndex = strategyIndex + 1;
            }
            manualIndex = manualIndex + 1;
        }

        Assert.That(kinds, Is.SupersetOf(new[]
        {
            CddlNodeKind.TypeChoice,
            CddlNodeKind.GroupChoice,
            CddlNodeKind.Group,
            CddlNodeKind.GroupEntry,
            CddlNodeKind.Array,
            CddlNodeKind.Map,
            CddlNodeKind.Literal,
            CddlNodeKind.RuleReference,
            CddlNodeKind.Control,
            CddlNodeKind.Range,
            CddlNodeKind.Unwrap,
            CddlNodeKind.GroupEnumeration,
            CddlNodeKind.Tag,
            CddlNodeKind.Representation,
            CddlNodeKind.ParenthesizedType,
            CddlNodeKind.ParenthesizedGroup
        }));
    }

    [Test]
    public void RepresentativeGenerationIsReproducibleAcrossComplexRecursion()
    {
        CddlSchema schema = Compile(
            "root = [? choice, 1*3 choice, tail]\n"
            + "choice = \"a\" / \"b\" / \"c\"\n"
            + "tail = 0 / [tail]"
        );
        CddlGenerationOptions options = Options(
            CddlGenerationStrategy.Representative,
            41
        );
        options.MaxNestingDepth = 4;
        CddlGenerationResult first = Generate(schema, options);
        CddlGenerationResult repeated = Generate(schema, options);
        string firstText = Format(first.Value!);
        string repeatedText = Format(repeated.Value!);
        HashSet<string> samples = new HashSet<string>();
        int seed = 0;
        while (seed < 16)
        {
            CddlGenerationOptions sampleOptions = Options(
                CddlGenerationStrategy.Representative,
                seed
            );
            sampleOptions.MaxNestingDepth = 4;
            CddlGenerationResult sample = Generate(schema, sampleOptions);
            AssertSuccessfulAndValid(schema, sample);
            samples.Add(Format(sample.Value!));
            seed = seed + 1;
        }

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(schema, first);
            AssertSuccessfulAndValid(schema, repeated);
            Assert.That(repeatedText, Is.EqualTo(firstText));
            Assert.That(samples.Count, Is.GreaterThan(1));
        });
    }

    [Test]
    public void ReportsRuntimeAttemptAndConfigurationBudgets()
    {
        CddlGenerationOptions attempts = Options(
            CddlGenerationStrategy.Minimal,
            0
        );
        attempts.MaxAttempts = 1;
        CddlGenerationResult attemptResult = Generate(
            Compile("root = (tstr .regexp \"a\") / 1"),
            attempts
        );
        CddlGenerationOptions completeAttempts = Options(
            CddlGenerationStrategy.Representative,
            0
        );
        completeAttempts.MaxAttempts = 2;
        CddlGenerationResult completeAttemptResult = Generate(
            Compile(
                "root = (tstr .regexp \"a\") / "
                + "(tstr .regexp \"b\")"
            ),
            completeAttempts
        );
        CddlGenerationOptions invalidStrategy = Options(
            (CddlGenerationStrategy)999,
            0
        );
        CddlGenerationResult strategyResult = Generate(
            Compile("root = 1"),
            invalidStrategy
        );
        CddlGenerationOptions invalidLimits = Options(
            CddlGenerationStrategy.Minimal,
            0
        );
        invalidLimits.MaxCollectionItems = -1;
        CddlGenerationResult limitResult = Generate(
            Compile("root = 1"),
            invalidLimits
        );

        Assert.Multiple(() =>
        {
            AssertFailure(
                attemptResult,
                "CDDL_GENERATION_ATTEMPT_LIMIT"
            );
            AssertFailure(
                completeAttemptResult,
                "CDDL_GENERATION_CHOICE_FAILED"
            );
            AssertFailure(
                strategyResult,
                "CDDL_GENERATION_STRATEGY_INVALID"
            );
            AssertFailure(
                limitResult,
                "CDDL_GENERATION_LIMIT_INVALID"
            );
        });
    }

    [Test]
    public void HandlesEveryGenerationHookOutcome()
    {
        OutcomeHook notApplicable = new OutcomeHook();
        notApplicable.Status = CddlControlStatus.NotApplicable;
        CddlGenerationResult fallback = GenerateWithHook(notApplicable);
        OutcomeHook none = new OutcomeHook();
        none.Status = CddlControlStatus.None;
        CddlGenerationResult noOutcome = GenerateWithHook(none);
        OutcomeHook applied = new OutcomeHook();
        applied.Status = CddlControlStatus.Applied;
        applied.Value = Integer(1);
        CddlGenerationResult supplied = GenerateWithHook(applied);
        OutcomeHook missing = new OutcomeHook();
        missing.Status = CddlControlStatus.Applied;
        CddlGenerationResult missingValue = GenerateWithHook(missing);
        OutcomeHook mismatch = new OutcomeHook();
        mismatch.Status = CddlControlStatus.Mismatch;
        CddlGenerationResult mismatched = GenerateWithHook(mismatch);
        OutcomeHook error = new OutcomeHook();
        error.Status = CddlControlStatus.Error;
        error.ErrorCode = "APPLICATION_GENERATION_FAILED";
        error.ErrorMessage = "The application declined generation.";
        CddlGenerationResult failed = GenerateWithHook(error);

        Assert.Multiple(() =>
        {
            Assert.That(fallback.IsSuccess, Is.True, fallback.ErrorMessage);
            Assert.That(noOutcome.IsSuccess,
                Is.True, noOutcome.ErrorMessage);
            Assert.That(supplied.IsSuccess,
                Is.True, supplied.ErrorMessage);
            AssertFailure(
                missingValue,
                "CDDL_GENERATION_HOOK_VALUE_MISSING"
            );
            AssertFailure(
                mismatched,
                "CDDL_GENERATION_HOOK_FAILED"
            );
            AssertFailure(failed, "APPLICATION_GENERATION_FAILED");
            Assert.That(failed.ErrorMessage,
                Is.EqualTo("The application declined generation."));
            Assert.That(error.EndResult, Is.SameAs(failed));
            Assert.That(error.EndResult?.IsSuccess, Is.False);
        });
    }

    [Test]
    public void CoversRelationalFeatureAndPatternControlGeneration()
    {
        CddlGenerationResult lessOrEqual = Generate(
            Compile("root = uint .le 5"),
            Options(CddlGenerationStrategy.Minimal, 0)
        );
        CddlGenerationResult greaterOrEqual = Generate(
            Compile("root = uint .ge 5"),
            Options(CddlGenerationStrategy.Boundary, 3)
        );
        CddlGenerationResult disabled = Generate(
            Compile("root = 1 .feature \"alpha\""),
            Options(CddlGenerationStrategy.Minimal, 0)
        );
        OutcomeHook textHook = new OutcomeHook();
        textHook.Status = CddlControlStatus.Applied;
        textHook.Value = Text("abc");
        CddlGenerationOptions textOptions = Options(
            CddlGenerationStrategy.Minimal,
            0
        );
        textOptions.Hook = textHook;
        CddlGenerationResult abnf = Generate(
            Compile("root = tstr .abnf \"%s\\\"abc\\\"\""),
            textOptions
        );
        OutcomeHook bytesHook = new OutcomeHook();
        bytesHook.Status = CddlControlStatus.Applied;
        bytesHook.Value = Bytes([0, 65]);
        CddlGenerationOptions bytesOptions = Options(
            CddlGenerationStrategy.Minimal,
            0
        );
        bytesOptions.Hook = bytesHook;
        CddlGenerationResult abnfBytes = Generate(
            Compile("root = bstr .abnfb \"(%x00 %x41-5A)\""),
            bytesOptions
        );

        Assert.Multiple(() =>
        {
            AssertSuccessfulAndValid(
                Compile("root = uint .le 5"),
                lessOrEqual
            );
            Assert.That(lessOrEqual.Value?.IntegerValue,
                Is.LessThanOrEqualTo(new BigInteger(5)));
            AssertSuccessfulAndValid(
                Compile("root = uint .ge 5"),
                greaterOrEqual
            );
            Assert.That(greaterOrEqual.Value?.IntegerValue,
                Is.GreaterThanOrEqualTo(new BigInteger(5)));
            AssertFailure(disabled, "CDDL_GENERATION_FEATURE_DISABLED");
            Assert.That(abnf.IsSuccess, Is.True, abnf.ErrorMessage);
            Assert.That(abnfBytes.IsSuccess,
                Is.True, abnfBytes.ErrorMessage);
        });
    }

    private static CddlGenerationResult GenerateWithHook(OutcomeHook hook)
    {
        CddlGenerationOptions options = Options(
            CddlGenerationStrategy.Minimal,
            0
        );
        options.Hook = hook;
        return Generate(Compile("root = 1"), options);
    }

    private static CddlSchema CreateParenthesizedTypeSchema()
    {
        CddlNode literal = IntegerLiteralNode();
        CddlNode parenthesized = new CddlNode();
        parenthesized.Kind = CddlNodeKind.ParenthesizedType;
        parenthesized.Children.Add(literal);
        return ManualSchema(
            parenthesized,
            new CddlNode[] { parenthesized, literal }
        );
    }

    private static CddlSchema CreateParenthesizedGroupSchema()
    {
        CddlNode literal = IntegerLiteralNode();
        CddlNode entry = new CddlNode();
        entry.Kind = CddlNodeKind.GroupEntry;
        entry.Children.Add(literal);
        CddlNode parenthesized = new CddlNode();
        parenthesized.Kind = CddlNodeKind.ParenthesizedGroup;
        parenthesized.Children.Add(entry);
        CddlNode array = new CddlNode();
        array.Kind = CddlNodeKind.Array;
        array.Children.Add(parenthesized);
        return ManualSchema(
            array,
            new CddlNode[] { array, parenthesized, entry, literal }
        );
    }

    private static CddlNode IntegerLiteralNode()
    {
        CddlNode literal = new CddlNode();
        literal.Kind = CddlNodeKind.Literal;
        literal.LiteralKind = CddlLiteralKind.UnsignedInteger;
        literal.HasIntegerValue = true;
        literal.IntegerValue = BigInteger.One;
        return literal;
    }

    private static CddlSchema ManualSchema(
        CddlNode rootNode,
        CddlNode[] nodes)
    {
        CddlRule rule = new CddlRule();
        rule.Name = "root";
        rule.Kind = CddlRuleKind.Type;
        rule.Value = rootNode;
        CddlSchema schema = new CddlSchema();
        schema.Rules = new CddlRule[] { rule };
        schema.RootRule = rule;
        schema.NormalizedNodes = nodes;
        schema.SourceMap = new CddlSourceSpan[nodes.Length];
        int index = 0;
        while (index < schema.SourceMap.Length)
        {
            schema.SourceMap[index] = new CddlSourceSpan();
            index = index + 1;
        }
        return schema;
    }

    private static string Format(CborValue value)
    {
        return new CborDiagnosticConverter().FormatDiagnostic(
            value,
            new CborDiagnosticOptions()
        );
    }

    private static CborValue Integer(int value)
    {
        return new CborValue
        {
            Kind = value < 0
                ? CborValueKind.NegativeInteger
                : CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
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

    private static CborValue Bytes(byte[] value)
    {
        return new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = value,
            LengthKind = CborLengthKind.Definite
        };
    }

    private static void AssertSuccessfulAndValid(
        CddlSchema schema,
        CddlGenerationResult result)
    {
        Assert.That(result.IsSuccess,
            Is.True, result.ErrorMessage ?? "generation failed");
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
        Assert.That(validation.IsValid, Is.True,
            validation.Issues.Length == 0
                ? "generated value did not validate"
                : validation.Issues[0].Message);
    }

    private static void AssertFailure(
        CddlGenerationResult result,
        string code)
    {
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(code));
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
                SourceName = "generation-coverage.cddl"
            }
        );
        return new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions()
        );
    }

    private class OutcomeHook : ICddlGenerationHook
    {
        public CddlControlStatus Status;
        public CborValue? Value;
        public string? ErrorCode;
        public string? ErrorMessage;
        public CddlGenerationResult? EndResult;

        public void BeginGeneration(CddlGenerationContext context)
        {
        }

        public CddlControlResult GenerateNode(CddlGenerationContext context)
        {
            return new CddlControlResult
            {
                Status = Status,
                Value = Value,
                ErrorCode = ErrorCode,
                ErrorMessage = ErrorMessage
            };
        }

        public void EndGeneration(
            CddlGenerationContext context,
            CddlGenerationResult result)
        {
            EndResult = result;
        }
    }
}
