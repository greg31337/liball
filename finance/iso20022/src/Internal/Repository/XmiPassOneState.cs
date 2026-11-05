using System;
using System.Collections.Generic;
using System.Xml;

namespace iso20022;

internal class XmiPassOneState
{
    Iso20022Ecore2013Adapter adapter;
    XmiPassOneOptions options;
    XmiPassOneResult result = new XmiPassOneResult();
    List<string> typeNames = new List<string>();
    List<string> identifiers = new List<string>();
    List<string> partitionIdentifiers = new List<string>();
    List<RepositoryEntityKind> partitionKinds = new List<RepositoryEntityKind>();

    internal XmiPassOneState(Iso20022Ecore2013Adapter adapter, XmiPassOneOptions options)
    {
        this.adapter = adapter;
        this.options = options;
    }

    internal void RecordElement(XmlReader reader, string sourceName)
    {
        int depth = reader.Depth;
        if (depth + 1 > options.MaximumDepth)
        {
            throw new XmlException("The XMI element depth exceeds MaximumDepth.");
        }

        if (reader.AttributeCount > options.MaximumAttributesPerElement)
        {
            throw new XmlException("An XMI element exceeds MaximumAttributesPerElement.");
        }

        result.ElementCount++;
        if (result.ElementCount > options.MaximumElementCount)
        {
            throw new XmlException("The XMI document exceeds MaximumElementCount.");
        }

        EnsureDepth(depth);
        string typeName = ResolveTypeName(reader, depth);
        string? identifierValue = reader.GetAttribute("id", Iso20022Namespaces.Xmi);
        string identifier = string.Empty;
        if (identifierValue != null)
        {
            identifier = identifierValue;
        }

        string parentIdentifier = ResolveParentIdentifier(depth);
        string partitionIdentifier = ResolvePartitionIdentifier(depth);
        RepositoryEntityKind partitionKind = ResolvePartitionKind(depth);
        bool isTopLevelPartition = depth == 1 &&
            typeNames[0] == "Repository" &&
            adapter.IsTopLevelPartitionType(typeName);
        RepositoryEntityKind kind = adapter.ResolveEntityKind(typeName);
        if (isTopLevelPartition)
        {
            partitionIdentifier = identifier;
            partitionKind = kind;
        }

        typeNames[depth] = typeName;
        identifiers[depth] = identifier;
        partitionIdentifiers[depth] = partitionIdentifier;
        partitionKinds[depth] = partitionKind;
        if (identifier.Length == 0)
        {
            result.UnidentifiedElementCount++;
            return;
        }

        result.IdentifiedElementCount++;
        if (result.IdentifiedElementCount > options.MaximumIdentifierCount)
        {
            throw new XmlException("The XMI identifier index exceeds MaximumIdentifierCount.");
        }

        XmiPassOneEntry entry = CreateEntry(
            reader,
            sourceName,
            identifier,
            typeName,
            kind,
            parentIdentifier,
            partitionIdentifier,
            partitionKind,
            isTopLevelPartition,
            depth);
        RecordIdentifier(entry);
    }

    internal XmiPassOneResult CompleteIndex()
    {
        if (result.Repository == null)
        {
            throw new XmlException("The XMI document does not contain an identified Repository root.");
        }

        return result;
    }

    internal void EnsureDepth(int depth)
    {
        while (typeNames.Count <= depth)
        {
            typeNames.Add(string.Empty);
            identifiers.Add(string.Empty);
            partitionIdentifiers.Add(string.Empty);
            partitionKinds.Add(RepositoryEntityKind.Unknown);
        }

        int removeIndex = typeNames.Count - 1;
        while (removeIndex > depth)
        {
            typeNames.RemoveAt(removeIndex);
            identifiers.RemoveAt(removeIndex);
            partitionIdentifiers.RemoveAt(removeIndex);
            partitionKinds.RemoveAt(removeIndex);
            removeIndex--;
        }
    }

