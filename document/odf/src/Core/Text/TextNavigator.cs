using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal static class TextNavigator
{
    internal static OdfTextRange? FindTextRange(
        OdfDocument document,
        OdfTextRangeKind kind,
        string name)
    {
        RequireDocument(document);
        OdfTextRange? result = null;
        int count = 0;
        int index = 0;
        while (index < document.TextRanges.Length)
        {
            OdfTextRange range = document.TextRanges[index];
            if (range.Kind == kind && range.Name == name)
            {
                result = range;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    internal static OdfNode? FindXmlIdNode(
        OdfDocument document,
        string xmlId)
    {
        RequireDocument(document);
        OdfNode[] matches = new OdfTreeEditor(document.Nodes)
            .SearchXmlId(xmlId);
        return matches.Length == 1 ? matches[0] : null;
    }

    internal static OdfNode[] NavigateTextRange(
        OdfDocument document,
        OdfTextRange range)
    {
        RequireDocument(document);
        if (range == null || !RangeBelongsToDocument(document, range))
        {
            return [];
        }
        if (!range.IsRange)
        {
            return range.StartNode == null ? [] : [range.StartNode];
        }
        if (!range.IsResolved
            || range.StartNode == null
            || range.EndNode == null)
        {
            return [];
        }
        OdfPart? part = FindRangePart(document, range.PartPath);
        if (part == null)
        {
            return [];
        }
        OdfNode[] nodes = new OdfTreeEditor(part.Nodes).TraverseAllNodes();
        int start = FindNodePosition(nodes, range.StartNode);
        int end = FindNodePosition(nodes, range.EndNode);
        if (start < 0 || end <= start)
        {
            return [];
        }
        OdfNode[] result = new OdfNode[end - start - 1];
        Array.Copy(nodes, start + 1, result, 0, result.Length);
        return result;
    }

    internal static string ExtractTextRange(
        OdfDocument document,
        OdfTextRange range)
    {
        OdfNode[] nodes = NavigateTextRange(document, range);
        if (!range.IsRange)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < nodes.Length)
        {
            AppendNavigatedNodeText(result, nodes[index], document);
            index = index + 1;
        }
        return result.ToString();
    }

    internal static void RenameTextRange(
        OdfDocument document,
        OdfTextRange range,
        string newName)
    {
        RequireDocument(document);
        if (range == null || !RangeBelongsToDocument(document, range))
        {
            throw new OdfEditException(
                "The text range to rename does not belong to the document.");
        }
        if (newName.Length == 0)
        {
            throw new OdfEditException("A text-range name cannot be empty.");
        }
        if (range.IsRange && !range.IsResolved)
        {
            throw new OdfEditException(
                "An unresolved or ambiguous text range cannot be renamed safely.");
        }
        OdfTextRange? conflict = FindTextRange(document, range.Kind, newName);
        if (conflict != null && conflict != range)
        {
            throw new OdfEditException(
                "A text range with the requested name already exists.");
        }
        string oldName = range.Name;
        if (range.StartRun != null)
        {
            if (!range.IsRange
                && (range.Kind == OdfTextRangeKind.TableOfContentMark
                    || range.Kind == OdfTextRangeKind.UserIndexMark
                    || range.Kind == OdfTextRangeKind.AlphabeticalIndexMark))
            {
                range.StartRun.StringValue = newName;
            }
            else
            {
                range.StartRun.MarkerName = newName;
            }
        }
        if (range.IsRange && range.EndRun != null)
        {
            range.EndRun.MarkerName = newName;
        }
        range.Name = newName;
        UpdateTextRangeReferences(document, range.Kind, oldName, newName);
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for text navigation.");
        }
    }

    static bool RangeBelongsToDocument(
        OdfDocument document,
        OdfTextRange target)
    {
        int index = 0;
        while (index < document.TextRanges.Length)
        {
            if (document.TextRanges[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfPart? FindRangePart(OdfDocument document, string partPath)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Path == partPath)
            {
                return part;
            }
            index = index + 1;
        }
        return null;
    }

    static int FindNodePosition(OdfNode[] nodes, OdfNode target)
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

    static void AppendNavigatedNodeText(
        StringBuilder result,
        OdfNode node,
        OdfDocument document)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            if (!IsRubyAnnotationText(node))
            {
                result.Append(node.Value);
            }
            return;
        }
        if (node.Kind != OdfNodeKind.Element || node.Name == null)
        {
            return;
        }
        if (node.Name.NamespaceName == OdfNamespaces.Text
            && (node.Name.LocalName == "p" || node.Name.LocalName == "h")
            && result.Length > 0)
        {
            result.Append('\n');
        }
        if (node.Name.NamespaceName != OdfNamespaces.Text)
        {
            return;
        }
        if (node.Name.LocalName == "s")
        {
            AppendExplicitSpaces(result, node, document);
        }
        else if (node.Name.LocalName == "tab")
        {
            result.Append('\t');
        }
        else if (node.Name.LocalName == "line-break")
        {
            result.Append('\n');
        }
    }

    static bool IsRubyAnnotationText(OdfNode node)
    {
        OdfNode? parent = node.Parent;
        return parent != null
            && parent.Name != null
            && parent.Name.NamespaceName == OdfNamespaces.Text
            && parent.Name.LocalName == "ruby-text";
    }

    static void AppendExplicitSpaces(
        StringBuilder result,
        OdfNode node,
        OdfDocument document)
    {
        string lexicalValue = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "c");
        int count = 1;
        if (lexicalValue.Length > 0)
        {
            OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(
                lexicalValue);
            if (parsed.IsValid && parsed.IntegerValue >= 0)
            {
                long maximum = document.LoadOptions.SecurityLimits
                    .MaximumXmlTextLength;
                count = parsed.IntegerValue > maximum
                    ? (int)maximum
                    : (int)parsed.IntegerValue;
            }
        }
        result.Append(new string(' ', count));
    }

    static void UpdateTextRangeReferences(
        OdfDocument document,
        OdfTextRangeKind kind,
        string oldName,
        string newName)
    {
        if (kind != OdfTextRangeKind.Bookmark
            && kind != OdfTextRangeKind.ReferenceMark)
        {
            return;
        }
        string elementName = kind == OdfTextRangeKind.Bookmark
            ? "bookmark-ref"
            : "reference-ref";
        OdfNode[] nodes = new OdfTreeEditor(document.Nodes)
            .TraverseAllNodes();
        int nodeIndex = 0;
        while (nodeIndex < nodes.Length)
        {
            OdfNode node = nodes[nodeIndex];
            if (node.Name != null
                && node.Name.NamespaceName == OdfNamespaces.Text
                && node.Name.LocalName == elementName)
            {
                int attributeIndex = 0;
                while (attributeIndex < node.Attributes.Length)
                {
                    OdfAttribute attribute = node.Attributes[attributeIndex];
                    if (attribute.Name.NamespaceName == OdfNamespaces.Text
                        && attribute.Name.LocalName == "ref-name"
                        && attribute.Value == oldName)
                    {
                        attribute.Value = newName;
                    }
                    attributeIndex = attributeIndex + 1;
                }
            }
            nodeIndex = nodeIndex + 1;
        }
        int fieldIndex = 0;
        while (fieldIndex < document.TextFields.Length)
        {
            OdfTextField field = document.TextFields[fieldIndex];
            if (field.ElementName == elementName
                && field.ReferenceName == oldName)
            {
                field.ReferenceName = newName;
            }
            fieldIndex = fieldIndex + 1;
        }
    }
}
