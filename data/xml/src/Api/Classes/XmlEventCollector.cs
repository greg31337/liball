using System;
using System.Collections.Generic;

namespace xml;

/// <summary>Provides bounded normalized XML event accumulation with handler backpressure.</summary>
public class XmlEventCollector : IXmlEventHandler, IXmlEventCollector
{
    internal const int DefaultMaximumEventCount = 1024;
    internal List<XmlEvent> Events;
    internal int MaximumEventCount;

    /// <summary>Creates an XML event collector.</summary>
    public XmlEventCollector()
    {
        Events = new List<XmlEvent>();
        MaximumEventCount = DefaultMaximumEventCount;
    }

    /// <summary>Accepts one normalized XML event.</summary>
    public XmlHandlerAction HandleXmlEvent(XmlEvent value)
    {
        Guard.NotNull(value, "value");
        if (Events.Count >= MaximumEventCount)
        {
            throw new InvalidOperationException("The XML event collector must be cleared before it can accept another event.");
        }

        Events.Add(value);
        return Events.Count == MaximumEventCount ? XmlHandlerAction.Pause : XmlHandlerAction.Continue;
    }

    /// <summary>Sets the maximum retained event count and clears retained events.</summary>
    public void ConfigureXmlEventCollection(int maximumCount)
    {
        Guard.Positive(maximumCount, "maximumCount");
        Events.Clear();
        MaximumEventCount = maximumCount;
    }

    /// <summary>Returns the retained events in arrival order.</summary>
    public XmlEvent[] SnapshotCollectedXmlEvents()
    {
        return Events.ToArray();
    }

    /// <summary>Returns the retained event count.</summary>
    public int CountCollectedXmlEvents()
    {
        return Events.Count;
    }

    /// <summary>Clears retained events while preserving the configured maximum.</summary>
    public void ClearCollectedXmlEvents()
    {
        Events.Clear();
    }

    /// <summary>Clears retained events and restores the default maximum.</summary>
    public void ResetXmlEventCollection()
    {
        Events.Clear();
        MaximumEventCount = DefaultMaximumEventCount;
    }
}
