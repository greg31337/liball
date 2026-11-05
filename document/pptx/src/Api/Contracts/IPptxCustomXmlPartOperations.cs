using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxCustomXmlPartOperations"/>.
/// </summary>
public interface IPptxCustomXmlPartOperations
{
    /// <summary>
    /// Returns the bytes without changing the custom xml part.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    /// <param name="target">The data value to operate on.</param>
    byte[] GetBytes(PptxCustomXmlPart target);
    /// <summary>
    /// Updates the XML text and the underlying custom XML part.
    /// </summary>
    /// <param name="xml">The XML text to store as UTF-8.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetXml(PptxCustomXmlPart target, string xml);
    /// <summary>
    /// Replaces the custom XML part bytes and content type.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="target">The data value to operate on.</param>
    void ReplaceBytes(PptxCustomXmlPart target, byte[] bytes, string contentType);
    /// <summary>
    /// Returns the relationships owned by the custom XML part without changing them.
    /// </summary>
    /// <returns>A copy of the relationships list in package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxRelationship> GetRelationships(PptxCustomXmlPart target);
    /// <summary>
    /// Adds a relationship owned by the custom XML part.
    /// </summary>
    /// <param name="type">The relationship type URI.</param>
    /// <param name="relationshipTarget">The stored internal or external target URI.</param>
    /// <param name="targetMode">Whether the relationship is internal or external.</param>
    /// <returns>The created relationship.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxRelationship AddRelationship(PptxCustomXmlPart target, string type, string relationshipTarget, PptxRelationshipTargetMode targetMode);
    /// <summary>
    /// Replaces the target of a relationship owned by the custom XML part.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier to edit.</param>
    /// <param name="relationshipTarget">The stored internal or external target URI.</param>
    /// <param name="targetMode">Whether the relationship is internal or external.</param>
    /// <returns>The updated relationship.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxRelationship SetRelationshipTarget(PptxCustomXmlPart target, string relationshipId, string relationshipTarget, PptxRelationshipTargetMode targetMode);
    /// <summary>
    /// Removes a relationship owned by the custom XML part.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier to remove.</param>
    /// <param name="target">The data value to operate on.</param>
    void RemoveRelationship(PptxCustomXmlPart target, string relationshipId);
    /// <summary>
    /// Removes the custom XML part and all package relationships that target it.
    /// </summary>
    /// <param name="target">The custom XML part to remove.</param>
    /// <returns><c>true</c> when the part was removed; otherwise, <c>false</c>.</returns>
    bool Remove(PptxCustomXmlPart target);
}
