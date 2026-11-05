using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents a shape in a slide shape tree and exposes its geometry, text, formatting, media, and actions.
/// </summary>
public class PptxShape
{
    internal const string CustomDataNamespace = "urn:pptx-library:custom-data";
    internal const string CustomDataExtensionUri = "urn:pptx-library:shape-custom-data";
    internal const string BackgroundDataName = "background";
    internal PptxShapeTree Tree;
    internal XmlElement Element;
    /// <summary>
    /// Stores the id used to identify or connect related presentation data.
    /// </summary>
    public int Id;
    /// <summary>
    /// Stores the name associated with this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the element type associated with this object.
    /// </summary>
    public string ElementType;
    /// <summary>
    /// Stores the preset geometry associated with this object.
    /// </summary>
    public string PresetGeometry;
    /// <summary>
    /// Indicates whether the hidden setting is enabled.
    /// </summary>
    public bool Hidden;
    /// <summary>
    /// Stores the alternative text associated with this object.
    /// </summary>
    public string AlternativeText;
    /// <summary>
    /// Stores the title associated with this object.
    /// </summary>
    public string Title;
    /// <summary>
    /// Indicates whether the no move setting is enabled.
    /// </summary>
    public bool NoMove;
    /// <summary>
    /// Indicates whether the no resize setting is enabled.
    /// </summary>
    public bool NoResize;
    /// <summary>
    /// Indicates whether the no rotate setting is enabled.
    /// </summary>
    public bool NoRotate;
    /// <summary>
    /// Indicates whether the no select setting is enabled.
    /// </summary>
    public bool NoSelect;
    /// <summary>
    /// Stores the hyperlink relationship id used to identify or connect related presentation data.
    /// </summary>
    public string HyperlinkRelationshipId;
    /// <summary>
    /// Stores the hyperlink target associated with this object.
    /// </summary>
    public string HyperlinkTarget;
    /// <summary>
    /// Stores the connector type associated with this object.
    /// </summary>
    public string ConnectorType;
    /// <summary>
    /// Stores the start value associated with this object.
    /// </summary>
    public PptxPoint Start;
    /// <summary>
    /// Stores the end value associated with this object.
    /// </summary>
    public PptxPoint End;
    internal PptxShape? ParentGroup;
    /// <summary>
    /// Contains the children in package or presentation order.
    /// </summary>
    public List<PptxShape> Children;
    /// <summary>
    /// Stores the effective bounds value associated with this object.
    /// </summary>
    public PptxBounds EffectiveBounds;
    /// <summary>
    /// Indicates whether the custom geometry setting is enabled.
    /// </summary>
    public bool CustomGeometry;
    internal string CustomGeometryXml;
    /// <summary>
    /// Stores the fill value associated with this object.
    /// </summary>
    public PptxFill Fill;
    /// <summary>
    /// Stores the line value associated with this object.
    /// </summary>
    public PptxLine Line;
    /// <summary>
    /// Stores the effect value associated with this object.
    /// </summary>
    public PptxEffect Effect;
    /// <summary>
    /// Stores the text frame value associated with this object.
    /// </summary>
    public PptxTextFrame TextFrame;

