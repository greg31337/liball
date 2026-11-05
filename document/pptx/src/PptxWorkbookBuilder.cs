using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace pptx;

internal static class PptxWorkbookBuilder
{
    internal static byte[] CreateWorkbookBytes(PptxChartData data, string chartType)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                AddEntry(archive, "[Content_Types].xml", ContentTypesXml());
                AddEntry(archive, "_rels/.rels", RootRelationshipsXml());
                AddEntry(archive, "xl/workbook.xml", WorkbookXml());
                AddEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml());
                AddEntry(archive, "xl/worksheets/sheet1.xml", WorksheetXml(data, chartType));
            }

            return stream.ToArray();
        }
    }

    internal static void AddEntry(ZipArchive archive, string name, string text)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name);
        using (Stream stream = entry.Open())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    internal static string ContentTypesXml()
    {
        return
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "</Types>";
    }

    internal static string RootRelationshipsXml()
    {
        return
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";
    }

    internal static string WorkbookXml()
    {
        return
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
            "</workbook>";
    }

    internal static string WorkbookRelationshipsXml()
    {
        return
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "</Relationships>";
    }

    internal static string WorksheetXml(PptxChartData data, string chartType)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        string normalized = PptxChartTypeCore.Normalize(chartType);
        AppendHeaderRow(builder, data, normalized);
        int rowCount = GetWorkbookRowCount(data, normalized);
        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            AppendDataRow(builder, data, normalized, rowIndex);
        }

        builder.Append("</sheetData></worksheet>");
        return builder.ToString();
    }

    internal static void AppendHeaderRow(StringBuilder builder, PptxChartData data, string chartType)
    {
        builder.Append("<row r=\"1\">");
        if (chartType == PptxChartType.Scatter || chartType == PptxChartType.Bubble)
        {
            for (int index = 0; index < data.Series.Count; index++)
            {
                PptxChartSeries series = data.Series[index];
                AppendInlineStringCell(builder, PptxChartBuilder.ColumnName(PptxChartBuilder.CoordinateXColumn(chartType, index)) + "1", series.Name + " X");
                AppendInlineStringCell(builder, PptxChartBuilder.ColumnName(PptxChartBuilder.CoordinateYColumn(chartType, index)) + "1", series.Name);
                if (chartType == PptxChartType.Bubble)
                {
                    AppendInlineStringCell(builder, PptxChartBuilder.ColumnName(PptxChartBuilder.CoordinateSizeColumn(index)) + "1", series.Name + " Size");
                }
            }

            builder.Append("</row>");
            return;
        }

        AppendInlineStringCell(builder, "A1", "Category");
        for (int index = 0; index < data.Series.Count; index++)
        {
            AppendInlineStringCell(builder, PptxChartBuilder.ColumnName(index + 2) + "1", data.Series[index].Name);
        }

        builder.Append("</row>");
    }

    internal static void AppendDataRow(StringBuilder builder, PptxChartData data, string chartType, int rowIndex)
    {
        int rowNumber = rowIndex + 2;
        builder.Append("<row r=\"");
        builder.Append(rowNumber.ToString());
        builder.Append("\">");
        if (chartType == PptxChartType.Scatter || chartType == PptxChartType.Bubble)
        {
            AppendCoordinateDataRow(builder, data, chartType, rowIndex, rowNumber);
            builder.Append("</row>");
            return;
        }

        string category = "";
        if (rowIndex < data.Categories.Count)
        {
            category = data.Categories[rowIndex];
        }

        if (data.CategoryKind == PptxChartCategoryKind.Numeric)
        {
            AppendValueCell(builder, "A" + rowNumber.ToString(), category);
        }
        else
        {
            AppendInlineStringCell(builder, "A" + rowNumber.ToString(), category);
        }
        for (int seriesIndex = 0; seriesIndex < data.Series.Count; seriesIndex++)
        {
            PptxChartSeries series = data.Series[seriesIndex];
            string value = "";
            if (rowIndex < series.Values.Count)
            {
                value = series.Values[rowIndex];
            }

            AppendValueCell(builder, PptxChartBuilder.ColumnName(seriesIndex + 2) + rowNumber.ToString(), value);
        }

        builder.Append("</row>");
    }

    internal static void AppendCoordinateDataRow(StringBuilder builder, PptxChartData data, string chartType, int rowIndex, int rowNumber)
    {
        for (int seriesIndex = 0; seriesIndex < data.Series.Count; seriesIndex++)
        {
            PptxChartSeries series = data.Series[seriesIndex];
            List<string> xValues = PptxChartBuilder.GetXValues(data, series);
            List<string> yValues = PptxChartBuilder.GetYValues(series);
            string x = GetPointValue(xValues, rowIndex);
            string y = GetPointValue(yValues, rowIndex);
            AppendValueCell(builder, PptxChartBuilder.ColumnName(PptxChartBuilder.CoordinateXColumn(chartType, seriesIndex)) + rowNumber.ToString(), x);
            AppendValueCell(builder, PptxChartBuilder.ColumnName(PptxChartBuilder.CoordinateYColumn(chartType, seriesIndex)) + rowNumber.ToString(), y);
            if (chartType == PptxChartType.Bubble)
            {
                string size = "1";
                if (series.BubbleSizes.Count > 0)
                {
                    size = GetPointValue(series.BubbleSizes, rowIndex);
                }

                AppendValueCell(builder, PptxChartBuilder.ColumnName(PptxChartBuilder.CoordinateSizeColumn(seriesIndex)) + rowNumber.ToString(), size);
            }
        }
    }

    internal static string GetPointValue(List<string> values, int index)
    {
        if (index >= 0 && index < values.Count)
        {
            return values[index];
        }

        return "";
    }

    internal static int GetWorkbookRowCount(PptxChartData data, string chartType)
    {
        int count = data.GetTableRowCount();
        if (chartType == PptxChartType.Scatter || chartType == PptxChartType.Bubble)
        {
            for (int index = 0; index < data.Series.Count; index++)
            {
                PptxChartSeries series = data.Series[index];
                if (series.XValues.Count > count)
                {
                    count = series.XValues.Count;
                }

                if (series.YValues.Count > count)
                {
                    count = series.YValues.Count;
                }

                if (series.BubbleSizes.Count > count)
                {
                    count = series.BubbleSizes.Count;
                }
            }
        }

        return count;
    }

    internal static void AppendInlineStringCell(StringBuilder builder, string reference, string value)
    {
        builder.Append("<c r=\"");
        builder.Append(reference);
        builder.Append("\" t=\"inlineStr\"><is><t>");
        builder.Append(EscapeXml(value));
        builder.Append("</t></is></c>");
    }

    internal static void AppendValueCell(StringBuilder builder, string reference, string value)
    {
        if (IsNumeric(value))
        {
            builder.Append("<c r=\"");
            builder.Append(reference);
            builder.Append("\"><v>");
            builder.Append(EscapeXml(value));
            builder.Append("</v></c>");
            return;
        }

        AppendInlineStringCell(builder, reference, value);
    }

    internal static bool IsNumeric(string value)
    {
        string clean = PptxDiagnostic.CleanText(value).Trim();
        if (clean.Length == 0)
        {
            return false;
        }

        double result = 0.0;
        if (!double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return false;
        }

        return !double.IsNaN(result) && !double.IsInfinity(result);
    }

    internal static string EscapeXml(string value)
    {
        string clean = PptxDiagnostic.CleanText(value);
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < clean.Length; index++)
        {
            char current = clean[index];
            if (current == '&')
            {
                builder.Append("&amp;");
            }
            else if (current == '<')
            {
                builder.Append("&lt;");
            }
            else if (current == '>')
            {
                builder.Append("&gt;");
            }
            else if (current == '"')
            {
                builder.Append("&quot;");
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}
