using System;
using System.Collections.Generic;

namespace odf;

internal class TextEditEndpoints
{
    internal OdfTextBlock? StartBlock;
    internal int StartOffset;
    internal OdfTextBlock? EndBlock;
    internal int EndOffset;
    internal int StartIndex;
    internal int EndIndex;
}

internal static class TextEditingModel
{
    internal static OdfTextCursor CreateCursor(
        OdfDocument document,
        OdfTextBlock block,
        int offset)
    {
        RequireBlockPosition(document, block, offset);
        OdfTextCursor cursor = new OdfTextCursor();
        cursor.Block = block;
        cursor.Offset = offset;
        cursor.AnchorBlock = block;
        cursor.AnchorOffset = offset;
        cursor.HasSelection = false;
        OdfTextCursor[] cursors =
            new OdfTextCursor[document.TextCursors.Length + 1];
        Array.Copy(document.TextCursors, cursors, document.TextCursors.Length);
        cursors[cursors.Length - 1] = cursor;
        document.TextCursors = cursors;
        return cursor;
    }

    internal static void SetSelection(
        OdfDocument document,
        OdfTextCursor cursor,
        OdfTextBlock anchorBlock,
        int anchorOffset)
    {
        RequireCursor(document, cursor);
        RequireBlockPosition(document, anchorBlock, anchorOffset);
        cursor.AnchorBlock = anchorBlock;
        cursor.AnchorOffset = anchorOffset;
        cursor.HasSelection = cursor.Block != anchorBlock
            || cursor.Offset != anchorOffset;
    }

    internal static void ClearSelection(
        OdfDocument document,
        OdfTextCursor cursor)
    {
        RequireCursor(document, cursor);
        cursor.AnchorBlock = cursor.Block;
        cursor.AnchorOffset = cursor.Offset;
        cursor.HasSelection = false;
    }

    internal static void InsertText(
        OdfDocument document,
        OdfTextCursor cursor,
        string text)
    {
        RequireCursor(document, cursor);
        if (text == null)
        {
            throw new OdfEditException("Inserted text cannot be null.");
        }
        if (cursor.HasSelection)
        {
            DeleteSelectedText(document, cursor);
        }
        OdfTextBlock block = cursor.Block ?? new OdfTextBlock();
        int offset = cursor.Offset;
        block.Text = block.Text.Substring(0, offset)
            + text
            + block.Text.Substring(offset);
        UpdateCursorsForInsertion(document.TextCursors, block, offset, text.Length);
        cursor.Offset = offset + text.Length;
        cursor.AnchorBlock = cursor.Block;
        cursor.AnchorOffset = cursor.Offset;
        cursor.HasSelection = false;
    }

    internal static void DeleteSelectedText(
        OdfDocument document,
        OdfTextCursor cursor)
    {
        RequireCursor(document, cursor);
        if (!cursor.HasSelection)
        {
            return;
        }
        TextEditEndpoints endpoints = SelectionEndpoints(document, cursor);
        OdfTextBlock start = endpoints.StartBlock ?? new OdfTextBlock();
        OdfTextBlock end = endpoints.EndBlock ?? new OdfTextBlock();
        string prefix = start.Text.Substring(0, endpoints.StartOffset);
        string suffix = end.Text.Substring(endpoints.EndOffset);
        UpdateCursorsForDeletion(document, endpoints, prefix.Length);
        start.Text = prefix + suffix;
        if (endpoints.EndIndex > endpoints.StartIndex)
        {
            RemoveBlockRange(
                document,
                endpoints.StartIndex + 1,
                endpoints.EndIndex);
        }
        cursor.Block = start;
        cursor.Offset = endpoints.StartOffset;
        cursor.AnchorBlock = start;
        cursor.AnchorOffset = endpoints.StartOffset;
        cursor.HasSelection = false;
        TextRangeModel.LoadTextRanges(document);
        AnnotationDocumentModel.LoadAnnotations(document);
    }

