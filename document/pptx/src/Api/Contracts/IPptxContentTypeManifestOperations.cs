using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxContentTypeManifestOperations"/>.
/// </summary>
public interface IPptxContentTypeManifestOperations
{
    /// <summary>
    /// Returns <c>true</c> when the operation has no error diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool Succeeded(PptxContentTypeManifest target);
    /// <summary>
    /// Returns <c>true</c> when the operation contains at least one error diagnostic.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool Failed(PptxContentTypeManifest target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int DefaultCount(PptxContentTypeManifest target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int OverrideCount(PptxContentTypeManifest target);
    /// <summary>
    /// Adds the default and returns the created model when applicable.
    /// </summary>
    /// <param name="extension">The extension value to use.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="target">The data value to operate on.</param>
    void AddDefault(PptxContentTypeManifest target, string extension, string contentType);
    /// <summary>
    /// Adds the override and returns the created model when applicable.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="contentType">The MIME content type for the package part.</param>
    /// <param name="target">The data value to operate on.</param>
    void AddOverride(PptxContentTypeManifest target, string partName, string contentType);
    /// <summary>
    /// Returns the default content type without changing the content type manifest.
    /// </summary>
    /// <param name="extension">The extension value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetDefaultContentType(PptxContentTypeManifest target, string extension);
    /// <summary>
    /// Returns the override content type without changing the content type manifest.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetOverrideContentType(PptxContentTypeManifest target, string partName);
    /// <summary>
    /// Returns the content type for part name without changing the content type manifest.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetContentTypeForPartName(PptxContentTypeManifest target, string partName);
}
