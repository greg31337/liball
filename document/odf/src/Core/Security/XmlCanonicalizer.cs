using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace odf;

internal class XmlCanonicalizationMode
{
    internal bool Exclusive;
    internal bool IncludeComments;
    internal bool Version11;
}

internal class CanonicalXmlAttribute
{
    internal string NamespaceName = "";
    internal string LocalName = "";
    internal string QualifiedName = "";
    internal string Value = "";
}

internal class CanonicalXmlOutput
{
    StringBuilder builder = new StringBuilder();
    long maximumBytes;

    internal CanonicalXmlOutput(long maximumBytesValue)
    {
        maximumBytes = maximumBytesValue;
    }

    internal void Write(string value)
    {
        if (value.Length > maximumBytes - builder.Length)
        {
            throw new OdfSecurityException("Canonical XML output exceeds the configured size limit.");
        }
        builder.Append(value);
    }

    internal byte[] GetUtf8Bytes()
    {
        string value = builder.ToString();
        if (Encoding.UTF8.GetByteCount(value) > maximumBytes)
        {
            throw new OdfSecurityException("Canonical XML output exceeds the configured size limit.");
        }
        return new UTF8Encoding(false).GetBytes(value);
    }
}

internal static class XmlCanonicalizer
{
    const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    internal static byte[] CanonicalizeBytes(
        byte[] input,
        string algorithm,
        string inclusivePrefixes,
        OdfSecurityLimits limits)
    {
        XmlCanonicalizationMode mode = ResolveMode(algorithm);
        ValidateLimits(limits);
        if (input.LongLength > limits.MaximumSingleEntryBytes)
        {
            throw new OdfSecurityException("Canonical XML input exceeds the configured size limit.");
        }

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.IgnoreComments = false;
        settings.IgnoreWhitespace = false;
        settings.MaxCharactersFromEntities = 0;
        settings.MaxCharactersInDocument = limits.MaximumSingleEntryBytes;
        XmlDocument document = new XmlDocument();
        document.XmlResolver = null;
        document.PreserveWhitespace = true;
        using (MemoryStream stream = new MemoryStream(input, false))
        using (XmlReader reader = XmlReader.Create(stream, settings))
        {
            document.Load(reader);
        }
        XmlCanonicalizationWriter writer = new XmlCanonicalizationWriter(
            mode,
            inclusivePrefixes,
            limits);
        return writer.WriteDocument(document);
    }

    internal static byte[] CanonicalizeElement(
        XmlElement element,
        string algorithm,
        string inclusivePrefixes,
        OdfSecurityLimits limits)
    {
        XmlCanonicalizationMode mode = ResolveMode(algorithm);
        ValidateLimits(limits);
        XmlCanonicalizationWriter writer = new XmlCanonicalizationWriter(
            mode,
            inclusivePrefixes,
            limits);
        return writer.WriteElementSubtree(element);
    }

    static XmlCanonicalizationMode ResolveMode(string algorithm)
    {
        XmlCanonicalizationMode result = new XmlCanonicalizationMode();
        if (algorithm == OdfAlgorithmIdentifiers.CanonicalXml10)
        {
            return result;
        }
        if (algorithm == OdfAlgorithmIdentifiers.CanonicalXml10WithComments)
        {
            result.IncludeComments = true;
            return result;
        }
        if (algorithm == OdfAlgorithmIdentifiers.CanonicalXml11)
        {
            result.Version11 = true;
            return result;
        }
        if (algorithm == OdfAlgorithmIdentifiers.CanonicalXml11WithComments)
        {
            result.Version11 = true;
            result.IncludeComments = true;
            return result;
        }
        if (algorithm == OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10)
        {
            result.Exclusive = true;
            return result;
        }
        if (algorithm == OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10WithComments)
        {
            result.Exclusive = true;
            result.IncludeComments = true;
            return result;
        }
        throw new OdfSecurityException("The XML canonicalization algorithm is not supported.");
    }

