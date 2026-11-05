namespace odf;

/// <summary>
/// Identifies an OpenDocument style family.
/// </summary>
public enum OdfStyleFamily
{
    /// <summary>
    /// The style family is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Paragraph styles.
    /// </summary>
    Paragraph = 1,
    /// <summary>
    /// Text styles.
    /// </summary>
    Text = 2,
    /// <summary>
    /// Section styles.
    /// </summary>
    Section = 3,
    /// <summary>
    /// Table styles.
    /// </summary>
    Table = 4,
    /// <summary>
    /// Table-column styles.
    /// </summary>
    TableColumn = 5,
    /// <summary>
    /// Table-row styles.
    /// </summary>
    TableRow = 6,
    /// <summary>
    /// Table-cell styles.
    /// </summary>
    TableCell = 7,
    /// <summary>
    /// Graphic styles.
    /// </summary>
    Graphic = 8,
    /// <summary>
    /// Chart styles.
    /// </summary>
    Chart = 9,
    /// <summary>
    /// Drawing-page styles.
    /// </summary>
    DrawingPage = 10,
    /// <summary>
    /// Presentation styles.
    /// </summary>
    Presentation = 11,
    /// <summary>
    /// Ruby styles.
    /// </summary>
    Ruby = 12,
    /// <summary>
    /// List and outline styles.
    /// </summary>
    List = 13
}
