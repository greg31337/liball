namespace svg;

/// <summary>Contains one constant scene paint.</summary>
public class SvgSceneSolidPaintData
{
    /// <summary>The unpremultiplied paint color, or <c>null</c>.</summary>
    public SvgColor? Color;
    /// <summary>The clamped paint opacity.</summary>
    public double Opacity;

    /// <summary>Creates transparent unassigned solid paint.</summary>
    public SvgSceneSolidPaintData()
    {
        Color = null;
        Opacity = 1;
    }
}

/// <summary>Contains one normalized gradient stop.</summary>
public class SvgSceneGradientStopData
{
    /// <summary>The nondecreasing stop offset from zero through one.</summary>
    public double Offset;
    /// <summary>The unpremultiplied stop color, or <c>null</c>.</summary>
    public SvgColor? Color;
    /// <summary>The clamped effective stop alpha.</summary>
    public double Alpha;

    /// <summary>Creates an empty stop at offset zero.</summary>
    public SvgSceneGradientStopData()
    {
        Offset = 0;
        Color = null;
        Alpha = 1;
    }
}

/// <summary>Contains normalized linear or radial gradient geometry.</summary>
public class SvgSceneGradientPaintData
{
    /// <summary>The linear or radial gradient kind.</summary>
    public SvgPaintServerKind Kind;
    /// <summary>The gradient extension behavior.</summary>
    public SvgGradientSpreadMethod SpreadMethod;
    /// <summary>The sRGB or linear-sRGB interpolation space.</summary>
    public SvgColorSpace InterpolationSpace;
    /// <summary>The gradient-coordinate to local-user-space transform.</summary>
    public SvgMatrix Transform;
    /// <summary>The linear start point or radial end center.</summary>
    public SvgPoint StartOrCenter;
    /// <summary>The linear end point or radial focal center.</summary>
    public SvgPoint EndOrFocus;
    /// <summary>The radial end radius.</summary>
    public double Radius;
    /// <summary>The radial focal radius.</summary>
    public double FocalRadius;
    /// <summary>The ordered normalized gradient stops.</summary>
    public System.Collections.Generic.List<SvgSceneGradientStopData> Stops;

    /// <summary>Creates empty linear-gradient data.</summary>
    public SvgSceneGradientPaintData()
    {
        Kind = SvgPaintServerKind.LinearGradient;
        SpreadMethod = SvgGradientSpreadMethod.Pad;
        InterpolationSpace = SvgColorSpace.Srgb;
        Transform = Identity();
        StartOrCenter = new SvgPoint(0, 0);
        EndOrFocus = new SvgPoint(1, 0);
        Radius = 0;
        FocalRadius = 0;
        Stops = new System.Collections.Generic.List<SvgSceneGradientStopData>();
    }

    internal static SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }
}

/// <summary>Contains one normalized repeating pattern tile and content graph.</summary>
public class SvgScenePatternPaintData
{
    /// <summary>The tile bounds before the pattern transform.</summary>
    public SvgRect? TileBounds;
    /// <summary>The tile-coordinate to local-user-space transform.</summary>
    public SvgMatrix Transform;
    /// <summary>The pattern-content to tile-coordinate transform.</summary>
    public SvgMatrix ContentTransform;
    /// <summary>The optional pattern view box.</summary>
    public SvgViewBox? ViewBox;
    /// <summary>The effective preserve-aspect-ratio value.</summary>
    public SvgAspectRatio AspectRatio;
    /// <summary>The normalized pattern content root, or <c>null</c>.</summary>
    public SvgSceneNode? Content;

    /// <summary>Creates empty pattern data with identity transforms.</summary>
    public SvgScenePatternPaintData()
    {
        TileBounds = null;
        Transform = Identity();
        ContentTransform = Identity();
        ViewBox = null;
        AspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false);
        Content = null;
    }

    internal static SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }
}

