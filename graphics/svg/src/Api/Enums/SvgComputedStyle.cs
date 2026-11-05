namespace svg;

/// <summary>Defines the typed payload stored for one computed property.</summary>
public enum SvgComputedValueKind
{
    /// <summary>A preserved computed token sequence.</summary>
    Text = 0,
    /// <summary>A normalized keyword.</summary>
    Keyword = 1,
    /// <summary>A finite number.</summary>
    Number = 2,
    /// <summary>A typed length or percentage.</summary>
    Length = 3,
    /// <summary>A typed angle.</summary>
    Angle = 4,
    /// <summary>A parsed CSS color.</summary>
    Color = 5,
    /// <summary>A parsed SVG paint.</summary>
    Paint = 6,
    /// <summary>A CSS transform list.</summary>
    TransformList = 7,
    /// <summary>A CSS transform origin.</summary>
    TransformOrigin = 8,
    /// <summary>A CSS transform reference box keyword.</summary>
    TransformBox = 9,
    /// <summary>A parsed font-family list.</summary>
    FontFamilyList = 10,
    /// <summary>A complete SVG paint order.</summary>
    PaintOrder = 11,
    /// <summary>A typed length list.</summary>
    LengthList = 12,
    /// <summary>A parsed URL reference.</summary>
    Reference = 13,
    /// <summary>A parsed cursor URL list and terminal keyword.</summary>
    Cursor = 14
}
