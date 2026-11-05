namespace odf;

/// <summary>Provides structural and field editing for document forms.</summary>
public interface IOdfFormEditor
{
    /// <summary>Gets all forms and controls in document order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array containing the controls.</returns>
    public OdfFormControl[] GetFormControls(OdfDocument document);
    /// <summary>Finds an unambiguously named form or control.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="name">The form or control name.</param>
    /// <returns>The unique match, or <c>null</c>.</returns>
    public OdfFormControl? FindFormControl(
        OdfDocument document,
        string name);
    /// <summary>Creates a form or control.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="parent">The containing form, grid, or column, or <c>null</c> for a top-level form.</param>
    /// <param name="kind">The control kind.</param>
    /// <param name="name">The control name.</param>
    /// <returns>The created form or control.</returns>
    public OdfFormControl CreateFormControl(
        OdfDocument document,
        OdfFormControl? parent,
        OdfFormControlKind kind,
        string name);
    /// <summary>Deletes a form or control and its descendants.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="control">The form or control to delete.</param>
    public void DeleteFormControl(
        OdfDocument document,
        OdfFormControl control);
    /// <summary>Adds an application-defined property to a control.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="control">The owning control.</param>
    /// <param name="property">The property values to copy.</param>
    /// <returns>The created property.</returns>
    public OdfFormProperty AddFormProperty(
        OdfDocument document,
        OdfFormControl control,
        OdfFormProperty property);
    /// <summary>Deletes an application-defined control property.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="control">The owning control.</param>
    /// <param name="property">The property to delete.</param>
    public void DeleteFormProperty(
        OdfDocument document,
        OdfFormControl control,
        OdfFormProperty property);
    /// <summary>Adds an option or item to a list control.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="control">The owning list-box or combo-box.</param>
    /// <param name="option">The option values to copy.</param>
    /// <returns>The created option.</returns>
    public OdfFormOption AddFormOption(
        OdfDocument document,
        OdfFormControl control,
        OdfFormOption option);
    /// <summary>Deletes an option or item from a list control.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="control">The owning list-box or combo-box.</param>
    /// <param name="option">The option to delete.</param>
    public void DeleteFormOption(
        OdfDocument document,
        OdfFormControl control,
        OdfFormOption option);
    /// <summary>Adds an inert event-listener description to a control.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="control">The owning control.</param>
    /// <param name="eventDescription">The listener values to copy.</param>
    /// <returns>The created event description.</returns>
    public OdfDrawingEvent AddFormEvent(
        OdfDocument document,
        OdfFormControl control,
        OdfDrawingEvent eventDescription);
    /// <summary>Deletes an event-listener description from a control.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="control">The owning control.</param>
    /// <param name="eventDescription">The listener to delete.</param>
    public void DeleteFormEvent(
        OdfDocument document,
        OdfFormControl control,
        OdfDrawingEvent eventDescription);
    /// <summary>Applies direct changes made to form data fields.</summary>
    /// <param name="document">The document to modify.</param>
    public void ApplyFormChanges(OdfDocument document);
}
