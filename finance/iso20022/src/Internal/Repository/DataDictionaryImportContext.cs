using System;
using System.Collections.Generic;

namespace iso20022;

internal class DataDictionaryImportContext
{
    internal XmiPassTwoResult Graph;
    Dictionary<int, List<XmiPassTwoEntity>> childrenByParentOrder =
        new Dictionary<int, List<XmiPassTwoEntity>>();
    Dictionary<int, List<XmiPassTwoReference>> referencesBySourceOrder =
        new Dictionary<int, List<XmiPassTwoReference>>();
    Dictionary<string, List<XmiPassTwoReference>> referencesByTargetIdentifier =
        new Dictionary<string, List<XmiPassTwoReference>>(StringComparer.Ordinal);

    internal DataDictionaryImportContext(XmiPassTwoResult graph)
    {
        if (graph == null)
        {
            throw new ArgumentNullException("graph");
        }

        Graph = graph;
        IndexChildren();
        IndexReferences();
    }

    internal void IndexChildren()
    {
        int entityIndex = 0;
        while (entityIndex < Graph.Entities.Count)
        {
            XmiPassTwoEntity entity = Graph.Entities[entityIndex];
            if (entity.ParentDocumentOrder >= 0)
            {
                if (!childrenByParentOrder.ContainsKey(entity.ParentDocumentOrder))
                {
                    childrenByParentOrder.Add(
                        entity.ParentDocumentOrder,
                        new List<XmiPassTwoEntity>());
                }

                childrenByParentOrder[entity.ParentDocumentOrder].Add(entity);
            }

            entityIndex++;
        }
    }

    internal void IndexReferences()
    {
        int referenceIndex = 0;
        while (referenceIndex < Graph.Relationships.Count)
        {
            XmiPassTwoReference reference = Graph.Relationships[referenceIndex];
            if (!referencesBySourceOrder.ContainsKey(reference.SourceDocumentOrder))
            {
                referencesBySourceOrder.Add(
                    reference.SourceDocumentOrder,
                    new List<XmiPassTwoReference>());
            }

            referencesBySourceOrder[reference.SourceDocumentOrder].Add(reference);
            if (reference.TargetIdentifier.Length > 0)
            {
                if (!referencesByTargetIdentifier.ContainsKey(reference.TargetIdentifier))
                {
                    referencesByTargetIdentifier.Add(
                        reference.TargetIdentifier,
                        new List<XmiPassTwoReference>());
                }

                referencesByTargetIdentifier[reference.TargetIdentifier].Add(reference);
            }

            referenceIndex++;
        }
    }

    internal List<XmiPassTwoEntity> GetChildren(XmiPassTwoEntity entity)
    {
        if (childrenByParentOrder.ContainsKey(entity.DocumentOrder))
        {
            return childrenByParentOrder[entity.DocumentOrder];
        }

        return new List<XmiPassTwoEntity>();
    }

    internal List<XmiPassTwoReference> GetSourceReferences(XmiPassTwoEntity entity)
    {
        if (referencesBySourceOrder.ContainsKey(entity.DocumentOrder))
        {
            return referencesBySourceOrder[entity.DocumentOrder];
        }

        return new List<XmiPassTwoReference>();
    }

    internal List<XmiPassTwoReference> GetTargetReferences(string identifier)
    {
        if (referencesByTargetIdentifier.ContainsKey(identifier))
        {
            return referencesByTargetIdentifier[identifier];
        }

        return new List<XmiPassTwoReference>();
    }

    internal XmiPassTwoReference? FindFirstReference(XmiPassTwoEntity entity, string propertyName)
    {
        List<XmiPassTwoReference> references = GetSourceReferences(entity);
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            XmiPassTwoReference reference = references[referenceIndex];
            if (reference.PropertyName == propertyName)
            {
                return reference;
            }

            referenceIndex++;
        }

