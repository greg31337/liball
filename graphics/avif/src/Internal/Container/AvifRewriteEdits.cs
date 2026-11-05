using System.Collections.Generic;

namespace avif;

internal class AvifRewriteMetadataAddition
{
    internal uint ItemId;
    internal List<uint> TargetItemIds;
    internal AvifMetadataKind Kind;
    internal string? ItemType;
    internal string? ContentType;
    internal string? ContentEncoding;
    internal string? ItemUriType;
    internal byte[] Data;

    internal AvifRewriteMetadataAddition(
        uint itemId,
        uint[] targetItemIds,
        AvifMetadata metadata)
    {
        ItemId = itemId;
        TargetItemIds = new List<uint>(targetItemIds);
        Kind = metadata.GetKind();
        ItemType = metadata.GetItemType();
        ContentType = metadata.GetContentType();
        ContentEncoding = metadata.GetContentEncoding();
        ItemUriType = metadata.GetUriType();
        Data = metadata.GetData();
    }
}

internal class AvifRewriteMetadataAssociation
{
    internal uint MetadataItemId;
    internal uint TargetItemId;

    internal AvifRewriteMetadataAssociation(
        uint metadataItemId,
        uint targetItemId)
    {
        MetadataItemId = metadataItemId;
        TargetItemId = targetItemId;
    }
}

internal class AvifRewriteMetadataObuAddition
{
    internal uint ItemId;
    internal byte[] Data;

    internal AvifRewriteMetadataObuAddition(
        uint itemId,
        byte[] data)
    {
        ItemId = itemId;
        Data = (byte[])data.Clone();
    }
}

internal class AvifRewriteMetadataRemoval
{
    internal uint TargetItemId;
    internal AvifMetadataKind Kind;

    internal AvifRewriteMetadataRemoval(
        uint targetItemId,
        AvifMetadataKind kind)
    {
        TargetItemId = targetItemId;
        Kind = kind;
    }
}

internal class AvifRewriteTransformEdit
{
    internal uint ItemId;
    internal AvifTransform Transform;

    internal AvifRewriteTransformEdit(
        uint itemId,
        AvifTransform transform)
    {
        ItemId = itemId;
        Transform = AvifTransform.CreateCopy(transform);
    }
}

internal class AvifRewritePropertyAddition
{
    internal uint ItemId;
    internal byte[] RawProperty;

    internal AvifRewritePropertyAddition(
        uint itemId,
        byte[] rawProperty)
    {
        ItemId = itemId;
        RawProperty = (byte[])rawProperty.Clone();
    }
}

internal class AvifRewriteEdits
{
    internal bool HasPrimaryItem;
    internal uint PrimaryItemId;
    internal List<AvifRewriteMetadataAddition> MetadataAdditions;
    internal List<AvifRewriteMetadataRemoval> MetadataRemovals;
    internal List<AvifRewriteMetadataAssociation> MetadataAssociations;
    internal List<AvifRewriteMetadataObuAddition> MetadataObuAdditions;
    internal List<AvifRewriteTransformEdit> TransformEdits;
    internal List<CanonicalItemPayload> ItemAdditions;
    internal List<AvifRewritePropertyAddition> PropertyAdditions;
    internal List<CanonicalItemReference> ReferenceAdditions;
    internal List<CanonicalEntityGroup> GroupAdditions;
    internal List<byte[]> TopLevelBoxAdditions;
    internal List<string> AddedBrands;
    internal List<string> RemovedBrands;

    internal AvifRewriteEdits()
    {
        MetadataAdditions = new List<AvifRewriteMetadataAddition>();
        MetadataRemovals = new List<AvifRewriteMetadataRemoval>();
        MetadataAssociations = new List<AvifRewriteMetadataAssociation>();
        MetadataObuAdditions = new List<AvifRewriteMetadataObuAddition>();
        TransformEdits = new List<AvifRewriteTransformEdit>();
        ItemAdditions = new List<CanonicalItemPayload>();
        PropertyAdditions = new List<AvifRewritePropertyAddition>();
        ReferenceAdditions = new List<CanonicalItemReference>();
        GroupAdditions = new List<CanonicalEntityGroup>();
        TopLevelBoxAdditions = new List<byte[]>();
        AddedBrands = new List<string>();
        RemovedBrands = new List<string>();
    }

    internal bool HasChanges()
    {
        return HasPrimaryItem
            || MetadataAdditions.Count != 0
            || MetadataRemovals.Count != 0
            || MetadataAssociations.Count != 0
            || MetadataObuAdditions.Count != 0
            || TransformEdits.Count != 0
            || ItemAdditions.Count != 0
            || PropertyAdditions.Count != 0
            || ReferenceAdditions.Count != 0
            || GroupAdditions.Count != 0
            || TopLevelBoxAdditions.Count != 0
            || AddedBrands.Count != 0
            || RemovedBrands.Count != 0;
    }
}
