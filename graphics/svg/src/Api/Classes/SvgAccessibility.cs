namespace svg;

/// <summary>Calculates bounded SVG accessible names, descriptions, roles, and ARIA mappings.</summary>
public class SvgAccessibility : ISvgAccessibility
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgAccessibility()
    {
    }

    /// <summary>Calculates accessible semantics with secure defaults.</summary>
    /// <param name="node">The current resolved source or instance node.</param>
    /// <returns>The accessible semantic snapshot.</returns>
    public SvgAccessibleSemantics ResolveWithNode(SvgResolvedNode node)
    {
        return new SvgAccessibility().ResolveWithNodeAndOptions(node, new SvgAccessibilityOptions());
    }

    /// <summary>Calculates accessible semantics with explicit bounds.</summary>
    /// <param name="node">The current resolved source or instance node.</param>
    /// <param name="options">The text and relationship bounds.</param>
    /// <returns>The accessible semantic snapshot.</returns>
    public SvgAccessibleSemantics ResolveWithNodeAndOptions(SvgResolvedNode? node, SvgAccessibilityOptions options)
    {
        SvgAccessibleSemantics result = new SvgAccessibleSemantics(node);
        if (node == null || node.SourceNode == null || node.SourceNode.Kind != SvgNodeKind.Element || options == null || options.MaximumNodes < 1 || options.MaximumTextCharacters < 1 || options.MaximumReferences < 1)
        {
            new SvgAccessibility().Add(result, SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "Accessibility calculation arguments are invalid.", node == null ? null : node.SourceNode);
            return result;
        }
        SvgAccessibilityWork work = new SvgAccessibilityWork(result, options);
        result.Focusable = new SvgAccessibility().Focusable(node);
        result.Hidden = new SvgAccessibility().Hidden(node, result);
        new SvgAccessibility().MapRole(node, result);
        new SvgAccessibility().MapAria(node.SourceNode, result);
        new SvgAccessibility().CalculateName(node.SourceNode, work);
        new SvgAccessibility().CalculateDescription(node.SourceNode, work);
        new SvgAccessibility().ApplyPresentation(node, result);
        if (result.Hidden || result.Presentational)
        {
            result.Name = "";
            result.Description = "";
            result.NameSource = SvgAccessibleNameSource.None;
            result.DescriptionSource = SvgAccessibleDescriptionSource.None;
            result.NameReferences.Clear();
            result.DescriptionReferences.Clear();
        }
        result.Success = !work.Failed;
        return result;
    }
void CalculateName(SvgNode node, SvgAccessibilityWork work)
    {
        SvgAttribute? labelled = new SvgAccessibility().Attribute(node, "aria-labelledby");
        if (labelled != null)
        {
            string value = new SvgAccessibility().RelationshipText(node, labelled, work.Result.NameReferences, work, true);
            if (value.Length > 0)
            {
                work.Result.Name = value;
                work.Result.NameSource = SvgAccessibleNameSource.AriaLabelledBy;
                return;
            }
        }
        SvgAttribute? label = new SvgAccessibility().Attribute(node, "aria-label");
        if (label != null)
        {
            string value = new SvgAccessibility().Normalize(label.Value);
            if (value.Length > 0)
            {
                work.Result.Name = value;
                work.Result.NameSource = SvgAccessibleNameSource.AriaLabel;
                return;
            }
        }
        SvgNode? title = new SvgAccessibility().DirectChild(node, SvgElementKind.Title);
        if (title != null)
        {
            string value = new SvgAccessibility().Text(title, work);
            if (value.Length > 0)
            {
                work.Result.Name = value;
                work.Result.NameSource = SvgAccessibleNameSource.Title;
            }
        }
    }
void CalculateDescription(SvgNode node, SvgAccessibilityWork work)
    {
        SvgAttribute? described = new SvgAccessibility().Attribute(node, "aria-describedby");
        if (described != null)
        {
            string value = new SvgAccessibility().RelationshipText(node, described, work.Result.DescriptionReferences, work, false);
            if (value.Length > 0)
            {
                work.Result.Description = value;
                work.Result.DescriptionSource = SvgAccessibleDescriptionSource.AriaDescribedBy;
                return;
            }
        }
        SvgNode? description = new SvgAccessibility().DirectChild(node, SvgElementKind.Desc);
        if (description != null)
        {
            string value = new SvgAccessibility().Text(description, work);
            if (value.Length > 0)
            {
                work.Result.Description = value;
                work.Result.DescriptionSource = SvgAccessibleDescriptionSource.Description;
            }
        }
    }
