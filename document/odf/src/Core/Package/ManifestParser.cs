using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace odf;

internal static class ManifestParser
{
    const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    internal static OdfManifest ParseBytes(byte[] data)
    {
        return ParseText(Encoding.UTF8.GetString(data));
    }

    internal static OdfManifest ParseText(string xml)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.IgnoreComments = false;
        settings.IgnoreWhitespace = false;
        XmlDocument document = new XmlDocument();
        document.XmlResolver = null;
        document.PreserveWhitespace = true;
        using (StringReader textReader = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(textReader, settings))
        {
            document.Load(reader);
        }
        return ParseDocument(document, xml);
    }

    internal static OdfManifest? ParsePackageManifest(
        byte[] packageData,
        OdfPackageEntry[] entries)
    {
        foreach (OdfPackageEntry entry in entries)
        {
            if (entry.Path == OdfPartNames.Manifest)
            {
                PackageEntryContent content = MimetypeRules.ReadEntryContent(
                    packageData,
                    entry);
                if (content.IsAvailable)
                {
                    return ParseBytes(content.Data);
                }
                return null;
            }
        }
        return null;
    }

    static OdfManifest ParseDocument(XmlDocument document, string originalXml)
    {
        OdfManifest manifest = new OdfManifest();
        manifest.OriginalXml = originalXml;
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MalformedXml,
                "The package manifest has no document element.");
            return manifest;
        }
        manifest.Root = BuildNode(root);
        if (root.NamespaceURI != OdfNamespaces.Manifest || root.LocalName != "manifest")
        {
            AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.UnexpectedElement,
                "The package manifest root element is not manifest:manifest.");
            return manifest;
        }

        bool valid = true;
        manifest.Version = GetManifestAttribute(root, "version");
        if (!root.HasAttribute("version", OdfNamespaces.Manifest))
        {
            AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "The package manifest has no manifest:version attribute.");
            valid = false;
        }
        else if (manifest.Version != "1.2"
            && manifest.Version != "1.3"
            && manifest.Version != "1.4")
        {
            AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.VersionUnsupported,
                $"Manifest version '{manifest.Version}' is not an ODF 1.2, 1.3, or 1.4 manifest.");
            valid = false;
        }
        manifest.ForeignAttributes = CollectForeignAttributes(root, ["version"]);

        List<OdfManifestEntry> entries = new List<OdfManifestEntry>();
        List<OdfManifestNode> encryptedKeys = new List<OdfManifestNode>();
        List<OdfEncryptedKey> encryptedKeyModels = new List<OdfEncryptedKey>();
        List<OdfManifestNode> foreignElements = new List<OdfManifestNode>();
        foreach (XmlNode child in root.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == OdfNamespaces.Manifest
                && element.LocalName == "file-entry")
            {
                OdfManifestEntry entry = ParseFileEntry(element, manifest);
                entries.Add(entry);
                if (!HasRequiredEntryValues(element)
                    || entry.HasSize && !entry.SizeIsValid)
                {
                    valid = false;
                }
            }
            else if (element.NamespaceURI == OdfNamespaces.Manifest
                && element.LocalName == "encrypted-key")
            {
                OdfManifestNode node = BuildNode(element);
                OdfEncryptedKey encryptedKey = ManifestEncryptionParser.ParseEncryptedKey(
                    node,
                    manifest);
                encryptedKeys.Add(node);
                encryptedKeyModels.Add(encryptedKey);
                if (manifest.Version == "1.2")
                {
                    AddManifestDiagnostic(
                        manifest,
                        OdfDiagnosticCodes.UnexpectedElement,
                        "OpenPGP encrypted-key metadata requires ODF 1.3 or later.");
                    valid = false;
                }
                if (!encryptedKey.IsValid)
                {
                    valid = false;
                }
            }
            else
            {
                foreignElements.Add(BuildNode(element));
            }
        }
        if (entries.Count == 0)
        {
            AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "The package manifest contains no file entries.");
            valid = false;
        }
        manifest.Entries = entries.ToArray();
        manifest.EncryptedKeys = encryptedKeys.ToArray();
        manifest.EncryptedKeyModels = encryptedKeyModels.ToArray();
        manifest.ForeignElements = foreignElements.ToArray();
        manifest.IsValid = valid;
        return manifest;
    }

    static OdfManifestEntry ParseFileEntry(
        XmlElement element,
        OdfManifest manifest)
    {
        OdfManifestEntry entry = new OdfManifestEntry();
        entry.FullPath = GetManifestAttribute(element, "full-path");
        entry.MediaType = GetManifestAttribute(element, "media-type");
        entry.Version = GetManifestAttribute(element, "version");
        entry.PreferredViewMode = GetManifestAttribute(element, "preferred-view-mode");
        entry.Node = BuildNode(element);
        entry.ForeignAttributes = CollectForeignAttributes(
            element,
            ["full-path", "size", "media-type", "preferred-view-mode", "version"]);

        if (!element.HasAttribute("full-path", OdfNamespaces.Manifest))
        {
            AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "A manifest file entry has no manifest:full-path attribute.");
        }
        if (!element.HasAttribute("media-type", OdfNamespaces.Manifest))
        {
            AddManifestDiagnostic(
                manifest,
                OdfDiagnosticCodes.MissingRequiredContent,
                "A manifest file entry has no manifest:media-type attribute.");
        }
        if (element.HasAttribute("size", OdfNamespaces.Manifest))
        {
            entry.HasSize = true;
            entry.SizeLexical = GetManifestAttribute(element, "size");
            ManifestIntegerValue size = ParseNonNegativeInteger(entry.SizeLexical);
            entry.Size = size.Value;
            entry.SizeIsValid = size.IsValid;
            if (!size.IsValid)
            {
                AddManifestDiagnostic(
                    manifest,
                    OdfDiagnosticCodes.InvalidDatatype,
                    $"Manifest entry size '{entry.SizeLexical}' is not a supported non-negative integer.");
            }
        }

        List<OdfManifestNode> foreignElements = new List<OdfManifestNode>();
        bool foundEncryptionData = false;
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement childElement = (XmlElement)child;
            if (!foundEncryptionData
                && childElement.NamespaceURI == OdfNamespaces.Manifest
                && childElement.LocalName == "encryption-data")
            {
                entry.EncryptionData = BuildNode(childElement);
                entry.Encryption = ManifestEncryptionParser.ParseEncryptionData(
                    entry.EncryptionData,
                    entry,
                    manifest);
                foundEncryptionData = true;
            }
            else
            {
                foreignElements.Add(BuildNode(childElement));
            }
        }
        entry.ForeignElements = foreignElements.ToArray();
        return entry;
    }

    static bool HasRequiredEntryValues(XmlElement element)
    {
        return element.HasAttribute("full-path", OdfNamespaces.Manifest)
            && element.HasAttribute("media-type", OdfNamespaces.Manifest);
    }

    static OdfManifestNode BuildNode(XmlElement element)
    {
        OdfManifestNode result = new OdfManifestNode();
        result.Name = new OdfQualifiedName
        {
            NamespaceName = element.NamespaceURI,
            LocalName = element.LocalName,
            Prefix = element.Prefix
        };
        result.IsManifestElement = element.NamespaceURI == OdfNamespaces.Manifest;
        result.TextContent = element.InnerText;
        result.OriginalXml = element.OuterXml;

        List<OdfNamespaceDeclaration> namespaces = new List<OdfNamespaceDeclaration>();
        List<OdfAttribute> attributes = new List<OdfAttribute>();
        foreach (XmlAttribute attribute in element.Attributes)
        {
            attributes.Add(BuildAttribute(attribute));
            if (attribute.NamespaceURI == XmlnsNamespace)
            {
                OdfNamespaceDeclaration declaration = new OdfNamespaceDeclaration();
                declaration.NamespaceName = attribute.Value;
                declaration.Prefix = attribute.Prefix == "xmlns"
                    ? attribute.LocalName
                    : "";
                namespaces.Add(declaration);
            }
        }
        result.Attributes = attributes.ToArray();
        result.NamespaceDeclarations = namespaces.ToArray();

        List<OdfManifestNode> children = new List<OdfManifestNode>();
        List<string> orderedContent = new List<string>();
        List<int> orderedElementIndexes = new List<int>();
        foreach (XmlNode child in element.ChildNodes)
        {
            orderedContent.Add(child.OuterXml);
            if (child.NodeType == XmlNodeType.Element)
            {
                orderedElementIndexes.Add(children.Count);
                children.Add(BuildNode((XmlElement)child));
            }
            else
            {
                orderedElementIndexes.Add(-1);
            }
        }
        result.Children = children.ToArray();
        result.OrderedContentXml = orderedContent.ToArray();
        result.OrderedContentElementIndexes = orderedElementIndexes.ToArray();
        return result;
    }

    static OdfAttribute BuildAttribute(XmlAttribute attribute)
    {
        OdfAttribute result = new OdfAttribute();
        result.Name = new OdfQualifiedName
        {
            NamespaceName = attribute.NamespaceURI,
            LocalName = attribute.LocalName,
            Prefix = attribute.Prefix
        };
        result.Value = attribute.Value;
        return result;
    }

    static OdfAttribute[] CollectForeignAttributes(
        XmlElement element,
        string[] knownLocalNames)
    {
        List<OdfAttribute> result = new List<OdfAttribute>();
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (attribute.NamespaceURI == XmlnsNamespace)
            {
                continue;
            }
            bool known = false;
            if (attribute.NamespaceURI == OdfNamespaces.Manifest)
            {
                int index = 0;
                while (index < knownLocalNames.Length)
                {
                    if (attribute.LocalName == knownLocalNames[index])
                    {
                        known = true;
                        break;
                    }
                    index = index + 1;
                }
            }
            if (!known)
            {
                result.Add(BuildAttribute(attribute));
            }
        }
        return result.ToArray();
    }

    static string GetManifestAttribute(XmlElement element, string localName)
    {
        return element.GetAttribute(localName, OdfNamespaces.Manifest);
    }

    static ManifestIntegerValue ParseNonNegativeInteger(string value)
    {
        ManifestIntegerValue result = new ManifestIntegerValue();
        if (value.Length == 0)
        {
            return result;
        }
        long parsed = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return result;
            }
            int digit = character - '0';
            if (parsed > (long.MaxValue - digit) / 10)
            {
                return result;
            }
            parsed = parsed * 10 + digit;
            index = index + 1;
        }
        result.Value = parsed;
        result.IsValid = true;
        return result;
    }

    internal static void AddManifestDiagnostic(
        OdfManifest manifest,
        string code,
        string message)
    {
        OdfLocation location = new OdfLocation();
        location.PartPath = OdfPartNames.Manifest;
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            code,
            message,
            OdfValidationSeverity.Error,
            location,
            null,
            OdfVersion.Unknown);
        OdfDiagnostic[] updated = new OdfDiagnostic[manifest.Diagnostics.Length + 1];
        Array.Copy(manifest.Diagnostics, updated, manifest.Diagnostics.Length);
        updated[updated.Length - 1] = diagnostic;
        manifest.Diagnostics = updated;
    }
}