    internal static void DeleteForward(
        OdfDocument document,
        OdfTextCursor cursor,
        int count)
    {
        RequireCursor(document, cursor);
        if (count < 0)
        {
            throw new OdfEditException("A deletion count cannot be negative.");
        }
        if (cursor.HasSelection)
        {
            DeleteSelectedText(document, cursor);
            return;
        }
        OdfTextBlock block = cursor.Block ?? new OdfTextBlock();
        int available = block.Text.Length - cursor.Offset;
        int removed = count < available ? count : available;
        if (removed == 0)
        {
            return;
        }
        int end = cursor.Offset + removed;
        block.Text = block.Text.Remove(cursor.Offset, removed);
        UpdateCursorsForLocalDeletion(
            document.TextCursors,
            block,
            cursor.Offset,
            end);
        cursor.AnchorBlock = cursor.Block;
        cursor.AnchorOffset = cursor.Offset;
    }

    internal static OdfTextBlock SplitBlock(
        OdfDocument document,
        OdfTextCursor cursor)
    {
        RequireCursor(document, cursor);
        if (cursor.HasSelection)
        {
            DeleteSelectedText(document, cursor);
        }
        OdfTextBlock source = cursor.Block ?? new OdfTextBlock();
        RequireAttachedBlock(source);
        int offset = cursor.Offset;
        OdfTextBlock created = CreateSiblingBlock(source);
        created.Text = source.Text.Substring(offset);
        source.Text = source.Text.Substring(0, offset);
        InsertBlockNodeAfter(source, created);
        int sourceIndex = FindBlockIndex(document.TextBlocks, source);
        document.TextBlocks = InsertBlockArray(
            document.TextBlocks,
            sourceIndex + 1,
            created);
        UpdateCursorsForSplit(document.TextCursors, source, created, offset);
        cursor.Block = created;
        cursor.Offset = 0;
        cursor.AnchorBlock = created;
        cursor.AnchorOffset = 0;
        cursor.HasSelection = false;
        return created;
    }

    internal static void MergeNextBlock(
        OdfDocument document,
        OdfTextBlock block)
    {
        int index = RequireBlock(document, block);
        if (index + 1 >= document.TextBlocks.Length)
        {
            throw new OdfEditException("The text block has no following block.");
        }
        OdfTextBlock next = document.TextBlocks[index + 1];
        RequireAdjacentSiblings(block, next);
        int boundary = block.Text.Length;
        block.Text = block.Text + next.Text;
        UpdateCursorsForMerge(document.TextCursors, block, next, boundary);
        RemoveAttachedNode(next.Node);
        document.TextBlocks = RemoveBlockArray(
            document.TextBlocks,
            index + 1,
            index + 1);
        TextRangeModel.LoadTextRanges(document);
        AnnotationDocumentModel.LoadAnnotations(document);
    }

    internal static OdfTextBlock CopyBlock(
        OdfDocument document,
        OdfTextBlock source,
        OdfTextBlock destination,
        bool before)
    {
        RequireBlock(document, source);
        int destinationIndex = RequireBlock(document, destination);
        RequireAttachedBlock(source);
        RequireAttachedBlock(destination);
        OdfNode sourceNode = source.Node ?? new OdfNode();
        OdfNode cloneNode = new OdfTreeEditor([]).CloneSubtree(sourceNode);
        InsertNodeBeside(destination, cloneNode, before);
        OdfTextBlock clone = TextDocumentModel.CreateTextBlock(
            cloneNode,
            destination.PartPath,
            document.LoadOptions.SecurityLimits.MaximumXmlTextLength);
        int insertionIndex = before
            ? destinationIndex
            : destinationIndex + 1;
        document.TextBlocks = InsertBlockArray(
            document.TextBlocks,
            insertionIndex,
            clone);
        TextRangeModel.LoadTextRanges(document);
        AnnotationDocumentModel.LoadAnnotations(document);
        return clone;
    }

