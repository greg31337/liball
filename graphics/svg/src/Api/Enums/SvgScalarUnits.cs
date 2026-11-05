namespace svg;

/// <summary>Defines supported angle units.</summary>
public enum SvgAngleUnit
{
    /// <summary>Unitless SVG degrees.</summary>
    Number = 0,

    /// <summary>Degrees.</summary>
    Deg = 1,

    /// <summary>Gradians.</summary>
    Grad = 2,

    /// <summary>Radians.</summary>
    Rad = 3,

    /// <summary>Turns.</summary>
    Turn = 4
}

/// <summary>Defines supported time units.</summary>
public enum SvgTimeUnit
{
    /// <summary>Seconds.</summary>
    Seconds = 0,

    /// <summary>Milliseconds.</summary>
    Milliseconds = 1
}

/// <summary>Defines supported frequency units.</summary>
public enum SvgFrequencyUnit
{
    /// <summary>Hertz.</summary>
    Hz = 0,

    /// <summary>Kilohertz.</summary>
    KHz = 1
}

/// <summary>Defines supported resolution units.</summary>
public enum SvgResolutionUnit
{
    /// <summary>Dots per inch.</summary>
    Dpi = 0,

    /// <summary>Dots per centimeter.</summary>
    Dpcm = 1,

    /// <summary>Dots per CSS pixel.</summary>
    Dppx = 2
}

/// <summary>Defines stable failures produced by scalar-unit conversion.</summary>
public enum SvgUnitConversionError
{
    /// <summary>No conversion failure occurred.</summary>
    None = 0,

    /// <summary>The source value, target unit, or conversion argument was invalid.</summary>
    InvalidArgument = 1,

    /// <summary>The converted result was not finite.</summary>
    NonFiniteResult = 2,

    /// <summary>A required positive divisor was zero.</summary>
    ZeroDivisor = 3
}
