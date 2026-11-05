namespace svg;

/// <summary>Builds bounded platform-neutral accessibility trees from resolved SVG documents.</summary>
public class SvgAccessibilityTreeBuilder : ISvgAccessibilityTreeBuilder
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgAccessibilityTreeBuilder()
    {
    }

    /// <summary>Builds an accessibility tree with secure deterministic defaults.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <returns>The complete or failed accessibility-tree result.</returns>
    public SvgAccessibilityTree BuildWithDocument(SvgResolvedDocument document)
    {
        return new SvgAccessibilityTreeBuilder().BuildWithDocumentAndOptions(document, new SvgAccessibilityTreeOptions());
    }

    /// <summary>Builds a bounded accessibility tree with explicit options.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="options">The traversal, text, and semantic limits.</param>
    /// <returns>The complete or failed accessibility-tree result.</returns>
    public SvgAccessibilityTree BuildWithDocumentAndOptions(SvgResolvedDocument document, SvgAccessibilityTreeOptions options)
    {
        SvgAccessibilityTree result = new SvgAccessibilityTree(document);
        if (!new SvgAccessibilityTreeBuilder().Valid(document, options)) return new SvgAccessibilityTreeBuilder().Fail(result, SvgAccessibilityTreeError.InvalidArgument, "Accessibility-tree arguments are invalid.", document == null || document.SourceDocument == null ? null : document.SourceDocument.DocumentElement);
        SvgAccessibilityTreeWork work = new SvgAccessibilityTreeWork(result, options);
        if (!new SvgAccessibilityTreeBuilder().PrepareFocus(document, work)) return result;
        System.Collections.Generic.List<SvgAccessibilityTreeFrame> stack = new System.Collections.Generic.List<SvgAccessibilityTreeFrame>();
        stack.Add(new SvgAccessibilityTreeFrame(document.Root, null));
        while (stack.Count > 0 && result.Error == SvgAccessibilityTreeError.None)
        {
            SvgAccessibilityTreeFrame frame = stack[stack.Count - 1];
            if (!frame.Entered)
            {
                frame.Entered = true;
                if (!new SvgAccessibilityTreeBuilder().Enter(frame, work)) break;
                if ((frame.Resolved ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceNode == null || (frame.Resolved.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind != SvgNodeKind.Element)
                {
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }
            }
            if (frame.NextChild < (frame.Resolved ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
            {
                SvgResolvedNode child = frame.Resolved.Children[frame.NextChild];
                frame.NextChild = frame.NextChild + 1;
                stack.Add(new SvgAccessibilityTreeFrame(child, frame.Created == null ? frame.AccessibleParent : frame.Created));
                continue;
            }
            new SvgAccessibilityTreeBuilder().CompleteNode(frame.Created, work);
            stack.RemoveAt(stack.Count - 1);
        }
        result.Text = work.Text.ToString();
        if (result.Error != SvgAccessibilityTreeError.None) return result;
        new SvgAccessibilityTreeBuilder().PopulateFocusOrder(work);
        result.Success = true;
        return result;
    }
bool Enter(SvgAccessibilityTreeFrame frame, SvgAccessibilityTreeWork work)
    {
        SvgAccessibilityTree result = work.Result;
        if (result.VisitedNodeCount >= work.Options.MaximumNodes)
        {
            new SvgAccessibilityTreeBuilder().Fail(result, SvgAccessibilityTreeError.LimitExceeded, "The accessibility-tree node limit was exceeded.", (frame.Resolved ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceNode);
            return false;
        }
        result.VisitedNodeCount = result.VisitedNodeCount + 1;
        SvgNode? source = (frame.Resolved ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceNode;
        if (source == null) return true;
        if (source.Kind != SvgNodeKind.Element)
        {
            new SvgAccessibilityTreeBuilder().AddText(frame.Resolved, frame.AccessibleParent, work);
            return work.Result.Error == SvgAccessibilityTreeError.None;
        }
        SvgAccessibleSemantics semantics = new SvgAccessibility().ResolveWithNodeAndOptions(frame.Resolved, work.Options.Semantics);
        new SvgAccessibilityTreeBuilder().CopyDiagnostics(semantics.Diagnostics, result.Diagnostics);
        if (!semantics.Success)
        {
            new SvgAccessibilityTreeBuilder().Fail(result, SvgAccessibilityTreeError.SemanticFailure, "Accessible semantics could not be calculated within the configured bounds.", source);
            return false;
        }
        if (semantics.Hidden)
        {
            frame.NextChild = frame.Resolved.Children.Count;
            return true;
        }
        if (semantics.Presentational || !new SvgAccessibilityTreeBuilder().Included(frame, semantics)) return true;
        SvgAccessibilityTreeNode created = new SvgAccessibilityTreeNode(result, frame.AccessibleParent, frame.Resolved, semantics);
        frame.Created = created;
        created.ReadingOrder = result.ReadingOrder.Count;
        result.Nodes.Add(created);
        result.ReadingOrder.Add(created);
        work.AccessibleNodes.Add(frame.Resolved, created);
        if (frame.AccessibleParent == null)
        {
            if (result.Root == null) result.Root = created;
            else result.Root.Children.Add(created);
        }
        else frame.AccessibleParent.Children.Add(created);
        SvgFocusEntry? focus;
        if (work.FocusEntries.TryGetValue(frame.Resolved, out focus))
        {
            created.Focusable = true;
            created.Sequential = focus.Sequential;
            created.HasTabIndex = focus.HasTabIndex;
            created.TabIndex = focus.TabIndex;
        }
        created.Bounds = new SvgAccessibilityTreeBuilder().OwnBounds(frame.Resolved, work);
        if (work.Result.Error != SvgAccessibilityTreeError.None) return false;
        created.HasGeometry = created.Bounds != null && !created.Bounds.IsEmpty;
        return true;
    }
bool Included(SvgAccessibilityTreeFrame frame, SvgAccessibleSemantics semantics)
    {
        if ((frame.Resolved ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parent == null) return true;
        return semantics.Role.Length != 0 || semantics.Name.Length != 0 || semantics.Description.Length != 0 || semantics.Focusable;
    }
void AddText(SvgResolvedNode resolved, SvgAccessibilityTreeNode? parent, SvgAccessibilityTreeWork work)
    {
        if (parent == null || !new SvgAccessibilityTreeBuilder().TextOwner(parent)) return;
        string value = new SvgAccessibilityTreeBuilder().Normalize(resolved.Text);
        if (value.Length == 0) return;
        int separator = work.Text.Length == 0 ? 0 : 1;
        if ((long)work.Text.Length + separator + value.Length > work.Options.MaximumTextCharacters)
        {
            new SvgAccessibilityTreeBuilder().Fail(work.Result, SvgAccessibilityTreeError.LimitExceeded, "The accessibility-tree text character limit was exceeded.", resolved.SourceNode);
            return;
        }
        if (separator != 0) work.Text.Append(' ');
        SvgAccessibilityTextRange range = new SvgAccessibilityTextRange(resolved, parent, work.Text.Length, value);
        work.Text.Append(value);
        parent.TextRanges.Add(range);
        work.Result.TextRanges.Add(range);
        new SvgAccessibilityTreeBuilder().MergeText(parent, range.Start, range.Length);
    }
bool TextOwner(SvgAccessibilityTreeNode node)
    {
        return node.Semantics != null && node.Semantics.Role == "text";
    }
string Normalize(string? value)
    {
        if (value == null || value.Length == 0) return "";
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        bool whitespace = true;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (System.Char.IsWhiteSpace(character)) whitespace = true;
            else
            {
                if (whitespace && result.Length > 0) result.Append(' ');
                result.Append(character);
                whitespace = false;
            }
            index = index + 1;
        }
        return result.ToString();
    }
void CompleteNode(SvgAccessibilityTreeNode? node, SvgAccessibilityTreeWork work)
    {
        if (node == null || work.Result.Error != SvgAccessibilityTreeError.None) return;
        int index = 0;
        while (index < node.Children.Count)
        {
            SvgAccessibilityTreeNode child = node.Children[index];
            if (child.Bounds != null && !child.Bounds.IsEmpty)
            {
                if (node.Bounds == null || node.Bounds.IsEmpty) node.Bounds = new SvgAccessibilityTreeBuilder().Copy(child.Bounds);
                else
                {
                    SvgRectOperationResult union = node.Bounds.Union(child.Bounds);
                    if (!union.Success)
                    {
                        new SvgAccessibilityTreeBuilder().Fail(work.Result, SvgAccessibilityTreeError.GeometryFailure, "Accessible child geometry could not be unioned.", node.SourceNode);
                        return;
                    }
                    node.Bounds = union.Rectangle;
                }
            }
            if (child.TextStart >= 0) new SvgAccessibilityTreeBuilder().MergeText(node, child.TextStart, child.TextLength);
            index = index + 1;
        }
        node.HasGeometry = node.Bounds != null && !node.Bounds.IsEmpty;
    }
void MergeText(SvgAccessibilityTreeNode node, int start, int length)
    {
        int end = start + length;
        if (node.TextStart < 0)
        {
            node.TextStart = start;
            node.TextLength = length;
            return;
        }
        int currentEnd = node.TextStart + node.TextLength;
        int minimum = start < node.TextStart ? start : node.TextStart;
        int maximum = end > currentEnd ? end : currentEnd;
        node.TextStart = minimum;
        node.TextLength = maximum - minimum;
    }
SvgRect? OwnBounds(SvgResolvedNode? node, SvgAccessibilityTreeWork work)
    {
        if (node == null || !node.Active || !node.Renderable) return null;
        SvgMatrixOperationResult world = new SvgDrawingBounds().World(node);
        if (!world.Success)
        {
            new SvgAccessibilityTreeBuilder().Fail(work.Result, SvgAccessibilityTreeError.GeometryFailure, "An accessible world transform is invalid.", node.SourceNode);
            return null;
        }
        SvgRect? bounds = null;
        if (node.Kind == SvgResolvedNodeKind.Shape && node.Geometry != null && node.Geometry.HasGeometry)
        {
            SvgDrawingBoundsOptions options = new SvgDrawingBoundsOptions();
            options.IncludeFill = node.Fill == null || node.Fill.Kind != SvgPaintKind.None;
            options.Stroke = new SvgDrawingBounds().ComputedStroke(node, work.Result.Document, true);
            options.Transform = world.Matrix;
            SvgDrawingBoundsResult drawing = new SvgDrawingBounds().Shape(node.Geometry, options);
            if (!drawing.Success)
            {
                new SvgAccessibilityTreeBuilder().Fail(work.Result, SvgAccessibilityTreeError.GeometryFailure, "Accessible shape geometry could not be calculated.", node.SourceNode);
                return null;
            }
            bounds = drawing.Bounds;
        }
        else if (node.Kind == SvgResolvedNodeKind.EmbeddedContent && node.Bounds != null && !node.Bounds.IsEmpty)
        {
            SvgRectTransformResult transformed = (world.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformRect(node.Bounds);
            if (!transformed.Success)
            {
                new SvgAccessibilityTreeBuilder().Fail(work.Result, SvgAccessibilityTreeError.GeometryFailure, "Accessible embedded-content geometry could not be transformed.", node.SourceNode);
                return null;
            }
            bounds = transformed.Rectangle;
        }
        else if (node.SourceNode != null && node.SourceNode.ElementKind == SvgElementKind.Text) bounds = new SvgAccessibilityTreeBuilder().TextBounds(node, world.Matrix, work);
        if (bounds == null || bounds.IsEmpty || work.Result.Error != SvgAccessibilityTreeError.None) return bounds;
        SvgRectOperationResult clip = new SvgDrawingBounds().ViewportClip(node);
        if (!clip.Success)
        {
            new SvgAccessibilityTreeBuilder().Fail(work.Result, SvgAccessibilityTreeError.GeometryFailure, "Accessible viewport geometry could not be calculated.", node.SourceNode);
            return null;
        }
        if (clip.Rectangle != null)
        {
            SvgRectOperationResult intersection = bounds.Intersect(clip.Rectangle);
            if (!intersection.Success)
            {
                new SvgAccessibilityTreeBuilder().Fail(work.Result, SvgAccessibilityTreeError.GeometryFailure, "Accessible geometry could not be clipped.", node.SourceNode);
                return null;
            }
            bounds = intersection.Rectangle;
        }
        return bounds;
    }
SvgRect? TextBounds(SvgResolvedNode node, SvgMatrix? world, SvgAccessibilityTreeWork work)
    {
        string text = new SvgAccessibilityTreeBuilder().Normalize(node.Text);
        if (text.Length == 0) return null;
        double size = node.LengthContext != null && node.LengthContext.HasFontSize && SvgFiniteMath.IsFinite(node.LengthContext.FontSize) && node.LengthContext.FontSize > 0 ? node.LengthContext.FontSize : 16;
        double x = new SvgAccessibilityTreeBuilder().TextPosition(node, SvgAttributeNames.X, SvgLengthPercentageBasis.ViewportWidth);
        double y = new SvgAccessibilityTreeBuilder().TextPosition(node, SvgAttributeNames.Y, SvgLengthPercentageBasis.ViewportHeight);
        double width = size * 0.625 * text.Length;
        if (!SvgFiniteMath.IsFinite(width))
        {
            new SvgAccessibilityTreeBuilder().Fail(work.Result, SvgAccessibilityTreeError.GeometryFailure, "Accessible text geometry is non-finite.", node.SourceNode);
            return null;
        }
        SvgRect local = SvgRect.Create(x, y - size, width, size, width == 0);
        SvgRectTransformResult transformed = (world ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformRect(local);
        if (!transformed.Success)
        {
            new SvgAccessibilityTreeBuilder().Fail(work.Result, SvgAccessibilityTreeError.GeometryFailure, "Accessible text geometry could not be transformed.", node.SourceNode);
            return null;
        }
        return transformed.Rectangle;
    }
double TextPosition(SvgResolvedNode node, string name, SvgLengthPercentageBasis basis)
    {
        if (node.SourceNode == null || node.LengthContext == null) return 0;
        string value = "";
        int index = 0;
        while (index < node.SourceNode.Attributes.Count)
        {
            SvgAttribute attribute = node.SourceNode.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name)
            {
                value = attribute.Value;
                break;
            }
            index = index + 1;
        }
        if (value.Length == 0) return 0;
        SvgLengthListParseResult parsed = new SvgLengthListParser().ParseTextPosition(value, 100000, 8192);
        if (!parsed.Success || parsed.Value == null || parsed.Value.Items.Count == 0) return 0;
        SvgLengthEvaluationResult evaluated = parsed.Value.Items[0].Length.Evaluate(node.LengthContext, basis);
        return evaluated.Success ? evaluated.Value : 0;
    }
bool PrepareFocus(SvgResolvedDocument document, SvgAccessibilityTreeWork work)
    {
        SvgNavigationOptions options = new SvgNavigationOptions();
        options.MaximumNodes = work.Options.MaximumNodes;
        SvgFocusOrderResult focus = new SvgNavigation().FocusOrderWithDocumentAndOptions(document, options);
        work.Focus = focus;
        new SvgAccessibilityTreeBuilder().CopyDiagnostics(focus.Diagnostics, work.Result.Diagnostics);
        if (!focus.Success)
        {
            SvgAccessibilityTreeError error = focus.Error == SvgNavigationError.LimitExceeded ? SvgAccessibilityTreeError.LimitExceeded : SvgAccessibilityTreeError.InvalidArgument;
            new SvgAccessibilityTreeBuilder().Fail(work.Result, error, "Focusability could not be calculated within the accessibility-tree bounds.", (document.SourceDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DocumentElement);
            return false;
        }
        int index = 0;
        while (index < focus.Entries.Count)
        {
            SvgFocusEntry entry = focus.Entries[index];
            if (!work.FocusEntries.ContainsKey(entry.Node)) work.FocusEntries.Add(entry.Node, entry);
            index = index + 1;
        }
        return true;
    }
void PopulateFocusOrder(SvgAccessibilityTreeWork work)
    {
        int index = 0;
        while (work.Focus != null && work.Focus.Success && index < work.Focus.TabOrder.Count)
        {
            SvgAccessibilityTreeNode? accessible;
            if (work.AccessibleNodes.TryGetValue(work.Focus.TabOrder[index].Node, out accessible)) work.Result.FocusOrder.Add(accessible);
            index = index + 1;
        }
    }
void CopyDiagnostics(System.Collections.Generic.List<SvgDiagnostic>? source, System.Collections.Generic.List<SvgDiagnostic> target)
    {
        if (source == null) return;
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }
SvgRect Copy(SvgRect source)
    {
        return SvgRect.Create(source.X, source.Y, source.Width, source.Height, source.IsEmpty);
    }
bool Valid(SvgResolvedDocument document, SvgAccessibilityTreeOptions options)
    {
        return document != null && document.SourceDocument != null && document.Root != null && document.Nodes != null && options != null && options.MaximumNodes > 0 && options.MaximumTextCharacters > 0 && options.Semantics != null && options.Semantics.MaximumNodes > 0 && options.Semantics.MaximumTextCharacters > 0 && options.Semantics.MaximumReferences > 0;
    }
SvgAccessibilityTree Fail(SvgAccessibilityTree result, SvgAccessibilityTreeError error, string message, SvgNode? source)
    {
        if (result.Error == SvgAccessibilityTreeError.None) result.Error = error;
        result.Success = false;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, error == SvgAccessibilityTreeError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue, message, source == null ? null : source.SourceSpan));
        return result;
    }
}
