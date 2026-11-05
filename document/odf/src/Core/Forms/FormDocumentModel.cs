using System;
using System.Collections.Generic;

namespace odf;

internal static class FormDocumentModel
{
    internal static OdfFormControl MaterializeCreatedControl(
        OdfNode node,
        OdfFormControl? parent)
    {
        List<OdfFormControl> controls = new List<OdfFormControl>();
        return LoadControl(node, parent, controls);
    }

    internal static OdfFormProperty MaterializeCreatedProperty(
        OdfNode node,
        bool isList)
    {
        return LoadProperty(node, isList);
    }

    internal static OdfFormOption MaterializeCreatedOption(
        OdfNode node,
        bool isOption)
    {
        return LoadOption(node, isOption);
    }

    internal static OdfDrawingEvent MaterializeCreatedEvent(
        OdfNode node,
        OdfDrawingEventKind kind)
    {
        return LoadEvent(node, kind);
    }

    internal static void RefreshReferences(OdfDocument document)
    {
        ResolveReferences(document);
    }

    internal static void LoadForms(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for form materialization.");
        }
        List<OdfFormControl> controls = new List<OdfFormControl>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode? root = document.Parts[partIndex].Root;
            if (root != null)
            {
                FindFormContainers(root, controls);
            }
            partIndex = partIndex + 1;
        }
        document.FormControls = controls.ToArray();
        ResolveReferences(document);
        FormDocumentState state = new FormDocumentState();
        state.Controls = document.FormControls;
        document.FormState = state;
    }

    internal static void SynchronizeForms(OdfDocument document)
    {
        if (document == null || document.FormControls == null)
        {
            throw new OdfDocumentException(
                "A document and form controls are required for writing.");
        }
        int index = 0;
        while (index < document.FormControls.Length)
        {
            OdfFormControl control = document.FormControls[index];
            if (control == null)
            {
                throw new OdfDocumentException(
                    "Form-control collections cannot contain null entries.");
            }
            SynchronizeControl(control);
            index = index + 1;
        }
        ResolveReferences(document);
        if (document.FormState == null)
        {
            document.FormState = new FormDocumentState();
        }
        document.FormState.Controls = document.FormControls;
    }

    static void FindFormContainers(
        OdfNode node,
        List<OdfFormControl> controls)
    {
        if (IsElement(node, OdfNamespaces.Office, "forms"))
        {
            int formIndex = 0;
            while (formIndex < node.Children.Length)
            {
                OdfNode child = node.Children[formIndex];
                if (ControlKind(child) == OdfFormControlKind.Form)
                {
                    LoadControl(child, null, controls);
                }
                formIndex = formIndex + 1;
            }
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            FindFormContainers(node.Children[index], controls);
            index = index + 1;
        }
    }

    static OdfFormControl LoadControl(
        OdfNode node,
        OdfFormControl? parent,
        List<OdfFormControl> controls)
    {
        OdfFormControl result = new OdfFormControl();
        result.Kind = ControlKind(node);
        result.ElementName = node.Name == null ? "" : node.Name.LocalName;
        result.Name = Read(node, OdfNamespaces.Form, "name");
        result.Id = Read(node, OdfNamespaces.Form, "id");
        result.XmlId = Read(node, OdfNamespaces.Xml, "id");
        result.ControlImplementation = Read(
            node,
            OdfNamespaces.Form,
            "control-implementation");
        result.Label = Read(node, OdfNamespaces.Form, "label");
        result.For = Read(node, OdfNamespaces.Form, "for");
        result.Value = Read(node, OdfNamespaces.Form, "value");
        result.CurrentValue = Read(
            node,
            OdfNamespaces.Form,
            "current-value");
        result.MinValue = Read(node, OdfNamespaces.Form, "min-value");
        result.MaxValue = Read(node, OdfNamespaces.Form, "max-value");
        result.Title = Read(node, OdfNamespaces.Form, "title");
        result.DataStyleName = Read(
            node,
            OdfNamespaces.Style,
            "data-style-name");
        result.Binding = LoadBinding(node);
        result.Validation = LoadValidation(node);
        result.Properties = LoadProperties(node);
        result.Options = LoadOptions(node);
        result.Events = LoadEvents(node);
        result.Parent = parent;
        result.Attributes = node.Attributes;
        result.PartPath = node.Location.PartPath;
        result.Node = node;
        controls.Add(result);
        List<OdfFormControl> children = new List<OdfFormControl>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            OdfFormControlKind kind = ControlKind(child);
            if (kind != OdfFormControlKind.Unknown)
            {
                children.Add(LoadControl(child, result, controls));
            }
            index = index + 1;
        }
        result.Children = children.ToArray();
        CaptureOriginalControl(result);
        return result;
    }

    static OdfFormBinding LoadBinding(OdfNode node)
    {
        OdfFormBinding result = new OdfFormBinding();
        result.LinkedCell = Read(node, OdfNamespaces.Form, "linked-cell");
        result.SourceCellRange = Read(
            node,
            OdfNamespaces.Form,
            "source-cell-range");
        result.DataField = Read(node, OdfNamespaces.Form, "data-field");
        result.ListSource = Read(node, OdfNamespaces.Form, "list-source");
        result.ListSourceType = Read(
            node,
            OdfNamespaces.Form,
            "list-source-type");
        result.BoundColumn = Read(node, OdfNamespaces.Form, "bound-column");
        result.XFormsBind = Read(node, OdfNamespaces.XForms, "bind");
        result.XFormsListSource = Read(
            node,
            OdfNamespaces.Form,
            "xforms-list-source");
        result.XFormsSubmission = Read(
            node,
            OdfNamespaces.Form,
            "xforms-submission");
        CaptureOriginalBinding(result);
        return result;
    }

    static OdfFormValidation LoadValidation(OdfNode node)
    {
        OdfFormValidation result = new OdfFormValidation();
        result.DisabledValue = Read(node, OdfNamespaces.Form, "disabled");
        result.IsDisabled = BooleanValue(result.DisabledValue);
        result.IsDisabledValid = BooleanValid(result.DisabledValue);
        result.InputRequiredValue = Read(
            node,
            OdfNamespaces.Form,
            "input-required");
        result.IsInputRequired = BooleanValue(result.InputRequiredValue);
        result.IsInputRequiredValid = BooleanValid(
            result.InputRequiredValue);
        result.ReadOnlyValue = Read(node, OdfNamespaces.Form, "readonly");
        result.IsReadOnly = BooleanValue(result.ReadOnlyValue);
        result.IsReadOnlyValid = BooleanValid(result.ReadOnlyValue);
        result.ValidationValue = Read(
            node,
            OdfNamespaces.Form,
            "validation");
        result.IsValidationEnabled = BooleanValue(result.ValidationValue);
        result.IsValidationValueValid = BooleanValid(
            result.ValidationValue);
        result.MaxLength = Read(node, OdfNamespaces.Form, "max-length");
        CaptureOriginalValidation(result);
        return result;
    }

    static OdfFormProperty[] LoadProperties(OdfNode node)
    {
        OdfNode? container = FindDirectChild(
            node,
            OdfNamespaces.Form,
            "properties");
        if (container == null)
        {
            return [];
        }
        List<OdfFormProperty> result = new List<OdfFormProperty>();
        int index = 0;
        while (index < container.Children.Length)
        {
            OdfNode child = container.Children[index];
            bool scalar = IsElement(
                child,
                OdfNamespaces.Form,
                "property");
            bool list = IsElement(
                child,
                OdfNamespaces.Form,
                "list-property");
            if (scalar || list)
            {
                result.Add(LoadProperty(child, list));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfFormProperty LoadProperty(OdfNode node, bool isList)
    {
        OdfFormProperty result = new OdfFormProperty();
        result.Name = Read(node, OdfNamespaces.Form, "property-name");
        result.ValueType = Read(node, OdfNamespaces.Office, "value-type");
        result.IsList = isList;
        result.Node = node;
        List<OdfFormPropertyValue> values =
            new List<OdfFormPropertyValue>();
        if (isList)
        {
            int index = 0;
            while (index < node.Children.Length)
            {
                OdfNode child = node.Children[index];
                if (IsElement(child, OdfNamespaces.Form, "list-value"))
                {
                    values.Add(LoadPropertyValue(child, result.ValueType));
                }
                index = index + 1;
            }
        }
        else
        {
            values.Add(LoadPropertyValue(node, result.ValueType));
        }
        result.Values = values.ToArray();
        result.OriginalName = result.Name;
        result.OriginalValueType = result.ValueType;
        return result;
    }

    static OdfFormPropertyValue LoadPropertyValue(
        OdfNode node,
        string valueType)
    {
        OdfFormPropertyValue result = new OdfFormPropertyValue();
        result.Value = ReadTypedValue(node, valueType);
        result.Currency = Read(node, OdfNamespaces.Office, "currency");
        result.Node = node;
        result.OriginalValue = result.Value;
        result.OriginalCurrency = result.Currency;
        return result;
    }

    static OdfFormOption[] LoadOptions(OdfNode node)
    {
        List<OdfFormOption> result = new List<OdfFormOption>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            bool option = IsElement(child, OdfNamespaces.Form, "option");
            bool item = IsElement(child, OdfNamespaces.Form, "item");
            if (option || item)
            {
                result.Add(LoadOption(child, option));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfFormOption LoadOption(OdfNode node, bool isOption)
    {
        OdfFormOption result = new OdfFormOption();
        result.IsOption = isOption;
        result.Label = Read(node, OdfNamespaces.Form, "label");
        result.Value = Read(node, OdfNamespaces.Form, "value");
        result.Text = TextContent(node);
        result.SelectedValue = Read(node, OdfNamespaces.Form, "selected");
        result.IsSelected = BooleanValue(result.SelectedValue);
        result.CurrentSelectedValue = Read(
            node,
            OdfNamespaces.Form,
            "current-selected");
        result.IsCurrentSelected = BooleanValue(
            result.CurrentSelectedValue);
        result.Node = node;
        result.OriginalLabel = result.Label;
        result.OriginalValue = result.Value;
        result.OriginalText = result.Text;
        result.OriginalSelectedValue = result.SelectedValue;
        result.OriginalIsSelected = result.IsSelected;
        result.OriginalCurrentSelectedValue = result.CurrentSelectedValue;
        result.OriginalIsCurrentSelected = result.IsCurrentSelected;
        return result;
    }

    static OdfDrawingEvent[] LoadEvents(OdfNode node)
    {
        OdfNode? container = FindDirectChild(
            node,
            OdfNamespaces.Office,
            "event-listeners");
        if (container == null)
        {
            return [];
        }
        List<OdfDrawingEvent> result = new List<OdfDrawingEvent>();
        int index = 0;
        while (index < container.Children.Length)
        {
            OdfNode child = container.Children[index];
            OdfDrawingEventKind kind = EventKind(child);
            if (kind != OdfDrawingEventKind.Unknown)
            {
                result.Add(LoadEvent(child, kind));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfDrawingEvent LoadEvent(
        OdfNode node,
        OdfDrawingEventKind kind)
    {
        OdfDrawingEvent result = new OdfDrawingEvent();
        result.Kind = kind;
        result.EventName = Read(node, OdfNamespaces.Script, "event-name");
        result.Language = Read(node, OdfNamespaces.Script, "language");
        result.MacroName = Read(node, OdfNamespaces.Script, "macro-name");
        result.Href = Read(node, OdfNamespaces.XLink, "href");
        result.Action = Read(node, OdfNamespaces.Presentation, "action");
        result.Effect = Read(node, OdfNamespaces.Presentation, "effect");
        result.Direction = Read(
            node,
            OdfNamespaces.Presentation,
            "direction");
        result.Speed = Read(node, OdfNamespaces.Presentation, "speed");
        result.StartScale = Read(
            node,
            OdfNamespaces.Presentation,
            "start-scale");
        result.Verb = Read(node, OdfNamespaces.Presentation, "verb");
        result.Show = Read(node, OdfNamespaces.XLink, "show");
        result.Actuate = Read(node, OdfNamespaces.XLink, "actuate");
        result.Node = node;
        return result;
    }

    static void ResolveReferences(OdfDocument document)
    {
        int index = 0;
        while (index < document.FormControls.Length)
        {
            OdfFormControl control = document.FormControls[index];
            List<OdfFormReference> references =
                new List<OdfFormReference>();
            string[] identifiers = control.For.Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries);
            int identifierIndex = 0;
            while (identifierIndex < identifiers.Length)
            {
                OdfFormControl? target = FindControl(
                    document.FormControls,
                    identifiers[identifierIndex]);
                OdfFormReference reference = new OdfFormReference();
                reference.Kind = OdfFormReferenceKind.LabelTarget;
                reference.TargetId = identifiers[identifierIndex];
                reference.TargetControl = target;
                reference.IsBroken = target == null;
                reference.Node = control.Node;
                references.Add(reference);
                identifierIndex = identifierIndex + 1;
            }
            int drawingIndex = 0;
            while (drawingIndex < document.DrawingObjects.Length)
            {
                OdfDrawingObject drawing = document.DrawingObjects[drawingIndex];
                if (drawing.ControlId.Length > 0
                    && MatchesControlId(control, drawing.ControlId))
                {
                    OdfFormReference reference = new OdfFormReference();
                    reference.Kind = OdfFormReferenceKind.DrawingControl;
                    reference.TargetId = drawing.ControlId;
                    reference.TargetControl = control;
                    reference.DrawingObject = drawing;
                    reference.Node = drawing.Node;
                    references.Add(reference);
                }
                drawingIndex = drawingIndex + 1;
            }
            control.References = references.ToArray();
            index = index + 1;
        }
    }

    static OdfFormControl? FindControl(
        OdfFormControl[] controls,
        string identifier)
    {
        int index = 0;
        while (index < controls.Length)
        {
            if (MatchesControlId(controls[index], identifier))
            {
                return controls[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool MatchesControlId(
        OdfFormControl control,
        string identifier)
    {
        return control.Id == identifier || control.XmlId == identifier;
    }

    static void SynchronizeControl(OdfFormControl control)
    {
        OdfNode? node = control.Node;
        if (node == null)
        {
            return;
        }
        WriteChanged(node, OdfNamespaces.Form, "name", OdfNamespaces.FormPrefix, control.Name, control.OriginalName);
        WriteChanged(node, OdfNamespaces.Form, "id", OdfNamespaces.FormPrefix, control.Id, control.OriginalId);
        WriteChanged(node, OdfNamespaces.Xml, "id", OdfNamespaces.XmlPrefix, control.XmlId, control.OriginalXmlId);
        WriteChanged(node, OdfNamespaces.Form, "control-implementation", OdfNamespaces.FormPrefix, control.ControlImplementation, control.OriginalControlImplementation);
        WriteChanged(node, OdfNamespaces.Form, "label", OdfNamespaces.FormPrefix, control.Label, control.OriginalLabel);
        WriteChanged(node, OdfNamespaces.Form, "for", OdfNamespaces.FormPrefix, control.For, control.OriginalFor);
        WriteChanged(node, OdfNamespaces.Form, "value", OdfNamespaces.FormPrefix, control.Value, control.OriginalValue);
        WriteChanged(node, OdfNamespaces.Form, "current-value", OdfNamespaces.FormPrefix, control.CurrentValue, control.OriginalCurrentValue);
        WriteChanged(node, OdfNamespaces.Form, "min-value", OdfNamespaces.FormPrefix, control.MinValue, control.OriginalMinValue);
        WriteChanged(node, OdfNamespaces.Form, "max-value", OdfNamespaces.FormPrefix, control.MaxValue, control.OriginalMaxValue);
        WriteChanged(node, OdfNamespaces.Form, "title", OdfNamespaces.FormPrefix, control.Title, control.OriginalTitle);
        WriteChanged(node, OdfNamespaces.Style, "data-style-name", OdfNamespaces.StylePrefix, control.DataStyleName, control.OriginalDataStyleName);
        SynchronizeBinding(node, control.Binding);
        SynchronizeValidation(node, control.Validation);
        SynchronizeProperties(control.Properties);
        SynchronizeOptions(control.Options);
        SynchronizeEvents(control.Events);
        control.Attributes = node.Attributes;
        CaptureOriginalControl(control);
    }

    static void SynchronizeBinding(OdfNode node, OdfFormBinding binding)
    {
        WriteChanged(node, OdfNamespaces.Form, "linked-cell", OdfNamespaces.FormPrefix, binding.LinkedCell, binding.OriginalLinkedCell);
        WriteChanged(node, OdfNamespaces.Form, "source-cell-range", OdfNamespaces.FormPrefix, binding.SourceCellRange, binding.OriginalSourceCellRange);
        WriteChanged(node, OdfNamespaces.Form, "data-field", OdfNamespaces.FormPrefix, binding.DataField, binding.OriginalDataField);
        WriteChanged(node, OdfNamespaces.Form, "list-source", OdfNamespaces.FormPrefix, binding.ListSource, binding.OriginalListSource);
        WriteChanged(node, OdfNamespaces.Form, "list-source-type", OdfNamespaces.FormPrefix, binding.ListSourceType, binding.OriginalListSourceType);
        WriteChanged(node, OdfNamespaces.Form, "bound-column", OdfNamespaces.FormPrefix, binding.BoundColumn, binding.OriginalBoundColumn);
        WriteChanged(node, OdfNamespaces.XForms, "bind", OdfNamespaces.XFormsPrefix, binding.XFormsBind, binding.OriginalXFormsBind);
        WriteChanged(node, OdfNamespaces.Form, "xforms-list-source", OdfNamespaces.FormPrefix, binding.XFormsListSource, binding.OriginalXFormsListSource);
        WriteChanged(node, OdfNamespaces.Form, "xforms-submission", OdfNamespaces.FormPrefix, binding.XFormsSubmission, binding.OriginalXFormsSubmission);
        CaptureOriginalBinding(binding);
    }

    static void SynchronizeValidation(
        OdfNode node,
        OdfFormValidation validation)
    {
        validation.DisabledValue = SelectBooleanText(
            validation.DisabledValue,
            validation.OriginalDisabledValue,
            validation.IsDisabled,
            validation.OriginalIsDisabled);
        validation.InputRequiredValue = SelectBooleanText(
            validation.InputRequiredValue,
            validation.OriginalInputRequiredValue,
            validation.IsInputRequired,
            validation.OriginalIsInputRequired);
        validation.ReadOnlyValue = SelectBooleanText(
            validation.ReadOnlyValue,
            validation.OriginalReadOnlyValue,
            validation.IsReadOnly,
            validation.OriginalIsReadOnly);
        validation.ValidationValue = SelectBooleanText(
            validation.ValidationValue,
            validation.OriginalValidationValue,
            validation.IsValidationEnabled,
            validation.OriginalIsValidationEnabled);
        WriteChanged(node, OdfNamespaces.Form, "disabled", OdfNamespaces.FormPrefix, validation.DisabledValue, validation.OriginalDisabledValue);
        WriteChanged(node, OdfNamespaces.Form, "input-required", OdfNamespaces.FormPrefix, validation.InputRequiredValue, validation.OriginalInputRequiredValue);
        WriteChanged(node, OdfNamespaces.Form, "readonly", OdfNamespaces.FormPrefix, validation.ReadOnlyValue, validation.OriginalReadOnlyValue);
        WriteChanged(node, OdfNamespaces.Form, "validation", OdfNamespaces.FormPrefix, validation.ValidationValue, validation.OriginalValidationValue);
        WriteChanged(node, OdfNamespaces.Form, "max-length", OdfNamespaces.FormPrefix, validation.MaxLength, validation.OriginalMaxLength);
        validation.IsDisabled = BooleanValue(validation.DisabledValue);
        validation.IsDisabledValid = BooleanValid(validation.DisabledValue);
        validation.IsInputRequired = BooleanValue(
            validation.InputRequiredValue);
        validation.IsInputRequiredValid = BooleanValid(
            validation.InputRequiredValue);
        validation.IsReadOnly = BooleanValue(validation.ReadOnlyValue);
        validation.IsReadOnlyValid = BooleanValid(validation.ReadOnlyValue);
        validation.IsValidationEnabled = BooleanValue(
            validation.ValidationValue);
        validation.IsValidationValueValid = BooleanValid(
            validation.ValidationValue);
        CaptureOriginalValidation(validation);
    }

    static string SelectBooleanText(
        string currentText,
        string originalText,
        bool currentValue,
        bool originalValue)
    {
        if (currentText != originalText || currentValue == originalValue)
        {
            return currentText;
        }
        return currentValue ? "true" : "false";
    }

    static void SynchronizeProperties(OdfFormProperty[] properties)
    {
        int index = 0;
        while (index < properties.Length)
        {
            OdfFormProperty property = properties[index];
            OdfNode? node = property.Node;
            if (node != null)
            {
                WriteChanged(node, OdfNamespaces.Form, "property-name", OdfNamespaces.FormPrefix, property.Name, property.OriginalName);
                WriteChanged(node, OdfNamespaces.Office, "value-type", OdfNamespaces.OfficePrefix, property.ValueType, property.OriginalValueType);
                int valueIndex = 0;
                while (valueIndex < property.Values.Length)
                {
                    SynchronizePropertyValue(
                        property.Values[valueIndex],
                        property.ValueType);
                    valueIndex = valueIndex + 1;
                }
                property.OriginalName = property.Name;
                property.OriginalValueType = property.ValueType;
            }
            index = index + 1;
        }
    }

    static void SynchronizePropertyValue(
        OdfFormPropertyValue value,
        string valueType)
    {
        OdfNode? node = value.Node;
        if (node == null)
        {
            return;
        }
        string localName = TypedValueAttribute(valueType);
        string originalLocalName = TypedValueAttributeForNode(
            node,
            value.OriginalValue);
        if (value.Value != value.OriginalValue || localName != originalLocalName)
        {
            RemoveTypedValueAttributes(node);
            WriteOptional(
                node,
                OdfNamespaces.Office,
                localName,
                OdfNamespaces.OfficePrefix,
                value.Value);
        }
        WriteChanged(node, OdfNamespaces.Office, "currency", OdfNamespaces.OfficePrefix, value.Currency, value.OriginalCurrency);
        value.OriginalValue = value.Value;
        value.OriginalCurrency = value.Currency;
    }

    static void SynchronizeOptions(OdfFormOption[] options)
    {
        int index = 0;
        while (index < options.Length)
        {
            OdfFormOption option = options[index];
            OdfNode? node = option.Node;
            if (node != null)
            {
                WriteChanged(node, OdfNamespaces.Form, "label", OdfNamespaces.FormPrefix, option.Label, option.OriginalLabel);
                WriteChanged(node, OdfNamespaces.Form, "value", OdfNamespaces.FormPrefix, option.Value, option.OriginalValue);
                option.SelectedValue = SelectBooleanText(option.SelectedValue, option.OriginalSelectedValue, option.IsSelected, option.OriginalIsSelected);
                option.CurrentSelectedValue = SelectBooleanText(option.CurrentSelectedValue, option.OriginalCurrentSelectedValue, option.IsCurrentSelected, option.OriginalIsCurrentSelected);
                WriteChanged(node, OdfNamespaces.Form, "selected", OdfNamespaces.FormPrefix, option.SelectedValue, option.OriginalSelectedValue);
                WriteChanged(node, OdfNamespaces.Form, "current-selected", OdfNamespaces.FormPrefix, option.CurrentSelectedValue, option.OriginalCurrentSelectedValue);
                if (option.Text != option.OriginalText)
                {
                    SetTextContent(node, option.Text);
                }
                option.OriginalLabel = option.Label;
                option.OriginalValue = option.Value;
                option.OriginalText = option.Text;
                option.OriginalSelectedValue = option.SelectedValue;
                option.OriginalIsSelected = option.IsSelected;
                option.OriginalCurrentSelectedValue = option.CurrentSelectedValue;
                option.OriginalIsCurrentSelected = option.IsCurrentSelected;
            }
            index = index + 1;
        }
    }

    static void SynchronizeEvents(OdfDrawingEvent[] events)
    {
        int index = 0;
        while (index < events.Length)
        {
            OdfDrawingEvent item = events[index];
            if (item.Node != null)
            {
                WriteOptional(item.Node, OdfNamespaces.Script, "event-name", OdfNamespaces.ScriptPrefix, item.EventName);
                WriteOptional(item.Node, OdfNamespaces.Script, "language", OdfNamespaces.ScriptPrefix, item.Language);
                WriteOptional(item.Node, OdfNamespaces.Script, "macro-name", OdfNamespaces.ScriptPrefix, item.MacroName);
                WriteOptional(item.Node, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, item.Href);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "action", OdfNamespaces.PresentationPrefix, item.Action);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "effect", OdfNamespaces.PresentationPrefix, item.Effect);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "direction", OdfNamespaces.PresentationPrefix, item.Direction);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "speed", OdfNamespaces.PresentationPrefix, item.Speed);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "start-scale", OdfNamespaces.PresentationPrefix, item.StartScale);
                WriteOptional(item.Node, OdfNamespaces.Presentation, "verb", OdfNamespaces.PresentationPrefix, item.Verb);
                WriteOptional(item.Node, OdfNamespaces.XLink, "show", OdfNamespaces.XLinkPrefix, item.Show);
                WriteOptional(item.Node, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, item.Actuate);
            }
            index = index + 1;
        }
    }

    static void CaptureOriginalControl(OdfFormControl control)
    {
        control.OriginalName = control.Name;
        control.OriginalId = control.Id;
        control.OriginalXmlId = control.XmlId;
        control.OriginalControlImplementation = control.ControlImplementation;
        control.OriginalLabel = control.Label;
        control.OriginalFor = control.For;
        control.OriginalValue = control.Value;
        control.OriginalCurrentValue = control.CurrentValue;
        control.OriginalMinValue = control.MinValue;
        control.OriginalMaxValue = control.MaxValue;
        control.OriginalTitle = control.Title;
        control.OriginalDataStyleName = control.DataStyleName;
    }

    static void CaptureOriginalBinding(OdfFormBinding binding)
    {
        binding.OriginalLinkedCell = binding.LinkedCell;
        binding.OriginalSourceCellRange = binding.SourceCellRange;
        binding.OriginalDataField = binding.DataField;
        binding.OriginalListSource = binding.ListSource;
        binding.OriginalListSourceType = binding.ListSourceType;
        binding.OriginalBoundColumn = binding.BoundColumn;
        binding.OriginalXFormsBind = binding.XFormsBind;
        binding.OriginalXFormsListSource = binding.XFormsListSource;
        binding.OriginalXFormsSubmission = binding.XFormsSubmission;
    }

    static void CaptureOriginalValidation(OdfFormValidation validation)
    {
        validation.OriginalDisabledValue = validation.DisabledValue;
        validation.OriginalIsDisabled = validation.IsDisabled;
        validation.OriginalInputRequiredValue = validation.InputRequiredValue;
        validation.OriginalIsInputRequired = validation.IsInputRequired;
        validation.OriginalReadOnlyValue = validation.ReadOnlyValue;
        validation.OriginalIsReadOnly = validation.IsReadOnly;
        validation.OriginalValidationValue = validation.ValidationValue;
        validation.OriginalIsValidationEnabled =
            validation.IsValidationEnabled;
        validation.OriginalMaxLength = validation.MaxLength;
    }

    static string ReadTypedValue(OdfNode node, string valueType)
    {
        return Read(
            node,
            OdfNamespaces.Office,
            TypedValueAttribute(valueType));
    }

    static string TypedValueAttribute(string valueType)
    {
        if (valueType == "date") return "date-value";
        if (valueType == "time") return "time-value";
        if (valueType == "boolean") return "boolean-value";
        if (valueType == "string") return "string-value";
        return "value";
    }

    static string TypedValueAttributeForNode(
        OdfNode node,
        string originalValue)
    {
        string[] names = [
            "value",
            "string-value",
            "date-value",
            "time-value",
            "boolean-value"];
        int index = 0;
        while (index < names.Length)
        {
            if (Read(node, OdfNamespaces.Office, names[index])
                == originalValue
                && HasAttribute(node, OdfNamespaces.Office, names[index]))
            {
                return names[index];
            }
            index = index + 1;
        }
        return "";
    }

    static void RemoveTypedValueAttributes(OdfNode node)
    {
        StyleDocumentModel.RemoveAttribute(node, OdfNamespaces.Office, "value");
        StyleDocumentModel.RemoveAttribute(node, OdfNamespaces.Office, "string-value");
        StyleDocumentModel.RemoveAttribute(node, OdfNamespaces.Office, "date-value");
        StyleDocumentModel.RemoveAttribute(node, OdfNamespaces.Office, "time-value");
        StyleDocumentModel.RemoveAttribute(node, OdfNamespaces.Office, "boolean-value");
    }

    static bool BooleanValue(string value)
    {
        return value == "true" || value == "1";
    }

    static bool BooleanValid(string value)
    {
        return value.Length == 0
            || value == "true"
            || value == "false"
            || value == "1"
            || value == "0";
    }

    static OdfDrawingEventKind EventKind(OdfNode node)
    {
        if (IsElement(node, OdfNamespaces.Script, "event-listener"))
        {
            return OdfDrawingEventKind.Script;
        }
        if (IsElement(node, OdfNamespaces.Presentation, "event-listener"))
        {
            return OdfDrawingEventKind.Presentation;
        }
        return OdfDrawingEventKind.Unknown;
    }

    static OdfFormControlKind ControlKind(OdfNode node)
    {
        if (node.Kind != OdfNodeKind.Element
            || node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Form)
        {
            return OdfFormControlKind.Unknown;
        }
        string name = node.Name.LocalName;
        if (name == "form") return OdfFormControlKind.Form;
        if (name == "button") return OdfFormControlKind.Button;
        if (name == "text") return OdfFormControlKind.Text;
        if (name == "textarea") return OdfFormControlKind.TextArea;
        if (name == "checkbox") return OdfFormControlKind.CheckBox;
        if (name == "radio") return OdfFormControlKind.RadioButton;
        if (name == "listbox") return OdfFormControlKind.ListBox;
        if (name == "combobox") return OdfFormControlKind.ComboBox;
        if (name == "value-range") return OdfFormControlKind.ValueRange;
        if (name == "image") return OdfFormControlKind.Image;
        if (name == "file") return OdfFormControlKind.File;
        if (name == "date") return OdfFormControlKind.Date;
        if (name == "time") return OdfFormControlKind.Time;
        if (name == "frame") return OdfFormControlKind.Frame;
        if (name == "hidden") return OdfFormControlKind.Hidden;
        if (name == "fixed-text") return OdfFormControlKind.Label;
        if (name == "formatted-text") return OdfFormControlKind.FormattedText;
        if (name == "number") return OdfFormControlKind.Number;
        if (name == "password") return OdfFormControlKind.Password;
        if (name == "image-frame") return OdfFormControlKind.ImageFrame;
        if (name == "grid") return OdfFormControlKind.Grid;
        if (name == "generic-control") return OdfFormControlKind.GenericControl;
        if (name == "column") return OdfFormControlKind.Column;
        return OdfFormControlKind.Unknown;
    }

    static OdfNode? FindDirectChild(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsElement(node.Children[index], namespaceName, localName))
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static bool HasAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string TextContent(OdfNode node)
    {
        string result = "";
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (child.Kind == OdfNodeKind.Text)
            {
                result = result + child.Value;
            }
            index = index + 1;
        }
        return result;
    }

    static void SetTextContent(OdfNode node, string text)
    {
        List<OdfNode> children = new List<OdfNode>();
        bool inserted = false;
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (child.Kind == OdfNodeKind.Text)
            {
                if (!inserted && text.Length > 0)
                {
                    OdfNode replacement = CreateTextNode(node, text);
                    children.Add(replacement);
                    inserted = true;
                }
                child.Parent = null;
            }
            else
            {
                children.Add(child);
            }
            index = index + 1;
        }
        if (!inserted && text.Length > 0)
        {
            children.Add(CreateTextNode(node, text));
        }
        node.Children = children.ToArray();
    }

    static OdfNode CreateTextNode(OdfNode parent, string text)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Text;
        result.Value = text;
        result.Parent = parent;
        result.Location.PartPath = parent.Location.PartPath;
        return result;
    }

    static void WriteChanged(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string original)
    {
        if (value != original)
        {
            WriteOptional(node, namespaceName, localName, prefix, value);
        }
    }

    static void WriteOptional(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }
}
