using System;
using System.Collections.Generic;
using System.IO;

namespace odf;

internal class RdfXmlPartState
{
    internal string Path = "";
    internal OdfRdfSourceKind SourceKind;
    internal OdfNode[] Nodes = [];
    internal OdfNode? Root;
    internal OdfRdfTriple[] OriginalTriples = [];
    internal bool IsChanged;
}

internal class RdfXmlDocumentState
{
    internal RdfXmlPartState[] Parts = [];
}

internal class RdfXmlParseContext
{
    internal string PartPath = "";
    internal OdfRdfSourceKind SourceKind;
    internal int BlankIndex;
}

internal static class RdfXmlDocumentModel
{
    const int LiteralProperty = 1;
    const int ResourceProperty = 2;
    const int TypedNode = 3;
    const int PropertyAttribute = 4;
    const int ComplexProperty = 5;

    internal static void LoadRdfParts(
        OdfDocument document,
        OdfLoadOptions options)
    {
        if (document.Package == null)
        {
            return;
        }
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(document.Package);
        PackagePartCatalog catalog = index.DiscoverParts();
        List<RdfXmlPartState> parts = new List<RdfXmlPartState>();
        List<OdfRdfTriple> allTriples = new List<OdfRdfTriple>();
        int existingIndex = 0;
        while (existingIndex < document.RdfTriples.Length)
        {
            allTriples.Add(document.RdfTriples[existingIndex]);
            existingIndex = existingIndex + 1;
        }
        int partIndex = 0;
        while (partIndex < catalog.RdfParts.Length)
        {
            DiscoveredPackagePart discovered = catalog.RdfParts[partIndex];
            if (!discovered.IsEncrypted)
            {
                RdfXmlPartState part = ReadPart(
                    index, discovered.Entry.Path, options);
                parts.Add(part);
                int tripleIndex = 0;
                while (tripleIndex < part.OriginalTriples.Length)
                {
                    allTriples.Add(part.OriginalTriples[tripleIndex]);
                    tripleIndex = tripleIndex + 1;
                }
            }
            partIndex = partIndex + 1;
        }
        RdfXmlDocumentState state = new RdfXmlDocumentState();
        state.Parts = parts.ToArray();
        document.RdfXmlState = state;
        document.RdfTriples = allTriples.ToArray();
    }

    internal static void SynchronizeRdfParts(OdfDocument document)
    {
        if (document.RdfTriples == null)
        {
            throw new OdfDocumentException("Document RDF triples are required.");
        }
        bool hasPackageTriples = HasPackageTriples(document.RdfTriples);
        if (document.PackageKind != OdfPackageKind.Package)
        {
            if (hasPackageTriples)
            {
                throw new OdfDocumentException(
                    "RDF/XML triples require a package document.");
            }
            return;
        }
        RdfXmlDocumentState? state = document.RdfXmlState;
        if (state == null)
        {
            state = new RdfXmlDocumentState();
            document.RdfXmlState = state;
        }

        int partIndex = 0;
        while (partIndex < state.Parts.Length)
        {
            SynchronizeOriginalPart(document, state.Parts[partIndex]);
            partIndex = partIndex + 1;
        }
        int tripleIndex = 0;
        while (tripleIndex < document.RdfTriples.Length)
        {
            OdfRdfTriple triple = document.RdfTriples[tripleIndex];
            if (triple == null)
            {
                throw new OdfDocumentException("An RDF triple cannot be null.");
            }
            if (triple.SourceKind == OdfRdfSourceKind.RdfXml
                || triple.SourceKind == OdfRdfSourceKind.PackageMetadata)
            {
                if (!IsOriginalTriple(state, triple))
                {
                    RdfXmlPartState part = RequirePart(state, triple);
                    AddTriple(part, triple);
                }
            }
            tripleIndex = tripleIndex + 1;
        }
        RefreshOriginalTriples(document, state);
    }

    internal static void WriteRdfParts(
        OdfDocument document,
        OdfSaveOptions options)
    {
        if (document.Package == null || document.RdfXmlState == null)
        {
            return;
        }
        OdfPackageWriter writer = new OdfPackageWriter();
        RdfXmlDocumentState state = document.RdfXmlState;
        int index = 0;
        while (index < state.Parts.Length)
        {
            RdfXmlPartState part = state.Parts[index];
            if (part.IsChanged)
            {
                byte[] data = OdfTreeWriter.WriteBytes(part.Nodes, options);
                OdfEditResult edit;
                if (HasPackageEntry(document.Package, part.Path))
                {
                    edit = writer.ReplacePackageEntry(
                        document.Package, part.Path, data);
                }
                else
                {
                    edit = writer.AddPackageEntry(document.Package, part.Path,
                        data, OdfCompressionMethod.Deflated);
                }
                if (edit.Status != OdfEditStatus.Success
                    && edit.Status != OdfEditStatus.NoChange)
                {
                    throw new OdfDocumentException(
                        "Could not write RDF metadata part " + part.Path + ".");
                }
                part.IsChanged = false;
            }
            index = index + 1;
        }
    }

