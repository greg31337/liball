using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CddlCompilerTests
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
    public void CompilesMergedNormalizedAndGenericRulesIntoAPublicSchema()
    {
        CddlDocument document = ParseDone(
            "root = [box<1>, box<1>, plain]\n"
            + "box<t> = [t]\n"
            + "plain = 7");
        CddlCompileOptions options = NoPrelude();
        ICddlCompiler compiler = new CddlCompiler();
        CddlCompileResult result = compiler.TryCompileSchema(document, options);
        CddlSchema throwingSchema = compiler.CompileSchema(document, options);
        CddlSchema schema = result.Schema!;
        CddlNode group = schema.RootRule!.Value!.Children[0];
        CddlNode firstBox = EntryValue(group.Children[0]);
        CddlNode secondBox = EntryValue(group.Children[1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(schema.Rules.Select(item => item.Name),
                Is.EqualTo(new string[] { "root", "plain", "box<0>" }));
            Assert.That(schema.RootRule, Is.SameAs(schema.Rules[0]));
            Assert.That(schema.NormalizedNodes, Is.Not.Empty);
            Assert.That(schema.SourceMap, Has.Length.EqualTo(
                schema.NormalizedNodes.Length));
            Assert.That(firstBox.Name, Is.EqualTo("box<0>"));
            Assert.That(secondBox.Name, Is.EqualTo("box<0>"));
            Assert.That(firstBox.Children, Is.Empty);
            Assert.That(secondBox.Children, Is.Empty);
            Assert.That(schema.Document, Is.Not.SameAs(document));
            Assert.That(throwingSchema.RootRule!.Name, Is.EqualTo("root"));
            Assert.That(throwingSchema, Is.Not.SameAs(schema));
            Assert.That(throwingSchema.NormalizedNodes,
                Is.Not.SameAs(schema.NormalizedNodes));
        });
    }

    [Test]
    public void MaterializedSchemaIsDetachedAndCapturesFeaturesAndControls()
    {
        CddlDocument document = ParseDone(
            "root = 1 .feature \"alpha\"\n"
            + "other = 2 .feature feature-name\n"
            + "feature-name = \"beta\"");
        CddlCompileOptions options = NoPrelude();
        CddlControlRegistration registration = new CddlControlRegistration
        {
            Name = ".application",
            Handler = new TestControlHandler()
        };
        options.ControlRegistrations.Add(registration);
        CddlSchema schema = new CddlCompiler().CompileSchema(document, options);
        int originalRootStart = schema.RootRule!.Span.Start;

        document.SourceText = "changed";
        document.Rules[0].Name = "changed";
        document.Rules[0].Span.Start = 999;
        document.Rules[0].Value!.IntegerValue = 999;
        registration.Name = ".changed";
        options.ControlRegistrations.Add(new CddlControlRegistration());

        Assert.Multiple(() =>
        {
            Assert.That(schema.Document!.SourceText,
                Does.StartWith("root = 1"));
            Assert.That(schema.Document.Rules[0].Name, Is.EqualTo("root"));
            Assert.That(schema.RootRule.Name, Is.EqualTo("root"));
            Assert.That(schema.RootRule.Span.Start, Is.EqualTo(originalRootStart));
            Assert.That(schema.RootRule.Value!.Children[0].IntegerValue,
                Is.EqualTo(new BigInteger(1)));
            Assert.That(schema.ControlRegistrations, Has.Length.EqualTo(1));
            Assert.That(schema.ControlRegistrations[0].Name,
                Is.EqualTo(".application"));
            Assert.That(schema.Features,
                Is.EqualTo(new string[] { "alpha", "beta" }));
            Assert.That(schema.SourceMap[0],
                Is.Not.SameAs(schema.NormalizedNodes[0].Span));
        });
    }

    [Test]
    public void OrdersDiagnosticsBySourceBeforeApplyingTheGlobalCap()
    {
        string source = "root = [3*2 1, missing]";
        CddlDocument document = ParseDone(source);
        CddlCompileOptions options = NoPrelude();
        ICddlCompiler compiler = new CddlCompiler();
        CddlCompileResult result = compiler.TryCompileSchema(document, options);
        CddlCompileOptions cappedOptions = NoPrelude();
        cappedOptions.MaxDiagnostics = 1;
        CddlCompileResult capped = compiler.TryCompileSchema(
            document,
            cappedOptions);
        CddlCompileException exception = Assert.Throws<CddlCompileException>(() =>
            compiler.CompileSchema(document, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Schema, Is.Null);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new string[]
                {
                    "CDDL_COMPILE_OCCURRENCE_INVALID",
                    "CDDL_COMPILE_REFERENCE_UNDEFINED"
                }));
            Assert.That(result.Diagnostics.Select(item => item.Span.Start),
                Is.Ordered.Ascending);
            Assert.That(capped.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(capped.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_OCCURRENCE_INVALID"));
            Assert.That(exception.ErrorCode,
                Is.EqualTo("CDDL_COMPILE_OCCURRENCE_INVALID"));
            Assert.That(exception.RuleName, Is.EqualTo("root"));
            Assert.That(exception.Span!.Start,
                Is.EqualTo(source.IndexOf("3*2", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void ReportsMissingInputsEmptyDocumentsAndParsedErrors()
    {
        ICddlCompiler compiler = new CddlCompiler();
        CddlCompileOptions options = NoPrelude();
        CddlCompileResult missingOptions = compiler.TryCompileSchema(
            new CddlDocument(),
            null!);
        CddlCompileResult missingDocument = compiler.TryCompileSchema(
            null!,
            options);
        CddlDocument empty = ParseDone("; empty\n");
        CddlCompileResult emptyResult = compiler.TryCompileSchema(empty, options);
        CddlDocument parsedError = new CddlDocument
        {
            SourceName = "partial.cddl"
        };
        parsedError.Diagnostics.Add(new CddlDiagnostic
        {
            Code = "CDDL_PARSE_PARTIAL",
            Message = "partial source",
            Span = new CddlSourceSpan
            {
                SourceName = "partial.cddl",
                Start = 4
            }
        });
        CddlCompileResult parsedErrorResult = compiler.TryCompileSchema(
            parsedError,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(missingOptions.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_OPTIONS_MISSING"));
            Assert.That(missingDocument.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_DOCUMENT_MISSING"));
            Assert.That(emptyResult.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_ROOT_MISSING"));
            Assert.That(parsedErrorResult.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(parsedErrorResult.Diagnostics[0].Code,
                Is.EqualTo("CDDL_PARSE_PARTIAL"));
            Assert.Throws<CddlCompileException>(() =>
                compiler.CompileSchema(empty, options));
        });
    }

    [Test]
    public void HonorsAnExplicitRootAndKeepsWarningsOnSuccess()
    {
        CddlDocument document = ParseDone("first = 1\nselected = 2");
        document.Diagnostics.Add(new CddlDiagnostic
        {
            Severity = CddlDiagnosticSeverity.Warning,
            Code = "CDDL_WARNING",
            Message = "warning",
            Span = document.Rules[0].Span
        });
        CddlCompileOptions options = NoPrelude();
        options.RootRuleName = "selected";
        CddlCompileResult result = new CddlCompiler().TryCompileSchema(
            document,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Schema!.RootRule!.Name, Is.EqualTo("selected"));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Severity,
                Is.EqualTo(CddlDiagnosticSeverity.Warning));
            Assert.That(result.Schema.Document!.Diagnostics,
                Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ErrorsStillFailWhenDiagnosticRetentionIsDisabled()
    {
        CddlCompileOptions options = NoPrelude();
        options.MaxDiagnostics = 0;
        CddlDocument document = ParseDone("root = missing");
        ICddlCompiler compiler = new CddlCompiler();
        CddlCompileResult result = compiler.TryCompileSchema(document, options);
        CddlCompileException exception = Assert.Throws<CddlCompileException>(() =>
            compiler.CompileSchema(document, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Schema, Is.Null);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(exception.ErrorCode, Is.EqualTo("CDDL_COMPILE_FAILED"));
        });
    }

    private static CddlNode EntryValue(CddlNode entry)
    {
        return entry.Children[entry.Children.Count - 1];
    }

    private static CddlDocument ParseDone(string source)
    {
        CddlParseResult result = new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "compile.cddl" });
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Document, Is.Not.Null);
        return result.Document!;
    }

    private static CddlCompileOptions NoPrelude()
    {
        return new CddlCompileOptions
        {
            IncludeStandardPrelude = false
        };
    }
}
