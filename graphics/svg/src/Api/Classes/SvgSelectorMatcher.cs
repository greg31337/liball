namespace svg;

/// <summary>Calculates specificity and performs bounded right-to-left selector matching.</summary>
public class SvgSelectorMatcher : ISvgSelectorMatcher
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSelectorMatcher()
    {
    }

    /// <summary>Populates the maximum specificity fields of a selector list.</summary>
    /// <param name="selector">The parsed selector list.</param>
    public void CalculateSpecificity(SvgSelector selector)
    {
        if (selector == null) throw new System.ArgumentNullException("selector");
        SvgSelectorSpecificityCalculator.Populate(selector);
    }

    /// <summary>Matches a parsed selector list against one element.</summary>
    /// <param name="selector">The parsed selector list.</param>
    /// <param name="node">The candidate node.</param>
    /// <param name="options">The namespace, scope, state, and work options.</param>
    /// <returns>The bounded match result.</returns>
    public SvgSelectorMatchResult Match(SvgSelector? selector, SvgNode node, SvgSelectorMatchOptions options)
    {
        if (selector == null || node == null || !new SvgSelectorMatcher().ValidOptions(options))
        {
            System.Collections.Generic.List<SvgDiagnostic> invalid = new System.Collections.Generic.List<SvgDiagnostic>();
            SvgSourceSpan? source = selector == null ? null : selector.Source;
            invalid.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "Selector matching arguments are invalid.", source));
            return new SvgSelectorMatchResult(false, false, false, 0, 0, 0, 0, invalid);
        }
        SvgSelectorMatchContext context = new SvgSelectorMatchContext(options, options.Scope == null ? node : options.Scope);
        new SvgSelectorMatcher().AppendDiagnostics(context, selector.Diagnostics);
        bool matches = selector.IsValid && node.Kind == SvgNodeKind.Element && new SvgSelectorMatcher().MatchTopLevelSelectorList(selector, node, context);
        return new SvgSelectorMatchResult(matches && !context.LimitExceeded, !context.LimitExceeded && selector.IsValid, context.LimitExceeded, context.Work, context.MatchedIds, context.MatchedClasses, context.MatchedTypes, context.Diagnostics);
    }

    internal SvgSelectorQueryResult Query(SvgNode root, SvgSelector? selector, SvgSelectorMatchOptions options, bool firstOnly)
    {
        System.Collections.Generic.List<SvgNode> matches = new System.Collections.Generic.List<SvgNode>();
        if (root == null || selector == null || !new SvgSelectorMatcher().ValidOptions(options))
        {
            System.Collections.Generic.List<SvgDiagnostic> invalid = new System.Collections.Generic.List<SvgDiagnostic>();
            SvgSourceSpan? source = selector == null ? null : selector.Source;
            invalid.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "Selector query arguments are invalid.", source));
            return new SvgSelectorQueryResult(false, matches, false, 0, invalid);
        }
        SvgSelectorMatchContext context = new SvgSelectorMatchContext(options, options.Scope == null ? root : options.Scope);
        new SvgSelectorMatcher().AppendDiagnostics(context, selector.Diagnostics);
        if (!selector.IsValid) return new SvgSelectorQueryResult(false, matches, false, 0, context.Diagnostics);
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.HashSet<SvgNode> visited = new System.Collections.Generic.HashSet<SvgNode>();
        stack.Add(root);
        while (stack.Count > 0 && !context.LimitExceeded)
        {
            int last = stack.Count - 1;
            SvgNode node = stack[last];
            stack.RemoveAt(last);
            if (!visited.Add(node)) continue;
            if (!new SvgSelectorMatcher().Consume(context)) break;
            if (node.Kind == SvgNodeKind.Element && new SvgSelectorMatcher().MatchSelectorList(selector, node, context))
            {
                matches.Add(node);
                if (firstOnly) break;
            }
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0 && !context.LimitExceeded)
            {
                if (!new SvgSelectorMatcher().Consume(context)) break;
                stack.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        return new SvgSelectorQueryResult(!context.LimitExceeded, matches, context.LimitExceeded, context.Work, context.Diagnostics);
    }
