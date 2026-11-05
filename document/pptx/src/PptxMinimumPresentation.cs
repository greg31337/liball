using System.Text;

namespace pptx;

internal static class PptxMinimumPresentation
{
    internal static PptxDocument CreateDocument()
    {
        return new PptxDocument(CreatePackage());
    }

    internal static PptxPackage CreatePackage()
    {
        PptxPackage package = PptxPackage.Create();

        AddXmlPart(package, PptxPartNames.CoreProperties, PptxContentTypes.CoreProperties, CorePropertiesXml());
        AddXmlPart(package, PptxPartNames.ExtendedProperties, PptxContentTypes.ExtendedProperties, ExtendedPropertiesXml());
        AddXmlPart(package, PptxPartNames.Presentation, PptxContentTypes.Presentation, PresentationXml());
        AddXmlPart(package, PptxPartNames.PresentationProperties, PptxContentTypes.PresentationProperties, PresentationPropertiesXml());
        AddXmlPart(package, PptxPartNames.ViewProperties, PptxContentTypes.ViewProperties, ViewPropertiesXml());
        AddXmlPart(package, PptxPartNames.TableStyles, PptxContentTypes.TableStyles, TableStylesXml());
        AddXmlPart(package, PptxPartNames.FirstSlideMaster, PptxContentTypes.SlideMaster, SlideMasterXml());
        AddXmlPart(package, PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout, SlideLayoutXml());
        AddXmlPart(package, PptxPartNames.FirstTheme, PptxContentTypes.Theme, ThemeXml());
        AddXmlPart(package, PptxPartNames.FirstSlide, PptxContentTypes.Slide, SlideXml());

        AddRelationships(package);

        return package;
    }

