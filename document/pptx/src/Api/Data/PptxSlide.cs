using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents one editable presentation slide and its shapes, notes, comments, transitions, and related content.
/// </summary>
public class PptxSlide
{
    internal PptxPart Part;
    internal PptxPresentation? Presentation;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the slide id used to identify or connect related presentation data.
    /// </summary>
    public int SlideId;
    /// <summary>
    /// Indicates whether the hidden setting is enabled.
    /// </summary>
    public bool Hidden;
    /// <summary>
    /// Stores the slide layout when one is related; otherwise, <c>null</c>.
    /// </summary>
    public PptxSlideLayout? Layout;
    /// <summary>
    /// Contains the placeholders in package or presentation order.
    /// </summary>
    public List<PptxPlaceholder> Placeholders;

    internal PptxSlide(PptxPresentation presentation, PptxPart part, string relationshipId)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        Presentation = presentation;
        Part = part;
        PartName = part.PartName;
        RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        SlideId = 0;
        Hidden = false;
        Layout = null;
        Placeholders = new List<PptxPlaceholder>();
    }
    internal PptxNotesSlide? GetNotes()
    {
        if (Presentation == null)
        {
            return null;
        }

        return Presentation.FindNotesSlide(this);
    }
    internal PptxNotesSlide GetOrCreateNotes()
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        return Presentation.GetOrCreateNotesSlide(this);
    }
    internal string GetNotesText()
    {
        PptxNotesSlide? notes = GetNotes();
        if (notes == null)
        {
            return "";
        }

        return notes.GetText();
    }
    internal void SetNotesText(string text)
    {
        GetOrCreateNotes().SetText(text);
    }
    internal List<PptxComment> GetComments()
    {
        if (Presentation == null)
        {
            return new List<PptxComment>();
        }

        return Presentation.GetSlideComments(this);
    }
    internal PptxComment AddComment(int authorId, string text)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        return Presentation.AddSlideComment(this, authorId, text);
    }
    internal PptxComment AddCommentWithDetails(int authorId, string text, string dateTime, long x, long y)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        return Presentation.AddSlideCommentWithDetails(this, authorId, text, dateTime, x, y);
    }
    internal List<PptxModernComment> GetModernComments()
    {
        if (Presentation == null)
        {
            return new List<PptxModernComment>();
        }

        return Presentation.GetSlideModernComments(this);
    }
    internal bool HasModernComments()
    {
        return GetModernComments().Count > 0;
    }
    internal List<PptxTag> GetTags()
    {
        if (Presentation == null)
        {
            return new List<PptxTag>();
        }

        return Presentation.GetSlideTags(this);
    }
    internal int TagCount()
    {
        return GetTags().Count;
    }
    internal string GetTagValue(string name)
    {
        if (Presentation == null)
        {
            return "";
        }

        return Presentation.GetSlideTagValue(this, name);
    }
    internal void SetTag(string name, string value)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        Presentation.SetSlideTag(this, name, value);
    }
    internal void RemoveTag(string name)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        Presentation.RemoveSlideTag(this, name);
    }
    internal void ClearTags()
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        Presentation.ClearSlideTags(this);
    }
    internal bool RemoveTagPart()
    {
        if (Presentation == null)
        {
            return false;
        }

        return Presentation.RemoveSlideTagsPart(this);
    }
    internal bool HasTiming()
    {
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return false;
        }

        if (PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "timing") != null)
        {
            return true;
        }

        return false;
    }

    internal string GetTimingXml()
    {
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return "";
        }

        XmlElement? timing = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "timing");
        if (timing == null)
        {
            return "";
        }

        return timing.OuterXml;
    }
    internal List<PptxAnimationEffect> GetAnimations()
    {
        return PptxAnimationEffect.FromSlide(this);
    }
    internal List<PptxHyperlink> GetHyperlinks()
    {
        List<PptxHyperlink> hyperlinks = new List<PptxHyperlink>();
        List<PptxShape> shapes = GetShapeTree().GetAllShapes();
        for (int shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
        {
            List<PptxHyperlink> shapeHyperlinks = shapes[shapeIndex].GetHyperlinks();
            for (int hyperlinkIndex = 0; hyperlinkIndex < shapeHyperlinks.Count; hyperlinkIndex++)
            {
                hyperlinks.Add(shapeHyperlinks[hyperlinkIndex]);
            }
        }

        return hyperlinks;
    }
    internal PptxResult AuditHyperlinks()
    {
        return PptxHyperlink.AuditAll(GetHyperlinks());
    }
    internal PptxSlideTransition GetTransition()
    {
        return new PptxSlideTransition(this);
    }
    internal bool HasTransition()
    {
        return GetTransition().HasTransition();
    }
    internal void SetTransition(string transitionType)
    {
        GetTransition().SetType(transitionType);
    }
    internal void SetTransitionOptions(string transitionType, string speed, bool advanceOnClick, string advanceAfterTime)
    {
        PptxSlideTransition transition = GetTransition();
        transition.SetType(transitionType);
        transition.SetSpeed(speed);
        transition.SetAdvance(advanceOnClick, advanceAfterTime);
    }
    internal void RemoveTransition()
    {
        GetTransition().Remove();
    }
    internal PptxAnimationEffect AddAnimation(int shapeId, string type, string effect)
    {
        return AddAnimationWithTiming(shapeId, type, effect, "onClick", "500", "0", "");
    }
    internal PptxAnimationEffect AddAnimationWithTiming(int shapeId, string type, string effect, string trigger, string duration, string delay, string repeatCount)
    {
        if (FindShapeById(shapeId) == null)
        {
            throw new System.ArgumentException("Animation target shape was not found.");
        }

        PptxAnimationEffect.ValidateEffectType(PptxDiagnostic.CleanText(type));
        if (PptxDiagnostic.CleanText(effect).Length == 0)
        {
            throw new System.ArgumentException("Animation effect is required.");
        }

        XmlDocument document = GetXmlDocument();
        XmlElement childList = GetOrCreateRootAnimationChildList(document);
        int timingId = AllocateTimingNodeId(document);
        XmlElement paragraph = CreateAnimationParagraph(document, timingId, shapeId.ToString(), type, effect, trigger, duration, delay, repeatCount);
        childList.AppendChild(paragraph);
        Part.SetXmlDocument(document);
        List<PptxAnimationEffect> effects = GetAnimations();
        return effects[effects.Count - 1];
    }
    internal PptxAnimationEffect AddEntranceAnimation(int shapeId, string effect)
    {
        return AddAnimationWithTiming(shapeId, PptxAnimationType.Entrance, effect, PptxAnimationTrigger.OnClick, "500", "0", "");
    }
    internal PptxAnimationEffect AddEmphasisAnimation(int shapeId, string effect)
    {
        return AddAnimationWithTiming(shapeId, PptxAnimationType.Emphasis, effect, PptxAnimationTrigger.OnClick, "500", "0", "");
    }
    internal PptxAnimationEffect AddExitAnimation(int shapeId, string effect)
    {
        return AddAnimationWithTiming(shapeId, PptxAnimationType.Exit, effect, PptxAnimationTrigger.OnClick, "500", "0", "");
    }
    internal PptxAnimationEffect AddMotionPathAnimation(int shapeId, string path)
    {
        return AddAnimationWithTiming(shapeId, PptxAnimationType.MotionPath, path, PptxAnimationTrigger.OnClick, "1000", "0", "");
    }
    internal PptxAnimationEffect AddMediaAnimation(int shapeId, string command)
    {
        return AddAnimationWithTiming(shapeId, PptxAnimationType.Media, command, PptxAnimationTrigger.OnClick, "indefinite", "0", "");
    }
    internal void RemoveAnimation(int index)
    {
        List<PptxAnimationEffect> effects = GetAnimations();
        if (index < 0 || index >= effects.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        effects[index].Remove();
    }
    internal void MoveAnimation(int oldIndex, int newIndex)
    {
        List<PptxAnimationEffect> effects = GetAnimations();
        if (oldIndex < 0 || oldIndex >= effects.Count)
        {
            throw new System.ArgumentOutOfRangeException("oldIndex");
        }

        if (newIndex < 0 || newIndex >= effects.Count)
        {
            throw new System.ArgumentOutOfRangeException("newIndex");
        }

        if (oldIndex == newIndex)
        {
            return;
        }

        XmlElement? moving = effects[oldIndex].GetContainerElement();
        XmlElement? target = effects[newIndex].GetContainerElement();
        if (moving == null || target == null)
        {
            throw new System.ArgumentException("Animation container was not found.");
        }

        XmlNode? parent = moving.ParentNode;
        if (parent == null)
        {
            throw new System.ArgumentException("Animation container parent was not found.");
        }

        parent.RemoveChild(moving);
        if (oldIndex < newIndex)
        {
            XmlNode? next = target.NextSibling;
            if (next == null)
            {
                parent.AppendChild(moving);
            }
            else
            {
                parent.InsertBefore(moving, next);
            }
        }
        else
        {
            parent.InsertBefore(moving, target);
        }

        Part.SetXmlDocument(PptxTextFrame.GetOwnerDocument(moving));
    }
    internal void MoveAnimationEffect(PptxAnimationEffect effect, int newIndex)
    {
        if (effect == null)
        {
            throw new System.ArgumentNullException("effect");
        }

        List<PptxAnimationEffect> effects = GetAnimations();
        int oldIndex = -1;
        for (int index = 0; index < effects.Count; index++)
        {
            if (effects[index].Element == effect.Element)
            {
                oldIndex = index;
            }
        }

        if (oldIndex < 0)
        {
            throw new System.ArgumentException("Animation effect belongs to another slide.");
        }

        MoveAnimation(oldIndex, newIndex);
    }

    internal XmlElement GetOrCreateRootAnimationChildList(XmlDocument document)
    {
        XmlElement root = GetSlideRoot(document);
        XmlElement? timing = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "timing");
        if (timing == null)
        {
            timing = document.CreateElement("p", "timing", PptxNamespaces.Presentation);
            XmlElement? colorMap = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "clrMapOvr");
            if (colorMap != null)
            {
                root.InsertBefore(timing, colorMap);
            }
            else
            {
                root.AppendChild(timing);
            }
        }

        XmlElement timingList = PptxXml.GetOrCreateChild(timing, "p", "tnLst", PptxNamespaces.Presentation);
        XmlElement? rootParagraph = PptxXml.FindFirstChild(timingList, PptxNamespaces.Presentation, "par");
        if (rootParagraph == null)
        {
            rootParagraph = document.CreateElement("p", "par", PptxNamespaces.Presentation);
            timingList.AppendChild(rootParagraph);
        }

        XmlElement? rootTiming = PptxXml.FindFirstChild(rootParagraph, PptxNamespaces.Presentation, "cTn");
        if (rootTiming == null)
        {
            rootTiming = document.CreateElement("p", "cTn", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(rootTiming, "", "", "id", "1");
            PptxXml.SetAttributeValue(rootTiming, "", "", "dur", "indefinite");
            PptxXml.SetAttributeValue(rootTiming, "", "", "restart", "never");
            PptxXml.SetAttributeValue(rootTiming, "", "", "nodeType", "tmRoot");
            rootParagraph.AppendChild(rootTiming);
        }

        return PptxXml.GetOrCreateChild(rootTiming, "p", "childTnLst", PptxNamespaces.Presentation);
    }

    internal XmlElement GetSlideRoot(XmlDocument document)
    {
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            root = document.CreateElement("p", "sld", PptxNamespaces.Presentation);
            PptxXml.SetAttributeValue(root, "xmlns", PptxNamespaces.Xmlns, "a", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(root, "xmlns", PptxNamespaces.Xmlns, "p", PptxNamespaces.Presentation);
            document.AppendChild(root);
        }

        return root;
    }

    internal int AllocateTimingNodeId(XmlDocument document)
    {
        int maximum = 0;
        XmlElement? root = document.DocumentElement;
        if (root != null)
        {
            maximum = FindMaximumTimingNodeId(root, maximum);
        }

        return maximum + 1;
    }

    internal int FindMaximumTimingNodeId(XmlNode node, int maximum)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "cTn")
            {
                int id = PptxXml.GetIntAttribute(element, "", "id", 0);
                if (id > maximum)
                {
                    maximum = id;
                }
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                maximum = FindMaximumTimingNodeId(child, maximum);
            }
        }

        return maximum;
    }

    internal XmlElement CreateAnimationParagraph(XmlDocument document, int timingId, string shapeId, string type, string effect, string trigger, string duration, string delay, string repeatCount)
    {
        XmlElement paragraph = document.CreateElement("p", "par", PptxNamespaces.Presentation);
        XmlElement timing = document.CreateElement("p", "cTn", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(timing, "", "", "id", timingId.ToString());
        PptxAnimationEffect.SetAttributeOrRemove(timing, "dur", PptxDiagnostic.CleanText(duration));
        PptxAnimationEffect.SetAttributeOrRemove(timing, "repeatCount", PptxDiagnostic.CleanText(repeatCount));
        XmlElement conditionList = document.CreateElement("p", "stCondLst", PptxNamespaces.Presentation);
        XmlElement condition = document.CreateElement("p", "cond", PptxNamespaces.Presentation);
        PptxAnimationEffect.SetTriggerAttributes(timing, condition, PptxDiagnostic.CleanText(trigger));
        PptxAnimationEffect.SetAttributeOrRemove(condition, "delay", PptxDiagnostic.CleanText(delay));
        conditionList.AppendChild(condition);
        timing.AppendChild(conditionList);
        XmlElement childList = document.CreateElement("p", "childTnLst", PptxNamespaces.Presentation);
        childList.AppendChild(CreateAnimationEffectElement(document, timingId + 1, shapeId, type, effect, duration));
        timing.AppendChild(childList);
        paragraph.AppendChild(timing);
        return paragraph;
    }

    internal XmlElement CreateAnimationEffectElement(XmlDocument document, int behaviorId, string shapeId, string type, string effect, string duration)
    {
        string cleanType = PptxDiagnostic.CleanText(type);
        XmlElement animation = document.CreateElement("p", PptxAnimationEffect.GetElementNameForType(cleanType), PptxNamespaces.Presentation);
        PptxAnimationEffect.ApplyEffectAttributes(animation, cleanType, PptxDiagnostic.CleanText(effect));

        animation.AppendChild(CreateCommonBehaviorElement(document, behaviorId, shapeId, duration));
        return animation;
    }

    internal XmlElement CreateCommonBehaviorElement(XmlDocument document, int behaviorId, string shapeId, string duration)
    {
        XmlElement behavior = document.CreateElement("p", "cBhvr", PptxNamespaces.Presentation);
        XmlElement timing = document.CreateElement("p", "cTn", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(timing, "", "", "id", behaviorId.ToString());
        PptxAnimationEffect.SetAttributeOrRemove(timing, "dur", PptxDiagnostic.CleanText(duration));
        behavior.AppendChild(timing);
        XmlElement targetElement = document.CreateElement("p", "tgtEl", PptxNamespaces.Presentation);
        XmlElement shapeTarget = document.CreateElement("p", "spTgt", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(shapeTarget, "", "", "spid", PptxDiagnostic.CleanText(shapeId));
        targetElement.AppendChild(shapeTarget);
        behavior.AppendChild(targetElement);
        return behavior;
    }
    internal int PlaceholderCount()
    {
        return Placeholders.Count;
    }
    internal PptxPlaceholder? FindPlaceholder(string type)
    {
        string normalizedType = PptxPresentation.NormalizePlaceholderType(type);
        for (int index = 0; index < Placeholders.Count; index++)
        {
            if (Placeholders[index].Type == normalizedType)
            {
                return Placeholders[index];
            }
        }

        return null;
    }
    internal PptxPlaceholder? FindPlaceholderByIndex(string type, int placeholderIndex)
    {
        string normalizedType = PptxPresentation.NormalizePlaceholderType(type);
        for (int index = 0; index < Placeholders.Count; index++)
        {
            if (Placeholders[index].Type == normalizedType && Placeholders[index].Index == placeholderIndex)
            {
                return Placeholders[index];
            }
        }

        return null;
    }
    internal bool HasPlaceholder(string type)
    {
        PptxPlaceholder? placeholder = FindPlaceholder(type);
        if (placeholder != null)
        {
            return true;
        }

        return false;
    }
    internal PptxShapeTree GetShapeTree()
    {
        return new PptxShapeTree(this);
    }
    internal PptxShape AddShape(string shapeType, PptxBounds bounds)
    {
        PptxShapeTree tree = GetShapeTree();
        return tree.AddShape(shapeType, bounds);
    }
    internal PptxShape AddAutoShape(string presetGeometry, PptxBounds bounds)
    {
        PptxShapeTree tree = GetShapeTree();
        return tree.AddAutoShape(presetGeometry, bounds);
    }
    internal PptxShape AddTextBox(string text, PptxBounds bounds)
    {
        PptxShape shape = AddAutoShape(PptxShapeType.TextBox, bounds);
        shape.SetTextBox(true);
        shape.SetText(text);
        return shape;
    }
    internal PptxShape AddConnector(string connectorType, PptxPoint start, PptxPoint end)
    {
        PptxShapeTree tree = GetShapeTree();
        return tree.AddConnector(connectorType, start, end);
    }
    internal PptxShape AddFreeform(PptxBounds bounds, List<PptxPoint> points)
    {
        PptxShapeTree tree = GetShapeTree();
        return tree.AddFreeform(bounds, points);
    }
    internal PptxShape AddImage(byte[] bytes, PptxBounds bounds)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        return Presentation.AddImageToSlide(this, bytes, bounds);
    }
    internal PptxTable AddTable(int rowCount, int columnCount, PptxBounds bounds)
    {
        PptxShapeTree tree = GetShapeTree();
        PptxShape shape = tree.AddTable(rowCount, columnCount, bounds);
        return shape.AsTable();
    }
    internal PptxChart AddChart(string chartType, PptxChartData data, PptxBounds bounds)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        return Presentation.AddChartToSlide(this, chartType, data, bounds);
    }
    internal List<PptxTable> GetTables()
    {
        PptxShapeTree tree = GetShapeTree();
        List<PptxShape> shapes = tree.GetAllShapes();
        List<PptxTable> tables = new List<PptxTable>();
        for (int index = 0; index < shapes.Count; index++)
        {
            if (shapes[index].IsTable())
            {
                tables.Add(shapes[index].AsTable());
            }
        }

        return tables;
    }
    internal PptxTable GetTable(int index)
    {
        List<PptxTable> tables = GetTables();
        if (index < 0 || index >= tables.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return tables[index];
    }
    internal List<PptxChart> GetCharts()
    {
        PptxShapeTree tree = GetShapeTree();
        List<PptxShape> shapes = tree.GetAllShapes();
        List<PptxChart> charts = new List<PptxChart>();
        for (int index = 0; index < shapes.Count; index++)
        {
            if (shapes[index].IsChart())
            {
                charts.Add(shapes[index].AsChart());
            }
        }

        return charts;
    }
    internal PptxChart GetChart(int index)
    {
        List<PptxChart> charts = GetCharts();
        if (index < 0 || index >= charts.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return charts[index];
    }
    internal List<List<List<string>>> ExtractChartDataTables()
    {
        List<PptxChart> charts = GetCharts();
        List<List<List<string>>> tables = new List<List<List<string>>>();
        for (int index = 0; index < charts.Count; index++)
        {
            tables.Add(charts[index].ExtractDataTable());
        }

        return tables;
    }
    internal List<PptxDiagram> GetDiagrams()
    {
        PptxShapeTree tree = GetShapeTree();
        List<PptxShape> shapes = tree.GetAllShapes();
        List<PptxDiagram> diagrams = new List<PptxDiagram>();
        for (int index = 0; index < shapes.Count; index++)
        {
            if (shapes[index].IsDiagram())
            {
                diagrams.Add(shapes[index].AsDiagram());
            }
        }

        return diagrams;
    }
    internal PptxDiagram GetDiagram(int index)
    {
        List<PptxDiagram> diagrams = GetDiagrams();
        if (index < 0 || index >= diagrams.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return diagrams[index];
    }
    internal List<PptxEmbeddedObject> GetEmbeddedObjects()
    {
        PptxShapeTree tree = GetShapeTree();
        List<PptxShape> shapes = tree.GetAllShapes();
        List<PptxEmbeddedObject> objects = new List<PptxEmbeddedObject>();
        for (int index = 0; index < shapes.Count; index++)
        {
            if (shapes[index].IsEmbeddedObject())
            {
                objects.Add(shapes[index].AsEmbeddedObject());
            }
        }

        return objects;
    }
    internal PptxEmbeddedObject GetEmbeddedObject(int index)
    {
        List<PptxEmbeddedObject> objects = GetEmbeddedObjects();
        if (index < 0 || index >= objects.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return objects[index];
    }
    internal List<PptxActiveXControl> GetActiveXControls()
    {
        if (Presentation == null)
        {
            return new List<PptxActiveXControl>();
        }

        return PptxActiveXControl.FromPart(Presentation, Part);
    }
    internal PptxActiveXControl GetActiveXControl(int index)
    {
        List<PptxActiveXControl> controls = GetActiveXControls();
        if (index < 0 || index >= controls.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return controls[index];
    }
    internal List<PptxImageFile> ExtractImages()
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        return Presentation.ExtractImages(this);
    }
    internal PptxAssetExtractionResult ExtractImagesToDirectory(string directoryPath)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        return PptxAssetExtractorCore.ExtractSlideImages(Presentation, this, directoryPath);
    }
    internal List<PptxMediaFile> GetMedia()
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        return Presentation.GetSlideMedia(this);
    }
    internal PptxAssetExtractionResult ExtractMediaToDirectory(string directoryPath)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        return PptxAssetExtractorCore.ExtractSlideMedia(Presentation, this, directoryPath);
    }
    internal string ExtractText()
    {
        return PptxTextExtractorCore.ExtractSlideText(this);
    }
    internal string ExportText()
    {
        return PptxTextOutlineExporterCore.ExportSlideText(this);
    }
    internal string ExportOutline()
    {
        return PptxTextOutlineExporterCore.ExportSlideOutline(this);
    }
    internal string ExportHtml()
    {
        return PptxHtmlExporterCore.ExportSlide(this);
    }
    internal string ExportSvg()
    {
        return PptxSvgExporterCore.ExportSlide(this);
    }
    internal List<PptxOutlineEntry> GetOutlineEntries()
    {
        return PptxTextOutlineExporterCore.GetSlideOutlineEntries(this);
    }
    internal int ReplaceText(string search, string replacement)
    {
        return PptxTextReplacerCore.ReplaceSlideText(this, search, replacement);
    }
    internal int FillTemplate(PptxTemplateData data)
    {
        return PptxTemplateFillerCore.FillSlide(this, data);
    }
    internal int FillTemplateFromDictionary(Dictionary<string, string> values)
    {
        return PptxTemplateFillerCore.FillSlide(this, values);
    }
    internal int FillTemplateFromLists(List<string> keys, List<string> values)
    {
        return PptxTemplateFillerCore.FillSlide(this, keys, values);
    }
    internal PptxFill GetBackgroundFill()
    {
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return new PptxFill();
        }

        XmlElement? commonSlideData = PptxXml.FindFirstChild(root, PptxNamespaces.Presentation, "cSld");
        if (commonSlideData == null)
        {
            return new PptxFill();
        }

        XmlElement? background = PptxXml.FindFirstChild(commonSlideData, PptxNamespaces.Presentation, "bg");
        if (background == null)
        {
            return new PptxFill();
        }

        XmlElement? backgroundProperties = PptxXml.FindFirstChild(background, PptxNamespaces.Presentation, "bgPr");
        return PptxFill.FromShapeProperties(backgroundProperties);
    }
    internal void ApplyLayout(PptxSlideLayout layout)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        Presentation.ApplySlideLayout(this, layout);
    }
    internal PptxPlaceholderInheritance InspectPlaceholderInheritance(string placeholderType, int placeholderIndex)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        return Presentation.InspectPlaceholderInheritance(this, placeholderType, placeholderIndex);
    }
    internal PptxInheritedBackground GetEffectiveBackground()
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        return Presentation.GetEffectiveBackground(this);
    }
    internal void SetBackground(PptxFill fill)
    {
        if (fill == null)
        {
            throw new System.ArgumentNullException("fill");
        }

        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Slide XML root is required.");
        }

        XmlElement commonSlideData = PptxXml.GetOrCreateChild(root, "p", "cSld", PptxNamespaces.Presentation);
        XmlElement background = PptxXml.GetOrCreateChild(commonSlideData, "p", "bg", PptxNamespaces.Presentation);
        XmlElement backgroundProperties = PptxXml.GetOrCreateChild(background, "p", "bgPr", PptxNamespaces.Presentation);
        RemoveFillChildren(backgroundProperties);
        backgroundProperties.AppendChild(fill.ToElement(document));
        Part.SetXmlDocument(document);
    }

    internal void RemoveFillChildren(XmlElement element)
    {
        for (int index = element.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = element.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement childElement = (XmlElement)child;
            if (childElement.NamespaceURI == PptxNamespaces.Drawing && PptxFill.IsFillElement(childElement.LocalName))
            {
                element.RemoveChild(child);
            }
        }
    }
    internal PptxShape? FindShapeById(int id)
    {
        PptxShapeTree tree = GetShapeTree();
        return tree.FindById(id);
    }
    internal PptxShape? FindShapeByName(string name)
    {
        PptxShapeTree tree = GetShapeTree();
        return tree.FindByName(name);
    }
    internal void PopulateTitle(string text)
    {
        PopulatePlaceholder("title", text);
    }
    internal void PopulateBody(string text)
    {
        PopulatePlaceholder("body", text);
    }
    internal void PopulatePicture(string text)
    {
        PopulatePlaceholder("picture", text);
    }
    internal void PopulateTable(string text)
    {
        PopulatePlaceholder("table", text);
    }
    internal void PopulateChart(string text)
    {
        PopulatePlaceholder("chart", text);
    }
    internal void PopulateMedia(string text)
    {
        PopulatePlaceholder("media", text);
    }
    internal void PopulateContent(string text)
    {
        PopulatePlaceholder("content", text);
    }
    internal void PopulatePlaceholder(string type, string text)
    {
        PptxPlaceholder? placeholder = FindPlaceholder(type);
        if (placeholder == null)
        {
            throw new System.ArgumentException("Placeholder was not found.");
        }

        PopulatePlaceholderByIndex(type, placeholder.Index, text);
    }
    internal void SetPlaceholderText(string type, string text)
    {
        PopulatePlaceholder(type, text);
    }
    internal void SetPlaceholderTextByIndex(string type, int placeholderIndex, string text)
    {
        PopulatePlaceholderByIndex(type, placeholderIndex, text);
    }
    internal void PopulatePlaceholderByIndex(string type, int placeholderIndex, string text)
    {
        string normalizedType = PptxPresentation.NormalizePlaceholderType(type);
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Slide XML root is required.");
        }

        XmlElement? shape = FindShapeForPlaceholder(root, normalizedType, placeholderIndex);
        if (shape == null)
        {
            throw new System.ArgumentException("Placeholder was not found.");
        }

        SetShapeText(shape, PptxDiagnostic.CleanText(text));
        Part.SetXmlDocument(document);
        ReloadPlaceholders();
    }
    internal PptxShape PopulatePlaceholderImage(string type, int placeholderIndex, byte[] bytes)
    {
        XmlElement placeholderShape = GetRequiredPlaceholderShape(type, placeholderIndex);
        PptxShape shape = AddImage(bytes, GetPlaceholderBounds(placeholderShape));
        ReplacePlaceholderShape(placeholderShape, shape);
        return shape;
    }
    internal PptxTable PopulatePlaceholderTable(string type, int placeholderIndex, int rowCount, int columnCount)
    {
        XmlElement placeholderShape = GetRequiredPlaceholderShape(type, placeholderIndex);
        PptxTable table = AddTable(rowCount, columnCount, GetPlaceholderBounds(placeholderShape));
        ReplacePlaceholderShape(placeholderShape, table.Shape);
        return table;
    }
    internal PptxChart PopulatePlaceholderChart(string type, int placeholderIndex, string chartType, PptxChartData data)
    {
        XmlElement placeholderShape = GetRequiredPlaceholderShape(type, placeholderIndex);
        PptxChart chart = AddChart(chartType, data, GetPlaceholderBounds(placeholderShape));
        ReplacePlaceholderShape(placeholderShape, chart.Shape);
        return chart;
    }
    internal PptxMediaFile PopulatePlaceholderMedia(string type, int placeholderIndex, byte[] bytes, string contentType, string mediaType)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        XmlElement placeholderShape = GetRequiredPlaceholderShape(type, placeholderIndex);
        PptxShape shape = Presentation.AddMediaShapeToSlide(this, bytes, contentType, mediaType, GetPlaceholderBounds(placeholderShape));
        ReplacePlaceholderShape(placeholderShape, shape);
        PptxRelationship? relationship = shape.Tree.FindRelationshipById(shape.GetMediaRelationshipId());
        if (relationship == null)
        {
            throw new System.ArgumentException("Media relationship was not found.");
        }

        return Presentation.CreateMediaFile(this, relationship);
    }
    internal PptxEmbeddedObject PopulatePlaceholderObject(string type, int placeholderIndex, byte[] bytes, string contentType, string programId, string objectName)
    {
        if (Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        XmlElement placeholderShape = GetRequiredPlaceholderShape(type, placeholderIndex);
        PptxShape shape = Presentation.AddEmbeddedObjectShapeToSlide(this, bytes, contentType, programId, objectName, GetPlaceholderBounds(placeholderShape));
        ReplacePlaceholderShape(placeholderShape, shape);
        return shape.AsEmbeddedObject();
    }
    internal XmlElement GetRequiredPlaceholderShape(string type, int placeholderIndex)
    {
        string normalizedType = PptxPresentation.NormalizePlaceholderType(type);
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Slide XML root is required.");
        }

        XmlElement? shape = FindShapeForPlaceholder(root, normalizedType, placeholderIndex);
        if (shape == null)
        {
            throw new System.ArgumentException("Placeholder was not found.");
        }

        Part.SetXmlDocument(document);
        return shape;
    }
    internal PptxBounds GetPlaceholderBounds(XmlElement placeholderShape)
    {
        PptxShapeTree tree = GetShapeTree();
        PptxShape shape = new PptxShape(tree, placeholderShape);
        PptxBounds bounds = shape.GetBounds();
        if (bounds.Width > 0L && bounds.Height > 0L)
        {
            return bounds;
        }

        long width = 1L;
        long height = 1L;
        if (Presentation != null)
        {
            if (Presentation.SlideWidth > 0L)
            {
                width = Presentation.SlideWidth;
            }

            if (Presentation.SlideHeight > 0L)
            {
                height = Presentation.SlideHeight;
            }
        }

        return new PptxBounds(0L, 0L, width, height);
    }
    internal void ReplacePlaceholderShape(XmlElement placeholderShape, PptxShape replacementShape)
    {
        XmlElement? oldProperties = FindFirstDescendant(placeholderShape, PptxNamespaces.Presentation, "cNvPr");
        XmlElement? oldPlaceholder = FindFirstDescendant(placeholderShape, PptxNamespaces.Presentation, "ph");
        XmlElement? newProperties = FindFirstDescendant(replacementShape.Element, PptxNamespaces.Presentation, "cNvPr");
        XmlElement? newApplicationProperties = FindFirstDescendant(replacementShape.Element, PptxNamespaces.Presentation, "nvPr");
        if (oldProperties == null || oldPlaceholder == null || newProperties == null || newApplicationProperties == null)
        {
            throw new System.ArgumentException("Placeholder replacement metadata is required.");
        }

        XmlNode? propertiesParent = newProperties.ParentNode;
        if (propertiesParent == null)
        {
            throw new System.ArgumentException("Replacement non-visual properties are required.");
        }

        propertiesParent.ReplaceChild(oldProperties.CloneNode(true), newProperties);
        for (int index = newApplicationProperties.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = newApplicationProperties.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)child;
                if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "ph")
                {
                    newApplicationProperties.RemoveChild(child);
                }
            }
        }

        newApplicationProperties.AppendChild(oldPlaceholder.CloneNode(true));
        XmlNode? parent = placeholderShape.ParentNode;
        if (parent == null)
        {
            throw new System.ArgumentException("Placeholder shape parent is required.");
        }

        parent.InsertBefore(replacementShape.Element, placeholderShape);
        parent.RemoveChild(placeholderShape);
        replacementShape.Tree.MarkDirty();
        replacementShape.LoadMetadata();
        replacementShape.RefreshEffectiveBounds();
        if (Presentation != null)
        {
            Presentation.ApplySlidePlaceholderInheritance(this);
        }
    }
    internal void SetTableCellText(int rowIndex, int columnIndex, string text)
    {
        SetTableCellTextInTable(0, rowIndex, columnIndex, text);
    }
    internal void SetTableCellTextInTable(int tableIndex, int rowIndex, int columnIndex, string text)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement cell = GetTableCell(document, tableIndex, rowIndex, columnIndex);
        XmlElement textBody = PptxTextFrame.GetOrCreateTextBody(cell, "a", PptxNamespaces.Drawing);
        PptxTextFrame.SetTextBodyText(textBody, PptxDiagnostic.CleanText(text));
        Part.SetXmlDocument(document);
    }
    internal string GetTableCellText(int rowIndex, int columnIndex)
    {
        return GetTableCellTextInTable(0, rowIndex, columnIndex);
    }
    internal string GetTableCellTextInTable(int tableIndex, int rowIndex, int columnIndex)
    {
        XmlDocument document = GetXmlDocument();
        XmlElement cell = GetTableCell(document, tableIndex, rowIndex, columnIndex);
        XmlElement? textBody = PptxXml.FindFirstChild(cell, PptxNamespaces.Drawing, "txBody");
        if (textBody == null)
        {
            return "";
        }

        return PptxTextFrame.GetTextBodyText(textBody);
    }
    internal string GetPlaceholderText(string type)
    {
        PptxPlaceholder? placeholder = FindPlaceholder(type);
        if (placeholder == null)
        {
            return "";
        }

        return GetPlaceholderTextByIndex(type, placeholder.Index);
    }
    internal string GetPlaceholderTextByIndex(string type, int placeholderIndex)
    {
        string normalizedType = PptxPresentation.NormalizePlaceholderType(type);
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return "";
        }

        XmlElement? shape = FindShapeForPlaceholder(root, normalizedType, placeholderIndex);
        if (shape == null)
        {
            return "";
        }

        return GetShapeText(shape);
    }

    internal void ReloadPlaceholders()
    {
        Placeholders.Clear();
        XmlDocument document = GetXmlDocument();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        CollectPlaceholders(root);
    }

    internal void CollectPlaceholders(XmlNode node)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "ph")
            {
                Placeholders.Add(CreatePlaceholder(element));
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                CollectPlaceholders(child);
            }
        }
    }

    internal PptxPlaceholder CreatePlaceholder(XmlElement element)
    {
        PptxPlaceholder placeholder = new PptxPlaceholder();
        placeholder.SourcePartName = PartName;
        placeholder.Type = PptxPresentation.NormalizePlaceholderType(PptxXml.GetAttributeValue(element, "", "type"));
        placeholder.Index = PptxXml.GetIntAttribute(element, "", "idx", -1);
        placeholder.Size = PptxXml.GetAttributeValue(element, "", "sz");

        XmlElement? shape = FindPlaceholderShape(element);
        if (shape != null)
        {
            XmlElement? properties = FindFirstDescendant(shape, PptxNamespaces.Presentation, "cNvPr");
            if (properties != null)
            {
                placeholder.ShapeId = PptxXml.GetAttributeValue(properties, "", "id");
                placeholder.ShapeName = PptxXml.GetAttributeValue(properties, "", "name");
            }

            placeholder.Text = GetShapeText(shape);
        }

        return placeholder;
    }

    internal XmlDocument GetXmlDocument()
    {
        if (Part.HasXmlDocument)
        {
            return Part.XmlDocument;
        }

        return PptxXml.LoadBytes(Part.GetBytes());
    }

    internal XmlElement? FindShapeForPlaceholder(XmlNode node, string type, int placeholderIndex)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "ph")
            {
                string elementType = PptxPresentation.NormalizePlaceholderType(PptxXml.GetAttributeValue(element, "", "type"));
                int elementIndex = PptxXml.GetIntAttribute(element, "", "idx", -1);
                if (elementType == type && elementIndex == placeholderIndex)
                {
                    return FindPlaceholderShape(element);
                }
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                XmlElement? result = FindShapeForPlaceholder(child, type, placeholderIndex);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    internal XmlElement? FindPlaceholderShape(XmlElement placeholder)
    {
        XmlNode? node = placeholder.ParentNode;
        while (node != null)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)node;
                if (element.NamespaceURI == PptxNamespaces.Presentation)
                {
                    if (element.LocalName == "sp" || element.LocalName == "pic" || element.LocalName == "graphicFrame" || element.LocalName == "grpSp")
                    {
                        return element;
                    }
                }
            }

            node = node.ParentNode;
        }

        return null;
    }

    internal XmlElement? FindFirstDescendant(XmlElement root, string namespaceUri, string localName)
    {
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                return element;
            }

            XmlElement? descendant = FindFirstDescendant(element, namespaceUri, localName);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    internal void SetShapeText(XmlElement shape, string text)
    {
        XmlElement textBody = GetOrCreateTextBody(shape);
        PptxTextFrame.SetTextBodyText(textBody, PptxDiagnostic.CleanText(text));
    }

    internal XmlElement GetOrCreateTextBody(XmlElement shape)
    {
        return PptxTextFrame.GetOrCreateTextBody(shape, "p", PptxNamespaces.Presentation);
    }

    internal void EnsureTextBodyDefaults(XmlElement textBody)
    {
        PptxTextFrame.EnsureTextBodyDefaults(textBody);
    }

    internal XmlElement? FindFirstChild(XmlElement root, string namespaceUri, string localName)
    {
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                return element;
            }
        }

        return null;
    }

    internal void RemoveParagraphs(XmlElement textBody)
    {
        PptxTextFrame.RemoveParagraphs(textBody);
    }

    internal string GetShapeText(XmlElement shape)
    {
        XmlElement? textBody = FindFirstChild(shape, PptxNamespaces.Presentation, "txBody");
        if (textBody == null)
        {
            return "";
        }

        return PptxTextFrame.GetTextBodyText(textBody);
    }

    internal void CollectText(XmlNode node, StringBuilder builder)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Drawing && element.LocalName == "t")
            {
                builder.Append(element.InnerText);
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                CollectText(child, builder);
            }
        }
    }

    internal XmlElement GetTableCell(XmlDocument document, int tableIndex, int rowIndex, int columnIndex)
    {
        XmlElement table = GetTableElement(document, tableIndex);
        XmlElement? row = FindDirectChildAt(table, PptxNamespaces.Drawing, "tr", rowIndex);
        if (row == null)
        {
            throw new System.ArgumentOutOfRangeException("rowIndex");
        }

        XmlElement? cell = FindDirectChildAt(row, PptxNamespaces.Drawing, "tc", columnIndex);
        if (cell == null)
        {
            throw new System.ArgumentOutOfRangeException("columnIndex");
        }

        return cell;
    }

    internal XmlElement GetTableElement(XmlDocument document, int tableIndex)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            throw new System.ArgumentException("Slide XML root is required.");
        }

        List<XmlElement> tables = new List<XmlElement>();
        CollectTables(root, tables);
        if (tableIndex < 0 || tableIndex >= tables.Count)
        {
            throw new System.ArgumentOutOfRangeException("tableIndex");
        }

        return tables[tableIndex];
    }

    internal void CollectTables(XmlNode node, List<XmlElement> tables)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == PptxNamespaces.Drawing && element.LocalName == "tbl")
            {
                tables.Add(element);
            }
        }

        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child != null)
            {
                CollectTables(child, tables);
            }
        }
    }

    internal XmlElement? FindDirectChildAt(XmlElement parent, string namespaceUri, string localName, int ordinal)
    {
        int current = 0;
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                if (current == ordinal)
                {
                    return element;
                }

                current = current + 1;
            }
        }

        return null;
    }
}