bool ValidOptions(SvgSelectorMatchOptions options)
    {
        return options != null &&
            options.Namespaces != null &&
            options.Namespaces.DefaultNamespace != null &&
            options.Namespaces.NamedNamespaces != null &&
            options.DynamicState != null &&
            options.DynamicState.GetVersion() >= 0 &&
            options.DynamicState.GetLanguage() != null &&
            options.MaxWork > 0;
    }
bool MatchSelectorList(SvgSelector? selector, SvgNode node, SvgSelectorMatchContext context)
    {
        int index = 0;
        while (index < (selector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Items.Count && !context.LimitExceeded)
        {
            if (new SvgSelectorMatcher().MatchComplex(selector.Items[index], node, context)) return true;
            index = index + 1;
        }
        return false;
    }
bool MatchTopLevelSelectorList(SvgSelector selector, SvgNode node, SvgSelectorMatchContext context)
    {
        bool matched = false;
        int index = 0;
        while (index < selector.Items.Count && !context.LimitExceeded)
        {
            SvgComplexSelector item = selector.Items[index];
            if (new SvgSelectorMatcher().MatchComplex(item, node, context))
            {
                matched = true;
                if (new SvgSelectorMatcher().IsSpecificityGreater(item.IdSpecificity, item.ClassSpecificity, item.TypeSpecificity, context.MatchedIds, context.MatchedClasses, context.MatchedTypes))
                {
                    context.MatchedIds = item.IdSpecificity;
                    context.MatchedClasses = item.ClassSpecificity;
                    context.MatchedTypes = item.TypeSpecificity;
                }
            }
            index = index + 1;
        }
        return matched;
    }
bool IsSpecificityGreater(int ids, int classes, int types, int otherIds, int otherClasses, int otherTypes)
    {
        if (ids != otherIds) return ids > otherIds;
        if (classes != otherClasses) return classes > otherClasses;
        return types > otherTypes;
    }
bool MatchComplex(SvgComplexSelector selector, SvgNode node, SvgSelectorMatchContext context)
    {
        System.Collections.Generic.List<SvgSelectorMatchFrame> stack = new System.Collections.Generic.List<SvgSelectorMatchFrame>();
        System.Collections.Generic.Dictionary<SvgNode, System.Collections.Generic.HashSet<int>> visited = new System.Collections.Generic.Dictionary<SvgNode, System.Collections.Generic.HashSet<int>>();
        stack.Add(new SvgSelectorMatchFrame(selector.Compounds.Count - 1, node));
        while (stack.Count > 0 && !context.LimitExceeded)
        {
            int last = stack.Count - 1;
            SvgSelectorMatchFrame frame = stack[last];
            stack.RemoveAt(last);
            if (new SvgSelectorMatcher().WasVisited(visited, frame.Node, frame.CompoundIndex)) continue;
            if (!new SvgSelectorMatcher().Consume(context)) break;
            if (!new SvgSelectorMatcher().MatchCompound(selector.Compounds[frame.CompoundIndex], frame.Node, context)) continue;
            if (frame.CompoundIndex == 0) return true;
            SvgSelectorCombinator combinator = selector.Combinators[frame.CompoundIndex - 1];
            new SvgSelectorMatcher().AddLeftCandidates(stack, frame.CompoundIndex - 1, frame.Node, combinator, context);
        }
        return false;
    }
bool MatchCompound(SvgCompoundSelector compound, SvgNode? node, SvgSelectorMatchContext context)
    {
        if (node == null || node.Kind != SvgNodeKind.Element) return false;
        int index = 0;
        while (index < compound.SimpleSelectors.Count)
        {
            if (!new SvgSelectorMatcher().Consume(context)) return false;
            if (!new SvgSelectorMatcher().MatchSimple(compound.SimpleSelectors[index], node, context)) return false;
            index = index + 1;
        }
        return true;
    }
bool MatchSimple(SvgSimpleSelector selector, SvgNode node, SvgSelectorMatchContext context)
    {
        if (selector.Kind == SvgSimpleSelectorKind.Type || selector.Kind == SvgSimpleSelectorKind.Universal)
        {
            if (!new SvgSelectorMatcher().MatchesElementNamespace(selector, node, context)) return false;
            if (selector.Kind == SvgSimpleSelectorKind.Universal) return true;
            if (!new SvgSelectorMatcher().ConsumeAmount(context, selector.Name.Length + node.LocalName.Length)) return false;
            return selector.Name == node.LocalName;
        }
        if (selector.Kind == SvgSimpleSelectorKind.Id)
        {
            SvgAttribute? attribute = new SvgSelectorMatcher().FindAttribute(node, "", SvgAttributeNames.Id, false, context);
            if (attribute == null || !new SvgSelectorMatcher().ConsumeAmount(context, attribute.Value.Length + selector.Name.Length)) return false;
            return attribute.Value == selector.Name;
        }
        if (selector.Kind == SvgSimpleSelectorKind.Class)
        {
            SvgAttribute? attribute = new SvgSelectorMatcher().FindAttribute(node, "", SvgAttributeNames.Class, false, context);
            return attribute != null && new SvgSelectorMatcher().ContainsWhitespaceToken(attribute.Value, selector.Name, false, context);
        }
        if (selector.Kind == SvgSimpleSelectorKind.Attribute) return new SvgSelectorMatcher().MatchAttributeSelector(selector, node, context);
        if (selector.Kind == SvgSimpleSelectorKind.PseudoClass) return new SvgSelectorMatcher().MatchPseudo(selector, node, context);
        return false;
    }
bool MatchAttributeSelector(SvgSimpleSelector selector, SvgNode node, SvgSelectorMatchContext context)
    {
        string? namespaceUri = new SvgSelectorMatcher().NamespaceFor(selector, true, context);
        if (selector.NamespaceKind == SvgSelectorNamespaceKind.Named && namespaceUri == null) return false;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            if (!new SvgSelectorMatcher().ConsumeAmount(context, node.Attributes[index].LocalName.Length + node.Attributes[index].NamespaceUri.Length)) return false;
            SvgAttribute attribute = node.Attributes[index];
            bool namespaceMatch = selector.NamespaceKind == SvgSelectorNamespaceKind.Any || attribute.NamespaceUri == namespaceUri;
            if (namespaceMatch && attribute.LocalName == selector.Name && new SvgSelectorMatcher().AttributeValueMatches(selector, attribute.Value, context)) return true;
            index = index + 1;
        }
        return false;
    }
