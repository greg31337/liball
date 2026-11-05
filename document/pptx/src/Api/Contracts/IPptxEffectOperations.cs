using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxEffectOperations"/>.
/// </summary>
public interface IPptxEffectOperations
{
    /// <summary>
    /// Creates a new effect using the supplied values.
    /// </summary>
    /// <returns>The resulting effect value.</returns>
    PptxEffect CreateNone();
    /// <summary>
    /// Creates a new effect using the supplied values.
    /// </summary>
    /// <param name="color">The color value to use.</param>
    /// <param name="blurRadius">The blur radius value to use.</param>
    /// <param name="distance">The distance value to use.</param>
    /// <param name="direction">The direction value to use.</param>
    /// <returns>The resulting effect value.</returns>
    PptxEffect CreateOuterShadow(PptxColor color, long blurRadius, long distance, int direction);
    /// <summary>
    /// Creates a new effect using the supplied values.
    /// </summary>
    /// <param name="color">The color value to use.</param>
    /// <param name="radius">The radius value to use.</param>
    /// <returns>The resulting effect value.</returns>
    PptxEffect CreateGlow(PptxColor color, long radius);
    /// <summary>
    /// Creates an inner shadow effect.
    /// </summary>
    /// <param name="color">The shadow color.</param>
    /// <param name="blurRadius">The blur radius in English Metric Units (EMUs).</param>
    /// <param name="distance">The shadow distance in English Metric Units (EMUs).</param>
    /// <param name="direction">The shadow direction in DrawingML angle units.</param>
    /// <returns>The resulting effect value.</returns>
    PptxEffect CreateInnerShadow(PptxColor color, long blurRadius, long distance, int direction);
    /// <summary>
    /// Creates a new effect using the supplied values.
    /// </summary>
    /// <param name="blurRadius">The blur radius value to use.</param>
    /// <param name="distance">The distance value to use.</param>
    /// <param name="direction">The direction value to use.</param>
    /// <returns>The resulting effect value.</returns>
    PptxEffect CreateReflection(long blurRadius, long distance, int direction);
    /// <summary>
    /// Updates the outer shadow and the underlying presentation data.
    /// </summary>
    /// <param name="color">The color value to use.</param>
    /// <param name="blurRadius">The blur radius value to use.</param>
    /// <param name="distance">The distance value to use.</param>
    /// <param name="direction">The direction value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetOuterShadow(PptxEffect target, PptxColor color, long blurRadius, long distance, int direction);
    /// <summary>
    /// Sets an inner shadow effect.
    /// </summary>
    /// <param name="target">The effect to update.</param>
    /// <param name="color">The shadow color.</param>
    /// <param name="blurRadius">The blur radius in English Metric Units (EMUs).</param>
    /// <param name="distance">The shadow distance in English Metric Units (EMUs).</param>
    /// <param name="direction">The shadow direction in DrawingML angle units.</param>
    void SetInnerShadow(PptxEffect target, PptxColor color, long blurRadius, long distance, int direction);
    /// <summary>
    /// Sets shadow transform and transparency settings.
    /// </summary>
    /// <param name="target">The effect to update.</param>
    /// <param name="alignment">The shadow alignment.</param>
    /// <param name="rotateWithShape">Whether the shadow rotates with its shape.</param>
    /// <param name="scaleX">Horizontal scale in thousandths of one percent.</param>
    /// <param name="scaleY">Vertical scale in thousandths of one percent.</param>
    /// <param name="skewX">Horizontal skew in DrawingML angle units.</param>
    /// <param name="skewY">Vertical skew in DrawingML angle units.</param>
    /// <param name="transparency">Transparency from zero through 100,000.</param>
    void SetShadowTransform(PptxEffect target, string alignment, bool rotateWithShape, int scaleX, int scaleY, int skewX, int skewY, int transparency);
    /// <summary>
    /// Updates the glow and the underlying presentation data.
    /// </summary>
    /// <param name="color">The color value to use.</param>
    /// <param name="radius">The radius value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetGlow(PptxEffect target, PptxColor color, long radius);
    /// <summary>
    /// Sets glow transparency.
    /// </summary>
    /// <param name="target">The effect to update.</param>
    /// <param name="transparency">Transparency from zero through 100,000.</param>
    void SetGlowTransparency(PptxEffect target, int transparency);
    /// <summary>
    /// Updates the reflection and the underlying presentation data.
    /// </summary>
    /// <param name="blurRadius">The blur radius value to use.</param>
    /// <param name="distance">The distance value to use.</param>
    /// <param name="direction">The direction value to use.</param>
    /// <param name="startAlpha">The start alpha value to use.</param>
    /// <param name="endAlpha">The end alpha value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetReflection(PptxEffect target, long blurRadius, long distance, int direction, int startAlpha, int endAlpha);
    /// <summary>
    /// Sets reflection placement, scaling, skew, and rotation behavior.
    /// </summary>
    /// <param name="target">The effect to update.</param>
    /// <param name="startPosition">The start position from zero through 100,000.</param>
    /// <param name="endPosition">The end position from zero through 100,000.</param>
    /// <param name="fadeDirection">The fade direction in DrawingML angle units.</param>
    /// <param name="rotateWithShape">Whether the reflection rotates with its shape.</param>
    /// <param name="scaleX">Horizontal scale in thousandths of one percent.</param>
    /// <param name="scaleY">Vertical scale in thousandths of one percent.</param>
    /// <param name="skewX">Horizontal skew in DrawingML angle units.</param>
    /// <param name="skewY">Vertical skew in DrawingML angle units.</param>
    /// <param name="alignment">The reflection alignment.</param>
    void SetReflectionTransform(PptxEffect target, int startPosition, int endPosition, int fadeDirection, bool rotateWithShape, int scaleX, int scaleY, int skewX, int skewY, string alignment);
    /// <summary>
    /// Enables soft edges.
    /// </summary>
    /// <param name="target">The effect to update.</param>
    /// <param name="radius">The radius in English Metric Units (EMUs).</param>
    void SetSoftEdge(PptxEffect target, long radius);
    /// <summary>
    /// Sets the top bevel.
    /// </summary>
    /// <param name="target">The effect to update.</param>
    /// <param name="width">The bevel width in English Metric Units (EMUs).</param>
    /// <param name="height">The bevel height in English Metric Units (EMUs).</param>
    /// <param name="preset">The DrawingML bevel preset.</param>
    void SetTopBevel(PptxEffect target, long width, long height, string preset);
    /// <summary>
    /// Sets the bottom bevel.
    /// </summary>
    /// <param name="target">The effect to update.</param>
    /// <param name="width">The bevel width in English Metric Units (EMUs).</param>
    /// <param name="height">The bevel height in English Metric Units (EMUs).</param>
    /// <param name="preset">The DrawingML bevel preset.</param>
    void SetBottomBevel(PptxEffect target, long width, long height, string preset);
    /// <summary>
    /// Sets shape extrusion, contour, material, and colors.
    /// </summary>
    /// <param name="target">The effect to update.</param>
    /// <param name="z">The shape depth position in English Metric Units (EMUs).</param>
    /// <param name="extrusionHeight">The extrusion height in English Metric Units (EMUs).</param>
    /// <param name="contourWidth">The contour width in English Metric Units (EMUs).</param>
    /// <param name="material">The preset material.</param>
    /// <param name="extrusionColor">The extrusion color.</param>
    /// <param name="contourColor">The contour color.</param>
    void SetShapeThreeD(PptxEffect target, long z, long extrusionHeight, long contourWidth, string material, PptxColor extrusionColor, PptxColor contourColor);
    /// <summary>
    /// Sets camera and light-rig scene settings.
    /// </summary>
    /// <param name="target">The effect to update.</param>
    /// <param name="cameraPreset">The camera preset.</param>
    /// <param name="fieldOfView">The camera field of view in DrawingML angle units.</param>
    /// <param name="zoom">The camera zoom in thousandths of one percent.</param>
    /// <param name="cameraLatitude">Camera latitude rotation.</param>
    /// <param name="cameraLongitude">Camera longitude rotation.</param>
    /// <param name="cameraRevolution">Camera revolution rotation.</param>
    /// <param name="lightRig">The light rig preset.</param>
    /// <param name="lightDirection">The light direction.</param>
    /// <param name="lightLatitude">Light latitude rotation.</param>
    /// <param name="lightLongitude">Light longitude rotation.</param>
    /// <param name="lightRevolution">Light revolution rotation.</param>
    void SetSceneThreeD(PptxEffect target, string cameraPreset, int fieldOfView, int zoom, int cameraLatitude, int cameraLongitude, int cameraRevolution, string lightRig, string lightDirection, int lightLatitude, int lightLongitude, int lightRevolution);
    /// <summary>
    /// Reads effects from a DrawingML shape-properties element.
    /// </summary>
    /// <param name="xml">The shape-properties XML.</param>
    /// <returns>The parsed effect value.</returns>
    PptxEffect ParseShapePropertiesXml(string xml);
    /// <summary>
    /// Writes effects into a DrawingML shape-properties element.
    /// </summary>
    /// <param name="effect">The effect to write.</param>
    /// <returns>The serialized shape-properties XML.</returns>
    string ToShapePropertiesXml(PptxEffect effect);
}
