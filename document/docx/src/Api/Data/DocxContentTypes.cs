using System.Text;
using System.Xml;

namespace docx;

/// <summary>
/// Maps package file extensions and part names to MIME content types.
/// </summary>
public class DocxContentTypes
{
    /// <summary>
    /// The default content type mappings.
    /// </summary>
    public List<DocxContentTypeDefault> Defaults;
    /// <summary>
    /// The part-specific content type mappings.
    /// </summary>
    public List<DocxContentTypeOverride> Overrides;

    /// <summary>
    /// Creates a content types instance.
    /// </summary>
    public DocxContentTypes()
    {
        this.Defaults = new List<DocxContentTypeDefault>();
        this.Overrides = new List<DocxContentTypeOverride>();
    }

    internal static DocxContentTypes Parse(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxContentTypes contentTypes = new DocxContentTypes();

        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element
                        && reader.LocalName == "Default"
                        && reader.NamespaceURI == DocxConstants.NamespaceContentTypes)
                    {
                        string? extension = reader.GetAttribute("Extension");
                        string? contentType = reader.GetAttribute("ContentType");

                        if (extension == null)
                        {
                            throw new InvalidDataException("Content type default is missing Extension.");
                        }

                        if (contentType == null)
                        {
                            throw new InvalidDataException("Content type default is missing ContentType.");
                        }

                        contentTypes.AddDefault(extension, contentType);
                    }

