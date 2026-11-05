using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class LinkReferenceMapTests
{
    [Test]
    public void LinkReferenceMap_NormalizesLabelsForLookup()
    {
        MarkdownLinkReferenceMap map = new MarkdownLinkReferenceMap();
        MarkdownNode definition = CreateDefinition(" A \t B ", "/url");

        Assert.That(map.Add(definition), Is.True);

        Assert.That(map.GetCount(), Is.EqualTo(1));
        Assert.That(map.Find("a b"), Is.SameAs(definition));
        Assert.That(map.Find("A   B"), Is.SameAs(definition));
        Assert.That(map.Contains("a\tb"), Is.True);
        Assert.That(MarkdownLinkReferenceMap.NormalizeLabel("  A\t\tB  "), Is.EqualTo("a b"));
    }

    [Test]
    public void LinkReferenceMap_KeepsFirstDefinitionForDuplicateLabels()
    {
        MarkdownLinkReferenceMap map = new MarkdownLinkReferenceMap();
        MarkdownNode first = CreateDefinition("ref", "/first");
        MarkdownNode second = CreateDefinition(" REF ", "/second");

        Assert.That(map.Add(first), Is.True);
        Assert.That(map.Add(second), Is.False);

        Assert.That(map.GetCount(), Is.EqualTo(1));
        Assert.That(map.Find("ref"), Is.SameAs(first));
        Assert.That(map.Find("ref")!.GetAttributeValue("destination"), Is.EqualTo("/first"));
    }

    [Test]
    public void LinkReferenceMap_AddsMissingNormalizedLabelAttribute()
    {
        MarkdownLinkReferenceMap map = new MarkdownLinkReferenceMap();
        MarkdownNode definition = MarkdownNode.Create(MarkdownNodeKind.LinkReferenceDefinition);
        definition.SetAttribute("label", "Ref");
        definition.SetAttribute("destination", "/url");

        Assert.That(map.Add(definition), Is.True);

        Assert.That(definition.GetAttributeValue("normalizedLabel"), Is.EqualTo("ref"));
        Assert.That(map.FindNormalized("ref"), Is.SameAs(definition));
    }

    [Test]
    public void LinkReferenceMap_ReturnsDefinitionsAndClears()
    {
        MarkdownLinkReferenceMap map = new MarkdownLinkReferenceMap();
        MarkdownNode definition = CreateDefinition("ref", "/url");

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
    public void LinkReferenceMap_RejectsInvalidArguments()
    {
        MarkdownLinkReferenceMap map = new MarkdownLinkReferenceMap();

        Assert.That(() => map.Add(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.Add(MarkdownNode.Create(MarkdownNodeKind.Paragraph)), Throws.TypeOf<ArgumentException>());
        Assert.That(() => map.Find(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.FindNormalized(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownLinkReferenceMap.NormalizeLabel(null!), Throws.TypeOf<ArgumentNullException>());
    }

    static MarkdownNode CreateDefinition(string label, string destination)
    {
        MarkdownNode definition = MarkdownNode.Create(MarkdownNodeKind.LinkReferenceDefinition);
        definition.SetAttribute("label", label);
        definition.SetAttribute("normalizedLabel", MarkdownLinkReferenceMap.NormalizeLabel(label));
        definition.SetAttribute("destination", destination);
        return definition;
    }
}
