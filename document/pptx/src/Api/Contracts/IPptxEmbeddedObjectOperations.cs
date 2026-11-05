using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxEmbeddedObjectOperations"/>.
/// </summary>
public interface IPptxEmbeddedObjectOperations
{
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
    PptxEmbeddedObject AddPackage(PptxSlide slide, byte[] bytes, string contentType, string programId, string objectName, PptxBounds bounds);
    /// <summary>
    /// Reports whether the embedded object is ole object.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsOleObject(PptxEmbeddedObject target);
    /// <summary>
    /// Reports whether the embedded object is embedded package.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsEmbeddedPackage(PptxEmbeddedObject target);
    /// <summary>
    /// Returns the bytes without changing the embedded object.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    /// <param name="target">The data value to operate on.</param>
    byte[] GetBytes(PptxEmbeddedObject target);
    /// <summary>
    /// Replaces bytes while preserving unrelated presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="target">The data value to operate on.</param>
    void ReplaceBytes(PptxEmbeddedObject target, byte[] bytes, string contentType);
    /// <summary>
    /// Removes the embedded object shape, relationship, and unreferenced package part.
    /// </summary>
    /// <param name="target">The embedded object to remove.</param>
    void Remove(PptxEmbeddedObject target);
    /// <summary>
    /// Scans the embedded object for potentially unsafe embedded or linked content.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxResult ScanSecurity(PptxEmbeddedObject target);
    /// <summary>
    /// Resolves a relationship identifier from the shape's owning slide.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <returns>The matching relationship, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxRelationship? ResolveRelationship(PptxEmbeddedObject target, string relationshipId);
}
