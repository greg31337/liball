namespace svg;

internal static class SvgLossyProcessJobs
{
    internal static void ConvertShapes(SvgProcessContext context, SvgConvertShapesToPathsOptions options)
    {
        if (options == null || options.LengthContext == null || options.MaximumCommands < 1 || options.MaximumTokenCharacters < 1)
        {
            context.Fail("Shape conversion settings are invalid.");
            return;
        }
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (BasicShape(node)) ConvertShape(context, node, options);
            index = index + 1;
        }
    }

    internal static void BakeTransforms(SvgProcessContext context, SvgBakeTransformsOptions options)
    {
        if (options == null || options.LengthContext == null || options.MaximumCommands < 1 || options.MaximumTransforms < 1 || options.MaximumTokenCharacters < 1)
        {
            context.Fail("Transform baking settings are invalid.");
            return;
        }
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            SvgAttribute? transform = Attribute(node, SvgAttributeNames.Transform, "");
            if (transform != null && (node.ElementKind == SvgElementKind.Path || options.ConvertBasicShapes && BasicShape(node))) BakeTransform(context, node, transform, options);
            index = index + 1;
        }
    }

    internal static void SimplifyPaths(SvgProcessContext context, SvgPathSimplifyOptions options)
    {
        if (options == null)
        {
            context.Fail("Path simplification settings are invalid.");
            return;
        }
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            SvgAttribute? data = node.ElementKind == SvgElementKind.Path ? Attribute(node, SvgAttributeNames.D, "") : null;
            if (data != null)
            {
                SvgPathParseResult parsed = new SvgPathParser().Parse(data.Value, options.MaximumCommands, 4096);
                if (parsed.Success)
                {
                    SvgPathSimplifyResult simplified = new SvgPathSimplifier().SimplifyWithPathAndOptions((parsed.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), options);
                    if (!simplified.Success) context.Fail("A path could not be simplified within the configured bounds.");
                    else
                    {
                        SvgPathFormatResult formatted = new SvgPathFormatter().FormatMinifiedWithPath((simplified.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
                        if (!formatted.Success) context.Fail("Simplified path formatting failed.");
                        else if (formatted.Text != data.Value) ReplaceAttribute(context, node, data, formatted.Text);
                    }
                }
            }
            index = index + 1;
        }
    }

    internal static void ConvertText(SvgProcessContext context, SvgTextToPathProcessOptions options)
    {
        if (options == null || options.MaximumCommands < 1)
        {
            context.Fail("Text conversion settings are invalid.");
            return;
        }
        if (options.OutlineProvider == null) return;
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.ElementKind == SvgElementKind.Text && node.Parent != null)
            {
                SvgPathData outline = options.OutlineProvider(node);
                if (outline != null)
                {
                    if (!outline.IsValid || outline.Commands.Count > options.MaximumCommands) context.Fail("A caller-provided text outline is invalid or exceeds the command bound.");
                    else ReplaceText(context, node, outline, options);
                }
            }
            index = index + 1;
        }
    }

    internal static void FlattenEffects(SvgProcessContext context, SvgFlattenEffectsProcessOptions options)
    {
        if (options == null || options.MaximumNodes < 1)
        {
            context.Fail("Effect flattening settings are invalid.");
            return;
        }
        if (options.ImageProvider == null) return;
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        if (nodes.Count > options.MaximumNodes)
        {
            context.Fail("Effect flattening exceeded the configured node bound.");
            return;
        }
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.Parent != null && EffectBearing(node))
            {
                SvgFlattenedEffectImage image = options.ImageProvider(node);
                if (image != null)
                {
                    if (!ValidImage(image)) context.Fail("Caller-provided flattened effect imagery is invalid.");
                    else ReplaceWithImage(context, node, image);
                }
            }
            index = index + 1;
        }
    }

    internal static void EmbedImages(SvgProcessContext context, SvgEmbedImagesProcessOptions options)
    {
        if (options == null || options.MaximumBytes < 0)
        {
            context.Fail("Image embedding settings are invalid.");
            return;
        }
        if (options.ImageProvider == null) return;
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.ElementKind == SvgElementKind.Image)
            {
                SvgAttribute? href = PreferredHref(node);
                if (href != null && ExternalImage(href.Value))
                {
                    SvgEmbeddedImageData image = options.ImageProvider(node);
                    if (image != null)
                    {
                        if (image.Bytes == null || image.MediaType == null || image.MediaType.Length == 0) context.Fail("Caller-provided embedded image data is invalid.");
                        else
                        {
                            SvgDataUrlEncodeResult encoded = new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64AndMaxDecodedBytes(image.Bytes, image.MediaType, options.UseBase64, options.MaximumBytes);
                            if (!encoded.Success) context.Fail("External image data could not be encoded within the configured bound.");
                            else ReplaceImageHrefs(context, node, encoded.DataUrl);
                        }
                    }
                }
            }
            index = index + 1;
        }
    }

    static void ConvertShape(SvgProcessContext context, SvgNode node, SvgConvertShapesToPathsOptions options)
    {
        SvgShapeResolveOptions resolve = new SvgShapeResolveOptions();
        resolve.LengthContext = options.LengthContext;
        resolve.MaxTokenCharacters = options.MaximumTokenCharacters;
        SvgShapeResult shape = new SvgShapeResolver().ResolveWithNodeAndOptions(node, resolve);
        if (!shape.Success || !shape.HasGeometry) return;
        SvgShapePathResult converted = shape.ToPathWithLimit(options.MaximumCommands);
        if (!converted.Success)
        {
            context.Fail("A basic shape could not be converted within the configured command bound.");
            return;
        }
        ReplaceWithPath(context, node, converted.PathData, false, true);
    }

    static void BakeTransform(SvgProcessContext context, SvgNode node, SvgAttribute transform, SvgBakeTransformsOptions options)
    {
        SvgTransformParseResult parsedTransform = SvgTransformParser.Parse(transform.Value, options.MaximumTransforms, options.MaximumTokenCharacters);
        if (!parsedTransform.Success) return;
        SvgTransformMatrixResult matrix = parsedTransform.TransformList.ToMatrix();
        if (!matrix.Success)
        {
            context.Fail("A transform list could not be composed.");
            return;
        }
        SvgPathData? path = null;
        if (node.ElementKind == SvgElementKind.Path)
        {
            SvgAttribute? data = Attribute(node, SvgAttributeNames.D, "");
            SvgPathParseResult? parsedPath = data == null ? null : new SvgPathParser().Parse(data.Value, options.MaximumCommands, options.MaximumTokenCharacters);
            if (parsedPath != null && parsedPath.Success) path = parsedPath.PathData;
        }
        else
        {
            SvgShapeResolveOptions resolve = new SvgShapeResolveOptions();
            resolve.LengthContext = options.LengthContext;
            resolve.MaxTokenCharacters = options.MaximumTokenCharacters;
            SvgShapeResult shape = new SvgShapeResolver().ResolveWithNodeAndOptions(node, resolve);
            SvgShapePathResult? converted = shape.Success && shape.HasGeometry ? shape.ToPathWithLimit(options.MaximumCommands) : null;
            if (converted != null && converted.Success) path = converted.PathData;
        }
        if (path == null) return;
        SvgPathTransformOptions transformOptions = new SvgPathTransformOptions();
        transformOptions.MaximumCommands = options.MaximumCommands;
        transformOptions.PreserveArcs = options.PreserveArcs;
        SvgPathTransformResult transformed = new SvgPathTransformer().TransformWithPathAndMatrixAndOptions(path, (matrix.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), transformOptions);
        if (!transformed.Success) context.Fail("Path transformation exceeded the configured bounds.");
        else ReplaceWithPath(context, node, transformed.PathData, true, true);
    }

    static void ReplaceText(SvgProcessContext context, SvgNode node, SvgPathData outline, SvgTextToPathProcessOptions options)
    {
        SvgNode replacement = PathElement(context, node);
        CopyAttributes(context, node, replacement, true, false);
        if (options.PreserveAccessibleText && Attribute(node, "aria-label", "") == null && Attribute(node, "aria-labelledby", "") == null)
        {
            string text = new SvgNodeApi().GetTextContent(node);
            if (text.Length != 0) AppendAttribute(context, replacement, "aria-label", "", "aria-label", "", text);
        }
        AppendPathData(context, replacement, outline);
        ReplaceNode(context, node, replacement);
    }

    static void ReplaceWithPath(SvgProcessContext context, SvgNode node, SvgPathData? path, bool removeTransform, bool children)
    {
        SvgNode replacement = PathElement(context, node);
        CopyAttributes(context, node, replacement, false, removeTransform);
        AppendPathData(context, replacement, path);
        if (children) CopyChildren(context, node, replacement);
        ReplaceNode(context, node, replacement);
    }

    static SvgNode PathElement(SvgProcessContext context, SvgNode source)
    {
        string qualified = source.Prefix.Length == 0 ? SvgElementNames.Path : source.Prefix + ":" + SvgElementNames.Path;
        return context.Documents.CreateElement(qualified, source.Prefix, SvgElementNames.Path, source.NamespaceUri);
    }

    static void AppendPathData(SvgProcessContext context, SvgNode path, SvgPathData? data)
    {
        SvgPathFormatResult formatted = new SvgPathFormatter().FormatMinifiedWithPath((data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!formatted.Success)
        {
            context.Fail("Generated path formatting failed.");
            return;
        }
        AppendAttribute(context, path, SvgAttributeNames.D, "", SvgAttributeNames.D, "", formatted.Text);
    }

    static void CopyAttributes(SvgProcessContext context, SvgNode source, SvgNode target, bool text, bool removeTransform)
    {
        int index = 0;
        while (index < source.Attributes.Count)
        {
            SvgAttribute attribute = source.Attributes[index];
            bool remove = attribute.NamespaceUri.Length == 0 && (GeometryAttribute(source.ElementKind, attribute.LocalName) || attribute.LocalName == SvgAttributeNames.D || removeTransform && attribute.LocalName == SvgAttributeNames.Transform || text && TextAttribute(attribute.LocalName));
            if (!remove) AppendAttribute(context, target, attribute.QualifiedName, attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
            index = index + 1;
        }
    }

    static void CopyChildren(SvgProcessContext context, SvgNode source, SvgNode target)
    {
        int index = 0;
        SvgNodeApi nodes = new SvgNodeApi();
        while (index < source.Children.Count && !context.IsFailed())
        {
            SvgNode clone = nodes.CloneNode(source.Children[index], true);
            SvgMutationResult mutation = nodes.AppendChild(target, clone);
            if (!mutation.Success) context.Fail(mutation.Message);
            index = index + 1;
        }
    }

    static void ReplaceWithImage(SvgProcessContext context, SvgNode source, SvgFlattenedEffectImage image)
    {
        string qualified = source.Prefix.Length == 0 ? SvgElementNames.Image : source.Prefix + ":" + SvgElementNames.Image;
        SvgNode replacement = context.Documents.CreateElement(qualified, source.Prefix, SvgElementNames.Image, source.NamespaceUri);
        CopySemanticAttributes(context, source, replacement);
        AppendAttribute(context, replacement, SvgAttributeNames.X, "", SvgAttributeNames.X, "", Number(image.X));
        AppendAttribute(context, replacement, SvgAttributeNames.Y, "", SvgAttributeNames.Y, "", Number(image.Y));
        AppendAttribute(context, replacement, SvgAttributeNames.Width, "", SvgAttributeNames.Width, "", Number(image.Width));
        AppendAttribute(context, replacement, SvgAttributeNames.Height, "", SvgAttributeNames.Height, "", Number(image.Height));
        AppendAttribute(context, replacement, SvgAttributeNames.Href, "", SvgAttributeNames.Href, "", image.DataUrl);
        ReplaceNode(context, source, replacement);
    }

    static void CopySemanticAttributes(SvgProcessContext context, SvgNode source, SvgNode target)
    {
        int index = 0;
        while (index < source.Attributes.Count)
        {
            SvgAttribute attribute = source.Attributes[index];
            string name = attribute.LocalName;
            bool copy = attribute.NamespaceUri.Length == 0 && (name == SvgAttributeNames.Id || name == SvgAttributeNames.Transform || name == SvgAttributeNames.TabIndex || name == "role" || name.StartsWith("aria-", System.StringComparison.OrdinalIgnoreCase) || name.StartsWith("data-", System.StringComparison.OrdinalIgnoreCase));
            if (copy) AppendAttribute(context, target, attribute.QualifiedName, attribute.Prefix, name, attribute.NamespaceUri, attribute.Value);
            index = index + 1;
        }
    }

    static void ReplaceImageHrefs(SvgProcessContext context, SvgNode node, string dataUrl)
    {
        System.Collections.Generic.List<SvgAttribute> attributes = new System.Collections.Generic.List<SvgAttribute>(node.Attributes);
        int index = 0;
        while (index < attributes.Count && !context.IsFailed())
        {
            SvgAttribute attribute = attributes[index];
            if (attribute.LocalName == SvgAttributeNames.Href && (attribute.NamespaceUri.Length == 0 || attribute.NamespaceUri == SvgNamespaces.XLink) && ExternalImage(attribute.Value)) ReplaceAttribute(context, node, attribute, dataUrl);
            index = index + 1;
        }
    }

    static SvgAttribute? PreferredHref(SvgNode node)
    {
        SvgAttribute? result = Attribute(node, SvgAttributeNames.Href, "");
        return result == null ? Attribute(node, SvgAttributeNames.Href, SvgNamespaces.XLink) : result;
    }

    static bool ExternalImage(string value)
    {
        string trimmed = value.Trim();
        return trimmed.Length != 0 && trimmed[0] != '#' && !trimmed.StartsWith("data:", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool ValidImage(SvgFlattenedEffectImage image)
    {
        return image.DataUrl != null && image.DataUrl.StartsWith("data:", System.StringComparison.OrdinalIgnoreCase) && SvgFiniteMath.IsFinite(image.X) && SvgFiniteMath.IsFinite(image.Y) && SvgFiniteMath.IsFinite(image.Width) && SvgFiniteMath.IsFinite(image.Height) && image.Width > 0 && image.Height > 0;
    }

    static bool EffectBearing(SvgNode node)
    {
        if (node.Kind != SvgNodeKind.Element || node.ElementKind == SvgElementKind.Defs || node.ElementKind == SvgElementKind.Filter || node.ElementKind == SvgElementKind.Mask || node.ElementKind == SvgElementKind.ClipPath || node.ElementKind == SvgElementKind.Marker) return false;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && (attribute.LocalName == "filter" || attribute.LocalName == "mask" || attribute.LocalName == "clip-path" || attribute.LocalName == "marker-start" || attribute.LocalName == "marker-mid" || attribute.LocalName == "marker-end")) return true;
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == SvgAttributeNames.Style && (attribute.Value.IndexOf("filter", System.StringComparison.OrdinalIgnoreCase) >= 0 || attribute.Value.IndexOf("mask", System.StringComparison.OrdinalIgnoreCase) >= 0 || attribute.Value.IndexOf("clip-path", System.StringComparison.OrdinalIgnoreCase) >= 0 || attribute.Value.IndexOf("marker-", System.StringComparison.OrdinalIgnoreCase) >= 0)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool BasicShape(SvgNode? node)
    {
        if (node == null || node.Kind != SvgNodeKind.Element) return false;
        SvgElementKind kind = node.ElementKind;
        return kind == SvgElementKind.Circle || kind == SvgElementKind.Ellipse || kind == SvgElementKind.Line || kind == SvgElementKind.Polygon || kind == SvgElementKind.Polyline || kind == SvgElementKind.Rect;
    }

    static bool GeometryAttribute(SvgElementKind kind, string name)
    {
        if (kind == SvgElementKind.Circle) return name == "cx" || name == "cy" || name == "r";
        if (kind == SvgElementKind.Ellipse) return name == "cx" || name == "cy" || name == "rx" || name == "ry";
        if (kind == SvgElementKind.Line) return name == "x1" || name == "y1" || name == "x2" || name == "y2";
        if (kind == SvgElementKind.Polygon || kind == SvgElementKind.Polyline) return name == SvgAttributeNames.Points;
        if (kind == SvgElementKind.Rect) return name == SvgAttributeNames.X || name == SvgAttributeNames.Y || name == SvgAttributeNames.Width || name == SvgAttributeNames.Height || name == "rx" || name == "ry";
        return false;
    }

    static bool TextAttribute(string name)
    {
        return name == SvgAttributeNames.X || name == SvgAttributeNames.Y || name == "dx" || name == "dy" || name == "rotate" || name == "textLength" || name == "lengthAdjust" || name.StartsWith("font-", System.StringComparison.OrdinalIgnoreCase);
    }

    static string Number(double value)
    {
        SvgNumberFormatResult result = SvgNumberFormatter.FormatCompact(value);
        return result.Success ? result.Text : "0";
    }

    static void AppendAttribute(SvgProcessContext context, SvgNode node, string qualifiedName, string prefix, string localName, string namespaceUri, string value)
    {
        SvgAttribute attribute = context.Documents.CreateAttribute(qualifiedName, prefix, localName, namespaceUri, value);
        SvgMutationResult mutation = new SvgNodeApi().AppendAttribute(node, attribute);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void ReplaceAttribute(SvgProcessContext context, SvgNode node, SvgAttribute current, string value)
    {
        SvgAttribute replacement = context.Documents.CreateAttribute(current.QualifiedName, current.Prefix, current.LocalName, current.NamespaceUri, value);
        SvgMutationResult mutation = context.Mutations.ReplaceAttribute(node, current, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void ReplaceNode(SvgProcessContext context, SvgNode current, SvgNode replacement)
    {
        if (context.IsFailed()) return;
        SvgMutationResult mutation = context.Mutations.ReplaceChild(current.Parent, current, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static SvgAttribute? Attribute(SvgNode? node, string localName, string namespaceUri)
    {
        if (node == null || node.Kind != SvgNodeKind.Element) return null;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.LocalName == localName && attribute.NamespaceUri == namespaceUri) return attribute;
            index = index + 1;
        }
        return null;
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
