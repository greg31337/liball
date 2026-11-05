using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlGenerationValidationTests
{
    [Test]
    public void ValidatesTheCompletedValueAgainstTheSelectedRule()
    {
        CountingControlHandler handler = new CountingControlHandler();
        CddlSchema schema = Compile(handler);

        CddlGenerationResult result = new CddlValueGenerator().GenerateValue(
            schema,
            Options()
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
            Assert.That(result.Value?.IntegerValue,
                Is.EqualTo(new BigInteger(42)));
            Assert.That(result.ValidationIssues, Is.Empty);
            Assert.That(handler.GenerateCalls, Is.EqualTo(1));
            Assert.That(handler.EvaluateCalls, Is.EqualTo(2));
        });
    }

    [Test]
    public void ReturnsStructuredDetailsWhenFinalValidationRejectsCandidate()
    {
        CountingControlHandler handler = new CountingControlHandler();
        handler.RejectAfterFirstEvaluation = true;
        ObservingGenerationHook hook = new ObservingGenerationHook();
        CddlGenerationOptions options = Options();
        options.Hook = hook;

        CddlGenerationResult result = new CddlValueGenerator().GenerateValue(
            Compile(handler),
            options
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.ErrorCode,
                Is.EqualTo("CDDL_GENERATION_VALUE_INVALID"));
            Assert.That(result.ErrorMessage,
                Is.EqualTo("The final candidate was rejected."));
            Assert.That(result.RuleName, Is.EqualTo("root"));
            Assert.That(result.Span.SourceName,
                Is.EqualTo("generation-validation.cddl"));
            Assert.That(result.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(result.ValidationIssues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(result.ValidationIssues[0].InstancePath,
                Is.EqualTo("$"));
            Assert.That(result.ValidationIssues[0].RuleName,
                Is.EqualTo("root"));
            Assert.That(result.ValidationIssues[0].Expected,
                Is.EqualTo("a successfully applied custom control"));
            Assert.That(result.ValidationIssues[0].Actual,
                Is.EqualTo("FINAL_CANDIDATE_REJECTED"));
            Assert.That(result.ControlResult?.Status,
                Is.EqualTo(CddlControlStatus.Mismatch));
            Assert.That(handler.EvaluateCalls, Is.EqualTo(2));
            Assert.That(hook.EndResult, Is.SameAs(result));
            Assert.That(hook.EndResult?.IsSuccess, Is.False);
        });
    }

    private static CddlSchema Compile(CountingControlHandler handler)
    {
        CddlControlRegistration registration = new CddlControlRegistration();
        registration.Name = ".answer";
        registration.Handler = handler;
        CddlCompileOptions compileOptions = new CddlCompileOptions();
        compileOptions.ControlRegistrations.Add(registration);
        CddlDocument document = new CddlParser().ParseDocument(
            "document = 0\nroot = uint .answer 1",
            new CddlParseOptions
            {
                SourceName = "generation-validation.cddl"
            }
        );
        return new CddlCompiler().CompileSchema(document, compileOptions);
    }

    private static CddlGenerationOptions Options()
    {
        return new CddlGenerationOptions
        {
            RootRuleName = "root",
            Strategy = CddlGenerationStrategy.Minimal,
            Seed = 0
        };
    }

    private class CountingControlHandler : ICddlControlHandler
    {
        public bool RejectAfterFirstEvaluation;
        public int GenerateCalls;
        public int EvaluateCalls;

        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            EvaluateCalls = EvaluateCalls + 1;
            if (RejectAfterFirstEvaluation && EvaluateCalls > 1)
            {
                return new CddlControlResult
                {
                    Status = CddlControlStatus.Mismatch,
                    ErrorCode = "FINAL_CANDIDATE_REJECTED",
                    ErrorMessage = "The final candidate was rejected."
                };
            }
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

    private class ObservingGenerationHook : ICddlGenerationHook
    {
        public CddlGenerationResult? EndResult;

        public void BeginGeneration(CddlGenerationContext context)
        {
        }

        public CddlControlResult GenerateNode(CddlGenerationContext context)
        {
            return new CddlControlResult
            {
                Status = CddlControlStatus.NotApplicable
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
