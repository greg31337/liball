using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class MasterLayoutPlaceholderTests
{
    [Test]
    public void LoadPresentation_LoadsMasterAndLayoutPlaceholdersWithInheritanceMetadata()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlideMaster, PptxContentTypes.SlideMaster, Encoding.UTF8.GetBytes(CreateMasterXml()));
        package.ReplacePart(PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(CreateLayoutXml()));

        PptxPresentation presentation = PptxPresentation.Load(Reopen(package));

        Assert.That(presentation.Succeeded(), Is.True, presentation.Diagnostics.FormatDiagnostics());
        Assert.That(presentation.SlideMasters, Has.Count.EqualTo(1));
        Assert.That(presentation.SlideLayouts, Has.Count.EqualTo(1));

        PptxSlideMaster master = presentation.SlideMasters[0];
        Assert.That(master.Placeholders, Has.Count.EqualTo(2));
        Assert.That(master.Placeholders[0].SourcePartName, Is.EqualTo(PptxPartNames.FirstSlideMaster));
        Assert.That(master.Placeholders[0].Type, Is.EqualTo("title"));
        Assert.That(master.Placeholders[0].Index, Is.EqualTo(1));
        Assert.That(master.Placeholders[0].Size, Is.EqualTo("full"));
        Assert.That(master.Placeholders[0].ShapeId, Is.EqualTo("2"));
        Assert.That(master.Placeholders[0].ShapeName, Is.EqualTo("Master Title"));

        PptxSlideLayout layout = presentation.SlideLayouts[0];
        Assert.That(layout.Master, Is.SameAs(master));
        Assert.That(layout.Placeholders, Has.Count.EqualTo(1));
        Assert.That(layout.Placeholders[0].Type, Is.EqualTo("title"));
        Assert.That(layout.Placeholders[0].Index, Is.EqualTo(1));
        Assert.That(layout.Placeholders[0].ShapeId, Is.EqualTo("10"));
        Assert.That(layout.Placeholders[0].ShapeName, Is.EqualTo("Layout Title"));
        Assert.That(layout.Placeholders[0].Inherited, Is.True);
        Assert.That(layout.Placeholders[0].InheritedFromPartName, Is.EqualTo(PptxPartNames.FirstSlideMaster));
        Assert.That(layout.Placeholders[0].InheritedShapeId, Is.EqualTo("2"));
        Assert.That(layout.Placeholders[0].InheritedShapeName, Is.EqualTo("Master Title"));

        Assert.That(layout.EffectivePlaceholders, Has.Count.EqualTo(2));
        Assert.That(layout.EffectivePlaceholders[0], Is.SameAs(layout.Placeholders[0]));
        Assert.That(layout.EffectivePlaceholders[1].Type, Is.EqualTo("body"));
        Assert.That(layout.EffectivePlaceholders[1].Index, Is.EqualTo(2));
        Assert.That(layout.EffectivePlaceholders[1].ShapeName, Is.EqualTo("Master Body"));
        Assert.That(layout.EffectivePlaceholders[1].Inherited, Is.True);
        Assert.That(layout.EffectivePlaceholders[1].InheritedFromPartName, Is.EqualTo(PptxPartNames.FirstSlideMaster));
    }

    [Test]
    public void LoadPresentation_LayoutWithoutOwnPlaceholdersExposesInheritedMasterPlaceholders()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlideMaster, PptxContentTypes.SlideMaster, Encoding.UTF8.GetBytes(CreateMasterXml()));
        package.ReplacePart(PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(CreateBlankLayoutXml()));

        PptxPresentation presentation = PptxPresentation.Load(Reopen(package));

        Assert.That(presentation.Succeeded(), Is.True, presentation.Diagnostics.FormatDiagnostics());
        PptxSlideLayout layout = presentation.SlideLayouts[0];
        Assert.That(layout.Placeholders, Is.Empty);
        Assert.That(layout.EffectivePlaceholders, Has.Count.EqualTo(2));
        Assert.That(layout.EffectivePlaceholders[0].Type, Is.EqualTo("title"));
        Assert.That(layout.EffectivePlaceholders[1].Type, Is.EqualTo("body"));
        Assert.That(layout.EffectivePlaceholders[0].SourcePartName, Is.EqualTo(PptxPartNames.FirstSlideLayout));
        Assert.That(layout.EffectivePlaceholders[0].Inherited, Is.True);
        Assert.That(layout.EffectivePlaceholders[0].InheritedFromPartName, Is.EqualTo(PptxPartNames.FirstSlideMaster));
    }

    [Test]
    public void LoadPresentation_DefaultPlaceholderTypeMatchesObjInheritance()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlideMaster, PptxContentTypes.SlideMaster, Encoding.UTF8.GetBytes(CreateDefaultTypeMasterXml()));
        package.ReplacePart(PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(CreateDefaultTypeLayoutXml()));

        PptxPresentation presentation = PptxPresentation.Load(Reopen(package));

        Assert.That(presentation.Succeeded(), Is.True, presentation.Diagnostics.FormatDiagnostics());
        PptxSlideLayout layout = presentation.SlideLayouts[0];
        Assert.That(presentation.SlideMasters[0].Placeholders[0].Type, Is.EqualTo("obj"));
        Assert.That(layout.Placeholders[0].Type, Is.EqualTo("obj"));
        Assert.That(layout.Placeholders[0].Inherited, Is.True);
        Assert.That(layout.EffectivePlaceholders, Has.Count.EqualTo(1));
    }

    static PptxPackage Reopen(PptxPackage package)
    {
        byte[] bytes = package.SaveBytes();
        PptxDocument document = PptxDocument.OpenBytes(bytes);
        return document.GetPackage();
    }

    static string CreateMasterXml()
    {
        return
            "<p:sldMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld><p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr/>" +
            CreateShapeXml("2", "Master Title", "title", "1", "full") +
            CreateShapeXml("3", "Master Body", "body", "2", "half") +
            "</p:spTree></p:cSld>" +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
            "<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rId2\"/></p:sldLayoutIdLst>" +
            "<p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles>" +
            "</p:sldMaster>";
    }

    static string CreateLayoutXml()
    {
        return
            "<p:sldLayout xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" type=\"title\" preserve=\"1\">" +
            "<p:cSld name=\"Title\"><p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr/>" +
            CreateShapeXml("10", "Layout Title", "title", "1", "full") +
            "</p:spTree></p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sldLayout>";
    }

    static string CreateBlankLayoutXml()
    {
        return
            "<p:sldLayout xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" type=\"blank\" preserve=\"1\">" +
            PptxMinimumPresentation.CommonSlideDataXml("Blank") +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sldLayout>";
    }

    static string CreateDefaultTypeMasterXml()
    {
        return
            "<p:sldMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>" +
            CreateShapeXml("2", "Master Object", "", "4", "") +
            "</p:spTree></p:cSld>" +
            "<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rId2\"/></p:sldLayoutIdLst>" +
            "</p:sldMaster>";
    }

    static string CreateDefaultTypeLayoutXml()
    {
        return
            "<p:sldLayout xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>" +
            CreateShapeXml("8", "Layout Object", "", "4", "") +
            "</p:spTree></p:cSld>" +
            "</p:sldLayout>";
    }

    static string CreateShapeXml(string id, string name, string type, string index, string size)
    {
        string typeAttribute = "";
        string indexAttribute = "";
        string sizeAttribute = "";
        if (type.Length > 0)
        {
            typeAttribute = " type=\"" + type + "\"";
        }

        if (index.Length > 0)
        {
            indexAttribute = " idx=\"" + index + "\"";
        }

        if (size.Length > 0)
        {
            sizeAttribute = " sz=\"" + size + "\"";
        }

        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr><p:ph" + typeAttribute + indexAttribute + sizeAttribute + "/></p:nvPr></p:nvSpPr>" +
            "<p:spPr/><p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody>" +
            "</p:sp>";
    }
}
