using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxPart values.
/// </summary>
public class PptxPartOperations : IPptxPartOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxPartOperations()
    {
    }

    /// <summary>
    /// Creates a new part using the supplied values.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting part value.</returns>
    public PptxPart Create(string partName, string contentType, byte[] bytes)
    {
        return PptxPart.Create(partName, contentType, bytes);
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ByteCount(PptxPart target)
    {
        return target.ByteCount();
    }

    /// <summary>
    /// Reports whether the part contains or enables relationships.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasRelationships(PptxPart target)
    {
        return target.HasRelationships();
    }

    /// <summary>
    /// Reports whether the part is xml content type.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsXmlContentType(PptxPart target)
    {
        return target.IsXmlContentType();
    }

    /// <summary>
    /// Returns the bytes without changing the part.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    /// <param name="target">The data value to operate on.</param>
    public byte[] GetBytes(PptxPart target)
    {
        return target.GetBytes();
    }

    /// <summary>
    /// Updates the bytes and the underlying presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetBytes(PptxPart target, byte[] bytes)
    {
        target.SetBytes(bytes);
    }

    /// <summary>
    /// Updates the content type and the underlying presentation data.
    /// </summary>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetContentType(PptxPart target, string contentType)
    {
        target.SetContentType(contentType);
    }

    /// <summary>
    /// Updates the relationships and the underlying presentation data.
    /// </summary>
    /// <param name="relationships">The relationships value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetRelationships(PptxPart target, PptxRelationshipCollection relationships)
    {
        target.SetRelationships(relationships);
    }
}
