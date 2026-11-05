namespace svg;

/// <summary>Identifies why baseline shaping requested an external shaper.</summary>
public enum SvgShapingCallbackReason
{
    /// <summary>No external shaping is required.</summary>
    None = 0,
    /// <summary>The requested script is outside the baseline script set.</summary>
    UnsupportedScript = 1,
    /// <summary>The font selected an unsupported layout lookup or shaping table.</summary>
    UnsupportedLayout = 2,
    /// <summary>Both the script and selected font layout require external shaping.</summary>
    UnsupportedScriptAndLayout = 3
}