/// <summary>Contains one shared encoded or host-provided image.</summary>
public class SvgSceneImageResourceData
{
    /// <summary>The final normalized image URI.</summary>
    public string Uri;
    /// <summary>The effective image media type.</summary>
    public string MediaType;
    /// <summary>The independently retained encoded bytes.</summary>
    public byte[] Bytes;
    /// <summary>The available intrinsic dimensions, or <c>null</c>.</summary>
    public SvgIntrinsicSize? IntrinsicSize;
    /// <summary>Whether decoded content is already available to the host.</summary>
    public bool DecodedContentAvailable;
    /// <summary>The optional decoded premultiplied pixels supplied by the host or a codec.</summary>
    public SvgPixelBuffer? DecodedPixels;

    /// <summary>Creates an empty image resource.</summary>
    public SvgSceneImageResourceData()
    {
        Uri = "";
        MediaType = "";
        Bytes = System.Array.Empty<byte>();
        IntrinsicSize = null;
        DecodedContentAvailable = false;
        DecodedPixels = null;
    }
}

/// <summary>Contains one normalized marker viewport and content graph.</summary>
public class SvgSceneMarkerData
{
    /// <summary>The marker coordinate-system mode.</summary>
    public SvgMarkerUnits Units;
    /// <summary>The marker orientation mode.</summary>
    public SvgMarkerOrientKind OrientKind;
    /// <summary>The fixed marker angle in degrees.</summary>
    public double OrientAngle;
    /// <summary>The marker viewport.</summary>
    public SvgRect? Viewport;
    /// <summary>The marker reference point in content coordinates.</summary>
    public SvgPoint ReferencePoint;
    /// <summary>The marker-content transform.</summary>
    public SvgMatrix Transform;
    /// <summary>The normalized marker content root, or <c>null</c>.</summary>
    public SvgSceneNode? Content;

    /// <summary>Creates empty marker data.</summary>
    public SvgSceneMarkerData()
    {
        Units = SvgMarkerUnits.StrokeWidth;
        OrientKind = SvgMarkerOrientKind.Angle;
        OrientAngle = 0;
        Viewport = null;
        ReferencePoint = new SvgPoint(0, 0);
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Content = null;
    }
}

/// <summary>Contains one normalized clipping graph.</summary>
public class SvgSceneClipData
{
    /// <summary>The clip coordinate-system mode.</summary>
    public SvgPaintServerUnits Units;
    /// <summary>The clip-content to local-user-space transform.</summary>
    public SvgMatrix Transform;
    /// <summary>The normalized clip content root, or <c>null</c>.</summary>
    public SvgSceneNode? Content;

    /// <summary>Creates empty user-space clip data.</summary>
    public SvgSceneClipData()
    {
        Units = SvgPaintServerUnits.UserSpaceOnUse;
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Content = null;
    }
}

/// <summary>Contains one normalized mask region and content graph.</summary>
public class SvgSceneMaskData
{
    /// <summary>The mask-region coordinate-system mode.</summary>
    public SvgPaintServerUnits Units;
    /// <summary>The mask-content coordinate-system mode.</summary>
    public SvgPaintServerUnits ContentUnits;
    /// <summary>The alpha or luminance interpretation.</summary>
    public SvgMaskType MaskType;
    /// <summary>The sRGB or linear-sRGB space used to calculate luminance.</summary>
    public SvgColorSpace ColorInterpolation;
    /// <summary>The mask region in local user space.</summary>
    public SvgRect? Region;
    /// <summary>The mask-content to local-user-space transform.</summary>
    public SvgMatrix ContentTransform;
    /// <summary>The normalized mask content root, or <c>null</c>.</summary>
    public SvgSceneNode? Content;

    /// <summary>Creates empty luminance-mask data.</summary>
    public SvgSceneMaskData()
    {
        Units = SvgPaintServerUnits.ObjectBoundingBox;
        ContentUnits = SvgPaintServerUnits.UserSpaceOnUse;
        MaskType = SvgMaskType.Luminance;
        ColorInterpolation = SvgColorSpace.Srgb;
        Region = null;
        ContentTransform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Content = null;
    }
}

