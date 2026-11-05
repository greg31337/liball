using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents animation effect data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxAnimationEffect
{
    internal PptxSlide Slide;
    internal XmlElement Element;
    /// <summary>
    /// Stores the node type associated with this object.
    /// </summary>
    public string NodeType;
    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the effect associated with this object.
    /// </summary>
    public string Effect;
    /// <summary>
    /// Stores the transition associated with this object.
    /// </summary>
    public string Transition;
    /// <summary>
    /// Stores the target shape id used to identify or connect related presentation data.
    /// </summary>
    public string TargetShapeId;
    /// <summary>
    /// Stores the timing node id used to identify or connect related presentation data.
    /// </summary>
    public string TimingNodeId;
    /// <summary>
    /// Stores the duration associated with this object.
    /// </summary>
    public string Duration;
    /// <summary>
    /// Stores the delay associated with this object.
    /// </summary>
    public string Delay;
    /// <summary>
    /// Stores the repeat count associated with this object.
    /// </summary>
    public string RepeatCount;
    /// <summary>
    /// Stores the trigger associated with this object.
    /// </summary>
    public string Trigger;
    /// <summary>
    /// Stores the path associated with this object.
    /// </summary>
    public string Path;
    internal string RawXml;
    internal PptxAnimationEffect(PptxSlide slide, XmlElement element)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        Slide = slide;
        Element = element;
        NodeType = "";
        Type = "";
        Effect = "";
        Transition = "";
        TargetShapeId = "";
        TimingNodeId = "";
        Duration = "";
        Delay = "";
        RepeatCount = "";
        Trigger = "";
        Path = "";
        RawXml = "";
        Load();
    }

    internal void Load()
    {
        NodeType = Element.LocalName;
        RawXml = Element.OuterXml;
        TargetShapeId = FindTargetShapeId(Element);
        Transition = PptxXml.GetAttributeValue(Element, "", "transition");
        Path = PptxXml.GetAttributeValue(Element, "", "path");
        Effect = ReadEffectName(Element);
        Type = ClassifyEffect(Element);
        LoadTimingAttributes();
    }

    internal void LoadTimingAttributes()
    {
        XmlElement? timing = FindNearestTimingNode(Element);
        if (timing == null)
        {
            timing = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cTn");
        }

        TimingNodeId = "";
        Duration = "";
        Delay = "";
        RepeatCount = "";
        Trigger = "";
        if (timing == null)
        {
            return;
        }

        TimingNodeId = PptxXml.GetAttributeValue(timing, "", "id");
        Duration = PptxXml.GetAttributeValue(timing, "", "dur");
        Delay = PptxXml.GetAttributeValue(timing, "", "delay");
        RepeatCount = PptxXml.GetAttributeValue(timing, "", "repeatCount");
        XmlElement? condition = FindStartCondition(timing);
        if (condition != null)
        {
            Trigger = PptxXml.GetAttributeValue(condition, "", "evt");
            if (Delay.Length == 0)
            {
                Delay = PptxXml.GetAttributeValue(condition, "", "delay");
            }
        }

        string sequenceTrigger = ReadSequenceTrigger(timing);
        if (sequenceTrigger.Length > 0)
        {
            Trigger = sequenceTrigger;
        }
    }
    internal void SetTiming(string trigger, string duration, string delay, string repeatCount)
    {
        XmlElement timing = GetOrCreateTimingNode();
        SetAttributeOrRemove(timing, "dur", PptxDiagnostic.CleanText(duration));
        SetAttributeOrRemove(timing, "repeatCount", PptxDiagnostic.CleanText(repeatCount));
        XmlElement condition = GetOrCreateStartCondition(timing);
        SetTriggerAttributes(timing, condition, PptxDiagnostic.CleanText(trigger));
        SetAttributeOrRemove(condition, "delay", PptxDiagnostic.CleanText(delay));
        MarkDirty();
        Load();
    }
    internal void SetTrigger(string trigger)
    {
        SetTiming(PptxDiagnostic.CleanText(trigger), Duration, Delay, RepeatCount);
    }
    internal void SetDuration(string duration)
    {
        SetTiming(Trigger, PptxDiagnostic.CleanText(duration), Delay, RepeatCount);
    }
    internal void SetDelay(string delay)
    {
        SetTiming(Trigger, Duration, PptxDiagnostic.CleanText(delay), RepeatCount);
    }
    internal void SetRepeatCount(string repeatCount)
    {
        SetTiming(Trigger, Duration, Delay, PptxDiagnostic.CleanText(repeatCount));
    }
    internal void SetTargetShapeId(string shapeId)
    {
        XmlElement target = GetOrCreateShapeTarget();
        PptxXml.SetAttributeValue(target, "", "", "spid", PptxDiagnostic.CleanText(shapeId));
        MarkDirty();
        Load();
    }
    internal void SetEffect(string type, string effect)
    {
        string cleanType = PptxDiagnostic.CleanText(type);
        string cleanEffect = PptxDiagnostic.CleanText(effect);
        ValidateEffectType(cleanType);
        if (cleanEffect.Length == 0)
        {
            throw new System.ArgumentException("Animation effect is required.");
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        XmlElement replacement = document.CreateElement("p", GetElementNameForType(cleanType), PptxNamespaces.Presentation);
        CopyUnrelatedAttributes(Element, replacement);
        while (Element.FirstChild != null)
        {
            replacement.AppendChild(Element.FirstChild);
        }

        ApplyEffectAttributes(replacement, cleanType, cleanEffect);
        XmlNode? parent = Element.ParentNode;
        if (parent == null)
        {
            throw new System.ArgumentException("Animation effect parent was not found.");
        }

        parent.ReplaceChild(replacement, Element);
        Element = replacement;
        MarkDirty();
        Load();
    }
    internal void MoveTo(int index)
    {
        Slide.MoveAnimationEffect(this, index);
    }
    internal void Remove()
    {
        XmlElement? container = GetContainerElement();
        if (container == null)
        {
            container = Element;
        }

        XmlNode? parent = container.ParentNode;
        if (parent != null)
        {
            parent.RemoveChild(container);
            MarkDirty();
        }
    }

    internal XmlElement GetOrCreateTimingNode()
    {
        XmlElement? timing = FindNearestTimingNode(Element);
        if (timing != null)
        {
            return timing;
        }

        timing = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cTn");
        if (timing != null)
        {
            return timing;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        timing = document.CreateElement("p", "cTn", PptxNamespaces.Presentation);
        Element.InsertBefore(timing, Element.FirstChild);
        return timing;
    }

    internal XmlElement GetOrCreateStartCondition(XmlElement timing)
    {
        XmlElement? condition = FindStartCondition(timing);
        if (condition != null)
        {
            return condition;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(timing);
        XmlElement? conditionList = PptxXml.FindFirstChild(timing, PptxNamespaces.Presentation, "stCondLst");
        if (conditionList == null)
        {
            conditionList = document.CreateElement("p", "stCondLst", PptxNamespaces.Presentation);
            timing.InsertBefore(conditionList, timing.FirstChild);
        }

        condition = document.CreateElement("p", "cond", PptxNamespaces.Presentation);
        conditionList.AppendChild(condition);
        return condition;
    }

    internal static XmlElement? FindStartCondition(XmlElement timing)
    {
        XmlElement? conditionList = PptxXml.FindFirstChild(timing, PptxNamespaces.Presentation, "stCondLst");
        if (conditionList == null)
        {
            return null;
        }

        return PptxXml.FindFirstChild(conditionList, PptxNamespaces.Presentation, "cond");
    }

    internal XmlElement GetOrCreateShapeTarget()
    {
        XmlElement? target = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Presentation, "spTgt");
        if (target != null)
        {
            return target;
        }

        XmlDocument document = PptxTextFrame.GetOwnerDocument(Element);
        XmlElement? behavior = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cBhvr");
        if (behavior == null)
        {
            behavior = document.CreateElement("p", "cBhvr", PptxNamespaces.Presentation);
            Element.AppendChild(behavior);
        }

        XmlElement? targetElement = PptxXml.FindFirstChild(behavior, PptxNamespaces.Presentation, "tgtEl");
        if (targetElement == null)
        {
            targetElement = document.CreateElement("p", "tgtEl", PptxNamespaces.Presentation);
            behavior.AppendChild(targetElement);
        }

        target = document.CreateElement("p", "spTgt", PptxNamespaces.Presentation);
        targetElement.AppendChild(target);
        return target;
    }

    internal XmlElement? GetContainerElement()
    {
        XmlNode? parent = Element.ParentNode;
        while (parent != null)
        {
            if (parent.NodeType == XmlNodeType.Element)
            {
                XmlElement parentElement = (XmlElement)parent;
                if (parentElement.NamespaceURI == PptxNamespaces.Presentation && parentElement.LocalName == "par")
                {
                    return parentElement;
                }
            }

            parent = parent.ParentNode;
        }

        return null;
    }

    internal void MarkDirty()
    {
        Slide.Part.SetXmlDocument(PptxTextFrame.GetOwnerDocument(Element));
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

    internal static void SetTriggerAttributes(XmlElement timing, XmlElement condition, string trigger)
    {
        if (trigger == PptxAnimationTrigger.OnClick)
        {
            PptxXml.SetAttributeValue(timing, "", "", "nodeType", "clickEffect");
            PptxXml.SetAttributeValue(condition, "", "", "evt", "onClick");
            return;
        }

        if (trigger == PptxAnimationTrigger.WithPrevious)
        {
            PptxXml.SetAttributeValue(timing, "", "", "nodeType", "withEffect");
            PptxXml.SetAttributeValue(condition, "", "", "evt", "onBegin");
            return;
        }

        if (trigger == PptxAnimationTrigger.AfterPrevious)
        {
            PptxXml.SetAttributeValue(timing, "", "", "nodeType", "afterEffect");
            PptxXml.SetAttributeValue(condition, "", "", "evt", "onBegin");
            return;
        }

        string nodeType = PptxXml.GetAttributeValue(timing, "", "nodeType");
        if (nodeType == "clickEffect" || nodeType == "withEffect" || nodeType == "afterEffect")
        {
            timing.RemoveAttribute("nodeType");
        }

        SetAttributeOrRemove(condition, "evt", trigger);
    }

    internal static string ReadSequenceTrigger(XmlElement timing)
    {
        string nodeType = PptxXml.GetAttributeValue(timing, "", "nodeType");
        if (nodeType == "clickEffect")
        {
            return PptxAnimationTrigger.OnClick;
        }

        if (nodeType == "withEffect")
        {
            return PptxAnimationTrigger.WithPrevious;
        }

        if (nodeType == "afterEffect")
        {
            return PptxAnimationTrigger.AfterPrevious;
        }

        return "";
    }

    internal static void ValidateEffectType(string type)
    {
        if (type != PptxAnimationType.Entrance && type != PptxAnimationType.Emphasis && type != PptxAnimationType.Exit && type != PptxAnimationType.MotionPath && type != PptxAnimationType.Media)
        {
            throw new System.ArgumentException("Animation type must be entrance, emphasis, exit, motion, or media.");
        }
    }

    internal static string GetElementNameForType(string type)
    {
        if (type == PptxAnimationType.MotionPath)
        {
            return "animMotion";
        }

        if (type == PptxAnimationType.Media)
        {
            return "cmd";
        }

        return "animEffect";
    }

    internal static void ApplyEffectAttributes(XmlElement element, string type, string effect)
    {
        if (type == PptxAnimationType.MotionPath)
        {
            PptxXml.SetAttributeValue(element, "", "", "path", effect);
            return;
        }

        if (type == PptxAnimationType.Media)
        {
            PptxXml.SetAttributeValue(element, "", "", "type", "call");
            PptxXml.SetAttributeValue(element, "", "", "cmd", effect);
            return;
        }

        string transition = "none";
        if (type == PptxAnimationType.Entrance)
        {
            transition = "in";
        }
        else if (type == PptxAnimationType.Exit)
        {
            transition = "out";
        }

        PptxXml.SetAttributeValue(element, "", "", "transition", transition);
        PptxXml.SetAttributeValue(element, "", "", "filter", effect);
    }

    internal static void CopyUnrelatedAttributes(XmlElement source, XmlElement target)
    {
        for (int index = 0; index < source.Attributes.Count; index++)
        {
            XmlAttribute? attribute = source.Attributes[index];
            if (attribute != null && !IsEffectAttribute(attribute.LocalName))
            {
                XmlAttribute copy = (XmlAttribute)attribute.CloneNode(true);
                target.Attributes.Append(copy);
            }
        }
    }

    internal static bool IsEffectAttribute(string localName)
    {
        return localName == "transition" || localName == "filter" || localName == "path" || localName == "type" || localName == "cmd";
    }

    internal static List<PptxAnimationEffect> FromSlide(PptxSlide slide)
    {
        List<PptxAnimationEffect> effects = new List<PptxAnimationEffect>();
        XmlDocument document = slide.GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return effects;
        }

        XmlElement? timing = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "timing");
        if (timing == null)
        {
            return effects;
        }

        AddAnimationElements(slide, timing, effects);
        return effects;
    }

    internal static void AddAnimationElements(PptxSlide slide, XmlNode node, List<PptxAnimationEffect> effects)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (IsAnimationElement(element))
            {
                effects.Add(new PptxAnimationEffect(slide, element));
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                AddAnimationElements(slide, child, effects);
            }
        }
    }

    internal static bool IsAnimationElement(XmlElement element)
    {
        if (element.NamespaceURI != PptxNamespaces.Presentation)
        {
            return false;
        }

        if (element.LocalName == "animEffect" || element.LocalName == "animMotion" || element.LocalName == "animClr")
        {
            return true;
        }

        if (element.LocalName == "animScale" || element.LocalName == "animRot" || element.LocalName == "cmd")
        {
            return true;
        }

        if (element.LocalName == "audio" || element.LocalName == "video" || element.LocalName == "cMediaNode")
        {
            return true;
        }

        return false;
    }

    internal static string ClassifyEffect(XmlElement element)
    {
        if (element.LocalName == "animMotion")
        {
            return "motion";
        }

        if (element.LocalName == "cmd" || element.LocalName == "audio" || element.LocalName == "video" || element.LocalName == "cMediaNode")
        {
            return "media";
        }

        if (element.LocalName == "animEffect")
        {
            string transition = PptxXml.GetAttributeValue(element, "", "transition");
            if (transition == "in")
            {
                return "entrance";
            }

            if (transition == "out")
            {
                return "exit";
            }
        }

        if (element.LocalName == "animClr" || element.LocalName == "animScale" || element.LocalName == "animRot")
        {
            return "emphasis";
        }

        return "emphasis";
    }

    internal static string ReadEffectName(XmlElement element)
    {
        string filter = PptxXml.GetAttributeValue(element, "", "filter");
        if (filter.Length > 0)
        {
            return filter;
        }

        string cmd = PptxXml.GetAttributeValue(element, "", "cmd");
        if (cmd.Length > 0)
        {
            return cmd;
        }

        if (element.LocalName == "animMotion")
        {
            return "motionPath";
        }

        return element.LocalName;
    }

    internal static string FindTargetShapeId(XmlElement element)
    {
        XmlElement? target = PptxXml.FindFirstDescendant(element, PptxNamespaces.Presentation, "spTgt");
        if (target == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(target, "", "spid");
    }

    internal static XmlElement? FindNearestTimingNode(XmlElement element)
    {
        XmlNode? parent = element.ParentNode;
        while (parent != null)
        {
            if (parent.NodeType == XmlNodeType.Element)
            {
                XmlElement parentElement = (XmlElement)parent;
                if (parentElement.NamespaceURI == PptxNamespaces.Presentation && parentElement.LocalName == "cTn")
                {
                    return parentElement;
                }
            }

            parent = parent.ParentNode;
        }

        return null;
    }
}
