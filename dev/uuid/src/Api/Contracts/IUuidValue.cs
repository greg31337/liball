using System;

namespace uuid;

/// <summary>Defines operations specific to a UUID value.</summary>
public interface IUuidValue
{
    /// <summary>Returns copied network-order bytes.</summary>
    byte[] ToBytes();
    /// <summary>Copies network-order bytes.</summary>
    void CopyTo(byte[]? destination);
    /// <summary>Formats uppercase canonical text.</summary>
    string ToUpperString();
    /// <summary>Formats lowercase compact text.</summary>
    string ToCompactString();
    /// <summary>Formats uppercase compact text.</summary>
    string ToCompactUpperString();
    /// <summary>Converts to a .NET GUID.</summary>
    Guid ToGuid();
    /// <summary>Returns the raw version number.</summary>
    int VersionNumber();
    /// <summary>Returns the recognized UUID version.</summary>
    UuidVersion Version();
    /// <summary>Returns the UUID variant.</summary>
    UuidVariant Variant();
    /// <summary>Checks RFC 9562 variant bits.</summary>
    bool HasRfc9562Variant();
    /// <summary>Checks for the Nil UUID.</summary>
    bool IsNil();
    /// <summary>Checks for the Max UUID.</summary>
    bool IsMax();
    /// <summary>Returns Unix milliseconds.</summary>
    long UnixMilliseconds();
    /// <summary>Returns the timestamp as date and time.</summary>
    DateTimeOffset Time();
    /// <summary>Tests value equality.</summary>
    bool EqualsValue(UuidValue? other);
    /// <summary>Tests byte equality.</summary>
    bool EqualsBytes(byte[]? bytes);
    /// <summary>Compares to another UUID.</summary>
    int CompareTo(UuidValue? other);
}
