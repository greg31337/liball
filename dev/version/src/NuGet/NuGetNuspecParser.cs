namespace version;

internal static class NuGetNuspecParser
{
    internal static NuGetPackageMetadata ParsePackageMetadata(string nuspecXml)
    {
        System.Xml.XmlElement metadata = GetMetadataElement(LoadDocument(nuspecXml));
        string id = ReadRequiredChildText(metadata, "id");
        string version = ReadRequiredChildText(metadata, "version");
        string minClientVersion = ReadAttributeText(metadata, "minClientVersion");

        return new NuGetPackageMetadata(
            NuGetPackageVersionIdentity.Create(id, version),
            ReadOptionalChildText(metadata, "title"),
            ReadOptionalChildText(metadata, "authors"),
            ReadOptionalChildText(metadata, "owners"),
            ReadOptionalChildText(metadata, "description"),
            ReadOptionalChildText(metadata, "tags"),
            ReadOptionalChildText(metadata, "projectUrl"),
            ReadOptionalChildText(metadata, "licenseUrl"),
            minClientVersion,
            ReadBooleanChild(metadata, "requireLicenseAcceptance"),
            ReadBooleanChild(metadata, "developmentDependency"),
            ParseDependencyGroups(metadata));
    }

    internal static NuGetDependencyGroup[] ParseDependencyGroups(string nuspecXml)
    {
        System.Xml.XmlElement metadata = GetMetadataElement(LoadDocument(nuspecXml));
        return ParseDependencyGroups(metadata);
    }

    internal static System.Xml.XmlDocument LoadDocument(string nuspecXml)
    {
        System.ArgumentNullException.ThrowIfNull(nuspecXml);

        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.XmlResolver = null;
        document.LoadXml(nuspecXml);
        return document;
    }

    internal static System.Xml.XmlElement GetMetadataElement(System.Xml.XmlDocument document)
    {
        if (document.DocumentElement == null || document.DocumentElement.LocalName != "package")
        {
            throw new System.ArgumentException("NuGet nuspec XML must contain a package root element.");
        }

        System.Xml.XmlElement? metadata = FindChildElement(document.DocumentElement, "metadata");
        if (metadata == null)
        {
            throw new System.ArgumentException("NuGet nuspec XML must contain a metadata element.");
        }

        return metadata;
    }

    internal static NuGetDependencyGroup[] ParseDependencyGroups(System.Xml.XmlElement metadata)
    {
        System.Xml.XmlElement? dependenciesElement = FindChildElement(metadata, "dependencies");
        if (dependenciesElement == null)
        {
            return new NuGetDependencyGroup[0];
        }

        System.Collections.Generic.List<NuGetDependencyGroup> groups = new System.Collections.Generic.List<NuGetDependencyGroup>();
        System.Collections.Generic.List<NuGetDependency> directDependencies = new System.Collections.Generic.List<NuGetDependency>();
        System.Xml.XmlNode? node = dependenciesElement.FirstChild;

        while (node != null)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                System.Xml.XmlElement element = (System.Xml.XmlElement)node;
                if (element.LocalName == "dependency")
                {
                    directDependencies.Add(ParseDependency(element));
                }
                else if (element.LocalName == "group")
                {
                    groups.Add(ParseDependencyGroup(element));
                }
            }

            node = node.NextSibling;
        }

        if (directDependencies.Count > 0)
        {
            groups.Insert(0, NuGetDependencyGroup.CreateAnyFramework(directDependencies.ToArray()));
        }

        return groups.ToArray();
    }

    internal static NuGetDependencyGroup ParseDependencyGroup(System.Xml.XmlElement groupElement)
    {
        string targetFramework = ReadAttributeText(groupElement, "targetFramework");
        System.Collections.Generic.List<NuGetDependency> dependencies = new System.Collections.Generic.List<NuGetDependency>();
        System.Xml.XmlNode? node = groupElement.FirstChild;

        while (node != null)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                System.Xml.XmlElement element = (System.Xml.XmlElement)node;
                if (element.LocalName == "dependency")
                {
                    dependencies.Add(ParseDependency(element));
                }
            }

            node = node.NextSibling;
        }

        return new NuGetDependencyGroup(targetFramework, dependencies.ToArray());
    }

    internal static NuGetDependency ParseDependency(System.Xml.XmlElement dependencyElement)
    {
        string id = ReadRequiredAttributeText(dependencyElement, "id");
        string version = ReadAttributeText(dependencyElement, "version");

        if (version.Length == 0)
        {
            return NuGetDependency.CreateAnyVersion(id);
        }

        return NuGetDependency.Create(id, version);
    }

    internal static string ReadRequiredChildText(System.Xml.XmlElement parent, string localName)
    {
        string text = ReadOptionalChildText(parent, localName);
        if (text.Length == 0)
        {
            throw new System.ArgumentException("NuGet nuspec metadata is missing required element " + localName + ".");
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

        throw new System.ArgumentException("NuGet nuspec boolean element has an invalid value.");
    }

    internal static string ReadRequiredAttributeText(System.Xml.XmlElement element, string localName)
    {
        string value = ReadAttributeText(element, localName);
        if (value.Length == 0)
        {
            throw new System.ArgumentException("NuGet nuspec element is missing required attribute " + localName + ".");
        }

        return value;
    }

    internal static string ReadAttributeText(System.Xml.XmlElement element, string localName)
    {
        int index = 0;

        while (index < element.Attributes.Count)
        {
            System.Xml.XmlAttribute attribute = element.Attributes[index];
            if (attribute.LocalName == localName)
            {
                return TrimText(attribute.Value);
            }

            index = index + 1;
        }

        return "";
    }

    internal static System.Xml.XmlElement? FindChildElement(System.Xml.XmlElement? parent, string localName)
    {
        if (parent == null)
        {
            return null;
        }

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
