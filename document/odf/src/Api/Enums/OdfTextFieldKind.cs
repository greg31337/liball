namespace odf;

/// <summary>
/// Identifies a standard text field family.
/// </summary>
public enum OdfTextFieldKind
{
    /// <summary>
    /// The text-field kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A page-number field.
    /// </summary>
    PageNumber = 1,
    /// <summary>
    /// A page-count field.
    /// </summary>
    PageCount = 2,
    /// <summary>
    /// A date field.
    /// </summary>
    Date = 3,
    /// <summary>
    /// A time field.
    /// </summary>
    Time = 4,
    /// <summary>
    /// An author field.
    /// </summary>
    AuthorName = 5,
    /// <summary>
    /// A sender field.
    /// </summary>
    Sender = 6,
    /// <summary>
    /// A file-name field.
    /// </summary>
    FileName = 7,
    /// <summary>
    /// A template-name field.
    /// </summary>
    TemplateName = 8,
    /// <summary>
    /// A chapter field.
    /// </summary>
    Chapter = 9,
    /// <summary>
    /// A word-count field.
    /// </summary>
    WordCount = 10,
    /// <summary>
    /// A character-count field.
    /// </summary>
    CharacterCount = 11,
    /// <summary>
    /// A paragraph-count field.
    /// </summary>
    ParagraphCount = 12,
    /// <summary>
    /// A table-count field.
    /// </summary>
    TableCount = 13,
    /// <summary>
    /// An image-count field.
    /// </summary>
    ImageCount = 14,
    /// <summary>
    /// An object-count field.
    /// </summary>
    ObjectCount = 15,
    /// <summary>
    /// A variable field.
    /// </summary>
    Variable = 16,
    /// <summary>
    /// A user-defined field.
    /// </summary>
    UserField = 17,
    /// <summary>
    /// A sequence field.
    /// </summary>
    Sequence = 18,
    /// <summary>
    /// An expression field.
    /// </summary>
    Expression = 19,
    /// <summary>
    /// A placeholder field.
    /// </summary>
    Placeholder = 20,
    /// <summary>
    /// An input field.
    /// </summary>
    Input = 21,
    /// <summary>
    /// A conditional-text field.
    /// </summary>
    ConditionalText = 22,
    /// <summary>
    /// A hidden-text field.
    /// </summary>
    HiddenText = 23,
    /// <summary>
    /// A hidden-paragraph field.
    /// </summary>
    HiddenParagraph = 24,
    /// <summary>
    /// A database or mail-merge field.
    /// </summary>
    Database = 25,
    /// <summary>
    /// A reference field.
    /// </summary>
    Reference = 26,
    /// <summary>
    /// A sheet-name field.
    /// </summary>
    SheetName = 27,
    /// <summary>
    /// A cell-address field.
    /// </summary>
    CellAddress = 28,
    /// <summary>
    /// A metadata field.
    /// </summary>
    Metadata = 29
}
