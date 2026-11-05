using System;
using htmlgui;

namespace htmlgui.Tests;

public class ChoiceTemplateExpanderTests
{
    [Test]
    public void ExpandsCheckboxValuesAndExplicitMixedState()
    {
        UiNode checkedNode = ChoiceControl("checkbox", "accepted", Array.Empty<UiChoice>());
        checkedNode.Input!.Value = BooleanValue(true);
        UiNode mixedNode = ChoiceControl("checkbox", "permission", Array.Empty<UiChoice>());
        mixedNode.State.HasCheckState = true;
        mixedNode.State.CheckState = UiCommandCheckState.Mixed;
        SemanticExpander expander = new SemanticExpander();

        DomNode checkedResult = expander.ExpandNode(checkedNode);
        DomNode mixedResult = expander.ExpandNode(mixedNode);

        Assert.Multiple(() =>
        {
            Assert.That(checkedResult.Name, Is.EqualTo(HtmlElements.Input));
            Assert.That(AttributeValue(checkedResult, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.Checkbox));
            Assert.That(AttributeValue(checkedResult, HtmlAttributes.Name), Is.EqualTo("accepted"));
            Assert.That(AttributeValue(checkedResult, HtmlAttributes.Value), Is.EqualTo("true"));
            Assert.That(FindAttribute(checkedResult, HtmlAttributes.Checked).IsBoolean, Is.True);
            Assert.That(BooleanProperty(checkedResult, HtmlAttributes.Checked), Is.True);
            Assert.That(BooleanProperty(checkedResult, "indeterminate"), Is.False);
            Assert.That(HasAttribute(mixedResult, HtmlAttributes.Checked), Is.False);
            Assert.That(AttributeValue(mixedResult, AriaAttributes.Checked), Is.EqualTo("mixed"));
            Assert.That(BooleanProperty(mixedResult, HtmlAttributes.Checked), Is.False);
            Assert.That(BooleanProperty(mixedResult, "indeterminate"), Is.True);
        });
    }

