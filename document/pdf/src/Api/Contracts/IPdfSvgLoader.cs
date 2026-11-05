using System;
using System.IO;
using System.Xml;

namespace pdf;

/// <summary>
/// Defines operations for securely loading the supported native SVG subset.
/// </summary>
public interface IPdfSvgLoader
{
    /// <summary>
    /// Loads SVG markup from a string.
    /// </summary>
    /// <param name="svgText">The complete SVG markup.</param>
    /// <returns>The validated SVG metadata and native element tree.</returns>
    /// <exception cref="ArgumentNullException">Thrown when SVG markup is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG markup is unsafe or unsupported.</exception>
    /// <exception cref="XmlException">Thrown when SVG markup is not well-formed XML.</exception>
    PdfSvgData Load(string svgText);

    /// <summary>
    /// Loads SVG markup from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete SVG markup.</param>
    /// <returns>The validated SVG metadata and native element tree.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG markup is unsafe or unsupported.</exception>
    /// <exception cref="XmlException">Thrown when SVG markup is not well-formed XML.</exception>
    PdfSvgData LoadFromStream(Stream stream);

    /// <summary>
    /// Loads SVG markup from a file.
    /// </summary>
    /// <param name="path">The path to the SVG file.</param>
    /// <returns>The validated SVG metadata and native element tree.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG markup is unsafe or unsupported.</exception>
    /// <exception cref="XmlException">Thrown when SVG markup is not well-formed XML.</exception>
    PdfSvgData LoadFromFile(string path);
}
