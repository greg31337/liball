using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTransform values.
/// </summary>
public class PptxTransformOperations : IPptxTransformOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxTransformOperations()
    {
    }

    /// <summary>
    /// Creates a transform with zero bounds and default group and rotation values.
    /// </summary>
    /// <returns>The resulting transform value.</returns>
    public PptxTransform CreateDefault()
    {
        return PptxTransform.CreateDefault();
    }

    /// <summary>
    /// Returns the shape bounds represented by the transform.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds Bounds(PptxTransform target)
    {
        return target.Bounds();
    }

    /// <summary>
    /// Returns the axis-aligned bounds occupied after applying rotation around the transform center.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds AnchorBounds(PptxTransform target)
    {
        return target.AnchorBounds();
    }

    /// <summary>
    /// Updates the bounds and the underlying presentation data.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetBounds(PptxTransform target, PptxBounds bounds)
    {
        target.SetBounds(bounds);
    }

    /// <summary>
    /// Returns the clockwise rotation in degrees.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public double RotationDegrees(PptxTransform target)
    {
        return target.RotationDegrees();
    }

    /// <summary>
    /// Updates the rotation degrees and the underlying presentation data.
    /// </summary>
    /// <param name="degrees">The degrees value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetRotationDegrees(PptxTransform target, double degrees)
    {
        target.SetRotationDegrees(degrees);
    }

    /// <summary>
    /// Reports whether the transform contains or enables child bounds.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasChildBounds(PptxTransform target)
    {
        return target.HasChildBounds();
    }

    /// <summary>
    /// Returns the child coordinate space used by a group transform.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds ChildBounds(PptxTransform target)
    {
        return target.ChildBounds();
    }

    /// <summary>
    /// Updates the child bounds and the underlying presentation data.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetChildBounds(PptxTransform target, PptxBounds bounds)
    {
        target.SetChildBounds(bounds);
    }

    /// <summary>
    /// Maps bounds from a group's child coordinate space into its parent coordinate space.
    /// </summary>
    /// <param name="childBounds">The child bounds value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds MapChildBounds(PptxTransform target, PptxBounds childBounds)
    {
        return target.MapChildBounds(childBounds);
    }

    /// <summary>
    /// Maps a point from a group's child coordinate space into its rotated parent coordinate space.
    /// </summary>
    /// <param name="childPoint">The child point value to use.</param>
    /// <returns>The resulting point value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPoint MapChildPoint(PptxTransform target, PptxPoint childPoint)
    {
        return target.MapChildPoint(childPoint);
    }

    /// <summary>
    /// Maps child bounds into axis-aligned parent bounds while applying scale, flips, and rotation.
    /// </summary>
    /// <param name="childBounds">The child bounds value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds MapChildAnchorBounds(PptxTransform target, PptxBounds childBounds)
    {
        return target.MapChildAnchorBounds(childBounds);
    }

    /// <summary>
    /// Scales an integer coordinate with checked arithmetic.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <param name="numerator">The numerator value to use.</param>
    /// <param name="denominator">The denominator value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    public long ScaleCoordinate(long value, long numerator, long denominator)
    {
        return PptxTransform.ScaleCoordinate(value, numerator, denominator);
    }
}
