using System.Collections.Generic;
using System.Text;

namespace pptx;

internal static class PptxHtmlExporterCore
{
    internal static string ExportPresentation(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>PowerPoint structural export</title>");
        builder.Append("<meta name=\"pptx-export\" content=\"structural-html\"></head><body>");
        builder.Append("<main class=\"pptx-document\" data-slide-count=\"");
        builder.Append(presentation.SlideCount().ToString());
        builder.Append("\">");
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            AppendSlide(builder, presentation.GetSlide(index), index);
        }

        builder.Append("</main></body></html>");
        return builder.ToString();
    }
    internal static string ExportSlide(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        StringBuilder builder = new StringBuilder();
        AppendSlide(builder, slide, FindSlideIndex(slide));
        return builder.ToString();
    }

    internal static void AppendSlide(StringBuilder builder, PptxSlide slide, int slideIndex)
    {
        builder.Append("<section class=\"pptx-slide\" data-slide-index=\"");
        builder.Append(slideIndex.ToString());
        builder.Append("\" data-part-name=\"");
        builder.Append(EscapeAttribute(slide.PartName));
        builder.Append("\"><h2>Slide ");
        if (slideIndex >= 0)
        {
            builder.Append((slideIndex + 1).ToString());
        }
        else
        {
            builder.Append("?");
        }

        builder.Append("</h2>");
        List<PptxShape> shapes = slide.GetShapeTree().GetAllShapes();
        for (int index = 0; index < shapes.Count; index++)
        {
            AppendShape(builder, shapes[index]);
        }

        builder.Append("</section>");
    }

    internal static void AppendShape(StringBuilder builder, PptxShape shape)
    {
        if (shape.IsTable())
        {
            AppendTable(builder, shape);
            return;
        }

        if (shape.IsChart())
        {
            AppendChartPlaceholder(builder, shape);
            return;
        }

        if (shape.IsPicture())
        {
            AppendImagePlaceholder(builder, shape);
            return;
        }

        PptxTextFrame frame = shape.GetTextFrame();
        if (frame.Present && frame.HasText())
        {
            AppendTextShape(builder, shape, frame);
            return;
        }

        AppendShapePlaceholder(builder, shape);
    }

    internal static void AppendShapeStart(StringBuilder builder, PptxShape shape, string className)
    {
        PptxBounds bounds = shape.GetEffectiveBounds();
        builder.Append("<div class=\"pptx-shape ");
        builder.Append(className);
        builder.Append("\" data-shape-id=\"");
        builder.Append(shape.Id.ToString());
        builder.Append("\" data-name=\"");
        builder.Append(EscapeAttribute(shape.Name));
        builder.Append("\" data-element-type=\"");
        builder.Append(EscapeAttribute(shape.ElementType));
        builder.Append("\" data-preset-geometry=\"");
        builder.Append(EscapeAttribute(shape.PresetGeometry));
        builder.Append("\" data-x=\"");
        builder.Append(bounds.X.ToString());
        builder.Append("\" data-y=\"");
        builder.Append(bounds.Y.ToString());
        builder.Append("\" data-width=\"");
        builder.Append(bounds.Width.ToString());
        builder.Append("\" data-height=\"");
        builder.Append(bounds.Height.ToString());
        builder.Append("\">");
    }

    internal static void AppendTextShape(StringBuilder builder, PptxShape shape, PptxTextFrame frame)
    {
        AppendShapeStart(builder, shape, "pptx-text-shape");
        for (int index = 0; index < frame.Paragraphs.Count; index++)
        {
            string text = frame.Paragraphs[index].GetText();
            if (text.Length == 0)
            {
                continue;
            }

            builder.Append("<p>");
            AppendEscapedTextWithBreaks(builder, text);
            builder.Append("</p>");
        }

        builder.Append("</div>");
    }

    internal static void AppendShapePlaceholder(StringBuilder builder, PptxShape shape)
    {
        AppendShapeStart(builder, shape, "pptx-shape-placeholder");
        builder.Append("<span>Shape placeholder</span></div>");
    }

    internal static void AppendImagePlaceholder(StringBuilder builder, PptxShape shape)
    {
        AppendShapeStart(builder, shape, "pptx-image-placeholder");
        string relationshipId = shape.GetImageRelationshipId();
        PptxRelationship? relationship = shape.Tree.FindRelationshipById(relationshipId);
        builder.Append("<span>Image placeholder</span>");
        builder.Append("<span class=\"pptx-placeholder-meta\" data-relationship-id=\"");
        builder.Append(EscapeAttribute(relationshipId));
        builder.Append("\"");
        if (relationship != null)
        {
            builder.Append(" data-part-name=\"");
            builder.Append(EscapeAttribute(relationship.TargetPartName));
            builder.Append("\" data-target=\"");
            builder.Append(EscapeAttribute(relationship.Target));
            builder.Append("\"");
        }

        builder.Append("></span></div>");
    }

    internal static void AppendChartPlaceholder(StringBuilder builder, PptxShape shape)
    {
        AppendShapeStart(builder, shape, "pptx-chart-placeholder");
        PptxChart chart = shape.AsChart();
        builder.Append("<span>Chart placeholder</span>");
        builder.Append("<span class=\"pptx-placeholder-meta\" data-relationship-id=\"");
        builder.Append(EscapeAttribute(chart.RelationshipId));
        builder.Append("\" data-part-name=\"");
        builder.Append(EscapeAttribute(chart.PartName));
        builder.Append("\"></span></div>");
    }

    internal static void AppendTable(StringBuilder builder, PptxShape shape)
    {
        PptxTable table = shape.AsTable();
        AppendShapeStart(builder, shape, "pptx-table-shape");
        builder.Append("<table>");
        for (int rowIndex = 0; rowIndex < table.RowCount(); rowIndex++)
        {
            builder.Append("<tr>");
            for (int columnIndex = 0; columnIndex < table.GetRow(rowIndex).CellCount(); columnIndex++)
            {
                builder.Append("<td>");
                AppendEscapedTextWithBreaks(builder, table.GetText(rowIndex, columnIndex));
                builder.Append("</td>");
            }

            builder.Append("</tr>");
        }

        builder.Append("</table></div>");
    }

    internal static void AppendEscapedTextWithBreaks(StringBuilder builder, string text)
    {
        string clean = PptxDiagnostic.CleanText(text);
        for (int index = 0; index < clean.Length; index++)
        {
            if (clean[index] == '\n')
            {
                builder.Append("<br>");
            }
            else
            {
                AppendEscapedCharacter(builder, clean[index]);
            }
        }
    }

    internal static string EscapeAttribute(string value)
    {
        StringBuilder builder = new StringBuilder();
        string clean = PptxDiagnostic.CleanText(value);
        for (int index = 0; index < clean.Length; index++)
        {
            AppendEscapedCharacter(builder, clean[index]);
        }

        return builder.ToString();
    }

    internal static void AppendEscapedCharacter(StringBuilder builder, char value)
    {
        if (value == '&')
        {
            builder.Append("&amp;");
            return;
        }

        if (value == '<')
        {
            builder.Append("&lt;");
            return;
        }

        if (value == '>')
        {
            builder.Append("&gt;");
            return;
        }

        if (value == '"')
        {
            builder.Append("&quot;");
            return;
        }

        if (value == '\'')
        {
            builder.Append("&#39;");
            return;
        }

        builder.Append(value);
    }

    internal static int FindSlideIndex(PptxSlide slide)
    {
        if (slide.Presentation == null)
        {
            return -1;
        }

        for (int index = 0; index < slide.Presentation.SlideCount(); index++)
        {
            if (slide.Presentation.GetSlide(index).PartName == slide.PartName)
            {
                return index;
            }
        }

        return -1;
    }
}

