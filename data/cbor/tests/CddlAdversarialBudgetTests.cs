using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlAdversarialBudgetTests
{
    [Test]
    public void PublicValidationCapsIssueCollectionWithoutLosingFailure()
    {
        CddlSchema schema = Compile("root = [1, 2, 3]");
        CborValue value = Array(Integer(4), Integer(5), Integer(6));
        ICddlValidator validator = new CddlValidator();

        CddlValidationResult capped = validator.ValidateCbor(
            schema,
            value,
            new CddlValidationOptions
            {
                CollectIssues = true,
                MaxIssues = 2
            });
        CddlValidationResult none = validator.ValidateCbor(
            schema,
            value,
            new CddlValidationOptions
            {
                CollectIssues = true,
                MaxIssues = 0
            });
        List<CddlValidationCode> cappedCodes = FlattenCodes(capped.Issues);

        Assert.Multiple(() =>
        {
            Assert.That(capped.IsValid, Is.False);
            Assert.That(cappedCodes, Has.Count.EqualTo(2));
            Assert.That(cappedCodes,
                Does.Contain(CddlValidationCode.LiteralMismatch));
            Assert.That(none.IsValid, Is.False);
            Assert.That(none.Issues, Is.Empty);
        });
    }

    [Test]
    public void ExhaustedRecursiveBudgetsDoNotPoisonValidatorReuse()
    {
        CddlSchema schema = Compile(
            "root = node\n"
            + "node = 0 / [node]");
        CborValue value = Array(Array(Array(Integer(0))));
        ICddlValidator validator = new CddlValidator();

        CddlValidationResult limited = validator.ValidateCbor(
            schema,
            value,
            new CddlValidationOptions
            {
                MaxMemoEntries = 2,
                MaxSteps = 5,
                MaxNestingDepth = 4
            });
        CddlValidationResult retry = validator.ValidateCbor(
            schema,
            value,
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(limited.IsValid, Is.False);
            Assert.That(FlattenCodes(limited.Issues),
                Does.Contain(CddlValidationCode.ResourceLimit));
            Assert.That(retry.IsValid, Is.True);
            Assert.That(retry.Issues, Is.Empty);
        });
    }

    [Test]
    public void CancellationFromGenerationHookIsContextualAndReusable()
    {
        CddlSchema schema = Compile("root = 1");
        CddlValueGenerator generator = new CddlValueGenerator();
        CddlGenerationOptions options = new CddlGenerationOptions();
        options.Hook = new CancellingHook();

        CddlGenerationException? failure = Assert.Throws<
            CddlGenerationException>(
                () => generator.GenerateValue(schema, options));
        CddlGenerationResult retry = generator.GenerateValue(
            schema,
            new CddlGenerationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(failure?.ErrorCode,
                Is.EqualTo("CDDL_GENERATION_HOOK_EXCEPTION"));
            Assert.That(failure?.RuleName, Is.EqualTo("root"));
            Assert.That(failure?.Span?.SourceName,
                Is.EqualTo("adversarial-budget.cddl"));
            Assert.That(retry.IsSuccess, Is.True, retry.ErrorMessage);
            Assert.That(retry.Value?.IntegerValue,
                Is.EqualTo(BigInteger.One));
        });
    }

    private static CddlSchema Compile(string source)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions
            {
                SourceName = "adversarial-budget.cddl"
            });
        return new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions());
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

    private static CborValue Array(params CborValue[] items)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = new List<CborValue>(items)
        };
    }

    private static List<CddlValidationCode> FlattenCodes(
        CddlValidationIssue[] issues)
    {
        List<CddlValidationCode> codes = new List<CddlValidationCode>();
        foreach (CddlValidationIssue issue in issues)
        {
            codes.Add(issue.Code);
            codes.AddRange(FlattenCodes(issue.Details));
        }
        return codes;
    }

    private class CancellingHook : ICddlGenerationHook
    {
        public void BeginGeneration(CddlGenerationContext context)
        {
        }

        public CddlControlResult GenerateNode(CddlGenerationContext context)
        {
            throw new OperationCanceledException("cancelled by test hook");
        }

        public void EndGeneration(
            CddlGenerationContext context,
            CddlGenerationResult result)
        {
        }
    }
}
