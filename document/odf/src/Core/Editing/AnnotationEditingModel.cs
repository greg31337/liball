using System;
using System.Collections.Generic;

namespace odf;

internal class AnnotationCursorRange
{
    internal OdfTextBlock? StartBlock;
    internal int StartOffset;
    internal OdfTextBlock? EndBlock;
    internal int EndOffset;
    internal int StartIndex;
    internal int EndIndex;
    internal bool IsRange;
}

internal class AnnotationInsertionSegment
{
    internal OdfNode? Node;
    internal int Start;
    internal int Length;
    internal bool IsText;
}

internal static class AnnotationEditingModel
{
    internal static OdfAnnotation[] GetAnnotations(OdfDocument document)
    {
        RequireDocument(document);
        OdfAnnotation[] result = new OdfAnnotation[
            document.Annotations.Length];
        Array.Copy(document.Annotations, result, result.Length);
        return result;
    }

    internal static OdfAnnotation? FindAnnotation(
        OdfDocument document,
        string name)
    {
        RequireDocument(document);
        RequireName(name);
        OdfAnnotation? result = null;
        int count = 0;
        int index = 0;
        while (index < document.Annotations.Length)
        {
            OdfAnnotation annotation = document.Annotations[index];
            if (annotation.Name == name)
            {
                result = annotation;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    internal static OdfAnnotation CreateAnnotation(
        OdfDocument document,
        OdfTextCursor cursor,
        OdfAnnotation template)
    {
        RequireDocument(document);
        ValidateTemplate(document, template);
        AnnotationCursorRange range = RequireCursorRange(document, cursor);
        RequireRangePart(range);
        string name = SelectName(document, template.Name);
        RequireUniqueXmlId(document, template.XmlId, null);
        TextDocumentModel.SynchronizeTextBlocks(document);
        AnnotationDocumentModel.SynchronizeAnnotations(document);
        OdfTextBlock startBlock = range.StartBlock ?? new OdfTextBlock();
        OdfTextBlock endBlock = range.EndBlock ?? startBlock;
        string partPath = startBlock.PartPath;
        OdfNode startNode = CreateOfficeNode("annotation", partPath);
        StyleDocumentModel.WriteAttribute(startNode,
            OdfNamespaces.Office, "name", OdfNamespaces.OfficePrefix, name);
        OdfNode? endNode = null;
        if (range.IsRange)
        {
            endNode = CreateOfficeNode("annotation-end", partPath);
            StyleDocumentModel.WriteAttribute(endNode,
                OdfNamespaces.Office, "name", OdfNamespaces.OfficePrefix,
                name);
            InsertMarker(endBlock, range.EndOffset, endNode);
        }
        InsertMarker(startBlock, range.StartOffset, startNode);
        OdfAnnotation created = CloneEditableFields(template);
        created.Name = name;
        created.Parent = null;
        created.ParentName = "";
        created.Node = startNode;
        created.EndNode = endNode;
        created.StartBlock = startBlock;
        created.EndBlock = range.IsRange ? endBlock : null;
        created.IsRange = range.IsRange;
        created.PartPath = partPath;
        created.OriginalText = "";
        document.Annotations = AppendAnnotation(
            document.Annotations, created);
        AnnotationDocumentModel.SynchronizeAnnotations(document);
        return created;
    }

    internal static OdfAnnotation CreateReply(
        OdfDocument document,
        OdfAnnotation parent,
        OdfAnnotation template)
    {
        RequireDocument(document);
        RequireAnnotation(document, parent);
        ValidateTemplate(document, template);
        string name = SelectName(document, template.Name);
        RequireUniqueXmlId(document, template.XmlId, null);
        AnnotationDocumentModel.SynchronizeAnnotations(document);
        OdfNode parentNode = parent.Node ?? new OdfNode();
        if (parentNode.Parent == null)
        {
            throw new OdfEditException(
                "An annotation reply requires an attached parent annotation.");
        }
        OdfAnnotation reference = ReplyInsertionReference(document, parent);
        OdfNode referenceNode = reference.Node ?? parentNode;
        OdfNode replyNode = CreateOfficeNode(
            "annotation", parent.PartPath);
        StyleDocumentModel.WriteAttribute(replyNode,
            OdfNamespaces.Office, "name", OdfNamespaces.OfficePrefix, name);
        InsertAfter(referenceNode, replyNode);
        OdfAnnotation created = CloneEditableFields(template);
        created.Name = name;
        created.Parent = parent;
        created.ParentName = parent.Name;
        created.Node = replyNode;
        created.StartBlock = parent.StartBlock;
        created.PartPath = parent.PartPath;
        created.OriginalText = "";
        document.Annotations = InsertAnnotationAfter(
            document.Annotations, reference, created);
        AnnotationDocumentModel.SynchronizeAnnotations(document);
        return created;
    }

    internal static OdfAnnotation UpdateAnnotation(
        OdfDocument document,
        OdfAnnotation annotation,
        OdfAnnotation replacement)
    {
        RequireDocument(document);
        RequireAnnotation(document, annotation);
        ValidateTemplate(document, replacement);
        string name = replacement.Name.Length == 0
            ? annotation.Name
            : replacement.Name;
        RequireUniqueName(document, name, annotation);
        RequireUniqueXmlId(document, replacement.XmlId, annotation);
        CopyEditableFields(annotation, replacement);
        annotation.Name = name;
        AnnotationDocumentModel.SynchronizeAnnotations(document);
        return annotation;
    }

    internal static OdfAnnotation ResolveAnnotation(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        RequireDocument(document);
        RequireAnnotation(document, annotation);
        annotation.State = OdfAnnotationState.Resolved;
        AnnotationDocumentModel.SynchronizeAnnotations(document);
        return annotation;
    }

    internal static OdfAnnotation ReopenAnnotation(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        RequireDocument(document);
        RequireAnnotation(document, annotation);
        annotation.State = OdfAnnotationState.Open;
        AnnotationDocumentModel.SynchronizeAnnotations(document);
        return annotation;
    }

    internal static bool RemoveAnnotation(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        RequireDocument(document);
        int match = AnnotationIndex(document.Annotations, annotation);
        if (match < 0)
        {
            return false;
        }
        int index = 0;
        while (index < document.Annotations.Length)
        {
            OdfAnnotation current = document.Annotations[index];
            if (current.Parent == annotation)
            {
                current.Parent = null;
                current.ParentName = "";
            }
            index = index + 1;
        }
        document.Annotations = RemoveAnnotationAt(
            document.Annotations, match);
        AnnotationDocumentModel.SynchronizeAnnotations(document);
        return true;
    }

    internal static int RemoveThread(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        RequireDocument(document);
        if (AnnotationIndex(document.Annotations, annotation) < 0)
        {
            return 0;
        }
        List<OdfAnnotation> retained = new List<OdfAnnotation>();
        int removed = 0;
        int index = 0;
        while (index < document.Annotations.Length)
        {
            OdfAnnotation current = document.Annotations[index];
            if (BelongsToThread(
                current, annotation, document.Annotations.Length))
            {
                removed = removed + 1;
            }
            else
            {
                retained.Add(current);
            }
            index = index + 1;
        }
        document.Annotations = retained.ToArray();
        AnnotationDocumentModel.SynchronizeAnnotations(document);
        return removed;
    }

    static AnnotationCursorRange RequireCursorRange(
        OdfDocument document,
        OdfTextCursor cursor)
    {
        if (cursor == null || !ContainsCursor(document.TextCursors, cursor))
        {
            throw new OdfEditException(
                "The text cursor does not belong to the document.");
        }
        OdfTextBlock? active = cursor.Block;
        OdfTextBlock? anchor = cursor.HasSelection
            ? cursor.AnchorBlock
            : cursor.Block;
        if (active == null || anchor == null)
        {
            throw new OdfEditException(
                "An annotation cursor requires text blocks.");
        }
        int activeIndex = BlockIndex(document.TextBlocks, active);
        int anchorIndex = BlockIndex(document.TextBlocks, anchor);
        if (activeIndex < 0 || anchorIndex < 0
            || cursor.Offset < 0 || cursor.Offset > active.Text.Length
            || cursor.AnchorOffset < 0
            || cursor.AnchorOffset > anchor.Text.Length)
        {
            throw new OdfEditException(
                "The annotation cursor is outside the document text.");
        }
        bool activeFirst = activeIndex < anchorIndex
            || activeIndex == anchorIndex
            && cursor.Offset <= cursor.AnchorOffset;
        AnnotationCursorRange result = new AnnotationCursorRange();
        result.StartBlock = activeFirst ? active : anchor;
        result.StartOffset = activeFirst
            ? cursor.Offset
            : cursor.AnchorOffset;
        result.StartIndex = activeFirst ? activeIndex : anchorIndex;
        result.EndBlock = activeFirst ? anchor : active;
        result.EndOffset = activeFirst
            ? cursor.AnchorOffset
            : cursor.Offset;
        result.EndIndex = activeFirst ? anchorIndex : activeIndex;
        result.IsRange = cursor.HasSelection
            && (result.StartIndex != result.EndIndex
                || result.StartOffset != result.EndOffset);
        return result;
    }

    static void RequireRangePart(AnnotationCursorRange range)
    {
        OdfTextBlock start = range.StartBlock ?? new OdfTextBlock();
        OdfTextBlock end = range.EndBlock ?? new OdfTextBlock();
        if (start.PartPath != end.PartPath)
        {
            throw new OdfEditException(
                "An annotation range cannot cross XML parts.");
        }
        if (start.Node == null || end.Node == null
            || start.Node.Parent == null || end.Node.Parent == null)
        {
            throw new OdfEditException(
                "An annotation requires attached text blocks.");
        }
    }

    static void InsertMarker(
        OdfTextBlock block,
        int offset,
        OdfNode marker)
    {
        OdfNode? blockNode = block.Node;
        if (blockNode == null)
        {
            throw new OdfEditException(
                "An annotation marker requires a text-block element.");
        }
        if (offset == 0)
        {
            InsertChild(blockNode, 0, marker);
            return;
        }
        List<AnnotationInsertionSegment> segments =
            new List<AnnotationInsertionSegment>();
        int length = CollectInsertionSegments(blockNode, 0, segments);
        if (offset > length)
        {
            throw new OdfEditException(
                "The annotation offset cannot be mapped to XML text.");
        }
        if (offset == length)
        {
            InsertChild(blockNode, blockNode.Children.Length, marker);
            return;
        }
        int index = 0;
        while (index < segments.Count)
        {
            AnnotationInsertionSegment segment = segments[index];
            int end = segment.Start + segment.Length;
            if (offset >= segment.Start && offset <= end)
            {
                OdfNode? segmentNode = segment.Node;
                if (segmentNode == null || IsInsideAnnotation(segmentNode))
                {
                    throw new OdfEditException(
                        "An annotation cannot be anchored inside another annotation body.");
                }
                if (segment.IsText
                    && offset > segment.Start && offset < end)
                {
                    SplitTextAndInsert(
                        segmentNode, offset - segment.Start, marker);
                }
                else if (offset == segment.Start)
                {
                    InsertBeside(segmentNode, marker, true);
                }
                else
                {
                    InsertBeside(segmentNode, marker, false);
                }
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The annotation offset cannot be mapped to XML text.");
    }

    static int CollectInsertionSegments(
        OdfNode node,
        int offset,
        List<AnnotationInsertionSegment> segments)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            AnnotationInsertionSegment text = new AnnotationInsertionSegment();
            text.Node = node;
            text.Start = offset;
            text.Length = node.Value.Length;
            text.IsText = true;
            segments.Add(text);
            return offset + text.Length;
        }
        if (IsTextControl(node))
        {
            AnnotationInsertionSegment control =
                new AnnotationInsertionSegment();
            control.Node = node;
            control.Start = offset;
            control.Length = TextControlLength(node);
            segments.Add(control);
            return offset + control.Length;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            offset = CollectInsertionSegments(
                node.Children[index], offset, segments);
            index = index + 1;
        }
        return offset;
    }

    static bool IsTextControl(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text
            && (node.Name.LocalName == "s"
                || node.Name.LocalName == "tab"
                || node.Name.LocalName == "line-break");
    }

    static int TextControlLength(OdfNode node)
    {
        if (node.Name != null && node.Name.LocalName == "s")
        {
            string lexical = StyleDocumentModel.ReadAttribute(
                node, OdfNamespaces.Text, "c");
            OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(lexical);
            if (parsed.IsValid && parsed.IntegerValue > 0
                && parsed.IntegerValue <= int.MaxValue)
            {
                return (int)parsed.IntegerValue;
            }
        }
        return 1;
    }

    static void SplitTextAndInsert(
        OdfNode text,
        int offset,
        OdfNode marker)
    {
        OdfNode? parent = text.Parent;
        if (parent == null)
        {
            throw new OdfEditException(
                "An annotation marker requires attached XML text.");
        }
        int index = NodeIndex(parent.Children, text);
        string value = text.Value;
        text.Value = value.Substring(0, offset);
        OdfNode suffix = new OdfNode();
        suffix.Kind = OdfNodeKind.Text;
        suffix.Value = value.Substring(offset);
        suffix.IsSignificantWhitespace = text.IsSignificantWhitespace;
        suffix.Location.PartPath = text.Location.PartPath;
        OdfNode[] children = new OdfNode[parent.Children.Length + 2];
        Array.Copy(parent.Children, 0, children, 0, index + 1);
        children[index + 1] = marker;
        children[index + 2] = suffix;
        Array.Copy(parent.Children, index + 1, children, index + 3,
            parent.Children.Length - index - 1);
        parent.Children = children;
        marker.Parent = parent;
        marker.Location.PartPath = parent.Location.PartPath;
        suffix.Parent = parent;
        parent.WasEmptyElement = false;
    }

    static void InsertBeside(
        OdfNode reference,
        OdfNode addition,
        bool before)
    {
        OdfNode? parent = reference.Parent;
        if (parent == null)
        {
            throw new OdfEditException(
                "An annotation marker requires an attached XML node.");
        }
        int index = NodeIndex(parent.Children, reference);
        InsertChild(parent, before ? index : index + 1, addition);
    }

    static void InsertAfter(OdfNode reference, OdfNode addition)
    {
        InsertBeside(reference, addition, false);
    }

    static void InsertChild(
        OdfNode parent,
        int index,
        OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, 0, children, 0, index);
        children[index] = child;
        Array.Copy(parent.Children, index, children, index + 1,
            parent.Children.Length - index);
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
    }

    static int NodeIndex(OdfNode[] nodes, OdfNode target)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The annotation insertion reference is detached.");
    }

    static bool IsInsideAnnotation(OdfNode node)
    {
        OdfNode? current = node;
        while (current != null)
        {
            if (current.Kind == OdfNodeKind.Element && current.Name != null
                && current.Name.NamespaceName == OdfNamespaces.Office
                && current.Name.LocalName == "annotation")
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static OdfAnnotation CloneEditableFields(OdfAnnotation source)
    {
        OdfAnnotation result = new OdfAnnotation();
        CopyEditableFields(result, source);
        return result;
    }

    static void CopyEditableFields(
        OdfAnnotation target,
        OdfAnnotation source)
    {
        target.Name = source.Name;
        target.XmlId = source.XmlId;
        target.Creator = source.Creator;
        target.Date = source.Date;
        target.DateString = source.DateString;
        target.Initials = source.Initials;
        target.Text = source.Text;
        target.State = source.State;
        target.Display = source.Display;
        target.X = source.X;
        target.Y = source.Y;
        target.Width = source.Width;
        target.Height = source.Height;
        target.StyleName = source.StyleName;
        target.TextStyleName = source.TextStyleName;
        target.AnchorType = source.AnchorType;
        target.AnchorPageNumber = source.AnchorPageNumber;
        target.EndCellAddress = source.EndCellAddress;
        target.EndX = source.EndX;
        target.EndY = source.EndY;
    }

    static void ValidateTemplate(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        if (annotation == null || annotation.Name == null
            || annotation.XmlId == null || annotation.Creator == null
            || annotation.Date == null || annotation.DateString == null
            || annotation.Initials == null || annotation.Text == null
            || annotation.ParentName == null || annotation.Display == null
            || annotation.X == null || annotation.Y == null
            || annotation.Width == null || annotation.Height == null
            || annotation.StyleName == null
            || annotation.TextStyleName == null
            || annotation.AnchorType == null
            || annotation.AnchorPageNumber == null
            || annotation.EndCellAddress == null || annotation.EndX == null
            || annotation.EndY == null)
        {
            throw new OdfEditException(
                "Annotation metadata and content cannot be null.");
        }
        if (annotation.Date.Length > 0 && !IsDate(annotation.Date))
        {
            throw new OdfEditException(
                "The annotation date is not a valid date or date-time.");
        }
        if (annotation.State != OdfAnnotationState.Unknown
            && annotation.State != OdfAnnotationState.Open
            && annotation.State != OdfAnnotationState.Resolved)
        {
            throw new OdfEditException(
                "The annotation state is not recognized.");
        }
        if (annotation.Text.Length
            > document.LoadOptions.SecurityLimits.MaximumXmlTextLength)
        {
            throw new OdfEditException(
                "The annotation content exceeds the XML text limit.");
        }
    }

    static bool IsDate(string value)
    {
        return OdfLexicalParser.ParseDate(value).IsValid
            || OdfLexicalParser.ParseDateTime(value).IsValid;
    }

    static string SelectName(OdfDocument document, string requested)
    {
        if (requested.Length > 0)
        {
            RequireUniqueName(document, requested, null);
            return requested;
        }
        int index = 1;
        string candidate = "__Annotation__1";
        while (CountNamed(document.Annotations, candidate, null) > 0)
        {
            index = index + 1;
            candidate = "__Annotation__"
                + index.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    static void RequireUniqueName(
        OdfDocument document,
        string name,
        OdfAnnotation? excluded)
    {
        RequireName(name);
        if (CountNamed(document.Annotations, name, excluded) > 0)
        {
            throw new OdfEditException(
                "The annotation name is already in use.");
        }
    }

    static int CountNamed(
        OdfAnnotation[] annotations,
        string name,
        OdfAnnotation? excluded)
    {
        int result = 0;
        int index = 0;
        while (index < annotations.Length)
        {
            OdfAnnotation annotation = annotations[index];
            if (annotation != excluded && annotation.Name == name)
            {
                result = result + 1;
            }
            index = index + 1;
        }
        return result;
    }

    static void RequireUniqueXmlId(
        OdfDocument document,
        string xmlId,
        OdfAnnotation? excluded)
    {
        if (xmlId.Length == 0)
        {
            return;
        }
        int index = 0;
        while (index < document.Annotations.Length)
        {
            OdfAnnotation annotation = document.Annotations[index];
            if (annotation != excluded && annotation.XmlId == xmlId)
            {
                throw new OdfEditException(
                    "The annotation XML ID is already in use.");
            }
            index = index + 1;
        }
    }

    static OdfAnnotation ReplyInsertionReference(
        OdfDocument document,
        OdfAnnotation parent)
    {
        OdfAnnotation result = parent;
        int parentIndex = AnnotationIndex(document.Annotations, parent);
        int index = parentIndex + 1;
        while (index < document.Annotations.Length)
        {
            OdfAnnotation candidate = document.Annotations[index];
            if (BelongsToThread(
                candidate, parent, document.Annotations.Length)
                && candidate.Node != null
                && candidate.Node.Parent == parent.Node?.Parent)
            {
                result = candidate;
            }
            index = index + 1;
        }
        return result;
    }

    static bool BelongsToThread(
        OdfAnnotation candidate,
        OdfAnnotation root,
        int limit)
    {
        OdfAnnotation? current = candidate;
        int remaining = limit + 1;
        while (current != null && remaining > 0)
        {
            if (current == root)
            {
                return true;
            }
            current = current.Parent;
            remaining = remaining - 1;
        }
        return false;
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

    static OdfAnnotation[] InsertAnnotationAfter(
        OdfAnnotation[] values,
        OdfAnnotation reference,
        OdfAnnotation value)
    {
        int index = AnnotationIndex(values, reference) + 1;
        OdfAnnotation[] result = new OdfAnnotation[values.Length + 1];
        Array.Copy(values, 0, result, 0, index);
        result[index] = value;
        Array.Copy(values, index, result, index + 1,
            values.Length - index);
        return result;
    }

    static OdfAnnotation[] RemoveAnnotationAt(
        OdfAnnotation[] values,
        int index)
    {
        OdfAnnotation[] result = new OdfAnnotation[values.Length - 1];
        Array.Copy(values, 0, result, 0, index);
        Array.Copy(values, index + 1, result, index,
            values.Length - index - 1);
        return result;
    }

    static int AnnotationIndex(
        OdfAnnotation[] annotations,
        OdfAnnotation target)
    {
        int index = 0;
        while (index < annotations.Length)
        {
            if (annotations[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
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

    static bool ContainsCursor(
        OdfTextCursor[] cursors,
        OdfTextCursor target)
    {
        int index = 0;
        while (index < cursors.Length)
        {
            if (cursors[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfNode CreateOfficeNode(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Office, localName, OdfNamespaces.OfficePrefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static void RequireAnnotation(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        if (annotation == null
            || AnnotationIndex(document.Annotations, annotation) < 0)
        {
            throw new OdfEditException(
                "The annotation does not belong to the document.");
        }
    }

    static void RequireName(string name)
    {
        if (name == null || name.Length == 0)
        {
            throw new OdfEditException(
                "A non-empty annotation name is required.");
        }
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null || document.Annotations == null
            || document.TextBlocks == null || document.TextCursors == null)
        {
            throw new OdfEditException(
                "A materialized document is required for annotation editing.");
        }
    }
}
