using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class MasterLayoutThemeOperationsTests
{
    [Test]
    public void PublicOperations_QueryInheritanceAndPersistLayoutAndThemeChanges()
    {
        PptxPresentation presentation = LoadPresentation(CreatePackage());
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxSlideOperations slides = new PptxSlideOperations();

        Assert.That(presentations.SlideMasterCount(presentation), Is.EqualTo(1));
        Assert.That(presentations.SlideLayoutCount(presentation), Is.EqualTo(2));
        Assert.That(presentations.ThemeCount(presentation), Is.EqualTo(2));
        PptxSlideMaster master = presentations.GetSlideMaster(presentation, 0);
        PptxSlideLayout titleLayout = presentations.FindSlideLayoutByName(presentation, "Title Layout")!;
        PptxSlideLayout alternateLayout = presentations.FindSlideLayoutByType(presentation, "blank")!;
        PptxTheme alternateTheme = presentations.FindThemeByName(presentation, "Alternate")!;

        Assert.That(presentations.GetSlideLayout(presentation, 0), Is.SameAs(titleLayout));
        Assert.That(presentations.FindSlideMasterByName(presentation, "Corporate Master"), Is.SameAs(master));
        Assert.That(presentations.FindThemeByName(presentation, "pptx"), Is.Not.Null);
        Assert.That(presentations.GetThemeForMaster(presentation, master), Is.Not.Null);

        PptxSlide slide = presentation.Slides[0];
        PptxPlaceholderInheritance inheritance = slides.InspectPlaceholderInheritance(slide, "title", 1);
        Assert.That(inheritance.SlidePlaceholder, Is.Not.Null);
        Assert.That(inheritance.LayoutPlaceholder, Is.Not.Null);
        Assert.That(inheritance.MasterPlaceholder, Is.Not.Null);
        Assert.That(inheritance.SlidePlaceholder!.ShapeName, Is.EqualTo("Slide Title"));
        Assert.That(inheritance.LayoutPlaceholder!.ShapeName, Is.EqualTo("Layout Title"));
        Assert.That(inheritance.MasterPlaceholder!.ShapeName, Is.EqualTo("Master Title"));

        PptxInheritedBackground masterBackground = slides.GetEffectiveBackground(slide);
        Assert.That(masterBackground.SourceLevel, Is.EqualTo("master"));
        Assert.That(masterBackground.SourcePartName, Is.EqualTo(PptxPartNames.FirstSlideMaster));
        Assert.That(masterBackground.Inherited, Is.True);
        Assert.That(masterBackground.Fill.Color.Value, Is.EqualTo("112233"));

        slides.ApplyLayout(slide, alternateLayout);
        PptxInheritedBackground layoutBackground = slides.GetEffectiveBackground(slide);
        Assert.That(layoutBackground.SourceLevel, Is.EqualTo("layout"));
        Assert.That(layoutBackground.Fill.Color.Value, Is.EqualTo("445566"));

        PptxPresentation applied = SaveReopenAndLoad(presentation);
        Assert.That(applied.Slides[0].Layout, Is.Not.Null);
        Assert.That(applied.Slides[0].Layout!.Name, Is.EqualTo("Alternate Layout"));

        PptxSlideLayout appliedLayout = presentations.FindSlideLayoutByName(applied, "Alternate Layout")!;
        PptxSlideLayout replacement = presentations.FindSlideLayoutByName(applied, "Title Layout")!;
        PptxTheme reboundTheme = presentations.FindThemeByName(applied, "Alternate")!;
        PptxSlideMaster appliedMaster = presentations.GetSlideMaster(applied, 0);
        Assert.That(presentations.ReplaceSlideLayout(applied, appliedLayout, replacement), Is.EqualTo(1));
        presentations.BindTheme(applied, appliedMaster, reboundTheme);

        PptxPresentation reopened = SaveReopenAndLoad(applied);
        PptxSlideMaster reopenedMaster = presentations.GetSlideMaster(reopened, 0);
        Assert.That(reopened.Slides[0].Layout, Is.Not.Null);
        Assert.That(reopened.Slides[0].Layout!.Name, Is.EqualTo("Title Layout"));
        Assert.That(reopenedMaster.Themes, Has.Count.EqualTo(1));
        Assert.That(presentations.GetThemeForMaster(reopened, reopenedMaster)!.Name, Is.EqualTo("Alternate"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void EffectiveBackground_PrefersSlideOverInheritedLevels()
    {
        PptxPresentation presentation = LoadPresentation(CreatePackage());
        PptxSlideOperations operations = new PptxSlideOperations();
        PptxSlide slide = presentation.Slides[0];

        operations.SetBackground(slide, PptxFill.CreateSolidRgb("778899"));
        PptxInheritedBackground background = operations.GetEffectiveBackground(slide);

        Assert.That(background.SourceLevel, Is.EqualTo("slide"));
        Assert.That(background.Inherited, Is.False);
        Assert.That(background.Fill.Color.Value, Is.EqualTo("778899"));
    }

    static PptxPackage CreatePackage()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlideMaster, PptxContentTypes.SlideMaster, Encoding.UTF8.GetBytes(CreateMasterXml()));
        package.ReplacePart(PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(CreateLayoutXml("Title Layout", "title", "")));
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateSlideXml()));

        string secondLayoutPartName = "/ppt/slideLayouts/slideLayout2.xml";
        package.AddPart(secondLayoutPartName, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(CreateLayoutXml("Alternate Layout", "blank", "445566")));
        package.AddRelationship(PptxPartNames.FirstSlideMaster, PptxRelationshipTypes.SlideLayout, "../slideLayouts/slideLayout2.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(secondLayoutPartName, PptxRelationshipTypes.SlideMaster, "../slideMasters/slideMaster1.xml", PptxRelationshipTargetMode.Internal);

        string secondThemePartName = "/ppt/theme/theme2.xml";
        string secondThemeXml = PptxMinimumPresentation.ThemeXml().Replace("name=\"pptx\"", "name=\"Alternate\"");
        package.AddPart(secondThemePartName, PptxContentTypes.Theme, Encoding.UTF8.GetBytes(secondThemeXml));
        package.AddRelationship(PptxPartNames.FirstSlideMaster, PptxRelationshipTypes.Theme, "../theme/theme2.xml", PptxRelationshipTargetMode.Internal);
        return package;
    }

    static PptxPresentation LoadPresentation(PptxPackage package)
    {
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static string CreateMasterXml()
    {
        return
            "<p:sldMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld name=\"Corporate Master\"><p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"112233\"/></a:solidFill></p:bgPr></p:bg><p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>" +
            CreateShapeXml("2", "Master Title") +
            "</p:spTree></p:cSld>" +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
            "<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rId2\"/><p:sldLayoutId id=\"2147483650\" r:id=\"rId3\"/></p:sldLayoutIdLst>" +
            "<p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles></p:sldMaster>";
    }

    static string CreateLayoutXml(string name, string type, string backgroundColor)
    {
        string background = "";
        if (backgroundColor.Length > 0)
        {
            background = "<p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"" + backgroundColor + "\"/></a:solidFill></p:bgPr></p:bg>";
        }

        return
            "<p:sldLayout xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" type=\"" + type + "\" preserve=\"1\">" +
            "<p:cSld name=\"" + name + "\">" + background + "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>" +
            CreateShapeXml("10", "Layout Title") +
            "</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sldLayout>";
    }

    static string CreateSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>" +
            CreateShapeXml("20", "Slide Title") +
            "</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>";
    }

    static string CreateShapeXml(string id, string name)
    {
        return
            "<p:sp><p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"title\" idx=\"1\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr/><p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody></p:sp>";
    }
}
