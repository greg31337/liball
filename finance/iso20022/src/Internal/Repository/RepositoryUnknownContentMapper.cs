using System;
using System.Collections.Generic;

namespace iso20022;

internal class RepositoryUnknownContentMapper
{
    DataDictionaryImportContext context;
    Iso20022Ecore2013Adapter adapter;

    internal RepositoryUnknownContentMapper(
        DataDictionaryImportContext context,
        Iso20022Ecore2013Adapter adapter)
    {
        this.context = context;
        this.adapter = adapter;
    }

    internal bool IsUnknownElement(XmiPassTwoEntity entity)
    {
        if (entity.Depth == 0 || entity.IsElementAttribute)
        {
            return false;
        }

        if (!adapter.IsKnownType(entity.TypeName))
        {
            return true;
        }

        XmiPassTwoEntity? parent = context.FindParent(entity);
        if (parent == null)
        {
            return false;
        }

        if (IsUnknownElement(parent))
        {
            return true;
        }

        return !adapter.IsKnownElementFeature(
            parent.TypeName,
            entity.ElementName.LocalName);
    }

    internal bool IsUnknownAttribute(
        XmiPassTwoEntity entity,
        XmiPassTwoAttribute attribute)
    {
        if (IsUnknownElement(entity))
        {
            return true;
        }

        if (attribute.NamespaceUri.Length > 0)
        {
            return true;
        }

        return !attribute.IsReference &&
            !adapter.IsKnownScalarAttributeFeature(entity.TypeName, attribute.LocalName);
    }

    internal RepositoryUnknownData BuildUnknownElement(
        XmiPassTwoEntity entity,
        string ownerIdentifier,
        RepositoryEntityKind ownerKind)
    {
        RepositoryUnknownData unknown = new RepositoryUnknownData();
        unknown.OwnerIdentifier = ownerIdentifier;
        unknown.OwnerKind = ownerKind;
        unknown.Identifier = entity.Identifier;
        unknown.TypeName = entity.SourceTypeName;
        if (unknown.TypeName.Length == 0)
        {
            unknown.TypeName = entity.TypeName;
        }

        unknown.Name = CopyQualifiedName(entity.ElementName);
        unknown.Value = entity.TextValue;
        unknown.Location = entity.Location;
        unknown.RawContent = BuildRawContentMetadata(entity);
        string childOwnerIdentifier = entity.Identifier;
        if (childOwnerIdentifier.Length == 0)
        {
            childOwnerIdentifier = ownerIdentifier;
        }

        int attributeIndex = 0;
        while (attributeIndex < entity.Attributes.Count)
        {
            XmiPassTwoAttribute attribute = entity.Attributes[attributeIndex];
            unknown.Children.Add(BuildUnknownAttribute(
                entity,
                attribute,
                childOwnerIdentifier,
                RepositoryEntityKind.Unknown));
            attributeIndex++;
        }

        List<XmiPassTwoReference> references = context.GetSourceReferences(entity);
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            unknown.References.Add(context.CopyReference(references[referenceIndex]));
            referenceIndex++;
        }

        List<XmiPassTwoEntity> children = context.GetChildren(entity);
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            XmiPassTwoEntity child = children[childIndex];
            if (!child.IsElementAttribute)
            {
                unknown.Children.Add(BuildUnknownElement(
                    child,
                    childOwnerIdentifier,
                    RepositoryEntityKind.Unknown));
            }

            childIndex++;
        }

        return unknown;
    }

    internal RepositoryUnknownData BuildUnknownAttribute(
        XmiPassTwoEntity entity,
        XmiPassTwoAttribute attribute,
        string ownerIdentifier,
        RepositoryEntityKind ownerKind)
    {
        RepositoryUnknownData unknown = new RepositoryUnknownData();
        unknown.OwnerIdentifier = ownerIdentifier;
        unknown.OwnerKind = ownerKind;
        unknown.Name.NamespaceUri = attribute.NamespaceUri;
        unknown.Name.LocalName = attribute.LocalName;
        unknown.Name.Prefix = attribute.Prefix;
        unknown.IsAttribute = true;
        unknown.Value = attribute.Value;
        unknown.Location = context.CreateAttributeLocation(entity, attribute);
        return unknown;
    }

    internal void AppendUnknownAttributes(
        XmiPassTwoEntity entity,
        List<RepositoryUnknownData> destination)
    {
        int attributeIndex = 0;
        while (attributeIndex < entity.Attributes.Count)
        {
            XmiPassTwoAttribute attribute = entity.Attributes[attributeIndex];
            if (IsUnknownAttribute(entity, attribute))
            {
                destination.Add(BuildUnknownAttribute(
                    entity,
                    attribute,
                    entity.Identifier,
                    entity.Kind));
            }

            attributeIndex++;
        }
    }

    internal RawXmlFragment BuildRawContentMetadata(XmiPassTwoEntity entity)
    {
        RawXmlFragment fragment = new RawXmlFragment();
        fragment.RootName = CopyQualifiedName(entity.ElementName);
        fragment.Location = entity.Location;
        AppendNamespaceDeclaration(
            fragment.NamespaceDeclarations,
            entity.ElementName.Prefix,
            entity.ElementName.NamespaceUri);
        if (entity.SourceTypeName.Length > 0 && entity.SourceTypeNamespaceUri.Length > 0)
        {
            string prefix = string.Empty;
            int separator = entity.SourceTypeName.IndexOf(':');
            if (separator >= 0)
            {
                prefix = entity.SourceTypeName.Substring(0, separator);
            }

            AppendNamespaceDeclaration(
                fragment.NamespaceDeclarations,
                prefix,
                entity.SourceTypeNamespaceUri);
        }

        int attributeIndex = 0;
        while (attributeIndex < entity.Attributes.Count)
        {
            XmiPassTwoAttribute attribute = entity.Attributes[attributeIndex];
            AppendNamespaceDeclaration(
                fragment.NamespaceDeclarations,
                attribute.Prefix,
                attribute.NamespaceUri);
            attributeIndex++;
        }

        return fragment;
    }

    internal void AppendNamespaceDeclaration(
        List<NamespaceDeclarationData> destination,
        string prefix,
        string namespaceUri)
    {
        if (namespaceUri.Length == 0)
        {
            return;
        }

        int declarationIndex = 0;
        while (declarationIndex < destination.Count)
        {
            NamespaceDeclarationData existing = destination[declarationIndex];
            if (existing.Prefix == prefix && existing.NamespaceUri == namespaceUri)
            {
                return;
            }

            declarationIndex++;
        }

        NamespaceDeclarationData declaration = new NamespaceDeclarationData();
        declaration.Prefix = prefix;
        declaration.NamespaceUri = namespaceUri;
        destination.Add(declaration);
    }

    internal QualifiedNameData CopyQualifiedName(QualifiedNameData source)
    {
        QualifiedNameData destination = new QualifiedNameData();
        destination.NamespaceUri = source.NamespaceUri;
        destination.LocalName = source.LocalName;
        destination.Prefix = source.Prefix;
        return destination;
    }
}
