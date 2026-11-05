using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxCustomXmlPart values.
/// </summary>
public class PptxCustomXmlPartOperations : IPptxCustomXmlPartOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxCustomXmlPartOperations()
    {
    }

    /// <summary>
    /// Returns the bytes without changing the custom xml part.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    /// <param name="target">The data value to operate on.</param>
    public byte[] GetBytes(PptxCustomXmlPart target)
    {
        return target.GetBytes();
    }
    /// <summary>
    /// Updates the XML text and the underlying custom XML part.
    /// </summary>
    /// <param name="xml">The XML text to store as UTF-8.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetXml(PptxCustomXmlPart target, string xml)
    {
        target.SetXml(xml);
    }
    /// <summary>
    /// Replaces the custom XML part bytes and content type.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="target">The data value to operate on.</param>
    public void ReplaceBytes(PptxCustomXmlPart target, byte[] bytes, string contentType)
    {
        target.ReplaceBytes(bytes, contentType);
    }
    /// <summary>
    /// Returns the relationships owned by the custom XML part without changing them.
    /// </summary>
    /// <returns>A copy of the relationships list in package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxRelationship> GetRelationships(PptxCustomXmlPart target)
    {
        return target.GetRelationships();
    }
    /// <summary>
    /// Adds a relationship owned by the custom XML part.
    /// </summary>
    /// <param name="type">The relationship type URI.</param>
    /// <param name="relationshipTarget">The stored internal or external target URI.</param>
    /// <param name="targetMode">Whether the relationship is internal or external.</param>
    /// <returns>The created relationship.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxRelationship AddRelationship(PptxCustomXmlPart target, string type, string relationshipTarget, PptxRelationshipTargetMode targetMode)
    {
        return target.AddRelationship(type, relationshipTarget, targetMode);
    }
    /// <summary>
    /// Replaces the target of a relationship owned by the custom XML part.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier to edit.</param>
    /// <param name="relationshipTarget">The stored internal or external target URI.</param>
    /// <param name="targetMode">Whether the relationship is internal or external.</param>
    /// <returns>The updated relationship.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxRelationship SetRelationshipTarget(PptxCustomXmlPart target, string relationshipId, string relationshipTarget, PptxRelationshipTargetMode targetMode)
    {
        return target.SetRelationshipTarget(relationshipId, relationshipTarget, targetMode);
    }
    /// <summary>
    /// Removes a relationship owned by the custom XML part.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier to remove.</param>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveRelationship(PptxCustomXmlPart target, string relationshipId)
    {
        target.RemoveRelationship(relationshipId);
    }
    /// <summary>
    /// Removes the custom XML part and all package relationships that target it.
    /// </summary>
    /// <param name="target">The custom XML part to remove.</param>
    /// <returns><c>true</c> when the part was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(PptxCustomXmlPart target)
    {
        return target.Remove();
    }
}
