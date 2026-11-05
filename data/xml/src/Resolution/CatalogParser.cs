using System;
using System.Collections.Generic;

namespace xml;

internal class CatalogParser
{
    internal bool DefaultPreferPublic;

    internal CatalogParser(bool defaultPreferPublic)
    {
        DefaultPreferPublic = defaultPreferPublic;
    }

    internal XmlCatalogEntry[] ParseXmlCatalog(XmlDocument document)
    {
        Guard.NotNull(document, "document");
        XmlNode? root = document.DocumentElement;
        if (root == null || root.Kind != XmlNodeKind.Element || CatalogNodeLocalName(root) != "catalog" || CatalogNodeNamespace(root) != XmlNamespaces.Catalog)
        {
            throw new FormatException("An XML catalog document must have a catalog root element in the XML Catalog namespace.");
        }

        ValidateXmlCatalogAttributes(root, [], true);
        string? documentBase = document.BaseUri == null ? document.DocumentUri : document.BaseUri;
        string? effectiveBase = ApplyXmlCatalogBase(root, documentBase);
        bool preferPublic = ApplyXmlCatalogPreference(root, DefaultPreferPublic);
        return ParseXmlCatalogChildren(root, effectiveBase, preferPublic, true);
    }

    internal XmlCatalogEntry[] ParseXmlCatalogChildren(XmlNode parent, string? inheritedBase, bool preferPublic, bool allowGroups)
    {
        List<XmlCatalogEntry> entries = new List<XmlCatalogEntry>();
        XmlNode[] children = parent.Children == null ? [] : parent.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                string namespaceUri = CatalogNodeNamespace(child);
                if (namespaceUri == XmlNamespaces.Catalog)
                {
                    XmlCatalogEntry entry = ParseXmlCatalogEntry(child, inheritedBase, preferPublic, allowGroups);
                    entries.Add(entry);
                }
            }
            else if (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData)
            {
                if (!IsXmlCatalogWhitespace(child.Value == null ? "" : child.Value))
                {
                    throw new FormatException("XML catalog containers may contain only catalog entries and whitespace.");
                }
            }