/// <summary>Contains one normalized filter primitive and graph edges.</summary>
public class SvgSceneFilterPrimitiveData
{
    /// <summary>The zero-based primitive order.</summary>
    public int Index;
    /// <summary>The filter primitive element kind.</summary>
    public SvgElementKind ElementKind;
    /// <summary>The primitive color space.</summary>
    public SvgFilterColorSpace ColorSpace;
    /// <summary>The ordered input graph edges.</summary>
    public System.Collections.Generic.List<SvgFilterInput> Inputs;
    /// <summary>The parsed primitive attributes.</summary>
    public System.Collections.Generic.List<SvgFilterPrimitiveAttribute> Attributes;
    /// <summary>The parsed transfer-function, merge-node, or lighting-source children.</summary>
    public System.Collections.Generic.List<SvgFilterPrimitiveChild> Children;
    /// <summary>The normalized primitive result name.</summary>
    public string ResultName;
    /// <summary>The primitive output region in local user space.</summary>
    public SvgRect? Region;
    /// <summary>The primitive output region in primitive coordinates.</summary>
    public SvgRect? PrimitiveRegion;
    /// <summary>The normalized horizontal feOffset distance in primitive coordinates.</summary>
    public double OffsetX;
    /// <summary>The normalized vertical feOffset distance in primitive coordinates.</summary>
    public double OffsetY;
    /// <summary>The computed feFlood color, or <c>null</c> for another primitive.</summary>
    public SvgColor? FloodColor;
    /// <summary>The computed feFlood opacity.</summary>
    public double FloodOpacity;
    /// <summary>The computed diffuse or specular lighting color, or <c>null</c>.</summary>
    public SvgColor? LightingColor;
    /// <summary>The policy-resolved feImage resource, or <c>null</c> when unavailable or inapplicable.</summary>
    public SvgSceneResource? ImageResource;
    /// <summary>The normalized feImage preserve-aspect-ratio mapping.</summary>
    public SvgAspectRatio ImageAspectRatio;

    /// <summary>Creates an empty primitive record.</summary>
    public SvgSceneFilterPrimitiveData()
    {
        Index = 0;
        ElementKind = SvgElementKind.Unknown;
        ColorSpace = SvgFilterColorSpace.LinearRgb;
        Inputs = new System.Collections.Generic.List<SvgFilterInput>();
        Attributes = new System.Collections.Generic.List<SvgFilterPrimitiveAttribute>();
        Children = new System.Collections.Generic.List<SvgFilterPrimitiveChild>();
        ResultName = "";
        Region = null;
        PrimitiveRegion = null;
        OffsetX = 0;
        OffsetY = 0;
        FloodColor = null;
        FloodOpacity = 1;
        LightingColor = null;
        ImageResource = null;
        ImageAspectRatio = new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false);
    }
}

/// <summary>Contains one normalized filter region and primitive graph.</summary>
public class SvgSceneFilterData
{
    /// <summary>The filter-region coordinate-system mode.</summary>
    public SvgPaintServerUnits Units;
    /// <summary>The primitive coordinate-system mode.</summary>
    public SvgPaintServerUnits PrimitiveUnits;
    /// <summary>The filter region in local user space.</summary>
    public SvgRect? Region;
    /// <summary>The primitive-coordinate to local-user-space transform.</summary>
    public SvgMatrix PrimitiveTransform;
    /// <summary>The ordered normalized primitive graph.</summary>
    public System.Collections.Generic.List<SvgSceneFilterPrimitiveData> Primitives;

    /// <summary>Creates empty filter data.</summary>
    public SvgSceneFilterData()
    {
        Units = SvgPaintServerUnits.ObjectBoundingBox;
        PrimitiveUnits = SvgPaintServerUnits.UserSpaceOnUse;
        Region = null;
        PrimitiveTransform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Primitives = new System.Collections.Generic.List<SvgSceneFilterPrimitiveData>();
    }
}

