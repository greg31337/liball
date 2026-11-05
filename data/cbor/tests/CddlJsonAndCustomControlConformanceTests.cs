using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlJsonAndCustomControlConformanceTests
{
    [Test]
    public void DirectJsonMapsEverySupportedJsonDataModelKind()
    {
        string source =
            "root = [tstr, true, false, null, nint, uint, float16, [], {}]";
        CddlValidationResult result = ValidateJson(
            Compile(source),
            "[\"text\",true,false,null,-1,0,1.5,[],{}]",
            new CddlValidationOptions());

        Assert.That(
            result.IsValid,
            Is.True,
            result.Issues.Length == 0 ? "" : result.Issues[0].Message);
    }

    [Test]
    public void DirectJsonPreservesIntegralValuesAndDuplicateObjectMembers()
    {
        CddlValidationResult positive = ValidateJson(
            Compile("root = 18446744073709551616"),
            "184467440737095516160e-1",
            new CddlValidationOptions());
        CddlValidationResult negative = ValidateJson(
            Compile("root = -18446744073709551617"),
            "-18446744073709551617.0",
            new CddlValidationOptions());
        CddlSchema duplicates = Compile("root = {2*2 tstr => uint}");
        CddlValidationResult twoMembers = ValidateJson(
            duplicates,
            "{\"x\":1,\"x\":2}",
            new CddlValidationOptions());
        CddlValidationResult oneMember = ValidateJson(
            duplicates,
            "{\"x\":1}",
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(positive.IsValid, Is.True);
            Assert.That(negative.IsValid, Is.True);
            Assert.That(twoMembers.IsValid, Is.True);
            Assert.That(oneMember.IsValid, Is.False);
        });
    }

    [Test]
    public void DirectJsonEnforcesJsonCompatibleCddlMappings()
    {
        Assert.Multiple(() =>
        {
            AssertInvalidJson("root = bstr", "\"AQ\"");
            AssertInvalidJson("root = #6.1(tstr)", "\"text\"");
            AssertInvalidJson("root = undefined", "null");
            AssertInvalidJson("root = #7.0", "0");
            AssertInvalidJson("root = {1 => uint}", "{\"1\":1}");
            AssertValidJson("root = {* tstr => uint}", "{\"1\":1}");
        });
    }

    [Test]
    public void DirectJsonChecksAllNumericMappingFormsAndWidths()
    {
        Assert.Multiple(() =>
        {
            AssertValidJson("root = 10", "10");
            AssertValidJson("root = 10", "10.0");
            AssertValidJson("root = 10", "1e1");
            AssertValidJson("root = 10", "100e-1");
            AssertValidJson("root = -1..1", "-0.5");
            AssertValidJson("root = float16", "1.5");
            AssertInvalidJson("root = float16", "1.1");
            AssertValidJson("root = float32", "16777216");
            AssertInvalidJson("root = float32", "16777217");
            AssertValidJson("root = float64", "9007199254740992");
            AssertInvalidJson("root = float64", "9007199254740993");
            AssertValidJson("root = any", "0.1");
        });
    }

    [Test]
    public void DirectJsonSelectsEnabledFeatureCombinationsWithoutLeaks()
    {
        string source =
            "root = (uint .feature \"numeric\") / "
            + "(tstr .feature [\"textual\", \"explicit\"]) / "
            + "(any .feature \"fallback\")";
        CddlSchema schema = Compile(source);
        CddlValidationResult numeric = ValidateJson(
            schema,
            "7",
            Features("numeric"));
        CddlValidationResult textual = ValidateJson(
            schema,
            "\"seven\"",
            Features("textual"));
        CddlValidationResult fallback = ValidateJson(
            schema,
            "true",
            Features("fallback"));
        CddlValidationResult all = ValidateJson(
            schema,
            "7",
            Features("numeric", "textual", "fallback"));
        CddlValidationResult disabled = ValidateJson(
            schema,
            "7",
            Features());

        Assert.Multiple(() =>
        {
            Assert.That(numeric.IsValid, Is.True);
            Assert.That(numeric.ControlResults.Single().FeatureName,
                Is.EqualTo("numeric"));
            Assert.That(numeric.ControlResults.Single().FeatureDetail!
                .IntegerValue, Is.EqualTo(new BigInteger(7)));
            Assert.That(textual.IsValid, Is.True);
            Assert.That(textual.ControlResults.Single().FeatureName,
                Is.EqualTo("textual"));
            Assert.That(textual.ControlResults.Single().FeatureDetail!
                .TextStringValue, Is.EqualTo("explicit"));
            Assert.That(fallback.IsValid, Is.True);
            Assert.That(fallback.ControlResults.Single().FeatureDetail!
                .SimpleValue, Is.EqualTo(CborSimpleValues.True));
            Assert.That(all.IsValid, Is.True);
            Assert.That(all.ControlResults, Has.Length.EqualTo(1));
            Assert.That(all.ControlResults[0].FeatureName,
                Is.EqualTo("numeric"));
            Assert.That(disabled.IsValid, Is.False);
            Assert.That(disabled.ControlResults, Is.Empty);
        });
    }

    [Test]
    public void DirectJsonIgnoresNullAndDuplicateFeatureNames()
    {
        CddlValidationOptions options = new CddlValidationOptions
        {
            EnabledFeatures = new string[] { "alpha", null!, "alpha" }
        };
        CddlValidationResult result = ValidateJson(
            Compile("root = uint .feature \"alpha\""),
            "1",
            options);
        options.EnabledFeatures[0] = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Options.EnabledFeatures,
                Is.EqualTo(new string[] { "alpha", null!, "alpha" }));
            Assert.That(result.ControlResults, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void NullFeatureCollectionBehavesAsNoEnabledFeatures()
    {
        CddlValidationOptions options = new CddlValidationOptions
        {
            EnabledFeatures = null!
        };
        CddlValidationResult result = ValidateJson(
            Compile("root = uint .feature \"alpha\""),
            "1",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.UnsupportedFeature));
            Assert.That(result.ControlResults, Is.Empty);
            Assert.That(result.Options.EnabledFeatures, Is.Empty);
        });
    }

    [Test]
    public void DirectJsonReportsNestedMismatchAndSyntaxContext()
    {
        string source =
            "root = {\n"
            + "  \"outer\": [\n"
            + "    {\"value\": uint},\n"
            + "    {\"value\": tstr}\n"
            + "  ]\n"
            + "}";
        CddlSchema schema = Compile(source);
        CddlValidationResult mismatch = ValidateJson(
            schema,
            "{\"outer\":[{\"value\":1},{\"value\":2}]}",
            new CddlValidationOptions());
        CddlValidationResult syntax = ValidateJson(
            schema,
            "{\"outer\":[{\"value\":1},]}",
            new CddlValidationOptions());
        CddlValidationIssue[] flattened = FlattenIssues(mismatch.Issues);
        CddlValidationIssue nested = flattened
            .First(item => item.InstancePath
                == "$[\"outer\"][1][\"value\"]");
        CddlValidationIssue userSchemaIssue = flattened.First(item =>
            item.Span.SourceName == "json-custom-conformance.cddl");

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.IsValid, Is.False);
            Assert.That(nested.SchemaPath, Does.Contain("/rule(root)"));
            Assert.That(userSchemaIssue.Span.SourceName,
                Is.EqualTo("json-custom-conformance.cddl"));
            Assert.That(userSchemaIssue.Span.Length, Is.GreaterThan(0));
            Assert.That(syntax.IsValid, Is.False);
            Assert.That(syntax.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidEncoding));
            Assert.That(syntax.Issues[0].InstancePath,
                Is.EqualTo("$.property[0][1]"));
            Assert.That(syntax.Issues[0].ByteOffset,
                Is.GreaterThanOrEqualTo(0));
            Assert.That(syntax.Issues[0].Span.SourceName,
                Is.EqualTo("json-custom-conformance.cddl"));
        });
    }

    [TestCase(CddlControlStatus.None, false,
        CddlValidationCode.ControlMismatch)]
    [TestCase(CddlControlStatus.Applied, true,
        CddlValidationCode.None)]
    [TestCase(CddlControlStatus.Mismatch, false,
        CddlValidationCode.ControlMismatch)]
    [TestCase(CddlControlStatus.NotApplicable, false,
        CddlValidationCode.UnsupportedFeature)]
    [TestCase(CddlControlStatus.Unsupported, false,
        CddlValidationCode.UnsupportedFeature)]
    [TestCase(CddlControlStatus.Error, false,
        CddlValidationCode.ControlMismatch)]
    public void EveryCustomHandlerStatusHasADefinedValidationOutcome(
        CddlControlStatus status,
        bool expectedValid,
        CddlValidationCode expectedCode)
    {
        FixedHandler handler = new FixedHandler(
            new CddlControlResult { Status = status });
        CddlValidationResult result = ValidateCbor(
            Compile(
                "root = uint .application \"controller\"",
                Registration(".application", handler)),
            Integer(7),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.EqualTo(expectedValid));
            Assert.That(result.ControlResults, Has.Length.EqualTo(1));
            Assert.That(result.ControlResults[0].Status, Is.EqualTo(status));
            if (expectedCode == CddlValidationCode.None)
            {
                Assert.That(result.Issues, Is.Empty);
            }
            else
            {
                Assert.That(result.Issues[0].Code,
                    Is.EqualTo(expectedCode));
                Assert.That(result.Issues[0].Actual,
                    Is.EqualTo(status.ToString()));
            }
        });
    }

    [Test]
    public void NullHandlerResultsBecomeStableErrorOutcomes()
    {
        FixedHandler handler = new FixedHandler(null);
        CddlValidationResult result = ValidateCbor(
            Compile(
                "root = uint .application 1",
                Registration(".application", handler)),
            Integer(1),
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(result.Issues[0].Actual,
                Is.EqualTo("CDDL_CONTROL_RESULT_MISSING"));
            Assert.That(result.ControlResults.Single().Status,
                Is.EqualTo(CddlControlStatus.Error));
            Assert.That(result.ControlResults.Single().ErrorCode,
                Is.EqualTo("CDDL_CONTROL_RESULT_MISSING"));
        });
    }

    [Test]
    public void NotApplicableCanBeIgnoredButUnsupportedAndErrorsCannot()
    {
        CddlValidationOptions ignore = new CddlValidationOptions
        {
            UnknownControlPolicy = CddlUnknownControlPolicy.Ignore
        };
        CddlValidationResult notApplicable = ValidateCbor(
            Compile(
                "root = uint .application 1",
                Registration(
                    ".application",
                    new FixedHandler(CddlControlStatus.NotApplicable))),
            Integer(1),
            ignore);
        CddlValidationResult unsupported = ValidateCbor(
            Compile(
                "root = uint .application 1",
                Registration(
                    ".application",
                    new FixedHandler(CddlControlStatus.Unsupported))),
            Integer(1),
            ignore);
        CddlValidationResult error = ValidateCbor(
            Compile(
                "root = uint .application 1",
                Registration(
                    ".application",
                    new FixedHandler(CddlControlStatus.Error))),
            Integer(1),
            ignore);

        Assert.Multiple(() =>
        {
            Assert.That(notApplicable.IsValid, Is.True);
            Assert.That(unsupported.IsValid, Is.False);
            Assert.That(error.IsValid, Is.False);
        });
    }

    [Test]
    public void DisabledAndUnknownControlsRespectValidationPolicy()
    {
        CddlControlRegistration disabled = new CddlControlRegistration
        {
            Name = ".application",
            IsEnabled = false
        };
        CddlSchema disabledSchema = CompileIgnoringUnknown(
            "root = uint .application 1",
            disabled);
        CddlSchema unknownSchema = CompileIgnoringUnknown(
            "root = uint .unknown 1");

        Assert.Multiple(() =>
        {
            Assert.That(ValidateCbor(
                disabledSchema,
                Integer(1),
                new CddlValidationOptions()).IsValid, Is.False);
            Assert.That(ValidateCbor(
                disabledSchema,
                Integer(1),
                IgnoreUnknown()).IsValid, Is.True);
            Assert.That(ValidateCbor(
                unknownSchema,
                Integer(1),
                new CddlValidationOptions()).IsValid, Is.False);
            Assert.That(ValidateCbor(
                unknownSchema,
                Integer(1),
                IgnoreUnknown()).IsValid, Is.True);
        });
    }

    [Test]
    public void RegistryAcceptsAllNameBoundariesAndDetachesEntries()
    {
        string[] validNames =
        [
            ".a", "._private", ".@application", ".$vendor",
            ".a0", ".a-b", ".a.b"
        ];
        string[] invalidNames =
        [
            "", ".", "application", ".0name", ".name-", ".name.",
            ".name?"
        ];
        FixedHandler handler = new FixedHandler(CddlControlStatus.Applied);
        CddlControlRegistry registry = new CddlControlRegistry();
        int index = 0;
        while (index < validNames.Length)
        {
            CddlControlRegistration registration = new CddlControlRegistration
            {
                Name = validNames[index],
                Handler = handler
            };
            registry.AddControl(registration);
            registration.Name = ".mutated" + index;
            CddlControlRegistration found = registry.FindControl(
                validNames[index])!;
            found.Name = ".returned-mutation";
            Assert.That(registry.FindControl(validNames[index])!.Name,
                Is.EqualTo(validNames[index]));
            index = index + 1;
        }
        registry.AddControl(new CddlControlRegistration
        {
            Name = ".disabled",
            IsEnabled = false
        });

        Assert.Multiple(() =>
        {
            Assert.That(registry.FindControl(".disabled")!.IsEnabled,
                Is.False);
            Assert.That(registry.FindControl(".disabled")!.Handler,
                Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                registry.FindControl(null!));
            index = 0;
            while (index < invalidNames.Length)
            {
                string name = invalidNames[index];
                Assert.Throws<ArgumentException>(() => registry.AddControl(
                    new CddlControlRegistration
                    {
                        Name = name,
                        Handler = handler
                    }));
                index = index + 1;
            }
        });
    }

    [Test]
    public void RegistryAndOperationMutationBoundariesAreIsolated()
    {
        OptionsMutationHandler mutator = new OptionsMutationHandler();
        FixedHandler observer = new FixedHandler(CddlControlStatus.Applied);
        CddlSchema schema = Compile(
            "root = ((uint .mutate 1) .observe 2) .feature \"alpha\"",
            Registration(".mutate", mutator),
            Registration(".observe", observer));
        CddlValidationOptions options = Features("alpha");
        CddlValidationResult result = ValidateCbor(
            schema,
            Integer(1),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(observer.Calls, Is.EqualTo(1));
            Assert.That(result.ControlResults.Select(item => item.Status),
                Is.EqualTo(new[]
                {
                    CddlControlStatus.Applied,
                    CddlControlStatus.Applied,
                    CddlControlStatus.Applied
                }));
            Assert.That(result.ControlResults[2].FeatureName,
                Is.EqualTo("alpha"));
            Assert.That(options.EnabledFeatures,
                Is.EqualTo(new string[] { "alpha" }));
            Assert.That(options.MaxSteps, Is.EqualTo(1000000));
        });
    }

    [Test]
    public void DirectJsonCustomControlsReceiveConvertedValuesAndContext()
    {
        InspectingHandler handler = new InspectingHandler();
        CddlSchema schema = Compile(
            "root = {\n"
            + "  \"items\": [uint, uint .application \"second\"]\n"
            + "}",
            Registration(".application", handler));
        CddlValidationResult result = ValidateJson(
            schema,
            "{\"items\":[1,2]}",
            Features("json-mode"));
        CddlValidationIssue detail = FlattenIssues(result.Issues).First(item =>
            item.Message == "The converted JSON integer was rejected.");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ControlResults, Is.Empty);
            Assert.That(handler.Calls, Is.EqualTo(1));
            Assert.That(handler.Kind,
                Is.EqualTo(CborValueKind.UnsignedInteger));
            Assert.That(handler.IntegerValue, Is.EqualTo(new BigInteger(2)));
            Assert.That(handler.ControllerText, Is.EqualTo("second"));
            Assert.That(handler.EnabledFeatures,
                Is.EqualTo(new string[] { "json-mode" }));
            Assert.That(detail.InstancePath,
                Is.EqualTo("$[\"items\"][1]"));
            Assert.That(detail.ByteOffset, Is.EqualTo(-1));
            Assert.That(detail.SchemaPath, Does.Contain(".application"));
            Assert.That(detail.RuleName, Is.EqualTo("root"));
            Assert.That(detail.Span.SourceName,
                Is.EqualTo("json-custom-conformance.cddl"));
            Assert.That(detail.Span.StartLine, Is.EqualTo(2));
        });
    }

    [Test]
    public void CustomIssueContextDefaultsAndExplicitFieldsAreBothPreserved()
    {
        CddlSourceSpan suppliedSpan = new CddlSourceSpan
        {
            SourceName = "handler",
            Start = 4,
            Length = 2,
            StartLine = 3,
            StartColumn = 5,
            EndLine = 3,
            EndColumn = 7
        };
        CddlValidationIssue explicitIssue = new CddlValidationIssue
        {
            Code = CddlValidationCode.RangeMismatch,
            Severity = CddlDiagnosticSeverity.Warning,
            Message = "explicit",
            InstancePath = "$.handler",
            ByteOffset = 99,
            SchemaPath = "/handler",
            RuleName = "handler-rule",
            Expected = "expected",
            Actual = "actual",
            Span = suppliedSpan
        };
        CddlValidationIssue defaulted = new CddlValidationIssue
        {
            Message = "",
            Span = null!,
            Details = [explicitIssue]
        };
        CddlControlResult supplied = new CddlControlResult
        {
            Status = CddlControlStatus.Mismatch,
            Issues = [defaulted]
        };
        CddlSchema schema = Compile(
            "root = [uint .application 1]",
            Registration(".application", new FixedHandler(supplied)));
        CddlValidationResult result = new CddlValidator().ValidateEncodedCbor(
            schema,
            [0x81, 0x01],
            new CborDecodeOptions(),
            new CddlValidationOptions());
        CddlValidationIssue wrapped = result.ControlResults[0].Issues[0];
        CddlValidationIssue nested = wrapped.Details[0];
        explicitIssue.Message = "mutated";
        suppliedSpan.Start = 100;

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.Code,
                Is.EqualTo(CddlValidationCode.ControlMismatch));
            Assert.That(wrapped.Message,
                Is.EqualTo("The custom control handler reported a validation issue."));
            Assert.That(wrapped.InstancePath, Is.EqualTo("$[0]"));
            Assert.That(wrapped.ByteOffset, Is.EqualTo(1));
            Assert.That(wrapped.SchemaPath, Does.Contain(".application"));
            Assert.That(wrapped.RuleName, Is.EqualTo("root"));
            Assert.That(nested.Code,
                Is.EqualTo(CddlValidationCode.RangeMismatch));
            Assert.That(nested.Severity,
                Is.EqualTo(CddlDiagnosticSeverity.Warning));
            Assert.That(nested.Message, Is.EqualTo("explicit"));
            Assert.That(nested.InstancePath, Is.EqualTo("$.handler"));
            Assert.That(nested.ByteOffset, Is.EqualTo(99));
            Assert.That(nested.SchemaPath, Is.EqualTo("/handler"));
            Assert.That(nested.RuleName, Is.EqualTo("handler-rule"));
            Assert.That(nested.Expected, Is.EqualTo("expected"));
            Assert.That(nested.Actual, Is.EqualTo("actual"));
            Assert.That(nested.Span.SourceName, Is.EqualTo("handler"));
            Assert.That(nested.Span.Start, Is.EqualTo(4));
        });
    }

    [Test]
    public void CustomIssueTreesAreBoundedAndCycleSafe()
    {
        CddlValidationIssue cycle = new CddlValidationIssue
        {
            Message = "cycle"
        };
        cycle.Details = [cycle];
        CddlControlResult supplied = new CddlControlResult
        {
            Status = CddlControlStatus.Mismatch,
            Issues = [cycle, null!]
        };
        CddlValidationResult result = ValidateCbor(
            Compile(
                "root = uint .application 1",
                Registration(".application", new FixedHandler(supplied))),
            Integer(1),
            new CddlValidationOptions { MaxNestingDepth = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ControlResults[0].Issues, Has.Length.EqualTo(2));
            Assert.That(result.ControlResults[0].Issues[0].Message,
                Is.EqualTo("cycle"));
            Assert.That(result.ControlResults[0].Issues[0].Details,
                Has.Length.EqualTo(1));
            Assert.That(result.ControlResults[0].Issues[0].Details[0].Details,
                Is.Empty);
            Assert.That(result.ControlResults[0].Issues[1].Message,
                Is.EqualTo(
                    "The custom control handler reported a validation issue."));
        });
    }

    private static void AssertValidJson(string source, string json)
    {
        CddlValidationResult result = ValidateJson(
            Compile(source),
            json,
            new CddlValidationOptions());
        Assert.That(
            result.IsValid,
            Is.True,
            result.Issues.Length == 0 ? "" : result.Issues[0].Message);
    }

    private static void AssertInvalidJson(string source, string json)
    {
        CddlValidationResult result = ValidateJson(
            Compile(source),
            json,
            new CddlValidationOptions());
        Assert.That(result.IsValid, Is.False);
    }

    private static CddlValidationResult ValidateJson(
        CddlSchema schema,
        string json,
        CddlValidationOptions options)
    {
        return new CddlJsonValidator().ValidateJson(schema, json, options);
    }

    private static CddlValidationResult ValidateCbor(
        CddlSchema schema,
        CborValue value,
        CddlValidationOptions options)
    {
        return new CddlValidator().ValidateCbor(schema, value, options);
    }

    private static CddlSchema Compile(
        string source,
        params CddlControlRegistration[] registrations)
    {
        CddlCompileOptions options = new CddlCompileOptions();
        options.ControlRegistrations.AddRange(registrations);
        return new CddlCompiler().CompileSchema(Parse(source), options);
    }

    private static CddlSchema CompileIgnoringUnknown(
        string source,
        params CddlControlRegistration[] registrations)
    {
        CddlCompileOptions options = new CddlCompileOptions
        {
            UnknownControlPolicy = CddlUnknownControlPolicy.Ignore
        };
        options.ControlRegistrations.AddRange(registrations);
        return new CddlCompiler().CompileSchema(Parse(source), options);
    }

    private static CddlDocument Parse(string source)
    {
        return new CddlParser().ParseDocument(
            source,
            new CddlParseOptions
            {
                SourceName = "json-custom-conformance.cddl"
            });
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

    private static CddlValidationOptions Features(params string[] names)
    {
        return new CddlValidationOptions { EnabledFeatures = names };
    }

    private static CddlValidationOptions IgnoreUnknown()
    {
        return new CddlValidationOptions
        {
            UnknownControlPolicy = CddlUnknownControlPolicy.Ignore
        };
    }

    private static CddlValidationIssue[] FlattenIssues(
        CddlValidationIssue[] issues)
    {
        List<CddlValidationIssue> result = new List<CddlValidationIssue>();
        List<CddlValidationIssue> pending = new List<CddlValidationIssue>(issues);
        while (pending.Count > 0)
        {
            CddlValidationIssue issue = pending[0];
            pending.RemoveAt(0);
            result.Add(issue);
            pending.AddRange(issue.Details);
        }
        return result.ToArray();
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

    private class FixedHandler : ICddlControlHandler
    {
        internal CddlControlResult? Result;
        internal int Calls;

        internal FixedHandler(CddlControlStatus status)
        {
            Result = new CddlControlResult { Status = status };
        }

        internal FixedHandler(CddlControlResult? result)
        {
            Result = result;
        }

        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            Calls = Calls + 1;
            return Result!;
        }

        public CddlControlResult GenerateControl(
            CddlNode target,
            CddlNode controller,
            CddlGenerationContext context)
        {
            return Result!;
        }
    }

    private sealed class OptionsMutationHandler : ICddlControlHandler
    {
        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            options.EnabledFeatures[0] = "mutated";
            options.MaxSteps = 0;
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

    private sealed class InspectingHandler : ICddlControlHandler
    {
        internal int Calls;
        internal CborValueKind Kind;
        internal BigInteger IntegerValue;
        internal string? ControllerText;
        internal string[] EnabledFeatures = [];

        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            Calls = Calls + 1;
            Kind = value.Kind;
            IntegerValue = value.IntegerValue;
            ControllerText = controller.TextValue;
            EnabledFeatures = options.EnabledFeatures;
            return new CddlControlResult
            {
                Status = CddlControlStatus.Mismatch,
                ErrorCode = "APPLICATION_REJECTED",
                ErrorMessage = "The application rejected the JSON value.",
                Issues =
                [
                    new CddlValidationIssue
                    {
                        Message = "The converted JSON integer was rejected."
                    }
                ]
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
}
