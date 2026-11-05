using System;
using System.Collections.Generic;

namespace html;

internal static class HtmlTreeWalker
{
    internal static HtmlNode[] GetDescendants(HtmlNode root)
    {
        int maximumVisits = HtmlLimits.CreateDefault().GetMaximumVisits();
        List<HtmlNode> result = new List<HtmlNode>();
        List<HtmlNode> pending = new List<HtmlNode>();
        AddChildrenInReverse(pending, root.GetChildrenStorage());
        while (pending.Count > 0)
        {
            if (result.Count == maximumVisits)
            {
                throw new InvalidOperationException(
                    "The tree traversal visit limit was exceeded.");
            }

            int lastIndex = pending.Count - 1;
            HtmlNode current = pending[lastIndex];
            pending.RemoveAt(lastIndex);
            result.Add(current);
            AddChildrenInReverse(pending, current.GetChildrenStorage());
        }

        return result.ToArray();
    }

    internal static HtmlTreeWalkResult WalkDepthFirst(HtmlNode root,
        Func<HtmlNode, HtmlTreeVisitPhase, int, bool> visitor,
        int maximumVisits)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException("visitor");
        }

        ValidateMaximumVisits(maximumVisits);
        HtmlTreeWalkSnapshot snapshot = CreateDepthFirstSnapshot(root,
            maximumVisits);
        HtmlTreeWalkEntry[] entries = snapshot.GetEntries();
        int enteredCount = 0;
        int index = 0;
        while (index < entries.Length)
        {
            HtmlTreeWalkEntry entry = entries[index];
            if (entry.GetPhase() == HtmlTreeVisitPhase.Enter)
            {
                enteredCount = enteredCount + 1;
            }

            if (!visitor(entry.GetNode(), entry.GetPhase(), entry.GetDepth()))
            {
                return HtmlTreeWalkResult.Create(
                    HtmlTreeWalkStatus.Stopped, enteredCount);
            }

            index = index + 1;
        }

        HtmlTreeWalkStatus status = snapshot.WasLimitExceeded()
            ? HtmlTreeWalkStatus.LimitExceeded
            : HtmlTreeWalkStatus.Completed;
        return HtmlTreeWalkResult.Create(status, enteredCount);
    }

    internal static HtmlTreeWalkResult WalkBreadthFirst(HtmlNode root,
        Func<HtmlNode, int, bool> visitor, int maximumVisits)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException("visitor");
        }

        ValidateMaximumVisits(maximumVisits);
        HtmlTreeWalkSnapshot snapshot = CreateBreadthFirstSnapshot(root,
            maximumVisits);
        HtmlTreeWalkEntry[] entries = snapshot.GetEntries();
        int index = 0;
        while (index < entries.Length)
        {
            HtmlTreeWalkEntry entry = entries[index];
            if (!visitor(entry.GetNode(), entry.GetDepth()))
            {
                return HtmlTreeWalkResult.Create(
                    HtmlTreeWalkStatus.Stopped, index + 1);
            }

            index = index + 1;
        }

        HtmlTreeWalkStatus status = snapshot.WasLimitExceeded()
            ? HtmlTreeWalkStatus.LimitExceeded
            : HtmlTreeWalkStatus.Completed;
        return HtmlTreeWalkResult.Create(status, entries.Length);
    }

    internal static HtmlTreeWalkSnapshot CreateDepthFirstSnapshot(HtmlNode root,
        int maximumVisits)
    {
        List<HtmlTreeWalkEntry> entries = new List<HtmlTreeWalkEntry>();
        List<HtmlTreeWalkFrame> stack = new List<HtmlTreeWalkFrame>();
        stack.Add(new HtmlTreeWalkFrame(root, 0));
        int nodeCount = 0;
        bool limitExceeded = false;
        while (stack.Count > 0)
        {
            HtmlTreeWalkFrame frame = stack[stack.Count - 1];
            if (!frame.WasEntered())
            {
                frame.MarkEntered();
                nodeCount = nodeCount + 1;
                entries.Add(new HtmlTreeWalkEntry(frame.GetNode(),
                    HtmlTreeVisitPhase.Enter, frame.GetDepth()));
                continue;
            }

            if (limitExceeded)
            {
                entries.Add(new HtmlTreeWalkEntry(frame.GetNode(),
                    HtmlTreeVisitPhase.Exit, frame.GetDepth()));
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            if (frame.HasNextChild())
            {
                if (nodeCount == maximumVisits)
                {
                    limitExceeded = true;
                    continue;
                }

                HtmlNode child = frame.TakeNextChild();
                stack.Add(new HtmlTreeWalkFrame(child,
                    frame.GetDepth() + 1));
                continue;
            }

            entries.Add(new HtmlTreeWalkEntry(frame.GetNode(),
                HtmlTreeVisitPhase.Exit, frame.GetDepth()));
            stack.RemoveAt(stack.Count - 1);
        }

        return new HtmlTreeWalkSnapshot(entries.ToArray(), limitExceeded);
    }

    internal static HtmlTreeWalkSnapshot CreateBreadthFirstSnapshot(HtmlNode root,
        int maximumVisits)
    {
        List<HtmlTreeWalkEntry> entries = new List<HtmlTreeWalkEntry>();
        entries.Add(new HtmlTreeWalkEntry(root, HtmlTreeVisitPhase.Enter, 0));
        int index = 0;
        while (index < entries.Count)
        {
            HtmlTreeWalkEntry current = entries[index];
            HtmlNode[] children = current.GetNode().GetChildrenStorage();
            int childIndex = 0;
            while (childIndex < children.Length)
            {
                if (entries.Count == maximumVisits)
                {
                    return new HtmlTreeWalkSnapshot(entries.ToArray(), true);
                }

                entries.Add(new HtmlTreeWalkEntry(children[childIndex],
                    HtmlTreeVisitPhase.Enter, current.GetDepth() + 1));
                childIndex = childIndex + 1;
            }

            index = index + 1;
        }

        return new HtmlTreeWalkSnapshot(entries.ToArray(), false);
    }

    internal static void AddChildrenInReverse(List<HtmlNode> pending,
        HtmlNode[] children)
    {
        int index = children.Length - 1;
        while (index >= 0)
        {
            pending.Add(children[index]);
            index = index - 1;
        }
    }

    internal static void ValidateMaximumVisits(int maximumVisits)
    {
        if (maximumVisits <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumVisits",
                maximumVisits, "A tree walk visit limit must be positive.");
        }
    }
}

