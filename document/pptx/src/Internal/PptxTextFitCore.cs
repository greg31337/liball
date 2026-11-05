namespace pptx;

internal class PptxTextMeasureState
{
    internal PptxTextFit Result;
    internal long CurrentWidth;
    internal long CurrentHeight;
    internal long AvailableWidth;
    internal bool Wrap;
    internal int Scale;

    internal PptxTextMeasureState(PptxTextFit result)
    {
        Result = result;
        CurrentWidth = 0L;
        CurrentHeight = 0L;
        AvailableWidth = 0L;
        Wrap = true;
        Scale = 100000;
    }
}

internal static class PptxTextFitCore
{
    internal static PptxTextFit Inspect(PptxTextFrame frame)
    {
        if (frame == null)
        {
            throw new System.ArgumentNullException("frame");
        }

        PptxTextFit result = new PptxTextFit();
        PptxTextFrameBody body = frame.GetBodySettings();
        PptxBounds bounds = frame.Shape.GetBounds();
        result.AvailableWidth = bounds.Width - body.Margins.Left - body.Margins.Right;
        result.AvailableHeight = bounds.Height - body.Margins.Top - body.Margins.Bottom;
        if (result.AvailableWidth < 0L)
        {
            result.AvailableWidth = 0L;
        }

        if (result.AvailableHeight < 0L)
        {
            result.AvailableHeight = 0L;
        }

        result.Autofit = body.Autofit;
        PptxTextMeasureState state = new PptxTextMeasureState(result);
        state.AvailableWidth = result.AvailableWidth;
        state.Wrap = body.Wrapping != "none";
        if (body.Autofit == PptxTextFrameBody.AutofitNormal)
        {
            state.Scale = body.AutofitFontScale;
        }

        for (int paragraphIndex = 0; paragraphIndex < frame.Paragraphs.Count; paragraphIndex++)
        {
            PptxParagraph paragraph = frame.Paragraphs[paragraphIndex];
            PptxParagraphFormat format = PptxParagraphFormatCore.GetFormat(paragraph);
            if (paragraph.Runs.Count == 0)
            {
                state.CurrentHeight = ScaleHeight(18.0, state.Scale);
            }

            for (int runIndex = 0; runIndex < paragraph.Runs.Count; runIndex++)
            {
                MeasureRun(state, paragraph.Runs[runIndex], format);
            }

            CommitLine(state);
        }

        if (body.VerticalText != "horz")
        {
            long width = result.EstimatedWidth;
            result.EstimatedWidth = result.EstimatedHeight;
            result.EstimatedHeight = width;
        }

        result.HorizontalOverflow = result.EstimatedWidth > result.AvailableWidth;
        result.VerticalOverflow = result.EstimatedHeight > result.AvailableHeight;
        result.Fits = !result.HorizontalOverflow && !result.VerticalOverflow;
        result.CanAutofit = !result.Fits && result.Autofit.Length > 0 && result.Autofit != PptxTextFrameBody.AutofitNone;
        return result;
    }

    internal static void MeasureRun(PptxTextMeasureState state, PptxRun run, PptxParagraphFormat format)
    {
        double size = run.Font.SizePoints;
        if (size <= 0.0)
        {
            size = format.DefaultFont.SizePoints;
        }

        if (size <= 0.0)
        {
            size = 18.0;
        }

        long characterWidth = ScaleWidth(size, state.Scale) + (long)run.Font.Spacing * 127L;
        if (characterWidth < 0L)
        {
            characterWidth = 0L;
        }

        long lineHeight = ScaleHeight(size, state.Scale);
        if (lineHeight > state.CurrentHeight)
        {
            state.CurrentHeight = lineHeight;
        }

        if (run.IsLineBreak())
        {
            CommitLine(state);
            return;
        }

        if (run.IsTab())
        {
            state.Result.CharacterCount = state.Result.CharacterCount + 1;
            state.CurrentWidth = state.CurrentWidth + characterWidth * 4L;
            return;
        }

        for (int index = 0; index < run.Text.Length; index++)
        {
            char value = run.Text[index];
            if (value == '\n' || value == '\r')
            {
                CommitLine(state);
            }
            else if (value == '\t')
            {
                state.Result.CharacterCount = state.Result.CharacterCount + 1;
                state.CurrentWidth = state.CurrentWidth + characterWidth * 4L;
            }
            else
            {
                state.Result.CharacterCount = state.Result.CharacterCount + 1;
                state.CurrentWidth = state.CurrentWidth + characterWidth;
            }
        }
    }

    internal static void CommitLine(PptxTextMeasureState state)
    {
        long height = state.CurrentHeight;
        if (height <= 0L)
        {
            height = ScaleHeight(18.0, state.Scale);
        }

        int lineCount = 1;
        long displayedWidth = state.CurrentWidth;
        if (state.Wrap && state.AvailableWidth > 0L && state.CurrentWidth > state.AvailableWidth)
        {
            lineCount = (int)((state.CurrentWidth + state.AvailableWidth - 1L) / state.AvailableWidth);
            displayedWidth = state.AvailableWidth;
        }

        state.Result.LineCount = state.Result.LineCount + lineCount;
        state.Result.EstimatedHeight = state.Result.EstimatedHeight + height * (long)lineCount;
        if (displayedWidth > state.Result.EstimatedWidth)
        {
            state.Result.EstimatedWidth = displayedWidth;
        }

        state.CurrentWidth = 0L;
        state.CurrentHeight = 0L;
    }

    internal static long ScaleWidth(double points, int scale)
    {
        return (long)(points * 6350.0 * (double)scale / 100000.0);
    }

    internal static long ScaleHeight(double points, int scale)
    {
        return (long)(points * 15240.0 * (double)scale / 100000.0);
    }
}