    internal static void MoveBlock(
        OdfDocument document,
        OdfTextBlock source,
        OdfTextBlock destination,
        bool before)
    {
        int sourceIndex = RequireBlock(document, source);
        int destinationIndex = RequireBlock(document, destination);
        if (source == destination)
        {
            return;
        }
        RequireAttachedBlock(source);
        RequireAttachedBlock(destination);
        OdfNode sourceNode = source.Node ?? new OdfNode();
        StyleDocumentModel.RemoveNodeFromParent(sourceNode);
        InsertNodeBeside(destination, sourceNode, before);
        OdfTextBlock[] without = RemoveBlockArray(
            document.TextBlocks,
            sourceIndex,
            sourceIndex);
        if (sourceIndex < destinationIndex)
        {
            destinationIndex = destinationIndex - 1;
        }
        int insertionIndex = before
            ? destinationIndex
            : destinationIndex + 1;
        document.TextBlocks = InsertBlockArray(
            without,
            insertionIndex,
            source);
        TextRangeModel.LoadTextRanges(document);
        AnnotationDocumentModel.LoadAnnotations(document);
    }

    internal static void ApplyParagraphStyle(
        OdfDocument document,
        OdfTextCursor cursor,
        string styleName)
    {
        RequireCursor(document, cursor);
        TextEditEndpoints endpoints = SelectionEndpoints(document, cursor);
        int index = endpoints.StartIndex;
        while (index <= endpoints.EndIndex)
        {
            document.TextBlocks[index].StyleName = styleName;
            index = index + 1;
        }
    }

    internal static void ApplyInlineStyle(
        OdfDocument document,
        OdfTextCursor cursor,
        string styleName)
    {
        RequireCursor(document, cursor);
        TextEditEndpoints endpoints = SelectionEndpoints(document, cursor);
        if (!cursor.HasSelection
            || endpoints.StartBlock != endpoints.EndBlock)
        {
            throw new OdfEditException(
                "Inline style application requires a selection in one block.");
        }
        OdfTextBlock block = endpoints.StartBlock ?? new OdfTextBlock();
        List<OdfTextRun> runs = new List<OdfTextRun>();
        AddTextRun(runs, block.Text.Substring(0, endpoints.StartOffset));
        OdfTextRun span = new OdfTextRun();
        span.Kind = OdfTextRunKind.Span;
        span.StyleName = styleName;
        span.Text = block.Text.Substring(
            endpoints.StartOffset,
            endpoints.EndOffset - endpoints.StartOffset);
        OdfTextRun selected = new OdfTextRun();
        selected.Kind = OdfTextRunKind.Text;
        selected.Text = span.Text;
        span.Runs = [selected];
        runs.Add(span);
        AddTextRun(runs, block.Text.Substring(endpoints.EndOffset));
        block.Runs = runs.ToArray();
        block.OriginalText = block.Text;
    }

    internal static void ReplaceTextRange(
        OdfDocument document,
        OdfTextBlock block,
        int offset,
        int length,
        string replacement)
    {
        RequireBlockPosition(document, block, offset);
        if (length < 0 || offset + length > block.Text.Length)
        {
            throw new OdfEditException(
                "The replacement range is outside the text block.");
        }
        if (replacement == null)
        {
            throw new OdfEditException("Replacement text cannot be null.");
        }
        block.Text = block.Text.Substring(0, offset)
            + replacement
            + block.Text.Substring(offset + length);
        UpdateCursorsForLocalDeletion(
            document.TextCursors,
            block,
            offset,
            offset + length);
        UpdateCursorsForInsertion(
            document.TextCursors,
            block,
            offset,
            replacement.Length);
    }

    internal static void TrackTextReplacement(
        OdfDocument document,
        OdfTextBlock block,
        int offset,
        int length,
        int replacementLength)
    {
        RequireBlockPosition(document, block, offset);
        if (length < 0
            || offset + length > block.Text.Length
            || replacementLength < 0)
        {
            throw new OdfEditException(
                "The replacement range is outside the text block.");
        }
        UpdateCursorsForLocalDeletion(
            document.TextCursors,
            block,
            offset,
            offset + length);
        UpdateCursorsForInsertion(
            document.TextCursors,
            block,
            offset,
            replacementLength);
    }

    static void AddTextRun(List<OdfTextRun> runs, string text)
    {
        if (text.Length == 0)
        {
            return;
        }
        OdfTextRun run = new OdfTextRun();
        run.Kind = OdfTextRunKind.Text;
        run.Text = text;
        runs.Add(run);
    }

