using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class TextDocumentState
{
    internal OdfNode[] OriginalNodes = [];
}

internal static class TextDocumentModel
{
    internal static void LoadTextBlocks(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for text materialization.");
        }
        List<OdfTextBlock> blocks = new List<OdfTextBlock>();
        List<OdfNode> nodes = new List<OdfNode>();
        int maximumTextLength = document.LoadOptions.SecurityLimits
            .MaximumXmlTextLength;
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Body != null)
            {
                CollectTextBlocks(
                    part.Body,
                    part.Path,
                    maximumTextLength,
                    blocks,
                    nodes);
            }
            partIndex = partIndex + 1;
        }
        int masterIndex = 0;
        while (masterIndex < document.MasterPages.Length)
        {
            OdfMasterPage master = document.MasterPages[masterIndex];
            if (master.Node != null)
            {
                CollectTextBlocks(
                    master.Node,
                    master.PartPath,
                    maximumTextLength,
                    blocks,
                    nodes);
            }
            masterIndex = masterIndex + 1;
        }
        document.TextBlocks = blocks.ToArray();
        TextDocumentState state = new TextDocumentState();
        state.OriginalNodes = nodes.ToArray();
        document.TextState = state;
    }

    internal static void SynchronizeTextBlocks(OdfDocument document)
    {
        if (document == null || document.TextBlocks == null)
        {
            throw new OdfDocumentException(
                "A document and text-block collection are required for text writing.");
        }
        TextDocumentState state = document.TextState
            ?? new TextDocumentState();
        RemoveDeletedBlocks(document.TextBlocks, state.OriginalNodes);
        List<OdfNode> currentNodes = new List<OdfNode>();
        int index = 0;
        while (index < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[index];
            if (block == null)
            {
                throw new OdfDocumentException(
                    "The document text-block collection contains a null block.");
            }
            SynchronizeTextBlock(document, block);
            if (block.OriginalNode != null)
            {
                currentNodes.Add(block.OriginalNode);
            }
            index = index + 1;
        }
        state.OriginalNodes = currentNodes.ToArray();
        document.TextState = state;
    }

    static void CollectTextBlocks(
        OdfNode node,
        string partPath,
        int maximumTextLength,
        List<OdfTextBlock> blocks,
        List<OdfNode> nodes)
    {
        if (IsTextBlockNode(node))
        {
            OdfTextBlock block = CreateTextBlock(
                node,
                partPath,
                maximumTextLength);
            blocks.Add(block);
            nodes.Add(node);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectTextBlocks(
                node.Children[index],
                partPath,
                maximumTextLength,
                blocks,
                nodes);
            index = index + 1;
        }
    }

    static bool IsTextBlockNode(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text
            && (node.Name.LocalName == "p" || node.Name.LocalName == "h");
    }

    internal static OdfTextBlock CreateTextBlock(
        OdfNode node,
        string partPath,
        int maximumTextLength)
    {
        OdfTextBlock result = new OdfTextBlock();
        result.IsHeading = node.Name != null && node.Name.LocalName == "h";
        result.OutlineLevel = ReadNonNegativeInteger(
            node,
            OdfNamespaces.Text,
            "outline-level",
            0,
            maximumTextLength);
        result.RestartNumbering = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "restart-numbering");
        result.StartValue = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "start-value");
        result.IsListHeader = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "is-list-header");
        result.StyleName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "style-name");
        result.ClassNames = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "class-names");
        result.ConditionalStyleName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "cond-style-name");
        result.Runs = CreateTextRuns(node.Children, maximumTextLength);
        result.Text = TextExtractor.ExtractRunText(result.Runs);
        result.PartPath = partPath;
        result.Node = node;
        result.OriginalNode = node;
        SnapshotTextBlock(result);
        return result;
    }

    static OdfTextRun[] CreateTextRuns(
        OdfNode[] nodes,
        int maximumTextLength)
    {
        OdfTextRun[] result = new OdfTextRun[nodes.Length];
        int index = 0;
        while (index < nodes.Length)
        {
            result[index] = CreateTextRun(nodes[index], maximumTextLength);
            index = index + 1;
        }
        return result;
    }

    static OdfTextRun CreateTextRun(
        OdfNode node,
        int maximumTextLength)
    {
        OdfTextRun result = new OdfTextRun();
        result.Node = node;
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Kind = OdfTextRunKind.Text;
            result.Text = node.Value;
            return result;
        }
        if (node.Kind != OdfNodeKind.Element || node.Name == null)
        {
            result.Kind = OdfTextRunKind.Unknown;
            return result;
        }
        result.Kind = ClassifyTextRun(node);
        result.Attributes = node.Attributes;
        result.StyleName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "style-name");
        result.ClassNames = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "class-names");
        if (IsAnnotationControl(node))
        {
            return result;
        }
        if (result.Kind == OdfTextRunKind.Hyperlink)
        {
            LoadHyperlinkFields(result, node);
        }
        if (result.Kind == OdfTextRunKind.Ruby)
        {
            LoadRubyFields(result, node, maximumTextLength);
            return result;
        }
        if (IsMarkerRunKind(result.Kind))
        {
            LoadMarkerFields(result, node);
            return result;
        }
        if (result.Kind == OdfTextRunKind.Space)
        {
            result.SpaceCount = ReadNonNegativeInteger(
                node,
                OdfNamespaces.Text,
                "c",
                1,
                maximumTextLength);
            result.Text = new string(' ', result.SpaceCount);
            return result;
        }
        if (result.Kind == OdfTextRunKind.Tab)
        {
            result.TabReference = StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Text,
                "tab-ref");
            result.Text = "\t";
            return result;
        }
        if (result.Kind == OdfTextRunKind.LineBreak)
        {
            result.Text = "\n";
            return result;
        }
        if (result.Kind == OdfTextRunKind.SoftPageBreak)
        {
            return result;
        }
        result.Runs = CreateTextRuns(node.Children, maximumTextLength);
        result.Text = TextExtractor.ExtractRunText(result.Runs);
        return result;
    }

    static bool IsAnnotationControl(OdfNode node)
    {
        return node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Office
            && (node.Name.LocalName == "annotation"
                || node.Name.LocalName == "annotation-end");
    }

    static OdfTextRunKind ClassifyTextRun(OdfNode node)
    {
        if (node.Name == null || node.Name.NamespaceName != OdfNamespaces.Text)
        {
            return OdfTextRunKind.Unknown;
        }
        if (node.Name.LocalName == "span")
        {
            return OdfTextRunKind.Span;
        }
        if (node.Name.LocalName == "a")
        {
            return OdfTextRunKind.Hyperlink;
        }
        if (node.Name.LocalName == "s")
        {
            return OdfTextRunKind.Space;
        }
        if (node.Name.LocalName == "tab")
        {
            return OdfTextRunKind.Tab;
        }
        if (node.Name.LocalName == "line-break")
        {
            return OdfTextRunKind.LineBreak;
        }
        if (node.Name.LocalName == "soft-page-break")
        {
            return OdfTextRunKind.SoftPageBreak;
        }
        if (node.Name.LocalName == "bookmark")
        {
            return OdfTextRunKind.Bookmark;
        }
        if (node.Name.LocalName == "bookmark-start")
        {
            return OdfTextRunKind.BookmarkStart;
        }
        if (node.Name.LocalName == "bookmark-end")
        {
            return OdfTextRunKind.BookmarkEnd;
        }
        if (node.Name.LocalName == "reference-mark")
        {
            return OdfTextRunKind.ReferenceMark;
        }
        if (node.Name.LocalName == "reference-mark-start")
        {
            return OdfTextRunKind.ReferenceMarkStart;
        }
        if (node.Name.LocalName == "reference-mark-end")
        {
            return OdfTextRunKind.ReferenceMarkEnd;
        }
        if (node.Name.LocalName == "toc-mark")
        {
            return OdfTextRunKind.TableOfContentMark;
        }
        if (node.Name.LocalName == "toc-mark-start")
        {
            return OdfTextRunKind.TableOfContentMarkStart;
        }
        if (node.Name.LocalName == "toc-mark-end")
        {
            return OdfTextRunKind.TableOfContentMarkEnd;
        }
        if (node.Name.LocalName == "user-index-mark")
        {
            return OdfTextRunKind.UserIndexMark;
        }
        if (node.Name.LocalName == "user-index-mark-start")
        {
            return OdfTextRunKind.UserIndexMarkStart;
        }
        if (node.Name.LocalName == "user-index-mark-end")
        {
            return OdfTextRunKind.UserIndexMarkEnd;
        }
        if (node.Name.LocalName == "alphabetical-index-mark")
        {
            return OdfTextRunKind.AlphabeticalIndexMark;
        }
        if (node.Name.LocalName == "alphabetical-index-mark-start")
        {
            return OdfTextRunKind.AlphabeticalIndexMarkStart;
        }
        if (node.Name.LocalName == "alphabetical-index-mark-end")
        {
            return OdfTextRunKind.AlphabeticalIndexMarkEnd;
        }
        if (node.Name.LocalName == "bibliography-mark")
        {
            return OdfTextRunKind.BibliographyMark;
        }
        if (node.Name.LocalName == "ruby")
        {
            return OdfTextRunKind.Ruby;
        }
        return OdfTextRunKind.Unknown;
    }

    static bool IsMarkerRunKind(OdfTextRunKind kind)
    {
        return kind >= OdfTextRunKind.Bookmark
            && kind <= OdfTextRunKind.BibliographyMark;
    }

    static void LoadMarkerFields(OdfTextRun run, OdfNode node)
    {
        string markerName = MarkerNameAttribute(run.Kind);
        if (markerName.Length > 0)
        {
            run.MarkerName = StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Text,
                markerName);
        }
        run.IndexName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "index-name");
        run.StringValue = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "string-value");
        run.Key1 = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "key1");
        run.Key2 = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "key2");
        run.MarkerOutlineLevel = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "outline-level");
        run.BibliographyType = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "bibliography-type");
    }

    static string MarkerNameAttribute(OdfTextRunKind kind)
    {
        if (kind == OdfTextRunKind.Bookmark
            || kind == OdfTextRunKind.BookmarkStart
            || kind == OdfTextRunKind.BookmarkEnd
            || kind == OdfTextRunKind.ReferenceMark
            || kind == OdfTextRunKind.ReferenceMarkStart
            || kind == OdfTextRunKind.ReferenceMarkEnd)
        {
            return "name";
        }
        if (kind == OdfTextRunKind.TableOfContentMarkStart
            || kind == OdfTextRunKind.TableOfContentMarkEnd
            || kind == OdfTextRunKind.UserIndexMarkStart
            || kind == OdfTextRunKind.UserIndexMarkEnd
            || kind == OdfTextRunKind.AlphabeticalIndexMarkStart
            || kind == OdfTextRunKind.AlphabeticalIndexMarkEnd)
        {
            return "id";
        }
        if (kind == OdfTextRunKind.BibliographyMark)
        {
            return "identifier";
        }
        return "";
    }

    static void LoadRubyFields(
        OdfTextRun run,
        OdfNode node,
        int maximumTextLength)
    {
        OdfNode? rubyBase = FindTextChild(node, "ruby-base");
        OdfNode? rubyText = FindTextChild(node, "ruby-text");
        if (rubyBase != null)
        {
            run.Runs = CreateTextRuns(rubyBase.Children, maximumTextLength);
            run.Text = TextExtractor.ExtractRunText(run.Runs);
        }
        if (rubyText != null)
        {
            OdfTextRun[] annotationRuns = CreateTextRuns(
                rubyText.Children,
                maximumTextLength);
            run.RubyText = TextExtractor.ExtractRunText(annotationRuns);
            run.RubyTextStyleName = StyleDocumentModel.ReadAttribute(
                rubyText,
                OdfNamespaces.Text,
                "style-name");
        }
    }

    static OdfNode? FindTextChild(OdfNode parent, string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Text
                && child.Name.LocalName == localName)
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static void LoadHyperlinkFields(OdfTextRun run, OdfNode node)
    {
        run.Href = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.XLink,
            "href");
        run.LinkName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Office,
            "name");
        run.LinkTitle = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Office,
            "title");
        run.TargetFrameName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Office,
            "target-frame-name");
        run.LinkShow = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.XLink,
            "show");
        run.LinkActuate = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.XLink,
            "actuate");
        run.VisitedStyleName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "visited-style-name");
    }

    static int ReadNonNegativeInteger(
        OdfNode node,
        string namespaceName,
        string localName,
        int defaultValue,
        int maximumValue)
    {
        string text = StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
        if (text.Length == 0)
        {
            return defaultValue;
        }
        OdfLexicalValue value = OdfLexicalParser.ParseInteger(text);
        if (!value.IsValid || value.IntegerValue < 0)
        {
            return defaultValue;
        }
        if (value.IntegerValue > maximumValue)
        {
            return maximumValue;
        }
        return (int)value.IntegerValue;
    }

    static void SynchronizeTextBlock(
        OdfDocument document,
        OdfTextBlock block)
    {
        bool isNew = block.OriginalNode == null;
        OdfNode node = block.OriginalNode
            ?? CreateTextBlockNode(block, SelectTextPartPath(document, block));
        if (isNew)
        {
            AppendNewTextBlock(document, node);
            block.OriginalNode = node;
        }
        NormalizeBlockText(block);
        ApplyTextBlockFields(block, node, isNew);
        string signature = CreateRunSignature(block.Runs);
        if (isNew || signature != block.OriginalRunSignature)
        {
            node.Children = BuildRunNodes(block.Runs, node);
        }
        block.Node = node;
        block.PartPath = node.Location.PartPath;
        SnapshotTextBlock(block);
    }

    static string SelectTextPartPath(
        OdfDocument document,
        OdfTextBlock block)
    {
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            return "";
        }
        if (block.PartPath.Length > 0)
        {
            return block.PartPath;
        }
        return OdfPartNames.Content;
    }

    static OdfNode CreateTextBlockNode(
        OdfTextBlock block,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = QualifiedTextName(block.IsHeading ? "h" : "p");
        result.Location.PartPath = partPath;
        return result;
    }

    static void AppendNewTextBlock(OdfDocument document, OdfNode node)
    {
        if (document.Body == null)
        {
            throw new OdfEditException(
                "A new text block requires a materialized document body.");
        }
        OdfNode target = document.Body;
        int index = 0;
        while (index < document.Body.Children.Length)
        {
            OdfNode child = document.Body.Children[index];
            if (child.Kind == OdfNodeKind.Element)
            {
                target = child;
                index = document.Body.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        OdfNode[] children = new OdfNode[target.Children.Length + 1];
        Array.Copy(target.Children, children, target.Children.Length);
        children[children.Length - 1] = node;
        target.Children = children;
        node.Parent = target;
        target.WasEmptyElement = false;
    }

    static void NormalizeBlockText(OdfTextBlock block)
    {
        if (block.Text != block.OriginalText)
        {
            OdfTextRun run = new OdfTextRun();
            run.Kind = OdfTextRunKind.Text;
            run.Text = block.Text;
            block.Runs = [run];
        }
        RefreshContainerText(block.Runs);
        block.Text = TextExtractor.ExtractRunText(block.Runs);
    }

    static void RefreshContainerText(OdfTextRun[] runs)
    {
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run.Kind == OdfTextRunKind.Space)
            {
                if (run.SpaceCount < 0)
                {
                    throw new OdfEditException(
                        "An explicit-space count cannot be negative.");
                }
                run.Text = new string(' ', run.SpaceCount);
            }
            else if (run.Kind == OdfTextRunKind.Tab)
            {
                run.Text = "\t";
            }
            else if (run.Kind == OdfTextRunKind.LineBreak)
            {
                run.Text = "\n";
            }
            else if (run.Kind == OdfTextRunKind.SoftPageBreak)
            {
                run.Text = "";
            }
            if ((run.Kind == OdfTextRunKind.Span
                    || run.Kind == OdfTextRunKind.Hyperlink
                    || run.Kind == OdfTextRunKind.Ruby)
                && run.Runs.Length == 0
                && run.Text.Length > 0)
            {
                OdfTextRun text = new OdfTextRun();
                text.Kind = OdfTextRunKind.Text;
                text.Text = run.Text;
                run.Runs = [text];
            }
            if (run.Runs.Length > 0)
            {
                RefreshContainerText(run.Runs);
                run.Text = TextExtractor.ExtractRunText(run.Runs);
            }
            index = index + 1;
        }
    }

    static void ApplyTextBlockFields(
        OdfTextBlock block,
        OdfNode node,
        bool isNew)
    {
        if (node.Name == null)
        {
            node.Name = QualifiedTextName(block.IsHeading ? "h" : "p");
        }
        if (isNew || block.IsHeading != block.OriginalIsHeading)
        {
            node.Name.NamespaceName = OdfNamespaces.Text;
            node.Name.LocalName = block.IsHeading ? "h" : "p";
            node.Name.Prefix = OdfNamespaces.TextPrefix;
        }
        WriteChangedTextAttribute(
            node,
            "style-name",
            block.StyleName,
            block.OriginalStyleName,
            isNew);
        WriteChangedTextAttribute(
            node,
            "class-names",
            block.ClassNames,
            block.OriginalClassNames,
            isNew);
        WriteChangedTextAttribute(
            node,
            "cond-style-name",
            block.ConditionalStyleName,
            block.OriginalConditionalStyleName,
            isNew);
        ApplyHeadingFields(block, node, isNew);
    }

    static void ApplyHeadingFields(
        OdfTextBlock block,
        OdfNode node,
        bool isNew)
    {
        if (!block.IsHeading)
        {
            if (block.OriginalIsHeading || isNew)
            {
                StyleDocumentModel.RemoveAttribute(
                    node,
                    OdfNamespaces.Text,
                    "outline-level");
                StyleDocumentModel.RemoveAttribute(
                    node,
                    OdfNamespaces.Text,
                    "restart-numbering");
                StyleDocumentModel.RemoveAttribute(
                    node,
                    OdfNamespaces.Text,
                    "start-value");
                StyleDocumentModel.RemoveAttribute(
                    node,
                    OdfNamespaces.Text,
                    "is-list-header");
            }
            return;
        }
        if (isNew
            || block.OutlineLevel != block.OriginalOutlineLevel
            || block.IsHeading != block.OriginalIsHeading)
        {
            if (block.OutlineLevel > 0)
            {
                StyleDocumentModel.WriteAttribute(
                    node,
                    OdfNamespaces.Text,
                    "outline-level",
                    OdfNamespaces.TextPrefix,
                    block.OutlineLevel.ToString());
            }
            else
            {
                StyleDocumentModel.RemoveAttribute(
                    node,
                    OdfNamespaces.Text,
                    "outline-level");
            }
        }
        WriteChangedTextAttribute(
            node,
            "restart-numbering",
            block.RestartNumbering,
            block.OriginalRestartNumbering,
            isNew);
        WriteChangedTextAttribute(
            node,
            "start-value",
            block.StartValue,
            block.OriginalStartValue,
            isNew);
        WriteChangedTextAttribute(
            node,
            "is-list-header",
            block.IsListHeader,
            block.OriginalIsListHeader,
            isNew);
    }

    static void WriteChangedTextAttribute(
        OdfNode node,
        string localName,
        string value,
        string originalValue,
        bool isNew)
    {
        if (!isNew && value == originalValue)
        {
            return;
        }
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Text,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Text,
                localName,
                OdfNamespaces.TextPrefix,
                value);
        }
    }

    static OdfNode[] BuildRunNodes(OdfTextRun[] runs, OdfNode parent)
    {
        OdfNode[] result = new OdfNode[runs.Length];
        int index = 0;
        while (index < runs.Length)
        {
            result[index] = BuildRunNode(runs[index], parent);
            index = index + 1;
        }
        return result;
    }

    static OdfNode BuildRunNode(OdfTextRun run, OdfNode parent)
    {
        if (run.Kind == OdfTextRunKind.Text)
        {
            OdfNode text = new OdfNode();
            text.Kind = OdfNodeKind.Text;
            text.Value = run.Text;
            text.Parent = parent;
            text.Location.PartPath = parent.Location.PartPath;
            run.Node = text;
            return text;
        }
        if (run.Kind == OdfTextRunKind.Unknown && run.Node != null)
        {
            OdfNode unknown = CloneRunNode(run.Node);
            unknown.Parent = parent;
            if (unknown.Kind == OdfNodeKind.Element)
            {
                unknown.Children = BuildRunNodes(run.Runs, unknown);
                run.Attributes = unknown.Attributes;
            }
            run.Node = unknown;
            return unknown;
        }
        OdfNode result = CreateOrCloneRunElement(run, parent);
        ApplyRunFields(run, result);
        if (run.Kind == OdfTextRunKind.Ruby)
        {
            result.Children = BuildRubyNodes(run, result);
            run.Attributes = result.Attributes;
            run.Node = result;
            return result;
        }
        if (run.Kind == OdfTextRunKind.Span
            || run.Kind == OdfTextRunKind.Hyperlink)
        {
            result.Children = BuildRunNodes(run.Runs, result);
        }
        else
        {
            result.Children = [];
        }
        run.Attributes = result.Attributes;
        run.Node = result;
        return result;
    }

    static OdfNode CreateOrCloneRunElement(
        OdfTextRun run,
        OdfNode parent)
    {
        OdfNode result;
        if (run.Node != null && run.Node.Kind == OdfNodeKind.Element)
        {
            result = CloneRunNode(run.Node);
        }
        else
        {
            result = new OdfNode();
            result.Kind = OdfNodeKind.Element;
        }
        result.Name = QualifiedTextName(RunElementName(run.Kind));
        result.Parent = parent;
        result.Location.PartPath = parent.Location.PartPath;
        return result;
    }

    static string RunElementName(OdfTextRunKind kind)
    {
        if (kind == OdfTextRunKind.Span)
        {
            return "span";
        }
        if (kind == OdfTextRunKind.Hyperlink)
        {
            return "a";
        }
        if (kind == OdfTextRunKind.Space)
        {
            return "s";
        }
        if (kind == OdfTextRunKind.Tab)
        {
            return "tab";
        }
        if (kind == OdfTextRunKind.LineBreak)
        {
            return "line-break";
        }
        if (kind == OdfTextRunKind.Bookmark)
        {
            return "bookmark";
        }
        if (kind == OdfTextRunKind.BookmarkStart)
        {
            return "bookmark-start";
        }
        if (kind == OdfTextRunKind.BookmarkEnd)
        {
            return "bookmark-end";
        }
        if (kind == OdfTextRunKind.ReferenceMark)
        {
            return "reference-mark";
        }
        if (kind == OdfTextRunKind.ReferenceMarkStart)
        {
            return "reference-mark-start";
        }
        if (kind == OdfTextRunKind.ReferenceMarkEnd)
        {
            return "reference-mark-end";
        }
        if (kind == OdfTextRunKind.TableOfContentMark)
        {
            return "toc-mark";
        }
        if (kind == OdfTextRunKind.TableOfContentMarkStart)
        {
            return "toc-mark-start";
        }
        if (kind == OdfTextRunKind.TableOfContentMarkEnd)
        {
            return "toc-mark-end";
        }
        if (kind == OdfTextRunKind.UserIndexMark)
        {
            return "user-index-mark";
        }
        if (kind == OdfTextRunKind.UserIndexMarkStart)
        {
            return "user-index-mark-start";
        }
        if (kind == OdfTextRunKind.UserIndexMarkEnd)
        {
            return "user-index-mark-end";
        }
        if (kind == OdfTextRunKind.AlphabeticalIndexMark)
        {
            return "alphabetical-index-mark";
        }
        if (kind == OdfTextRunKind.AlphabeticalIndexMarkStart)
        {
            return "alphabetical-index-mark-start";
        }
        if (kind == OdfTextRunKind.AlphabeticalIndexMarkEnd)
        {
            return "alphabetical-index-mark-end";
        }
        if (kind == OdfTextRunKind.BibliographyMark)
        {
            return "bibliography-mark";
        }
        if (kind == OdfTextRunKind.Ruby)
        {
            return "ruby";
        }
        return "soft-page-break";
    }

    static void ApplyRunFields(OdfTextRun run, OdfNode node)
    {
        ApplyPreservedRunAttributes(run, node);
        if (run.Kind == OdfTextRunKind.Span
            || run.Kind == OdfTextRunKind.Hyperlink
            || run.Kind == OdfTextRunKind.Ruby)
        {
            WriteOptionalAttribute(
                node,
                OdfNamespaces.Text,
                "style-name",
                OdfNamespaces.TextPrefix,
                run.StyleName);
        }
        if (run.Kind == OdfTextRunKind.Span)
        {
            WriteOptionalAttribute(
                node,
                OdfNamespaces.Text,
                "class-names",
                OdfNamespaces.TextPrefix,
                run.ClassNames);
        }
        if (run.Kind == OdfTextRunKind.Hyperlink)
        {
            ApplyHyperlinkFields(run, node);
        }
        if (IsMarkerRunKind(run.Kind))
        {
            ApplyMarkerFields(run, node);
        }
        if (run.Kind == OdfTextRunKind.Space)
        {
            string count = run.SpaceCount == 1
                ? ""
                : run.SpaceCount.ToString();
            WriteOptionalAttribute(
                node,
                OdfNamespaces.Text,
                "c",
                OdfNamespaces.TextPrefix,
                count);
        }
        if (run.Kind == OdfTextRunKind.Tab)
        {
            WriteOptionalAttribute(
                node,
                OdfNamespaces.Text,
                "tab-ref",
                OdfNamespaces.TextPrefix,
                run.TabReference);
        }
    }

    static void ApplyPreservedRunAttributes(
        OdfTextRun run,
        OdfNode node)
    {
        int index = 0;
        while (index < run.Attributes.Length)
        {
            OdfAttribute attribute = run.Attributes[index];
            StyleDocumentModel.WriteAttribute(
                node,
                attribute.Name.NamespaceName,
                attribute.Name.LocalName,
                attribute.Name.Prefix,
                attribute.Value);
            index = index + 1;
        }
    }

    static void ApplyMarkerFields(OdfTextRun run, OdfNode node)
    {
        string markerName = MarkerNameAttribute(run.Kind);
        if (markerName.Length > 0)
        {
            WriteOptionalAttribute(
                node,
                OdfNamespaces.Text,
                markerName,
                OdfNamespaces.TextPrefix,
                run.MarkerName);
        }
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Text,
            "index-name",
            OdfNamespaces.TextPrefix,
            run.IndexName);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Text,
            "string-value",
            OdfNamespaces.TextPrefix,
            run.StringValue);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Text,
            "key1",
            OdfNamespaces.TextPrefix,
            run.Key1);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Text,
            "key2",
            OdfNamespaces.TextPrefix,
            run.Key2);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Text,
            "outline-level",
            OdfNamespaces.TextPrefix,
            run.MarkerOutlineLevel);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Text,
            "bibliography-type",
            OdfNamespaces.TextPrefix,
            run.BibliographyType);
    }

    static OdfNode[] BuildRubyNodes(OdfTextRun run, OdfNode ruby)
    {
        OdfNode rubyBase = CreateRubyChild(run.Node, "ruby-base", ruby);
        rubyBase.Children = BuildRunNodes(run.Runs, rubyBase);
        OdfNode rubyText = CreateRubyChild(run.Node, "ruby-text", ruby);
        WriteOptionalAttribute(
            rubyText,
            OdfNamespaces.Text,
            "style-name",
            OdfNamespaces.TextPrefix,
            run.RubyTextStyleName);
        OdfNode annotation = new OdfNode();
        annotation.Kind = OdfNodeKind.Text;
        annotation.Value = run.RubyText;
        annotation.Location.PartPath = ruby.Location.PartPath;
        annotation.Parent = rubyText;
        rubyText.Children = [annotation];
        return [rubyBase, rubyText];
    }

    static OdfNode CreateRubyChild(
        OdfNode? originalRuby,
        string localName,
        OdfNode parent)
    {
        OdfNode? original = null;
        if (originalRuby != null)
        {
            original = FindTextChild(originalRuby, localName);
        }
        OdfNode result;
        if (original != null)
        {
            result = CloneRunNode(original);
        }
        else
        {
            result = new OdfNode();
            result.Kind = OdfNodeKind.Element;
            result.Name = QualifiedTextName(localName);
        }
        result.Parent = parent;
        result.Location.PartPath = parent.Location.PartPath;
        return result;
    }

    static void ApplyHyperlinkFields(OdfTextRun run, OdfNode node)
    {
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.XLink,
            "type",
            OdfNamespaces.XLinkPrefix,
            "simple");
        WriteOptionalAttribute(
            node,
            OdfNamespaces.XLink,
            "href",
            OdfNamespaces.XLinkPrefix,
            run.Href);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Office,
            "name",
            OdfNamespaces.OfficePrefix,
            run.LinkName);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Office,
            "title",
            OdfNamespaces.OfficePrefix,
            run.LinkTitle);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Office,
            "target-frame-name",
            OdfNamespaces.OfficePrefix,
            run.TargetFrameName);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.XLink,
            "show",
            OdfNamespaces.XLinkPrefix,
            run.LinkShow);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.XLink,
            "actuate",
            OdfNamespaces.XLinkPrefix,
            run.LinkActuate);
        WriteOptionalAttribute(
            node,
            OdfNamespaces.Text,
            "visited-style-name",
            OdfNamespaces.TextPrefix,
            run.VisitedStyleName);
    }

    static void WriteOptionalAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(node, namespaceName, localName);
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

    static OdfNode CloneRunNode(OdfNode node)
    {
        return new OdfTreeEditor([]).CloneSubtree(node);
    }

    static OdfQualifiedName QualifiedTextName(string localName)
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = OdfNamespaces.Text;
        result.LocalName = localName;
        result.Prefix = OdfNamespaces.TextPrefix;
        return result;
    }

    static string CreateRunSignature(OdfTextRun[] runs)
    {
        StringBuilder result = new StringBuilder();
        AppendRunSignatures(result, runs);
        return result.ToString();
    }

    static void AppendRunSignatures(
        StringBuilder result,
        OdfTextRun[] runs)
    {
        result.Append('[');
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            result.Append((int)run.Kind);
            result.Append(':');
            result.Append(run.SpaceCount);
            AppendSignatureValue(result, run.Text);
            AppendSignatureValue(result, run.StyleName);
            AppendSignatureValue(result, run.ClassNames);
            AppendSignatureValue(result, run.Href);
            AppendSignatureValue(result, run.LinkName);
            AppendSignatureValue(result, run.LinkTitle);
            AppendSignatureValue(result, run.TargetFrameName);
            AppendSignatureValue(result, run.LinkShow);
            AppendSignatureValue(result, run.LinkActuate);
            AppendSignatureValue(result, run.VisitedStyleName);
            AppendSignatureValue(result, run.MarkerName);
            AppendSignatureValue(result, run.IndexName);
            AppendSignatureValue(result, run.StringValue);
            AppendSignatureValue(result, run.Key1);
            AppendSignatureValue(result, run.Key2);
            AppendSignatureValue(result, run.MarkerOutlineLevel);
            AppendSignatureValue(result, run.BibliographyType);
            AppendSignatureValue(result, run.RubyText);
            AppendSignatureValue(result, run.RubyTextStyleName);
            AppendSignatureValue(result, run.TabReference);
            AppendAttributeSignatures(result, run.Attributes);
            AppendRunSignatures(result, run.Runs);
            index = index + 1;
        }
        result.Append(']');
    }

    static void AppendAttributeSignatures(
        StringBuilder result,
        OdfAttribute[] attributes)
    {
        result.Append('{');
        int index = 0;
        while (index < attributes.Length)
        {
            OdfAttribute attribute = attributes[index];
            AppendSignatureValue(result, attribute.Name.NamespaceName);
            AppendSignatureValue(result, attribute.Name.LocalName);
            AppendSignatureValue(result, attribute.Value);
            index = index + 1;
        }
        result.Append('}');
    }

    static void AppendSignatureValue(StringBuilder result, string value)
    {
        result.Append(value.Length);
        result.Append(':');
        result.Append(value);
    }

    static void SnapshotTextBlock(OdfTextBlock block)
    {
        block.OriginalIsHeading = block.IsHeading;
        block.OriginalOutlineLevel = block.OutlineLevel;
        block.OriginalRestartNumbering = block.RestartNumbering;
        block.OriginalStartValue = block.StartValue;
        block.OriginalIsListHeader = block.IsListHeader;
        block.OriginalStyleName = block.StyleName;
        block.OriginalClassNames = block.ClassNames;
        block.OriginalConditionalStyleName = block.ConditionalStyleName;
        block.OriginalText = block.Text;
        block.OriginalRunSignature = CreateRunSignature(block.Runs);
    }

    static void RemoveDeletedBlocks(
        OdfTextBlock[] blocks,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            if (!BlockRetainsNode(blocks, node) && node.Parent != null)
            {
                RemoveBlockFromParent(node);
            }
            index = index + 1;
        }
    }

    static bool BlockRetainsNode(OdfTextBlock[] blocks, OdfNode node)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextBlock block = blocks[index];
            if (block != null && block.OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveBlockFromParent(OdfNode node)
    {
        OdfNode? parent = node.Parent;
        if (parent == null)
        {
            return;
        }
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] != node)
            {
                children.Add(parent.Children[index]);
            }
            index = index + 1;
        }
        parent.Children = children.ToArray();
        node.Parent = null;
    }
}
