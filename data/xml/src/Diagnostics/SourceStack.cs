using System;
using System.Collections.Generic;

namespace xml;

internal enum SourceFrameKind
{
    Document = 0,
    Entity = 1,
    Include = 2
}

internal class SourceFrame
{
    internal SourceFrameKind Kind;
    internal string TraceLabel;
    internal SourceTracker Tracker;

    internal SourceFrame(SourceFrameKind kind, string traceLabel, SourceTracker tracker)
    {
        Kind = kind;
        TraceLabel = traceLabel;
        Tracker = tracker;
    }
}

internal class SourceStack
{
    internal List<SourceFrame> Frames;

    internal SourceStack()
    {
        Frames = new List<SourceFrame>();
    }

    internal void PushDocumentSource(SourceTracker tracker, string documentLabel)
    {
        Guard.NotNull(tracker, "tracker");
        Guard.NotNullOrEmpty(documentLabel, "documentLabel");
        if (Frames.Count != 0)
        {
            throw new InvalidOperationException("A document source must be the first source frame.");
        }

        Frames.Add(new SourceFrame(SourceFrameKind.Document, documentLabel, tracker));
    }

    internal void PushEntitySource(SourceTracker tracker, string entityName)
    {
        Guard.NotNull(tracker, "tracker");
        Guard.NotNullOrEmpty(entityName, "entityName");
        EnsureSourceStackNotEmpty();
        Frames.Add(new SourceFrame(SourceFrameKind.Entity, entityName, tracker));
    }

    internal void PushIncludeSource(SourceTracker tracker, string includeLabel)
    {
        Guard.NotNull(tracker, "tracker");
        Guard.NotNullOrEmpty(includeLabel, "includeLabel");
        EnsureSourceStackNotEmpty();
        Frames.Add(new SourceFrame(SourceFrameKind.Include, includeLabel, tracker));
    }

    internal SourceTracker PopSourceFrame()
    {
        EnsureSourceStackNotEmpty();
        int index = Frames.Count - 1;
        SourceTracker tracker = Frames[index].Tracker;
        Frames.RemoveAt(index);
        return tracker;
    }

    internal SourceTracker CurrentSourceTracker()
    {
        EnsureSourceStackNotEmpty();
        return Frames[Frames.Count - 1].Tracker;
    }

    internal int SourceFrameDepth()
    {
        return Frames.Count;
    }

    internal string[] CreateEntitySourceTrace()
    {
        int count = 0;
        foreach (SourceFrame frame in Frames)
        {
            if (frame.Kind == SourceFrameKind.Document || frame.Kind == SourceFrameKind.Entity)
            {
                count = count + 1;
            }
        }

        string[] trace = new string[count];
        int index = 0;
        foreach (SourceFrame frame in Frames)
        {
            if (frame.Kind == SourceFrameKind.Document || frame.Kind == SourceFrameKind.Entity)
            {
                trace[index] = frame.TraceLabel;
                index = index + 1;
            }
        }

        return trace;
    }

    internal string[] CreateIncludeSourceTrace()
    {
        int count = 0;
        foreach (SourceFrame frame in Frames)
        {
            if (frame.Kind == SourceFrameKind.Include)
            {
                count = count + 1;
            }
        }

        string[] trace = new string[count];
        int index = 0;
        foreach (SourceFrame frame in Frames)
        {
            if (frame.Kind == SourceFrameKind.Include)
            {
                trace[index] = frame.TraceLabel;
                index = index + 1;
            }
        }

        return trace;
    }

    internal XmlDiagnostic CaptureDiagnosticSourceTrace(XmlDiagnostic diagnostic, int rangeStartUtf16Offset)
    {
        Guard.NotNull(diagnostic, "diagnostic");
        SourceTracker current = CurrentSourceTracker();
        diagnostic.Range = current.CreateTrackedRangeToCurrent(rangeStartUtf16Offset);
        diagnostic.EntityStack = CreateEntitySourceTrace();
        diagnostic.IncludeStack = CreateIncludeSourceTrace();
        return diagnostic;
    }

    internal void EnsureSourceStackNotEmpty()
    {
        if (Frames.Count == 0)
        {
            throw new InvalidOperationException("The source stack is empty.");
        }
    }
}
