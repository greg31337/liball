using System;

namespace html;

internal enum HtmlSelectorNamespaceKind
{
    Unspecified = 0,
    None = 1,
    Any = 2,
    Prefix = 3
}

internal enum HtmlSelectorSimpleKind
{
    Type = 0,
    Universal = 1,
    Id = 2,
    Class = 3,
    Attribute = 4,
    PseudoClass = 5
}

internal enum HtmlSelectorPseudoClassKind
{
    Root = 0,
    Scope = 1,
    Empty = 2,
    NthChild = 3,
    NthLastChild = 4,
    FirstChild = 5,
    LastChild = 6,
    OnlyChild = 7,
    NthOfType = 8,
    NthLastOfType = 9,
    FirstOfType = 10,
    LastOfType = 11,
    OnlyOfType = 12,
    Is = 13,
    Where = 14,
    Not = 15,
    Has = 16,
    Lang = 17,
    Dir = 18,
    AnyLink = 19,
    Link = 20,
    Enabled = 21,
    Disabled = 22,
    Checked = 23,
    Indeterminate = 24,
    Required = 25,
    Optional = 26,
    ReadOnly = 27,
    ReadWrite = 28,
    PlaceholderShown = 29,
    Default = 30
}

internal enum HtmlSelectorAttributeOperator
{
    Exists = 0,
    Equals = 1,
    Includes = 2,
    DashMatch = 3,
    PrefixMatch = 4,
    SuffixMatch = 5,
    SubstringMatch = 6
}

internal enum HtmlSelectorAttributeCase
{
    Default = 0,
    Insensitive = 1,
    Sensitive = 2
}

internal enum HtmlSelectorCombinator
{
    Descendant = 0,
    Child = 1,
    NextSibling = 2,
    SubsequentSibling = 3,
    Column = 4
}

internal class HtmlSelectorQualifiedNameAst
{
    internal HtmlSelectorNamespaceKind namespaceKind;
    internal string namespacePrefix;
    internal string resolvedNamespaceUri;
    internal string localName;
    internal bool universal;
    internal HtmlSourceSpan sourceSpan;

    internal HtmlSelectorQualifiedNameAst(
        HtmlSelectorNamespaceKind namespaceKind, string namespacePrefix,
        string localName, bool universal, HtmlSourceSpan sourceSpan)
    {
        if (namespacePrefix == null)
        {
            throw new ArgumentNullException("namespacePrefix");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        if (namespaceKind == HtmlSelectorNamespaceKind.Prefix
            && namespacePrefix.Length == 0)
        {
            throw new ArgumentException(
                "A named selector namespace requires a prefix.",
                "namespacePrefix");
        }

        if (namespaceKind != HtmlSelectorNamespaceKind.Prefix
            && namespacePrefix.Length != 0)
        {
            throw new ArgumentException(
                "Only a named selector namespace can store a prefix.",
                "namespacePrefix");
        }

        if (universal && localName.Length != 0)
        {
            throw new ArgumentException(
                "A universal qualified name cannot store a local name.",
                "localName");
        }

        if (!universal && localName.Length == 0)
        {
            throw new ArgumentException(
                "A qualified name requires a local name.", "localName");
        }

        this.namespaceKind = namespaceKind;
        this.namespacePrefix = namespacePrefix;
        this.resolvedNamespaceUri = "";
        this.localName = localName;
        this.universal = universal;
        this.sourceSpan = sourceSpan;
    }

    internal HtmlSelectorNamespaceKind GetNamespaceKind()
    {
        return this.namespaceKind;
    }

    internal string GetNamespacePrefix()
    {
        return this.namespacePrefix;
    }

    internal string GetResolvedNamespaceUri()
    {
        return this.resolvedNamespaceUri;
    }

    internal void SetResolvedNamespaceUri(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.resolvedNamespaceUri = value;
    }

    internal string GetLocalName()
    {
        return this.localName;
    }

    internal bool IsUniversal()
    {
        return this.universal;
    }

    internal HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }
}

internal class HtmlSimpleSelectorAst
{
    internal HtmlSelectorSimpleKind kind;
    internal HtmlSelectorQualifiedNameAst? qualifiedName;
    internal string name;
    internal HtmlSelectorAttributeOperator attributeOperator;
    internal string attributeValue;
    internal HtmlSelectorAttributeCase attributeCase;
    internal HtmlSelectorPseudoClassKind pseudoClassKind;
    internal HtmlNthExpression? nthExpression;
    internal HtmlComplexSelectorAst[] selectorArguments;
    internal HtmlRelativeSelectorAst[] relativeSelectorArguments;
    internal string[] pseudoClassArguments;
    internal HtmlSourceSpan sourceSpan;

