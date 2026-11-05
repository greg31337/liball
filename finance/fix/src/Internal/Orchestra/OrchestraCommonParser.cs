using System;
using System.Collections.Generic;
using System.Xml;

namespace fix;

internal static class OrchestraCommonParser
{
    internal const string DublinCoreTermsNamespace = "http://purl.org/dc/terms/";
    internal const string DublinCoreElementsNamespace = "http://purl.org/dc/elements/1.1/";
    internal const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    internal const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    internal static OrchestraRepository ParseDocument(OrchestraXmlLoadResult source)
    {
        OrchestraRepository repository = new OrchestraRepository();
        repository.Version = source.Version;
        XmlElement? root = source.Root;
        if (root == null) return repository;
        ParseMetadata(root, repository.Metadata);
        ParseAnnotations(root, repository.Annotations, root.NamespaceURI);
        ParseCategories(root, repository);
        ParseSections(root, repository);
        ParseConcepts(root, repository);
        AddForeignExtensions(root, root.NamespaceURI, repository.Extensions);
        return repository;
    }

    internal static void ParseMetadata(XmlElement root, OrchestraMetadata metadata)
    {
        metadata.Name = root.GetAttribute("name").Trim();
        metadata.Release = root.GetAttribute("version").Trim();
        metadata.FixVersion = metadata.Release;
        metadata.NamespaceUri = root.NamespaceURI;
        metadata.RepositoryNamespace = root.GetAttribute("namespace").Trim();
        metadata.SpecificationUri = root.GetAttribute("specUrl").Trim();
        int marker = metadata.Release.LastIndexOf("_EP", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0)
        {
            metadata.FixVersion = metadata.Release.Substring(0, marker);
            string extensionPack = metadata.Release.Substring(marker + 3);
            int parsedExtensionPack = ParseNonnegativeInteger(extensionPack);
            if (parsedExtensionPack >= 0)
            {
                metadata.HasExtensionPack = true;
                metadata.ExtensionPack = parsedExtensionPack;
            }
        }
        XmlElement? container = FindDirectChild(root, "metadata", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null)
            {
                OrchestraMetadataTerm term = new OrchestraMetadataTerm();
                term.NamespaceUri = element.NamespaceURI;
                term.Name = element.LocalName;
                term.Language = element.GetAttribute("lang", XmlNamespace);
                term.Value = element.InnerText;
                term.Xml = element.OuterXml;
                metadata.Terms.Add(term);
            }
        }
        if (metadata.Name.Length == 0) metadata.Name = FindMetadataValue(metadata.Terms, "title");
        metadata.PublicationDate = FindMetadataValue(metadata.Terms, "created");
        if (metadata.PublicationDate.Length == 0) metadata.PublicationDate = FindMetadataValue(metadata.Terms, "issued");
        if (metadata.PublicationDate.Length == 0) metadata.PublicationDate = FindMetadataValue(metadata.Terms, "date");
    }

    internal static string FindMetadataValue(List<OrchestraMetadataTerm> terms, string name)
    {
        for (int index = 0; index < terms.Count; index++)
        {
            OrchestraMetadataTerm term = terms[index];
            bool dublinCore = term.NamespaceUri == DublinCoreTermsNamespace || term.NamespaceUri == DublinCoreElementsNamespace;
            if (dublinCore && term.Name == name) return term.Value;
        }
        return "";
    }

    internal static void ParseCategories(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = FindDirectChild(root, "categories", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null && element.NamespaceURI == root.NamespaceURI && element.LocalName == "category")
            {
                OrchestraCategory category = new OrchestraCategory();
                category.Name = element.GetAttribute("name").Trim();
                category.Section = element.GetAttribute("section").Trim();
                category.FixmlFileName = element.GetAttribute("FIXMLFileName").Trim();
                category.ComponentType = element.GetAttribute("componentType").Trim();
                category.IncludeFile = element.GetAttribute("includeFile").Trim();
                category.Pedigree = ParsePedigree(element);
                ParseAnnotations(element, category.Annotations, root.NamespaceURI);
                AddForeignExtensions(element, root.NamespaceURI, category.Extensions);
                repository.Categories.Add(category);
            }
        }
        ParseAnnotations(container, repository.Annotations, root.NamespaceURI);
        AddForeignExtensions(container, root.NamespaceURI, repository.Extensions);
    }

    internal static void ParseSections(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = FindDirectChild(root, "sections", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null && element.NamespaceURI == root.NamespaceURI && element.LocalName == "section")
            {
                OrchestraSection section = new OrchestraSection();
                section.Name = element.GetAttribute("name").Trim();
                section.DisplayOrderText = element.GetAttribute("displayOrder").Trim();
                section.HasDisplayOrder = section.DisplayOrderText.Length != 0;
                section.DisplayOrder = ParseSignedInteger(section.DisplayOrderText);
                section.FixmlFileName = element.GetAttribute("FIXMLFileName").Trim();
                section.Pedigree = ParsePedigree(element);
                ParseAnnotations(element, section.Annotations, root.NamespaceURI);
                AddForeignExtensions(element, root.NamespaceURI, section.Extensions);
                repository.Sections.Add(section);
            }
        }
        ParseAnnotations(container, repository.Annotations, root.NamespaceURI);
        AddForeignExtensions(container, root.NamespaceURI, repository.Extensions);
    }

    internal static void ParseConcepts(XmlElement root, OrchestraRepository repository)
    {
        XmlElement? container = FindDirectChild(root, "concepts", root.NamespaceURI);
        if (container == null) return;
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null && element.NamespaceURI == root.NamespaceURI && element.LocalName == "concept")
            {
                OrchestraConcept concept = new OrchestraConcept();
                concept.Name = element.GetAttribute("name").Trim();
                ParseConceptMembers(element, concept, root.NamespaceURI);
                ParseAnnotations(element, concept.Annotations, root.NamespaceURI);
                AddForeignExtensions(element, root.NamespaceURI, concept.Extensions);
                repository.Concepts.Add(concept);
            }
        }
        ParseAnnotations(container, repository.Annotations, root.NamespaceURI);
        AddForeignExtensions(container, root.NamespaceURI, repository.Extensions);
    }

    internal static void ParseConceptMembers(XmlElement parent, OrchestraConcept concept, string namespaceUri)
    {
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? element = parent.ChildNodes[index] as XmlElement;
            if (element != null && element.NamespaceURI == namespaceUri && IsConceptMember(element.LocalName))
            {
                OrchestraConceptMember member = new OrchestraConceptMember();
                member.Kind = element.LocalName;
                member.Id = element.GetAttribute("id").Trim();
                member.Name = element.GetAttribute("name").Trim();
                member.Scenario = element.GetAttribute("scenario").Trim();
                member.MessageType = element.GetAttribute("msgType");
                member.Pedigree = ParsePedigree(element);
                ParseAnnotations(element, member.Annotations, namespaceUri);
                AddForeignExtensions(element, namespaceUri, member.Extensions);
                concept.Members.Add(member);
            }
        }
    }

    internal static bool IsConceptMember(string name)
    {
        return name == "fieldRef" || name == "componentRef" || name == "groupRef" || name == "messageRef";
    }

    internal static void ParseAnnotations(XmlElement parent, List<OrchestraAnnotation> destination, string namespaceUri)
    {
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? container = parent.ChildNodes[index] as XmlElement;
            if (container != null && container.NamespaceURI == namespaceUri && container.LocalName == "annotation")
            {
                ParseAnnotationContainer(container, destination, namespaceUri);
            }
        }
    }

    internal static void ParseAnnotationContainer(XmlElement container, List<OrchestraAnnotation> destination, string namespaceUri)
    {
        OrchestraPedigree containerPedigree = ParsePedigree(container);
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? item = container.ChildNodes[index] as XmlElement;
            if (item != null)
            {
                if (item.NamespaceURI == namespaceUri && (item.LocalName == "documentation" || item.LocalName == "appinfo"))
                {
                    OrchestraAnnotation annotation = ParseAnnotationItem(item, containerPedigree);
                    destination.Add(annotation);
                }
                else
                {
                    OrchestraAnnotation annotation = new OrchestraAnnotation();
                    annotation.Kind = "extension";
                    annotation.ContainerPedigree = containerPedigree;
                    annotation.Content = item.InnerText;
                    annotation.XmlContent = item.InnerXml;
                    annotation.Extensions.Add(CreateExtension(item));
                    destination.Add(annotation);
                }
            }
        }
    }

    internal static OrchestraAnnotation ParseAnnotationItem(XmlElement item, OrchestraPedigree containerPedigree)
    {
        OrchestraAnnotation annotation = new OrchestraAnnotation();
        annotation.Kind = item.LocalName;
        annotation.Purpose = item.GetAttribute("purpose").Trim();
        annotation.Language = item.GetAttribute("langId").Trim();
        if (annotation.Language.Length == 0) annotation.Language = item.GetAttribute("lang", XmlNamespace).Trim();
        annotation.ContentType = item.GetAttribute("contentType").Trim();
        annotation.SpecificationUri = item.GetAttribute("specUrl").Trim();
        annotation.Content = item.InnerText;
        annotation.XmlContent = item.InnerXml;
        annotation.ContainerPedigree = containerPedigree;
        annotation.Pedigree = ParsePedigree(item);
        AddAnnotationExtensions(item, annotation.Extensions);
        return annotation;
    }

    internal static OrchestraPedigree ParsePedigree(XmlElement element)
    {
        OrchestraPedigree pedigree = new OrchestraPedigree();
        pedigree.AddedVersion = element.GetAttribute("added").Trim();
        pedigree.AddedExtensionPack = element.GetAttribute("addedEP").Trim();
        pedigree.ChangeType = element.GetAttribute("changeType").Trim();
        pedigree.DeprecatedVersion = element.GetAttribute("deprecated").Trim();
        pedigree.DeprecatedExtensionPack = element.GetAttribute("deprecatedEP").Trim();
        pedigree.Issue = element.GetAttribute("issue").Trim();
        pedigree.LastModifiedVersion = element.GetAttribute("lastModified").Trim();
        pedigree.ReplacedVersion = element.GetAttribute("replaced").Trim();
        pedigree.ReplacedExtensionPack = element.GetAttribute("replacedEP").Trim();
        pedigree.ReplacedByFieldId = element.GetAttribute("replacedByField").Trim();
        pedigree.Supported = element.GetAttribute("supported").Trim();
        pedigree.UpdatedVersion = element.GetAttribute("updated").Trim();
        pedigree.UpdatedExtensionPack = element.GetAttribute("updatedEP").Trim();
        return pedigree;
    }

    internal static void AddAnnotationExtensions(XmlElement item, List<OrchestraExtension> destination)
    {
        for (int index = 0; index < item.ChildNodes.Count; index++)
        {
            XmlElement? child = item.ChildNodes[index] as XmlElement;
            if (child != null) destination.Add(CreateExtension(child));
        }
        for (int index = 0; index < item.Attributes.Count; index++)
        {
            XmlAttribute? attribute = item.Attributes[index];
            if (attribute != null && attribute.NamespaceURI.Length != 0 && attribute.NamespaceURI != XmlnsNamespace && attribute.NamespaceURI != XmlNamespace)
            {
                destination.Add(CreateExtension(attribute));
            }
        }
    }

    internal static void AddForeignExtensions(XmlElement parent, string namespaceUri, List<OrchestraExtension> destination)
    {
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? child = parent.ChildNodes[index] as XmlElement;
            if (child != null && child.NamespaceURI != namespaceUri) destination.Add(CreateExtension(child));
        }
        for (int index = 0; index < parent.Attributes.Count; index++)
        {
            XmlAttribute? attribute = parent.Attributes[index];
            if (attribute != null && attribute.NamespaceURI.Length != 0 && attribute.NamespaceURI != XmlnsNamespace && attribute.NamespaceURI != XmlNamespace)
            {
                destination.Add(CreateExtension(attribute));
            }
        }
    }

    internal static OrchestraExtension CreateExtension(XmlNode node)
    {
        OrchestraExtension extension = new OrchestraExtension();
        extension.NamespaceUri = node.NamespaceURI;
        extension.Name = node.LocalName;
        extension.Prefix = node.Prefix;
        extension.Xml = node.OuterXml;
        return extension;
    }

    internal static XmlElement? FindDirectChild(XmlElement parent, string name, string namespaceUri)
    {
        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlElement? child = parent.ChildNodes[index] as XmlElement;
            if (child != null && child.LocalName == name && child.NamespaceURI == namespaceUri) return child;
        }
        return null;
    }

    internal static int ParseNonnegativeInteger(string value)
    {
        if (value.Length == 0) return -1;
        int parsed = 0;
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character < '0' || character > '9') return -1;
            int digit = character - '0';
            if (parsed > (int.MaxValue - digit) / 10) return -1;
            parsed = parsed * 10 + digit;
        }
        return parsed;
    }

    internal static int ParseSignedInteger(string value)
    {
        if (value.Length == 0) return 0;
        bool negative = value[0] == '-';
        int start = negative ? 1 : 0;
        if (start == value.Length) return 0;
        int parsed = 0;
        for (int index = start; index < value.Length; index++)
        {
            char character = value[index];
            if (character < '0' || character > '9') return 0;
            int digit = character - '0';
            if (parsed > (int.MaxValue - digit) / 10) return 0;
            parsed = parsed * 10 + digit;
        }
        return negative ? -parsed : parsed;
    }
}
