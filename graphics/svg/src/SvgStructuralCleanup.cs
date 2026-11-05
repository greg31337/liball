namespace svg;

internal static class SvgStructuralCleanup
{
    internal static void CollapseGroups(SvgProcessContext context)
    {
        if (UnsafeDocument(context.Document)) return;
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = nodes.Count - 1;
        while (index >= 0 && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (CollapsibleGroup(node)) CollapseGroup(context, node);
            index = index - 1;
        }
        if (context.IsFailed()) return;
        nodes = CollectNodes(context.Document);
        index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            CombineTransformList(context, nodes[index], SvgAttributeNames.Transform);
            CombineTransformList(context, nodes[index], "gradientTransform");
            CombineTransformList(context, nodes[index], "patternTransform");
            index = index + 1;
        }
    }

    internal static void HoistCommonAttributes(SvgProcessContext context)
    {
        if (UnsafeDocument(context.Document)) return;
        SvgPropertyCatalog properties = SvgPropertyCatalog.CreateDefault();
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode group = nodes[index];
            if (SafeGroup(group))
            {
                System.Collections.Generic.List<SvgNode>? children = DirectElementChildren(group);
                if (children != null && children.Count > 1) Hoist(context, group, children, properties);
                else if (children != null && children.Count == 1) Sink(context, group, children[0], properties);
            }
            index = index + 1;
        }
    }

    internal static void MergeAdjacentPaths(SvgProcessContext context)
    {
        if (UnsafeDocument(context.Document)) return;
        System.Collections.Generic.List<SvgNode> parents = CollectNodes(context.Document);
        int parentIndex = 0;
        while (parentIndex < parents.Count && !context.IsFailed())
        {
            SvgNode parent = parents[parentIndex];
            int index = 0;
            while (index < parent.Children.Count && !context.IsFailed())
            {
                SvgNode first = parent.Children[index];
                int secondIndex = NextSignificantChild(parent, index + 1);
                if (secondIndex >= 0)
                {
                    SvgNode second = parent.Children[secondIndex];
                    if (CompatiblePaths(first, second)) MergePaths(context, first, second);
                }
                index = index + 1;
            }
            parentIndex = parentIndex + 1;
        }
    }

    static bool CollapsibleGroup(SvgNode group)
    {
        if (!SafeGroup(group) || group.Parent == null || UnsafeCollapseParent(group.Parent)) return false;
        SvgAttribute? transform = null;
        int attributeIndex = 0;
        while (attributeIndex < group.Attributes.Count)
        {
            SvgAttribute attribute = group.Attributes[attributeIndex];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == SvgAttributeNames.Transform && transform == null) transform = attribute;
            else return false;
            attributeIndex = attributeIndex + 1;
        }
        System.Collections.Generic.List<SvgNode>? children = DirectElementChildren(group);
        if (children == null || children.Count == 0) return false;
        if (transform == null) return true;
        return children.Count == 1 && Transformable(children[0]);
    }

    static void CollapseGroup(SvgProcessContext context, SvgNode group)
    {
        SvgNode? parent = group.Parent;
        if (parent == null) return;
        SvgAttribute? groupTransform = FindAttribute(group, SvgAttributeNames.Transform);
        if (groupTransform != null)
        {
            SvgNode child = (DirectElementChildren(group) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[0];
            SvgAttribute? childTransform = FindAttribute(child, SvgAttributeNames.Transform);
            string? combined = CombineTransforms(groupTransform.Value, childTransform == null ? "" : childTransform.Value);
            if (combined == null) return;
            SetAttribute(context, child, childTransform, SvgAttributeNames.Transform, combined);
            if (context.IsFailed()) return;
        }
        int insertion = ChildIndex(parent, group);
        while (group.Children.Count != 0 && !context.IsFailed())
        {
            SvgNode child = group.Children[0];
            SvgMutationResult moved = context.Mutations.MoveChild(child, parent, insertion);
            if (!moved.Success) context.Fail(moved.Message);
            insertion = insertion + 1;
        }
        if (!context.IsFailed()) RemoveNode(context, group);
    }

    static void CombineTransformList(SvgProcessContext context, SvgNode node, string name)
    {
        if (node.Kind != SvgNodeKind.Element || node.Parent == null) return;
        SvgAttribute? attribute = FindAttribute(node, name);
        if (attribute == null) return;
        SvgTransformParseResult parsed = SvgTransformParser.Parse(attribute.Value, 1000000, 4096);
        if (!parsed.Success || parsed.TransformList.Items.Count < 2) return;
        SvgTransformMatrixResult matrix = parsed.TransformList.ToMatrix();
        if (!matrix.Success) return;
        string value = MatrixText(matrix.Matrix);
        if (value.Length != 0 && value != attribute.Value) SetAttribute(context, node, attribute, name, value);
    }

    static string? CombineTransforms(string outer, string inner)
    {
        if (inner.Length == 0)
        {
            SvgTransformParseResult single = SvgTransformParser.Parse(outer, 1000000, 4096);
            return single.Success ? outer : null;
        }
        SvgTransformParseResult parsed = SvgTransformParser.Parse(outer + " " + inner, 1000000, 4096);
        if (!parsed.Success) return null;
        SvgTransformMatrixResult matrix = parsed.TransformList.ToMatrix();
        return matrix.Success ? MatrixText(matrix.Matrix) : null;
    }

    static string MatrixText(SvgMatrix? matrix)
    {
        if (matrix == null) return "";
        SvgTransform transform = new SvgTransform(SvgTransformKind.Matrix, new double[] { matrix.A, matrix.B, matrix.C, matrix.D, matrix.E, matrix.F });
        return transform.ToText();
    }

    static void Hoist(SvgProcessContext context, SvgNode group, System.Collections.Generic.List<SvgNode> children, SvgPropertyCatalog properties)
    {
        if (FindAttribute(group, SvgAttributeNames.Style) != null) return;
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            if (!AttributeChild(children[childIndex]) || FindAttribute(children[childIndex], SvgAttributeNames.Style) != null) return;
            childIndex = childIndex + 1;
        }
        SvgNode first = children[0];
        System.Collections.Generic.List<SvgAttribute> candidates = new System.Collections.Generic.List<SvgAttribute>();
        int attributeIndex = 0;
        while (attributeIndex < first.Attributes.Count)
        {
            SvgAttribute attribute = first.Attributes[attributeIndex];
            if (HoistableAttribute(attribute, properties) && FindAttribute(group, attribute.LocalName) == null && StableInheritedValue(attribute.Value) && AllChildrenMatch(children, attribute)) candidates.Add(attribute);
            attributeIndex = attributeIndex + 1;
        }
        attributeIndex = 0;
        while (attributeIndex < candidates.Count && !context.IsFailed())
        {
            SvgAttribute candidate = candidates[attributeIndex];
            SvgAttribute groupAttribute = context.Documents.CreateAttribute(candidate.QualifiedName, candidate.Prefix, candidate.LocalName, candidate.NamespaceUri, candidate.Value);
            SvgMutationResult appended = context.Mutations.AppendAttribute(group, groupAttribute);
            if (!appended.Success) context.Fail(appended.Message);
            childIndex = 0;
            while (childIndex < children.Count && !context.IsFailed())
            {
                SvgAttribute? childAttribute = FindAttribute(children[childIndex], candidate.LocalName);
                RemoveAttribute(context, children[childIndex], childAttribute);
                childIndex = childIndex + 1;
            }
            attributeIndex = attributeIndex + 1;
        }
    }

    static void Sink(SvgProcessContext context, SvgNode group, SvgNode child, SvgPropertyCatalog properties)
    {
        if (!AttributeChild(child) || FindAttribute(group, SvgAttributeNames.Style) != null || FindAttribute(child, SvgAttributeNames.Style) != null) return;
        System.Collections.Generic.List<SvgAttribute> candidates = new System.Collections.Generic.List<SvgAttribute>();
        int index = 0;
        while (index < group.Attributes.Count)
        {
            SvgAttribute attribute = group.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == SvgAttributeNames.Transform)
            {
                index = index + 1;
                continue;
            }
            if (!HoistableAttribute(attribute, properties)) return;
            SvgAttribute? childAttribute = FindAttribute(child, attribute.LocalName);
            if (childAttribute == null && !StableInheritedValue(attribute.Value)) return;
            if (childAttribute != null && !StableInheritedValue(childAttribute.Value)) return;
            candidates.Add(attribute);
            index = index + 1;
        }
        index = 0;
        while (index < candidates.Count && !context.IsFailed())
        {
            SvgAttribute candidate = candidates[index];
            if (FindAttribute(child, candidate.LocalName) == null)
            {
                SvgAttribute copy = context.Documents.CreateAttribute(candidate.QualifiedName, candidate.Prefix, candidate.LocalName, candidate.NamespaceUri, candidate.Value);
                SvgMutationResult appended = context.Mutations.AppendAttribute(child, copy);
                if (!appended.Success) context.Fail(appended.Message);
            }
            if (!context.IsFailed()) RemoveAttribute(context, group, candidate);
            index = index + 1;
        }
    }

    static bool HoistableAttribute(SvgAttribute? attribute, SvgPropertyCatalog properties)
    {
        if (attribute == null || attribute.NamespaceUri.Length != 0) return false;
        SvgPropertySupportEntry? property = properties.FindPresentationAttribute(attribute.LocalName);
        if (property == null || !property.Inherited) return false;
        string name = property.Name;
        return name != "cursor" && name != "pointer-events";
    }

    static bool StableInheritedValue(string value)
    {
        string text = value.Trim();
        if (text.Length == 0 || text.IndexOf("currentcolor", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("context-fill", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("context-stroke", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (text.IndexOf("var(", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("env(", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("calc(", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (string.Equals(text, "inherit", System.StringComparison.OrdinalIgnoreCase) || string.Equals(text, "unset", System.StringComparison.OrdinalIgnoreCase) || string.Equals(text, "revert", System.StringComparison.OrdinalIgnoreCase) || string.Equals(text, "revert-layer", System.StringComparison.OrdinalIgnoreCase)) return false;
        if (text.IndexOf('%') >= 0 || text.IndexOf("em", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("ex", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("ch", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("rem", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
        return true;
    }

    static bool AllChildrenMatch(System.Collections.Generic.List<SvgNode> children, SvgAttribute candidate)
    {
        int index = 0;
        while (index < children.Count)
        {
            SvgAttribute? attribute = FindAttribute(children[index], candidate.LocalName);
            if (attribute == null || attribute.NamespaceUri != candidate.NamespaceUri || attribute.Value != candidate.Value) return false;
            index = index + 1;
        }
        return true;
    }

    static bool CompatiblePaths(SvgNode first, SvgNode second)
    {
        if (first.Kind != SvgNodeKind.Element || second.Kind != SvgNodeKind.Element || first.ElementKind != SvgElementKind.Path || second.ElementKind != SvgElementKind.Path) return false;
        if (first.NamespaceUri != SvgNamespaces.Svg || second.NamespaceUri != SvgNamespaces.Svg || first.Children.Count != 0 || second.Children.Count != 0) return false;
        if (InheritedMarkerRisk(first)) return false;
        if (!MergeAttributes(first) || !MergeAttributes(second) || !EquivalentPathAttributes(first, second)) return false;
        SvgAttribute? firstFill = FindAttribute(first, "fill");
        SvgAttribute? secondFill = FindAttribute(second, "fill");
        SvgAttribute? firstStroke = FindAttribute(first, "stroke");
        SvgAttribute? secondStroke = FindAttribute(second, "stroke");
        if (firstFill == null || secondFill == null || firstStroke == null || secondStroke == null) return false;
        SvgColorParseResult fill = new SvgColorParser().Parse(firstFill.Value, 4096);
        if (!fill.Success || fill.Color == null || fill.Color.IsCurrentColor) return false;
        if (!string.Equals(firstStroke.Value.Trim(), "none", System.StringComparison.OrdinalIgnoreCase) || !string.Equals(secondStroke.Value.Trim(), "none", System.StringComparison.OrdinalIgnoreCase)) return false;
        if (!UnitOpacity(first, "opacity") || !UnitOpacity(second, "opacity") || !UnitOpacity(first, "fill-opacity") || !UnitOpacity(second, "fill-opacity")) return false;
        SvgAttribute? firstData = FindAttribute(first, SvgAttributeNames.D);
        SvgAttribute? secondData = FindAttribute(second, SvgAttributeNames.D);
        if (firstData == null || secondData == null || firstData.Value.Trim().Length == 0 || secondData.Value.Trim().Length == 0) return false;
        SvgPathParseResult firstPath = new SvgPathParser().Parse(firstData.Value, 1000000, 4096);
        SvgPathParseResult secondPath = new SvgPathParser().Parse(secondData.Value, 1000000, 4096);
        if (!firstPath.Success || !secondPath.Success) return false;
        SvgDrawingBoundsResult firstBounds = (firstPath.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GetFillBounds();
        SvgDrawingBoundsResult secondBounds = (secondPath.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GetFillBounds();
        return firstBounds.Success && secondBounds.Success && Disjoint(firstBounds.FillBounds, secondBounds.FillBounds);
    }

    static bool InheritedMarkerRisk(SvgNode node)
    {
        SvgNode? current = node.Parent;
        while (current != null && current.Kind == SvgNodeKind.Element)
        {
            if (FindAttribute(current, SvgAttributeNames.Style) != null || FindAttribute(current, SvgCssPropertyNames.Marker) != null || FindAttribute(current, SvgCssPropertyNames.MarkerStart) != null || FindAttribute(current, SvgCssPropertyNames.MarkerMid) != null || FindAttribute(current, SvgCssPropertyNames.MarkerEnd) != null) return true;
            current = current.Parent;
        }
        return false;
    }

    static bool MergeAttributes(SvgNode node)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length != 0 || !MergeAttributeName(attribute.LocalName)) return false;
            index = index + 1;
        }
        return true;
    }

    static bool MergeAttributeName(string name)
    {
        return name == SvgAttributeNames.D || name == "fill" || name == "fill-opacity" || name == "fill-rule" || name == "stroke" || name == "stroke-opacity" || name == "stroke-width" || name == "stroke-linecap" || name == "stroke-linejoin" || name == "stroke-miterlimit" || name == "stroke-dasharray" || name == "stroke-dashoffset" || name == "opacity" || name == "shape-rendering" || name == "color-rendering" || name == "color-interpolation";
    }

    static bool EquivalentPathAttributes(SvgNode first, SvgNode second)
    {
        if (first.Attributes.Count != second.Attributes.Count) return false;
        int firstIndex = 0;
        while (firstIndex < first.Attributes.Count)
        {
            SvgAttribute firstAttribute = first.Attributes[firstIndex];
            if (firstAttribute.LocalName != SvgAttributeNames.D)
            {
                SvgAttribute? secondAttribute = FindAttribute(second, firstAttribute.LocalName);
                if (secondAttribute == null || secondAttribute.NamespaceUri != firstAttribute.NamespaceUri || secondAttribute.Value != firstAttribute.Value) return false;
            }
            firstIndex = firstIndex + 1;
        }
        return true;
    }

    static bool UnitOpacity(SvgNode node, string name)
    {
        SvgAttribute? attribute = FindAttribute(node, name);
        if (attribute == null) return true;
        SvgNumberParseResult parsed = SvgNumberParser.ParseNumber(attribute.Value, 128);
        return parsed.Success && parsed.Value == 1;
    }

    static bool Disjoint(SvgRect? first, SvgRect? second)
    {
        if (first == null || second == null || first.IsEmpty || second.IsEmpty) return true;
        return first.X + first.Width < second.X || second.X + second.Width < first.X || first.Y + first.Height < second.Y || second.Y + second.Height < first.Y;
    }

    static void MergePaths(SvgProcessContext context, SvgNode first, SvgNode second)
    {
        SvgAttribute? firstData = FindAttribute(first, SvgAttributeNames.D);
        SvgAttribute? secondData = FindAttribute(second, SvgAttributeNames.D);
        string value = (firstData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value.TrimEnd() + " " + (secondData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value.TrimStart();
        SetAttribute(context, first, firstData, SvgAttributeNames.D, value);
        if (!context.IsFailed()) RemoveNode(context, second);
    }

    static bool UnsafeDocument(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(document);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            SvgNode node = nodes[nodeIndex];
            if (node.Kind == SvgNodeKind.Element)
            {
                if (node.ElementKind == SvgElementKind.Script || AnimationKind(node.ElementKind)) return true;
                if (node.ElementKind == SvgElementKind.Style && new SvgNodeApi().GetTextContent(node).Trim().Length != 0) return true;
                if (node.LocalName == "link" && AttributeValue(node, "rel").IndexOf("stylesheet", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
                int attributeIndex = 0;
                while (attributeIndex < node.Attributes.Count)
                {
                    if (EventAttribute(node.Attributes[attributeIndex])) return true;
                    attributeIndex = attributeIndex + 1;
                }
            }
            if (node.Kind == SvgNodeKind.ProcessingInstruction && string.Equals(node.LocalName, "xml-stylesheet", System.StringComparison.OrdinalIgnoreCase)) return true;
            nodeIndex = nodeIndex + 1;
        }
        return false;
    }

    static bool SafeGroup(SvgNode group)
    {
        if (group.Kind != SvgNodeKind.Element || group.ElementKind != SvgElementKind.G || group.NamespaceUri != SvgNamespaces.Svg) return false;
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(group);
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (node.Kind == SvgNodeKind.Element)
            {
                SvgElementKind kind = node.ElementKind;
                if (kind == SvgElementKind.Unknown || kind == SvgElementKind.Title || kind == SvgElementKind.Desc || kind == SvgElementKind.Metadata || kind == SvgElementKind.Script || kind == SvgElementKind.ForeignObject || AnimationKind(kind)) return false;
                int attributeIndex = 0;
                while (attributeIndex < node.Attributes.Count)
                {
                    SvgAttribute attribute = node.Attributes[attributeIndex];
                    if (AccessibilityAttribute(attribute) || EventAttribute(attribute)) return false;
                    attributeIndex = attributeIndex + 1;
                }
            }
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                pending.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        return true;
    }

    static bool UnsafeCollapseParent(SvgNode parent)
    {
        SvgElementKind kind = parent.ElementKind;
        return kind == SvgElementKind.Switch || kind == SvgElementKind.Text || kind == SvgElementKind.TextPath || kind == SvgElementKind.TSpan;
    }

    static bool Transformable(SvgNode node)
    {
        if (!AttributeChild(node)) return false;
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        SvgElementDefinition? definition = catalog.Find(node.ElementKind);
        return definition != null && definition.FindAttribute("", SvgAttributeNames.Transform) != null;
    }

    static bool AttributeChild(SvgNode node)
    {
        if (node.Kind != SvgNodeKind.Element || node.NamespaceUri != SvgNamespaces.Svg || node.ElementKind == SvgElementKind.Unknown) return false;
        SvgElementKind kind = node.ElementKind;
        return kind != SvgElementKind.Defs && kind != SvgElementKind.Style && kind != SvgElementKind.Script && kind != SvgElementKind.Metadata && kind != SvgElementKind.Title && kind != SvgElementKind.Desc && !AnimationKind(kind);
    }

    static System.Collections.Generic.List<SvgNode>? DirectElementChildren(SvgNode group)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < group.Children.Count)
        {
            SvgNode child = group.Children[index];
            if (child.Kind == SvgNodeKind.Element) result.Add(child);
            else if (child.Kind != SvgNodeKind.Comment && !((child.Kind == SvgNodeKind.Text || child.Kind == SvgNodeKind.CData) && child.Value.Trim().Length == 0)) return null;
            index = index + 1;
        }
        return result;
    }

    static bool AnimationKind(SvgElementKind kind)
    {
        return kind == SvgElementKind.Animate || kind == SvgElementKind.AnimateMotion || kind == SvgElementKind.AnimateTransform || kind == SvgElementKind.Set || kind == SvgElementKind.Discard;
    }

    static bool AccessibilityAttribute(SvgAttribute attribute)
    {
        if (attribute.NamespaceUri.Length != 0) return false;
        return attribute.LocalName == "role" || attribute.LocalName == SvgAttributeNames.TabIndex || attribute.LocalName.StartsWith("aria-", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool EventAttribute(SvgAttribute attribute)
    {
        return attribute.LocalName.StartsWith("on", System.StringComparison.OrdinalIgnoreCase);
    }

    static int NextSignificantChild(SvgNode parent, int start)
    {
        int index = start;
        while (index < parent.Children.Count)
        {
            SvgNode child = parent.Children[index];
            if (child.Kind == SvgNodeKind.Comment || (child.Kind == SvgNodeKind.Text || child.Kind == SvgNodeKind.CData) && child.Value.Trim().Length == 0) index = index + 1;
            else return index;
        }
        return -1;
    }

    static int ChildIndex(SvgNode parent, SvgNode child)
    {
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (object.ReferenceEquals(parent.Children[index], child)) return index;
            index = index + 1;
        }
        return -1;
    }

    static SvgAttribute? FindAttribute(SvgNode node, string name)
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

    static string AttributeValue(SvgNode node, string name)
    {
        SvgAttribute? attribute = FindAttribute(node, name);
        return attribute == null ? "" : attribute.Value;
    }

    static void SetAttribute(SvgProcessContext context, SvgNode node, SvgAttribute? current, string name, string value)
    {
        SvgAttribute replacement = context.Documents.CreateAttribute(name, "", name, "", value);
        SvgMutationResult mutation = current == null ? context.Mutations.AppendAttribute(node, replacement) : context.Mutations.ReplaceAttribute(node, current, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void RemoveAttribute(SvgProcessContext context, SvgNode node, SvgAttribute? attribute)
    {
        if (attribute == null) return;
        SvgMutationResult mutation = context.Mutations.RemoveAttribute(node, attribute);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void RemoveNode(SvgProcessContext context, SvgNode node)
    {
        if (node.Parent == null) return;
        SvgMutationResult mutation = context.Mutations.RemoveChild(node.Parent, node);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static System.Collections.Generic.List<SvgNode> CollectNodes(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(document.DocumentNode);
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            result.Add(node);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }
}
