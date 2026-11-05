namespace svg;

/// <summary>Configures bounded filter-graph planning and standard-input capture.</summary>
public class SvgFilterGraphOptions
{
    /// <summary>The local-user-space to destination-pixel transform.</summary>
    public SvgMatrix Transform;
    /// <summary>An optional destination-pixel clip.</summary>
    public SvgPixelRect? Clip;
    /// <summary>The graph and surface-count limits.</summary>
    public SvgFilterLimits FilterLimits;
    /// <summary>The pixel-buffer dimension and byte limits.</summary>
    public SvgRasterLimits RasterLimits;
    /// <summary>The source graphic in destination-pixel coordinates, or <c>null</c> for transparent black.</summary>
    public SvgPixelBuffer? SourceGraphic;
    /// <summary>The background image in destination-pixel coordinates, or <c>null</c> for transparent black.</summary>
    public SvgPixelBuffer? BackgroundImage;
    /// <summary>The fill paint image in destination-pixel coordinates, or <c>null</c> for transparent black.</summary>
    public SvgPixelBuffer? FillPaint;
    /// <summary>The stroke paint image in destination-pixel coordinates, or <c>null</c> for transparent black.</summary>
    public SvgPixelBuffer? StrokePaint;
    /// <summary>The concrete color space used for absent standard inputs and <c>auto</c> primitive operations.</summary>
    public SvgPixelColorSpace DefaultColorSpace;
    /// <summary>Whether owned snapshot and reusable output buffers are allocated.</summary>
    public bool AllocateSurfaces;

    /// <summary>Creates identity-transform planning defaults with secure limits and allocated surfaces.</summary>
    public SvgFilterGraphOptions()
    {
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Clip = null;
        FilterLimits = new SvgFilterLimits();
        RasterLimits = new SvgRasterLimits();
        SourceGraphic = null;
        BackgroundImage = null;
        FillPaint = null;
        StrokePaint = null;
        DefaultColorSpace = SvgPixelColorSpace.SrgbLinear;
        AllocateSurfaces = true;
    }
}

/// <summary>Records one primitive-output lifetime assigned to a reusable surface.</summary>
public class SvgFilterSurfaceUse
{
    /// <summary>The primitive index producing the image.</summary>
    public int PrimitiveIndex;
    /// <summary>The clipped destination-pixel subregion represented by the image.</summary>
    public SvgPixelRect Bounds;
    /// <summary>The producing step index.</summary>
    public int FirstStep;
    /// <summary>The final step that reads the image, including final-output retention.</summary>
    public int LastStep;

    internal SvgFilterSurfaceUse(int primitiveIndex, SvgPixelRect bounds, int firstStep, int lastStep)
    {
        PrimitiveIndex = primitiveIndex;
        Bounds = bounds;
        FirstStep = firstStep;
        LastStep = lastStep;
    }
}

/// <summary>Contains one immutable snapshot or lifetime-reused primitive surface.</summary>
public class SvgFilterSurface
{
    /// <summary>The stable zero-based surface slot.</summary>
    public int Index;
    /// <summary>The surface lifetime role.</summary>
    public SvgFilterSurfaceKind Kind;
    /// <summary>The standard input represented by a snapshot slot.</summary>
    public SvgFilterStandardInput StandardInput;
    /// <summary>The concrete pixel color space.</summary>
    public SvgPixelColorSpace ColorSpace;
    /// <summary>The maximum width required by any assigned image.</summary>
    public int Width;
    /// <summary>The maximum height required by any assigned image.</summary>
    public int Height;
    /// <summary>The owned buffer, or <c>null</c> when allocation was disabled.</summary>
    public SvgPixelBuffer? Buffer;
    /// <summary>The ordered nonoverlapping primitive lifetimes assigned to this slot.</summary>
    public System.Collections.Generic.List<SvgFilterSurfaceUse> Uses;

    internal SvgFilterSurface(int index, SvgFilterSurfaceKind kind, SvgFilterStandardInput standardInput, SvgPixelColorSpace colorSpace, int width, int height)
    {
        Index = index;
        Kind = kind;
        StandardInput = standardInput;
        ColorSpace = colorSpace;
        Width = width;
        Height = height;
        Buffer = null;
        Uses = new System.Collections.Generic.List<SvgFilterSurfaceUse>();
    }
}