string RelationshipText(SvgNode owner, SvgAttribute attribute, System.Collections.Generic.List<SvgNode> references, SvgAccessibilityWork work, bool naming)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int position = 0;
        int count = 0;
        while (position < attribute.Value.Length)
        {
            while (position < attribute.Value.Length && System.Char.IsWhiteSpace(attribute.Value[position])) position = position + 1;
            if (position >= attribute.Value.Length) break;
            int start = position;
            while (position < attribute.Value.Length && !System.Char.IsWhiteSpace(attribute.Value[position])) position = position + 1;
            count = count + 1;
            if (count > work.Options.MaximumReferences)
            {
                new SvgAccessibility().Limit(work, attribute.SourceSpan, "The ARIA relationship reference limit was exceeded.");
                return new SvgAccessibility().Normalize(builder.ToString());
            }
            string id = attribute.Value.Substring(start, position - start);
            SvgNode? referenced = owner.OwnerDocument == null ? null : owner.OwnerDocument.Index.GetById(id);
            if (referenced == null)
            {
                new SvgAccessibility().Add(work.Result, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidReference, "An ARIA relationship references a missing element: " + id + ".", owner);
                continue;
            }
            references.Add(referenced);
            string text = naming ? new SvgAccessibility().ReferenceName(referenced, work) : new SvgAccessibility().Text(referenced, work);
            if (text.Length > 0)
            {
                if (builder.Length > 0) builder.Append(' ');
                builder.Append(text);
            }
        }
        return new SvgAccessibility().Normalize(builder.ToString());
    }
string ReferenceName(SvgNode node, SvgAccessibilityWork work)
    {
        if (!work.Naming.Add(node))
        {
            new SvgAccessibility().Add(work.Result, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidReference, "A cyclic aria-labelledby relationship was ignored.", node);
            return "";
        }
        string value = "";
        SvgAttribute? labelled = new SvgAccessibility().Attribute(node, "aria-labelledby");
        if (labelled != null) value = new SvgAccessibility().RelationshipText(node, labelled, new System.Collections.Generic.List<SvgNode>(), work, true);
        if (value.Length == 0)
        {
            SvgAttribute? label = new SvgAccessibility().Attribute(node, "aria-label");
            if (label != null) value = new SvgAccessibility().Normalize(label.Value);
        }
        if (value.Length == 0)
        {
            SvgNode? title = new SvgAccessibility().DirectChild(node, SvgElementKind.Title);
            if (title != null) value = new SvgAccessibility().Text(title, work);
        }
        if (value.Length == 0) value = new SvgAccessibility().Text(node, work);
        work.Naming.Remove(node);
        return value;
    }
string Text(SvgNode node, SvgAccessibilityWork work)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        stack.Add(node);
        while (stack.Count > 0)
        {
            if (work.Nodes >= work.Options.MaximumNodes)
            {
                new SvgAccessibility().Limit(work, node.SourceSpan, "The accessible text node limit was exceeded.");
                break;
            }
            int last = stack.Count - 1;
            SvgNode current = stack[last];
            stack.RemoveAt(last);
            work.Nodes = work.Nodes + 1;
            if (current.Kind == SvgNodeKind.Text || current.Kind == SvgNodeKind.CData)
            {
                if ((long)work.Characters + current.Value.Length > work.Options.MaximumTextCharacters)
                {
                    new SvgAccessibility().Limit(work, current.SourceSpan, "The accessible text character limit was exceeded.");
                    break;
                }
                builder.Append(current.Value);
                work.Characters = work.Characters + current.Value.Length;
            }
            int index = current.Children.Count - 1;
            while (index >= 0)
            {
                stack.Add(current.Children[index]);
                index = index - 1;
            }
        }
        return new SvgAccessibility().Normalize(builder.ToString());
    }
void MapRole(SvgResolvedNode node, SvgAccessibleSemantics result)
    {
        SvgAttribute? role = new SvgAccessibility().Attribute((node.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), "role");
        if (role != null)
        {
            string explicitRole = new SvgAccessibility().FirstRole(role.Value);
            if (explicitRole.Length > 0)
            {
                result.Role = explicitRole == "none" ? "presentation" : explicitRole;
                result.ExplicitRole = true;
                return;
            }
            new SvgAccessibility().Add(result, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "The role attribute contains no recognized non-abstract role.", node.SourceNode);
        }
        result.Role = new SvgAccessibility().ImplicitRole(node);
    }
void ApplyPresentation(SvgResolvedNode node, SvgAccessibleSemantics result)
    {
        if (result.Role != "presentation") return;
        if (result.Focusable || new SvgAccessibility().HasGlobalAria((node.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))))
        {
            result.Role = new SvgAccessibility().ImplicitRole(node);
            result.ExplicitRole = false;
            new SvgAccessibility().Add(result, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "The presentation role was ignored on a focusable element or an element with global ARIA semantics.", node.SourceNode);
            return;
        }
        result.Presentational = true;
    }
