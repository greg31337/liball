using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlEmitterGoldenTests
{
    [Test]
    public void Emit_GoldenBlockDocument_CoversSupportedNodeStyles()
    {
        global::yaml.YamlDocument document = DocumentWithRoot(BuildGoldenBlockRoot());
        document.AddLeadingComment("golden document");
        document.AddDirective(global::yaml.YamlDirective.YamlVersionDirective(
            global::yaml.YamlVersion.Version12,
            "",
            global::yaml.YamlSourceSpan.Empty()));
        string expected = string.Join(
            "\n",
            new string[]
            {
                "# golden document",
                "%YAML 1.2",
                "---",
                "# root mapping",
                "&root !root",
                "# plain scalar",
                "plain: plain value # inline plain",
                "single: 'Ada''s notes'",
                "double: \"line\\nquote \\\" slash \\\\\"",
                "literal: |",
                "  line one",
                "  line two",
                "folded: >",
                "  folded line",
                "sequence: &items",
                "  - one",
                "  - *root",
                "flow: {enabled: true, empty: null, values: [null, 'true', 'a, b']}",
                "tagged: &person !person Ada",
                ""
            });

        string text = global::yaml.YamlEmitter.EmitDocument(document);

        Assert.That(text, Is.EqualTo(expected));
    }

    [Test]
    public void Emit_GoldenOptionDrivenFlowDocument_IsDeterministic()
    {
        global::yaml.YamlNode mapping = global::yaml.YamlNode.Mapping(
            global::yaml.YamlCollectionStyle.Any,
            global::yaml.YamlSourceSpan.Empty());
        mapping.AddMappingValue("zeta", AnyScalar("last"));
        mapping.AddMappingValue("alpha", AnyScalar("first"));
        mapping.AddMappingValue("bravo", AnyScalar("second"));
        global::yaml.YamlEmitterOptions options = new global::yaml.YamlEmitterOptions();
        options.DefaultCollectionStyle = global::yaml.YamlCollectionStyle.Flow;
        options.DefaultScalarStyle = global::yaml.YamlScalarStyle.DoubleQuoted;
        options.SortStringKeys = true;
        options.LineWidth = 20;
        options.Indent = 4;
        options.WriteDocumentStart = true;
        options.WriteDocumentEnd = true;
        string expected = string.Join(
            "\n",
            new string[]
            {
                "---",
                "{",
                "    alpha: \"first\",",
                "    bravo: \"second\",",
                "    zeta: \"last\"",
                "}",
                "...",
                ""
            });

        string text = global::yaml.YamlEmitter.EmitDocument(DocumentWithRoot(mapping), options);

        Assert.That(text, Is.EqualTo(expected));
    }

    static global::yaml.YamlNode BuildGoldenBlockRoot()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        root.Anchor = "root";
        root.Tag = "!root";
        root.AddLeadingComment("root mapping");

        global::yaml.YamlPair plain = global::yaml.YamlNodeFactory.Pair(
            "plain",
            global::yaml.YamlNodeFactory.Scalar("plain value"));
        plain.AddLeadingComment("plain scalar");
        plain.AddTrailingComment("inline plain");
        root.AddMappingPair(plain);

        root.AddMappingValue("single", global::yaml.YamlNodeFactory.SingleQuotedScalar("Ada's notes"));
        root.AddMappingValue("double", global::yaml.YamlNodeFactory.DoubleQuotedScalar("line\nquote \" slash \\"));
        root.AddMappingValue("literal", global::yaml.YamlNodeFactory.LiteralScalar("line one\nline two\n"));
        root.AddMappingValue("folded", global::yaml.YamlNodeFactory.FoldedScalar("folded line\n"));

        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.Sequence();
        sequence.Anchor = "items";
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("one"));
        sequence.AddSequenceItem(global::yaml.YamlNodeFactory.AliasTo("root", root));
        root.AddMappingValue("sequence", sequence);

        global::yaml.YamlNode values = global::yaml.YamlNodeFactory.FlowSequence();
        values.AddSequenceItem(global::yaml.YamlNodeFactory.FromJsonValue(null));
        values.AddSequenceItem(global::yaml.YamlNodeFactory.FromJsonValue("true"));
        values.AddSequenceItem(global::yaml.YamlNodeFactory.Scalar("a, b"));
        global::yaml.YamlNode flow = global::yaml.YamlNodeFactory.FlowMapping();
        flow.AddMappingValue("enabled", global::yaml.YamlNodeFactory.FromJsonValue(true));
        flow.AddMappingValue("empty", global::yaml.YamlNodeFactory.Empty());
        flow.AddMappingValue("values", values);
        root.AddMappingValue("flow", flow);

        global::yaml.YamlNode tagged = global::yaml.YamlNodeFactory.Scalar("Ada");
        tagged.Anchor = "person";
        tagged.Tag = "!person";
        root.AddMappingValue("tagged", tagged);

        return root;
    }

    static global::yaml.YamlNode AnyScalar(string value)
    {
        return global::yaml.YamlNode.Scalar(
            value,
            global::yaml.YamlScalarStyle.Any,
            global::yaml.YamlSourceSpan.Empty());
    }

    static global::yaml.YamlDocument DocumentWithRoot(global::yaml.YamlNode root)
    {
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        document.SetRoot(root);
        return document;
    }
}
