using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlDirectiveProcessingTests
{
    [Test]
    public void Pipeline_EmitsDistinctStructuredYamlAndTagDirectiveEvents()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        string text = "%YAML 1.2\n%TAG !e! tag:example.com,2026:\n---\n!e!thing value\n";

        global::yaml.YamlParserResult result = Parse(pipeline, text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.YamlVersionDirective,
            global::yaml.YamlEventKind.TagDirective,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));

        global::yaml.YamlEvent yamlDirective = result.Events[1];
        global::yaml.YamlEvent tagDirective = result.Events[2];
        global::yaml.YamlEvent scalar = result.Events[4];
        Assert.That(yamlDirective.Directive.Name, Is.EqualTo("YAML"));
        Assert.That(yamlDirective.Directive.Version, Is.EqualTo(global::yaml.YamlVersion.Version12));
        Assert.That(yamlDirective.Directive.Value, Is.EqualTo("1.2"));
        Assert.That(tagDirective.Directive.Name, Is.EqualTo("TAG"));
        Assert.That(tagDirective.Directive.Handle, Is.EqualTo("!e!"));
        Assert.That(tagDirective.Directive.Prefix, Is.EqualTo("tag:example.com,2026:"));
        Assert.That(scalar.Tag, Is.EqualTo("tag:example.com,2026:thing"));
    }

    [Test]
    public void Processor_CarriesDirectivesWarningsAndTagExpansionIntoDocumentAndReemitsThem()
    {
        global::yaml.IYamlProcessor processor = new global::yaml.YamlProcessor();
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        string input = "%YAML 1.2\n%TAG !e! tag:example.com,2026:\n%VENDOR alpha beta\n---\n!e!thing value\n";

        global::yaml.YamlDocument document = processor.Parse(input);
        string emitted = pipeline.EmitDocument(document, null);

        Assert.That(document.Directives.Select(directive => directive.Name), Is.EqualTo(new string[] { "YAML", "TAG", "VENDOR" }));
        Assert.That(document.Directives[0].Version, Is.EqualTo(global::yaml.YamlVersion.Version12));
        Assert.That(document.Directives[1].Handle, Is.EqualTo("!e!"));
        Assert.That(document.Directives[2].Value, Is.EqualTo("alpha beta"));
        Assert.That(document.Root!.Tag, Is.EqualTo("tag:example.com,2026:thing"));
        Assert.That(document.Diagnostics.Single().Code, Is.EqualTo("YAMLP011"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(global::yaml.YamlDiagnosticSeverity.Warning));
        Assert.That(emitted, Is.EqualTo(input));
    }

    [Test]
    public void Parser_ReportsRepeatedYamlAndTagDirectives()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;
        string text = "%YAML 1.2\n%YAML 1.1\n%TAG !e! tag:first:\n%TAG !e! tag:second:\n---\nvalue\n";

        global::yaml.YamlParserResult result = Parse(pipeline, text, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLP007", "YAMLP010" }));
        Assert.That(result.Events.Count(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.YamlVersionDirective), Is.EqualTo(2));
        Assert.That(result.Events.Count(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.TagDirective), Is.EqualTo(2));
    }

    [TestCase("%YAML one.two\n---\nvalue\n", "YAMLP006")]
    [TestCase("%YAML 1.2 extra\n---\nvalue\n", "YAMLP006")]
    [TestCase("%TAG !bad value extra\n---\nvalue\n", "YAMLP009")]
    [TestCase("%TAG !e!\n---\nvalue\n", "YAMLP009")]
    public void Parser_ValidatesDirectiveArguments(string text, string expectedCode)
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;

        global::yaml.YamlParserResult result = Parse(pipeline, text, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain(expectedCode));
    }

    [Test]
    public void Parser_PreservesReservedDirectiveAsWarning()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();

        global::yaml.YamlParserResult result = Parse(pipeline, "%RESERVED one two\n---\nvalue\n", new global::yaml.YamlParserOptions());

        global::yaml.YamlEvent directiveEvent = result.Events.Single(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.ReservedDirective);
        Assert.That(result.Success, Is.True);
        Assert.That(directiveEvent.Directive.Name, Is.EqualTo("RESERVED"));
        Assert.That(directiveEvent.Directive.Value, Is.EqualTo("one two"));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLP011"));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(global::yaml.YamlDiagnosticSeverity.Warning));
    }

    [Test]
    public void Parser_ExpandsPrimarySecondaryAndNamedTagHandles()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        string text = "%TAG ! tag:local/\n%TAG !! tag:standard/\n%TAG !e! tag:example/\n---\n[!item a, !!text b, !e!kind c]\n";

        global::yaml.YamlParserResult result = Parse(pipeline, text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(
            result.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Tag),
            Is.EqualTo(new string[] { "tag:local/item", "tag:standard/text", "tag:example/kind" }));
    }

    [Test]
    public void Parser_ReportsUndefinedNamedTagHandle()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;

        global::yaml.YamlParserResult result = Parse(pipeline, "---\n!missing!thing value\n", options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLP013"));
    }

    [Test]
    public void Directives_ResetBetweenDocumentsAndUseEachDocumentsTagMapping()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        string text = "%YAML 1.2\n%TAG !e! tag:first/\n---\n!e!thing one\n...\n%YAML 1.1\n%TAG !e! tag:second/\n---\n!e!thing two\n";

        global::yaml.YamlParserResult parser = Parse(pipeline, text, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = pipeline.ComposeParserResult(parser, null);

        Assert.That(parser.Success, Is.True);
        Assert.That(
            parser.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Tag),
            Is.EqualTo(new string[] { "tag:first/thing", "tag:second/thing" }));
        Assert.That(composer.Stream.Documents.Count, Is.EqualTo(2));
        Assert.That(composer.Stream.Documents[0].Directives.Count, Is.EqualTo(2));
        Assert.That(composer.Stream.Documents[1].Directives.Count, Is.EqualTo(2));
        Assert.That(composer.Stream.Documents[0].Directives[0].Version, Is.EqualTo(global::yaml.YamlVersion.Version12));
        Assert.That(composer.Stream.Documents[1].Directives[0].Version, Is.EqualTo(global::yaml.YamlVersion.Version11));
    }

    public static global::yaml.YamlParserResult Parse(global::yaml.IYamlPipeline pipeline, string text, global::yaml.YamlParserOptions options)
    {
        global::yaml.YamlScannerResult scanner = pipeline.Scan(text, options);
        return pipeline.ParseScannerResult(scanner, options);
    }
}