            index = index + 1;
        }

        return entries.ToArray();
    }

    internal XmlCatalogEntry ParseXmlCatalogEntry(XmlNode node, string? inheritedBase, bool inheritedPreferPublic, bool allowGroups)
    {
        string name = CatalogNodeLocalName(node);
        string? effectiveBase = ApplyXmlCatalogBase(node, inheritedBase);
        bool preferPublic = inheritedPreferPublic;
        if (name == "group")
        {
            if (!allowGroups)
            {
                throw new FormatException("An XML catalog group cannot contain another group.");
            }

            preferPublic = ApplyXmlCatalogPreference(node, inheritedPreferPublic);
            ValidateXmlCatalogAttributes(node, [], true);
            XmlCatalogEntry group = CreateXmlCatalogEntry(XmlCatalogEntryKind.Group, effectiveBase, preferPublic, node);
            group.Entries = ParseXmlCatalogChildren(node, effectiveBase, preferPublic, false);
            return group;
        }

        EnsureEmptyXmlCatalogEntry(node);
        XmlCatalogEntry entry;
        if (name == "public")
        {
            ValidateXmlCatalogAttributes(node, ["publicId", "uri"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.Public, effectiveBase, preferPublic, node);
            entry.PublicIdentifier = RequiredXmlCatalogAttribute(node, "publicId");
            entry.TargetUri = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "uri"), effectiveBase, true);
        }
        else if (name == "system")
        {
            ValidateXmlCatalogAttributes(node, ["systemId", "uri"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.System, effectiveBase, preferPublic, node);
            entry.SystemIdentifier = RequiredXmlCatalogAttribute(node, "systemId");
            entry.TargetUri = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "uri"), effectiveBase, true);
        }
        else if (name == "uri")
        {
            ValidateXmlCatalogAttributes(node, ["name", "uri"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.Uri, effectiveBase, preferPublic, node);
            entry.Uri = RequiredXmlCatalogAttribute(node, "name");
            entry.TargetUri = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "uri"), effectiveBase, false);
        }
        else if (name == "rewriteSystem")
        {
            ValidateXmlCatalogAttributes(node, ["systemIdStartString", "rewritePrefix"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.RewriteSystem, effectiveBase, preferPublic, node);
            entry.SystemIdentifier = RequiredXmlCatalogAttribute(node, "systemIdStartString");
            entry.RewritePrefix = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "rewritePrefix"), effectiveBase, false);
        }
        else if (name == "rewriteURI")
        {
            ValidateXmlCatalogAttributes(node, ["uriStartString", "rewritePrefix"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.RewriteUri, effectiveBase, preferPublic, node);
            entry.Uri = RequiredXmlCatalogAttribute(node, "uriStartString");
            entry.RewritePrefix = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "rewritePrefix"), effectiveBase, false);
        }
        else if (name == "systemSuffix")
        {
            ValidateXmlCatalogAttributes(node, ["systemIdSuffix", "uri"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.SystemSuffix, effectiveBase, preferPublic, node);
            entry.Suffix = RequiredXmlCatalogAttribute(node, "systemIdSuffix");
            entry.TargetUri = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "uri"), effectiveBase, true);
        }
        else if (name == "uriSuffix")
        {
            ValidateXmlCatalogAttributes(node, ["uriSuffix", "uri"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.UriSuffix, effectiveBase, preferPublic, node);
            entry.Suffix = RequiredXmlCatalogAttribute(node, "uriSuffix");
            entry.TargetUri = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "uri"), effectiveBase, false);
        }
        else if (name == "delegatePublic")
        {
            ValidateXmlCatalogAttributes(node, ["publicIdStartString", "catalog"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.DelegatePublic, effectiveBase, preferPublic, node);
            entry.PublicIdentifier = RequiredXmlCatalogAttribute(node, "publicIdStartString");
            entry.CatalogUri = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "catalog"), effectiveBase, true);
        }
        else if (name == "delegateSystem")
        {
            ValidateXmlCatalogAttributes(node, ["systemIdStartString", "catalog"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.DelegateSystem, effectiveBase, preferPublic, node);
            entry.SystemIdentifier = RequiredXmlCatalogAttribute(node, "systemIdStartString");
            entry.CatalogUri = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "catalog"), effectiveBase, true);
        }
        else if (name == "delegateURI")
        {
            ValidateXmlCatalogAttributes(node, ["uriStartString", "catalog"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.DelegateUri, effectiveBase, preferPublic, node);
            entry.Uri = RequiredXmlCatalogAttribute(node, "uriStartString");
            entry.CatalogUri = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "catalog"), effectiveBase, true);
        }
        else if (name == "nextCatalog")
        {
            ValidateXmlCatalogAttributes(node, ["catalog"], false);
            entry = CreateXmlCatalogEntry(XmlCatalogEntryKind.NextCatalog, effectiveBase, preferPublic, node);
            entry.CatalogUri = ResolveXmlCatalogReference(RequiredXmlCatalogAttribute(node, "catalog"), effectiveBase, true);
        }
        else
        {
            throw new FormatException("The XML Catalog namespace element '" + name + "' is not recognized.");
        }

        return entry;
    }

    internal void ValidateXmlCatalogAttributes(XmlNode node, string[] allowedNames, bool allowPreference)
    {
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            string namespaceUri = CatalogNodeNamespace(attribute);
            string localName = CatalogNodeLocalName(attribute);
            if (namespaceUri == "")
            {
                bool allowed = localName == "id" || allowPreference && localName == "prefer" || ContainsXmlCatalogAttributeName(allowedNames, localName);
                if (!allowed)
                {
                    throw new FormatException("Attribute '" + localName + "' is not allowed on XML catalog element '" + CatalogNodeLocalName(node) + "'.");
                }
            }
            else if (namespaceUri == XmlNamespaces.Catalog)
            {
                throw new FormatException("XML catalog attributes must not be namespace-qualified.");
            }

            index = index + 1;
        }
    }

    internal bool ContainsXmlCatalogAttributeName(string[] names, string candidate)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == candidate)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XmlCatalogEntry CreateXmlCatalogEntry(XmlCatalogEntryKind kind, string? baseUri, bool preferPublic, XmlNode source)
    {
        XmlCatalogEntry entry = new XmlCatalogEntry();
        entry.Kind = kind;
        entry.BaseUri = baseUri;
        entry.PreferPublic = preferPublic;
        entry.Entries = [];
        entry.Range = source.Range;
        return entry;
    }

    internal string RequiredXmlCatalogAttribute(XmlNode node, string name)
    {
        string? value = FindXmlCatalogAttribute(node, name, "");
        if (value == null || value.Length == 0)
        {
            throw new FormatException("XML catalog element '" + CatalogNodeLocalName(node) + "' requires a non-empty '" + name + "' attribute.");
        }

        return value;
    }

    internal string? FindXmlCatalogAttribute(XmlNode node, string localName, string namespaceUri)
    {
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            if (attribute.Kind == XmlNodeKind.Attribute && CatalogNodeLocalName(attribute) == localName && CatalogNodeNamespace(attribute) == namespaceUri)
            {
                return attribute.Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal string? ApplyXmlCatalogBase(XmlNode node, string? inheritedBase)
    {
        string? reference = FindXmlCatalogAttribute(node, "base", XmlNamespaces.Xml);
        if (reference == null)
        {
            return inheritedBase == null ? null : UriTools.CanonicalAbsoluteUri(UriTools.CreateAbsolute(inheritedBase));
        }

        return ResolveXmlCatalogReference(reference, inheritedBase, true);
    }

    internal bool ApplyXmlCatalogPreference(XmlNode node, bool inheritedPreference)
    {
        string? preference = FindXmlCatalogAttribute(node, "prefer", "");
        if (preference == null)
        {
            return inheritedPreference;
        }

        if (preference == "public")
        {
            return true;
        }

        if (preference == "system")
        {
            return false;
        }

        throw new FormatException("XML catalog prefer must be either 'public' or 'system'.");
    }

    internal string ResolveXmlCatalogReference(string reference, string? baseUri, bool rejectFragment)
    {
        string normalizedReference = CatalogIdentifiers.NormalizeXmlCatalogIdentifier(reference);
        Uri resolved;
        if (UriTools.HasUriScheme(normalizedReference))
        {
            resolved = UriTools.CreateAbsolute(normalizedReference);
        }
        else
        {
            if (baseUri == null)
            {
                throw new FormatException("A relative XML catalog URI requires an absolute catalog base URI.");
            }

            resolved = UriTools.ResolveReference(UriTools.CreateAbsolute(baseUri), normalizedReference);
        }

        if (rejectFragment && UriTools.HasFragment(resolved.OriginalString))
        {
            throw new FormatException("This XML catalog URI must not contain a fragment identifier.");
        }

        return UriTools.CanonicalAbsoluteUri(resolved);
    }

    internal void EnsureEmptyXmlCatalogEntry(XmlNode node)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element || (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && !IsXmlCatalogWhitespace(child.Value == null ? "" : child.Value))
            {
                throw new FormatException("XML catalog entry '" + CatalogNodeLocalName(node) + "' must be empty.");
            }

            index = index + 1;
        }
    }

    internal bool IsXmlCatalogWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current != ' ' && current != '\t' && current != '\r' && current != '\n')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string CatalogNodeLocalName(XmlNode node)
    {
        XmlName? name = node.Name;
        if (name == null)
        {
            return "";
        }

        return name.LocalName == null ? "" : name.LocalName;
    }

    internal string CatalogNodeNamespace(XmlNode node)
    {
        XmlName? name = node.Name;
        if (name == null || name.NamespaceUri == null)
        {
            return "";
        }

        return name.NamespaceUri;
    }
}