bool MatchPseudo(SvgSimpleSelector selector, SvgNode node, SvgSelectorMatchContext context)
    {
        SvgPseudoClassKind kind = selector.PseudoClass;
        if (kind == SvgPseudoClassKind.Root) return node.OwnerDocument != null && object.ReferenceEquals(node.OwnerDocument.DocumentElement, node);
        if (kind == SvgPseudoClassKind.Scope) return object.ReferenceEquals(context.Scope, node);
        if (kind == SvgPseudoClassKind.Empty) return new SvgSelectorMatcher().IsEmpty(node, context);
        if (kind == SvgPseudoClassKind.FirstChild) return new SvgSelectorMatcher().ElementPosition(node, false, false, null, context) == 1;
        if (kind == SvgPseudoClassKind.LastChild) return new SvgSelectorMatcher().ElementPosition(node, true, false, null, context) == 1;
        if (kind == SvgPseudoClassKind.OnlyChild) return new SvgSelectorMatcher().ElementPosition(node, false, false, null, context) == 1 && new SvgSelectorMatcher().ElementPosition(node, true, false, null, context) == 1;
        if (kind == SvgPseudoClassKind.FirstOfType) return new SvgSelectorMatcher().ElementPosition(node, false, true, null, context) == 1;
        if (kind == SvgPseudoClassKind.LastOfType) return new SvgSelectorMatcher().ElementPosition(node, true, true, null, context) == 1;
        if (kind == SvgPseudoClassKind.OnlyOfType) return new SvgSelectorMatcher().ElementPosition(node, false, true, null, context) == 1 && new SvgSelectorMatcher().ElementPosition(node, true, true, null, context) == 1;
        if (kind == SvgPseudoClassKind.NthChild || kind == SvgPseudoClassKind.NthLastChild || kind == SvgPseudoClassKind.NthOfType || kind == SvgPseudoClassKind.NthLastOfType)
        {
            bool reverse = kind == SvgPseudoClassKind.NthLastChild || kind == SvgPseudoClassKind.NthLastOfType;
            bool sameType = kind == SvgPseudoClassKind.NthOfType || kind == SvgPseudoClassKind.NthLastOfType;
            SvgSelector? filter = selector.Nth == null ? null : selector.Nth.OfSelector;
            int position = new SvgSelectorMatcher().ElementPosition(node, reverse, sameType, filter, context);
            return selector.Nth != null && position > 0 && new SvgSelectorMatcher().MatchesNth(position, selector.Nth.A, selector.Nth.B);
        }
        if (kind == SvgPseudoClassKind.Lang) return new SvgSelectorMatcher().MatchesLanguage(selector, node, context);
        if (kind == SvgPseudoClassKind.Is || kind == SvgPseudoClassKind.Where) return new SvgSelectorMatcher().MatchSelectorList(selector.SelectorArgument, node, context);
        if (kind == SvgPseudoClassKind.Not)
        {
            bool nested = new SvgSelectorMatcher().MatchSelectorList(selector.SelectorArgument, node, context);
            return !context.LimitExceeded && !nested;
        }
        if (kind == SvgPseudoClassKind.Has) return new SvgSelectorMatcher().MatchRelativeSelectorList(selector.SelectorArgument, node, context);
        if (kind == SvgPseudoClassKind.Link || kind == SvgPseudoClassKind.Visited || kind == SvgPseudoClassKind.Hover || kind == SvgPseudoClassKind.Active || kind == SvgPseudoClassKind.Focus || kind == SvgPseudoClassKind.Target || kind == SvgPseudoClassKind.UnsupportedDynamic)
        {
            SvgDynamicSelectorMatchResult dynamicResult = new SvgDynamicSelectorMatcher().Match(selector, node, context.Options.DynamicState);
            new SvgSelectorMatcher().AppendDiagnostics(context, dynamicResult.Diagnostics);
            return dynamicResult.Matches;
        }
        return false;
    }