    static TextEditEndpoints SelectionEndpoints(
        OdfDocument document,
        OdfTextCursor cursor)
    {
        OdfTextBlock active = cursor.Block ?? new OdfTextBlock();
        OdfTextBlock anchor = cursor.AnchorBlock ?? active;
        int activeIndex = RequireBlock(document, active);
        int anchorIndex = RequireBlock(document, anchor);
        TextEditEndpoints result = new TextEditEndpoints();
        bool activeFirst = activeIndex < anchorIndex
            || activeIndex == anchorIndex
            && cursor.Offset <= cursor.AnchorOffset;
        result.StartBlock = activeFirst ? active : anchor;
        result.StartOffset = activeFirst ? cursor.Offset : cursor.AnchorOffset;
        result.StartIndex = activeFirst ? activeIndex : anchorIndex;
        result.EndBlock = activeFirst ? anchor : active;
        result.EndOffset = activeFirst ? cursor.AnchorOffset : cursor.Offset;
        result.EndIndex = activeFirst ? anchorIndex : activeIndex;
        return result;
    }

    static OdfTextBlock CreateSiblingBlock(OdfTextBlock source)
    {
        OdfTextBlock result = new OdfTextBlock();
        result.IsHeading = source.IsHeading;
        result.OutlineLevel = source.OutlineLevel;
        result.RestartNumbering = source.RestartNumbering;
        result.StartValue = source.StartValue;
        result.IsListHeader = source.IsListHeader;
        result.StyleName = source.StyleName;
        result.ClassNames = source.ClassNames;
        result.ConditionalStyleName = source.ConditionalStyleName;
        result.PartPath = source.PartPath;
        OdfNode node = new OdfNode();
        node.Kind = OdfNodeKind.Element;
        node.Name = new OdfQualifiedName();
        node.Name.NamespaceName = OdfNamespaces.Text;
        node.Name.LocalName = source.IsHeading ? "h" : "p";
        node.Name.Prefix = OdfNamespaces.TextPrefix;
        node.Location.PartPath = source.PartPath;
        result.Node = node;
        result.OriginalNode = node;
        return result;
    }

    static void InsertBlockNodeAfter(
        OdfTextBlock source,
        OdfTextBlock created)
    {
        OdfNode sourceNode = source.Node ?? new OdfNode();
        OdfNode createdNode = created.Node ?? new OdfNode();
        InsertNodeBeside(source, createdNode, false);
        createdNode.Location.PartPath = sourceNode.Location.PartPath;
    }