void MapAria(SvgNode node, SvgAccessibleSemantics result)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            string name = attribute.LocalName;
            if (attribute.NamespaceUri.Length == 0 && name.StartsWith("aria-", System.StringComparison.Ordinal) && name != "aria-label" && name != "aria-labelledby" && name != "aria-describedby")
            {
                SvgAccessibilityValueKind kind = new SvgAccessibility().StateName(name) ? SvgAccessibilityValueKind.State : SvgAccessibilityValueKind.Property;
                string? value = new SvgAccessibility().NormalizeAria(name, attribute.Value);
                bool valid = value != null;
                string? mapped = valid ? value : attribute.Value.Trim();
                SvgAccessibilityValue item = new SvgAccessibilityValue(name, mapped, kind, attribute, valid);
                if (kind == SvgAccessibilityValueKind.State) result.States.Add(item);
                else result.Properties.Add(item);
                if (!valid) new SvgAccessibility().Add(result, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "The " + name + " value is invalid.", node);
            }
            index = index + 1;
        }
    }
string? NormalizeAria(string name, string source)
    {
        string value = source.Trim();
        string lower = value.ToLowerInvariant();
        if (new SvgAccessibility().BooleanName(name)) return lower == "true" || lower == "false" ? lower : null;
        if (name == "aria-checked" || name == "aria-pressed") return lower == "true" || lower == "false" || lower == "mixed" || lower == "undefined" ? lower : null;
        if (new SvgAccessibility().IntegerName(name)) return SvgNumberParser.ParseInteger(value, 64).Success ? value : null;
        if (new SvgAccessibility().NumberName(name)) return SvgNumberParser.ParseNumber(value, 128).Success ? value : null;
        if (name == "aria-live") return new SvgAccessibility().OneOfWithValueAndFirstAndSecondAndThird(lower, "off", "polite", "assertive") ? lower : null;
        if (name == "aria-orientation") return new SvgAccessibility().OneOfWithValueAndFirstAndSecondAndThird(lower, "horizontal", "vertical", "undefined") ? lower : null;
        if (name == "aria-sort") return new SvgAccessibility().OneOfWithValueAndFirstAndSecondAndThirdAndFourth(lower, "ascending", "descending", "none", "other") ? lower : null;
        if (name == "aria-autocomplete") return new SvgAccessibility().OneOfWithValueAndFirstAndSecondAndThirdAndFourth(lower, "inline", "list", "both", "none") ? lower : null;
        if (name == "aria-current") return new SvgAccessibility().OneOfWithValueAndFirstAndSecondAndThirdAndFourthAndFifthAndSixthAndSeventh(lower, "false", "true", "page", "step", "location", "date", "time") ? lower : null;
        return value;
    }
bool Hidden(SvgResolvedNode node, SvgAccessibleSemantics result)
    {
        SvgResolvedNode? current = node;
        System.Collections.Generic.HashSet<SvgResolvedNode> visited = new System.Collections.Generic.HashSet<SvgResolvedNode>();
        while (current != null && visited.Add(current))
        {
            if (!current.Active) return true;
            if (current.ComputedStyle != null && current.ComputedStyle.GetKeyword(SvgCssPropertyNames.Display) == "none") return true;
            SvgAttribute? hidden = current.SourceNode == null ? null : new SvgAccessibility().Attribute(current.SourceNode, "aria-hidden");
            if (hidden != null)
            {
                string value = hidden.Value.Trim().ToLowerInvariant();
                if (value == "true") return true;
                if (value != "false") new SvgAccessibility().Add(result, SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "The aria-hidden value is invalid.", current.SourceNode);
            }
            current = current.Parent;
        }
        SvgElementKind kind = (node.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ElementKind;
        return kind == SvgElementKind.Defs || kind == SvgElementKind.Style || kind == SvgElementKind.Script || kind == SvgElementKind.Metadata || kind == SvgElementKind.Title || kind == SvgElementKind.Desc;
    }
bool Focusable(SvgResolvedNode node)
    {
        SvgAttribute? tab = new SvgAccessibility().Attribute((node.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.TabIndex);
        if (tab != null && SvgNumberParser.ParseInteger(tab.Value, 64).Success) return true;
        return node.Kind == SvgResolvedNodeKind.Link && node.Structural != null && node.Structural.Success && (node.Structural.Href != null || node.Structural.XLinkHref != null);
    }
string ImplicitRole(SvgResolvedNode node)
    {
        SvgElementKind kind = (node.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ElementKind;
        if (node.Kind == SvgResolvedNodeKind.Link && node.Structural != null && (node.Structural.Href != null || node.Structural.XLinkHref != null)) return "link";
        if (kind == SvgElementKind.Svg) return node.Parent == null ? "graphics-document" : "group";
        if (kind == SvgElementKind.Image) return "img";
        if (kind == SvgElementKind.Text || kind == SvgElementKind.TSpan || kind == SvgElementKind.TextPath) return "text";
        if (kind == SvgElementKind.G || kind == SvgElementKind.Switch || kind == SvgElementKind.Use) return "group";
        if (node.Kind == SvgResolvedNodeKind.Shape) return "graphics-symbol";
        return "";
    }
string FirstRole(string source)
    {
        int position = 0;
        while (position < source.Length)
        {
            while (position < source.Length && System.Char.IsWhiteSpace(source[position])) position = position + 1;
            int start = position;
            while (position < source.Length && !System.Char.IsWhiteSpace(source[position])) position = position + 1;
            if (position > start)
            {
                string value = source.Substring(start, position - start).ToLowerInvariant();
                if (new SvgAccessibility().KnownRole(value)) return value;
            }
        }
        return "";
    }
bool KnownRole(string role)
    {
        switch (role)
        {
            case "alert": case "application": case "button": case "checkbox": case "document": case "figure": case "graphics-document": case "graphics-object": case "graphics-symbol": case "group": case "heading": case "img": case "link": case "list": case "listitem": case "meter": case "none": case "presentation": case "progressbar": case "radio": case "region": case "scrollbar": case "separator": case "slider": case "spinbutton": case "status": case "switch": case "text": case "textbox": case "timer": return true;
            default: return false;
        }
    }
bool HasGlobalAria(SvgNode node)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName.StartsWith("aria-", System.StringComparison.Ordinal)) return true;
            index = index + 1;
        }
        return false;
    }
