using System;
using System.Collections.Generic;
using System.Xml;

namespace iso20022;

internal class BoundedXmlReader : IDisposable
{
    XmlReader reader;
    ResourceLimitGuard guard;
    List<int> childElementCounts = new List<int>();

    internal BoundedXmlReader(XmlReader reader, ResourceLimitGuard guard)
    {
        this.reader = reader;
        this.guard = guard;
    }

    internal bool ReadNext()
    {
        bool hasNode = reader.Read();
        if (!hasNode)
        {
            return false;
        }

        XmlNodeType nodeType = reader.NodeType;
        if (nodeType == XmlNodeType.Element)
        {
            RecordElementNode();
        }

        if (nodeType == XmlNodeType.Text ||
            nodeType == XmlNodeType.CDATA ||
            nodeType == XmlNodeType.Whitespace ||
            nodeType == XmlNodeType.SignificantWhitespace ||
            nodeType == XmlNodeType.Comment ||
            nodeType == XmlNodeType.ProcessingInstruction)
        {
            guard.EnsureTextLength(reader.Value.Length);
        }

        return true;
    }

    internal XmlNodeType GetNodeType()
    {
        return reader.NodeType;
    }

    internal int GetDepth()
    {
        return reader.Depth;
    }

    internal string GetQualifiedName()
    {
        return reader.Name;
    }

    internal string GetLocalName()
    {
        return reader.LocalName;
    }

    internal string GetNamespaceUri()
    {
        return reader.NamespaceURI;
    }

    internal string GetPrefix()
    {
        return reader.Prefix;
    }

    internal string GetValue()
    {
        return reader.Value;
    }

    internal bool GetIsEmptyElement()
    {
        return reader.IsEmptyElement;
    }

    internal int GetAttributeCount()
    {
        return reader.AttributeCount;
    }

    internal SourceLocation CaptureLocation(string? sourceName)
    {
        SourceLocation location = new SourceLocation();
        string? effectiveSourceName = sourceName;
        if (effectiveSourceName == null && reader.BaseURI.Length > 0)
        {
            effectiveSourceName = reader.BaseURI;
        }

        location.SourceName = effectiveSourceName;
        IXmlLineInfo lineInformation = (IXmlLineInfo)reader;
        if (lineInformation.HasLineInfo())
        {
            location.HasLineInformation = true;
            location.LineNumber = lineInformation.LineNumber;
            location.ColumnNumber = lineInformation.LinePosition;
        }

        return location;
    }

    internal bool MoveToFirstAttribute()
    {
        return reader.MoveToFirstAttribute();
    }

    internal bool MoveToNextAttribute()
    {
        return reader.MoveToNextAttribute();
    }

    internal bool MoveBackToElement()
    {
        return reader.MoveToElement();
    }

    internal void DisposeReader()
    {
        reader.Dispose();
    }

    /// <summary>Releases the underlying XML reader and its owned input.</summary>
    public void Dispose()
    {
        reader.Dispose();
    }

    internal void RecordElementNode()
    {
        int depth = reader.Depth;
        guard.RecordElement(depth + 1, reader.AttributeCount);
        EnsureDepthSlot(depth);
        if (depth > 0)
        {
            int parentDepth = depth - 1;
            int itemCount = childElementCounts[parentDepth] + 1;
            childElementCounts[parentDepth] = itemCount;
            guard.EnsureCollectionCount(itemCount);
        }

        childElementCounts[depth] = 0;
        int attributeIndex = 0;
        while (attributeIndex < reader.AttributeCount)
        {
            string? attributeValue = reader.GetAttribute(attributeIndex);
            if (attributeValue != null)
            {
                guard.EnsureTextLength(attributeValue.Length);
            }

            attributeIndex++;
        }
    }

    internal void EnsureDepthSlot(int depth)
    {
        while (childElementCounts.Count <= depth)
        {
            childElementCounts.Add(0);
        }

        int removeIndex = childElementCounts.Count - 1;
        while (removeIndex > depth)
        {
            childElementCounts.RemoveAt(removeIndex);
            removeIndex--;
        }
    }
}
