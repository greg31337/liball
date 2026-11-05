namespace pdf;

internal class ReportLayout
{
    internal const double MarginLeft = 64;
    internal const double MarginRight = 64;
    internal const double HeaderY = 756;
    internal const double FooterY = 34;

    internal double ContentLeft;
    internal double ContentRight;
    internal double ContentWidth;

    internal ReportLayout()
    {
        ContentLeft = MarginLeft;
        ContentRight = PdfPageSize.LetterWidth - MarginRight;
        ContentWidth = ContentRight - ContentLeft;
    }

    internal void DrawTitle(PdfPage page, string title, string subtitle)
    {
        page.Canvas.DrawTextWithFont(title, ContentLeft, 690, 28, PdfFont.Bold);
        page.Canvas.SetStrokeColor(0.20, 0.33, 0.42);
        page.Canvas.SetLineWidth(1.5);
        page.Canvas.DrawLine(ContentLeft, 672, ContentRight, 672);
        page.Canvas.SetStrokeColor(0, 0, 0);
        page.Canvas.DrawText(subtitle, ContentLeft, 646, 11);
    }

    internal double DrawSection(PdfPage page, string heading, string paragraph, double y)
    {
        page.Canvas.DrawTextWithFont(heading, ContentLeft, y, 16, PdfFont.Bold);
        y -= 24;

        return DrawWrappedText(page, paragraph, ContentLeft, y, ContentWidth, 11, 16);
    }

    internal double DrawWrappedText(
        PdfPage page,
        string text,
        double x,
        double y,
        double maxWidth,
        double fontSize,
        double lineHeight)
    {
        string[] lines = PdfTextWrapper.WrapText(text, maxWidth, fontSize);

        foreach (string line in lines)
        {
            page.Canvas.DrawText(line, x, y, fontSize);
            y -= lineHeight;
        }

        return y;
    }

    internal void DrawImagePanel(PdfPage page, PdfImage image, double x, double y, double width, double height, string caption)
    {
        page.Canvas.SetStrokeColor(0.75, 0.78, 0.80);
        page.Canvas.SetLineWidth(0.8);
        page.Canvas.StrokeRectangle(x, y - 24, width, height + 46);
        page.Canvas.DrawImage(image, x + 16, y, width - 32, height);
        page.Canvas.SetFillColor(0.24, 0.29, 0.33);
        page.Canvas.DrawText(caption, x + 16, y - 15, 9);
        page.Canvas.SetFillColor(0, 0, 0);
    }

    internal double DrawTable(PdfPage page, string[] headers, string[][] rows, double x, double y, double[] columnWidths)
    {
        const double rowHeight = 24;
        double tableWidth = SumArray(columnWidths);

        page.Canvas.SetFillColor(0.89, 0.93, 0.94);
        page.Canvas.FillRectangle(x, y - rowHeight, tableWidth, rowHeight);
        page.Canvas.SetFillColor(0, 0, 0);

        double currentX = x;
        for (int column = 0; column < headers.Length; column++)
        {
            page.Canvas.DrawTextWithFont(headers[column], currentX + 8, y - 16, 10, PdfFont.Bold);
            currentX += columnWidths[column];
        }

        double currentY = y - rowHeight;
        for (int row = 0; row < rows.Length; row++)
        {
            if (row % 2 == 1)
            {
                page.Canvas.SetFillColor(0.97, 0.98, 0.98);
                page.Canvas.FillRectangle(x, currentY - rowHeight, tableWidth, rowHeight);
                page.Canvas.SetFillColor(0, 0, 0);
            }

            currentX = x;
            for (int column = 0; column < rows[row].Length; column++)
            {
                page.Canvas.DrawText(rows[row][column], currentX + 8, currentY - 16, 9.5);
                currentX += columnWidths[column];
            }

            currentY -= rowHeight;
        }

        page.Canvas.SetStrokeColor(0.68, 0.72, 0.74);
        page.Canvas.SetLineWidth(0.6);
        DrawTableGrid(page, x, y, columnWidths, rowHeight, rows.Length + 1);
        page.Canvas.SetStrokeColor(0, 0, 0);
        return currentY;
    }

    internal void DrawHeaderFooter(PdfDocument document, string header, string footer)
    {
        for (int index = 0; index < document.Pages.Count; index++)
        {
            PdfPage page = document.Pages[index];
            PdfCanvas canvas = page.Canvas;
            int pageNumber = index + 1;

            canvas.SetStrokeColor(0.78, 0.81, 0.83);
            canvas.SetLineWidth(0.6);
            canvas.DrawLine(ContentLeft, HeaderY - 10, ContentRight, HeaderY - 10);
            canvas.DrawLine(ContentLeft, FooterY + 15, ContentRight, FooterY + 15);

            canvas.SetFillColor(0.22, 0.27, 0.30);
            canvas.DrawTextWithFont(header, ContentLeft, HeaderY, 9, PdfFont.Bold);
            canvas.DrawText(footer, ContentLeft, FooterY, 8);
            canvas.DrawText("Page " + pageNumber.ToString() + " of " + document.Pages.Count.ToString(), ContentRight - 58, FooterY, 8);
            canvas.SetFillColor(0, 0, 0);
            canvas.SetStrokeColor(0, 0, 0);
        }
    }

    internal static void DrawTableGrid(PdfPage page, double x, double y, double[] columnWidths, double rowHeight, int rowCount)
    {
        double tableWidth = SumArray(columnWidths);
        double tableHeight = rowHeight * rowCount;

        for (int row = 0; row <= rowCount; row++)
        {
            double rowY = y - row * rowHeight;
            page.Canvas.DrawLine(x, rowY, x + tableWidth, rowY);
        }

        double currentX = x;
        page.Canvas.DrawLine(currentX, y, currentX, y - tableHeight);

        foreach (double width in columnWidths)
        {
            currentX += width;
            page.Canvas.DrawLine(currentX, y, currentX, y - tableHeight);
        }
    }

    internal static double SumArray(double[] values)
    {
        double total = 0;

        foreach (double value in values)
        {
            total += value;
        }

        return total;
    }
}
