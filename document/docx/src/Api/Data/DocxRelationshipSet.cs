using System.Text;
using System.Xml;

namespace docx;

/// <summary>
/// Stores the relationships originating from the package root or one package part.
/// </summary>
public class DocxRelationshipSet
{
    /// <summary>
    /// The absolute package part name for the source.
    /// </summary>
    public string SourcePartName;
    internal List<DocxRelationship> Relationships;

    /// <summary>
    /// Creates a relationship set instance.
    /// </summary>
    /// <param name="sourcePartName">The package part from which the relationship originates.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxRelationshipSet(string sourcePartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        this.SourcePartName = sourcePartName;
        this.Relationships = new List<DocxRelationship>();
    }

    internal static DocxRelationshipSet ParsePackageRelationships(string xml)
    {
        return Parse("/", xml);
    }

    internal static DocxRelationshipSet ParsePackageRelationships(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
        {
            return ParsePackageRelationships(reader.ReadToEnd());
        }
    }

    internal static DocxRelationshipSet ParsePartRelationships(string sourcePartName, string xml)
    {
        string normalizedSourcePartName = DocxPartName.Normalize(sourcePartName);
        return Parse(normalizedSourcePartName, xml);
    }

    internal static DocxRelationshipSet ParsePartRelationships(string sourcePartName, Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
        {
            return ParsePartRelationships(sourcePartName, reader.ReadToEnd());
        }
    }

    internal static DocxRelationshipSet ParsePartRelationshipsFromZip(Stream packageStream, string sourcePartName)
    {
        if (packageStream == null)
        {
            throw new ArgumentNullException("packageStream");
        }

        string relationshipPartName = GetRelationshipPartName(sourcePartName);
        DocxZipReader reader = new DocxZipReader();
        string xml = reader.ReadXmlEntry(packageStream, DocxPartName.ToZipEntryName(relationshipPartName));
        return ParsePartRelationships(sourcePartName, xml);
    }

    internal static DocxRelationshipSet Parse(string sourcePartName, string xml)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxRelationshipSet relationshipSet = new DocxRelationshipSet(sourcePartName);

        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element
                        && reader.LocalName == "Relationship"
                        && reader.NamespaceURI == DocxConstants.NamespacePackageRelationships)
                    {
                        DocxRelationship relationship = ReadRelationship(reader);
                        relationshipSet.AddRelationship(relationship);
                    }
                }
            }
        }

        return relationshipSet;
    }

    internal static string GetRelationshipPartName(string sourcePartName)
    {
        string normalizedSourcePartName = DocxPartName.Normalize(sourcePartName);
        int slashIndex = normalizedSourcePartName.LastIndexOf('/');
        string sourceDirectory = normalizedSourcePartName.Substring(0, slashIndex + 1);
        string sourceFileName = normalizedSourcePartName.Substring(slashIndex + 1);

        return sourceDirectory + "_rels/" + sourceFileName + ".rels";
    }

    internal static string GetSourcePartNameFromRelationshipPartName(string relationshipPartName)
    {
        string normalizedRelationshipPartName = DocxPartName.Normalize(relationshipPartName);

        if (normalizedRelationshipPartName == "/_rels/.rels")
        {
            return "/";
        }

        if (!normalizedRelationshipPartName.EndsWith(".rels"))
        {
            throw new ArgumentException("Relationship part name must end with '.rels'.", "relationshipPartName");
        }

        int relsIndex = normalizedRelationshipPartName.LastIndexOf("/_rels/");

        if (relsIndex < 0)
        {
            throw new ArgumentException("Relationship part name must contain an '_rels' segment.", "relationshipPartName");
        }

        string sourceDirectory = normalizedRelationshipPartName.Substring(0, relsIndex + 1);
        string relationshipFileName = normalizedRelationshipPartName.Substring(relsIndex + 7);
        string sourceFileName = relationshipFileName.Substring(0, relationshipFileName.Length - 5);

        if (sourceFileName == "")
        {
            throw new ArgumentException("Relationship part name is missing a source file name.", "relationshipPartName");
        }

        return sourceDirectory + sourceFileName;
    }

    internal static string ResolveTargetPartName(string sourcePartName, string target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (target == "")
        {
            throw new ArgumentException("Relationship target is required.", "target");
        }

        string sourceBaseDirectory = GetSourceBaseDirectory(sourcePartName);
        string combinedTarget = target;

        if (!combinedTarget.StartsWith("/"))
        {
            combinedTarget = sourceBaseDirectory + combinedTarget;
        }

        string resolvedTarget = ResolvePathSegments(combinedTarget);
        return DocxPartName.Normalize(resolvedTarget);
    }

    internal static string GetSourceBaseDirectory(string sourcePartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (sourcePartName == "/")
        {
            return "/";
        }

        string normalizedSourcePartName = DocxPartName.Normalize(sourcePartName);
        int slashIndex = normalizedSourcePartName.LastIndexOf('/');
        return normalizedSourcePartName.Substring(0, slashIndex + 1);
    }

    internal static string ResolvePathSegments(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        string normalizedPath = path.Replace('\\', '/');
        List<string> segments = new List<string>();
        int segmentStart = 0;

        if (normalizedPath.StartsWith("/"))
        {
            segmentStart = 1;
        }

        for (int index = segmentStart; index <= normalizedPath.Length; index = index + 1)
        {
            if (index == normalizedPath.Length || normalizedPath[index] == '/')
            {
                int segmentLength = index - segmentStart;
                string segment = normalizedPath.Substring(segmentStart, segmentLength);

                if (segment == "" || segment == ".")
                {
                }
                else if (segment == "..")
                {
                    if (segments.Count == 0)
                    {
                        throw new ArgumentException("Relationship target cannot resolve above the package root.", "path");
                    }

                    segments.RemoveAt(segments.Count - 1);
                }
                else
                {
                    segments.Add(segment);
                }

                segmentStart = index + 1;
            }
        }

        StringBuilder builder = new StringBuilder();

        foreach (string segment in segments)
        {
            builder.Append("/");
            builder.Append(segment);
        }

        if (builder.Length == 0)
        {
            return "/";
        }

        return builder.ToString();
    }

    internal static DocxRelationship ReadRelationship(XmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string? id = reader.GetAttribute("Id");
        string? type = reader.GetAttribute("Type");
        string? target = reader.GetAttribute("Target");
        string? targetMode = reader.GetAttribute("TargetMode");

        if (id == null)
        {
            throw new InvalidDataException("Relationship is missing Id.");
        }

        if (type == null)
        {
            throw new InvalidDataException("Relationship is missing Type.");
        }

        if (target == null)
        {
            throw new InvalidDataException("Relationship is missing Target.");
        }

        if (targetMode == null)
        {
            targetMode = "";
        }

        DocxRelationship relationship = new DocxRelationship(id, type, target, targetMode);
        ReadUnknownRelationshipAttributes(reader, relationship);
        return relationship;
    }

    internal static void ReadUnknownRelationshipAttributes(XmlReader reader, DocxRelationship relationship)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        if (!reader.HasAttributes)
        {
            return;
        }

        for (int index = 0; index < reader.AttributeCount; index = index + 1)
        {
            reader.MoveToAttribute(index);

            if (!IsKnownRelationshipAttribute(reader.LocalName, reader.NamespaceURI))
            {
                relationship.AddUnknownAttribute(new DocxRelationshipAttribute(
                    reader.Prefix,
                    reader.LocalName,
                    reader.NamespaceURI,
                    reader.Value));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsKnownRelationshipAttribute(string localName, string namespaceUri)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (namespaceUri != "")
        {
            return false;
        }

        return localName == "Id"
            || localName == "Type"
            || localName == "Target"
            || localName == "TargetMode";
    }

    internal void AddRelationship(DocxRelationship relationship)
    {
        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        if (ContainsRelationshipId(relationship.Id))
        {
            throw new InvalidOperationException("Relationship id already exists.");
        }

        this.Relationships.Add(relationship);
    }

    internal DocxRelationshipSet CreateDiagnosticCopy()
    {
        DocxRelationshipSet copy = new DocxRelationshipSet(this.SourcePartName);

        foreach (DocxRelationship relationship in this.Relationships)
        {
            copy.Relationships.Add(relationship.CreateDiagnosticCopy());
        }

        return copy;
    }

    internal DocxRelationship CreateRelationship(string type, string target, bool external)
    {
        string id = AllocateRelationshipId();
        return CreateRelationshipWithId(id, type, target, external);
    }

    internal DocxRelationship CreateRelationshipWithId(string id, string type, string target, bool external)
    {
        DocxRelationship relationship = new DocxRelationship(id, type, target, GetTargetMode(external));
        AddRelationship(relationship);
        return relationship;
    }

    internal DocxRelationship? FindById(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        foreach (DocxRelationship relationship in this.Relationships)
        {
            if (relationship.Id == id)
            {
                return relationship;
            }
        }

        return null;
    }

    internal bool ContainsRelationshipId(string id)
    {
        return FindById(id) != null;
    }

    internal string AllocateRelationshipId()
    {
        DocxIdAllocator allocator = new DocxIdAllocator("rId");
        List<string> existingIds = new List<string>();

        foreach (DocxRelationship relationship in this.Relationships)
        {
            existingIds.Add(relationship.Id);
        }

        return allocator.AllocateNext(existingIds);
    }

    internal bool UpdateRelationship(string id, string type, string target, bool external)
    {
        DocxRelationship? relationship = FindById(id);

        if (relationship == null)
        {
            return false;
        }

        relationship.Update(type, target, GetTargetMode(external));
        return true;
    }

    internal bool RemoveRelationship(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        for (int index = 0; index < this.Relationships.Count; index = index + 1)
        {
            DocxRelationship relationship = this.Relationships[index];

            if (relationship.Id == id)
            {
                this.Relationships.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    internal string GetTargetMode(bool external)
    {
        if (external)
        {
            return "External";
        }

        return "";
    }

    internal string WriteXml()
    {
        StringWriter stringWriter = new StringWriter();

        using (XmlWriter writer = XmlWriter.Create(stringWriter))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("Relationships", DocxConstants.NamespacePackageRelationships);

            foreach (DocxRelationship relationship in this.Relationships)
            {
                WriteRelationship(writer, relationship);
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return stringWriter.ToString();
    }

    internal void WriteRelationship(XmlWriter writer, DocxRelationship relationship)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        writer.WriteStartElement("Relationship", DocxConstants.NamespacePackageRelationships);
        writer.WriteAttributeString("Id", relationship.Id);
        writer.WriteAttributeString("Type", relationship.Type);
        writer.WriteAttributeString("Target", relationship.Target);

        if (relationship.TargetMode != "")
        {
            writer.WriteAttributeString("TargetMode", relationship.TargetMode);
        }

        foreach (DocxRelationshipAttribute attribute in relationship.UnknownAttributes)
        {
            writer.WriteAttributeString(attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, attribute.Value);
        }

        writer.WriteEndElement();
    }

    internal string ResolveTargetPartName(DocxRelationship relationship)
    {
        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        return relationship.ResolveTargetPartName(this.SourcePartName);
    }

    internal string ResolveTargetPartNameById(string relationshipId)
    {
        DocxRelationship? relationship = FindById(relationshipId);

        if (relationship == null)
        {
            return "";
        }

        return ResolveTargetPartName(relationship);
    }

    internal int Count()
    {
        return this.Relationships.Count;
    }
}