/// <summary>Contains one tagged shared normalized scene resource.</summary>
public class SvgSceneResource
{
    /// <summary>The deterministic document-local resource key.</summary>
    public string Key;
    /// <summary>The tagged resource payload kind.</summary>
    public SvgSceneResourceKind Kind;
    /// <summary>The source resource node, or <c>null</c> for synthesized data.</summary>
    public SvgNode? SourceNode;
    /// <summary>The captured source version.</summary>
    public long SourceVersion;
    /// <summary>The matching solid-paint payload, or <c>null</c>.</summary>
    public SvgSceneSolidPaintData? SolidPaint;
    /// <summary>The matching gradient-paint payload, or <c>null</c>.</summary>
    public SvgSceneGradientPaintData? GradientPaint;
    /// <summary>The matching pattern-paint payload, or <c>null</c>.</summary>
    public SvgScenePatternPaintData? PatternPaint;
    /// <summary>The matching image payload, or <c>null</c>.</summary>
    public SvgSceneImageResourceData? Image;
    /// <summary>The matching marker payload, or <c>null</c>.</summary>
    public SvgSceneMarkerData? Marker;
    /// <summary>The matching clip payload, or <c>null</c>.</summary>
    public SvgSceneClipData? Clip;
    /// <summary>The matching mask payload, or <c>null</c>.</summary>
    public SvgSceneMaskData? Mask;
    /// <summary>The matching filter payload, or <c>null</c>.</summary>
    public SvgSceneFilterData? Filter;

    /// <summary>Creates a resource with exactly one payload matching the supplied tag.</summary>
    /// <param name="key">The nonempty deterministic resource key.</param>
    /// <param name="kind">The normalized resource kind.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for an empty key or unknown tag.</exception>
    public SvgSceneResource(string key, SvgSceneResourceKind kind)
    {
        if (key == null || key.Length == 0) throw new System.ArgumentOutOfRangeException("key");
        if ((int)kind < 0 || (int)kind > 7) throw new System.ArgumentOutOfRangeException("kind");
        Key = key;
        Kind = kind;
        SourceNode = null;
        SourceVersion = 0;
        SolidPaint = kind == SvgSceneResourceKind.SolidPaint ? new SvgSceneSolidPaintData() : null;
        GradientPaint = kind == SvgSceneResourceKind.GradientPaint ? new SvgSceneGradientPaintData() : null;
        PatternPaint = kind == SvgSceneResourceKind.PatternPaint ? new SvgScenePatternPaintData() : null;
        Image = kind == SvgSceneResourceKind.Image ? new SvgSceneImageResourceData() : null;
        Marker = kind == SvgSceneResourceKind.Marker ? new SvgSceneMarkerData() : null;
        Clip = kind == SvgSceneResourceKind.Clip ? new SvgSceneClipData() : null;
        Mask = kind == SvgSceneResourceKind.Mask ? new SvgSceneMaskData() : null;
        Filter = kind == SvgSceneResourceKind.Filter ? new SvgSceneFilterData() : null;
    }

    /// <summary>Tests whether exactly one payload matches this resource's tag.</summary>
    /// <returns><c>true</c> when the tagged payload and key are structurally valid.</returns>
    internal bool HasValidTag()
    {
        if (Key == null || Key.Length == 0) return false;
        int count = 0;
        if (SolidPaint != null) count = count + 1;
        if (GradientPaint != null) count = count + 1;
        if (PatternPaint != null) count = count + 1;
        if (Image != null) count = count + 1;
        if (Marker != null) count = count + 1;
        if (Clip != null) count = count + 1;
        if (Mask != null) count = count + 1;
        if (Filter != null) count = count + 1;
        if (count != 1) return false;
        if (Kind == SvgSceneResourceKind.SolidPaint) return SolidPaint != null;
        if (Kind == SvgSceneResourceKind.GradientPaint) return GradientPaint != null;
        if (Kind == SvgSceneResourceKind.PatternPaint) return PatternPaint != null;
        if (Kind == SvgSceneResourceKind.Image) return Image != null;
        if (Kind == SvgSceneResourceKind.Marker) return Marker != null;
        if (Kind == SvgSceneResourceKind.Clip) return Clip != null;
        if (Kind == SvgSceneResourceKind.Mask) return Mask != null;
        if (Kind == SvgSceneResourceKind.Filter) return Filter != null;
        return false;
    }

