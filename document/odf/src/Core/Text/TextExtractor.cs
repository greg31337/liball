using System;
using System.Text;

namespace odf;

internal static class TextExtractor
{
    internal static string ExtractPlainText(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for plain-text extraction.");
        }
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < document.TextBlocks.Length)
        {
            if (index > 0)
            {
                result.Append('\n');
            }
            result.Append(ExtractBlockText(document.TextBlocks[index]));
            index = index + 1;
        }
        return result.ToString();
    }

    internal static OdfTextBlock[] ExtractStructuredText(
        OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for structured-text extraction.");
        }
        OdfTextBlock[] result = new OdfTextBlock[document.TextBlocks.Length];
        Array.Copy(document.TextBlocks, result, document.TextBlocks.Length);
        return result;
    }

    internal static string ExtractBlockText(OdfTextBlock block)
    {
        if (block == null)
        {
            throw new OdfDocumentException(
                "A text block is required for text extraction.");
        }
        if (block.Text != block.OriginalText)
        {
            return block.Text;
        }
        return ExtractRunText(block.Runs);
    }

    internal static string ExtractRunText(OdfTextRun[] runs)
    {
        if (runs == null)
        {
            throw new OdfDocumentException(
                "A structured run collection is required for text extraction.");
        }
        StringBuilder result = new StringBuilder();
        AppendRunsText(result, runs);
        return result.ToString();
    }

    static void AppendRunsText(StringBuilder result, OdfTextRun[] runs)
    {
        int index = 0;
        while (index < runs.Length)
        {
            AppendRunText(result, runs[index]);
            index = index + 1;
        }
    }

    static void AppendRunText(StringBuilder result, OdfTextRun run)
    {
        if (run.Runs.Length > 0)
        {
            AppendRunsText(result, run.Runs);
            return;
        }
        if (run.Kind != OdfTextRunKind.SoftPageBreak)
        {
            result.Append(run.Text);
        }
    }
}
