using System;
using htmlgui;

namespace htmlgui.Tests;

public class AccessibilityMetadataTests
{
    [Test]
    public void ResolvesInputNameDescriptionAndErrorRelationshipsOnTheNativeControl()
    {
        UiNode node = Control("text-input");
        node.Input = new UiInput
        {
            Kind = UiInputKind.Text,
            Name = "account",
            Value = new UiValue { Kind = UiValueKind.String, StringValue = "Ada" },
            HelperText = "Use your account name",
            ErrorText = "The account is unavailable"
        };
        node.Accessibility = new UiAccessibility
        {
            Label = "Ignored direct name",
            Description = "Ignored direct description",
            Relationships = new UiAriaRelationship[]
            {
                new UiAriaRelationship { Name = AriaAttributes.LabelledBy, TargetIds = new string[] { "account-label" } },
                new UiAriaRelationship { Name = AriaAttributes.DescribedBy, TargetIds = new string[] { "external-help" } },
                new UiAriaRelationship { Name = AriaAttributes.ErrorMessage, TargetIds = new string[] { "server-error" } }
            }
        };

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode input = result.Children[0];
        string helperId = result.Children[1].Id;
        string errorId = result.Children[2].Id;

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(result, AriaAttributes.LabelledBy), Is.Null);
            Assert.That(input.Name, Is.EqualTo(HtmlElements.Input));
            Assert.That(AttributeValue(input, AriaAttributes.Label), Is.Null);
            Assert.That(AttributeValue(input, AriaAttributes.LabelledBy), Is.EqualTo("account-label"));
            Assert.That(AttributeValue(input, AriaAttributes.Description), Is.Null);
            Assert.That(AttributeValue(input, AriaAttributes.DescribedBy), Is.EqualTo(helperId + " " + errorId + " external-help server-error"));
            Assert.That(AttributeValue(input, AriaAttributes.ErrorMessage), Is.EqualTo(errorId + " server-error"));
            Assert.That(AttributeValue(input, AriaAttributes.Invalid), Is.EqualTo("true"));
        });
    }

    [TestCase(AriaRoles.Banner, HtmlElements.Header)]
    [TestCase(AriaRoles.Complementary, HtmlElements.Aside)]
    [TestCase(AriaRoles.ContentInfo, HtmlElements.Footer)]
    [TestCase(AriaRoles.Form, HtmlElements.Form)]
    [TestCase(AriaRoles.Main, HtmlElements.Main)]
    [TestCase(AriaRoles.Navigation, HtmlElements.Navigation)]
    [TestCase(AriaRoles.Region, HtmlElements.Section)]
    [TestCase(AriaRoles.Search, HtmlElements.Search)]
    public void SelectsNativeLandmarkElementsFromExplicitMetadata(string role, string elementName)
    {
        UiNode node = Control("landmark");
        node.TextContent = new UiText { Kind = UiTextKind.Landmark, Text = "Landmark content" };
        node.Accessibility = new UiAccessibility { Role = role, Label = "Landmark label" };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(elementName));
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.Null);
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.EqualTo("Landmark label"));
            Assert.That(result.Children[0].Text, Is.EqualTo("Landmark content"));
        });
    }

    [Test]
    public void UsesNativeHeadingLevelsAndAriaMetadataOnlyWhenNativeHeadingIsUnavailable()
    {
        UiNode native = Control("heading");
        native.TextContent = new UiText { Kind = UiTextKind.Heading, Text = "Native heading" };
        native.Accessibility = new UiAccessibility
        {
            Role = AriaRoles.Heading,
            HasHeadingLevel = true,
            HeadingLevel = 4
        };
        UiNode overridden = Control("button");
        overridden.Button = new UiButton { Label = "Heading action" };
        overridden.Accessibility = new UiAccessibility
        {
            Role = AriaRoles.Heading,
            HasHeadingLevel = true,
            HeadingLevel = 3
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode nativeResult = expander.ExpandNode(native);
        DomNode overriddenResult = expander.ExpandNode(overridden);

        Assert.Multiple(() =>
        {
            Assert.That(nativeResult.Name, Is.EqualTo(HtmlElements.Heading4));
            Assert.That(AttributeValue(nativeResult, HtmlAttributes.Role), Is.Null);
            Assert.That(AttributeValue(nativeResult, AriaAttributes.Level), Is.Null);
            Assert.That(overriddenResult.Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(overriddenResult, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Heading));
            Assert.That(AttributeValue(overriddenResult, AriaAttributes.Level), Is.EqualTo("3"));
        });
    }

    [Test]
    public void UsesNativeAlternativeTextAsTheImageNameInput()
    {
        UiNode node = Control("image");
        node.TextContent = new UiText
        {
            Kind = UiTextKind.Image,
            Text = "Fallback",
            Url = "/portrait.png",
            AlternativeText = "Original description"
        };
        node.Accessibility = new UiAccessibility { Label = "Updated portrait description" };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Image));
            Assert.That(AttributeValue(result, HtmlAttributes.AlternativeText), Is.EqualTo("Updated portrait description"));
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.Null);
        });
    }

    [Test]
    public void EmitsNodeAndAccessibilityAnnouncementsAlongsideInteractiveControls()
    {
        UiNode node = Control("button");
        node.Button = new UiButton { Label = "Save" };
        node.State.Version = 7;
        node.Announcements = new UiLiveAnnouncement[]
        {
            new UiLiveAnnouncement { Id = "saved", Message = "Saved", Mode = UiAriaLiveMode.Polite }
        };
        node.Accessibility = new UiAccessibility
        {
            Announcements = new UiLiveAnnouncement[]
            {
                new UiLiveAnnouncement { Id = "failed", Message = "Save failed", Mode = UiAriaLiveMode.Assertive, Atomic = true }
            }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);
        DomNode polite = first.Children[1];
        DomNode assertive = first.Children[2];

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(first.Children[0].Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(first.Children[0].Children[0].Text, Is.EqualTo("Save"));
            Assert.That(polite.Key, Is.EqualTo("saved"));
            Assert.That(AttributeValue(polite, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Status));
            Assert.That(AttributeValue(polite, AriaAttributes.Live), Is.EqualTo("polite"));
            Assert.That(AttributeValue(polite, AriaAttributes.Atomic), Is.EqualTo("false"));
            Assert.That(polite.Children[0].Text, Is.EqualTo("Saved"));
            Assert.That(assertive.Key, Is.EqualTo("failed"));
            Assert.That(AttributeValue(assertive, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Alert));
            Assert.That(AttributeValue(assertive, AriaAttributes.Live), Is.EqualTo("assertive"));
            Assert.That(AttributeValue(assertive, AriaAttributes.Atomic), Is.EqualTo("true"));
            Assert.That(assertive.Children[0].Text, Is.EqualTo("Save failed"));
            Assert.That(second.Children[1].Id, Is.EqualTo(polite.Id));
            Assert.That(second.Children[2].Id, Is.EqualTo(assertive.Id));
            Assert.That(assertive.Version, Is.EqualTo(7));
        });
    }

    [Test]
    public void DoesNotDuplicateAnnouncementsAlreadyExpandedByFeedbackControls()
    {
        UiNode node = Control("status");
        node.Feedback = new UiFeedback { Kind = "status", Message = "Working" };
        node.Announcements = new UiLiveAnnouncement[]
        {
            new UiLiveAnnouncement { Id = "complete", Message = "Complete" }
        };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(result.Children, Has.Length.EqualTo(2));
            Assert.That(result.Children[1].Key, Is.EqualTo("complete"));
        });
    }

    [Test]
    public void RejectsInvalidHeadingLevelsAndDuplicateAnnouncementIdentifiers()
    {
        UiNode heading = Control("heading");
        heading.TextContent = new UiText { Kind = UiTextKind.Heading, Text = "Invalid" };
        heading.Accessibility = new UiAccessibility { HasHeadingLevel = true, HeadingLevel = 7 };
        UiNode duplicate = Control("button");
        duplicate.Button = new UiButton { Label = "Save" };
        duplicate.Announcements = new UiLiveAnnouncement[]
        {
            new UiLiveAnnouncement { Id = "same", Message = "First" }
        };
        duplicate.Accessibility = new UiAccessibility
        {
            Announcements = new UiLiveAnnouncement[]
            {
                new UiLiveAnnouncement { Id = "same", Message = "Second" }
            }
        };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(heading));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicate));
        });
    }

    private static UiNode Control(string name)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = name,
            Identity = new UiNodeIdentity
            {
                AuthorKey = name,
                SiblingIdentity = name,
                StableId = name + "-id"
            }
        };
    }

    private static string? AttributeValue(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index].Value;
            }

            index++;
        }

        return null;
    }
}