    [Test]
    public void ExpandsSwitchWithRequiredRoleAndCheckedState()
    {
        UiNode node = ChoiceControl("switch", "notifications", Array.Empty<UiChoice>());
        node.Input!.Value = BooleanValue(false);
        node.State.HasCheckState = true;
        node.State.CheckState = UiCommandCheckState.Checked;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-choice htmlgui-switch"));
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Switch));
            Assert.That(AttributeValue(result, AriaAttributes.Checked), Is.EqualTo("true"));
            Assert.That(FindAttribute(result, HtmlAttributes.Checked).IsBoolean, Is.True);
            Assert.That(BooleanProperty(result, HtmlAttributes.Checked), Is.True);
        });
    }

    [Test]
    public void ExpandsNativeRadioGroupWithLabelsValuesAndSelection()
    {
        UiChoice first = Choice("small", "Small", "s", false);
        first.IsDisabled = true;
        UiChoice second = Choice("large", "Large", "l", true);
        UiNode node = ChoiceControl("radio-group", "size", new UiChoice[] { first, second });
        node.State.Version = 4;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode firstInput = result.Children[0].Children[0];
        DomNode secondInput = result.Children[1].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.RadioGroup));
            Assert.That(AttributeValue(result, "data-field-name"), Is.EqualTo("size"));
            Assert.That(result.Children[0].Name, Is.EqualTo(HtmlElements.Label));
            Assert.That(result.Children[0].Key, Is.EqualTo("small"));
            Assert.That(firstInput.Name, Is.EqualTo(HtmlElements.Input));
            Assert.That(AttributeValue(firstInput, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.Radio));
            Assert.That(AttributeValue(firstInput, HtmlAttributes.Value), Is.EqualTo("s"));
            Assert.That(FindAttribute(firstInput, HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(BooleanProperty(firstInput, HtmlAttributes.Checked), Is.False);
            Assert.That(FindAttribute(secondInput, HtmlAttributes.Checked).IsBoolean, Is.True);
            Assert.That(BooleanProperty(secondInput, HtmlAttributes.Checked), Is.True);
            Assert.That(result.Children[1].Children[1].Text, Is.EqualTo("Large"));
            Assert.That(secondInput.Version, Is.EqualTo(4));
            Assert.That(secondInput.Id, Is.Not.EqualTo(firstInput.Id));
        });
    }

    [Test]
    public void ExpandsSingleAndMultipleNativeSelects()
    {
        UiChoice first = Choice("red", "Red", "#f00", true);
        UiChoice second = Choice("blue", "Blue", "#00f", false);
        second.IsDisabled = true;
        UiNode single = ChoiceControl("select", "color", new UiChoice[] { first, second });
        UiChoice multipleFirst = Choice("one", "One", "1", true);
        UiChoice multipleSecond = Choice("two", "Two", "2", true);
        UiNode multiple = ChoiceControl("multi-select", "numbers", new UiChoice[] { multipleFirst, multipleSecond });
        SemanticExpander expander = new SemanticExpander();

        DomNode singleResult = expander.ExpandNode(single);
        DomNode multipleResult = expander.ExpandNode(multiple);

        Assert.Multiple(() =>
        {
            Assert.That(singleResult.Name, Is.EqualTo(HtmlElements.Select));
            Assert.That(AttributeValue(singleResult, HtmlAttributes.Class), Is.EqualTo("htmlgui-choice htmlgui-select"));
            Assert.That(HasAttribute(singleResult, HtmlAttributes.Multiple), Is.False);
            Assert.That(singleResult.Properties[0].Value.StringValue, Is.EqualTo("#f00"));
            Assert.That(AttributeValue(singleResult.Children[0], "data-choice-key"), Is.EqualTo("red"));
            Assert.That(FindAttribute(singleResult.Children[0], HtmlAttributes.Selected).IsBoolean, Is.True);
            Assert.That(BooleanProperty(singleResult.Children[0], HtmlAttributes.Selected), Is.True);
            Assert.That(FindAttribute(singleResult.Children[1], HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(singleResult.Children[1].Children[0].Text, Is.EqualTo("Blue"));
            Assert.That(multipleResult.Name, Is.EqualTo(HtmlElements.Select));
            Assert.That(AttributeValue(multipleResult, HtmlAttributes.Class), Is.EqualTo("htmlgui-choice htmlgui-multi-select"));
            Assert.That(FindAttribute(multipleResult, HtmlAttributes.Multiple).IsBoolean, Is.True);
            Assert.That(multipleResult.Properties, Is.Empty);
            Assert.That(FindAttribute(multipleResult.Children[0], HtmlAttributes.Selected).IsBoolean, Is.True);
            Assert.That(FindAttribute(multipleResult.Children[1], HtmlAttributes.Selected).IsBoolean, Is.True);
        });
    }

    [Test]
    public void ProjectsEveryPortableScalarChoiceValueAndSelectedLiveValue()
    {
        UiChoice nullChoice = Choice("null", "Null", string.Empty, false);
        nullChoice.Value = new UiValue { Kind = UiValueKind.Null };
        UiChoice booleanChoice = Choice("boolean", "Boolean", string.Empty, true);
        booleanChoice.Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true };
        UiChoice integerChoice = Choice("integer", "Integer", string.Empty, false);
        integerChoice.Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = -12 };
        UiChoice floatingChoice = Choice("floating", "Floating", string.Empty, false);
        floatingChoice.Value = new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = 2.5 };
        UiChoice decimalChoice = Choice("decimal", "Decimal", string.Empty, false);
        decimalChoice.Value = new UiValue { Kind = UiValueKind.Decimal, DecimalValue = 3.75m };
        UiChoice stringChoice = Choice("string", "String", "value", false);
        UiChoice dateTimeChoice = Choice("date-time", "Date/time", string.Empty, false);
        dateTimeChoice.Value = new UiValue { Kind = UiValueKind.DateTime, DateTimeValue = "2026-09-14" };
        UiChoice binaryChoice = Choice("binary", "Binary", string.Empty, false);
        binaryChoice.Value = new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[] { 1, 2, 3 } };
        UiNode node = ChoiceControl(
            "select",
            "portable",
            new UiChoice[] { nullChoice, booleanChoice, integerChoice, floatingChoice, decimalChoice, stringChoice, dateTimeChoice, binaryChoice });

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result.Children[0], HtmlAttributes.Value), Is.Empty);
            Assert.That(AttributeValue(result.Children[1], HtmlAttributes.Value), Is.EqualTo("true"));
            Assert.That(AttributeValue(result.Children[2], HtmlAttributes.Value), Is.EqualTo("-12"));
            Assert.That(AttributeValue(result.Children[3], HtmlAttributes.Value), Is.EqualTo("2.5"));
            Assert.That(AttributeValue(result.Children[4], HtmlAttributes.Value), Is.EqualTo("3.75"));
            Assert.That(AttributeValue(result.Children[5], HtmlAttributes.Value), Is.EqualTo("value"));
            Assert.That(AttributeValue(result.Children[6], HtmlAttributes.Value), Is.EqualTo("2026-09-14"));
            Assert.That(AttributeValue(result.Children[7], HtmlAttributes.Value), Is.EqualTo("AQID"));
            Assert.That(result.Properties[0].Name, Is.EqualTo(HtmlAttributes.Value));
            Assert.That(result.Properties[0].Value.Kind, Is.EqualTo(UiValueKind.String));
            Assert.That(result.Properties[0].Value.StringValue, Is.EqualTo("true"));
        });
    }

    [Test]
    public void SelectMultipleFlagEnablesNativeMultipleSelection()
    {
        UiNode node = ChoiceControl(
            "select",
            "items",
            new UiChoice[] { Choice("one", "One", "1", true), Choice("two", "Two", "2", true) });
        node.Input!.Multiple = true;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-choice htmlgui-multi-select"));
            Assert.That(FindAttribute(result, HtmlAttributes.Multiple).IsBoolean, Is.True);
        });
    }

    [Test]
    public void ExpandsAriaListBoxWithMultipleSelectionAndDisabledOptions()
    {
        UiChoice selected = Choice("alpha", "Alpha", "a", true);
        UiChoice disabled = Choice("beta", "Beta", "b", false);
        disabled.IsDisabled = true;
        UiNode node = ChoiceControl("listbox", "letters", new UiChoice[] { selected, disabled });
        node.Input!.Multiple = true;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.ListBox));
            Assert.That(AttributeValue(result, AriaAttributes.MultiSelectable), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, HtmlAttributes.TabIndex), Is.EqualTo("0"));
            Assert.That(AttributeValue(result.Children[0], HtmlAttributes.Role), Is.EqualTo(AriaRoles.Option));
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(AttributeValue(result.Children[0], "data-value"), Is.EqualTo("a"));
            Assert.That(AttributeValue(result.Children[0], HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.Disabled), Is.EqualTo("true"));
        });
    }

    [Test]
    public void ExpandsSelectOnlyComboWithStableControlledPopup()
    {
        UiNode node = ChoiceControl(
            "combo-box",
            "country",
            new UiChoice[] { Choice("ca", "Canada", "CA", true), Choice("us", "United States", "US", false) });
        SemanticExpander expander = new SemanticExpander();

        DomNode closed = expander.ExpandNode(node);
        node.State.HasExpanded = true;
        node.State.IsExpanded = true;
        DomNode open = expander.ExpandNode(node);
        DomNode trigger = closed.Children[0];
        DomNode popup = closed.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(closed, HtmlAttributes.Class), Is.EqualTo("htmlgui-choice htmlgui-combo-box"));
            Assert.That(trigger.Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(trigger, HtmlAttributes.Role), Is.EqualTo(AriaRoles.ComboBox));
            Assert.That(AttributeValue(trigger, AriaAttributes.Expanded), Is.EqualTo("false"));
            Assert.That(AttributeValue(trigger, AriaAttributes.Controls), Is.EqualTo(popup.Id));
            Assert.That(trigger.Children[0].Text, Is.EqualTo("Canada"));
            Assert.That(AttributeValue(popup, HtmlAttributes.Role), Is.EqualTo(AriaRoles.ListBox));
            Assert.That(FindAttribute(popup, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(popup.Children[0].Key, Is.EqualTo("ca"));
            Assert.That(AttributeValue(open.Children[0], AriaAttributes.Expanded), Is.EqualTo("true"));
            Assert.That(HasAttribute(open.Children[1], HtmlAttributes.Hidden), Is.False);
            Assert.That(open.Children[1].Id, Is.EqualTo(popup.Id));
        });
    }

    [TestCase("autocomplete", "htmlgui-choice htmlgui-autocomplete", "list")]
    [TestCase("editable-combo", "htmlgui-choice htmlgui-editable-combo", "both")]
    public void ExpandsEditableComboVariantsWithTextEditorAndPopup(string name, string className, string autocomplete)
    {
        UiNode node = ChoiceControl(name, "city", new UiChoice[] { Choice("sea", "Seattle", "Seattle", true) });
        node.Input!.Value = TextValue("Sea");
        node.State.HasExpanded = true;
        node.State.IsExpanded = true;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode editor = result.Children[0];
        DomNode popup = result.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo(className));
            Assert.That(editor.Name, Is.EqualTo(HtmlElements.Input));
            Assert.That(AttributeValue(editor, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.Text));
            Assert.That(AttributeValue(editor, HtmlAttributes.Role), Is.EqualTo(AriaRoles.ComboBox));
            Assert.That(AttributeValue(editor, AriaAttributes.AutoComplete), Is.EqualTo(autocomplete));
            Assert.That(AttributeValue(editor, AriaAttributes.Expanded), Is.EqualTo("true"));
            Assert.That(AttributeValue(editor, AriaAttributes.Controls), Is.EqualTo(popup.Id));
            Assert.That(AttributeValue(editor, HtmlAttributes.Value), Is.EqualTo("Sea"));
            Assert.That(editor.Properties[0].Value.StringValue, Is.EqualTo("Sea"));
            Assert.That(HasAttribute(popup, HtmlAttributes.Hidden), Is.False);
        });
    }

    [Test]
    public void ExpandsSegmentedChoiceWithRovingTabStop()
    {
        UiChoice disabled = Choice("day", "Day", "day", false);
        disabled.IsDisabled = true;
        UiChoice selected = Choice("week", "Week", "week", true);
        UiChoice other = Choice("month", "Month", "month", false);
        UiNode node = ChoiceControl("segmented-control", "period", new UiChoice[] { disabled, selected, other });

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.RadioGroup));
            Assert.That(result.Children[0].Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(result.Children[0], HtmlAttributes.Role), Is.EqualTo(AriaRoles.Radio));
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.Checked), Is.EqualTo("false"));
            Assert.That(AttributeValue(result.Children[0], HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(FindAttribute(result.Children[0], HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(AttributeValue(result.Children[1], AriaAttributes.Checked), Is.EqualTo("true"));
            Assert.That(AttributeValue(result.Children[1], HtmlAttributes.TabIndex), Is.EqualTo("0"));
            Assert.That(AttributeValue(result.Children[1], "data-value"), Is.EqualTo("week"));
            Assert.That(AttributeValue(result.Children[2], HtmlAttributes.TabIndex), Is.EqualTo("-1"));
        });
    }

    [Test]
    public void SegmentedChoiceUsesFirstEnabledChoiceWhenNoneIsSelected()
    {
        UiChoice first = Choice("first", "First", "1", false);
        first.IsDisabled = true;
        UiChoice second = Choice("second", "Second", "2", false);
        UiNode node = ChoiceControl("segmented-choice", "choice", new UiChoice[] { first, second });

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result.Children[0], HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(result.Children[1], HtmlAttributes.TabIndex), Is.EqualTo("0"));
        });
    }

    [Test]
    public void ProjectsSharedDisabledVisibilityBusyAndTabIndexState()
    {
        UiNode checkbox = ChoiceControl("checkbox", "enabled", Array.Empty<UiChoice>());
        checkbox.Input!.IsDisabled = true;
        checkbox.State.IsVisible = false;
        checkbox.State.IsBusy = true;
        checkbox.State.HasTabIndex = true;
        checkbox.State.TabIndex = -1;
        UiNode listbox = ChoiceControl("listbox", "options", new UiChoice[] { Choice("one", "One", "1", false) });
        listbox.State.IsEnabled = false;
        SemanticExpander expander = new SemanticExpander();

        DomNode checkboxResult = expander.ExpandNode(checkbox);
        DomNode listboxResult = expander.ExpandNode(listbox);

        Assert.Multiple(() =>
        {
            Assert.That(FindAttribute(checkboxResult, HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(FindAttribute(checkboxResult, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(checkboxResult, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(checkboxResult, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(listboxResult, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(AttributeValue(listboxResult, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(listboxResult.Children[0], AriaAttributes.Disabled), Is.EqualTo("true"));
        });
    }

    [Test]
    public void RejectsMalformedChoiceModelsAndUnsupportedStates()
    {
        UiNode missingPayload = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "select",
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode missingName = ChoiceControl("select", " ", Array.Empty<UiChoice>());
        UiNode missingValue = ChoiceControl("select", "field", Array.Empty<UiChoice>());
        missingValue.Input!.Value = null!;
        UiNode nullChoices = ChoiceControl("select", "field", Array.Empty<UiChoice>());
        nullChoices.Input!.Choices = null!;
        UiNode nullChoice = ChoiceControl("select", "field", new UiChoice[] { null! });
        UiNode blankKey = ChoiceControl("select", "field", new UiChoice[] { Choice(" ", "Blank", "blank", false) });
        UiChoice nullLabelChoice = Choice("null-label", "Label", "value", false);
        nullLabelChoice.Label = null!;
        UiNode nullLabel = ChoiceControl("select", "field", new UiChoice[] { nullLabelChoice });
        UiChoice nullValueChoice = Choice("null-value", "Label", "value", false);
        nullValueChoice.Value = null!;
        UiNode nullValue = ChoiceControl("select", "field", new UiChoice[] { nullValueChoice });
        UiChoice duplicateFirst = Choice("same", "First", "1", false);
        UiChoice duplicateSecond = Choice("same", "Second", "2", false);
        UiNode duplicates = ChoiceControl("select", "field", new UiChoice[] { duplicateFirst, duplicateSecond });
        UiChoice selectedFirst = Choice("first", "First", "1", true);
        UiChoice selectedSecond = Choice("second", "Second", "2", true);
        UiNode tooMany = ChoiceControl("radio", "field", new UiChoice[] { selectedFirst, selectedSecond });
        UiNode aggregateChoice = ChoiceControl("select", "field", new UiChoice[] { Choice("array", "Array", "value", false) });
        aggregateChoice.Input!.Choices[0].Value = new UiValue { Kind = UiValueKind.Array, ArrayValue = Array.Empty<UiValue>() };
        UiNode wrongCheckbox = ChoiceControl("checkbox", "field", Array.Empty<UiChoice>());
        wrongCheckbox.Input!.Value = TextValue("true");
        UiNode wrongEditable = ChoiceControl("editable-combo", "field", Array.Empty<UiChoice>());
        wrongEditable.Input!.Value = BooleanValue(true);
        UiNode mixedSwitch = ChoiceControl("switch", "field", Array.Empty<UiChoice>());
        mixedSwitch.State.HasCheckState = true;
        mixedSwitch.State.CheckState = UiCommandCheckState.Mixed;
        UiNode undefinedState = ChoiceControl("checkbox", "field", Array.Empty<UiChoice>());
        undefinedState.State.HasCheckState = true;
        undefinedState.State.CheckState = (UiCommandCheckState)999;
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPayload))!.NodeId, Is.EqualTo("missing-id"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingName));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingValue));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullChoices));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullChoice));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(blankKey));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullLabel));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullValue));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicates));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(tooMany));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(aggregateChoice));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongCheckbox));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongEditable));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(mixedSwitch));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(undefinedState))!.ParamName, Is.EqualTo("checkState"));
        });
    }

    [Test]
    public void RepeatedExpansionIsFreshStableAndDoesNotMutateChoices()
    {
        UiChoice choice = Choice("stable", "Stable", "value", true);
        UiNode node = ChoiceControl("listbox", "items", new UiChoice[] { choice });
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);
        first.Children[0].Attributes[0].Value = "changed";
        first.Children[0].Children[0].Text = "Changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Attributes, Is.Not.SameAs(second.Attributes));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(first.Children[0], Is.Not.SameAs(second.Children[0]));
            Assert.That(first.Children[0].Attributes, Is.Not.SameAs(second.Children[0].Attributes));
            Assert.That(first.Children[0].Id, Is.EqualTo(second.Children[0].Id));
            Assert.That(choice.Key, Is.EqualTo("stable"));
            Assert.That(choice.Label, Is.EqualTo("Stable"));
            Assert.That(choice.Value.StringValue, Is.EqualTo("value"));
            Assert.That(second.Children[0].Children[0].Text, Is.EqualTo("Stable"));
        });
    }

    private static UiNode ChoiceControl(string name, string fieldName, UiChoice[] choices)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = name,
            Identity = new UiNodeIdentity { AuthorKey = "choice", SiblingIdentity = "choice", StableId = "choice-id" },
            Input = new UiInput { Name = fieldName, Choices = choices }
        };
    }

    private static UiChoice Choice(string key, string label, string value, bool selected)
    {
        return new UiChoice
        {
            Key = key,
            Label = label,
            Value = TextValue(value),
            IsSelected = selected
        };
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue BooleanValue(bool value)
    {
        return new UiValue { Kind = UiValueKind.Boolean, BooleanValue = value };
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

        throw new AssertionException("Property was not found: " + name);
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
}
