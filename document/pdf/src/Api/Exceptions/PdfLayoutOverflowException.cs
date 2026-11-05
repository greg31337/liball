namespace pdf;

/// <summary>
/// Reports a layout element that cannot be split or placed in an available page or column area.
/// </summary>
public class PdfLayoutOverflowException : System.Exception
{
    /// <summary>
    /// Stores the layout element that could not be placed.
    /// </summary>
    public IPdfLayoutElement Element;
    /// <summary>
    /// Stores the element type name for diagnostics and logging.
    /// </summary>
    public string ElementType;
    /// <summary>
    /// Specifies the measured width required by the element in PDF points.
    /// </summary>
    public double RequiredWidth;
    /// <summary>
    /// Specifies the measured height required by the element in PDF points.
    /// </summary>
    public double RequiredHeight;
    /// <summary>
    /// Specifies the available width in PDF points.
    /// </summary>
    public double AvailableWidth;
    /// <summary>
    /// Specifies the available height in PDF points.
    /// </summary>
    public double AvailableHeight;
    /// <summary>
    /// Specifies the one-based logical page number where placement failed.
    /// </summary>
    public int PageNumber;
    /// <summary>
    /// Specifies the one-based column number where placement failed.
    /// </summary>
    public int ColumnNumber;

    /// <summary>
    /// Creates an exception with the element, measured size, available area, and flow position that caused the failure.
    /// </summary>
    /// <param name="element">The layout element that could not be placed.</param>
    /// <param name="elementType">The element type name used in diagnostics.</param>
    /// <param name="requiredWidth">The measured element width in PDF points.</param>
    /// <param name="requiredHeight">The measured element height in PDF points.</param>
    /// <param name="availableWidth">The available area width in PDF points.</param>
    /// <param name="availableHeight">The available area height in PDF points.</param>
    /// <param name="pageNumber">The one-based logical page number.</param>
    /// <param name="columnNumber">The one-based column number.</param>
    /// <param name="message">The explanation of the placement failure.</param>
    public PdfLayoutOverflowException(IPdfLayoutElement element, string elementType, double requiredWidth, double requiredHeight, double availableWidth, double availableHeight, int pageNumber, int columnNumber, string message) : base(message)
    {
        Element = element;
        ElementType = elementType;
        RequiredWidth = requiredWidth;
        RequiredHeight = requiredHeight;
        AvailableWidth = availableWidth;
        AvailableHeight = availableHeight;
        PageNumber = pageNumber;
        ColumnNumber = columnNumber;
    }
}
