using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class FootnoteDefinitionMapTests
{
    [Test]
    public void FootnoteDefinitionMap_NormalizesLabelsForLookup()
    {
        MarkdownFootnoteDefinitionMap map = new MarkdownFootnoteDefinitionMap();
        MarkdownNode definition = CreateDefinition(" A \t B ");

        Assert.That(map.Add(definition), Is.True);

        Assert.That(map.GetCount(), Is.EqualTo(1));
        Assert.That(map.Find("a b"), Is.SameAs(definition));
        Assert.That(map.Find("A   B"), Is.SameAs(definition));
        Assert.That(map.Contains("a\tb"), Is.True);
        Assert.That(MarkdownFootnoteDefinitionMap.NormalizeLabel("  A\t\tB  "), Is.EqualTo("a b"));
    }

    [Test]
    public void FootnoteDefinitionMap_KeepsFirstDefinitionForDuplicateLabels()
    {
        MarkdownFootnoteDefinitionMap map = new MarkdownFootnoteDefinitionMap();
        MarkdownNode first = CreateDefinition("ref");
        MarkdownNode second = CreateDefinition(" REF ");

        Assert.That(map.Add(first), Is.True);
        Assert.That(map.Add(second), Is.False);

        Assert.That(map.GetCount(), Is.EqualTo(1));
        Assert.That(map.Find("ref"), Is.SameAs(first));
    }

    [Test]
    public void FootnoteDefinitionMap_AddsMissingNormalizedLabelAttribute()
    {
        MarkdownFootnoteDefinitionMap map = new MarkdownFootnoteDefinitionMap();
        MarkdownNode definition = MarkdownNode.Create(MarkdownNodeKind.FootnoteDefinition);
        definition.SetAttribute("label", "Ref");

        Assert.That(map.Add(definition), Is.True);

        Assert.That(definition.GetAttributeValue("normalizedLabel"), Is.EqualTo("ref"));
        Assert.That(map.FindNormalized("ref"), Is.SameAs(definition));
    }

    [Test]
    public void FootnoteDefinitionMap_ReturnsDefinitionsAndClears()
    {
        MarkdownFootnoteDefinitionMap map = new MarkdownFootnoteDefinitionMap();
        MarkdownNode definition = CreateDefinition("ref");

        map.Add(definition);
        MarkdownNode[] definitions = map.GetDefinitions();

        Assert.That(definitions.Length, Is.EqualTo(1));
        Assert.That(definitions[0], Is.SameAs(definition));
        Assert.That(map.IsEmpty(), Is.False);

        map.Clear();

        Assert.That(map.IsEmpty(), Is.True);
        Assert.That(map.Find("ref"), Is.Null);
    }

    [Test]
    public void FootnoteDefinitionMap_RejectsInvalidArguments()
    {
        MarkdownFootnoteDefinitionMap map = new MarkdownFootnoteDefinitionMap();

        Assert.That(() => map.Add(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.Add(MarkdownNode.Create(MarkdownNodeKind.Paragraph)), Throws.TypeOf<ArgumentException>());
        Assert.That(() => map.Find(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.FindNormalized(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownFootnoteDefinitionMap.NormalizeLabel(null!), Throws.TypeOf<ArgumentNullException>());
    }

    static MarkdownNode CreateDefinition(string label)
    {
        MarkdownNode definition = MarkdownNode.Create(MarkdownNodeKind.FootnoteDefinition);
        definition.SetAttribute("label", label);
        definition.SetAttribute("normalizedLabel", MarkdownFootnoteDefinitionMap.NormalizeLabel(label));
        return definition;
    }
}
