using System;
using System.Collections.Generic;

namespace htmlgui;

internal class ChoiceTemplateExpander
{
    internal bool IsChoiceControlName(string name)
    {
        switch (name)
        {
            case "checkbox":
            case "checkbox-input":
            case "radio":
            case "radio-input":
            case "radio-group":
            case "radiogroup":
            case "switch":
            case "switch-input":
            case "select":
            case "select-input":
            case "multi-select":
            case "multiselect":
            case "multi-select-input":
            case "listbox":
            case "list-box":
            case "listbox-input":
            case "combo-box":
            case "combobox":
            case "combobox-input":
            case "autocomplete":
            case "autocomplete-input":
            case "editable-combo":
            case "editable-combo-box":
            case "editable-combobox":
            case "segmented-choice":
            case "segmented-control":
            case "segmented":
                return true;
            default:
                return false;
        }
    }

    internal DomNode ExpandChoiceControl(UiNode node)
    {
        UiInput input = RequireInput(node);
        switch (node.Name)
        {
            case "checkbox":
            case "checkbox-input":
                return ExpandBinaryChoice(node, input, false);
            case "switch":
            case "switch-input":
                return ExpandBinaryChoice(node, input, true);
            case "radio":
            case "radio-input":
            case "radio-group":
            case "radiogroup":
                return ExpandRadioGroup(node, input);
            case "select":
            case "select-input":
                return ExpandNativeSelect(node, input, input.Multiple);
            case "multi-select":
            case "multiselect":
            case "multi-select-input":
                return ExpandNativeSelect(node, input, true);
            case "listbox":
            case "list-box":
            case "listbox-input":
                return ExpandListBox(node, input);
            case "combo-box":
            case "combobox":
            case "combobox-input":
                return ExpandSelectOnlyCombo(node, input);
            case "autocomplete":
            case "autocomplete-input":
                return ExpandEditableCombo(node, input, false);
            case "editable-combo":
            case "editable-combo-box":
            case "editable-combobox":
                return ExpandEditableCombo(node, input, true);
            case "segmented-choice":
            case "segmented-control":
            case "segmented":
                return ExpandSegmentedChoice(node, input);
            default:
                throw CreateRenderException(node, "The choice control name is not supported.");
        }
    }