        return null;
    }

    internal XmiPassTwoEntity? FindParent(XmiPassTwoEntity entity)
    {
        if (entity.ParentDocumentOrder < 0 || entity.ParentDocumentOrder >= Graph.Entities.Count)
        {
            return null;
        }

        return Graph.Entities[entity.ParentDocumentOrder];
    }

    internal RepositoryReference CopyReference(XmiPassTwoReference source)
    {
        RepositoryReference destination = new RepositoryReference();
        destination.SourceIdentifier = source.SourceIdentifier;
        destination.SourceKind = source.SourceKind;
        destination.PropertyName = source.PropertyName;
        destination.Position = source.Position;
        destination.TargetIdentifier = source.TargetIdentifier;
        destination.TargetKind = source.TargetKind;
        destination.RawValue = source.RawValue;
        destination.IsContainment = source.IsContainment;
        destination.IsResolved = source.IsResolved;
        destination.ErrorCode = source.ErrorCode;
        destination.ErrorMessage = source.ErrorMessage;
        destination.Location = CreateReferenceLocation(source);
        return destination;
    }

    internal RepositoryReference CreateReverseReference(
        XmiPassTwoEntity owner,
        XmiPassTwoReference source,
        string propertyName)
    {
        RepositoryReference destination = new RepositoryReference();
        destination.SourceIdentifier = owner.Identifier;
        destination.SourceKind = owner.Kind;
        destination.PropertyName = propertyName;
        destination.Position = source.Position;
        destination.TargetIdentifier = source.SourceIdentifier;
        destination.TargetKind = source.SourceKind;
        destination.RawValue = source.RawValue;
        destination.IsContainment = false;
        destination.IsResolved = source.SourceIdentifier.Length > 0;
        destination.Location = CreateReferenceLocation(source);
        return destination;
    }

    internal RepositoryReference CreateParentReference(
        XmiPassTwoEntity source,
        XmiPassTwoEntity target,
        string propertyName)
    {
        RepositoryReference reference = new RepositoryReference();
        reference.SourceIdentifier = source.Identifier;
        reference.SourceKind = source.Kind;
        reference.PropertyName = propertyName;
        reference.TargetIdentifier = target.Identifier;
        reference.TargetKind = target.Kind;
        reference.IsResolved = target.Identifier.Length > 0;
        reference.Location = source.Location;
        return reference;
    }

    internal RepositoryReference CreatePartitionReference(
        XmiPassTwoEntity source,
        string propertyName)
    {
        RepositoryReference reference = new RepositoryReference();
        reference.SourceIdentifier = source.Identifier;
        reference.SourceKind = source.Kind;
        reference.PropertyName = propertyName;
        reference.TargetIdentifier = source.TopLevelPartitionIdentifier;
        reference.TargetKind = source.TopLevelPartitionKind;
        reference.IsResolved = source.TopLevelPartitionIdentifier.Length > 0;
        reference.Location = source.Location;
        return reference;
    }

    internal SourceLocation CreateReferenceLocation(XmiPassTwoReference reference)
    {
        SourceLocation location = new SourceLocation();
        if (reference.SourceDocumentOrder >= 0 &&
            reference.SourceDocumentOrder < Graph.Entities.Count)
        {
            location.SourceName = Graph.Entities[reference.SourceDocumentOrder].Location.SourceName;
        }

        location.HasLineInformation = reference.HasLineInformation;
        location.LineNumber = reference.LineNumber;
        location.ColumnNumber = reference.ColumnNumber;
        return location;
    }

    internal SourceLocation CreateAttributeLocation(
        XmiPassTwoEntity entity,
        XmiPassTwoAttribute attribute)
    {
        SourceLocation location = new SourceLocation();
        location.SourceName = entity.Location.SourceName;
        location.HasLineInformation = attribute.HasLineInformation;
        location.LineNumber = attribute.LineNumber;
        location.ColumnNumber = attribute.ColumnNumber;
        return location;
    }
}
