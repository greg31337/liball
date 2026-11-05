namespace avif;

/// <summary>
/// Configures AVIF parsing, source selection, AV1 decoding, and high-level rendering.
/// </summary>
public class AvifDecoderOptions
{
    /// <summary>The conformance policy applied before and during decoding.</summary>
    public AvifValidationMode ValidationMode;

    /// <summary>The source selected when a file has both a primary item and an image sequence.</summary>
    public AvifSourceSelection SourceSelection;

    /// <summary>The color, alpha, and auxiliary content selected for decoding.</summary>
    public AvifContentSelection ContentSelection;

    /// <summary>Whether progressive layers are exposed individually.</summary>
    public bool AllowProgressive;

    /// <summary>Whether frame decoding may begin before all bytes for the frame are available.</summary>
    public bool AllowIncremental;

    /// <summary>Whether normative AV1 film grain is applied to displayed output.</summary>
    public bool ApplyFilmGrain;

    /// <summary>The AV1 operating-point index, or negative one for automatic selection.</summary>
    public int OperatingPoint;

    /// <summary>The requested worker count, bounded by the active security limits.</summary>
    public int WorkerCount;

    /// <summary>The rendering settings used by high-level decode operations.</summary>
    public AvifRenderOptions RenderOptions;

    /// <summary>The resource limits applied while parsing and decoding.</summary>
    public AvifSecurityLimits Limits;

    /// <summary>The cancellation signal checked at bounded parsing and decoding checkpoints.</summary>
    public AvifCancellation Cancellation;

    /// <summary>
    /// Creates strict, deterministic decoder options for the primary color and alpha result.
    /// </summary>
    public AvifDecoderOptions()
    {
        ValidationMode = AvifValidationMode.Strict;
        SourceSelection = AvifSourceSelection.Automatic;
        ContentSelection = AvifContentSelection.ColorAndAlpha;
        AllowProgressive = false;
        AllowIncremental = false;
        ApplyFilmGrain = true;
        OperatingPoint = -1;
        WorkerCount = 1;
        RenderOptions = new AvifRenderOptions();
        Limits = new AvifSecurityLimits();
        Cancellation = new AvifCancellation();
    }
}
