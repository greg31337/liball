namespace svg;

/// <summary>Identifies the typed payload stored by an SVG attribute access result.</summary>
public enum SvgTypedAttributeKind
{
    /// <summary>No effective value is available.</summary>
    Missing = 0,
    /// <summary>The effective value depends on later document or rendering context.</summary>
    ContextDependent = 1,
    /// <summary>The value is retained as text.</summary>
    Text = 2,
    /// <summary>The value is a recognized keyword.</summary>
    Keyword = 3,
    /// <summary>The value is a finite number stored as a boxed <c>double</c>.</summary>
    Number = 4,
    /// <summary>The value is a signed integer stored as a boxed <c>int</c>.</summary>
    Integer = 5,
    /// <summary>The value is an <see cref="SvgLength"/>.</summary>
    Length = 6,
    /// <summary>The value is an <see cref="SvgAngle"/>.</summary>
    Angle = 7,
    /// <summary>The value is an <see cref="SvgPathLength"/>.</summary>
    PathLength = 8,
    /// <summary>The value is an <see cref="SvgLengthListValue"/>.</summary>
    LengthList = 9,
    /// <summary>The value is a list of <see cref="SvgRotateItem"/> entries.</summary>
    AngleList = 10,
    /// <summary>The value is an array of finite numbers.</summary>
    NumberList = 11,
    /// <summary>The value is an array of signed integers.</summary>
    IntegerList = 12,
    /// <summary>The value is an alternating coordinate array for a points list.</summary>
    Points = 13,
    /// <summary>The value is an <see cref="SvgPathData"/>.</summary>
    PathData = 14,
    /// <summary>The value is an <see cref="SvgTransformList"/>.</summary>
    TransformList = 15,
    /// <summary>The value is an <see cref="SvgViewBox"/>.</summary>
    ViewBox = 16,
    /// <summary>The value is an <see cref="SvgAspectRatio"/>.</summary>
    AspectRatio = 17,
    /// <summary>The value is an <see cref="SvgIriReference"/>.</summary>
    Iri = 18
}

/// <summary>Identifies where an effective attribute value came from.</summary>
public enum SvgTypedAttributeOrigin
{
    /// <summary>No authored or default value is available.</summary>
    Missing = 0,
    /// <summary>The value was authored on the element.</summary>
    Authored = 1,
    /// <summary>The element catalog supplied a literal default.</summary>
    LiteralDefault = 2,
    /// <summary>The element catalog copied another attribute's effective value.</summary>
    OtherAttributeDefault = 3,
    /// <summary>The element catalog defers the value to later context.</summary>
    ContextDependentDefault = 4
}
