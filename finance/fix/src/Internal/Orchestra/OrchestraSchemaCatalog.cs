using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Schema;

namespace fix;

internal static class OrchestraSchemaCatalog
{
    internal const string DublinCoreTermsNamespace = "http://purl.org/dc/terms/";
    internal const string DublinCoreElementsNamespace = "http://purl.org/dc/elements/1.1/";
    internal const string DublinCoreTypesNamespace = "http://purl.org/dc/dcmitype/";
    internal const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    internal static readonly XmlSchemaSet Repository10Schemas = CreateRepository10Schemas();
    internal static readonly XmlSchemaSet Interfaces10Schemas = CreateInterfaces10Schemas();
    internal static readonly XmlSchemaSet Repository11Schemas = CreateRepository11Schemas();
    internal static readonly XmlSchemaSet Interfaces11Schemas = CreateInterfaces11Schemas();

    internal static XmlSchemaSet GetSchemas(OrchestraVersion version, bool interfacesDocument)
    {
        if (version == OrchestraVersion.Version10)
        {
            return interfacesDocument ? Interfaces10Schemas : Repository10Schemas;
        }
        return interfacesDocument ? Interfaces11Schemas : Repository11Schemas;
    }

    internal static XmlSchemaSet CreateRepository10Schemas()
    {
        return CreateRepositorySchemas(OrchestraSchemaResources.Repository10, OrchestraSchemaResources.RepositoryTypes10, false);
    }

    internal static XmlSchemaSet CreateInterfaces10Schemas()
    {
        return CreateInterfacesSchemas(OrchestraSchemaResources.Interfaces10);
    }

    internal static XmlSchemaSet CreateRepository11Schemas()
    {
        return CreateRepositorySchemas(OrchestraSchemaResources.Repository11, OrchestraSchemaResources.RepositoryTypes11, true);
    }

    internal static XmlSchemaSet CreateInterfaces11Schemas()
    {
        return CreateInterfacesSchemas(OrchestraSchemaResources.Interfaces11);
    }

    internal static XmlSchemaSet CreateRepositorySchemas(string repositoryData, string repositoryTypesData, bool normalizeRootKeys)
    {
        XmlSchema repository = ReadSchema(repositoryData);
        XmlSchema repositoryTypes = ReadSchema(repositoryTypesData);
        XmlSchema terms = ReadSchema(OrchestraSchemaResources.DublinCoreTerms);
        XmlSchema elements = ReadSchema(OrchestraSchemaResources.DublinCoreElements);
        XmlSchema types = ReadSchema(OrchestraSchemaResources.DublinCoreTypes);
        XmlSchema xml = ReadSchema(OrchestraSchemaResources.XmlNamespace);
        if (normalizeRootKeys) AddRepository11RootKeys(repository);
        ConnectExternalSchemas(repository, repositoryTypes, terms, elements, types, xml);
        ConnectExternalSchemas(terms, null, terms, elements, types, xml);
        ConnectExternalSchemas(elements, null, terms, elements, types, xml);
        return CompileSchemas(repository);
    }

    internal static void AddRepository11RootKeys(XmlSchema schema)
    {
        XmlSchemaElement? repository = FindSchemaElement(schema, "repository");
        XmlSchemaElement? datatypes = FindSchemaElement(schema, "datatypes");
        XmlSchemaElement? codeSets = FindSchemaElement(schema, "codeSets");
        if (repository == null || datatypes == null || codeSets == null) throw new InvalidOperationException("The built-in Orchestra repository schema has incomplete key scopes.");
        RemoveSchemaConstraint(datatypes, "datatypeKey");
        RemoveSchemaConstraint(codeSets, "codeSetNameKey");
        XmlSchemaKey datatypeKey = CreateSchemaKey("datatypeKey", "fixr:datatypes/fixr:datatype", ["@name"]);
        XmlSchemaKey codeSetNameKey = CreateSchemaKey("codeSetNameKey", "fixr:codeSets/fixr:codeSet", ["@name", "@scenario"]);
        repository.Constraints.Add(datatypeKey);
        repository.Constraints.Add(codeSetNameKey);
    }

