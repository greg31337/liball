using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxContentTypeManifest values.
/// </summary>
public class PptxContentTypeManifestOperations : IPptxContentTypeManifestOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxContentTypeManifestOperations()
    {
    }

    /// <summary>
    /// Returns <c>true</c> when the operation has no error diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Succeeded(PptxContentTypeManifest target)
    {
        return target.Succeeded();
    }

    /// <summary>
    /// Returns <c>true</c> when the operation contains at least one error diagnostic.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Failed(PptxContentTypeManifest target)
    {
        return target.Failed();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int DefaultCount(PptxContentTypeManifest target)
    {
        return target.DefaultCount();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int OverrideCount(PptxContentTypeManifest target)
    {
        return target.OverrideCount();
    }

    /// <summary>
    /// Adds the default and returns the created model when applicable.
    /// </summary>
    /// <param name="extension">The extension value to use.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="target">The data value to operate on.</param>
    public void AddDefault(PptxContentTypeManifest target, string extension, string contentType)
    {
        target.AddDefault(extension, contentType);
    }

    /// <summary>
    /// Adds the override and returns the created model when applicable.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="target">The data value to operate on.</param>
    public void AddOverride(PptxContentTypeManifest target, string partName, string contentType)
    {
        target.AddOverride(partName, contentType);
    }

    /// <summary>
    /// Returns the default content type without changing the content type manifest.
    /// </summary>
    /// <param name="extension">The extension value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetDefaultContentType(PptxContentTypeManifest target, string extension)
    {
        return target.GetDefaultContentType(extension);
    }

    /// <summary>
    /// Returns the override content type without changing the content type manifest.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetOverrideContentType(PptxContentTypeManifest target, string partName)
    {
        return target.GetOverrideContentType(partName);
    }

    /// <summary>
    /// Returns the content type for part name without changing the content type manifest.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetContentTypeForPartName(PptxContentTypeManifest target, string partName)
    {
        return target.GetContentTypeForPartName(partName);
    }
}
