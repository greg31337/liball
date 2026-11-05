using System;
using htmlgui;

namespace htmlgui.Tests;

public class AccessibilityProjectorTests
{
    [TestCase(HtmlElements.Button, null, AriaRoles.Button)]
    [TestCase(HtmlElements.Input, HtmlInputTypes.Checkbox, AriaRoles.Checkbox)]
    [TestCase(HtmlElements.Input, HtmlInputTypes.Search, AriaRoles.SearchBox)]
    [TestCase(HtmlElements.TextArea, null, AriaRoles.TextBox)]
    [TestCase(HtmlElements.Navigation, null, AriaRoles.Navigation)]
    [TestCase(HtmlElements.Table, null, AriaRoles.Table)]
    [TestCase(HtmlElements.Progress, null, AriaRoles.ProgressBar)]
    [TestCase(HtmlElements.Dialog, null, AriaRoles.Dialog)]
    [TestCase(HtmlElements.Details, null, AriaRoles.Group)]
    public void OmitsRolesAlreadyProvidedByNativeElements(string elementName, string? inputType, string role)
    {
        UiNode source = Control("native");
        source.Accessibility = new UiAccessibility { Role = role };
        DomNode rendered = new DomNode
        {
            Kind = DomNodeKind.Element,
            Name = elementName,
            Attributes = inputType == null
                ? new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Role, Value = role } }
                : new DomAttribute[]
                {
                    new DomAttribute { Name = HtmlAttributes.Role, Value = role },
                    new DomAttribute { Name = HtmlAttributes.Type, Value = inputType }
                }
        };

        DomNode result = new AccessibilityProjector().ProjectControl(source, rendered);

        Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.Null);
    }

    [Test]
    public void ProjectsSharedStatePropertiesAndRelationshipsOntoAControl()
    {
        UiNode node = Control("button");
        node.Button = new UiButton { Kind = UiButtonKind.Push, Label = "Save" };
        node.State = new UiNodeState
        {
            IsVisible = false,
            IsEnabled = false,
            IsBusy = true,
            HasTabIndex = true,
            TabIndex = -1,
            HasPressed = true,
            IsPressed = true,
            HasSelected = true,
            IsSelected = false,
            HasExpanded = true,
            IsExpanded = true,
            HasCheckState = true,
            CheckState = UiCommandCheckState.Mixed,
            HasCurrentKind = true,
            CurrentKind = UiAriaCurrentKind.Page
        };
        node.Accessibility = new UiAccessibility
        {
            Role = AriaRoles.Button,
            Label = "Save changes",
            Description = "Stores the current draft",
            LiveMode = UiAriaLiveMode.Polite,
            HasCurrentKind = true,
            CurrentKind = UiAriaCurrentKind.Step,
            IsHidden = true,
            Relationships = new UiAriaRelationship[]
            {
                new UiAriaRelationship { Name = AriaAttributes.Controls, TargetIds = new string[] { "panel", "preview", "panel" } },
                new UiAriaRelationship { Name = AriaAttributes.DescribedBy, TargetIds = new string[] { "save-help" } },
                new UiAriaRelationship { Name = AriaAttributes.FlowTo, TargetIds = new string[] { "next-action" } }
            }
        };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.Null);
            Assert.That(HasAttribute(result, HtmlAttributes.Disabled), Is.True);
            Assert.That(AttributeValue(result, AriaAttributes.Disabled), Is.Null);
            Assert.That(HasAttribute(result, HtmlAttributes.Hidden), Is.True);
            Assert.That(AttributeValue(result, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.Pressed), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.Selected), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, AriaAttributes.Expanded), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.Checked), Is.EqualTo("mixed"));
            Assert.That(AttributeValue(result, AriaAttributes.Current), Is.EqualTo("step"));
            Assert.That(AttributeValue(result, AriaAttributes.Hidden), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.EqualTo("Save changes"));
            Assert.That(AttributeValue(result, AriaAttributes.Description), Is.Null);
            Assert.That(AttributeValue(result, AriaAttributes.Live), Is.EqualTo("polite"));
            Assert.That(AttributeValue(result, AriaAttributes.Controls), Is.EqualTo("panel preview"));
            Assert.That(AttributeValue(result, AriaAttributes.DescribedBy), Is.EqualTo("save-help"));
            Assert.That(AttributeValue(result, AriaAttributes.FlowTo), Is.EqualTo("next-action"));
        });
    }

    [Test]
    public void ProjectsNativeCheckedAndExpandedStateThroughLiveProperties()
    {
        UiNode checkbox = Control("checkbox");
        checkbox.Input = new UiInput
        {
            Kind = UiInputKind.Text,
            Name = "permission",
            Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = false }
        };
        checkbox.State.HasCheckState = true;
        checkbox.State.CheckState = UiCommandCheckState.Mixed;
        checkbox.Accessibility = new UiAccessibility { Role = AriaRoles.Checkbox };
        UiNode disclosure = Control("disclosure");
        disclosure.Disclosure = new UiDisclosure { Summary = "Advanced", IsExpanded = false };
        disclosure.State.HasExpanded = true;
        disclosure.State.IsExpanded = true;
        disclosure.Accessibility = new UiAccessibility { Role = AriaRoles.Group };
        SemanticExpander expander = new SemanticExpander();

        DomNode checkboxResult = expander.ExpandNode(checkbox);
        DomNode disclosureResult = expander.ExpandNode(disclosure);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(checkboxResult, HtmlAttributes.Role), Is.Null);
            Assert.That(AttributeValue(checkboxResult, AriaAttributes.Checked), Is.EqualTo("mixed"));
            Assert.That(HasAttribute(checkboxResult, HtmlAttributes.Checked), Is.False);
            Assert.That(BooleanProperty(checkboxResult, HtmlAttributes.Checked), Is.False);
            Assert.That(BooleanProperty(checkboxResult, "indeterminate"), Is.True);
            Assert.That(AttributeValue(disclosureResult, HtmlAttributes.Role), Is.Null);
            Assert.That(AttributeValue(disclosureResult, AriaAttributes.Expanded), Is.Null);
            Assert.That(HasAttribute(disclosureResult, HtmlAttributes.Open), Is.True);
            Assert.That(BooleanProperty(disclosureResult, HtmlAttributes.Open), Is.True);
        });
    }

    [Test]
    public void ProjectsDialogAccessibilityOntoItsNestedNativeDialog()
    {
        UiNode node = Control("modal-dialog");
        node.Dialog = new UiDialog
        {
            Kind = UiDialogKind.Modal,
            Key = "confirm",
            Title = "Confirm",
            State = new UiOverlayState { IsOpen = true, HasBackdrop = true }
        };
        node.Accessibility = new UiAccessibility
        {
            Role = AriaRoles.Dialog,
            Description = "Confirm the requested operation",
            Relationships = new UiAriaRelationship[]
            {
                new UiAriaRelationship { Name = AriaAttributes.Details, TargetIds = new string[] { "confirmation-details" } }
            }
        };

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode dialog = result.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(result, AriaAttributes.Description), Is.Null);
            Assert.That(dialog.Name, Is.EqualTo(HtmlElements.Dialog));
            Assert.That(AttributeValue(dialog, HtmlAttributes.Role), Is.Null);
            Assert.That(AttributeValue(dialog, AriaAttributes.Description), Is.EqualTo("Confirm the requested operation"));
            Assert.That(AttributeValue(dialog, AriaAttributes.Details), Is.EqualTo("confirmation-details"));
        });
    }

    [Test]
    public void CreatesAnElementHostWhenAFragmentControlNeedsSemantics()
    {
        UiNode child = new UiNode
        {
            Kind = UiNodeKind.Text,
            Text = "Custom content",
            Identity = new UiNodeIdentity { AuthorKey = "text", StableId = "text-id" }
        };
        UiNode node = Control("custom-control");
        node.Children = new UiNode[] { child };
        node.State.IsBusy = true;
        node.Accessibility = new UiAccessibility { Role = AriaRoles.Region, Label = "Custom region" };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Section));
            Assert.That(result.Key, Is.EqualTo("custom-control"));
            Assert.That(result.Id, Is.EqualTo("custom-control-id"));
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.Null);
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.EqualTo("Custom region"));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(result.Children[0].Text, Is.EqualTo("Custom content"));
        });
    }

    [Test]
    public void MergesGeneratedAndExplicitRelationshipsWithoutReorderingTargets()
    {
        UiNode source = Control("direct");
        source.Accessibility = new UiAccessibility
        {
            Relationships = new UiAriaRelationship[]
            {
                new UiAriaRelationship { Name = AriaAttributes.Controls, TargetIds = new string[] { "existing", "second" } },
                new UiAriaRelationship { Name = AriaAttributes.Controls, TargetIds = new string[] { "third", "second" } }
            }
        };
        DomNode rendered = new DomNode
        {
            Kind = DomNodeKind.Element,
            Name = HtmlElements.Button,
            Attributes = new DomAttribute[] { new DomAttribute { Name = AriaAttributes.Controls, Value = "existing first" } }
        };

        DomNode result = new AccessibilityProjector().ProjectControl(source, rendered);

        Assert.That(AttributeValue(result, AriaAttributes.Controls), Is.EqualTo("existing first second third"));
    }

    [Test]
    public void RejectsMalformedRelationshipProjection()
    {
        UiNode unsupported = Control("button");
        unsupported.Button = new UiButton { Label = "Action" };
        unsupported.Accessibility = new UiAccessibility
        {
            Relationships = new UiAriaRelationship[]
            {
                new UiAriaRelationship { Name = AriaAttributes.Label, TargetIds = new string[] { "label" } }
            }
        };
        UiNode invalidTarget = Control("button");
        invalidTarget.Button = new UiButton { Label = "Action" };
        invalidTarget.Accessibility = new UiAccessibility
        {
            Relationships = new UiAriaRelationship[]
            {
                new UiAriaRelationship { Name = AriaAttributes.Controls, TargetIds = new string[] { "bad target" } }
            }
        };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unsupported));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(invalidTarget));
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

    private static bool HasAttribute(DomNode node, string name)
    {
        return AttributeValue(node, name) != null;
    }

    private static bool BooleanProperty(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Properties.Length)
        {
            if (node.Properties[index].Name == name)
            {
                return node.Properties[index].Value.BooleanValue;
            }

            index++;
        }

        Assert.Fail("Property '" + name + "' was not found.");
        return false;
    }
}
