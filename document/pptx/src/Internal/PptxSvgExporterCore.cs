using System.Collections.Generic;
using System.Text;

namespace pptx;

internal static class PptxSvgExporterCore
{
    internal const long DefaultSlideWidth = 9144000L;
    internal const long DefaultSlideHeight = 6858000L;
    internal const long StrokeWidth = 12700L;
    internal const long TextBaselineOffset = 220000L;
    internal const long TextLineHeight = 240000L;
    internal const long PlaceholderInset = 50000L;
    internal static string ExportPresentation(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        long width = NormalizeDimension(presentation.SlideWidth, DefaultSlideWidth);
        long slideHeight = NormalizeDimension(presentation.SlideHeight, DefaultSlideHeight);
        long documentHeight = slideHeight;
        if (presentation.SlideCount() > 0)
        {
            documentHeight = slideHeight * (long)presentation.SlideCount();
        }

        StringBuilder builder = new StringBuilder();
        AppendSvgStart(builder, "pptx-svg-document", width, documentHeight);
        builder.Append(" data-slide-count=\"");
        builder.Append(presentation.SlideCount().ToString());
        builder.Append("\">");
        AppendMetadata(builder);
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            builder.Append("<g class=\"pptx-svg-slide\" data-slide-index=\"");
            builder.Append(index.ToString());
            builder.Append("\" data-part-name=\"");
            builder.Append(EscapeAttribute(presentation.GetSlide(index).PartName));
            builder.Append("\" transform=\"translate(0 ");
            builder.Append((slideHeight * (long)index).ToString());
            builder.Append(")\">");
            AppendSlideBackground(builder, width, slideHeight);
            AppendSlideShapes(builder, presentation.GetSlide(index));
            builder.Append("</g>");
        }

