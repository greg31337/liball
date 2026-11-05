using System.Text.Json;
using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlDeterministicRandomizedTests
{
    public static readonly int[] Seeds = new int[] { 101, 809, 2027, 4093, 8191, 16381 };

    [TestCaseSource(nameof(Seeds))]
    public void GeneratedScalarAndCollectionGraphs_RoundTripThroughPublicModelAndProcessor(int seed)
    {
        Random random = new Random(seed);
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(null);

        int iteration = 0;
        while (iteration < 32)
        {
            object? expected = GenerateValue(random, 0, iteration % 3 == 0);
            global::yaml.YamlNode node = model.FromJsonValue(expected);
            object? modelValue = model.ToJsonValue(node);
            string emitted = processor.Dump(expected);
            object? loaded = processor.Load(emitted);

            AssertEquivalent(expected, modelValue, seed, iteration, "model conversion");
            AssertEquivalent(expected, loaded, seed, iteration, "processor round trip");
            iteration = iteration + 1;
        }
    }

    [TestCaseSource(nameof(Seeds))]
    public void GeneratedGraphs_ParseEmitParseWithoutChangingValues(int seed)
    {
        Random random = new Random(seed);
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(null);
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;

        int iteration = 0;
        while (iteration < 24)
        {
            object? expected = GenerateValue(random, 0, iteration % 2 == 0);
            string firstText = processor.Dump(expected);
            global::yaml.YamlDocument first = processor.Parse(firstText);
            string secondText = pipeline.EmitDocument(first, null);
            global::yaml.YamlDocument second = processor.Parse(secondText);

            AssertEquivalent(model.ToJsonValue(first.Root), model.ToJsonValue(second.Root), seed, iteration, "parse-emit-parse");
            iteration = iteration + 1;
        }
    }

    [Test]
    public void FixedRegression_NestedBlockSequenceDoesNotGainAnEmptyItem()
    {
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(null);
        List<object?> inner = new List<object?>();
        inner.Add(1L);
        List<object?> expected = new List<object?>();
        expected.Add(inner);

        string text = processor.Dump(expected);
        object? actual = processor.Load(text);

        AssertEquivalent(expected, actual, 0, 0, "fixed nested block sequence regression");
    }

    [TestCase("[one, two\n")]
    [TestCase("{one: 1\n")]
    public void FixedRegression_UnterminatedFlowCollectionReportsAnError(string text)
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;

        global::yaml.YamlParserResult result = pipeline.ParseScannerResult(pipeline.Scan(text, options), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == "YAMLP015"), Is.True);
    }

    [TestCaseSource(nameof(Seeds))]
    public void RandomizedEditorOperations_RejectInvalidEditsAndRemainUsable(int seed)
    {
        Random random = new Random(seed);
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(null);
        global::yaml.YamlNode sequence = model.CreateSequenceNode(global::yaml.YamlCollectionStyle.Block, null);
        List<string> expected = new List<string>();

        int operation = 0;
        while (operation < 96)
        {
            int kind = random.Next(6);
            string value = "value-" + seed.ToString() + "-" + operation.ToString();
            if (kind == 0 || expected.Count == 0)
            {
                Assert.That(model.AddSequenceItem(sequence, Scalar(model, value)), Is.True, Context(seed, operation, "append"));
                expected.Add(value);
            }
            else if (kind == 1)
            {
                int index = random.Next(expected.Count + 1);
                Assert.That(model.InsertSequenceItem(sequence, index, Scalar(model, value)), Is.True, Context(seed, operation, "insert"));
                expected.Insert(index, value);
            }
            else if (kind == 2)
            {
                int index = random.Next(expected.Count);
                Assert.That(model.SetSequenceItem(sequence, index, Scalar(model, value)), Is.True, Context(seed, operation, "replace"));
                expected[index] = value;
            }
            else if (kind == 3)
            {
                int index = random.Next(expected.Count);
                Assert.That(model.RemoveSequenceItem(sequence, index), Is.True, Context(seed, operation, "remove"));
                expected.RemoveAt(index);
            }
            else
            {
                int invalidIndex = kind == 4 ? -1 - random.Next(8) : expected.Count + random.Next(1, 9);
                Assert.That(model.InsertSequenceItem(sequence, invalidIndex, Scalar(model, value)), Is.False, Context(seed, operation, "invalid insert"));
                Assert.That(model.SetSequenceItem(sequence, invalidIndex, Scalar(model, value)), Is.False, Context(seed, operation, "invalid replace"));
                Assert.That(model.RemoveSequenceItem(sequence, invalidIndex), Is.False, Context(seed, operation, "invalid remove"));
                Assert.That(model.GetSequenceItem(sequence, invalidIndex), Is.Null, Context(seed, operation, "invalid lookup"));
            }

            Assert.That(model.SequenceItemCount(sequence), Is.EqualTo(expected.Count), Context(seed, operation, "count after edit"));
            int verificationIndex = 0;
            while (verificationIndex < expected.Count)
            {
                Assert.That(model.GetSequenceItem(sequence, verificationIndex)!.Value, Is.EqualTo(expected[verificationIndex]), Context(seed, operation, "contents after edit"));
                verificationIndex = verificationIndex + 1;
            }

            operation = operation + 1;
        }

        string text = pipeline.EmitNode(sequence, null);
        List<object?> loaded = (List<object?>)processor.Load(text)!;
        Assert.That(loaded.Select(value => value == null ? "" : value.ToString()), Is.EqualTo(expected), Context(seed, operation, "emit after recovery"));
    }

    [TestCaseSource(nameof(Seeds))]
    public void RandomizedMalformedInput_ProducesDiagnosticsAndRecoveryBoundaries(int seed)
    {
        string[] malformed = new string[]
        {
            "\u0001\n",
            "[one, two\n",
            "{one: 1\n",
            "\"unterminated\n",
            "'unterminated\n",
            "]\n",
            "key: [one, }\n",
            "%YAML 2.0\n---\nvalue\n"
        };
        Random random = new Random(seed);
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;

        int iteration = 0;
        while (iteration < malformed.Length * 2)
        {
            int selected = random.Next(malformed.Length);
            string text = new string('#', random.Next(1, 4)) + " randomized\n" + malformed[selected];
            global::yaml.YamlScannerResult scanner = pipeline.Scan(text, options);
            global::yaml.YamlParserResult parser = pipeline.ParseScannerResult(scanner, options);

            Assert.That(parser.Success, Is.False, Context(seed, iteration, "malformed input accepted at case " + selected.ToString()));
            Assert.That(parser.Diagnostics, Is.Not.Empty, Context(seed, iteration, "missing malformed diagnostic at case " + selected.ToString()));
            Assert.That(parser.Events.First().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamStart), Context(seed, iteration, "recovery stream start"));
            Assert.That(parser.Events.Last().Kind, Is.EqualTo(global::yaml.YamlEventKind.StreamEnd), Context(seed, iteration, "recovery stream end"));
            iteration = iteration + 1;
        }
    }

    [TestCaseSource(nameof(Seeds))]
    public void RandomizedAliasesAndCycles_ResolveEmitAndFailSafely(int seed)
    {
        Random random = new Random(seed);
        int aliasCount = random.Next(1, 12);
        string yaml = AliasDocument(aliasCount);
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(null);
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        global::yaml.YamlDocument document = processor.Parse(yaml);

        Assert.That(model.RebuildAnchorTable(document), Is.EqualTo(1), Context(seed, 0, "anchor table"));
        Assert.That(model.ResolveAliases(document), Is.EqualTo(aliasCount), Context(seed, 0, "alias resolution"));
        int index = 0;
        while (index < aliasCount)
        {
            global::yaml.YamlNode alias = model.FindMappingValue(document.Root, "alias" + index.ToString())!;
            Assert.That(alias.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias), Context(seed, index, "alias kind"));
            Assert.That(alias.AliasTarget, Is.SameAs(model.FindAnchor(document, "base")), Context(seed, index, "alias target"));
            index = index + 1;
        }

        string emitted = pipeline.EmitDocument(document, null);
        global::yaml.YamlDocument reparsed = processor.Parse(emitted);
        Assert.That(model.ResolveAliases(reparsed), Is.EqualTo(aliasCount), Context(seed, 0, "emitted aliases"));

        global::yaml.YamlDocument recursive = processor.Parse("&root [*root]\n");
        global::yaml.YamlNode recursiveAlias = model.GetSequenceItem(recursive.Root, 0)!;
        Assert.That(recursiveAlias.AliasTarget, Is.SameAs(recursive.Root), Context(seed, 0, "recursive alias"));
        string recursiveText = pipeline.EmitDocument(recursive, null);
        global::yaml.YamlDocument recursiveRoundTrip = processor.Parse(recursiveText);
        Assert.That(model.GetSequenceItem(recursiveRoundTrip.Root, 0)!.AliasTarget, Is.SameAs(recursiveRoundTrip.Root), Context(seed, 0, "recursive alias round trip"));

        global::yaml.YamlNode cycle = model.CreateSequenceNode(global::yaml.YamlCollectionStyle.Block, null);
        Assert.That(model.AddSequenceItem(cycle, cycle), Is.True, Context(seed, 0, "create direct cycle"));
        global::yaml.YamlResult validation = pipeline.ValidateNode(cycle, null);
        Assert.That(validation.Success, Is.False, Context(seed, 0, "cycle validation"));
        Assert.That(validation.Diagnostics.Any(diagnostic => diagnostic.Code == "YAMLV001"), Is.True, Context(seed, 0, "cycle diagnostic"));
        Assert.That(() => pipeline.EmitNode(cycle, null), Throws.TypeOf<global::yaml.YamlException>(), Context(seed, 0, "cycle emission"));
    }

    [TestCaseSource(nameof(Seeds))]
    public void RandomizedSecurityLimitBoundaries_AcceptAtLimitAndRejectPastIt(int seed)
    {
        Random random = new Random(seed);
        int scalarLength = random.Next(2, 32);
        int collectionSize = random.Next(2, 16);
        int aliasCount = random.Next(2, 16);
        int depth = random.Next(2, 12);
        string scalar = new string('x', scalarLength) + "\n";
        string collection = "[" + string.Join(", ", Enumerable.Range(0, collectionSize)) + "]\n";
        string aliases = AliasDocument(aliasCount);
        string nested = NestedFlowSequence(depth);

        AssertSecurityBoundary(scalar, "YAMLS002", options => options.Security.MaxScalarLength = scalarLength, options => options.Security.MaxScalarLength = scalarLength - 1, seed, "scalar");
        AssertSecurityBoundary(collection, "YAMLS005", options => options.Security.MaxCollectionSize = collectionSize, options => options.Security.MaxCollectionSize = collectionSize - 1, seed, "collection");
        AssertSecurityBoundary(aliases, "YAMLS004", options => options.Security.MaxAliasCount = aliasCount, options => options.Security.MaxAliasCount = aliasCount - 1, seed, "aliases");
        AssertSecurityBoundary(nested, "YAMLS003", options => options.Security.MaxDepth = depth + 1, options => options.Security.MaxDepth = depth, seed, "depth");

        int inputLength = scalar.Length;
        AssertSecurityBoundary(scalar, "YAMLS001", options => options.Security.MaxInputLength = inputLength, options => options.Security.MaxInputLength = inputLength - 1, seed, "input");
    }

    public static object? GenerateValue(Random random, int depth, bool requireCollection)
    {
        if (depth >= 4 || (!requireCollection && random.Next(100) < 48))
        {
            return GenerateScalar(random);
        }

        if (random.Next(2) == 0)
        {
            List<object?> sequence = new List<object?>();
            int count = random.Next(0, 6);
            int index = 0;
            while (index < count)
            {
                sequence.Add(GenerateValue(random, depth + 1, false));
                index = index + 1;
            }

            return sequence;
        }

        Dictionary<string, object?> mapping = new Dictionary<string, object?>();
        int pairCount = random.Next(0, 6);
        int pairIndex = 0;
        while (pairIndex < pairCount)
        {
            mapping[GenerateKey(random, depth, pairIndex)] = GenerateValue(random, depth + 1, false);
            pairIndex = pairIndex + 1;
        }

        return mapping;
    }

    public static object? GenerateScalar(Random random)
    {
        string[] strings = new string[]
        {
            "",
            "plain",
            "null",
            "true",
            " leading",
            "trailing ",
            "a: b",
            "#hash",
            "- dash",
            "line\nbreak",
            "quote'and\"double",
            "[braces] {mapping}",
            "comma, colon:",
            "caf\u00e9 \u2615"
        };
        int kind = random.Next(5);
        if (kind == 0)
        {
            return null;
        }

        if (kind == 1)
        {
            return random.Next(2) == 0;
        }

        if (kind == 2)
        {
            return (long)random.Next(-1000000, 1000001);
        }

        return strings[random.Next(strings.Length)];
    }

    public static string GenerateKey(Random random, int depth, int index)
    {
        string[] prefixes = new string[] { "key", "null", "a: b", "#hash", " leading", "caf\u00e9" };
        return prefixes[random.Next(prefixes.Length)] + "-" + depth.ToString() + "-" + index.ToString();
    }

    public static global::yaml.YamlNode Scalar(global::yaml.IYamlModel model, string value)
    {
        return model.CreateScalarNode(value, global::yaml.YamlScalarStyle.Plain, null);
    }

    public static string AliasDocument(int aliasCount)
    {
        string text = "base: &base [one, two]\n";
        int index = 0;
        while (index < aliasCount)
        {
            text = text + "alias" + index.ToString() + ": *base\n";
            index = index + 1;
        }

        return text;
    }

    public static string NestedFlowSequence(int depth)
    {
        string text = "0";
        int index = 0;
        while (index < depth)
        {
            text = "[" + text + "]";
            index = index + 1;
        }

        return text + "\n";
    }

    public static void AssertEquivalent(object? expected, object? actual, int seed, int iteration, string operation)
    {
        string expectedJson = JsonSerializer.Serialize(expected);
        string actualJson = JsonSerializer.Serialize(actual);
        Assert.That(actualJson, Is.EqualTo(expectedJson), Context(seed, iteration, operation));
    }

    public static void AssertSecurityBoundary(
        string text,
        string code,
        Action<global::yaml.YamlOptions> configureAccepted,
        Action<global::yaml.YamlOptions> configureRejected,
        int seed,
        string scenario)
    {
        global::yaml.YamlOptions acceptedOptions = new global::yaml.YamlOptions();
        configureAccepted(acceptedOptions);
        global::yaml.YamlParseResult accepted = new global::yaml.YamlParseResult();
        bool acceptedSuccess = global::yaml.YamlApi.Processors.CreateProcessor(acceptedOptions).TryParse(text, accepted);
        Assert.That(acceptedSuccess, Is.True, Context(seed, 0, scenario + " at limit"));

        global::yaml.YamlOptions rejectedOptions = new global::yaml.YamlOptions();
        configureRejected(rejectedOptions);
        global::yaml.YamlParseResult rejected = new global::yaml.YamlParseResult();
        bool rejectedSuccess = global::yaml.YamlApi.Processors.CreateProcessor(rejectedOptions).TryParse(text, rejected);
        Assert.That(rejectedSuccess, Is.False, Context(seed, 0, scenario + " past limit"));
        Assert.That(rejected.Diagnostics.Any(diagnostic => diagnostic.Code == code), Is.True, Context(seed, 0, scenario + " diagnostic"));
    }

    public static string Context(int seed, int iteration, string operation)
    {
        return operation + "; seed=" + seed.ToString() + "; iteration=" + iteration.ToString();
    }
}