    static void ValidateLimits(OdfSecurityLimits limits)
    {
        if (limits == null
            || limits.MaximumSingleEntryBytes <= 0
            || limits.MaximumXmlDepth <= 0
            || limits.MaximumXmlAttributesPerElement < 0
            || limits.MaximumXmlTextLength < 0)
        {
            throw new OdfSecurityException("Canonical XML limits must be non-negative and bounded.");
        }
    }

    internal static bool IsNamespaceAttribute(XmlAttribute attribute)
    {
        return attribute.NamespaceURI == XmlnsNamespace;
    }

    internal static string GetNamespacePrefix(XmlAttribute attribute)
    {
        return attribute.Prefix == "xmlns" ? attribute.LocalName : "";
    }

    internal static bool IsXmlAttribute(XmlAttribute attribute)
    {
        return attribute.NamespaceURI == XmlNamespace;
    }
}

internal class XmlCanonicalizationWriter
{
    const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    XmlCanonicalizationMode mode;
    OdfSecurityLimits limits;
    CanonicalXmlOutput output;
    HashSet<string> inclusivePrefixes = new HashSet<string>(StringComparer.Ordinal);

    internal XmlCanonicalizationWriter(
        XmlCanonicalizationMode modeValue,
        string inclusivePrefixList,
        OdfSecurityLimits limitsValue)
    {
        mode = modeValue;
        limits = limitsValue;
        output = new CanonicalXmlOutput(limits.MaximumSingleEntryBytes);
        string list = inclusivePrefixList;
        if (list == null)
        {
            list = "";
        }
        string[] prefixes = list.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries);
        foreach (string prefix in prefixes)
        {
            if (prefix == "#default")
            {
                inclusivePrefixes.Add("");
            }
            else
            {
                if (!OdfNameRules.IsNcName(prefix))
                {
                    throw new OdfSecurityException("The exclusive canonicalization prefix list is invalid.");
                }
                inclusivePrefixes.Add(prefix);
            }
        }
    }

    internal byte[] WriteDocument(XmlDocument document)
    {
        ValidateNodeTree(document, 0);
        bool foundElement = false;
        foreach (XmlNode child in document.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                Dictionary<string, string> sourceNamespaces = CreateEmptyNamespaces();
                Dictionary<string, string> renderedNamespaces = CreateRenderedNamespaces();
                WriteElementNode(
                    (XmlElement)child,
                    sourceNamespaces,
                    renderedNamespaces,
                    true);
                foundElement = true;
            }
            else if (child.NodeType == XmlNodeType.ProcessingInstruction)
            {
                WriteDocumentProcessingInstruction(
                    (XmlProcessingInstruction)child,
                    foundElement);
            }
            else if (child.NodeType == XmlNodeType.Comment
                && mode.IncludeComments)
            {
                WriteDocumentComment((XmlComment)child, foundElement);
            }
        }
        return output.GetUtf8Bytes();
    }

    internal byte[] WriteElementSubtree(XmlElement element)
    {
        int elementDepth = GetElementDepth(element);
        ValidateAncestorContext(element);
        ValidateNodeTree(element, elementDepth);
        Dictionary<string, string> sourceNamespaces = CreateEmptyNamespaces();
        AddAncestorNamespaces(element, sourceNamespaces);
        Dictionary<string, string> renderedNamespaces = CreateRenderedNamespaces();
        WriteElementNode(element, sourceNamespaces, renderedNamespaces, true);
        return output.GetUtf8Bytes();
    }

    void ValidateNodeTree(XmlNode node, int depth)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            ValidateElementNode((XmlElement)node, depth);
        }
        string value = node.Value ?? "";
        if (value.Length > limits.MaximumXmlTextLength)
        {
            throw new OdfSecurityException("Canonical XML exceeds the configured text-value limit.");
        }
        foreach (XmlNode child in node.ChildNodes)
        {
            int childDepth = depth;
            if (child.NodeType == XmlNodeType.Element)
            {
                childDepth = depth + 1;
            }
            ValidateNodeTree(child, childDepth);
        }
    }

    int GetElementDepth(XmlElement element)
    {
        int result = 1;
        XmlNode? current = element.ParentNode;
        while (current != null && current.NodeType == XmlNodeType.Element)
        {
            result = result + 1;
            current = current.ParentNode;
        }
        return result;
    }

    void ValidateAncestorContext(XmlElement element)
    {
        int depth = GetElementDepth(element) - 1;
        XmlNode? current = element.ParentNode;
        while (current != null && current.NodeType == XmlNodeType.Element)
        {
            ValidateElementNode((XmlElement)current, depth);
            depth = depth - 1;
            current = current.ParentNode;
        }
    }

    void ValidateElementNode(XmlElement element, int depth)
    {
        if (depth > limits.MaximumXmlDepth)
        {
            throw new OdfSecurityException("Canonical XML exceeds the configured element-depth limit.");
        }
        if (element.Attributes.Count > limits.MaximumXmlAttributesPerElement)
        {
            throw new OdfSecurityException("Canonical XML exceeds the configured per-element attribute limit.");
        }
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (attribute.Value.Length > limits.MaximumXmlTextLength)
            {
                throw new OdfSecurityException("Canonical XML exceeds the configured text-value limit.");
            }
            if (XmlCanonicalizer.IsNamespaceAttribute(attribute)
                && attribute.Value.Length > 0
                && !OdfIriResolver.IsAbsoluteIri(attribute.Value))
            {
                throw new OdfSecurityException("Canonical XML cannot contain a relative namespace IRI.");
            }
        }
    }

    Dictionary<string, string> CreateEmptyNamespaces()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    Dictionary<string, string> CreateRenderedNamespaces()
    {
        Dictionary<string, string> result = new Dictionary<string, string>(
            StringComparer.Ordinal);
        result.Add("", "");
        return result;
    }

    void AddAncestorNamespaces(
        XmlElement element,
        Dictionary<string, string> namespaces)
    {
        List<XmlElement> ancestors = new List<XmlElement>();
        XmlNode? current = element.ParentNode;
        while (current != null && current.NodeType == XmlNodeType.Element)
        {
            ancestors.Add((XmlElement)current);
            current = current.ParentNode;
        }
        int index = ancestors.Count - 1;
        while (index >= 0)
        {
            ApplyLocalNamespaces(ancestors[index], namespaces);
            index = index - 1;
        }
    }

    void ApplyLocalNamespaces(
        XmlElement element,
        Dictionary<string, string> namespaces)
    {
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (XmlCanonicalizer.IsNamespaceAttribute(attribute))
            {
                string prefix = XmlCanonicalizer.GetNamespacePrefix(attribute);
                namespaces[prefix] = attribute.Value;
            }
        }
        if (element.Prefix.Length > 0)
        {
            namespaces[element.Prefix] = element.NamespaceURI;
        }
        else if (element.NamespaceURI.Length > 0)
        {
            namespaces[""] = element.NamespaceURI;
        }
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (!XmlCanonicalizer.IsNamespaceAttribute(attribute)
                && attribute.Prefix.Length > 0
                && attribute.Prefix != "xml")
            {
                namespaces[attribute.Prefix] = attribute.NamespaceURI;
            }
        }
    }

    void WriteElementNode(
        XmlElement element,
        Dictionary<string, string> inheritedNamespaces,
        Dictionary<string, string> renderedNamespaces,
        bool apex)
    {
        Dictionary<string, string> sourceNamespaces = new Dictionary<string, string>(
            inheritedNamespaces,
            StringComparer.Ordinal);
        ApplyLocalNamespaces(element, sourceNamespaces);
        Dictionary<string, string> currentRendered = new Dictionary<string, string>(
            renderedNamespaces,
            StringComparer.Ordinal);

        output.Write("<");
        output.Write(element.Name);
        WriteNamespaceDeclarations(element, sourceNamespaces, currentRendered);
        WriteSortedAttributes(element, apex);
        output.Write(">");

        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                WriteElementNode(
                    (XmlElement)child,
                    sourceNamespaces,
                    currentRendered,
                    false);
            }
            else if (child.NodeType == XmlNodeType.Text
                || child.NodeType == XmlNodeType.CDATA
                || child.NodeType == XmlNodeType.Whitespace
                || child.NodeType == XmlNodeType.SignificantWhitespace)
            {
                WriteEscapedText(child.Value ?? "");
            }
            else if (child.NodeType == XmlNodeType.ProcessingInstruction)
            {
                WriteProcessingInstruction((XmlProcessingInstruction)child);
            }
            else if (child.NodeType == XmlNodeType.Comment
                && mode.IncludeComments)
            {
                WriteComment((XmlComment)child);
            }
        }
        output.Write("</");
        output.Write(element.Name);
        output.Write(">");
    }

    void WriteNamespaceDeclarations(
        XmlElement element,
        Dictionary<string, string> sourceNamespaces,
        Dictionary<string, string> renderedNamespaces)
    {
        List<string> candidates = new List<string>();
        if (mode.Exclusive)
        {
            AddExclusiveNamespaceCandidates(element, sourceNamespaces, candidates);
        }
        else
        {
            foreach (string prefix in sourceNamespaces.Keys)
            {
                candidates.Add(prefix);
            }
        }
        SortStrings(candidates);
        string previous = "\0";
        foreach (string prefix in candidates)
        {
            if (prefix == previous || prefix == "xml")
            {
                continue;
            }
            previous = prefix;
            if (!sourceNamespaces.ContainsKey(prefix))
            {
                continue;
            }
            string namespaceName = sourceNamespaces[prefix];
            bool alreadyRendered = renderedNamespaces.ContainsKey(prefix)
                && renderedNamespaces[prefix] == namespaceName;
            if (alreadyRendered)
            {
                continue;
            }
            output.Write(" xmlns");
            if (prefix.Length > 0)
            {
                output.Write(":");
                output.Write(prefix);
            }
            output.Write("=\"");
            WriteEscapedAttribute(namespaceName);
            output.Write("\"");
            renderedNamespaces[prefix] = namespaceName;
        }
    }

    void AddExclusiveNamespaceCandidates(
        XmlElement element,
        Dictionary<string, string> sourceNamespaces,
        List<string> candidates)
    {
        candidates.Add(element.Prefix);
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (!XmlCanonicalizer.IsNamespaceAttribute(attribute)
                && attribute.Prefix.Length > 0
                && attribute.Prefix != "xml")
            {
                candidates.Add(attribute.Prefix);
            }
        }
        foreach (string prefix in inclusivePrefixes)
        {
            if (sourceNamespaces.ContainsKey(prefix))
            {
                candidates.Add(prefix);
            }
        }
    }

    void WriteSortedAttributes(XmlElement element, bool apex)
    {
        List<CanonicalXmlAttribute> attributes = CollectAttributes(element, apex);
        SortAttributes(attributes);
        foreach (CanonicalXmlAttribute attribute in attributes)
        {
            output.Write(" ");
            output.Write(attribute.QualifiedName);
            output.Write("=\"");
            WriteEscapedAttribute(attribute.Value);
            output.Write("\"");
        }
    }

    List<CanonicalXmlAttribute> CollectAttributes(XmlElement element, bool apex)
    {
        List<CanonicalXmlAttribute> result = new List<CanonicalXmlAttribute>();
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (!XmlCanonicalizer.IsNamespaceAttribute(attribute))
            {
                result.Add(CreateAttribute(attribute));
            }
        }
        if (!apex || mode.Exclusive)
        {
            return result;
        }
        if (mode.Version11)
        {
            AddVersion11InheritedAttributes(element, result);
        }
        else
        {
            AddVersion10InheritedAttributes(element, result);
        }
        return result;
    }

    CanonicalXmlAttribute CreateAttribute(XmlAttribute attribute)
    {
        CanonicalXmlAttribute result = new CanonicalXmlAttribute();
        result.NamespaceName = attribute.NamespaceURI;
        result.LocalName = attribute.LocalName;
        result.QualifiedName = attribute.Name;
        result.Value = attribute.Value;
        return result;
    }

    void AddVersion10InheritedAttributes(
        XmlElement element,
        List<CanonicalXmlAttribute> attributes)
    {
        XmlNode? current = element.ParentNode;
        while (current != null && current.NodeType == XmlNodeType.Element)
        {
            XmlElement ancestor = (XmlElement)current;
            foreach (XmlAttribute attribute in ancestor.Attributes)
            {
                if (XmlCanonicalizer.IsXmlAttribute(attribute)
                    && !ContainsAttribute(attributes, attribute.LocalName))
                {
                    attributes.Add(CreateAttribute(attribute));
                }
            }
            current = current.ParentNode;
        }
    }

    void AddVersion11InheritedAttributes(
        XmlElement element,
        List<CanonicalXmlAttribute> attributes)
    {
        XmlNode? current = element.ParentNode;
        while (current != null && current.NodeType == XmlNodeType.Element)
        {
            XmlElement ancestor = (XmlElement)current;
            foreach (XmlAttribute attribute in ancestor.Attributes)
            {
                if (XmlCanonicalizer.IsXmlAttribute(attribute)
                    && (attribute.LocalName == "lang" || attribute.LocalName == "space")
                    && !ContainsAttribute(attributes, attribute.LocalName))
                {
                    attributes.Add(CreateAttribute(attribute));
                }
            }
            current = current.ParentNode;
        }
        FixVersion11Base(element, attributes);
    }

    void FixVersion11Base(
        XmlElement element,
        List<CanonicalXmlAttribute> attributes)
    {
        List<XmlElement> ancestors = new List<XmlElement>();
        XmlNode? current = element.ParentNode;
        bool ancestorHasBase = false;
        while (current != null && current.NodeType == XmlNodeType.Element)
        {
            XmlElement ancestor = (XmlElement)current;
            ancestors.Add(ancestor);
            if (ancestor.HasAttribute("base", XmlNamespace))
            {
                ancestorHasBase = true;
            }
            current = current.ParentNode;
        }
        if (!ancestorHasBase)
        {
            return;
        }

        string combined = "";
        bool hasCombined = false;
        int index = ancestors.Count - 1;
        while (index >= 0)
        {
            XmlElement ancestor = ancestors[index];
            if (ancestor.HasAttribute("base", XmlNamespace))
            {
                string value = ancestor.GetAttribute("base", XmlNamespace);
                combined = hasCombined ? JoinUriReferences(combined, value) : value;
                hasCombined = true;
            }
            index = index - 1;
        }
        if (element.HasAttribute("base", XmlNamespace))
        {
            string value = element.GetAttribute("base", XmlNamespace);
            combined = hasCombined ? JoinUriReferences(combined, value) : value;
            hasCombined = true;
        }
        RemoveAttribute(attributes, "base");
        if (hasCombined && combined.Length > 0)
        {
            CanonicalXmlAttribute fixedBase = new CanonicalXmlAttribute();
            fixedBase.NamespaceName = XmlNamespace;
            fixedBase.LocalName = "base";
            fixedBase.QualifiedName = "xml:base";
            fixedBase.Value = combined;
            attributes.Add(fixedBase);
        }
    }

    bool ContainsAttribute(
        List<CanonicalXmlAttribute> attributes,
        string localName)
    {
        foreach (CanonicalXmlAttribute attribute in attributes)
        {
            if (attribute.NamespaceName == XmlNamespace
                && attribute.LocalName == localName)
            {
                return true;
            }
        }
        return false;
    }

    void RemoveAttribute(
        List<CanonicalXmlAttribute> attributes,
        string localName)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            CanonicalXmlAttribute attribute = attributes[index];
            if (attribute.NamespaceName == XmlNamespace
                && attribute.LocalName == localName)
            {
                attributes.RemoveAt(index);
            }
            else
            {
                index = index + 1;
            }
        }
    }

    string JoinUriReferences(string baseValue, string referenceValue)
    {
        string reference = RemoveFragment(referenceValue);
        if (HasUriScheme(reference))
        {
            return NormalizeUriPath(reference);
        }
        string baseReference = RemoveFragment(baseValue);
        int queryIndex = reference.IndexOf('?');
        string query = queryIndex >= 0 ? reference.Substring(queryIndex) : "";
        string referencePath = queryIndex >= 0
            ? reference.Substring(0, queryIndex)
            : reference;
        if (referencePath.Length == 0)
        {
            if (query.Length > 0)
            {
                return RemoveQuery(baseReference) + query;
            }
            return baseReference;
        }

        string baseWithoutQuery = RemoveQuery(baseReference);
        int basePathStart = FindUriPathStart(baseWithoutQuery);
        string prefix = baseWithoutQuery.Substring(0, basePathStart);
        if (referencePath.StartsWith("/", StringComparison.Ordinal))
        {
            return prefix + NormalizePath(referencePath) + query;
        }
        string basePath = baseWithoutQuery.Substring(basePathStart);
        int separator = basePath.LastIndexOf('/');
        string directory = "";
        if (basePath == ".." || basePath.EndsWith("/..", StringComparison.Ordinal))
        {
            directory = basePath + "/";
        }
        else if (separator >= 0)
        {
            directory = basePath.Substring(0, separator + 1);
        }
        return prefix + NormalizePath(directory + referencePath) + query;
    }

    string RemoveFragment(string value)
    {
        int fragment = value.IndexOf('#');
        return fragment >= 0 ? value.Substring(0, fragment) : value;
    }

    string RemoveQuery(string value)
    {
        int query = value.IndexOf('?');
        return query >= 0 ? value.Substring(0, query) : value;
    }

    bool HasUriScheme(string value)
    {
        int colon = value.IndexOf(':');
        int slash = value.IndexOf('/');
        int query = value.IndexOf('?');
        int end = value.Length;
        if (slash >= 0 && slash < end)
        {
            end = slash;
        }
        if (query >= 0 && query < end)
        {
            end = query;
        }
        return colon > 0 && colon < end;
    }

    string NormalizeUriPath(string value)
    {
        int query = value.IndexOf('?');
        string suffix = query >= 0 ? value.Substring(query) : "";
        string withoutQuery = query >= 0 ? value.Substring(0, query) : value;
        int pathStart = FindUriPathStart(withoutQuery);
        string prefix = withoutQuery.Substring(0, pathStart);
        string path = withoutQuery.Substring(pathStart);
        return prefix + NormalizePath(path) + suffix;
    }

    int FindUriPathStart(string value)
    {
        int scheme = value.IndexOf(':');
        int authorityStart = -1;
        if (scheme >= 0
            && scheme + 2 < value.Length
            && value[scheme + 1] == '/'
            && value[scheme + 2] == '/')
        {
            authorityStart = scheme + 3;
        }
        else if (value.StartsWith("//", StringComparison.Ordinal))
        {
            authorityStart = 2;
        }
        if (authorityStart < 0)
        {
            return 0;
        }
        int slash = value.IndexOf('/', authorityStart);
        return slash >= 0 ? slash : value.Length;
    }

    string NormalizePath(string path)
    {
        bool absolute = path.StartsWith("/", StringComparison.Ordinal);
        bool trailingSlash = path.EndsWith("/", StringComparison.Ordinal)
            || path.EndsWith("/.", StringComparison.Ordinal)
            || path.EndsWith("/..", StringComparison.Ordinal);
        string[] segments = path.Split('/');
        List<string> normalized = new List<string>();
        foreach (string segment in segments)
        {
            if (segment.Length == 0 || segment == ".")
            {
                continue;
            }
            if (segment == "..")
            {
                if (normalized.Count > 0
                    && normalized[normalized.Count - 1] != "..")
                {
                    normalized.RemoveAt(normalized.Count - 1);
                }
                else if (!absolute)
                {
                    normalized.Add(segment);
                }
            }
            else
            {
                normalized.Add(segment);
            }
        }
        string result = string.Join("/", normalized);
        if (absolute)
        {
            result = "/" + result;
        }
        if (trailingSlash && result.Length > 0
            && !result.EndsWith("/", StringComparison.Ordinal))
        {
            result = result + "/";
        }
        return result;
    }

    void SortStrings(List<string> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            string current = values[index];
            int destination = index;
            while (destination > 0
                && string.CompareOrdinal(current, values[destination - 1]) < 0)
            {
                values[destination] = values[destination - 1];
                destination = destination - 1;
            }
            values[destination] = current;
            index = index + 1;
        }
    }

    void SortAttributes(List<CanonicalXmlAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            CanonicalXmlAttribute current = attributes[index];
            int destination = index;
            while (destination > 0
                && CompareAttributes(current, attributes[destination - 1]) < 0)
            {
                attributes[destination] = attributes[destination - 1];
                destination = destination - 1;
            }
            attributes[destination] = current;
            index = index + 1;
        }
    }

    int CompareAttributes(
        CanonicalXmlAttribute first,
        CanonicalXmlAttribute second)
    {
        int namespaceComparison = string.CompareOrdinal(
            first.NamespaceName,
            second.NamespaceName);
        if (namespaceComparison != 0)
        {
            return namespaceComparison;
        }
        return string.CompareOrdinal(first.LocalName, second.LocalName);
    }

    void WriteEscapedAttribute(string value)
    {
        int start = 0;
        int index = 0;
        while (index < value.Length)
        {
            string replacement = "";
            char character = value[index];
            if (character == '&')
            {
                replacement = "&amp;";
            }
            else if (character == '<')
            {
                replacement = "&lt;";
            }
            else if (character == '"')
            {
                replacement = "&quot;";
            }
            else if (character == '\t')
            {
                replacement = "&#x9;";
            }
            else if (character == '\n')
            {
                replacement = "&#xA;";
            }
            else if (character == '\r')
            {
                replacement = "&#xD;";
            }
            if (replacement.Length > 0)
            {
                if (index > start)
                {
                    output.Write(value.Substring(start, index - start));
                }
                output.Write(replacement);
                start = index + 1;
            }
            index = index + 1;
        }
        if (start < value.Length)
        {
            output.Write(value.Substring(start));
        }
    }

    void WriteEscapedText(string value)
    {
        int start = 0;
        int index = 0;
        while (index < value.Length)
        {
            string replacement = "";
            char character = value[index];
            if (character == '&')
            {
                replacement = "&amp;";
            }
            else if (character == '<')
            {
                replacement = "&lt;";
            }
            else if (character == '>')
            {
                replacement = "&gt;";
            }
            else if (character == '\r')
            {
                replacement = "&#xD;";
            }
            if (replacement.Length > 0)
            {
                if (index > start)
                {
                    output.Write(value.Substring(start, index - start));
                }
                output.Write(replacement);
                start = index + 1;
            }
            index = index + 1;
        }
        if (start < value.Length)
        {
            output.Write(value.Substring(start));
        }
    }

    void WriteProcessingInstruction(XmlProcessingInstruction instruction)
    {
        output.Write("<?");
        output.Write(instruction.Target);
        if (instruction.Data.Length > 0)
        {
            output.Write(" ");
            WriteCarriageReturns(instruction.Data);
        }
        output.Write("?>");
    }

    void WriteComment(XmlComment comment)
    {
        output.Write("<!--");
        WriteCarriageReturns(comment.Value ?? "");
        output.Write("-->");
    }

    void WriteDocumentProcessingInstruction(
        XmlProcessingInstruction instruction,
        bool afterDocumentElement)
    {
        if (afterDocumentElement)
        {
            output.Write("\n");
        }
        WriteProcessingInstruction(instruction);
        if (!afterDocumentElement)
        {
            output.Write("\n");
        }
    }

    void WriteDocumentComment(XmlComment comment, bool afterDocumentElement)
    {
        if (afterDocumentElement)
        {
            output.Write("\n");
        }
        WriteComment(comment);
        if (!afterDocumentElement)
        {
            output.Write("\n");
        }
    }

    void WriteCarriageReturns(string value)
    {
        int start = 0;
        int index = value.IndexOf('\r');
        while (index >= 0)
        {
            if (index > start)
            {
                output.Write(value.Substring(start, index - start));
            }
            output.Write("&#xD;");
            start = index + 1;
            index = value.IndexOf('\r', start);
        }
        if (start < value.Length)
        {
            output.Write(value.Substring(start));
        }
    }
}
