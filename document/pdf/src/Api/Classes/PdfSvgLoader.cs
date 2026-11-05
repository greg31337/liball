using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace pdf;

/// <summary>
/// Securely loads intrinsic SVG metadata using built-in XML APIs.
/// </summary>
public class PdfSvgLoader : IPdfSvgLoader
{
    /// <summary>
    /// Initializes an SVG loader.
    /// </summary>
    public PdfSvgLoader()
    {
    }

    /// <summary>
    /// Loads SVG markup from a string.
    /// </summary>
    /// <param name="svgText">The complete SVG markup.</param>
    /// <returns>The validated SVG metadata and native element tree.</returns>
    /// <exception cref="ArgumentNullException">Thrown when SVG markup is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG markup is unsafe or unsupported.</exception>
    /// <exception cref="XmlException">Thrown when SVG markup is not well-formed XML.</exception>
    public PdfSvgData Load(string svgText)
    {
        if (svgText == null)
        {
            throw new ArgumentNullException("svgText");
        }

        RejectUnsafeDeclarations(svgText);
        XmlReaderSettings settings = CreateSettings();
        using (StringReader input = new StringReader(svgText))
        {
            using (XmlReader reader = XmlReader.Create(input, settings))
            {
                return ParseReader(reader);
            }
        }
    }

    /// <summary>
    /// Loads SVG markup from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete SVG markup.</param>
    /// <returns>The validated SVG metadata and native element tree.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG markup is unsafe or unsupported.</exception>
    /// <exception cref="XmlException">Thrown when SVG markup is not well-formed XML.</exception>
    public PdfSvgData LoadFromStream(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("SVG stream must be readable.", "stream");
        }

        XmlReaderSettings settings = CreateSettings();
        using (XmlReader reader = XmlReader.Create(stream, settings))
        {
            return ParseReader(reader);
        }
    }

    /// <summary>
    /// Loads SVG markup from a file.
    /// </summary>
    /// <param name="path">The path to the SVG file.</param>
    /// <returns>The validated SVG metadata and native element tree.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG markup is unsafe or unsupported.</exception>
    /// <exception cref="XmlException">Thrown when SVG markup is not well-formed XML.</exception>
    public PdfSvgData LoadFromFile(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        using (Stream input = File.OpenRead(path))
        {
            return LoadFromStream(input);
        }
    }

    internal static XmlReaderSettings CreateSettings()
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.MaxCharactersFromEntities = 0;
        settings.MaxCharactersInDocument = 10000000;
        return settings;
    }

    internal static void RejectUnsafeDeclarations(string svgText)
    {
        if (svgText.IndexOf("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) >= 0 || svgText.IndexOf("<!ENTITY", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new InvalidDataException("SVG document type and entity declarations are not permitted.");
        }
    }

    internal static PdfSvgData ParseReader(XmlReader reader)
    {
        XmlDocument document = new XmlDocument();
        document.XmlResolver = null;
        document.PreserveWhitespace = false;
        document.Load(reader);
        XmlElement? root = document.DocumentElement;
        if (root == null || root.LocalName != "svg")
        {
            throw new InvalidDataException("SVG markup must contain an svg root element.");
        }

        if (root.NamespaceURI.Length > 0 && root.NamespaceURI != "http://www.w3.org/2000/svg")
        {
            throw new InvalidDataException("SVG root element uses an unsupported namespace.");
        }

        bool hasViewBox = root.HasAttribute("viewBox");
        double viewBoxX = 0;
        double viewBoxY = 0;
        double viewBoxWidth = 0;
        double viewBoxHeight = 0;
        if (hasViewBox)
        {
            double[] values = PdfSvgValueParser.ParseNumberList(root.GetAttribute("viewBox"));
            if (values.Length != 4 || values[2] <= 0 || values[3] <= 0)
            {
                throw new InvalidDataException("SVG viewBox must contain four numbers with positive dimensions.");
            }

            viewBoxX = values[0];
            viewBoxY = values[1];
            viewBoxWidth = values[2];
            viewBoxHeight = values[3];
        }

        bool hasWidth = root.HasAttribute("width");
        bool hasHeight = root.HasAttribute("height");
        double width = hasWidth ? PdfSvgValueParser.ParseLengthPoints(root.GetAttribute("width")) : hasViewBox ? viewBoxWidth * 0.75 : 225;
        double height = hasHeight ? PdfSvgValueParser.ParseLengthPoints(root.GetAttribute("height")) : hasViewBox ? viewBoxHeight * 0.75 : 112.5;
        if (width <= 0 || height <= 0 || double.IsNaN(width) || double.IsNaN(height) || double.IsInfinity(width) || double.IsInfinity(height))
        {
            throw new InvalidDataException("SVG intrinsic dimensions must be positive and finite.");
        }

        if (!hasViewBox)
        {
            viewBoxWidth = hasWidth ? PdfSvgValueParser.ParseLengthUserUnits(root.GetAttribute("width")) : 300;
            viewBoxHeight = hasHeight ? PdfSvgValueParser.ParseLengthUserUnits(root.GetAttribute("height")) : 150;
        }

        return new PdfSvgData(width, height, hasViewBox, viewBoxX, viewBoxY, viewBoxWidth, viewBoxHeight, root);
    }
}
