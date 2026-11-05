namespace docx;

/// <summary>
/// Traverses, creates, reads, binds, and fills supported content controls.
/// </summary>
public interface IDocxContentControlManagement
{
    /// <summary>Returns content controls from all supported document stories.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The controls in story and nesting order.</returns>
    List<DocxContentControlInfo> GetDocumentContentControls(DocxDocument document);

    /// <summary>Returns all controls nested below one content control.</summary>
    /// <param name="contentControl">The containing control.</param>
    /// <returns>The descendants in XML order.</returns>
    List<DocxContentControlInfo> GetDescendantContentControls(DocxContentControl contentControl);

    /// <summary>Returns metadata and type-specific values for one control.</summary>
    /// <param name="contentControl">The control to inspect.</param>
    /// <returns>The content-control descriptor.</returns>
    DocxContentControlInfo GetContentControlInfo(DocxContentControl contentControl);

    /// <summary>Finds the first document control with a tag.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="tag">The tag to find.</param>
    /// <returns>The matching control, or null.</returns>
    DocxContentControl? FindContentControlByTag(DocxDocument document, string tag);

    /// <summary>Finds the first document control with an alias.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="alias">The alias to find.</param>
    /// <returns>The matching control, or null.</returns>
    DocxContentControl? FindContentControlByAlias(DocxDocument document, string alias);

    /// <summary>Sets the user-facing alias.</summary>
    /// <param name="contentControl">The control to edit.</param>
    /// <param name="alias">The alias value.</param>
    void SetContentControlAlias(DocxContentControl contentControl, string alias);

    /// <summary>Sets the programmatic tag.</summary>
    /// <param name="contentControl">The control to edit.</param>
    /// <param name="tag">The tag value.</param>
    void SetContentControlTag(DocxContentControl contentControl, string tag);

    /// <summary>Sets custom XML data-binding metadata.</summary>
    /// <param name="contentControl">The control to edit.</param>
    /// <param name="storeItemId">The custom XML store item identifier.</param>
    /// <param name="xpath">The binding XPath.</param>
    /// <param name="prefixMappings">The namespace prefix mappings.</param>
    void SetContentControlDataBinding(DocxContentControl contentControl, string storeItemId, string xpath, string prefixMappings);

    /// <summary>Removes custom XML data-binding metadata.</summary>
    /// <param name="contentControl">The control to edit.</param>
    void ClearContentControlDataBinding(DocxContentControl contentControl);

    /// <summary>Returns a control's visible value.</summary>
    /// <param name="contentControl">The control to inspect.</param>
    /// <returns>The visible value.</returns>
    string GetContentControlValue(DocxContentControl contentControl);

    /// <summary>Replaces a control's visible value.</summary>
    /// <param name="contentControl">The control to edit.</param>
    /// <param name="value">The replacement value.</param>
    void SetContentControlValue(DocxContentControl contentControl, string value);

    /// <summary>Fills tagged or aliased controls throughout all supported stories.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="values">Values keyed by tag, falling back to alias.</param>
    /// <returns>The number of filled controls.</returns>
    int FillDocumentContentControls(DocxDocument document, Dictionary<string, string> values);

    /// <summary>Creates an inline plain-text control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="value">The initial value.</param>
    /// <returns>The created control.</returns>
    DocxContentControl CreatePlainTextControl(string alias, string tag, string value);

    /// <summary>Creates a block-level rich-text control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="blocks">The initial rich-text blocks.</param>
    /// <returns>The created control.</returns>
    DocxContentControl CreateRichTextControl(string alias, string tag, List<DocxBlock> blocks);

    /// <summary>Replaces rich-text control blocks.</summary>
    /// <param name="contentControl">The control to edit.</param>
    /// <param name="blocks">The replacement blocks.</param>
    void SetRichTextBlocks(DocxContentControl contentControl, List<DocxBlock> blocks);

    /// <summary>Creates a checkbox control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="isChecked">The initial checked state.</param>
    /// <returns>The created control.</returns>
    DocxContentControl CreateCheckboxControl(string alias, string tag, bool isChecked);

    /// <summary>Sets a checkbox state and visible glyph.</summary>
    /// <param name="contentControl">The checkbox control.</param>
    /// <param name="isChecked">The checked state.</param>
    void SetCheckboxValue(DocxContentControl contentControl, bool isChecked);

    /// <summary>Creates an editable combo-box control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="items">The available items.</param>
    /// <param name="selectedValue">The initially selected stored value.</param>
    /// <returns>The created control.</returns>
    DocxContentControl CreateComboBoxControl(string alias, string tag, List<DocxContentControlListItem> items, string selectedValue);

    /// <summary>Creates a drop-down-list control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="items">The available items.</param>
    /// <param name="selectedValue">The initially selected stored value.</param>
    /// <returns>The created control.</returns>
    DocxContentControl CreateDropDownListControl(string alias, string tag, List<DocxContentControlListItem> items, string selectedValue);

    /// <summary>Selects an item by stored value.</summary>
    /// <param name="contentControl">The combo-box or drop-down-list control.</param>
    /// <param name="selectedValue">The stored value to select.</param>
    void SetListSelection(DocxContentControl contentControl, string selectedValue);

    /// <summary>Creates a date-picker control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="dateValue">The full-date value.</param>
    /// <param name="displayText">The displayed date.</param>
    /// <param name="format">The display format.</param>
    /// <param name="language">The language identifier.</param>
    /// <returns>The created control.</returns>
    DocxContentControl CreateDatePickerControl(string alias, string tag, string dateValue, string displayText, string format, string language);

    /// <summary>Sets a date-picker value and displayed date.</summary>
    /// <param name="contentControl">The date-picker control.</param>
    /// <param name="dateValue">The full-date value.</param>
    /// <param name="displayText">The displayed date.</param>
    void SetDatePickerValue(DocxContentControl contentControl, string dateValue, string displayText);

    /// <summary>Creates a picture control around a run containing picture markup.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="pictureRun">The run containing the picture.</param>
    /// <returns>The created control.</returns>
    DocxContentControl CreatePictureControl(string alias, string tag, DocxRun pictureRun);

    /// <summary>Replaces the run contained by a picture control.</summary>
    /// <param name="contentControl">The picture control.</param>
    /// <param name="pictureRun">The replacement picture run.</param>
    void SetPictureContent(DocxContentControl contentControl, DocxRun pictureRun);

    /// <summary>Creates a repeating section containing repeating-section items.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="itemValues">The initial item values.</param>
    /// <returns>The created repeating section.</returns>
    DocxContentControl CreateRepeatingSectionControl(string alias, string tag, List<string> itemValues);

    /// <summary>Appends a repeating-section item.</summary>
    /// <param name="contentControl">The repeating-section control.</param>
    /// <param name="value">The item value.</param>
    /// <returns>The created item control.</returns>
    DocxContentControl AddRepeatingSectionItem(DocxContentControl contentControl, string value);
}