    internal string ResolveTypeName(XmlReader reader, int depth)
    {
        if (depth == 0)
        {
            if (reader.LocalName != "Repository" || reader.NamespaceURI != adapter.GetMetamodelNamespace())
            {
                throw new XmlException("The XMI root does not match the selected repository adapter.");
            }

            return "Repository";
        }

        string? xsiTypeValue = reader.GetAttribute("type", Iso20022Namespaces.XmlSchemaInstance);
        string xsiType = string.Empty;
        if (xsiTypeValue != null)
        {
            xsiType = xsiTypeValue;
        }

        string parentTypeName = typeNames[depth - 1];
        string typeName = adapter.ResolveContainedType(parentTypeName, reader.LocalName, xsiType);
        if (typeName.Length == 0 && adapter.IsKnownType(reader.LocalName))
        {
            typeName = adapter.NormalizeTypeName(reader.LocalName);
        }

        return typeName;
    }

    internal string ResolveParentIdentifier(int depth)
    {
        if (depth == 0)
        {
            return string.Empty;
        }

        return identifiers[depth - 1];
    }

    internal string ResolvePartitionIdentifier(int depth)
    {
        if (depth == 0)
        {
            return string.Empty;
        }

        return partitionIdentifiers[depth - 1];
    }

    internal RepositoryEntityKind ResolvePartitionKind(int depth)
    {
        if (depth == 0)
        {
            return RepositoryEntityKind.Unknown;
        }

        return partitionKinds[depth - 1];
    }

    internal XmiPassOneEntry CreateEntry(
        XmlReader reader,
        string sourceName,
        string identifier,
        string typeName,
        RepositoryEntityKind kind,
        string parentIdentifier,
        string partitionIdentifier,
        RepositoryEntityKind partitionKind,
        bool isTopLevelPartition,
        int depth)
    {
        XmiPassOneEntry entry = new XmiPassOneEntry();
        entry.Identifier = identifier;
        entry.TypeName = typeName;
        entry.Kind = kind;
        entry.ElementName.NamespaceUri = reader.NamespaceURI;
        entry.ElementName.LocalName = reader.LocalName;
        entry.ElementName.Prefix = reader.Prefix;
        entry.Location = CaptureLocation(reader, sourceName);
        entry.DocumentOrder = result.ElementCount - 1;
        entry.Depth = depth;
        entry.ParentIdentifier = parentIdentifier;
        entry.TopLevelPartitionIdentifier = partitionIdentifier;
        entry.TopLevelPartitionKind = partitionKind;
        entry.IsTopLevelPartition = isTopLevelPartition;
        return entry;
    }

    internal void RecordIdentifier(XmiPassOneEntry entry)
    {
        result.Entries.Add(entry);
        if (result.EntriesByIdentifier.ContainsKey(entry.Identifier))
        {
            entry.IsDuplicate = true;
            if (result.Duplicates.Count >= options.MaximumDuplicateCount)
            {
                throw new XmlException("The duplicate XMI identifier collection exceeds MaximumDuplicateCount.");
            }

            XmiDuplicateIdentifier duplicate = new XmiDuplicateIdentifier();
            duplicate.Identifier = entry.Identifier;
            duplicate.FirstEntry = result.EntriesByIdentifier[entry.Identifier];
            duplicate.DuplicateEntry = entry;
            result.Duplicates.Add(duplicate);
            return;
        }

        result.EntriesByIdentifier.Add(entry.Identifier, entry);
        if (entry.Depth == 0)
        {
            result.Repository = entry;
        }

        if (entry.IsTopLevelPartition && entry.Kind == RepositoryEntityKind.DataDictionary)
        {
            result.DataDictionary = entry;
        }

        if (entry.IsTopLevelPartition && entry.Kind == RepositoryEntityKind.BusinessProcessCatalogue)
        {
            result.BusinessProcessCatalogue = entry;
        }
    }

    internal SourceLocation CaptureLocation(XmlReader reader, string sourceName)
    {
        SourceLocation location = new SourceLocation();
        location.SourceName = sourceName;
        IXmlLineInfo lineInformation = (IXmlLineInfo)reader;
        if (lineInformation.HasLineInfo())
        {
            location.HasLineInformation = true;
            location.LineNumber = lineInformation.LineNumber;
            location.ColumnNumber = lineInformation.LinePosition;
        }

        return location;
    }
}