/// <summary>Contains one resolved input binding for a planned primitive.</summary>
public class SvgFilterGraphInput
{
    /// <summary>The resolved input kind.</summary>
    public SvgFilterInputKind Kind;
    /// <summary>The standard input kind when applicable.</summary>
    public SvgFilterStandardInput StandardInput;
    /// <summary>The producing primitive index, or <c>-1</c>.</summary>
    public int ProducerIndex;
    /// <summary>The supplying surface slot, or <c>-1</c> for transparent black.</summary>
    public int SurfaceIndex;
    /// <summary>The supplying image's destination-pixel bounds.</summary>
    public SvgPixelRect Bounds;
    /// <summary>The supplying surface's concrete color space.</summary>
    public SvgPixelColorSpace ColorSpace;
    /// <summary>Whether the primitive must convert this input to its operation color space.</summary>
    public bool RequiresColorConversion;

    internal SvgFilterGraphInput()
    {
        Kind = SvgFilterInputKind.TransparentBlack;
        StandardInput = SvgFilterStandardInput.SourceGraphic;
        ProducerIndex = -1;
        SurfaceIndex = -1;
        Bounds = new SvgPixelRect(0, 0, 0, 0);
        ColorSpace = SvgPixelColorSpace.SrgbLinear;
        RequiresColorConversion = false;
    }
}

/// <summary>Contains one ordered filter primitive execution step.</summary>
public class SvgFilterGraphStep
{
    /// <summary>The zero-based execution index.</summary>
    public int Index;
    /// <summary>The normalized primitive data.</summary>
    public SvgSceneFilterPrimitiveData Primitive;
    /// <summary>The clipped destination-pixel primitive subregion.</summary>
    public SvgPixelRect Bounds;
    /// <summary>The concrete operation and output color space.</summary>
    public SvgPixelColorSpace ColorSpace;
    /// <summary>The ordered resolved input bindings.</summary>
    public System.Collections.Generic.List<SvgFilterGraphInput> Inputs;
    /// <summary>The output surface slot, or <c>-1</c> for an empty output.</summary>
    public int OutputSurfaceIndex;
    /// <summary>The final step that needs this output.</summary>
    public int LastUseStep;
    /// <summary>Whether the output surface slot previously carried another primitive result.</summary>
    public bool ReusesSurface;

    internal SvgFilterGraphStep(int index, SvgSceneFilterPrimitiveData primitive)
    {
        Index = index;
        Primitive = primitive;
        Bounds = new SvgPixelRect(0, 0, 0, 0);
        ColorSpace = SvgPixelColorSpace.SrgbLinear;
        Inputs = new System.Collections.Generic.List<SvgFilterGraphInput>();
        OutputSurfaceIndex = -1;
        LastUseStep = index;
        ReusesSurface = false;
    }
}

/// <summary>Contains a complete bounded filter execution and surface-lifetime plan.</summary>
public class SvgFilterGraphPlan
{
    /// <summary>Whether planning and requested allocation succeeded.</summary>
    public bool Success;
    /// <summary>The stable planning outcome.</summary>
    public SvgFilterGraphError Error;
    /// <summary>The clipped destination-pixel filter region.</summary>
    public SvgPixelRect FilterBounds;
    /// <summary>The primitive-coordinate to destination-pixel transform used by primitive parameters.</summary>
    public SvgMatrix PrimitiveTransform;
    /// <summary>The ordered primitive execution steps.</summary>
    public System.Collections.Generic.List<SvgFilterGraphStep> Steps;
    /// <summary>The immutable snapshot and reusable output surface slots.</summary>
    public System.Collections.Generic.List<SvgFilterSurface> Surfaces;
    /// <summary>The final output surface slot, or <c>-1</c> for transparent black.</summary>
    public int FinalSurfaceIndex;
    /// <summary>The final output image bounds.</summary>
    public SvgPixelRect FinalBounds;
    /// <summary>The number of immutable standard-input snapshots.</summary>
    public int SnapshotCount;
    /// <summary>The number of primitive results assigned to an already used output slot.</summary>
    public int ReusedSurfaceCount;

    internal SvgFilterGraphPlan()
    {
        Success = false;
        Error = SvgFilterGraphError.None;
        FilterBounds = new SvgPixelRect(0, 0, 0, 0);
        PrimitiveTransform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Steps = new System.Collections.Generic.List<SvgFilterGraphStep>();
        Surfaces = new System.Collections.Generic.List<SvgFilterSurface>();
        FinalSurfaceIndex = -1;
        FinalBounds = new SvgPixelRect(0, 0, 0, 0);
        SnapshotCount = 0;
        ReusedSurfaceCount = 0;
    }
}
