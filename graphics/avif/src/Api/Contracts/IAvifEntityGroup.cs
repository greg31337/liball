using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifEntityGroup"/>.</summary>
public interface IAvifEntityGroup
{
    /// <summary>Returns the interpreted group kind.</summary>
    /// <returns>The group kind.</returns>
    AvifEntityGroupKind GetKind();

    /// <summary>Returns the original four-character grouping type.</summary>
    /// <returns>The grouping type.</returns>
    string GetTypeName();

    /// <summary>Returns the group FullBox version.</summary>
    /// <returns>The version.</returns>
    int GetVersion();

    /// <summary>Returns the group FullBox flags.</summary>
    /// <returns>The flags.</returns>
    int GetFlags();

    /// <summary>Returns the group identifier.</summary>
    /// <returns>The group identifier.</returns>
    uint GetGroupId();

    /// <summary>Returns the number of entity identifiers.</summary>
    /// <returns>The entity count.</returns>
    int GetEntityCount();

    /// <summary>Returns one entity identifier.</summary>
    /// <param name="index">The zero-based entity index.</param>
    /// <returns>The identifier, or zero for an invalid index.</returns>
    uint GetEntityId(int index);

    /// <summary>Reports whether opaque group bytes were retained.</summary>
    /// <returns><c>true</c> when retained bytes are available.</returns>
    bool HasRawData();

    /// <summary>Returns a copy of retained opaque group bytes.</summary>
    /// <returns>The retained bytes.</returns>
    byte[] GetRawData();
}
