using System;
using System.Collections.Generic;

namespace html;

internal class HtmlSelectorCandidateHint
{
    internal HtmlSelectorCandidateKind kind;
    internal string value;

    internal HtmlSelectorCandidateHint(HtmlSelectorCandidateKind kindValue,
        string valueValue)
    {
        this.kind = kindValue;
        this.value = valueValue;
    }

    internal HtmlSelectorCandidateKind GetKind()
    {
        return this.kind;
    }

    internal string GetValue()
    {
        return this.value;
    }
}

internal static class HtmlSelectorCompiler
{
    internal static HtmlSelectorCompileResult Compile(string source,
        HtmlLimits limits)
    {
        return CompileConfigured(source, limits,
            new HtmlSelectorNamespaceBinding[0], false);
    }

    internal static HtmlSelectorCompileResult CompileForgiving(string source,
        HtmlLimits limits)
    {
        return CompileConfigured(source, limits,
            new HtmlSelectorNamespaceBinding[0], true);
    }

    internal static HtmlSelectorCompileResult CompileWithNamespaces(
        string source, HtmlSelectorNamespaceBinding[] namespaceBindings,
        HtmlLimits limits)
    {
        return CompileConfigured(source, limits, namespaceBindings, false);
    }

    internal static HtmlSelectorCompileResult CompileForgivingWithNamespaces(
        string source, HtmlSelectorNamespaceBinding[] namespaceBindings,
        HtmlLimits limits)
    {
        return CompileConfigured(source, limits, namespaceBindings, true);
    }

