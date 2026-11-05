namespace pptx;

internal static class PptxReportGeneratorCore
{
    internal const long Margin = 685800L;
    internal const long TitleHeight = 914400L;
    internal const long SubtitleTop = 2057400L;
    internal const long SubtitleHeight = 685800L;
    internal const long SectionBodyTop = 1828800L;

    internal static PptxDocument Generate(PptxReportData data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (data.Sections == null)
        {
            throw new System.ArgumentException("Report sections are required.", "data");
        }

        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PopulateTitleSlide(presentation.GetSlide(0), presentation, CleanText(data.Title), CleanText(data.Subtitle));

        for (int index = 0; index < data.Sections.Count; index++)
        {
            PptxReportSection section = data.Sections[index];
            if (section == null)
            {
                throw new System.ArgumentException("Report sections cannot contain null values.", "data");
            }

            PopulateSectionSlide(presentation.AddSlide(), presentation, section);
        }

        return document;
    }

    internal static void PopulateTitleSlide(PptxSlide slide, PptxPresentation presentation, string title, string subtitle)
    {
        long width = ContentWidth(presentation);
        PptxShape titleShape = slide.AddTextBox(title, new PptxBounds(Margin, Margin, width, TitleHeight));
        FormatShapeText(titleShape, 32.0, true);
        if (subtitle.Length > 0)
        {
            PptxShape subtitleShape = slide.AddTextBox(subtitle, new PptxBounds(Margin, SubtitleTop, width, SubtitleHeight));
            FormatShapeText(subtitleShape, 20.0, false);
        }
    }

    internal static void PopulateSectionSlide(PptxSlide slide, PptxPresentation presentation, PptxReportSection section)
    {
        if (section.Lines == null)
        {
            throw new System.ArgumentException("Report section lines are required.", "section");
        }

        long width = ContentWidth(presentation);
        PptxShape titleShape = slide.AddTextBox(CleanText(section.Title), new PptxBounds(Margin, Margin, width, TitleHeight));
        FormatShapeText(titleShape, 28.0, true);
        if (section.Lines.Count == 0)
        {
            return;
        }

        long bodyHeight = presentation.SlideHeight - SectionBodyTop - Margin;
        if (bodyHeight < TitleHeight)
        {
            bodyHeight = TitleHeight;
        }

        PptxShape bodyShape = slide.AddTextBox(CleanText(section.Lines[0]), new PptxBounds(Margin, SectionBodyTop, width, bodyHeight));
        PptxTextFrame textFrame = bodyShape.GetTextFrame();
        for (int index = 1; index < section.Lines.Count; index++)
        {
            textFrame.AddParagraph(CleanText(section.Lines[index]));
        }

        FormatShapeText(bodyShape, 20.0, false);
    }

    internal static void FormatShapeText(PptxShape shape, double sizePoints, bool bold)
    {
        PptxTextFrame frame = shape.GetTextFrame();
        for (int paragraphIndex = 0; paragraphIndex < frame.Paragraphs.Count; paragraphIndex++)
        {
            PptxParagraph paragraph = frame.Paragraphs[paragraphIndex];
            for (int runIndex = 0; runIndex < paragraph.Runs.Count; runIndex++)
            {
                PptxRun run = paragraph.Runs[runIndex];
                PptxFont font = PptxRunFormatCore.CloneFont(run.Font);
                font.SizePoints = sizePoints;
                font.Bold = bold;
                font.Italic = false;
                PptxRunFormatCore.SetFont(run, font);
            }
        }
    }

    internal static long ContentWidth(PptxPresentation presentation)
    {
        long width = presentation.SlideWidth - Margin - Margin;
        if (width < TitleHeight)
        {
            return TitleHeight;
        }

        return width;
    }

    internal static string CleanText(string value)
    {
        if (value == null)
        {
            return "";
        }

        return PptxDiagnostic.CleanText(value);
    }
}
