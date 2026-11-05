using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxContentTypesParser
{
    internal static PptxContentTypeManifest ParseBytes(byte[] bytes)
    {
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();

        if (bytes == null)
        {
            manifest.Diagnostics.AddError("CT001", "Content types XML bytes are required.", PptxPartNames.ContentTypesEntry, "", "");
            return manifest;
        }

        return ParseString(Encoding.UTF8.GetString(bytes));
    }

    internal static PptxContentTypeManifest ParseString(string xml)
    {
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();

        if (string.IsNullOrWhiteSpace(xml))
        {
            manifest.Diagnostics.AddError("CT001", "Content types XML is required.", PptxPartNames.ContentTypesEntry, "", "");
            return manifest;
        }

        XmlDocument document = LoadXml(xml);
        XmlElement? root = document.DocumentElement;

        if (root == null)
        {
            manifest.Diagnostics.AddError("CT002", "Content types XML must have a root element.", PptxPartNames.ContentTypesEntry, "", "");
            return manifest;
        }

        if (root.LocalName != "Types" || root.NamespaceURI != PptxNamespaces.ContentTypes)
        {
            manifest.Diagnostics.AddError("CT003", "Content types XML root element must be ct:Types.", PptxPartNames.ContentTypesEntry, "", "");
            return manifest;
        }

        ParseChildren(root, manifest);
        return manifest;
    }

    internal static XmlDocument LoadXml(string xml)
    {
        return PptxXml.LoadString(xml);
    }

    internal static void ParseChildren(XmlElement root, PptxContentTypeManifest manifest)
    {
        Dictionary<string, string> defaultExtensions = new Dictionary<string, string>();
        Dictionary<string, string> overridePartNames = new Dictionary<string, string>();

        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];

            if (child == null)
            {
                continue;
            }

            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI != PptxNamespaces.ContentTypes)
            {
                manifest.Diagnostics.AddWarning("CT004", "Unknown content types element namespace was ignored.", PptxPartNames.ContentTypesEntry, "", "");
                continue;
            }

            if (element.LocalName == "Default")
            {
                ParseDefault(element, manifest, defaultExtensions);
                continue;
            }

            if (element.LocalName == "Override")
            {
                ParseOverride(element, manifest, overridePartNames);
                continue;
            }

            manifest.Diagnostics.AddWarning("CT005", "Unknown content types element was ignored.", PptxPartNames.ContentTypesEntry, "", "");
        }
    }

    internal static void ParseDefault(XmlElement element, PptxContentTypeManifest manifest, Dictionary<string, string> seenExtensions)
    {
        string extension = NormalizeExtension(GetAttribute(element, "Extension"));
        string contentType = GetAttribute(element, "ContentType");

        if (!ValidateExtension(extension, manifest))
        {
            return;
        }

        if (!ValidateContentType(contentType, manifest))
        {
            return;
        }

        if (seenExtensions.ContainsKey(extension))
        {
            manifest.Diagnostics.AddError("CT020", "Duplicate content type default extension.", PptxPartNames.ContentTypesEntry, "", "");
            return;
        }

        seenExtensions.Add(extension, contentType);
        manifest.AddDefault(extension, contentType);
    }

    internal static void ParseOverride(XmlElement element, PptxContentTypeManifest manifest, Dictionary<string, string> seenPartNames)
    {
        string partName = GetAttribute(element, "PartName");
        string contentType = GetAttribute(element, "ContentType");

        PptxResult partNameResult = PptxUriCore.ValidatePartName(partName);
        if (partNameResult.Failed())
        {
            PptxUriCore.CopyDiagnostics(partNameResult, manifest.Diagnostics);
            return;
        }

        if (!ValidateContentType(contentType, manifest))
        {
            return;
        }

        string normalizedPartName = PptxUriCore.NormalizePartName(partName);
        if (seenPartNames.ContainsKey(normalizedPartName))
        {
            manifest.Diagnostics.AddError("CT021", "Duplicate content type override part name.", normalizedPartName, "", "");
            return;
        }

        seenPartNames.Add(normalizedPartName, contentType);
        manifest.AddOverride(normalizedPartName, contentType);
    }

    internal static bool ValidateExtension(string extension, PptxContentTypeManifest manifest)
    {
        if (extension.Length == 0)
        {
            manifest.Diagnostics.AddError("CT010", "Content type default extension is required.", PptxPartNames.ContentTypesEntry, "", "");
            return false;
        }

        if (extension[0] == '.')
        {
            manifest.Diagnostics.AddError("CT011", "Content type default extension cannot start with '.'.", PptxPartNames.ContentTypesEntry, "", "");
            return false;
        }

        if (HasInvalidExtensionCharacters(extension))
        {
            manifest.Diagnostics.AddError("CT012", "Content type default extension contains invalid characters.", PptxPartNames.ContentTypesEntry, "", "");
            return false;
        }

        return true;
    }

    internal static bool ValidateContentType(string contentType, PptxContentTypeManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            manifest.Diagnostics.AddError("CT030", "Content type value is required.", PptxPartNames.ContentTypesEntry, "", "");
            return false;
        }

        if (contentType.IndexOf('/') <= 0 || contentType[contentType.Length - 1] == '/')
        {
            manifest.Diagnostics.AddError("CT031", "Content type value must use type/subtype form.", PptxPartNames.ContentTypesEntry, "", "");
            return false;
        }

        for (int index = 0; index < contentType.Length; index++)
        {
            char value = contentType[index];
            if (value <= ' ')
            {
                manifest.Diagnostics.AddError("CT032", "Content type value contains invalid characters.", PptxPartNames.ContentTypesEntry, "", "");
                return false;
            }
        }

        return true;
    }

    internal static string GetAttribute(XmlElement element, string name)
    {
        XmlAttribute? attribute = element.GetAttributeNode(name);

        if (attribute == null)
        {
            return "";
        }

        return attribute.Value;
    }

    internal static string NormalizeExtension(string extension)
    {
        if (extension == null)
        {
            return "";
        }

        string result = "";
        for (int index = 0; index < extension.Length; index++)
        {
            result = result + ToLowerAscii(extension[index]);
        }

        return result;
    }

    internal static string GetPartNameExtension(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        int slashIndex = normalized.LastIndexOf('/');
        int dotIndex = normalized.LastIndexOf('.');

        if (dotIndex <= slashIndex)
        {
            return "";
        }

        if (dotIndex == normalized.Length - 1)
        {
            return "";
        }

        return NormalizeExtension(normalized.Substring(dotIndex + 1));
    }

    internal static bool HasInvalidExtensionCharacters(string extension)
    {
        for (int index = 0; index < extension.Length; index++)
        {
            char value = extension[index];
            if (value <= ' ')
            {
                return true;
            }

            if (value == '/' || value == '\\' || value == ':' || value == '?' || value == '#')
            {
                return true;
            }
        }

        return false;
    }

    internal static char ToLowerAscii(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return (char)(value + 32);
        }

        return value;
    }
}
