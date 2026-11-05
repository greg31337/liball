namespace version;

internal static class MavenMetadataParser
{
    internal static MavenMetadataParseResult ParseWithDiagnostics(string metadataXml)
    {
        if (metadataXml == null)
        {
            return MetadataFailure(
                "",
                MavenMetadataDiagnosticCodes.MalformedMetadata,
                "Maven metadata XML cannot be null.",
                0);
        }

        int startIndex = GetTrimmedStartIndex(metadataXml);
        if (startIndex >= metadataXml.Length || !StartsWithMetadataRoot(metadataXml, startIndex))
        {
            return MetadataFailure(
                metadataXml,
                MavenMetadataDiagnosticCodes.MalformedMetadata,
                "Maven metadata XML must contain a metadata root element.",
                startIndex);
        }

        if (!HasMetadataClose(metadataXml, startIndex))
        {
            return MetadataFailure(
                metadataXml,
                MavenMetadataDiagnosticCodes.MalformedMetadata,
                "Maven metadata XML must contain a closed metadata root element.",
                startIndex);
        }

        if (!ContainsElement(metadataXml, "groupId") || !ContainsElement(metadataXml, "artifactId"))
        {
            return MetadataFailure(
                metadataXml,
                MavenMetadataDiagnosticCodes.MalformedMetadata,
                "Maven metadata XML must contain groupId and artifactId elements.",
                startIndex);
        }

        if (ContainsElement(metadataXml, "snapshot") && !HasRootVersionBeforeVersioning(metadataXml))
        {
            return MetadataFailure(
                metadataXml,
                MavenMetadataDiagnosticCodes.MissingSnapshotMetadata,
                "Maven snapshot metadata requires a root snapshot version element.",
                FindElementIndex(metadataXml, "snapshot"));
        }

        return MavenMetadataParseResult.CreateSuccess(metadataXml, Parse(metadataXml));
    }

    internal static MavenRepositoryMetadata Parse(string metadataXml)
    {
        System.Xml.XmlElement metadata = GetMetadataElement(LoadDocument(metadataXml));
        string groupIdText = ReadRequiredChildText(metadata, "groupId");
        string artifactIdText = ReadRequiredChildText(metadata, "artifactId");
        string versionText = ReadOptionalChildText(metadata, "version");
        System.Xml.XmlElement? versioning = FindChildElement(metadata, "versioning");

        if (versioning == null)
        {
            return new MavenRepositoryMetadata(
                new MavenGroupId(groupIdText),
                new MavenArtifactId(artifactIdText),
                versionText,
                "",
                "",
                new string[0],
                "",
                null,
                new MavenSnapshotVersionMetadata[0]);
        }

        return new MavenRepositoryMetadata(
            new MavenGroupId(groupIdText),
            new MavenArtifactId(artifactIdText),
            versionText,
            ReadOptionalChildText(versioning, "latest"),
            ReadOptionalChildText(versioning, "release"),
            ParseVersions(versioning),
            ReadOptionalChildText(versioning, "lastUpdated"),
            ParseSnapshotMetadata(groupIdText, artifactIdText, versionText, versioning),
            ParseSnapshotVersions(versioning));
    }

    internal static string[] ParseVersions(string metadataXml)
    {
        System.Xml.XmlElement metadata = GetMetadataElement(LoadDocument(metadataXml));
        System.Xml.XmlElement? versioning = FindChildElement(metadata, "versioning");
        if (versioning == null)
        {
            return new string[0];
        }

        return ParseVersions(versioning);
    }

    internal static MavenSnapshotVersionMetadata[] ParseSnapshotVersions(string metadataXml)
    {
        System.Xml.XmlElement metadata = GetMetadataElement(LoadDocument(metadataXml));
        System.Xml.XmlElement? versioning = FindChildElement(metadata, "versioning");
        if (versioning == null)
        {
            return new MavenSnapshotVersionMetadata[0];
        }

        return ParseSnapshotVersions(versioning);
    }

    internal static System.Xml.XmlDocument LoadDocument(string metadataXml)
    {
        System.ArgumentNullException.ThrowIfNull(metadataXml);

        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.XmlResolver = null;
        document.LoadXml(metadataXml);
        return document;
    }

