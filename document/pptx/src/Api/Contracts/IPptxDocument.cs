using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxDocument"/>.
/// </summary>
public interface IPptxDocument
{
    /// <summary>
    /// Serializes the current document and writes it to the requested destination.
    /// </summary>
    /// <remarks>An existing file at the path is replaced. Saving does not close or invalidate the document.</remarks>
    /// <param name="path">The destination presentation path.</param>
    void SaveFile(string path);
    /// <summary>
    /// Serializes the current document and writes it to the requested destination.
    /// </summary>
    /// <param name="stream">The readable or writable stream. The caller retains ownership of the stream.</param>
    void SaveStream(Stream stream);
    /// <summary>
    /// Serializes the current document and writes it to the requested destination.
    /// </summary>
    /// <param name="stream">The readable or writable stream. The caller retains ownership of the stream.</param>
    void Save(Stream stream);
    /// <summary>
    /// Serializes the current document and writes it to the requested destination.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    byte[] SaveBytes();
    /// <summary>
    /// Validates the document and returns all diagnostics found during the check.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    PptxResult Validate();
    /// <summary>
    /// Repairs recoverable package issues and returns diagnostics describing the changes.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    PptxResult Repair();
    /// <summary>
    /// Returns a deep copy that can be modified independently of the original document.
    /// </summary>
    /// <returns>The resulting document value.</returns>
    PptxDocument CloneDocument();
    /// <summary>
    /// Returns a deep copy that can be modified independently of the original document.
    /// </summary>
    /// <returns>The resulting document value.</returns>
    PptxDocument Clone();
    /// <summary>
    /// Returns the package without changing the document.
    /// </summary>
    /// <remarks>This is the mutable low-level package used by the document. Changes made through it affect subsequent validation and saves.</remarks>
    /// <returns>The resulting package value.</returns>
    PptxPackage GetPackage();
}
