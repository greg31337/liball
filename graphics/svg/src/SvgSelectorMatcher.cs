namespace svg;

internal class SvgSelectorMatchContext
{
    internal SvgSelectorMatchOptions Options;
    internal SvgNode Scope;
    internal long Work;
    internal bool LimitExceeded;
    internal int MatchedIds;
    internal int MatchedClasses;
    internal int MatchedTypes;
    internal System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgSelectorMatchContext(SvgSelectorMatchOptions options, SvgNode scope)
    {
        Options = options;
        Scope = scope;
        Work = 0;
        LimitExceeded = false;
        MatchedIds = 0;
        MatchedClasses = 0;
        MatchedTypes = 0;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }
}

internal class SvgSelectorMatchFrame
{
    internal int CompoundIndex;
    internal SvgNode Node;

    internal SvgSelectorMatchFrame(int compoundIndex, SvgNode node)
    {
        CompoundIndex = compoundIndex;
        Node = node;
    }
}

internal static class SvgSelectorSpecificityCalculator
{
    internal static void Populate(SvgSelector selector)
    {
        SvgSelectorSpecificity maximum = new SvgSelectorSpecificity(0, 0, 0);
        int index = 0;
        while (index < selector.Items.Count)
        {
            SvgSelectorSpecificity current = Complex(selector.Items[index]);
            selector.Items[index].IdSpecificity = current.Ids;
            selector.Items[index].ClassSpecificity = current.Classes;
            selector.Items[index].TypeSpecificity = current.Types;
            if (current.IsGreaterThan(maximum)) maximum = current;
            index = index + 1;
        }
        selector.IdSpecificity = maximum.Ids;
        selector.ClassSpecificity = maximum.Classes;
        selector.TypeSpecificity = maximum.Types;
    }

    static SvgSelectorSpecificity Complex(SvgComplexSelector selector)
    {
        int ids = 0;
        int classes = 0;
        int types = 0;
        int compoundIndex = 0;
        while (compoundIndex < selector.Compounds.Count)
        {
            int simpleIndex = 0;
            while (simpleIndex < selector.Compounds[compoundIndex].SimpleSelectors.Count)
            {
                SvgSimpleSelector simple = selector.Compounds[compoundIndex].SimpleSelectors[simpleIndex];
                if (simple.Kind == SvgSimpleSelectorKind.Id) ids = ids + 1;
                else if (simple.Kind == SvgSimpleSelectorKind.Class || simple.Kind == SvgSimpleSelectorKind.Attribute) classes = classes + 1;
                else if (simple.Kind == SvgSimpleSelectorKind.Type) types = types + 1;
                else if (simple.Kind == SvgSimpleSelectorKind.PseudoClass)
                {
                    ids = ids + simple.SpecificityIds;
                    classes = classes + simple.SpecificityClasses;
                    types = types + simple.SpecificityTypes;
                }
                simpleIndex = simpleIndex + 1;
            }
            compoundIndex = compoundIndex + 1;
        }
        return new SvgSelectorSpecificity(ids, classes, types);
    }
}
