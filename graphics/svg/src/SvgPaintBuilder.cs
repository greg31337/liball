namespace svg;

internal class SvgDefinitionCreateResult
{
    internal bool Success;
    internal SvgNode? Definition;
    internal string Id;
    internal SvgMutationError MutationError;
    internal string Message;

    internal SvgDefinitionCreateResult(bool success, SvgNode? definition, string id, SvgMutationError mutationError, string message)
    {
        Success = success;
        Definition = definition;
        Id = id;
        MutationError = mutationError;
        Message = message;
    }
}

internal static class SvgDefinitionBuilderSupport
{
    internal static SvgDefinitionCreateResult Create(SvgBuilder? builder, SvgDocumentApi? documents, SvgNodeApi? nodes, SvgElementKind kind, string? prefix)
    {
        if (builder == null || builder.Document == null || builder.Root == null || documents == null || nodes == null || prefix == null || prefix.Length == 0) return Failure(SvgMutationError.InvalidArgument, "A valid document builder and ID prefix are required.");
        string? localName = LocalName(kind);
        if (localName == null) return Failure(SvgMutationError.InvalidArgument, "The requested resource kind is not supported.");
        SvgNode? definitions = FindDefinitions(builder.Root);
        if (definitions == null)
        {
            definitions = documents.CreateElement(SvgElementNames.Defs, "", SvgElementNames.Defs, SvgNamespaces.Svg);
            SvgMutationResult inserted = nodes.InsertChild(builder.Root, 0, definitions);
            if (!inserted.Success) return Failure(inserted.Error, inserted.Message);
        }
        string id = AllocateId(builder.Document, prefix);
        SvgNode definition = documents.CreateElement(localName, "", localName, SvgNamespaces.Svg);
        SvgMutationResult attribute = SetAttribute(documents, nodes, definition, SvgAttributeNames.Id, id);
        if (!attribute.Success) return Failure(attribute.Error, attribute.Message);
        SvgMutationResult appended = nodes.AppendChild(definitions, definition);
        if (!appended.Success) return Failure(appended.Error, appended.Message);
        return new SvgDefinitionCreateResult(true, definition, id, SvgMutationError.None, "");
    }

    internal static SvgMutationResult SetAttribute(SvgDocumentApi? documents, SvgNodeApi? nodes, SvgNode? element, string name, string value)
    {
        SvgAttribute? current = FindAttribute(element, name);
        if (current != null && current.Value == value) return new SvgMutationResult(true, SvgMutationError.None, element, current, null, null, "");
        SvgAttribute replacement = (documents ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).CreateAttribute(name, "", name, "", value);
        return current == null ? (nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).AppendAttribute(element, replacement) : (nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ReplaceAttribute(element, current, replacement);
    }

    internal static bool ValidName(string value)
    {
        return value != null && value.Length != 0 && System.Xml.XmlConvert.EncodeLocalName(value) == value;
    }

    internal static string Url(string id)
    {
        return "url(#" + id + ")";
    }

    internal static bool StandardFilterInput(string value)
    {
        return value == "SourceGraphic" || value == "SourceAlpha" || value == "BackgroundImage" || value == "BackgroundAlpha" || value == "FillPaint" || value == "StrokePaint";
    }

    static SvgDefinitionCreateResult Failure(SvgMutationError error, string message)
    {
        return new SvgDefinitionCreateResult(false, null, "", error, message);
    }

    static SvgNode? FindDefinitions(SvgNode root)
    {
        int index = 0;
        while (index < root.Children.Count)
        {
            SvgNode child = root.Children[index];
            if (child.Kind == SvgNodeKind.Element && child.ElementKind == SvgElementKind.Defs) return child;
            index = index + 1;
        }
        return null;
    }

    static string AllocateId(SvgDocument document, string prefix)
    {
        int suffix = 1;
        string candidate = prefix + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        while (document.Index.GetById(candidate) != null)
        {
            suffix = suffix + 1;
            candidate = prefix + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    static SvgAttribute? FindAttribute(SvgNode? element, string name)
    {
        int index = 0;
        while (index < (element ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Attributes.Count)
        {
            SvgAttribute attribute = element.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name) return attribute;
            index = index + 1;
        }
        return null;
    }

    internal static string? LocalName(SvgElementKind kind)
    {
        if (kind == SvgElementKind.LinearGradient) return SvgElementNames.LinearGradient;
        if (kind == SvgElementKind.RadialGradient) return SvgElementNames.RadialGradient;
        if (kind == SvgElementKind.Pattern) return SvgElementNames.Pattern;
        if (kind == SvgElementKind.Marker) return SvgElementNames.Marker;
        if (kind == SvgElementKind.ClipPath) return SvgElementNames.ClipPath;
        if (kind == SvgElementKind.Mask) return SvgElementNames.Mask;
        if (kind == SvgElementKind.Filter) return SvgElementNames.Filter;
        if (kind == SvgElementKind.FeBlend) return SvgElementNames.FeBlend;
        if (kind == SvgElementKind.FeColorMatrix) return SvgElementNames.FeColorMatrix;
        if (kind == SvgElementKind.FeComponentTransfer) return SvgElementNames.FeComponentTransfer;
        if (kind == SvgElementKind.FeComposite) return SvgElementNames.FeComposite;
        if (kind == SvgElementKind.FeConvolveMatrix) return SvgElementNames.FeConvolveMatrix;
        if (kind == SvgElementKind.FeDiffuseLighting) return SvgElementNames.FeDiffuseLighting;
        if (kind == SvgElementKind.FeDisplacementMap) return SvgElementNames.FeDisplacementMap;
        if (kind == SvgElementKind.FeDropShadow) return SvgElementNames.FeDropShadow;
        if (kind == SvgElementKind.FeFlood) return SvgElementNames.FeFlood;
        if (kind == SvgElementKind.FeGaussianBlur) return SvgElementNames.FeGaussianBlur;
        if (kind == SvgElementKind.FeImage) return SvgElementNames.FeImage;
        if (kind == SvgElementKind.FeMerge) return SvgElementNames.FeMerge;
        if (kind == SvgElementKind.FeMergeNode) return SvgElementNames.FeMergeNode;
        if (kind == SvgElementKind.FeMorphology) return SvgElementNames.FeMorphology;
        if (kind == SvgElementKind.FeOffset) return SvgElementNames.FeOffset;
        if (kind == SvgElementKind.FeSpecularLighting) return SvgElementNames.FeSpecularLighting;
        if (kind == SvgElementKind.FeTile) return SvgElementNames.FeTile;
        if (kind == SvgElementKind.FeTurbulence) return SvgElementNames.FeTurbulence;
        if (kind == SvgElementKind.FeFuncA) return SvgElementNames.FeFuncA;
        if (kind == SvgElementKind.FeFuncB) return SvgElementNames.FeFuncB;
        if (kind == SvgElementKind.FeFuncG) return SvgElementNames.FeFuncG;
        if (kind == SvgElementKind.FeFuncR) return SvgElementNames.FeFuncR;
        if (kind == SvgElementKind.FeDistantLight) return SvgElementNames.FeDistantLight;
        if (kind == SvgElementKind.FePointLight) return SvgElementNames.FePointLight;
        if (kind == SvgElementKind.FeSpotLight) return SvgElementNames.FeSpotLight;
        return null;
    }
}