    /// <summary>Captures source identity for current-version checks.</summary>
    /// <param name="source">The source resource node.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when the source is <c>null</c>.</exception>
    internal void SetSource(SvgNode source)
    {
        if (source == null) throw new System.ArgumentNullException("source");
        SourceNode = source;
        SourceVersion = source.Version;
    }

    /// <summary>Checks the captured source version.</summary>
    /// <returns><c>true</c> for synthesized data or a current source node.</returns>
    internal bool IsCurrent()
    {
        return SourceNode == null || SourceNode.Version == SourceVersion;
    }
}

/// <summary>Contains one normalized backend-neutral scene and its shared resources.</summary>
public class SvgScene
{
    /// <summary>The source document, or <c>null</c> for a generated scene.</summary>
    public SvgDocument? SourceDocument;
    /// <summary>The captured source document version.</summary>
    public long SourceVersion;
    /// <summary>The normalized root group, or <c>null</c>.</summary>
    public SvgSceneNode? Root;
    /// <summary>Every registered node in deterministic preorder.</summary>
    public System.Collections.Generic.List<SvgSceneNode>? Nodes;
    /// <summary>The shared resources in deterministic insertion order.</summary>
    public System.Collections.Generic.List<SvgSceneResource>? Resources;
    /// <summary>The aggregate scene bounds, or <c>null</c> before bounds computation.</summary>
    public SvgRect? Bounds;
    /// <summary>The deterministic compositing and layer plan, or <c>null</c> before planning.</summary>
    public SvgSceneLayerPlan? LayerPlan;
    /// <summary>The deterministic backend-neutral render command list, or <c>null</c> before enumeration.</summary>
    public SvgRenderCommandList? RenderCommands;
    /// <summary>Scene construction diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic>? Diagnostics;
    /// <summary>Whether scene construction completed successfully.</summary>
    public bool Success;

    /// <summary>Creates an empty generated scene.</summary>
    public SvgScene()
    {
        Initialize(null);
    }

    /// <summary>Creates an empty scene tied to one source document version.</summary>
    /// <param name="sourceDocument">The source document.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when the document is <c>null</c>.</exception>
    /// <returns>A new source-backed scene.</returns>
    internal static SvgScene Create(SvgDocument? sourceDocument)
    {
        if (sourceDocument == null) throw new System.ArgumentNullException("sourceDocument");
        SvgScene result = new SvgScene();
        result.Initialize(sourceDocument);
        return result;
    }

    /// <summary>Registers a valid root group and rebuilds deterministic node preorder.</summary>
    /// <param name="root">The unattached root group.</param>
    /// <returns><c>true</c> when the complete tree was structurally valid.</returns>
    internal bool SetRoot(SvgSceneNode? root)
    {
        if (root == null || root.Kind != SvgSceneNodeKind.Group || root.Parent != null) return false;
        System.Collections.Generic.List<SvgSceneNode>? nodes = CollectNodes(root);
        if (nodes == null) return false;
        Root = root;
        Nodes = nodes;
        return true;
    }