bool MatchRelativeSelectorList(SvgSelector? selector, SvgNode anchor, SvgSelectorMatchContext context)
    {
        int index = 0;
        while (index < (selector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Items.Count && !context.LimitExceeded)
        {
            if (new SvgSelectorMatcher().MatchRelativeComplex(selector.Items[index], anchor, context)) return true;
            index = index + 1;
        }
        return false;
    }
bool MatchRelativeComplex(SvgComplexSelector selector, SvgNode anchor, SvgSelectorMatchContext context)
    {
        System.Collections.Generic.List<SvgNode> current = new SvgSelectorMatcher().ForwardCandidates(anchor, selector.LeadingCombinator, context);
        current = new SvgSelectorMatcher().FilterCompound(current, selector.Compounds[0], context);
        int compoundIndex = 1;
        while (compoundIndex < selector.Compounds.Count && current.Count > 0 && !context.LimitExceeded)
        {
            System.Collections.Generic.List<SvgNode> next = new System.Collections.Generic.List<SvgNode>();
            System.Collections.Generic.HashSet<SvgNode> unique = new System.Collections.Generic.HashSet<SvgNode>();
            int currentIndex = 0;
            while (currentIndex < current.Count && !context.LimitExceeded)
            {
                System.Collections.Generic.List<SvgNode> candidates = new SvgSelectorMatcher().ForwardCandidates(current[currentIndex], selector.Combinators[compoundIndex - 1], context);
                int candidateIndex = 0;
                while (candidateIndex < candidates.Count)
                {
                    if (unique.Add(candidates[candidateIndex])) next.Add(candidates[candidateIndex]);
                    candidateIndex = candidateIndex + 1;
                }
                currentIndex = currentIndex + 1;
            }
            current = new SvgSelectorMatcher().FilterCompound(next, selector.Compounds[compoundIndex], context);
            compoundIndex = compoundIndex + 1;
        }
        return !context.LimitExceeded && current.Count > 0;
    }
System.Collections.Generic.List<SvgNode> FilterCompound(System.Collections.Generic.List<SvgNode> candidates, SvgCompoundSelector compound, SvgSelectorMatchContext context)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < candidates.Count && !context.LimitExceeded)
        {
            if (new SvgSelectorMatcher().MatchCompound(compound, candidates[index], context)) result.Add(candidates[index]);
            index = index + 1;
        }
        return result;
    }