    internal PptxShape(PptxShapeTree tree, XmlElement element)
    {
        if (tree == null)
        {
            throw new System.ArgumentNullException("tree");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        Tree = tree;
        Element = element;
        Id = 0;
        Name = "";
        ElementType = element.LocalName;
        PresetGeometry = "";
        Hidden = false;
        AlternativeText = "";
        Title = "";
        NoMove = false;
        NoResize = false;
        NoRotate = false;
        NoSelect = false;
        HyperlinkRelationshipId = "";
        HyperlinkTarget = "";
        ConnectorType = "";
        Start = new PptxPoint(0L, 0L);
        End = new PptxPoint(0L, 0L);
        ParentGroup = null;
        Children = new List<PptxShape>();
        EffectiveBounds = new PptxBounds(0L, 0L, 0L, 0L);
        CustomGeometry = false;
        CustomGeometryXml = "";
        Fill = PptxFill.CreateNone();
        Line = PptxLine.CreateNone();
        Effect = PptxEffect.CreateNone();
        TextFrame = new PptxTextFrame(this);
        LoadMetadata();
        LoadChildren();
        RefreshEffectiveBounds();
    }

    internal void LoadMetadata()
    {
        Id = 0;
        Name = "";
        ElementType = Element.LocalName;
        PresetGeometry = "";
        Hidden = false;
        AlternativeText = "";
        Title = "";
        NoMove = false;
        NoResize = false;
        NoRotate = false;
        NoSelect = false;
        HyperlinkRelationshipId = "";
        HyperlinkTarget = "";
        ConnectorType = "";
        Start = new PptxPoint(0L, 0L);
        End = new PptxPoint(0L, 0L);
        CustomGeometry = false;
        CustomGeometryXml = "";
        Fill = PptxFill.CreateNone();
        Line = PptxLine.CreateNone();
        Effect = PptxEffect.CreateNone();

        XmlElement? properties = Tree.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvPr");
        if (properties != null)
        {
            Id = PptxXml.GetIntAttribute(properties, "", "id", 0);
            Name = PptxXml.GetAttributeValue(properties, "", "name");
            Hidden = PptxXml.GetBoolAttribute(properties, "", "hidden", false);
            AlternativeText = PptxXml.GetAttributeValue(properties, "", "descr");
            Title = PptxXml.GetAttributeValue(properties, "", "title");

            XmlElement? hyperlink = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "hlinkClick");
            if (hyperlink != null)
            {
                HyperlinkRelationshipId = PptxXml.GetAttributeValue(hyperlink, PptxNamespaces.Relationships, "id");
                PptxRelationship? relationship = Tree.FindRelationshipById(HyperlinkRelationshipId);
                if (relationship != null)
                {
                    HyperlinkTarget = relationship.Target;
                }
            }
        }

        XmlElement? shapeProperties = PptxTransform.FindShapeProperties(Element);
        XmlElement? geometry = null;
        if (shapeProperties != null)
        {
            geometry = PptxXml.FindFirstChild(shapeProperties, PptxNamespaces.Drawing, "prstGeom");
        }

        if (geometry != null)
        {
            PresetGeometry = PptxXml.GetAttributeValue(geometry, "", "prst");
        }

        XmlElement? customGeometry = null;
        if (shapeProperties != null)
        {
            customGeometry = PptxXml.FindFirstChild(shapeProperties, PptxNamespaces.Drawing, "custGeom");
        }

        if (customGeometry != null)
        {
            CustomGeometry = true;
            CustomGeometryXml = customGeometry.OuterXml;
            PresetGeometry = "";
        }

        Fill = PptxFill.FromShapeProperties(shapeProperties);
        Line = PptxLine.FromShapeProperties(shapeProperties);
        Effect = PptxEffect.FromShapeProperties(shapeProperties);

        XmlElement? locks = GetLockElement(false);
        if (locks != null)
        {
            NoMove = PptxXml.GetBoolAttribute(locks, "", "noMove", false);
            NoResize = PptxXml.GetBoolAttribute(locks, "", "noResize", false);
            NoRotate = PptxXml.GetBoolAttribute(locks, "", "noRot", false);
            NoSelect = PptxXml.GetBoolAttribute(locks, "", "noSelect", false);
        }

        if (ElementType == "cxnSp")
        {
            ConnectorType = PptxConnectorCore.NormalizeConnectorType(PresetGeometry);
            PptxTransform transform = PptxTransform.FromShape(Element);
            Start = new PptxPoint(transform.X, transform.Y);
            End = new PptxPoint(transform.X + transform.Width, transform.Y + transform.Height);
            if (transform.FlipHorizontal)
            {
                Start.X = transform.X + transform.Width;
                End.X = transform.X;
            }

            if (transform.FlipVertical)
            {
                Start.Y = transform.Y + transform.Height;
                End.Y = transform.Y;
            }
        }
    }
    internal PptxTransform GetTransform()
    {
        return PptxTransform.FromShape(Element);
    }
    internal void SetTransform(PptxTransform transform)
    {
        if (transform == null)
        {
            throw new System.ArgumentNullException("transform");
        }

        transform.ApplyToShape(Element);
        Tree.MarkDirty();
        RefreshEffectiveBounds();
    }
    internal PptxBounds GetBounds()
    {
        PptxTransform transform = PptxTransform.FromShape(Element);
        return transform.Bounds();
    }
    internal PptxBounds GetAnchorBounds()
    {
        return GetTransform().AnchorBounds();
    }
    internal PptxBounds GetEffectiveBounds()
    {
        EffectiveBounds = CalculateEffectiveBounds();
        return EffectiveBounds;
    }
    internal PptxBounds GetEffectiveAnchorBounds()
    {
        PptxBounds bounds = GetAnchorBounds();
        PptxShape? group = ParentGroup;
        while (group != null)
        {
            bounds = group.GetTransform().MapChildAnchorBounds(bounds);
            group = group.ParentGroup;
        }

        return bounds;
    }
    internal PptxTransform GetEffectiveTransform()
    {
        PptxTransform transform = GetTransform();
        transform.SetBounds(GetEffectiveBounds());
        return transform;
    }
    internal double GetRotationDegrees()
    {
        return GetTransform().RotationDegrees();
    }
    internal void SetRotationDegrees(double degrees)
    {
        PptxTransform transform = GetTransform();
        transform.SetRotationDegrees(degrees);
        SetTransform(transform);
    }
    internal void SetFlips(bool horizontal, bool vertical)
    {
        PptxTransform transform = GetTransform();
        transform.FlipHorizontal = horizontal;
        transform.FlipVertical = vertical;
        SetTransform(transform);
        if (IsConnector())
        {
            LoadMetadata();
        }
    }
    internal bool IsAutoShape()
    {
        if (PresetGeometry.Length > 0)
        {
            return true;
        }

        return false;
    }
    internal bool IsCustomGeometry()
    {
        return CustomGeometry;
    }
    internal bool IsFreeform()
    {
        if (CustomGeometry)
        {
            return true;
        }

        return false;
    }
    internal string GetPresetGeometry()
    {
        return PresetGeometry;
    }
    internal List<PptxGeometryAdjustment> GetPresetAdjustments()
    {
        List<PptxGeometryAdjustment> adjustments = new List<PptxGeometryAdjustment>();
        XmlElement? geometry = FindPresetGeometryElement();
        if (geometry == null)
        {
            return adjustments;
        }

        XmlElement? list = PptxXml.FindFirstChild(geometry, PptxNamespaces.Drawing, "avLst");
        if (list == null)
        {
            return adjustments;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement guide = (XmlElement)child;
            if (guide.NamespaceURI != PptxNamespaces.Drawing || guide.LocalName != "gd")
            {
                continue;
            }

            string formula = PptxXml.GetAttributeValue(guide, "", "fmla");
            if (!formula.StartsWith("val ", System.StringComparison.Ordinal))
            {
                continue;
            }

            string valueText = formula.Substring(4);
            if (!IsSignedIntegerText(valueText))
            {
                continue;
            }

            long value = long.Parse(valueText, System.Globalization.CultureInfo.InvariantCulture);
            if (value < -2147483648L || value > 2147483647L)
            {
                continue;
            }

            string name = PptxXml.GetAttributeValue(guide, "", "name");
            if (name.Length > 0)
            {
                adjustments.Add(new PptxGeometryAdjustment(name, (int)value));
            }
        }

        return adjustments;
    }
    internal void SetPresetAdjustment(string name, int value)
    {
        string clean = PptxDiagnostic.CleanText(name);
        if (clean.Length == 0)
        {
            throw new System.ArgumentException("Adjustment name is required.");
        }

        XmlElement? geometry = FindPresetGeometryElement();
        if (geometry == null)
        {
            throw new System.ArgumentException("Shape does not use preset geometry.");
        }

        XmlElement list = PptxXml.GetOrCreateChild(geometry, "a", "avLst", PptxNamespaces.Drawing);
        XmlElement? match = null;
        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement guide = (XmlElement)child;
            if (guide.NamespaceURI == PptxNamespaces.Drawing && guide.LocalName == "gd" && PptxXml.GetAttributeValue(guide, "", "name") == clean)
            {
                match = guide;
                break;
            }
        }

        if (match == null)
        {
            match = Element.OwnerDocument.CreateElement("a", "gd", PptxNamespaces.Drawing);
            list.AppendChild(match);
        }

        PptxXml.SetAttributeValue(match, "", "", "name", clean);
        PptxXml.SetAttributeValue(match, "", "", "fmla", "val " + value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Tree.MarkDirty();
    }
    internal bool RemovePresetAdjustment(string name)
    {
        string clean = PptxDiagnostic.CleanText(name);
        XmlElement? geometry = FindPresetGeometryElement();
        if (geometry == null)
        {
            return false;
        }

        XmlElement? list = PptxXml.FindFirstChild(geometry, PptxNamespaces.Drawing, "avLst");
        if (list == null)
        {
            return false;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement guide = (XmlElement)child;
            if (guide.NamespaceURI == PptxNamespaces.Drawing && guide.LocalName == "gd" && PptxXml.GetAttributeValue(guide, "", "name") == clean)
            {
                list.RemoveChild(guide);
                Tree.MarkDirty();
                return true;
            }
        }

        return false;
    }

