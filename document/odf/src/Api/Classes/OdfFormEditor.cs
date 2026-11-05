namespace odf;

/// <summary>Provides structural and field editing for document forms.</summary>
public class OdfFormEditor : IOdfFormEditor
{
    /// <summary>Initializes a form editor.</summary>
    public OdfFormEditor()
    {
    }

    /// <summary>Gets all forms and controls in document order.</summary>
    public OdfFormControl[] GetFormControls(OdfDocument document)
    {
        return FormEditingModel.GetFormControls(document);
    }

    /// <summary>Finds an unambiguously named form or control.</summary>
    public OdfFormControl? FindFormControl(
        OdfDocument document,
        string name)
    {
        return FormEditingModel.FindFormControl(document, name);
    }

    /// <summary>Creates a form or control.</summary>
    public OdfFormControl CreateFormControl(
        OdfDocument document,
        OdfFormControl? parent,
        OdfFormControlKind kind,
        string name)
    {
        return FormEditingModel.CreateFormControl(
            document,
            parent,
            kind,
            name);
    }

    /// <summary>Deletes a form or control and its descendants.</summary>
    public void DeleteFormControl(
        OdfDocument document,
        OdfFormControl control)
    {
        FormEditingModel.DeleteFormControl(document, control);
    }

    /// <summary>Adds an application-defined property to a control.</summary>
    public OdfFormProperty AddFormProperty(
        OdfDocument document,
        OdfFormControl control,
        OdfFormProperty property)
    {
        return FormEditingModel.AddFormProperty(
            document,
            control,
            property);
    }

    /// <summary>Deletes an application-defined control property.</summary>
    public void DeleteFormProperty(
        OdfDocument document,
        OdfFormControl control,
        OdfFormProperty property)
    {
        FormEditingModel.DeleteFormProperty(document, control, property);
    }

    /// <summary>Adds an option or item to a list control.</summary>
    public OdfFormOption AddFormOption(
        OdfDocument document,
        OdfFormControl control,
        OdfFormOption option)
    {
        return FormEditingModel.AddFormOption(document, control, option);
    }

    /// <summary>Deletes an option or item from a list control.</summary>
    public void DeleteFormOption(
        OdfDocument document,
        OdfFormControl control,
        OdfFormOption option)
    {
        FormEditingModel.DeleteFormOption(document, control, option);
    }

    /// <summary>Adds an inert event-listener description to a control.</summary>
    public OdfDrawingEvent AddFormEvent(
        OdfDocument document,
        OdfFormControl control,
        OdfDrawingEvent eventDescription)
    {
        return FormEditingModel.AddFormEvent(
            document,
            control,
            eventDescription);
    }

    /// <summary>Deletes an event-listener description from a control.</summary>
    public void DeleteFormEvent(
        OdfDocument document,
        OdfFormControl control,
        OdfDrawingEvent eventDescription)
    {
        FormEditingModel.DeleteFormEvent(
            document,
            control,
            eventDescription);
    }

    /// <summary>Applies direct changes made to form data fields.</summary>
    public void ApplyFormChanges(OdfDocument document)
    {
        FormEditingModel.ApplyFormChanges(document);
    }
}