System.Collections.Generic.List<SvgNode> ForwardCandidates(SvgNode node, SvgSelectorCombinator combinator, SvgSelectorMatchContext context)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        if (combinator == SvgSelectorCombinator.Child)
        {
            int index = 0;
            while (index < node.Children.Count && !context.LimitExceeded)
            {
                if (!new SvgSelectorMatcher().Consume(context)) break;
                if (node.Children[index].Kind == SvgNodeKind.Element) result.Add(node.Children[index]);
                index = index + 1;
            }
            return result;
        }
        if (combinator == SvgSelectorCombinator.Descendant)
        {
            System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
            System.Collections.Generic.HashSet<SvgNode> visited = new System.Collections.Generic.HashSet<SvgNode>();
            int index = node.Children.Count - 1;
            while (index >= 0 && !context.LimitExceeded)
            {
                if (!new SvgSelectorMatcher().Consume(context)) break;
                stack.Add(node.Children[index]);
                index = index - 1;
            }
            while (stack.Count > 0 && !context.LimitExceeded)
            {
                int last = stack.Count - 1;
                SvgNode child = stack[last];
                stack.RemoveAt(last);
                if (!visited.Add(child)) continue;
                if (!new SvgSelectorMatcher().Consume(context)) break;
                if (child.Kind == SvgNodeKind.Element) result.Add(child);
                int childIndex = child.Children.Count - 1;
                while (childIndex >= 0 && !context.LimitExceeded)
                {
                    if (!new SvgSelectorMatcher().Consume(context)) break;
                    stack.Add(child.Children[childIndex]);
                    childIndex = childIndex - 1;
                }
            }
            return result;
        }
        SvgNode? parent = node.Parent;
        if (parent == null) return result;
        int nodeIndex = new SvgSelectorMatcher().ChildIndex(parent, node, context);
        if (nodeIndex < 0) return result;
        int siblingIndex = nodeIndex + 1;
        while (siblingIndex < parent.Children.Count && !context.LimitExceeded)
        {
            if (!new SvgSelectorMatcher().Consume(context)) break;
            SvgNode sibling = parent.Children[siblingIndex];
            if (sibling.Kind == SvgNodeKind.Element)
            {
                result.Add(sibling);
                if (combinator == SvgSelectorCombinator.AdjacentSibling) break;
            }
            siblingIndex = siblingIndex + 1;
        }
        return result;
    }
void AddLeftCandidates(System.Collections.Generic.List<SvgSelectorMatchFrame> stack, int compoundIndex, SvgNode node, SvgSelectorCombinator combinator, SvgSelectorMatchContext context)
    {
        if (combinator == SvgSelectorCombinator.Child)
        {
            SvgNode? parent = new SvgSelectorMatcher().ParentElement(node);
            if (parent != null) stack.Add(new SvgSelectorMatchFrame(compoundIndex, parent));
            return;
        }
        if (combinator == SvgSelectorCombinator.Descendant)
        {
            SvgNode? ancestor = new SvgSelectorMatcher().ParentElement(node);
            while (ancestor != null && !context.LimitExceeded)
            {
                if (!new SvgSelectorMatcher().Consume(context)) break;
                stack.Add(new SvgSelectorMatchFrame(compoundIndex, ancestor));
                ancestor = new SvgSelectorMatcher().ParentElement(ancestor);
            }
            return;
        }
        SvgNode? parentNode = node.Parent;
        if (parentNode == null) return;
        int nodeIndex = new SvgSelectorMatcher().ChildIndex(parentNode, node, context);
        if (nodeIndex < 0) return;
        int siblingIndex = nodeIndex - 1;
        while (siblingIndex >= 0 && !context.LimitExceeded)
        {
            if (!new SvgSelectorMatcher().Consume(context)) break;
            SvgNode sibling = parentNode.Children[siblingIndex];
            if (sibling.Kind == SvgNodeKind.Element)
            {
                stack.Add(new SvgSelectorMatchFrame(compoundIndex, sibling));
                if (combinator == SvgSelectorCombinator.AdjacentSibling) break;
            }
            siblingIndex = siblingIndex - 1;
        }
    }
