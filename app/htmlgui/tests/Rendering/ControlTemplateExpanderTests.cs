using System;
using htmlgui;

namespace htmlgui.Tests;

public class ControlTemplateExpanderTests
{
    [Test]
    public void ExpandsSpacerAndSeparatorToNativePresentationalElements()
    {
        UiNode ignoredChild = TextNode("ignored", "ignored-id", "Ignored");
        UiNode spacer = ControlNode("spacer", "space", "space-id");
        spacer.Children = new UiNode[] { ignoredChild };
        UiNode separator = ControlNode("separator", "rule", "rule-id");
        separator.Children = new UiNode[] { ignoredChild };
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedSpacer = expander.ExpandNode(spacer);
        DomNode expandedSeparator = expander.ExpandNode(separator);

        Assert.Multiple(() =>
        {
            Assert.That(expandedSpacer.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(expandedSpacer, HtmlAttributes.Class), Is.EqualTo("htmlgui-spacer"));
            Assert.That(AttributeValue(expandedSpacer, AriaAttributes.Hidden), Is.EqualTo("true"));
            Assert.That(expandedSpacer.Children, Is.Empty);
            Assert.That(expandedSeparator.Name, Is.EqualTo(HtmlElements.HorizontalRule));
            Assert.That(AttributeValue(expandedSeparator, HtmlAttributes.Class), Is.EqualTo("htmlgui-separator"));
            Assert.That(expandedSeparator.Children, Is.Empty);
        });
    }

    [Test]
    public void ExpandsCardGroupBoxAndFieldSetWithVisibleAccessibleLabels()
    {
        UiNode child = TextNode("content", "content-id", "Content");
        UiNode card = ControlNode("card", "card", "card-id");
        card.Text = "Card title";
        card.Children = new UiNode[] { child };
        UiNode groupBox = ControlNode("group-box", "group", "group-id");
        groupBox.Text = "Group title";
        groupBox.Children = new UiNode[] { child };
        UiNode fieldSet = ControlNode("field-set", "fields", "fields-id");
        fieldSet.Text = "Field title";
        fieldSet.Children = new UiNode[] { child };
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedCard = expander.ExpandNode(card);
        DomNode expandedGroupBox = expander.ExpandNode(groupBox);
        DomNode expandedFieldSet = expander.ExpandNode(fieldSet);

        Assert.Multiple(() =>
        {
            Assert.That(expandedCard.Name, Is.EqualTo(HtmlElements.Article));
            Assert.That(expandedCard.Children[0].Name, Is.EqualTo(HtmlElements.Header));
            Assert.That(expandedCard.Children[0].Children[0].Text, Is.EqualTo("Card title"));
            Assert.That(AttributeValue(expandedCard, AriaAttributes.LabelledBy), Is.EqualTo(expandedCard.Children[0].Id));
            Assert.That(expandedCard.Children[1].Text, Is.EqualTo("Content"));

            Assert.That(expandedGroupBox.Name, Is.EqualTo(HtmlElements.Section));
            Assert.That(AttributeValue(expandedGroupBox, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Group));
            Assert.That(AttributeValue(expandedGroupBox, AriaAttributes.LabelledBy), Is.EqualTo(expandedGroupBox.Children[0].Id));
            Assert.That(expandedGroupBox.Children[0].Children[0].Text, Is.EqualTo("Group title"));
            Assert.That(expandedGroupBox.Children[1].Text, Is.EqualTo("Content"));

            Assert.That(expandedFieldSet.Name, Is.EqualTo(HtmlElements.FieldSet));
            Assert.That(expandedFieldSet.Children[0].Name, Is.EqualTo(HtmlElements.Legend));
            Assert.That(expandedFieldSet.Children[0].Children[0].Text, Is.EqualTo("Field title"));
            Assert.That(expandedFieldSet.Children[1].Text, Is.EqualTo("Content"));
        });
    }

