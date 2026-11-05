using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class EffectiveStyleResolutionCompletionTests
{
    const string TableStyleId = "{11111111-2222-3333-4444-555555555555}";

    [Test]
    public void ResolveShapeAndRun_ComposesThemeMasterLayoutSlideAndRunFormatting()
    {
        PptxPresentation presentation = LoadInheritancePresentation();
        PptxShape shape = presentation.GetSlide(0).GetShapeTree().Get(0);
        PptxRun run = shape.GetTextFrame().GetParagraph(0).GetRun(0);
        PptxStyleResolverOperations resolver = new PptxStyleResolverOperations();

        PptxEffectiveStyle shapeStyle = resolver.ResolveShape(shape);
        PptxEffectiveStyle runStyle = resolver.ResolveRun(run);

        Assert.That(shapeStyle.Fill.Color.Value, Is.EqualTo("444444"));
        Assert.That(shapeStyle.FillSource, Is.EqualTo("layout"));
        Assert.That(shapeStyle.Line.Color.Value, Is.EqualTo("FF0000"));
        Assert.That(shapeStyle.LineSource, Is.EqualTo("shape"));
        Assert.That(shapeStyle.Effect.HasGlow, Is.True);
        Assert.That(shapeStyle.Effect.GlowColor.Value, Is.EqualTo("555555"));
        Assert.That(shapeStyle.EffectSource, Is.EqualTo("master"));
        Assert.That(shapeStyle.Background.Color.Value, Is.EqualTo("EEEEEE"));
        Assert.That(shapeStyle.BackgroundSource, Is.EqualTo("layout"));
        Assert.That(runStyle.Font.Family, Is.EqualTo("Minor Face"));
        Assert.That(runStyle.Font.SizePoints, Is.EqualTo(24.0));
        Assert.That(runStyle.TextColor.Value, Is.EqualTo("666666"));
        Assert.That(runStyle.TextSource, Is.EqualTo("run"));
    }

    [Test]
    public void ResolveShape_AppliesThemeStyleReferencesWithPlaceholderColors()
    {
        PptxPresentation presentation = LoadInheritancePresentation();
        PptxShape shape = presentation.GetSlide(0).GetShapeTree().Get(1);

        PptxEffectiveStyle style = new PptxStyleResolverOperations().ResolveShape(shape);

        Assert.That(style.Fill.Color.Value, Is.EqualTo("333333"));
        Assert.That(style.FillSource, Is.EqualTo("shape-style"));
        Assert.That(style.Line.Color.Value, Is.EqualTo("222222"));
        Assert.That(style.LineSource, Is.EqualTo("shape-style"));
        Assert.That(style.Effect.HasGlow, Is.True);
        Assert.That(style.Effect.GlowColor.Value, Is.EqualTo("444444"));
        Assert.That(style.EffectSource, Is.EqualTo("shape-style"));
        Assert.That(style.Font.Family, Is.EqualTo("Minor Face"));
        Assert.That(style.TextColor.Value, Is.EqualTo("555555"));
    }

    [Test]
    public void ResolveTableCell_ComposesWholeTableFirstRowAndDirectCellFormatting()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxTable table = presentation.GetSlide(0).AddTable(2, 2, new PptxBounds(0L, 0L, 600L, 200L));
        table.SetStyleId(TableStyleId);
        table.SetFirstRow(true);
        Assert.That(presentation.TableStylesPart, Is.Not.Null);
        presentation.TableStylesPart!.SetXmlDocument(PptxXml.LoadString(CreateTableStylesXml()));
        PptxStyleResolverOperations resolver = new PptxStyleResolverOperations();

        PptxEffectiveStyle header = resolver.ResolveTableCell(table.GetCell(0, 0));
        PptxEffectiveStyle body = resolver.ResolveTableCell(table.GetCell(1, 0));

        Assert.That(header.Fill.Color.Value, Is.EqualTo("F59E0B"));
        Assert.That(header.Line.Color.Value, Is.EqualTo("DC2626"));
        Assert.That(header.Font.Bold, Is.True);
        Assert.That(header.Font.Family, Is.EqualTo("Aptos"));
        Assert.That(header.TextColor.Value, Is.EqualTo("7C3AED"));
        Assert.That(body.Fill.Color.Value, Is.EqualTo("16A34A"));
        Assert.That(body.Line.Color.Value, Is.EqualTo("DC2626"));

        PptxTableCell direct = table.GetCell(1, 1);
        direct.SetFill(PptxFill.CreateSolid(PptxColor.CreateScheme("accent6")));
        direct.SetBorder(PptxTableCell.BorderLeft, PptxLine.CreateSolid(PptxColor.CreateRgb("010203"), 12700L));
        PptxEffectiveStyle directStyle = resolver.ResolveTableCell(direct);
        Assert.That(directStyle.Fill.Color.Value, Is.EqualTo("0891B2"));
        Assert.That(directStyle.FillSource, Is.EqualTo("cell"));
        Assert.That(directStyle.Line.Color.Value, Is.EqualTo("010203"));
        Assert.That(directStyle.LineSource, Is.EqualTo("cell"));
    }

    [Test]
    public void ResolveChart_ComposesChartDrawingTextAndColorStyleFormatting()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxChartData data = new PptxChartData();
        data.Categories.Add("Q1");
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Revenue";
        series.Values.Add("10");
        data.Series.Add(series);
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Column, data, new PptxBounds(0L, 0L, 600L, 300L));
        XmlDocument document = chart.GetXmlDocument();
        XmlElement root = document.DocumentElement!;
        root.AppendChild(CreateChartShapeProperties(document));
        root.AppendChild(CreateChartTextProperties(document));
        chart.SaveXmlDocument(document);
        PptxStyleResolverOperations resolver = new PptxStyleResolverOperations();

        PptxEffectiveStyle direct = resolver.ResolveChart(chart);

        Assert.That(direct.Fill.Color.Value, Is.EqualTo("DC2626"));
        Assert.That(direct.FillSource, Is.EqualTo("chart"));
        Assert.That(direct.Line.Color.Value, Is.EqualTo("F59E0B"));
        Assert.That(direct.Effect.GlowColor.Value, Is.EqualTo("7C3AED"));
        Assert.That(direct.Font.Family, Is.EqualTo("Aptos Display"));
        Assert.That(direct.TextColor.Value, Is.EqualTo("0891B2"));

        chart.SetColorStyleXml("<cs:colorStyle xmlns:cs=\"http://schemas.microsoft.com/office/drawing/2012/chartStyle\" xmlns:a=\"" + PptxNamespaces.Drawing + "\"><a:schemeClr val=\"accent1\"/></cs:colorStyle>");
        PptxEffectiveStyle colorStyled = resolver.ResolveChart(chart);
        Assert.That(colorStyled.TextColor.Value, Is.EqualTo("2563EB"));
        Assert.That(colorStyled.TextSource, Is.EqualTo("chart-color-style"));
    }

    static PptxPresentation LoadInheritancePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstTheme, PptxContentTypes.Theme, Encoding.UTF8.GetBytes(CreateThemeXml()));
        package.ReplacePart(PptxPartNames.FirstSlideMaster, PptxContentTypes.SlideMaster, Encoding.UTF8.GetBytes(CreateMasterXml()));
        package.ReplacePart(PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(CreateLayoutXml()));
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateSlideXml()));
        return PptxDocument.OpenBytes(package.SaveBytes()).LoadPresentation();
    }

    static string CreateThemeXml()
    {
        return
            "<a:theme xmlns:a=\"" + PptxNamespaces.Drawing + "\" name=\"Inheritance Theme\"><a:themeElements>" +
            "<a:clrScheme name=\"Colors\">" +
            "<a:dk1><a:srgbClr val=\"000000\"/></a:dk1><a:lt1><a:srgbClr val=\"FFFFFF\"/></a:lt1>" +
            "<a:dk2><a:srgbClr val=\"111111\"/></a:dk2><a:lt2><a:srgbClr val=\"EEEEEE\"/></a:lt2>" +
            "<a:accent1><a:srgbClr val=\"111111\"/></a:accent1><a:accent2><a:srgbClr val=\"222222\"/></a:accent2>" +
            "<a:accent3><a:srgbClr val=\"333333\"/></a:accent3><a:accent4><a:srgbClr val=\"444444\"/></a:accent4>" +
            "<a:accent5><a:srgbClr val=\"555555\"/></a:accent5><a:accent6><a:srgbClr val=\"666666\"/></a:accent6>" +
            "<a:hlink><a:srgbClr val=\"0000EE\"/></a:hlink><a:folHlink><a:srgbClr val=\"551A8B\"/></a:folHlink>" +
            "</a:clrScheme>" +
            "<a:fontScheme name=\"Fonts\"><a:majorFont><a:latin typeface=\"Major Face\"/></a:majorFont><a:minorFont><a:latin typeface=\"Minor Face\"/></a:minorFont></a:fontScheme>" +
            "<a:fmtScheme name=\"Formats\">" +
            "<a:fillStyleLst><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:fillStyleLst>" +
            "<a:lnStyleLst><a:ln w=\"19050\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:ln></a:lnStyleLst>" +
            "<a:effectStyleLst><a:effectStyle><a:effectLst><a:glow rad=\"20000\"><a:schemeClr val=\"phClr\"/></a:glow></a:effectLst></a:effectStyle></a:effectStyleLst>" +
            "<a:bgFillStyleLst><a:solidFill><a:schemeClr val=\"lt1\"/></a:solidFill></a:bgFillStyleLst>" +
            "</a:fmtScheme></a:themeElements></a:theme>";
    }

    static string CreateMasterXml()
    {
        return
            "<p:sldMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld><p:spTree>" + ShapeTreePrefix() +
            "<p:sp><p:nvSpPr><p:cNvPr id=\"2\" name=\"Master Body\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"body\" idx=\"1\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr><a:effectLst><a:glow rad=\"10000\"><a:schemeClr val=\"accent5\"/></a:glow></a:effectLst></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody></p:sp>" +
            "</p:spTree></p:cSld>" +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
            "<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rId2\"/></p:sldLayoutIdLst>" +
            "<p:txStyles><p:titleStyle/><p:bodyStyle><a:lvl1pPr><a:defRPr sz=\"1800\"><a:latin typeface=\"+mn-lt\"/><a:solidFill><a:schemeClr val=\"accent5\"/></a:solidFill></a:defRPr></a:lvl1pPr></p:bodyStyle><p:otherStyle/></p:txStyles>" +
            "</p:sldMaster>";
    }

    static string CreateLayoutXml()
    {
        return
            "<p:sldLayout xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" type=\"blank\" preserve=\"1\">" +
            "<p:cSld name=\"Layout\"><p:bg><p:bgPr><a:solidFill><a:schemeClr val=\"lt2\"/></a:solidFill><a:effectLst/></p:bgPr></p:bg><p:spTree>" + ShapeTreePrefix() +
            "<p:sp><p:nvSpPr><p:cNvPr id=\"2\" name=\"Layout Body\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"body\" idx=\"1\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr><a:solidFill><a:schemeClr val=\"accent4\"/></a:solidFill></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle><a:lvl1pPr><a:defRPr><a:solidFill><a:schemeClr val=\"accent6\"/></a:solidFill></a:defRPr></a:lvl1pPr></a:lstStyle><a:p/></p:txBody></p:sp>" +
            "</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sldLayout>";
    }

    static string CreateSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld><p:spTree>" + ShapeTreePrefix() +
            "<p:sp><p:nvSpPr><p:cNvPr id=\"2\" name=\"Slide Body\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"body\" idx=\"1\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr><a:ln w=\"12700\"><a:solidFill><a:srgbClr val=\"FF0000\"/></a:solidFill></a:ln></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:rPr sz=\"2400\"/><a:t>Inherited</a:t></a:r></a:p></p:txBody></p:sp>" +
            "<p:sp><p:nvSpPr><p:cNvPr id=\"3\" name=\"Styled Shape\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr/>" +
            "<p:style><a:lnRef idx=\"1\"><a:schemeClr val=\"accent2\"/></a:lnRef><a:fillRef idx=\"1\"><a:schemeClr val=\"accent3\"/></a:fillRef><a:effectRef idx=\"1\"><a:schemeClr val=\"accent4\"/></a:effectRef><a:fontRef idx=\"minor\"><a:schemeClr val=\"accent5\"/></a:fontRef></p:style>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody></p:sp>" +
            "</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>";
    }

    static string ShapeTreePrefix()
    {
        return "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>";
    }

    static string CreateTableStylesXml()
    {
        return
            "<a:tblStyleLst xmlns:a=\"" + PptxNamespaces.Drawing + "\" def=\"" + TableStyleId + "\"><a:tblStyle styleId=\"" + TableStyleId + "\" styleName=\"Resolver\">" +
            "<a:wholeTbl><a:tcTxStyle b=\"0\"><a:fontRef idx=\"minor\"><a:schemeClr val=\"accent1\"/></a:fontRef></a:tcTxStyle>" +
            "<a:tcStyle><a:fill><a:solidFill><a:schemeClr val=\"accent2\"/></a:solidFill></a:fill><a:tcBdr><a:lnL w=\"12700\"><a:solidFill><a:schemeClr val=\"accent3\"/></a:solidFill></a:lnL></a:tcBdr></a:tcStyle></a:wholeTbl>" +
            "<a:firstRow><a:tcTxStyle b=\"1\"><a:fontRef idx=\"minor\"><a:schemeClr val=\"accent5\"/></a:fontRef></a:tcTxStyle><a:tcStyle><a:fill><a:solidFill><a:schemeClr val=\"accent4\"/></a:solidFill></a:fill></a:tcStyle></a:firstRow>" +
            "</a:tblStyle></a:tblStyleLst>";
    }

    static XmlElement CreateChartShapeProperties(XmlDocument document)
    {
        XmlElement properties = document.CreateElement("c", "spPr", PptxNamespaces.Chart);
        properties.InnerXml =
            "<a:solidFill xmlns:a=\"" + PptxNamespaces.Drawing + "\"><a:schemeClr val=\"accent3\"/></a:solidFill>" +
            "<a:ln xmlns:a=\"" + PptxNamespaces.Drawing + "\"><a:solidFill><a:schemeClr val=\"accent4\"/></a:solidFill></a:ln>" +
            "<a:effectLst xmlns:a=\"" + PptxNamespaces.Drawing + "\"><a:glow rad=\"10000\"><a:schemeClr val=\"accent5\"/></a:glow></a:effectLst>";
        return properties;
    }

    static XmlElement CreateChartTextProperties(XmlDocument document)
    {
        XmlElement properties = document.CreateElement("c", "txPr", PptxNamespaces.Chart);
        properties.InnerXml =
            "<a:bodyPr xmlns:a=\"" + PptxNamespaces.Drawing + "\"/><a:lstStyle xmlns:a=\"" + PptxNamespaces.Drawing + "\"/>" +
            "<a:p xmlns:a=\"" + PptxNamespaces.Drawing + "\"><a:pPr><a:defRPr><a:latin typeface=\"+mj-lt\"/><a:solidFill><a:schemeClr val=\"accent6\"/></a:solidFill></a:defRPr></a:pPr><a:endParaRPr/></a:p>";
        return properties;
    }
}
