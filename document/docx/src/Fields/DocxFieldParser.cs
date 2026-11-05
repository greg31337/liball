namespace docx;

internal class DocxFieldParser
{
    internal static bool ContainsComplexField(DocxParagraph paragraph)
    {
        return CountComplexFieldElements(paragraph) > 0;
    }

    internal static int CountComplexFieldElements(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        int count = 0;

        foreach (DocxRun run in paragraph.Runs)
        {
            count = count + run.ComplexFieldElementCount();
        }

        return count;
    }

    internal static string GetComplexFieldInstructionText(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        string text = "";

        foreach (DocxRun run in paragraph.Runs)
        {
            text = text + run.GetComplexFieldInstructionText();
        }

        return text;
    }
}
