using System;
using System.Collections.Generic;

namespace odf;

internal class TextFlowDocumentState
{
    internal OdfNode[] OriginalSectionNodes = [];
    internal OdfNode[] OriginalFrameNodes = [];
}

internal static class TextFlowDocumentModel
{
    internal static void LoadTextFlows(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for text-flow materialization.");
        }
        List<OdfTextSection> sections = new List<OdfTextSection>();
        List<OdfTextFrame> frames = new List<OdfTextFrame>();
        List<OdfNode> sectionNodes = new List<OdfNode>();
        List<OdfNode> frameNodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Body != null)
            {
                CollectFlows(
                    document,
                    part.Body,
                    part.Path,
                    sections,
                    frames,
                    sectionNodes,
                    frameNodes);
            }
            partIndex = partIndex + 1;
        }
        int masterIndex = 0;
        while (masterIndex < document.MasterPages.Length)
        {
            OdfMasterPage master = document.MasterPages[masterIndex];
            if (master.Node != null)
            {
                CollectFlows(
                    document,
                    master.Node,
                    master.PartPath,
                    sections,
                    frames,
                    sectionNodes,
                    frameNodes);
            }
            masterIndex = masterIndex + 1;
        }
        document.TextSections = sections.ToArray();
        document.TextFrames = frames.ToArray();
        ResolveNestedSections(document.TextSections);
        ResolveFrameChains(document.TextFrames);
        LoadBreaks(document);
        TextFlowDocumentState state = new TextFlowDocumentState();
        state.OriginalSectionNodes = sectionNodes.ToArray();
        state.OriginalFrameNodes = frameNodes.ToArray();
        document.TextFlowState = state;
    }

    internal static void SynchronizeTextFlows(OdfDocument document)
    {
        if (document == null
            || document.TextSections == null
            || document.TextFrames == null)
        {
            throw new OdfDocumentException(
                "Text-section and text-frame collections are required for writing.");
        }
        SynchronizeBreakStyles(document);
        TextFlowDocumentState state = document.TextFlowState
            ?? new TextFlowDocumentState();
        RemoveDeletedNodes(
            document.TextSections,
            state.OriginalSectionNodes);
        RemoveDeletedFrameNodes(
            document.TextFrames,
            state.OriginalFrameNodes);
        List<OdfNode> sectionNodes = new List<OdfNode>();
        int index = 0;
        while (index < document.TextSections.Length)
        {
            OdfTextSection section = document.TextSections[index];
            if (section == null || section.Node == null)
            {
                throw new OdfEditException(
                    "A text section requires an underlying XML element.");
            }
            ApplySectionFields(section);
            SynchronizeColumnSet(section.Columns);
            sectionNodes.Add(section.Node);
            index = index + 1;
        }
        List<OdfNode> frameNodes = new List<OdfNode>();
        index = 0;
        while (index < document.TextFrames.Length)
        {
            OdfTextFrame frame = document.TextFrames[index];
            if (frame == null || frame.Node == null)
            {
                throw new OdfEditException(
                    "A text frame requires an underlying XML element.");
            }
            ApplyFrameFields(frame);
            ApplyTextBoxFields(frame.TextBox);
            frameNodes.Add(frame.Node);
            index = index + 1;
        }
        state.OriginalSectionNodes = sectionNodes.ToArray();
        state.OriginalFrameNodes = frameNodes.ToArray();
        document.TextFlowState = state;
        ResolveNestedSections(document.TextSections);
        ResolveFrameChains(document.TextFrames);
        LoadBreaks(document);
    }

    internal static void ResolveFrameChains(OdfTextFrame[] frames)
    {
        int index = 0;
        while (index < frames.Length)
        {
            OdfTextFrame frame = frames[index];
            frame.NextFrame = null;
            frame.PreviousFrame = null;
            frame.HasBrokenChain = false;
            frame.HasChainCycle = false;
            index = index + 1;
        }
        index = 0;
        while (index < frames.Length)
        {
            OdfTextFrame frame = frames[index];
            if (frame.ChainNextName.Length > 0)
            {
                OdfTextFrame? next = FindUniqueFrame(
                    frames,
                    frame.ChainNextName,
                    frame.PartPath);
                if (next == null)
                {
                    frame.HasBrokenChain = true;
                }
                else if (next.PreviousFrame != null)
                {
                    frame.HasBrokenChain = true;
                    next.PreviousFrame.HasBrokenChain = true;
                    next.PreviousFrame = null;
                }
                else
                {
                    frame.NextFrame = next;
                    next.PreviousFrame = frame;
                }
            }
            index = index + 1;
        }
        MarkChainCycles(frames);
    }

    static void CollectFlows(
        OdfDocument document,
        OdfNode node,
        string partPath,
        List<OdfTextSection> sections,
        List<OdfTextFrame> frames,
        List<OdfNode> sectionNodes,
        List<OdfNode> frameNodes)
    {
        if (IsElement(node, OdfNamespaces.Text, "section"))
        {
            sections.Add(CreateSection(document, node, partPath));
            sectionNodes.Add(node);
        }
        if (IsElement(node, OdfNamespaces.Drawing, "frame"))
        {
            frames.Add(CreateFrame(document, node, partPath));
            frameNodes.Add(node);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectFlows(
                document,
                node.Children[index],
                partPath,
                sections,
                frames,
                sectionNodes,
                frameNodes);
            index = index + 1;
        }
    }

    static OdfTextSection CreateSection(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfTextSection result = new OdfTextSection();
        result.Name = Read(node, OdfNamespaces.Text, "name");
        result.StyleName = Read(node, OdfNamespaces.Text, "style-name");
        result.Condition = Read(node, OdfNamespaces.Text, "condition");
        result.Display = Read(node, OdfNamespaces.Text, "display");
        result.ProtectedValue = Read(
            node,
            OdfNamespaces.Text,
            "protected");
        result.IsProtected = result.ProtectedValue == "true";
        result.OriginalIsProtected = result.IsProtected;
        result.ProtectionKey = Read(
            node,
            OdfNamespaces.Text,
            "protection-key");
        result.ProtectionKeyDigestAlgorithm = Read(
            node,
            OdfNamespaces.Text,
            "protection-key-digest-algorithm");
        OdfNode? source = FindDirectChild(
            node,
            OdfNamespaces.Text,
            "section-source");
        result.SourceNode = source;
        if (source != null)
        {
            result.SourceHref = Read(source, OdfNamespaces.XLink, "href");
            result.SourceSectionName = Read(
                source,
                OdfNamespaces.Text,
                "section-name");
            result.SourceFilterName = Read(
                source,
                OdfNamespaces.Text,
                "filter-name");
            result.SourceShow = Read(source, OdfNamespaces.XLink, "show");
            result.SourceActuate = Read(
                source,
                OdfNamespaces.XLink,
                "actuate");
        }
        result.Columns = FindSectionColumns(document, result.StyleName);
        result.TextBlocks = ContainedBlocks(document.TextBlocks, node);
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static OdfTextFrame CreateFrame(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfTextFrame result = new OdfTextFrame();
        result.Name = Read(node, OdfNamespaces.Drawing, "name");
        result.StyleName = Read(node, OdfNamespaces.Drawing, "style-name");
        result.TextStyleName = Read(
            node,
            OdfNamespaces.Drawing,
            "text-style-name");
        result.Layer = Read(node, OdfNamespaces.Drawing, "layer");
        result.AnchorType = Read(node, OdfNamespaces.Text, "anchor-type");
        result.AnchorKind = ParseAnchorKind(result.AnchorType);
        result.AnchorPageNumber = Read(
            node,
            OdfNamespaces.Text,
            "anchor-page-number");
        result.X = Read(node, OdfNamespaces.Svg, "x");
        result.Y = Read(node, OdfNamespaces.Svg, "y");
        result.Width = Read(node, OdfNamespaces.Svg, "width");
        result.Height = Read(node, OdfNamespaces.Svg, "height");
        result.MinimumWidth = Read(node, OdfNamespaces.Fo, "min-width");
        result.MinimumHeight = Read(node, OdfNamespaces.Fo, "min-height");
        result.RelativeWidth = Read(
            node,
            OdfNamespaces.Style,
            "rel-width");
        result.RelativeHeight = Read(
            node,
            OdfNamespaces.Style,
            "rel-height");
        result.ZIndex = Read(node, OdfNamespaces.Drawing, "z-index");
        result.Transform = Read(node, OdfNamespaces.Drawing, "transform");
        result.ChainNextName = Read(
            node,
            OdfNamespaces.Drawing,
            "chain-next-name");
        OdfNode? textBoxNode = FindDirectChild(
            node,
            OdfNamespaces.Drawing,
            "text-box");
        if (textBoxNode != null)
        {
            result.TextBox = CreateTextBox(
                document,
                result,
                textBoxNode);
            if (result.TextBox.ChainNextName.Length > 0)
            {
                result.ChainNextName = result.TextBox.ChainNextName;
            }
        }
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static OdfTextBox CreateTextBox(
        OdfDocument document,
        OdfTextFrame frame,
        OdfNode node)
    {
        OdfTextBox result = new OdfTextBox();
        result.XmlId = Read(node, OdfNamespaces.Xml, "id");
        if (result.XmlId.Length == 0)
        {
            result.XmlId = node.XmlId;
        }
        result.TextId = Read(node, OdfNamespaces.Text, "id");
        result.ChainNextName = Read(
            node,
            OdfNamespaces.Drawing,
            "chain-next-name");
        result.MinimumWidth = Read(node, OdfNamespaces.Fo, "min-width");
        result.MinimumHeight = Read(node, OdfNamespaces.Fo, "min-height");
        result.MaximumWidth = Read(node, OdfNamespaces.Fo, "max-width");
        result.MaximumHeight = Read(node, OdfNamespaces.Fo, "max-height");
        result.CornerRadius = Read(node, OdfNamespaces.Drawing, "corner-radius");
        result.TextBlocks = ContainedBlocks(document.TextBlocks, node);
        result.Frame = frame;
        result.Node = node;
        result.OriginalXmlId = result.XmlId;
        result.OriginalTextId = result.TextId;
        result.OriginalChainNextName = result.ChainNextName;
        return result;
    }

    static void ResolveNestedSections(OdfTextSection[] sections)
    {
        int index = 0;
        while (index < sections.Length)
        {
            OdfTextSection parent = sections[index];
            List<OdfTextSection> children = new List<OdfTextSection>();
            int candidateIndex = 0;
            while (candidateIndex < sections.Length)
            {
                OdfTextSection candidate = sections[candidateIndex];
                if (candidate != parent
                    && candidate.Node != null
                    && NearestSectionParent(candidate.Node) == parent.Node)
                {
                    children.Add(candidate);
                }
                candidateIndex = candidateIndex + 1;
            }
            parent.Sections = children.ToArray();
            index = index + 1;
        }
    }

    static OdfNode? NearestSectionParent(OdfNode node)
    {
        OdfNode? current = node.Parent;
        while (current != null)
        {
            if (IsElement(current, OdfNamespaces.Text, "section"))
            {
                return current;
            }
            current = current.Parent;
        }
        return null;
    }

    static OdfTextColumnSet? FindSectionColumns(
        OdfDocument document,
        string styleName)
    {
        OdfStyle? style = FindStyle(
            document,
            styleName,
            OdfStyleFamily.Section);
        if (style == null)
        {
            return null;
        }
        OdfNode? columns = FindStyleChild(
            style.SectionPropertyChildren,
            "columns");
        if (columns == null)
        {
            StyleResolutionResult resolution =
                StyleResolver.ResolveEffectiveProperties(document, style);
            OdfNode? properties = FindStyleChild(
                resolution.EffectiveProperties,
                "section-properties");
            if (properties != null)
            {
                columns = FindDirectChild(
                    properties,
                    OdfNamespaces.Style,
                    "columns");
            }
        }
        if (columns == null)
        {
            return null;
        }
        return CreateColumnSet(columns);
    }

    static OdfTextColumnSet CreateColumnSet(OdfNode node)
    {
        OdfTextColumnSet result = new OdfTextColumnSet();
        result.Count = ParseNonNegativeInteger(
            Read(node, OdfNamespaces.Fo, "column-count"));
        result.Gap = Read(node, OdfNamespaces.Fo, "column-gap");
        List<OdfTextColumn> columns = new List<OdfTextColumn>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, OdfNamespaces.Style, "column"))
            {
                OdfTextColumn column = new OdfTextColumn();
                column.RelativeWidth = Read(
                    child,
                    OdfNamespaces.Style,
                    "rel-width");
                column.StartIndent = Read(
                    child,
                    OdfNamespaces.Fo,
                    "start-indent");
                column.EndIndent = Read(
                    child,
                    OdfNamespaces.Fo,
                    "end-indent");
                column.Node = child;
                columns.Add(column);
            }
            else if (IsElement(child, OdfNamespaces.Style, "column-sep"))
            {
                result.SeparatorNode = child;
                result.SeparatorStyle = Read(
                    child,
                    OdfNamespaces.Style,
                    "style");
                result.SeparatorWidth = Read(
                    child,
                    OdfNamespaces.Style,
                    "width");
                result.SeparatorColor = Read(
                    child,
                    OdfNamespaces.Style,
                    "color");
                result.SeparatorHeight = Read(
                    child,
                    OdfNamespaces.Style,
                    "height");
                result.SeparatorVerticalAlignment = Read(
                    child,
                    OdfNamespaces.Style,
                    "vertical-align");
            }
            index = index + 1;
        }
        result.Columns = columns.ToArray();
        result.Node = node;
        return result;
    }

    static void LoadBreaks(OdfDocument document)
    {
        List<OdfTextBreak> breaks = new List<OdfTextBreak>();
        int blockIndex = 0;
        while (blockIndex < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[blockIndex];
            block.BreakBefore = EffectiveParagraphProperty(
                document,
                block,
                "break-before");
            block.BreakAfter = EffectiveParagraphProperty(
                document,
                block,
                "break-after");
            block.OriginalBreakBefore = block.BreakBefore;
            block.OriginalBreakAfter = block.BreakAfter;
            AddStyleBreak(breaks, block, block.BreakBefore, true);
            AddStyleBreak(breaks, block, block.BreakAfter, false);
            CollectRunBreaks(breaks, block, block.Runs);
            blockIndex = blockIndex + 1;
        }
        document.TextBreaks = breaks.ToArray();
    }

    static void CollectRunBreaks(
        List<OdfTextBreak> breaks,
        OdfTextBlock block,
        OdfTextRun[] runs)
    {
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run.Kind == OdfTextRunKind.LineBreak
                || run.Kind == OdfTextRunKind.SoftPageBreak)
            {
                OdfTextBreak item = new OdfTextBreak();
                item.Kind = run.Kind == OdfTextRunKind.LineBreak
                    ? OdfTextBreakKind.Line
                    : OdfTextBreakKind.SoftPage;
                item.Value = run.Kind == OdfTextRunKind.LineBreak
                    ? "line"
                    : "page";
                item.Block = block;
                item.Run = run;
                item.Node = run.Node;
                breaks.Add(item);
            }
            if (run.Runs.Length > 0)
            {
                CollectRunBreaks(breaks, block, run.Runs);
            }
            index = index + 1;
        }
    }

    static void AddStyleBreak(
        List<OdfTextBreak> breaks,
        OdfTextBlock block,
        string value,
        bool before)
    {
        OdfTextBreakKind kind = ParseBreakKind(value, before);
        if (kind == OdfTextBreakKind.Unknown)
        {
            return;
        }
        OdfTextBreak item = new OdfTextBreak();
        item.Kind = kind;
        item.Value = value;
        item.IsBefore = before;
        item.Block = block;
        item.Node = block.Node;
        breaks.Add(item);
    }

    static string EffectiveParagraphProperty(
        OdfDocument document,
        OdfTextBlock block,
        string localName)
    {
        OdfStyle? style = FindStyle(
            document,
            block.StyleName,
            OdfStyleFamily.Paragraph);
        if (style == null)
        {
            return "";
        }
        StyleResolutionResult resolution =
            StyleResolver.ResolveEffectiveProperties(document, style);
        OdfNode? properties = FindStyleChild(
            resolution.EffectiveProperties,
            "paragraph-properties");
        if (properties == null)
        {
            return "";
        }
        return Read(properties, OdfNamespaces.Fo, localName);
    }

    static void SynchronizeBreakStyles(OdfDocument document)
    {
        int index = 0;
        while (index < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[index];
            if (block.BreakBefore != block.OriginalBreakBefore)
            {
                WriteParagraphBreak(
                    document,
                    block,
                    "break-before",
                    block.BreakBefore);
                block.OriginalBreakBefore = block.BreakBefore;
            }
            if (block.BreakAfter != block.OriginalBreakAfter)
            {
                WriteParagraphBreak(
                    document,
                    block,
                    "break-after",
                    block.BreakAfter);
                block.OriginalBreakAfter = block.BreakAfter;
            }
            index = index + 1;
        }
    }

    static void WriteParagraphBreak(
        OdfDocument document,
        OdfTextBlock block,
        string localName,
        string value)
    {
        OdfStyle? style = FindStyle(
            document,
            block.StyleName,
            OdfStyleFamily.Paragraph);
        if (style == null)
        {
            throw new OdfEditException(
                "A paragraph break requires a referenced paragraph style.");
        }
        OdfStyleProperty? property = FindProperty(
            style.ParagraphProperties,
            OdfNamespaces.Fo,
            localName);
        if (property != null)
        {
            property.LexicalValue = value;
            return;
        }
        if (value.Length == 0)
        {
            return;
        }
        OdfStyleProperty created = new OdfStyleProperty();
        created.Name.NamespaceName = OdfNamespaces.Fo;
        created.Name.LocalName = localName;
        created.Name.Prefix = OdfNamespaces.FoPrefix;
        created.LexicalValue = value;
        OdfStyleProperty[] properties = new OdfStyleProperty[
            style.ParagraphProperties.Length + 1];
        Array.Copy(
            style.ParagraphProperties,
            properties,
            style.ParagraphProperties.Length);
        properties[properties.Length - 1] = created;
        style.ParagraphProperties = properties;
    }

    static void ApplySectionFields(OdfTextSection section)
    {
        OdfNode node = section.Node ?? new OdfNode();
        WriteOptional(node, OdfNamespaces.Text, "name", OdfNamespaces.TextPrefix, section.Name);
        WriteOptional(node, OdfNamespaces.Text, "style-name", OdfNamespaces.TextPrefix, section.StyleName);
        WriteOptional(node, OdfNamespaces.Text, "condition", OdfNamespaces.TextPrefix, section.Condition);
        WriteOptional(node, OdfNamespaces.Text, "display", OdfNamespaces.TextPrefix, section.Display);
        string protectedValue = section.ProtectedValue;
        if (section.IsProtected != section.OriginalIsProtected)
        {
            protectedValue = section.IsProtected ? "true" : "false";
        }
        WriteOptional(node, OdfNamespaces.Text, "protected", OdfNamespaces.TextPrefix, protectedValue);
        section.ProtectedValue = protectedValue;
        section.IsProtected = protectedValue == "true";
        section.OriginalIsProtected = section.IsProtected;
        WriteOptional(node, OdfNamespaces.Text, "protection-key", OdfNamespaces.TextPrefix, section.ProtectionKey);
        WriteOptional(node, OdfNamespaces.Text, "protection-key-digest-algorithm", OdfNamespaces.TextPrefix, section.ProtectionKeyDigestAlgorithm);
        OdfNode? source = section.SourceNode;
        if (source == null && HasSource(section))
        {
            source = CreateElement(OdfNamespaces.Text, "section-source", OdfNamespaces.TextPrefix, node.Location.PartPath);
            PrependNode(node, source);
            section.SourceNode = source;
        }
        if (source != null)
        {
            if (HasSource(section))
            {
                StyleDocumentModel.WriteAttribute(source, OdfNamespaces.XLink, "type", OdfNamespaces.XLinkPrefix, "simple");
            }
            WriteOptional(source, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, section.SourceHref);
            WriteOptional(source, OdfNamespaces.Text, "section-name", OdfNamespaces.TextPrefix, section.SourceSectionName);
            WriteOptional(source, OdfNamespaces.Text, "filter-name", OdfNamespaces.TextPrefix, section.SourceFilterName);
            WriteOptional(source, OdfNamespaces.XLink, "show", OdfNamespaces.XLinkPrefix, section.SourceShow);
            WriteOptional(source, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, section.SourceActuate);
        }
    }

    static void ApplyFrameFields(OdfTextFrame frame)
    {
        OdfNode node = frame.Node ?? new OdfNode();
        WriteOptional(node, OdfNamespaces.Drawing, "name", OdfNamespaces.DrawingPrefix, frame.Name);
        WriteOptional(node, OdfNamespaces.Drawing, "style-name", OdfNamespaces.DrawingPrefix, frame.StyleName);
        WriteOptional(node, OdfNamespaces.Drawing, "text-style-name", OdfNamespaces.DrawingPrefix, frame.TextStyleName);
        WriteOptional(node, OdfNamespaces.Drawing, "layer", OdfNamespaces.DrawingPrefix, frame.Layer);
        if (frame.AnchorType.Length == 0 && frame.AnchorKind != OdfTextAnchorKind.Unknown)
        {
            frame.AnchorType = AnchorKindText(frame.AnchorKind);
        }
        else
        {
            frame.AnchorKind = ParseAnchorKind(frame.AnchorType);
        }
        WriteOptional(node, OdfNamespaces.Text, "anchor-type", OdfNamespaces.TextPrefix, frame.AnchorType);
        WriteOptional(node, OdfNamespaces.Text, "anchor-page-number", OdfNamespaces.TextPrefix, frame.AnchorPageNumber);
        WriteOptional(node, OdfNamespaces.Svg, "x", OdfNamespaces.SvgPrefix, frame.X);
        WriteOptional(node, OdfNamespaces.Svg, "y", OdfNamespaces.SvgPrefix, frame.Y);
        WriteOptional(node, OdfNamespaces.Svg, "width", OdfNamespaces.SvgPrefix, frame.Width);
        WriteOptional(node, OdfNamespaces.Svg, "height", OdfNamespaces.SvgPrefix, frame.Height);
        WriteOptional(node, OdfNamespaces.Fo, "min-width", OdfNamespaces.FoPrefix, frame.MinimumWidth);
        WriteOptional(node, OdfNamespaces.Fo, "min-height", OdfNamespaces.FoPrefix, frame.MinimumHeight);
        WriteOptional(node, OdfNamespaces.Style, "rel-width", OdfNamespaces.StylePrefix, frame.RelativeWidth);
        WriteOptional(node, OdfNamespaces.Style, "rel-height", OdfNamespaces.StylePrefix, frame.RelativeHeight);
        WriteOptional(node, OdfNamespaces.Drawing, "z-index", OdfNamespaces.DrawingPrefix, frame.ZIndex);
        WriteOptional(node, OdfNamespaces.Drawing, "transform", OdfNamespaces.DrawingPrefix, frame.Transform);
        if (frame.TextBox == null)
        {
            WriteOptional(node, OdfNamespaces.Drawing, "chain-next-name", OdfNamespaces.DrawingPrefix, frame.ChainNextName);
        }
        else
        {
            if (frame.TextBox.ChainNextName
                != frame.TextBox.OriginalChainNextName)
            {
                frame.ChainNextName = frame.TextBox.ChainNextName;
            }
            else
            {
                frame.TextBox.ChainNextName = frame.ChainNextName;
            }
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Drawing,
                "chain-next-name");
        }
    }

    static void ApplyTextBoxFields(OdfTextBox? textBox)
    {
        if (textBox == null || textBox.Node == null)
        {
            return;
        }
        WriteOptional(textBox.Node, OdfNamespaces.Xml, "id", OdfNamespaces.XmlPrefix, textBox.XmlId);
        WriteOptional(textBox.Node, OdfNamespaces.Text, "id", OdfNamespaces.TextPrefix, textBox.TextId);
        WriteOptional(textBox.Node, OdfNamespaces.Drawing, "chain-next-name", OdfNamespaces.DrawingPrefix, textBox.ChainNextName);
        WriteOptional(textBox.Node, OdfNamespaces.Fo, "min-width", OdfNamespaces.FoPrefix, textBox.MinimumWidth);
        WriteOptional(textBox.Node, OdfNamespaces.Fo, "min-height", OdfNamespaces.FoPrefix, textBox.MinimumHeight);
        WriteOptional(textBox.Node, OdfNamespaces.Fo, "max-width", OdfNamespaces.FoPrefix, textBox.MaximumWidth);
        WriteOptional(textBox.Node, OdfNamespaces.Fo, "max-height", OdfNamespaces.FoPrefix, textBox.MaximumHeight);
        WriteOptional(textBox.Node, OdfNamespaces.Drawing, "corner-radius", OdfNamespaces.DrawingPrefix, textBox.CornerRadius);
        textBox.OriginalXmlId = textBox.XmlId;
        textBox.OriginalTextId = textBox.TextId;
        textBox.OriginalChainNextName = textBox.ChainNextName;
    }

    static void SynchronizeColumnSet(OdfTextColumnSet? set)
    {
        if (set == null || set.Node == null)
        {
            return;
        }
        string count = set.Count > 0 ? set.Count.ToString() : "";
        WriteOptional(set.Node, OdfNamespaces.Fo, "column-count", OdfNamespaces.FoPrefix, count);
        WriteOptional(set.Node, OdfNamespaces.Fo, "column-gap", OdfNamespaces.FoPrefix, set.Gap);
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < set.Node.Children.Length)
        {
            OdfNode child = set.Node.Children[index];
            if (!IsElement(child, OdfNamespaces.Style, "column")
                && !IsElement(child, OdfNamespaces.Style, "column-sep"))
            {
                children.Add(child);
            }
            index = index + 1;
        }
        index = 0;
        while (index < set.Columns.Length)
        {
            OdfTextColumn column = set.Columns[index];
            OdfNode node = column.Node
                ?? CreateElement(OdfNamespaces.Style, "column", OdfNamespaces.StylePrefix, set.Node.Location.PartPath);
            WriteOptional(node, OdfNamespaces.Style, "rel-width", OdfNamespaces.StylePrefix, column.RelativeWidth);
            WriteOptional(node, OdfNamespaces.Fo, "start-indent", OdfNamespaces.FoPrefix, column.StartIndent);
            WriteOptional(node, OdfNamespaces.Fo, "end-indent", OdfNamespaces.FoPrefix, column.EndIndent);
            node.Parent = set.Node;
            column.Node = node;
            children.Add(node);
            index = index + 1;
        }
        OdfNode? separator = set.SeparatorNode;
        if (separator == null && HasSeparator(set))
        {
            separator = CreateElement(OdfNamespaces.Style, "column-sep", OdfNamespaces.StylePrefix, set.Node.Location.PartPath);
        }
        if (separator != null && HasSeparator(set))
        {
            WriteOptional(separator, OdfNamespaces.Style, "style", OdfNamespaces.StylePrefix, set.SeparatorStyle);
            WriteOptional(separator, OdfNamespaces.Style, "width", OdfNamespaces.StylePrefix, set.SeparatorWidth);
            WriteOptional(separator, OdfNamespaces.Style, "color", OdfNamespaces.StylePrefix, set.SeparatorColor);
            WriteOptional(separator, OdfNamespaces.Style, "height", OdfNamespaces.StylePrefix, set.SeparatorHeight);
            WriteOptional(separator, OdfNamespaces.Style, "vertical-align", OdfNamespaces.StylePrefix, set.SeparatorVerticalAlignment);
            separator.Parent = set.Node;
            set.SeparatorNode = separator;
            children.Add(separator);
        }
        set.Node.Children = children.ToArray();
    }

    static void RemoveDeletedNodes(
        OdfTextSection[] sections,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            bool retained = false;
            int sectionIndex = 0;
            while (sectionIndex < sections.Length && !retained)
            {
                retained = sections[sectionIndex] != null
                    && sections[sectionIndex].Node == node;
                sectionIndex = sectionIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static void RemoveDeletedFrameNodes(
        OdfTextFrame[] frames,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            bool retained = false;
            int frameIndex = 0;
            while (frameIndex < frames.Length && !retained)
            {
                retained = frames[frameIndex] != null
                    && frames[frameIndex].Node == node;
                frameIndex = frameIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static void MarkChainCycles(OdfTextFrame[] frames)
    {
        int index = 0;
        while (index < frames.Length)
        {
            List<OdfTextFrame> path = new List<OdfTextFrame>();
            OdfTextFrame? current = frames[index];
            while (current != null && !path.Contains(current))
            {
                path.Add(current);
                current = current.NextFrame;
            }
            if (current != null)
            {
                int cycleStart = path.IndexOf(current);
                int cycleIndex = cycleStart;
                while (cycleIndex >= 0 && cycleIndex < path.Count)
                {
                    path[cycleIndex].HasChainCycle = true;
                    cycleIndex = cycleIndex + 1;
                }
            }
            index = index + 1;
        }
    }

    static OdfTextFrame? FindUniqueFrame(
        OdfTextFrame[] frames,
        string name,
        string partPath)
    {
        OdfTextFrame? result = null;
        int count = 0;
        int index = 0;
        while (index < frames.Length)
        {
            OdfTextFrame frame = frames[index];
            if (frame.Name == name && frame.PartPath == partPath)
            {
                result = frame;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static OdfTextBlock[] ContainedBlocks(
        OdfTextBlock[] blocks,
        OdfNode ancestor)
    {
        List<OdfTextBlock> result = new List<OdfTextBlock>();
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextBlock block = blocks[index];
            if (block.Node != null && IsDescendant(block.Node, ancestor))
            {
                result.Add(block);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static bool IsDescendant(OdfNode node, OdfNode ancestor)
    {
        OdfNode? current = node.Parent;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static OdfStyle? FindStyle(
        OdfDocument document,
        string name,
        OdfStyleFamily family)
    {
        OdfStyle? defaultStyle = null;
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Family == family)
            {
                if (style.Name == name && !style.IsDefault)
                {
                    return style;
                }
                if (name.Length == 0 && style.IsDefault)
                {
                    defaultStyle = style;
                }
            }
            index = index + 1;
        }
        return defaultStyle;
    }

    static OdfStyleProperty? FindProperty(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property.Name.NamespaceName == namespaceName
                && property.Name.LocalName == localName)
            {
                return property;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindStyleChild(OdfNode[] nodes, string localName)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            OdfNode node = nodes[index];
            if (IsElement(node, OdfNamespaces.Style, localName))
            {
                return node;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirectChild(
        OdfNode parent,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (IsElement(child, namespaceName, localName))
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
    }

    static int ParseNonNegativeInteger(string text)
    {
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(text);
        if (!parsed.IsValid
            || parsed.IntegerValue < 0
            || parsed.IntegerValue > int.MaxValue)
        {
            return 0;
        }
        return (int)parsed.IntegerValue;
    }

    static OdfTextAnchorKind ParseAnchorKind(string value)
    {
        if (value == "paragraph")
        {
            return OdfTextAnchorKind.Paragraph;
        }
        if (value == "char")
        {
            return OdfTextAnchorKind.Character;
        }
        if (value == "as-char")
        {
            return OdfTextAnchorKind.AsCharacter;
        }
        if (value == "page")
        {
            return OdfTextAnchorKind.Page;
        }
        if (value == "frame")
        {
            return OdfTextAnchorKind.Frame;
        }
        if (value == "cell")
        {
            return OdfTextAnchorKind.Cell;
        }
        return OdfTextAnchorKind.Unknown;
    }

    static string AnchorKindText(OdfTextAnchorKind kind)
    {
        if (kind == OdfTextAnchorKind.Paragraph)
        {
            return "paragraph";
        }
        if (kind == OdfTextAnchorKind.Character)
        {
            return "char";
        }
        if (kind == OdfTextAnchorKind.AsCharacter)
        {
            return "as-char";
        }
        if (kind == OdfTextAnchorKind.Page)
        {
            return "page";
        }
        if (kind == OdfTextAnchorKind.Frame)
        {
            return "frame";
        }
        if (kind == OdfTextAnchorKind.Cell)
        {
            return "cell";
        }
        return "";
    }

    static OdfTextBreakKind ParseBreakKind(string value, bool before)
    {
        if (value == "page")
        {
            return before
                ? OdfTextBreakKind.PageBefore
                : OdfTextBreakKind.PageAfter;
        }
        if (value == "column")
        {
            return before
                ? OdfTextBreakKind.ColumnBefore
                : OdfTextBreakKind.ColumnAfter;
        }
        if (value == "even-page")
        {
            return before
                ? OdfTextBreakKind.EvenPageBefore
                : OdfTextBreakKind.EvenPageAfter;
        }
        if (value == "odd-page")
        {
            return before
                ? OdfTextBreakKind.OddPageBefore
                : OdfTextBreakKind.OddPageAfter;
        }
        return OdfTextBreakKind.Unknown;
    }

    static void WriteOptional(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static bool HasSource(OdfTextSection section)
    {
        return section.SourceHref.Length > 0
            || section.SourceSectionName.Length > 0
            || section.SourceFilterName.Length > 0
            || section.SourceShow.Length > 0
            || section.SourceActuate.Length > 0;
    }

    static bool HasSeparator(OdfTextColumnSet set)
    {
        return set.SeparatorStyle.Length > 0
            || set.SeparatorWidth.Length > 0
            || set.SeparatorColor.Length > 0
            || set.SeparatorHeight.Length > 0
            || set.SeparatorVerticalAlignment.Length > 0;
    }

    static OdfNode CreateElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = namespaceName;
        result.Name.LocalName = localName;
        result.Name.Prefix = prefix;
        result.Location.PartPath = partPath;
        return result;
    }

    static void PrependNode(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        children[0] = child;
        Array.Copy(parent.Children, 0, children, 1, parent.Children.Length);
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
    }
}
