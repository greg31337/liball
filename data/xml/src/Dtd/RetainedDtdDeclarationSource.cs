using System;
using System.Collections.Generic;

namespace xml;

internal class RetainedDtdDeclarationSource : IDtdContentModelSource, IDtdAttributeDeclarationSource, IDtdDocumentDeclarationSource
{
    internal Dictionary<string, XmlElementDeclaration> Elements;
    internal Dictionary<string, DtdCompiledContentModel> ContentModels;
    internal Dictionary<string, XmlAttributeDeclaration> Attributes;
    internal Dictionary<string, List<XmlAttributeDeclaration>> AttributesByElement;
    internal Dictionary<string, XmlEntityDeclaration> Entities;
    internal Dictionary<string, XmlNotationDeclaration> Notations;
    internal XmlAttributeDeclaration[] RetainedAttributes;
    internal XmlEntityDeclaration[] RetainedEntities;
    internal List<string> DuplicateElementNames;
    internal List<string> DuplicateNotationNames;

    internal RetainedDtdDeclarationSource(XmlDocumentType documentType, XmlVersion version)
    {
        Guard.NotNull(documentType, "documentType");
        Elements = new Dictionary<string, XmlElementDeclaration>(StringComparer.Ordinal);
        ContentModels = new Dictionary<string, DtdCompiledContentModel>(StringComparer.Ordinal);
        Attributes = new Dictionary<string, XmlAttributeDeclaration>(StringComparer.Ordinal);
        AttributesByElement = new Dictionary<string, List<XmlAttributeDeclaration>>(StringComparer.Ordinal);
        Entities = new Dictionary<string, XmlEntityDeclaration>(StringComparer.Ordinal);
        Notations = new Dictionary<string, XmlNotationDeclaration>(StringComparer.Ordinal);
        RetainedAttributes = documentType.AttributeDeclarations == null ? [] : documentType.AttributeDeclarations;
        RetainedEntities = documentType.EntityDeclarations == null ? [] : documentType.EntityDeclarations;
        DuplicateElementNames = new List<string>();
        DuplicateNotationNames = new List<string>();
        IndexRetainedDtdElements(documentType.ElementDeclarations == null ? [] : documentType.ElementDeclarations, version);
        IndexRetainedDtdAttributes(RetainedAttributes);
        IndexRetainedDtdEntities(RetainedEntities);
        IndexRetainedDtdNotations(documentType.NotationDeclarations == null ? [] : documentType.NotationDeclarations);
    }

    internal void IndexRetainedDtdElements(XmlElementDeclaration[] declarations, XmlVersion version)
    {
        int index = 0;
        while (index < declarations.Length)
        {
            XmlElementDeclaration declaration = declarations[index];
            string name = RetainedDtdQualifiedName(declaration.Name);
            if (name.Length > 0 && !Elements.ContainsKey(name))
            {
                Elements.Add(name, declaration);
                ContentModels.Add(name, CompileRetainedDtdContentModel(name, declaration.ContentModel, version));
            }
            else if (name.Length > 0)
            {
                DuplicateElementNames.Add(name);
            }

            index = index + 1;
        }
    }

    internal void IndexRetainedDtdAttributes(XmlAttributeDeclaration[] declarations)
    {
        int index = 0;
        while (index < declarations.Length)
        {
            XmlAttributeDeclaration declaration = declarations[index];
            string elementName = RetainedDtdQualifiedName(declaration.ElementName);
            string attributeName = RetainedDtdQualifiedName(declaration.Name);
            string key = elementName + "\0" + attributeName;
            if (elementName.Length > 0 && attributeName.Length > 0 && !Attributes.ContainsKey(key))
            {
                Attributes.Add(key, declaration);
                if (!AttributesByElement.ContainsKey(elementName))
                {
                    AttributesByElement.Add(elementName, new List<XmlAttributeDeclaration>());
                }

                AttributesByElement[elementName].Add(declaration);
            }

            index = index + 1;
        }
    }

    internal void IndexRetainedDtdEntities(XmlEntityDeclaration[] declarations)
    {
        int index = 0;
        while (index < declarations.Length)
        {
            XmlEntityDeclaration declaration = declarations[index];
            string name = declaration.Name == null ? "" : declaration.Name;
            if (!declaration.IsParameter && name.Length > 0 && !Entities.ContainsKey(name))
            {
                Entities.Add(name, declaration);
            }

            index = index + 1;
        }
    }