    internal static void RemoveSchemaConstraint(XmlSchemaElement element, string name)
    {
        int index = element.Constraints.Count - 1;
        while (index >= 0)
        {
            XmlSchemaIdentityConstraint? constraint = element.Constraints[index] as XmlSchemaIdentityConstraint;
            if (constraint != null && constraint.Name == name) element.Constraints.RemoveAt(index);
            index--;
        }
    }

    internal static XmlSchemaElement? FindSchemaElement(XmlSchema schema, string name)
    {
        for (int index = 0; index < schema.Items.Count; index++)
        {
            XmlSchemaElement? element = schema.Items[index] as XmlSchemaElement;
            if (element != null && element.Name == name) return element;
        }
        return null;
    }

    internal static XmlSchemaKey CreateSchemaKey(string name, string selectorPath, string[] fieldPaths)
    {
        XmlSchemaKey key = new XmlSchemaKey();
        key.Name = name;
        XmlSchemaXPath selector = new XmlSchemaXPath();
        selector.XPath = selectorPath;
        key.Selector = selector;
        for (int index = 0; index < fieldPaths.Length; index++)
        {
            XmlSchemaXPath field = new XmlSchemaXPath();
            field.XPath = fieldPaths[index];
            key.Fields.Add(field);
        }
        return key;
    }

    internal static XmlSchemaSet CreateInterfacesSchemas(string interfacesData)
    {
        XmlSchema interfaces = ReadSchema(interfacesData);
        XmlSchema terms = ReadSchema(OrchestraSchemaResources.DublinCoreTerms);
        XmlSchema elements = ReadSchema(OrchestraSchemaResources.DublinCoreElements);
        XmlSchema types = ReadSchema(OrchestraSchemaResources.DublinCoreTypes);
        XmlSchema xml = ReadSchema(OrchestraSchemaResources.XmlNamespace);
        ConnectExternalSchemas(interfaces, null, terms, elements, types, xml);
        ConnectExternalSchemas(terms, null, terms, elements, types, xml);
        ConnectExternalSchemas(elements, null, terms, elements, types, xml);
        return CompileSchemas(interfaces);
    }

    internal static XmlSchema ReadSchema(string data)
    {
        byte[] compressed = Convert.FromBase64String(data);
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        using (MemoryStream input = new MemoryStream(compressed, false))
        using (GZipStream decompressor = new GZipStream(input, CompressionMode.Decompress, false))
        using (XmlReader reader = XmlReader.Create(decompressor, settings))
        {
            XmlSchema? schema = XmlSchema.Read(reader, null);
            if (schema == null) throw new InvalidOperationException("A built-in Orchestra schema could not be read.");
            return schema;
        }
    }

    internal static void ConnectExternalSchemas(XmlSchema schema, XmlSchema? sameNamespace, XmlSchema terms, XmlSchema elements, XmlSchema types, XmlSchema xml)
    {
        for (int index = 0; index < schema.Includes.Count; index++)
        {
            XmlSchemaExternal? external = schema.Includes[index] as XmlSchemaExternal;
            if (external != null)
            {
                XmlSchemaInclude? include = external as XmlSchemaInclude;
                XmlSchemaImport? import = external as XmlSchemaImport;
                if (include != null)
                {
                    external.Schema = sameNamespace;
                }
                else if (import != null && import.Namespace == DublinCoreTermsNamespace)
                {
                    external.Schema = terms;
                }
                else if (import != null && import.Namespace == DublinCoreElementsNamespace)
                {
                    external.Schema = elements;
                }
                else if (import != null && import.Namespace == DublinCoreTypesNamespace)
                {
                    external.Schema = types;
                }
                else if (import != null && import.Namespace == XmlNamespace)
                {
                    external.Schema = xml;
                }
                external.SchemaLocation = null;
            }
        }
    }

    internal static XmlSchemaSet CompileSchemas(XmlSchema root)
    {
        XmlSchemaSet schemas = new XmlSchemaSet();
        schemas.XmlResolver = null;
        schemas.Add(root);
        schemas.Compile();
        return schemas;
    }
}
