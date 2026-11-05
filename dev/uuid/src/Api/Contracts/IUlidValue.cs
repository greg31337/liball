using System;

namespace uuid;

/// <summary>Defines operations specific to a ULID value.</summary>
public interface IUlidValue
{
    /// <summary>Returns copied bytes.</summary>
    byte[] ToBytes();
    /// <summary>Returns UUID-compatible bytes.</summary>
    byte[] ToUuidBytes();
    /// <summary>Copies bytes.</summary>
    void CopyTo(byte[]? destination);
    /// <summary>Returns Unix milliseconds.</summary>
    long UnixMilliseconds();
    /// <summary>Returns the timestamp as date and time.</summary>
    DateTimeOffset Time();
    /// <summary>Returns copied randomness bytes.</summary>
    byte[] RandomnessBytes();
    /// <summary>Tests value equality.</summary>
    bool EqualsValue(UlidValue? other);
    /// <summary>Tests byte equality.</summary>
    bool EqualsBytes(byte[]? bytes);
    /// <summary>Compares to another ULID.</summary>
    int CompareTo(UlidValue? other);
}
