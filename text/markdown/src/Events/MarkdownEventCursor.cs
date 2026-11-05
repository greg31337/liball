using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownEventCursor
{
    internal MarkdownNode Root;
    internal MarkdownDocument? Document;
    internal MarkdownPipeline Pipeline;
    internal string Source;
    internal List<MarkdownEventCursorFrame> Frames;
    internal MarkdownEvent Current;

    internal MarkdownEventCursor(
        MarkdownNode root,
        MarkdownDocument? document,
        MarkdownPipeline pipeline,
        string source)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        Root = root;
        Document = document;
        Pipeline = pipeline.Copy();
        Source = source;
        Frames = new List<MarkdownEventCursorFrame>();
        Current = new MarkdownEvent();
        Reset();
    }

    internal bool Read()
    {
        while (Frames.Count > 0)
        {
            MarkdownEventCursorFrame frame = Frames[Frames.Count - 1];
            if (!frame.Entered)
            {
                frame.Entered = true;
                Current = MarkdownEvent.Create(
                    MarkdownEventKind.EnterNode,
                    frame.Node,
                    frame.Depth,
                    frame.ChildIndex);
                return true;
            }

            if (frame.NextChildIndex < frame.Node.GetChildCount())
            {
                int childIndex = frame.NextChildIndex;
                frame.NextChildIndex = frame.NextChildIndex + 1;
                Frames.Add(new MarkdownEventCursorFrame(
                    frame.Node.GetChild(childIndex),
                    frame.Depth + 1,
                    childIndex));
            }
            else
            {
                Frames.RemoveAt(Frames.Count - 1);
                Current = MarkdownEvent.Create(
                    MarkdownEventKind.LeaveNode,
                    frame.Node,
                    frame.Depth,
                    frame.ChildIndex);
                return true;
            }
        }

        Current = new MarkdownEvent();
        return false;
    }

    internal MarkdownEvent GetCurrent()
    {
        return Current;
    }

    internal void Reset()
    {
        Frames.Clear();
        Frames.Add(new MarkdownEventCursorFrame(Root, 0, -1));
        Current = new MarkdownEvent();
    }

    internal MarkdownDiagnostic[] GetDiagnostics()
    {
        if (Document == null)
        {
            return new MarkdownDiagnostic[0];
        }

        return Document.GetDiagnostics();
    }

    internal bool HasDiagnostics()
    {
        return Document != null && Document.HasDiagnostics();
    }

    internal bool HasErrors()
    {
        return Document != null && Document.HasErrors();
    }
}
