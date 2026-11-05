namespace ttf;

/// <summary>Identifies an encoded or synthesized COLR paint format.</summary>
public enum ColorPaintFormat
{
    /// <summary>The encoded paint format is not supported.</summary>
    Unsupported = -1,
    /// <summary>A synthesized COLR version zero layer operation.</summary>
    VersionZero = 0,
    /// <summary>PaintColrLayers.</summary>
    LayerList = 1,
    /// <summary>PaintSolid.</summary>
    Solid = 2,
    /// <summary>PaintVarSolid.</summary>
    VariableSolid = 3,
    /// <summary>PaintLinearGradient.</summary>
    LinearGradient = 4,
    /// <summary>PaintVarLinearGradient.</summary>
    VariableLinearGradient = 5,
    /// <summary>PaintRadialGradient.</summary>
    RadialGradient = 6,
    /// <summary>PaintVarRadialGradient.</summary>
    VariableRadialGradient = 7,
    /// <summary>PaintSweepGradient.</summary>
    SweepGradient = 8,
    /// <summary>PaintVarSweepGradient.</summary>
    VariableSweepGradient = 9,
    /// <summary>PaintGlyph.</summary>
    Glyph = 10,
    /// <summary>PaintColrGlyph.</summary>
    ColrGlyph = 11,
    /// <summary>PaintTransform.</summary>
    Transform = 12,
    /// <summary>PaintVarTransform.</summary>
    VariableTransform = 13,
    /// <summary>PaintTranslate.</summary>
    Translate = 14,
    /// <summary>PaintVarTranslate.</summary>
    VariableTranslate = 15,
    /// <summary>PaintScale.</summary>
    Scale = 16,
    /// <summary>PaintVarScale.</summary>
    VariableScale = 17,
    /// <summary>PaintScaleAroundCenter.</summary>
    ScaleAroundCenter = 18,
    /// <summary>PaintVarScaleAroundCenter.</summary>
    VariableScaleAroundCenter = 19,
    /// <summary>PaintScaleUniform.</summary>
    UniformScale = 20,
    /// <summary>PaintVarScaleUniform.</summary>
    VariableUniformScale = 21,
    /// <summary>PaintScaleUniformAroundCenter.</summary>
    UniformScaleAroundCenter = 22,
    /// <summary>PaintVarScaleUniformAroundCenter.</summary>
    VariableUniformScaleAroundCenter = 23,
    /// <summary>PaintRotate.</summary>
    Rotate = 24,
    /// <summary>PaintVarRotate.</summary>
    VariableRotate = 25,
    /// <summary>PaintRotateAroundCenter.</summary>
    RotateAroundCenter = 26,
    /// <summary>PaintVarRotateAroundCenter.</summary>
    VariableRotateAroundCenter = 27,
    /// <summary>PaintSkew.</summary>
    Skew = 28,
    /// <summary>PaintVarSkew.</summary>
    VariableSkew = 29,
    /// <summary>PaintSkewAroundCenter.</summary>
    SkewAroundCenter = 30,
    /// <summary>PaintVarSkewAroundCenter.</summary>
    VariableSkewAroundCenter = 31,
    /// <summary>PaintComposite.</summary>
    Composite = 32
}
