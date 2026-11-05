using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class AnnotationDocumentState
{
    internal OdfAnnotation[] OriginalAnnotations = [];
}

internal class AnnotationEndpoint
{
    internal bool IsStart;
    internal string Name = "";
    internal string PartPath = "";
    internal int Order;
    internal OdfNode? Node;
    internal OdfTextBlock? Block;
    internal OdfTextRun? Run;
}

internal static class AnnotationDocumentModel
{
    internal static void LoadAnnotations(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for annotation materialization.");
        }
        List<AnnotationEndpoint> endpoints = CollectEndpoints(document);
        List<OdfAnnotation> annotations = new List<OdfAnnotation>();
        int index = 0;
        while (index < endpoints.Count)
        {
            AnnotationEndpoint endpoint = endpoints[index];
            if (endpoint.IsStart && endpoint.Node != null)
            {
                annotations.Add(CreateAnnotation(
                    document, endpoint, endpoints));
            }
            index = index + 1;
        }
        document.Annotations = annotations.ToArray();
        ResolveThreads(document.Annotations);
        AnnotationDocumentState state = new AnnotationDocumentState();
        state.OriginalAnnotations = CopyAnnotations(document.Annotations);
        document.AnnotationState = state;
    }

    internal static void SynchronizeAnnotations(OdfDocument document)
    {
        if (document == null || document.Annotations == null)
        {
            throw new OdfDocumentException(
                "A document and annotation collection are required for writing.");
        }
        AnnotationDocumentState state = document.AnnotationState
            ?? new AnnotationDocumentState();
        RemoveDeletedAnnotations(
            document, document.Annotations, state.OriginalAnnotations);
        NormalizeParentReferences(document.Annotations);
        int index = 0;
        while (index < document.Annotations.Length)
        {
            OdfAnnotation annotation = document.Annotations[index];
            if (annotation == null)
            {
                throw new OdfDocumentException(
                    "The annotation collection contains a null annotation.");
            }
            SynchronizeAnnotation(document, annotation);
            index = index + 1;
        }
        RefreshRanges(document);
        ResolveThreads(document.Annotations);
        index = 0;
        while (index < document.Annotations.Length)
        {
            OdfAnnotation annotation = document.Annotations[index];
            annotation.OriginalName = annotation.Name;
            annotation.OriginalText = annotation.Text;
            index = index + 1;
        }
        state.OriginalAnnotations = CopyAnnotations(document.Annotations);
        document.AnnotationState = state;
    }

    static OdfAnnotation CreateAnnotation(
        OdfDocument document,
        AnnotationEndpoint endpoint,
        List<AnnotationEndpoint> endpoints)
    {
        OdfNode node = endpoint.Node ?? new OdfNode();
        OdfAnnotation result = new OdfAnnotation();
        result.Name = endpoint.Name;
        result.XmlId = Read(node, OdfNamespaces.Xml, "id");
        if (result.XmlId.Length == 0)
        {
            result.XmlId = node.XmlId;
        }
        result.Creator = ReadChildText(
            node, OdfNamespaces.DublinCore, "creator");
        result.Date = ReadChildText(
            node, OdfNamespaces.DublinCore, "date");
        result.IsDateValid = IsDate(result.Date);
        result.DateString = ReadChildText(
            node, OdfNamespaces.Metadata, "date-string");
        result.Initials = ReadChildText(
            node, OdfNamespaces.Metadata, "creator-initials");
        if (result.Initials.Length == 0)
        {
            result.Initials = ReadChildText(
                node, OdfNamespaces.LibreOffice, "sender-initials");
        }
        result.ParentName = Read(
            node, OdfNamespaces.LibreOffice, "parent-name");
        if (result.ParentName.Length == 0)
        {
            result.ParentName = Read(
                node, OdfNamespaces.Office, "parent-annotation-name");
        }
        LoadState(result, node);
        result.Display = Read(node, OdfNamespaces.Office, "display");
        result.X = Read(node, OdfNamespaces.Svg, "x");
        result.Y = Read(node, OdfNamespaces.Svg, "y");
        result.Width = Read(node, OdfNamespaces.Svg, "width");
        result.Height = Read(node, OdfNamespaces.Svg, "height");
        result.StyleName = Read(
            node, OdfNamespaces.Drawing, "style-name");
        result.TextStyleName = Read(
            node, OdfNamespaces.Drawing, "text-style-name");
        result.AnchorType = Read(
            node, OdfNamespaces.Text, "anchor-type");
        result.AnchorPageNumber = Read(
            node, OdfNamespaces.Text, "anchor-page-number");
        result.EndCellAddress = Read(
            node, OdfNamespaces.Table, "end-cell-address");
        result.EndX = Read(node, OdfNamespaces.Table, "end-x");
        result.EndY = Read(node, OdfNamespaces.Table, "end-y");
        result.TextBlocks = ContainedBlocks(document.TextBlocks, node);
        result.Text = AnnotationText(result.TextBlocks);
        result.PartPath = endpoint.PartPath;
        result.StartBlock = endpoint.Block;
        result.StartRun = endpoint.Run;
        result.Node = node;
        ResolveRange(result, endpoint, endpoints);
        result.OriginalName = result.Name;
        result.OriginalText = result.Text;
        return result;
    }

    static void LoadState(OdfAnnotation annotation, OdfNode node)
    {
        string value = Read(node, OdfNamespaces.LibreOffice, "resolved");
        if (value.Length == 0)
        {
            value = Read(node, OdfNamespaces.Office, "resolved");
        }
        annotation.HasExplicitState = value.Length > 0;
        if (value == "true" || value == "1")
        {
            annotation.State = OdfAnnotationState.Resolved;
        }
        else if (value == "false" || value == "0")
        {
            annotation.State = OdfAnnotationState.Open;
        }
        else
        {
            annotation.State = OdfAnnotationState.Unknown;
        }
    }

    static void ResolveRange(
        OdfAnnotation annotation,
        AnnotationEndpoint start,
        List<AnnotationEndpoint> endpoints)
    {
        int startCount = 0;
        int endCount = 0;
        AnnotationEndpoint? end = null;
        int index = 0;
        while (index < endpoints.Count)
        {
            AnnotationEndpoint current = endpoints[index];
            if (current.PartPath == start.PartPath
                && current.Name == start.Name)
            {
                if (current.IsStart)
                {
                    startCount = startCount + 1;
                }
                else
                {
                    endCount = endCount + 1;
                    if (end == null)
                    {
                        end = current;
                    }
                }
            }
            index = index + 1;
        }
        annotation.IsRange = endCount > 0;
        annotation.HasDuplicateEndpoint = startCount > 1 || endCount > 1;
        if (end == null)
        {
            return;
        }
        annotation.EndNode = end.Node;
        annotation.EndBlock = end.Block;
        annotation.EndRun = end.Run;
        annotation.IsReversed = end.Order < start.Order;
        OdfNode? startNode = start.Node;
        OdfNode? endNode = end.Node;
        annotation.IsRangeResolved = !annotation.HasDuplicateEndpoint
            && !annotation.IsReversed
            && startNode != null
            && endNode != null;
        if (annotation.IsRangeResolved
            && startNode != null && endNode != null)
        {
            annotation.AnnotatedText = ExtractAnnotatedText(
                DocumentRoot(startNode), startNode, endNode);
        }
    }

    static OdfNode DocumentRoot(OdfNode node)
    {
        OdfNode result = node;
        while (result.Parent != null)
        {
            result = result.Parent;
        }
        return result;
    }

    static void SynchronizeAnnotation(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        OdfNode? previousNode = annotation.Node;
        if (annotation.StartRun != null && annotation.StartRun.Node != null)
        {
            annotation.Node = annotation.StartRun.Node;
        }
        if (annotation.EndRun != null && annotation.EndRun.Node != null)
        {
            annotation.EndNode = annotation.EndRun.Node;
        }
        if (annotation.Node == null
            || !IsOfficeElement(annotation.Node, "annotation"))
        {
            throw new OdfEditException(
                "An annotation requires an underlying office:annotation element.");
        }
        RequireAnnotationStrings(annotation);
        bool rebound = previousNode != null && previousNode != annotation.Node;
        WriteOptional(annotation.Node, OdfNamespaces.Office, "name",
            OdfNamespaces.OfficePrefix, annotation.Name);
        WriteOptional(annotation.Node, OdfNamespaces.Xml, "id",
            OdfNamespaces.XmlPrefix, annotation.XmlId);
        annotation.Node.XmlId = annotation.XmlId;
        WriteOptional(annotation.Node, OdfNamespaces.Office, "display",
            OdfNamespaces.OfficePrefix, annotation.Display);
        WriteOptional(annotation.Node, OdfNamespaces.Svg, "x",
            OdfNamespaces.SvgPrefix, annotation.X);
        WriteOptional(annotation.Node, OdfNamespaces.Svg, "y",
            OdfNamespaces.SvgPrefix, annotation.Y);
        WriteOptional(annotation.Node, OdfNamespaces.Svg, "width",
            OdfNamespaces.SvgPrefix, annotation.Width);
        WriteOptional(annotation.Node, OdfNamespaces.Svg, "height",
            OdfNamespaces.SvgPrefix, annotation.Height);
        WriteOptional(annotation.Node, OdfNamespaces.Drawing, "style-name",
            OdfNamespaces.DrawingPrefix, annotation.StyleName);
        WriteOptional(annotation.Node, OdfNamespaces.Drawing,
            "text-style-name", OdfNamespaces.DrawingPrefix,
            annotation.TextStyleName);
        WriteOptional(annotation.Node, OdfNamespaces.Text, "anchor-type",
            OdfNamespaces.TextPrefix, annotation.AnchorType);
        WriteOptional(annotation.Node, OdfNamespaces.Text,
            "anchor-page-number", OdfNamespaces.TextPrefix,
            annotation.AnchorPageNumber);
        WriteOptional(annotation.Node, OdfNamespaces.Table,
            "end-cell-address", OdfNamespaces.TablePrefix,
            annotation.EndCellAddress);
        WriteOptional(annotation.Node, OdfNamespaces.Table, "end-x",
            OdfNamespaces.TablePrefix, annotation.EndX);
        WriteOptional(annotation.Node, OdfNamespaces.Table, "end-y",
            OdfNamespaces.TablePrefix, annotation.EndY);
        SynchronizeState(annotation);
        WriteOptional(annotation.Node, OdfNamespaces.LibreOffice,
            "parent-name", OdfNamespaces.LibreOfficePrefix,
            annotation.ParentName);
        StyleDocumentModel.RemoveAttribute(annotation.Node,
            OdfNamespaces.Office, "parent-annotation-name");
        SynchronizeMetadataChildren(annotation);
        if (annotation.Text != annotation.OriginalText || rebound)
        {
            ReplaceAnnotationText(document, annotation);
        }
        else
        {
            annotation.Text = AnnotationText(annotation.TextBlocks);
        }
        if (annotation.EndNode != null)
        {
            WriteOptional(annotation.EndNode, OdfNamespaces.Office, "name",
                OdfNamespaces.OfficePrefix, annotation.Name);
        }
        annotation.PartPath = annotation.Node.Location.PartPath;
        if (annotation.StartRun != null)
        {
            annotation.StartRun.Attributes = annotation.Node.Attributes;
        }
    }

    static void SynchronizeState(OdfAnnotation annotation)
    {
        OdfNode? node = annotation.Node;
        if (node == null)
        {
            throw new OdfEditException(
                "An annotation requires an underlying XML element.");
        }
        if (annotation.State == OdfAnnotationState.Unknown)
        {
            StyleDocumentModel.RemoveAttribute(node,
                OdfNamespaces.LibreOffice, "resolved");
            StyleDocumentModel.RemoveAttribute(node,
                OdfNamespaces.Office, "resolved");
            annotation.HasExplicitState = false;
            return;
        }
        string value = annotation.State == OdfAnnotationState.Resolved
            ? "true"
            : "false";
        WriteOptional(node, OdfNamespaces.LibreOffice,
            "resolved", OdfNamespaces.LibreOfficePrefix, value);
        StyleDocumentModel.RemoveAttribute(node,
            OdfNamespaces.Office, "resolved");
        annotation.HasExplicitState = true;
    }

    static void SynchronizeMetadataChildren(OdfAnnotation annotation)
    {
        OdfNode node = annotation.Node ?? new OdfNode();
        SynchronizeChildText(node, OdfNamespaces.DublinCore, "creator",
            OdfNamespaces.DublinCorePrefix, annotation.Creator);
        SynchronizeChildText(node, OdfNamespaces.DublinCore, "date",
            OdfNamespaces.DublinCorePrefix, annotation.Date);
        annotation.IsDateValid = IsDate(annotation.Date);
        SynchronizeChildText(node, OdfNamespaces.Metadata, "date-string",
            OdfNamespaces.MetadataPrefix, annotation.DateString);
        OdfNode? legacy = FindDirectChild(
            node, OdfNamespaces.LibreOffice, "sender-initials");
        OdfNode? standard = FindDirectChild(
            node, OdfNamespaces.Metadata, "creator-initials");
        if (annotation.Initials.Length == 0)
        {
            RemoveDirectChild(node, standard);
            RemoveDirectChild(node, legacy);
        }
        else if (standard != null)
        {
            ReplaceElementText(standard, annotation.Initials);
            RemoveDirectChild(node, legacy);
        }
        else if (legacy != null)
        {
            ReplaceElementText(legacy, annotation.Initials);
        }
        else
        {
            SynchronizeChildText(node, OdfNamespaces.Metadata,
                "creator-initials", OdfNamespaces.MetadataPrefix,
                annotation.Initials);
        }
    }

    static void SynchronizeChildText(
        OdfNode parent,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        OdfNode? child = FindDirectChild(parent, namespaceName, localName);
        if (value.Length == 0)
        {
            RemoveDirectChild(parent, child);
            return;
        }
        if (child == null)
        {
            child = CreateElement(namespaceName, localName, prefix,
                parent.Location.PartPath);
            InsertMetadataChild(parent, child, MetadataRank(localName));
        }
        ReplaceElementText(child, value);
    }

    static int MetadataRank(string localName)
    {
        if (localName == "creator")
        {
            return 1;
        }
        if (localName == "date")
        {
            return 2;
        }
        if (localName == "date-string")
        {
            return 3;
        }
        if (localName == "creator-initials"
            || localName == "sender-initials")
        {
            return 4;
        }
        return 5;
    }

    static void InsertMetadataChild(
        OdfNode parent,
        OdfNode child,
        int rank)
    {
        int insertion = parent.Children.Length;
        int index = 0;
        while (index < parent.Children.Length && insertion == parent.Children.Length)
        {
            OdfNode current = parent.Children[index];
            int currentRank = 5;
            if (current.Name != null)
            {
                currentRank = MetadataRank(current.Name.LocalName);
            }
            if (currentRank > rank)
            {
                insertion = index;
            }
            index = index + 1;
        }
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, 0, children, 0, insertion);
        children[insertion] = child;
        Array.Copy(parent.Children, insertion, children, insertion + 1,
            parent.Children.Length - insertion);
        parent.Children = children;
        child.Parent = parent;
        parent.WasEmptyElement = false;
    }

    static void ReplaceAnnotationText(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        OdfNode parent = annotation.Node ?? new OdfNode();
        OdfTextBlock[] oldBlocks = annotation.TextBlocks;
        List<OdfNode> retained = new List<OdfNode>();
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (IsTextBodyElement(child))
            {
                child.Parent = null;
            }
            else
            {
                retained.Add(child);
            }
            index = index + 1;
        }
        OdfTextBlock? createdBlock = null;
        if (annotation.Text.Length > 0)
        {
            OdfNode paragraph = CreateElement(
                OdfNamespaces.Text, "p", OdfNamespaces.TextPrefix,
                parent.Location.PartPath);
            OdfNode text = new OdfNode();
            text.Kind = OdfNodeKind.Text;
            text.Value = annotation.Text;
            text.Location.PartPath = parent.Location.PartPath;
            text.Parent = paragraph;
            paragraph.Children = [text];
            paragraph.Parent = parent;
            retained.Add(paragraph);
            createdBlock = TextDocumentModel.CreateTextBlock(
                paragraph,
                parent.Location.PartPath,
                document.LoadOptions.SecurityLimits.MaximumXmlTextLength);
            annotation.TextBlocks = [createdBlock];
        }
        else
        {
            annotation.TextBlocks = [];
        }
        parent.Children = retained.ToArray();
        ReplaceDocumentBlocks(document, oldBlocks, createdBlock,
            annotation.StartBlock);
        parent.WasEmptyElement = parent.Children.Length == 0;
    }

    static void ReplaceDocumentBlocks(
        OdfDocument document,
        OdfTextBlock[] removed,
        OdfTextBlock? added,
        OdfTextBlock? anchor)
    {
        int insertion = document.TextBlocks.Length;
        int retainedCount = 0;
        int index = 0;
        while (index < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[index];
            if (ContainsBlock(removed, block))
            {
                if (insertion == document.TextBlocks.Length)
                {
                    insertion = retainedCount;
                }
            }
            else
            {
                retainedCount = retainedCount + 1;
            }
            index = index + 1;
        }
        if (insertion == document.TextBlocks.Length && anchor != null)
        {
            insertion = BlockIndex(document.TextBlocks, anchor) + 1;
            if (insertion <= 0)
            {
                insertion = retainedCount;
            }
        }
        if (insertion > retainedCount)
        {
            insertion = retainedCount;
        }
        OdfTextBlock[] result = new OdfTextBlock[
            retainedCount + (added == null ? 0 : 1)];
        int destination = 0;
        index = 0;
        while (index < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[index];
            if (!ContainsBlock(removed, block))
            {
                if (destination == insertion && added != null)
                {
                    result[destination] = added;
                    destination = destination + 1;
                }
                result[destination] = block;
                destination = destination + 1;
            }
            index = index + 1;
        }
        if (destination == insertion && added != null)
        {
            result[destination] = added;
        }
        document.TextBlocks = result;
    }

    static int BlockIndex(OdfTextBlock[] blocks, OdfTextBlock target)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            if (blocks[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static bool ContainsBlock(OdfTextBlock[] blocks, OdfTextBlock target)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            if (blocks[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveDeletedAnnotations(
        OdfDocument document,
        OdfAnnotation[] current,
        OdfAnnotation[] original)
    {
        int index = 0;
        while (index < original.Length)
        {
            OdfAnnotation annotation = original[index];
            if (!RetainsAnnotation(current, annotation))
            {
                RemoveAnnotationBlocks(document, annotation);
                if (annotation.Node != null)
                {
                    StyleDocumentModel.RemoveNodeFromParent(annotation.Node);
                }
                if (annotation.EndNode != null)
                {
                    StyleDocumentModel.RemoveNodeFromParent(annotation.EndNode);
                }
            }
            index = index + 1;
        }
    }

    static bool RetainsAnnotation(
        OdfAnnotation[] annotations,
        OdfAnnotation original)
    {
        int index = 0;
        while (index < annotations.Length)
        {
            OdfAnnotation current = annotations[index];
            if (current == original
                || current != null && current.Node == original.Node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveAnnotationBlocks(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        List<OdfTextBlock> retained = new List<OdfTextBlock>();
        int index = 0;
        while (index < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[index];
            bool remove = ContainsBlock(annotation.TextBlocks, block);
            if (!remove && block.Node != null && annotation.Node != null)
            {
                remove = IsDescendantOf(block.Node, annotation.Node);
            }
            if (!remove)
            {
                retained.Add(block);
            }
            index = index + 1;
        }
        document.TextBlocks = retained.ToArray();
    }

    static void NormalizeParentReferences(OdfAnnotation[] annotations)
    {
        int index = 0;
        while (index < annotations.Length)
        {
            OdfAnnotation annotation = annotations[index];
            if (annotation.Parent != null)
            {
                if (annotation.Parent != annotation
                    && ContainsAnnotation(annotations, annotation.Parent))
                {
                    annotation.ParentName = annotation.Parent.Name;
                }
                else
                {
                    annotation.Parent = null;
                    annotation.ParentName = "";
                }
            }
            else if (annotation.ParentName.Length > 0
                && CountAnnotationsNamed(
                    annotations, annotation.ParentName) == 0)
            {
                annotation.ParentName = "";
            }
            index = index + 1;
        }
        ResolveThreads(annotations);
        index = 0;
        while (index < annotations.Length)
        {
            OdfAnnotation annotation = annotations[index];
            if (HasParentCycle(annotation))
            {
                annotation.Parent = null;
                annotation.ParentName = "";
            }
            index = index + 1;
        }
        ResolveThreads(annotations);
    }

    static void ResolveThreads(OdfAnnotation[] annotations)
    {
        int index = 0;
        while (index < annotations.Length)
        {
            annotations[index].Parent = null;
            annotations[index].Replies = [];
            index = index + 1;
        }
        index = 0;
        while (index < annotations.Length)
        {
            OdfAnnotation annotation = annotations[index];
            if (annotation.ParentName.Length > 0)
            {
                annotation.Parent = FindUniqueNamedAnnotation(
                    annotations, annotation.ParentName, annotation);
            }
            index = index + 1;
        }
        index = 0;
        while (index < annotations.Length)
        {
            OdfAnnotation annotation = annotations[index];
            if (annotation.Parent != null)
            {
                annotation.Parent.Replies = AppendAnnotation(
                    annotation.Parent.Replies, annotation);
            }
            index = index + 1;
        }
    }

    static OdfAnnotation? FindUniqueNamedAnnotation(
        OdfAnnotation[] annotations,
        string name,
        OdfAnnotation excluded)
    {
        OdfAnnotation? result = null;
        int count = 0;
        int index = 0;
        while (index < annotations.Length)
        {
            OdfAnnotation candidate = annotations[index];
            if (candidate != excluded && candidate.Name == name)
            {
                result = candidate;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static int CountAnnotationsNamed(
        OdfAnnotation[] annotations,
        string name)
    {
        int result = 0;
        int index = 0;
        while (index < annotations.Length)
        {
            if (annotations[index].Name == name)
            {
                result = result + 1;
            }
            index = index + 1;
        }
        return result;
    }

    static bool ContainsAnnotation(
        OdfAnnotation[] annotations,
        OdfAnnotation target)
    {
        int index = 0;
        while (index < annotations.Length)
        {
            if (annotations[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasParentCycle(OdfAnnotation annotation)
    {
        OdfAnnotation? current = annotation.Parent;
        int remaining = 100000;
        while (current != null && remaining > 0)
        {
            if (current == annotation)
            {
                return true;
            }
            current = current.Parent;
            remaining = remaining - 1;
        }
        return remaining == 0;
    }

    static OdfAnnotation[] AppendAnnotation(
        OdfAnnotation[] values,
        OdfAnnotation value)
    {
        OdfAnnotation[] result = new OdfAnnotation[values.Length + 1];
        Array.Copy(values, result, values.Length);
        result[result.Length - 1] = value;
        return result;
    }

    static void RefreshRanges(OdfDocument document)
    {
        List<AnnotationEndpoint> endpoints = CollectEndpoints(document);
        int index = 0;
        while (index < document.Annotations.Length)
        {
            OdfAnnotation annotation = document.Annotations[index];
            AnnotationEndpoint? start = FindEndpoint(
                endpoints, annotation.Node, true);
            annotation.IsRange = false;
            annotation.IsRangeResolved = false;
            annotation.IsReversed = false;
            annotation.HasDuplicateEndpoint = false;
            annotation.AnnotatedText = "";
            annotation.EndNode = null;
            annotation.EndBlock = null;
            annotation.EndRun = null;
            if (start != null)
            {
                annotation.StartBlock = start.Block;
                annotation.StartRun = start.Run;
                annotation.PartPath = start.PartPath;
                ResolveRange(annotation, start, endpoints);
            }
            index = index + 1;
        }
    }

    static AnnotationEndpoint? FindEndpoint(
        List<AnnotationEndpoint> endpoints,
        OdfNode? node,
        bool isStart)
    {
        int index = 0;
        while (index < endpoints.Count)
        {
            AnnotationEndpoint endpoint = endpoints[index];
            if (endpoint.Node == node && endpoint.IsStart == isStart)
            {
                return endpoint;
            }
            index = index + 1;
        }
        return null;
    }

    static List<AnnotationEndpoint> CollectEndpoints(OdfDocument document)
    {
        List<AnnotationEndpoint> result = new List<AnnotationEndpoint>();
        int order = 0;
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Body != null)
            {
                order = CollectEndpointNodes(
                    document, part.Body, part.Path, result, order);
            }
            partIndex = partIndex + 1;
        }
        return result;
    }

    static int CollectEndpointNodes(
        OdfDocument document,
        OdfNode node,
        string partPath,
        List<AnnotationEndpoint> endpoints,
        int order)
    {
        bool isStart = IsOfficeElement(node, "annotation");
        bool isEnd = IsOfficeElement(node, "annotation-end");
        if (isStart || isEnd)
        {
            AnnotationEndpoint endpoint = new AnnotationEndpoint();
            endpoint.IsStart = isStart;
            endpoint.Name = Read(node, OdfNamespaces.Office, "name");
            endpoint.PartPath = partPath;
            endpoint.Order = order;
            endpoint.Node = node;
            endpoint.Block = FindContainingBlock(document.TextBlocks, node);
            endpoint.Run = FindRun(document.TextBlocks, node);
            endpoints.Add(endpoint);
            order = order + 1;
            if (isStart)
            {
                return order;
            }
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            order = CollectEndpointNodes(document, node.Children[index],
                partPath, endpoints, order);
            index = index + 1;
        }
        return order;
    }

    static OdfTextBlock? FindContainingBlock(
        OdfTextBlock[] blocks,
        OdfNode node)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            OdfNode? blockNode = blocks[index].Node;
            if (blockNode != null && IsDescendantOf(node, blockNode))
            {
                return blocks[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfTextRun? FindRun(OdfTextBlock[] blocks, OdfNode node)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextRun? result = FindNestedRun(blocks[index].Runs, node);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfTextRun? FindNestedRun(OdfTextRun[] runs, OdfNode node)
    {
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run.Node == node)
            {
                return run;
            }
            OdfTextRun? nested = FindNestedRun(run.Runs, node);
            if (nested != null)
            {
                return nested;
            }
            index = index + 1;
        }
        return null;
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
            if (block.Node != null && IsDescendantOf(block.Node, ancestor)
                && block.Node != ancestor)
            {
                result.Add(block);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static bool IsDescendantOf(OdfNode node, OdfNode ancestor)
    {
        OdfNode? current = node;
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

    static string AnnotationText(OdfTextBlock[] blocks)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < blocks.Length)
        {
            if (index > 0)
            {
                result.Append('\n');
            }
            result.Append(blocks[index].Text);
            index = index + 1;
        }
        return result.ToString();
    }

    static string ExtractAnnotatedText(
        OdfNode root,
        OdfNode start,
        OdfNode end)
    {
        List<OdfNode> nodes = new List<OdfNode>();
        CollectAllNodes(root, nodes);
        int startIndex = NodeIndex(nodes, start);
        int endIndex = NodeIndex(nodes, end);
        if (startIndex < 0 || endIndex <= startIndex)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        int index = startIndex + 1;
        while (index < endIndex)
        {
            OdfNode node = nodes[index];
            if (!IsInsideAnnotation(node))
            {
                AppendVisibleNode(result, node);
            }
            index = index + 1;
        }
        while (result.Length > 0 && result[result.Length - 1] == '\n')
        {
            result.Length = result.Length - 1;
        }
        return result.ToString();
    }

    static void CollectAllNodes(OdfNode node, List<OdfNode> result)
    {
        result.Add(node);
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectAllNodes(node.Children[index], result);
            index = index + 1;
        }
    }

    static int NodeIndex(List<OdfNode> nodes, OdfNode target)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static bool IsInsideAnnotation(OdfNode node)
    {
        OdfNode? current = node;
        while (current != null)
        {
            if (IsOfficeElement(current, "annotation"))
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static void AppendVisibleNode(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
            return;
        }
        if (node.Kind != OdfNodeKind.Element || node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Text)
        {
            return;
        }
        if ((node.Name.LocalName == "p" || node.Name.LocalName == "h")
            && result.Length > 0 && result[result.Length - 1] != '\n')
        {
            result.Append('\n');
        }
        else if (node.Name.LocalName == "tab")
        {
            result.Append('\t');
        }
        else if (node.Name.LocalName == "line-break")
        {
            result.Append('\n');
        }
        else if (node.Name.LocalName == "s")
        {
            AppendExplicitSpaces(result, node);
        }
    }

    static void AppendExplicitSpaces(StringBuilder result, OdfNode node)
    {
        string lexical = Read(node, OdfNamespaces.Text, "c");
        int count = 1;
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(lexical);
        if (lexical.Length > 0 && parsed.IsValid
            && parsed.IntegerValue > 0 && parsed.IntegerValue <= int.MaxValue)
        {
            count = (int)parsed.IntegerValue;
        }
        result.Append(new string(' ', count));
    }

    static string ReadChildText(
        OdfNode parent,
        string namespaceName,
        string localName)
    {
        OdfNode? child = FindDirectChild(parent, namespaceName, localName);
        if (child == null)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        AppendElementText(result, child);
        return result.ToString();
    }

    static void AppendElementText(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendElementText(result, node.Children[index]);
            index = index + 1;
        }
    }

    static void ReplaceElementText(OdfNode node, string value)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            node.Children[index].Parent = null;
            index = index + 1;
        }
        if (value.Length == 0)
        {
            node.Children = [];
            return;
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Location.PartPath = node.Location.PartPath;
        text.Parent = node;
        node.Children = [text];
        node.WasEmptyElement = false;
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
            if (child.Kind == OdfNodeKind.Element && child.Name != null
                && child.Name.NamespaceName == namespaceName
                && child.Name.LocalName == localName)
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static void RemoveDirectChild(OdfNode parent, OdfNode? child)
    {
        if (child == null)
        {
            return;
        }
        StyleDocumentModel.RemoveNodeFromParent(child);
    }

    static OdfNode CreateElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            namespaceName, localName, prefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node, namespaceName, localName);
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
                node, namespaceName, localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node, namespaceName, localName, prefix, value);
        }
    }

    static bool IsOfficeElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Office
            && node.Name.LocalName == localName;
    }

    static bool IsTextBodyElement(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text
            && (node.Name.LocalName == "p" || node.Name.LocalName == "list");
    }

    static bool IsDate(string value)
    {
        return value.Length > 0
            && (OdfLexicalParser.ParseDate(value).IsValid
                || OdfLexicalParser.ParseDateTime(value).IsValid);
    }

    static void RequireAnnotationStrings(OdfAnnotation annotation)
    {
        if (annotation.Name == null || annotation.XmlId == null
            || annotation.Creator == null || annotation.Date == null
            || annotation.DateString == null || annotation.Initials == null
            || annotation.Text == null || annotation.AnnotatedText == null
            || annotation.ParentName == null || annotation.Replies == null
            || annotation.Display == null || annotation.X == null
            || annotation.Y == null || annotation.Width == null
            || annotation.Height == null || annotation.StyleName == null
            || annotation.TextStyleName == null
            || annotation.AnchorType == null
            || annotation.AnchorPageNumber == null
            || annotation.EndCellAddress == null || annotation.EndX == null
            || annotation.EndY == null || annotation.PartPath == null
            || annotation.TextBlocks == null)
        {
            throw new OdfEditException(
                "Annotation fields and collections cannot be null.");
        }
    }

    static OdfAnnotation[] CopyAnnotations(OdfAnnotation[] values)
    {
        OdfAnnotation[] result = new OdfAnnotation[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}
