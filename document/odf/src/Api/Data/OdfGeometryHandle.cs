namespace odf;

/// <summary>
/// Describes an interactive Cartesian, polar, or legacy geometry handle.
/// </summary>
public class OdfGeometryHandle
{
    /// <summary>The handle coordinate form.</summary>
    public OdfGeometryHandleKind Kind;
    /// <summary>The Cartesian horizontal position expression.</summary>
    public string PositionX = "";
    /// <summary>The Cartesian vertical position expression.</summary>
    public string PositionY = "";
    /// <summary>The minimum horizontal Cartesian range.</summary>
    public string RangeXMinimum = "";
    /// <summary>The maximum horizontal Cartesian range.</summary>
    public string RangeXMaximum = "";
    /// <summary>The minimum vertical Cartesian range.</summary>
    public string RangeYMinimum = "";
    /// <summary>The maximum vertical Cartesian range.</summary>
    public string RangeYMaximum = "";
    /// <summary>The polar pole horizontal position expression.</summary>
    public string PolarPoleX = "";
    /// <summary>The polar pole vertical position expression.</summary>
    public string PolarPoleY = "";
    /// <summary>The polar radius expression.</summary>
    public string PolarRadius = "";
    /// <summary>The polar angle expression.</summary>
    public string PolarAngle = "";
    /// <summary>The minimum polar radius.</summary>
    public string RadiusRangeMinimum = "";
    /// <summary>The maximum polar radius.</summary>
    public string RadiusRangeMaximum = "";
    /// <summary>The deprecated combined position expression.</summary>
    public string LegacyPosition = "";
    /// <summary>The deprecated combined polar expression.</summary>
    public string LegacyPolar = "";
    /// <summary>The preserved vertical mirror value.</summary>
    public string MirrorVerticalValue = "";
    /// <summary>Whether the handle is mirrored vertically.</summary>
    public bool MirrorVertical;
    /// <summary>Whether the vertical mirror value is valid when specified.</summary>
    public bool MirrorVerticalValid = true;
    /// <summary>The preserved horizontal mirror value.</summary>
    public string MirrorHorizontalValue = "";
    /// <summary>Whether the handle is mirrored horizontally.</summary>
    public bool MirrorHorizontal;
    /// <summary>Whether the horizontal mirror value is valid when specified.</summary>
    public bool MirrorHorizontalValid = true;
    /// <summary>The preserved switched-coordinate value.</summary>
    public string SwitchedValue = "";
    /// <summary>Whether the handle coordinates are switched.</summary>
    public bool IsSwitched;
    /// <summary>Whether the switched-coordinate value is valid when specified.</summary>
    public bool IsSwitchedValid = true;
    /// <summary>The underlying <c>draw:handle</c> element.</summary>
    public OdfNode? Node;
    internal string OriginalPositionX = "";
    internal string OriginalPositionY = "";
    internal string OriginalRangeXMinimum = "";
    internal string OriginalRangeXMaximum = "";
    internal string OriginalRangeYMinimum = "";
    internal string OriginalRangeYMaximum = "";
    internal string OriginalPolarPoleX = "";
    internal string OriginalPolarPoleY = "";
    internal string OriginalPolarRadius = "";
    internal string OriginalPolarAngle = "";
    internal string OriginalRadiusRangeMinimum = "";
    internal string OriginalRadiusRangeMaximum = "";
    internal string OriginalLegacyPosition = "";
    internal string OriginalLegacyPolar = "";
    internal string OriginalMirrorVerticalValue = "";
    internal bool OriginalMirrorVertical;
    internal string OriginalMirrorHorizontalValue = "";
    internal bool OriginalMirrorHorizontal;
    internal string OriginalSwitchedValue = "";
    internal bool OriginalIsSwitched;
}