    internal static MavenMetadataParseResult MetadataFailure(string sourceText, string code, string message, int diagnosticIndex)
    {
        return MavenMetadataParseResult.CreateFailure(
            sourceText,
            VersionDiagnostic.FromIndex(code, message, sourceText, diagnosticIndex));
    }

    internal static int GetTrimmedStartIndex(string text)
    {
        int index = 0;

        while (index < text.Length && VersionChar.IsWhitespace(text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal static bool StartsWithMetadataRoot(string text, int startIndex)
    {
        string token = "<metadata";

        if (startIndex + token.Length > text.Length)
        {
            return false;
        }

        if (text.Substring(startIndex, token.Length) != token)
        {
            return false;
        }

        if (startIndex + token.Length == text.Length)
        {
            return false;
        }

        char next = text[startIndex + token.Length];
        return VersionChar.IsWhitespace(next) || next == '>' || next == '/';
    }

    internal static bool HasMetadataClose(string text, int startIndex)
    {
        int rootEndIndex = FindNext(text, ">", startIndex);
        if (rootEndIndex < 0)
        {
            return false;
        }

        if (rootEndIndex > startIndex && text[rootEndIndex - 1] == '/')
        {
            return true;
        }

        return FindNext(text, "</metadata>", rootEndIndex) >= 0;
    }

    internal static bool ContainsElement(string text, string localName)
    {
        return FindElementIndex(text, localName) >= 0;
    }

    internal static int FindElementIndex(string text, string localName)
    {
        string token = "<" + localName;
        int index = 0;

        while (index < text.Length)
        {
            int foundIndex = FindNext(text, token, index);
            if (foundIndex < 0)
            {
                return -1;
            }

            int endIndex = foundIndex + token.Length;
            if (endIndex < text.Length)
            {
                char next = text[endIndex];
                if (VersionChar.IsWhitespace(next) || next == '>' || next == '/')
                {
                    return foundIndex;
                }
            }

            index = foundIndex + 1;
        }

        return -1;
    }

    internal static bool HasRootVersionBeforeVersioning(string text)
    {
        int versioningIndex = FindElementIndex(text, "versioning");
        int versionIndex = FindElementIndex(text, "version");

        if (versioningIndex < 0)
        {
            return versionIndex >= 0;
        }

        return versionIndex >= 0 && versionIndex < versioningIndex;
    }

    internal static int FindNext(string text, string value, int startIndex)
    {
        return text.IndexOf(value, startIndex, System.StringComparison.Ordinal);
    }

    internal static System.Xml.XmlElement GetMetadataElement(System.Xml.XmlDocument document)
    {
        if (document.DocumentElement == null || document.DocumentElement.LocalName != "metadata")
        {
            throw new System.ArgumentException("Maven metadata XML must contain a metadata root element.");
        }

        return document.DocumentElement;
    }

    internal static string[] ParseVersions(System.Xml.XmlElement versioning)
    {
        System.Xml.XmlElement? versionsElement = FindChildElement(versioning, "versions");
        if (versionsElement == null)
        {
            return new string[0];
        }

        System.Collections.Generic.List<string> versions = new System.Collections.Generic.List<string>();
        System.Xml.XmlNode? node = versionsElement.FirstChild;

        while (node != null)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                System.Xml.XmlElement element = (System.Xml.XmlElement)node;
                if (element.LocalName == "version")
                {
                    versions.Add(TrimText(element.InnerText));
                }
            }

            node = node.NextSibling;
        }

        return versions.ToArray();
    }

    internal static MavenSnapshotMetadata? ParseSnapshotMetadata(
        string groupIdText,
        string artifactIdText,
        string versionText,
        System.Xml.XmlElement versioning)
    {
        System.Xml.XmlElement? snapshotElement = FindChildElement(versioning, "snapshot");
        if (snapshotElement == null)
        {
            return null;
        }

        if (versionText.Length == 0)
        {
            throw new System.ArgumentException("Maven snapshot metadata requires a root version element.");
        }

        MavenArtifactVersionIdentity snapshotIdentity = MavenArtifactVersionIdentity.Create(groupIdText, artifactIdText, versionText);
        if (ReadBooleanChild(snapshotElement, "localCopy"))
        {
            return MavenSnapshotMetadata.CreateLocalSnapshot(snapshotIdentity);
        }

        return MavenSnapshotMetadata.CreateRemoteSnapshot(
            snapshotIdentity,
            ReadOptionalChildText(snapshotElement, "timestamp"),
            ReadIntegerChild(snapshotElement, "buildNumber"),
            ReadOptionalChildText(versioning, "lastUpdated"));
    }

    internal static MavenSnapshotVersionMetadata[] ParseSnapshotVersions(System.Xml.XmlElement versioning)
    {
        System.Xml.XmlElement? snapshotVersionsElement = FindChildElement(versioning, "snapshotVersions");
        if (snapshotVersionsElement == null)
        {
            return new MavenSnapshotVersionMetadata[0];
        }

        System.Collections.Generic.List<MavenSnapshotVersionMetadata> snapshotVersions = new System.Collections.Generic.List<MavenSnapshotVersionMetadata>();
        System.Xml.XmlNode? node = snapshotVersionsElement.FirstChild;

        while (node != null)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                System.Xml.XmlElement element = (System.Xml.XmlElement)node;
                if (element.LocalName == "snapshotVersion")
                {
                    snapshotVersions.Add(ParseSnapshotVersion(element));
                }
            }

            node = node.NextSibling;
        }

