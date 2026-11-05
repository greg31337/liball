using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlCustomControlTests
{
    [Test]
    public void RegistryStoresDetachedRegistrationsAndRejectsInvalidEntries()
    {
        CddlControlRegistry registry = new CddlControlRegistry();
        ResultHandler handler = new ResultHandler(CddlControlStatus.Applied);
        CddlControlRegistration registration = new CddlControlRegistration
        {
            Name = ".application",
            Handler = handler,
            IsEnabled = true
        };

        registry.AddControl(registration);
        registration.Name = ".mutated";
        registration.IsEnabled = false;
        CddlControlRegistration first = registry.FindControl(".application")!;
        first.Name = ".returned-mutation";
        CddlControlRegistration second = registry.FindControl(".application")!;

        Assert.Multiple(() =>
        {
            Assert.That(first.Handler, Is.SameAs(handler));
            Assert.That(second.Name, Is.EqualTo(".application"));
            Assert.That(second.IsEnabled, Is.True);
            Assert.That(registry.FindControl(".missing"), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                registry.AddControl(null!));
            Assert.Throws<ArgumentException>(() => registry.AddControl(
                new CddlControlRegistration { Name = "application" }));
            Assert.Throws<ArgumentException>(() => registry.AddControl(
                new CddlControlRegistration { Name = ".missing-handler" }));
            Assert.Throws<ArgumentException>(() => registry.AddControl(
                new CddlControlRegistration
                {
                    Name = ".application",
                    Handler = handler
                }));
        });
    }

    [Test]
    public void AppliedAndMismatchResultsDriveValidationAndAreDetached()
    {
        ResultHandler appliedHandler =
            new ResultHandler(CddlControlStatus.Applied);
        CddlSchema appliedSchema = Compile(
            "root = uint .application \"controller\"",
            Registration(".application", appliedHandler));
        CddlValidationResult applied = Validate(appliedSchema, Integer(7));

        CddlControlResult mismatchResult = new CddlControlResult
        {
            Status = CddlControlStatus.Mismatch,
            ErrorCode = "APPLICATION_REJECTED",
            ErrorMessage = "The application constraint rejected the value."
        };
        ResultHandler mismatchHandler = new ResultHandler(mismatchResult);
        CddlSchema mismatchSchema = Compile(
            "root = uint .application \"controller\"",
            Registration(".application", mismatchHandler));
        CddlValidationResult mismatch = Validate(mismatchSchema, Integer(7));
        mismatchResult.Status = CddlControlStatus.Applied;
        mismatchResult.ErrorCode = "MUTATED";

        Assert.Multiple(() =>
        {
            Assert.That(applied.IsValid, Is.True);
            Assert.That(applied.ControlResults, Has.Length.EqualTo(1));
            Assert.That(applied.ControlResults[0].Status,
                Is.EqualTo(CddlControlStatus.Applied));
            Assert.That(appliedHandler.EvaluationCalls, Is.EqualTo(1));
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(mismatch.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(mismatch.Issues[0].Actual,
                Is.EqualTo("APPLICATION_REJECTED"));
            Assert.That(mismatch.ControlResults[0].Status,
                Is.EqualTo(CddlControlStatus.Mismatch));
            Assert.That(mismatch.ControlResults[0].ErrorCode,
                Is.EqualTo("APPLICATION_REJECTED"));
        });
    }

    [Test]
    public void UnknownAndNotApplicableControlsFollowExplicitPolicy()
    {
        CddlCompileOptions compileOptions = new CddlCompileOptions
        {
            UnknownControlPolicy = CddlUnknownControlPolicy.Ignore
        };
        CddlSchema unknownSchema = new CddlCompiler().CompileSchema(
            Parse("root = uint .unknown 1"),
            compileOptions);
        CddlValidationResult rejected = Validate(unknownSchema, Integer(1));
        CddlValidationResult ignored = new CddlValidator().ValidateCbor(
            unknownSchema,
            Integer(1),
            new CddlValidationOptions
            {
                UnknownControlPolicy = CddlUnknownControlPolicy.Ignore
            });

        ResultHandler notApplicable =
            new ResultHandler(CddlControlStatus.NotApplicable);
        CddlSchema registeredSchema = Compile(
            "root = uint .application 1",
            Registration(".application", notApplicable));
        CddlValidationResult registeredRejected = Validate(
            registeredSchema,
            Integer(1));
        CddlValidationResult registeredIgnored =
            new CddlValidator().ValidateCbor(
                registeredSchema,
                Integer(1),
                new CddlValidationOptions
                {
                    UnknownControlPolicy = CddlUnknownControlPolicy.Ignore
                });

        Assert.Multiple(() =>
        {
            Assert.That(rejected.IsValid, Is.False);
            Assert.That(rejected.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.UnsupportedFeature));
            Assert.That(ignored.IsValid, Is.True);
            Assert.That(registeredRejected.IsValid, Is.False);
            Assert.That(registeredRejected.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.UnsupportedFeature));
            Assert.That(registeredIgnored.IsValid, Is.True);
            Assert.That(registeredIgnored.ControlResults[0].Status,
                Is.EqualTo(CddlControlStatus.NotApplicable));
        });
    }

    [Test]
    public void HandlerIssuesReceiveCurrentInstanceAndSchemaSourceContext()
    {
        CddlControlResult supplied = new CddlControlResult
        {
            Status = CddlControlStatus.Mismatch,
            ErrorCode = "APPLICATION_REJECTED",
            Issues =
            [
                new CddlValidationIssue
                {
                    Message = "A detailed application mismatch."
                }
            ]
        };
        ResultHandler handler = new ResultHandler(supplied);
        string source = "root = [uint .application \"controller\"]";
        CddlSchema schema = Compile(
            source,
            Registration(".application", handler));
        CddlValidationResult result = new CddlValidator().ValidateEncodedCbor(
            schema,
            [0x81, 0x07],
            new CborDecodeOptions(),
            new CddlValidationOptions());
        CddlValidationIssue detail = result.ControlResults[0].Issues[0];
        CddlNode control = schema.NormalizedNodes.Single(node =>
            node.ControlName == ".application");
        supplied.Issues[0].Message = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(detail.Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(detail.Message,
                Is.EqualTo("A detailed application mismatch."));
            Assert.That(detail.InstancePath, Is.EqualTo("$[0]"));
            Assert.That(detail.ByteOffset, Is.EqualTo(1));
            Assert.That(detail.SchemaPath, Does.Contain(".application"));
            Assert.That(detail.RuleName, Is.EqualTo("root"));
            Assert.That(detail.Span.Start,
                Is.EqualTo(control.Span.Start));
        });
    }

    [Test]
    public void RegistrationLookupIsStableForOneValidationOperation()
    {
        ResultHandler original = new ResultHandler(CddlControlStatus.Applied);
        ResultHandler replacement = new ResultHandler(CddlControlStatus.Mismatch);
        MutationHandler mutator = new MutationHandler();
        CddlSchema schema = Compile(
            "root = (uint .mutate 1) .observed 2",
            Registration(".mutate", mutator),
            Registration(".observed", original));
        mutator.Schema = schema;
        mutator.Replacement = replacement;

        CddlValidationResult first = Validate(schema, Integer(1));
        CddlValidationResult second = Validate(schema, Integer(1));

        Assert.Multiple(() =>
        {
            Assert.That(first.IsValid, Is.True);
            Assert.That(first.ControlResults.Select(item => item.Status),
                Is.EqualTo(new[]
                {
                    CddlControlStatus.Applied,
                    CddlControlStatus.Applied
                }));
            Assert.That(second.IsValid, Is.False);
            Assert.That(original.EvaluationCalls, Is.EqualTo(1));
            Assert.That(replacement.EvaluationCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void CompilationSnapshotsRegistrationsBeforeResolverCallbacks()
    {
        ResultHandler handler = new ResultHandler(CddlControlStatus.Applied);
        CddlCompileOptions options = new CddlCompileOptions();
        options.ControlRegistrations.Add(
            Registration(".application", handler));
        MutatingResolver resolver = new MutatingResolver();
        resolver.Options = options;
        resolver.Rule = Parse("external = uint").Rules[0];
        options.RuleResolver = resolver;

        CddlSchema schema = new CddlCompiler().CompileSchema(
            Parse("root = external .application 1"),
            options);
        CddlValidationResult result = Validate(schema, Integer(1));

        Assert.Multiple(() =>
        {
            Assert.That(options.ControlRegistrations, Is.Empty);
            Assert.That(schema.ControlRegistrations, Has.Length.EqualTo(1));
            Assert.That(schema.ControlRegistrations[0].Name,
                Is.EqualTo(".application"));
            Assert.That(result.IsValid, Is.True);
            Assert.That(handler.EvaluationCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidOperationRegistrationSetsFailDeterministically()
    {
        ResultHandler handler = new ResultHandler(CddlControlStatus.Applied);
        CddlCompileOptions duplicateOptions = new CddlCompileOptions();
        duplicateOptions.ControlRegistrations.Add(
            Registration(".application", handler));
        duplicateOptions.ControlRegistrations.Add(
            Registration(".application", handler));
        CddlCompileResult duplicate = new CddlCompiler().TryCompileSchema(
            Parse("root = uint .application 1"),
            duplicateOptions);

        CddlSchema schema = Compile(
            "root = uint .application 1",
            Registration(".application", handler));
        schema.ControlRegistrations =
        [
            Registration(".application", handler),
            Registration(".application", handler)
        ];
        CddlValidationResult validation = Validate(schema, Integer(1));

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.IsSuccess, Is.False);
            Assert.That(duplicate.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_CONTROL_REGISTRATION_INVALID"));
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
            Assert.That(validation.Issues[0].Message,
                Does.Contain("registered more than once"));
        });
    }

    private static CddlSchema Compile(
        string source,
        params CddlControlRegistration[] registrations)
    {
        CddlCompileOptions options = new CddlCompileOptions
        {
        };
        options.ControlRegistrations.AddRange(registrations);
        return new CddlCompiler().CompileSchema(Parse(source), options);
    }

    private static CddlDocument Parse(string source)
    {
        return new CddlParser().ParseDocument(source, new CddlParseOptions());
    }

    private static CddlValidationResult Validate(
        CddlSchema schema,
        CborValue value)
    {
        return new CddlValidator().ValidateCbor(
            schema,
            value,
            new CddlValidationOptions());
    }

    private static CddlControlRegistration Registration(
        string name,
        ICddlControlHandler handler)
    {
        return new CddlControlRegistration
        {
            Name = name,
            Handler = handler
        };
    }

    private static CborValue Integer(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private sealed class ResultHandler : ICddlControlHandler
    {
        public CddlControlResult Result;
        public int EvaluationCalls;

        public ResultHandler(CddlControlStatus status)
        {
            Result = new CddlControlResult { Status = status };
        }

        public ResultHandler(CddlControlResult result)
        {
            Result = result;
        }

        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            EvaluationCalls = EvaluationCalls + 1;
            return Result;
        }

        public CddlControlResult GenerateControl(
            CddlNode target,
            CddlNode controller,
            CddlGenerationContext context)
        {
            return Result;
        }
    }

    private sealed class MutationHandler : ICddlControlHandler
    {
        public CddlSchema? Schema;
        public ICddlControlHandler? Replacement;

        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            Schema!.ControlRegistrations[1].Handler = Replacement;
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
            return new CddlControlResult();
        }
    }

    private sealed class MutatingResolver : ICddlRuleResolver
    {
        public CddlCompileOptions? Options;
        public CddlRule? Rule;

        public CddlRule? ResolveRule(string name, CddlRuleKind kind)
        {
            Options!.ControlRegistrations.Clear();
            if (name == "external" && kind == CddlRuleKind.Type)
            {
                return Rule;
            }
            return null;
        }
    }
}
