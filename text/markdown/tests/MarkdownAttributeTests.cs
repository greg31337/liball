using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownAttributeTests
{
    [Test]
    public void Attribute_StoresNameAndValue()
    {
        MarkdownAttribute attribute = MarkdownAttribute.Create("id", "heading");

        Assert.That(attribute.Name, Is.EqualTo("id"));
        Assert.That(attribute.Value, Is.EqualTo("heading"));
    }

    [Test]
    public void Attribute_CopyCreatesIndependentValue()
    {
        MarkdownAttribute attribute = MarkdownAttribute.Create("class", "lead");
        MarkdownAttribute copy = attribute.Copy();

        attribute.SetValue("mutated");

        Assert.That(copy.Name, Is.EqualTo("class"));
        Assert.That(copy.Value, Is.EqualTo("lead"));
    }

    [Test]
    public void Attribute_RejectsInvalidValues()
    {
        MarkdownAttribute attribute = new MarkdownAttribute();

        Assert.That(() => MarkdownAttribute.Create(null!, "value"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAttribute.Create("", "value"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownAttribute.Create("name", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => attribute.SetValue(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAttribute.ValidateName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAttribute.ValidateName(""), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Node_SetAttributeAddsAndUpdatesByName()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Heading);

        node.SetAttribute("id", "intro");
        node.SetAttribute("class", "lead");
        node.SetAttribute("id", "intro-2");

        Assert.That(node.HasAttributes(), Is.True);
        Assert.That(node.GetAttributeCount(), Is.EqualTo(2));
        Assert.That(node.HasAttribute("id"), Is.True);
        Assert.That(node.GetAttributeValue("id"), Is.EqualTo("intro-2"));
        Assert.That(node.GetAttributeValue("class"), Is.EqualTo("lead"));
        Assert.That(node.GetAttributeValue("missing"), Is.EqualTo(""));
    }

    [Test]
    public void Node_SetAttributeCopiesAttributeInput()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Heading);
        MarkdownAttribute attribute = MarkdownAttribute.Create("id", "intro");

        node.SetAttributeCopy(attribute);
        attribute.SetValue("mutated");

        Assert.That(node.GetAttributeValue("id"), Is.EqualTo("intro"));
    }

    [Test]
    public void Node_GetAttributeAndGetAttributesReturnCopies()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Heading);

        node.SetAttribute("id", "intro");
        MarkdownAttribute? attribute = node.GetAttribute("id");
        MarkdownAttribute[] attributes = node.GetAttributes();

        attribute!.SetValue("mutated");
        attributes[0].SetValue("also-mutated");

        Assert.That(node.GetAttributeValue("id"), Is.EqualTo("intro"));
    }

    [Test]
    public void Node_AttributeNamesAreCaseSensitive()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Heading);

        node.SetAttribute("id", "lower");
        node.SetAttribute("ID", "upper");

        Assert.That(node.GetAttributeCount(), Is.EqualTo(2));
        Assert.That(node.GetAttributeValue("id"), Is.EqualTo("lower"));
        Assert.That(node.GetAttributeValue("ID"), Is.EqualTo("upper"));
    }

    [Test]
    public void Node_RemoveAndClearAttributes()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Heading);

        node.SetAttribute("id", "intro");
        node.SetAttribute("class", "lead");

        Assert.That(node.RemoveAttribute("id"), Is.True);
        Assert.That(node.RemoveAttribute("id"), Is.False);
        Assert.That(node.GetAttributeCount(), Is.EqualTo(1));

        node.ClearAttributes();

        Assert.That(node.GetAttributeCount(), Is.EqualTo(0));
        Assert.That(node.HasAttributes(), Is.False);
    }

    [Test]
    public void Node_AttributeOperationsRejectInvalidArguments()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Heading);

        Assert.That(() => node.IndexOfAttribute(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => node.IndexOfAttribute(""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => node.SetAttribute(null!, "value"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => node.SetAttribute("name", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => node.SetAttributeCopy((MarkdownAttribute)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => node.GetAttribute(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => node.GetAttributeValue(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => node.RemoveAttribute(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
