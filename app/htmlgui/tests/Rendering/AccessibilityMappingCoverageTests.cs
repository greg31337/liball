using System;
using System.Linq;
using System.Reflection;
using htmlgui;

namespace htmlgui.Tests;

public class AccessibilityMappingCoverageTests
{
    [Test]
    public void ProjectsEveryCanonicalControlRoleAndUsesNativeLandmarksWhenAvailable()
    {
        FieldInfo[] fields = typeof(AriaRoles)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .ToArray();
        AccessibilityProjector projector = new AccessibilityProjector();

        Assert.That(fields, Is.Not.Empty);
        foreach (FieldInfo field in fields)
        {
            string role = (string)field.GetRawConstantValue()!;
            UiNode source = Control("role-" + field.Name);
            source.Accessibility = new UiAccessibility { Role = role, Label = "Named control" };
            DomNode result = projector.ProjectControl(source, Element(HtmlElements.Division));
            string? explicitRole = AttributeValue(result, HtmlAttributes.Role);
            string? implicitRole = projector.GetImplicitRole(result);

            Assert.That(explicitRole ?? implicitRole, Is.EqualTo(role), field.Name);
            if (implicitRole == role)
            {
                Assert.That(explicitRole, Is.Null, field.Name + " should use native semantics");
            }
        }
    }

