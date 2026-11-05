using System;

namespace markdown;

/// <summary>
/// Adapts caller-provided callbacks to <see cref="IMarkdownNodeVisitor"/>.
/// </summary>
public class MarkdownNodeVisitorAdapter : IMarkdownNodeVisitor
{
    internal Func<MarkdownVisitContext, MarkdownTreeWalkResult>? EnterCallback;
    internal Action<MarkdownVisitContext>? LeaveCallback;

    /// <summary>
    /// Initializes a visitor from optional preorder-enter and postorder-leave callbacks.
    /// </summary>
    /// <param name="enterCallback">The preorder callback, or <c>null</c> to continue automatically.</param>
    /// <param name="leaveCallback">The postorder callback, or <c>null</c> for no leave action.</param>
    public MarkdownNodeVisitorAdapter(
        Func<MarkdownVisitContext, MarkdownTreeWalkResult>? enterCallback,
        Action<MarkdownVisitContext>? leaveCallback)
    {
        EnterCallback = enterCallback;
        LeaveCallback = leaveCallback;
    }

    /// <summary>Invokes the preorder callback.</summary>
    /// <param name="context">The current traversal context.</param>
    /// <returns>The requested traversal action.</returns>
    public MarkdownTreeWalkResult VisitEnter(MarkdownVisitContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (EnterCallback == null)
        {
            return MarkdownTreeWalkResult.Continue;
        }

        return EnterCallback(context);
    }

    /// <summary>Invokes the postorder callback.</summary>
    /// <param name="context">The current traversal context.</param>
    public void VisitLeave(MarkdownVisitContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (LeaveCallback != null)
        {
            LeaveCallback(context);
        }
    }
}