    internal static HtmlSelectorCompileResult CompileConfigured(string source,
        HtmlLimits limits,
        HtmlSelectorNamespaceBinding[] namespaceBindings, bool forgiving)
    {
        if (source == null)
        {
            throw new ArgumentNullException("selector");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        ValidateNamespaceBindings(namespaceBindings);
        limits.Validate();
        HtmlSelectorParser parser = HtmlSelectorParser.Create(source,
            limits);
        HtmlSelectorParseResult? parsed = forgiving
            ? parser.ParseForgiving()
            : parser.Parse();
        if (parsed == null)
        {
            throw new InvalidOperationException(
                "Selector parsing must produce a result.");
        }

        HtmlSelectorListAst? selectorList = parsed.GetSelectorList();
        if (selectorList == null)
        {
            return new HtmlSelectorCompileResult(null,
                parsed.GetDiagnostics());
        }

        HtmlComplexSelectorAst[] selectors = selectorList.GetSelectors();
        List<HtmlComplexSelectorAst> accepted =
            new List<HtmlComplexSelectorAst>();
        List<HtmlDiagnostic> diagnostics = new List<HtmlDiagnostic>(
            parsed.GetDiagnostics());
        int validationIndex = 0;
        while (validationIndex < selectors.Length)
        {
            HtmlDiagnostic? validation = ValidateAndResolve(
                selectors[validationIndex], namespaceBindings);
            if (validation == null)
            {
                accepted.Add(selectors[validationIndex]);
            }
            else
            {
                diagnostics.Add(validation);
                if (!forgiving)
                {
                    SortDiagnostics(diagnostics);
                    return new HtmlSelectorCompileResult(null,
                        diagnostics.ToArray());
                }
            }

            validationIndex = validationIndex + 1;
        }

        if (accepted.Count == 0)
        {
            SortDiagnostics(diagnostics);
            return new HtmlSelectorCompileResult(null,
                diagnostics.ToArray());
        }

        HtmlSelectorMatchPlan[] plans = new HtmlSelectorMatchPlan[
            accepted.Count];
        int index = 0;
        while (index < accepted.Count)
        {
            HtmlSelectorCandidateHint hint = GetCandidateHint(
                accepted[index]);
            plans[index] = new HtmlSelectorMatchPlan(accepted[index],
                GetSpecificity(accepted[index]), hint.GetKind(),
                hint.GetValue());
            index = index + 1;
        }

        SortDiagnostics(diagnostics);
        return new HtmlSelectorCompileResult(
            new HtmlSelector(source, plans, limits),
            diagnostics.ToArray());
    }

    internal static void SortDiagnostics(List<HtmlDiagnostic> diagnostics)
    {
        diagnostics.Sort((left, right) => left.GetSpan().GetStart()
            .GetCharacterOffset().CompareTo(right.GetSpan().GetStart()
                .GetCharacterOffset()));
    }

    internal static void ValidateNamespaceBindings(
        HtmlSelectorNamespaceBinding[] namespaceBindings)
    {
        if (namespaceBindings == null)
        {
            throw new ArgumentNullException("namespaceBindings");
        }

        int index = 0;
        while (index < namespaceBindings.Length)
        {
            HtmlSelectorNamespaceBinding? binding =
                namespaceBindings[index];
            if (binding == null)
            {
                throw new ArgumentException(
                    "A selector namespace binding cannot be null.",
                    "namespaceBindings");
            }

            if (binding.Prefix == null || binding.Prefix.Length == 0)
            {
                throw new ArgumentException(
                    "A selector namespace prefix cannot be null or empty.",
                    "namespaceBindings");
            }

            if (binding.NamespaceUri == null)
            {
                throw new ArgumentException(
                    "A selector namespace URI cannot be null.",
                    "namespaceBindings");
            }

            int earlier = 0;
            while (earlier < index)
            {
                if (namespaceBindings[earlier].Prefix == binding.Prefix)
                {
                    throw new ArgumentException(
                        "A selector namespace prefix cannot be bound more than once.",
                        "namespaceBindings");
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal static HtmlDiagnostic? ValidateAndResolve(
        HtmlComplexSelectorAst selector,
        HtmlSelectorNamespaceBinding[] namespaceBindings)
    {
        HtmlCompoundSelectorAst[] compounds =
            selector.GetCompoundsStorage();
        int index = 0;
        while (index < compounds.Length)
        {
            HtmlSimpleSelectorAst[] simpleSelectors =
                compounds[index].GetSimpleSelectorsStorage();
            int simpleIndex = 0;
            while (simpleIndex < simpleSelectors.Length)
            {
                HtmlDiagnostic? diagnostic = ValidateAndResolve(
                    simpleSelectors[simpleIndex], namespaceBindings);
                if (diagnostic != null)
                {
                    return diagnostic;
                }

                simpleIndex = simpleIndex + 1;
            }

            index = index + 1;
        }

        return null;
    }

    internal static HtmlDiagnostic? ValidateAndResolve(
        HtmlSimpleSelectorAst simple,
        HtmlSelectorNamespaceBinding[] namespaceBindings)
    {
        if (simple.GetKind() == HtmlSelectorSimpleKind.Type
            || simple.GetKind() == HtmlSelectorSimpleKind.Universal
            || simple.GetKind() == HtmlSelectorSimpleKind.Attribute)
        {
            HtmlSelectorQualifiedNameAst? name = simple.GetQualifiedName();
            if (name == null)
            {
                throw new InvalidOperationException(
                    "This selector kind requires a qualified name.");
            }

            if (name.GetNamespaceKind()
                == HtmlSelectorNamespaceKind.Prefix)
            {
                string? namespaceUri = FindNamespaceUri(
                    name.GetNamespacePrefix(), namespaceBindings);
                if (namespaceUri == null)
                {
                    return HtmlDiagnostic.Create(
                        HtmlDiagnosticCode.SelectorInvalidSyntax,
                        HtmlDiagnosticSeverity.Error,
                        "The selector namespace prefix '"
                            + name.GetNamespacePrefix()
                            + "' has not been declared.",
                        name.GetSourceSpan());
                }

                name.SetResolvedNamespaceUri(namespaceUri);
            }
        }

        if (simple.GetKind() != HtmlSelectorSimpleKind.PseudoClass)
        {
            return null;
        }

        HtmlComplexSelectorAst[] nested =
            simple.GetSelectorArgumentsStorage();
        int nestedIndex = 0;
        while (nestedIndex < nested.Length)
        {
            HtmlDiagnostic? nestedDiagnostic = ValidateAndResolve(
                nested[nestedIndex], namespaceBindings);
            if (nestedDiagnostic != null)
            {
                return nestedDiagnostic;
            }

            nestedIndex = nestedIndex + 1;
        }

        HtmlRelativeSelectorAst[] relative =
            simple.GetRelativeSelectorArgumentsStorage();
        int index = 0;
        while (index < relative.Length)
        {
            HtmlDiagnostic? diagnostic = ValidateAndResolve(
                relative[index].GetSelector(), namespaceBindings);
            if (diagnostic != null)
            {
                return diagnostic;
            }

            index = index + 1;
        }

        return null;
    }

    internal static string? FindNamespaceUri(string prefix,
        HtmlSelectorNamespaceBinding[] namespaceBindings)
    {
        int index = 0;
        while (index < namespaceBindings.Length)
        {
            if (namespaceBindings[index].Prefix == prefix)
            {
                return namespaceBindings[index].NamespaceUri;
            }

            index = index + 1;
        }

        return null;
    }

    internal static HtmlSelectorSpecificity GetSpecificity(
        HtmlComplexSelectorAst selector)
    {
        int ids = 0;
        int classes = 0;
        int types = 0;
        HtmlCompoundSelectorAst[] compounds =
            selector.GetCompoundsStorage();
        int compoundIndex = 0;
        while (compoundIndex < compounds.Length)
        {
            HtmlSimpleSelectorAst[] simpleSelectors =
                compounds[compoundIndex].GetSimpleSelectorsStorage();
            int simpleIndex = 0;
            while (simpleIndex < simpleSelectors.Length)
            {
                HtmlSelectorSpecificity part = GetSpecificity(
                    simpleSelectors[simpleIndex]);
                ids = ids + part.GetIdCount();
                classes = classes + part.GetClassCount();
                types = types + part.GetTypeCount();
                simpleIndex = simpleIndex + 1;
            }

            compoundIndex = compoundIndex + 1;
        }

        return new HtmlSelectorSpecificity(ids, classes, types);
    }

    internal static HtmlSelectorSpecificity GetSpecificity(
        HtmlSimpleSelectorAst simple)
    {
        switch (simple.GetKind())
        {
            case HtmlSelectorSimpleKind.Type:
                return new HtmlSelectorSpecificity(0, 0, 1);
            case HtmlSelectorSimpleKind.Universal:
                return new HtmlSelectorSpecificity(0, 0, 0);
            case HtmlSelectorSimpleKind.Id:
                return new HtmlSelectorSpecificity(1, 0, 0);
            case HtmlSelectorSimpleKind.Class:
            case HtmlSelectorSimpleKind.Attribute:
                return new HtmlSelectorSpecificity(0, 1, 0);
            case HtmlSelectorSimpleKind.PseudoClass:
                return GetPseudoClassSpecificity(simple);
            default:
                return new HtmlSelectorSpecificity(0, 0, 0);
        }
    }

    internal static HtmlSelectorSpecificity GetPseudoClassSpecificity(
        HtmlSimpleSelectorAst simple)
    {
        HtmlSelectorPseudoClassKind kind = simple.GetPseudoClassKind();
        if (kind == HtmlSelectorPseudoClassKind.Where)
        {
            return new HtmlSelectorSpecificity(0, 0, 0);
        }

        if (kind == HtmlSelectorPseudoClassKind.Is
            || kind == HtmlSelectorPseudoClassKind.Not)
        {
            return GetMaximumSpecificity(
                simple.GetSelectorArgumentsStorage());
        }

        if (kind == HtmlSelectorPseudoClassKind.Has)
        {
            HtmlRelativeSelectorAst[] relative =
                simple.GetRelativeSelectorArgumentsStorage();
            HtmlSelectorSpecificity maximum =
                new HtmlSelectorSpecificity(0, 0, 0);
            int index = 0;
            while (index < relative.Length)
            {
                HtmlSelectorSpecificity current = GetSpecificity(
                    relative[index].GetSelector());
                if (current.CompareTo(maximum) > 0)
                {
                    maximum = current;
                }

                index = index + 1;
            }

            return maximum;
        }

        return new HtmlSelectorSpecificity(0, 1, 0);
    }

    internal static HtmlSelectorSpecificity GetMaximumSpecificity(
        HtmlComplexSelectorAst[] selectors)
    {
        HtmlSelectorSpecificity maximum = new HtmlSelectorSpecificity(
            0, 0, 0);
        int index = 0;
        while (index < selectors.Length)
        {
            HtmlSelectorSpecificity current = GetSpecificity(
                selectors[index]);
            if (current.CompareTo(maximum) > 0)
            {
                maximum = current;
            }

            index = index + 1;
        }

        return maximum;
    }

    internal static HtmlSelectorCandidateHint GetCandidateHint(
        HtmlComplexSelectorAst selector)
    {
        HtmlCompoundSelectorAst[] compounds =
            selector.GetCompoundsStorage();
        HtmlSimpleSelectorAst[] simpleSelectors = compounds[
            compounds.Length - 1].GetSimpleSelectorsStorage();
        HtmlSelectorCandidateKind kind =
            HtmlSelectorCandidateKind.AllElements;
        string value = "";
        int index = 0;
        while (index < simpleSelectors.Length)
        {
            HtmlSimpleSelectorAst simple = simpleSelectors[index];
            if (simple.GetKind() == HtmlSelectorSimpleKind.Id)
            {
                kind = HtmlSelectorCandidateKind.Id;
                value = simple.GetName();
                return new HtmlSelectorCandidateHint(kind, value);
            }

            if (simple.GetKind() == HtmlSelectorSimpleKind.Class
                && kind != HtmlSelectorCandidateKind.Class)
            {
                kind = HtmlSelectorCandidateKind.Class;
                value = simple.GetName();
            }
            else if (simple.GetKind() == HtmlSelectorSimpleKind.Type
                && kind == HtmlSelectorCandidateKind.AllElements)
            {
                kind = HtmlSelectorCandidateKind.Type;
                HtmlSelectorQualifiedNameAst? name =
                    simple.GetQualifiedName();
                if (name == null)
                {
                    throw new InvalidOperationException(
                        "A type selector requires a qualified name.");
                }

                value = name.GetLocalName();
            }

            index = index + 1;
        }

        return new HtmlSelectorCandidateHint(kind, value);
    }
}
