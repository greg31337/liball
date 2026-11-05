using System;

namespace markdown;

/// <summary>
/// Holds callbacks that run while a Markdown tree is traversed.
/// </summary>
internal class MarkdownNodeVisitor : IMarkdownNodeVisitor
{
    /// <summary>
    /// Stores the enter node value used by this instance.
    /// </summary>
    internal Func<MarkdownVisitContext, MarkdownTreeWalkResult>? EnterNode;
    /// <summary>
    /// Stores the enter document value used by this instance.
    /// </summary>
    internal Func<MarkdownVisitContext, MarkdownTreeWalkResult>? EnterDocument;
    /// <summary>
    /// Stores the enter block value used by this instance.
    /// </summary>
    internal Func<MarkdownVisitContext, MarkdownTreeWalkResult>? EnterBlock;
    /// <summary>
    /// Stores the enter inline value used by this instance.
    /// </summary>
    internal Func<MarkdownVisitContext, MarkdownTreeWalkResult>? EnterInline;
    /// <summary>
    /// Stores the enter extension value used by this instance.
    /// </summary>
    internal Func<MarkdownVisitContext, MarkdownTreeWalkResult>? EnterExtension;
    /// <summary>
    /// Stores the leave node value used by this instance.
    /// </summary>
    internal Action<MarkdownVisitContext>? LeaveNode;
    /// <summary>
    /// Stores the leave document value used by this instance.
    /// </summary>
    internal Action<MarkdownVisitContext>? LeaveDocument;
    /// <summary>
    /// Stores the leave block value used by this instance.
    /// </summary>
    internal Action<MarkdownVisitContext>? LeaveBlock;
    /// <summary>
    /// Stores the leave inline value used by this instance.
    /// </summary>
    internal Action<MarkdownVisitContext>? LeaveInline;
    /// <summary>
    /// Stores the leave extension value used by this instance.
    /// </summary>
    internal Action<MarkdownVisitContext>? LeaveExtension;

    /// <summary>
    /// Initializes a new <see cref="MarkdownNodeVisitor"/> instance.
    /// </summary>
    internal MarkdownNodeVisitor()
    {
        EnterNode = null;
        EnterDocument = null;
        EnterBlock = null;
        EnterInline = null;
        EnterExtension = null;
        LeaveNode = null;
        LeaveDocument = null;
        LeaveBlock = null;
        LeaveInline = null;
        LeaveExtension = null;
    }

    /// <summary>
    /// Invokes visitor callbacks for enter.
    /// </summary>
    /// <param name="context">The context shared by the current operation.</param>
    /// <returns>The resulting <see cref="MarkdownTreeWalkResult"/> value.</returns>
    public MarkdownTreeWalkResult VisitEnter(MarkdownVisitContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownTreeWalkResult result = InvokeEnter(EnterNode, context);
        if (result != MarkdownTreeWalkResult.Continue)
        {
            return result;
        }

        if (context.IsDocument())
        {
            result = InvokeEnter(EnterDocument, context);
            if (result != MarkdownTreeWalkResult.Continue)
            {
                return result;
            }
        }

        if (context.IsBlock())
        {
            result = InvokeEnter(EnterBlock, context);
            if (result != MarkdownTreeWalkResult.Continue)
            {
                return result;
            }
        }

        if (context.IsInline())
        {
            result = InvokeEnter(EnterInline, context);
            if (result != MarkdownTreeWalkResult.Continue)
            {
                return result;
            }
        }

        if (context.IsExtension())
        {
            result = InvokeEnter(EnterExtension, context);
            if (result != MarkdownTreeWalkResult.Continue)
            {
                return result;
            }
        }

        return MarkdownTreeWalkResult.Continue;
    }

    /// <summary>
    /// Invokes visitor callbacks for leave.
    /// </summary>
    /// <param name="context">The context shared by the current operation.</param>
    public void VisitLeave(MarkdownVisitContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (context.IsExtension())
        {
            InvokeLeave(LeaveExtension, context);
        }

        if (context.IsInline())
        {
            InvokeLeave(LeaveInline, context);
        }

        if (context.IsBlock())
        {
            InvokeLeave(LeaveBlock, context);
        }

        if (context.IsDocument())
        {
            InvokeLeave(LeaveDocument, context);
        }

        InvokeLeave(LeaveNode, context);
    }

    internal static MarkdownTreeWalkResult InvokeEnter(Func<MarkdownVisitContext, MarkdownTreeWalkResult>? handler, MarkdownVisitContext context)
    {
        if (handler == null)
        {
            return MarkdownTreeWalkResult.Continue;
        }

        return handler(context);
    }

    internal static void InvokeLeave(Action<MarkdownVisitContext>? handler, MarkdownVisitContext context)
    {
        if (handler == null)
        {
            return;
        }

        handler(context);
    }
}