        return snapshotVersions.ToArray();
    }

    internal static MavenSnapshotVersionMetadata ParseSnapshotVersion(System.Xml.XmlElement snapshotVersionElement)
    {
        return new MavenSnapshotVersionMetadata(
            ReadRequiredChildText(snapshotVersionElement, "extension"),
            ReadOptionalChildText(snapshotVersionElement, "classifier"),
            ReadRequiredChildText(snapshotVersionElement, "value"),
            ReadOptionalChildText(snapshotVersionElement, "updated"));
    }

    internal static string ReadRequiredChildText(System.Xml.XmlElement parent, string localName)
    {
        string text = ReadOptionalChildText(parent, localName);
        if (text.Length == 0)
        {
            throw new System.ArgumentException("Maven metadata XML is missing required element " + localName + ".");
        }

        return text;
    }

    internal static string ReadOptionalChildText(System.Xml.XmlElement parent, string localName)
    {
        System.Xml.XmlElement? element = FindChildElement(parent, localName);
        if (element == null)
        {
            return "";
        }

        if (element.InnerText == null)
        {
            return "";
        }

        return TrimText(element.InnerText);
    }

    internal static bool ReadBooleanChild(System.Xml.XmlElement parent, string localName)
    {
        string text = ReadOptionalChildText(parent, localName);
        if (text.Length == 0)
        {
            return false;
        }

        if (string.Equals(text, "true", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(text, "false", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        throw new System.ArgumentException("Maven metadata boolean element has an invalid value.");
    }

    internal static int ReadIntegerChild(System.Xml.XmlElement parent, string localName)
    {
        string text = ReadRequiredChildText(parent, localName);
        int value = 0;
        int index = 0;

        while (index < text.Length)
        {
            if (!VersionChar.IsDigit(text[index]))
            {
                throw new System.ArgumentException("Maven metadata integer element has an invalid value.");
            }

            int digit = text[index] - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                throw new System.ArgumentException("Maven metadata integer element has an invalid value.");
            }

            value = (value * 10) + digit;
            index = index + 1;
        }

        return value;
    }

    internal static System.Xml.XmlElement? FindChildElement(System.Xml.XmlElement parent, string localName)
    {
        System.Xml.XmlNode? node = parent.FirstChild;

        while (node != null)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                System.Xml.XmlElement element = (System.Xml.XmlElement)node;
                if (element.LocalName == localName)
                {
                    return element;
                }
            }

            node = node.NextSibling;
        }

        return null;
    }

    internal static string TrimText(string text)
    {
        int startIndex = 0;
        int endIndex = text.Length;

        while (startIndex < endIndex && VersionChar.IsWhitespace(text[startIndex]))
        {
            startIndex = startIndex + 1;
        }

        while (endIndex > startIndex && VersionChar.IsWhitespace(text[endIndex - 1]))
        {
            endIndex = endIndex - 1;
        }

        return text.Substring(startIndex, endIndex - startIndex);
    }
}
