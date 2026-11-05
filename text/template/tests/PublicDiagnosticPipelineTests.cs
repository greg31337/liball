using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class PublicDiagnosticPipelineTests
{
    [Test]
    public void CollectionSink_PreservesOrderReturnsCopiesAndClears()
    {
        ITemplateDiagnosticSink sink = new TemplateDiagnosticCollectionSink();
        TemplateDiagnostic first = TemplateDiagnostic.Create("ONE", TemplateDiagnosticSeverity.Warning, "first");
        TemplateDiagnostic second = TemplateDiagnostic.Create("TWO", TemplateDiagnosticSeverity.Error, "second");

        sink.Report(first);
        sink.Report(second);
        List<TemplateDiagnostic> copy = sink.GetDiagnostics();
        copy.Clear();

        Assert.That(sink.GetDiagnostics(), Is.EqualTo(new TemplateDiagnostic[] { first, second }));
        sink.Clear();
        Assert.That(sink.GetDiagnostics(), Is.Empty);
        Assert.Throws<ArgumentNullException>(() => sink.Report(null!));
    }

    [Test]
    public void LexerAndParser_ReportDiagnosticsAndRecoverThroughPublicInterfaces()
    {
        TemplateDiagnosticCollectionSink lexerSink = new TemplateDiagnosticCollectionSink();
        ITemplateLexer lexer = new TemplateLexer(TemplateSource.FromString("Hello {{ name"));
        lexer.SetDiagnosticSink(lexerSink);

        TemplateLexerResult lexerResult = lexer.Lex();

        Assert.That(lexerResult.Tokens[lexerResult.Tokens.Count - 1].Kind, Is.EqualTo(TemplateTokenKind.EndOfFile));
        AssertDiagnostic(lexerSink.GetDiagnostics(), "LEX001");

        TemplateLexer cleanLexer = new TemplateLexer(TemplateSource.FromString("{% mystery %}tail"));
        TemplateDiagnosticCollectionSink parserSink = new TemplateDiagnosticCollectionSink();
        ITemplateParser parser = new TemplateParser(cleanLexer.Lex());
        parser.SetDiagnosticSink(parserSink);

        TemplateParseResult parseResult = parser.Parse();

        AssertDiagnostic(parserSink.GetDiagnostics(), "PARSE006");
        Assert.That(parseResult.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Error));
        Assert.That(parseResult.Document.Children[1].Text!.Text, Is.EqualTo("tail"));
    }

    [Test]
    public void Renderer_ReportsEvaluationCompositionAndMemberSecurityDiagnosticsWithLocations()
    {
        TemplateOptions options = new TemplateOptions();
        options.StrictUnsafeMembers = false;
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, new TemplateRegistry(), new TemplateMemberResolver());
        TemplateDiagnosticCollectionSink sink = new TemplateDiagnosticCollectionSink();
        renderer.SetDiagnosticSink(sink);

        string output = renderer.RenderToString("{{ mystery() }}{{ missing }}{{ \"x\"..2 }}{{ GetType }}{% include \"absent\" %}", EmptyModel());
        List<TemplateDiagnostic> diagnostics = sink.GetDiagnostics();

        Assert.That(output, Is.EqualTo(""));
        AssertDiagnostic(diagnostics, "EVAL001");
        AssertDiagnostic(diagnostics, "EVAL002");
        AssertDiagnostic(diagnostics, "EVAL003");
        AssertDiagnostic(diagnostics, "SECURITY011");
        AssertDiagnostic(diagnostics, "COMPOSE001");
    }

    [Test]
    public async Task RendererAsync_ReportsUnknownCallableAndMissingCompositionDiagnostics()
    {
        TemplateRenderer renderer = new TemplateRenderer();
        TemplateDiagnosticCollectionSink sink = new TemplateDiagnosticCollectionSink();
        renderer.SetDiagnosticSink(sink);

        string output = await renderer.RenderToStringAsync("{{ mystery() }}{% render \"absent\" %}", EmptyModel());
        List<TemplateDiagnostic> diagnostics = sink.GetDiagnostics();

        Assert.That(output, Is.EqualTo(""));
        AssertDiagnostic(diagnostics, "EVAL001");
        AssertDiagnostic(diagnostics, "COMPOSE001");
    }

    [Test]
    public void Linter_ReportsStaticDiagnosticFamiliesToReturnValueAndSink()
    {
        TemplateOptions options = new TemplateOptions();
        options.AutoEscape = true;
        options.EscapingMode = TemplateEscapingMode.Html;
        options.CompatibilityMode = TemplateCompatibilityMode.Pragmatic;
        TemplateDiagnosticCollectionSink sink = new TemplateDiagnosticCollectionSink();
        ITemplateLinter linter = new TemplateLinter();
        linter.Configure(options, new TemplateRegistry());
        linter.SetDiagnosticSink(sink);
        string text = "{% macro unused value %}{{ value }}{% endmacro %}{% if false %}dead{% endif %}{% include \"missing\" %}{{ mystery() }}{{ value | safe }}{% return %}tail";

        List<TemplateDiagnostic> diagnostics = linter.Lint(text);

        AssertDiagnostic(diagnostics, "EVAL001");
        AssertDiagnostic(diagnostics, "COMPOSE001");
        AssertDiagnostic(diagnostics, "SECURITY009");
        AssertDiagnostic(diagnostics, "FLOW001");
        AssertDiagnostic(diagnostics, "FLOW002");
        AssertDiagnostic(diagnostics, "UNUSED001");
        AssertDiagnostic(diagnostics, "COMPAT009");
        Assert.That(sink.GetDiagnostics(), Is.EqualTo(diagnostics));
    }

    [Test]
    public void Linter_ReportsDynamicCompositionAndLiquidDeprecation()
    {
        ITemplateLinter nativeLinter = new TemplateLinter();
        List<TemplateDiagnostic> dynamicDiagnostics = nativeLinter.Lint("{% include templateName %}");
        TemplateProfileCatalog catalog = new TemplateProfileCatalog();
        ITemplateLinter liquidLinter = new TemplateLinter();
        liquidLinter.Configure(catalog.CreateLiquidOptions(), new TemplateRegistry());

        List<TemplateDiagnostic> liquidDiagnostics = liquidLinter.Lint("{% include 'legacy' %}");

        AssertDiagnostic(dynamicDiagnostics, "COMPOSE002");
        AssertDiagnostic(liquidDiagnostics, "DEPRECATION001");
    }

    [Test]
    public void Linter_RecognizesRegisteredDependenciesCallablesAndFormatsStableOutput()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("card", "card");
        registry.RegisterFunction("known", delegate(List<TemplateValue> arguments)
        {
            return TemplateValue.Null();
        });
        ITemplateLinter linter = new TemplateLinter();
        linter.Configure(new TemplateOptions(), registry);

        List<TemplateDiagnostic> registeredDiagnostics = linter.Lint("{% include \"card\" %}{{ known() }}");
        List<TemplateDiagnostic> diagnostics = linter.Lint("{{ mystery() }}");
        TemplateDiagnostic unknown = FindDiagnostic(diagnostics, "EVAL001");

        Assert.That(HasDiagnostic(registeredDiagnostics, "COMPOSE001"), Is.False);
        Assert.That(HasDiagnostic(registeredDiagnostics, "EVAL001"), Is.False);
        Assert.That(
            unknown.Format(),
            Is.EqualTo("(anonymous):1:4: warning EVAL001: Unknown template callable 'mystery'.\n{{ mystery() }}\n   ^^^^^^^^^\nhint: Register the callable or correct its name."));
    }

    static TemplateValue EmptyModel()
    {
        return TemplateValue.FromMap(new Dictionary<string, TemplateValue>());
    }

    static void AssertDiagnostic(List<TemplateDiagnostic> diagnostics, string code)
    {
        TemplateDiagnostic diagnostic = FindDiagnostic(diagnostics, code);
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.Span, Is.Not.Null, code + " should identify source text.");
        Assert.That(diagnostic.Hint, Is.Not.Null.And.Not.Empty, code + " should provide remediation guidance.");
    }

    static TemplateDiagnostic FindDiagnostic(List<TemplateDiagnostic> diagnostics, string code)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Code == code)
            {
                return diagnostics[index];
            }

            index = index + 1;
        }

        return TemplateDiagnostic.Create("", TemplateDiagnosticSeverity.Information, "");
    }

    static bool HasDiagnostic(List<TemplateDiagnostic> diagnostics, string code)
    {
        return FindDiagnostic(diagnostics, code).Code == code;
    }
}
