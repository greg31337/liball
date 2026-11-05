namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgNavigation"/>.</summary>
public interface ISvgNavigation
{
    /// <summary>Creates empty caller-controlled interaction state for an editable document.</summary>
    /// <param name="document">The source SVG document.</param>
    /// <returns>New interaction state backed by dynamic selector callbacks.</returns>
    SvgInteractionState CreateInteractionStateWithDocument(SvgDocument document);
    /// <summary>Computes focusable entries and sequential tab order with secure defaults.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <returns>The bounded focus-order result.</returns>
    SvgFocusOrderResult FocusOrderWithDocument(SvgResolvedDocument document);
    /// <summary>Computes focusable entries and sequential tab order.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <param name="options">The traversal and URI bounds.</param>
    /// <returns>The bounded focus-order result.</returns>
    SvgFocusOrderResult FocusOrderWithDocumentAndOptions(SvgResolvedDocument document, SvgNavigationOptions options);
    /// <summary>Plans activation for a resolved link or one of its descendants.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <param name="node">The activated resolved node.</param>
    /// <returns>A fragment activation or host navigation request.</returns>
    SvgActivationRecord ActivateWithDocumentAndNode(SvgResolvedDocument document, SvgResolvedNode? node);
    /// <summary>Plans bounded activation for a resolved link or descendant.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <param name="node">The activated resolved node.</param>
    /// <param name="options">The traversal and URI bounds.</param>
    /// <returns>A fragment activation or host navigation request.</returns>
    SvgActivationRecord ActivateWithDocumentAndNodeAndOptions(SvgResolvedDocument document, SvgResolvedNode? node, SvgNavigationOptions options);
    /// <summary>Plans activation for the target of a successful hit-test match.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <param name="match">The hit-test match to activate.</param>
    /// <returns>A fragment activation or host navigation request.</returns>
    SvgActivationRecord ActivateWithDocumentAndMatch(SvgResolvedDocument document, SvgHitTestMatch? match);
}