    internal HtmlSimpleSelectorAst(HtmlSelectorSimpleKind kind,
        HtmlSelectorQualifiedNameAst? qualifiedName, string name,
        HtmlSelectorAttributeOperator attributeOperator,
        string attributeValue, HtmlSelectorAttributeCase attributeCase,
        HtmlSelectorPseudoClassKind pseudoClassKind,
        HtmlNthExpression? nthExpression,
        HtmlSourceSpan sourceSpan)
    {
        this.kind = kind;
        this.qualifiedName = qualifiedName;
        this.name = name;
        this.attributeOperator = attributeOperator;
        this.attributeValue = attributeValue;
        this.attributeCase = attributeCase;
        this.pseudoClassKind = pseudoClassKind;
        this.nthExpression = nthExpression;
        this.selectorArguments = new HtmlComplexSelectorAst[0];
        this.relativeSelectorArguments = new HtmlRelativeSelectorAst[0];
        this.pseudoClassArguments = new string[0];
        this.sourceSpan = sourceSpan;
    }

    internal static HtmlSimpleSelectorAst CreateType(
        HtmlSelectorQualifiedNameAst name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        HtmlSelectorSimpleKind kind = name.IsUniversal()
            ? HtmlSelectorSimpleKind.Universal
            : HtmlSelectorSimpleKind.Type;
        return new HtmlSimpleSelectorAst(kind, name, "",
            HtmlSelectorAttributeOperator.Exists, "",
            HtmlSelectorAttributeCase.Default,
            HtmlSelectorPseudoClassKind.Root, null, name.GetSourceSpan());
    }

    internal static HtmlSimpleSelectorAst CreateId(string name,
        HtmlSourceSpan sourceSpan)
    {
        return CreateNamed(HtmlSelectorSimpleKind.Id, name, sourceSpan);
    }

    internal static HtmlSimpleSelectorAst CreateClass(string name,
        HtmlSourceSpan sourceSpan)
    {
        return CreateNamed(HtmlSelectorSimpleKind.Class, name, sourceSpan);
    }

    internal static HtmlSimpleSelectorAst CreateAttribute(
        HtmlSelectorQualifiedNameAst name,
        HtmlSelectorAttributeOperator attributeOperator,
        string attributeValue, HtmlSelectorAttributeCase attributeCase,
        HtmlSourceSpan sourceSpan)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (attributeValue == null)
        {
            throw new ArgumentNullException("attributeValue");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        if ((int)attributeOperator
                < (int)HtmlSelectorAttributeOperator.Exists
            || (int)attributeOperator
                > (int)HtmlSelectorAttributeOperator.SubstringMatch)
        {
            throw new ArgumentOutOfRangeException("attributeOperator",
                (int)attributeOperator,
                "Unknown selector attribute operator.");
        }

        if ((int)attributeCase < (int)HtmlSelectorAttributeCase.Default
            || (int)attributeCase
                > (int)HtmlSelectorAttributeCase.Sensitive)
        {
            throw new ArgumentOutOfRangeException("attributeCase",
                (int)attributeCase,
                "Unknown selector attribute case modifier.");
        }

        if (attributeOperator == HtmlSelectorAttributeOperator.Exists
            && (attributeValue.Length != 0
                || attributeCase != HtmlSelectorAttributeCase.Default))
        {
            throw new ArgumentException(
                "A presence attribute selector cannot have a value or "
                    + "case modifier.", "attributeOperator");
        }

        return new HtmlSimpleSelectorAst(HtmlSelectorSimpleKind.Attribute,
            name, "", attributeOperator, attributeValue, attributeCase,
            HtmlSelectorPseudoClassKind.Root, null, sourceSpan);
    }

