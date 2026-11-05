using System.Collections.Generic;

namespace pptx;

internal static class PptxTemplateFillerCore
{
    internal static int FillPresentation(PptxPresentation presentation, PptxTemplateData data)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        int count = 0;
        for (int index = 0; index < data.Count(); index++)
        {
            count = count + presentation.ReplaceText(CreateToken(data.Keys[index]), data.Values[index]);
        }

        return count;
    }
    internal static int FillPresentation(PptxPresentation presentation, Dictionary<string, string> values)
    {
        return FillPresentation(presentation, PptxTemplateData.FromDictionary(values));
    }
    internal static int FillPresentation(PptxPresentation presentation, List<string> keys, List<string> values)
    {
        return FillPresentation(presentation, PptxTemplateData.FromLists(keys, values));
    }
    internal static int FillSlide(PptxSlide slide, PptxTemplateData data)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        int count = 0;
        for (int index = 0; index < data.Count(); index++)
        {
            count = count + slide.ReplaceText(CreateToken(data.Keys[index]), data.Values[index]);
        }

        return count;
    }
    internal static int FillSlide(PptxSlide slide, Dictionary<string, string> values)
    {
        return FillSlide(slide, PptxTemplateData.FromDictionary(values));
    }
    internal static int FillSlide(PptxSlide slide, List<string> keys, List<string> values)
    {
        return FillSlide(slide, PptxTemplateData.FromLists(keys, values));
    }

    internal static string CreateToken(string key)
    {
        string cleanKey = PptxTemplateData.CleanKey(key);
        if (cleanKey.Length == 0)
        {
            throw new System.ArgumentException("Template key is required.");
        }

        return "{{" + cleanKey + "}}";
    }
}

