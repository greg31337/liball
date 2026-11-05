namespace svg;

/// <summary>Evaluates supports conditions against property and capability catalogs.</summary>
public class SvgSupportsQueryEvaluator : ISvgSupportsQueryEvaluator
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSupportsQueryEvaluator()
    {
    }

    /// <summary>Evaluates a parsed supports query.</summary>
    /// <param name="query">The parsed supports query.</param>
    /// <param name="environment">The property and capability environment.</param>
    /// <param name="limits">The CSS parsing limits.</param>
    /// <returns><c>true</c> when the valid query evaluates true.</returns>
    public bool Matches(SvgSupportsQuery? query, SvgSupportsEnvironment? environment, SvgCssLimits? limits)
    {
        if (query == null || !query.IsValid || environment == null || environment.Properties == null || environment.Capabilities == null || limits == null) return false;
        return new SvgSupportsQueryEvaluator().Evaluate(query.Condition, environment, limits);
    }
bool Evaluate(SvgSupportsCondition? condition, SvgSupportsEnvironment environment, SvgCssLimits limits)
    {
        if (condition == null || !condition.IsValid) return false;
        if (condition.Kind == SvgSupportsConditionKind.Declaration) return environment.Properties.Supports(condition.PropertyName, condition.ValueText, limits, environment.Capabilities);
        if (condition.Kind == SvgSupportsConditionKind.Selector) return environment.Capabilities.Contains(SvgFeatureIdentifiers.CssSelectors) && new SvgSupportsQueryEvaluator().SelectorSupported(condition.Selector);
        if (condition.Kind == SvgSupportsConditionKind.Unknown) return false;
        if (condition.Kind == SvgSupportsConditionKind.Not) return condition.Children.Count == 1 && !new SvgSupportsQueryEvaluator().Evaluate(condition.Children[0], environment, limits);
        if (condition.Kind == SvgSupportsConditionKind.And)
        {
            int index = 0;
            while (index < condition.Children.Count)
            {
                if (!new SvgSupportsQueryEvaluator().Evaluate(condition.Children[index], environment, limits)) return false;
                index = index + 1;
            }
            return condition.Children.Count > 0;
        }
        if (condition.Kind == SvgSupportsConditionKind.Or)
        {
            int index = 0;
            while (index < condition.Children.Count)
            {
                if (new SvgSupportsQueryEvaluator().Evaluate(condition.Children[index], environment, limits)) return true;
                index = index + 1;
            }
        }
        return false;
    }
bool SelectorSupported(SvgSelector? selector)
    {
        if (selector == null || !selector.IsValid) return false;
        int itemIndex = 0;
        while (itemIndex < selector.Items.Count)
        {
            SvgComplexSelector item = selector.Items[itemIndex];
            int compoundIndex = 0;
            while (compoundIndex < item.Compounds.Count)
            {
                SvgCompoundSelector compound = item.Compounds[compoundIndex];
                int simpleIndex = 0;
                while (simpleIndex < compound.SimpleSelectors.Count)
                {
                    SvgSimpleSelector simple = compound.SimpleSelectors[simpleIndex];
                    if (simple.PseudoClass == SvgPseudoClassKind.UnsupportedDynamic) return false;
                    if (simple.SelectorArgument != null && !new SvgSupportsQueryEvaluator().SelectorSupported(simple.SelectorArgument)) return false;
                    simpleIndex = simpleIndex + 1;
                }
                compoundIndex = compoundIndex + 1;
            }
            itemIndex = itemIndex + 1;
        }
        return true;
    }
}
