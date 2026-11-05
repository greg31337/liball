namespace pdf;

/// <summary>
/// Groups reusable body, heading, block, and table styles for a document.
/// </summary>
public class PdfDocumentTheme
{
    /// <summary>
    /// Stores the default body paragraph style.
    /// </summary>
    public PdfParagraphStyle BodyParagraph;
    /// <summary>
    /// Stores the reusable heading paragraph style.
    /// </summary>
    public PdfParagraphStyle HeadingParagraph;
    /// <summary>
    /// Stores the default block visual and layout style.
    /// </summary>
    public PdfElementStyle Block;
    /// <summary>
    /// Stores the default table style.
    /// </summary>
    public PdfTableStyle Table;

    /// <summary>
    /// Initializes a document theme with default styles.
    /// </summary>
    public PdfDocumentTheme()
    {
        BodyParagraph = new PdfParagraphStyle();
        HeadingParagraph = new PdfParagraphStyle();
        Block = new PdfElementStyle();
        Table = new PdfTableStyle();
    }

    internal PdfDocumentTheme Copy()
    {
        if (BodyParagraph == null)
        {
            throw new System.ArgumentNullException("BodyParagraph");
        }

        if (HeadingParagraph == null)
        {
            throw new System.ArgumentNullException("HeadingParagraph");
        }

        if (Block == null)
        {
            throw new System.ArgumentNullException("Block");
        }

        if (Table == null)
        {
            throw new System.ArgumentNullException("Table");
        }

        PdfDocumentTheme copied = new PdfDocumentTheme();
        copied.BodyParagraph = BodyParagraph.Copy();
        copied.HeadingParagraph = HeadingParagraph.Copy();
        copied.Block = Block.Copy();
        copied.Table = Table.Copy();
        return copied;
    }
}
