namespace docx;

/// <summary>
/// Describes a content control, its binding, and type-specific metadata.
/// </summary>
public class DocxContentControlInfo
{
    /// <summary>The parsed content control.</summary>
    public DocxContentControl Control;
    /// <summary>The supported control kind.</summary>
    public DocxContentControlKind Kind;
    /// <summary>The user-facing alias.</summary>
    public string Alias;
    /// <summary>The programmatic tag.</summary>
    public string Tag;
    /// <summary>The custom XML store item identifier.</summary>
    public string DataBindingStoreItemId;
    /// <summary>The custom XML XPath.</summary>
    public string DataBindingXPath;
    /// <summary>The data-binding prefix mappings.</summary>
    public string DataBindingPrefixMappings;
    /// <summary>The extracted visible value.</summary>
    public string Value;
    /// <summary>Whether a checkbox state was supplied.</summary>
    public bool HasCheckboxValue;
    /// <summary>The checkbox state.</summary>
    public bool CheckboxValue;
    /// <summary>The date-picker full-date value.</summary>
    public string DateValue;
    /// <summary>The date-picker display format.</summary>
    public string DateFormat;
    /// <summary>The date-picker language identifier.</summary>
    public string DateLanguage;
    /// <summary>The combo-box or drop-down-list items.</summary>
    public List<DocxContentControlListItem> ListItems;

    /// <summary>Creates a content-control descriptor.</summary>
    /// <param name="control">The parsed control.</param>
    /// <param name="kind">The supported kind.</param>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="storeItemId">The binding store item identifier.</param>
    /// <param name="xpath">The binding XPath.</param>
    /// <param name="prefixMappings">The binding prefix mappings.</param>
    /// <param name="value">The visible value.</param>
    /// <param name="hasCheckboxValue">Whether a checkbox state was supplied.</param>
    /// <param name="checkboxValue">The checkbox state.</param>
    /// <param name="dateValue">The date value.</param>
    /// <param name="dateFormat">The date format.</param>
    /// <param name="dateLanguage">The date language.</param>
    /// <param name="listItems">The list items.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxContentControlInfo(DocxContentControl control, DocxContentControlKind kind, string alias, string tag, string storeItemId, string xpath, string prefixMappings, string value, bool hasCheckboxValue, bool checkboxValue, string dateValue, string dateFormat, string dateLanguage, List<DocxContentControlListItem> listItems)
    {
        if (control == null)
        {
            throw new ArgumentNullException("control");
        }

        if (alias == null || tag == null || storeItemId == null || xpath == null || prefixMappings == null || value == null || dateValue == null || dateFormat == null || dateLanguage == null || listItems == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Control = control;
        this.Kind = kind;
        this.Alias = alias;
        this.Tag = tag;
        this.DataBindingStoreItemId = storeItemId;
        this.DataBindingXPath = xpath;
        this.DataBindingPrefixMappings = prefixMappings;
        this.Value = value;
        this.HasCheckboxValue = hasCheckboxValue;
        this.CheckboxValue = checkboxValue;
        this.DateValue = dateValue;
        this.DateFormat = dateFormat;
        this.DateLanguage = dateLanguage;
        this.ListItems = listItems;
    }
}