    internal static HtmlSimpleSelectorAst CreatePseudoClass(
        HtmlSelectorPseudoClassKind pseudoClassKind,
        HtmlNthExpression? nthExpression, HtmlSourceSpan sourceSpan)
    {
        if ((int)pseudoClassKind < (int)HtmlSelectorPseudoClassKind.Root
            || (int)pseudoClassKind
                > (int)HtmlSelectorPseudoClassKind.Default)
        {
            throw new ArgumentOutOfRangeException("pseudoClassKind",
                (int)pseudoClassKind, "Unknown selector pseudo-class.");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        bool nth = IsNthPseudoClass(pseudoClassKind);
        if (IsLogicalPseudoClass(pseudoClassKind))
        {
            throw new ArgumentException(
                "A logical pseudo-class requires selector arguments.",
                "pseudoClassKind");
        }

        if (pseudoClassKind == HtmlSelectorPseudoClassKind.Has)
        {
            throw new ArgumentException(
                "The :has() pseudo-class requires relative selectors.",
                "pseudoClassKind");
        }

        if (pseudoClassKind == HtmlSelectorPseudoClassKind.Lang
            || pseudoClassKind == HtmlSelectorPseudoClassKind.Dir)
        {
            throw new ArgumentException(
                "This pseudo-class requires string arguments.",
                "pseudoClassKind");
        }

        if (nth && nthExpression == null)
        {
            throw new ArgumentNullException("nthExpression");
        }

        if (!nth && nthExpression != null)
        {
            throw new ArgumentException(
                "Only an nth pseudo-class can store an An+B expression.",
                "nthExpression");
        }

        return new HtmlSimpleSelectorAst(HtmlSelectorSimpleKind.PseudoClass,
            null, "", HtmlSelectorAttributeOperator.Exists, "",
            HtmlSelectorAttributeCase.Default, pseudoClassKind,
            nthExpression, sourceSpan);
    }

    internal static HtmlSimpleSelectorAst CreateLogicalPseudoClass(
        HtmlSelectorPseudoClassKind pseudoClassKind,
        HtmlComplexSelectorAst[] selectorArguments,
        HtmlSourceSpan sourceSpan)
    {
        if (!IsLogicalPseudoClass(pseudoClassKind))
        {
            throw new ArgumentException(
                "Selector arguments require a logical pseudo-class.",
                "pseudoClassKind");
        }

        if (selectorArguments == null)
        {
            throw new ArgumentNullException("selectorArguments");
        }

        if (pseudoClassKind == HtmlSelectorPseudoClassKind.Not
            && selectorArguments.Length == 0)
        {
            throw new ArgumentException(
                "The :not() pseudo-class requires a selector argument.",
                "selectorArguments");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        HtmlSimpleSelectorAst? result = new HtmlSimpleSelectorAst(
            HtmlSelectorSimpleKind.PseudoClass, null, "",
            HtmlSelectorAttributeOperator.Exists, "",
            HtmlSelectorAttributeCase.Default, pseudoClassKind, null,
            sourceSpan);
        result.selectorArguments = CopySelectorArguments(selectorArguments);
        return result;
    }

    internal static HtmlSimpleSelectorAst CreateHasPseudoClass(
        HtmlRelativeSelectorAst[] relativeSelectorArguments,
        HtmlSourceSpan sourceSpan)
    {
        if (relativeSelectorArguments == null)
        {
            throw new ArgumentNullException("relativeSelectorArguments");
        }

        if (relativeSelectorArguments.Length == 0)
        {
            throw new ArgumentException(
                "The :has() pseudo-class requires a relative selector.",
                "relativeSelectorArguments");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        HtmlSimpleSelectorAst? result = new HtmlSimpleSelectorAst(
            HtmlSelectorSimpleKind.PseudoClass, null, "",
            HtmlSelectorAttributeOperator.Exists, "",
            HtmlSelectorAttributeCase.Default,
            HtmlSelectorPseudoClassKind.Has, null, sourceSpan);
        result.relativeSelectorArguments = CopyRelativeSelectorArguments(
            relativeSelectorArguments);
        return result;
    }

    internal static HtmlSimpleSelectorAst CreateArgumentPseudoClass(
        HtmlSelectorPseudoClassKind pseudoClassKind, string[] arguments,
        HtmlSourceSpan sourceSpan)
    {
        if (pseudoClassKind != HtmlSelectorPseudoClassKind.Lang
            && pseudoClassKind != HtmlSelectorPseudoClassKind.Dir)
        {
            throw new ArgumentException(
                "String arguments require :lang() or :dir().",
                "pseudoClassKind");
        }

        if (arguments == null)
        {
            throw new ArgumentNullException("arguments");
        }

        if (arguments.Length == 0
            || pseudoClassKind == HtmlSelectorPseudoClassKind.Dir
                && arguments.Length != 1)
        {
            throw new ArgumentException(
                "The pseudo-class has an invalid number of arguments.",
                "arguments");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        HtmlSimpleSelectorAst? result = new HtmlSimpleSelectorAst(
            HtmlSelectorSimpleKind.PseudoClass, null, "",
            HtmlSelectorAttributeOperator.Exists, "",
            HtmlSelectorAttributeCase.Default, pseudoClassKind, null,
            sourceSpan);
        result.pseudoClassArguments = CopyStringArguments(arguments);
        return result;
    }

    internal HtmlSelectorSimpleKind GetKind()
    {
        return this.kind;
    }

    internal HtmlSelectorQualifiedNameAst? GetQualifiedName()
    {
        return this.qualifiedName;
    }

    internal string GetName()
    {
        return this.name;
    }

    internal HtmlSelectorAttributeOperator GetAttributeOperator()
    {
        return this.attributeOperator;
    }

    internal string GetAttributeValue()
    {
        return this.attributeValue;
    }

    internal HtmlSelectorAttributeCase GetAttributeCase()
    {
        return this.attributeCase;
    }

    internal HtmlSelectorPseudoClassKind GetPseudoClassKind()
    {
        return this.pseudoClassKind;
    }

    internal HtmlNthExpression? GetNthExpression()
    {
        return this.nthExpression;
    }

    internal HtmlComplexSelectorAst[] GetSelectorArguments()
    {
        return CopySelectorArguments(this.selectorArguments);
    }

    internal HtmlComplexSelectorAst[] GetSelectorArgumentsStorage()
    {
        return this.selectorArguments;
    }

    internal HtmlRelativeSelectorAst[] GetRelativeSelectorArguments()
    {
        return CopyRelativeSelectorArguments(
            this.relativeSelectorArguments);
    }

    internal HtmlRelativeSelectorAst[] GetRelativeSelectorArgumentsStorage()
    {
        return this.relativeSelectorArguments;
    }

    internal string[] GetPseudoClassArguments()
    {
        return CopyStringArguments(this.pseudoClassArguments);
    }

    internal string[] GetPseudoClassArgumentsStorage()
    {
        return this.pseudoClassArguments;
    }

    internal HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }

    internal static HtmlSimpleSelectorAst CreateNamed(HtmlSelectorSimpleKind kind,
        string name, HtmlSourceSpan sourceSpan)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException(
                "A named simple selector cannot be empty.", "name");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        return new HtmlSimpleSelectorAst(kind, null, name,
            HtmlSelectorAttributeOperator.Exists, "",
            HtmlSelectorAttributeCase.Default,
            HtmlSelectorPseudoClassKind.Root, null, sourceSpan);
    }

    internal static bool IsNthPseudoClass(
        HtmlSelectorPseudoClassKind pseudoClassKind)
    {
        return pseudoClassKind == HtmlSelectorPseudoClassKind.NthChild
            || pseudoClassKind == HtmlSelectorPseudoClassKind.NthLastChild
            || pseudoClassKind == HtmlSelectorPseudoClassKind.NthOfType
            || pseudoClassKind
                == HtmlSelectorPseudoClassKind.NthLastOfType;
    }

    internal static bool IsLogicalPseudoClass(
        HtmlSelectorPseudoClassKind pseudoClassKind)
    {
        return pseudoClassKind == HtmlSelectorPseudoClassKind.Is
            || pseudoClassKind == HtmlSelectorPseudoClassKind.Where
            || pseudoClassKind == HtmlSelectorPseudoClassKind.Not;
    }

    internal static HtmlComplexSelectorAst[] CopySelectorArguments(
        HtmlComplexSelectorAst[] values)
    {
        HtmlComplexSelectorAst[] result = new HtmlComplexSelectorAst[
            values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A logical selector argument cannot be null.",
                    "selectorArguments");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static HtmlRelativeSelectorAst[] CopyRelativeSelectorArguments(
        HtmlRelativeSelectorAst[] values)
    {
        HtmlRelativeSelectorAst[] result = new HtmlRelativeSelectorAst[
            values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A relative selector argument cannot be null.",
                    "relativeSelectorArguments");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static string[] CopyStringArguments(string[] values)
    {
        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A pseudo-class argument cannot be null.",
                    "arguments");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}

internal class HtmlRelativeSelectorAst
{
    internal HtmlSelectorCombinator leadingCombinator;
    internal HtmlComplexSelectorAst selector;
    internal HtmlSourceSpan sourceSpan;

    internal HtmlRelativeSelectorAst(
        HtmlSelectorCombinator leadingCombinator,
        HtmlComplexSelectorAst selector, HtmlSourceSpan sourceSpan)
    {
        if ((int)leadingCombinator
                < (int)HtmlSelectorCombinator.Descendant
            || (int)leadingCombinator
                > (int)HtmlSelectorCombinator.SubsequentSibling)
        {
            throw new ArgumentOutOfRangeException("leadingCombinator",
                (int)leadingCombinator,
                "Unsupported relative selector combinator.");
        }

        if (selector == null)
        {
            throw new ArgumentNullException("selector");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        this.leadingCombinator = leadingCombinator;
        this.selector = selector;
        this.sourceSpan = sourceSpan;
    }

    internal HtmlSelectorCombinator GetLeadingCombinator()
    {
        return this.leadingCombinator;
    }

    internal HtmlComplexSelectorAst GetSelector()
    {
        return this.selector;
    }

    internal HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }
}

internal class HtmlCompoundSelectorAst
{
    internal HtmlSimpleSelectorAst[] simpleSelectors;
    internal HtmlSourceSpan sourceSpan;

    internal HtmlCompoundSelectorAst(HtmlSimpleSelectorAst[] simpleSelectors,
        HtmlSourceSpan sourceSpan)
    {
        if (simpleSelectors == null)
        {
            throw new ArgumentNullException("simpleSelectors");
        }

        if (simpleSelectors.Length == 0)
        {
            throw new ArgumentException(
                "A compound selector cannot be empty.", "simpleSelectors");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        this.simpleSelectors = CopySimpleSelectors(simpleSelectors);
        this.sourceSpan = sourceSpan;
    }

    internal HtmlSimpleSelectorAst[] GetSimpleSelectors()
    {
        return CopySimpleSelectors(this.simpleSelectors);
    }

    internal HtmlSimpleSelectorAst[] GetSimpleSelectorsStorage()
    {
        return this.simpleSelectors;
    }

    internal HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }

    internal static HtmlSimpleSelectorAst[] CopySimpleSelectors(
        HtmlSimpleSelectorAst[] values)
    {
        HtmlSimpleSelectorAst[] result = new HtmlSimpleSelectorAst[
            values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A simple selector cannot be null.", "simpleSelectors");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}

internal class HtmlComplexSelectorAst
{
    internal HtmlCompoundSelectorAst[] compounds;
    internal HtmlSelectorCombinator[] combinators;
    internal HtmlSourceSpan sourceSpan;

    internal HtmlComplexSelectorAst(HtmlCompoundSelectorAst[] compounds,
        HtmlSelectorCombinator[] combinators, HtmlSourceSpan sourceSpan)
    {
        if (compounds == null)
        {
            throw new ArgumentNullException("compounds");
        }

        if (combinators == null)
        {
            throw new ArgumentNullException("combinators");
        }

        if (compounds.Length == 0)
        {
            throw new ArgumentException(
                "A complex selector cannot be empty.", "compounds");
        }

        if (combinators.Length + 1 != compounds.Length)
        {
            throw new ArgumentException(
                "A complex selector requires one combinator between compounds.",
                "combinators");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        this.compounds = CopyCompounds(compounds);
        this.combinators = CopyCombinators(combinators);
        this.sourceSpan = sourceSpan;
    }

    internal HtmlCompoundSelectorAst[] GetCompounds()
    {
        return CopyCompounds(this.compounds);
    }

    internal HtmlCompoundSelectorAst[] GetCompoundsStorage()
    {
        return this.compounds;
    }

    internal HtmlSelectorCombinator[] GetCombinators()
    {
        return CopyCombinators(this.combinators);
    }

    internal HtmlSelectorCombinator[] GetCombinatorsStorage()
    {
        return this.combinators;
    }

    internal HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }

    internal static HtmlCompoundSelectorAst[] CopyCompounds(
        HtmlCompoundSelectorAst[] values)
    {
        HtmlCompoundSelectorAst[] result = new HtmlCompoundSelectorAst[
            values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A compound selector cannot be null.", "compounds");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static HtmlSelectorCombinator[] CopyCombinators(
        HtmlSelectorCombinator[] values)
    {
        HtmlSelectorCombinator[] result = new HtmlSelectorCombinator[
            values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}

internal class HtmlSelectorListAst
{
    internal HtmlComplexSelectorAst[] selectors;
    internal HtmlSourceSpan sourceSpan;

    internal HtmlSelectorListAst(HtmlComplexSelectorAst[] selectors,
        HtmlSourceSpan sourceSpan)
    {
        if (selectors == null)
        {
            throw new ArgumentNullException("selectors");
        }

        if (selectors.Length == 0)
        {
            throw new ArgumentException(
                "A selector list cannot be empty.", "selectors");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        this.selectors = CopySelectors(selectors);
        this.sourceSpan = sourceSpan;
    }

    internal HtmlComplexSelectorAst[] GetSelectors()
    {
        return CopySelectors(this.selectors);
    }

    internal HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }

    internal static HtmlComplexSelectorAst[] CopySelectors(
        HtmlComplexSelectorAst[] values)
    {
        HtmlComplexSelectorAst[] result = new HtmlComplexSelectorAst[
            values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A selector cannot be null.", "selectors");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