    /// <summary>Adds one valid uniquely keyed shared resource.</summary>
    /// <param name="resource">The tagged resource.</param>
    /// <returns><c>true</c> when the resource was added.</returns>
    internal bool AddResource(SvgSceneResource? resource)
    {
        if (resource == null || !resource.HasValidTag() || FindResource(resource.Key) != null) return false;
        (Resources ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Add(resource);
        return true;
    }

    /// <summary>Finds a shared resource by its ordinal key.</summary>
    /// <param name="key">The exact resource key.</param>
    /// <returns>The shared resource, or <c>null</c>.</returns>
    internal SvgSceneResource? FindResource(string? key)
    {
        if (key == null) return null;
        int index = 0;
        while (index < (Resources ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            if (Resources[index].Key == key) return Resources[index];
            index = index + 1;
        }
        return null;
    }

    /// <summary>Precomputes absolute transforms and component, effect, and aggregate bounds.</summary>
    /// <param name="document">The current resolved document that supplied scene identities.</param>
    /// <returns>The completed bounds outcome.</returns>
    internal SvgSceneBoundsResult ComputeBounds(SvgResolvedDocument document)
    {
        return new SvgSceneBounds().ComputeWithSceneAndDocument(this, document);
    }

    /// <summary>Precomputes bounds using explicit component and traversal options.</summary>
    /// <param name="document">The current resolved document that supplied scene identities.</param>
    /// <param name="options">The component and traversal options.</param>
    /// <returns>The completed or failed bounds outcome.</returns>
    internal SvgSceneBoundsResult ComputeBoundsWithOptions(SvgResolvedDocument document, SvgSceneBoundsOptions options)
    {
        return new SvgSceneBounds().ComputeWithSceneAndDocumentAndOptions(this, document, options);
    }

    /// <summary>Plans display, visibility, paint order, clips, and compositing layers.</summary>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <returns>The deterministic layer plan.</returns>
    internal SvgSceneLayerPlan PlanLayers(SvgResolvedDocument document)
    {
        return new SvgSceneLayerPlanner().PlanWithSceneAndDocument(this, document);
    }

    /// <summary>Plans layers under an explicit record bound.</summary>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <param name="options">The layer traversal options.</param>
    /// <returns>The complete or failed deterministic layer plan.</returns>
    internal SvgSceneLayerPlan PlanLayersWithOptions(SvgResolvedDocument document, SvgSceneLayerOptions options)
    {
        return new SvgSceneLayerPlanner().PlanWithSceneAndDocumentAndOptions(this, document, options);
    }

    /// <summary>Enumerates and attaches balanced backend-neutral render commands.</summary>
    /// <returns>The complete balanced command list.</returns>
    internal SvgRenderCommandList EnumerateCommands()
    {
        return new SvgRenderCommandEnumerator().EnumerateWithScene(this);
    }

    /// <summary>Enumerates commands using explicit bounds and attachment behavior.</summary>
    /// <param name="options">The command enumeration options.</param>
    /// <returns>The complete balanced or failed command list.</returns>
    internal SvgRenderCommandList EnumerateCommandsWithOptions(SvgRenderCommandOptions options)
    {
        return new SvgRenderCommandEnumerator().EnumerateWithSceneAndOptions(this, options);
    }

    /// <summary>Finds a scene node by source identity and exact instance path.</summary>
    /// <param name="source">The source node.</param>
    /// <param name="instancePath">The exact instance path.</param>
    /// <returns>The matching scene node, or <c>null</c>.</returns>
    internal SvgSceneNode? FindNode(SvgNode? source, string? instancePath)
    {
        if (source == null || instancePath == null) return null;
        int index = 0;
        while (index < (Nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            SvgSceneNode node = Nodes[index];
            if (object.ReferenceEquals(node.SourceNode, source) && node.InstancePath == instancePath) return node;
            index = index + 1;
        }
        return null;
    }

    /// <summary>Extracts a fragment scene while retaining its ancestor context.</summary>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <param name="fragment">The ID with or without a leading number sign.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    internal SvgSceneExtractionResult ExtractFragment(SvgResolvedDocument document, string fragment)
    {
        return new SvgSceneExtractor().ExtractFragmentWithSceneAndDocumentAndFragment(this, document, fragment);
    }

    /// <summary>Extracts a fragment scene under explicit bounds.</summary>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <param name="fragment">The ID with or without a leading number sign.</param>
    /// <param name="options">The extraction bounds.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    internal SvgSceneExtractionResult ExtractFragmentWithOptions(SvgResolvedDocument document, string fragment, SvgSceneExtractionOptions options)
    {
        return new SvgSceneExtractor().ExtractFragmentWithSceneAndDocumentAndFragmentAndOptions(this, document, fragment, options);
    }

    /// <summary>Extracts an authored source-node scene while retaining its ancestor context.</summary>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    internal SvgSceneExtractionResult ExtractSource(SvgResolvedDocument document, SvgNode source)
    {
        return new SvgSceneExtractor().ExtractSourceWithSceneAndDocumentAndSource(this, document, source);
    }

    /// <summary>Extracts an authored source-node scene under explicit bounds.</summary>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <param name="options">The extraction bounds.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    internal SvgSceneExtractionResult ExtractSourceWithOptions(SvgResolvedDocument document, SvgNode source, SvgSceneExtractionOptions options)
    {
        return new SvgSceneExtractor().ExtractSourceWithSceneAndDocumentAndSourceAndOptions(this, document, source, options);
    }

    /// <summary>Extracts an exact use-instance scene while retaining its ancestor context.</summary>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    internal SvgSceneExtractionResult ExtractInstance(SvgResolvedDocument document, string instancePath)
    {
        return new SvgSceneExtractor().ExtractInstanceWithSceneAndDocumentAndInstancePath(this, document, instancePath);
    }

    /// <summary>Extracts an exact use-instance scene under explicit bounds.</summary>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <param name="options">The extraction bounds.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    internal SvgSceneExtractionResult ExtractInstanceWithOptions(SvgResolvedDocument document, string instancePath, SvgSceneExtractionOptions options)
    {
        return new SvgSceneExtractor().ExtractInstanceWithSceneAndDocumentAndInstancePathAndOptions(this, document, instancePath, options);
    }

    /// <summary>Checks captured document, node, and resource versions.</summary>
    /// <returns><c>true</c> when every retained source identity remains current.</returns>
    internal bool IsCurrent()
    {
        if (SourceDocument != null && SourceDocument.Version != SourceVersion) return false;
        int index = 0;
        while (index < (Nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            SvgSceneNode node = Nodes[index];
            if (node.SourceNode != null && node.SourceNode.Version != node.SourceVersion) return false;
            index = index + 1;
        }
        index = 0;
        while (index < (Resources ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            if (!Resources[index].IsCurrent()) return false;
            index = index + 1;
        }
        return true;
    }

    internal void Initialize(SvgDocument? sourceDocument)
    {
        SourceDocument = sourceDocument;
        SourceVersion = sourceDocument == null ? 0 : sourceDocument.Version;
        Root = null;
        Nodes = new System.Collections.Generic.List<SvgSceneNode>();
        Resources = new System.Collections.Generic.List<SvgSceneResource>();
        Bounds = null;
        LayerPlan = null;
        RenderCommands = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    internal static System.Collections.Generic.List<SvgSceneNode>? CollectNodes(SvgSceneNode root)
    {
        System.Collections.Generic.List<SvgSceneNode> result = new System.Collections.Generic.List<SvgSceneNode>();
        System.Collections.Generic.List<SvgSceneNode> stack = new System.Collections.Generic.List<SvgSceneNode>();
        stack.Add(root);
        while (stack.Count != 0)
        {
            int last = stack.Count - 1;
            SvgSceneNode node = stack[last];
            stack.RemoveAt(last);
            if (node == null || !node.HasValidTag() || node.Children == null || ContainsNode(result, node)) return null;
            if (node.Kind != SvgSceneNodeKind.Group && node.Children.Count != 0) return null;
            result.Add(node);
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                SvgSceneNode child = node.Children[childIndex];
                if (child == null || !object.ReferenceEquals(child.Parent, node)) return null;
                stack.Add(child);
                childIndex = childIndex - 1;
            }
        }
        return result;
    }

    internal static bool ContainsNode(System.Collections.Generic.List<SvgSceneNode> nodes, SvgSceneNode target)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], target)) return true;
            index = index + 1;
        }
        return false;
    }
}