    internal string GetCustomGeometryXml()
    {
        return CustomGeometryXml;
    }
    internal List<string> GetCustomDataNames()
    {
        List<string> names = new List<string>();
        XmlElement? extension = FindCustomDataExtension();
        if (extension == null)
        {
            return names;
        }

        for (int index = 0; index < extension.ChildNodes.Count; index++)
        {
            XmlNode? child = extension.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement data = (XmlElement)child;
            if (data.NamespaceURI == CustomDataNamespace && data.LocalName == "data")
            {
                string name = PptxXml.GetAttributeValue(data, "", "name");
                if (name.Length > 0)
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }
    internal string GetCustomData(string name)
    {
        XmlElement? data = FindCustomDataElement(PptxDiagnostic.CleanText(name));
        if (data == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(data, "", "value");
    }
    internal void SetCustomData(string name, string value)
    {
        string cleanName = PptxDiagnostic.CleanText(name);
        if (cleanName.Length == 0)
        {
            throw new System.ArgumentException("Custom data name is required.");
        }

        XmlElement properties = GetNonVisualDrawingProperties();
        XmlElement extensionList = PptxXml.GetOrCreateChild(properties, "a", "extLst", PptxNamespaces.Drawing);
        XmlElement? extension = FindCustomDataExtension();
        if (extension == null)
        {
            extension = Element.OwnerDocument.CreateElement("a", "ext", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(extension, "", "", "uri", CustomDataExtensionUri);
            extensionList.AppendChild(extension);
        }

        XmlElement? data = FindCustomDataElement(cleanName);
        if (data == null)
        {
            data = Element.OwnerDocument.CreateElement("pptxlib", "data", CustomDataNamespace);
            PptxXml.SetAttributeValue(data, "", "", "name", cleanName);
            extension.AppendChild(data);
        }

        PptxXml.SetAttributeValue(data, "", "", "value", PptxDiagnostic.CleanText(value));
        Tree.MarkDirty();
    }
    internal bool RemoveCustomData(string name)
    {
        XmlElement? data = FindCustomDataElement(PptxDiagnostic.CleanText(name));
        if (data == null)
        {
            return false;
        }

        XmlNode? extensionNode = data.ParentNode;
        if (extensionNode == null || extensionNode.NodeType != XmlNodeType.Element)
        {
            return false;
        }

        XmlElement extension = (XmlElement)extensionNode;
        extension.RemoveChild(data);
        XmlNode? listNode = extension.ParentNode;
        if (!HasElementChildren(extension) && listNode != null && listNode.NodeType == XmlNodeType.Element)
        {
            XmlElement list = (XmlElement)listNode;
            list.RemoveChild(extension);
            if (!HasElementChildren(list) && list.ParentNode != null)
            {
                list.ParentNode.RemoveChild(list);
            }
        }

        Tree.MarkDirty();
        return true;
    }
    internal List<string> GetExtensionXml()
    {
        List<string> extensions = new List<string>();
        XmlElement properties = GetNonVisualDrawingProperties();
        XmlElement? list = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "extLst");
        if (list == null)
        {
            return extensions;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element)
            {
                extensions.Add(((XmlElement)child).OuterXml);
            }
        }

        return extensions;
    }
    internal bool IsBackgroundShape()
    {
        return GetCustomData(BackgroundDataName) == "1";
    }
    internal void SetBackgroundShape(bool background)
    {
        if (background)
        {
            SetCustomData(BackgroundDataName, "1");
        }
        else
        {
            RemoveCustomData(BackgroundDataName);
        }
    }
    internal PptxFill GetFill()
    {
        return Fill;
    }
    internal PptxLine GetLine()
    {
        return Line;
    }
    internal PptxEffect GetEffect()
    {
        return Effect;
    }
    internal PptxTextFrame GetTextFrame()
    {
        TextFrame = new PptxTextFrame(this);
        return TextFrame;
    }
    internal string GetText()
    {
        return GetTextFrame().GetText();
    }
    internal void SetText(string text)
    {
        GetTextFrame().SetText(text);
    }
    internal PptxParagraph AddTextParagraph(string text)
    {
        return GetTextFrame().AddParagraph(text);
    }
    internal int ReplaceText(string search, string replacement)
    {
        return GetTextFrame().ReplaceText(search, replacement);
    }
    internal bool IsPicture()
    {
        if (ElementType == "pic")
        {
            return true;
        }

        if (GetBlipElement() != null)
        {
            return true;
        }

        return false;
    }
    internal bool IsTable()
    {
        if (ElementType != "graphicFrame")
        {
            return false;
        }

        if (PptxXml.FindFirstDescendant(Element, PptxNamespaces.Drawing, "tbl") != null)
        {
            return true;
        }

        return false;
    }
    internal PptxTable AsTable()
    {
        return new PptxTable(this);
    }
    internal bool IsChart()
    {
        if (ElementType != "graphicFrame")
        {
            return false;
        }

        if (PptxXml.FindFirstDescendant(Element, PptxNamespaces.Chart, "chart") != null)
        {
            return true;
        }

        return false;
    }
    internal string GetChartRelationshipId()
    {
        XmlElement? chart = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Chart, "chart");
        if (chart == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(chart, PptxNamespaces.Relationships, "id");
    }
    internal PptxChart AsChart()
    {
        return new PptxChart(this);
    }
    internal bool IsDiagram()
    {
        if (ElementType != "graphicFrame")
        {
            return false;
        }

        if (PptxXml.FindFirstDescendant(Element, PptxNamespaces.Diagram, "relIds") != null)
        {
            return true;
        }

        return false;
    }
    internal string GetDiagramRelationshipId(string relationshipName)
    {
        XmlElement? relationships = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Diagram, "relIds");
        if (relationships == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(relationships, PptxNamespaces.Relationships, PptxDiagnostic.CleanText(relationshipName));
    }
    internal PptxDiagram AsDiagram()
    {
        return new PptxDiagram(this);
    }
    internal bool IsEmbeddedObject()
    {
        if (ElementType != "graphicFrame")
        {
            return false;
        }

        if (PptxXml.FindFirstDescendant(Element, PptxNamespaces.Presentation, "oleObj") != null)
        {
            return true;
        }

        return false;
    }
    internal string GetEmbeddedObjectRelationshipId()
    {
        XmlElement? embeddedObject = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Presentation, "oleObj");
        if (embeddedObject == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(embeddedObject, PptxNamespaces.Relationships, "id");
    }
    internal string GetEmbeddedObjectProgramId()
    {
        XmlElement? embeddedObject = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Presentation, "oleObj");
        if (embeddedObject == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(embeddedObject, "", "progId");
    }
    internal string GetEmbeddedObjectName()
    {
        XmlElement? embeddedObject = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Presentation, "oleObj");
        if (embeddedObject == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(embeddedObject, "", "name");
    }
    internal bool GetEmbeddedObjectShowAsIcon()
    {
        XmlElement? embeddedObject = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Presentation, "oleObj");
        if (embeddedObject == null)
        {
            return false;
        }

        return PptxXml.GetBoolAttribute(embeddedObject, "", "showAsIcon", false);
    }
    internal PptxEmbeddedObject AsEmbeddedObject()
    {
        return new PptxEmbeddedObject(this);
    }
    internal string GetImageRelationshipId()
    {
        XmlElement? blip = GetBlipElement();
        if (blip == null)
        {
            return "";
        }

        string embed = PptxXml.GetAttributeValue(blip, PptxNamespaces.Relationships, "embed");
        if (embed.Length > 0)
        {
            return embed;
        }

        return PptxXml.GetAttributeValue(blip, PptxNamespaces.Relationships, "link");
    }
    internal void SetImageRelationshipLinked(bool linked)
    {
        XmlElement? blip = GetBlipElement();
        if (blip == null)
        {
            throw new System.ArgumentException("Shape is not a picture.");
        }

        string relationshipId = GetImageRelationshipId();
        blip.RemoveAttribute("embed", PptxNamespaces.Relationships);
        blip.RemoveAttribute("link", PptxNamespaces.Relationships);
        if (linked)
        {
            PptxXml.SetAttributeValue(blip, "r", PptxNamespaces.Relationships, "link", relationshipId);
        }
        else
        {
            PptxXml.SetAttributeValue(blip, "r", PptxNamespaces.Relationships, "embed", relationshipId);
        }

        Tree.MarkDirty();
    }
    internal PptxImageFile ExtractImage()
    {
        if (Tree.Slide.Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        return Tree.Slide.Presentation.ExtractImage(this);
    }
    internal void ReplaceImage(byte[] bytes)
    {
        if (Tree.Slide.Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        Tree.Slide.Presentation.ReplaceImage(this, bytes);
    }
    internal bool IsMediaShape()
    {
        if (GetMediaRelationshipId().Length > 0)
        {
            return true;
        }

        return false;
    }
    internal string GetMediaRelationshipId()
    {
        XmlElement? video = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Drawing, "videoFile");
        if (video != null)
        {
            string link = PptxXml.GetAttributeValue(video, PptxNamespaces.Relationships, "link");
            if (link.Length > 0)
            {
                return link;
            }

            return PptxXml.GetAttributeValue(video, PptxNamespaces.Relationships, "embed");
        }

        XmlElement? audio = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Drawing, "audioFile");
        if (audio != null)
        {
            string link = PptxXml.GetAttributeValue(audio, PptxNamespaces.Relationships, "link");
            if (link.Length > 0)
            {
                return link;
            }

            return PptxXml.GetAttributeValue(audio, PptxNamespaces.Relationships, "embed");
        }

        return "";
    }
    internal bool HasPosterFrame()
    {
        if (IsMediaShape() && GetImageRelationshipId().Length > 0)
        {
            return true;
        }

        return false;
    }
    internal void SetPosterFrameRelationshipId(string relationshipId)
    {
        if (!IsMediaShape())
        {
            throw new System.ArgumentException("Shape is not an audio or video shape.");
        }

        XmlElement blipFill = GetOrCreateBlipFill();
        XmlElement? blip = PptxXml.FindFirstChild(blipFill, PptxNamespaces.Drawing, "blip");
        if (blip == null)
        {
            blip = PptxTextFrame.GetOwnerDocument(blipFill).CreateElement("a", "blip", PptxNamespaces.Drawing);
            blipFill.PrependChild(blip);
        }

        blip.RemoveAttribute("link", PptxNamespaces.Relationships);
        PptxXml.SetAttributeValue(blip, "r", PptxNamespaces.Relationships, "embed", PptxDiagnostic.CleanText(relationshipId));
        Tree.MarkDirty();
    }
    internal void ClearPosterFrameRelationship()
    {
        XmlElement? blip = GetBlipElement();
        if (blip == null)
        {
            return;
        }

        blip.RemoveAttribute("embed", PptxNamespaces.Relationships);
        blip.RemoveAttribute("link", PptxNamespaces.Relationships);
        Tree.MarkDirty();
    }
    internal PptxImageFile ExtractPosterFrame()
    {
        return ExtractImage();
    }
    internal void ReplacePosterFrame(byte[] bytes)
    {
        ReplaceImage(bytes);
    }
    internal void SetImageCrop(int left, int top, int right, int bottom)
    {
        PptxImageCrop crop = new PptxImageCrop();
        crop.Left = left;
        crop.Top = top;
        crop.Right = right;
        crop.Bottom = bottom;
        SetImageCropRectangle(crop);
    }
    internal void SetImageCropRectangle(PptxImageCrop crop)
    {
        if (crop == null)
        {
            throw new System.ArgumentNullException("crop");
        }

        if (!crop.IsValid())
        {
            throw new System.ArgumentException("Image crop values must be between zero and 100,000 and opposing edges cannot exceed 100,000.");
        }

        if (!IsPicture())
        {
            throw new System.ArgumentException("Shape is not a picture.");
        }

        XmlElement blipFill = GetOrCreateBlipFill();
        XmlElement sourceRectangle = PptxXml.GetOrCreateChild(blipFill, "a", "srcRect", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(sourceRectangle, "", "", "l", crop.Left.ToString());
        PptxXml.SetAttributeValue(sourceRectangle, "", "", "t", crop.Top.ToString());
        PptxXml.SetAttributeValue(sourceRectangle, "", "", "r", crop.Right.ToString());
        PptxXml.SetAttributeValue(sourceRectangle, "", "", "b", crop.Bottom.ToString());
        Tree.MarkDirty();
    }
    internal PptxImageCrop GetImageCrop()
    {
        PptxImageCrop crop = new PptxImageCrop();
        XmlElement? blipFill = GetBlipFillElement();
        if (blipFill == null)
        {
            return crop;
        }

        XmlElement? sourceRectangle = PptxXml.FindFirstChild(blipFill, PptxNamespaces.Drawing, "srcRect");
        if (sourceRectangle == null)
        {
            return crop;
        }

        crop.Left = PptxXml.GetIntAttribute(sourceRectangle, "", "l", 0);
        crop.Top = PptxXml.GetIntAttribute(sourceRectangle, "", "t", 0);
        crop.Right = PptxXml.GetIntAttribute(sourceRectangle, "", "r", 0);
        crop.Bottom = PptxXml.GetIntAttribute(sourceRectangle, "", "b", 0);
        return crop;
    }
    internal void SetImageStretch()
    {
        SetImageStretchSettings(new PptxFillStretch());
    }
    internal void SetImageStretchSettings(PptxFillStretch settings)
    {
        if (settings == null)
        {
            throw new System.ArgumentNullException("settings");
        }

        if (!IsPicture())
        {
            throw new System.ArgumentException("Shape is not a picture.");
        }

        XmlElement blipFill = GetOrCreateBlipFill();
        RemoveDirectChild(blipFill, PptxNamespaces.Drawing, "tile");
        XmlElement stretch = PptxXml.GetOrCreateChild(blipFill, "a", "stretch", PptxNamespaces.Drawing);
        XmlElement? rectangle = PptxXml.FindFirstChild(stretch, PptxNamespaces.Drawing, "fillRect");
        if (rectangle == null)
        {
            rectangle = PptxTextFrame.GetOwnerDocument(stretch).CreateElement("a", "fillRect", PptxNamespaces.Drawing);
            stretch.AppendChild(rectangle);
        }

        PptxXml.SetAttributeValue(rectangle, "", "", "l", settings.Left.ToString());
        PptxXml.SetAttributeValue(rectangle, "", "", "t", settings.Top.ToString());
        PptxXml.SetAttributeValue(rectangle, "", "", "r", settings.Right.ToString());
        PptxXml.SetAttributeValue(rectangle, "", "", "b", settings.Bottom.ToString());

        Tree.MarkDirty();
    }
    internal PptxFillStretch GetImageStretchSettings()
    {
        PptxFillStretch settings = new PptxFillStretch();
        XmlElement? blipFill = GetBlipFillElement();
        if (blipFill == null)
        {
            return settings;
        }

        XmlElement? stretch = PptxXml.FindFirstChild(blipFill, PptxNamespaces.Drawing, "stretch");
        if (stretch == null)
        {
            return settings;
        }

        XmlElement? rectangle = PptxXml.FindFirstChild(stretch, PptxNamespaces.Drawing, "fillRect");
        if (rectangle != null)
        {
            settings.Left = PptxXml.GetIntAttribute(rectangle, "", "l", 0);
            settings.Top = PptxXml.GetIntAttribute(rectangle, "", "t", 0);
            settings.Right = PptxXml.GetIntAttribute(rectangle, "", "r", 0);
            settings.Bottom = PptxXml.GetIntAttribute(rectangle, "", "b", 0);
        }

        return settings;
    }
    internal void SetImageTile()
    {
        SetImageTileSettings(new PptxFillTile());
    }
    internal void SetImageTileSettings(PptxFillTile settings)
    {
        if (settings == null)
        {
            throw new System.ArgumentNullException("settings");
        }

        if (settings.ScaleX <= 0 || settings.ScaleY <= 0)
        {
            throw new System.ArgumentOutOfRangeException("settings", "Image tile scales must be greater than zero.");
        }

        if (!IsPicture())
        {
            throw new System.ArgumentException("Shape is not a picture.");
        }

        XmlElement blipFill = GetOrCreateBlipFill();
        RemoveDirectChild(blipFill, PptxNamespaces.Drawing, "stretch");
        XmlElement tile = PptxXml.GetOrCreateChild(blipFill, "a", "tile", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(tile, "", "", "tx", settings.OffsetX.ToString());
        PptxXml.SetAttributeValue(tile, "", "", "ty", settings.OffsetY.ToString());
        PptxXml.SetAttributeValue(tile, "", "", "sx", settings.ScaleX.ToString());
        PptxXml.SetAttributeValue(tile, "", "", "sy", settings.ScaleY.ToString());
        string alignment = PptxDiagnostic.CleanText(settings.Alignment);
        string flip = PptxDiagnostic.CleanText(settings.Flip);
        if (alignment.Length > 0)
        {
            PptxXml.SetAttributeValue(tile, "", "", "algn", alignment);
        }
        else
        {
            tile.RemoveAttribute("algn");
        }

        if (flip.Length > 0)
        {
            PptxXml.SetAttributeValue(tile, "", "", "flip", flip);
        }
        else
        {
            tile.RemoveAttribute("flip");
        }
        Tree.MarkDirty();
    }
    internal PptxFillTile GetImageTileSettings()
    {
        PptxFillTile settings = new PptxFillTile();
        XmlElement? blipFill = GetBlipFillElement();
        if (blipFill == null)
        {
            return settings;
        }

        XmlElement? tile = PptxXml.FindFirstChild(blipFill, PptxNamespaces.Drawing, "tile");
        if (tile != null)
        {
            settings.OffsetX = PptxTransform.GetLongAttribute(tile, "tx", 0L);
            settings.OffsetY = PptxTransform.GetLongAttribute(tile, "ty", 0L);
            settings.ScaleX = PptxXml.GetIntAttribute(tile, "", "sx", 100000);
            settings.ScaleY = PptxXml.GetIntAttribute(tile, "", "sy", 100000);
            settings.Alignment = PptxXml.GetAttributeValue(tile, "", "algn");
            settings.Flip = PptxXml.GetAttributeValue(tile, "", "flip");
        }

        return settings;
    }
    internal bool IsImageTiled()
    {
        XmlElement? blipFill = GetBlipFillElement();
        if (blipFill == null)
        {
            return false;
        }

        return PptxXml.FindFirstChild(blipFill, PptxNamespaces.Drawing, "tile") != null;
    }
    internal bool IsImageStretched()
    {
        XmlElement? blipFill = GetBlipFillElement();
        if (blipFill == null)
        {
            return false;
        }

        return PptxXml.FindFirstChild(blipFill, PptxNamespaces.Drawing, "stretch") != null;
    }

    internal XmlElement? GetBlipElement()
    {
        return PptxXml.FindFirstDescendant(Element, PptxNamespaces.Drawing, "blip");
    }

    internal XmlElement? GetBlipFillElement()
    {
        return PptxXml.FindFirstChild(Element, PptxNamespaces.Presentation, "blipFill");
    }

    internal XmlElement GetOrCreateBlipFill()
    {
        XmlElement? existing = GetBlipFillElement();
        if (existing != null)
        {
            return existing;
        }

        XmlElement blipFill = PptxTextFrame.GetOwnerDocument(Element).CreateElement("p", "blipFill", PptxNamespaces.Presentation);
        Element.AppendChild(blipFill);
        return blipFill;
    }
    internal bool IsTextBox()
    {
        XmlElement? nonVisualShapeProperties = Tree.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvSpPr");
        if (nonVisualShapeProperties == null)
        {
            return false;
        }

        return PptxXml.GetBoolAttribute(nonVisualShapeProperties, "", "txBox", false);
    }
    internal void SetTextBox(bool enabled)
    {
        XmlElement? nonVisualShapeProperties = Tree.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvSpPr");
        if (nonVisualShapeProperties == null)
        {
            return;
        }

        if (enabled)
        {
            PptxXml.SetAttributeValue(nonVisualShapeProperties, "", "", "txBox", "1");
        }
        else
        {
            nonVisualShapeProperties.RemoveAttribute("txBox");
        }

        Tree.MarkDirty();
    }
    internal void SetPresetGeometry(string geometry)
    {
        string preset = "";
        if (IsConnector())
        {
            preset = PptxConnectorCore.GetPresetGeometry(geometry);
        }
        else
        {
            preset = PptxShapeTypeCore.NormalizePresetGeometry(geometry);
        }

        XmlElement properties = GetOrCreateShapeProperties();
        RemoveDirectChild(properties, PptxNamespaces.Drawing, "custGeom");
        XmlElement? presetGeometry = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "prstGeom");
        if (presetGeometry == null)
        {
            presetGeometry = Element.OwnerDocument.CreateElement("a", "prstGeom", PptxNamespaces.Drawing);
            properties.AppendChild(presetGeometry);
        }

        PptxXml.SetAttributeValue(presetGeometry, "", "", "prst", preset);
        if (PptxXml.FindFirstChild(presetGeometry, PptxNamespaces.Drawing, "avLst") == null)
        {
            presetGeometry.AppendChild(Element.OwnerDocument.CreateElement("a", "avLst", PptxNamespaces.Drawing));
        }

        PresetGeometry = preset;
        CustomGeometry = false;
        CustomGeometryXml = "";
        if (IsConnector())
        {
            ConnectorType = PptxConnectorCore.NormalizeConnectorType(preset);
        }

        Tree.MarkDirty();
    }

    internal void SetCustomGeometryXml(string customGeometryXml)
    {
        string clean = PptxDiagnostic.CleanText(customGeometryXml);
        XmlDocument document = PptxXml.LoadString(clean);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Custom geometry XML root is required.");
        }

        SetCustomGeometryElement(root);
    }
    internal void SetFreeformGeometry(List<PptxPoint> points)
    {
        PptxBounds bounds = GetBounds();
        XmlElement geometry = PptxGeometry.CreateFreeformGeometryElement(Element.OwnerDocument, points, bounds.Width, bounds.Height);
        SetCustomGeometryElement(geometry);
    }
    internal List<PptxFreeformPath> GetFreeformPaths()
    {
        XmlElement? geometry = FindCustomGeometryElement();
        PptxBounds bounds = GetBounds();
        return PptxGeometry.ParseFreeformPaths(geometry, bounds.Width, bounds.Height);
    }
    internal void SetFreeformPaths(List<PptxFreeformPath> paths)
    {
        XmlElement geometry = PptxGeometry.CreateFreeformPathsElement(Element.OwnerDocument, paths);
        SetCustomGeometryElement(geometry);
    }

    internal void SetCustomGeometryElement(XmlElement geometry)
    {
        if (geometry == null)
        {
            throw new System.ArgumentNullException("geometry");
        }

        if (geometry.LocalName != "custGeom")
        {
            throw new System.ArgumentException("Custom geometry XML root must be custGeom.");
        }

        XmlElement properties = GetOrCreateShapeProperties();
        RemoveDirectChild(properties, PptxNamespaces.Drawing, "prstGeom");
        RemoveDirectChild(properties, PptxNamespaces.Drawing, "custGeom");
        XmlNode imported = Element.OwnerDocument.ImportNode(geometry, true);
        properties.AppendChild(imported);
        XmlElement importedElement = (XmlElement)imported;
        CustomGeometry = true;
        CustomGeometryXml = importedElement.OuterXml;
        PresetGeometry = "";
        Tree.MarkDirty();
    }
    internal void SetFill(PptxFill fill)
    {
        if (fill == null)
        {
            throw new System.ArgumentNullException("fill");
        }

        XmlElement properties = GetOrCreateShapeProperties();
        RemoveFillChildren(properties);
        XmlElement fillElement = fill.ToElement(Element.OwnerDocument);
        properties.AppendChild(fillElement);
        Fill = PptxFill.FromElement(fillElement);
        Tree.MarkDirty();
    }

    internal void RemoveFillChildren(XmlElement properties)
    {
        for (int index = properties.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = properties.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Drawing && PptxFill.IsFillElement(element.LocalName))
            {
                properties.RemoveChild(child);
            }
        }
    }
    internal void SetLine(PptxLine line)
    {
        if (line == null)
        {
            throw new System.ArgumentNullException("line");
        }

        XmlElement properties = GetOrCreateShapeProperties();
        RemoveLineChildren(properties);
        XmlElement lineElement = line.ToElement(Element.OwnerDocument);
        properties.AppendChild(lineElement);
        Line = PptxLine.FromElement(lineElement);
        Tree.MarkDirty();
    }