    internal DomNode ExpandBinaryChoice(UiNode node, UiInput input, bool isSwitch)
    {
        ValidateBinaryValue(node, input.Value);
        UiCommandCheckState checkState = GetCheckState(node, input.Value);
        if (isSwitch && checkState == UiCommandCheckState.Mixed)
        {
            throw CreateRenderException(node, "Switch controls cannot use a mixed check state.");
        }

        string kindToken = "checkbox";
        if (isSwitch)
        {
            kindToken = "switch";
        }

        List<DomAttribute> attributes = CreateNativeAttributes(node, input, "htmlgui-choice htmlgui-" + kindToken);
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Checkbox });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Value, Value = "true" });
        if (checkState == UiCommandCheckState.Checked)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Checked, Value = string.Empty, IsBoolean = true });
        }

        if (isSwitch)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Checked, Value = FormatCheckState(checkState) });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Switch });
        }
        else if (checkState == UiCommandCheckState.Mixed)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Checked, Value = "mixed" });
        }

        SortAttributes(attributes);
        DomProperty[] properties = new DomProperty[]
        {
            CreateBooleanProperty(HtmlAttributes.Checked, checkState == UiCommandCheckState.Checked),
            CreateBooleanProperty("indeterminate", checkState == UiCommandCheckState.Mixed)
        };
        return CreateElement(node, HtmlElements.Input, attributes.ToArray(), properties, Array.Empty<DomNode>());
    }

    internal DomNode ExpandRadioGroup(UiNode node, UiInput input)
    {
        UiChoice[] choices = ValidateChoices(node, input, false);
        List<DomAttribute> attributes = CreateContainerAttributes(node, input, "htmlgui-choice-group htmlgui-radio-group");
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.RadioGroup });
        SortAttributes(attributes);
        DomNode[] children = new DomNode[choices.Length];
        int index = 0;
        while (index < choices.Length)
        {
            children[index] = CreateRadioChoice(node, input, choices[index]);
            index++;
        }

        return CreateElement(node, HtmlElements.Division, attributes.ToArray(), Array.Empty<DomProperty>(), children);
    }

    internal DomNode ExpandNativeSelect(UiNode node, UiInput input, bool multiple)
    {
        UiChoice[] choices = ValidateChoices(node, input, multiple);
        string className = "htmlgui-choice htmlgui-select";
        if (multiple)
        {
            className = "htmlgui-choice htmlgui-multi-select";
        }

        List<DomAttribute> attributes = CreateNativeAttributes(node, input, className);
        if (multiple)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Multiple, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(attributes);
        DomNode[] options = CreateOptionNodes(node, choices);
        DomProperty[] properties = Array.Empty<DomProperty>();
        if (!multiple)
        {
            properties = CreateSelectedValueProperty(choices);
        }

        return CreateElement(node, HtmlElements.Select, attributes.ToArray(), properties, options);
    }

    internal DomNode ExpandListBox(UiNode node, UiInput input)
    {
        UiChoice[] choices = ValidateChoices(node, input, input.Multiple);
        List<DomAttribute> attributes = CreateCompositeAttributes(node, input, "htmlgui-choice htmlgui-listbox");
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.ListBox });
        if (input.Multiple)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.MultiSelectable, Value = "true" });
        }

        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Division, attributes.ToArray(), Array.Empty<DomProperty>(), CreateAriaOptions(node, input, choices, "listbox-option"));
    }

    internal DomNode ExpandSelectOnlyCombo(UiNode node, UiInput input)
    {
        UiChoice[] choices = ValidateChoices(node, input, false);
        bool expanded = IsExpanded(node);
        string popupId = CreateSyntheticId(node, "combo-popup", input.Name);
        List<DomAttribute> rootAttributes = CreateContainerAttributes(node, input, "htmlgui-choice htmlgui-combo-box");
        DomNode trigger = CreateComboTrigger(node, input, choices, popupId, expanded);
        DomNode popup = CreateComboPopup(node, input, choices, popupId, expanded, "combo-option");
        return CreateElement(node, HtmlElements.Division, rootAttributes.ToArray(), Array.Empty<DomProperty>(), new DomNode[] { trigger, popup });
    }

    internal DomNode ExpandEditableCombo(UiNode node, UiInput input, bool allowsCustomValue)
    {
        ValidateTextValue(node, input.Value);
        UiChoice[] choices = ValidateChoices(node, input, false);
        bool expanded = IsExpanded(node);
        string popupId = CreateSyntheticId(node, "combo-popup", input.Name);
        string value = new InputTemplateExpander().FormatScalarValue(input.Value);
        List<DomAttribute> rootAttributes = CreateContainerAttributes(node, input, "htmlgui-choice htmlgui-editable-combo");
        if (!allowsCustomValue)
        {
            rootAttributes = CreateContainerAttributes(node, input, "htmlgui-choice htmlgui-autocomplete");
        }

        DomNode editor = CreateComboEditor(node, input, popupId, expanded, value, allowsCustomValue);
        DomNode popup = CreateComboPopup(node, input, choices, popupId, expanded, "combo-option");
        return CreateElement(node, HtmlElements.Division, rootAttributes.ToArray(), Array.Empty<DomProperty>(), new DomNode[] { editor, popup });
    }

    internal DomNode ExpandSegmentedChoice(UiNode node, UiInput input)
    {
        UiChoice[] choices = ValidateChoices(node, input, false);
        List<DomAttribute> attributes = CreateContainerAttributes(node, input, "htmlgui-choice-group htmlgui-segmented-choice");
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.RadioGroup });
        SortAttributes(attributes);
        DomNode[] children = new DomNode[choices.Length];
        int activeIndex = FindSegmentTabIndex(node, input, choices);
        int index = 0;
        while (index < choices.Length)
        {
            children[index] = CreateSegment(node, input, choices[index], index == activeIndex);
            index++;
        }

        return CreateElement(node, HtmlElements.Division, attributes.ToArray(), Array.Empty<DomProperty>(), children);
    }

    internal DomNode CreateRadioChoice(UiNode node, UiInput input, UiChoice choice)
    {
        string choiceId = CreateSyntheticId(node, "radio", choice.Key);
        List<DomAttribute> inputAttributes = new List<DomAttribute>();
        inputAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-radio-input" });
        inputAttributes.Add(new DomAttribute { Name = HtmlAttributes.Name, Value = input.Name });
        inputAttributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Radio });
        inputAttributes.Add(new DomAttribute { Name = HtmlAttributes.Value, Value = FormatChoiceValue(choice.Value) });
        AddNativeDisabled(inputAttributes, IsChoiceDisabled(node, input, choice));
        if (choice.IsSelected)
        {
            inputAttributes.Add(new DomAttribute { Name = HtmlAttributes.Checked, Value = string.Empty, IsBoolean = true });
        }

        AddExplicitTabIndex(node, inputAttributes);
        SortAttributes(inputAttributes);
        DomNode radio = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$radio-input",
            Id = choiceId,
            Name = HtmlElements.Input,
            Attributes = inputAttributes.ToArray(),
            Properties = new DomProperty[] { CreateBooleanProperty(HtmlAttributes.Checked, choice.IsSelected) },
            Version = GetNodeVersion(node)
        };
        DomNode labelText = CreateTextNode(node, "$radio-label", "radio-label", choice.Key, choice.Label);
        DomNode label = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = choice.Key,
            Id = CreateSyntheticId(node, "radio-label", choice.Key),
            Name = HtmlElements.Label,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-radio-choice" } },
            Children = new DomNode[] { radio, labelText },
            Version = GetNodeVersion(node)
        };
        return label;
    }

    internal DomNode[] CreateOptionNodes(UiNode node, UiChoice[] choices)
    {
        DomNode[] result = new DomNode[choices.Length];
        int index = 0;
        while (index < choices.Length)
        {
            UiChoice choice = choices[index];
            List<DomAttribute> attributes = new List<DomAttribute>();
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "choice-key", Value = choice.Key });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Value, Value = FormatChoiceValue(choice.Value) });
            if (choice.IsDisabled)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
            }

            if (choice.IsSelected)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.Selected, Value = string.Empty, IsBoolean = true });
            }

            SortAttributes(attributes);
            result[index] = new DomNode
            {
                Kind = DomNodeKind.Element,
                Key = choice.Key,
                Id = CreateSyntheticId(node, "option", choice.Key),
                Name = HtmlElements.Option,
                Attributes = attributes.ToArray(),
                Properties = new DomProperty[] { CreateBooleanProperty(HtmlAttributes.Selected, choice.IsSelected) },
                Children = new DomNode[] { CreateTextNode(node, "$option-label", "option-label", choice.Key, choice.Label) },
                Version = GetNodeVersion(node)
            };
            index++;
        }

        return result;
    }

    internal DomNode[] CreateAriaOptions(UiNode node, UiInput input, UiChoice[] choices, string suffix)
    {
        DomNode[] result = new DomNode[choices.Length];
        int index = 0;
        while (index < choices.Length)
        {
            UiChoice choice = choices[index];
            List<DomAttribute> attributes = new List<DomAttribute>();
            attributes.Add(new DomAttribute { Name = AriaAttributes.Selected, Value = FormatBoolean(choice.IsSelected) });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-choice-option" });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "value", Value = FormatChoiceValue(choice.Value) });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Option });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = "-1" });
            if (IsChoiceDisabled(node, input, choice))
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
            }

            SortAttributes(attributes);
            result[index] = new DomNode
            {
                Kind = DomNodeKind.Element,
                Key = choice.Key,
                Id = CreateSyntheticId(node, suffix, choice.Key),
                Name = HtmlElements.Division,
                Attributes = attributes.ToArray(),
                Children = new DomNode[] { CreateTextNode(node, "$choice-label", "choice-label", choice.Key, choice.Label) },
                Version = GetNodeVersion(node)
            };
            index++;
        }

        return result;
    }

    internal DomNode CreateComboTrigger(UiNode node, UiInput input, UiChoice[] choices, string popupId, bool expanded)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Controls, Value = popupId });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Expanded, Value = FormatBoolean(expanded) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-combo-trigger" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "field-name", Value = input.Name });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.ComboBox });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        AddNativeDisabled(attributes, IsInputDisabled(node, input));
        AddExplicitTabIndex(node, attributes);
        SortAttributes(attributes);
        string label = FindSelectedLabel(choices);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$combo-trigger",
            Id = CreateSyntheticId(node, "combo-trigger", input.Name),
            Name = HtmlElements.Button,
            Attributes = attributes.ToArray(),
            Children = new DomNode[] { CreateTextNode(node, "$combo-value", "combo-value", input.Name, label) },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateComboEditor(UiNode node, UiInput input, string popupId, bool expanded, string value, bool allowsCustomValue)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        if (allowsCustomValue)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.AutoComplete, Value = "both" });
        }
        else
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.AutoComplete, Value = "list" });
        }

        attributes.Add(new DomAttribute { Name = AriaAttributes.Controls, Value = popupId });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Expanded, Value = FormatBoolean(expanded) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-combo-editor" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Name, Value = input.Name });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.ComboBox });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Text });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Value, Value = value });
        AddNativeDisabled(attributes, IsInputDisabled(node, input));
        AddExplicitTabIndex(node, attributes);
        SortAttributes(attributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$combo-editor",
            Id = CreateSyntheticId(node, "combo-editor", input.Name),
            Name = HtmlElements.Input,
            Attributes = attributes.ToArray(),
            Properties = new DomProperty[] { CreateStringProperty(HtmlAttributes.Value, value) },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateComboPopup(UiNode node, UiInput input, UiChoice[] choices, string popupId, bool expanded, string suffix)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-combo-popup" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.ListBox });
        if (!expanded)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(attributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$combo-popup",
            Id = popupId,
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = CreateAriaOptions(node, input, choices, suffix),
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateSegment(UiNode node, UiInput input, UiChoice choice, bool isTabStop)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Checked, Value = FormatBoolean(choice.IsSelected) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-segment" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "value", Value = FormatChoiceValue(choice.Value) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Radio });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        AddNativeDisabled(attributes, IsChoiceDisabled(node, input, choice));
        if (isTabStop)
        {
            AddExplicitTabIndexOrDefault(node, attributes, "0");
        }
        else
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = "-1" });
        }

        SortAttributes(attributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = choice.Key,
            Id = CreateSyntheticId(node, "segment", choice.Key),
            Name = HtmlElements.Button,
            Attributes = attributes.ToArray(),
            Children = new DomNode[] { CreateTextNode(node, "$segment-label", "segment-label", choice.Key, choice.Label) },
            Version = GetNodeVersion(node)
        };
    }

    internal UiInput RequireInput(UiNode node)
    {
        UiInput? input = node.Input;
        if (input == null)
        {
            throw CreateRenderException(node, "A choice control requires an input description.");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw CreateRenderException(node, "Choice controls require a non-empty field name.");
        }

        if (input.Value == null)
        {
            throw CreateRenderException(node, "Choice controls require a portable value.");
        }

        ValueJsonCodec.ValidateValue(input.Value, 64, 10000, 1048576, 1048576);
        return input;
    }

    internal UiChoice[] ValidateChoices(UiNode node, UiInput input, bool allowsMultiple)
    {
        UiChoice[]? choices = input.Choices;
        if (choices == null)
        {
            throw CreateRenderException(node, "Choice collections cannot be null.");
        }

        int selectedCount = 0;
        int index = 0;
        while (index < choices.Length)
        {
            UiChoice? choice = choices[index];
            if (choice == null || string.IsNullOrWhiteSpace(choice.Key))
            {
                throw CreateRenderException(node, "Choices require non-empty stable keys.");
            }

            if (choice.Label == null)
            {
                throw CreateRenderException(node, "Choice labels cannot be null.");
            }

            if (choice.Value == null)
            {
                throw CreateRenderException(node, "Choices require portable values.");
            }

            ValueJsonCodec.ValidateValue(choice.Value, 64, 10000, 1048576, 1048576);
            if (choice.Value.Kind == UiValueKind.Array || choice.Value.Kind == UiValueKind.Object)
            {
                throw CreateRenderException(node, "Choice values must be scalar.");
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (choices[earlierIndex].Key == choice.Key)
                {
                    throw CreateRenderException(node, "Choice keys must be unique.");
                }

                earlierIndex++;
            }

            if (choice.IsSelected)
            {
                selectedCount++;
            }

            index++;
        }

        if (!allowsMultiple && selectedCount > 1)
        {
            throw CreateRenderException(node, "This choice control permits at most one selected choice.");
        }

        return choices;
    }

    internal void ValidateBinaryValue(UiNode node, UiValue value)
    {
        if (value.Kind != UiValueKind.Null && value.Kind != UiValueKind.Boolean)
        {
            throw CreateRenderException(node, "Checkbox and switch controls require a Boolean or null value.");
        }
    }

    internal void ValidateTextValue(UiNode node, UiValue value)
    {
        if (value.Kind != UiValueKind.Null && value.Kind != UiValueKind.String)
        {
            throw CreateRenderException(node, "Editable combo controls require a string or null value.");
        }
    }

    internal UiCommandCheckState GetCheckState(UiNode node, UiValue value)
    {
        UiNodeState? state = node.State;
        if (state != null && state.HasCheckState)
        {
            switch (state.CheckState)
            {
                case UiCommandCheckState.Unchecked:
                case UiCommandCheckState.Checked:
                case UiCommandCheckState.Mixed:
                    return state.CheckState;
                default:
                    throw new ArgumentOutOfRangeException("checkState", state.CheckState, "The check state is not supported.");
            }
        }

        if (value.Kind == UiValueKind.Boolean && value.BooleanValue)
        {
            return UiCommandCheckState.Checked;
        }

        return UiCommandCheckState.Unchecked;
    }

    internal List<DomAttribute> CreateNativeAttributes(UiNode node, UiInput input, string className)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = className });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Name, Value = input.Name });
        AddNativeDisabled(attributes, IsInputDisabled(node, input));
        AddNodePresentationState(node, attributes);
        AddExplicitTabIndex(node, attributes);
        SortAttributes(attributes);
        return attributes;
    }

    internal List<DomAttribute> CreateContainerAttributes(UiNode node, UiInput input, string className)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = className });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "field-name", Value = input.Name });
        if (IsInputDisabled(node, input))
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }

        AddNodePresentationState(node, attributes);
        SortAttributes(attributes);
        return attributes;
    }

    internal List<DomAttribute> CreateCompositeAttributes(UiNode node, UiInput input, string className)
    {
        List<DomAttribute> attributes = CreateContainerAttributes(node, input, className);
        string fallback = "0";
        if (IsInputDisabled(node, input))
        {
            fallback = "-1";
        }

        AddExplicitTabIndexOrDefault(node, attributes, fallback);
        SortAttributes(attributes);
        return attributes;
    }

    internal void AddNodePresentationState(UiNode node, List<DomAttribute> attributes)
    {
        UiNodeState? state = node.State;
        if (state == null)
        {
            return;
        }

        if (!state.IsVisible)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (state.IsBusy)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }
    }

    internal void AddNativeDisabled(List<DomAttribute> attributes, bool disabled)
    {
        if (disabled)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }
    }

    internal void AddExplicitTabIndex(UiNode node, List<DomAttribute> attributes)
    {
        if (node.State != null && node.State.HasTabIndex)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = InvariantFormatter.FormatInt32(node.State.TabIndex) });
        }
    }

    internal void AddExplicitTabIndexOrDefault(UiNode node, List<DomAttribute> attributes, string fallback)
    {
        if (node.State != null && node.State.HasTabIndex)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = InvariantFormatter.FormatInt32(node.State.TabIndex) });
        }
        else
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = fallback });
        }
    }

    internal bool IsInputDisabled(UiNode node, UiInput input)
    {
        if (input.IsDisabled)
        {
            return true;
        }

        return node.State != null && !node.State.IsEnabled;
    }

    internal bool IsChoiceDisabled(UiNode node, UiInput input, UiChoice choice)
    {
        return choice.IsDisabled || IsInputDisabled(node, input);
    }

    internal bool IsExpanded(UiNode node)
    {
        return node.State != null && node.State.HasExpanded && node.State.IsExpanded;
    }

    internal string FindSelectedLabel(UiChoice[] choices)
    {
        int index = 0;
        while (index < choices.Length)
        {
            if (choices[index].IsSelected)
            {
                return choices[index].Label;
            }

            index++;
        }

        return string.Empty;
    }

    internal int FindSegmentTabIndex(UiNode node, UiInput input, UiChoice[] choices)
    {
        int firstEnabledIndex = -1;
        int index = 0;
        while (index < choices.Length)
        {
            if (!IsChoiceDisabled(node, input, choices[index]))
            {
                if (firstEnabledIndex < 0)
                {
                    firstEnabledIndex = index;
                }

                if (choices[index].IsSelected)
                {
                    return index;
                }
            }

            index++;
        }

        return firstEnabledIndex;
    }

    internal DomProperty[] CreateSelectedValueProperty(UiChoice[] choices)
    {
        int index = 0;
        while (index < choices.Length)
        {
            if (choices[index].IsSelected)
            {
                return new DomProperty[] { CreateStringProperty(HtmlAttributes.Value, FormatChoiceValue(choices[index].Value)) };
            }

            index++;
        }

        return new DomProperty[] { CreateStringProperty(HtmlAttributes.Value, string.Empty) };
    }

    internal DomProperty CreateBooleanProperty(string name, bool value)
    {
        return new DomProperty
        {
            Name = name,
            Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = value }
        };
    }

    internal DomProperty CreateStringProperty(string name, string value)
    {
        return new DomProperty
        {
            Name = name,
            Value = new UiValue { Kind = UiValueKind.String, StringValue = value }
        };
    }

    internal DomNode CreateTextNode(UiNode node, string key, string suffix, string identity, string text)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = key,
            Id = CreateSyntheticId(node, suffix, identity),
            Text = text,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateElement(UiNode node, string name, DomAttribute[] attributes, DomProperty[] properties, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = name,
            Attributes = attributes,
            Properties = properties,
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal string FormatChoiceValue(UiValue value)
    {
        return new InputTemplateExpander().FormatScalarValue(value);
    }

    internal string FormatCheckState(UiCommandCheckState state)
    {
        if (state == UiCommandCheckState.Checked)
        {
            return "true";
        }

        if (state == UiCommandCheckState.Mixed)
        {
            return "mixed";
        }

        return "false";
    }

    internal string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }

    internal string CreateSyntheticId(UiNode node, string suffix, string identity)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "choice", suffix, identity });
        return "ui-choice-" + token.Substring(0, 20);
    }

    internal string GetNodeKey(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        if (node.Identity.AuthorKey.Length > 0)
        {
            return node.Identity.AuthorKey;
        }

        return node.Identity.SiblingIdentity;
    }

    internal string GetNodeId(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        return node.Identity.StableId;
    }

    internal int GetNodeVersion(UiNode node)
    {
        if (node.State == null)
        {
            return 0;
        }

        return node.State.Version;
    }

    internal void SortAttributes(List<DomAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            DomAttribute candidate = attributes[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(attributes[insertionIndex].Name, candidate.Name) > 0)
            {
                attributes[insertionIndex + 1] = attributes[insertionIndex];
                insertionIndex--;
            }

            attributes[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal UiRenderException CreateRenderException(UiNode node, string message)
    {
        string nodeId = GetNodeId(node);
        if (nodeId.Length == 0)
        {
            nodeId = "unknown";
        }

        return new UiRenderException(message, nodeId, "dom");
    }
}
