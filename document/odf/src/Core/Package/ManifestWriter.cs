using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace odf;

internal static class ManifestWriter
{
    const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    internal static string WriteText(OdfManifest manifest, OdfSaveOptions options)
    {
        byte[] data = WriteBytes(manifest, options);
        return Encoding.UTF8.GetString(data);
    }

    internal static byte[] WriteBytes(OdfManifest manifest, OdfSaveOptions options)
    {
        if (CanUseOriginalXml(manifest, options))
        {
            return new UTF8Encoding(false).GetBytes(manifest.OriginalXml);
        }

        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Encoding = new UTF8Encoding(false);
        settings.Indent = options.IndentXml;
        settings.NewLineChars = "\n";
        settings.NewLineHandling = NewLineHandling.None;
        settings.OmitXmlDeclaration = false;
        settings.CloseOutput = false;

        using (MemoryStream output = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(output, settings))
            {
                writer.WriteStartDocument();
                WriteManifest(writer, manifest, options);
                writer.WriteEndDocument();
                writer.Flush();
            }
            return output.ToArray();
        }
    }

    static bool CanUseOriginalXml(OdfManifest manifest, OdfSaveOptions options)
    {
        if (manifest.OriginalXml.Length == 0
            || !options.PreserveLexicalValues
            || !PreservesUnknownContent(options)
            || !CanUseOriginalForProfile(options)
            || options.Deterministic
            || options.IndentXml
            || options.SortManifestEntries)
        {
            return false;
        }
        OdfManifest original = ManifestParser.ParseText(manifest.OriginalXml);
        return ManifestModelsMatch(manifest, original);
    }

    static bool ManifestModelsMatch(OdfManifest first, OdfManifest second)
    {
        return first.Version == second.Version
            && EntryArraysMatch(first.Entries, second.Entries)
            && NodeArraysMatch(first.EncryptedKeys, second.EncryptedKeys)
            && AttributeArraysMatch(first.ForeignAttributes, second.ForeignAttributes)
            && NodeArraysMatch(first.ForeignElements, second.ForeignElements)
            && NodesMatch(first.Root, second.Root);
    }

    static bool EntryArraysMatch(
        OdfManifestEntry[] first,
        OdfManifestEntry[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (!EntriesMatch(first[index], second[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool EntriesMatch(OdfManifestEntry first, OdfManifestEntry second)
    {
        bool encryptionMatches = first.EncryptionData == null
            && second.EncryptionData == null;
        if (first.EncryptionData != null && second.EncryptionData != null)
        {
            encryptionMatches = NodesMatch(first.EncryptionData, second.EncryptionData);
        }
        return first.FullPath == second.FullPath
            && first.MediaType == second.MediaType
            && first.Version == second.Version
            && first.PreferredViewMode == second.PreferredViewMode
            && first.Size == second.Size
            && first.SizeLexical == second.SizeLexical
            && first.HasSize == second.HasSize
            && first.SizeIsValid == second.SizeIsValid
            && encryptionMatches
            && AttributeArraysMatch(first.ForeignAttributes, second.ForeignAttributes)
            && NodeArraysMatch(first.ForeignElements, second.ForeignElements)
            && NodesMatch(first.Node, second.Node);
    }

    static bool NodeArraysMatch(
        OdfManifestNode[] first,
        OdfManifestNode[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (!NodesMatch(first[index], second[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool NodesMatch(OdfManifestNode first, OdfManifestNode second)
    {
        return first.Name.NamespaceName == second.Name.NamespaceName
            && first.Name.LocalName == second.Name.LocalName
            && first.Name.Prefix == second.Name.Prefix
            && NamespaceArraysMatch(
                first.NamespaceDeclarations,
                second.NamespaceDeclarations)
            && AttributeArraysMatch(first.Attributes, second.Attributes)
            && NodeArraysMatch(first.Children, second.Children)
            && StringArraysMatch(first.OrderedContentXml, second.OrderedContentXml)
            && IntegerArraysMatch(
                first.OrderedContentElementIndexes,
                second.OrderedContentElementIndexes)
            && first.TextContent == second.TextContent
            && first.OriginalXml == second.OriginalXml
            && first.IsManifestElement == second.IsManifestElement;
    }

    static bool NamespaceArraysMatch(
        OdfNamespaceDeclaration[] first,
        OdfNamespaceDeclaration[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index].Prefix != second[index].Prefix
                || first[index].NamespaceName != second[index].NamespaceName)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool AttributeArraysMatch(OdfAttribute[] first, OdfAttribute[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index].Name.NamespaceName != second[index].Name.NamespaceName
                || first[index].Name.LocalName != second[index].Name.LocalName
                || first[index].Name.Prefix != second[index].Name.Prefix
                || first[index].Value != second[index].Value)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool StringArraysMatch(string[] first, string[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IntegerArraysMatch(int[] first, int[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static void WriteManifest(
        XmlWriter writer,
        OdfManifest manifest,
        OdfSaveOptions options)
    {
        writer.WriteStartElement("manifest", "manifest", OdfNamespaces.Manifest);
        writer.WriteAttributeString(
            "xmlns",
            "manifest",
            XmlnsNamespace,
            OdfNamespaces.Manifest);
        WriteRootNamespaces(writer, manifest.Root.NamespaceDeclarations, options);
        writer.WriteAttributeString(
            "manifest",
            "version",
            OdfNamespaces.Manifest,
            manifest.Version);
        if (PreservesUnknownContent(options))
        {
            WriteForeignAttributes(
                writer,
                manifest.ForeignAttributes,
                options,
                "version");
        }

        if (options.Deterministic || options.SortManifestEntries)
        {
            WriteCanonicalRootContent(writer, manifest, options);
        }
        else
        {
            WriteSourceRootContent(writer, manifest, options);
        }
        writer.WriteEndElement();
    }

    static void WriteRootNamespaces(
        XmlWriter writer,
        OdfNamespaceDeclaration[] declarations,
        OdfSaveOptions options)
    {
        OdfNamespaceDeclaration[] ordered = declarations;
        if (options.Deterministic)
        {
            ordered = SortNamespaces(declarations);
        }
        foreach (OdfNamespaceDeclaration declaration in ordered)
        {
            if (declaration.Prefix == "manifest")
            {
                continue;
            }
            if (!AllowsNamespace(options, declaration.NamespaceName))
            {
                continue;
            }
            WriteNamespace(writer, declaration);
        }
    }

    static void WriteNamespace(
        XmlWriter writer,
        OdfNamespaceDeclaration declaration)
    {
        if (declaration.Prefix.Length == 0)
        {
            writer.WriteAttributeString(
                "",
                "xmlns",
                XmlnsNamespace,
                declaration.NamespaceName);
        }
        else
        {
            writer.WriteAttributeString(
                "xmlns",
                declaration.Prefix,
                XmlnsNamespace,
                declaration.NamespaceName);
        }
    }

    static void WriteCanonicalRootContent(
        XmlWriter writer,
        OdfManifest manifest,
        OdfSaveOptions options)
    {
        foreach (OdfManifestNode encryptedKey in manifest.EncryptedKeys)
        {
            WriteNode(writer, encryptedKey, options);
        }
        if (PreservesUnknownContent(options))
        {
            foreach (OdfManifestNode foreignElement in manifest.ForeignElements)
            {
                WriteNode(writer, foreignElement, options);
            }
        }
        OdfManifestEntry[] entries = SortEntries(manifest.Entries);
        foreach (OdfManifestEntry entry in entries)
        {
            WriteEntry(writer, entry, options);
        }
    }

    static void WriteSourceRootContent(
        XmlWriter writer,
        OdfManifest manifest,
        OdfSaveOptions options)
    {
        int entryIndex = 0;
        int encryptedKeyIndex = 0;
        int foreignIndex = 0;
        int tokenIndex = 0;
        bool hasOrder = manifest.Root.OrderedContentXml.Length
            == manifest.Root.OrderedContentElementIndexes.Length;
        while (hasOrder && tokenIndex < manifest.Root.OrderedContentXml.Length)
        {
            int childIndex = manifest.Root.OrderedContentElementIndexes[tokenIndex];
            if (childIndex < 0)
            {
                WriteRetainedToken(writer, manifest.Root.OrderedContentXml[tokenIndex], options);
            }
            else if (childIndex < manifest.Root.Children.Length)
            {
                OdfManifestNode sourceChild = manifest.Root.Children[childIndex];
                if (sourceChild.IsManifestElement
                    && sourceChild.Name.LocalName == "file-entry")
                {
                    if (entryIndex < manifest.Entries.Length)
                    {
                        WriteEntry(writer, manifest.Entries[entryIndex], options);
                        entryIndex = entryIndex + 1;
                    }
                }
                else if (sourceChild.IsManifestElement
                    && sourceChild.Name.LocalName == "encrypted-key")
                {
                    if (encryptedKeyIndex < manifest.EncryptedKeys.Length)
                    {
                        WriteNode(writer, manifest.EncryptedKeys[encryptedKeyIndex], options);
                        encryptedKeyIndex = encryptedKeyIndex + 1;
                    }
                }
                else if (PreservesUnknownContent(options)
                    && foreignIndex < manifest.ForeignElements.Length)
                {
                    WriteNode(writer, manifest.ForeignElements[foreignIndex], options);
                    foreignIndex = foreignIndex + 1;
                }
            }
            tokenIndex = tokenIndex + 1;
        }

        while (encryptedKeyIndex < manifest.EncryptedKeys.Length)
        {
            WriteNode(writer, manifest.EncryptedKeys[encryptedKeyIndex], options);
            encryptedKeyIndex = encryptedKeyIndex + 1;
        }
        if (PreservesUnknownContent(options))
        {
            while (foreignIndex < manifest.ForeignElements.Length)
            {
                WriteNode(writer, manifest.ForeignElements[foreignIndex], options);
                foreignIndex = foreignIndex + 1;
            }
        }
        while (entryIndex < manifest.Entries.Length)
        {
            WriteEntry(writer, manifest.Entries[entryIndex], options);
            entryIndex = entryIndex + 1;
        }
    }

    static void WriteRetainedToken(
        XmlWriter writer,
        string token,
        OdfSaveOptions options)
    {
        if (!PreservesUnknownContent(options))
        {
            return;
        }
        if (options.IndentXml && token.Trim().Length == 0)
        {
            return;
        }
        writer.WriteRaw(token);
    }

    static void WriteEntry(
        XmlWriter writer,
        OdfManifestEntry entry,
        OdfSaveOptions options)
    {
        writer.WriteStartElement("manifest", "file-entry", OdfNamespaces.Manifest);
        writer.WriteAttributeString(
            "manifest",
            "full-path",
            OdfNamespaces.Manifest,
            entry.FullPath);
        writer.WriteAttributeString(
            "manifest",
            "media-type",
            OdfNamespaces.Manifest,
            entry.MediaType);
        if (entry.HasSize)
        {
            writer.WriteAttributeString(
                "manifest",
                "size",
                OdfNamespaces.Manifest,
                FormatSize(entry, options));
        }
        if (entry.PreferredViewMode.Length > 0)
        {
            writer.WriteAttributeString(
                "manifest",
                "preferred-view-mode",
                OdfNamespaces.Manifest,
                entry.PreferredViewMode);
        }
        if (entry.Version.Length > 0)
        {
            writer.WriteAttributeString(
                "manifest",
                "version",
                OdfNamespaces.Manifest,
                entry.Version);
        }
        if (PreservesUnknownContent(options))
        {
            WriteForeignAttributes(
                writer,
                entry.ForeignAttributes,
                options,
                "full-path|media-type|size|preferred-view-mode|version");
        }
        WriteEntryContent(writer, entry, options);
        writer.WriteEndElement();
    }

    static string FormatSize(OdfManifestEntry entry, OdfSaveOptions options)
    {
        if (options.PreserveLexicalValues && entry.SizeLexical.Length > 0)
        {
            return entry.SizeLexical;
        }
        if (entry.SizeIsValid)
        {
            return entry.Size.ToString(CultureInfo.InvariantCulture);
        }
        return entry.SizeLexical;
    }

    static void WriteEntryContent(
        XmlWriter writer,
        OdfManifestEntry entry,
        OdfSaveOptions options)
    {
        int encryptionIndex = 0;
        int foreignIndex = 0;
        int tokenIndex = 0;
        bool hasOrder = entry.Node.OrderedContentXml.Length
            == entry.Node.OrderedContentElementIndexes.Length;
        while (hasOrder && tokenIndex < entry.Node.OrderedContentXml.Length)
        {
            int childIndex = entry.Node.OrderedContentElementIndexes[tokenIndex];
            if (childIndex < 0)
            {
                WriteRetainedToken(writer, entry.Node.OrderedContentXml[tokenIndex], options);
            }
            else if (childIndex < entry.Node.Children.Length)
            {
                OdfManifestNode sourceChild = entry.Node.Children[childIndex];
                if (sourceChild.IsManifestElement
                    && sourceChild.Name.LocalName == "encryption-data"
                    && encryptionIndex == 0)
                {
                    if (entry.EncryptionData != null)
                    {
                        WriteNode(writer, entry.EncryptionData, options);
                    }
                    encryptionIndex = 1;
                }
                else if (PreservesUnknownContent(options)
                    && foreignIndex < entry.ForeignElements.Length)
                {
                    WriteNode(writer, entry.ForeignElements[foreignIndex], options);
                    foreignIndex = foreignIndex + 1;
                }
            }
            tokenIndex = tokenIndex + 1;
        }
        if (encryptionIndex == 0 && entry.EncryptionData != null)
        {
            WriteNode(writer, entry.EncryptionData, options);
        }
        if (PreservesUnknownContent(options))
        {
            while (foreignIndex < entry.ForeignElements.Length)
            {
                WriteNode(writer, entry.ForeignElements[foreignIndex], options);
                foreignIndex = foreignIndex + 1;
            }
        }
    }

    static void WriteNode(
        XmlWriter writer,
        OdfManifestNode node,
        OdfSaveOptions options)
    {
        if (!AllowsNamespace(options, node.Name.NamespaceName))
        {
            return;
        }
        string prefix = node.Name.Prefix;
        if (node.Name.NamespaceName == OdfNamespaces.Manifest)
        {
            prefix = "manifest";
        }
        writer.WriteStartElement(prefix, node.Name.LocalName, node.Name.NamespaceName);
        WriteNodeNamespaces(writer, node.NamespaceDeclarations, prefix, node.Name.NamespaceName, options);
        WriteNodeAttributes(writer, node.Attributes, options);
        WriteNodeContent(writer, node, options);
        writer.WriteEndElement();
    }

    static void WriteNodeNamespaces(
        XmlWriter writer,
        OdfNamespaceDeclaration[] declarations,
        string elementPrefix,
        string elementNamespace,
        OdfSaveOptions options)
    {
        OdfNamespaceDeclaration[] ordered = declarations;
        if (options.Deterministic)
        {
            ordered = SortNamespaces(declarations);
        }
        foreach (OdfNamespaceDeclaration declaration in ordered)
        {
            if (declaration.Prefix == "manifest")
            {
                continue;
            }
            if (declaration.Prefix == elementPrefix
                && declaration.NamespaceName == elementNamespace)
            {
                continue;
            }
            if (!AllowsNamespace(options, declaration.NamespaceName))
            {
                continue;
            }
            WriteNamespace(writer, declaration);
        }
    }

    static void WriteNodeAttributes(
        XmlWriter writer,
        OdfAttribute[] attributes,
        OdfSaveOptions options)
    {
        OdfAttribute[] ordered = attributes;
        if (options.Deterministic)
        {
            ordered = SortAttributes(attributes);
        }
        foreach (OdfAttribute attribute in ordered)
        {
            if (attribute.Name.NamespaceName == XmlnsNamespace)
            {
                continue;
            }
            if (!AllowsNamespace(options, attribute.Name.NamespaceName))
            {
                continue;
            }
            string prefix = attribute.Name.Prefix;
            if (attribute.Name.NamespaceName == OdfNamespaces.Manifest)
            {
                prefix = "manifest";
            }
            writer.WriteAttributeString(
                prefix,
                attribute.Name.LocalName,
                attribute.Name.NamespaceName,
                attribute.Value);
        }
    }

    static void WriteNodeContent(
        XmlWriter writer,
        OdfManifestNode node,
        OdfSaveOptions options)
    {
        bool hasOrder = node.OrderedContentXml.Length
            == node.OrderedContentElementIndexes.Length;
        if (hasOrder && node.OrderedContentXml.Length > 0)
        {
            int tokenIndex = 0;
            while (tokenIndex < node.OrderedContentXml.Length)
            {
                int childIndex = node.OrderedContentElementIndexes[tokenIndex];
                if (childIndex >= 0 && childIndex < node.Children.Length)
                {
                    WriteNode(writer, node.Children[childIndex], options);
                }
                else
                {
                    writer.WriteRaw(node.OrderedContentXml[tokenIndex]);
                }
                tokenIndex = tokenIndex + 1;
            }
            return;
        }
        if (node.TextContent.Length > 0)
        {
            writer.WriteString(node.TextContent);
        }
        foreach (OdfManifestNode child in node.Children)
        {
            WriteNode(writer, child, options);
        }
    }

    static void WriteForeignAttributes(
        XmlWriter writer,
        OdfAttribute[] attributes,
        OdfSaveOptions options,
        string knownNames)
    {
        OdfAttribute[] ordered = attributes;
        if (options.Deterministic)
        {
            ordered = SortAttributes(attributes);
        }
        foreach (OdfAttribute attribute in ordered)
        {
            if (attribute.Name.NamespaceName == XmlnsNamespace)
            {
                continue;
            }
            if (attribute.Name.NamespaceName == OdfNamespaces.Manifest
                && ContainsKnownName(knownNames, attribute.Name.LocalName))
            {
                continue;
            }
            if (!AllowsNamespace(options, attribute.Name.NamespaceName))
            {
                continue;
            }
            writer.WriteAttributeString(
                attribute.Name.Prefix,
                attribute.Name.LocalName,
                attribute.Name.NamespaceName,
                attribute.Value);
        }
    }

    static bool ContainsKnownName(string knownNames, string localName)
    {
        string padded = "|" + knownNames + "|";
        return padded.IndexOf(
            "|" + localName + "|",
            StringComparison.Ordinal) >= 0;
    }

    static bool PreservesUnknownContent(OdfSaveOptions options)
    {
        CompatibilityProfilePolicy policy =
            OdfCompatibilityProfileRules.Resolve(
                options.CompatibilityProfile);
        if (!policy.IsValid)
        {
            throw new OdfPackageException(
                "The requested output compatibility profile is not recognized.");
        }
        return options.PreserveUnknownContent
            || policy.PreserveAllExtensions
            || policy.PreserveLibreOfficeExtensions
            || policy.PreserveOpenOfficeExtensions
            || policy.PreserveMicrosoftOfficeExtensions;
    }

    static bool AllowsNamespace(
        OdfSaveOptions options,
        string namespaceName)
    {
        CompatibilityProfilePolicy policy =
            OdfCompatibilityProfileRules.Resolve(
                options.CompatibilityProfile);
        if (!policy.IsValid)
        {
            throw new OdfPackageException(
                "The requested output compatibility profile is not recognized.");
        }
        return OdfCompatibilityProfileRules.AllowsNamespace(
            policy,
            namespaceName);
    }

    static bool CanUseOriginalForProfile(OdfSaveOptions options)
    {
        return options.CompatibilityProfile.Length == 0
            || options.CompatibilityProfile
                == OdfCompatibilityProfiles.PreserveExisting;
    }

    static OdfManifestEntry[] SortEntries(OdfManifestEntry[] source)
    {
        OdfManifestEntry[] result = new OdfManifestEntry[source.Length];
        Array.Copy(source, result, source.Length);
        int index = 1;
        while (index < result.Length)
        {
            OdfManifestEntry current = result[index];
            int destination = index;
            while (destination > 0
                && CompareEntries(current, result[destination - 1]) < 0)
            {
                result[destination] = result[destination - 1];
                destination = destination - 1;
            }
            result[destination] = current;
            index = index + 1;
        }
        return result;
    }

    static int CompareEntries(OdfManifestEntry first, OdfManifestEntry second)
    {
        int comparison = string.CompareOrdinal(first.FullPath, second.FullPath);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(first.MediaType, second.MediaType);
    }

    static OdfNamespaceDeclaration[] SortNamespaces(
        OdfNamespaceDeclaration[] source)
    {
        OdfNamespaceDeclaration[] result = new OdfNamespaceDeclaration[source.Length];
        Array.Copy(source, result, source.Length);
        int index = 1;
        while (index < result.Length)
        {
            OdfNamespaceDeclaration current = result[index];
            int destination = index;
            while (destination > 0
                && CompareNamespaces(current, result[destination - 1]) < 0)
            {
                result[destination] = result[destination - 1];
                destination = destination - 1;
            }
            result[destination] = current;
            index = index + 1;
        }
        return result;
    }

    static int CompareNamespaces(
        OdfNamespaceDeclaration first,
        OdfNamespaceDeclaration second)
    {
        int comparison = string.CompareOrdinal(first.Prefix, second.Prefix);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(first.NamespaceName, second.NamespaceName);
    }

    static OdfAttribute[] SortAttributes(OdfAttribute[] source)
    {
        OdfAttribute[] result = new OdfAttribute[source.Length];
        Array.Copy(source, result, source.Length);
        int index = 1;
        while (index < result.Length)
        {
            OdfAttribute current = result[index];
            int destination = index;
            while (destination > 0
                && CompareAttributes(current, result[destination - 1]) < 0)
            {
                result[destination] = result[destination - 1];
                destination = destination - 1;
            }
            result[destination] = current;
            index = index + 1;
        }
        return result;
    }

    static int CompareAttributes(OdfAttribute first, OdfAttribute second)
    {
        int comparison = string.CompareOrdinal(
            first.Name.NamespaceName,
            second.Name.NamespaceName);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(
            first.Name.LocalName,
            second.Name.LocalName);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(first.Name.Prefix, second.Name.Prefix);
    }
}
