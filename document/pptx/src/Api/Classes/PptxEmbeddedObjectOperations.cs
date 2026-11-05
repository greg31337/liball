using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxEmbeddedObject values.
/// </summary>
public class PptxEmbeddedObjectOperations : IPptxEmbeddedObjectOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxEmbeddedObjectOperations()
    {
    }

    /// <summary>
    /// Adds a generic embedded package to a slide.
    /// </summary>
    /// <param name="slide">The slide to modify.</param>
    /// <param name="bytes">The embedded package payload.</param>
    /// <param name="contentType">The embedded package content type.</param>
    /// <param name="programId">The program identifier or package role.</param>
    /// <param name="objectName">The display name for the embedded object.</param>
    /// <param name="bounds">The object bounds in English Metric Units (EMUs).</param>
    /// <returns>The created embedded package object.</returns>
    public PptxEmbeddedObject AddPackage(PptxSlide slide, byte[] bytes, string contentType, string programId, string objectName, PptxBounds bounds)
    {
        if (slide.Presentation == null)
        {
            throw new System.ArgumentException("Slide is not attached to a presentation.");
        }

        PptxShape shape = slide.Presentation.AddEmbeddedPackageShapeToSlide(slide, bytes, contentType, programId, objectName, bounds);
        return shape.AsEmbeddedObject();
    }

    /// <summary>
    /// Reports whether the embedded object is ole object.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsOleObject(PptxEmbeddedObject target)
    {
        return target.IsOleObject();
    }

    /// <summary>
    /// Reports whether the embedded object is embedded package.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsEmbeddedPackage(PptxEmbeddedObject target)
    {
        return target.IsEmbeddedPackage();
    }

    /// <summary>
    /// Returns the bytes without changing the embedded object.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    /// <param name="target">The data value to operate on.</param>
    public byte[] GetBytes(PptxEmbeddedObject target)
    {
        return target.GetBytes();
    }

    /// <summary>
    /// Replaces bytes while preserving unrelated presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="target">The data value to operate on.</param>
    public void ReplaceBytes(PptxEmbeddedObject target, byte[] bytes, string contentType)
    {
        target.ReplaceBytes(bytes, contentType);
    }

    /// <summary>
    /// Removes the embedded object shape, relationship, and unreferenced package part.
    /// </summary>
    /// <param name="target">The embedded object to remove.</param>
    public void Remove(PptxEmbeddedObject target)
    {
        target.Remove();
    }

    /// <summary>
    /// Scans the embedded object for potentially unsafe embedded or linked content.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxResult ScanSecurity(PptxEmbeddedObject target)
    {
        return target.ScanSecurity();
    }

    /// <summary>
    /// Resolves a relationship identifier from the shape's owning slide.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <returns>The matching relationship, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxRelationship? ResolveRelationship(PptxEmbeddedObject target, string relationshipId)
    {
        return target.ResolveRelationship(relationshipId);
    }
}
