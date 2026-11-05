using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxDeckGeneratorCore
{
    internal const string TitleType = "title";
    internal const string SubtitleType = "subTitle";
    internal const string ContentType = "content";
    internal const int TitleIndex = 0;
    internal const int SubtitleIndex = 1;
    internal const int ContentIndex = 1;

    internal static PptxDocument CreateDefaultDeck()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlideMaster, PptxContentTypes.SlideMaster, Encoding.UTF8.GetBytes(DefaultMasterXml()));
        package.ReplacePart(PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(BlankLayoutXml()));
        AddLayout(package, 2, TitleSlideLayoutXml());
        AddLayout(package, 3, TitleAndContentLayoutXml());
        AddLayout(package, 4, SectionHeaderLayoutXml());
        AddLayout(package, 5, TwoContentLayoutXml());
        AddLayout(package, 6, TitleOnlyLayoutXml());

        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        PptxPresentation presentation = document.LoadPresentation();
        while (presentation.SlideCount() > 0)
        {
            presentation.RemoveSlideAt(0);
        }

        return document;
    }

    internal static void AddLayout(PptxPackage package, int number, string xml)
    {
        string partName = "/ppt/slideLayouts/slideLayout" + number.ToString() + ".xml";
        package.AddPart(partName, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(xml));
        package.AddRelationship(PptxPartNames.FirstSlideMaster, PptxRelationshipTypes.SlideLayout, "../slideLayouts/slideLayout" + number.ToString() + ".xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(partName, PptxRelationshipTypes.SlideMaster, "../slideMasters/slideMaster1.xml", PptxRelationshipTargetMode.Internal);
    }

    internal static PptxSlide AddTitleSlide(PptxPresentation presentation, string title, string subtitle)
    {
        ValidatePresentation(presentation);
        PptxSlide slide = presentation.AddSlideWithLayout(PptxBuiltInLayout.TitleSlide);
        SetPlaceholderText(slide, TitleType, TitleIndex, title, 32.0, true);
        SetPlaceholderText(slide, SubtitleType, SubtitleIndex, subtitle, 20.0, false);
        return slide;
    }

    internal static PptxSlide AddContentSlide(PptxPresentation presentation, string title, string content)
    {
        ValidatePresentation(presentation);
        PptxSlide slide = presentation.AddSlideWithLayout(PptxBuiltInLayout.TitleAndContent);
        SetPlaceholderText(slide, TitleType, TitleIndex, title, 28.0, true);
        SetPlaceholderText(slide, ContentType, ContentIndex, content, 20.0, false);
        return slide;
    }

    internal static PptxSlide AddBulletSlide(PptxPresentation presentation, string title, List<string> items)
    {
        ValidatePresentation(presentation);
        if (items == null)
        {
            throw new System.ArgumentNullException("items");
        }

        PptxSlide slide = presentation.AddSlideWithLayout(PptxBuiltInLayout.TitleAndContent);
        SetPlaceholderText(slide, TitleType, TitleIndex, title, 28.0, true);
        string first = "";
        if (items.Count > 0)
        {
            first = CleanText(items[0]);
        }

        slide.SetPlaceholderTextByIndex(ContentType, ContentIndex, first);
        PptxShape body = GetPlaceholderShape(slide, ContentType, ContentIndex);
        PptxTextFrame frame = body.GetTextFrame();
        for (int index = 1; index < items.Count; index++)
        {
            frame.AddParagraph(CleanText(items[index]));
        }

        for (int index = 0; index < frame.Paragraphs.Count; index++)
        {
            frame.Paragraphs[index].SetCharacterBullet("•");
        }

        PptxReportGeneratorCore.FormatShapeText(body, 20.0, false);
        return slide;
    }

    internal static PptxSlide AddImageSlide(PptxPresentation presentation, string title, byte[] bytes, string contentType)
    {
        ValidatePresentation(presentation);
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        PptxSlide slide = presentation.AddSlideWithLayout(PptxBuiltInLayout.TitleAndContent);
        SetPlaceholderText(slide, TitleType, TitleIndex, title, 28.0, true);
        XmlElement placeholder = slide.GetRequiredPlaceholderShape(ContentType, ContentIndex);
        PptxShape image = presentation.AddImageWithContentType(slide, bytes, contentType, slide.GetPlaceholderBounds(placeholder));
        slide.ReplacePlaceholderShape(placeholder, image);
        return slide;
    }

    internal static PptxSlide AddTableSlide(PptxPresentation presentation, string title, List<List<string>> data)
    {
        ValidatePresentation(presentation);
        ValidateTableData(data);
        PptxSlide slide = presentation.AddSlideWithLayout(PptxBuiltInLayout.TitleAndContent);
        SetPlaceholderText(slide, TitleType, TitleIndex, title, 28.0, true);
        PptxTable table = slide.PopulatePlaceholderTable(ContentType, ContentIndex, data.Count, data[0].Count);
        table.SetData(data);
        return slide;
    }

    internal static PptxSlide AddChartSlide(PptxPresentation presentation, string title, string chartType, PptxChartData data)
    {
        ValidatePresentation(presentation);
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        PptxSlide slide = presentation.AddSlideWithLayout(PptxBuiltInLayout.TitleAndContent);
        SetPlaceholderText(slide, TitleType, TitleIndex, title, 28.0, true);
        slide.PopulatePlaceholderChart(ContentType, ContentIndex, chartType, data);
        return slide;
    }

    internal static void ValidatePresentation(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (presentation.FindSlideLayoutByName(PptxBuiltInLayout.TitleSlide) == null || presentation.FindSlideLayoutByName(PptxBuiltInLayout.TitleAndContent) == null)
        {
            throw new System.ArgumentException("Presentation does not contain the default built-in layouts.", "presentation");
        }
    }

    internal static void ValidateTableData(List<List<string>> data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (data.Count == 0 || data[0] == null || data[0].Count == 0)
        {
            throw new System.ArgumentException("Table data must contain at least one cell.", "data");
        }

        int columnCount = data[0].Count;
        for (int index = 0; index < data.Count; index++)
        {
            if (data[index] == null || data[index].Count != columnCount)
            {
                throw new System.ArgumentException("Table data must be rectangular.", "data");
            }
        }
    }

    internal static void SetPlaceholderText(PptxSlide slide, string type, int index, string text, double sizePoints, bool bold)
    {
        slide.SetPlaceholderTextByIndex(type, index, CleanText(text));
        PptxReportGeneratorCore.FormatShapeText(GetPlaceholderShape(slide, type, index), sizePoints, bold);
    }

    internal static PptxShape GetPlaceholderShape(PptxSlide slide, string type, int index)
    {
        PptxShapeTree tree = slide.GetShapeTree();
        XmlElement element = slide.GetRequiredPlaceholderShape(type, index);
        return new PptxShape(tree, element);
    }

    internal static string CleanText(string value)
    {
        if (value == null)
        {
            return "";
        }

        return PptxDiagnostic.CleanText(value);
    }

    internal static string DefaultMasterXml()
    {
        return
            "<p:sldMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            PptxMinimumPresentation.CommonSlideDataXml("") +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
            "<p:sldLayoutIdLst>" +
            "<p:sldLayoutId id=\"2147483649\" r:id=\"rId2\"/>" +
            "<p:sldLayoutId id=\"2147483650\" r:id=\"rId3\"/>" +
            "<p:sldLayoutId id=\"2147483651\" r:id=\"rId4\"/>" +
            "<p:sldLayoutId id=\"2147483652\" r:id=\"rId5\"/>" +
            "<p:sldLayoutId id=\"2147483653\" r:id=\"rId6\"/>" +
            "<p:sldLayoutId id=\"2147483654\" r:id=\"rId7\"/>" +
            "</p:sldLayoutIdLst>" +
            "<p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles>" +
            "</p:sldMaster>";
    }

    internal static string BlankLayoutXml()
    {
        return LayoutXml(PptxBuiltInLayout.Blank, "blank", "");
    }

    internal static string TitleSlideLayoutXml()
    {
        return LayoutXml(
            PptxBuiltInLayout.TitleSlide,
            "title",
            PlaceholderXml(2, "Title 1", TitleType, TitleIndex, 1371600L, 1600200L, 9326880L, 1143000L) +
            PlaceholderXml(3, "Subtitle 2", SubtitleType, SubtitleIndex, 1371600L, 3200400L, 9326880L, 914400L));
    }

    internal static string TitleAndContentLayoutXml()
    {
        return LayoutXml(
            PptxBuiltInLayout.TitleAndContent,
            "obj",
            PlaceholderXml(2, "Title 1", TitleType, TitleIndex, 685800L, 365760L, 10820400L, 914400L) +
            PlaceholderXml(3, "Content Placeholder 2", "obj", ContentIndex, 685800L, 1600200L, 10820400L, 4572000L));
    }

    internal static string SectionHeaderLayoutXml()
    {
        return LayoutXml(
            PptxBuiltInLayout.SectionHeader,
            "secHead",
            PlaceholderXml(2, "Section Title 1", TitleType, TitleIndex, 1371600L, 2057400L, 9326880L, 1143000L) +
            PlaceholderXml(3, "Section Text 2", "body", ContentIndex, 1371600L, 3429000L, 9326880L, 914400L));
    }

    internal static string TwoContentLayoutXml()
    {
        return LayoutXml(
            PptxBuiltInLayout.TwoContent,
            "twoObj",
            PlaceholderXml(2, "Title 1", TitleType, TitleIndex, 685800L, 365760L, 10820400L, 914400L) +
            PlaceholderXml(3, "Content Placeholder 2", "obj", 1, 685800L, 1600200L, 5257800L, 4572000L) +
            PlaceholderXml(4, "Content Placeholder 3", "obj", 2, 6256020L, 1600200L, 5257800L, 4572000L));
    }

    internal static string TitleOnlyLayoutXml()
    {
        return LayoutXml(
            PptxBuiltInLayout.TitleOnly,
            "titleOnly",
            PlaceholderXml(2, "Title 1", TitleType, TitleIndex, 685800L, 365760L, 10820400L, 914400L));
    }

    internal static string LayoutXml(string name, string type, string shapes)
    {
        return
            "<p:sldLayout xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" type=\"" + type + "\" preserve=\"1\">" +
            "<p:cSld name=\"" + name + "\">" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            shapes +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sldLayout>";
    }

    internal static string PlaceholderXml(int id, string name, string type, int index, long x, long y, long width, long height)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id.ToString() + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"" + type + "\" idx=\"" + index.ToString() + "\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"" + x.ToString() + "\" y=\"" + y.ToString() + "\"/><a:ext cx=\"" + width.ToString() + "\" cy=\"" + height.ToString() + "\"/></a:xfrm></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody>" +
            "</p:sp>";
    }
}
