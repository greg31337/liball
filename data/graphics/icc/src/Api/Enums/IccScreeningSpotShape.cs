namespace icc;

/// <summary>Identifies a legacy screening spot function.</summary>
public enum IccScreeningSpotShape
{
    /// <summary>Indicates an unknown spot function.</summary>
    Unknown = 0,
    /// <summary>Uses the printer-default spot function.</summary>
    PrinterDefault = 1,
    /// <summary>Uses round spots.</summary>
    Round = 2,
    /// <summary>Uses diamond spots.</summary>
    Diamond = 3,
    /// <summary>Uses elliptical spots.</summary>
    Ellipse = 4,
    /// <summary>Uses line spots.</summary>
    Line = 5,
    /// <summary>Uses square spots.</summary>
    Square = 6,
    /// <summary>Uses cross spots.</summary>
    Cross = 7
}
