namespace pdf;

/// <summary>
/// Stores reusable table-level and header, body, and footer cell styles.
/// </summary>
public class PdfTableStyle
{
    /// <summary>
    /// Stores the style applied to the table container.
    /// </summary>
    public PdfElementStyle Table;
    /// <summary>
    /// Stores the style applied to header cells.
    /// </summary>
    public PdfElementStyle HeaderCell;
    /// <summary>
    /// Stores the style applied to body cells.
    /// </summary>
    public PdfElementStyle BodyCell;
    /// <summary>
    /// Stores the style applied to footer cells.
    /// </summary>
    public PdfElementStyle FooterCell;

    /// <summary>
    /// Initializes a table style with default element styles.
    /// </summary>
    public PdfTableStyle()
    {
        Table = new PdfElementStyle();
        HeaderCell = new PdfElementStyle();
        BodyCell = new PdfElementStyle();
        FooterCell = new PdfElementStyle();
    }

    internal PdfTableStyle Copy()
    {
        if (Table == null)
        {
            throw new System.ArgumentNullException("Table");
        }

        if (HeaderCell == null)
        {
            throw new System.ArgumentNullException("HeaderCell");
        }

        if (BodyCell == null)
        {
            throw new System.ArgumentNullException("BodyCell");
        }

        if (FooterCell == null)
        {
            throw new System.ArgumentNullException("FooterCell");
        }

        PdfTableStyle copied = new PdfTableStyle();
        copied.Table = Table.Copy();
        copied.HeaderCell = HeaderCell.Copy();
        copied.BodyCell = BodyCell.Copy();
        copied.FooterCell = FooterCell.Copy();
        return copied;
    }
}