        builder.Append("</svg>");
        return builder.ToString();
    }
    internal static string ExportSlide(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        long width = DefaultSlideWidth;
        long height = DefaultSlideHeight;
        if (slide.Presentation != null)
        {
            width = NormalizeDimension(slide.Presentation.SlideWidth, DefaultSlideWidth);
            height = NormalizeDimension(slide.Presentation.SlideHeight, DefaultSlideHeight);
        }

        StringBuilder builder = new StringBuilder();
        AppendSvgStart(builder, "pptx-svg-slide", width, height);
        builder.Append(" data-slide-index=\"");
        builder.Append(FindSlideIndex(slide).ToString());
        builder.Append("\" data-part-name=\"");
        builder.Append(EscapeAttribute(slide.PartName));
        builder.Append("\">");
        AppendMetadata(builder);
        AppendSlideBackground(builder, width, height);
        AppendSlideShapes(builder, slide);
        builder.Append("</svg>");
        return builder.ToString();
    }
    internal static string GetFidelityLimitations()
    {
        return "Structural SVG export supports common vector geometry and text only; styling, effects, images, tables, charts, media, animations, and unsupported geometry are represented as placeholders.";
    }

    internal static void AppendSvgStart(StringBuilder builder, string className, long width, long height)
    {
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"");
        builder.Append(className);
        builder.Append("\" viewBox=\"0 0 ");
        builder.Append(width.ToString());
        builder.Append(" ");
        builder.Append(height.ToString());
        builder.Append("\" width=\"");
        builder.Append(width.ToString());
        builder.Append("\" height=\"");
        builder.Append(height.ToString());
        builder.Append("\" role=\"img\"");
    }

    internal static void AppendMetadata(StringBuilder builder)
    {
        builder.Append("<title>PowerPoint structural SVG export</title><desc>");
        AppendEscapedText(builder, GetFidelityLimitations());
        builder.Append("</desc>");
    }

    internal static void AppendSlideBackground(StringBuilder builder, long width, long height)
    {
        builder.Append("<rect class=\"pptx-svg-slide-background\" x=\"0\" y=\"0\" width=\"");
        builder.Append(width.ToString());
        builder.Append("\" height=\"");
        builder.Append(height.ToString());
        builder.Append("\" fill=\"#ffffff\"/>");
    }

    internal static void AppendSlideShapes(StringBuilder builder, PptxSlide slide)
    {
        List<PptxShape> shapes = slide.GetShapeTree().GetAllShapes();
        for (int index = 0; index < shapes.Count; index++)
        {
            AppendShape(builder, shapes[index]);
        }
    }

    internal static void AppendShape(StringBuilder builder, PptxShape shape)
    {
        if (shape.IsTable())
        {
            AppendPlaceholder(builder, shape, "table", "Table placeholder");
            return;
        }

        if (shape.IsChart())
        {
            AppendPlaceholder(builder, shape, "chart", "Chart placeholder");
            return;
        }

        if (shape.IsPicture())
        {
            AppendPlaceholder(builder, shape, "image", "Image placeholder");
            return;
        }

        if (shape.IsConnector())
        {
            AppendConnector(builder, shape);
            return;
        }

        string geometry = PptxShapeTypeCore.NormalizePresetGeometry(shape.PresetGeometry);
        if (IsSupportedGeometry(geometry))
        {
            AppendGeometry(builder, shape, geometry);
            AppendShapeText(builder, shape);
            return;
        }

        AppendPlaceholder(builder, shape, "shape", "Shape placeholder");
        AppendShapeText(builder, shape);
    }

    internal static bool IsSupportedGeometry(string geometry)
    {
        string clean = PptxShapeTypeCore.NormalizePresetGeometry(geometry);
        if (clean == PptxShapeType.Rectangle)
        {
            return true;
        }

        if (clean == PptxShapeType.RoundRectangle)
        {
            return true;
        }

        if (clean == PptxShapeType.Ellipse)
        {
            return true;
        }

        if (clean == PptxShapeType.Triangle)
        {
            return true;
        }

        if (clean == PptxShapeType.RightTriangle)
        {
            return true;
        }

        if (clean == PptxShapeType.Diamond)
        {
            return true;
        }

        return false;
    }

    internal static void AppendGeometry(StringBuilder builder, PptxShape shape, string geometry)
    {
        if (geometry == PptxShapeType.Rectangle)
        {
            AppendRect(builder, shape, "pptx-svg-rect", 0L);
            return;
        }

        if (geometry == PptxShapeType.RoundRectangle)
        {
            PptxBounds bounds = shape.GetEffectiveBounds();
            AppendRect(builder, shape, "pptx-svg-round-rect", Minimum(bounds.Width, bounds.Height) / 10L);
            return;
        }

        if (geometry == PptxShapeType.Ellipse)
        {
            AppendEllipse(builder, shape);
            return;
        }

        if (geometry == PptxShapeType.Triangle)
        {
            AppendTriangle(builder, shape);
            return;
        }

        if (geometry == PptxShapeType.RightTriangle)
        {
            AppendRightTriangle(builder, shape);
            return;
        }

        if (geometry == PptxShapeType.Diamond)
        {
            AppendDiamond(builder, shape);
        }
    }

    internal static void AppendRect(StringBuilder builder, PptxShape shape, string geometryClass, long radius)
    {
        PptxBounds bounds = shape.GetEffectiveBounds();
        builder.Append("<rect class=\"pptx-svg-shape ");
        builder.Append(geometryClass);
        builder.Append("\"");
        AppendShapeAttributes(builder, shape, bounds);
        if (radius > 0L)
        {
            builder.Append(" rx=\"");
            builder.Append(radius.ToString());
            builder.Append("\" ry=\"");
            builder.Append(radius.ToString());
            builder.Append("\"");
        }

        AppendVectorStyle(builder);
        builder.Append("/>");
    }

    internal static void AppendEllipse(StringBuilder builder, PptxShape shape)
    {
        PptxBounds bounds = shape.GetEffectiveBounds();
        builder.Append("<ellipse class=\"pptx-svg-shape pptx-svg-ellipse\" cx=\"");
        builder.Append((bounds.X + bounds.Width / 2L).ToString());
        builder.Append("\" cy=\"");
        builder.Append((bounds.Y + bounds.Height / 2L).ToString());
        builder.Append("\" rx=\"");
        builder.Append((bounds.Width / 2L).ToString());
        builder.Append("\" ry=\"");
        builder.Append((bounds.Height / 2L).ToString());
        builder.Append("\"");
        AppendShapeMetadata(builder, shape);
        AppendVectorStyle(builder);
        builder.Append("/>");
    }

    internal static void AppendTriangle(StringBuilder builder, PptxShape shape)
    {
        PptxBounds bounds = shape.GetEffectiveBounds();
        builder.Append("<polygon class=\"pptx-svg-shape pptx-svg-triangle\"");
        AppendShapeMetadata(builder, shape);
        builder.Append(" points=\"");
        AppendPoint(builder, bounds.X + bounds.Width / 2L, bounds.Y);
        builder.Append(" ");
        AppendPoint(builder, bounds.X + bounds.Width, bounds.Y + bounds.Height);
        builder.Append(" ");
        AppendPoint(builder, bounds.X, bounds.Y + bounds.Height);
        builder.Append("\"");
        AppendVectorStyle(builder);
        builder.Append("/>");
    }

    internal static void AppendRightTriangle(StringBuilder builder, PptxShape shape)
    {
        PptxBounds bounds = shape.GetEffectiveBounds();
        builder.Append("<polygon class=\"pptx-svg-shape pptx-svg-right-triangle\"");
        AppendShapeMetadata(builder, shape);
        builder.Append(" points=\"");
        AppendPoint(builder, bounds.X, bounds.Y);
        builder.Append(" ");
        AppendPoint(builder, bounds.X + bounds.Width, bounds.Y + bounds.Height);
        builder.Append(" ");
        AppendPoint(builder, bounds.X, bounds.Y + bounds.Height);
        builder.Append("\"");
        AppendVectorStyle(builder);
        builder.Append("/>");
    }

    internal static void AppendDiamond(StringBuilder builder, PptxShape shape)
    {
        PptxBounds bounds = shape.GetEffectiveBounds();
        builder.Append("<polygon class=\"pptx-svg-shape pptx-svg-diamond\"");
        AppendShapeMetadata(builder, shape);
        builder.Append(" points=\"");
        AppendPoint(builder, bounds.X + bounds.Width / 2L, bounds.Y);
        builder.Append(" ");
        AppendPoint(builder, bounds.X + bounds.Width, bounds.Y + bounds.Height / 2L);
        builder.Append(" ");
        AppendPoint(builder, bounds.X + bounds.Width / 2L, bounds.Y + bounds.Height);
        builder.Append(" ");
        AppendPoint(builder, bounds.X, bounds.Y + bounds.Height / 2L);
        builder.Append("\"");
        AppendVectorStyle(builder);
        builder.Append("/>");
    }

    internal static void AppendConnector(StringBuilder builder, PptxShape shape)
    {
        builder.Append("<line class=\"pptx-svg-shape pptx-svg-connector\" x1=\"");
        builder.Append(shape.Start.X.ToString());
        builder.Append("\" y1=\"");
        builder.Append(shape.Start.Y.ToString());
        builder.Append("\" x2=\"");
        builder.Append(shape.End.X.ToString());
        builder.Append("\" y2=\"");
        builder.Append(shape.End.Y.ToString());
        builder.Append("\"");
        AppendShapeMetadata(builder, shape);
        AppendVectorStyle(builder);
        builder.Append("/>");
    }

    internal static void AppendShapeText(StringBuilder builder, PptxShape shape)
    {
        PptxTextFrame frame = shape.GetTextFrame();
        if (!frame.Present)
        {
            return;
        }

        if (!frame.HasText())
        {
            return;
        }

        PptxBounds bounds = shape.GetEffectiveBounds();
        long y = bounds.Y + TextBaselineOffset;
        for (int index = 0; index < frame.Paragraphs.Count; index++)
        {
            string text = frame.Paragraphs[index].GetText();
            if (text.Length == 0)
            {
                continue;
            }

            builder.Append("<text class=\"pptx-svg-text\" x=\"");
            builder.Append((bounds.X + PlaceholderInset).ToString());
            builder.Append("\" y=\"");
            builder.Append(y.ToString());
            builder.Append("\" data-shape-id=\"");
            builder.Append(shape.Id.ToString());
            builder.Append("\">");
            AppendEscapedSingleLineText(builder, text);
            builder.Append("</text>");
            y += TextLineHeight;
        }
    }

    internal static void AppendPlaceholder(StringBuilder builder, PptxShape shape, string kind, string label)
    {
        PptxBounds bounds = shape.GetEffectiveBounds();
        builder.Append("<g class=\"pptx-svg-placeholder pptx-svg-");
        builder.Append(kind);
        builder.Append("-placeholder\"");
        AppendShapeMetadata(builder, shape);
        builder.Append(">");
        builder.Append("<rect x=\"");
        builder.Append(bounds.X.ToString());
        builder.Append("\" y=\"");
        builder.Append(bounds.Y.ToString());
        builder.Append("\" width=\"");
        builder.Append(bounds.Width.ToString());
        builder.Append("\" height=\"");
        builder.Append(bounds.Height.ToString());
        builder.Append("\" fill=\"none\" stroke=\"#6b7280\" stroke-width=\"");
        builder.Append(StrokeWidth.ToString());
        builder.Append("\" stroke-dasharray=\"");
        builder.Append((StrokeWidth * 4L).ToString());
        builder.Append(" ");
        builder.Append((StrokeWidth * 2L).ToString());
        builder.Append("\"/>");
        builder.Append("<text class=\"pptx-svg-placeholder-label\" x=\"");
        builder.Append((bounds.X + PlaceholderInset).ToString());
        builder.Append("\" y=\"");
        builder.Append((bounds.Y + TextBaselineOffset).ToString());
        builder.Append("\">");
        AppendEscapedText(builder, label);
        builder.Append("</text></g>");
    }

    internal static void AppendShapeAttributes(StringBuilder builder, PptxShape shape, PptxBounds bounds)
    {
        builder.Append(" x=\"");
        builder.Append(bounds.X.ToString());
        builder.Append("\" y=\"");
        builder.Append(bounds.Y.ToString());
        builder.Append("\" width=\"");
        builder.Append(bounds.Width.ToString());
        builder.Append("\" height=\"");
        builder.Append(bounds.Height.ToString());
        builder.Append("\"");
        AppendShapeMetadata(builder, shape);
    }

    internal static void AppendShapeMetadata(StringBuilder builder, PptxShape shape)
    {
        builder.Append(" data-shape-id=\"");
        builder.Append(shape.Id.ToString());
        builder.Append("\" data-name=\"");
        builder.Append(EscapeAttribute(shape.Name));
        builder.Append("\" data-element-type=\"");
        builder.Append(EscapeAttribute(shape.ElementType));
        builder.Append("\" data-preset-geometry=\"");
        builder.Append(EscapeAttribute(shape.PresetGeometry));
        builder.Append("\"");
    }

    internal static void AppendVectorStyle(StringBuilder builder)
    {
        builder.Append(" fill=\"none\" stroke=\"#111827\" stroke-width=\"");
        builder.Append(StrokeWidth.ToString());
        builder.Append("\"");
    }

    internal static void AppendPoint(StringBuilder builder, long x, long y)
    {
        builder.Append(x.ToString());
        builder.Append(",");
        builder.Append(y.ToString());
    }

    internal static string EscapeAttribute(string value)
    {
        StringBuilder builder = new StringBuilder();
        AppendEscapedText(builder, value);
        return builder.ToString();
    }

    internal static void AppendEscapedSingleLineText(StringBuilder builder, string text)
    {
        string clean = PptxDiagnostic.CleanText(text);
        for (int index = 0; index < clean.Length; index++)
        {
            if (clean[index] == '\n')
            {
                builder.Append(" ");
            }
            else if (clean[index] == '\t')
            {
                builder.Append(" ");
            }
            else
            {
                AppendEscapedCharacter(builder, clean[index]);
            }
        }
    }

    internal static void AppendEscapedText(StringBuilder builder, string text)
    {
        string clean = PptxDiagnostic.CleanText(text);
        for (int index = 0; index < clean.Length; index++)
        {
            AppendEscapedCharacter(builder, clean[index]);
        }
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
            builder.Append("&apos;");
            return;
        }

        builder.Append(value);
    }

    internal static long NormalizeDimension(long value, long fallback)
    {
        if (value > 0L)
        {
            return value;
        }

        return fallback;
    }

    internal static long Minimum(long left, long right)
    {
        if (left < right)
        {
            return left;
        }

        return right;
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
