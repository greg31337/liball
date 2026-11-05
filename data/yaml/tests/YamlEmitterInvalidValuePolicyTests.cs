using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlEmitterInvalidValuePolicyTests
{
    [Test]
    public void EmitNode_QuoteAsStringPolicySafelyQuotesInvalidPlainScalars()
    {
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        global::yaml.YamlNode scalar = Scalar("a: b", global::yaml.YamlScalarStyle.Plain);
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.InvalidValuePolicy = global::yaml.YamlInvalidValuePolicy.QuoteAsString;

        string first = pipeline.EmitNode(scalar, options);
        string second = pipeline.EmitNode(scalar, options);

        Assert.That(first, Is.EqualTo("'a: b'"));
        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void EmitNode_ErrorPolicyRejectsInvalidPlainScalarsButEmitsValidOnes()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.InvalidValuePolicy = global::yaml.YamlInvalidValuePolicy.Error;

        Assert.That(
            () => pipeline.EmitNode(Scalar("a: b", global::yaml.YamlScalarStyle.Plain), options),
            Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(pipeline.EmitNode(Scalar("valid", global::yaml.YamlScalarStyle.Plain), options), Is.EqualTo("valid"));
    }

    [Test]
    public void EmitNode_PolicyControlsBlockScalarFallbackInsideFlowCollections()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlNode sequence = new global::yaml.YamlNode();
        sequence.Kind = global::yaml.YamlNodeKind.Sequence;
        sequence.CollectionStyle = global::yaml.YamlCollectionStyle.Flow;
        sequence.Children.Add(Scalar("line\nbreak", global::yaml.YamlScalarStyle.Literal));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.InvalidValuePolicy = global::yaml.YamlInvalidValuePolicy.QuoteAsString;

        Assert.That(pipeline.EmitNode(sequence, options), Is.EqualTo("[\"line\\nbreak\"]"));

        options.InvalidValuePolicy = global::yaml.YamlInvalidValuePolicy.Error;
        Assert.That(() => pipeline.EmitNode(sequence, options), Throws.TypeOf<global::yaml.YamlException>());
    }

    [Test]
    public void EmitNode_PolicyHandlesInvalidTaggedAndNonPrintableScalarValues()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.InvalidValuePolicy = global::yaml.YamlInvalidValuePolicy.QuoteAsString;
        global::yaml.YamlNode invalidInteger = Scalar("not-an-integer", global::yaml.YamlScalarStyle.Plain);
        invalidInteger.Tag = global::yaml.YamlTags.Int;
        global::yaml.YamlNode nonPrintable = Scalar("\u0001\u0080\uD800", global::yaml.YamlScalarStyle.Plain);

        Assert.That(pipeline.EmitNode(invalidInteger, options), Is.EqualTo("'not-an-integer'"));
        Assert.That(pipeline.EmitNode(nonPrintable, options), Is.EqualTo("\"\\x01\\x80\\uFFFD\""));

        options.InvalidValuePolicy = global::yaml.YamlInvalidValuePolicy.Error;
        Assert.That(() => pipeline.EmitNode(invalidInteger, options), Throws.TypeOf<global::yaml.YamlException>());
        Assert.That(() => pipeline.EmitNode(nonPrintable, options), Throws.TypeOf<global::yaml.YamlException>());
    }

    [Test]
    public void EmitNode_RejectsDirectCyclesAndPreservesRecursiveAliasGraphs()
    {
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        global::yaml.YamlNode directCycle = new global::yaml.YamlNode();
        directCycle.Kind = global::yaml.YamlNodeKind.Sequence;
        directCycle.Children.Add(directCycle);

        Assert.That(() => pipeline.EmitNode(directCycle, options), Throws.TypeOf<global::yaml.YamlException>());

        global::yaml.YamlNode recursive = new global::yaml.YamlNode();
        recursive.Kind = global::yaml.YamlNodeKind.Sequence;
        recursive.Anchor = "self";
        global::yaml.YamlNode alias = new global::yaml.YamlNode();
        alias.Kind = global::yaml.YamlNodeKind.Alias;
        alias.AliasName = "self";
        alias.AliasTarget = recursive;
        recursive.Children.Add(alias);

        string first = pipeline.EmitNode(recursive, options);
        string second = pipeline.EmitNode(recursive, options);

        Assert.That(first, Is.EqualTo("&self\n- *self"));
        Assert.That(second, Is.EqualTo(first));
    }

    public static global::yaml.YamlNode Scalar(string value, global::yaml.YamlScalarStyle style)
    {
        global::yaml.YamlNode node = new global::yaml.YamlNode();
        node.Kind = global::yaml.YamlNodeKind.Scalar;
        node.Value = value;
        node.ScalarStyle = style;
        return node;
    }
}
