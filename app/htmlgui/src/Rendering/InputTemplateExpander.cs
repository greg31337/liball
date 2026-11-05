using System;
using System.Collections.Generic;

namespace htmlgui;

internal class InputTemplateExpander
{
    internal DomNode ExpandInput(UiNode node)
    {
        UiInput? input = node.Input;
        if (input == null)
        {
            throw CreateRenderException(node, "An input control requires an input description.");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw CreateRenderException(node, "Inputs require a non-empty field name.");
        }

        UiValue? suppliedValue = input.Value;
        if (suppliedValue == null)
        {
            throw CreateRenderException(node, "Inputs require a portable value.");
        }

        ValueJsonCodec.ValidateValue(suppliedValue, 64, 10000, 1048576, 1048576);
        DomNode result;
        switch (input.Kind)
        {
            case UiInputKind.Text:
                result = ExpandTextInput(node, input, "text", HtmlInputTypes.Text);
                break;
            case UiInputKind.Password:
                string passwordType = HtmlInputTypes.Password;
                if (input.IsRevealed)
                {
                    passwordType = HtmlInputTypes.Text;
                }

                result = ExpandTextInput(node, input, "password", passwordType);
                break;
            case UiInputKind.Search:
                result = ExpandTextInput(node, input, "search", HtmlInputTypes.Search);
                break;
            case UiInputKind.Email:
                result = ExpandTextInput(node, input, "email", HtmlInputTypes.Email);
                break;
            case UiInputKind.Url:
                result = ExpandTextInput(node, input, "url", HtmlInputTypes.Url);
                break;
            case UiInputKind.Telephone:
                result = ExpandTextInput(node, input, "telephone", HtmlInputTypes.Telephone);
                break;
            case UiInputKind.Multiline:
                result = ExpandMultilineInput(node, input);
                break;
            case UiInputKind.Number:
                result = ExpandNumericInput(node, input, "number", HtmlInputTypes.Number);
                break;
            case UiInputKind.Range:
                result = ExpandNumericInput(node, input, "range", HtmlInputTypes.Range);
                break;
            case UiInputKind.Date:
                result = ExpandTemporalInput(node, input, "date", HtmlInputTypes.Date);
                break;
            case UiInputKind.Time:
                result = ExpandTemporalInput(node, input, "time", HtmlInputTypes.Time);
                break;
            case UiInputKind.Month:
                result = ExpandTemporalInput(node, input, "month", HtmlInputTypes.Month);
                break;
            case UiInputKind.Week:
                result = ExpandTemporalInput(node, input, "week", HtmlInputTypes.Week);
                break;
            case UiInputKind.LocalDateTime:
                result = ExpandTemporalInput(node, input, "local-date-time", HtmlInputTypes.LocalDateTime);
                break;
            case UiInputKind.Color:
                result = ExpandColorInput(node, input);
                break;
            case UiInputKind.File:
                result = ExpandFileInput(node, input);
                break;
            case UiInputKind.Hidden:
                result = ExpandHiddenInput(node, input);
                break;
            case UiInputKind.ReadOnly:
                result = ExpandReadOnlyInput(node, input);
                break;
            default:
                throw new ArgumentOutOfRangeException("kind", input.Kind, "The input kind is not supported.");
        }

        return new InputOptionExpander().ApplyScalarOptions(node, input, result);
    }

    internal bool IsInputControlName(string name)
    {
        switch (name)
        {
            case "input":
            case "text-input":
            case "password-input":
            case "search-input":
            case "email-input":
            case "url-input":
            case "telephone-input":
            case "tel-input":
            case "multiline-input":
            case "textarea":
            case "number-input":
            case "numeric-input":
            case "range-input":
            case "date-input":
            case "time-input":
            case "month-input":
            case "week-input":
            case "local-date-time-input":
            case "datetime-local-input":
            case "color-input":
            case "file-input":
            case "hidden-input":
            case "read-only-input":
            case "readonly-input":
                return true;
            default:
                return false;
        }
    }

    internal DomNode ExpandTextInput(UiNode node, UiInput input, string kindToken, string type)
    {
        EnsureTextValue(node, input.Value);
        return CreateNativeInput(node, input, kindToken, type, FormatScalarValue(input.Value));
    }

    internal DomNode ExpandNumericInput(UiNode node, UiInput input, string kindToken, string type)
    {
        EnsureNumericValue(node, input.Value);
        return CreateNativeInput(node, input, kindToken, type, FormatScalarValue(input.Value));
    }

    internal DomNode ExpandTemporalInput(UiNode node, UiInput input, string kindToken, string type)
    {
        EnsureTemporalValue(node, input.Value);
        return CreateNativeInput(node, input, kindToken, type, FormatScalarValue(input.Value));
    }

    internal DomNode ExpandColorInput(UiNode node, UiInput input)
    {
        EnsureTextValue(node, input.Value);
        return CreateNativeInput(node, input, "color", HtmlInputTypes.Color, FormatScalarValue(input.Value));
    }