    static void InsertNodeBeside(
        OdfTextBlock destination,
        OdfNode node,
        bool before)
    {
        OdfNode destinationNode = destination.Node ?? new OdfNode();
        OdfNode? parent = destinationNode.Parent;
        if (parent == null)
        {
            throw new OdfEditException(
                "A block insertion requires an attached destination block.");
        }
        int index = FindNodeIndex(parent.Children, destinationNode);
        int insertionIndex = before ? index : index + 1;
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, 0, children, 0, insertionIndex);
        children[insertionIndex] = node;
        Array.Copy(
            parent.Children,
            insertionIndex,
            children,
            insertionIndex + 1,
            parent.Children.Length - insertionIndex);
        parent.Children = children;
        node.Parent = parent;
        node.Location.PartPath = destinationNode.Location.PartPath;
        parent.WasEmptyElement = false;
    }

    static void RemoveBlockRange(
        OdfDocument document,
        int first,
        int last)
    {
        int index = first;
        while (index <= last)
        {
            RemoveAttachedNode(document.TextBlocks[index].Node);
            index = index + 1;
        }
        document.TextBlocks = RemoveBlockArray(
            document.TextBlocks,
            first,
            last);
    }

    static void RemoveAttachedNode(OdfNode? node)
    {
        if (node != null && node.Parent != null)
        {
            StyleDocumentModel.RemoveNodeFromParent(node);
        }
    }

    static OdfTextBlock[] InsertBlockArray(
        OdfTextBlock[] blocks,
        int index,
        OdfTextBlock block)
    {
        OdfTextBlock[] result = new OdfTextBlock[blocks.Length + 1];
        Array.Copy(blocks, 0, result, 0, index);
        result[index] = block;
        Array.Copy(blocks, index, result, index + 1, blocks.Length - index);
        return result;
    }

    static OdfTextBlock[] RemoveBlockArray(
        OdfTextBlock[] blocks,
        int first,
        int last)
    {
        OdfTextBlock[] result =
            new OdfTextBlock[blocks.Length - (last - first + 1)];
        Array.Copy(blocks, 0, result, 0, first);
        Array.Copy(
            blocks,
            last + 1,
            result,
            first,
            blocks.Length - last - 1);
        return result;
    }

    static void UpdateCursorsForInsertion(
        OdfTextCursor[] cursors,
        OdfTextBlock block,
        int offset,
        int length)
    {
        int index = 0;
        while (index < cursors.Length)
        {
            OdfTextCursor cursor = cursors[index];
            if (cursor.Block == block && cursor.Offset >= offset)
            {
                cursor.Offset = cursor.Offset + length;
            }
            if (cursor.AnchorBlock == block && cursor.AnchorOffset >= offset)
            {
                cursor.AnchorOffset = cursor.AnchorOffset + length;
            }
            index = index + 1;
        }
    }

    static void UpdateCursorsForLocalDeletion(
        OdfTextCursor[] cursors,
        OdfTextBlock block,
        int start,
        int end)
    {
        int index = 0;
        while (index < cursors.Length)
        {
            OdfTextCursor cursor = cursors[index];
            if (cursor.Block == block)
            {
                cursor.Offset = DeletedOffset(cursor.Offset, start, end);
            }
            if (cursor.AnchorBlock == block)
            {
                cursor.AnchorOffset = DeletedOffset(
                    cursor.AnchorOffset,
                    start,
                    end);
            }
            index = index + 1;
        }
    }

    static int DeletedOffset(int offset, int start, int end)
    {
        if (offset <= start)
        {
            return offset;
        }
        if (offset <= end)
        {
            return start;
        }
        return offset - (end - start);
    }

    static void UpdateCursorsForSplit(
        OdfTextCursor[] cursors,
        OdfTextBlock source,
        OdfTextBlock created,
        int offset)
    {
        int index = 0;
        while (index < cursors.Length)
        {
            OdfTextCursor cursor = cursors[index];
            if (cursor.Block == source && cursor.Offset > offset)
            {
                cursor.Block = created;
                cursor.Offset = cursor.Offset - offset;
            }
            if (cursor.AnchorBlock == source && cursor.AnchorOffset > offset)
            {
                cursor.AnchorBlock = created;
                cursor.AnchorOffset = cursor.AnchorOffset - offset;
            }
            index = index + 1;
        }
    }

    static void UpdateCursorsForMerge(
        OdfTextCursor[] cursors,
        OdfTextBlock first,
        OdfTextBlock second,
        int boundary)
    {
        int index = 0;
        while (index < cursors.Length)
        {
            OdfTextCursor cursor = cursors[index];
            if (cursor.Block == second)
            {
                cursor.Block = first;
                cursor.Offset = boundary + cursor.Offset;
            }
            if (cursor.AnchorBlock == second)
            {
                cursor.AnchorBlock = first;
                cursor.AnchorOffset = boundary + cursor.AnchorOffset;
            }
            index = index + 1;
        }
    }

    static void UpdateCursorsForDeletion(
        OdfDocument document,
        TextEditEndpoints endpoints,
        int prefixLength)
    {
        OdfTextBlock start = endpoints.StartBlock ?? new OdfTextBlock();
        OdfTextBlock end = endpoints.EndBlock ?? new OdfTextBlock();
        int index = 0;
        while (index < document.TextCursors.Length)
        {
            OdfTextCursor cursor = document.TextCursors[index];
            UpdateCursorPointForDeletion(
                document,
                cursor,
                true,
                endpoints,
                start,
                end,
                prefixLength);
            UpdateCursorPointForDeletion(
                document,
                cursor,
                false,
                endpoints,
                start,
                end,
                prefixLength);
            index = index + 1;
        }
    }

    static void UpdateCursorPointForDeletion(
        OdfDocument document,
        OdfTextCursor cursor,
        bool active,
        TextEditEndpoints endpoints,
        OdfTextBlock start,
        OdfTextBlock end,
        int prefixLength)
    {
        OdfTextBlock? block = active ? cursor.Block : cursor.AnchorBlock;
        int offset = active ? cursor.Offset : cursor.AnchorOffset;
        int blockIndex = FindBlockIndex(document.TextBlocks, block);
        if (blockIndex < endpoints.StartIndex
            || blockIndex == endpoints.StartIndex
            && offset <= endpoints.StartOffset)
        {
            return;
        }
        if (blockIndex > endpoints.EndIndex
            || blockIndex == endpoints.EndIndex
            && offset >= endpoints.EndOffset)
        {
            if (block == end)
            {
                block = start;
                offset = prefixLength + offset - endpoints.EndOffset;
            }
        }
        else
        {
            block = start;
            offset = endpoints.StartOffset;
        }
        if (active)
        {
            cursor.Block = block;
            cursor.Offset = offset;
        }
        else
        {
            cursor.AnchorBlock = block;
            cursor.AnchorOffset = offset;
        }
    }

    static void RequireBlockPosition(
        OdfDocument document,
        OdfTextBlock block,
        int offset)
    {
        RequireBlock(document, block);
        if (offset < 0 || offset > block.Text.Length)
        {
            throw new OdfEditException(
                "The text cursor offset is outside the block.");
        }
    }

    static void RequireCursor(
        OdfDocument document,
        OdfTextCursor cursor)
    {
        if (document == null || cursor == null)
        {
            throw new OdfDocumentException(
                "A document and text cursor are required.");
        }
        bool found = false;
        int index = 0;
        while (index < document.TextCursors.Length)
        {
            if (document.TextCursors[index] == cursor)
            {
                found = true;
            }
            index = index + 1;
        }
        if (!found || cursor.Block == null)
        {
            throw new OdfEditException(
                "The text cursor does not belong to the document.");
        }
        RequireBlockPosition(document, cursor.Block, cursor.Offset);
        if (cursor.AnchorBlock != null)
        {
            RequireBlockPosition(
                document,
                cursor.AnchorBlock,
                cursor.AnchorOffset);
        }
    }

    static int RequireBlock(OdfDocument document, OdfTextBlock block)
    {
        if (document == null || block == null)
        {
            throw new OdfDocumentException(
                "A document and text block are required.");
        }
        int index = FindBlockIndex(document.TextBlocks, block);
        if (index < 0)
        {
            throw new OdfEditException(
                "The text block does not belong to the document.");
        }
        return index;
    }

    static void RequireAttachedBlock(OdfTextBlock block)
    {
        if (block.Node == null || block.Node.Parent == null)
        {
            throw new OdfEditException(
                "The text block is not attached to the document tree.");
        }
    }

    static void RequireAdjacentSiblings(
        OdfTextBlock first,
        OdfTextBlock second)
    {
        RequireAttachedBlock(first);
        RequireAttachedBlock(second);
        OdfNode firstNode = first.Node ?? new OdfNode();
        OdfNode secondNode = second.Node ?? new OdfNode();
        if (firstNode.Parent != secondNode.Parent)
        {
            throw new OdfEditException(
                "Only adjacent sibling text blocks can be merged.");
        }
        OdfNode parent = firstNode.Parent ?? new OdfNode();
        int firstIndex = FindNodeIndex(parent.Children, firstNode);
        int secondIndex = FindNodeIndex(parent.Children, secondNode);
        if (secondIndex != firstIndex + 1)
        {
            throw new OdfEditException(
                "Only adjacent sibling text blocks can be merged.");
        }
    }

    static int FindBlockIndex(
        OdfTextBlock[] blocks,
        OdfTextBlock? target)
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

    static int FindNodeIndex(OdfNode[] nodes, OdfNode target)
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
        return -1;
    }
}
