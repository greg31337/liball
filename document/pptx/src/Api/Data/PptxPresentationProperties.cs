using System.Globalization;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents presentation properties data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxPresentationProperties
{
    internal PptxPart Part;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>Indicates whether the slide show loops after its final slide.</summary>
    public bool Loop;
    /// <summary>Indicates whether animations are shown during the slide show.</summary>
    public bool ShowAnimation;
    /// <summary>Indicates whether narration is played during the slide show.</summary>
    public bool ShowNarration;
    /// <summary>Indicates whether recorded slide timings are used.</summary>
    public bool UseTimings;
    /// <summary>Stores <c>present</c>, <c>browse</c>, or <c>kiosk</c>.</summary>
    public string ShowMode;
    /// <summary>Indicates whether the show includes all slides.</summary>
    public bool ShowAllSlides;
    /// <summary>Stores the first slide number in a restricted slide range.</summary>
    public int SlideRangeStart;
    /// <summary>Stores the final slide number in a restricted slide range.</summary>
    public int SlideRangeEnd;
    /// <summary>Stores the custom show identifier, or zero when no custom show is selected.</summary>
    public int CustomShowId;

    internal PptxPresentationProperties(PptxPart part, string relationshipId)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        Part = part;
        PartName = part.PartName;
        RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        Loop = false;
        ShowAnimation = true;
        ShowNarration = true;
        UseTimings = true;
        ShowMode = "present";
        ShowAllSlides = true;
        SlideRangeStart = 0;
        SlideRangeEnd = 0;
        CustomShowId = 0;
        Load();
    }

    internal void Load()
    {
        XmlDocument document = PptxXml.LoadBytes(GetCurrentBytes());
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        XmlElement? showProperties = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "showPr");
        if (showProperties == null)
        {
            return;
        }

        Loop = PptxXml.GetBoolAttribute(showProperties, "", "loop", false);
        ShowAnimation = PptxXml.GetBoolAttribute(showProperties, "", "showAnimation", true);
        ShowNarration = PptxXml.GetBoolAttribute(showProperties, "", "showNarration", true);
        UseTimings = PptxXml.GetBoolAttribute(showProperties, "", "useTimings", true);
        ReadShowMode(showProperties);
        ReadSlideScope(showProperties);
    }

    internal void Save()
    {
        if (ShowMode != "present" && ShowMode != "browse" && ShowMode != "kiosk")
        {
            throw new System.ArgumentException("Show mode must be present, browse, or kiosk.");
        }

        if (!ShowAllSlides && CustomShowId <= 0 && (SlideRangeStart <= 0 || SlideRangeEnd < SlideRangeStart))
        {
            throw new System.ArgumentException("A valid slide range or custom show identifier is required.");
        }

        XmlDocument document = PptxXml.LoadBytes(GetCurrentBytes());
        XmlElement? rootValue = document.DocumentElement;
        if (rootValue == null)
        {
            throw new System.ArgumentException("Presentation properties XML root is required.");
        }

        XmlElement root = rootValue;
        XmlElement showProperties = PptxPresentationSettingsCore.GetOrCreateOrderedChild(document, root, "showPr", "extLst");
        SetBooleanAttribute(showProperties, "loop", Loop);
        SetBooleanAttribute(showProperties, "showAnimation", ShowAnimation);
        SetBooleanAttribute(showProperties, "showNarration", ShowNarration);
        SetBooleanAttribute(showProperties, "useTimings", UseTimings);
        RemoveKnownShowChildren(showProperties);
        XmlNode? insertionPoint = showProperties.FirstChild;
        XmlElement mode = document.CreateElement("p", ShowMode, PptxNamespaces.Presentation);
        showProperties.InsertBefore(mode, insertionPoint);
        XmlElement scope = CreateSlideScope(document);
        showProperties.InsertBefore(scope, insertionPoint);
        Part.SetXmlDocument(document);
    }
    internal void SetLoop(bool loop)
    {
        Loop = loop;
        Save();
    }
    internal void SetUseTimings(bool useTimings)
    {
        UseTimings = useTimings;
        Save();
    }

    internal void ReadShowMode(XmlElement showProperties)
    {
        ShowMode = "present";
        if (PptxXml.FindFirstChild(showProperties, PptxNamespaces.Presentation, "browse") != null)
        {
            ShowMode = "browse";
        }

        if (PptxXml.FindFirstChild(showProperties, PptxNamespaces.Presentation, "kiosk") != null)
        {
            ShowMode = "kiosk";
        }
    }

    internal void ReadSlideScope(XmlElement showProperties)
    {
        ShowAllSlides = true;
        SlideRangeStart = 0;
        SlideRangeEnd = 0;
        CustomShowId = 0;
        XmlElement? range = PptxXml.FindFirstChild(showProperties, PptxNamespaces.Presentation, "sldRg");
        if (range != null)
        {
            ShowAllSlides = false;
            SlideRangeStart = PptxXml.GetIntAttribute(range, "", "st", 0);
            SlideRangeEnd = PptxXml.GetIntAttribute(range, "", "end", 0);
        }

        XmlElement? customShow = PptxXml.FindFirstChild(showProperties, PptxNamespaces.Presentation, "custShow");
        if (customShow != null)
        {
            ShowAllSlides = false;
            CustomShowId = PptxXml.GetIntAttribute(customShow, "", "id", 0);
        }
    }

    internal XmlElement CreateSlideScope(XmlDocument document)
    {
        if (ShowAllSlides)
        {
            return document.CreateElement("p", "sldAll", PptxNamespaces.Presentation);
        }

        if (CustomShowId > 0)
        {
            XmlElement customShow = document.CreateElement("p", "custShow", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(customShow, "", "", "id", CustomShowId.ToString(CultureInfo.InvariantCulture));
            return customShow;
        }

        XmlElement range = document.CreateElement("p", "sldRg", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(range, "", "", "st", SlideRangeStart.ToString(CultureInfo.InvariantCulture));
        PptxXml.SetAttributeValue(range, "", "", "end", SlideRangeEnd.ToString(CultureInfo.InvariantCulture));
        return range;
    }

    internal void RemoveKnownShowChildren(XmlElement showProperties)
    {
        for (int index = showProperties.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = showProperties.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && IsKnownShowChild(element.LocalName))
            {
                showProperties.RemoveChild(element);
            }
        }
    }

    internal bool IsKnownShowChild(string localName)
    {
        return localName == "present" || localName == "browse" || localName == "kiosk" || localName == "sldAll" || localName == "sldRg" || localName == "custShow";
    }

    internal byte[] GetCurrentBytes()
    {
        if (Part.HasXmlDocument)
        {
            return PptxXml.WriteBytes(Part.XmlDocument);
        }

        return Part.GetBytes();
    }

    internal void SetBooleanAttribute(XmlElement element, string localName, bool value)
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
