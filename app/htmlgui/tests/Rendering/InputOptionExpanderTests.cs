using System;
using htmlgui;

namespace htmlgui.Tests;

public class InputOptionExpanderTests
{
    [Test]
    public void ExpandsTextHintsConstraintsAdornmentsActionsAndDescriptions()
    {
        UiNode node = InputControl(UiInputKind.Password, "secret");
        node.Input!.Value = TextValue("value");
        node.Input.Placeholder = "Password";
        node.Input.HelperText = "Use twelve characters";
        node.Input.ErrorText = "Password is too short";
        node.Input.Prefix = "Key";
        node.Input.Suffix = "required";
        node.Input.HasClearAction = true;
        node.Input.HasRevealAction = true;
        node.Input.IsRevealed = true;
        node.Input.InputMode = "text";
        node.Input.AutoComplete = "current-password";
        node.Input.EnterKeyHint = "done";
        node.Input.HasSpellCheck = true;
        node.Input.SpellCheck = false;
        node.Input.HasMinimumLength = true;
        node.Input.MinimumLength = 12;
        node.Input.HasMaximumLength = true;
        node.Input.MaximumLength = 64;
        node.Input.Pattern = "[A-Za-z]{12,64}";
        node.Input.IsRequired = true;
        node.Input.UpdateTrigger = UiBindingTrigger.Input;
        node.State.Version = 9;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode control = result.Children[1];
        DomNode clear = result.Children[3];
        DomNode reveal = result.Children[4];
        DomNode helper = result.Children[5];
        DomNode error = result.Children[6];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(result.Key, Is.EqualTo("input"));
            Assert.That(result.Id, Is.EqualTo("input-id"));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-input-field"));
            Assert.That(result.Children[0].Children[0].Text, Is.EqualTo("Key"));
            Assert.That(control.Name, Is.EqualTo(HtmlElements.Input));
            Assert.That(AttributeValue(control, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.Text));
            Assert.That(control.Key, Is.EqualTo("$input-control"));
            Assert.That(control.Id, Is.Not.EqualTo(result.Id));
            Assert.That(AttributeValue(control, HtmlAttributes.Placeholder), Is.EqualTo("Password"));
            Assert.That(AttributeValue(control, HtmlAttributes.InputMode), Is.EqualTo("text"));
            Assert.That(AttributeValue(control, HtmlAttributes.AutoComplete), Is.EqualTo("current-password"));
            Assert.That(AttributeValue(control, HtmlAttributes.EnterKeyHint), Is.EqualTo("done"));
            Assert.That(AttributeValue(control, HtmlAttributes.SpellCheck), Is.EqualTo("false"));
            Assert.That(AttributeValue(control, HtmlAttributes.MinimumLength), Is.EqualTo("12"));
            Assert.That(AttributeValue(control, HtmlAttributes.MaximumLength), Is.EqualTo("64"));
            Assert.That(AttributeValue(control, HtmlAttributes.Pattern), Is.EqualTo("[A-Za-z]{12,64}"));
            Assert.That(FindAttribute(control, HtmlAttributes.Required).IsBoolean, Is.True);
            Assert.That(AttributeValue(control, "data-update-trigger"), Is.EqualTo("input"));
            Assert.That(AttributeValue(control, AriaAttributes.DescribedBy), Is.EqualTo(helper.Id + " " + error.Id));
            Assert.That(AttributeValue(control, AriaAttributes.ErrorMessage), Is.EqualTo(error.Id));
            Assert.That(AttributeValue(control, AriaAttributes.Invalid), Is.EqualTo("true"));
            Assert.That(result.Children[2].Children[0].Text, Is.EqualTo("required"));
            Assert.That(AttributeValue(clear, "data-input-action"), Is.EqualTo("clear"));
            Assert.That(AttributeValue(clear, AriaAttributes.Label), Is.EqualTo("Clear secret"));
            Assert.That(clear.Children[0].Text, Is.EqualTo("Clear"));
            Assert.That(AttributeValue(reveal, "data-input-action"), Is.EqualTo("reveal"));
            Assert.That(AttributeValue(reveal, AriaAttributes.Pressed), Is.EqualTo("true"));
            Assert.That(AttributeValue(reveal, AriaAttributes.Label), Is.EqualTo("Hide secret"));
            Assert.That(reveal.Children[0].Text, Is.EqualTo("Hide"));
            Assert.That(helper.Children[0].Text, Is.EqualTo("Use twelve characters"));
            Assert.That(error.Children[0].Text, Is.EqualTo("Password is too short"));
            Assert.That(error.Version, Is.EqualTo(9));
        });
    }

    [Test]
    public void ExpandsNumericAndTemporalRangeConstraintsInvariantly()
    {
        UiNode number = InputControl(UiInputKind.Number, "quantity");
        number.Input!.Value = IntegerValue(4);
        number.Input.Minimum = IntegerValue(-2);
        number.Input.Maximum = DecimalValue(10.5m);
        number.Input.Step = FloatingValue(0.25);
        UiNode date = InputControl(UiInputKind.Date, "date");
        date.Input!.Value = DateTimeValue("2026-09-14");
        date.Input.Minimum = TextValue("2026-01-01");
        date.Input.Maximum = DateTimeValue("2026-12-31");
        date.Input.Step = TextValue("any");
        SemanticExpander expander = new SemanticExpander();

        DomNode numberResult = expander.ExpandNode(number);
        DomNode dateResult = expander.ExpandNode(date);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(numberResult, HtmlAttributes.Minimum), Is.EqualTo("-2"));
            Assert.That(AttributeValue(numberResult, HtmlAttributes.Maximum), Is.EqualTo("10.5"));
            Assert.That(AttributeValue(numberResult, HtmlAttributes.Step), Is.EqualTo("0.25"));
            Assert.That(AttributeValue(dateResult, HtmlAttributes.Minimum), Is.EqualTo("2026-01-01"));
            Assert.That(AttributeValue(dateResult, HtmlAttributes.Maximum), Is.EqualTo("2026-12-31"));
            Assert.That(AttributeValue(dateResult, HtmlAttributes.Step), Is.EqualTo("any"));
        });
    }

    [Test]
    public void PasswordRevealActionReflectsMaskedAndDisabledState()
    {
        UiNode node = InputControl(UiInputKind.Password, "pin");
        node.Input!.HasRevealAction = true;
        node.Input.IsDisabled = true;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode control = result.Children[0];
        DomNode reveal = result.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(control, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.Password));
            Assert.That(AttributeValue(reveal, AriaAttributes.Pressed), Is.EqualTo("false"));
            Assert.That(AttributeValue(reveal, AriaAttributes.Label), Is.EqualTo("Reveal pin"));
            Assert.That(reveal.Children[0].Text, Is.EqualTo("Show"));
            Assert.That(FindAttribute(reveal, HtmlAttributes.Disabled).IsBoolean, Is.True);
        });
    }

    [Test]
    public void ExpandsCanonicalFileAcceptanceMultiplicityAndHelperText()
    {
        UiNode node = InputControl(UiInputKind.File, "attachments");
        node.Input!.AcceptedFileTypes = new string[] { " image/png ", ".txt", "image/png", "application/pdf" };
        node.Input.Multiple = true;
        node.Input.HelperText = "Choose documents";
        node.Input.UpdateTrigger = UiBindingTrigger.Change;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode control = result.Children[0];
        DomNode helper = result.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(control, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.File));
            Assert.That(AttributeValue(control, HtmlAttributes.Accept), Is.EqualTo(".txt,application/pdf,image/png"));
            Assert.That(FindAttribute(control, HtmlAttributes.Multiple).IsBoolean, Is.True);
            Assert.That(AttributeValue(control, "data-update-trigger"), Is.EqualTo("change"));
            Assert.That(AttributeValue(control, AriaAttributes.DescribedBy), Is.EqualTo(helper.Id));
            Assert.That(helper.Children[0].Text, Is.EqualTo("Choose documents"));
            Assert.That(node.Input.AcceptedFileTypes, Is.EqualTo(new string[] { " image/png ", ".txt", "image/png", "application/pdf" }));
        });
    }

    [Test]
    public void ExpandsEmailMultiplicity()
    {
        UiNode node = InputControl(UiInputKind.Email, "recipients");
        node.Input!.Value = TextValue("one@example.test,two@example.test");
        node.Input.Multiple = true;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.That(FindAttribute(result, HtmlAttributes.Multiple).IsBoolean, Is.True);
    }

    [TestCase(UiBindingTrigger.Immediate, "immediate")]
    [TestCase(UiBindingTrigger.Input, "input")]
    [TestCase(UiBindingTrigger.Change, "change")]
    [TestCase(UiBindingTrigger.Blur, "blur")]
    [TestCase(UiBindingTrigger.Explicit, "explicit")]
    public void ProjectsEveryUpdateTrigger(UiBindingTrigger trigger, string token)
    {
        UiNode node = InputControl(UiInputKind.Text, "field");
        node.Input!.UpdateTrigger = trigger;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.That(AttributeValue(result, "data-update-trigger"), Is.EqualTo(token));
    }

    [Test]
    public void AppliesHintsRequiredStateAndMessagesToEditableComboFocusTarget()
    {
        UiNode node = ChoiceControl("editable-combo", "city");
        node.Input!.Value = TextValue("Sea");
        node.Input.Placeholder = "City";
        node.Input.InputMode = "search";
        node.Input.AutoComplete = "address-level2";
        node.Input.EnterKeyHint = "search";
        node.Input.HasSpellCheck = true;
        node.Input.SpellCheck = true;
        node.Input.IsRequired = true;
        node.Input.HasClearAction = true;
        node.Input.HelperText = "Start typing";
        node.Input.ErrorText = "Choose a city";
        node.Input.UpdateTrigger = UiBindingTrigger.Blur;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode combo = result.Children[0];
        DomNode editor = combo.Children[0];
        DomNode helper = result.Children[2];
        DomNode error = result.Children[3];

        Assert.Multiple(() =>
        {
            Assert.That(combo.Key, Is.EqualTo("$input-control"));
            Assert.That(AttributeValue(editor, HtmlAttributes.Placeholder), Is.EqualTo("City"));
            Assert.That(AttributeValue(editor, HtmlAttributes.InputMode), Is.EqualTo("search"));
            Assert.That(AttributeValue(editor, HtmlAttributes.AutoComplete), Is.EqualTo("address-level2"));
            Assert.That(AttributeValue(editor, HtmlAttributes.EnterKeyHint), Is.EqualTo("search"));
            Assert.That(AttributeValue(editor, HtmlAttributes.SpellCheck), Is.EqualTo("true"));
            Assert.That(FindAttribute(editor, HtmlAttributes.Required).IsBoolean, Is.True);
            Assert.That(AttributeValue(editor, "data-update-trigger"), Is.EqualTo("blur"));
            Assert.That(AttributeValue(editor, AriaAttributes.DescribedBy), Is.EqualTo(helper.Id + " " + error.Id));
            Assert.That(AttributeValue(result.Children[1], "data-input-action"), Is.EqualTo("clear"));
        });
    }

    [Test]
    public void AppliesRequiredAndUpdateTriggerToNativeSelect()
    {
        UiNode node = ChoiceControl("select", "color");
        node.Input!.AutoComplete = "off";
        node.Input.IsRequired = true;
        node.Input.UpdateTrigger = UiBindingTrigger.Explicit;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(FindAttribute(result, HtmlAttributes.Required).IsBoolean, Is.True);
            Assert.That(AttributeValue(result, HtmlAttributes.AutoComplete), Is.EqualTo("off"));
            Assert.That(AttributeValue(result, "data-update-trigger"), Is.EqualTo("explicit"));
        });
    }

    [Test]
    public void ProjectsChoiceTriggersRequiredStateAndDescriptionsToFocusTargets()
    {
        UiNode listbox = ChoiceControl("listbox", "items");
        listbox.Input!.IsRequired = true;
        listbox.Input.UpdateTrigger = UiBindingTrigger.Immediate;
        listbox.Input.ErrorText = "Select an item";
        listbox.State.IsVisible = false;
        listbox.State.IsBusy = true;
        UiNode combo = ChoiceControl("combo-box", "country");
        combo.Input!.UpdateTrigger = UiBindingTrigger.Input;
        combo.Input.HelperText = "Choose a country";
        SemanticExpander expander = new SemanticExpander();

        DomNode listboxResult = expander.ExpandNode(listbox);
        DomNode listboxControl = listboxResult.Children[0];
        DomNode error = listboxResult.Children[1];
        DomNode comboResult = expander.ExpandNode(combo);
        DomNode comboControl = comboResult.Children[0];
        DomNode comboTrigger = comboControl.Children[0];
        DomNode helper = comboResult.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(FindAttribute(listboxResult, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(listboxResult, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(listboxControl, AriaAttributes.Required), Is.EqualTo("true"));
            Assert.That(AttributeValue(listboxControl, "data-update-trigger"), Is.EqualTo("immediate"));
            Assert.That(AttributeValue(listboxControl, AriaAttributes.DescribedBy), Is.EqualTo(error.Id));
            Assert.That(AttributeValue(listboxControl, AriaAttributes.ErrorMessage), Is.EqualTo(error.Id));
            Assert.That(AttributeValue(listboxControl, AriaAttributes.Invalid), Is.EqualTo("true"));
            Assert.That(error.Children[0].Text, Is.EqualTo("Select an item"));
            Assert.That(AttributeValue(comboTrigger, "data-update-trigger"), Is.EqualTo("input"));
            Assert.That(AttributeValue(comboTrigger, AriaAttributes.DescribedBy), Is.EqualTo(helper.Id));
            Assert.That(helper.Children[0].Text, Is.EqualTo("Choose a country"));
        });
    }

    [Test]
    public void RejectsInvalidTemporalStepsFileDecorationsAndEmptyHintTokens()
    {
        UiNode temporalTextStep = InputControl(UiInputKind.Date, "date");
        temporalTextStep.Input!.Step = TextValue("daily");
        UiNode temporalZeroStep = InputControl(UiInputKind.Time, "time");
        temporalZeroStep.Input!.Step = DecimalValue(0m);
        UiNode filePrefix = InputControl(UiInputKind.File, "file");
        filePrefix.Input!.Prefix = "File";
        UiNode emptyInputMode = InputControl(UiInputKind.Text, "input-mode");
        emptyInputMode.Input!.InputMode = " ";
        UiNode emptyAutoComplete = InputControl(UiInputKind.Text, "autocomplete");
        emptyAutoComplete.Input!.AutoComplete = " ";
        UiNode emptyEnterKeyHint = InputControl(UiInputKind.Text, "enter-key");
        emptyEnterKeyHint.Input!.EnterKeyHint = " ";
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(temporalTextStep));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(temporalZeroStep));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(filePrefix));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(emptyInputMode));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(emptyAutoComplete));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(emptyEnterKeyHint));
        });
    }

    [Test]
    public void RejectsInvalidScalarOptionCombinations()
    {
        UiNode negativeMinimumLength = InputControl(UiInputKind.Text, "field");
        negativeMinimumLength.Input!.HasMinimumLength = true;
        negativeMinimumLength.Input.MinimumLength = -1;
        UiNode negativeMaximumLength = InputControl(UiInputKind.Text, "field");
        negativeMaximumLength.Input!.HasMaximumLength = true;
        negativeMaximumLength.Input.MaximumLength = -1;
        UiNode invertedLengths = InputControl(UiInputKind.Text, "field");
        invertedLengths.Input!.HasMinimumLength = true;
        invertedLengths.Input.MinimumLength = 5;
        invertedLengths.Input.HasMaximumLength = true;
        invertedLengths.Input.MaximumLength = 4;
        UiNode lengthOnNumber = InputControl(UiInputKind.Number, "field");
        lengthOnNumber.Input!.HasMaximumLength = true;
        lengthOnNumber.Input.MaximumLength = 3;
        UiNode boundOnText = InputControl(UiInputKind.Text, "field");
        boundOnText.Input!.Minimum = IntegerValue(0);
        UiNode stringNumberBound = InputControl(UiInputKind.Number, "field");
        stringNumberBound.Input!.Maximum = TextValue("10");
        UiNode integerDateBound = InputControl(UiInputKind.Date, "field");
        integerDateBound.Input!.Minimum = IntegerValue(1);
        UiNode zeroStep = InputControl(UiInputKind.Number, "field");
        zeroStep.Input!.Step = IntegerValue(0);
        UiNode wrongAccept = InputControl(UiInputKind.Text, "field");
        wrongAccept.Input!.AcceptedFileTypes = new string[] { ".txt" };
        UiNode nullAccept = InputControl(UiInputKind.File, "field");
        nullAccept.Input!.AcceptedFileTypes = null!;
        UiNode emptyAccept = InputControl(UiInputKind.File, "field");
        emptyAccept.Input!.AcceptedFileTypes = new string[] { " " };
        UiNode commaAccept = InputControl(UiInputKind.File, "field");
        commaAccept.Input!.AcceptedFileTypes = new string[] { ".txt,image/png" };
        UiNode wrongMultiple = InputControl(UiInputKind.Text, "field");
        wrongMultiple.Input!.Multiple = true;
        UiNode wrongPlaceholder = InputControl(UiInputKind.Date, "field");
        wrongPlaceholder.Input!.Placeholder = "Date";
        UiNode wrongHint = InputControl(UiInputKind.File, "field");
        wrongHint.Input!.InputMode = "text";
        UiNode wrongSpellcheck = InputControl(UiInputKind.Number, "field");
        wrongSpellcheck.Input!.HasSpellCheck = true;
        UiNode wrongReveal = InputControl(UiInputKind.Text, "field");
        wrongReveal.Input!.HasRevealAction = true;
        UiNode hiddenMessage = InputControl(UiInputKind.Hidden, "field");
        hiddenMessage.Input!.HelperText = "Hidden";
        UiNode readOnlyClear = InputControl(UiInputKind.ReadOnly, "field");
        readOnlyClear.Input!.HasClearAction = true;
        UiNode wrongRequired = InputControl(UiInputKind.Range, "field");
        wrongRequired.Input!.IsRequired = true;
        UiNode orphanRevealState = InputControl(UiInputKind.Password, "field");
        orphanRevealState.Input!.IsRevealed = true;
        UiNode undefinedTrigger = InputControl(UiInputKind.Text, "field");
        undefinedTrigger.Input!.UpdateTrigger = (UiBindingTrigger)999;
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(negativeMinimumLength))!.ParamName, Is.EqualTo("minimumLength"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(negativeMaximumLength))!.ParamName, Is.EqualTo("maximumLength"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(invertedLengths));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(lengthOnNumber));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(boundOnText));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(stringNumberBound));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(integerDateBound));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(zeroStep));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongAccept));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullAccept));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(emptyAccept));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(commaAccept));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongMultiple));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongPlaceholder));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongHint));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongSpellcheck));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongReveal));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(hiddenMessage));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(readOnlyClear));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongRequired));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(orphanRevealState));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(undefinedTrigger))!.ParamName, Is.EqualTo("updateTrigger"));
        });
    }

    [Test]
    public void RejectsOptionsThatDoNotApplyToChoiceControls()
    {
        UiNode range = ChoiceControl("select", "field");
        range.Input!.Minimum = IntegerValue(0);
        UiNode accept = ChoiceControl("listbox", "field");
        accept.Input!.AcceptedFileTypes = new string[] { ".txt" };
        UiNode reveal = ChoiceControl("autocomplete", "field");
        reveal.Input!.HasRevealAction = true;
        UiNode spellcheck = ChoiceControl("select", "field");
        spellcheck.Input!.HasSpellCheck = true;
        UiNode hint = ChoiceControl("listbox", "field");
        hint.Input!.InputMode = "text";
        UiNode autocomplete = ChoiceControl("combo-box", "field");
        autocomplete.Input!.AutoComplete = "off";
        UiNode revealed = ChoiceControl("select", "field");
        revealed.Input!.IsRevealed = true;
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(range));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(accept));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(reveal));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(spellcheck));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(hint));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(autocomplete));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(revealed));
        });
    }

    [Test]
    public void RepeatedDecoratedExpansionIsFreshStableAndDoesNotMutateOptions()
    {
        UiNode node = InputControl(UiInputKind.Search, "query");
        node.Input!.Value = TextValue("term");
        node.Input.Prefix = "Search";
        node.Input.HelperText = "Type a query";
        string[] accepted = node.Input.AcceptedFileTypes;
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);
        first.Children[0].Children[0].Text = "Changed";
        first.Children[1].Attributes[0].Value = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(first.Children[1], Is.Not.SameAs(second.Children[1]));
            Assert.That(first.Children[1].Id, Is.EqualTo(second.Children[1].Id));
            Assert.That(second.Children[0].Children[0].Text, Is.EqualTo("Search"));
            Assert.That(node.Input.Prefix, Is.EqualTo("Search"));
            Assert.That(node.Input.HelperText, Is.EqualTo("Type a query"));
            Assert.That(node.Input.AcceptedFileTypes, Is.SameAs(accepted));
        });
    }

    private static UiNode InputControl(UiInputKind kind, string name)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "input", SiblingIdentity = "input", StableId = "input-id" },
            Input = new UiInput { Kind = kind, Name = name }
        };
    }

    private static UiNode ChoiceControl(string name, string fieldName)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = name,
            Identity = new UiNodeIdentity { AuthorKey = "choice", SiblingIdentity = "choice", StableId = "choice-id" },
            Input = new UiInput
            {
                Name = fieldName,
                Choices = new UiChoice[]
                {
                    new UiChoice { Key = "one", Label = "One", Value = TextValue("one"), IsSelected = true }
                }
            }
        };
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue IntegerValue(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiValue FloatingValue(double value)
    {
        return new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = value };
    }

    private static UiValue DecimalValue(decimal value)
    {
        return new UiValue { Kind = UiValueKind.Decimal, DecimalValue = value };
    }

    private static UiValue DateTimeValue(string value)
    {
        return new UiValue { Kind = UiValueKind.DateTime, DateTimeValue = value };
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
