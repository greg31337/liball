namespace svg;

internal static class SvgAccessibilityValidator
{
    internal static void Validate(SvgValidationWork work)
    {
        System.Collections.Generic.List<SvgNode> nodes = work.ReferenceNodes;
        if (nodes.Count == 0) nodes = CollectNodes((work.Result.Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DocumentElement);
        SvgAccessibilityValidationState state = new SvgAccessibilityValidationState(work);
        int index = 0;
        while (index < nodes.Count && work.Result.Error == SvgValidationError.None)
        {
            ValidateNode(nodes[index], state);
            index = index + 1;
        }
    }

    static void ValidateNode(SvgNode node, SvgAccessibilityValidationState state)
    {
        ValidateViewBoxRisk(node, state);
        state.Reset();
        SvgAccessibilityNameResult name = AccessibleName(node, state);
        if (name.FromTitle)
        {
            Add(state, SvgValidationRule.TitlePreservationRisk, SvgDiagnosticSeverity.Info, SvgDiagnosticCode.PreservedFeature, "A direct title element supplies this element's accessible name.", node, null, (name.Title ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan, "Preserve the title unless an equivalent accessible name is supplied first.");
        }
        string role = EffectiveRole(node);
        bool focusable = Focusable(node);
        ValidateContradictions(node, role, focusable, state);
        if (Hidden(node)) return;
        if (NameRelevant(node, role, focusable) && name.Value.Length == 0)
        {
            Add(state, SvgValidationRule.MissingAccessibleName, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "The accessible element has no nonempty accessible name.", node, null, node.SourceSpan, "Supply aria-labelledby, aria-label, a direct title, or meaningful text where the role permits name-from-content.");
        }
        string description = AccessibleDescription(node, state);
        if (DescriptionRelevant(node, role, name.Value) && description.Length == 0)
        {
            SvgAttribute? described = Attribute(node, "aria-describedby");
            SvgSourceSpan? source = described == null ? node.SourceSpan : described.SourceSpan;
            Add(state, SvgValidationRule.MissingAccessibleDescription, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "The element has no nonempty accessible description.", node, described, source, "Supply aria-describedby or a direct desc element with meaningful text.");
        }
        ValidateKeyboardLink(node, role, state);
    }

    static void ValidateViewBoxRisk(SvgNode node, SvgAccessibilityValidationState state)
    {
        SvgAttribute? viewBox = Attribute(node, SvgAttributeNames.ViewBox);
        if (viewBox == null || !ViewBoxElement(node.ElementKind)) return;
        SvgViewBoxParseResult parsed = SvgViewBoxParser.ParseViewBox(viewBox.Value, (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TypedValues.MaxTokenCharacters);
        if (!parsed.Success) return;
        Add(state, SvgValidationRule.ViewBoxPreservationRisk, SvgDiagnosticSeverity.Info, SvgDiagnosticCode.PreservedFeature, "The authored viewBox supplies scalable coordinate and aspect-ratio semantics.", node, viewBox, viewBox.SourceSpan, "Preserve the viewBox and its relationship with preserveAspectRatio during processing.");
    }

    static void ValidateContradictions(SvgNode node, string role, bool focusable, SvgAccessibilityValidationState state)
    {
        SvgAttribute? roleAttribute = Attribute(node, "role");
        if (role == "presentation" && (focusable || HasSemanticAria(node)))
        {
            Add(state, SvgValidationRule.AriaContradiction, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "A presentation role conflicts with focusability or authored ARIA semantics.", node, roleAttribute, roleAttribute == null ? node.SourceSpan : roleAttribute.SourceSpan, "Remove the presentation role or remove the conflicting interactive semantics.");
        }
        SvgAttribute? hidden = Attribute(node, "aria-hidden");
        if (hidden != null && string.Equals(hidden.Value.Trim(), "true", System.StringComparison.OrdinalIgnoreCase) && focusable)
        {
            Add(state, SvgValidationRule.AriaContradiction, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "aria-hidden=true conflicts with a focusable element.", node, hidden, hidden.SourceSpan, "Keep focusable content in the accessibility tree or remove its focusability.");
        }
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && ContradictoryState(attribute.LocalName, role))
            {
                Add(state, SvgValidationRule.AriaContradiction, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "The " + attribute.LocalName + " state is incompatible with role '" + role + "'.", node, attribute, attribute.SourceSpan, "Remove the state or use a role that supports it.");
            }
            index = index + 1;
        }
        ValidateRange(node, role, state);
    }

    static void ValidateRange(SvgNode node, string role, SvgAccessibilityValidationState state)
    {
        if (!RangeRole(role)) return;
        SvgAttribute? minimum = Attribute(node, "aria-valuemin");
        SvgAttribute? maximum = Attribute(node, "aria-valuemax");
        SvgAttribute? current = Attribute(node, "aria-valuenow");
        SvgNumberParseResult? min = minimum == null ? null : SvgNumberParser.ParseNumber(minimum.Value, 128);
        SvgNumberParseResult? max = maximum == null ? null : SvgNumberParser.ParseNumber(maximum.Value, 128);
        SvgNumberParseResult? now = current == null ? null : SvgNumberParser.ParseNumber(current.Value, 128);
        if (min != null && max != null && min.Success && max.Success && min.Value > max.Value)
        {
            Add(state, SvgValidationRule.AriaContradiction, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "aria-valuemin is greater than aria-valuemax.", node, maximum, (maximum ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan, "Author an ordered minimum and maximum range.");
            return;
        }
        if (now != null && now.Success && (min != null && min.Success && now.Value < min.Value || max != null && max.Success && now.Value > max.Value))
        {
            Add(state, SvgValidationRule.AriaContradiction, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "aria-valuenow lies outside the authored ARIA value range.", node, current, (current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan, "Keep aria-valuenow within aria-valuemin and aria-valuemax.");
        }
    }

    static void ValidateKeyboardLink(SvgNode node, string role, SvgAccessibilityValidationState state)
    {
        if (role != "link") return;
        SvgAttribute? tab = Attribute(node, SvgAttributeNames.TabIndex);
        SvgIntegerParseResult? parsed = tab == null ? null : SvgNumberParser.ParseInteger(tab.Value, 64);
        bool nativeLink = node.ElementKind == SvgElementKind.A && HasUsableHref(node);
        bool sequential = nativeLink && tab == null || parsed != null && parsed.Success && parsed.Value >= 0;
        if (sequential) return;
        SvgAttribute? source = tab == null ? Attribute(node, "role") : tab;
        Add(state, SvgValidationRule.KeyboardAccessibility, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "The link is not reachable through sequential keyboard navigation.", node, source, source == null ? node.SourceSpan : source.SourceSpan, "Use a native link with href or provide a nonnegative tabindex and keyboard activation behavior.");
    }

    static SvgAccessibilityNameResult AccessibleName(SvgNode node, SvgAccessibilityValidationState state)
    {
        SvgAccessibilityNameResult result = new SvgAccessibilityNameResult();
        SvgAttribute? labelled = Attribute(node, "aria-labelledby");
        if (labelled != null)
        {
            result.Value = RelationshipText(node, labelled, true, state, new System.Collections.Generic.HashSet<SvgNode>());
            if (result.Value.Length != 0) return result;
        }
        SvgAttribute? label = Attribute(node, "aria-label");
        if (label != null)
        {
            result.Value = Normalize(label.Value);
            if (result.Value.Length != 0) return result;
        }
        SvgNode? title = DirectChild(node, SvgElementKind.Title);
        if (title != null)
        {
            result.Value = Text(title, state);
            if (result.Value.Length != 0)
            {
                result.FromTitle = true;
                result.Title = title;
                return result;
            }
        }
        string role = EffectiveRole(node);
        if (NameFromContent(role)) result.Value = TextWithoutDescriptions(node, state);
        return result;
    }

    static string AccessibleDescription(SvgNode node, SvgAccessibilityValidationState state)
    {
        SvgAttribute? described = Attribute(node, "aria-describedby");
        if (described != null)
        {
            string value = RelationshipText(node, described, false, state, new System.Collections.Generic.HashSet<SvgNode>());
            if (value.Length != 0) return value;
        }
        SvgNode? description = DirectChild(node, SvgElementKind.Desc);
        return description == null ? "" : Text(description, state);
    }

    static string RelationshipText(SvgNode owner, SvgAttribute attribute, bool naming, SvgAccessibilityValidationState state, System.Collections.Generic.HashSet<SvgNode> active)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int position = 0;
        while (position < attribute.Value.Length && state.References < (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Accessibility.MaximumReferences)
        {
            while (position < attribute.Value.Length && System.Char.IsWhiteSpace(attribute.Value[position])) position = position + 1;
            if (position >= attribute.Value.Length) break;
            int start = position;
            while (position < attribute.Value.Length && !System.Char.IsWhiteSpace(attribute.Value[position])) position = position + 1;
            state.References = state.References + 1;
            string id = attribute.Value.Substring(start, position - start);
            SvgNode? referenced = owner.OwnerDocument == null ? null : owner.OwnerDocument.Index.GetById(id);
            string text = referenced == null ? "" : naming ? ReferencedName(referenced, state, active) : Text(referenced, state);
            if (text.Length != 0)
            {
                if (result.Length != 0) result.Append(' ');
                result.Append(text);
            }
        }
        return Normalize(result.ToString());
    }

    static string ReferencedName(SvgNode node, SvgAccessibilityValidationState state, System.Collections.Generic.HashSet<SvgNode> active)
    {
        if (!active.Add(node)) return "";
        string result = "";
        SvgAttribute? labelled = Attribute(node, "aria-labelledby");
        if (labelled != null) result = RelationshipText(node, labelled, true, state, active);
        if (result.Length == 0)
        {
            SvgAttribute? label = Attribute(node, "aria-label");
            if (label != null) result = Normalize(label.Value);
        }
        if (result.Length == 0)
        {
            SvgNode? title = DirectChild(node, SvgElementKind.Title);
            if (title != null) result = Text(title, state);
        }
        if (result.Length == 0) result = Text(node, state);
        active.Remove(node);
        return result;
    }

    static string Text(SvgNode node, SvgAccessibilityValidationState state)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        stack.Add(node);
        while (stack.Count != 0 && state.Nodes < (state.Work.Options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Accessibility.MaximumNodes && state.Characters < state.Work.Options.Accessibility.MaximumTextCharacters)
        {
            SvgNode current = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            state.Nodes = state.Nodes + 1;
            if (current.Kind == SvgNodeKind.Text || current.Kind == SvgNodeKind.CData)
            {
                int available = state.Work.Options.Accessibility.MaximumTextCharacters - state.Characters;
                int length = current.Value.Length < available ? current.Value.Length : available;
                if (length > 0) result.Append(current.Value.Substring(0, length));
                state.Characters = state.Characters + length;
            }
            int index = current.Children.Count - 1;
            while (index >= 0)
            {
                stack.Add(current.Children[index]);
                index = index - 1;
            }
        }
        return Normalize(result.ToString());
    }

    static string TextWithoutDescriptions(SvgNode node, SvgAccessibilityValidationState state)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < node.Children.Count)
        {
            SvgNode child = node.Children[index];
            if (child.ElementKind != SvgElementKind.Title && child.ElementKind != SvgElementKind.Desc)
            {
                string value = Text(child, state);
                if (value.Length != 0)
                {
                    if (result.Length != 0) result.Append(' ');
                    result.Append(value);
                }
            }
            index = index + 1;
        }
        return Normalize(result.ToString());
    }

    static bool NameRelevant(SvgNode node, string role, bool focusable)
    {
        if (role == "presentation" && !focusable && !HasSemanticAria(node)) return false;
        if (node.ElementKind == SvgElementKind.Image || node.ElementKind == SvgElementKind.A && HasUsableHref(node) || focusable) return true;
        SvgAttribute? authoredRole = Attribute(node, "role");
        return authoredRole != null && RequiredNameRole(role);
    }

    static bool DescriptionRelevant(SvgNode node, string role, string name)
    {
        if (Attribute(node, "aria-describedby") != null) return true;
        if (name.Length == 0 || Attribute(node, "role") == null) return false;
        return role == "graphics-document" || role == "document" || role == "application";
    }

    static bool RequiredNameRole(string role)
    {
        switch (role)
        {
            case "application": case "button": case "checkbox": case "figure": case "graphics-document": case "graphics-object": case "graphics-symbol": case "heading": case "img": case "link": case "meter": case "progressbar": case "radio": case "region": case "scrollbar": case "slider": case "spinbutton": case "switch": case "textbox": return true;
            default: return false;
        }
    }

    static bool NameFromContent(string role)
    {
        return role == "button" || role == "heading" || role == "link" || role == "listitem" || role == "text";
    }

    static bool ContradictoryState(string name, string role)
    {
        if (name == "aria-checked") return role != "checkbox" && role != "radio" && role != "switch";
        if (name == "aria-pressed") return role != "button";
        if (name == "aria-level") return role != "heading";
        if (name == "aria-valuemin" || name == "aria-valuemax" || name == "aria-valuenow") return !RangeRole(role);
        return false;
    }

    static bool RangeRole(string role)
    {
        return role == "meter" || role == "progressbar" || role == "scrollbar" || role == "slider" || role == "spinbutton";
    }

    static bool Focusable(SvgNode node)
    {
        SvgAttribute? tab = Attribute(node, SvgAttributeNames.TabIndex);
        if (tab != null && SvgNumberParser.ParseInteger(tab.Value, 64).Success) return true;
        return node.ElementKind == SvgElementKind.A && HasUsableHref(node);
    }

    static bool Hidden(SvgNode node)
    {
        SvgNode? current = node;
        while (current != null)
        {
            SvgAttribute? hidden = Attribute(current, "aria-hidden");
            if (hidden != null && string.Equals(hidden.Value.Trim(), "true", System.StringComparison.OrdinalIgnoreCase)) return true;
            current = current.Parent;
        }
        return false;
    }

    static string EffectiveRole(SvgNode node)
    {
        SvgAttribute? role = Attribute(node, "role");
        if (role != null)
        {
            string explicitRole = FirstKnownRole(role.Value);
            if (explicitRole.Length != 0) return explicitRole == "none" ? "presentation" : explicitRole;
        }
        if (node.ElementKind == SvgElementKind.A && HasUsableHref(node)) return "link";
        if (node.ElementKind == SvgElementKind.Svg) return node.Parent == null || node.Parent.Kind == SvgNodeKind.Document ? "graphics-document" : "group";
        if (node.ElementKind == SvgElementKind.Image) return "img";
        if (node.ElementKind == SvgElementKind.Text || node.ElementKind == SvgElementKind.TSpan || node.ElementKind == SvgElementKind.TextPath) return "text";
        if (node.ElementKind == SvgElementKind.G || node.ElementKind == SvgElementKind.Switch || node.ElementKind == SvgElementKind.Use) return "group";
        if (Shape(node.ElementKind)) return "graphics-symbol";
        return "";
    }

    static string FirstKnownRole(string value)
    {
        int position = 0;
        while (position < value.Length)
        {
            while (position < value.Length && System.Char.IsWhiteSpace(value[position])) position = position + 1;
            int start = position;
            while (position < value.Length && !System.Char.IsWhiteSpace(value[position])) position = position + 1;
            if (position > start)
            {
                string role = value.Substring(start, position - start).ToLowerInvariant();
                if (KnownRole(role)) return role;
            }
        }
        return "";
    }

    static bool KnownRole(string role)
    {
        switch (role)
        {
            case "alert": case "application": case "button": case "checkbox": case "document": case "figure": case "graphics-document": case "graphics-object": case "graphics-symbol": case "group": case "heading": case "img": case "link": case "list": case "listitem": case "meter": case "none": case "presentation": case "progressbar": case "radio": case "region": case "scrollbar": case "separator": case "slider": case "spinbutton": case "status": case "switch": case "text": case "textbox": case "timer": return true;
            default: return false;
        }
    }

    static bool HasSemanticAria(SvgNode node)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName.StartsWith("aria-", System.StringComparison.Ordinal) && attribute.LocalName != "aria-hidden") return true;
            index = index + 1;
        }
        return false;
    }

    static bool HasUsableHref(SvgNode node)
    {
        SvgAttribute? href = Attribute(node, SvgAttributeNames.Href);
        if (href == null) href = Attribute(node, SvgNamespaces.XLink, SvgAttributeNames.Href);
        return href != null && href.Value.Trim().Length != 0;
    }

    static bool ViewBoxElement(SvgElementKind kind)
    {
        return kind == SvgElementKind.Svg || kind == SvgElementKind.Symbol || kind == SvgElementKind.View || kind == SvgElementKind.Marker || kind == SvgElementKind.Pattern;
    }

    static bool Shape(SvgElementKind kind)
    {
        return kind == SvgElementKind.Circle || kind == SvgElementKind.Ellipse || kind == SvgElementKind.Line || kind == SvgElementKind.Path || kind == SvgElementKind.Polygon || kind == SvgElementKind.Polyline || kind == SvgElementKind.Rect;
    }

    static SvgNode? DirectChild(SvgNode node, SvgElementKind kind)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            SvgNode child = node.Children[index];
            if (child.Kind == SvgNodeKind.Element && child.ElementKind == kind) return child;
            index = index + 1;
        }
        return null;
    }

    static SvgAttribute? Attribute(SvgNode node, string name)
    {
        return Attribute(node, "", name);
    }

    static SvgAttribute? Attribute(SvgNode? node, string namespaceUri, string name)
    {
        if (node == null) return null;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri == namespaceUri && attribute.LocalName == name) return attribute;
            index = index + 1;
        }
        return null;
    }

    static string Normalize(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        bool space = false;
        int index = 0;
        while (index < value.Length)
        {
            if (System.Char.IsWhiteSpace(value[index])) space = result.Length != 0;
            else
            {
                if (space) result.Append(' ');
                result.Append(value[index]);
                space = false;
            }
            index = index + 1;
        }
        return result.ToString();
    }

    static System.Collections.Generic.List<SvgNode> CollectNodes(SvgNode? root)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        if (root == null) return result;
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        stack.Add(root);
        while (stack.Count != 0)
        {
            SvgNode node = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (node.Kind == SvgNodeKind.Element) result.Add(node);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                stack.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }

    static void Add(SvgAccessibilityValidationState state, SvgValidationRule rule, SvgDiagnosticSeverity severity, SvgDiagnosticCode code, string message, SvgNode node, SvgAttribute? attribute, SvgSourceSpan? source, string recovery)
    {
        new SvgValidator().AddLocated(state.Work, rule, severity, code, message, node, attribute, source, recovery);
    }
}

internal class SvgAccessibilityNameResult
{
    internal string Value;
    internal bool FromTitle;
    internal SvgNode? Title;

    internal SvgAccessibilityNameResult()
    {
        Value = "";
        FromTitle = false;
        Title = null;
    }
}

internal class SvgAccessibilityValidationState
{
    internal SvgValidationWork Work;
    internal int Nodes;
    internal int Characters;
    internal int References;

    internal SvgAccessibilityValidationState(SvgValidationWork work)
    {
        Work = work;
        Reset();
    }

    internal void Reset()
    {
        Nodes = 0;
        Characters = 0;
        References = 0;
    }
}
