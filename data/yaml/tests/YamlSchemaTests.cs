using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlSchemaTests
{
    [Test]
    public void ResolveScalarTag_FailsafeSchema_ReturnsStringTagForEveryScalarText()
    {
        global::yaml.YamlSchemaOptions options = FailsafeOptions();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("null", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("true", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("123", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void Resolve_FailsafeSchema_TagsScalarNodesInSequencesAndMappings()
    {
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        global::yaml.YamlNode root = global::yaml.YamlNode.Mapping(global::yaml.YamlCollectionStyle.Block, global::yaml.YamlSourceSpan.Empty());
        global::yaml.YamlNode sequence = global::yaml.YamlNode.Sequence(global::yaml.YamlCollectionStyle.Block, global::yaml.YamlSourceSpan.Empty());
        sequence.AddChild(global::yaml.YamlNode.Scalar("true", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty()));
        root.AddPair(global::yaml.YamlPair.Create(
            global::yaml.YamlNode.Scalar("items", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty()),
            sequence,
            global::yaml.YamlSourceSpan.Empty()));
        document.SetRoot(root);
        stream.AddDocument(document);

        global::yaml.YamlSchemaResolver.Resolve(stream, FailsafeOptions());

        Assert.That(root.Pairs[0].Key!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(root.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.MappingTag));
        Assert.That(sequence.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.SequenceTag));
        Assert.That(sequence.Children[0].Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void Resolve_FailsafeSchema_PreservesExplicitScalarTags()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNode.Scalar("value", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());
        scalar.Tag = "!custom";

        global::yaml.YamlSchemaResolver.ResolveNode(scalar, FailsafeOptions());

        Assert.That(scalar.Tag, Is.EqualTo("!custom"));
    }

    [Test]
    public void Resolve_ResolveScalarsFalse_LeavesScalarTagsEmpty()
    {
        global::yaml.YamlSchemaOptions options = FailsafeOptions();
        options.ResolveScalars = false;
        global::yaml.YamlNode scalar = global::yaml.YamlNode.Scalar("value", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());

        global::yaml.YamlSchemaResolver.ResolveNode(scalar, options);

        Assert.That(scalar.Tag, Is.EqualTo(""));
    }

    [Test]
    public void Resolve_ComposedParserResultWithFailsafeSchema_TagsKeysAndValuesAsStrings()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("truth: true\ncount: 123\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlSchemaResolver.Resolve(composer.Stream, FailsafeOptions());

        global::yaml.YamlNode root = composer.Stream.Documents[0].Root!;

        Assert.That(root.Pairs.Select(pair => pair.Key!.Tag), Is.EqualTo(new string[]
        {
            global::yaml.YamlScalarResolver.FailsafeStringTag,
            global::yaml.YamlScalarResolver.FailsafeStringTag
        }));
        Assert.That(root.Pairs.Select(pair => pair.Value!.Tag), Is.EqualTo(new string[]
        {
            global::yaml.YamlScalarResolver.FailsafeStringTag,
            global::yaml.YamlScalarResolver.FailsafeStringTag
        }));
    }

    [Test]
    public void ResolveScalarTag_JsonSchema_RecognizesNullBoolIntFloatAndStringScalars()
    {
        global::yaml.YamlSchemaOptions options = JsonOptions();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.NullTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("null", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.NullTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("true", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("false", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("-12", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("1.25", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("hello", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void ResolveScalarTag_JsonSchema_UsesJsonNumberGrammar()
    {
        global::yaml.YamlSchemaOptions options = JsonOptions();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("0", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("-0", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("1e+2", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("-0.5", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("01", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("+1", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("1.", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag(".5", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("1e", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void ResolveScalarTag_JsonSchema_DoesNotImplicitlyTypeQuotedScalars()
    {
        global::yaml.YamlSchemaOptions options = JsonOptions();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("true", global::yaml.YamlScalarStyle.SingleQuoted, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("123", global::yaml.YamlScalarStyle.DoubleQuoted, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void Resolve_ComposedParserResultWithJsonSchema_TagsScalarValuesByJsonRules()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("truth: true\ncount: 123\nratio: 1.25\nempty:\nquoted: 'false'\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlSchemaResolver.Resolve(composer.Stream, JsonOptions());

        global::yaml.YamlNode root = composer.Stream.Documents[0].Root!;

        Assert.That(root.Pairs.Select(pair => pair.Key!.Tag), Is.EqualTo(new string[]
        {
            global::yaml.YamlScalarResolver.FailsafeStringTag,
            global::yaml.YamlScalarResolver.FailsafeStringTag,
            global::yaml.YamlScalarResolver.FailsafeStringTag,
            global::yaml.YamlScalarResolver.FailsafeStringTag,
            global::yaml.YamlScalarResolver.FailsafeStringTag
        }));
        Assert.That(root.Pairs.Select(pair => pair.Value!.Tag), Is.EqualTo(new string[]
        {
            global::yaml.YamlScalarResolver.BoolTag,
            global::yaml.YamlScalarResolver.IntTag,
            global::yaml.YamlScalarResolver.FloatTag,
            global::yaml.YamlScalarResolver.NullTag,
            global::yaml.YamlScalarResolver.FailsafeStringTag
        }));
    }

    [Test]
    public void ResolveScalarTag_CoreSchema_RecognizesYaml12NullAndBoolForms()
    {
        global::yaml.YamlSchemaOptions options = CoreOptions();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.NullTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("~", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.NullTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("NULL", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.NullTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("True", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("FALSE", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("yes", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void ResolveScalarTag_CoreSchema_RecognizesYaml12IntegerAndFloatForms()
    {
        global::yaml.YamlSchemaOptions options = CoreOptions();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("+12", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("01", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("0xFFEEBB", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("0o17", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("1.", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag(".5", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("+1e-2", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag(".inf", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("-.INF", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag(".NaN", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("1e", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void ScalarBoundaryHelpers_RejectIndexesThatCannotFitWithoutOverflow()
    {
        Assert.That(global::yaml.YamlScalarResolver.StartsWithAt("value", int.MaxValue, "v"), Is.False);
        Assert.That(global::yaml.YamlScalarResolver.StartsWithAt("value", int.MinValue, "v"), Is.False);
        Assert.That(global::yaml.YamlScalarResolver.IsTimestampDate("2026-08-22", int.MaxValue), Is.False);
        Assert.That(global::yaml.YamlScalarResolver.IsTimestampTime("12:34:56", int.MaxValue), Is.False);
        Assert.That(global::yaml.YamlScalarResolver.IsTimestampOffset("12:34", int.MaxValue), Is.False);
    }

    [Test]
    public void ResolveScalarTag_CoreSchema_DoesNotImplicitlyTypeQuotedScalars()
    {
        global::yaml.YamlSchemaOptions options = CoreOptions();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("NULL", global::yaml.YamlScalarStyle.SingleQuoted, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("+12", global::yaml.YamlScalarStyle.DoubleQuoted, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void ResolveScalarTag_NullOptions_UseCoreSchemaRules()
    {
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("TRUE", global::yaml.YamlScalarStyle.Plain, null), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
    }

    [Test]
    public void Resolve_ComposedParserResultWithCoreSchema_TagsScalarValuesByYaml12CoreRules()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("truth: TRUE\nlegacy: yes\ncount: +12\nratio: .5\nempty:\nquoted: 'NULL'\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlSchemaResolver.Resolve(composer.Stream, CoreOptions());

        global::yaml.YamlNode root = composer.Stream.Documents[0].Root!;

        Assert.That(root.Pairs.Select(pair => pair.Value!.Tag), Is.EqualTo(new string[]
        {
            global::yaml.YamlScalarResolver.BoolTag,
            global::yaml.YamlScalarResolver.FailsafeStringTag,
            global::yaml.YamlScalarResolver.IntTag,
            global::yaml.YamlScalarResolver.FloatTag,
            global::yaml.YamlScalarResolver.NullTag,
            global::yaml.YamlScalarResolver.FailsafeStringTag
        }));
    }

    [Test]
    public void ResolveScalarTag_Yaml11Schema_RecognizesLegacyBooleanForms()
    {
        global::yaml.YamlSchemaOptions options = Yaml11Options();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("yes", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("NO", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("On", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("off", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("Y", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("n", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
    }

    [Test]
    public void ResolveScalarTag_Yaml11Schema_RecognizesIntegerBasesAndUnderscores()
    {
        global::yaml.YamlSchemaOptions options = Yaml11Options();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("1_000", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("0b1010", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("-0x1f", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("0b102", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("0x", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void ResolveScalarTag_Yaml11Schema_RecognizesFloatFormsWithUnderscores()
    {
        global::yaml.YamlSchemaOptions options = Yaml11Options();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("1_000.5", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("+1_2e-3", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag(".INF", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag(".nan", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
    }

    [Test]
    public void ResolveScalarTag_Yaml11Schema_DoesNotImplicitlyTypeQuotedScalars()
    {
        global::yaml.YamlSchemaOptions options = Yaml11Options();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("yes", global::yaml.YamlScalarStyle.SingleQuoted, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("0x1f", global::yaml.YamlScalarStyle.DoubleQuoted, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void ResolveScalarTag_CoreSchemaWithYaml11BooleanOption_RecognizesLegacyBooleansOnly()
    {
        global::yaml.YamlSchemaOptions options = CoreOptions();
        options.AllowYaml11Booleans = true;

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("yes", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("0x1f", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
    }

    [Test]
    public void Resolve_ComposedParserResultWithYaml11Schema_TagsScalarValuesByYaml11Rules()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("enabled: ON\ndisabled: no\nhex: 0x10\nfloat: 1_000.5\nquoted: 'yes'\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlSchemaResolver.Resolve(composer.Stream, Yaml11Options());

        global::yaml.YamlNode root = composer.Stream.Documents[0].Root!;

        Assert.That(root.Pairs.Select(pair => pair.Value!.Tag), Is.EqualTo(new string[]
        {
            global::yaml.YamlScalarResolver.BoolTag,
            global::yaml.YamlScalarResolver.BoolTag,
            global::yaml.YamlScalarResolver.IntTag,
            global::yaml.YamlScalarResolver.FloatTag,
            global::yaml.YamlScalarResolver.FailsafeStringTag
        }));
    }

    [Test]
    public void ResolveScalarTag_AmbiguousScalars_DifferBetweenCoreJsonAndYaml11Schemas()
    {
        global::yaml.YamlSchemaOptions core = CoreOptions();
        global::yaml.YamlSchemaOptions json = JsonOptions();
        global::yaml.YamlSchemaOptions yaml11 = Yaml11Options();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("ON", global::yaml.YamlScalarStyle.Plain, core), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("ON", global::yaml.YamlScalarStyle.Plain, yaml11), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("True", global::yaml.YamlScalarStyle.Plain, json), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("True", global::yaml.YamlScalarStyle.Plain, core), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("0x10", global::yaml.YamlScalarStyle.Plain, core), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("0x10", global::yaml.YamlScalarStyle.Plain, yaml11), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("1_000.5", global::yaml.YamlScalarStyle.Plain, core), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("1_000.5", global::yaml.YamlScalarStyle.Plain, yaml11), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
    }

    [Test]
    public void Resolve_ExplicitTagsOverrideAmbiguousScalarResolution()
    {
        global::yaml.YamlNode stringScalar = global::yaml.YamlNode.Scalar("true", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());
        stringScalar.Tag = "!!str";
        global::yaml.YamlNode boolScalar = global::yaml.YamlNode.Scalar("not-a-bool", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());
        boolScalar.Tag = "!!bool";
        global::yaml.YamlNode intScalar = global::yaml.YamlNode.Scalar("not-an-int", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());
        intScalar.Tag = "!!int";

        global::yaml.YamlSchemaResolver.ResolveNode(stringScalar, CoreOptions());
        global::yaml.YamlSchemaResolver.ResolveNode(boolScalar, CoreOptions());
        global::yaml.YamlSchemaResolver.ResolveNode(intScalar, CoreOptions());

        Assert.That(stringScalar.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(boolScalar.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(intScalar.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
    }

    [Test]
    public void Resolve_PreservesUnknownAndLocalTagsOnScalarsAndCollections()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNode.Scalar("true", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());
        scalar.Tag = "!local";
        global::yaml.YamlNode sequence = global::yaml.YamlNode.Sequence(global::yaml.YamlCollectionStyle.Block, global::yaml.YamlSourceSpan.Empty());
        sequence.Tag = "!unknown-sequence";
        global::yaml.YamlNode mapping = global::yaml.YamlNode.Mapping(global::yaml.YamlCollectionStyle.Block, global::yaml.YamlSourceSpan.Empty());
        mapping.Tag = "tag:example.com,2026:custom-map";
        mapping.AddPair(global::yaml.YamlPair.Create(scalar, sequence, global::yaml.YamlSourceSpan.Empty()));

        global::yaml.YamlSchemaResolver.ResolveNode(mapping, CoreOptions());

        Assert.That(mapping.Tag, Is.EqualTo("tag:example.com,2026:custom-map"));
        Assert.That(mapping.Pairs[0].Key!.Tag, Is.EqualTo("!local"));
        Assert.That(mapping.Pairs[0].Value!.Tag, Is.EqualTo("!unknown-sequence"));
    }

    [Test]
    public void Resolve_ComposedParserResult_PreservesExplicitUnknownAndLocalTags()
    {
        string text = "plain: yes\nquoted: 'yes'\nforcedString: !!str true\nforcedBool: !!bool no\nlocal: !local 12\nunknown: !<tag:example.com,2026:thing> value\n";
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlSchemaResolver.Resolve(composer.Stream, Yaml11Options());

        global::yaml.YamlNode root = composer.Stream.Documents[0].Root!;
        Assert.That(root.Pairs[0].Value!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(root.Pairs[1].Value!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(root.Pairs[2].Value!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(root.Pairs[3].Value!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(root.Pairs[4].Value!.Tag, Is.EqualTo("!local"));
        Assert.That(root.Pairs[5].Value!.Tag, Is.EqualTo("tag:example.com,2026:thing"));
    }

    [Test]
    public void NormalizeStandardTag_RecognizesStandardShorthandTags()
    {
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!null"), Is.EqualTo(global::yaml.YamlScalarResolver.NullTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!bool"), Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!int"), Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!float"), Is.EqualTo(global::yaml.YamlScalarResolver.FloatTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!str"), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!seq"), Is.EqualTo(global::yaml.YamlScalarResolver.SequenceTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!map"), Is.EqualTo(global::yaml.YamlScalarResolver.MappingTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!local"), Is.EqualTo("!local"));
    }

    [Test]
    public void NormalizeStandardTag_RecognizesOptionalStandardTags()
    {
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!timestamp"), Is.EqualTo(global::yaml.YamlScalarResolver.TimestampTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!binary"), Is.EqualTo(global::yaml.YamlScalarResolver.BinaryTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!set"), Is.EqualTo(global::yaml.YamlScalarResolver.SetTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!omap"), Is.EqualTo(global::yaml.YamlScalarResolver.OrderedMapTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!pairs"), Is.EqualTo(global::yaml.YamlScalarResolver.PairsTag));
        Assert.That(global::yaml.YamlScalarResolver.NormalizeStandardTag("!!merge"), Is.EqualTo(global::yaml.YamlScalarResolver.MergeTag));
        Assert.That(global::yaml.YamlScalarResolver.IsStandardTag("!!timestamp"), Is.True);
        Assert.That(global::yaml.YamlScalarResolver.IsStandardTag(global::yaml.YamlScalarResolver.BinaryTag), Is.True);
    }

    [Test]
    public void Resolve_NormalizesExplicitOptionalTags()
    {
        global::yaml.YamlNode root = global::yaml.YamlNode.Mapping(global::yaml.YamlCollectionStyle.Block, global::yaml.YamlSourceSpan.Empty());
        root.Tag = "!!set";
        global::yaml.YamlNode timestamp = global::yaml.YamlNode.Scalar("2026-08-01", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());
        timestamp.Tag = "!!timestamp";
        global::yaml.YamlNode binary = global::yaml.YamlNode.Scalar("SGVsbG8=", global::yaml.YamlScalarStyle.Literal, global::yaml.YamlSourceSpan.Empty());
        binary.Tag = "!!binary";
        global::yaml.YamlNode omap = global::yaml.YamlNode.Sequence(global::yaml.YamlCollectionStyle.Flow, global::yaml.YamlSourceSpan.Empty());
        omap.Tag = "!!omap";
        global::yaml.YamlNode pairs = global::yaml.YamlNode.Sequence(global::yaml.YamlCollectionStyle.Flow, global::yaml.YamlSourceSpan.Empty());
        pairs.Tag = "!!pairs";

        root.AddPair(global::yaml.YamlPair.Create(timestamp, binary, global::yaml.YamlSourceSpan.Empty()));
        root.AddPair(global::yaml.YamlPair.Create(
            global::yaml.YamlNode.Scalar("ordered", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty()),
            omap,
            global::yaml.YamlSourceSpan.Empty()));
        root.AddPair(global::yaml.YamlPair.Create(
            global::yaml.YamlNode.Scalar("pairs", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty()),
            pairs,
            global::yaml.YamlSourceSpan.Empty()));

        global::yaml.YamlSchemaResolver.ResolveNode(root, CoreOptions());

        Assert.That(root.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.SetTag));
        Assert.That(root.Pairs[0].Key!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.TimestampTag));
        Assert.That(root.Pairs[0].Value!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BinaryTag));
        Assert.That(root.Pairs[1].Value!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.OrderedMapTag));
        Assert.That(root.Pairs[2].Value!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.PairsTag));
    }

    [Test]
    public void ResolveScalarTag_AllowTimestamp_TagsPlainDatesAndDateTimes()
    {
        global::yaml.YamlSchemaOptions options = CoreOptions();
        options.AllowTimestamp = true;

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("2026-08-01", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.TimestampTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("2026-08-01T12:30:45Z", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.TimestampTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("2026-08-01 12:30:45-07:00", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.TimestampTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("2026-02-29", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("2026-08-01", global::yaml.YamlScalarStyle.SingleQuoted, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void ResolveScalarTag_AllowTimestampFalse_LeavesDatesAsStrings()
    {
        global::yaml.YamlSchemaOptions options = CoreOptions();

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("2026-08-01", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void ResolveScalarTag_CustomTagResolver_RunsBeforeBuiltInSchemaRules()
    {
        global::yaml.YamlSchemaOptions options = CoreOptions();
        options.CustomTags.RegisterScalarResolver("!answer", (value, style) => style == global::yaml.YamlScalarStyle.Plain && value == "42");

        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("42", global::yaml.YamlScalarStyle.Plain, options), Is.EqualTo("!answer"));
        Assert.That(global::yaml.YamlScalarResolver.ResolveScalarTag("42", global::yaml.YamlScalarStyle.SingleQuoted, options), Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void Resolve_AllowMergeKeys_TagsPlainMergeKeyScalars()
    {
        global::yaml.YamlSchemaOptions options = CoreOptions();
        options.AllowMergeKeys = true;
        global::yaml.YamlNode root = global::yaml.YamlNode.Mapping(global::yaml.YamlCollectionStyle.Block, global::yaml.YamlSourceSpan.Empty());
        root.AddPair(global::yaml.YamlPair.Create(
            global::yaml.YamlNode.Scalar("<<", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty()),
            global::yaml.YamlNode.Scalar("*defaults", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty()),
            global::yaml.YamlSourceSpan.Empty()));
        root.AddPair(global::yaml.YamlPair.Create(
            global::yaml.YamlNode.Scalar("<<", global::yaml.YamlScalarStyle.SingleQuoted, global::yaml.YamlSourceSpan.Empty()),
            global::yaml.YamlNode.Scalar("literal", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty()),
            global::yaml.YamlSourceSpan.Empty()));

        global::yaml.YamlSchemaResolver.ResolveNode(root, options);

        Assert.That(root.Pairs[0].Key!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.MergeTag));
        Assert.That(root.Pairs[1].Key!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void Resolve_AllowMergeKeysFalse_LeavesMergeKeyAsString()
    {
        global::yaml.YamlNode root = global::yaml.YamlNode.Mapping(global::yaml.YamlCollectionStyle.Block, global::yaml.YamlSourceSpan.Empty());
        root.AddPair(global::yaml.YamlPair.Create(
            global::yaml.YamlNode.Scalar("<<", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty()),
            global::yaml.YamlNode.Scalar("value", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty()),
            global::yaml.YamlSourceSpan.Empty()));

        global::yaml.YamlSchemaResolver.ResolveNode(root, CoreOptions());

        Assert.That(root.Pairs[0].Key!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
    }

    [Test]
    public void Resolve_ComposedParserResult_TagsOptionalTimestampBinaryAndMergeKey()
    {
        string text = "defaults: &defaults { name: base }\ncreated: 2026-08-01T12:30:45Z\npayload: !!binary SGVsbG8=\n<<: *defaults\n";
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());
        global::yaml.YamlSchemaOptions options = CoreOptions();
        options.AllowTimestamp = true;
        options.AllowMergeKeys = true;

        global::yaml.YamlSchemaResolver.Resolve(composer.Stream, options);

        global::yaml.YamlNode root = composer.Stream.Documents[0].Root!;
        Assert.That(root.Pairs[1].Value!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.TimestampTag));
        Assert.That(root.Pairs[2].Value!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BinaryTag));
        Assert.That(root.Pairs[3].Key!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.MergeTag));
    }

    [Test]
    public void Resolve_AssignsStandardTagsToSequenceAndMappingNodes()
    {
        global::yaml.YamlNode root = global::yaml.YamlNode.Mapping(global::yaml.YamlCollectionStyle.Block, global::yaml.YamlSourceSpan.Empty());
        global::yaml.YamlNode sequence = global::yaml.YamlNode.Sequence(global::yaml.YamlCollectionStyle.Flow, global::yaml.YamlSourceSpan.Empty());
        root.AddPair(global::yaml.YamlPair.Create(
            global::yaml.YamlNode.Scalar("items", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty()),
            sequence,
            global::yaml.YamlSourceSpan.Empty()));

        global::yaml.YamlSchemaResolver.ResolveNode(root, CoreOptions());

        Assert.That(root.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.MappingTag));
        Assert.That(sequence.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.SequenceTag));
    }

    [Test]
    public void Resolve_NormalizesExplicitStandardTagsAndPreservesLocalTags()
    {
        global::yaml.YamlNode scalar = global::yaml.YamlNode.Scalar("123", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.Empty());
        scalar.Tag = "!!int";
        global::yaml.YamlNode sequence = global::yaml.YamlNode.Sequence(global::yaml.YamlCollectionStyle.Flow, global::yaml.YamlSourceSpan.Empty());
        sequence.Tag = "!!seq";
        global::yaml.YamlNode mapping = global::yaml.YamlNode.Mapping(global::yaml.YamlCollectionStyle.Flow, global::yaml.YamlSourceSpan.Empty());
        mapping.Tag = "!local";
        mapping.AddPair(global::yaml.YamlPair.Create(scalar, sequence, global::yaml.YamlSourceSpan.Empty()));

        global::yaml.YamlSchemaResolver.ResolveNode(mapping, CoreOptions());

        Assert.That(mapping.Tag, Is.EqualTo("!local"));
        Assert.That(mapping.Pairs[0].Key!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(mapping.Pairs[0].Value!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.SequenceTag));
    }

    [Test]
    public void Resolve_ComposedParserResult_AssignsStandardCollectionAndScalarTags()
    {
        global::yaml.YamlScannerResult scanner = global::yaml.YamlScanner.Scan("numbers: [1, 2]\n", new global::yaml.YamlParserOptions());
        global::yaml.YamlParserResult parser = global::yaml.YamlParser.ParseScannerResult(scanner, new global::yaml.YamlParserOptions());
        global::yaml.YamlComposerResult composer = global::yaml.YamlComposer.ComposeParserResult(parser, new global::yaml.YamlComposerOptions());

        global::yaml.YamlSchemaResolver.Resolve(composer.Stream, CoreOptions());

        global::yaml.YamlNode root = composer.Stream.Documents[0].Root!;
        global::yaml.YamlNode sequence = root.Pairs[0].Value!;

        Assert.That(root.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.MappingTag));
        Assert.That(root.Pairs[0].Key!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.FailsafeStringTag));
        Assert.That(sequence.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.SequenceTag));
        Assert.That(sequence.Children.Select(child => child.Tag), Is.EqualTo(new string[]
        {
            global::yaml.YamlScalarResolver.IntTag,
            global::yaml.YamlScalarResolver.IntTag
        }));
    }

    static global::yaml.YamlSchemaOptions FailsafeOptions()
    {
        global::yaml.YamlSchemaOptions options = new global::yaml.YamlSchemaOptions();
        options.Schema = global::yaml.YamlSchemaKind.Failsafe;
        return options;
    }

    static global::yaml.YamlSchemaOptions JsonOptions()
    {
        global::yaml.YamlSchemaOptions options = new global::yaml.YamlSchemaOptions();
        options.Schema = global::yaml.YamlSchemaKind.Json;
        return options;
    }

    static global::yaml.YamlSchemaOptions CoreOptions()
    {
        global::yaml.YamlSchemaOptions options = new global::yaml.YamlSchemaOptions();
        options.Schema = global::yaml.YamlSchemaKind.Core;
        return options;
    }

    static global::yaml.YamlSchemaOptions Yaml11Options()
    {
        global::yaml.YamlSchemaOptions options = new global::yaml.YamlSchemaOptions();
        options.Schema = global::yaml.YamlSchemaKind.Yaml11;
        return options;
    }
}
