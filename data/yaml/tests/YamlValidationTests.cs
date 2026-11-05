using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlValidationTests
{
    [Test]
    public void PipelineValidation_AcceptsValidStreamsDocumentsAndNodeGraphs()
    {
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        global::yaml.YamlStream stream = new global::yaml.YamlStream();
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        global::yaml.YamlNode timestamp = Scalar("2026-08-27");
        timestamp.Tag = global::yaml.YamlTags.Timestamp;
        global::yaml.YamlNode binary = Scalar("SGVsbG8=");
        binary.Tag = global::yaml.YamlTags.Binary;
        global::yaml.YamlNode root = Sequence(Scalar("one"), timestamp, binary);
        document.Root = root;
        stream.Documents.Add(document);

        global::yaml.YamlResult streamResult = pipeline.ValidateStream(stream, null);
        global::yaml.YamlResult documentResult = pipeline.ValidateDocument(document, null);
        global::yaml.YamlResult nodeResult = pipeline.ValidateNode(root, null);

        Assert.That(streamResult.Success, Is.True);
        Assert.That(streamResult.Diagnostics, Is.Empty);
        Assert.That(documentResult.Success, Is.True);
        Assert.That(documentResult.Diagnostics, Is.Empty);
        Assert.That(nodeResult.Success, Is.True);
        Assert.That(nodeResult.Diagnostics, Is.Empty);
    }

    [Test]
    public void ValidateNode_ReportsConfiguredGraphAndSecurityDiagnosticsTogether()
    {
        global::yaml.YamlNode root = Mapping();
        root.Pairs.Add(Pair(Scalar("same"), Scalar("first")));
        root.Pairs.Add(Pair(Scalar("same"), Scalar("second")));

        global::yaml.YamlNode nested = Sequence(Scalar("long"), Scalar("extra"));
        nested.Tag = "!unknown";
        root.Pairs.Add(Pair(Scalar("nested"), nested));

        global::yaml.YamlNode anchor = Scalar("base");
        anchor.Anchor = "base";
        root.Pairs.Add(Pair(Scalar("anchor"), anchor));
        root.Pairs.Add(Pair(Scalar("alias-one"), Alias("base", anchor)));
        root.Pairs.Add(Pair(Scalar("alias-two"), Alias("base", anchor)));

        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Error;
        options.Security.MaxDepth = 2;
        options.Security.MaxScalarLength = 3;
        options.Security.MaxAliasCount = 1;
        options.Security.MaxCollectionSize = 1;

        global::yaml.YamlResult result = new global::yaml.YamlPipeline().ValidateNode(root, options);

        Assert.That(result.Success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLC009"), Is.True);
        Assert.That(HasDiagnostic(result, "YAMLS006"), Is.True);
        Assert.That(HasDiagnostic(result, "YAMLS002"), Is.True);
        Assert.That(HasDiagnostic(result, "YAMLS003"), Is.True);
        Assert.That(HasDiagnostic(result, "YAMLS004"), Is.True);
        Assert.That(HasDiagnostic(result, "YAMLS005"), Is.True);
    }

    [Test]
    public void ValidateNode_ReportsSchemaAnchorAndAliasRules()
    {
        global::yaml.YamlNode root = Sequence();
        root.Anchor = "root";

        global::yaml.YamlNode invalidTag = Scalar("value");
        invalidTag.Tag = global::yaml.YamlTags.Sequence;
        root.Children.Add(invalidTag);

        global::yaml.YamlNode firstAnchor = Scalar("first");
        firstAnchor.Anchor = "duplicate";
        root.Children.Add(firstAnchor);
        global::yaml.YamlNode secondAnchor = Scalar("second");
        secondAnchor.Anchor = "duplicate";
        root.Children.Add(secondAnchor);
        root.Children.Add(Alias("missing", null));
        root.Children.Add(Alias("root", root));

        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Composer.AllowRecursiveAliases = false;

        global::yaml.YamlResult result = new global::yaml.YamlPipeline().ValidateNode(root, options);

        Assert.That(result.Success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLC006"), Is.False);
        Assert.That(HasDiagnostic(result, "YAMLV003"), Is.True);
        Assert.That(HasDiagnostic(result, "YAMLC007"), Is.True);
        Assert.That(HasDiagnostic(result, "YAMLC008"), Is.True);
    }

    [Test]
    public void ValidateNode_HonorsWarningPreserveAndDisabledLimitPolicies()
    {
        global::yaml.YamlNode root = Mapping();
        global::yaml.YamlNode unknown = Scalar("large value");
        unknown.Tag = "!custom";
        root.Pairs.Add(Pair(Scalar("key"), unknown));
        root.Pairs.Add(Pair(Scalar("key"), Scalar("duplicate")));

        global::yaml.YamlOptions warningOptions = new global::yaml.YamlOptions();
        warningOptions.Composer.DuplicateKeyPolicy = global::yaml.YamlDuplicateKeyPolicy.Warn;
        warningOptions.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Warn;
        warningOptions.Security.EnforceLimits = false;
        warningOptions.Security.MaxScalarLength = 1;
        global::yaml.YamlResult warnings = new global::yaml.YamlPipeline().ValidateNode(root, warningOptions);

        Assert.That(warnings.Success, Is.True);
        Assert.That(HasSeverity(warnings, "YAMLC009", global::yaml.YamlDiagnosticSeverity.Warning), Is.True);
        Assert.That(HasSeverity(warnings, "YAMLS006", global::yaml.YamlDiagnosticSeverity.Warning), Is.True);
        Assert.That(HasDiagnostic(warnings, "YAMLS002"), Is.False);

        warningOptions.Composer.DuplicateKeyPolicy = global::yaml.YamlDuplicateKeyPolicy.PreserveAll;
        warningOptions.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Preserve;
        global::yaml.YamlResult preserved = new global::yaml.YamlPipeline().ValidateNode(root, warningOptions);

        Assert.That(preserved.Success, Is.True);
        Assert.That(preserved.Diagnostics, Is.Empty);
    }

    [Test]
    public void PipelineValidation_ReturnsErrorsForNullInputsAndCapsDiagnostics()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();

        global::yaml.YamlResult streamResult = pipeline.ValidateStream(null, null);
        global::yaml.YamlResult documentResult = pipeline.ValidateDocument(null, null);
        global::yaml.YamlResult nodeResult = pipeline.ValidateNode(null, null);

        Assert.That(streamResult.Success, Is.False);
        Assert.That(documentResult.Success, Is.False);
        Assert.That(nodeResult.Success, Is.False);
        Assert.That(HasDiagnostic(streamResult, "YAMLV001"), Is.True);
        Assert.That(HasDiagnostic(documentResult, "YAMLV001"), Is.True);
        Assert.That(HasDiagnostic(nodeResult, "YAMLV001"), Is.True);

        global::yaml.YamlNode root = Sequence(Scalar("long"), Scalar("longer"));
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.MaxScalarLength = 1;
        options.Security.MaxCollectionSize = 1;
        options.Security.MaxDiagnostics = 2;

        global::yaml.YamlResult capped = pipeline.ValidateNode(root, options);

        Assert.That(capped.Success, Is.False);
        Assert.That(capped.Diagnostics.Count, Is.EqualTo(2));
    }

    public static global::yaml.YamlNode Scalar(string value)
    {
        global::yaml.YamlNode node = new global::yaml.YamlNode();
        node.Kind = global::yaml.YamlNodeKind.Scalar;
        node.Value = value;
        return node;
    }

    public static global::yaml.YamlNode Sequence(params global::yaml.YamlNode[] children)
    {
        global::yaml.YamlNode node = new global::yaml.YamlNode();
        node.Kind = global::yaml.YamlNodeKind.Sequence;
        node.Children.AddRange(children);
        return node;
    }

    public static global::yaml.YamlNode Mapping()
    {
        global::yaml.YamlNode node = new global::yaml.YamlNode();
        node.Kind = global::yaml.YamlNodeKind.Mapping;
        return node;
    }

    public static global::yaml.YamlNode Alias(string name, global::yaml.YamlNode? target)
    {
        global::yaml.YamlNode node = new global::yaml.YamlNode();
        node.Kind = global::yaml.YamlNodeKind.Alias;
        node.AliasName = name;
        node.AliasTarget = target;
        return node;
    }

    public static global::yaml.YamlPair Pair(global::yaml.YamlNode key, global::yaml.YamlNode value)
    {
        global::yaml.YamlPair pair = new global::yaml.YamlPair();
        pair.Key = key;
        pair.Value = value;
        return pair;
    }

    public static bool HasDiagnostic(global::yaml.YamlResult result, string code)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            if (result.Diagnostics[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    public static bool HasSeverity(global::yaml.YamlResult result, string code, global::yaml.YamlDiagnosticSeverity severity)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            if (result.Diagnostics[index].Code == code && result.Diagnostics[index].Severity == severity)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
