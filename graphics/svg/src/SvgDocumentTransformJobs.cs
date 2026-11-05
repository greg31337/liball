namespace svg;

internal enum SvgColorRewriteMode
{
    Recolor = 0,
    Theme = 1
}

internal class SvgColorTextResult
{
    internal bool Valid;
    internal bool Changed;
    internal string Text;

    internal SvgColorTextResult(bool valid, bool changed, string text)
    {
        Valid = valid;
        Changed = changed;
        Text = text;
    }
}

internal class SvgCssTextReplacement
{
    internal int Start;
    internal int End;
    internal string Text;

    internal SvgCssTextReplacement(int start, int end, string text)
    {
        Start = start;
        End = end;
        Text = text;
    }
}

internal static class SvgDocumentTransformJobs
{
    internal static void Recolor(SvgProcessContext context, SvgRecolorProcessOptions options)
    {
        if (!ValidRecolor(options))
        {
            context.Fail("Document recolor settings are invalid.");
            return;
        }
        if (options.ColorProvider == null) return;
        RewriteColors(context, options, null, SvgColorRewriteMode.Recolor);
    }

    internal static void ConvertThemeColors(SvgProcessContext context, SvgThemeColorProcessOptions options)
    {
        if (!ValidTheme(options))
        {
            context.Fail("Theme color conversion settings are invalid.");
            return;
        }
        RewriteColors(context, null, options, SvgColorRewriteMode.Theme);
    }

    internal static void ResizeViewBox(SvgProcessContext context, SvgViewBoxResizeOptions options)
    {
        if (!ValidResize(options))
        {
            context.Fail("View-box resize settings are invalid.");
            return;
        }
        SvgNode? root = context.Document.DocumentElement;
        if (root == null || root.ElementKind != SvgElementKind.Svg) return;
        if (options.Mode == SvgViewBoxResizeMode.ResizeViewport)
        {
            SetAttribute(context, root, SvgAttributeNames.Width, Number(options.ViewportWidth));
            if (!context.IsFailed()) SetAttribute(context, root, SvgAttributeNames.Height, Number(options.ViewportHeight));
            return;
        }
        SvgViewBox? result = options.ViewBox;
        if (options.Mode == SvgViewBoxResizeMode.FitViewBox || options.Mode == SvgViewBoxResizeMode.CropViewBox)
        {
            SvgViewBox? current = CurrentViewBox(root, options.MaximumTokenCharacters);
            if (current == null || current.Width <= 0 || current.Height <= 0) return;
            result = AspectViewBox(current, options.TargetAspectRatio, options.AlignX, options.AlignY, options.Mode == SvgViewBoxResizeMode.FitViewBox);
        }
        if (result != null && result.Width > 0 && result.Height > 0) SetAttribute(context, root, SvgAttributeNames.ViewBox, result.ToText());
    }