    internal static PptxPart AddXmlPart(PptxPackage package, string partName, string contentType, string xml)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        return package.AddPart(partName, contentType, Encoding.UTF8.GetBytes(xml));
    }

    internal static void AddRelationships(PptxPackage package)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("", PptxRelationshipTypes.CoreProperties, "docProps/core.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("", PptxRelationshipTypes.ExtendedProperties, "docProps/app.xml", PptxRelationshipTargetMode.Internal);

        package.AddRelationship(PptxPartNames.Presentation, PptxRelationshipTypes.SlideMaster, "slideMasters/slideMaster1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.Presentation, PptxRelationshipTypes.Slide, "slides/slide1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.Presentation, PptxRelationshipTypes.PresentationProperties, "presProps.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.Presentation, PptxRelationshipTypes.ViewProperties, "viewProps.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.Presentation, PptxRelationshipTypes.TableStyles, "tableStyles.xml", PptxRelationshipTargetMode.Internal);

        package.AddRelationship(PptxPartNames.FirstSlideMaster, PptxRelationshipTypes.Theme, "../theme/theme1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.FirstSlideMaster, PptxRelationshipTypes.SlideLayout, "../slideLayouts/slideLayout1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.FirstSlideLayout, PptxRelationshipTypes.SlideMaster, "../slideMasters/slideMaster1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship(PptxPartNames.FirstSlide, PptxRelationshipTypes.SlideLayout, "../slideLayouts/slideLayout1.xml", PptxRelationshipTargetMode.Internal);
    }

    internal static string PresentationXml()
    {
        return
            "<p:presentation xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:sldMasterIdLst><p:sldMasterId id=\"2147483648\" r:id=\"rId1\"/></p:sldMasterIdLst>" +
            "<p:sldIdLst><p:sldId id=\"256\" r:id=\"rId2\"/></p:sldIdLst>" +
            "<p:sldSz cx=\"12192000\" cy=\"6858000\" type=\"wide\"/>" +
            "<p:notesSz cx=\"6858000\" cy=\"9144000\"/>" +
            "<p:defaultTextStyle/>" +
            "</p:presentation>";
    }

    internal static string CorePropertiesXml()
    {
        return
            "<cp:coreProperties xmlns:cp=\"" + PptxNamespaces.CoreProperties + "\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:dcmitype=\"http://purl.org/dc/dcmitype/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
            "<dc:title/>" +
            "<dc:creator>pptx</dc:creator>" +
            "<cp:lastModifiedBy>pptx</cp:lastModifiedBy>" +
            "</cp:coreProperties>";
    }

    internal static string ExtendedPropertiesXml()
    {
        return
            "<Properties xmlns=\"" + PptxNamespaces.ExtendedProperties + "\" xmlns:vt=\"" + PptxNamespaces.VariantTypes + "\">" +
            "<Application>pptx</Application>" +
            "<PresentationFormat>On-screen Show (16:9)</PresentationFormat>" +
            "<Slides>1</Slides>" +
            "<Notes>0</Notes>" +
            "<HiddenSlides>0</HiddenSlides>" +
            "</Properties>";
    }

    internal static string PresentationPropertiesXml()
    {
        return "<p:presentationPr xmlns:p=\"" + PptxNamespaces.Presentation + "\"/>";
    }

    internal static string ViewPropertiesXml()
    {
        return
            "<p:viewPr xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:normalViewPr/>" +
            "</p:viewPr>";
    }

    internal static string TableStylesXml()
    {
        return "<a:tblStyleLst xmlns:a=\"" + PptxNamespaces.Drawing + "\" def=\"{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}\"/>";
    }

    internal static string SlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            CommonSlideDataXml("") +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    internal static string SlideLayoutXml()
    {
        return
            "<p:sldLayout xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" type=\"blank\" preserve=\"1\">" +
            CommonSlideDataXml("Blank") +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sldLayout>";
    }

    internal static string SlideMasterXml()
    {
        return
            "<p:sldMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            CommonSlideDataXml("") +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
            "<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rId2\"/></p:sldLayoutIdLst>" +
            "<p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles>" +
            "</p:sldMaster>";
    }

    internal static string CommonSlideDataXml(string name)
    {
        string nameAttribute = "";
        if (name.Length > 0)
        {
            nameAttribute = " name=\"" + name + "\"";
        }

        return
            "<p:cSld" + nameAttribute + ">" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "</p:spTree>" +
            "</p:cSld>";
    }

    internal static string ThemeXml()
    {
        return
            "<a:theme xmlns:a=\"" + PptxNamespaces.Drawing + "\" name=\"pptx\">" +
            "<a:themeElements>" +
            "<a:clrScheme name=\"pptx\">" +
            "<a:dk1><a:sysClr val=\"windowText\" lastClr=\"000000\"/></a:dk1>" +
            "<a:lt1><a:sysClr val=\"window\" lastClr=\"FFFFFF\"/></a:lt1>" +
            "<a:dk2><a:srgbClr val=\"1F2937\"/></a:dk2>" +
            "<a:lt2><a:srgbClr val=\"F9FAFB\"/></a:lt2>" +
            "<a:accent1><a:srgbClr val=\"2563EB\"/></a:accent1>" +
            "<a:accent2><a:srgbClr val=\"16A34A\"/></a:accent2>" +
            "<a:accent3><a:srgbClr val=\"DC2626\"/></a:accent3>" +
            "<a:accent4><a:srgbClr val=\"F59E0B\"/></a:accent4>" +
            "<a:accent5><a:srgbClr val=\"7C3AED\"/></a:accent5>" +
            "<a:accent6><a:srgbClr val=\"0891B2\"/></a:accent6>" +
            "<a:hlink><a:srgbClr val=\"0000EE\"/></a:hlink>" +
            "<a:folHlink><a:srgbClr val=\"551A8B\"/></a:folHlink>" +
            "</a:clrScheme>" +
            "<a:fontScheme name=\"pptx\"><a:majorFont><a:latin typeface=\"Aptos Display\"/></a:majorFont><a:minorFont><a:latin typeface=\"Aptos\"/></a:minorFont></a:fontScheme>" +
            "<a:fmtScheme name=\"pptx\">" +
            "<a:fillStyleLst><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:fillStyleLst>" +
            "<a:lnStyleLst><a:ln w=\"9525\" cap=\"flat\" cmpd=\"sng\" algn=\"ctr\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill><a:prstDash val=\"solid\"/></a:ln></a:lnStyleLst>" +
            "<a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle></a:effectStyleLst>" +
            "<a:bgFillStyleLst><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:bgFillStyleLst>" +
            "</a:fmtScheme>" +
            "</a:themeElements>" +
            "</a:theme>";
    }
}
