namespace svg;

/// <summary>Configures graph planning and bounded filter primitive evaluation.</summary>
public class SvgFilterRenderOptions
{
    /// <summary>The graph transform, standard inputs, allocation choice, and limits.</summary>
    public SvgFilterGraphOptions GraphOptions;
    /// <summary>The positive maximum cumulative primitive pixel work.</summary>
    public long MaximumPixels;
    /// <summary>The positive maximum table entries accepted by one component transfer function.</summary>
    public int MaximumTableValues;

    /// <summary>Creates allocated graph defaults with bounded primitive work.</summary>
    public SvgFilterRenderOptions()
    {
        GraphOptions = new SvgFilterGraphOptions();
        MaximumPixels = 1000000000;
        MaximumTableValues = 65536;
    }
}

/// <summary>Contains one completed filter evaluation and its positioned output image.</summary>
public class SvgFilterRenderResult
{
    /// <summary>Whether every planned primitive was evaluated.</summary>
    public bool Success;
    /// <summary>The stable filter evaluation outcome.</summary>
    public SvgFilterRenderError Error;
    /// <summary>The graph plan used for evaluation, including its owned surfaces.</summary>
    public SvgFilterGraphPlan? Plan;
    /// <summary>The final output view, or <c>null</c> after failure.</summary>
    public SvgPixelBuffer? Output;
    /// <summary>The destination-pixel bounds represented by Output.</summary>
    public SvgPixelRect Bounds;
    /// <summary>The cumulative bounded pixel work.</summary>
    public long PixelsProcessed;
    /// <summary>The number of evaluated primitives.</summary>
    public int PrimitivesEvaluated;

    internal SvgFilterRenderResult()
    {
        Success = false;
        Error = SvgFilterRenderError.None;
        Plan = null;
        Output = null;
        Bounds = new SvgPixelRect(0, 0, 0, 0);
        PixelsProcessed = 0;
        PrimitivesEvaluated = 0;
    }
}