int ElementPosition(SvgNode node, bool reverse, bool sameType, SvgSelector? filter, SvgSelectorMatchContext context)
    {
        SvgNode? parent = node.Parent;
        if (parent == null) return -1;
        int position = 0;
        int index = reverse ? parent.Children.Count - 1 : 0;
        while (reverse ? index >= 0 : index < parent.Children.Count)
        {
            if (!new SvgSelectorMatcher().Consume(context)) return -1;
            SvgNode sibling = parent.Children[index];
            bool typeMatch = !sameType || new SvgSelectorMatcher().SameType(node, sibling, context);
            if (context.LimitExceeded) return -1;
            if (sibling.Kind == SvgNodeKind.Element && typeMatch)
            {
                bool included = filter == null || new SvgSelectorMatcher().MatchSelectorList(filter, sibling, context);
                if (context.LimitExceeded) return -1;
                if (included)
                {
                    position = position + 1;
                    if (object.ReferenceEquals(sibling, node)) return position;
                }
            }
            index = reverse ? index - 1 : index + 1;
        }
        return -1;
    }
bool MatchesNth(int position, int a, int b)
    {
        if (a == 0) return position == b;
        long difference = (long)position - b;
        if (difference % a != 0) return false;
        return difference / a >= 0;
    }
bool MatchesLanguage(SvgSimpleSelector selector, SvgNode node, SvgSelectorMatchContext context)
    {
        string language = new SvgSelectorMatcher().LookupLanguage(node, context);
        if (language.Length == 0) return false;
        int index = 0;
        while (index < selector.Languages.Count)
        {
            string range = selector.Languages[index];
            if (range == "*") return true;
            if (!new SvgSelectorMatcher().ConsumeAmount(context, language.Length + range.Length)) return false;
            if (new SvgSelectorMatcher().AsciiEquals(language, range)) return true;
            if (language.Length > range.Length && language[range.Length] == '-' && new SvgSelectorMatcher().AsciiStartsWith(language, range)) return true;
            index = index + 1;
        }
        return false;
    }
string LookupLanguage(SvgNode node, SvgSelectorMatchContext context)
    {
        SvgNode? current = node;
        while (current != null)
        {
            if (current.Kind == SvgNodeKind.Element)
            {
                SvgAttribute? attribute = new SvgSelectorMatcher().FindAttribute(current, SvgNamespaces.Xml, SvgAttributeNames.Lang, false, context);
                if (attribute != null) return attribute.Value;
            }
            current = current.Parent;
        }
        return context.Options.DynamicState.GetLanguage();
    }
bool IsEmpty(SvgNode node, SvgSelectorMatchContext context)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            if (!new SvgSelectorMatcher().Consume(context)) return false;
            SvgNode child = node.Children[index];
            if (child.Kind == SvgNodeKind.Element) return false;
            if ((child.Kind == SvgNodeKind.Text || child.Kind == SvgNodeKind.CData) && child.Value.Length > 0) return false;
            index = index + 1;
        }
        return true;
    }
bool MatchesElementNamespace(SvgSimpleSelector selector, SvgNode node, SvgSelectorMatchContext context)
    {
        if (selector.NamespaceKind == SvgSelectorNamespaceKind.Any) return true;
        string? namespaceUri = new SvgSelectorMatcher().NamespaceFor(selector, false, context);
        if (selector.NamespaceKind == SvgSelectorNamespaceKind.Named && namespaceUri == null) return false;
        if (selector.NamespaceKind == SvgSelectorNamespaceKind.Default && !context.Options.Namespaces.HasDefaultNamespace) return true;
        if (!new SvgSelectorMatcher().ConsumeAmount(context, node.NamespaceUri.Length + (namespaceUri ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length)) return false;
        return node.NamespaceUri == namespaceUri;
    }
string? NamespaceFor(SvgSimpleSelector selector, bool attribute, SvgSelectorMatchContext context)
    {
        if (selector.NamespaceKind == SvgSelectorNamespaceKind.None) return "";
        if (selector.NamespaceKind == SvgSelectorNamespaceKind.Default)
        {
            if (attribute) return "";
            return context.Options.Namespaces.HasDefaultNamespace ? context.Options.Namespaces.DefaultNamespace : "";
        }
        if (selector.NamespaceKind == SvgSelectorNamespaceKind.Any) return "";
        if (context.Options.Namespaces.NamedNamespaces.ContainsKey(selector.NamespacePrefix)) return context.Options.Namespaces.NamedNamespaces[selector.NamespacePrefix];
        new SvgSelectorMatcher().AddDiagnostic(context, new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidCss, "The selector namespace prefix " + selector.NamespacePrefix + " is not bound.", selector.Source));
        return null;
    }
