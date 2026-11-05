using System;

namespace odf;

internal static class FormEditingModel
{
    internal static OdfFormControl[] GetFormControls(OdfDocument document)
    {
        RequireDocument(document);
        OdfFormControl[] result =
            new OdfFormControl[document.FormControls.Length];
        Array.Copy(document.FormControls, result, result.Length);
        return result;
    }

    internal static OdfFormControl? FindFormControl(
        OdfDocument document,
        string name)
    {
        RequireDocument(document);
        if (name == null || name.Length == 0)
        {
            return null;
        }
        OdfFormControl? result = null;
        int index = 0;
        while (index < document.FormControls.Length)
        {
            OdfFormControl control = document.FormControls[index];
            if (control.Name == name)
            {
                if (result != null)
                {
                    return null;
                }
                result = control;
            }
            index = index + 1;
        }
        return result;
    }

    internal static OdfFormControl CreateFormControl(
        OdfDocument document,
        OdfFormControl? parent,
        OdfFormControlKind kind,
        string name)
    {
        RequireDocument(document);
        ValidateControlParent(document, parent, kind);
        if (name == null)
        {
            throw new OdfEditException("A form-control name is required.");
        }
        string localName = ControlLocalName(kind);
        OdfNode node = CreateElementNode(
            OdfNamespaces.Form,
            localName,
            OdfNamespaces.FormPrefix,
            ControlPartPath(document, parent));
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Form,
            "name",
            OdfNamespaces.FormPrefix,
            name);
        if (parent == null)
        {
            OdfNode forms = EnsureFormsContainer(document);
            StyleDocumentModel.AppendNode(forms, node);
        }
        else
        {
            OdfNode parentNode = parent.Node ?? new OdfNode();
            StyleDocumentModel.AppendNode(parentNode, node);
        }
        OdfFormControl created =
            FormDocumentModel.MaterializeCreatedControl(node, parent);
        InsertDocumentControl(document, created, parent);
        if (parent != null)
        {
            AppendChildControl(parent, created);
        }
        FormDocumentModel.RefreshReferences(document);
        return created;
    }

    internal static void DeleteFormControl(
        OdfDocument document,
        OdfFormControl control)
    {
        RequireCurrentControl(document, control);
        OdfNode node = control.Node ?? new OdfNode();
        StyleDocumentModel.RemoveNodeFromParent(node);
        OdfFormControl? parent = control.Parent;
        if (parent != null)
        {
            parent.Children = RemoveChildControl(parent.Children, control);
        }
        OdfFormControl[] survivors = new OdfFormControl[
            CountSurvivingControls(document.FormControls, control)];
        int source = 0;
        int destination = 0;
        while (source < document.FormControls.Length)
        {
            OdfFormControl item = document.FormControls[source];
            if (!IsControlOrDescendant(item, control))
            {
                survivors[destination] = item;
                destination = destination + 1;
            }
            source = source + 1;
        }
        document.FormControls = survivors;
        UpdateFormState(document);
        FormDocumentModel.RefreshReferences(document);
    }

    internal static OdfFormProperty AddFormProperty(
        OdfDocument document,
        OdfFormControl control,
        OdfFormProperty property)
    {
        RequireCurrentControl(document, control);
        ValidateProperty(property);
        OdfNode controlNode = control.Node ?? new OdfNode();
        OdfNode container = EnsureDirectContainer(
            controlNode,
            OdfNamespaces.Form,
            "properties",
            OdfNamespaces.FormPrefix);
        string localName = property.IsList ? "list-property" : "property";
        OdfNode node = CreateElementNode(
            OdfNamespaces.Form,
            localName,
            OdfNamespaces.FormPrefix,
            control.PartPath);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Form,
            "property-name",
            OdfNamespaces.FormPrefix,
            property.Name);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Office,
            "value-type",
            OdfNamespaces.OfficePrefix,
            property.ValueType);
        if (property.IsList)
        {
            int index = 0;
            while (index < property.Values.Length)
            {
                OdfNode valueNode = CreateElementNode(
                    OdfNamespaces.Form,
                    "list-value",
                    OdfNamespaces.FormPrefix,
                    control.PartPath);
                WritePropertyValue(
                    valueNode,
                    property.ValueType,
                    property.Values[index]);
                StyleDocumentModel.AppendNode(node, valueNode);
                index = index + 1;
            }
        }
        else
        {
            WritePropertyValue(node, property.ValueType, property.Values[0]);
        }
        StyleDocumentModel.AppendNode(container, node);
        OdfFormProperty created =
            FormDocumentModel.MaterializeCreatedProperty(
                node,
                property.IsList);
        control.Properties = AppendFormProperty(control.Properties, created);
        return created;
    }

    internal static void DeleteFormProperty(
        OdfDocument document,
        OdfFormControl control,
        OdfFormProperty property)
    {
        RequireCurrentControl(document, control);
        int index = IndexOfFormProperty(control.Properties, property);
        if (index < 0 || property.Node == null)
        {
            throw new OdfEditException(
                "The form property does not belong to the control.");
        }
        StyleDocumentModel.RemoveNodeFromParent(property.Node);
        control.Properties = RemoveFormProperty(control.Properties, index);
    }

    internal static OdfFormOption AddFormOption(
        OdfDocument document,
        OdfFormControl control,
        OdfFormOption option)
    {
        RequireCurrentControl(document, control);
        if (option == null)
        {
            throw new OdfEditException("A form option is required.");
        }
        bool isOption = control.Kind == OdfFormControlKind.ListBox;
        if (!isOption && control.Kind != OdfFormControlKind.ComboBox)
        {
            throw new OdfEditException(
                "Options can be added only to list-box and combo-box controls.");
        }
        OdfNode node = CreateElementNode(
            OdfNamespaces.Form,
            isOption ? "option" : "item",
            OdfNamespaces.FormPrefix,
            control.PartPath);
        WriteOptionalAttribute(node, OdfNamespaces.Form, "label", OdfNamespaces.FormPrefix, option.Label);
        WriteOptionalAttribute(node, OdfNamespaces.Form, "value", OdfNamespaces.FormPrefix, option.Value);
        WriteOptionalAttribute(node, OdfNamespaces.Form, "selected", OdfNamespaces.FormPrefix, SelectBooleanText(option.SelectedValue, option.IsSelected));
        WriteOptionalAttribute(node, OdfNamespaces.Form, "current-selected", OdfNamespaces.FormPrefix, SelectBooleanText(option.CurrentSelectedValue, option.IsCurrentSelected));
        AppendTextNode(node, option.Text);
        OdfNode controlNode = control.Node ?? new OdfNode();
        StyleDocumentModel.AppendNode(controlNode, node);
        OdfFormOption created =
            FormDocumentModel.MaterializeCreatedOption(node, isOption);
        control.Options = AppendFormOption(control.Options, created);
        return created;
    }

    internal static void DeleteFormOption(
        OdfDocument document,
        OdfFormControl control,
        OdfFormOption option)
    {
        RequireCurrentControl(document, control);
        int index = IndexOfFormOption(control.Options, option);
        if (index < 0 || option.Node == null)
        {
            throw new OdfEditException(
                "The form option does not belong to the control.");
        }
        StyleDocumentModel.RemoveNodeFromParent(option.Node);
        control.Options = RemoveFormOption(control.Options, index);
    }

    internal static OdfDrawingEvent AddFormEvent(
        OdfDocument document,
        OdfFormControl control,
        OdfDrawingEvent eventDescription)
    {
        RequireCurrentControl(document, control);
        if (eventDescription == null
            || (eventDescription.Kind != OdfDrawingEventKind.Script
                && eventDescription.Kind != OdfDrawingEventKind.Presentation))
        {
            throw new OdfEditException(
                "A script or presentation event description is required.");
        }
        OdfNode controlNode = control.Node ?? new OdfNode();
        OdfNode container = EnsureDirectContainer(
            controlNode,
            OdfNamespaces.Office,
            "event-listeners",
            OdfNamespaces.OfficePrefix);
        bool script = eventDescription.Kind == OdfDrawingEventKind.Script;
        OdfNode node = CreateElementNode(
            script ? OdfNamespaces.Script : OdfNamespaces.Presentation,
            "event-listener",
            script ? OdfNamespaces.ScriptPrefix : OdfNamespaces.PresentationPrefix,
            control.PartPath);
        CopyEventAttributes(node, eventDescription);
        StyleDocumentModel.AppendNode(container, node);
        OdfDrawingEvent created =
            FormDocumentModel.MaterializeCreatedEvent(
                node,
                eventDescription.Kind);
        control.Events = AppendFormEvent(control.Events, created);
        return created;
    }

    internal static void DeleteFormEvent(
        OdfDocument document,
        OdfFormControl control,
        OdfDrawingEvent eventDescription)
    {
        RequireCurrentControl(document, control);
        int index = IndexOfFormEvent(control.Events, eventDescription);
        if (index < 0 || eventDescription.Node == null)
        {
            throw new OdfEditException(
                "The form event does not belong to the control.");
        }
        StyleDocumentModel.RemoveNodeFromParent(eventDescription.Node);
        control.Events = RemoveFormEvent(control.Events, index);
    }

    internal static void ApplyFormChanges(OdfDocument document)
    {
        RequireDocument(document);
        FormDocumentModel.SynchronizeForms(document);
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfEditException("A document is required.");
        }
    }

    static void RequireCurrentControl(
        OdfDocument document,
        OdfFormControl control)
    {
        RequireDocument(document);
        if (control == null || IndexOfDocumentControl(document, control) < 0
            || control.Node == null)
        {
            throw new OdfEditException(
                "The form control does not belong to the document.");
        }
    }

    static int IndexOfDocumentControl(
        OdfDocument document,
        OdfFormControl control)
    {
        int index = 0;
        while (index < document.FormControls.Length)
        {
            if (document.FormControls[index] == control)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static void ValidateControlParent(
        OdfDocument document,
        OdfFormControl? parent,
        OdfFormControlKind kind)
    {
        if (kind == OdfFormControlKind.Unknown)
        {
            throw new OdfEditException("A recognized form-control kind is required.");
        }
        if (parent == null)
        {
            if (kind != OdfFormControlKind.Form)
            {
                throw new OdfEditException(
                    "Only a form can be created without a parent control.");
            }
            return;
        }
        RequireCurrentControl(document, parent);
        if (parent.Kind == OdfFormControlKind.Grid)
        {
            if (kind != OdfFormControlKind.Column)
            {
                throw new OdfEditException(
                    "A grid can directly contain only columns.");
            }
            return;
        }
        if (kind == OdfFormControlKind.Column)
        {
            throw new OdfEditException("A column must be created inside a grid.");
        }
        if (parent.Kind != OdfFormControlKind.Form
            && parent.Kind != OdfFormControlKind.Column)
        {
            throw new OdfEditException(
                "The selected control cannot contain child controls.");
        }
        if (parent.Kind == OdfFormControlKind.Column
            && kind == OdfFormControlKind.Form)
        {
            throw new OdfEditException("A grid column cannot contain a form.");
        }
    }

    static string ControlLocalName(OdfFormControlKind kind)
    {
        if (kind == OdfFormControlKind.Form) return "form";
        if (kind == OdfFormControlKind.Button) return "button";
        if (kind == OdfFormControlKind.Text) return "text";
        if (kind == OdfFormControlKind.TextArea) return "textarea";
        if (kind == OdfFormControlKind.CheckBox) return "checkbox";
        if (kind == OdfFormControlKind.RadioButton) return "radio";
        if (kind == OdfFormControlKind.ListBox) return "listbox";
        if (kind == OdfFormControlKind.ComboBox) return "combobox";
        if (kind == OdfFormControlKind.ValueRange) return "value-range";
        if (kind == OdfFormControlKind.Image) return "image";
        if (kind == OdfFormControlKind.File) return "file";
        if (kind == OdfFormControlKind.Date) return "date";
        if (kind == OdfFormControlKind.Time) return "time";
        if (kind == OdfFormControlKind.Frame) return "frame";
        if (kind == OdfFormControlKind.Hidden) return "hidden";
        if (kind == OdfFormControlKind.Label) return "fixed-text";
        if (kind == OdfFormControlKind.FormattedText) return "formatted-text";
        if (kind == OdfFormControlKind.Number) return "number";
        if (kind == OdfFormControlKind.Password) return "password";
        if (kind == OdfFormControlKind.ImageFrame) return "image-frame";
        if (kind == OdfFormControlKind.Grid) return "grid";
        if (kind == OdfFormControlKind.GenericControl) return "generic-control";
        if (kind == OdfFormControlKind.Column) return "column";
        throw new OdfEditException("A recognized form-control kind is required.");
    }

    static string ControlPartPath(
        OdfDocument document,
        OdfFormControl? parent)
    {
        if (parent != null)
        {
            return parent.PartPath;
        }
        if (document.Body != null)
        {
            return document.Body.Location.PartPath;
        }
        return "";
    }

    static OdfNode EnsureFormsContainer(OdfDocument document)
    {
        OdfNode? body = document.Body;
        if (body == null)
        {
            throw new OdfEditException(
                "A document body is required before creating a form.");
        }
        OdfNode? content = FindOfficeContent(body);
        if (content == null)
        {
            throw new OdfEditException(
                "A document content element is required before creating a form.");
        }
        OdfNode? existing = FindDirectElement(
            content,
            OdfNamespaces.Office,
            "forms");
        if (existing != null)
        {
            return existing;
        }
        OdfNode forms = CreateElementNode(
            OdfNamespaces.Office,
            "forms",
            OdfNamespaces.OfficePrefix,
            content.Location.PartPath);
        forms.NamespaceDeclarations =
        [
            CreateNamespaceDeclaration(
                OdfNamespaces.FormPrefix,
                OdfNamespaces.Form,
                content.Location.PartPath)
        ];
        PrependChild(content, forms);
        return forms;
    }

    static OdfNode? FindOfficeContent(OdfNode body)
    {
        int index = 0;
        while (index < body.Children.Length)
        {
            OdfNode child = body.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Office)
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode EnsureDirectContainer(
        OdfNode parent,
        string namespaceName,
        string localName,
        string prefix)
    {
        OdfNode? existing = FindDirectElement(parent, namespaceName, localName);
        if (existing != null)
        {
            return existing;
        }
        OdfNode created = CreateElementNode(
            namespaceName,
            localName,
            prefix,
            parent.Location.PartPath);
        StyleDocumentModel.AppendNode(parent, created);
        return created;
    }

    static OdfNode? FindDirectElement(
        OdfNode parent,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == namespaceName
                && child.Name.LocalName == localName)
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode CreateElementNode(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            namespaceName,
            localName,
            prefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static OdfNamespaceDeclaration CreateNamespaceDeclaration(
        string prefix,
        string namespaceName,
        string partPath)
    {
        OdfNamespaceDeclaration result = new OdfNamespaceDeclaration();
        result.Prefix = prefix;
        result.NamespaceName = namespaceName;
        result.Location.PartPath = partPath;
        return result;
    }

    static void WriteOptionalAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value != null && value.Length > 0)
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static void ValidateProperty(OdfFormProperty property)
    {
        if (property == null || property.Name == null
            || property.Name.Length == 0 || property.ValueType == null
            || property.ValueType.Length == 0 || property.Values == null
            || property.Values.Length == 0)
        {
            throw new OdfEditException(
                "A named, typed form property with values is required.");
        }
        if (!property.IsList && property.Values.Length != 1)
        {
            throw new OdfEditException(
                "A scalar form property requires exactly one value.");
        }
        int index = 0;
        while (index < property.Values.Length)
        {
            if (property.Values[index] == null)
            {
                throw new OdfEditException(
                    "Form-property values cannot contain null entries.");
            }
            index = index + 1;
        }
    }

    static void WritePropertyValue(
        OdfNode node,
        string valueType,
        OdfFormPropertyValue value)
    {
        string attributeName = "value";
        if (valueType == "date") attributeName = "date-value";
        else if (valueType == "time") attributeName = "time-value";
        else if (valueType == "boolean") attributeName = "boolean-value";
        else if (valueType == "string") attributeName = "string-value";
        WriteOptionalAttribute(node, OdfNamespaces.Office, attributeName, OdfNamespaces.OfficePrefix, value.Value);
        WriteOptionalAttribute(node, OdfNamespaces.Office, "currency", OdfNamespaces.OfficePrefix, value.Currency);
    }

    static string SelectBooleanText(string lexicalValue, bool value)
    {
        if (lexicalValue != null && lexicalValue.Length > 0)
        {
            return lexicalValue;
        }
        return value ? "true" : "";
    }

    static void AppendTextNode(OdfNode parent, string value)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        StyleDocumentModel.AppendNode(parent, text);
    }

    static void CopyEventAttributes(
        OdfNode node,
        OdfDrawingEvent eventDescription)
    {
        WriteOptionalAttribute(node, OdfNamespaces.Script, "event-name", OdfNamespaces.ScriptPrefix, eventDescription.EventName);
        WriteOptionalAttribute(node, OdfNamespaces.Script, "language", OdfNamespaces.ScriptPrefix, eventDescription.Language);
        WriteOptionalAttribute(node, OdfNamespaces.Script, "macro-name", OdfNamespaces.ScriptPrefix, eventDescription.MacroName);
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, eventDescription.Href);
        WriteOptionalAttribute(node, OdfNamespaces.Presentation, "action", OdfNamespaces.PresentationPrefix, eventDescription.Action);
        WriteOptionalAttribute(node, OdfNamespaces.Presentation, "effect", OdfNamespaces.PresentationPrefix, eventDescription.Effect);
        WriteOptionalAttribute(node, OdfNamespaces.Presentation, "direction", OdfNamespaces.PresentationPrefix, eventDescription.Direction);
        WriteOptionalAttribute(node, OdfNamespaces.Presentation, "speed", OdfNamespaces.PresentationPrefix, eventDescription.Speed);
        WriteOptionalAttribute(node, OdfNamespaces.Presentation, "start-scale", OdfNamespaces.PresentationPrefix, eventDescription.StartScale);
        WriteOptionalAttribute(node, OdfNamespaces.Presentation, "verb", OdfNamespaces.PresentationPrefix, eventDescription.Verb);
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "show", OdfNamespaces.XLinkPrefix, eventDescription.Show);
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, eventDescription.Actuate);
    }

    static void PrependChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        children[0] = child;
        Array.Copy(parent.Children, 0, children, 1, parent.Children.Length);
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
    }

    static void InsertDocumentControl(
        OdfDocument document,
        OdfFormControl control,
        OdfFormControl? parent)
    {
        int insertion = document.FormControls.Length;
        if (parent != null)
        {
            insertion = IndexOfDocumentControl(document, parent) + 1;
            while (insertion < document.FormControls.Length
                && IsControlOrDescendant(
                    document.FormControls[insertion],
                    parent))
            {
                insertion = insertion + 1;
            }
        }
        OdfFormControl[] controls =
            new OdfFormControl[document.FormControls.Length + 1];
        Array.Copy(document.FormControls, 0, controls, 0, insertion);
        controls[insertion] = control;
        Array.Copy(
            document.FormControls,
            insertion,
            controls,
            insertion + 1,
            document.FormControls.Length - insertion);
        document.FormControls = controls;
        UpdateFormState(document);
    }

    static void AppendChildControl(
        OdfFormControl parent,
        OdfFormControl child)
    {
        OdfFormControl[] children =
            new OdfFormControl[parent.Children.Length + 1];
        Array.Copy(parent.Children, children, parent.Children.Length);
        children[children.Length - 1] = child;
        parent.Children = children;
    }

    static OdfFormProperty[] AppendFormProperty(
        OdfFormProperty[] source,
        OdfFormProperty item)
    {
        OdfFormProperty[] result = new OdfFormProperty[source.Length + 1];
        Array.Copy(source, result, source.Length);
        result[result.Length - 1] = item;
        return result;
    }

    static OdfFormOption[] AppendFormOption(
        OdfFormOption[] source,
        OdfFormOption item)
    {
        OdfFormOption[] result = new OdfFormOption[source.Length + 1];
        Array.Copy(source, result, source.Length);
        result[result.Length - 1] = item;
        return result;
    }

    static OdfDrawingEvent[] AppendFormEvent(
        OdfDrawingEvent[] source,
        OdfDrawingEvent item)
    {
        OdfDrawingEvent[] result = new OdfDrawingEvent[source.Length + 1];
        Array.Copy(source, result, source.Length);
        result[result.Length - 1] = item;
        return result;
    }

    static int IndexOfFormProperty(
        OdfFormProperty[] source,
        OdfFormProperty item)
    {
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == item) return index;
            index = index + 1;
        }
        return -1;
    }

    static int IndexOfFormOption(
        OdfFormOption[] source,
        OdfFormOption item)
    {
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == item) return index;
            index = index + 1;
        }
        return -1;
    }

    static int IndexOfFormEvent(
        OdfDrawingEvent[] source,
        OdfDrawingEvent item)
    {
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == item) return index;
            index = index + 1;
        }
        return -1;
    }

    static OdfFormProperty[] RemoveFormProperty(
        OdfFormProperty[] source,
        int removedIndex)
    {
        OdfFormProperty[] result = new OdfFormProperty[source.Length - 1];
        Array.Copy(source, 0, result, 0, removedIndex);
        Array.Copy(source, removedIndex + 1, result, removedIndex, source.Length - removedIndex - 1);
        return result;
    }

    static OdfFormOption[] RemoveFormOption(
        OdfFormOption[] source,
        int removedIndex)
    {
        OdfFormOption[] result = new OdfFormOption[source.Length - 1];
        Array.Copy(source, 0, result, 0, removedIndex);
        Array.Copy(source, removedIndex + 1, result, removedIndex, source.Length - removedIndex - 1);
        return result;
    }

    static OdfDrawingEvent[] RemoveFormEvent(
        OdfDrawingEvent[] source,
        int removedIndex)
    {
        OdfDrawingEvent[] result = new OdfDrawingEvent[source.Length - 1];
        Array.Copy(source, 0, result, 0, removedIndex);
        Array.Copy(source, removedIndex + 1, result, removedIndex, source.Length - removedIndex - 1);
        return result;
    }

    static OdfFormControl[] RemoveChildControl(
        OdfFormControl[] source,
        OdfFormControl item)
    {
        int index = 0;
        while (index < source.Length && source[index] != item)
        {
            index = index + 1;
        }
        if (index == source.Length) return source;
        OdfFormControl[] result = new OdfFormControl[source.Length - 1];
        Array.Copy(source, 0, result, 0, index);
        Array.Copy(source, index + 1, result, index, source.Length - index - 1);
        return result;
    }

    static int CountSurvivingControls(
        OdfFormControl[] source,
        OdfFormControl removed)
    {
        int count = 0;
        int index = 0;
        while (index < source.Length)
        {
            if (!IsControlOrDescendant(source[index], removed))
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    static bool IsControlOrDescendant(
        OdfFormControl candidate,
        OdfFormControl ancestor)
    {
        OdfFormControl? current = candidate;
        while (current != null)
        {
            if (current == ancestor) return true;
            current = current.Parent;
        }
        return false;
    }

    static void UpdateFormState(OdfDocument document)
    {
        if (document.FormState == null)
        {
            document.FormState = new FormDocumentState();
        }
        document.FormState.Controls = document.FormControls;
    }
}