    internal static void ConvertCoordinateSystem(SvgProcessContext context, SvgCoordinateSystemProcessOptions options)
    {
        if (!ValidCoordinates(options))
        {
            context.Fail("Coordinate-system conversion settings are invalid.");
            return;
        }
        SvgNode? root = context.Document.DocumentElement;
        if (root == null || root.ElementKind != SvgElementKind.Svg || Identity(options.Transform)) return;
        string transform = MatrixText(options.Transform);
        System.Collections.Generic.List<SvgNode> children = GraphicalRootChildren(root);
        if (children.Count > options.MaximumChildren)
        {
            context.Fail("Coordinate-system conversion exceeded the configured child bound.");
            return;
        }
        if (AlreadyWrapped(children, transform)) return;
        if (children.Count == 0) return;
        SvgNode group = context.Documents.CreateElement(SvgElementNames.G, "", SvgElementNames.G, root.NamespaceUri);
        AppendAttribute(context, group, SvgAttributeNames.Transform, transform);
        if (context.IsFailed()) return;
        int insertion = ChildIndex(root, children[0]);
        SvgMutationResult inserted = context.Mutations.InsertChild(root, insertion, group);
        if (!inserted.Success)
        {
            context.Fail(inserted.Message);
            return;
        }
        int index = 0;
        while (index < children.Count && !context.IsFailed())
        {
            SvgMutationResult moved = context.Mutations.MoveChild(children[index], group, group.Children.Count);
            if (!moved.Success) context.Fail(moved.Message);
            index = index + 1;
        }
        if (!context.IsFailed() && options.TransformViewBox)
        {
            SvgViewBox? target = options.TargetViewBox;
            if (target == null)
            {
                SvgViewBox? current = CurrentViewBox(root, 4096);
                if (current != null)
                {
                    SvgRectTransformResult transformed = options.Transform.TransformRect(new SvgRect(current.X, current.Y, current.Width, current.Height));
                    if (transformed.Success) target = new SvgViewBox((transformed.Rectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, transformed.Rectangle.Y, transformed.Rectangle.Width, transformed.Rectangle.Height);
                    else context.Fail("The transformed view box is not finite.");
                }
            }
            if (!context.IsFailed() && target != null && target.Width > 0 && target.Height > 0) SetAttribute(context, root, SvgAttributeNames.ViewBox, target.ToText());
        }
    }

    internal static void CropDrawingBounds(SvgProcessContext context, SvgDrawingBoundsCropOptions options)
    {
        if (!ValidBoundsCrop(options))
        {
            context.Fail("Drawing-bounds crop settings are invalid.");
            return;
        }
        SvgNode? root = context.Document.DocumentElement;
        if (root == null || root.ElementKind != SvgElementKind.Svg) return;
        SvgResolvedDocument resolved = new SvgDocumentResolver().ResolveWithOptions(context.Document, options.ResolveOptions);
        if (!resolved.Success)
        {
            context.Fail("The document could not be resolved for drawing-bounds cropping.");
            return;
        }
        SvgDocumentBoundsResult bounds = new SvgDrawingBounds().Document(resolved, options.BoundsOptions);
        if (!bounds.Success)
        {
            context.Fail("Document drawing bounds could not be computed.");
            return;
        }
        if (bounds.Bounds == null || bounds.Bounds.IsEmpty) return;
        SvgMatrixOperationResult inverse = (resolved.Root ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform.Invert();
        if (!inverse.Success)
        {
            context.Fail("The root viewport transform could not be inverted.");
            return;
        }
        SvgRectTransformResult local = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformRect(bounds.Bounds);
        if (!local.Success)
        {
            context.Fail("Drawing bounds could not be converted to root coordinates.");
            return;
        }
        SvgRectOperationResult expanded = (local.Rectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ExpandAxes(options.HorizontalPadding, options.VerticalPadding);
        if (!expanded.Success || (expanded.Rectangle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IsEmpty || expanded.Rectangle.Width <= 0 || expanded.Rectangle.Height <= 0)
        {
            context.Fail("The padded drawing bounds are invalid.");
            return;
        }
        SvgViewBox crop = new SvgViewBox(expanded.Rectangle.X, expanded.Rectangle.Y, expanded.Rectangle.Width, expanded.Rectangle.Height);
        SetAttribute(context, root, SvgAttributeNames.ViewBox, crop.ToText());
        if (options.ResizeViewport && !context.IsFailed())
        {
            SetAttribute(context, root, SvgAttributeNames.Width, Number(crop.Width));
            if (!context.IsFailed()) SetAttribute(context, root, SvgAttributeNames.Height, Number(crop.Height));
        }
    }

    static void RewriteColors(SvgProcessContext context, SvgRecolorProcessOptions? recolor, SvgThemeColorProcessOptions? theme, SvgColorRewriteMode mode)
    {
        int maximumNodes = mode == SvgColorRewriteMode.Recolor ? (recolor ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumNodes : (theme ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumNodes;
        bool presentation = mode == SvgColorRewriteMode.Recolor ? (recolor ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IncludePresentationAttributes : (theme ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IncludePresentationAttributes;
        bool styles = mode == SvgColorRewriteMode.Recolor ? (recolor ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IncludeStyleAttributes : (theme ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IncludeStyleAttributes;
        bool sheets = mode == SvgColorRewriteMode.Recolor ? (recolor ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IncludeStyleSheets : (theme ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IncludeStyleSheets;
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        if (nodes.Count > maximumNodes)
        {
            context.Fail("Color conversion exceeded the configured node bound.");
            return;
        }
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.Element)
            {
                if (presentation) RewritePresentationAttributes(context, node, recolor, theme, mode);
                if (styles && !context.IsFailed()) RewriteStyleAttribute(context, node, recolor, theme, mode);
                if (sheets && !context.IsFailed() && node.ElementKind == SvgElementKind.Style) RewriteStyleSheet(context, node, recolor, theme, mode);
            }
            index = index + 1;
        }
    }

    static void RewritePresentationAttributes(SvgProcessContext context, SvgNode node, SvgRecolorProcessOptions? recolor, SvgThemeColorProcessOptions? theme, SvgColorRewriteMode mode)
    {
        System.Collections.Generic.List<SvgAttribute> attributes = new System.Collections.Generic.List<SvgAttribute>(node.Attributes);
        int index = 0;
        while (index < attributes.Count && !context.IsFailed())
        {
            SvgAttribute attribute = attributes[index];
            if (attribute.NamespaceUri.Length == 0 && ColorProperty(attribute.LocalName))
            {
                SvgColorTextResult rewritten = RewriteProperty(attribute.LocalName, attribute.Value, recolor, theme, mode);
                if (!rewritten.Valid) context.Fail("A recolor callback returned an invalid color.");
                else if (rewritten.Changed) ReplaceAttribute(context, node, attribute, rewritten.Text);
            }
            index = index + 1;
        }
    }

    static void RewriteStyleAttribute(SvgProcessContext context, SvgNode node, SvgRecolorProcessOptions? recolor, SvgThemeColorProcessOptions? theme, SvgColorRewriteMode mode)
    {
        SvgAttribute? style = Attribute(node, SvgAttributeNames.Style);
        if (style == null) return;
        string? rewritten = RewriteDeclarations(style.Value, new SvgCssParser().ParseStyleAttributeWithValueAndLimits(style.Value, new SvgCssLimits()), recolor, theme, mode);
        if (rewritten == null) return;
        if (rewritten.Length == 0 && style.Value.Length != 0) ReplaceAttribute(context, node, style, rewritten);
        else if (rewritten != style.Value) ReplaceAttribute(context, node, style, rewritten);
    }

    static void RewriteStyleSheet(SvgProcessContext context, SvgNode node, SvgRecolorProcessOptions? recolor, SvgThemeColorProcessOptions? theme, SvgColorRewriteMode mode)
    {
        string source = new SvgNodeApi().GetTextContent(node);
        if (source.Length == 0) return;
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(source, new SvgCssLimits());
        if (!parsed.Success || parsed.StyleSheet == null) return;
        System.Collections.Generic.List<SvgCssDeclaration> declarations = new System.Collections.Generic.List<SvgCssDeclaration>();
        CollectRuleDeclarations(parsed.StyleSheet.Rules, declarations);
        System.Collections.Generic.List<SvgCssTextReplacement>? replacements = DeclarationReplacements(source, declarations, recolor, theme, mode);
        if (replacements == null)
        {
            context.Fail("A recolor callback returned an invalid color.");
            return;
        }
        if (replacements.Count == 0) return;
        replacements.Sort((left, right) => right.Start.CompareTo(left.Start));
        System.Text.StringBuilder builder = new System.Text.StringBuilder(source);
        int index = 0;
        while (index < replacements.Count)
        {
            SvgCssTextReplacement replacement = replacements[index];
            builder.Remove(replacement.Start, replacement.End - replacement.Start);
            builder.Insert(replacement.Start, replacement.Text);
            index = index + 1;
        }
        SvgMutationResult mutation = context.Mutations.SetTextContent(node, builder.ToString());
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static string? RewriteDeclarations(string source, SvgCssParseResult parsed, SvgRecolorProcessOptions? recolor, SvgThemeColorProcessOptions? theme, SvgColorRewriteMode mode)
    {
        if (!parsed.Success) return null;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool changed = false;
        int index = 0;
        while (index < parsed.Declarations.Count)
        {
            SvgCssDeclaration declaration = parsed.Declarations[index];
            SvgColorTextResult value = RewriteProperty(declaration.Name, declaration.ValueText.Trim(), recolor, theme, mode);
            if (!value.Valid) return null;
            if (value.Changed)
            {
                AppendDeclaration(builder, declaration, value.Text);
                changed = true;
            }
            else builder.Append(declaration.RawText);
            index = index + 1;
        }
        return changed ? builder.ToString() : source;
    }

    static System.Collections.Generic.List<SvgCssTextReplacement>? DeclarationReplacements(string source, System.Collections.Generic.List<SvgCssDeclaration> declarations, SvgRecolorProcessOptions? recolor, SvgThemeColorProcessOptions? theme, SvgColorRewriteMode mode)
    {
        System.Collections.Generic.List<SvgCssTextReplacement> result = new System.Collections.Generic.List<SvgCssTextReplacement>();
        int index = 0;
        while (index < declarations.Count)
        {
            SvgCssDeclaration declaration = declarations[index];
            SvgColorTextResult value = RewriteProperty(declaration.Name, declaration.ValueText.Trim(), recolor, theme, mode);
            if (!value.Valid) return null;
            if (value.Changed && declaration.Source != null && declaration.Source.StartOffset >= 0 && declaration.Source.EndOffset <= source.Length)
            {
                System.Text.StringBuilder replacement = new System.Text.StringBuilder();
                AppendDeclaration(replacement, declaration, value.Text);
                result.Add(new SvgCssTextReplacement((int)declaration.Source.StartOffset, (int)declaration.Source.EndOffset, replacement.ToString()));
            }
            index = index + 1;
        }
        return result;
    }

    static SvgColorTextResult RewriteProperty(string property, string value, SvgRecolorProcessOptions? recolor, SvgThemeColorProcessOptions? theme, SvgColorRewriteMode mode)
    {
        if (!ColorProperty(property) && !property.StartsWith("--", System.StringComparison.Ordinal)) return Unchanged(value);
        if (mode == SvgColorRewriteMode.Theme && property == (theme ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).CustomPropertyName) return Unchanged(value);
        if (PaintProperty(property))
        {
            int maximum = mode == SvgColorRewriteMode.Recolor ? (recolor ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumTokenCharacters : (theme ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumTokenCharacters;
            SvgPaintParseResult parsed = new SvgPaintParser().Parse(value, 32, maximum, false);
            if (!parsed.Success || parsed.Paint == null) return Unchanged(value);
            return RewritePaint(parsed.Paint, recolor, theme, mode);
        }
        int tokenLimit = mode == SvgColorRewriteMode.Recolor ? (recolor ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumTokenCharacters : (theme ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MaximumTokenCharacters;
        SvgColorParseResult color = new SvgColorParser().Parse(value, tokenLimit);
        if (!color.Success || color.Color == null || color.Color.IsCurrentColor) return Unchanged(value);
        return RewriteColor(color.Color, recolor, theme, mode);
    }

    static SvgColorTextResult RewritePaint(SvgPaint paint, SvgRecolorProcessOptions? recolor, SvgThemeColorProcessOptions? theme, SvgColorRewriteMode mode)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool changed = false;
        SvgColorTextResult first = PaintAtom(paint, recolor, theme, mode);
        if (!first.Valid) return first;
        builder.Append(first.Text);
        changed = first.Changed;
        int index = 0;
        while (index < paint.Fallbacks.Count)
        {
            SvgColorTextResult fallback = PaintAtom(paint.Fallbacks[index], recolor, theme, mode);
            if (!fallback.Valid) return fallback;
            builder.Append(' ');
            builder.Append(fallback.Text);
            if (fallback.Changed) changed = true;
            index = index + 1;
        }
        return new SvgColorTextResult(true, changed, changed ? builder.ToString() : paint.Source);
    }

    static SvgColorTextResult PaintAtom(SvgPaint paint, SvgRecolorProcessOptions? recolor, SvgThemeColorProcessOptions? theme, SvgColorRewriteMode mode)
    {
        if (paint.Kind == SvgPaintKind.Color && paint.Color != null && !paint.Color.IsCurrentColor) return RewriteColor(paint.Color, recolor, theme, mode);
        if (paint.Kind == SvgPaintKind.None) return Unchanged("none");
        if (paint.Kind == SvgPaintKind.ContextFill) return Unchanged("context-fill");
        if (paint.Kind == SvgPaintKind.ContextStroke) return Unchanged("context-stroke");
        if (paint.Kind == SvgPaintKind.Url) return Unchanged("url(\"" + EscapeUrl(paint.Url) + "\")");
        return Unchanged(paint.Source);
    }

    static SvgColorTextResult RewriteColor(SvgColor color, SvgRecolorProcessOptions? recolor, SvgThemeColorProcessOptions? theme, SvgColorRewriteMode mode)
    {
        if (mode == SvgColorRewriteMode.Recolor)
        {
            SvgColor replacement = ((recolor ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ColorProvider ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))(color);
            if (replacement == null) return Unchanged(color.ToText());
            string text = replacement.ToText();
            if (text.Length == 0) return new SvgColorTextResult(false, false, "");
            string original = color.ToText();
            return new SvgColorTextResult(true, text != original, text);
        }
        if ((theme ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceColor != null && !SameColor(color, theme.SourceColor)) return Unchanged(color.ToText());
        string source = color.ToText();
        if (theme.Mode == SvgThemeColorMode.CurrentColor) return new SvgColorTextResult(true, source != "currentColor", "currentColor");
        string custom = "var(" + theme.CustomPropertyName;
        if (theme.PreserveFallback) custom = custom + ", " + source;
        custom = custom + ")";
        return new SvgColorTextResult(true, true, custom);
    }

    static void CollectRuleDeclarations(System.Collections.Generic.List<SvgCssRule> rules, System.Collections.Generic.List<SvgCssDeclaration> result)
    {
        int index = 0;
        while (index < rules.Count)
        {
            SvgCssRule rule = rules[index];
            AddDeclarations(rule.Declarations, result);
            if (rule.AtRule != null && rule.AtRule.FontFace != null) AddDeclarations(rule.AtRule.FontFace.Descriptors, result);
            if (rule.AtRule != null && rule.AtRule.Keyframes != null)
            {
                int keyframe = 0;
                while (keyframe < rule.AtRule.Keyframes.Keyframes.Count)
                {
                    AddDeclarations(rule.AtRule.Keyframes.Keyframes[keyframe].Declarations, result);
                    keyframe = keyframe + 1;
                }
            }
            CollectRuleDeclarations(rule.ChildRules, result);
            index = index + 1;
        }
    }

    static void AddDeclarations(System.Collections.Generic.List<SvgCssDeclaration> source, System.Collections.Generic.List<SvgCssDeclaration> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    static void AppendDeclaration(System.Text.StringBuilder builder, SvgCssDeclaration declaration, string value)
    {
        builder.Append(declaration.RawName);
        builder.Append(':');
        builder.Append(value);
        if (declaration.Important) builder.Append("!important");
        builder.Append(';');
    }

    static SvgViewBox AspectViewBox(SvgViewBox current, double ratio, double alignX, double alignY, bool fit)
    {
        double currentRatio = current.Width / current.Height;
        double width = current.Width;
        double height = current.Height;
        if (fit && currentRatio > ratio || !fit && currentRatio < ratio) height = width / ratio;
        else width = height * ratio;
        double x = current.X + (current.Width - width) * alignX;
        double y = current.Y + (current.Height - height) * alignY;
        return new SvgViewBox(x, y, width, height);
    }

    static SvgViewBox? CurrentViewBox(SvgNode root, int maximum)
    {
        SvgAttribute? attribute = Attribute(root, SvgAttributeNames.ViewBox);
        if (attribute == null) return null;
        SvgViewBoxParseResult parsed = SvgViewBoxParser.ParseViewBox(attribute.Value, maximum);
        return parsed.Success ? parsed.ViewBox : null;
    }

    static System.Collections.Generic.List<SvgNode> GraphicalRootChildren(SvgNode root)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < root.Children.Count)
        {
            SvgNode child = root.Children[index];
            if (child.Kind == SvgNodeKind.Element && child.ElementKind != SvgElementKind.Title && child.ElementKind != SvgElementKind.Desc && child.ElementKind != SvgElementKind.Metadata && child.ElementKind != SvgElementKind.Defs && child.ElementKind != SvgElementKind.Style) result.Add(child);
            index = index + 1;
        }
        return result;
    }

    static bool AlreadyWrapped(System.Collections.Generic.List<SvgNode> children, string transform)
    {
        if (children.Count != 1 || children[0].ElementKind != SvgElementKind.G) return false;
        SvgAttribute? current = Attribute(children[0], SvgAttributeNames.Transform);
        return current != null && current.Value == transform;
    }

    static bool ColorProperty(string name)
    {
        return name == SvgCssPropertyNames.Color || name == SvgCssPropertyNames.Fill || name == SvgCssPropertyNames.Stroke || name == SvgCssPropertyNames.StopColor || name == SvgCssPropertyNames.FloodColor || name == SvgCssPropertyNames.LightingColor || name == SvgCssPropertyNames.TextDecorationColor;
    }

    static bool PaintProperty(string name)
    {
        return name == SvgCssPropertyNames.Fill || name == SvgCssPropertyNames.Stroke;
    }

    static bool SameColor(SvgColor? first, SvgColor? second)
    {
        if (first == null || second == null || first.IsCurrentColor || second.IsCurrentColor) return false;
        SvgColorOperationResult a = first.ConvertTo(SvgColorSpace.Srgb);
        SvgColorOperationResult b = second.ConvertTo(SvgColorSpace.Srgb);
        if (!a.Success || !b.Success) return false;
        double tolerance = 0.000000001;
        return System.Math.Abs((a.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red - (b.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red) <= tolerance && System.Math.Abs(a.Color.Green - b.Color.Green) <= tolerance && System.Math.Abs(a.Color.Blue - b.Color.Blue) <= tolerance && System.Math.Abs(a.Color.Alpha - b.Color.Alpha) <= tolerance;
    }

    static bool ValidRecolor(SvgRecolorProcessOptions options)
    {
        return options != null && options.MaximumNodes > 0 && options.MaximumTokenCharacters > 0;
    }

    static bool ValidTheme(SvgThemeColorProcessOptions? options)
    {
        if (options == null || options.Mode < SvgThemeColorMode.CurrentColor || options.Mode > SvgThemeColorMode.CustomProperty || options.MaximumNodes < 1 || options.MaximumTokenCharacters < 1) return false;
        if (options.SourceColor != null && options.SourceColor.ToText().Length == 0) return false;
        return options.Mode != SvgThemeColorMode.CustomProperty || ValidCustomPropertyName(options.CustomPropertyName);
    }

    static bool ValidCustomPropertyName(string? value)
    {
        if (value == null || value.Length < 3 || value[0] != '-' || value[1] != '-') return false;
        int index = 2;
        while (index < value.Length)
        {
            char current = value[index];
            if (!(current == '-' || current == '_' || current >= 'a' && current <= 'z' || current >= 'A' && current <= 'Z' || current >= '0' && current <= '9')) return false;
            index = index + 1;
        }
        return true;
    }

    static bool ValidResize(SvgViewBoxResizeOptions? options)
    {
        if (options == null || options.Mode < SvgViewBoxResizeMode.ResizeViewport || options.Mode > SvgViewBoxResizeMode.CropViewBox || options.MaximumTokenCharacters < 1) return false;
        if (!SvgFiniteMath.IsFinite(options.AlignX) || !SvgFiniteMath.IsFinite(options.AlignY) || options.AlignX < 0 || options.AlignX > 1 || options.AlignY < 0 || options.AlignY > 1) return false;
        if (options.Mode == SvgViewBoxResizeMode.ResizeViewport) return SvgFiniteMath.IsFinite(options.ViewportWidth) && SvgFiniteMath.IsFinite(options.ViewportHeight) && options.ViewportWidth > 0 && options.ViewportHeight > 0;
        if (options.Mode == SvgViewBoxResizeMode.SetViewBox) return options.ViewBox == null || options.ViewBox.Width > 0 && options.ViewBox.Height > 0;
        return SvgFiniteMath.IsFinite(options.TargetAspectRatio) && options.TargetAspectRatio > 0;
    }

    static bool ValidCoordinates(SvgCoordinateSystemProcessOptions options)
    {
        return options != null && options.Transform != null && options.MaximumChildren > 0 && SvgFiniteMath.IsFinite(options.Transform.A) && SvgFiniteMath.IsFinite(options.Transform.B) && SvgFiniteMath.IsFinite(options.Transform.C) && SvgFiniteMath.IsFinite(options.Transform.D) && SvgFiniteMath.IsFinite(options.Transform.E) && SvgFiniteMath.IsFinite(options.Transform.F) && (options.TargetViewBox == null || options.TargetViewBox.Width > 0 && options.TargetViewBox.Height > 0);
    }

    static bool ValidBoundsCrop(SvgDrawingBoundsCropOptions options)
    {
        return options != null && options.ResolveOptions != null && options.BoundsOptions != null && SvgFiniteMath.IsFinite(options.HorizontalPadding) && SvgFiniteMath.IsFinite(options.VerticalPadding) && options.HorizontalPadding >= 0 && options.VerticalPadding >= 0;
    }

    static bool Identity(SvgMatrix matrix)
    {
        return matrix.A == 1 && matrix.B == 0 && matrix.C == 0 && matrix.D == 1 && matrix.E == 0 && matrix.F == 0;
    }

    static string MatrixText(SvgMatrix matrix)
    {
        return "matrix(" + Number(matrix.A) + " " + Number(matrix.B) + " " + Number(matrix.C) + " " + Number(matrix.D) + " " + Number(matrix.E) + " " + Number(matrix.F) + ")";
    }

    static string Number(double value)
    {
        SvgNumberFormatResult result = SvgNumberFormatter.FormatCompact(value);
        return result.Success ? result.Text : "0";
    }

    static string EscapeUrl(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\\' || current == '"') result.Append('\\');
            result.Append(current);
            index = index + 1;
        }
        return result.ToString();
    }

    static SvgColorTextResult Unchanged(string value)
    {
        return new SvgColorTextResult(true, false, value);
    }

    static void SetAttribute(SvgProcessContext context, SvgNode node, string name, string value)
    {
        SvgAttribute? current = Attribute(node, name);
        if (current != null && current.Value == value) return;
        SvgAttribute replacement = context.Documents.CreateAttribute(name, "", name, "", value);
        SvgMutationResult mutation = current == null ? context.Mutations.AppendAttribute(node, replacement) : context.Mutations.ReplaceAttribute(node, current, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void AppendAttribute(SvgProcessContext context, SvgNode node, string name, string value)
    {
        SvgAttribute attribute = context.Documents.CreateAttribute(name, "", name, "", value);
        SvgMutationResult mutation = new SvgNodeApi().AppendAttribute(node, attribute);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void ReplaceAttribute(SvgProcessContext context, SvgNode node, SvgAttribute current, string value)
    {
        if (current.Value == value) return;
        SvgAttribute replacement = context.Documents.CreateAttribute(current.QualifiedName, current.Prefix, current.LocalName, current.NamespaceUri, value);
        SvgMutationResult mutation = context.Mutations.ReplaceAttribute(node, current, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static SvgAttribute? Attribute(SvgNode node, string name)
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

    static int ChildIndex(SvgNode parent, SvgNode child)
    {
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (object.ReferenceEquals(parent.Children[index], child)) return index;
            index = index + 1;
        }
        return parent.Children.Count;
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
