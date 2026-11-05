using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class RdfaDocumentState
{
    internal OdfNode[] Nodes = [];
}

internal static class RdfaDocumentModel
{
    internal static void LoadRdfa(OdfDocument document)
    {
        List<OdfRdfTriple> triples = new List<OdfRdfTriple>();
        List<OdfNode> nodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Body != null)
            {
                LoadNode(part.Body, part.Path, triples, nodes);
            }
            partIndex = partIndex + 1;
        }
        document.RdfTriples = triples.ToArray();
        RdfaDocumentState state = new RdfaDocumentState();
        state.Nodes = nodes.ToArray();
        document.RdfaState = state;
    }

    internal static void SynchronizeRdfa(OdfDocument document)
    {
        if (document.RdfTriples == null)
        {
            throw new OdfDocumentException("Document RDF triples are required.");
        }
        RdfaDocumentState? state = document.RdfaState;
        if (state == null)
        {
            state = new RdfaDocumentState();
            document.RdfaState = state;
        }

        int originalIndex = 0;
        while (originalIndex < state.Nodes.Length)
        {
            OdfNode node = state.Nodes[originalIndex];
            if (!HasRdfaTriple(document.RdfTriples, node))
            {
                RemoveRdfaAttributes(node);
            }
            originalIndex = originalIndex + 1;
        }

        List<OdfNode> synchronized = new List<OdfNode>();
        int tripleIndex = 0;
        while (tripleIndex < document.RdfTriples.Length)
        {
            OdfRdfTriple triple = document.RdfTriples[tripleIndex];
            if (triple == null)
            {
                throw new OdfDocumentException("An RDF triple cannot be null.");
            }
            if (triple.SourceKind == OdfRdfSourceKind.Rdfa)
            {
                if (triple.Node == null
                    || triple.Node.Kind != OdfNodeKind.Element)
                {
                    throw new OdfDocumentException(
                        "An RDFa triple requires a content element.");
                }
                if (!ContainsNode(synchronized, triple.Node))
                {
                    SynchronizeNode(document.RdfTriples, triple.Node);
                    synchronized.Add(triple.Node);
                }
            }
            tripleIndex = tripleIndex + 1;
        }
        state.Nodes = synchronized.ToArray();
    }

    static void LoadNode(
        OdfNode node,
        string partPath,
        List<OdfRdfTriple> triples,
        List<OdfNode> nodes)
    {
        if (node.Kind == OdfNodeKind.Element)
        {
            string property = ReadXhtml(node, "property");
            if (property.Length > 0)
            {
                LoadStatement(node, partPath, property, triples);
                nodes.Add(node);
            }
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            LoadNode(node.Children[index], partPath, triples, nodes);
            index = index + 1;
        }
    }

    static void LoadStatement(
        OdfNode node,
        string partPath,
        string property,
        List<OdfRdfTriple> triples)
    {
        string about = ReadXhtml(node, "about");
        string datatype = ReadXhtml(node, "datatype");
        string content = ReadXhtml(node, "content");
        string resource = ReadXhtml(node, "resource");
        bool hasContent = HasAttribute(node, OdfNamespaces.Xhtml, "content");
        bool hasResource = HasAttribute(node, OdfNamespaces.Xhtml, "resource");
        string literal = hasContent ? content : ReadText(node);
        string[] properties = SplitTerms(property);
        int index = 0;
        while (index < properties.Length)
        {
            OdfRdfTriple triple = new OdfRdfTriple();
            triple.SourceKind = OdfRdfSourceKind.Rdfa;
            triple.SubjectLexical = about;
            triple.Subject = ResolveSubject(node, partPath, about);
            triple.PredicateLexical = properties[index];
            triple.Predicate = ResolveCurie(node, properties[index], false);
            triple.DatatypeLexical = datatype;
            triple.Datatype = datatype.Length == 0
                ? ""
                : ResolveCurie(node, datatype, false);
            triple.IsLiteral = !hasResource;
            if (triple.IsLiteral)
            {
                triple.Object = literal;
                triple.ObjectLexical = literal;
                triple.Language = FindLanguage(node);
            }
            else
            {
                triple.ObjectLexical = resource;
                triple.Object = ResolveSubject(node, partPath, resource);
            }
            triple.IsValid = IsTripleValid(triple);
            triple.PartPath = partPath;
            triple.Node = node;
            triple.OriginalHadContent = hasContent;
            RememberTriple(triple);
            triples.Add(triple);
            index = index + 1;
        }
    }

    static void SynchronizeNode(OdfRdfTriple[] triples, OdfNode node)
    {
        List<OdfRdfTriple> group = new List<OdfRdfTriple>();
        int index = 0;
        while (index < triples.Length)
        {
            OdfRdfTriple triple = triples[index];
            if (triple.SourceKind == OdfRdfSourceKind.Rdfa
                && triple.Node == node)
            {
                ValidateStrings(triple);
                group.Add(triple);
            }
            index = index + 1;
        }
        if (group.Count == 0)
        {
            RemoveRdfaAttributes(node);
            return;
        }
        OdfRdfTriple first = group[0];
        index = 1;
        while (index < group.Count)
        {
            RequireCommonStatement(first, group[index]);
            index = index + 1;
        }

        string about = SubjectLexicalForWrite(first);
        List<string> predicates = new List<string>();
        index = 0;
        while (index < group.Count)
        {
            predicates.Add(PredicateLexicalForWrite(group[index]));
            index = index + 1;
        }
        WriteXhtml(node, "about", about);
        WriteXhtml(node, "property", JoinTerms(predicates));
        if (first.IsLiteral)
        {
            RemoveXhtml(node, "resource");
            string datatype = DatatypeLexicalForWrite(first);
            WriteOptionalXhtml(node, "datatype", datatype);
            bool changedObject = first.Object != first.OriginalObject
                || first.ObjectLexical != first.OriginalObjectLexical;
            if (first.OriginalHadContent || changedObject)
            {
                string content = LiteralLexicalForWrite(first);
                WriteXhtml(node, "content", content);
            }
            else
            {
                RemoveXhtml(node, "content");
            }
            if (first.Language != first.OriginalLanguage)
            {
                WriteOptionalXml(node, "lang", first.Language);
            }
        }
        else
        {
            RemoveXhtml(node, "content");
            RemoveXhtml(node, "datatype");
            WriteXhtml(node, "resource", ResourceLexicalForWrite(first));
        }

        index = 0;
        while (index < group.Count)
        {
            OdfRdfTriple triple = group[index];
            triple.SubjectLexical = about;
            triple.PredicateLexical = predicates[index];
            triple.Subject = ResolveSubject(
                node, triple.PartPath, triple.SubjectLexical);
            triple.Predicate = ResolveCurie(
                node, triple.PredicateLexical, false);
            if (triple.IsLiteral)
            {
                triple.ObjectLexical = LiteralLexicalForWrite(triple);
                triple.Object = triple.ObjectLexical;
                triple.DatatypeLexical = DatatypeLexicalForWrite(triple);
                triple.Datatype = triple.DatatypeLexical.Length == 0
                    ? ""
                    : ResolveCurie(node, triple.DatatypeLexical, false);
            }
            else
            {
                triple.ObjectLexical = ResourceLexicalForWrite(triple);
                triple.Object = ResolveSubject(
                    node, triple.PartPath, triple.ObjectLexical);
                triple.Datatype = "";
                triple.DatatypeLexical = "";
                triple.Language = "";
            }
            triple.IsValid = IsTripleValid(triple);
            triple.OriginalHadContent = HasAttribute(
                node, OdfNamespaces.Xhtml, "content");
            RememberTriple(triple);
            index = index + 1;
        }
    }

    static void RequireCommonStatement(
        OdfRdfTriple first,
        OdfRdfTriple current)
    {
        if (first.Subject != current.Subject
            || first.SubjectLexical != current.SubjectLexical
            || first.Object != current.Object
            || first.ObjectLexical != current.ObjectLexical
            || first.Datatype != current.Datatype
            || first.DatatypeLexical != current.DatatypeLexical
            || first.Language != current.Language
            || first.IsLiteral != current.IsLiteral)
        {
            throw new OdfDocumentException(
                "RDFa triples on one content node must share a subject and object.");
        }
    }

    static string SubjectLexicalForWrite(OdfRdfTriple triple)
    {
        if (triple.SubjectLexical != triple.OriginalSubjectLexical)
        {
            return triple.SubjectLexical;
        }
        if (triple.Subject == triple.OriginalSubject)
        {
            return triple.SubjectLexical;
        }
        if (triple.Subject.StartsWith("_:", StringComparison.Ordinal))
        {
            return "[" + triple.Subject + "]";
        }
        return triple.Subject;
    }

    static string PredicateLexicalForWrite(OdfRdfTriple triple)
    {
        if (triple.PredicateLexical != triple.OriginalPredicateLexical)
        {
            return triple.PredicateLexical;
        }
        if (triple.Predicate == triple.OriginalPredicate
            && triple.PredicateLexical.Length > 0)
        {
            return triple.PredicateLexical;
        }
        return GenerateCurie(triple.Node, triple.Predicate);
    }

    static string DatatypeLexicalForWrite(OdfRdfTriple triple)
    {
        if (triple.DatatypeLexical != triple.OriginalDatatypeLexical)
        {
            return triple.DatatypeLexical;
        }
        if (triple.Datatype == triple.OriginalDatatype
            && triple.DatatypeLexical.Length > 0)
        {
            return triple.DatatypeLexical;
        }
        if (triple.Datatype.Length == 0)
        {
            return "";
        }
        return GenerateCurie(triple.Node, triple.Datatype);
    }

    static string ResourceLexicalForWrite(OdfRdfTriple triple)
    {
        if (triple.ObjectLexical != triple.OriginalObjectLexical)
        {
            return triple.ObjectLexical;
        }
        if (triple.Object == triple.OriginalObject)
        {
            return triple.ObjectLexical;
        }
        if (triple.Object.StartsWith("_:", StringComparison.Ordinal))
        {
            return "[" + triple.Object + "]";
        }
        return triple.Object;
    }

    static string LiteralLexicalForWrite(OdfRdfTriple triple)
    {
        if (triple.ObjectLexical != triple.OriginalObjectLexical)
        {
            return triple.ObjectLexical;
        }
        return triple.Object;
    }

    static string GenerateCurie(OdfNode? node, string iri)
    {
        if (node == null || iri.Length == 0)
        {
            throw new OdfDocumentException(
                "A non-empty RDF IRI and content node are required.");
        }
        Dictionary<string, string> bindings = EffectiveBindings(node);
        string selectedPrefix = "";
        string selectedNamespace = "";
        foreach (KeyValuePair<string, string> binding in bindings)
        {
            if (binding.Key.Length > 0
                && binding.Key != OdfNamespaces.XmlPrefix
                && iri.StartsWith(binding.Value, StringComparison.Ordinal)
                && binding.Value.Length > selectedNamespace.Length)
            {
                selectedPrefix = binding.Key;
                selectedNamespace = binding.Value;
            }
        }
        if (selectedPrefix.Length == 0)
        {
            throw new OdfDocumentException(
                "The RDF IRI has no in-scope namespace prefix.");
        }
        return selectedPrefix + ":" + iri.Substring(selectedNamespace.Length);
    }

    static string ResolveSubject(
        OdfNode node,
        string partPath,
        string lexical)
    {
        if (lexical.StartsWith("[", StringComparison.Ordinal))
        {
            return ResolveCurie(node, lexical, true);
        }
        if (!OdfIriResolver.IsIriReference(lexical))
        {
            return "";
        }
        return ResolveIriReference(node, partPath, lexical);
    }

    static string ResolveCurie(
        OdfNode node,
        string lexical,
        bool safe)
    {
        OdfCurieValue curie = safe
            ? OdfIriResolver.ParseSafeCurie(lexical)
            : OdfIriResolver.ParseCurie(lexical);
        if (!curie.IsValid)
        {
            return "";
        }
        if (curie.Prefix == "_")
        {
            return "_:" + curie.Reference;
        }
        string? namespaceName = LookupNamespace(node, curie.Prefix);
        if (namespaceName == null)
        {
            return curie.Prefix.Length == 0 ? curie.Reference : "";
        }
        return namespaceName + curie.Reference;
    }

    static string ResolveIriReference(
        OdfNode node,
        string partPath,
        string lexical)
    {
        if (OdfIriResolver.IsAbsoluteIri(lexical))
        {
            return lexical;
        }
        string baseIri = FindXmlBase(node);
        if (baseIri.Length == 0)
        {
            baseIri = partPath;
        }
        if (baseIri.Length == 0)
        {
            return lexical;
        }
        if (lexical.StartsWith("#", StringComparison.Ordinal))
        {
            int fragment = baseIri.IndexOf('#');
            if (fragment >= 0)
            {
                baseIri = baseIri.Substring(0, fragment);
            }
            return baseIri + lexical;
        }
        int slash = baseIri.LastIndexOf('/');
        if (baseIri.EndsWith("/", StringComparison.Ordinal))
        {
            return baseIri + lexical;
        }
        if (slash >= 0)
        {
            return baseIri.Substring(0, slash + 1) + lexical;
        }
        return lexical;
    }

    static string FindXmlBase(OdfNode node)
    {
        OdfNode? current = node;
        while (current != null)
        {
            string value = ReadAttribute(current, OdfNamespaces.Xml, "base");
            if (value.Length > 0)
            {
                return value;
            }
            current = current.Parent;
        }
        return "";
    }

    static string FindLanguage(OdfNode node)
    {
        OdfNode? current = node;
        while (current != null)
        {
            string value = ReadAttribute(current, OdfNamespaces.Xml, "lang");
            if (value.Length > 0)
            {
                return value;
            }
            current = current.Parent;
        }
        return "";
    }

    static string? LookupNamespace(OdfNode node, string prefix)
    {
        OdfNode? current = node;
        while (current != null)
        {
            int index = 0;
            while (index < current.NamespaceDeclarations.Length)
            {
                OdfNamespaceDeclaration declaration =
                    current.NamespaceDeclarations[index];
                if (declaration.Prefix == prefix)
                {
                    return declaration.NamespaceName;
                }
                index = index + 1;
            }
            current = current.Parent;
        }
        if (prefix == OdfNamespaces.XmlPrefix)
        {
            return OdfNamespaces.Xml;
        }
        return null;
    }

    static Dictionary<string, string> EffectiveBindings(OdfNode node)
    {
        List<OdfNode> ancestors = new List<OdfNode>();
        OdfNode? current = node;
        while (current != null)
        {
            ancestors.Add(current);
            current = current.Parent;
        }
        Dictionary<string, string> result =
            new Dictionary<string, string>(StringComparer.Ordinal);
        result.Add(OdfNamespaces.XmlPrefix, OdfNamespaces.Xml);
        int ancestorIndex = ancestors.Count - 1;
        while (ancestorIndex >= 0)
        {
            OdfNode ancestor = ancestors[ancestorIndex];
            int declarationIndex = 0;
            while (declarationIndex < ancestor.NamespaceDeclarations.Length)
            {
                OdfNamespaceDeclaration declaration =
                    ancestor.NamespaceDeclarations[declarationIndex];
                result[declaration.Prefix] = declaration.NamespaceName;
                declarationIndex = declarationIndex + 1;
            }
            ancestorIndex = ancestorIndex - 1;
        }
        return result;
    }

    static bool IsTripleValid(OdfRdfTriple triple)
    {
        if (triple.Subject.Length == 0 || triple.Predicate.Length == 0)
        {
            return false;
        }
        if (!triple.IsLiteral && triple.Object.Length == 0)
        {
            return false;
        }
        return triple.DatatypeLexical.Length == 0
            || triple.Datatype.Length > 0;
    }

    static void RememberTriple(OdfRdfTriple triple)
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

    static void ValidateStrings(OdfRdfTriple triple)
    {
        if (triple.Subject == null
            || triple.SubjectLexical == null
            || triple.Predicate == null
            || triple.PredicateLexical == null
            || triple.Object == null
            || triple.ObjectLexical == null
            || triple.Datatype == null
            || triple.DatatypeLexical == null
            || triple.Language == null)
        {
            throw new OdfDocumentException(
                "RDF triple string values cannot be null.");
        }
    }

    static bool HasRdfaTriple(OdfRdfTriple[] triples, OdfNode node)
    {
        int index = 0;
        while (index < triples.Length)
        {
            OdfRdfTriple triple = triples[index];
            if (triple != null
                && triple.SourceKind == OdfRdfSourceKind.Rdfa
                && triple.Node == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveRdfaAttributes(OdfNode node)
    {
        RemoveXhtml(node, "about");
        RemoveXhtml(node, "property");
        RemoveXhtml(node, "content");
        RemoveXhtml(node, "datatype");
        RemoveXhtml(node, "resource");
    }

    static string ReadXhtml(OdfNode node, string localName)
    {
        return ReadAttribute(node, OdfNamespaces.Xhtml, localName);
    }

    static string ReadAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(node, namespaceName, localName);
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

    static void WriteXhtml(OdfNode node, string localName, string value)
    {
        StyleDocumentModel.WriteAttribute(node, OdfNamespaces.Xhtml,
            localName, OdfNamespaces.XhtmlPrefix, value);
    }

    static void WriteOptionalXhtml(
        OdfNode node,
        string localName,
        string value)
    {
        if (value.Length == 0)
        {
            RemoveXhtml(node, localName);
        }
        else
        {
            WriteXhtml(node, localName, value);
        }
    }

    static void RemoveXhtml(OdfNode node, string localName)
    {
        StyleDocumentModel.RemoveAttribute(
            node, OdfNamespaces.Xhtml, localName);
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

    static string[] SplitTerms(string value)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && IsXmlWhitespace(value[index]))
            {
                index = index + 1;
            }
            int start = index;
            while (index < value.Length && !IsXmlWhitespace(value[index]))
            {
                index = index + 1;
            }
            if (index > start)
            {
                result.Add(value.Substring(start, index - start));
            }
        }
        return result.ToArray();
    }

    static string JoinTerms(List<string> values)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0)
            {
                result.Append(' ');
            }
            result.Append(values[index]);
            index = index + 1;
        }
        return result.ToString();
    }

    static bool IsXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    static string ReadText(OdfNode node)
    {
        StringBuilder result = new StringBuilder();
        AppendText(node, result);
        return result.ToString();
    }

    static void AppendText(OdfNode node, StringBuilder result)
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

    static bool ContainsNode(List<OdfNode> nodes, OdfNode node)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
