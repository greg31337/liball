using System;

namespace uuid;

/// <summary>Defines operations specific to an ObjectId value.</summary>
public interface IObjectIdValue
{
    /// <summary>Returns copied bytes.</summary>
    byte[] ToBytes();
    /// <summary>Copies bytes.</summary>
    void CopyTo(byte[]? destination);
    /// <summary>Returns Unix seconds.</summary>
    long UnixSeconds();
    /// <summary>Returns the timestamp as date and time.</summary>
    DateTimeOffset Time();
    /// <summary>Formats lowercase hexadecimal text.</summary>
    string ToHexString();
    /// <summary>Formats uppercase hexadecimal text.</summary>
    string ToUpperHexString();
    /// <summary>Tests value equality.</summary>
    bool EqualsValue(ObjectIdValue? other);
    /// <summary>Tests byte equality.</summary>
    bool EqualsBytes(byte[]? bytes);
    /// <summary>Compares to another ObjectId.</summary>
    int CompareTo(ObjectIdValue? other);
}
