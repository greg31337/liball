using System.Collections.Generic;
using System.Text;

namespace pptx;

internal static class PptxTextOutlineExporterCore
{
    internal static string ExportPresentationText(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        return presentation.ExtractText();
    }
    internal static string ExportSlideText(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        return slide.ExtractText();
    }
    internal static string ExportPresentationOutline(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            if (index > 0)
            {
                builder.Append("\n\n");
            }

            builder.Append("Slide ");
            builder.Append((index + 1).ToString());
            List<PptxOutlineEntry> entries = GetSlideOutlineEntries(presentation.GetSlide(index), index);
            if (entries.Count > 0)
            {
                builder.Append("\n");
                AppendOutlineEntries(builder, entries);
            }
        }

        return builder.ToString();
    }
    internal static string ExportSlideOutline(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        List<PptxOutlineEntry> entries = GetSlideOutlineEntries(slide, FindSlideIndex(slide));
        StringBuilder builder = new StringBuilder();
        AppendOutlineEntries(builder, entries);
        return builder.ToString();
    }
    internal static List<PptxOutlineEntry> GetPresentationOutlineEntries(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        List<PptxOutlineEntry> entries = new List<PptxOutlineEntry>();
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            AddEntries(entries, GetSlideOutlineEntries(presentation.GetSlide(index), index));
        }

        return entries;
    }
    internal static List<PptxOutlineEntry> GetSlideOutlineEntries(PptxSlide slide)
    {
        return GetSlideOutlineEntries(slide, FindSlideIndex(slide));
    }
    internal static List<PptxOutlineEntry> GetSlideOutlineEntries(PptxSlide slide, int slideIndex)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        List<PptxOutlineEntry> entries = new List<PptxOutlineEntry>();
        List<PptxShape> shapes = slide.GetShapeTree().GetAllShapes();
        for (int shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
        {
            AddShapeEntries(entries, slide, slideIndex, shapes[shapeIndex]);
        }

        return entries;
    }

    internal static void AddShapeEntries(List<PptxOutlineEntry> entries, PptxSlide slide, int slideIndex, PptxShape shape)
    {
        PptxTextFrame frame = shape.GetTextFrame();
        if (!frame.Present)
        {
            return;
        }

        for (int index = 0; index < frame.Paragraphs.Count; index++)
        {
            PptxParagraph paragraph = frame.Paragraphs[index];
            string text = paragraph.GetText();
            if (text.Length == 0)
            {
                continue;
            }

            PptxOutlineEntry entry = new PptxOutlineEntry();
            entry.SlideIndex = slideIndex;
            entry.SlidePartName = slide.PartName;
            entry.ShapeId = shape.Id;
            entry.ShapeName = shape.Name;
            entry.Level = paragraph.Level;
            entry.HasBullet = paragraph.HasBullet;
            entry.BulletType = paragraph.BulletType;
            entry.BulletCharacter = paragraph.BulletCharacter;
            entry.NumberingType = paragraph.NumberingType;
            entry.NumberingStartAt = paragraph.NumberingStartAt;
            entry.Text = text;
            entries.Add(entry);
        }
    }

    internal static void AppendOutlineEntries(StringBuilder builder, List<PptxOutlineEntry> entries)
    {
        for (int index = 0; index < entries.Count; index++)
        {
            if (index > 0)
            {
                builder.Append("\n");
            }

            AppendOutlineEntry(builder, entries[index]);
        }
    }

    internal static void AppendOutlineEntry(StringBuilder builder, PptxOutlineEntry entry)
    {
        for (int index = 0; index < entry.Level; index++)
        {
            builder.Append("  ");
        }

        if (entry.HasBullet)
        {
            builder.Append(GetBulletPrefix(entry));
        }

        builder.Append(entry.Text);
    }
    internal static string GetBulletPrefix(PptxOutlineEntry entry)
    {
        if (entry.BulletType == PptxBullet.Number)
        {
            if (entry.NumberingStartAt > 0)
            {
                return entry.NumberingStartAt.ToString() + ". ";
            }

            return "1. ";
        }

        if (entry.BulletType == PptxBullet.Character && entry.BulletCharacter.Length > 0)
        {
            return entry.BulletCharacter + " ";
        }

        return "- ";
    }

    internal static void AddEntries(List<PptxOutlineEntry> target, List<PptxOutlineEntry> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            target.Add(source[index]);
        }
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

