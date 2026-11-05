using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxUnits values.
/// </summary>
public class PptxUnitsOperations : IPptxUnitsOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxUnitsOperations()
    {
    }

    /// <summary>
    /// Converts to emu using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="inches">The inches value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public long InchesToEmu(double inches)
    {
        return PptxUnitsCore.InchesToEmu(inches);
    }

    /// <summary>
    /// Converts to inches using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="emu">The emu value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public double EmuToInches(long emu)
    {
        return PptxUnitsCore.EmuToInches(emu);
    }

    /// <summary>
    /// Converts to emu using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="points">The points value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public long PointsToEmu(double points)
    {
        return PptxUnitsCore.PointsToEmu(points);
    }

    /// <summary>
    /// Converts to points using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="emu">The emu value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public double EmuToPoints(long emu)
    {
        return PptxUnitsCore.EmuToPoints(emu);
    }

    /// <summary>
    /// Converts to emu using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="centimeters">The centimeters value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public long CentimetersToEmu(double centimeters)
    {
        return PptxUnitsCore.CentimetersToEmu(centimeters);
    }

    /// <summary>
    /// Converts to centimeters using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="emu">The emu value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public double EmuToCentimeters(long emu)
    {
        return PptxUnitsCore.EmuToCentimeters(emu);
    }

    /// <summary>
    /// Converts to emu using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="pixels">The pixels value to use.</param>
    /// <param name="dpi">The dpi value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public long PixelsToEmu(double pixels, double dpi)
    {
        return PptxUnitsCore.PixelsToEmu(pixels, dpi);
    }

    /// <summary>
    /// Converts to pixels using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="emu">The emu value to use.</param>
    /// <param name="dpi">The dpi value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public double EmuToPixels(long emu, double dpi)
    {
        return PptxUnitsCore.EmuToPixels(emu, dpi);
    }

    /// <summary>
    /// Converts to rounded pixels using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="emu">The emu value to use.</param>
    /// <param name="dpi">The dpi value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public int EmuToRoundedPixels(long emu, double dpi)
    {
        return PptxUnitsCore.EmuToRoundedPixels(emu, dpi);
    }

    /// <summary>
    /// Converts to angle units using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="degrees">The degrees value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public int DegreesToAngleUnits(double degrees)
    {
        return PptxUnitsCore.DegreesToAngleUnits(degrees);
    }

    /// <summary>
    /// Converts units to degrees using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="angleUnits">The angle units value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public double AngleUnitsToDegrees(int angleUnits)
    {
        return PptxUnitsCore.AngleUnitsToDegrees(angleUnits);
    }

    /// <summary>
    /// Converts to percentage units using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="percent">The percent value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public int PercentToPercentageUnits(double percent)
    {
        return PptxUnitsCore.PercentToPercentageUnits(percent);
    }

    /// <summary>
    /// Converts units to percent using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="percentageUnits">The percentage units value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public double PercentageUnitsToPercent(int percentageUnits)
    {
        return PptxUnitsCore.PercentageUnitsToPercent(percentageUnits);
    }

    /// <summary>
    /// Converts to percentage units using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="fraction">The fraction value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public int FractionToPercentageUnits(double fraction)
    {
        return PptxUnitsCore.FractionToPercentageUnits(fraction);
    }

    /// <summary>
    /// Converts units to fraction using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="percentageUnits">The percentage units value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public double PercentageUnitsToFraction(int percentageUnits)
    {
        return PptxUnitsCore.PercentageUnitsToFraction(percentageUnits);
    }

    /// <summary>
    /// Converts to long using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public long RoundToLong(double value)
    {
        return PptxUnitsCore.RoundToLong(value);
    }

    /// <summary>
    /// Converts to int using the unit scale encoded in the method name.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public int RoundToInt(double value)
    {
        return PptxUnitsCore.RoundToInt(value);
    }
}
