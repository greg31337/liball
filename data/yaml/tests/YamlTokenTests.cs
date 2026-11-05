using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlTokenTests
{
    [Test]
    public void Constructors_DefaultAndNullInputsUseStableEmptyValues()
    {
        global::yaml.YamlToken empty = new global::yaml.YamlToken();
        global::yaml.YamlToken normalized = global::yaml.YamlToken.Create(global::yaml.YamlTokenKind.PlainScalar, null, 3, null);

        Assert.That(empty.Kind, Is.EqualTo(global::yaml.YamlTokenKind.Unknown));
        Assert.That(empty.Value, Is.EqualTo(""));
        Assert.That(empty.Header, Is.EqualTo(""));
        Assert.That(empty.Indent, Is.EqualTo(0));
        Assert.That(empty.Span.IsEmpty(), Is.True);
        Assert.That(normalized.Kind, Is.EqualTo(global::yaml.YamlTokenKind.PlainScalar));
        Assert.That(normalized.Value, Is.EqualTo(""));
        Assert.That(normalized.Header, Is.EqualTo(""));
        Assert.That(normalized.Indent, Is.EqualTo(3));
        Assert.That(normalized.Span.IsEmpty(), Is.True);
    }

    [Test]
    public void Factories_CreateScalarIndentAndBlockScalarTokens()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.FromBounds(4, 2, 5, 9, 2, 10);

        global::yaml.YamlToken scalar = global::yaml.YamlToken.Create(global::yaml.YamlTokenKind.PlainScalar, "value", span);
        global::yaml.YamlToken indent = global::yaml.YamlToken.CreateIndent(global::yaml.YamlTokenKind.Indent, 4, span);
        global::yaml.YamlToken block = global::yaml.YamlToken.CreateBlockScalar(global::yaml.YamlTokenKind.LiteralScalar, "|-", "text", 2, span);

        Assert.That(scalar.Kind, Is.EqualTo(global::yaml.YamlTokenKind.PlainScalar));
        Assert.That(scalar.Value, Is.EqualTo("value"));
        Assert.That(scalar.Indent, Is.EqualTo(0));
        Assert.That(scalar.Span, Is.SameAs(span));
        Assert.That(indent.Kind, Is.EqualTo(global::yaml.YamlTokenKind.Indent));
        Assert.That(indent.Value, Is.EqualTo("4"));
        Assert.That(indent.Indent, Is.EqualTo(4));
        Assert.That(block.Kind, Is.EqualTo(global::yaml.YamlTokenKind.LiteralScalar));
        Assert.That(block.Header, Is.EqualTo("|-"));
        Assert.That(block.Value, Is.EqualTo("text"));
        Assert.That(block.Indent, Is.EqualTo(2));
        Assert.That(block.Span, Is.SameAs(span));
    }
}
