using System.Text;
using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlPipelineContractTests
{
    [Test]
    public void PipelineServices_ExposeConcreteImplementationAndHandleNullInputs()
    {
        global::yaml.IYamlPipeline shared = global::yaml.YamlApi.Pipeline;
        global::yaml.IYamlPipeline concrete = new global::yaml.YamlPipeline();

        global::yaml.YamlScannerResult scan = shared.Scan(null, null);
        List<global::yaml.YamlToken> tokens = concrete.ScanTokens(null, null);
        global::yaml.YamlParserResult parsedTokens = shared.ParseTokens(null, null);
        global::yaml.YamlParserResult parsedResult = concrete.ParseScannerResult(null, null);
        global::yaml.YamlComposerResult composedEvents = shared.ComposeEvents(null, null);
        global::yaml.YamlComposerResult composedResult = concrete.ComposeParserResult(null, null);
        shared.ResolveStream(null, null);
        concrete.ResolveDocument(null, null);
        shared.ResolveNode(null, null);
        global::yaml.YamlResult streamValidation = concrete.ValidateStream(null, null);
        global::yaml.YamlResult documentValidation = shared.ValidateDocument(null, null);
        global::yaml.YamlResult nodeValidation = concrete.ValidateNode(null, null);

        Assert.That(shared, Is.TypeOf<global::yaml.YamlPipeline>());
        Assert.That(concrete, Is.TypeOf<global::yaml.YamlPipeline>());
        Assert.That(scan.Success, Is.True);
        Assert.That(scan.Tokens.Select(token => token.Kind), Is.EqualTo(new global::yaml.YamlTokenKind[]
        {
            global::yaml.YamlTokenKind.StreamStart,
            global::yaml.YamlTokenKind.StreamEnd
        }));
        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new global::yaml.YamlTokenKind[]
        {
            global::yaml.YamlTokenKind.StreamStart,
            global::yaml.YamlTokenKind.StreamEnd
        }));
        Assert.That(parsedTokens.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(parsedResult.Success, Is.True);
        Assert.That(composedEvents.Success, Is.True);
        Assert.That(composedEvents.Stream.Documents, Is.Empty);
        Assert.That(composedResult.Success, Is.True);
        Assert.That(composedResult.Stream.Documents, Is.Empty);
        Assert.That(streamValidation.Diagnostics.Single().Code, Is.EqualTo("YAMLV001"));
        Assert.That(documentValidation.Diagnostics.Single().Code, Is.EqualTo("YAMLV001"));
        Assert.That(nodeValidation.Diagnostics.Single().Code, Is.EqualTo("YAMLV001"));
        Assert.That(shared.EmitStream(null, null), Is.Empty);
        Assert.That(concrete.EmitDocument(null, null), Is.Empty);
        Assert.That(shared.EmitNode(null, null), Is.Empty);
    }

    [Test]
    public void ScanOperations_ReturnOrderedTokensCommentsAndMalformedInputDiagnostics()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = true;

        global::yaml.YamlScannerResult result = pipeline.Scan("name: Ada # person\n", options);
        List<global::yaml.YamlToken> malformed = pipeline.ScanTokens("bad\u0001\n", options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Select(token => token.Kind), Does.Contain(global::yaml.YamlTokenKind.Comment));
        Assert.That(
            result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.PlainScalar).Select(token => token.Value),
            Is.EqualTo(new string[] { "name", "Ada" }));
        Assert.That(malformed.Select(token => token.Kind), Does.Contain(global::yaml.YamlTokenKind.Error));
        Assert.That(result.Tokens.First().Kind, Is.EqualTo(global::yaml.YamlTokenKind.StreamStart));
        Assert.That(result.Tokens.Last().Kind, Is.EqualTo(global::yaml.YamlTokenKind.StreamEnd));
    }

    [Test]
    public void ParseOperations_PreserveEventOrderingDirectivesDiagnosticsAndRecovery()
    {
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        string text = "%YAML 1.2\n%TAG !e! tag:example/\n---\n!e!item value\n";
        global::yaml.YamlScannerResult scanner = pipeline.Scan(text, options);

        global::yaml.YamlParserResult tokensResult = pipeline.ParseTokens(scanner.Tokens, options);
        global::yaml.YamlParserResult scannerResult = pipeline.ParseScannerResult(scanner, options);

        Assert.That(tokensResult.Success, Is.True);
        Assert.That(tokensResult.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(new global::yaml.YamlEventKind[]
        {
            global::yaml.YamlEventKind.StreamStart,
            global::yaml.YamlEventKind.YamlVersionDirective,
            global::yaml.YamlEventKind.TagDirective,
            global::yaml.YamlEventKind.DocumentStart,
            global::yaml.YamlEventKind.Scalar,
            global::yaml.YamlEventKind.DocumentEnd,
            global::yaml.YamlEventKind.StreamEnd
        }));
        Assert.That(tokensResult.Events[4].Tag, Is.EqualTo("tag:example/item"));
        Assert.That(scannerResult.Events.Select(yamlEvent => yamlEvent.Kind), Is.EqualTo(tokensResult.Events.Select(yamlEvent => yamlEvent.Kind)));

        global::yaml.YamlParserOptions recovery = new global::yaml.YamlParserOptions();
        recovery.Recover = true;
        global::yaml.YamlParserResult malformed = pipeline.ParseScannerResult(pipeline.Scan("[one, } , two]\n", recovery), recovery);

        Assert.That(malformed.Success, Is.False);
        Assert.That(malformed.Diagnostics, Is.Not.Empty);
        Assert.That(malformed.Events.First().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamStart));
        Assert.That(malformed.Events.Last().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamEnd));
        Assert.That(malformed.Events.Count(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Diagnostic), Is.GreaterThan(0));

        recovery.MaxDiagnostics = 0;
        global::yaml.YamlParserResult unlimited = pipeline.ParseScannerResult(pipeline.Scan("]}\n", recovery), recovery);

        Assert.That(unlimited.Diagnostics.Count, Is.EqualTo(2));
    }

    [Test]
    public void ParseTokens_ReportsMalformedPublicTokenEntries()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;
        List<global::yaml.YamlToken> tokens = new List<global::yaml.YamlToken>();
        tokens.Add(new global::yaml.YamlToken());
        tokens.Add(null!);

        global::yaml.YamlParserResult result = pipeline.ParseTokens(tokens, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLP014", "YAMLP014" }));
        Assert.That(result.Events.Count(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Diagnostic), Is.EqualTo(2));
        Assert.That(result.Events.First().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamStart));
        Assert.That(result.Events.Last().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamEnd));
    }

    [Test]
    public void ComposeOperations_BuildEquivalentGraphsAndPropagateDirectivesAndDiagnostics()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlParserOptions parserOptions = new global::yaml.YamlParserOptions();
        string text = "%YAML 1.2\n---\nname: Ada\n";
        global::yaml.YamlParserResult parser = pipeline.ParseScannerResult(pipeline.Scan(text, parserOptions), parserOptions);

        global::yaml.YamlComposerResult eventsResult = pipeline.ComposeEvents(parser.Events, null);
        global::yaml.YamlComposerResult parserResult = pipeline.ComposeParserResult(parser, null);

        Assert.That(eventsResult.Success, Is.True);
        Assert.That(parserResult.Success, Is.True);
        Assert.That(eventsResult.Stream.Documents.Single().Directives.Single().Name, Is.EqualTo("YAML"));
        Assert.That(parserResult.Stream.Documents.Single().Directives.Single().Version, Is.EqualTo(global::yaml.YamlVersion.Version12));
        Assert.That(eventsResult.Stream.Documents[0].Root!.Pairs[0].Key!.Value, Is.EqualTo("name"));
        Assert.That(eventsResult.Stream.Documents[0].Root!.Pairs[0].Value!.Value, Is.EqualTo("Ada"));

        global::yaml.YamlParserOptions recovery = new global::yaml.YamlParserOptions();
        recovery.Recover = true;
        global::yaml.YamlParserResult malformed = pipeline.ParseScannerResult(pipeline.Scan("]\n", recovery), recovery);
        global::yaml.YamlComposerResult diagnostics = pipeline.ComposeParserResult(malformed, null);

        Assert.That(diagnostics.Success, Is.False);
        Assert.That(diagnostics.Diagnostics, Is.Not.Empty);
        Assert.That(diagnostics.Stream.Diagnostics, Is.Not.Empty);
    }

    [Test]
    public void ComposeEvents_ReportsMalformedPublicEventEntries()
    {
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        List<global::yaml.YamlEvent> events = new List<global::yaml.YamlEvent>();
        events.Add(new global::yaml.YamlEvent());
        events.Add(null!);

        global::yaml.YamlComposerResult result = pipeline.ComposeEvents(events, null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLC010", "YAMLC010" }));
        Assert.That(result.Stream.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "YAMLC010", "YAMLC010" }));
    }

    [Test]
    public void ResolveOperations_ApplySchemaTagsAtNodeDocumentAndStreamScopes()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlNode node = Scalar("true");
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        document.Root = Scalar("37");
        global::yaml.YamlDocument streamDocument = new global::yaml.YamlDocument();
        streamDocument.Root = Scalar("null");
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        stream.Documents.Add(streamDocument);

        pipeline.ResolveNode(node, null);
        pipeline.ResolveDocument(document, null);
        pipeline.ResolveStream(stream, null);

        Assert.That(node.Tag, Is.EqualTo(global::yaml.YamlTags.Bool));
        Assert.That(document.Root!.Tag, Is.EqualTo(global::yaml.YamlTags.Int));
        Assert.That(stream.Documents[0].Root!.Tag, Is.EqualTo(global::yaml.YamlTags.Null));
    }

    [Test]
    public void ValidateOperations_ReportValidAndMalformedGraphContracts()
    {
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        global::yaml.YamlNode node = Scalar("value");
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        document.Root = node;
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        stream.Documents.Add(document);

        global::yaml.YamlResult nodeResult = pipeline.ValidateNode(node, null);
        global::yaml.YamlResult documentResult = pipeline.ValidateDocument(document, null);
        global::yaml.YamlResult streamResult = pipeline.ValidateStream(stream, null);

        Assert.That(nodeResult.Success, Is.True);
        Assert.That(documentResult.Success, Is.True);
        Assert.That(streamResult.Success, Is.True);
        Assert.That(nodeResult.Diagnostics, Is.Empty);
        Assert.That(documentResult.Diagnostics, Is.Empty);
        Assert.That(streamResult.Diagnostics, Is.Empty);

        global::yaml.YamlNode invalid = new global::yaml.YamlNode();
        invalid.Kind = global::yaml.YamlNodeKind.Unknown;
        global::yaml.YamlResult invalidResult = pipeline.ValidateNode(invalid, null);

        Assert.That(invalidResult.Success, Is.False);
        Assert.That(invalidResult.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("YAMLV001"));
    }

    [Test]
    public void EmitOperations_ProduceNodeDocumentAndStreamFormsWithConfiguredMarkers()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlNode node = new global::yaml.YamlNode();
        node.Kind = global::yaml.YamlNodeKind.Sequence;
        node.CollectionStyle = global::yaml.YamlCollectionStyle.Flow;
        node.Children.Add(Scalar("one"));
        node.Children.Add(Scalar("two"));
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        document.Root = node;
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        stream.Documents.Add(document);
        stream.Documents.Add(document);
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.WriteDocumentEnd = true;

        string nodeText = pipeline.EmitNode(node, options);
        string documentText = pipeline.EmitDocument(document, options);
        string streamText = pipeline.EmitStream(stream, options);

        Assert.That(nodeText, Is.EqualTo("[one, two]"));
        Assert.That(documentText, Is.EqualTo("[one, two]\n...\n"));
        Assert.That(streamText, Is.EqualTo("---\n[one, two]\n...\n---\n[one, two]\n...\n"));
    }

    [Test]
    public void ByteOperations_DecodeBomInputsAndReportEncodingAndSecurityDiagnostics()
    {
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        byte[] utf16 = Combine(Encoding.Unicode.GetPreamble(), Encoding.Unicode.GetBytes("name: Ada\n"));

        global::yaml.YamlScannerResult scan = pipeline.ScanBytes(utf16, null, null);
        global::yaml.YamlParserResult parse = pipeline.ParseBytes(utf16, null, null);

        Assert.That(scan.Success, Is.True);
        Assert.That(scan.Tokens.Any(token => token.Kind == global::yaml.YamlTokenKind.PlainScalar && token.Value == "Ada"), Is.True);
        Assert.That(parse.Success, Is.True);
        Assert.That(parse.Events.Where(yamlEvent => yamlEvent.Kind == global::yaml.YamlEventKind.Scalar).Select(yamlEvent => yamlEvent.Value), Is.EqualTo(new string[] { "name", "Ada" }));

        global::yaml.YamlScannerResult encodingFailure = pipeline.ScanBytes(new byte[] { 192, 175 }, null, null);
        global::yaml.YamlSecurityOptions security = new global::yaml.YamlSecurityOptions();
        security.MaxInputLength = 1;
        global::yaml.YamlParserResult securityFailure = pipeline.ParseBytes(Encoding.UTF8.GetBytes("ab"), null, security);

        Assert.That(encodingFailure.Success, Is.False);
        Assert.That(encodingFailure.Diagnostics.Single().Code, Is.EqualTo("YAMLE001"));
        Assert.That(securityFailure.Success, Is.False);
        Assert.That(securityFailure.Diagnostics.Single().Code, Is.EqualTo("YAMLS001"));
    }

    public static global::yaml.YamlNode Scalar(string value)
    {
        global::yaml.YamlNode node = new global::yaml.YamlNode();
        node.Kind = global::yaml.YamlNodeKind.Scalar;
        node.Value = value;
        return node;
    }

    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
