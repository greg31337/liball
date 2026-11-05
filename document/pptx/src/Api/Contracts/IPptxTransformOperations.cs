using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxTransformOperations"/>.
/// </summary>
public interface IPptxTransformOperations
{
    /// <summary>
    /// Creates a transform with zero bounds and default group and rotation values.
    /// </summary>
    /// <returns>The resulting transform value.</returns>
    PptxTransform CreateDefault();
    /// <summary>
    /// Returns the shape bounds represented by the transform.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds Bounds(PptxTransform target);
    /// <summary>
    /// Returns the axis-aligned bounds occupied after applying rotation around the transform center.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds AnchorBounds(PptxTransform target);
    /// <summary>
    /// Updates the bounds and the underlying presentation data.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    void SetBounds(PptxTransform target, PptxBounds bounds);
    /// <summary>
    /// Returns the clockwise rotation in degrees.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    double RotationDegrees(PptxTransform target);
    /// <summary>
    /// Updates the rotation degrees and the underlying presentation data.
    /// </summary>
    /// <param name="degrees">The degrees value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetRotationDegrees(PptxTransform target, double degrees);
    /// <summary>
    /// Reports whether the transform contains or enables child bounds.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasChildBounds(PptxTransform target);
    /// <summary>
    /// Returns the child coordinate space used by a group transform.
    /// </summary>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds ChildBounds(PptxTransform target);
    /// <summary>
    /// Updates the child bounds and the underlying presentation data.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    void SetChildBounds(PptxTransform target, PptxBounds bounds);
    /// <summary>
    /// Maps bounds from a group's child coordinate space into its parent coordinate space.
    /// </summary>
    /// <param name="childBounds">The child bounds value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds MapChildBounds(PptxTransform target, PptxBounds childBounds);
    /// <summary>
    /// Maps a point from a group's child coordinate space into its rotated parent coordinate space.
    /// </summary>
    /// <param name="childPoint">The child point value to use.</param>
    /// <returns>The resulting point value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPoint MapChildPoint(PptxTransform target, PptxPoint childPoint);
    /// <summary>
    /// Maps child bounds into axis-aligned parent bounds while applying scale, flips, and rotation.
    /// </summary>
    /// <param name="childBounds">The child bounds value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds MapChildAnchorBounds(PptxTransform target, PptxBounds childBounds);
    /// <summary>
    /// Scales an integer coordinate with checked arithmetic.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <param name="numerator">The numerator value to use.</param>
    /// <param name="denominator">The denominator value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    long ScaleCoordinate(long value, long numerator, long denominator);
}