    [Test]
    public void ExpandsNativeDisclosureFromItsDisclosurePayload()
    {
        UiNode node = ControlNode("disclosure", "details", "details-id");
        node.State.Version = 7;
        node.Children = new UiNode[] { TextNode("ignored", "ignored-id", "Logical child") };
        node.Disclosure = new UiDisclosure
        {
            Key = "details-payload",
            Summary = "More information",
            Content = new UiNode[] { TextNode("body", "body-id", "Disclosure body") },
            IsExpanded = true,
            IsDisabled = true
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Details));
            Assert.That(result.Key, Is.EqualTo("details"));
            Assert.That(result.Id, Is.EqualTo("details-id"));
            Assert.That(result.Version, Is.EqualTo(7));
            Assert.That(FindAttribute(result, HtmlAttributes.Open).IsBoolean, Is.True);
            Assert.That(AttributeValue(result, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(result.Children, Has.Length.EqualTo(2));
            Assert.That(result.Children[0].Name, Is.EqualTo(HtmlElements.Summary));
            Assert.That(result.Children[0].Children[0].Text, Is.EqualTo("More information"));
            Assert.That(result.Children[1].Text, Is.EqualTo("Disclosure body"));
        });
    }

    [Test]
    public void ExpandsCollapsibleRegionWithStableToggleAndRegionRelationships()
    {
        UiNode node = ControlNode("collapsible-region", "filters", "filters-id");
        node.Disclosure = new UiDisclosure
        {
            Key = "filter-details",
            Summary = "Filters",
            Content = new UiNode[] { TextNode("filter-content", "filter-content-id", "Filter choices") },
            IsDisabled = true
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode collapsed = expander.ExpandNode(node);
        node.Disclosure.IsExpanded = true;
        DomNode expanded = expander.ExpandNode(node);
        DomNode collapsedToggle = collapsed.Children[0];
        DomNode collapsedRegion = collapsed.Children[1];
        DomNode expandedToggle = expanded.Children[0];
        DomNode expandedRegion = expanded.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(collapsed.Name, Is.EqualTo(HtmlElements.Section));
            Assert.That(collapsedToggle.Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(collapsedToggle, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.Button));
            Assert.That(AttributeValue(collapsedToggle, AriaAttributes.Expanded), Is.EqualTo("false"));
            Assert.That(AttributeValue(collapsedToggle, AriaAttributes.Controls), Is.EqualTo(collapsedRegion.Id));
            Assert.That(FindAttribute(collapsedToggle, HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(collapsedToggle.Children[0].Text, Is.EqualTo("Filters"));
            Assert.That(AttributeValue(collapsedRegion, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Region));
            Assert.That(AttributeValue(collapsedRegion, AriaAttributes.LabelledBy), Is.EqualTo(collapsedToggle.Id));
            Assert.That(FindAttribute(collapsedRegion, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(collapsedRegion.Children[0].Text, Is.EqualTo("Filter choices"));
            Assert.That(expandedToggle.Id, Is.EqualTo(collapsedToggle.Id));
            Assert.That(expandedRegion.Id, Is.EqualTo(collapsedRegion.Id));
            Assert.That(AttributeValue(expandedToggle, AriaAttributes.Expanded), Is.EqualTo("true"));
            Assert.That(HasAttribute(expandedRegion, HtmlAttributes.Hidden), Is.False);
        });
    }

    [Test]
    public void ExpandsResizablePanelWithCanonicalOrientationAndOverflow()
    {
        UiNode horizontal = ControlNode("resizable-panel", "horizontal", "horizontal-id");
        horizontal.Children = new UiNode[] { TextNode("content", "content-id", "Panel") };
        UiNode vertical = ControlNode("resizable-panel", "vertical", "vertical-id");
        vertical.Layout = new UiLayout { ResizeOrientation = UiOrientation.Vertical };
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedHorizontal = expander.ExpandNode(horizontal);
        DomNode expandedVertical = expander.ExpandNode(vertical);

        Assert.Multiple(() =>
        {
            Assert.That(expandedHorizontal.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(expandedHorizontal, HtmlAttributes.Class), Is.EqualTo("htmlgui-resizable-panel"));
            Assert.That(AttributeValue(expandedHorizontal, "data-resize"), Is.EqualTo("horizontal"));
            Assert.That(AttributeValue(expandedHorizontal, HtmlAttributes.Style), Is.EqualTo("overflow:auto;resize:horizontal;"));
            Assert.That(expandedHorizontal.Children[0].Text, Is.EqualTo("Panel"));
            Assert.That(AttributeValue(expandedVertical, "data-resize"), Is.EqualTo("vertical"));
            Assert.That(AttributeValue(expandedVertical, HtmlAttributes.Style), Is.EqualTo("overflow:auto;resize:vertical;"));
        });
    }

    [Test]
    public void PreservesUnknownControlsAsKeyedFragmentsAndReturnsFreshTrees()
    {
        UiNode node = ControlNode("future-control", "future", "future-id");
        node.Children = new UiNode[] { TextNode("body", "body-id", "Body") };
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);
        first.Children[0].Text = "Changed";

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(first.Key, Is.EqualTo("future"));
            Assert.That(first.Id, Is.EqualTo("future-id"));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(first.Children[0], Is.Not.SameAs(second.Children[0]));
            Assert.That(second.Children[0].Text, Is.EqualTo("Body"));
            Assert.That(node.Children[0].Text, Is.EqualTo("Body"));
        });
    }

    [Test]
    public void RejectsMissingDisclosurePayloadsAndUndefinedResizeOrientations()
    {
        UiNode missingNative = ControlNode("disclosure", "native", "native-id");
        UiNode missingCollapsible = ControlNode("collapsible-region", "collapsible", "collapsible-id");
        UiNode invalidResize = ControlNode("resizable-panel", "resize", "resize-id");
        invalidResize.Layout = new UiLayout { ResizeOrientation = (UiOrientation)999 };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingNative))!.NodeId, Is.EqualTo("native-id"));
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingCollapsible))!.NodeId, Is.EqualTo("collapsible-id"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidResize))!.ParamName, Is.EqualTo("resizeOrientation"));
        });
    }

    private static UiNode ControlNode(string name, string key, string id)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = name,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id }
        };
    }

    private static UiNode TextNode(string key, string id, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Text = text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id }
        };
    }

    private static DomAttribute FindAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index];
            }

            index++;
        }

        throw new AssertionException("Attribute was not found: " + name);
    }

    private static string AttributeValue(DomNode node, string name)
    {
        return FindAttribute(node, name).Value;
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
