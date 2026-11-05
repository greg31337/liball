using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlPublicDataContractTests
{
    [Test]
    public void MetadataConstructors_InitializeStableCallerMutableDefaults()
    {
        global::yaml.YamlSourceSpan span = new global::yaml.YamlSourceSpan();
        global::yaml.YamlDiagnostic diagnostic = new global::yaml.YamlDiagnostic();
        global::yaml.YamlToken token = new global::yaml.YamlToken();
        global::yaml.YamlEvent yamlEvent = new global::yaml.YamlEvent();
        global::yaml.YamlDirective directive = new global::yaml.YamlDirective();
        global::yaml.YamlTagDirective tagDirective = new global::yaml.YamlTagDirective();
        global::yaml.YamlTrivia trivia = new global::yaml.YamlTrivia();
        global::yaml.YamlPathSegment segment = new global::yaml.YamlPathSegment();
        global::yaml.YamlPath path = new global::yaml.YamlPath();

        Assert.Multiple(() =>
        {
            Assert.That(new int[]
            {
                span.StartIndex,
                span.StartLine,
                span.StartColumn,
                span.EndIndex,
                span.EndLine,
                span.EndColumn
            }, Is.All.Zero);
            Assert.That(diagnostic.Severity, Is.EqualTo(global::yaml.YamlDiagnosticSeverity.Error));
            Assert.That(diagnostic.Code, Is.Empty);
            Assert.That(diagnostic.Message, Is.Empty);
            Assert.That(diagnostic.Span, Is.Not.Null);
            Assert.That(token.Kind, Is.EqualTo(global::yaml.YamlTokenKind.Unknown));
            Assert.That(token.Value, Is.Empty);
            Assert.That(token.Header, Is.Empty);
            Assert.That(token.Indent, Is.Zero);
            Assert.That(token.Span, Is.Not.Null);
            Assert.That(yamlEvent.Kind, Is.EqualTo(global::yaml.YamlEventKind.Unknown));
            Assert.That(yamlEvent.Value, Is.Empty);
            Assert.That(yamlEvent.Tag, Is.Empty);
            Assert.That(yamlEvent.Anchor, Is.Empty);
            Assert.That(yamlEvent.AliasName, Is.Empty);
            Assert.That(yamlEvent.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.Any));
            Assert.That(yamlEvent.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Any));
            Assert.That(yamlEvent.Diagnostic, Is.Not.Null);
            Assert.That(yamlEvent.Directive, Is.Not.Null);
            Assert.That(yamlEvent.Trivia, Is.Not.Null);
            Assert.That(directive.Name, Is.Empty);
            Assert.That(directive.Value, Is.Empty);
            Assert.That(directive.Version, Is.EqualTo(global::yaml.YamlVersion.Unknown));
            Assert.That(directive.Handle, Is.Empty);
            Assert.That(directive.Prefix, Is.Empty);
            Assert.That(tagDirective.Handle, Is.Empty);
            Assert.That(tagDirective.Prefix, Is.Empty);
            Assert.That(trivia.Kind, Is.EqualTo(global::yaml.YamlTriviaKind.Unknown));
            Assert.That(trivia.Placement, Is.EqualTo(global::yaml.YamlTriviaPlacement.Unknown));
            Assert.That(trivia.Text, Is.Empty);
            Assert.That(segment.Kind, Is.EqualTo(global::yaml.YamlPathSegmentKind.Unknown));
            Assert.That(segment.Key, Is.Empty);
            Assert.That(segment.Index, Is.EqualTo(-1));
            Assert.That(path.Segments, Is.Empty);
            Assert.That(path.Source, Is.Empty);
            Assert.That(path.Valid, Is.True);
            Assert.That(path.Error, Is.Empty);
        });
    }

    [Test]
    public void GraphAndResultConstructors_CreateIndependentEmptyCollections()
    {
        global::yaml.YamlAnchorTable anchors = new global::yaml.YamlAnchorTable();
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        global::yaml.YamlDocument secondDocument = new global::yaml.YamlDocument();
        global::yaml.YamlNode node = new global::yaml.YamlNode();
        global::yaml.YamlPair pair = new global::yaml.YamlPair();
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        global::yaml.YamlScannerResult scanner = new global::yaml.YamlScannerResult();
        global::yaml.YamlParserResult parser = new global::yaml.YamlParserResult();
        global::yaml.YamlComposerResult composer = new global::yaml.YamlComposerResult();
        global::yaml.YamlResult result = new global::yaml.YamlResult();
        global::yaml.YamlParseResult parseResult = new global::yaml.YamlParseResult();

        document.LeadingComments.Add("caller");
        node.Children.Add(new global::yaml.YamlNode());
        stream.Documents.Add(document);
        scanner.Tokens.Add(new global::yaml.YamlToken());
        parser.Events.Add(new global::yaml.YamlEvent());
        composer.Stream.Documents.Add(document);
        result.Diagnostics.Add(new global::yaml.YamlDiagnostic());
        parseResult.Diagnostics.Add(new global::yaml.YamlDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(anchors.Names, Is.Empty);
            Assert.That(anchors.Nodes, Is.Empty);
            Assert.That(anchors.Spans, Is.Empty);
            Assert.That(document.Directives, Is.Empty);
            Assert.That(document.Diagnostics, Is.Empty);
            Assert.That(document.LeadingBlankLines, Is.Empty);
            Assert.That(document.Trivia, Is.Empty);
            Assert.That(document.Anchors, Is.Not.Null);
            Assert.That(document.Root, Is.Null);
            Assert.That(secondDocument.LeadingComments, Is.Empty);
            Assert.That(document.LeadingComments, Is.EqualTo(new string[] { "caller" }));
            Assert.That(node.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
            Assert.That(node.AliasTarget, Is.Null);
            Assert.That(node.Children.Count, Is.EqualTo(1));
            Assert.That(node.Pairs, Is.Empty);
            Assert.That(node.LeadingComments, Is.Empty);
            Assert.That(node.TrailingComments, Is.Empty);
            Assert.That(node.LeadingBlankLines, Is.Empty);
            Assert.That(node.TrailingBlankLines, Is.Empty);
            Assert.That(node.Trivia, Is.Empty);
            Assert.That(pair.Key, Is.Null);
            Assert.That(pair.Value, Is.Null);
            Assert.That(pair.ComplexKey, Is.False);
            Assert.That(pair.LeadingComments, Is.Empty);
            Assert.That(pair.TrailingComments, Is.Empty);
            Assert.That(pair.LeadingBlankLines, Is.Empty);
            Assert.That(pair.TrailingBlankLines, Is.Empty);
            Assert.That(pair.Trivia, Is.Empty);
            Assert.That(stream.Documents, Is.EqualTo(new global::yaml.YamlDocument[] { document }));
            Assert.That(stream.Diagnostics, Is.Empty);
            Assert.That(scanner.Success, Is.True);
            Assert.That(scanner.Tokens.Count, Is.EqualTo(1));
            Assert.That(scanner.Diagnostics, Is.Empty);
            Assert.That(parser.Success, Is.True);
            Assert.That(parser.Events.Count, Is.EqualTo(1));
            Assert.That(parser.Diagnostics, Is.Empty);
            Assert.That(composer.Success, Is.True);
            Assert.That(composer.Stream.Documents.Count, Is.EqualTo(1));
            Assert.That(composer.Diagnostics, Is.Empty);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(parseResult.Success, Is.True);
            Assert.That(parseResult.Value, Is.Null);
            Assert.That(parseResult.Diagnostics.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void RegistryConstructors_ExposeEmptyMutableRegistrationLists()
    {
        global::yaml.YamlTagRegistration tag = new global::yaml.YamlTagRegistration();
        global::yaml.YamlScalarConverterRegistration scalar = new global::yaml.YamlScalarConverterRegistration();
        global::yaml.YamlTypeConverterRegistration type = new global::yaml.YamlTypeConverterRegistration();
        global::yaml.YamlTagRegistry tags = new global::yaml.YamlTagRegistry();
        global::yaml.YamlScalarConverterRegistry scalars = new global::yaml.YamlScalarConverterRegistry();
        global::yaml.YamlTypeConverterRegistry types = new global::yaml.YamlTypeConverterRegistry();

        tags.Registrations.Add(tag);
        scalars.Registrations.Add(scalar);
        types.Registrations.Add(type);

        Assert.Multiple(() =>
        {
            Assert.That(tag.Tag, Is.Empty);
            Assert.That(tag.NodeKind, Is.EqualTo(global::yaml.YamlNodeKind.Unknown));
            Assert.That(tag.ClrType, Is.Null);
            Assert.That(tag.ScalarMatcher, Is.Null);
            Assert.That(tag.ScalarResolver, Is.Null);
            Assert.That(scalar.Tag, Is.Empty);
            Assert.That(scalar.ClrType, Is.Null);
            Assert.That(scalar.PreferredStyle, Is.EqualTo(global::yaml.YamlScalarStyle.Any));
            Assert.That(scalar.ReadScalar, Is.Null);
            Assert.That(scalar.WriteScalar, Is.Null);
            Assert.That(scalar.Converter, Is.Null);
            Assert.That(type.ClrType, Is.Null);
            Assert.That(type.ReadValue, Is.Null);
            Assert.That(type.WriteValue, Is.Null);
            Assert.That(type.Converter, Is.Null);
            Assert.That(tags.Registrations, Is.EqualTo(new global::yaml.YamlTagRegistration[] { tag }));
            Assert.That(scalars.Registrations, Is.EqualTo(new global::yaml.YamlScalarConverterRegistration[] { scalar }));
            Assert.That(types.Registrations, Is.EqualTo(new global::yaml.YamlTypeConverterRegistration[] { type }));
        });
    }

    [Test]
    public void ProcessorConfiguration_CopiesEveryOptionFieldAndRegistrationIndependently()
    {
        global::yaml.YamlOptions supplied = PopulatedOptions();
        global::yaml.YamlTagRegistration originalTag = supplied.Schema.CustomTags.Registrations[0];
        global::yaml.YamlScalarConverterRegistration originalScalar = supplied.Object.ScalarConverters.Registrations[0];
        global::yaml.YamlTypeConverterRegistration originalType = supplied.Object.TypeConverters.Registrations[0];
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(supplied);

        supplied.Parser.Version = global::yaml.YamlVersion.Version12;
        supplied.Parser.Recover = false;
        supplied.Composer.DuplicateKeyPolicy = global::yaml.YamlDuplicateKeyPolicy.Error;
        supplied.Schema.Schema = global::yaml.YamlSchemaKind.Core;
        supplied.Emitter.Indent = 2;
        supplied.Object.RequiredMembers.Clear();
        supplied.Security.MaxDepth = 100;
        originalTag.Tag = "!mutated";
        originalScalar.Tag = "!mutated";
        originalType.ClrType = typeof(int);

        global::yaml.YamlOptions copy = processor.GetOptions();

        AssertPopulatedOptions(copy);
        Assert.That(copy.Schema.CustomTags, Is.Not.SameAs(supplied.Schema.CustomTags));
        Assert.That(copy.Schema.CustomTags.Registrations[0], Is.Not.SameAs(originalTag));
        Assert.That(copy.Object.ScalarConverters, Is.Not.SameAs(supplied.Object.ScalarConverters));
        Assert.That(copy.Object.ScalarConverters.Registrations[0], Is.Not.SameAs(originalScalar));
        Assert.That(copy.Object.TypeConverters, Is.Not.SameAs(supplied.Object.TypeConverters));
        Assert.That(copy.Object.TypeConverters.Registrations[0], Is.Not.SameAs(originalType));

        copy.Parser.MaxDiagnostics = 1;
        copy.Schema.CustomTags.Registrations.Clear();
        copy.Object.RequiredMembers.Clear();
        copy.Security.MaxDepth = 1;

        global::yaml.YamlOptions secondCopy = processor.GetOptions();
        AssertPopulatedOptions(secondCopy);
        Assert.That(secondCopy, Is.Not.SameAs(copy));
        Assert.That(secondCopy.Parser, Is.Not.SameAs(copy.Parser));
        Assert.That(secondCopy.Schema, Is.Not.SameAs(copy.Schema));
        Assert.That(secondCopy.Object, Is.Not.SameAs(copy.Object));
    }

    [Test]
    public void PipelineOperations_PopulateEveryPublicResultShapeAndDiagnostics()
    {
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        global::yaml.YamlScannerResult scanner = pipeline.Scan("name: Ada\n", null);
        global::yaml.YamlParserResult parser = pipeline.ParseScannerResult(scanner, null);
        global::yaml.YamlComposerResult composer = pipeline.ComposeParserResult(parser, null);
        global::yaml.YamlResult validation = pipeline.ValidateStream(composer.Stream, null);
        global::yaml.YamlParseResult highLevel = new global::yaml.YamlParseResult();
        bool parsed = global::yaml.YamlApi.Processors.CreateProcessor(null).TryParse("name: Ada\n", highLevel);

        Assert.Multiple(() =>
        {
            Assert.That(scanner.Success, Is.True);
            Assert.That(scanner.Tokens, Is.Not.Empty);
            Assert.That(scanner.Diagnostics, Is.Empty);
            Assert.That(parser.Success, Is.True);
            Assert.That(parser.Events, Is.Not.Empty);
            Assert.That(parser.Diagnostics, Is.Empty);
            Assert.That(composer.Success, Is.True);
            Assert.That(composer.Stream.Documents.Count, Is.EqualTo(1));
            Assert.That(composer.Diagnostics, Is.Empty);
            Assert.That(validation.Success, Is.True);
            Assert.That(validation.Diagnostics, Is.Empty);
            Assert.That(parsed, Is.True);
            Assert.That(highLevel.Success, Is.True);
            Assert.That(highLevel.Value, Is.TypeOf<global::yaml.YamlDocument>());
            Assert.That(highLevel.Diagnostics, Is.Empty);
        });

        global::yaml.YamlScannerResult badScanner = pipeline.Scan("\u0001\n", null);
        global::yaml.YamlParserResult badParser = pipeline.ParseScannerResult(badScanner, null);
        global::yaml.YamlComposerResult badComposer = pipeline.ComposeParserResult(badParser, null);
        global::yaml.YamlNode invalid = new global::yaml.YamlNode();
        invalid.Kind = global::yaml.YamlNodeKind.Unknown;
        global::yaml.YamlResult badValidation = pipeline.ValidateNode(invalid, null);
        bool badParse = global::yaml.YamlApi.Processors.CreateProcessor(null).TryParse("\u0001\n", highLevel);

        Assert.That(badScanner.Success, Is.False);
        Assert.That(badScanner.Diagnostics, Is.Not.Empty);
        Assert.That(badParser.Success, Is.False);
        Assert.That(badParser.Diagnostics, Is.Not.Empty);
        Assert.That(badComposer.Success, Is.False);
        Assert.That(badComposer.Diagnostics, Is.Not.Empty);
        Assert.That(badComposer.Stream.Diagnostics, Is.Not.Empty);
        Assert.That(badValidation.Success, Is.False);
        Assert.That(badValidation.Diagnostics, Is.Not.Empty);
        Assert.That(badParse, Is.False);
        Assert.That(highLevel.Success, Is.False);
        Assert.That(highLevel.Value, Is.Null);
        Assert.That(highLevel.Diagnostics, Is.Not.Empty);
    }

    [Test]
    public void ParsedSourceMetadata_PopulatesSpansDirectivesDocumentsNodesPairsAndAnchors()
    {
        string source =
            "%YAML 1.2\n" +
            "%TAG !e! tag:example/\n" +
            "---\n" +
            "name: &person !e!kind Ada\n" +
            "alias: *person\n";
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlScannerResult scanner = pipeline.Scan(source, null);
        global::yaml.YamlParserResult parser = pipeline.ParseScannerResult(scanner, null);
        global::yaml.YamlComposerResult composer = pipeline.ComposeParserResult(parser, null);
        global::yaml.YamlDocument document = composer.Stream.Documents.Single();
        global::yaml.YamlPair namePair = document.Root!.Pairs[0];
        global::yaml.YamlPair aliasPair = document.Root.Pairs[1];
        global::yaml.YamlNode person = namePair.Value!;
        global::yaml.YamlNode alias = aliasPair.Value!;
        global::yaml.YamlToken adaToken = scanner.Tokens.Single(item => item.Kind == global::yaml.YamlTokenKind.PlainScalar && item.Value == "Ada");
        global::yaml.YamlEvent adaEvent = parser.Events.Single(item => item.Kind == global::yaml.YamlEventKind.Scalar && item.Value == "Ada");

        Assert.Multiple(() =>
        {
            Assert.That(Slice(source, adaToken.Span), Is.EqualTo("Ada"));
            Assert.That(Slice(source, adaEvent.Span), Is.EqualTo("Ada"));
            Assert.That(parser.Events.Count(item => item.Kind == global::yaml.YamlEventKind.YamlVersionDirective), Is.EqualTo(1));
            Assert.That(parser.Events.Count(item => item.Kind == global::yaml.YamlEventKind.TagDirective), Is.EqualTo(1));
            Assert.That(document.Directives.Count, Is.EqualTo(2));
            Assert.That(document.Directives[0].Version, Is.EqualTo(global::yaml.YamlVersion.Version12));
            Assert.That(document.Directives[1].Handle, Is.EqualTo("!e!"));
            Assert.That(document.Directives[1].Prefix, Is.EqualTo("tag:example/"));
            Assert.That(document.Span.EndIndex, Is.GreaterThan(document.Span.StartIndex));
            Assert.That(document.Root.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
            Assert.That(document.Root.Span.EndIndex, Is.GreaterThan(document.Root.Span.StartIndex));
            Assert.That(namePair.Span.EndIndex, Is.GreaterThan(namePair.Span.StartIndex));
            Assert.That(aliasPair.Span.EndIndex, Is.GreaterThan(aliasPair.Span.StartIndex));
            Assert.That(person.Value, Is.EqualTo("Ada"));
            Assert.That(person.Tag, Is.EqualTo("tag:example/kind"));
            Assert.That(person.Anchor, Is.EqualTo("person"));
            Assert.That(alias.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias));
            Assert.That(alias.AliasName, Is.EqualTo("person"));
            Assert.That(alias.AliasTarget, Is.SameAs(person));
            Assert.That(document.Anchors.Names, Is.EqualTo(new string[] { "person" }));
            Assert.That(document.Anchors.Nodes, Is.EqualTo(new global::yaml.YamlNode[] { person }));
            Assert.That(document.Anchors.Spans.Single().EndIndex, Is.GreaterThan(document.Anchors.Spans.Single().StartIndex));
        });
    }

    [Test]
    public void CallerMutatedGraphData_IsConsumedByThePublicEmitter()
    {
        global::yaml.YamlSourceSpan span = new global::yaml.YamlSourceSpan();
        span.StartIndex = 4;
        span.StartLine = 2;
        span.StartColumn = 1;
        span.EndIndex = 13;
        span.EndLine = 2;
        span.EndColumn = 10;
        global::yaml.YamlNode key = new global::yaml.YamlNode();
        key.Kind = global::yaml.YamlNodeKind.Scalar;
        key.Value = "name";
        global::yaml.YamlNode value = new global::yaml.YamlNode();
        value.Kind = global::yaml.YamlNodeKind.Scalar;
        value.Value = "Ada";
        value.Anchor = "person";
        global::yaml.YamlPair pair = new global::yaml.YamlPair();
        pair.Key = key;
        pair.Value = value;
        pair.Span = span;
        global::yaml.YamlNode mapping = new global::yaml.YamlNode();
        mapping.Kind = global::yaml.YamlNodeKind.Mapping;
        mapping.Pairs.Add(pair);
        global::yaml.YamlTrivia comment = new global::yaml.YamlTrivia();
        comment.Kind = global::yaml.YamlTriviaKind.Comment;
        comment.Placement = global::yaml.YamlTriviaPlacement.Before;
        comment.Text = "# caller";
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        document.Root = mapping;
        document.Trivia.Add(comment);
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.WriteDocumentStart = true;

        string emitted = global::yaml.YamlApi.Pipeline.EmitDocument(document, options);
        global::yaml.YamlDocument reparsed = global::yaml.YamlApi.Processors.CreateProcessor(null).Parse(emitted);

        Assert.That(emitted, Is.EqualTo("# caller\n---\nname: &person Ada\n"));
        Assert.That(global::yaml.YamlApi.Model.FindMappingValue(reparsed.Root, "name")!.Value, Is.EqualTo("Ada"));
        Assert.That(pair.Span, Is.SameAs(span));
    }

    [Test]
    public void StandardTags_ExposeEveryExpandedTagAndNormalizeEveryShorthand()
    {
        string[] expanded = new string[]
        {
            global::yaml.YamlTags.String,
            global::yaml.YamlTags.Null,
            global::yaml.YamlTags.Bool,
            global::yaml.YamlTags.Int,
            global::yaml.YamlTags.Float,
            global::yaml.YamlTags.Sequence,
            global::yaml.YamlTags.Mapping,
            global::yaml.YamlTags.Timestamp,
            global::yaml.YamlTags.Binary,
            global::yaml.YamlTags.Set,
            global::yaml.YamlTags.OrderedMap,
            global::yaml.YamlTags.Pairs,
            global::yaml.YamlTags.Merge
        };
        string[] shorthand = new string[]
        {
            "!!str",
            "!!null",
            "!!bool",
            "!!int",
            "!!float",
            "!!seq",
            "!!map",
            "!!timestamp",
            "!!binary",
            "!!set",
            "!!omap",
            "!!pairs",
            "!!merge"
        };
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;

        Assert.That(expanded, Is.All.StartWith("tag:yaml.org,2002:"));
        Assert.That(expanded.Distinct().Count(), Is.EqualTo(expanded.Length));
        for (int index = 0; index < expanded.Length; index = index + 1)
        {
            Assert.That(model.IsStandardTag(expanded[index]), Is.True);
            Assert.That(model.NormalizeStandardTag(shorthand[index]), Is.EqualTo(expanded[index]));
        }
    }

    [Test]
    public void YamlException_PreservesMessageCauseAndPublicProcessingContext()
    {
        InvalidOperationException cause = new InvalidOperationException("root cause");
        global::yaml.YamlException direct = new global::yaml.YamlException("wrapper", cause);
        global::yaml.YamlException? processing = Assert.Throws<global::yaml.YamlException>(
            () => global::yaml.YamlApi.Processors.CreateProcessor(null).Parse("\u0001\n"));

        Assert.That(direct.Message, Is.EqualTo("wrapper"));
        Assert.That(direct.InnerException, Is.SameAs(cause));
        Assert.That(direct, Is.InstanceOf<Exception>());
        Assert.That(processing!.Message, Does.Contain("YAML"));
        Assert.That(processing.Message, Does.Contain("YAMLP002"));
        Assert.That(processing.Message, Does.Contain("YAML013"));
    }

    public static global::yaml.YamlOptions PopulatedOptions()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Parser.Version = global::yaml.YamlVersion.Version11;
        options.Parser.Recover = true;
        options.Parser.IncludeComments = false;
        options.Parser.AllowDirectives = false;
        options.Parser.MaxDiagnostics = 7;
        options.Composer.DuplicateKeyPolicy = global::yaml.YamlDuplicateKeyPolicy.Warn;
        options.Composer.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Error;
        options.Composer.ResolveAliases = false;
        options.Composer.AllowRecursiveAliases = false;
        options.Composer.AllowMergeKeys = true;
        options.Composer.PreserveComments = false;
        options.Schema.Schema = global::yaml.YamlSchemaKind.Yaml11;
        options.Schema.ResolveScalars = false;
        options.Schema.AllowYaml11Booleans = true;
        options.Schema.AllowTimestamp = true;
        options.Schema.AllowBinary = true;
        options.Schema.AllowMergeKeys = true;
        options.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Warn;
        global::yaml.YamlTagRegistration tag = new global::yaml.YamlTagRegistration();
        tag.Tag = "!copy";
        tag.NodeKind = global::yaml.YamlNodeKind.Mapping;
        tag.ClrType = typeof(YamlPublicDataContractTests);
        options.Schema.CustomTags.Registrations.Add(tag);
        options.Emitter.Indent = 4;
        options.Emitter.LineWidth = 20;
        options.Emitter.WriteDocumentStart = true;
        options.Emitter.WriteDocumentEnd = true;
        options.Emitter.SortStringKeys = true;
        options.Emitter.EmitComments = false;
        options.Emitter.DefaultScalarStyle = global::yaml.YamlScalarStyle.DoubleQuoted;
        options.Emitter.DefaultCollectionStyle = global::yaml.YamlCollectionStyle.Flow;
        options.Emitter.InvalidValuePolicy = global::yaml.YamlInvalidValuePolicy.Error;
        options.Object.NamingConvention = global::yaml.YamlNamingConvention.KebabCase;
        options.Object.IncludePublicFields = false;
        options.Object.IgnoreNulls = true;
        options.Object.IgnoreDefaults = true;
        options.Object.ErrorOnUnknownMembers = false;
        options.Object.RequireConfiguredMembers = true;
        options.Object.UseTypeTags = true;
        options.Object.RequiredMembers.Add("Name");
        global::yaml.YamlScalarConverterRegistration scalar = new global::yaml.YamlScalarConverterRegistration();
        scalar.Tag = "!scalar";
        scalar.ClrType = typeof(string);
        scalar.PreferredStyle = global::yaml.YamlScalarStyle.SingleQuoted;
        scalar.ReadScalar = value => value;
        scalar.WriteScalar = value => value.ToString() ?? "";
        options.Object.ScalarConverters.Registrations.Add(scalar);
        global::yaml.YamlTypeConverterRegistration type = new global::yaml.YamlTypeConverterRegistration();
        type.ClrType = typeof(DateTime);
        type.ReadValue = value => value;
        type.WriteValue = value => value;
        options.Object.TypeConverters.Registrations.Add(type);
        options.Security.EnforceLimits = false;
        options.Security.MaxInputLength = 11;
        options.Security.MaxScalarLength = 12;
        options.Security.MaxDepth = 13;
        options.Security.MaxAliasCount = 14;
        options.Security.MaxCollectionSize = 15;
        options.Security.MaxDiagnostics = 16;
        return options;
    }

    public static void AssertPopulatedOptions(global::yaml.YamlOptions options)
    {
        Assert.Multiple(() =>
        {
            Assert.That(options.Parser.Version, Is.EqualTo(global::yaml.YamlVersion.Version11));
            Assert.That(options.Parser.Recover, Is.True);
            Assert.That(options.Parser.IncludeComments, Is.False);
            Assert.That(options.Parser.AllowDirectives, Is.False);
            Assert.That(options.Parser.MaxDiagnostics, Is.EqualTo(7));
            Assert.That(options.Composer.DuplicateKeyPolicy, Is.EqualTo(global::yaml.YamlDuplicateKeyPolicy.Warn));
            Assert.That(options.Composer.UnknownTagPolicy, Is.EqualTo(global::yaml.YamlUnknownTagPolicy.Error));
            Assert.That(options.Composer.ResolveAliases, Is.False);
            Assert.That(options.Composer.AllowRecursiveAliases, Is.False);
            Assert.That(options.Composer.AllowMergeKeys, Is.True);
            Assert.That(options.Composer.PreserveComments, Is.False);
            Assert.That(options.Schema.Schema, Is.EqualTo(global::yaml.YamlSchemaKind.Yaml11));
            Assert.That(options.Schema.ResolveScalars, Is.False);
            Assert.That(options.Schema.AllowYaml11Booleans, Is.True);
            Assert.That(options.Schema.AllowTimestamp, Is.True);
            Assert.That(options.Schema.AllowBinary, Is.True);
            Assert.That(options.Schema.AllowMergeKeys, Is.True);
            Assert.That(options.Schema.UnknownTagPolicy, Is.EqualTo(global::yaml.YamlUnknownTagPolicy.Warn));
            Assert.That(options.Schema.CustomTags.Registrations.Single().Tag, Is.EqualTo("!copy"));
            Assert.That(options.Schema.CustomTags.Registrations[0].NodeKind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
            Assert.That(options.Schema.CustomTags.Registrations[0].ClrType, Is.EqualTo(typeof(YamlPublicDataContractTests)));
            Assert.That(options.Emitter.Indent, Is.EqualTo(4));
            Assert.That(options.Emitter.LineWidth, Is.EqualTo(20));
            Assert.That(options.Emitter.WriteDocumentStart, Is.True);
            Assert.That(options.Emitter.WriteDocumentEnd, Is.True);
            Assert.That(options.Emitter.SortStringKeys, Is.True);
            Assert.That(options.Emitter.EmitComments, Is.False);
            Assert.That(options.Emitter.DefaultScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.DoubleQuoted));
            Assert.That(options.Emitter.DefaultCollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
            Assert.That(options.Emitter.InvalidValuePolicy, Is.EqualTo(global::yaml.YamlInvalidValuePolicy.Error));
            Assert.That(options.Object.NamingConvention, Is.EqualTo(global::yaml.YamlNamingConvention.KebabCase));
            Assert.That(options.Object.IncludePublicFields, Is.False);
            Assert.That(options.Object.IgnoreNulls, Is.True);
            Assert.That(options.Object.IgnoreDefaults, Is.True);
            Assert.That(options.Object.ErrorOnUnknownMembers, Is.False);
            Assert.That(options.Object.RequireConfiguredMembers, Is.True);
            Assert.That(options.Object.UseTypeTags, Is.True);
            Assert.That(options.Object.RequiredMembers, Is.EqualTo(new string[] { "Name" }));
            Assert.That(options.Object.ScalarConverters.Registrations.Single().Tag, Is.EqualTo("!scalar"));
            Assert.That(options.Object.ScalarConverters.Registrations[0].ClrType, Is.EqualTo(typeof(string)));
            Assert.That(options.Object.ScalarConverters.Registrations[0].PreferredStyle, Is.EqualTo(global::yaml.YamlScalarStyle.SingleQuoted));
            Assert.That(options.Object.TypeConverters.Registrations.Single().ClrType, Is.EqualTo(typeof(DateTime)));
            Assert.That(options.Security.EnforceLimits, Is.False);
            Assert.That(options.Security.MaxInputLength, Is.EqualTo(11));
            Assert.That(options.Security.MaxScalarLength, Is.EqualTo(12));
            Assert.That(options.Security.MaxDepth, Is.EqualTo(13));
            Assert.That(options.Security.MaxAliasCount, Is.EqualTo(14));
            Assert.That(options.Security.MaxCollectionSize, Is.EqualTo(15));
            Assert.That(options.Security.MaxDiagnostics, Is.EqualTo(16));
        });
    }

    public static string Slice(string source, global::yaml.YamlSourceSpan span)
    {
        return source.Substring(span.StartIndex, span.EndIndex - span.StartIndex);
    }
}