    internal void IndexRetainedDtdNotations(XmlNotationDeclaration[] declarations)
    {
        int index = 0;
        while (index < declarations.Length)
        {
            XmlNotationDeclaration declaration = declarations[index];
            string name = declaration.Name == null ? "" : declaration.Name;
            if (name.Length > 0 && !Notations.ContainsKey(name))
            {
                Notations.Add(name, declaration);
            }
            else if (name.Length > 0)
            {
                DuplicateNotationNames.Add(name);
            }

            index = index + 1;
        }
    }

    internal DtdCompiledContentModel CompileRetainedDtdContentModel(string elementName, string? contentModel, XmlVersion version)
    {
        string model = contentModel == null || contentModel.Length == 0 ? "ANY" : contentModel;
        string declarationText = " " + elementName + " " + model;
        DtdScanner scanner = new DtdScanner(declarationText.AsMemory(), version);
        List<DtdTokenEntry> entries = new List<DtdTokenEntry>();
        while (true)
        {
            DtdToken token = scanner.ScanNextDtdToken();
            if (token.Kind == DtdTokenKind.EndOfInput)
            {
                break;
            }

            entries.Add(new DtdTokenEntry(token, DtdSourceKind.InternalSubset, null, null, 0, false));
        }

        DtdDeclarationReader reader = new DtdDeclarationReader(entries, 0, entries.Count);
        DtdElementDeclarationParser parser = new DtdElementDeclarationParser(reader, 0);
        parser.ParseDtdElementDeclaration();
        DtdCompiledContentModel? compiled = parser.CompiledModel;
        if (compiled == null)
        {
            throw new FormatException("The retained DTD element content model could not be compiled.");
        }

        return compiled;
    }

    internal string RetainedDtdQualifiedName(XmlName? name)
    {
        if (name == null)
        {
            return "";
        }

        if (name.QualifiedName != null)
        {
            return name.QualifiedName;
        }

        if (name.LocalName == null)
        {
            return "";
        }

        return name.Prefix == null ? name.LocalName : name.Prefix + ":" + name.LocalName;
    }

    public DtdCompiledContentModel? FindDtdCompiledContentModel(string elementName)
    {
        return ContentModels.ContainsKey(elementName) ? ContentModels[elementName] : null;
    }

    public XmlAttributeDeclaration? FindDtdAttributeDeclaration(string elementName, string attributeName)
    {
        string key = elementName + "\0" + attributeName;
        return Attributes.ContainsKey(key) ? Attributes[key] : null;
    }

    public XmlAttributeDeclaration[] FindDtdAttributeDeclarationsForElement(string elementName)
    {
        return AttributesByElement.ContainsKey(elementName) ? AttributesByElement[elementName].ToArray() : [];
    }

    public XmlAttributeDeclaration[] FindAllDtdAttributeDeclarations()
    {
        return RetainedAttributes;
    }

    public bool IsDtdAttributeDeclarationExternal(string elementName, string attributeName)
    {
        return false;
    }

    public XmlEntityDeclaration? FindDtdAttributeEntity(string name)
    {
        return Entities.ContainsKey(name) ? Entities[name] : null;
    }

    public bool DoesDtdAttributeDefaultUseExternalEntity(string elementName, string attributeName)
    {
        return false;
    }

    public bool HasDtdAttributeNotationDeclaration(string name)
    {
        return Notations.ContainsKey(name);
    }

    public XmlElementDeclaration? FindDtdAttributeElementDeclaration(string name)
    {
        return Elements.ContainsKey(name) ? Elements[name] : null;
    }

    public XmlElementDeclaration? FindDtdDocumentElementDeclaration(string name)
    {
        return Elements.ContainsKey(name) ? Elements[name] : null;
    }

    public string[] FindDtdDuplicateElementDeclarationNames()
    {
        return DuplicateElementNames.ToArray();
    }

    public string[] FindDtdDuplicateNotationDeclarationNames()
    {
        return DuplicateNotationNames.ToArray();
    }

    public string[] FindDtdDeclarationValidityErrors()
    {
        return [];
    }

    public XmlEntityDeclaration[] FindAllDtdDocumentEntityDeclarations()
    {
        return RetainedEntities;
    }

    public XmlNotationDeclaration[] FindAllDtdDocumentNotationDeclarations()
    {
        XmlNotationDeclaration[] declarations = new XmlNotationDeclaration[Notations.Count];
        Notations.Values.CopyTo(declarations, 0);
        return declarations;
    }

    public bool HasDtdDocumentNotationDeclaration(string name)
    {
        return Notations.ContainsKey(name);
    }

    public bool IsDtdDocumentElementDeclarationExternal(string name)
    {
        return false;
    }

    public bool IsDtdDocumentEntityDeclarationExternal(string name)
    {
        return false;
    }
}
