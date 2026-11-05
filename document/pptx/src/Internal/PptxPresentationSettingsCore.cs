using System.Globalization;
using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxPresentationSettingsCore
{
    internal static PptxPresentationSettings Read(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        PptxPresentationSettings settings = new PptxPresentationSettings();
        ReadPresentationSettings(presentation, settings);
        ReadViewSettings(presentation, settings.View);
        ReadShowSettings(presentation, settings);
        ReadSlideNumberSettings(presentation, settings);
        ReadSlideTimings(presentation, settings);
        return settings;
    }

    internal static void Write(PptxPresentation presentation, PptxPresentationSettings settings)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        Validate(settings);
        WritePresentationSettings(presentation, settings);
        WriteViewSettings(presentation, settings.View);
        WriteShowSettings(presentation, settings);
        WriteSlideNumberSettings(presentation, settings.ShowSlideNumbers);
        WriteSlideTimings(presentation, settings);
    }

    internal static void ReadPresentationSettings(PptxPresentation presentation, PptxPresentationSettings settings)
    {
        XmlDocument document = presentation.GetPresentationXmlDocument();
        XmlElement root = presentation.GetPresentationRoot(document);
        settings.FirstSlideNumber = PptxXml.GetIntAttribute(root, "", "firstSlideNum", 1);
        settings.ShowSpecialPlaceholdersOnTitleSlides = PptxXml.GetBoolAttribute(root, "", "showSpecialPlsOnTitleSld", true);
        settings.Orientation = presentation.GetSlideOrientation();
        XmlElement? defaultTextStyle = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "defaultTextStyle");
        if (defaultTextStyle != null)
        {
            settings.DefaultTextStyle.Xml = defaultTextStyle.OuterXml;
        }
    }

    internal static void ReadViewSettings(PptxPresentation presentation, PptxPresentationViewSettings settings)
    {
        if (presentation.ViewPropertiesPart == null)
        {
            return;
        }

        XmlDocument document = presentation.GetPartXmlDocument(presentation.ViewPropertiesPart);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        settings.LastView = PptxXml.GetAttributeValue(root, "", "lastView");
        settings.ShowComments = PptxXml.GetBoolAttribute(root, "", "showComments", false);
        XmlElement? gridSpacing = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "gridSpacing");
        if (gridSpacing != null)
        {
            settings.GridSpacingX = PptxPresentation.GetLongAttribute(gridSpacing, "cx", 0L);
            settings.GridSpacingY = PptxPresentation.GetLongAttribute(gridSpacing, "cy", 0L);
        }

        XmlElement? slideView = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "slideViewPr");
        if (slideView == null)
        {
            return;
        }

        XmlElement? common = PptxXml.FindFirstChild(slideView, PptxNamespaces.Presentation, "cSldViewPr");
        if (common == null)
        {
            return;
        }

        settings.ShowGuides = PptxXml.GetBoolAttribute(common, "", "showGuides", false);
        settings.SnapToGrid = PptxXml.GetBoolAttribute(common, "", "snapToGrid", false);
        settings.SnapToObjects = PptxXml.GetBoolAttribute(common, "", "snapToObjects", false);
        XmlElement? guideList = PptxXml.FindFirstChild(common, PptxNamespaces.Presentation, "guideLst");
        if (guideList == null)
        {
            return;
        }

        for (int index = 0; index < guideList.ChildNodes.Count; index++)
        {
            XmlNode? child = guideList.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "guide")
            {
                PptxPresentationGuide guide = new PptxPresentationGuide();
                guide.Orientation = PptxXml.GetAttributeValue(element, "", "orient");
                if (guide.Orientation.Length == 0)
                {
                    guide.Orientation = "horz";
                }

                guide.Position = PptxXml.GetIntAttribute(element, "", "pos", 0);
                settings.Guides.Add(guide);
            }
        }
    }

    internal static void ReadShowSettings(PptxPresentation presentation, PptxPresentationSettings settings)
    {
        if (presentation.Properties == null)
        {
            return;
        }

        XmlDocument document = presentation.GetPartXmlDocument(presentation.Properties.Part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        XmlElement? showProperties = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "showPr");
        if (showProperties != null)
        {
            settings.Loop = PptxXml.GetBoolAttribute(showProperties, "", "loop", false);
            settings.UseTimings = PptxXml.GetBoolAttribute(showProperties, "", "useTimings", true);
        }
    }

    internal static void ReadSlideNumberSettings(PptxPresentation presentation, PptxPresentationSettings settings)
    {
        settings.ShowSlideNumbers = true;
        for (int index = 0; index < presentation.SlideMasters.Count; index++)
        {
            if (!ReadHeaderFooterSlideNumber(presentation, presentation.SlideMasters[index].Part))
            {
                settings.ShowSlideNumbers = false;
            }
        }

        for (int index = 0; index < presentation.SlideLayouts.Count; index++)
        {
            if (!ReadHeaderFooterSlideNumber(presentation, presentation.SlideLayouts[index].Part))
            {
                settings.ShowSlideNumbers = false;
            }
        }
    }

    internal static bool ReadHeaderFooterSlideNumber(PptxPresentation presentation, PptxPart part)
    {
        XmlDocument document = presentation.GetPartXmlDocument(part);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return true;
        }

        XmlElement? headerFooter = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "hf");
        if (headerFooter == null)
        {
            return true;
        }

        return PptxXml.GetBoolAttribute(headerFooter, "", "sldNum", true);
    }

    internal static void ReadSlideTimings(PptxPresentation presentation, PptxPresentationSettings settings)
    {
        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            PptxSlide slide = presentation.GetSlide(index);
            PptxSlideTransition transition = slide.GetTransition();
            PptxSlideTimingSettings timing = new PptxSlideTimingSettings();
            timing.SlideId = slide.SlideId;
            timing.AdvanceOnClick = transition.AdvanceOnClick;
            timing.AdvanceAfter = transition.AdvanceAfter;
            if (transition.Element != null)
            {
                timing.AdvanceAfterMilliseconds = PptxPresentation.GetLongAttribute(transition.Element, "advTm", 0L);
            }

            settings.SlideTimings.Add(timing);
        }
    }

    internal static void Validate(PptxPresentationSettings settings)
    {
        if (settings == null)
        {
            throw new System.ArgumentNullException("settings");
        }

        if (settings.View == null)
        {
            throw new System.ArgumentException("Presentation view settings are required.");
        }

        if (settings.DefaultTextStyle == null)
        {
            throw new System.ArgumentException("Default text style settings are required.");
        }

        if (settings.SlideTimings == null)
        {
            throw new System.ArgumentException("Slide timing settings are required.");
        }

        if (settings.Orientation != "landscape" && settings.Orientation != "portrait")
        {
            throw new System.ArgumentException("Orientation must be landscape or portrait.");
        }

        bool noGrid = settings.View.GridSpacingX == 0L && settings.View.GridSpacingY == 0L;
        bool validGrid = settings.View.GridSpacingX > 0L && settings.View.GridSpacingY > 0L;
        if (!noGrid && !validGrid)
        {
            throw new System.ArgumentOutOfRangeException("GridSpacing");
        }

        for (int index = 0; index < settings.View.Guides.Count; index++)
        {
            PptxPresentationGuide guide = settings.View.Guides[index];
            if (guide == null)
            {
                throw new System.ArgumentException("Presentation guides cannot contain null values.");
            }

            if (guide.Orientation != "horz" && guide.Orientation != "vert")
            {
                throw new System.ArgumentException("Guide orientation must be horz or vert.");
            }
        }

        for (int index = 0; index < settings.SlideTimings.Count; index++)
        {
            PptxSlideTimingSettings timing = settings.SlideTimings[index];
            if (timing == null)
            {
                throw new System.ArgumentException("Slide timings cannot contain null values.");
            }

            if (timing.AdvanceAfterMilliseconds < 0L)
            {
                throw new System.ArgumentOutOfRangeException("AdvanceAfterMilliseconds");
            }
        }

        ValidateDefaultTextStyle(settings.DefaultTextStyle.Xml);
    }

    internal static void ValidateDefaultTextStyle(string xml)
    {
        if (xml == null)
        {
            throw new System.ArgumentNullException("xml");
        }

        if (xml.Length == 0)
        {
            return;
        }

        XmlDocument document = PptxXml.LoadString(xml);
        XmlElement? root = document.DocumentElement;
        if (root == null || root.NamespaceURI != PptxNamespaces.Presentation || root.LocalName != "defaultTextStyle")
        {
            throw new System.ArgumentException("Default text style XML must contain a p:defaultTextStyle root element.");
        }
    }

    internal static void WritePresentationSettings(PptxPresentation presentation, PptxPresentationSettings settings)
    {
        XmlDocument document = presentation.GetPresentationXmlDocument();
        XmlElement root = presentation.GetPresentationRoot(document);
        PptxXml.SetAttributeValue(root, "", "", "firstSlideNum", settings.FirstSlideNumber.ToString(CultureInfo.InvariantCulture));
        SetBooleanAttribute(root, "showSpecialPlsOnTitleSld", settings.ShowSpecialPlaceholdersOnTitleSlides);
        WriteOrientation(presentation, root, settings.Orientation);
        WriteDefaultTextStyle(document, root, settings.DefaultTextStyle.Xml);
        presentation.SetPresentationXmlDocument(document);
    }

    internal static void WriteOrientation(PptxPresentation presentation, XmlElement root, string orientation)
    {
        bool swap = orientation == "portrait" && presentation.SlideWidth > presentation.SlideHeight;
        if (orientation == "landscape" && presentation.SlideHeight > presentation.SlideWidth)
        {
            swap = true;
        }

        if (swap)
        {
            long width = presentation.SlideWidth;
            presentation.SlideWidth = presentation.SlideHeight;
            presentation.SlideHeight = width;
        }

        XmlElement slideSize = PptxXml.GetOrCreateChild(root, "p", "sldSz", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(slideSize, "", "", "cx", presentation.SlideWidth.ToString(CultureInfo.InvariantCulture));
        PptxXml.SetAttributeValue(slideSize, "", "", "cy", presentation.SlideHeight.ToString(CultureInfo.InvariantCulture));
    }

    internal static void WriteDefaultTextStyle(XmlDocument document, XmlElement root, string xml)
    {
        XmlElement? existing = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "defaultTextStyle");
        if (xml.Length == 0)
        {
            if (existing != null)
            {
                root.RemoveChild(existing);
            }

            return;
        }

        XmlDocument styleDocument = PptxXml.LoadString(xml);
        XmlElement? styleRoot = styleDocument.DocumentElement;
        if (styleRoot == null)
        {
            throw new System.ArgumentException("Default text style XML root is required.");
        }

        XmlNode imported = document.ImportNode(styleRoot, true);
        if (existing != null)
        {
            root.ReplaceChild(imported, existing);
            return;
        }

        XmlElement? extensionList = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "extLst");
        root.InsertBefore(imported, extensionList);
    }

    internal static void WriteViewSettings(PptxPresentation presentation, PptxPresentationViewSettings settings)
    {
        PptxPart part = EnsureViewPropertiesPart(presentation);
        XmlDocument document = presentation.GetPartXmlDocument(part);
        XmlElement root = GetOrCreateRoot(document, "viewPr");
        SetTextAttribute(root, "lastView", settings.LastView);
        SetBooleanAttribute(root, "showComments", settings.ShowComments);
        WriteGridSpacing(document, root, settings);
        XmlElement slideView = GetOrCreateOrderedChild(document, root, "slideViewPr", "gridSpacing");
        XmlElement common = PptxXml.GetOrCreateChild(slideView, "p", "cSldViewPr", PptxNamespaces.Presentation);
        SetBooleanAttribute(common, "showGuides", settings.ShowGuides);
        SetBooleanAttribute(common, "snapToGrid", settings.SnapToGrid);
        SetBooleanAttribute(common, "snapToObjects", settings.SnapToObjects);
        if (PptxXml.FindFirstChild(common, PptxNamespaces.Presentation, "cViewPr") == null)
        {
            XmlElement commonView = document.CreateElement("p", "cViewPr", PptxNamespaces.Presentation);
            common.InsertBefore(commonView, common.FirstChild);
        }

        WriteGuides(document, common, settings);
        part.SetXmlDocument(document);
    }

    internal static void WriteGridSpacing(XmlDocument document, XmlElement root, PptxPresentationViewSettings settings)
    {
        XmlElement? existing = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "gridSpacing");
        if (settings.GridSpacingX == 0L && settings.GridSpacingY == 0L)
        {
            if (existing != null)
            {
                root.RemoveChild(existing);
            }

            return;
        }

        XmlElement? grid = existing;
        if (grid == null)
        {
            grid = document.CreateElement("p", "gridSpacing", PptxNamespaces.Presentation);
            XmlElement? extensionList = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "extLst");
            root.InsertBefore(grid, extensionList);
        }

        PptxXml.SetAttributeValue(grid, "", "", "cx", settings.GridSpacingX.ToString(CultureInfo.InvariantCulture));
        PptxXml.SetAttributeValue(grid, "", "", "cy", settings.GridSpacingY.ToString(CultureInfo.InvariantCulture));
    }

    internal static void WriteGuides(XmlDocument document, XmlElement common, PptxPresentationViewSettings settings)
    {
        XmlElement? oldList = PptxXml.FindFirstChild(common, PptxNamespaces.Presentation, "guideLst");
        if (oldList != null)
        {
            common.RemoveChild(oldList);
        }

        if (settings.Guides.Count == 0)
        {
            return;
        }

        XmlElement list = document.CreateElement("p", "guideLst", PptxNamespaces.Presentation);
        for (int index = 0; index < settings.Guides.Count; index++)
        {
            PptxPresentationGuide value = settings.Guides[index];
            XmlElement guide = document.CreateElement("p", "guide", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(guide, "", "", "orient", value.Orientation);
            PptxXml.SetAttributeValue(guide, "", "", "pos", value.Position.ToString(CultureInfo.InvariantCulture));
            list.AppendChild(guide);
        }

        common.AppendChild(list);
    }

    internal static void WriteShowSettings(PptxPresentation presentation, PptxPresentationSettings settings)
    {
        PptxPart part = EnsurePresentationPropertiesPart(presentation);
        XmlDocument document = presentation.GetPartXmlDocument(part);
        XmlElement root = GetOrCreateRoot(document, "presentationPr");
        XmlElement showProperties = GetOrCreateOrderedChild(document, root, "showPr", "extLst");
        SetBooleanAttribute(showProperties, "loop", settings.Loop);
        SetBooleanAttribute(showProperties, "useTimings", settings.UseTimings);
        part.SetXmlDocument(document);
        if (presentation.Properties != null)
        {
            presentation.Properties.Load();
        }
    }

    internal static void WriteSlideNumberSettings(PptxPresentation presentation, bool showSlideNumbers)
    {
        for (int index = 0; index < presentation.SlideMasters.Count; index++)
        {
            WriteHeaderFooterSlideNumber(presentation, presentation.SlideMasters[index].Part, showSlideNumbers);
        }

        for (int index = 0; index < presentation.SlideLayouts.Count; index++)
        {
            WriteHeaderFooterSlideNumber(presentation, presentation.SlideLayouts[index].Part, showSlideNumbers);
        }
    }

    internal static void WriteHeaderFooterSlideNumber(PptxPresentation presentation, PptxPart part, bool showSlideNumbers)
    {
        XmlDocument document = presentation.GetPartXmlDocument(part);
        XmlElement root = GetOrCreateRoot(document, "sldMaster");
        XmlElement headerFooter = GetOrCreateOrderedChild(document, root, "hf", "txStyles");
        SetBooleanAttribute(headerFooter, "sldNum", showSlideNumbers);
        part.SetXmlDocument(document);
    }

    internal static void WriteSlideTimings(PptxPresentation presentation, PptxPresentationSettings settings)
    {
        for (int index = 0; index < settings.SlideTimings.Count; index++)
        {
            PptxSlideTimingSettings timing = settings.SlideTimings[index];
            PptxSlide? slide = presentation.FindSlideById(timing.SlideId);
            if (slide != null)
            {
                string advanceAfterTime = "";
                if (timing.AdvanceAfter)
                {
                    advanceAfterTime = timing.AdvanceAfterMilliseconds.ToString(CultureInfo.InvariantCulture);
                }

                slide.GetTransition().SetAdvance(timing.AdvanceOnClick, advanceAfterTime);
            }
        }
    }

    internal static PptxPart EnsureViewPropertiesPart(PptxPresentation presentation)
    {
        if (presentation.ViewPropertiesPart != null)
        {
            return presentation.ViewPropertiesPart;
        }

        PptxPart part;
        if (presentation.Package.HasPart(PptxPartNames.ViewProperties))
        {
            part = presentation.Package.GetPart(PptxPartNames.ViewProperties);
        }
        else
        {
            string xml = "<p:viewPr xmlns:p=\"" + PptxNamespaces.Presentation + "\"><p:normalViewPr/></p:viewPr>";
            part = presentation.Package.AddPart(PptxPartNames.ViewProperties, PptxContentTypes.ViewProperties, Encoding.UTF8.GetBytes(xml));
        }

        EnsurePresentationRelationship(presentation, PptxRelationshipTypes.ViewProperties, "viewProps.xml");
        presentation.ViewPropertiesPart = part;
        return part;
    }

    internal static PptxPart EnsurePresentationPropertiesPart(PptxPresentation presentation)
    {
        if (presentation.Properties != null)
        {
            return presentation.Properties.Part;
        }

        PptxPart part;
        if (presentation.Package.HasPart(PptxPartNames.PresentationProperties))
        {
            part = presentation.Package.GetPart(PptxPartNames.PresentationProperties);
        }
        else
        {
            string xml = "<p:presentationPr xmlns:p=\"" + PptxNamespaces.Presentation + "\"/>";
            part = presentation.Package.AddPart(PptxPartNames.PresentationProperties, PptxContentTypes.PresentationProperties, Encoding.UTF8.GetBytes(xml));
        }

        PptxRelationship relationship = EnsurePresentationRelationship(presentation, PptxRelationshipTypes.PresentationProperties, "presProps.xml");
        presentation.Properties = new PptxPresentationProperties(part, relationship.Id);
        return part;
    }

    internal static PptxRelationship EnsurePresentationRelationship(PptxPresentation presentation, string type, string target)
    {
        if (presentation.Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        for (int index = 0; index < presentation.Part.Relationships.Relationships.Count; index++)
        {
            PptxRelationship existing = presentation.Part.Relationships.Relationships[index];
            if (existing.Type == type)
            {
                return existing;
            }
        }

        return presentation.Package.AddRelationship(presentation.Part.PartName, type, target, PptxRelationshipTargetMode.Internal);
    }

    internal static XmlElement GetOrCreateRoot(XmlDocument document, string localName)
    {
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            root = document.CreateElement("p", localName, PptxNamespaces.Presentation);
            document.AppendChild(root);
        }

        return root;
    }

    internal static XmlElement GetOrCreateOrderedChild(XmlDocument document, XmlElement parent, string localName, string beforeLocalName)
    {
        XmlElement? existing = PptxXml.FindFirstChild(parent, PptxNamespaces.Presentation, localName);
        if (existing != null)
        {
            return existing;
        }

        XmlElement created = document.CreateElement("p", localName, PptxNamespaces.Presentation);
        XmlElement? before = PptxXml.FindFirstChild(parent, PptxNamespaces.Presentation, beforeLocalName);
        parent.InsertBefore(created, before);
        return created;
    }

    internal static void SetTextAttribute(XmlElement element, string localName, string value)
    {
        string clean = PptxDiagnostic.CleanText(value);
        if (clean.Length == 0)
        {
            element.RemoveAttribute(localName);
        }
        else
        {
            PptxXml.SetAttributeValue(element, "", "", localName, clean);
        }
    }

    internal static void SetBooleanAttribute(XmlElement element, string localName, bool value)
    {
        if (value)
        {
            PptxXml.SetAttributeValue(element, "", "", localName, "1");
        }
        else
        {
            PptxXml.SetAttributeValue(element, "", "", localName, "0");
        }
    }
}
