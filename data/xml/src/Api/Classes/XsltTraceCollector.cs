using System.Collections.Generic;

namespace xml;

/// <summary>Provides the baseline XSLT trace collector.</summary>
public class XsltTraceCollector : IXsltTraceReceiver
{
    internal XsltTraceEvent? Value;
    internal List<XsltTraceEvent> Events;

    /// <summary>Creates an XSLT trace collector.</summary>
    public XsltTraceCollector()
    {
        Events = new List<XsltTraceEvent>();
    }

    /// <summary>Receives an XSLT trace event.</summary>
    public void ReceiveXsltTrace(XsltTraceEvent value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        XsltTraceEvent copy = CopyXsltCollectedTrace(value);
        lock (Events)
        {
            Value = copy;
            Events.Add(copy);
        }
    }

    internal XsltTraceEvent[] SnapshotXsltTraceEvents()
    {
        lock (Events)
        {
            XsltTraceEvent[] result = new XsltTraceEvent[Events.Count];
            int index = 0;
            while (index < Events.Count)
            {
                result[index] = CopyXsltCollectedTrace(Events[index]);
                index = index + 1;
            }

            return result;
        }
    }

    internal XsltTraceEvent CopyXsltCollectedTrace(XsltTraceEvent source)
    {
        XsltTraceEvent result = new XsltTraceEvent();
        result.Kind = source.Kind;
        result.ComponentName = source.ComponentName == null ? null : new XmlName { Prefix = source.ComponentName.Prefix, LocalName = source.ComponentName.LocalName, NamespaceUri = source.ComponentName.NamespaceUri, QualifiedName = source.ComponentName.QualifiedName };
        result.InstructionName = source.InstructionName;
        result.Message = source.Message;
        result.Range = CopyXsltCollectedTraceRange(source.Range);
        result.Depth = source.Depth;
        result.ContextItem = source.ContextItem == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(source.ContextItem);
        return result;
    }

    internal SourceRange? CopyXsltCollectedTraceRange(SourceRange? source)
    {
        if (source == null) return null;
        SourceRange result = new SourceRange();
        result.Start = CopyXsltCollectedTraceLocation(source.Start);
        result.End = CopyXsltCollectedTraceLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXsltCollectedTraceLocation(SourceLocation? source)
    {
        if (source == null) return null;
        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }
}
