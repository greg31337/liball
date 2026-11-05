using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlGenerationHookTests
{
    [Test]
    public void RunsLifecycleAndSuppliesNamedRuleWithSnapshotContext()
    {
        CddlSchema schema = Compile("root = child\nchild = 1 / 2");
        RecordingHook hook = new RecordingHook();
        hook.RuleName = "child";
        hook.Value = Integer(2);
        CddlGenerationOptions options = Options();
        options.Hook = hook;

        CddlGenerationResult result = new CddlValueGenerator().GenerateValue(
            schema,
            options
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
            Assert.That(result.Value?.IntegerValue,
                Is.EqualTo(new BigInteger(2)));
            Assert.That(hook.Events,
                Is.EqualTo(new[] { "begin", "root", "child", "end" }));
            Assert.That(hook.BeginContext?.Schema, Is.SameAs(schema));
            Assert.That(hook.BeginContext?.Rule?.Name, Is.EqualTo("root"));
            Assert.That(hook.BeginContext?.Span.SourceName,
                Is.EqualTo("generation-hook.cddl"));
            Assert.That(hook.BeginContext?.RemainingSteps,
                Is.EqualTo(options.MaxSteps));
            Assert.That(hook.BeginContext?.Options, Is.Not.SameAs(options));
            Assert.That(hook.EndContext?.RemainingSteps,
                Is.LessThan(options.MaxSteps));
            Assert.That(hook.EndResult, Is.SameAs(result));
            Assert.That(hook.EndResult?.IsSuccess, Is.True);
        });
    }

    [Test]
    public void InterceptsNestedTagsAndControls()
    {
        CddlSchema tagSchema = Compile("root = [#6.42(tstr)]");
        RecordingHook tagHook = new RecordingHook();
        tagHook.NodeKind = CddlNodeKind.Tag;
        tagHook.Value = Tag(42, Text("hooked"));
        CddlGenerationOptions tagOptions = Options();
        tagOptions.Hook = tagHook;
        CddlGenerationResult tagResult =
            new CddlValueGenerator().GenerateValue(tagSchema, tagOptions);

        CddlSchema controlSchema = Compile("root = [uint .gt 5]");
        RecordingHook controlHook = new RecordingHook();
        controlHook.NodeKind = CddlNodeKind.Control;
        controlHook.Value = Integer(6);
        CddlGenerationOptions controlOptions = Options();
        controlOptions.Hook = controlHook;
        CddlGenerationResult controlResult =
            new CddlValueGenerator().GenerateValue(
                controlSchema,
                controlOptions
            );

        Assert.Multiple(() =>
        {
            Assert.That(tagResult.IsSuccess, Is.True, tagResult.ErrorMessage);
            Assert.That(tagResult.Value?.ArrayItems?[0].TagNumber,
                Is.EqualTo(42));
            Assert.That(tagHook.MatchedCalls, Is.EqualTo(1));
            Assert.That(controlResult.IsSuccess,
                Is.True, controlResult.ErrorMessage);
            Assert.That(controlResult.Value?.ArrayItems?[0].IntegerValue,
                Is.EqualTo(new BigInteger(6)));
            Assert.That(controlHook.MatchedCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsAValueThatDoesNotMatchTheInterceptedNode()
    {
        RecordingHook hook = new RecordingHook();
        hook.RuleName = "root";
        hook.Value = Text("not-an-integer");
        CddlGenerationOptions options = Options();
        options.Hook = hook;

        CddlGenerationResult result = new CddlValueGenerator().GenerateValue(
            Compile("root = uint"),
            options
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.ErrorCode,
                Is.EqualTo("CDDL_GENERATION_HOOK_VALUE_INVALID"));
            Assert.That(result.ControlResult?.Value, Is.SameAs(hook.Value));
            Assert.That(hook.EndResult, Is.SameAs(result));
            Assert.That(hook.EndResult?.IsSuccess, Is.False);
        });
    }

    [Test]
    public void RejectsAnInvalidValueFromARegisteredControlHandler()
    {
        InvalidControlHandler handler = new InvalidControlHandler();
        CddlControlRegistration registration = new CddlControlRegistration();
        registration.Name = ".answer";
        registration.Handler = handler;
        CddlCompileOptions compileOptions = new CddlCompileOptions();
        compileOptions.ControlRegistrations.Add(registration);
        CddlSchema schema = Compile(
            "root = uint .answer 1",
            compileOptions
        );

        CddlGenerationResult result = new CddlValueGenerator().GenerateValue(
            schema,
            Options()
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode,
                Is.EqualTo("CDDL_GENERATION_HOOK_VALUE_INVALID"));
            Assert.That(handler.GenerateCalls, Is.EqualTo(1));
            Assert.That(handler.EvaluateCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsReentryAndAllowsTheOuterOperationToFinish()
    {
        CddlValueGenerator generator = new CddlValueGenerator();
        CddlSchema schema = Compile("root = 1");
        ReenteringHook hook = new ReenteringHook();
        hook.Generator = generator;
        hook.Schema = schema;
        CddlGenerationOptions options = Options();
        options.Hook = hook;

        CddlGenerationResult result = generator.GenerateValue(
            schema,
            options
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
            Assert.That(hook.ReentrantResult?.IsSuccess, Is.False);
            Assert.That(hook.ReentrantResult?.ErrorCode,
                Is.EqualTo("CDDL_GENERATION_REENTRANT"));
        });
    }

    [TestCase("begin")]
    [TestCase("node")]
    [TestCase("end")]
    public void WrapsHookExceptionsWithRuleAndSourceContext(string phase)
    {
        CddlValueGenerator generator = new CddlValueGenerator();
        ThrowingHook hook = new ThrowingHook();
        hook.Phase = phase;
        CddlGenerationOptions options = Options();
        options.Hook = hook;
        CddlSchema schema = Compile("root = 1");

        CddlGenerationException? exception = Assert.Throws<
            CddlGenerationException>(
                () => generator.GenerateValue(schema, options)
            );
        CddlGenerationResult retry = generator.GenerateValue(
            schema,
            Options()
        );

        Assert.Multiple(() =>
        {
            Assert.That(exception?.ErrorCode,
                Is.EqualTo("CDDL_GENERATION_HOOK_EXCEPTION"));
            Assert.That(exception?.RuleName, Is.EqualTo("root"));
            Assert.That(exception?.Span?.SourceName,
                Is.EqualTo("generation-hook.cddl"));
            Assert.That(exception?.Message, Does.Contain(phase));
            Assert.That(retry.IsSuccess, Is.True, retry.ErrorMessage);
        });
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
                SourceName = "generation-hook.cddl"
            }
        );
        return new CddlCompiler().CompileSchema(document, options);
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

    private static CborValue Tag(ulong number, CborValue content)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = number,
            TaggedValue = content
        };
    }

    private class RecordingHook : ICddlGenerationHook
    {
        public string? RuleName;
        public CddlNodeKind NodeKind = CddlNodeKind.Literal;
        public CborValue? Value;
        public int MatchedCalls;
        public List<string> Events = new List<string>();
        public CddlGenerationContext? BeginContext;
        public CddlGenerationContext? EndContext;
        public CddlGenerationResult? EndResult;

        public void BeginGeneration(CddlGenerationContext context)
        {
            Events.Add("begin");
            BeginContext = context;
            context.Options.MaxSteps = 1;
        }

        public CddlControlResult GenerateNode(CddlGenerationContext context)
        {
            Events.Add(context.Rule?.Name ?? "no-rule");
            bool matches = RuleName != null
                ? context.Rule?.Name == RuleName
                : context.Node?.Kind == NodeKind;
            if (!matches)
            {
                return new CddlControlResult
                {
                    Status = CddlControlStatus.NotApplicable
                };
            }
            MatchedCalls = MatchedCalls + 1;
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
            Events.Add("end");
            EndContext = context;
            EndResult = result;
        }
    }

    private class ReenteringHook : ICddlGenerationHook
    {
        public CddlValueGenerator? Generator;
        public CddlSchema? Schema;
        public CddlGenerationResult? ReentrantResult;

        public void BeginGeneration(CddlGenerationContext context)
        {
        }

        public CddlControlResult GenerateNode(CddlGenerationContext context)
        {
            ReentrantResult = Generator!.GenerateValue(
                Schema!,
                Options()
            );
            return new CddlControlResult
            {
                Status = CddlControlStatus.NotApplicable
            };
        }

        public void EndGeneration(
            CddlGenerationContext context,
            CddlGenerationResult result)
        {
        }
    }

    private class ThrowingHook : ICddlGenerationHook
    {
        public string Phase = "";

        public void BeginGeneration(CddlGenerationContext context)
        {
            if (Phase == "begin")
            {
                throw new InvalidOperationException("begin failed");
            }
        }

        public CddlControlResult GenerateNode(CddlGenerationContext context)
        {
            if (Phase == "node")
            {
                throw new InvalidOperationException("node failed");
            }
            return new CddlControlResult
            {
                Status = CddlControlStatus.NotApplicable
            };
        }

        public void EndGeneration(
            CddlGenerationContext context,
            CddlGenerationResult result)
        {
            if (Phase == "end")
            {
                throw new InvalidOperationException("end failed");
            }
        }
    }

    private class InvalidControlHandler : ICddlControlHandler
    {
        public int GenerateCalls;
        public int EvaluateCalls;

        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            EvaluateCalls = EvaluateCalls + 1;
            return new CddlControlResult
            {
                Status = CddlControlStatus.Mismatch
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
                Value = Integer(42)
            };
        }
    }
}