bool StateName(string name)
    {
        return name == "aria-busy" || name == "aria-checked" || name == "aria-current" || name == "aria-disabled" || name == "aria-expanded" || name == "aria-grabbed" || name == "aria-hidden" || name == "aria-invalid" || name == "aria-pressed" || name == "aria-selected";
    }
bool BooleanName(string name)
    {
        return name == "aria-atomic" || name == "aria-busy" || name == "aria-disabled" || name == "aria-expanded" || name == "aria-grabbed" || name == "aria-hidden" || name == "aria-modal" || name == "aria-multiline" || name == "aria-multiselectable" || name == "aria-readonly" || name == "aria-required" || name == "aria-selected";
    }
bool IntegerName(string name)
    {
        return name == "aria-colcount" || name == "aria-colindex" || name == "aria-colspan" || name == "aria-level" || name == "aria-posinset" || name == "aria-rowcount" || name == "aria-rowindex" || name == "aria-rowspan" || name == "aria-setsize";
    }
bool NumberName(string name)
    {
        return name == "aria-valuemax" || name == "aria-valuemin" || name == "aria-valuenow";
    }
bool OneOfWithValueAndFirstAndSecondAndThird(string value, string first, string second, string third)
    {
        return value == first || value == second || value == third;
    }
bool OneOfWithValueAndFirstAndSecondAndThirdAndFourth(string value, string first, string second, string third, string fourth)
    {
        return value == first || value == second || value == third || value == fourth;
    }
bool OneOfWithValueAndFirstAndSecondAndThirdAndFourthAndFifthAndSixthAndSeventh(string value, string first, string second, string third, string fourth, string fifth, string sixth, string seventh)
    {
        return value == first || value == second || value == third || value == fourth || value == fifth || value == sixth || value == seventh;
    }
SvgNode? DirectChild(SvgNode node, SvgElementKind kind)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            if (node.Children[index].Kind == SvgNodeKind.Element && node.Children[index].ElementKind == kind) return node.Children[index];
            index = index + 1;
        }
        return null;
    }
SvgAttribute? Attribute(SvgNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name) return attribute;
            index = index + 1;
        }
        return null;
    }
string Normalize(string source)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool space = false;
        int index = 0;
        while (index < source.Length)
        {
            char value = source[index];
            if (System.Char.IsWhiteSpace(value)) space = builder.Length > 0;
            else
            {
                if (space) builder.Append(' ');
                builder.Append(value);
                space = false;
            }
            index = index + 1;
        }
        return builder.ToString();
    }
void Limit(SvgAccessibilityWork work, SvgSourceSpan? source, string message)
    {
        if (!work.Failed) work.Result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, message, source));
        work.Failed = true;
    }
void Add(SvgAccessibleSemantics result, SvgDiagnosticSeverity severity, SvgDiagnosticCode code, string message, SvgNode? node)
    {
        result.Diagnostics.Add(new SvgDiagnostic(severity, code, message, node == null ? null : node.SourceSpan));
    }
}