    internal DomNode ExpandFileInput(UiNode node, UiInput input)
    {
        EnsureEmptyFileValue(node, input.Value);
        List<DomAttribute> attributes = CreateInputAttributes(node, input, "file");
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.File });
        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Input, attributes.ToArray(), Array.Empty<DomProperty>(), Array.Empty<DomNode>());
    }

    internal DomNode ExpandHiddenInput(UiNode node, UiInput input)
    {
        EnsureScalarValue(node, input.Value);
        return CreateNativeInput(node, input, "hidden", HtmlInputTypes.Hidden, FormatScalarValue(input.Value));
    }

    internal DomNode ExpandMultilineInput(UiNode node, UiInput input)
    {
        EnsureTextValue(node, input.Value);
        string value = InvariantFormatter.NormalizeLineEndings(FormatScalarValue(input.Value));
        List<DomAttribute> attributes = CreateInputAttributes(node, input, "multiline");
        DomNode text = CreateTextNode(node, value);
        return CreateElement(node, HtmlElements.TextArea, attributes.ToArray(), CreateValueProperty(value), new DomNode[] { text });
    }

    internal DomNode ExpandReadOnlyInput(UiNode node, UiInput input)
    {
        EnsureScalarValue(node, input.Value);
        string value = FormatScalarValue(input.Value);
        List<DomAttribute> attributes = CreateInputAttributes(node, input, "read-only");
        if (IsDisabled(node, input))
        {
            RemoveAttribute(attributes, HtmlAttributes.Disabled);
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
            SortAttributes(attributes);
        }

        DomNode text = CreateTextNode(node, value);
        return CreateElement(node, HtmlElements.Output, attributes.ToArray(), CreateValueProperty(value), new DomNode[] { text });
    }

    internal DomNode CreateNativeInput(UiNode node, UiInput input, string kindToken, string type, string value)
    {
        List<DomAttribute> attributes = CreateInputAttributes(node, input, kindToken);
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = type });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Value, Value = value });
        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Input, attributes.ToArray(), CreateValueProperty(value), Array.Empty<DomNode>());
    }

    internal List<DomAttribute> CreateInputAttributes(UiNode node, UiInput input, string kindToken)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-input htmlgui-input-" + kindToken });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Name, Value = input.Name });
        if (IsDisabled(node, input))
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }

        UiNodeState? state = node.State;
        if (state != null)
        {
            if (!state.IsVisible)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
            }

            if (state.IsBusy)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
            }

            if (state.HasTabIndex)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = InvariantFormatter.FormatInt32(state.TabIndex) });
            }
        }

        SortAttributes(attributes);
        return attributes;
    }

    internal DomProperty[] CreateValueProperty(string value)
    {
        return new DomProperty[]
        {
            new DomProperty
            {
                Name = HtmlAttributes.Value,
                Value = new UiValue { Kind = UiValueKind.String, StringValue = value }
            }
        };
    }

    internal DomNode CreateTextNode(UiNode node, string value)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = "$input-value",
            Id = CreateSyntheticId(node, "input-value"),
            Text = value,
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

    internal void EnsureTextValue(UiNode node, UiValue value)
    {
        if (value.Kind != UiValueKind.Null && value.Kind != UiValueKind.String)
        {
            throw CreateRenderException(node, "This input kind requires a string or null value.");
        }
    }

    internal void EnsureNumericValue(UiNode node, UiValue value)
    {
        if (value.Kind != UiValueKind.Null && value.Kind != UiValueKind.Integer && value.Kind != UiValueKind.FloatingPoint && value.Kind != UiValueKind.Decimal)
        {
            throw CreateRenderException(node, "Numeric inputs require a numeric or null value.");
        }
    }

    internal void EnsureTemporalValue(UiNode node, UiValue value)
    {
        if (value.Kind != UiValueKind.Null && value.Kind != UiValueKind.String && value.Kind != UiValueKind.DateTime)
        {
            throw CreateRenderException(node, "Temporal inputs require a text, date-time, or null value.");
        }
    }

    internal void EnsureScalarValue(UiNode node, UiValue value)
    {
        if (value.Kind == UiValueKind.Array || value.Kind == UiValueKind.Object)
        {
            throw CreateRenderException(node, "Scalar inputs cannot display aggregate values.");
        }
    }

    internal void EnsureEmptyFileValue(UiNode node, UiValue value)
    {
        if (value.Kind == UiValueKind.Null)
        {
            return;
        }

        if (value.Kind == UiValueKind.String && value.StringValue != null && value.StringValue.Length == 0)
        {
            return;
        }

        throw CreateRenderException(node, "File inputs cannot be prepopulated with a value.");
    }

    internal string FormatScalarValue(UiValue value)
    {
        switch (value.Kind)
        {
            case UiValueKind.Null:
                return string.Empty;
            case UiValueKind.Boolean:
                return InvariantFormatter.FormatBoolean(value.BooleanValue);
            case UiValueKind.Integer:
                return InvariantFormatter.FormatInt64(value.IntegerValue);
            case UiValueKind.FloatingPoint:
                return InvariantFormatter.FormatDouble(value.FloatingPointValue);
            case UiValueKind.Decimal:
                return InvariantFormatter.FormatDecimal(value.DecimalValue);
            case UiValueKind.String:
                return value.StringValue ?? string.Empty;
            case UiValueKind.DateTime:
                return value.DateTimeValue ?? string.Empty;
            case UiValueKind.Binary:
                return Convert.ToBase64String(value.BinaryValue ?? Array.Empty<byte>());
            default:
                throw new ArgumentOutOfRangeException("kind", value.Kind, "The scalar input value kind is not supported.");
        }
    }

    internal bool IsDisabled(UiNode node, UiInput input)
    {
        if (input.IsDisabled)
        {
            return true;
        }

        return node.State != null && !node.State.IsEnabled;
    }

    internal void RemoveAttribute(List<DomAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (attributes[index].Name == name)
            {
                attributes.RemoveAt(index);
                return;
            }

            index++;
        }
    }

    internal string CreateSyntheticId(UiNode node, string suffix)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "input", suffix });
        return "ui-input-" + token.Substring(0, 20);
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
