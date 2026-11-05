namespace svg;

/// <summary>Matches host-driven dynamic pseudo-classes independently of structural selector matching.</summary>
public class SvgDynamicSelectorMatcher : ISvgDynamicSelectorMatcher
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgDynamicSelectorMatcher()
    {
    }

    /// <summary>Matches one parsed dynamic pseudo-class against a node and explicit host state.</summary>
    /// <param name="selector">The parsed dynamic pseudo-class.</param>
    /// <param name="node">The candidate element node.</param>
    /// <param name="state">The host-provided state service.</param>
    /// <returns>A safe match result with diagnostics for invalid or unsupported input.</returns>
    public SvgDynamicSelectorMatchResult Match(SvgSimpleSelector? selector, SvgNode node, ISvgDynamicSelectorState state)
    {
        System.Collections.Generic.List<SvgDiagnostic> diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        if (selector == null || node == null || state == null || selector.Kind != SvgSimpleSelectorKind.PseudoClass)
        {
            SvgSourceSpan? source = selector == null ? null : selector.Source;
            diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "A dynamic pseudo-class, node, and host state are required.", source));
            return new SvgDynamicSelectorMatchResult(false, false, diagnostics);
        }
        if (selector.PseudoClass == SvgPseudoClassKind.UnsupportedDynamic)
        {
            string message = "The dynamic pseudo-class :" + selector.PseudoName + " is preserved but is unsupported and will not match.";
            diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.UnsupportedFeature, message, selector.Source));
            return new SvgDynamicSelectorMatchResult(false, false, diagnostics);
        }
        if (node.Kind != SvgNodeKind.Element) return new SvgDynamicSelectorMatchResult(false, true, diagnostics);
        if (selector.PseudoClass == SvgPseudoClassKind.Link || selector.PseudoClass == SvgPseudoClassKind.Visited)
        {
            SvgSelectorLinkState link = state.GetLinkState(node);
            if (link < SvgSelectorLinkState.None || link > SvgSelectorLinkState.Visited)
            {
                diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidArgument, "The host returned an invalid link state; the pseudo-class does not match.", selector.Source));
                return new SvgDynamicSelectorMatchResult(false, true, diagnostics);
            }
            bool matches = selector.PseudoClass == SvgPseudoClassKind.Link ? link == SvgSelectorLinkState.Unvisited : link == SvgSelectorLinkState.Visited;
            return new SvgDynamicSelectorMatchResult(matches, true, diagnostics);
        }
        if (selector.PseudoClass == SvgPseudoClassKind.Hover)
        {
            bool matches = state.IsHovered(node);
            return new SvgDynamicSelectorMatchResult(matches, true, diagnostics);
        }
        if (selector.PseudoClass == SvgPseudoClassKind.Active)
        {
            bool matches = state.IsActive(node);
            return new SvgDynamicSelectorMatchResult(matches, true, diagnostics);
        }
        if (selector.PseudoClass == SvgPseudoClassKind.Focus)
        {
            bool matches = state.IsFocused(node);
            return new SvgDynamicSelectorMatchResult(matches, true, diagnostics);
        }
        if (selector.PseudoClass == SvgPseudoClassKind.Target)
        {
            bool matches = state.IsTarget(node);
            return new SvgDynamicSelectorMatchResult(matches, true, diagnostics);
        }
        diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "The selector is not a dynamic pseudo-class.", selector.Source));
        return new SvgDynamicSelectorMatchResult(false, false, diagnostics);
    }
}