                    if (reader.NodeType == XmlNodeType.Element
                        && reader.LocalName == "Override"
                        && reader.NamespaceURI == DocxConstants.NamespaceContentTypes)
                    {
                        string? partName = reader.GetAttribute("PartName");
                        string? contentType = reader.GetAttribute("ContentType");

                        if (partName == null)
                        {
                            throw new InvalidDataException("Content type override is missing PartName.");
                        }

                        if (contentType == null)
                        {
                            throw new InvalidDataException("Content type override is missing ContentType.");
                        }

                        contentTypes.AddOverride(partName, contentType);
                    }
                }
            }
        }

        return contentTypes;
    }

    internal static DocxContentTypes Parse(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
        {
            return Parse(reader.ReadToEnd());
        }
    }

    internal void AddDefault(string extension, string contentType)
    {
        string normalizedExtension = NormalizeExtension(extension);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        this.Defaults.Add(new DocxContentTypeDefault(normalizedExtension, contentType));
    }

    internal void SetDefault(string extension, string contentType)
    {
        string normalizedExtension = NormalizeExtension(extension);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        DocxContentTypeDefault? value = FindDefault(normalizedExtension);

        if (value == null)
        {
            this.Defaults.Add(new DocxContentTypeDefault(normalizedExtension, contentType));
        }
        else
        {
            value.ContentType = contentType;
        }
    }

    internal DocxContentTypeDefault CreateDefaultMapping(string extension, string contentType)
    {
        string normalizedExtension = NormalizeExtension(extension);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        if (FindDefault(normalizedExtension) != null)
        {
            throw new InvalidOperationException("Default content type already exists.");
        }

        DocxContentTypeDefault value = new DocxContentTypeDefault(normalizedExtension, contentType);
        this.Defaults.Add(value);
        return value;
    }

    internal bool UpdateDefaultMapping(string extension, string contentType)
    {
        string normalizedExtension = NormalizeExtension(extension);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        DocxContentTypeDefault? value = FindDefault(normalizedExtension);

        if (value == null)
        {
            return false;
        }

        value.ContentType = contentType;
        return true;
    }

    internal bool RemoveDefault(string extension)
    {
        string normalizedExtension = NormalizeExtension(extension);

        for (int index = 0; index < this.Defaults.Count; index = index + 1)
        {
            DocxContentTypeDefault value = this.Defaults[index];

            if (string.Equals(value.Extension, normalizedExtension, StringComparison.OrdinalIgnoreCase))
            {
                this.Defaults.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    internal void AddOverride(string partName, string contentType)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        this.Overrides.Add(new DocxContentTypeOverride(normalizedPartName, contentType));
    }

    internal void SetOverride(string partName, string contentType)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        DocxContentTypeOverride? value = FindOverride(normalizedPartName);

        if (value == null)
        {
            this.Overrides.Add(new DocxContentTypeOverride(normalizedPartName, contentType));
        }
        else
        {
            value.ContentType = contentType;
        }
    }

    internal DocxContentTypeOverride CreateOverrideMapping(string partName, string contentType)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        if (FindOverride(normalizedPartName) != null)
        {
            throw new InvalidOperationException("Override content type already exists.");
        }

        DocxContentTypeOverride value = new DocxContentTypeOverride(normalizedPartName, contentType);
        this.Overrides.Add(value);
        return value;
    }

    internal bool UpdateOverrideMapping(string partName, string contentType)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        DocxContentTypeOverride? value = FindOverride(normalizedPartName);

        if (value == null)
        {
            return false;
        }

        value.ContentType = contentType;
        return true;
    }

    internal bool RemoveOverride(string partName)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        for (int index = 0; index < this.Overrides.Count; index = index + 1)
        {
            DocxContentTypeOverride value = this.Overrides[index];

            if (value.PartName == normalizedPartName)
            {
                this.Overrides.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    internal void SetContentTypeForPart(string partName, string contentType)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == "")
        {
            throw new ArgumentException("Content type is required.", "contentType");
        }

        string extension = GetExtensionFromPartName(normalizedPartName);
        string defaultContentType = "";

        if (extension != "")
        {
            defaultContentType = GetDefaultContentType(extension);
        }

        if (defaultContentType == contentType)
        {
            RemoveOverride(normalizedPartName);
            return;
        }

        SetOverride(normalizedPartName, contentType);
    }

    internal bool RemoveContentTypeForPart(string partName)
    {
        return RemoveOverride(partName);
    }

    internal DocxContentTypeDefault? FindDefault(string extension)
    {
        string normalizedExtension = NormalizeExtension(extension);

        foreach (DocxContentTypeDefault value in this.Defaults)
        {
            if (string.Equals(value.Extension, normalizedExtension, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    internal string GetDefaultContentType(string extension)
    {
        DocxContentTypeDefault? value = FindDefault(extension);

        if (value == null)
        {
            return "";
        }

        return value.ContentType;
    }

    internal DocxContentTypeOverride? FindOverride(string partName)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);

        foreach (DocxContentTypeOverride value in this.Overrides)
        {
            if (value.PartName == normalizedPartName)
            {
                return value;
            }
        }

        return null;
    }

    internal string GetOverrideContentType(string partName)
    {
        DocxContentTypeOverride? value = FindOverride(partName);

        if (value == null)
        {
            return "";
        }

        return value.ContentType;
    }

    internal string GetContentType(string partName)
    {
        string overrideContentType = GetOverrideContentType(partName);

        if (overrideContentType != "")
        {
            return overrideContentType;
        }

        string extension = GetExtensionFromPartName(partName);

        if (extension == "")
        {
            return "";
        }

        return GetDefaultContentType(extension);
    }

    internal List<DocxContentTypeDefault> GetDefaultMappings()
    {
        List<DocxContentTypeDefault> values = new List<DocxContentTypeDefault>();

        foreach (DocxContentTypeDefault value in this.Defaults)
        {
            values.Add(value);
        }

        return values;
    }

    internal List<DocxContentTypeOverride> GetOverrideMappings()
    {
        List<DocxContentTypeOverride> values = new List<DocxContentTypeOverride>();

        foreach (DocxContentTypeOverride value in this.Overrides)
        {
            values.Add(value);
        }

        return values;
    }

    internal DocxContentTypes CopyMappings()
    {
        DocxContentTypes copy = new DocxContentTypes();

        foreach (DocxContentTypeDefault value in this.Defaults)
        {
            copy.AddDefault(value.Extension, value.ContentType);
        }

        foreach (DocxContentTypeOverride value in this.Overrides)
        {
            copy.AddOverride(value.PartName, value.ContentType);
        }

        return copy;
    }

    internal static string GetExtensionFromPartName(string partName)
    {
        string normalizedPartName = DocxPartName.Normalize(partName);
        int slashIndex = normalizedPartName.LastIndexOf('/');
        int dotIndex = normalizedPartName.LastIndexOf('.');

        if (dotIndex < 0)
        {
            return "";
        }

        if (dotIndex < slashIndex)
        {
            return "";
        }

        if (dotIndex == normalizedPartName.Length - 1)
        {
            return "";
        }

        return normalizedPartName.Substring(dotIndex + 1);
    }

    internal int DefaultCount()
    {
        return this.Defaults.Count;
    }

    internal int OverrideCount()
    {
        return this.Overrides.Count;
    }

    internal string WriteXml()
    {
        return WriteDeterministicXml(false);
    }

    internal string WriteDeterministicXml(bool deterministic)
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;

        StringWriter stringWriter = new StringWriter();
        List<DocxContentTypeDefault> defaults = GetOrderedDefaults(deterministic);
        List<DocxContentTypeOverride> overrides = GetOrderedOverrides(deterministic);

        using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
        {
            writer.WriteStartElement("Types", DocxConstants.NamespaceContentTypes);

            foreach (DocxContentTypeDefault value in defaults)
            {
                writer.WriteStartElement("Default", DocxConstants.NamespaceContentTypes);
                writer.WriteAttributeString("Extension", value.Extension);
                writer.WriteAttributeString("ContentType", value.ContentType);
                writer.WriteEndElement();
            }

            foreach (DocxContentTypeOverride value in overrides)
            {
                writer.WriteStartElement("Override", DocxConstants.NamespaceContentTypes);
                writer.WriteAttributeString("PartName", value.PartName);
                writer.WriteAttributeString("ContentType", value.ContentType);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        return stringWriter.ToString();
    }

    internal List<DocxContentTypeDefault> GetOrderedDefaults(bool deterministic)
    {
        List<DocxContentTypeDefault> values = new List<DocxContentTypeDefault>();

        foreach (DocxContentTypeDefault value in this.Defaults)
        {
            values.Add(value);
        }

        if (!deterministic)
        {
            return values;
        }

        for (int index = 1; index < values.Count; index = index + 1)
        {
            DocxContentTypeDefault current = values[index];
            int previousIndex = index - 1;

            while (previousIndex >= 0 && CompareDefaults(values[previousIndex], current) > 0)
            {
                values[previousIndex + 1] = values[previousIndex];
                previousIndex = previousIndex - 1;
            }

            values[previousIndex + 1] = current;
        }

        return values;
    }

    internal List<DocxContentTypeOverride> GetOrderedOverrides(bool deterministic)
    {
        List<DocxContentTypeOverride> values = new List<DocxContentTypeOverride>();

        foreach (DocxContentTypeOverride value in this.Overrides)
        {
            values.Add(value);
        }

        if (!deterministic)
        {
            return values;
        }

        for (int index = 1; index < values.Count; index = index + 1)
        {
            DocxContentTypeOverride current = values[index];
            int previousIndex = index - 1;

            while (previousIndex >= 0 && CompareOverrides(values[previousIndex], current) > 0)
            {
                values[previousIndex + 1] = values[previousIndex];
                previousIndex = previousIndex - 1;
            }

            values[previousIndex + 1] = current;
        }

        return values;
    }

    internal static int CompareDefaults(DocxContentTypeDefault left, DocxContentTypeDefault right)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        int extensionComparison = string.CompareOrdinal(left.Extension, right.Extension);

        if (extensionComparison != 0)
        {
            return extensionComparison;
        }

        return string.CompareOrdinal(left.ContentType, right.ContentType);
    }

    internal static int CompareOverrides(DocxContentTypeOverride left, DocxContentTypeOverride right)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        int partNameComparison = string.CompareOrdinal(left.PartName, right.PartName);

        if (partNameComparison != 0)
        {
            return partNameComparison;
        }

        return string.CompareOrdinal(left.ContentType, right.ContentType);
    }

    internal static string NormalizeExtension(string extension)
    {
        if (extension == null)
        {
            throw new ArgumentNullException("extension");
        }

        string normalized = extension;

        if (normalized.StartsWith("."))
        {
            normalized = normalized.Substring(1);
        }

        if (normalized == "")
        {
            throw new ArgumentException("Extension is required.", "extension");
        }

        if (normalized.Contains("/") || normalized.Contains("\\"))
        {
            throw new ArgumentException("Extension cannot contain path separators.", "extension");
        }

        return normalized;
    }
}
