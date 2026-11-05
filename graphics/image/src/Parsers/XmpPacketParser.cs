using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace image;

internal class XmpPacketParser
{
    internal const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    internal const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    internal const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    internal static void ParseProfile(ImageMetadataDocument document, MetadataProfile profile, ImageMetadataReaderOptions? options, string pathPrefix)
    {
        if (document == null || profile == null || profile.Text == null || profile.Text.Length == 0)
        {
            return;
        }

        if (options != null && !options.ParseXmp)
        {
            return;
        }

        long maxXmlBytes = options == null ? 16777216 : options.MaxXmlBytes;
        int maxTagCount = options == null ? 65536 : options.MaxTagCount;
        int maxNestingDepth = options == null ? 64 : options.MaxNestingDepth;
        Parse(document, profile.Text, profile.Offset, profile.Length, pathPrefix, profile.Name, maxXmlBytes, maxTagCount, maxNestingDepth);
    }

    internal static void Parse(ImageMetadataDocument document, string xmlText, long offset, long length, string pathPrefix, string blockType, long maxXmlBytes, int maxTagCount, int maxNestingDepth)
    {
        if (document == null)
        {
            return;
        }

        if (xmlText == null || xmlText.IndexOf(RdfNamespace, StringComparison.Ordinal) < 0)
        {
            return;
        }

        long encodedLength = Encoding.UTF8.GetByteCount(xmlText);
        if (maxXmlBytes > 0 && encodedLength > maxXmlBytes)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "xmp.xml.byte.limit", "XMP packet exceeded the configured XML byte limit.", blockType, offset, encodedLength);
            return;
        }

        if (!MetadataXmlReader.ValidateXmlText(xmlText, maxXmlBytes, document.Diagnostics, blockType))
        {
            return;
        }

        if (MetadataXmlReader.ContainsDtd(xmlText))
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "xmp.dtd.rejected", "XMP packets containing a DTD are not parsed.", blockType);
            return;
        }

        if (!MetadataXmlReader.HasBalancedElements(xmlText) || !HasSafeEntityReferences(xmlText))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "xmp.xml.malformed", "XMP XML markup was malformed or contained an unsupported entity reference.", blockType, offset, length);
            return;
        }

        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.XmlResolver = null;
        xmlDocument.LoadXml(xmlText);

        XmpParserState state = new XmpParserState(document);
        state.Offset = offset;
        state.Length = length;
        state.PathPrefix = pathPrefix == null || pathPrefix.Length == 0 ? "XMP" : pathPrefix;
        state.BlockType = blockType == null || blockType.Length == 0 ? "XMP" : blockType;
        state.MaxTagCount = maxTagCount;
        state.MaxNestingDepth = maxNestingDepth;
        FindRdfDescriptions(state, xmlDocument);
    }

    internal static void FindRdfDescriptions(XmpParserState state, XmlNode node)
    {
        if (node == null)
        {
            return;
        }

        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;
            if (element.NamespaceURI == RdfNamespace && element.LocalName == "RDF")
            {
                foreach (XmlNode child in element.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                    {
                        ProcessDescription(state, (XmlElement)child);
                    }
                }

                return;
            }

            if (element.NamespaceURI == RdfNamespace && element.LocalName == "Description")
            {
                ProcessDescription(state, element);
                return;
            }
        }

        foreach (XmlNode child in node.ChildNodes)
        {
            FindRdfDescriptions(state, child);
        }
    }

    internal static void ProcessDescription(XmpParserState state, XmlElement description)
    {
        ProcessPropertyAttributes(state, null, description, "", 0);
        foreach (XmlNode child in description.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                ProcessProperty(state, null, (XmlElement)child, "", 0);
            }
        }
    }

    internal static void ProcessPropertyAttributes(XmpParserState state, MetadataDirectory? target, XmlElement element, string parentPath, int depth)
    {
        if (element.Attributes == null)
        {
            return;
        }

        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (IsMetadataName(attribute.NamespaceURI, attribute.LocalName))
            {
                string alias = NamespaceAlias(attribute.NamespaceURI, attribute.Prefix);
                MetadataDirectory directory = target == null ? FindOrAddNamespaceDirectory(state, alias, attribute.NamespaceURI) : target;
                string key = BuildKey(alias, parentPath, attribute.LocalName);
                AddTag(state, directory, key, attribute.LocalName, ScalarValue(attribute.LocalName, attribute.Value), attribute.Value);
            }
        }
    }

    internal static void ProcessProperty(XmpParserState state, MetadataDirectory? target, XmlElement property, string parentPath, int depth)
    {
        if (!IsMetadataName(property.NamespaceURI, property.LocalName))
        {
            return;
        }

        string alias = NamespaceAlias(property.NamespaceURI, property.Prefix);
        MetadataDirectory directory = target == null ? FindOrAddNamespaceDirectory(state, alias, property.NamespaceURI) : target;
        string key = BuildKey(alias, parentPath, property.LocalName);
        XmlElement? array = FindArray(property);
        if (array != null)
        {
            if (array.LocalName == "Alt")
            {
                ProcessLanguageAlternative(state, directory, property, array, key);
            }
            else if (ArrayHasStructuredItems(array))
            {
                ProcessStructuredArray(state, directory, property, array, key, parentPath, depth);
            }
            else
            {
                ProcessSimpleArray(state, directory, property, array, key);
            }

            return;
        }

        if (IsStructured(property))
        {
            ProcessStructuredProperty(state, directory, property, key, parentPath, depth);
            return;
        }

        string text = SimpleValue(property);
        AddTag(state, directory, key, property.LocalName, ScalarValue(property.LocalName, text), text);
    }

    internal static void ProcessSimpleArray(XmpParserState state, MetadataDirectory directory, XmlElement property, XmlElement array, string key)
    {
        List<string> values = new List<string>();
        foreach (XmlNode child in array.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement item = (XmlElement)child;
                if (item.NamespaceURI == RdfNamespace && item.LocalName == "li")
                {
                    values.Add(SimpleValue(item));
                }
            }
        }

        AddTag(state, directory, key, property.LocalName, MetadataValue.FromStringList(values.ToArray()), JoinValues(values));
    }

    internal static void ProcessLanguageAlternative(XmpParserState state, MetadataDirectory directory, XmlElement property, XmlElement array, string key)
    {
        List<string> languages = new List<string>();
        List<string> values = new List<string>();
        foreach (XmlNode child in array.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement item = (XmlElement)child;
                if (item.NamespaceURI == RdfNamespace && item.LocalName == "li")
                {
                    languages.Add(item.GetAttribute("lang", XmlNamespace));
                    values.Add(SimpleValue(item));
                }
            }
        }

        if (values.Count == 0)
        {
            AddTag(state, directory, key, property.LocalName, MetadataValue.FromString(""), "");
            return;
        }

        int selected = 0;
        int index = 0;
        while (index < languages.Count)
        {
            if (string.Equals(languages[index], "x-default", StringComparison.OrdinalIgnoreCase))
            {
                selected = index;
                break;
            }

            index = index + 1;
        }

        AddTag(state, directory, key, property.LocalName, MetadataValue.FromString(values[selected]), values[selected]);
        index = 0;
        while (index < values.Count)
        {
            string language = languages[index].Length == 0 ? "und" : SanitizeName(languages[index]);
            AddTag(state, directory, key + "." + language, property.LocalName + " [" + language + "]", MetadataValue.FromString(values[index]), values[index]);
            index = index + 1;
        }
    }

    internal static void ProcessStructuredProperty(XmpParserState state, MetadataDirectory directory, XmlElement property, string key, string parentPath, int depth)
    {
        if (!CanDescend(state, depth))
        {
            return;
        }

        string childPath = AppendPath(parentPath, property.LocalName);
        MetadataDirectory structure = AddStructureDirectory(state, directory, property.LocalName, childPath, 0);
        AddTag(state, directory, key, property.LocalName, MetadataValue.FromDirectory(structure), structure.Name);
        XmlElement container = StructuredContainer(property);
        ProcessPropertyAttributes(state, structure, container, childPath, depth + 1);
        foreach (XmlNode child in container.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                ProcessProperty(state, structure, (XmlElement)child, childPath, depth + 1);
            }
        }
    }

    internal static void ProcessStructuredArray(XmpParserState state, MetadataDirectory directory, XmlElement property, XmlElement array, string key, string parentPath, int depth)
    {
        if (!CanDescend(state, depth))
        {
            return;
        }

        string childPath = AppendPath(parentPath, property.LocalName);
        MetadataDirectory structure = AddStructureDirectory(state, directory, property.LocalName, childPath, 0);
        AddTag(state, directory, key, property.LocalName, MetadataValue.FromDirectory(structure), structure.Name);
        int itemIndex = 0;
        foreach (XmlNode child in array.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement item = (XmlElement)child;
                if (item.NamespaceURI == RdfNamespace && item.LocalName == "li")
                {
                    MetadataDirectory itemDirectory = AddStructureDirectory(state, structure, "Item " + (itemIndex + 1).ToString(), childPath + ".Item", itemIndex);
                    XmlElement container = StructuredContainer(item);
                    ProcessPropertyAttributes(state, itemDirectory, container, childPath, depth + 1);
                    foreach (XmlNode fieldNode in container.ChildNodes)
                    {
                        if (fieldNode.NodeType == XmlNodeType.Element)
                        {
                            ProcessProperty(state, itemDirectory, (XmlElement)fieldNode, childPath, depth + 1);
                        }
                    }

                    itemIndex = itemIndex + 1;
                }
            }
        }
    }

    internal static MetadataDirectory AddStructureDirectory(XmpParserState state, MetadataDirectory parent, string name, string path, int repeatIndex)
    {
        MetadataDirectory directory = parent.AddChildWithName(MetadataFamily.Xmp, path, name);
        directory.Path = parent.Path + "/" + name;
        directory.Offset = state.Offset;
        directory.Length = state.Length;
        directory.RepeatIndex = repeatIndex;
        return directory;
    }

    internal static MetadataDirectory FindOrAddNamespaceDirectory(XmpParserState state, string alias, string namespaceUri)
    {
        foreach (MetadataDirectory existing in state.NamespaceDirectories)
        {
            if (existing.Group == alias)
            {
                return existing;
            }
        }

        MetadataDirectory directory = MetadataDirectory.Create(MetadataFamily.Xmp, alias, NamespaceDisplayName(alias, namespaceUri));
        directory.Path = state.PathPrefix + "/" + alias;
        directory.Offset = state.Offset;
        directory.Length = state.Length;
        state.Document.AddDirectory(directory);
        state.NamespaceDirectories.Add(directory);
        return directory;
    }

    internal static void AddTag(XmpParserState state, MetadataDirectory directory, string key, string name, MetadataValue value, string displayValue)
    {
        if (state.MaxTagCount <= 0 || state.TagCount >= state.MaxTagCount)
        {
            if (!state.TagLimitReported)
            {
                MetadataDiagnostic.AddLimitExceeded(state.Document.Diagnostics, "xmp.tag.limit", "XMP tag count exceeded the configured limit.", state.BlockType, state.Offset, state.Length);
                state.TagLimitReported = true;
            }

            return;
        }

        MetadataTag tag = MetadataTag.Create(key, name, MetadataFamily.Xmp, value);
        tag.Group = directory.Group;
        tag.Offset = state.Offset;
        tag.Length = state.Length;
        tag.Confidence = 100;
        tag.DisplayValue = displayValue;
        directory.AddTag(tag);
        state.TagCount = state.TagCount + 1;
    }

    internal static bool CanDescend(XmpParserState state, int depth)
    {
        if (state.MaxNestingDepth > 0 && depth < state.MaxNestingDepth)
        {
            return true;
        }

        if (state.MaxNestingDepth <= 0 || depth >= state.MaxNestingDepth)
        {
            if (!state.DepthLimitReported)
            {
                MetadataDiagnostic.AddLimitExceeded(state.Document.Diagnostics, "xmp.nesting.limit", "XMP structured-value nesting exceeded the configured limit.", state.BlockType, state.Offset, state.Length);
                state.DepthLimitReported = true;
            }

            return false;
        }

        return true;
    }

    internal static XmlElement? FindArray(XmlElement property)
    {
        foreach (XmlNode child in property.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)child;
                if (element.NamespaceURI == RdfNamespace && (element.LocalName == "Bag" || element.LocalName == "Seq" || element.LocalName == "Alt"))
                {
                    return element;
                }
            }
        }

        return null;
    }

    internal static bool ArrayHasStructuredItems(XmlElement array)
    {
        foreach (XmlNode child in array.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement item = (XmlElement)child;
                if (item.HasAttribute("parseType", RdfNamespace) && item.GetAttribute("parseType", RdfNamespace) == "Resource")
                {
                    return true;
                }

                foreach (XmlNode itemChild in item.ChildNodes)
                {
                    if (itemChild.NodeType == XmlNodeType.Element)
                    {
                        return true;
                    }
                }

                if (HasMetadataAttributes(item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool IsStructured(XmlElement property)
    {
        if (property.HasAttribute("parseType", RdfNamespace) && property.GetAttribute("parseType", RdfNamespace) == "Resource")
        {
            return true;
        }

        foreach (XmlNode child in property.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                return true;
            }
        }

        return false;
    }

    internal static XmlElement StructuredContainer(XmlElement element)
    {
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement childElement = (XmlElement)child;
                if (childElement.NamespaceURI == RdfNamespace && childElement.LocalName == "Description")
                {
                    return childElement;
                }
            }
        }

        return element;
    }

    internal static bool HasMetadataAttributes(XmlElement element)
    {
        if (element.Attributes == null)
        {
            return false;
        }

        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (IsMetadataName(attribute.NamespaceURI, attribute.LocalName))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsMetadataName(string namespaceUri, string localName)
    {
        if (namespaceUri == null || namespaceUri.Length == 0 || namespaceUri == RdfNamespace || namespaceUri == XmlNamespace || namespaceUri == XmlnsNamespace)
        {
            return false;
        }

        return localName != null && localName.Length > 0;
    }

    internal static string SimpleValue(XmlElement element)
    {
        if (element.HasAttribute("resource", RdfNamespace))
        {
            return element.GetAttribute("resource", RdfNamespace);
        }

        return element.InnerText.Trim();
    }

    internal static MetadataValue ScalarValue(string localName, string text)
    {
        if (IsDateProperty(localName))
        {
            return MetadataValue.FromDateText(text);
        }

        if (IsIntegerProperty(localName))
        {
            XmpIntegerResult integer = ParseInteger(text);
            if (integer.Valid)
            {
                return MetadataValue.FromInteger(integer.Value);
            }
        }

        return MetadataValue.FromString(text);
    }

    internal static bool IsDateProperty(string localName)
    {
        return localName == "CreateDate" || localName == "ModifyDate" || localName == "MetadataDate" || localName == "DateCreated" || localName == "DateTimeOriginal" || localName == "DateTimeDigitized" || localName == "GPSDateTime";
    }

    internal static bool IsIntegerProperty(string localName)
    {
        return localName == "Rating" || localName == "Orientation" || localName == "Urgency" || localName == "Marked";
    }

    internal static XmpIntegerResult ParseInteger(string text)
    {
        XmpIntegerResult result = new XmpIntegerResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        int index = 0;
        bool negative = false;
        if (text[0] == '-' || text[0] == '+')
        {
            negative = text[0] == '-';
            index = 1;
        }

        if (index == text.Length)
        {
            return result;
        }

        long value = 0;
        while (index < text.Length)
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9 || value > (long.MaxValue - digit) / 10)
            {
                return result;
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        result.Valid = true;
        result.Value = negative ? -value : value;
        return result;
    }

    internal static string BuildKey(string alias, string parentPath, string localName)
    {
        string key = "Xmp." + alias + ".";
        if (parentPath != null && parentPath.Length > 0)
        {
            key = key + parentPath + ".";
        }

        return key + localName;
    }

    internal static string AppendPath(string parentPath, string localName)
    {
        if (parentPath == null || parentPath.Length == 0)
        {
            return localName;
        }

        return parentPath + "." + localName;
    }

    internal static string NamespaceAlias(string namespaceUri, string prefix)
    {
        if (namespaceUri == "http://purl.org/dc/elements/1.1/") return "dc";
        if (namespaceUri == "http://ns.adobe.com/xap/1.0/") return "xmp";
        if (namespaceUri == "http://ns.adobe.com/photoshop/1.0/") return "photoshop";
        if (namespaceUri == "http://ns.adobe.com/tiff/1.0/") return "tiff";
        if (namespaceUri == "http://ns.adobe.com/exif/1.0/") return "exif";
        if (namespaceUri == "http://cipa.jp/exif/1.0/") return "exifEX";
        if (namespaceUri == "http://ns.adobe.com/exif/1.0/aux/") return "aux";
        if (namespaceUri == "http://ns.adobe.com/xap/1.0/rights/") return "xmpRights";
        if (namespaceUri == "http://ns.adobe.com/xap/1.0/mm/") return "xmpMM";
        if (namespaceUri == "http://ns.adobe.com/xap/1.0/bj/") return "xmpBJ";
        if (namespaceUri == "http://ns.adobe.com/xap/1.0/t/pg/") return "xmpTPg";
        if (namespaceUri == "http://ns.adobe.com/xmp/1.0/DynamicMedia/") return "xmpDM";
        if (namespaceUri == "http://ns.adobe.com/pdf/1.3/") return "pdf";
        if (namespaceUri == "http://iptc.org/std/Iptc4xmpCore/1.0/xmlns/") return "iptcCore";
        if (namespaceUri == "http://iptc.org/std/Iptc4xmpExt/2008-02-29/") return "iptcExt";
        if (namespaceUri == "http://ns.useplus.org/ldf/xmp/1.0/") return "plus";
        if (namespaceUri == "http://ns.adobe.com/camera-raw-settings/1.0/") return "crs";
        if (namespaceUri == "http://ns.microsoft.com/photo/1.0/") return "MicrosoftPhoto";
        if (namespaceUri == "http://ns.adobe.com/xap/1.0/sType/Dimensions#") return "stDim";
        if (namespaceUri == "http://ns.adobe.com/xap/1.0/sType/ResourceRef#") return "stRef";
        if (namespaceUri == "http://ns.adobe.com/xap/1.0/sType/ResourceEvent#") return "stEvt";
        if (namespaceUri == "http://ns.adobe.com/xap/1.0/sType/Version#") return "stVer";
        if (namespaceUri == "http://ns.adobe.com/xap/1.0/sType/Job#") return "stJob";
        if (prefix != null && prefix.Length > 0) return SanitizeName(prefix);
        return "ns";
    }

    internal static string NamespaceDisplayName(string alias, string namespaceUri)
    {
        if (alias == "dc") return "Dublin Core";
        if (alias == "xmp") return "XMP Basic";
        if (alias == "photoshop") return "Adobe Photoshop";
        if (alias == "tiff") return "TIFF";
        if (alias == "exif") return "EXIF";
        if (alias == "exifEX") return "EXIF Extensions";
        if (alias == "aux") return "EXIF Auxiliary";
        if (alias == "xmpRights") return "XMP Rights";
        if (alias == "xmpMM") return "XMP Media Management";
        if (alias == "xmpBJ") return "XMP Basic Job Ticket";
        if (alias == "xmpTPg") return "XMP Paged Text";
        if (alias == "xmpDM") return "XMP Dynamic Media";
        if (alias == "pdf") return "Adobe PDF";
        if (alias == "iptcCore") return "IPTC Core";
        if (alias == "iptcExt") return "IPTC Extension";
        if (alias == "plus") return "PLUS";
        if (alias == "crs") return "Camera Raw Settings";
        if (alias == "MicrosoftPhoto") return "Microsoft Photo";
        if (alias == "stDim") return "XMP Dimensions Structure";
        if (alias == "stRef") return "XMP Resource Reference Structure";
        if (alias == "stEvt") return "XMP Resource Event Structure";
        if (alias == "stVer") return "XMP Version Structure";
        if (alias == "stJob") return "XMP Job Structure";
        return namespaceUri == null || namespaceUri.Length == 0 ? alias : namespaceUri;
    }

    internal static string SanitizeName(string value)
    {
        if (value == null || value.Length == 0)
        {
            return "ns";
        }

        char[] characters = value.ToCharArray();
        int index = 0;
        while (index < characters.Length)
        {
            char character = characters[index];
            if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
            {
                characters[index] = '_';
            }

            index = index + 1;
        }

        return new string(characters);
    }

    internal static string JoinValues(List<string> values)
    {
        string text = "";
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0)
            {
                text = text + ", ";
            }

            text = text + values[index];
            index = index + 1;
        }

        return text;
    }

    internal static bool HasSafeEntityReferences(string xmlText)
    {
        int index = 0;
        while (index < xmlText.Length)
        {
            int start = xmlText.IndexOf('&', index);
            if (start < 0)
            {
                return true;
            }

            int end = xmlText.IndexOf(';', start + 1);
            if (end < 0)
            {
                return false;
            }

            string name = xmlText.Substring(start + 1, end - start - 1);
            if (name != "amp" && name != "lt" && name != "gt" && name != "apos" && name != "quot" && !IsNumericEntity(name))
            {
                return false;
            }

            index = end + 1;
        }

        return true;
    }

    internal static bool IsNumericEntity(string name)
    {
        if (name == null || name.Length < 2 || name[0] != '#')
        {
            return false;
        }

        int index = 1;
        bool hexadecimal = false;
        if (index < name.Length && (name[index] == 'x' || name[index] == 'X'))
        {
            hexadecimal = true;
            index = index + 1;
        }

        if (index == name.Length)
        {
            return false;
        }

        while (index < name.Length)
        {
            char character = name[index];
            bool decimalDigit = character >= '0' && character <= '9';
            bool hexDigit = decimalDigit || character >= 'a' && character <= 'f' || character >= 'A' && character <= 'F';
            if (hexadecimal ? !hexDigit : !decimalDigit)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
