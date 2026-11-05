namespace odf;

/// <summary>
/// Describes an ODF custom shape's enhanced geometry definition.
/// </summary>
public class OdfEnhancedGeometry
{
    /// <summary>The named or application-defined shape preset.</summary>
    public OdfShapePreset Preset = new OdfShapePreset();
    /// <summary>The enhanced geometry view box.</summary>
    public string ViewBox = "";
    /// <summary>The parsed enhanced geometry view box.</summary>
    public OdfSvgViewBox ViewBoxValue = new OdfSvgViewBox();
    /// <summary>The preserved modifier expression list.</summary>
    public string ModifierText = "";
    /// <summary>The individual modifier expressions.</summary>
    public OdfGeometryModifier[] Modifiers = [];
    /// <summary>The enhanced path expression.</summary>
    public string EnhancedPath = "";
    /// <summary>The horizontal path stretch point.</summary>
    public string StretchPointX = "";
    /// <summary>The vertical path stretch point.</summary>
    public string StretchPointY = "";
    /// <summary>The enhanced text-area expressions.</summary>
    public string TextAreas = "";
    /// <summary>The enhanced glue-point coordinate expressions.</summary>
    public string GluePoints = "";
    /// <summary>The enhanced glue-point routing type.</summary>
    public string GluePointType = "";
    /// <summary>The enhanced glue-point leaving directions.</summary>
    public string GluePointLeavingDirections = "";
    /// <summary>The preserved vertical-mirror value.</summary>
    public string MirrorVerticalValue = "";
    /// <summary>Whether the enhanced geometry is mirrored vertically.</summary>
    public bool MirrorVertical;
    /// <summary>Whether the vertical-mirror value is valid when specified.</summary>
    public bool MirrorVerticalValid = true;
    /// <summary>The preserved horizontal-mirror value.</summary>
    public string MirrorHorizontalValue = "";
    /// <summary>Whether the enhanced geometry is mirrored horizontally.</summary>
    public bool MirrorHorizontal;
    /// <summary>Whether the horizontal-mirror value is valid when specified.</summary>
    public bool MirrorHorizontalValid = true;
    /// <summary>The text rotation angle.</summary>
    public string TextRotateAngle = "";
    /// <summary>The equations referenced by enhanced path expressions.</summary>
    public OdfGeometryEquation[] Equations = [];
    /// <summary>The interactive adjustment handles.</summary>
    public OdfGeometryHandle[] Handles = [];
    /// <summary>The underlying <c>draw:enhanced-geometry</c> element.</summary>
    public OdfNode? Node;
    internal string OriginalViewBox = "";
    internal string OriginalModifierText = "";
    internal string OriginalEnhancedPath = "";
    internal string OriginalStretchPointX = "";
    internal string OriginalStretchPointY = "";
    internal string OriginalTextAreas = "";
    internal string OriginalGluePoints = "";
    internal string OriginalGluePointType = "";
    internal string OriginalGluePointLeavingDirections = "";
    internal string OriginalMirrorVerticalValue = "";
    internal bool OriginalMirrorVertical;
    internal string OriginalMirrorHorizontalValue = "";
    internal bool OriginalMirrorHorizontal;
    internal string OriginalTextRotateAngle = "";
    internal OdfNode[] OriginalEquationNodes = [];
    internal OdfNode[] OriginalHandleNodes = [];
}
