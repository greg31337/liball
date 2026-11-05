namespace pptx;


/// <summary>
/// Converts between the coordinate, angle, percentage, and display units used by PresentationML.
/// </summary>
public static class PptxUnits
{
    /// <summary>
    /// Identifies the emus per inch value accepted by the API.
    /// </summary>
    public const long EmusPerInch = 914400L;
    /// <summary>
    /// Identifies the emus per point value accepted by the API.
    /// </summary>
    public const long EmusPerPoint = 12700L;
    /// <summary>
    /// Identifies the centimeters per inch value accepted by the API.
    /// </summary>
    public const double CentimetersPerInch = 2.54;
    /// <summary>
    /// Identifies the angle units per degree value accepted by the API.
    /// </summary>
    public const int AngleUnitsPerDegree = 60000;
    /// <summary>
    /// Identifies the percentage units per percent value accepted by the API.
    /// </summary>
    public const int PercentageUnitsPerPercent = 1000;
    /// <summary>
    /// Identifies the percentage units per whole value accepted by the API.
    /// </summary>
    public const int PercentageUnitsPerWhole = 100000;
}

