using System.Collections.Generic;

namespace avif;

/// <summary>
/// Describes a parsed group of image items or other entity identifiers.
/// </summary>
public class AvifEntityGroup : IAvifEntityGroup
{
    AvifEntityGroupKind kind;
    string type;
    int version;
    int flags;
    uint groupId;
    List<uint> entityIds;
    byte[] rawData;

    internal AvifEntityGroup(
        AvifEntityGroupKind kind,
        string type,
        int version,
        int flags,
        uint groupId,
        List<uint> entityIds,
        byte[] rawData)
    {
        this.kind = kind;
        this.type = type;
        this.version = version;
        this.flags = flags;
        this.groupId = groupId;
        this.entityIds = new List<uint>(entityIds);
        this.rawData = (byte[])rawData.Clone();
    }

    /// <summary>Returns the interpreted group kind.</summary>
    /// <returns>The group kind.</returns>
    public AvifEntityGroupKind GetKind()
    {
        return kind;
    }

    /// <summary>Returns the original four-character grouping type.</summary>
    /// <returns>The grouping type.</returns>
    public string GetTypeName()
    {
        return type;
    }

    /// <summary>Returns the group FullBox version.</summary>
    /// <returns>The version.</returns>
    public int GetVersion()
    {
        return version;
    }

    /// <summary>Returns the group FullBox flags.</summary>
    /// <returns>The flags.</returns>
    public int GetFlags()
    {
        return flags;
    }

    /// <summary>Returns the group identifier.</summary>
    /// <returns>The group identifier.</returns>
    public uint GetGroupId()
    {
        return groupId;
    }

    /// <summary>Returns the number of entity identifiers.</summary>
    /// <returns>The entity count.</returns>
    public int GetEntityCount()
    {
        return entityIds.Count;
    }

    /// <summary>Returns one entity identifier.</summary>
    /// <param name="index">The zero-based entity index.</param>
    /// <returns>The identifier, or zero for an invalid index.</returns>
    public uint GetEntityId(int index)
    {
        if (index < 0 || index >= entityIds.Count)
        {
            return 0;
        }

        return entityIds[index];
    }

    /// <summary>Reports whether opaque group bytes were retained.</summary>
    /// <returns><c>true</c> when retained bytes are available.</returns>
    public bool HasRawData()
    {
        return rawData.Length != 0;
    }

    /// <summary>Returns a copy of retained opaque group bytes.</summary>
    /// <returns>The retained bytes.</returns>
    public byte[] GetRawData()
    {
        return (byte[])rawData.Clone();
    }
}