internal class HtmlTreeWalkEntry
{
    internal HtmlNode node;
    internal HtmlTreeVisitPhase phase;
    internal int depth;

    internal HtmlTreeWalkEntry(HtmlNode node, HtmlTreeVisitPhase phase, int depth)
    {
        this.node = node;
        this.phase = phase;
        this.depth = depth;
    }

    internal HtmlNode GetNode()
    {
        return this.node;
    }

    internal HtmlTreeVisitPhase GetPhase()
    {
        return this.phase;
    }

    internal int GetDepth()
    {
        return this.depth;
    }
}

internal class HtmlTreeWalkSnapshot
{
    internal HtmlTreeWalkEntry[] entries;
    internal bool limitExceeded;

    internal HtmlTreeWalkSnapshot(HtmlTreeWalkEntry[] entries,
        bool limitExceeded)
    {
        this.entries = entries;
        this.limitExceeded = limitExceeded;
    }

    internal HtmlTreeWalkEntry[] GetEntries()
    {
        return this.entries;
    }

    internal bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }
}

internal class HtmlTreeWalkFrame
{
    internal HtmlNode node;
    internal HtmlNode[] children;
    internal int depth;
    internal int childIndex;
    internal bool entered;

    internal HtmlTreeWalkFrame(HtmlNode node, int depth)
    {
        this.node = node;
        this.children = node.GetChildrenStorage();
        this.depth = depth;
        this.childIndex = 0;
        this.entered = false;
    }

    internal HtmlNode GetNode()
    {
        return this.node;
    }

    internal int GetDepth()
    {
        return this.depth;
    }

    internal bool WasEntered()
    {
        return this.entered;
    }

    internal void MarkEntered()
    {
        this.entered = true;
    }

    internal bool HasNextChild()
    {
        return this.childIndex < this.children.Length;
    }

    internal HtmlNode TakeNextChild()
    {
        HtmlNode result = this.children[this.childIndex];
        this.childIndex = this.childIndex + 1;
        return result;
    }
}
