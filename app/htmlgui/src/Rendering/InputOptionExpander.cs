using System;
using System.Collections.Generic;

namespace htmlgui;

internal class InputOptionExpander
{
    internal DomNode ApplyScalarOptions(UiNode node, UiInput input, DomNode control)
    {
        ValidateScalarOptions(node, input);
        List<DomAttribute> attributes = CopyAttributes(control.Attributes);
        AddUpdateTrigger(node, input, attributes);
        AddPlaceholder(node, input, attributes);
        AddEditingHints(node, input, attributes);
        AddConstraints(node, input, attributes);
        AddFileOptions(node, input, attributes);
        AddDescriptions(node, input, attributes);
        SortAttributes(attributes);
        control.Attributes = attributes.ToArray();
        return DecorateControl(node, input, control);
    }

    internal DomNode ApplyChoiceOptions(UiNode node, UiInput input, DomNode control)
    {
        ValidateChoiceOptions(node, input);
        DomNode focusTarget = FindChoiceFocusTarget(control);
        List<DomAttribute> attributes = CopyAttributes(focusTarget.Attributes);
        AddUpdateTrigger(node, input, attributes);
        if (input.IsRequired)
        {
            if (focusTarget.Name == HtmlElements.Input || focusTarget.Name == HtmlElements.Select)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.Required, Value = string.Empty, IsBoolean = true });
            }
            else
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.Required, Value = "true" });
            }
        }

        AddChoiceEditingHints(node, input, focusTarget, attributes);
        AddDescriptions(node, input, attributes);
        SortAttributes(attributes);
        focusTarget.Attributes = attributes.ToArray();
        return DecorateControl(node, input, control);
    }

    internal void ValidateScalarOptions(UiNode node, UiInput input)
    {
        GetUpdateTriggerToken(input.UpdateTrigger);
        ValidateLengthConstraints(node, input);
        ValidatePatternConstraint(node, input);
        ValidateRangeConstraints(node, input);
        ValidateFileOptions(node, input);
        ValidateEditingHints(node, input);
        if (input.HasRevealAction && input.Kind != UiInputKind.Password)
        {
            throw CreateRenderException(node, "Only password inputs can expose a reveal action.");
        }

        if (input.IsRevealed && (!input.HasRevealAction || input.Kind != UiInputKind.Password))
        {
            throw CreateRenderException(node, "Only password inputs with reveal actions can be revealed.");
        }

        if (input.Kind == UiInputKind.Hidden && HasDecorations(input))
        {
            throw CreateRenderException(node, "Hidden inputs cannot use visible decorations or messages.");
        }

        if ((input.HasClearAction || HasDecorativeText(input)) && input.Kind == UiInputKind.File)
        {
            throw CreateRenderException(node, "File inputs cannot use text adornments or clear actions.");
        }

        if (input.HasClearAction && input.Kind == UiInputKind.ReadOnly)
        {
            throw CreateRenderException(node, "Read-only inputs cannot expose a clear action.");
        }

        if (input.IsRequired && (input.Kind == UiInputKind.Hidden || input.Kind == UiInputKind.ReadOnly || input.Kind == UiInputKind.Range || input.Kind == UiInputKind.Color))
        {
            throw CreateRenderException(node, "This input kind does not support the required constraint.");
        }
    }

    internal void ValidateChoiceOptions(UiNode node, UiInput input)
    {
        GetUpdateTriggerToken(input.UpdateTrigger);
        if (input.Minimum != null || input.Maximum != null || input.Step != null || input.Pattern != null || input.HasMinimumLength || input.HasMaximumLength)
        {
            throw CreateRenderException(node, "Choice controls do not support scalar range or length constraints.");
        }

        if (input.AcceptedFileTypes == null || input.AcceptedFileTypes.Length > 0)
        {
            throw CreateRenderException(node, "Choice controls do not support accepted file types.");
        }

        if (input.HasRevealAction)
        {
            throw CreateRenderException(node, "Choice controls cannot expose a password reveal action.");
        }

        if (input.IsRevealed)
        {
            throw CreateRenderException(node, "Choice controls cannot expose revealed password state.");
        }

        if (input.HasSpellCheck && !IsEditableChoiceName(node.Name))
        {
            throw CreateRenderException(node, "Spellcheck is supported only by editable choice controls.");
        }

        if ((input.InputMode != null || input.EnterKeyHint != null || input.Placeholder != null) && !IsEditableChoiceName(node.Name))
        {
            throw CreateRenderException(node, "Editing hints are supported only by editable choice controls.");
        }


        if (input.AutoComplete != null && !SupportsChoiceAutoComplete(node.Name))
        {
            throw CreateRenderException(node, "Autocomplete is not supported by this choice control.");
        }
    }

    internal void ValidateLengthConstraints(UiNode node, UiInput input)
    {
        if (!input.HasMinimumLength && !input.HasMaximumLength)
        {
            return;
        }

        if (!IsTextualKind(input.Kind))
        {
            throw CreateRenderException(node, "Length constraints require a text-like input.");
        }

        if (input.HasMinimumLength && input.MinimumLength < 0)
        {
            throw new ArgumentOutOfRangeException("minimumLength", input.MinimumLength, "Minimum length cannot be negative.");
        }

        if (input.HasMaximumLength && input.MaximumLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLength", input.MaximumLength, "Maximum length cannot be negative.");
        }

        if (input.HasMinimumLength && input.HasMaximumLength && input.MinimumLength > input.MaximumLength)
        {
            throw CreateRenderException(node, "Minimum length cannot exceed maximum length.");
        }
    }

    internal void ValidateRangeConstraints(UiNode node, UiInput input)
    {
        ValidateBoundValue(node, input, input.Minimum, false);
        ValidateBoundValue(node, input, input.Maximum, false);
        ValidateBoundValue(node, input, input.Step, true);
    }

    internal void ValidatePatternConstraint(UiNode node, UiInput input)
    {
        if (input.Pattern != null && (!IsTextualKind(input.Kind) || input.Kind == UiInputKind.Multiline))
        {
            throw CreateRenderException(node, "Pattern constraints require a text-like input.");
        }
    }

    internal void ValidateBoundValue(UiNode node, UiInput input, UiValue? value, bool isStep)
    {
        if (value == null)
        {
            return;
        }

        ValueJsonCodec.ValidateValue(value, 64, 10000, 1048576, 1048576);
        if (input.Kind == UiInputKind.Number || input.Kind == UiInputKind.Range)
        {
            if (!IsNumericValue(value) && !(isStep && IsAnyStep(value)))
            {
                throw CreateRenderException(node, "Numeric range constraints require numeric values or an any step.");
            }

            if (isStep && IsNumericValue(value) && !IsPositiveNumericValue(value))
            {
                throw CreateRenderException(node, "Numeric step constraints must be positive.");
            }

            return;
        }

        if (IsTemporalKind(input.Kind))
        {
            if (isStep)
            {
                if (!IsNumericValue(value) && !IsAnyStep(value))
                {
                    throw CreateRenderException(node, "Temporal step constraints require numeric values or an any step.");
                }

                if (IsNumericValue(value) && !IsPositiveNumericValue(value))
                {
                    throw CreateRenderException(node, "Temporal step constraints must be positive.");
                }
            }
            else if (value.Kind != UiValueKind.String && value.Kind != UiValueKind.DateTime)
            {
                throw CreateRenderException(node, "Temporal bounds require text or date-time values.");
            }

            return;
        }

        throw CreateRenderException(node, "Range constraints require a numeric or temporal input.");
    }

    internal void ValidateFileOptions(UiNode node, UiInput input)
    {
        string[]? accepted = input.AcceptedFileTypes;
        if (accepted == null)
        {
            throw CreateRenderException(node, "Accepted file types cannot be null.");
        }

        if (accepted.Length > 0 && input.Kind != UiInputKind.File)
        {
            throw CreateRenderException(node, "Accepted file types can be used only with file inputs.");
        }

        if (input.Multiple && input.Kind != UiInputKind.File && input.Kind != UiInputKind.Email)
        {
            throw CreateRenderException(node, "Multiple values are supported only by file and email inputs.");
        }

        int index = 0;
        while (index < accepted.Length)
        {
            string? value = accepted[index];
            if (string.IsNullOrWhiteSpace(value) || value.Contains(",", StringComparison.Ordinal))
            {
                throw CreateRenderException(node, "Accepted file types must be non-empty MIME types or extensions without commas.");
            }

            index++;
        }
    }

    internal void ValidateEditingHints(UiNode node, UiInput input)
    {
        if (input.Placeholder != null && !SupportsPlaceholder(input.Kind))
        {
            throw CreateRenderException(node, "This input kind does not support placeholder text.");
        }

        if ((input.InputMode != null || input.AutoComplete != null || input.EnterKeyHint != null || input.HasSpellCheck) && !SupportsEditingHints(input.Kind))
        {
            throw CreateRenderException(node, "This input kind does not support text editing hints.");
        }

        if (input.HasSpellCheck && !IsTextualKind(input.Kind))
        {
            throw CreateRenderException(node, "Spellcheck requires a text-like input.");
        }
    }

    internal void AddUpdateTrigger(UiNode node, UiInput input, List<DomAttribute> attributes)
    {
        attributes.Add(new DomAttribute
        {
            Name = HtmlAttributes.DataPrefix + "update-trigger",
            Value = GetUpdateTriggerToken(input.UpdateTrigger)
        });
    }

    internal void AddPlaceholder(UiNode node, UiInput input, List<DomAttribute> attributes)
    {
        if (input.Placeholder != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Placeholder, Value = input.Placeholder });
        }
    }

    internal void AddEditingHints(UiNode node, UiInput input, List<DomAttribute> attributes)
    {
        if (input.InputMode != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.InputMode, Value = RequireToken(node, input.InputMode, "Input mode") });
        }

        if (input.AutoComplete != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.AutoComplete, Value = RequireToken(node, input.AutoComplete, "Autocomplete") });
        }

        if (input.EnterKeyHint != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.EnterKeyHint, Value = RequireToken(node, input.EnterKeyHint, "Enter-key hint") });
        }

        if (input.HasSpellCheck)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.SpellCheck, Value = FormatBoolean(input.SpellCheck) });
        }
    }

    internal void AddChoiceEditingHints(UiNode node, UiInput input, DomNode focusTarget, List<DomAttribute> attributes)
    {
        if (input.AutoComplete != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.AutoComplete, Value = RequireToken(node, input.AutoComplete, "Autocomplete") });
        }

        if (IsEditableChoiceName(node.Name))
        {
            if (input.Placeholder != null)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.Placeholder, Value = input.Placeholder });
            }

            if (input.InputMode != null)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.InputMode, Value = RequireToken(node, input.InputMode, "Input mode") });
            }

            if (input.EnterKeyHint != null)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.EnterKeyHint, Value = RequireToken(node, input.EnterKeyHint, "Enter-key hint") });
            }

            if (input.HasSpellCheck)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.SpellCheck, Value = FormatBoolean(input.SpellCheck) });
            }
        }
    }

    internal void AddConstraints(UiNode node, UiInput input, List<DomAttribute> attributes)
    {
        AddOptionalConstraint(node, attributes, HtmlAttributes.Minimum, input.Minimum);
        AddOptionalConstraint(node, attributes, HtmlAttributes.Maximum, input.Maximum);
        AddOptionalConstraint(node, attributes, HtmlAttributes.Step, input.Step);
        if (input.HasMinimumLength)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.MinimumLength, Value = InvariantFormatter.FormatInt32(input.MinimumLength) });
        }

        if (input.HasMaximumLength)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.MaximumLength, Value = InvariantFormatter.FormatInt32(input.MaximumLength) });
        }

        if (input.Pattern != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Pattern, Value = input.Pattern });
        }

        if (input.IsRequired)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Required, Value = string.Empty, IsBoolean = true });
        }
    }

    internal void AddOptionalConstraint(UiNode node, List<DomAttribute> attributes, string name, UiValue? value)
    {
        if (value != null)
        {
            attributes.Add(new DomAttribute { Name = name, Value = new InputTemplateExpander().FormatScalarValue(value) });
        }
    }

    internal void AddFileOptions(UiNode node, UiInput input, List<DomAttribute> attributes)
    {
        if (input.AcceptedFileTypes.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Accept, Value = CanonicalizeAcceptedTypes(input.AcceptedFileTypes) });
        }

        if (input.Multiple)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Multiple, Value = string.Empty, IsBoolean = true });
        }
    }

    internal void AddDescriptions(UiNode node, UiInput input, List<DomAttribute> attributes)
    {
        string helperId = string.Empty;
        string errorId = string.Empty;
        if (input.HelperText != null)
        {
            helperId = CreateSyntheticId(node, "helper");
        }

        if (input.ErrorText != null)
        {
            errorId = CreateSyntheticId(node, "error");
        }

        string describedBy = helperId;
        if (errorId.Length > 0)
        {
            if (describedBy.Length > 0)
            {
                describedBy += " ";
            }

            describedBy += errorId;
            attributes.Add(new DomAttribute { Name = AriaAttributes.ErrorMessage, Value = errorId });
            attributes.Add(new DomAttribute { Name = AriaAttributes.Invalid, Value = "true" });
        }

        if (describedBy.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.DescribedBy, Value = describedBy });
        }
    }

    internal DomNode DecorateControl(UiNode node, UiInput input, DomNode control)
    {
        if (!HasDecorations(input))
        {
            return control;
        }

        control.Key = "$input-control";
        control.Id = CreateSyntheticId(node, "control");
        List<DomNode> children = new List<DomNode>();
        AddAdornment(node, children, input.Prefix, "prefix");
        children.Add(control);
        AddAdornment(node, children, input.Suffix, "suffix");
        if (input.HasClearAction)
        {
            children.Add(CreateAction(node, input, "clear", "Clear " + input.Name));
        }

        if (input.HasRevealAction)
        {
            children.Add(CreateAction(node, input, "reveal", "Reveal " + input.Name));
        }

        AddMessage(node, children, input.HelperText, "helper");
        AddMessage(node, children, input.ErrorText, "error");
        List<DomAttribute> wrapperAttributes = new List<DomAttribute>();
        wrapperAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-input-field" });
        if (node.State != null && !node.State.IsVisible)
        {
            wrapperAttributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (node.State != null && node.State.IsBusy)
        {
            wrapperAttributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }

        SortAttributes(wrapperAttributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = HtmlElements.Division,
            Attributes = wrapperAttributes.ToArray(),
            Children = children.ToArray(),
            Version = GetNodeVersion(node)
        };
    }

    internal void AddAdornment(UiNode node, List<DomNode> children, string? text, string suffix)
    {
        if (text != null)
        {
            children.Add(CreateTextElement(node, "htmlgui-input-" + suffix, "$input-" + suffix, suffix, text));
        }
    }

    internal void AddMessage(UiNode node, List<DomNode> children, string? text, string suffix)
    {
        if (text != null)
        {
            children.Add(CreateTextElement(node, "htmlgui-input-" + suffix, "$input-" + suffix, suffix, text));
        }
    }

    internal DomNode CreateAction(UiNode node, UiInput input, string action, string label)
    {
        string actionText = "Clear";
        if (action == "reveal")
        {
            actionText = "Show";
            if (input.IsRevealed)
            {
                label = "Hide " + input.Name;
                actionText = "Hide";
            }
        }

        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = label });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-input-action htmlgui-input-" + action });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "input-action", Value = action });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        if (action == "reveal")
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Pressed, Value = FormatBoolean(input.IsRevealed) });
        }

        if (IsDisabled(node, input))
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(attributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$input-" + action,
            Id = CreateSyntheticId(node, action),
            Name = HtmlElements.Button,
            Attributes = attributes.ToArray(),
            Children = new DomNode[] { CreateText(node, "$input-" + action + "-label", action + "-label", actionText) },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTextElement(UiNode node, string className, string key, string suffix, string text)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = CreateSyntheticId(node, suffix),
            Name = HtmlElements.Span,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = className } },
            Children = new DomNode[] { CreateText(node, key + "-text", suffix + "-text", text) },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateText(UiNode node, string key, string suffix, string text)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = key,
            Id = CreateSyntheticId(node, suffix),
            Text = text,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode FindChoiceFocusTarget(DomNode control)
    {
        if (control.Name == HtmlElements.Division && control.Children.Length > 0)
        {
            DomNode first = control.Children[0];
            if (first.Name == HtmlElements.Input || first.Name == HtmlElements.Button)
            {
                return first;
            }
        }

        return control;
    }

    internal bool HasDecorations(UiInput input)
    {
        return HasDecorativeText(input) || input.HasClearAction || input.HasRevealAction || input.HelperText != null || input.ErrorText != null;
    }

    internal bool HasDecorativeText(UiInput input)
    {
        return input.Prefix != null || input.Suffix != null;
    }

    internal bool IsTextualKind(UiInputKind kind)
    {
        return kind == UiInputKind.Text || kind == UiInputKind.Password || kind == UiInputKind.Search || kind == UiInputKind.Email || kind == UiInputKind.Url || kind == UiInputKind.Telephone || kind == UiInputKind.Multiline;
    }

    internal bool IsTemporalKind(UiInputKind kind)
    {
        return kind == UiInputKind.Date || kind == UiInputKind.Time || kind == UiInputKind.Month || kind == UiInputKind.Week || kind == UiInputKind.LocalDateTime;
    }

    internal bool IsNumericValue(UiValue value)
    {
        return value.Kind == UiValueKind.Integer || value.Kind == UiValueKind.FloatingPoint || value.Kind == UiValueKind.Decimal;
    }

    internal bool IsPositiveNumericValue(UiValue value)
    {
        switch (value.Kind)
        {
            case UiValueKind.Integer:
                return value.IntegerValue > 0;
            case UiValueKind.FloatingPoint:
                return value.FloatingPointValue > 0.0;
            case UiValueKind.Decimal:
                return value.DecimalValue > 0m;
            default:
                return false;
        }
    }

    internal bool IsAnyStep(UiValue value)
    {
        return value.Kind == UiValueKind.String && value.StringValue == "any";
    }

    internal bool SupportsPlaceholder(UiInputKind kind)
    {
        return IsTextualKind(kind) || kind == UiInputKind.Number;
    }

    internal bool SupportsEditingHints(UiInputKind kind)
    {
        return IsTextualKind(kind) || kind == UiInputKind.Number;
    }

    internal bool IsEditableChoiceName(string name)
    {
        return name == "autocomplete" || name == "autocomplete-input" || name == "editable-combo" || name == "editable-combo-box" || name == "editable-combobox";
    }

    internal bool SupportsChoiceAutoComplete(string name)
    {
        return name == "checkbox" || name == "checkbox-input" || name == "switch" || name == "switch-input" || name == "select" || name == "select-input" || name == "multi-select" || name == "multiselect" || name == "multi-select-input" || IsEditableChoiceName(name);
    }

    internal bool IsDisabled(UiNode node, UiInput input)
    {
        return input.IsDisabled || (node.State != null && !node.State.IsEnabled);
    }

    internal string CanonicalizeAcceptedTypes(string[] values)
    {
        List<string> unique = new List<string>();
        int index = 0;
        while (index < values.Length)
        {
            string value = values[index].Trim();
            if (!unique.Contains(value))
            {
                unique.Add(value);
            }

            index++;
        }

        unique.Sort(StringComparer.Ordinal);
        return string.Join(",", unique);
    }

    internal string GetUpdateTriggerToken(UiBindingTrigger trigger)
    {
        switch (trigger)
        {
            case UiBindingTrigger.Immediate:
                return "immediate";
            case UiBindingTrigger.Input:
                return "input";
            case UiBindingTrigger.Change:
                return "change";
            case UiBindingTrigger.Blur:
                return "blur";
            case UiBindingTrigger.Explicit:
                return "explicit";
            default:
                throw new ArgumentOutOfRangeException("updateTrigger", trigger, "The update trigger is not supported.");
        }
    }

    internal string RequireToken(UiNode node, string value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw CreateRenderException(node, description + " tokens cannot be empty.");
        }

        return value;
    }

    internal string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }

    internal List<DomAttribute> CopyAttributes(DomAttribute[] attributes)
    {
        List<DomAttribute> result = new List<DomAttribute>();
        int index = 0;
        while (index < attributes.Length)
        {
            result.Add(attributes[index]);
            index++;
        }

        return result;
    }

    internal string CreateSyntheticId(UiNode node, string suffix)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "input-option", suffix });
        return "ui-input-option-" + token.Substring(0, 20);
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