bool AttributeValueMatches(SvgSimpleSelector selector, string actual, SvgSelectorMatchContext context)
    {
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.Exists) return true;
        bool insensitive = selector.AttributeCase == SvgAttributeSelectorCase.Insensitive;
        long cost = actual.Length + selector.Value.Length;
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.SubstringMatch && selector.Value.Length > 0)
        {
            cost = actual.Length > long.MaxValue / selector.Value.Length ? long.MaxValue : (long)actual.Length * selector.Value.Length;
        }
        if (!new SvgSelectorMatcher().ConsumeAmount(context, cost)) return false;
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.Equals) return new SvgSelectorMatcher().TextEquals(actual, selector.Value, insensitive);
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.Includes) return selector.Value.Length > 0 && new SvgSelectorMatcher().ContainsWhitespaceTokenUncharged(actual, selector.Value, insensitive);
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.DashMatch) return new SvgSelectorMatcher().TextEquals(actual, selector.Value, insensitive) || actual.Length > selector.Value.Length && actual[selector.Value.Length] == '-' && new SvgSelectorMatcher().TextStartsWith(actual, selector.Value, insensitive);
        if (selector.Value.Length == 0) return false;
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.PrefixMatch) return new SvgSelectorMatcher().TextStartsWith(actual, selector.Value, insensitive);
        if (selector.AttributeOperator == SvgAttributeSelectorOperator.SuffixMatch) return new SvgSelectorMatcher().TextEndsWith(actual, selector.Value, insensitive);
        return new SvgSelectorMatcher().TextContains(actual, selector.Value, insensitive);
    }
SvgAttribute? FindAttribute(SvgNode node, string namespaceUri, string localName, bool anyNamespace, SvgSelectorMatchContext context)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (!new SvgSelectorMatcher().ConsumeAmount(context, attribute.LocalName.Length + attribute.NamespaceUri.Length)) return null;
            if ((anyNamespace || attribute.NamespaceUri == namespaceUri) && attribute.LocalName == localName) return attribute;
            index = index + 1;
        }
        return null;
    }
bool ContainsWhitespaceToken(string value, string token, bool insensitive, SvgSelectorMatchContext context)
    {
        if (!new SvgSelectorMatcher().ConsumeAmount(context, value.Length + token.Length)) return false;
        return new SvgSelectorMatcher().ContainsWhitespaceTokenUncharged(value, token, insensitive);
    }
bool ContainsWhitespaceTokenUncharged(string value, string token, bool insensitive)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && new SvgSelectorMatcher().IsWhitespace(value[index])) index = index + 1;
            int start = index;
            while (index < value.Length && !new SvgSelectorMatcher().IsWhitespace(value[index])) index = index + 1;
            if (index > start && new SvgSelectorMatcher().TextEquals(value.Substring(start, index - start), token, insensitive)) return true;
        }
        return false;
    }
bool TextEquals(string left, string right, bool insensitive)
    {
        if (left.Length != right.Length) return false;
        int index = 0;
        while (index < left.Length)
        {
            if (new SvgSelectorMatcher().Fold(left[index], insensitive) != new SvgSelectorMatcher().Fold(right[index], insensitive)) return false;
            index = index + 1;
        }
        return true;
    }
