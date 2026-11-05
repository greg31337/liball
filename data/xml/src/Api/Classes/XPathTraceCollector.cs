using System.Collections.Generic;

namespace xml;

/// <summary>Provides a thread-safe XPath trace collector.</summary>
public class XPathTraceCollector : IXPathTraceReceiver
{
    internal string? Label;
    internal XdmSequence? Value;
    internal SourceRange? Range;
    internal List<XPathTraceEvent> Events;

    /// <summary>Creates an XPath trace collector.</summary>
    public XPathTraceCollector()
    {
        Events = new List<XPathTraceEvent>();
    }

    /// <summary>Receives an XPath trace value.</summary>
    public void ReceiveXPathTrace(string label, XdmSequence value, SourceRange? range)
    {
        if (label == null)
        {
            throw new System.ArgumentNullException("label");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        XPathTraceEvent item = new XPathTraceEvent();
        item.Label = label;
        item.Value = new XdmSequenceOperations().FreezeXdmSequenceStorage(value);
        item.Range = CopyXPathTraceRange(range);
        lock (Events)
        {
            Label = item.Label;
            Value = item.Value;
            Range = item.Range;
            Events.Add(item);
        }
    }

    internal XPathTraceEvent[] SnapshotXPathTraceEvents()
    {
        lock (Events)
        {
            XPathTraceEvent[] result = new XPathTraceEvent[Events.Count];
            int index = 0;
            while (index < Events.Count)
            {
                XPathTraceEvent source = Events[index];
                XPathTraceEvent copy = new XPathTraceEvent();
                copy.Label = source.Label;
                copy.Value = source.Value == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(source.Value);
                copy.Range = CopyXPathTraceRange(source.Range);
                result[index] = copy;
                index = index + 1;
            }

            return result;
        }
    }

    internal SourceRange? CopyXPathTraceRange(SourceRange? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceRange result = new SourceRange();
        result.Start = CopyXPathTraceLocation(source.Start);
        result.End = CopyXPathTraceLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathTraceLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }
}
