using System;

namespace html;

internal static class HtmlSelectorLogicalMatcher
{
    internal static bool Matches(HtmlSelectorPseudoClassKind pseudoClassKind,
        bool[] argumentMatches)
    {
        if (argumentMatches == null)
        {
            throw new ArgumentNullException("argumentMatches");
        }

        if (pseudoClassKind != HtmlSelectorPseudoClassKind.Is
            && pseudoClassKind != HtmlSelectorPseudoClassKind.Where
            && pseudoClassKind != HtmlSelectorPseudoClassKind.Not)
        {
            throw new ArgumentException(
                "Logical matching requires :is(), :where(), or :not().",
                "pseudoClassKind");
        }

        if (pseudoClassKind == HtmlSelectorPseudoClassKind.Not
            && argumentMatches.Length == 0)
        {
            throw new ArgumentException(
                "The :not() pseudo-class requires an argument.",
                "argumentMatches");
        }

        bool any = false;
        int index = 0;
        while (index < argumentMatches.Length)
        {
            if (argumentMatches[index])
            {
                any = true;
                break;
            }

            index = index + 1;
        }

        return pseudoClassKind == HtmlSelectorPseudoClassKind.Not
            ? !any
            : any;
    }
}
