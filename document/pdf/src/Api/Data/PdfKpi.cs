using System;

namespace pdf;

/// <summary>
/// Represents a KPI used by the PDF generation API.
/// </summary>
public class PdfKpi
{
    /// <summary>
    /// Stores the label used by this object.
    /// </summary>
    public string Label = string.Empty;
    /// <summary>
    /// Stores the value used by this object.
    /// </summary>
    public string Value = string.Empty;

    /// <summary>
    /// Initializes a new KPI.
    /// </summary>
    /// <param name="label">The label to use for this operation.</param>
    /// <param name="value">The value to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfKpi(string label, string value)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        Label = label;
        Value = value;
    }

    internal PdfTableBlock CreateBlock(double width, PdfParagraphStyle labelStyle, PdfParagraphStyle valueStyle)
    {
        if (labelStyle == null)
        {
            throw new ArgumentNullException("labelStyle");
        }

        if (valueStyle == null)
        {
            throw new ArgumentNullException("valueStyle");
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "KPI width must be positive.");
        }

        PdfTableBlock table = new PdfTableBlock(new double[] { width });
        PdfTableRow valueRow = table.AddRow();
        PdfTableRow labelRow = table.AddRow();
        PdfTableCell valueCell = valueRow.AddTextBlock(new PdfTextBlock(Value, valueStyle));
        PdfTableCell labelCell = labelRow.AddTextBlock(new PdfTextBlock(Label, labelStyle));

        valueCell.SetPadding(new PdfInsets(8, 8, 8, 2));
        valueCell.SetBackground(PdfColor.Gray(0.94));
        labelCell.SetPadding(new PdfInsets(8, 2, 8, 8));
        labelCell.SetBackground(PdfColor.Gray(0.94));
        return table;
    }
}
