using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlPublicModelTests
{
    private sealed class ExampleControlHandler : ICddlControlHandler
    {
        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            return new CddlControlResult
            {
                Status = value.Kind == CborValueKind.TextString
                    ? CddlControlStatus.Applied
                    : CddlControlStatus.Mismatch
            };
        }

        public CddlControlResult GenerateControl(
            CddlNode target,
            CddlNode controller,
            CddlGenerationContext context)
        {
            return new CddlControlResult
            {
                Status = CddlControlStatus.Applied,
                Value = new CborValue
                {
                    Kind = CborValueKind.TextString,
                    TextStringValue = "generated"
                }
            };
        }
    }

    [Test]
    public void AstModelRetainsOrderedRulesTriviaAndNodePayloads()
    {
        CddlSourceSpan span = new CddlSourceSpan
        {
            SourceName = "example.cddl",
            Start = 10,
            Length = 4,
            StartLine = 2,
            StartColumn = 3,
            EndLine = 2,
            EndColumn = 7
        };
        CddlNode reference = new CddlNode
        {
            Kind = CddlNodeKind.RuleReference,
            Name = "uint",
            Span = span
        };
        CddlNode entry = new CddlNode
        {
            Kind = CddlNodeKind.GroupEntry,
            Occurrence = CddlOccurrenceKind.Bounded,
            MinimumOccurrence = 1,
            MaximumOccurrence = 3
        };
        entry.Children.Add(reference);
        CddlNode array = new CddlNode { Kind = CddlNodeKind.Array };
        array.Children.Add(entry);
        CddlRule rule = new CddlRule
        {
            Name = "numbers",
            Kind = CddlRuleKind.Type,
            Value = array,
            SourceOrder = 0,
            Span = span
        };
        CddlDocument document = new CddlDocument
        {
            SourceName = "example.cddl",
            SourceText = "numbers = [1*3 uint]",
            RootRuleName = "numbers"
        };
        document.Trivia.Add(" ");
        document.TriviaSpans.Add(span);
        document.Rules.Add(rule);
        document.Diagnostics.Add(new CddlDiagnostic
        {
            Severity = CddlDiagnosticSeverity.Warning,
            Code = "CDDL_EXAMPLE",
            Message = "example",
            RuleName = "numbers",
            Span = span
        });
        CddlSchema schema = new CddlSchema
        {
            Document = document,
            Rules = [rule],
            RootRule = rule,
            NormalizedNodes = [array, entry, reference],
            SourceMap = [span]
        };

        Assert.Multiple(() =>
        {
            Assert.That(schema.RootRule, Is.SameAs(rule));
            Assert.That(schema.Document?.Rules[0].Value?.Children[0], Is.SameAs(entry));
            Assert.That(entry.Children[0].Name, Is.EqualTo("uint"));
            Assert.That(entry.Occurrence, Is.EqualTo(CddlOccurrenceKind.Bounded));
            Assert.That(entry.MinimumOccurrence, Is.EqualTo(1));
            Assert.That(entry.MaximumOccurrence, Is.EqualTo(3));
            Assert.That(document.Trivia, Is.EqualTo(new string[] { " " }));
            Assert.That(document.Diagnostics[0].Severity,
                Is.EqualTo(CddlDiagnosticSeverity.Warning));
            Assert.That(reference.Span.Start, Is.EqualTo(10));
            Assert.That(reference.Span.EndColumn, Is.EqualTo(7));
        });
    }

    [Test]
    public void AstLiteralAndRepresentationFieldsPreserveExactPresenceInformation()
    {
        BigInteger integer = (BigInteger.One << 100) + 9;
        CddlNode integerNode = new CddlNode
        {
            Kind = CddlNodeKind.Literal,
            LiteralKind = CddlLiteralKind.UnsignedInteger,
            IntegerValue = integer,
            HasIntegerValue = true,
            TextValue = "0x10000000000000000000000009"
        };
        CddlNode hexFloat = new CddlNode
        {
            Kind = CddlNodeKind.Literal,
            LiteralKind = CddlLiteralKind.HexadecimalFloat,
            FloatValue = -0.0,
            HasFloatValue = true,
            TextValue = "-0x0p0"
        };
        CddlNode representation = new CddlNode
        {
            Kind = CddlNodeKind.Representation,
            MajorType = 7,
            ArgumentValue = 20,
            HasArgumentValue = true
        };
        CddlNode range = new CddlNode
        {
            Kind = CddlNodeKind.Range,
            IsRangeUpperExclusive = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(integerNode.IntegerValue, Is.EqualTo(integer));
            Assert.That(integerNode.HasIntegerValue, Is.True);
            Assert.That(hexFloat.HasFloatValue, Is.True);
            Assert.That(BitConverter.DoubleToInt64Bits(hexFloat.FloatValue),
                Is.EqualTo(long.MinValue));
            Assert.That(representation.MajorType, Is.EqualTo(7));
            Assert.That(representation.ArgumentValue, Is.EqualTo(new BigInteger(20)));
            Assert.That(range.IsRangeUpperExclusive, Is.True);
        });
    }

    [Test]
    public void OperationModelsHaveSafeIndependentDefaults()
    {
        CddlParseOptions parse = new CddlParseOptions();
        CddlCompileOptions compile = new CddlCompileOptions();
        CddlCompileOptions secondCompile = new CddlCompileOptions();
        CddlValidationOptions validation = new CddlValidationOptions();
        CddlGenerationOptions generation = new CddlGenerationOptions();
        CddlEmissionOptions emission = new CddlEmissionOptions();
        CddlParseResult firstParseResult = new CddlParseResult();
        CddlParseResult secondParseResult = new CddlParseResult();
        CddlValidationIssue issue = new CddlValidationIssue();
        CddlEmissionContext emissionContext = new CddlEmissionContext();

        Assert.Multiple(() =>
        {
            Assert.That(parse.Recover, Is.True);
            Assert.That(parse.MaxDiagnostics, Is.GreaterThan(0));
            Assert.That(parse.MaxNestingDepth, Is.GreaterThan(0));
            Assert.That(compile.IncludeStandardPrelude, Is.True);
            Assert.That(compile.MaxGenericInstantiations, Is.GreaterThan(0));
            Assert.That(validation.CollectIssues, Is.True);
            Assert.That(validation.MaxSteps, Is.GreaterThan(validation.MaxIssues));
            Assert.That(generation.Strategy,
                Is.EqualTo(CddlGenerationStrategy.Representative));
            Assert.That(generation.MaxAttempts, Is.GreaterThan(0));
            Assert.That(emission.IncludeNormalizedNodes, Is.True);
            Assert.That(emission.IncludeSourceContext, Is.True);
            Assert.That(firstParseResult.Diagnostics, Is.Empty);
            Assert.That(firstParseResult.Options,
                Is.Not.SameAs(secondParseResult.Options));
            Assert.That(compile.ControlRegistrations,
                Is.Not.SameAs(secondCompile.ControlRegistrations));
            Assert.That(issue.Code, Is.EqualTo(CddlValidationCode.None));
            Assert.That(issue.InstancePath, Is.EqualTo("$"));
            Assert.That(emissionContext.RuleIndex, Is.EqualTo(-1));
            Assert.That(emissionContext.NodeIndex, Is.EqualTo(-1));
        });
    }

    [Test]
    public void StandardPreludeExposesEveryNormativeRuleInOrder()
    {
        string[] sourceNames = CddlPrelude.Standard
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Substring(0, line.IndexOf('=')).Trim())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(CddlPrelude.RuleNames, Has.Length.EqualTo(40));
            Assert.That(sourceNames, Is.EqualTo(CddlPrelude.RuleNames));
            Assert.That(CddlPrelude.RuleNames[0], Is.EqualTo("any"));
            Assert.That(CddlPrelude.RuleNames[^1], Is.EqualTo("undefined"));
            Assert.That(CddlPrelude.Standard, Does.Contain("decfrac = #6.4"));
            Assert.That(CddlPrelude.Standard, Does.Contain("float16 = #7.25"));
            Assert.That(CddlPrelude.Standard, Does.Contain("cbor-any = #6.55799(any)"));
        });
    }

    [Test]
    public void StandardControlNamesCoverRfc8610Rfc9165AndRfc9741()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CddlControlNames.RegisteredNames, Has.Length.EqualTo(34));
            Assert.That(CddlControlNames.RegisteredNames.Distinct().ToArray(),
                Has.Length.EqualTo(34));
            Assert.That(CddlControlNames.RegisteredNames.Take(14), Is.EqualTo(new string[]
            {
                ".size", ".bits", ".regexp", ".cbor", ".cborseq", ".within",
                ".and", ".lt", ".le", ".gt", ".ge", ".eq", ".ne", ".default"
            }));
            Assert.That(CddlControlNames.RegisteredNames.Skip(14).Take(6),
                Is.EqualTo(new string[]
                {
                    ".plus", ".cat", ".det", ".abnf", ".abnfb", ".feature"
                }));
            Assert.That(CddlControlNames.RegisteredNames, Does.Contain(".b64u-sloppy"));
            Assert.That(CddlControlNames.RegisteredNames, Does.Contain(".printf"));
            Assert.That(CddlControlNames.RegisteredNames[^1], Is.EqualTo(".join"));
        });
    }

    [Test]
    public void ControlRegistrationCarriesHandlerEvaluationAndGenerationResults()
    {
        ExampleControlHandler handler = new ExampleControlHandler();
        CddlControlRegistration registration = new CddlControlRegistration
        {
            Name = ".example",
            Handler = handler
        };
        CddlControlResult matched = registration.Handler!.EvaluateControl(
            new CddlNode(),
            new CddlNode(),
            new CborValue { Kind = CborValueKind.TextString },
            new CddlValidationOptions());
        CddlControlResult mismatch = registration.Handler.EvaluateControl(
            new CddlNode(),
            new CddlNode(),
            new CborValue { Kind = CborValueKind.Array },
            new CddlValidationOptions());
        CddlControlResult generated = registration.Handler.GenerateControl(
            new CddlNode(),
            new CddlNode(),
            new CddlGenerationContext());

        Assert.Multiple(() =>
        {
            Assert.That(registration.IsEnabled, Is.True);
            Assert.That(matched.Status, Is.EqualTo(CddlControlStatus.Applied));
            Assert.That(mismatch.Status, Is.EqualTo(CddlControlStatus.Mismatch));
            Assert.That(generated.Status, Is.EqualTo(CddlControlStatus.Applied));
            Assert.That(generated.Value?.TextStringValue, Is.EqualTo("generated"));
        });
    }

    [Test]
    public void CddlExceptionsRetainStructuredFailureContext()
    {
        CddlSourceSpan span = new CddlSourceSpan
        {
            SourceName = "bad.cddl",
            Start = 4,
            Length = 2
        };
        CddlParseException parse = new CddlParseException(
            "CDDL_PARSE_TOKEN",
            "bad token",
            span);
        CddlCompileException compile = new CddlCompileException(
            "CDDL_COMPILE_RULE",
            "bad rule",
            span,
            "root");
        CddlValidationException validation = new CddlValidationException(
            "CDDL_VALIDATE_TYPE",
            "wrong type",
            CddlValidationCode.TypeMismatch,
            "$[0]",
            span,
            "root");
        CddlGenerationException generation = new CddlGenerationException(
            "CDDL_GENERATE_EMPTY",
            "no value",
            span,
            "root");

        Assert.Multiple(() =>
        {
            Assert.That(parse.ErrorCode, Is.EqualTo("CDDL_PARSE_TOKEN"));
            Assert.That(parse.Message, Is.EqualTo("bad token"));
            Assert.That(parse.Span, Is.SameAs(span));
            Assert.That(compile.ErrorCode, Is.EqualTo("CDDL_COMPILE_RULE"));
            Assert.That(compile.RuleName, Is.EqualTo("root"));
            Assert.That(validation.ValidationCode,
                Is.EqualTo(CddlValidationCode.TypeMismatch));
            Assert.That(validation.InstancePath, Is.EqualTo("$[0]"));
            Assert.That(validation.Span, Is.SameAs(span));
            Assert.That(generation.ErrorCode, Is.EqualTo("CDDL_GENERATE_EMPTY"));
            Assert.That(generation.RuleName, Is.EqualTo("root"));
        });
    }
}
