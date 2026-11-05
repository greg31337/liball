using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxUnitsOperations"/>.
/// </summary>
public interface IPptxUnitsOperations
{
    /// <summary>
    /// Converts to emu using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="inches">The inches value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    long InchesToEmu(double inches);
    /// <summary>
    /// Converts to inches using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="emu">The emu value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    double EmuToInches(long emu);
    /// <summary>
    /// Converts to emu using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="points">The points value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    long PointsToEmu(double points);
    /// <summary>
    /// Converts to points using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="emu">The emu value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    double EmuToPoints(long emu);
    /// <summary>
    /// Converts to emu using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="centimeters">The centimeters value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    long CentimetersToEmu(double centimeters);
    /// <summary>
    /// Converts to centimeters using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="emu">The emu value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    double EmuToCentimeters(long emu);
    /// <summary>
    /// Converts to emu using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="pixels">The pixels value to use.</param>
    /// <param name="dpi">The dpi value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    long PixelsToEmu(double pixels, double dpi);
    /// <summary>
    /// Converts to pixels using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="emu">The emu value to use.</param>
    /// <param name="dpi">The dpi value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    double EmuToPixels(long emu, double dpi);
    /// <summary>
    /// Converts to rounded pixels using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="emu">The emu value to use.</param>
    /// <param name="dpi">The dpi value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    int EmuToRoundedPixels(long emu, double dpi);
    /// <summary>
    /// Converts to angle units using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="degrees">The degrees value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    int DegreesToAngleUnits(double degrees);
    /// <summary>
    /// Converts units to degrees using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="angleUnits">The angle units value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    double AngleUnitsToDegrees(int angleUnits);
    /// <summary>
    /// Converts to percentage units using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="percent">The percent value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    int PercentToPercentageUnits(double percent);
    /// <summary>
    /// Converts units to percent using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="percentageUnits">The percentage units value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    double PercentageUnitsToPercent(int percentageUnits);
    /// <summary>
    /// Converts to percentage units using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="fraction">The fraction value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    int FractionToPercentageUnits(double fraction);
    /// <summary>
    /// Converts units to fraction using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="percentageUnits">The percentage units value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    double PercentageUnitsToFraction(int percentageUnits);
    /// <summary>
    /// Converts to long using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    long RoundToLong(double value);
    /// <summary>
    /// Converts to int using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    int RoundToInt(double value);
}
