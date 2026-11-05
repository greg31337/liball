using System.Xml;

namespace pptx;

/// <summary>
/// Represents slide transition data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxSlideTransition
{
    internal PptxSlide Slide;
    internal XmlElement? Element;
    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the speed associated with this object.
    /// </summary>
    public string Speed;
    /// <summary>
    /// Stores the direction associated with this object.
    /// </summary>
    public string Direction;
    /// <summary>
    /// Indicates whether the advance on click setting is enabled.
    /// </summary>
    public bool AdvanceOnClick;
    /// <summary>
    /// Indicates whether the advance after setting is enabled.
    /// </summary>
    public bool AdvanceAfter;
    /// <summary>
    /// Stores the advance after time associated with this object.
    /// </summary>
    public string AdvanceAfterTime;
    /// <summary>
    /// Stores the timed slide advance in milliseconds, or zero when timed advance is disabled.
    /// </summary>
    public long AdvanceAfterMilliseconds;
    internal string RawXml;
    internal PptxSlideTransition(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        Slide = slide;
        Element = null;
        Type = "";
        Speed = "";
        Direction = "";
        AdvanceOnClick = true;
        AdvanceAfter = false;
        AdvanceAfterTime = "";
        AdvanceAfterMilliseconds = 0L;
        RawXml = "";
        Load();
    }

    internal void Load()
    {
        Element = FindTransitionElement();
        Type = "";
        Speed = "";
        Direction = "";
        AdvanceOnClick = true;
        AdvanceAfter = false;
        AdvanceAfterTime = "";
        AdvanceAfterMilliseconds = 0L;
        RawXml = "";
        if (Element == null)
        {
            return;
        }

        Speed = PptxXml.GetAttributeValue(Element, "", "spd");
        AdvanceOnClick = PptxXml.GetBoolAttribute(Element, "", "advClick", true);
        AdvanceAfterTime = PptxXml.GetAttributeValue(Element, "", "advTm");
        AdvanceAfter = AdvanceAfterTime.Length > 0;
        AdvanceAfterMilliseconds = PptxPresentation.GetLongAttribute(Element, "advTm", 0L);
        RawXml = Element.OuterXml;
        XmlElement? choice = FindTransitionChoiceElement(Element);
        if (choice != null)
        {
            Type = choice.LocalName;
            Direction = ReadDirection(choice);
        }
    }
    internal bool HasTransition()
    {
        return Element != null;
    }
    internal void SetType(string transitionType)
    {
        XmlElement transition = GetOrCreateTransitionElement();
        RemoveTransitionChoiceElements(transition);
        string clean = PptxDiagnostic.CleanText(transitionType);
        if (clean.Length > 0 && clean != "none")
        {
            XmlDocument document = PptxTextFrame.GetOwnerDocument(transition);
            XmlElement choice = document.CreateElement("p", clean, PptxNamespaces.Presentation);
            transition.InsertBefore(choice, transition.FirstChild);
        }

        MarkDirty(transition);
        Load();
    }
    internal void SetSpeed(string speed)
    {
        XmlElement transition = GetOrCreateTransitionElement();
        SetAttributeOrRemove(transition, "spd", PptxDiagnostic.CleanText(speed));
        MarkDirty(transition);
        Load();
    }
    internal void SetDirection(string direction)
    {
        XmlElement transition = GetOrCreateTransitionElement();
        XmlElement? choice = FindTransitionChoiceElement(transition);
        if (choice == null)
        {
            SetType("wipe");
            transition = GetOrCreateTransitionElement();
            choice = FindTransitionChoiceElement(transition);
        }

        if (choice != null)
        {
            SetAttributeOrRemove(choice, "dir", PptxDiagnostic.CleanText(direction));
        }

        MarkDirty(transition);
        Load();
    }
    internal void SetAdvance(bool advanceOnClick, string advanceAfterTime)
    {
        XmlElement transition = GetOrCreateTransitionElement();
        if (advanceOnClick)
        {
            PptxXml.SetAttributeValue(transition, "", "", "advClick", "1");
        }
        else
        {
            PptxXml.SetAttributeValue(transition, "", "", "advClick", "0");
        }

        SetAttributeOrRemove(transition, "advTm", PptxDiagnostic.CleanText(advanceAfterTime));
        MarkDirty(transition);
        Load();
    }
    internal void SetAdvanceOnClick(bool advanceOnClick)
    {
        SetAdvance(advanceOnClick, AdvanceAfterTime);
    }
    internal void SetAdvanceAfterMilliseconds(long milliseconds)
    {
        if (milliseconds < 0L)
        {
            throw new System.ArgumentOutOfRangeException("milliseconds");
        }

        SetAdvance(AdvanceOnClick, milliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    internal void ClearAdvanceAfter()
    {
        SetAdvance(AdvanceOnClick, "");
    }
    internal void Remove()
    {
        XmlElement? transition = FindTransitionElement();
        if (transition == null)
        {
            return;
        }

        XmlNode? parent = transition.ParentNode;
        if (parent != null)
        {
            parent.RemoveChild(transition);
            MarkDirty(parent);
        }
    }

    internal XmlElement? FindTransitionElement()
    {
        XmlDocument document = Slide.GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return null;
        }

        return PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "transition");
    }

    internal XmlElement GetOrCreateTransitionElement()
    {
        XmlDocument document = Slide.GetXmlDocument();
        XmlElement root = Slide.GetSlideRoot(document);
        XmlElement? transition = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "transition");
        if (transition != null)
        {
            return transition;
        }

        transition = document.CreateElement("p", "transition", PptxNamespaces.Presentation);
        XmlElement? timing = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "timing");
        if (timing != null)
        {
            root.InsertBefore(transition, timing);
            return transition;
        }

        XmlElement? extensionList = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "extLst");
        if (extensionList != null)
        {
            root.InsertBefore(transition, extensionList);
            return transition;
        }

        XmlElement? colorMap = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "clrMapOvr");
        if (colorMap != null)
        {
            root.InsertAfter(transition, colorMap);
            return transition;
        }

        root.AppendChild(transition);
        return transition;
    }

    internal static XmlElement? FindTransitionChoiceElement(XmlElement transition)
    {
        for (int index = 0; index < transition.ChildNodes.Count; index++)
        {
            XmlNode? child = transition.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (IsTransitionChoiceElement(element))
            {
                return element;
            }
        }

        return null;
    }

    internal static void RemoveTransitionChoiceElements(XmlElement transition)
    {
        for (int index = transition.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = transition.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (IsTransitionChoiceElement(element))
            {
                transition.RemoveChild(element);
            }
        }
    }

    internal static bool IsTransitionChoiceElement(XmlElement element)
    {
        if (element.NamespaceURI != PptxNamespaces.Presentation)
        {
            return false;
        }

        string name = element.LocalName;
        if (name == "fade" || name == "push" || name == "wipe" || name == "split" || name == "cover" || name == "pull")
        {
            return true;
        }

        if (name == "cut" || name == "blinds" || name == "checker" || name == "comb" || name == "diamond" || name == "dissolve")
        {
            return true;
        }

        if (name == "newsflash" || name == "plus" || name == "random" || name == "randomBar" || name == "strips")
        {
            return true;
        }

        if (name == "wedge" || name == "wheel" || name == "zoom" || name == "flash" || name == "warp")
        {
            return true;
        }

        if (name == "flythrough" || name == "gallery" || name == "glitter" || name == "honeycomb" || name == "pan")
        {
            return true;
        }

        if (name == "prestige" || name == "reveal" || name == "ripple" || name == "shred" || name == "switch" || name == "vortex")
        {
            return true;
        }

        return false;
    }

    internal static string ReadDirection(XmlElement choice)
    {
        string direction = PptxXml.GetAttributeValue(choice, "", "dir");
        if (direction.Length > 0)
        {
            return direction;
        }

        direction = PptxXml.GetAttributeValue(choice, "", "orient");
        if (direction.Length > 0)
        {
            return direction;
        }

        return PptxXml.GetAttributeValue(choice, "", "thruBlk");
    }

    internal static void SetAttributeOrRemove(XmlElement element, string localName, string value)
    {
        if (value.Length == 0)
        {
            element.RemoveAttribute(localName);
            return;
        }

        PptxXml.SetAttributeValue(element, "", "", localName, value);
    }

    internal void MarkDirty(XmlNode node)
    {
        XmlDocument? document = node.OwnerDocument;
        if (node.NodeType == XmlNodeType.Document)
        {
            document = (XmlDocument)node;
        }

        if (document != null)
        {
            Slide.Part.SetXmlDocument(document);
        }
    }
}