    internal static OdfRdfTriple[] QueryTriples(
        OdfDocument document,
        string? subject,
        string? predicate,
        string? objectValue)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for an RDF query.");
        }
        List<OdfRdfTriple> result = new List<OdfRdfTriple>();
        int index = 0;
        while (index < document.RdfTriples.Length)
        {
            OdfRdfTriple triple = document.RdfTriples[index];
            bool subjectMatch = subject == null
                || triple.Subject == subject;
            bool predicateMatch = predicate == null
                || triple.Predicate == predicate;
            bool objectMatch = objectValue == null
                || triple.Object == objectValue;
            if (subjectMatch && predicateMatch && objectMatch)
            {
                result.Add(triple);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static RdfXmlPartState ReadPart(
        ZipEntryIndex index,
        string path,
        OdfLoadOptions options)
    {
        byte[] data;
        using (Stream input = index.OpenEntry(path))
        using (MemoryStream buffer = new MemoryStream())
        {
            input.CopyTo(buffer);
            data = buffer.ToArray();
        }
        OdfNode[] nodes = OdfTreeBuilder.ReadBytes(data, path, options);
        RdfXmlPartState part = new RdfXmlPartState();
        part.Path = path;
        part.SourceKind = path == OdfPartNames.MetadataManifest
            ? OdfRdfSourceKind.PackageMetadata
            : OdfRdfSourceKind.RdfXml;
        part.Nodes = nodes;
        part.Root = FindRoot(nodes);
        List<OdfRdfTriple> triples = new List<OdfRdfTriple>();
        if (part.Root != null)
        {
            RdfXmlParseContext context = new RdfXmlParseContext();
            context.PartPath = path;
            context.SourceKind = part.SourceKind;
            if (IsRdf(part.Root, "RDF"))
            {
                int indexValue = 0;
                while (indexValue < part.Root.Children.Length)
                {
                    OdfNode child = part.Root.Children[indexValue];
                    if (child.Kind == OdfNodeKind.Element)
                    {
                        ParseSubjectNode(child, context, triples);
                    }
                    indexValue = indexValue + 1;
                }
            }
            else
            {
                ParseSubjectNode(part.Root, context, triples);
            }
        }
        part.OriginalTriples = triples.ToArray();
        return part;
    }

    static void ParseSubjectNode(
        OdfNode node,
        RdfXmlParseContext context,
        List<OdfRdfTriple> triples)
    {
        string subjectLexical = SubjectLexical(node);
        string subject = ResolveRdfTerm(
            context.PartPath, subjectLexical, false);
        if (subjectLexical.Length == 0 && !HasSubjectIdentifier(node))
        {
            context.BlankIndex = context.BlankIndex + 1;
            subject = GeneratedBlank(context.PartPath, context.BlankIndex);
        }
        if (!IsRdf(node, "Description") && node.Name != null)
        {
            OdfRdfTriple type = CreateTriple(
                context, node, node, subject, subjectLexical);
            type.Predicate = OdfNamespaces.Rdf + "type";
            type.PredicateLexical = "rdf:type";
            type.Object = node.Name.NamespaceName + node.Name.LocalName;
            type.ObjectLexical = type.Object;
            type.IsLiteral = false;
            type.IsValid = true;
            type.RdfXmlRepresentation = TypedNode;
            RememberRdfTriple(type);
            triples.Add(type);
        }
        ParsePropertyAttributes(
            node, context, subject, subjectLexical, triples);
        int childIndex = 0;
        int listIndex = 1;
        while (childIndex < node.Children.Length)
        {
            OdfNode property = node.Children[childIndex];
            if (property.Kind == OdfNodeKind.Element)
            {
                ParsePropertyNode(property, context, subject,
                    subjectLexical, listIndex, triples);
                if (IsRdf(property, "li"))
                {
                    listIndex = listIndex + 1;
                }
            }
            childIndex = childIndex + 1;
        }
    }

    static void ParsePropertyAttributes(
        OdfNode node,
        RdfXmlParseContext context,
        string subject,
        string subjectLexical,
        List<OdfRdfTriple> triples)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName.Length > 0
                && attribute.Name.NamespaceName != OdfNamespaces.Rdf
                && attribute.Name.NamespaceName != OdfNamespaces.Xml)
            {
                OdfRdfTriple triple = CreateTriple(
                    context, node, node, subject, subjectLexical);
                triple.Predicate = attribute.Name.NamespaceName
                    + attribute.Name.LocalName;
                triple.PredicateLexical = attribute.Name.Prefix + ":"
                    + attribute.Name.LocalName;
                triple.Object = attribute.Value;
                triple.ObjectLexical = attribute.Value;
                triple.Language = FindLanguage(node);
                triple.IsLiteral = true;
                triple.IsValid = true;
                triple.RdfXmlRepresentation = PropertyAttribute;
                triple.RdfXmlAttributeName = CopyName(attribute.Name);
                RememberRdfTriple(triple);
                triples.Add(triple);
            }
            index = index + 1;
        }
    }

    static void ParsePropertyNode(
        OdfNode property,
        RdfXmlParseContext context,
        string subject,
        string subjectLexical,
        int listIndex,
        List<OdfRdfTriple> triples)
    {
        if (property.Name == null)
        {
            return;
        }
        OdfRdfTriple triple = CreateTriple(
            context, property, property.Parent, subject, subjectLexical);
        if (IsRdf(property, "li"))
        {
            triple.Predicate = OdfNamespaces.Rdf + "_"
                + listIndex.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
            triple.PredicateLexical = "rdf:_"
                + listIndex.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            triple.Predicate = property.Name.NamespaceName
                + property.Name.LocalName;
            triple.PredicateLexical = property.Name.Prefix + ":"
                + property.Name.LocalName;
        }
        string resource = ReadRdf(property, "resource");
        string nodeId = ReadRdf(property, "nodeID");
        string parseType = ReadRdf(property, "parseType");
        OdfNode? nested = FirstElementChild(property);
        if (resource.Length > 0 || nodeId.Length > 0)
        {
            triple.ObjectLexical = resource.Length > 0
                ? resource
                : "_:" + nodeId;
            triple.Object = ResolveRdfTerm(
                context.PartPath, triple.ObjectLexical, true);
            triple.IsLiteral = false;
            triple.RdfXmlRepresentation = ResourceProperty;
        }
        else if (parseType == "Resource")
        {
            context.BlankIndex = context.BlankIndex + 1;
            triple.Object = GeneratedBlank(
                context.PartPath, context.BlankIndex);
            triple.ObjectLexical = triple.Object;
            triple.IsLiteral = false;
            triple.RdfXmlRepresentation = ComplexProperty;
            ParsePropertyContainer(property, context, triple.Object, triples);
        }
        else if (nested != null)
        {
            string nestedLexical = SubjectLexical(nested);
            string nestedSubject = ResolveRdfTerm(
                context.PartPath, nestedLexical, false);
            if (nestedLexical.Length == 0
                && !HasSubjectIdentifier(nested))
            {
                context.BlankIndex = context.BlankIndex + 1;
                nestedSubject = GeneratedBlank(
                    context.PartPath, context.BlankIndex);
            }
            triple.Object = nestedSubject;
            triple.ObjectLexical = nestedLexical;
            triple.IsLiteral = false;
            triple.RdfXmlRepresentation = ComplexProperty;
            ParseSubjectNode(nested, context, triples);
        }
        else
        {
            triple.Object = ReadText(property);
            triple.ObjectLexical = triple.Object;
            triple.DatatypeLexical = ReadRdf(property, "datatype");
            triple.Datatype = ResolveRdfTerm(
                context.PartPath, triple.DatatypeLexical, true);
            triple.Language = FindLanguage(property);
            triple.IsLiteral = true;
            triple.RdfXmlRepresentation = parseType.Length == 0
                ? LiteralProperty
                : ComplexProperty;
        }
        triple.IsValid = triple.Subject.Length > 0
            && triple.Predicate.Length > 0
            && (triple.IsLiteral || triple.Object.Length > 0);
        RememberRdfTriple(triple);
        triples.Add(triple);
    }

    static void ParsePropertyContainer(
        OdfNode container,
        RdfXmlParseContext context,
        string subject,
        List<OdfRdfTriple> triples)
    {
        int index = 0;
        int listIndex = 1;
        while (index < container.Children.Length)
        {
            OdfNode child = container.Children[index];
            if (child.Kind == OdfNodeKind.Element)
            {
                ParsePropertyNode(child, context, subject, "",
                    listIndex, triples);
                if (IsRdf(child, "li"))
                {
                    listIndex = listIndex + 1;
                }
            }
            index = index + 1;
        }
    }

    static OdfRdfTriple CreateTriple(
        RdfXmlParseContext context,
        OdfNode node,
        OdfNode? subjectNode,
        string subject,
        string subjectLexical)
    {
        OdfRdfTriple result = new OdfRdfTriple();
        result.SourceKind = context.SourceKind;
        result.PartPath = context.PartPath;
        result.Node = node;
        result.RdfXmlSubjectNode = subjectNode;
        result.Subject = subject;
        result.SubjectLexical = subjectLexical;
        return result;
    }

    static void SynchronizeOriginalPart(
        OdfDocument document,
        RdfXmlPartState part)
    {
        int index = 0;
        while (index < part.OriginalTriples.Length)
        {
            OdfRdfTriple triple = part.OriginalTriples[index];
            if (!ContainsTriple(document.RdfTriples, triple))
            {
                RemoveOriginalTriple(triple);
                part.IsChanged = true;
            }
            else if (RdfTripleChanged(triple))
            {
                SynchronizeOriginalTriple(part, triple);
                part.IsChanged = true;
            }
            index = index + 1;
        }
    }

    static void RemoveOriginalTriple(OdfRdfTriple triple)
    {
        if (triple.RdfXmlRepresentation == TypedNode)
        {
            if (triple.Node != null)
            {
                triple.Node.Name = QualifiedName(
                    OdfNamespaces.Rdf, "Description", OdfNamespaces.RdfPrefix);
            }
            return;
        }
        if (triple.RdfXmlRepresentation == PropertyAttribute)
        {
            if (triple.Node != null && triple.RdfXmlAttributeName != null)
            {
                StyleDocumentModel.RemoveAttribute(triple.Node,
                    triple.RdfXmlAttributeName.NamespaceName,
                    triple.RdfXmlAttributeName.LocalName);
            }
            return;
        }
        if (triple.Node != null)
        {
            StyleDocumentModel.RemoveNodeFromParent(triple.Node);
        }
    }

    static void SynchronizeOriginalTriple(
        RdfXmlPartState part,
        OdfRdfTriple triple)
    {
        if (triple.Node == null)
        {
            throw new OdfDocumentException(
                "An RDF/XML triple lost its source node.");
        }
        if (triple.RdfXmlRepresentation == ComplexProperty)
        {
            throw new OdfDocumentException(
                "Complex RDF/XML syntax can be preserved but not directly rewritten.");
        }
        WriteSubject(triple);
        if (triple.RdfXmlRepresentation == TypedNode)
        {
            triple.Node.Name = NameForIri(part, triple.Object);
        }
        else if (triple.RdfXmlRepresentation == PropertyAttribute)
        {
            OdfQualifiedName name = NameForIri(part, triple.Predicate);
            OdfQualifiedName? original = triple.RdfXmlAttributeName;
            if (original != null
                && (original.NamespaceName != name.NamespaceName
                    || original.LocalName != name.LocalName))
            {
                StyleDocumentModel.RemoveAttribute(triple.Node,
                    original.NamespaceName, original.LocalName);
            }
            StyleDocumentModel.WriteAttribute(triple.Node,
                name.NamespaceName, name.LocalName, name.Prefix, triple.Object);
            triple.RdfXmlAttributeName = name;
        }
        else
        {
            triple.Node.Name = NameForIri(part, triple.Predicate);
            if (triple.IsLiteral)
            {
                StyleDocumentModel.RemoveAttribute(
                    triple.Node, OdfNamespaces.Rdf, "resource");
                StyleDocumentModel.RemoveAttribute(
                    triple.Node, OdfNamespaces.Rdf, "nodeID");
                string datatype = DatatypeLexicalForWrite(triple);
                WriteOptionalRdf(triple.Node, "datatype", datatype);
                WriteOptionalXml(triple.Node, "lang", triple.Language);
                string literal = ObjectLexicalForWrite(triple);
                WriteText(triple.Node, literal);
                triple.ObjectLexical = literal;
                triple.DatatypeLexical = datatype;
                triple.RdfXmlRepresentation = LiteralProperty;
            }
            else
            {
                string resource = ObjectLexicalForWrite(triple);
                WriteResource(triple.Node, resource);
                StyleDocumentModel.RemoveAttribute(
                    triple.Node, OdfNamespaces.Rdf, "datatype");
                WriteText(triple.Node, "");
                triple.ObjectLexical = resource;
                triple.RdfXmlRepresentation = ResourceProperty;
            }
        }
        triple.SubjectLexical = SubjectLexicalForWrite(triple);
        OdfQualifiedName? synchronizedName = triple.Node.Name;
        if (synchronizedName == null)
        {
            throw new OdfDocumentException(
                "An RDF/XML property requires an XML name.");
        }
        triple.PredicateLexical = PrefixForNode(triple.Node)
            + ":" + synchronizedName.LocalName;
        RememberRdfTriple(triple);
    }

    static void AddTriple(RdfXmlPartState part, OdfRdfTriple triple)
    {
        ValidateTriple(triple);
        OdfNode? subjectNode = FindSubjectNode(part, triple.Subject);
        if (subjectNode == null)
        {
            subjectNode = CreateElement(
                OdfNamespaces.Rdf, "Description", OdfNamespaces.RdfPrefix,
                part.Path);
            WriteSubjectValue(subjectNode, triple.Subject);
            if (part.Root == null)
            {
                throw new OdfDocumentException(
                    "The RDF/XML part has no document element.");
            }
            StyleDocumentModel.AppendNode(part.Root, subjectNode);
        }
        OdfQualifiedName predicateName = NameForIri(part, triple.Predicate);
        OdfNode property = CreateElement(predicateName.NamespaceName,
            predicateName.LocalName, predicateName.Prefix, part.Path);
        if (triple.IsLiteral)
        {
            WriteOptionalRdf(property, "datatype", triple.Datatype);
            WriteOptionalXml(property, "lang", triple.Language);
            WriteText(property, triple.Object);
            triple.RdfXmlRepresentation = LiteralProperty;
        }
        else
        {
            WriteResource(property, triple.Object);
            triple.RdfXmlRepresentation = ResourceProperty;
        }
        StyleDocumentModel.AppendNode(subjectNode, property);
        triple.Node = property;
        triple.RdfXmlSubjectNode = subjectNode;
        triple.PartPath = part.Path;
        triple.SubjectLexical = triple.Subject;
        triple.PredicateLexical = predicateName.Prefix + ":"
            + predicateName.LocalName;
        triple.ObjectLexical = triple.Object;
        triple.DatatypeLexical = triple.Datatype;
        triple.IsValid = true;
        RememberRdfTriple(triple);
        part.IsChanged = true;
    }

    static RdfXmlPartState RequirePart(
        RdfXmlDocumentState state,
        OdfRdfTriple triple)
    {
        string path = triple.PartPath;
        if (path.Length == 0)
        {
            path = triple.SourceKind == OdfRdfSourceKind.PackageMetadata
                ? OdfPartNames.MetadataManifest
                : "Metadata/metadata.rdf";
            triple.PartPath = path;
        }
        int index = 0;
        while (index < state.Parts.Length)
        {
            if (state.Parts[index].Path == path)
            {
                return state.Parts[index];
            }
            index = index + 1;
        }
        RdfXmlPartState created = CreatePart(path, triple.SourceKind);
        RdfXmlPartState[] parts = new RdfXmlPartState[state.Parts.Length + 1];
        Array.Copy(state.Parts, parts, state.Parts.Length);
        parts[parts.Length - 1] = created;
        state.Parts = parts;
        return created;
    }

    static RdfXmlPartState CreatePart(
        string path,
        OdfRdfSourceKind sourceKind)
    {
        RdfXmlPartState result = new RdfXmlPartState();
        result.Path = path;
        result.SourceKind = sourceKind;
        OdfNode root = CreateElement(
            OdfNamespaces.Rdf, "RDF", OdfNamespaces.RdfPrefix, path);
        AddNamespace(root, OdfNamespaces.RdfPrefix, OdfNamespaces.Rdf);
        result.Root = root;
        result.Nodes = [root];
        result.IsChanged = true;
        return result;
    }

    static OdfNode? FindSubjectNode(
        RdfXmlPartState part,
        string subject)
    {
        int index = 0;
        while (index < part.OriginalTriples.Length)
        {
            OdfRdfTriple triple = part.OriginalTriples[index];
            if (triple.Subject == subject && triple.RdfXmlSubjectNode != null)
            {
                return triple.RdfXmlSubjectNode;
            }
            index = index + 1;
        }
        if (part.Root != null)
        {
            index = 0;
            while (index < part.Root.Children.Length)
            {
                OdfNode child = part.Root.Children[index];
                if (child.Kind == OdfNodeKind.Element
                    && ResolveRdfTerm(part.Path,
                        SubjectLexical(child), false) == subject)
                {
                    return child;
                }
                index = index + 1;
            }
        }
        return null;
    }

    static OdfQualifiedName NameForIri(
        RdfXmlPartState part,
        string iri)
    {
        if (part.Root == null || iri.Length == 0)
        {
            throw new OdfDocumentException(
                "An RDF predicate IRI and RDF root are required.");
        }
        string selectedPrefix = "";
        string selectedNamespace = "";
        int index = 0;
        while (index < part.Root.NamespaceDeclarations.Length)
        {
            OdfNamespaceDeclaration declaration =
                part.Root.NamespaceDeclarations[index];
            if (declaration.Prefix.Length > 0
                && iri.StartsWith(
                    declaration.NamespaceName, StringComparison.Ordinal)
                && declaration.NamespaceName.Length > selectedNamespace.Length)
            {
                selectedPrefix = declaration.Prefix;
                selectedNamespace = declaration.NamespaceName;
            }
            index = index + 1;
        }
        if (selectedNamespace.Length == 0)
        {
            selectedNamespace = SplitNamespace(iri);
            selectedPrefix = NewPrefix(part.Root);
            AddNamespace(part.Root, selectedPrefix, selectedNamespace);
        }
        string localName = iri.Substring(selectedNamespace.Length);
        if (!OdfNameRules.IsNcName(localName))
        {
            throw new OdfDocumentException(
                "The RDF predicate cannot be represented as an XML name.");
        }
        return QualifiedName(selectedNamespace, localName, selectedPrefix);
    }

    static string SplitNamespace(string iri)
    {
        int hash = iri.LastIndexOf('#');
        int slash = iri.LastIndexOf('/');
        int colon = iri.LastIndexOf(':');
        int separator = hash;
        if (slash > separator)
        {
            separator = slash;
        }
        if (colon > separator)
        {
            separator = colon;
        }
        if (separator < 0 || separator == iri.Length - 1)
        {
            throw new OdfDocumentException(
                "The RDF IRI cannot be split into a namespace and local name.");
        }
        return iri.Substring(0, separator + 1);
    }

    static string NewPrefix(OdfNode root)
    {
        int number = 1;
        while (number < int.MaxValue)
        {
            string candidate = "ns" + number.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
            if (!HasPrefix(root, candidate))
            {
                return candidate;
            }
            number = number + 1;
        }
        throw new OdfDocumentException("No RDF namespace prefix is available.");
    }

    static bool HasPrefix(OdfNode root, string prefix)
    {
        int index = 0;
        while (index < root.NamespaceDeclarations.Length)
        {
            if (root.NamespaceDeclarations[index].Prefix == prefix)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void AddNamespace(
        OdfNode root,
        string prefix,
        string namespaceName)
    {
        if (HasPrefix(root, prefix))
        {
            return;
        }
        OdfNamespaceDeclaration declaration = new OdfNamespaceDeclaration();
        declaration.Prefix = prefix;
        declaration.NamespaceName = namespaceName;
        declaration.Location.PartPath = root.Location.PartPath;
        OdfNamespaceDeclaration[] declarations =
            new OdfNamespaceDeclaration[root.NamespaceDeclarations.Length + 1];
        Array.Copy(root.NamespaceDeclarations, declarations,
            root.NamespaceDeclarations.Length);
        declarations[declarations.Length - 1] = declaration;
        root.NamespaceDeclarations = declarations;
    }

    static void WriteSubject(OdfRdfTriple triple)
    {
        if (triple.RdfXmlSubjectNode == null)
        {
            return;
        }
        WriteSubjectValue(
            triple.RdfXmlSubjectNode, SubjectLexicalForWrite(triple));
    }

    static void WriteSubjectValue(OdfNode node, string subject)
    {
        StyleDocumentModel.RemoveAttribute(node, OdfNamespaces.Rdf, "ID");
        if (subject.StartsWith("_:", StringComparison.Ordinal))
        {
            StyleDocumentModel.RemoveAttribute(
                node, OdfNamespaces.Rdf, "about");
            StyleDocumentModel.WriteAttribute(node, OdfNamespaces.Rdf,
                "nodeID", OdfNamespaces.RdfPrefix, subject.Substring(2));
        }
        else
        {
            StyleDocumentModel.RemoveAttribute(
                node, OdfNamespaces.Rdf, "nodeID");
            StyleDocumentModel.WriteAttribute(node, OdfNamespaces.Rdf,
                "about", OdfNamespaces.RdfPrefix, subject);
        }
    }

    static void WriteResource(OdfNode node, string resource)
    {
        if (resource.StartsWith("_:", StringComparison.Ordinal))
        {
            StyleDocumentModel.RemoveAttribute(
                node, OdfNamespaces.Rdf, "resource");
            StyleDocumentModel.WriteAttribute(node, OdfNamespaces.Rdf,
                "nodeID", OdfNamespaces.RdfPrefix, resource.Substring(2));
        }
        else
        {
            StyleDocumentModel.RemoveAttribute(
                node, OdfNamespaces.Rdf, "nodeID");
            StyleDocumentModel.WriteAttribute(node, OdfNamespaces.Rdf,
                "resource", OdfNamespaces.RdfPrefix, resource);
        }
    }

    static string SubjectLexicalForWrite(OdfRdfTriple triple)
    {
        if (triple.SubjectLexical != triple.OriginalSubjectLexical)
        {
            return triple.SubjectLexical;
        }
        return triple.Subject;
    }

    static string ObjectLexicalForWrite(OdfRdfTriple triple)
    {
        if (triple.ObjectLexical != triple.OriginalObjectLexical)
        {
            return triple.ObjectLexical;
        }
        if (triple.Object == triple.OriginalObject)
        {
            return triple.ObjectLexical;
        }
        return triple.Object;
    }

    static string DatatypeLexicalForWrite(OdfRdfTriple triple)
    {
        if (triple.DatatypeLexical != triple.OriginalDatatypeLexical)
        {
            return triple.DatatypeLexical;
        }
        if (triple.Datatype == triple.OriginalDatatype)
        {
            return triple.DatatypeLexical;
        }
        return triple.Datatype;
    }

    static void RefreshOriginalTriples(
        OdfDocument document,
        RdfXmlDocumentState state)
    {
        int partIndex = 0;
        while (partIndex < state.Parts.Length)
        {
            RdfXmlPartState part = state.Parts[partIndex];
            List<OdfRdfTriple> triples = new List<OdfRdfTriple>();
            int tripleIndex = 0;
            while (tripleIndex < document.RdfTriples.Length)
            {
                OdfRdfTriple triple = document.RdfTriples[tripleIndex];
                if (triple != null
                    && triple.PartPath == part.Path
                    && (triple.SourceKind == OdfRdfSourceKind.RdfXml
                        || triple.SourceKind
                            == OdfRdfSourceKind.PackageMetadata))
                {
                    triples.Add(triple);
                }
                tripleIndex = tripleIndex + 1;
            }
            part.OriginalTriples = triples.ToArray();
            partIndex = partIndex + 1;
        }
    }

    static bool RdfTripleChanged(OdfRdfTriple triple)
    {
        return triple.Subject != triple.OriginalSubject
            || triple.SubjectLexical != triple.OriginalSubjectLexical
            || triple.Predicate != triple.OriginalPredicate
            || triple.PredicateLexical != triple.OriginalPredicateLexical
            || triple.Object != triple.OriginalObject
            || triple.ObjectLexical != triple.OriginalObjectLexical
            || triple.Datatype != triple.OriginalDatatype
            || triple.DatatypeLexical != triple.OriginalDatatypeLexical
            || triple.Language != triple.OriginalLanguage
            || triple.IsLiteral != triple.OriginalIsLiteral;
    }

    static void RememberRdfTriple(OdfRdfTriple triple)
    {
        triple.OriginalSubject = triple.Subject;
        triple.OriginalSubjectLexical = triple.SubjectLexical;
        triple.OriginalPredicate = triple.Predicate;
        triple.OriginalPredicateLexical = triple.PredicateLexical;
        triple.OriginalObject = triple.Object;
        triple.OriginalObjectLexical = triple.ObjectLexical;
        triple.OriginalDatatype = triple.Datatype;
        triple.OriginalDatatypeLexical = triple.DatatypeLexical;
        triple.OriginalLanguage = triple.Language;
        triple.OriginalIsLiteral = triple.IsLiteral;
    }

    static bool IsOriginalTriple(
        RdfXmlDocumentState state,
        OdfRdfTriple triple)
    {
        int partIndex = 0;
        while (partIndex < state.Parts.Length)
        {
            if (ContainsTriple(state.Parts[partIndex].OriginalTriples, triple))
            {
                return true;
            }
            partIndex = partIndex + 1;
        }
        return false;
    }

    static bool ContainsTriple(
        OdfRdfTriple[] triples,
        OdfRdfTriple expected)
    {
        int index = 0;
        while (index < triples.Length)
        {
            if (triples[index] == expected)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasPackageTriples(OdfRdfTriple[] triples)
    {
        int index = 0;
        while (index < triples.Length)
        {
            OdfRdfTriple triple = triples[index];
            if (triple != null
                && (triple.SourceKind == OdfRdfSourceKind.RdfXml
                    || triple.SourceKind
                        == OdfRdfSourceKind.PackageMetadata))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void ValidateTriple(OdfRdfTriple triple)
    {
        if (triple.Subject == null
            || triple.Predicate == null
            || triple.Object == null
            || triple.Datatype == null
            || triple.Language == null
            || triple.Predicate.Length == 0)
        {
            throw new OdfDocumentException(
                "A new RDF/XML triple requires valid RDF terms.");
        }
    }

    static bool HasPackageEntry(OdfPackage package, string path)
    {
        int index = 0;
        while (index < package.Entries.Length)
        {
            if (package.Entries[index].Path == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfNode? FindRoot(OdfNode[] nodes)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index].Kind == OdfNodeKind.Element)
            {
                return nodes[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FirstElementChild(OdfNode node)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (node.Children[index].Kind == OdfNodeKind.Element)
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static string SubjectLexical(OdfNode node)
    {
        string about = ReadRdf(node, "about");
        if (about.Length > 0 || HasAttribute(node, OdfNamespaces.Rdf, "about"))
        {
            return about;
        }
        string id = ReadRdf(node, "ID");
        if (id.Length > 0)
        {
            return "#" + id;
        }
        string nodeId = ReadRdf(node, "nodeID");
        if (nodeId.Length > 0)
        {
            return "_:" + nodeId;
        }
        return "";
    }

    static bool HasSubjectIdentifier(OdfNode node)
    {
        return HasAttribute(node, OdfNamespaces.Rdf, "about")
            || HasAttribute(node, OdfNamespaces.Rdf, "ID")
            || HasAttribute(node, OdfNamespaces.Rdf, "nodeID");
    }

    static string ResolveRdfTerm(
        string partPath,
        string lexical,
        bool preserveEmpty)
    {
        if (lexical.StartsWith("_:", StringComparison.Ordinal)
            || OdfIriResolver.IsAbsoluteIri(lexical))
        {
            return lexical;
        }
        if (lexical.Length == 0)
        {
            return preserveEmpty ? "" : partPath;
        }
        if (lexical.StartsWith("#", StringComparison.Ordinal))
        {
            return partPath + lexical;
        }
        int slash = partPath.LastIndexOf('/');
        if (slash >= 0)
        {
            return partPath.Substring(0, slash + 1) + lexical;
        }
        return lexical;
    }

    static string GeneratedBlank(string path, int index)
    {
        global::System.Text.StringBuilder result =
            new global::System.Text.StringBuilder();
        result.Append("_:b_");
        int characterIndex = 0;
        while (characterIndex < path.Length)
        {
            char value = path[characterIndex];
            if (value >= 'a' && value <= 'z'
                || value >= 'A' && value <= 'Z'
                || value >= '0' && value <= '9')
            {
                result.Append(value);
            }
            else
            {
                result.Append('_');
            }
            characterIndex = characterIndex + 1;
        }
        result.Append('_');
        result.Append(index.ToString(
            global::System.Globalization.CultureInfo.InvariantCulture));
        return result.ToString();
    }

    static string FindLanguage(OdfNode node)
    {
        OdfNode? current = node;
        while (current != null)
        {
            string language = StyleDocumentModel.ReadAttribute(
                current, OdfNamespaces.Xml, "lang");
            if (language.Length > 0)
            {
                return language;
            }
            current = current.Parent;
        }
        return "";
    }

    static string ReadRdf(OdfNode node, string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node, OdfNamespaces.Rdf, localName);
    }

    static bool HasAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsRdf(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Rdf
            && node.Name.LocalName == localName;
    }

    static string PrefixForNode(OdfNode node)
    {
        return node.Name == null ? "" : node.Name.Prefix;
    }

    static OdfQualifiedName QualifiedName(
        string namespaceName,
        string localName,
        string prefix)
    {
        return StyleDocumentModel.QualifiedName(
            namespaceName, localName, prefix);
    }

    static OdfQualifiedName CopyName(OdfQualifiedName source)
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = source.NamespaceName;
        result.LocalName = source.LocalName;
        result.Prefix = source.Prefix;
        return result;
    }

    static OdfNode CreateElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = QualifiedName(namespaceName, localName, prefix);
        result.Location.PartPath = partPath;
        result.WasEmptyElement = true;
        return result;
    }

    static void WriteOptionalRdf(
        OdfNode node,
        string localName,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node, OdfNamespaces.Rdf, localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(node, OdfNamespaces.Rdf,
                localName, OdfNamespaces.RdfPrefix, value);
        }
    }

    static void WriteOptionalXml(
        OdfNode node,
        string localName,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node, OdfNamespaces.Xml, localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(node, OdfNamespaces.Xml,
                localName, OdfNamespaces.XmlPrefix, value);
        }
    }

    static string ReadText(OdfNode node)
    {
        global::System.Text.StringBuilder result =
            new global::System.Text.StringBuilder();
        AppendText(node, result);
        return result.ToString();
    }

    static void AppendText(
        OdfNode node,
        global::System.Text.StringBuilder result)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (child.Kind == OdfNodeKind.Text)
            {
                result.Append(child.Value);
            }
            else if (child.Kind == OdfNodeKind.Element)
            {
                AppendText(child, result);
            }
            index = index + 1;
        }
    }

    static void WriteText(OdfNode node, string value)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            node.Children[index].Parent = null;
            index = index + 1;
        }
        if (value.Length == 0)
        {
            node.Children = [];
            node.WasEmptyElement = true;
            return;
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = node;
        text.Location.PartPath = node.Location.PartPath;
        node.Children = [text];
        node.WasEmptyElement = false;
    }
}