    [TestCase(HtmlElements.Button, null, null, AriaRoles.Button)]
    [TestCase(HtmlElements.Summary, null, null, AriaRoles.Button)]
    [TestCase(HtmlElements.Input, null, null, AriaRoles.TextBox)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Text, AriaRoles.TextBox)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Search, AriaRoles.SearchBox)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Number, AriaRoles.SpinButton)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Range, AriaRoles.Slider)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Checkbox, AriaRoles.Checkbox)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Radio, AriaRoles.Radio)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Button, AriaRoles.Button)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Submit, AriaRoles.Button)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Reset, AriaRoles.Button)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Hidden, null)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Color, null)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Date, null)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Time, null)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Month, null)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.Week, null)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.LocalDateTime, null)]
    [TestCase(HtmlElements.Input, HtmlAttributes.Type, HtmlInputTypes.File, null)]
    [TestCase(HtmlElements.TextArea, null, null, AriaRoles.TextBox)]
    [TestCase(HtmlElements.Select, null, null, AriaRoles.ComboBox)]
    [TestCase(HtmlElements.Select, HtmlAttributes.Multiple, "", AriaRoles.ListBox)]
    [TestCase(HtmlElements.Option, null, null, AriaRoles.Option)]
    [TestCase(HtmlElements.Anchor, HtmlAttributes.HyperlinkReference, "/target", AriaRoles.Link)]
    [TestCase(HtmlElements.Anchor, null, null, null)]
    [TestCase(HtmlElements.Image, HtmlAttributes.AlternativeText, "Portrait", AriaRoles.Image)]
    [TestCase(HtmlElements.Image, HtmlAttributes.AlternativeText, "", AriaRoles.Presentation)]
    [TestCase(HtmlElements.Navigation, null, null, AriaRoles.Navigation)]
    [TestCase(HtmlElements.Search, null, null, AriaRoles.Search)]
    [TestCase(HtmlElements.Main, null, null, AriaRoles.Main)]
    [TestCase(HtmlElements.Aside, null, null, AriaRoles.Complementary)]
    [TestCase(HtmlElements.Header, null, null, AriaRoles.Banner)]
    [TestCase(HtmlElements.Footer, null, null, AriaRoles.ContentInfo)]
    [TestCase(HtmlElements.Form, AriaAttributes.Label, "Account", AriaRoles.Form)]
    [TestCase(HtmlElements.Form, null, null, null)]
    [TestCase(HtmlElements.Section, AriaAttributes.LabelledBy, "heading", AriaRoles.Region)]
    [TestCase(HtmlElements.Section, null, null, null)]
    [TestCase(HtmlElements.OrderedList, null, null, AriaRoles.List)]
    [TestCase(HtmlElements.UnorderedList, null, null, AriaRoles.List)]
    [TestCase(HtmlElements.ListItem, null, null, AriaRoles.ListItem)]
    [TestCase(HtmlElements.Table, null, null, AriaRoles.Table)]
    [TestCase(HtmlElements.TableRow, null, null, AriaRoles.Row)]
    [TestCase(HtmlElements.TableHeader, null, null, AriaRoles.ColumnHeader)]
    [TestCase(HtmlElements.TableData, null, null, AriaRoles.Cell)]
    [TestCase(HtmlElements.Progress, null, null, AriaRoles.ProgressBar)]
    [TestCase(HtmlElements.Output, null, null, AriaRoles.Status)]
    [TestCase(HtmlElements.HorizontalRule, null, null, AriaRoles.Separator)]
    [TestCase(HtmlElements.Dialog, null, null, AriaRoles.Dialog)]
    [TestCase(HtmlElements.Details, null, null, AriaRoles.Group)]
    [TestCase(HtmlElements.FieldSet, null, null, AriaRoles.Group)]
    [TestCase(HtmlElements.Heading1, null, null, AriaRoles.Heading)]
    [TestCase(HtmlElements.Heading2, null, null, AriaRoles.Heading)]
    [TestCase(HtmlElements.Heading3, null, null, AriaRoles.Heading)]
    [TestCase(HtmlElements.Heading4, null, null, AriaRoles.Heading)]
    [TestCase(HtmlElements.Heading5, null, null, AriaRoles.Heading)]
    [TestCase(HtmlElements.Heading6, null, null, AriaRoles.Heading)]
    [TestCase(HtmlElements.Article, null, null, HtmlElements.Article)]
    [TestCase(HtmlElements.Meter, null, null, HtmlElements.Meter)]
    public void RecognizesEverySupportedImplicitHtmlRole(string elementName, string? attributeName, string? attributeValue, string? expectedRole)
    {
        DomNode element = Element(elementName);
        if (attributeName != null)
        {
            element.Attributes = new DomAttribute[]
            {
                new DomAttribute { Name = attributeName, Value = attributeValue ?? string.Empty }
            };
        }

        AccessibilityProjector projector = new AccessibilityProjector();
        string? implicitRole = projector.GetImplicitRole(element);

        Assert.That(implicitRole, Is.EqualTo(expectedRole));
        if (expectedRole != null)
        {
            UiNode source = Control("native-" + elementName);
            source.Accessibility = new UiAccessibility { Role = expectedRole };
            element.Attributes = element.Attributes
                .Append(new DomAttribute { Name = HtmlAttributes.Role, Value = expectedRole })
                .ToArray();

            DomNode result = projector.ProjectControl(source, element);

            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.Null);
        }
    }

    [Test]
    public void ProjectsEverySupportedIdReferenceRelationshipAndResolvesNamePrecedence()
    {
        UiNode source = Control("relationships");
        source.Accessibility = new UiAccessibility
        {
            Label = "Direct label",
            Description = "Direct description",
            Relationships = new UiAriaRelationship[]
            {
                Relationship(AriaAttributes.ActiveDescendant, "active"),
                Relationship(AriaAttributes.Controls, "panel", "panel"),
                Relationship(AriaAttributes.DescribedBy, "help"),
                Relationship(AriaAttributes.Details, "details"),
                Relationship(AriaAttributes.ErrorMessage, "error"),
                Relationship(AriaAttributes.FlowTo, "next"),
                Relationship(AriaAttributes.LabelledBy, "label"),
                Relationship(AriaAttributes.Owns, "popup")
            }
        };
        DomNode rendered = Element(HtmlElements.Button);
        rendered.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = AriaAttributes.Controls, Value = "existing" }
        };

        DomNode result = new AccessibilityProjector().ProjectControl(source, rendered);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, AriaAttributes.ActiveDescendant), Is.EqualTo("active"));
            Assert.That(AttributeValue(result, AriaAttributes.Controls), Is.EqualTo("existing panel"));
            Assert.That(AttributeValue(result, AriaAttributes.DescribedBy), Is.EqualTo("help error"));
            Assert.That(AttributeValue(result, AriaAttributes.Details), Is.EqualTo("details"));
            Assert.That(AttributeValue(result, AriaAttributes.ErrorMessage), Is.EqualTo("error"));
            Assert.That(AttributeValue(result, AriaAttributes.FlowTo), Is.EqualTo("next"));
            Assert.That(AttributeValue(result, AriaAttributes.LabelledBy), Is.EqualTo("label"));
            Assert.That(AttributeValue(result, AriaAttributes.Owns), Is.EqualTo("popup"));
            Assert.That(AttributeValue(result, AriaAttributes.Invalid), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.Null);
            Assert.That(AttributeValue(result, AriaAttributes.Description), Is.Null);
        });
    }

    [Test]
    public void UsesNativeDisabledSelectedExpandedAndCheckedStateWhereSupported()
    {
        DomNode button = ProjectState(Element(HtmlElements.Button), new UiNodeState { IsEnabled = false });
        DomNode option = ProjectState(Element(HtmlElements.Option), new UiNodeState { HasSelected = true, IsSelected = true });
        DomNode details = ProjectState(Element(HtmlElements.Details), new UiNodeState { HasExpanded = true, IsExpanded = true });
        DomNode checkbox = ProjectState(Input(HtmlInputTypes.Checkbox), new UiNodeState { HasCheckState = true, CheckState = UiCommandCheckState.Mixed });
        DomNode radio = ProjectState(Input(HtmlInputTypes.Radio), new UiNodeState { HasCheckState = true, CheckState = UiCommandCheckState.Checked });
        DomNode mixedRadio = ProjectState(Input(HtmlInputTypes.Radio), new UiNodeState { HasCheckState = true, CheckState = UiCommandCheckState.Mixed });

        Assert.Multiple(() =>
        {
            Assert.That(HasAttribute(button, HtmlAttributes.Disabled), Is.True);
            Assert.That(AttributeValue(button, AriaAttributes.Disabled), Is.Null);
            Assert.That(HasAttribute(option, HtmlAttributes.Selected), Is.True);
            Assert.That(BooleanProperty(option, HtmlAttributes.Selected), Is.True);
            Assert.That(AttributeValue(option, AriaAttributes.Selected), Is.Null);
            Assert.That(HasAttribute(details, HtmlAttributes.Open), Is.True);
            Assert.That(BooleanProperty(details, HtmlAttributes.Open), Is.True);
            Assert.That(AttributeValue(details, AriaAttributes.Expanded), Is.Null);
            Assert.That(HasAttribute(checkbox, HtmlAttributes.Checked), Is.False);
            Assert.That(BooleanProperty(checkbox, HtmlAttributes.Checked), Is.False);
            Assert.That(BooleanProperty(checkbox, "indeterminate"), Is.True);
            Assert.That(AttributeValue(checkbox, AriaAttributes.Checked), Is.Null);
            Assert.That(HasAttribute(radio, HtmlAttributes.Checked), Is.True);
            Assert.That(BooleanProperty(radio, HtmlAttributes.Checked), Is.True);
            Assert.That(AttributeValue(radio, AriaAttributes.Checked), Is.Null);
            Assert.That(AttributeValue(mixedRadio, AriaAttributes.Checked), Is.EqualTo("mixed"));
        });
    }

    [Test]
    public void UsesAriaStateForControlsWithoutEquivalentNativeState()
    {
        UiNodeState state = new UiNodeState
        {
            IsVisible = false,
            IsEnabled = false,
            IsBusy = true,
            HasTabIndex = true,
            TabIndex = -1,
            HasPressed = true,
            IsPressed = false,
            HasSelected = true,
            IsSelected = true,
            HasExpanded = true,
            IsExpanded = false,
            HasCheckState = true,
            CheckState = UiCommandCheckState.Unchecked
        };

        DomNode result = ProjectState(Element(HtmlElements.Division), state);

        Assert.Multiple(() =>
        {
            Assert.That(HasAttribute(result, HtmlAttributes.Hidden), Is.True);
            Assert.That(AttributeValue(result, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(result, AriaAttributes.Pressed), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.Expanded), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, AriaAttributes.Checked), Is.EqualTo("false"));
        });
    }

    [TestCase(UiAriaCurrentKind.None, "false")]
    [TestCase(UiAriaCurrentKind.Page, "page")]
    [TestCase(UiAriaCurrentKind.Step, "step")]
    [TestCase(UiAriaCurrentKind.Location, "location")]
    [TestCase(UiAriaCurrentKind.Date, "date")]
    [TestCase(UiAriaCurrentKind.Time, "time")]
    [TestCase(UiAriaCurrentKind.True, "true")]
    public void ProjectsEveryCurrentItemState(UiAriaCurrentKind kind, string expected)
    {
        DomNode result = ProjectState(
            Element(HtmlElements.Anchor),
            new UiNodeState { HasCurrentKind = true, CurrentKind = kind });

        Assert.That(AttributeValue(result, AriaAttributes.Current), Is.EqualTo(expected));
    }

    [Test]
    public void PreservesExplicitRenderedStatesInsteadOfOverwritingThem()
    {
        DomNode rendered = Element(HtmlElements.Button);
        rendered.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = AriaAttributes.Busy, Value = "false" },
            new DomAttribute { Name = AriaAttributes.Pressed, Value = "mixed" },
            new DomAttribute { Name = HtmlAttributes.TabIndex, Value = "2" }
        };
        UiNodeState state = new UiNodeState
        {
            IsBusy = true,
            HasPressed = true,
            IsPressed = true,
            HasTabIndex = true,
            TabIndex = -1
        };

        DomNode result = ProjectState(rendered, state);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, AriaAttributes.Pressed), Is.EqualTo("mixed"));
            Assert.That(AttributeValue(result, HtmlAttributes.TabIndex), Is.EqualTo("2"));
        });
    }

    [Test]
    public void UsesAlternativeTextForImageNamesAndAriaLabelsForOtherControls()
    {
        UiNode imageSource = Control("image");
        imageSource.Accessibility = new UiAccessibility { Label = "Profile portrait", Description = "Current account" };
        UiNode buttonSource = Control("button");
        buttonSource.Accessibility = new UiAccessibility { Label = "Save changes", Description = "Stores the draft" };
        AccessibilityProjector projector = new AccessibilityProjector();

        DomNode image = projector.ProjectControl(imageSource, Element(HtmlElements.Image));
        DomNode button = projector.ProjectControl(buttonSource, Element(HtmlElements.Button));

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(image, HtmlAttributes.AlternativeText), Is.EqualTo("Profile portrait"));
            Assert.That(AttributeValue(image, AriaAttributes.Label), Is.Null);
            Assert.That(AttributeValue(image, AriaAttributes.Description), Is.EqualTo("Current account"));
            Assert.That(AttributeValue(button, AriaAttributes.Label), Is.EqualTo("Save changes"));
            Assert.That(AttributeValue(button, AriaAttributes.Description), Is.EqualTo("Stores the draft"));
        });
    }

    private static DomNode ProjectState(DomNode rendered, UiNodeState state)
    {
        UiNode source = Control("state");
        source.State = state;
        return new AccessibilityProjector().ProjectControl(source, rendered);
    }

    private static UiAriaRelationship Relationship(string name, params string[] targetIds)
    {
        return new UiAriaRelationship { Name = name, TargetIds = targetIds };
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

    private static DomNode Element(string name)
    {
        return new DomNode { Kind = DomNodeKind.Element, Name = name };
    }

    private static DomNode Input(string type)
    {
        DomNode result = Element(HtmlElements.Input);
        result.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = HtmlAttributes.Type, Value = type }
        };
        return result;
    }

    private static string? AttributeValue(DomNode node, string name)
    {
        return node.Attributes.FirstOrDefault(attribute => attribute.Name == name)?.Value;
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        return node.Attributes.Any(attribute => attribute.Name == name);
    }

    private static bool BooleanProperty(DomNode node, string name)
    {
        return node.Properties.Single(property => property.Name == name).Value.BooleanValue;
    }
}
