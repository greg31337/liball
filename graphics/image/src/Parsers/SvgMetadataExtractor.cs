using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace image;

internal class SvgMetadataExtractor
{
    internal const string XlinkNamespace = "http://www.w3.org/1999/xlink";
    internal const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    internal const string XmpMetaNamespace = "adobe:ns:meta/";

    internal static void Extract(ImageMetadataDocument document, string xmlText, long sourceLength, long xmlLength, ImageMetadataReaderOptions options)
    {
        if (document == null || xmlText == null || xmlText.Length == 0)
        {
            return;
        }

        SvgMetadataState state = ReadState(xmlText, SvgMetadataParser.MaxXmlCharacters(options), document.Diagnostics);
        MetadataDirectory documentDirectory = FindDocumentDirectory(document);
        AddDocumentFields(document, documentDirectory, state, sourceLength, xmlLength, options);
    }

    internal static SvgMetadataState ReadState(string xmlText, long maxCharacters, System.Collections.Generic.List<MetadataDiagnostic> diagnostics)
    {
        SvgMetadataState state = new SvgMetadataState();
        XmlReaderSettings settings = MetadataXmlReader.CreateSafeSettings(maxCharacters, diagnostics, "SVG");
        using (StringReader stringReader = new StringReader(xmlText))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.XmlResolver = null;
                xmlDocument.PreserveWhitespace = true;
                xmlDocument.Load(reader);
                XmlElement? root = xmlDocument.DocumentElement;
                if (root == null)
                {
                    return state;
                }

                state.Width = Attribute(root, "width", "");
                state.Height = Attribute(root, "height", "");
                state.ViewBox = Attribute(root, "viewBox", "");
                state.PreserveAspectRatio = Attribute(root, "preserveAspectRatio", "");
                ReadRootChildren(state, root);
                ReadImageLinks(state, root);
            }
        }

        return state;
    }

    internal static void ReadRootChildren(SvgMetadataState state, XmlElement root)
    {
        foreach (XmlNode node in root.ChildNodes)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)node;
                if (IsSvgElement(element, "title") && state.Title.Length == 0)
                {
                    state.Title = element.InnerText.Trim();
                }
                else if (IsSvgElement(element, "desc") && state.Description.Length == 0)
                {
                    state.Description = element.InnerText.Trim();
                }
                else if (IsSvgElement(element, "metadata"))
                {
                    state.MetadataXml.Add(element.OuterXml);
                }
            }
        }
    }

    internal static void ReadImageLinks(SvgMetadataState state, XmlElement root)
    {
        System.Collections.Generic.List<XmlNode> pending = new System.Collections.Generic.List<XmlNode>();
        pending.Add(root);
        while (pending.Count > 0)
        {
            int last = pending.Count - 1;
            XmlNode node = pending[last];
            pending.RemoveAt(last);
            if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)node;
                if (IsSvgElement(element, "image"))
                {
                    string href = Attribute(element, "href", "");
                    if (href.Length == 0)
                    {
                        href = Attribute(element, "href", XlinkNamespace);
                    }

                    if (href.Length > 0)
                    {
                        state.ImageLinks.Add(href);
                    }
                }
            }

            int index = node.ChildNodes.Count - 1;
            while (index >= 0)
            {
                XmlNode? child = node.ChildNodes[index];
                if (child != null)
                {
                    pending.Add(child);
                }
                index = index - 1;
            }
        }
    }

    internal static string Attribute(XmlElement element, string localName, string namespaceUri)
    {
        string value = namespaceUri.Length == 0 ? element.GetAttribute(localName) : element.GetAttribute(localName, namespaceUri);

        return value.Trim();
    }

    internal static bool IsSvgElement(XmlElement element, string localName)
    {
        if (element == null || element.LocalName != localName)
        {
            return false;
        }

        return element.NamespaceURI.Length == 0 || element.NamespaceURI == SvgMetadataParser.SvgNamespace;
    }

    internal static void AddDocumentFields(ImageMetadataDocument document, MetadataDirectory directory, SvgMetadataState state, long sourceLength, long xmlLength, ImageMetadataReaderOptions options)
    {
        SvgLength width = ParseLength(state.Width);
        SvgLength height = ParseLength(state.Height);
        SvgViewBox viewBox = ParseViewBox(state.ViewBox);

        if (state.Width.Length > 0)
        {
            SvgMetadataParser.AddTag(directory, "Svg.Document.Width", "Width", MetadataValue.FromString(state.Width), 0, sourceLength, state.Width);
            ValidateLength(document, width, "width");
        }

        if (state.Height.Length > 0)
        {
            SvgMetadataParser.AddTag(directory, "Svg.Document.Height", "Height", MetadataValue.FromString(state.Height), 0, sourceLength, state.Height);
            ValidateLength(document, height, "height");
        }

        if (state.ViewBox.Length > 0)
        {
            SvgMetadataParser.AddTag(directory, "Svg.Document.ViewBox", "View Box", MetadataValue.FromString(state.ViewBox), 0, sourceLength, state.ViewBox);
            if (viewBox.Valid)
            {
                SvgMetadataParser.AddTag(directory, "Svg.Document.ViewBoxMinX", "View Box Minimum X", MetadataValue.FromDouble(viewBox.MinX), 0, sourceLength, "");
                SvgMetadataParser.AddTag(directory, "Svg.Document.ViewBoxMinY", "View Box Minimum Y", MetadataValue.FromDouble(viewBox.MinY), 0, sourceLength, "");
                SvgMetadataParser.AddTag(directory, "Svg.Document.ViewBoxWidth", "View Box Width", MetadataValue.FromDouble(viewBox.Width), 0, sourceLength, "");
                SvgMetadataParser.AddTag(directory, "Svg.Document.ViewBoxHeight", "View Box Height", MetadataValue.FromDouble(viewBox.Height), 0, sourceLength, "");
                if (viewBox.Width <= 0 || viewBox.Height <= 0)
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg.viewbox.size.invalid", "SVG viewBox width and height must be greater than zero for metadata dimensions.", "SVG", 0, xmlLength);
                }
            }
            else
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg.viewbox.invalid", "SVG viewBox did not contain four valid numbers.", "SVG", 0, xmlLength);
            }
        }

        if (state.PreserveAspectRatio.Length > 0)
        {
            SvgMetadataParser.AddTag(directory, "Svg.Document.PreserveAspectRatio", "Preserve Aspect Ratio", MetadataValue.FromString(state.PreserveAspectRatio), 0, sourceLength, state.PreserveAspectRatio);
        }

        AddTextFields(document, directory, state, sourceLength);
        AddImageLinks(directory, state, sourceLength);
        AddMetadata(document, state, sourceLength, options);
        AddFrame(document, width, height, viewBox, options);
    }

    internal static void AddTextFields(ImageMetadataDocument document, MetadataDirectory directory, SvgMetadataState state, long sourceLength)
    {
        if (state.Title.Length > 0)
        {
            SvgMetadataParser.AddTag(directory, "Svg.Title", "Title", MetadataValue.FromString(state.Title), 0, sourceLength, state.Title);
        }

        if (state.Description.Length > 0)
        {
            SvgMetadataParser.AddTag(directory, "Svg.Description", "Description", MetadataValue.FromString(state.Description), 0, sourceLength, state.Description);
        }
    }

    internal static void AddImageLinks(MetadataDirectory directory, SvgMetadataState state, long sourceLength)
    {
        if (state.ImageLinks.Count == 0)
        {
            return;
        }

        MetadataValue value = new MetadataValue();
        value.Kind = MetadataValueKind.StringList;
        foreach (string link in state.ImageLinks)
        {
            value.AddTextValue(link);
        }

        SvgMetadataParser.AddTag(directory, "Svg.ImageLinks", "Image Links", value, 0, sourceLength, Join(state.ImageLinks));
        SvgMetadataParser.AddTag(directory, "Svg.ImageLinkCount", "Image Link Count", MetadataValue.FromInteger(state.ImageLinks.Count), 0, sourceLength, "");
    }

    internal static void AddMetadata(ImageMetadataDocument document, SvgMetadataState state, long sourceLength, ImageMetadataReaderOptions options)
    {
        int index = 0;
        foreach (string xml in state.MetadataXml)
        {
            MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Svg, "Metadata", "SVG Metadata");
            directory.Path = "SVG/Metadata/" + index.ToString();
            directory.Offset = 0;
            directory.Length = sourceLength;
            directory.RepeatIndex = index;
            SvgMetadataParser.AddTag(directory, "Svg.Metadata.Index", "Metadata Index", MetadataValue.FromInteger(index), 0, sourceLength, "");
            SvgMetadataParser.AddTag(directory, "Svg.Metadata.Xml", "Metadata XML", MetadataValue.FromXmlText(xml), 0, sourceLength, xml);
            bool xmp = IsXmp(xml);
            SvgMetadataParser.AddTag(directory, "Svg.Metadata.IsXmp", "Contains RDF or XMP", MetadataValue.FromBoolean(xmp), 0, sourceLength, "");
            if (xmp && ShouldParseXmp(options))
            {
                AddXmpProfile(document, xml, index, options);
            }

            index = index + 1;
        }
    }

    internal static void AddXmpProfile(ImageMetadataDocument document, string xml, int index, ImageMetadataReaderOptions options)
    {
        byte[] data = Encoding.UTF8.GetBytes(xml);
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Xmp;
        profile.Family = MetadataFamily.Xmp;
        profile.Key = "Xmp.SVG.Metadata";
        profile.Name = "SVG RDF/XMP Metadata";
        profile.MimeType = "application/rdf+xml";
        profile.Offset = 0;
        profile.Length = data.Length;
        profile.SequenceNumber = index + 1;
        profile.SequenceCount = 0;
        if ((long)data.Length <= MaxProfileBytes(options))
        {
            profile.Data = data;
            profile.Text = xml;
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "svg.xmp.profile.limit", "SVG RDF/XMP metadata exceeded the configured profile limit.", "metadata", 0, data.Length);
        }

        document.AddProfile(profile);
        XmpPacketParser.ParseProfile(document, profile, options, "SVG/metadata/XMP");
    }

    internal static void AddFrame(ImageMetadataDocument document, SvgLength width, SvgLength height, SvgViewBox viewBox, ImageMetadataReaderOptions options)
    {
        double widthPixels = width.Resolved ? width.Pixels : 0;
        double heightPixels = height.Resolved ? height.Pixels : 0;
        if (widthPixels <= 0 && heightPixels > 0 && ValidViewBoxSize(viewBox))
        {
            widthPixels = heightPixels * viewBox.Width / viewBox.Height;
        }
        else if (heightPixels <= 0 && widthPixels > 0 && ValidViewBoxSize(viewBox))
        {
            heightPixels = widthPixels * viewBox.Height / viewBox.Width;
        }
        else if (widthPixels <= 0 && heightPixels <= 0 && ValidViewBoxSize(viewBox))
        {
            widthPixels = viewBox.Width;
            heightPixels = viewBox.Height;
        }

        int pixelWidth = PixelDimension(widthPixels);
        int pixelHeight = PixelDimension(heightPixels);
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return;
        }

        if (document.Frames.Count >= MaxFrameCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "svg.frame.limit", "SVG frame count exceeded the configured frame limit.", "SVG", 0, 0);
            return;
        }

        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Width = pixelWidth;
        frame.Height = pixelHeight;
        frame.CanvasWidth = pixelWidth;
        frame.CanvasHeight = pixelHeight;
        frame.ChannelCount = 4;
        frame.HasAlpha = true;
        frame.Compression = ImageCompressionKind.None;
        frame.ColorSpace = ImageColorSpaceKind.Rgb;
        frame.Orientation = ImageOrientation.Normal;
        document.AddFrame(frame);
    }

    internal static SvgLength ParseLength(string text)
    {
        SvgLength length = new SvgLength();
        length.Text = text == null ? "" : text.Trim();
        if (length.Text.ToLowerInvariant() == "auto")
        {
            length.Unit = "auto";
            length.Valid = true;
            return length;
        }

        SvgNumber number = ParseNumber(length.Text, 0);
        if (!number.Valid)
        {
            return length;
        }

        string unit = length.Text.Substring(number.End).Trim().ToLowerInvariant();
        length.Unit = unit;
        length.Valid = true;
        double factor = 0;
        if (unit.Length == 0 || unit == "px")
        {
            factor = 1;
        }
        else if (unit == "in")
        {
            factor = 96;
        }
        else if (unit == "cm")
        {
            factor = 96.0 / 2.54;
        }
        else if (unit == "mm")
        {
            factor = 96.0 / 25.4;
        }
        else if (unit == "q")
        {
            factor = 96.0 / 101.6;
        }
        else if (unit == "pt")
        {
            factor = 96.0 / 72.0;
        }
        else if (unit == "pc")
        {
            factor = 16;
        }

        if (factor > 0)
        {
            length.Pixels = number.Value * factor;
            length.Resolved = !double.IsNaN(length.Pixels) && !double.IsInfinity(length.Pixels);
        }

        return length;
    }

    internal static SvgViewBox ParseViewBox(string text)
    {
        SvgViewBox viewBox = new SvgViewBox();
        viewBox.Text = text == null ? "" : text.Trim();
        double[] values = new double[4];
        int offset = 0;
        int index = 0;
        while (index < values.Length)
        {
            offset = SkipSeparators(viewBox.Text, offset);
            SvgNumber number = ParseNumber(viewBox.Text, offset);
            if (!number.Valid)
            {
                return viewBox;
            }

            values[index] = number.Value;
            offset = number.End;
            index = index + 1;
        }

        offset = SkipSeparators(viewBox.Text, offset);
        if (offset != viewBox.Text.Length)
        {
            return viewBox;
        }

        viewBox.MinX = values[0];
        viewBox.MinY = values[1];
        viewBox.Width = values[2];
        viewBox.Height = values[3];
        viewBox.Valid = true;
        return viewBox;
    }

    internal static SvgNumber ParseNumber(string text, int offset)
    {
        SvgNumber result = new SvgNumber();
        if (text == null || offset < 0 || offset >= text.Length)
        {
            return result;
        }

        int index = offset;
        bool negative = false;
        if (text[index] == '+' || text[index] == '-')
        {
            negative = text[index] == '-';
            index = index + 1;
        }

        double value = 0;
        int digitCount = 0;
        while (index < text.Length && text[index] >= '0' && text[index] <= '9')
        {
            value = value * 10 + (text[index] - '0');
            digitCount = digitCount + 1;
            index = index + 1;
        }

        if (index < text.Length && text[index] == '.')
        {
            index = index + 1;
            double place = 0.1;
            while (index < text.Length && text[index] >= '0' && text[index] <= '9')
            {
                value = value + (text[index] - '0') * place;
                place = place * 0.1;
                digitCount = digitCount + 1;
                index = index + 1;
            }
        }

        if (digitCount == 0)
        {
            return result;
        }

        int exponent = 0;
        bool exponentNegative = false;
        if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
        {
            int exponentMarker = index;
            index = index + 1;
            if (index < text.Length && (text[index] == '+' || text[index] == '-'))
            {
                exponentNegative = text[index] == '-';
                index = index + 1;
            }

            int exponentDigits = 0;
            while (index < text.Length && text[index] >= '0' && text[index] <= '9')
            {
                if (exponent < 10000)
                {
                    exponent = exponent * 10 + (text[index] - '0');
                }

                exponentDigits = exponentDigits + 1;
                index = index + 1;
            }

            if (exponentDigits == 0)
            {
                index = exponentMarker;
                exponent = 0;
                exponentNegative = false;
            }
        }

        if (exponent != 0)
        {
            value = value * Math.Pow(10, exponentNegative ? -exponent : exponent);
        }

        if (negative)
        {
            value = -value;
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return result;
        }

        result.Valid = true;
        result.Value = value;
        result.End = index;
        return result;
    }

    internal static int SkipSeparators(string text, int offset)
    {
        while (offset < text.Length && (char.IsWhiteSpace(text[offset]) || text[offset] == ','))
        {
            offset = offset + 1;
        }

        return offset;
    }

    internal static void ValidateLength(ImageMetadataDocument document, SvgLength length, string name)
    {
        if (!length.Valid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg." + name + ".invalid", "SVG " + name + " was not a valid length.", "SVG", 0, length.Text.Length);
        }
        else if (!length.Resolved)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "svg." + name + ".unresolved", "SVG " + name + " used a relative or unsupported unit.", "SVG", 0, length.Text.Length);
        }
        else if (length.Pixels <= 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg." + name + ".nonpositive", "SVG " + name + " must be greater than zero for metadata dimensions.", "SVG", 0, length.Text.Length);
        }
        else if (length.Pixels > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "svg." + name + ".limit", "SVG " + name + " exceeded the supported pixel dimension.", "SVG", 0, length.Text.Length);
        }
    }

    internal static int PixelDimension(double value)
    {
        if (value <= 0 || value > 2147483647 || double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        double rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        if (rounded < 1)
        {
            return 1;
        }

        return (int)rounded;
    }

    internal static bool ValidViewBoxSize(SvgViewBox viewBox)
    {
        return viewBox != null && viewBox.Valid && viewBox.Width > 0 && viewBox.Height > 0;
    }

    internal static bool IsXmp(string xml)
    {
        if (xml == null)
        {
            return false;
        }

        return xml.IndexOf(RdfNamespace, StringComparison.OrdinalIgnoreCase) >= 0
            || xml.IndexOf(XmpMetaNamespace, StringComparison.OrdinalIgnoreCase) >= 0
            || xml.IndexOf("<rdf:RDF", StringComparison.OrdinalIgnoreCase) >= 0
            || xml.IndexOf("<x:xmpmeta", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static string Join(System.Collections.Generic.List<string> values)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        foreach (string value in values)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(value);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static MetadataDirectory FindDocumentDirectory(ImageMetadataDocument document)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Family == MetadataFamily.Svg && directory.Group == "Document")
            {
                return directory;
            }
        }

        return document.AddDirectoryWithName(MetadataFamily.Svg, "Document", "SVG Document");
    }

    internal static bool ShouldParseXmp(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseXmp;
    }

    internal static long MaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }

    internal static int MaxFrameCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxFrameCount : options.MaxFrameCount;
    }
}
