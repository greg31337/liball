using System;

namespace pdf;

internal class PdfChartPlaceholderContent : IPdfLayoutElement
{
    internal string Label = string.Empty;
    internal double Width;
    internal double Height;
    internal PdfTextStyle TextStyle = new PdfTextStyle();

    internal PdfChartPlaceholderContent(string label, double width, double height)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        ValidateDimension(width, "width");
        ValidateDimension(height, "height");
        Label = label;
        Width = width;
        Height = height;
        TextStyle = new PdfTextStyle();
    }

    internal void SetTextStyle(PdfTextStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        TextStyle = style.Copy();
    }

    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        PdfBlock.ValidateContext(context);
        PdfLayoutSize size = new PdfLayoutSize(Width, Height);
        return new PdfLayoutResult(size, PdfBox.FromCoordinates(0, 0, Width, Height), PdfBlock.SizeFits(size, context));
    }

    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        PdfBlock.ValidateDrawContext(context);
        PdfLayoutResult result = Measure(context.ToLayoutContext());
        PdfCanvas canvas = context.Page.Canvas;
        canvas.SaveState();
        canvas.SetStrokeColorValue(PdfColor.Gray(0.5));
        canvas.SetLineWidth(0.75);
        canvas.StrokeRectangle(context.X, context.Y, Width, Height);
        canvas.DrawLine(context.X, context.Y, context.X + Width, context.Y + Height);
        canvas.DrawLine(context.X, context.Y + Height, context.X + Width, context.Y);
        if (Label.Length > 0)
        {
            double textWidth = PdfCanvas.MeasureText(Label, TextStyle);
            double textX = context.X + (Width - textWidth) / 2;
            double textY = context.Y + (Height - TextStyle.FontSize) / 2;
            canvas.DrawStyledText(Label, textX, textY, TextStyle);
        }
        canvas.RestoreState();
        return result;
    }

    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        if (cursor == null)
        {
            throw new ArgumentNullException("cursor");
        }

        return cursor.DrawItem(this);
    }

    internal static void ValidateDimension(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, "Chart placeholder dimensions must be finite and positive.");
        }
    }
}
