using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents a label sheet used by the PDF generation API.
/// </summary>
public class PdfLabelSheet : IPdfLabelSheet
{
    /// <summary>
    /// Stores the numeric rows used by this object.
    /// </summary>
    internal int Rows;
    /// <summary>
    /// Stores the numeric columns used by this object.
    /// </summary>
    internal int Columns;
    internal List<string> _labels = new List<string>();
    /// <summary>
    /// Exposes the labels currently stored by this object.
    /// </summary>
    internal IReadOnlyList<string> Labels = new List<string>();
    internal bool Built;
    /// <summary>
    /// Stores the document used by this object.
    /// </summary>
    internal PdfDocument Document = new PdfDocument();

    /// <summary>
    /// Initializes a new label sheet.
    /// </summary>
    /// <param name="rows">The rows to use for this operation.</param>
    /// <param name="columns">The columns to use for this operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public PdfLabelSheet(int rows, int columns)
    {
        if (rows <= 0)
        {
            throw new ArgumentOutOfRangeException("rows", "Label sheet rows must be positive.");
        }

        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException("columns", "Label sheet columns must be positive.");
        }

        Rows = rows;
        Columns = columns;
        _labels = new List<string>();
        Labels = _labels;
        Built = false;
        Document = new PdfDocument();
    }

    /// <summary>
    /// Adds a label to this label sheet.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the object is not in a state that permits this operation.</exception>
    public void AddLabel(string text)
    {
        EnsureNotBuilt();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        long capacity = (long)Rows * Columns;
        if (_labels.Count >= capacity)
        {
            throw new InvalidOperationException("Label sheet is full.");
        }

        _labels.Add(text);
    }

    /// <summary>
    /// Builds a label sheet from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            PdfPage page = Document.AddPageWithDimensions(new PdfPageSizeFactory().Letter());
            PdfTextStyle style = PdfTextStyle.WithFont(9, PdfFont.Regular);
            double margin = 36;
            double labelWidth = (page.Width - margin * 2) / Columns;
            double labelHeight = (page.Height - margin * 2) / Rows;

            for (int index = 0; index < _labels.Count; index++)
            {
                int row = index / Columns;
                int column = index % Columns;
                double x = margin + column * labelWidth;
                double y = page.Height - margin - (row + 1) * labelHeight;

                page.Canvas.SetStrokeColorValue(PdfColor.Gray(0.75));
                page.Canvas.SetLineWidth(0.4);
                page.Canvas.StrokeRectangle(x, y, labelWidth, labelHeight);
                page.Canvas.DrawAlignedStyledText(_labels[index], x + 6, y + labelHeight - 18, labelWidth - 12, style, PdfTextAlignment.Left);
            }

            Built = true;
        }

        return Document;
    }

    internal void EnsureNotBuilt()
    {
        if (Built)
        {
            throw new InvalidOperationException("Label sheet has already been built.");
        }
    }
}