bool TextStartsWith(string value, string prefix, bool insensitive)
    {
        if (prefix.Length > value.Length) return false;
        int index = 0;
        while (index < prefix.Length)
        {
            if (new SvgSelectorMatcher().Fold(value[index], insensitive) != new SvgSelectorMatcher().Fold(prefix[index], insensitive)) return false;
            index = index + 1;
        }
        return true;
    }
bool TextEndsWith(string value, string suffix, bool insensitive)
    {
        if (suffix.Length > value.Length) return false;
        int offset = value.Length - suffix.Length;
        int index = 0;
        while (index < suffix.Length)
        {
            if (new SvgSelectorMatcher().Fold(value[offset + index], insensitive) != new SvgSelectorMatcher().Fold(suffix[index], insensitive)) return false;
            index = index + 1;
        }
        return true;
    }
bool TextContains(string value, string part, bool insensitive)
    {
        int index = 0;
        while (index + part.Length <= value.Length)
        {
            int partIndex = 0;
            while (partIndex < part.Length && new SvgSelectorMatcher().Fold(value[index + partIndex], insensitive) == new SvgSelectorMatcher().Fold(part[partIndex], insensitive)) partIndex = partIndex + 1;
            if (partIndex == part.Length) return true;
            index = index + 1;
        }
        return false;
    }
char Fold(char value, bool insensitive)
    {
        if (insensitive && value >= 'A' && value <= 'Z') return (char)(value + 32);
        return value;
    }
bool AsciiEquals(string left, string right)
    {
        return new SvgSelectorMatcher().TextEquals(left, right, true);
    }
bool AsciiStartsWith(string value, string prefix)
    {
        return new SvgSelectorMatcher().TextStartsWith(value, prefix, true);
    }
bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\n' || value == '\r' || value == '\f';
    }
bool SameType(SvgNode left, SvgNode right, SvgSelectorMatchContext context)
    {
        if (!new SvgSelectorMatcher().ConsumeAmount(context, left.NamespaceUri.Length + right.NamespaceUri.Length + left.LocalName.Length + right.LocalName.Length)) return false;
        return left.NamespaceUri == right.NamespaceUri && left.LocalName == right.LocalName;
    }
SvgNode? ParentElement(SvgNode node)
    {
        return node.Parent != null && node.Parent.Kind == SvgNodeKind.Element ? node.Parent : null;
    }
int ChildIndex(SvgNode parent, SvgNode child, SvgSelectorMatchContext context)
    {
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (!new SvgSelectorMatcher().Consume(context)) return -1;
            if (object.ReferenceEquals(parent.Children[index], child)) return index;
            index = index + 1;
        }
        return -1;
    }
bool WasVisited(System.Collections.Generic.Dictionary<SvgNode, System.Collections.Generic.HashSet<int>> visited, SvgNode node, int compoundIndex)
    {
        if (!visited.ContainsKey(node)) visited[node] = new System.Collections.Generic.HashSet<int>();
        return !visited[node].Add(compoundIndex);
    }
bool Consume(SvgSelectorMatchContext context)
    {
        return new SvgSelectorMatcher().ConsumeAmount(context, 1);
    }
bool ConsumeAmount(SvgSelectorMatchContext context, long amount)
    {
        if (context.LimitExceeded) return false;
        if (amount < 1) amount = 1;
        if (amount > context.Options.MaxWork - context.Work)
        {
            context.Work = context.Options.MaxWork;
            context.LimitExceeded = true;
            new SvgSelectorMatcher().AddDiagnostic(context, new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, "The configured selector matching work limit was exceeded.", null));
            return false;
        }
        context.Work = context.Work + amount;
        return true;
    }
void AppendDiagnostics(SvgSelectorMatchContext context, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            new SvgSelectorMatcher().AddDiagnostic(context, diagnostics[index]);
            index = index + 1;
        }
    }
void AddDiagnostic(SvgSelectorMatchContext context, SvgDiagnostic diagnostic)
    {
        int index = 0;
        while (index < context.Diagnostics.Count)
        {
            SvgDiagnostic current = context.Diagnostics[index];
            if (current.Code == diagnostic.Code && current.Message == diagnostic.Message && object.ReferenceEquals(current.Source, diagnostic.Source)) return;
            index = index + 1;
        }
        context.Diagnostics.Add(diagnostic);
    }
}