    internal void RemoveLineChildren(XmlElement properties)
    {
        for (int index = properties.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = properties.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Drawing && element.LocalName == "ln")
            {
                properties.RemoveChild(child);
            }
        }
    }
    internal void SetEffect(PptxEffect effect)
    {
        if (effect == null)
        {
            throw new System.ArgumentNullException("effect");
        }

        XmlElement properties = GetOrCreateShapeProperties();
        RemoveEffectChildren(properties);
        XmlElement effectElement = effect.ToElement(Element.OwnerDocument);
        properties.AppendChild(effectElement);
        effect.AppendThreeD(Element.OwnerDocument, properties);
        Effect = PptxEffect.FromShapeProperties(properties);
        Tree.MarkDirty();
    }

    internal void RemoveEffectChildren(XmlElement properties)
    {
        for (int index = properties.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = properties.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Drawing && IsEffectElement(element.LocalName))
            {
                properties.RemoveChild(child);
            }
        }
    }

    internal bool IsEffectElement(string localName)
    {
        if (localName == "effectLst")
        {
            return true;
        }

        if (localName == "effectDag")
        {
            return true;
        }

        if (localName == "scene3d")
        {
            return true;
        }

        if (localName == "sp3d")
        {
            return true;
        }

        return false;
    }
    internal void SetBounds(PptxBounds bounds)
    {
        PptxTransform transform = PptxTransform.FromShape(Element);
        transform.SetBounds(bounds);
        transform.ApplyToShape(Element);
        if (IsConnector())
        {
            Start = new PptxPoint(bounds.X, bounds.Y);
            End = new PptxPoint(bounds.X + bounds.Width, bounds.Y + bounds.Height);
        }

        Tree.MarkDirty();
        RefreshEffectiveBounds();
    }
    internal bool IsGroup()
    {
        if (ElementType == "grpSp")
        {
            return true;
        }

        return false;
    }
    internal PptxGroupShape AsGroup()
    {
        return new PptxGroupShape(this);
    }
    internal int ChildCount()
    {
        return Children.Count;
    }
    internal PptxShape GetChild(int index)
    {
        if (index < 0 || index >= Children.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Children[index];
    }
    internal PptxShape? FindDescendantById(int id)
    {
        if (Id == id)
        {
            return this;
        }

        for (int index = 0; index < Children.Count; index++)
        {
            PptxShape? match = Children[index].FindDescendantById(id);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
    internal PptxShape? FindDescendantByName(string name)
    {
        string clean = PptxDiagnostic.CleanText(name);
        if (Name == clean)
        {
            return this;
        }

        for (int index = 0; index < Children.Count; index++)
        {
            PptxShape? match = Children[index].FindDescendantByName(clean);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
    internal bool IsConnector()
    {
        if (ElementType == "cxnSp")
        {
            return true;
        }

        return false;
    }
    internal string GetConnectorType()
    {
        return ConnectorType;
    }
    internal void SetConnectorType(string connectorType)
    {
        if (!IsConnector())
        {
            throw new System.ArgumentException("Shape is not a connector.");
        }

        SetPresetGeometry(PptxConnectorCore.GetPresetGeometry(connectorType));
    }
    internal void SetConnectorEnds(PptxPoint start, PptxPoint end)
    {
        if (start == null)
        {
            throw new System.ArgumentNullException("start");
        }

        if (end == null)
        {
            throw new System.ArgumentNullException("end");
        }

        if (!IsConnector())
        {
            throw new System.ArgumentException("Shape is not a connector.");
        }

        long x = start.X;
        long y = start.Y;
        long width = end.X - start.X;
        long height = end.Y - start.Y;
        bool flipHorizontal = false;
        bool flipVertical = false;
        if (width < 0L)
        {
            x = end.X;
            width = start.X - end.X;
            flipHorizontal = true;
        }

        if (height < 0L)
        {
            y = end.Y;
            height = start.Y - end.Y;
            flipVertical = true;
        }

        PptxTransform transform = PptxTransform.FromShape(Element);
        transform.SetBounds(new PptxBounds(x, y, width, height));
        transform.FlipHorizontal = flipHorizontal;
        transform.FlipVertical = flipVertical;
        transform.ApplyToShape(Element);
        Start = new PptxPoint(start.X, start.Y);
        End = new PptxPoint(end.X, end.Y);
        Tree.MarkDirty();
    }
    internal void SetConnectorEndCoordinates(long startX, long startY, long endX, long endY)
    {
        SetConnectorEnds(new PptxPoint(startX, startY), new PptxPoint(endX, endY));
    }
    internal PptxConnectionPoint GetConnectorStartConnection()
    {
        return GetConnectorConnection("stCxn", Start);
    }
    internal PptxConnectionPoint GetConnectorEndConnection()
    {
        return GetConnectorConnection("endCxn", End);
    }
    internal void SetConnectorConnections(PptxConnectionPoint start, PptxConnectionPoint end)
    {
        if (start == null)
        {
            throw new System.ArgumentNullException("start");
        }

        if (end == null)
        {
            throw new System.ArgumentNullException("end");
        }

        if (!IsConnector())
        {
            throw new System.ArgumentException("Shape is not a connector.");
        }

        ValidateConnectionPoint(start);
        ValidateConnectionPoint(end);
        SetConnectorEnds(start.Position, end.Position);
        WriteConnectorConnection("stCxn", start);
        WriteConnectorConnection("endCxn", end);
        Tree.MarkDirty();
    }
    internal PptxConnectionPoint GetConnectorConnection(string localName, PptxPoint position)
    {
        if (!IsConnector())
        {
            throw new System.ArgumentException("Shape is not a connector.");
        }

        PptxConnectionPoint connection = new PptxConnectionPoint(position);
        XmlElement? container = GetNonVisualLockContainer();
        if (container == null)
        {
            return connection;
        }

        XmlElement? element = PptxXml.FindFirstChild(container, PptxNamespaces.Drawing, localName);
        if (element == null)
        {
            return connection;
        }

        connection.ShapeId = PptxXml.GetIntAttribute(element, "", "id", 0);
        connection.SiteIndex = PptxXml.GetIntAttribute(element, "", "idx", 0);
        connection.Connected = connection.ShapeId > 0;
        return connection;
    }
    internal void WriteConnectorConnection(string localName, PptxConnectionPoint connection)
    {
        XmlElement? container = GetNonVisualLockContainer();
        if (container == null)
        {
            throw new System.ArgumentException("Connector non-visual properties are required.");
        }

        RemoveDirectChild(container, PptxNamespaces.Drawing, localName);
        if (!connection.Connected)
        {
            return;
        }

        XmlElement element = Element.OwnerDocument.CreateElement("a", localName, PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(element, "", "", "id", connection.ShapeId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        PptxXml.SetAttributeValue(element, "", "", "idx", connection.SiteIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
        XmlNode? before = null;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlNode? child = container.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement existing = (XmlElement)child;
            if (existing.NamespaceURI != PptxNamespaces.Drawing)
            {
                continue;
            }

            if (existing.LocalName == "extLst" || (localName == "stCxn" && existing.LocalName == "endCxn"))
            {
                before = existing;
                break;
            }
        }

        if (before == null)
        {
            container.AppendChild(element);
        }
        else
        {
            container.InsertBefore(element, before);
        }
    }
    internal static void ValidateConnectionPoint(PptxConnectionPoint connection)
    {
        if (connection.Position == null)
        {
            throw new System.ArgumentException("Connector endpoint position is required.");
        }

        if (connection.Connected && (connection.ShapeId <= 0 || connection.SiteIndex < 0))
        {
            throw new System.ArgumentException("Connected endpoints require a positive shape id and a non-negative site index.");
        }
    }
    internal void SetName(string name)
    {
        XmlElement? properties = Tree.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvPr");
        if (properties == null)
        {
            throw new System.ArgumentException("Shape non-visual properties are required.");
        }

        Name = PptxDiagnostic.CleanText(name);
        PptxXml.SetAttributeValue(properties, "", "", "name", Name);
        Tree.MarkDirty();
    }
    internal bool IsVisible()
    {
        if (Hidden)
        {
            return false;
        }

        return true;
    }
    internal void SetVisible(bool visible)
    {
        SetHidden(!visible);
    }
    internal void SetHidden(bool hidden)
    {
        XmlElement properties = GetNonVisualDrawingProperties();
        Hidden = hidden;
        PptxXml.SetAttributeValue(properties, "", "", "hidden", Tree.BoolToXml(hidden));
        Tree.MarkDirty();
    }
    internal void SetAlternativeText(string alternativeText)
    {
        XmlElement properties = GetNonVisualDrawingProperties();
        AlternativeText = PptxDiagnostic.CleanText(alternativeText);
        PptxXml.SetAttributeValue(properties, "", "", "descr", AlternativeText);
        Tree.MarkDirty();
    }
    internal void SetTitle(string title)
    {
        XmlElement properties = GetNonVisualDrawingProperties();
        Title = PptxDiagnostic.CleanText(title);
        PptxXml.SetAttributeValue(properties, "", "", "title", Title);
        Tree.MarkDirty();
    }
    internal void SetLock(string lockName, bool locked)
    {
        XmlElement? lockElement = GetLockElement(true);
        if (lockElement == null)
        {
            throw new System.ArgumentException("Shape lock properties are required.");
        }

        string attributeName = NormalizeLockName(lockName);
        PptxXml.SetAttributeValue(lockElement, "", "", attributeName, Tree.BoolToXml(locked));
        LoadMetadata();
        Tree.MarkDirty();
    }
    internal bool GetLock(string lockName)
    {
        XmlElement? locks = GetLockElement(false);
        if (locks == null)
        {
            return false;
        }

        return PptxXml.GetBoolAttribute(locks, "", NormalizeLockName(lockName), false);
    }
    internal void SetLocks(bool noMove, bool noResize, bool noRotate, bool noSelect)
    {
        SetLock("noMove", noMove);
        SetLock("noResize", noResize);
        SetLock("noRot", noRotate);
        SetLock("noSelect", noSelect);
    }
    internal void SetHyperlink(string url)
    {
        SetExternalHyperlink(url);
    }
    internal PptxHyperlink GetHyperlink()
    {
        return new PptxHyperlink(this, null);
    }
    internal List<PptxHyperlink> GetHyperlinks()
    {
        List<PptxHyperlink> hyperlinks = new List<PptxHyperlink>();
        PptxHyperlink shapeHyperlink = GetHyperlink();
        if (shapeHyperlink.HasLink())
        {
            hyperlinks.Add(shapeHyperlink);
        }

        PptxTextFrame textFrame = GetTextFrame();
        for (int paragraphIndex = 0; paragraphIndex < textFrame.Paragraphs.Count; paragraphIndex++)
        {
            PptxParagraph paragraph = textFrame.Paragraphs[paragraphIndex];
            for (int runIndex = 0; runIndex < paragraph.Runs.Count; runIndex++)
            {
                PptxHyperlink runHyperlink = paragraph.Runs[runIndex].GetHyperlink();
                if (runHyperlink.HasLink())
                {
                    hyperlinks.Add(runHyperlink);
                }
            }
        }

        return hyperlinks;
    }

    internal XmlElement? GetHyperlinkClickElement()
    {
        XmlElement? properties = PptxXml.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvPr");
        if (properties == null)
        {
            return null;
        }

        return PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "hlinkClick");
    }
    internal void SetExternalHyperlink(string url)
    {
        string clean = PptxDiagnostic.CleanText(url);
        if (clean.Length == 0)
        {
            RemoveHyperlink();
            return;
        }

        if (!PptxUriCore.IsExternalTarget(clean))
        {
            throw new System.ArgumentException("Hyperlink target must be external.");
        }

        XmlElement properties = GetNonVisualDrawingProperties();
        XmlElement hyperlink = PptxXml.GetOrCreateChild(properties, "a", "hlinkClick", PptxNamespaces.Drawing);
        string relationshipId = PptxXml.GetAttributeValue(hyperlink, PptxNamespaces.Relationships, "id");
        PptxRelationship? existing = Tree.FindRelationshipById(relationshipId);
        if (relationshipId.Length == 0 || existing == null || existing.Type != PptxRelationshipTypes.Hyperlink)
        {
            if (relationshipId.Length > 0)
            {
                hyperlink.RemoveAttribute("id", PptxNamespaces.Relationships);
                Tree.RemoveRelationshipIfUnused(relationshipId);
            }

            PptxRelationship relationship = Tree.AddExternalRelationship(PptxRelationshipTypes.Hyperlink, clean);
            relationshipId = relationship.Id;
            PptxXml.SetAttributeValue(hyperlink, "r", PptxNamespaces.Relationships, "id", relationshipId);
        }
        else
        {
            Tree.SetRelationshipTarget(relationshipId, clean);
        }

        HyperlinkRelationshipId = relationshipId;
        HyperlinkTarget = clean;
        hyperlink.RemoveAttribute("action");
        Tree.MarkDirty();
    }
    internal void SetFileHyperlink(string fileUri)
    {
        SetExternalHyperlink(fileUri);
    }
    internal void SetSlideHyperlink(int slideIndex)
    {
        if (Tree.Slide.Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        PptxSlide targetSlide = Tree.Slide.Presentation.GetSlide(slideIndex);
        XmlElement properties = GetNonVisualDrawingProperties();
        XmlElement hyperlink = PptxXml.GetOrCreateChild(properties, "a", "hlinkClick", PptxNamespaces.Drawing);
        RemoveHyperlinkRelationshipOnly(hyperlink);
        string target = PptxSlideCloner.BuildRelativeTargetPath(Tree.Slide.PartName, targetSlide.PartName);
        PptxRelationship relationship = Tree.Slide.Presentation.Package.AddRelationship(Tree.Slide.PartName, PptxRelationshipTypes.Slide, target, PptxRelationshipTargetMode.Internal);
        PptxXml.SetAttributeValue(hyperlink, "r", PptxNamespaces.Relationships, "id", relationship.Id);
        PptxXml.SetAttributeValue(hyperlink, "", "", "action", "ppaction://hlinksldjump");
        HyperlinkRelationshipId = relationship.Id;
        HyperlinkTarget = target;
        Tree.MarkDirty();
    }
    internal void SetNextSlideAction()
    {
        SetAction("ppaction://hlinkshowjump?jump=nextslide");
    }
    internal void SetPreviousSlideAction()
    {
        SetAction("ppaction://hlinkshowjump?jump=previousslide");
    }
    internal void SetFirstSlideAction()
    {
        SetAction("ppaction://hlinkshowjump?jump=firstslide");
    }
    internal void SetLastSlideAction()
    {
        SetAction("ppaction://hlinkshowjump?jump=lastslide");
    }
    internal void SetCustomShowAction(string customShowName)
    {
        SetAction("ppaction://customshow?id=" + PptxDiagnostic.CleanText(customShowName));
    }
    internal void SetAction(string action)
    {
        string clean = PptxDiagnostic.CleanText(action);
        if (clean.Length == 0)
        {
            RemoveHyperlink();
            return;
        }

        XmlElement properties = GetNonVisualDrawingProperties();
        XmlElement hyperlink = PptxXml.GetOrCreateChild(properties, "a", "hlinkClick", PptxNamespaces.Drawing);
        RemoveHyperlinkRelationshipOnly(hyperlink);
        PptxXml.SetAttributeValue(hyperlink, "", "", "action", clean);
        HyperlinkRelationshipId = "";
        HyperlinkTarget = "";
        Tree.MarkDirty();
    }

    internal void RemoveHyperlinkRelationshipOnly(XmlElement hyperlink)
    {
        string relationshipId = PptxXml.GetAttributeValue(hyperlink, PptxNamespaces.Relationships, "id");
        if (relationshipId.Length > 0)
        {
            hyperlink.RemoveAttribute("id", PptxNamespaces.Relationships);
            Tree.RemoveRelationshipIfUnused(relationshipId);
        }
    }
    internal void RemoveHyperlink()
    {
        XmlElement properties = GetNonVisualDrawingProperties();
        XmlElement? hyperlink = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "hlinkClick");
        if (hyperlink == null)
        {
            HyperlinkRelationshipId = "";
            HyperlinkTarget = "";
            return;
        }

        string relationshipId = PptxXml.GetAttributeValue(hyperlink, PptxNamespaces.Relationships, "id");
        properties.RemoveChild(hyperlink);
        if (relationshipId.Length > 0)
        {
            Tree.RemoveRelationshipIfUnused(relationshipId);
        }

        HyperlinkRelationshipId = "";
        HyperlinkTarget = "";
        Tree.MarkDirty();
    }
    internal void Remove()
    {
        Tree.Remove(this);
    }
    internal PptxShape Clone()
    {
        return Tree.CloneShape(this);
    }

    internal void LoadChildren()
    {
        Children.Clear();
        if (!IsGroup())
        {
            return;
        }

        for (int index = 0; index < Element.ChildNodes.Count; index++)
        {
            XmlNode? child = Element.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (!PptxShapeTree.IsShapeElement(element))
            {
                continue;
            }

            PptxShape shape = new PptxShape(Tree, element);
            shape.ParentGroup = this;
            shape.RefreshEffectiveBounds();
            Children.Add(shape);
        }
    }

    internal void RefreshEffectiveBounds()
    {
        EffectiveBounds = CalculateEffectiveBounds();
        for (int index = 0; index < Children.Count; index++)
        {
            Children[index].RefreshEffectiveBounds();
        }
    }

    internal PptxBounds CalculateEffectiveBounds()
    {
        PptxBounds bounds = GetBounds();
        PptxShape? group = ParentGroup;
        while (group != null)
        {
            bounds = group.GetTransform().MapChildBounds(bounds);
            group = group.ParentGroup;
        }

        return bounds;
    }

    internal XmlElement GetNonVisualDrawingProperties()
    {
        XmlElement? properties = Tree.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvPr");
        if (properties == null)
        {
            throw new System.ArgumentException("Shape non-visual properties are required.");
        }

        return properties;
    }

    internal XmlElement GetOrCreateShapeProperties()
    {
        XmlElement? properties = PptxTransform.FindShapeProperties(Element);
        if (properties != null)
        {
            return properties;
        }

        PptxTransform.GetOrCreateTransformElement(Element);
        properties = PptxTransform.FindShapeProperties(Element);
        if (properties == null)
        {
            throw new System.ArgumentException("Shape properties are required.");
        }

        return properties;
    }

    internal XmlElement? FindPresetGeometryElement()
    {
        XmlElement? properties = PptxTransform.FindShapeProperties(Element);
        if (properties == null)
        {
            return null;
        }

        return PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "prstGeom");
    }

    internal XmlElement? FindCustomGeometryElement()
    {
        XmlElement? properties = PptxTransform.FindShapeProperties(Element);
        if (properties == null)
        {
            return null;
        }

        return PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "custGeom");
    }

    internal XmlElement? FindCustomDataExtension()
    {
        XmlElement properties = GetNonVisualDrawingProperties();
        XmlElement? list = PptxXml.FindFirstChild(properties, PptxNamespaces.Drawing, "extLst");
        if (list == null)
        {
            return null;
        }

        for (int index = 0; index < list.ChildNodes.Count; index++)
        {
            XmlNode? child = list.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement extension = (XmlElement)child;
            if (extension.NamespaceURI == PptxNamespaces.Drawing && extension.LocalName == "ext" && PptxXml.GetAttributeValue(extension, "", "uri") == CustomDataExtensionUri)
            {
                return extension;
            }
        }

        return null;
    }

    internal XmlElement? FindCustomDataElement(string name)
    {
        XmlElement? extension = FindCustomDataExtension();
        if (extension == null)
        {
            return null;
        }

        for (int index = 0; index < extension.ChildNodes.Count; index++)
        {
            XmlNode? child = extension.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement data = (XmlElement)child;
            if (data.NamespaceURI == CustomDataNamespace && data.LocalName == "data" && PptxXml.GetAttributeValue(data, "", "name") == name)
            {
                return data;
            }
        }

        return null;
    }

    internal static bool HasElementChildren(XmlElement element)
    {
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlNode? child = element.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsSignedIntegerText(string value)
    {
        if (value.Length == 0 || value.Length > 11)
        {
            return false;
        }

        int start = 0;
        if (value[0] == '-' || value[0] == '+')
        {
            start = 1;
        }

        if (start == value.Length)
        {
            return false;
        }

        for (int index = start; index < value.Length; index++)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
        }

        return true;
    }

    internal void RemoveDirectChild(XmlElement root, string namespaceUri, string localName)
    {
        for (int index = root.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                root.RemoveChild(child);
            }
        }
    }

    internal XmlElement? GetNonVisualLockContainer()
    {
        if (Element.LocalName == "grpSp")
        {
            return Tree.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvGrpSpPr");
        }

        if (Element.LocalName == "pic")
        {
            return Tree.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvPicPr");
        }

        if (Element.LocalName == "cxnSp")
        {
            return Tree.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvCxnSpPr");
        }

        if (Element.LocalName == "graphicFrame")
        {
            return Tree.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvGraphicFramePr");
        }

        return Tree.FindFirstDescendant(Element, PptxNamespaces.Presentation, "cNvSpPr");
    }

    internal XmlElement? GetLockElement(bool create)
    {
        XmlElement? container = GetNonVisualLockContainer();
        if (container == null)
        {
            return null;
        }

        string localName = GetLockElementLocalName();
        XmlElement? locks = PptxXml.FindFirstChild(container, PptxNamespaces.Drawing, localName);
        if (locks == null && create)
        {
            locks = Element.OwnerDocument.CreateElement("a", localName, PptxNamespaces.Drawing);
            container.AppendChild(locks);
        }

        return locks;
    }

    internal string GetLockElementLocalName()
    {
        if (Element.LocalName == "grpSp")
        {
            return "grpSpLocks";
        }

        if (Element.LocalName == "pic")
        {
            return "picLocks";
        }

        if (Element.LocalName == "cxnSp")
        {
            return "cxnSpLocks";
        }

        if (Element.LocalName == "graphicFrame")
        {
            return "graphicFrameLocks";
        }

        return "spLocks";
    }

    internal static string NormalizeLockName(string lockName)
    {
        string clean = PptxDiagnostic.CleanText(lockName);
        if (clean == "noRotate")
        {
            return "noRot";
        }

        if (clean.Length == 0)
        {
            return "noSelect";
        }

        return clean;
    }
}
