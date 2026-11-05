using System;

namespace uuid;

/// <summary>Defines operations specific to a KSUID value.</summary>
public interface IKsuidValue
{
    /// <summary>Returns copied bytes.</summary>
    byte[] ToBytes();
    /// <summary>Copies bytes.</summary>
    void CopyTo(byte[]? destination);
    /// <summary>Returns seconds since the KSUID epoch.</summary>
    long AdjustedSeconds();
    /// <summary>Returns Unix seconds.</summary>
    long UnixSeconds();
    /// <summary>Returns the timestamp as date and time.</summary>
    DateTimeOffset Time();
    /// <summary>Returns copied payload bytes.</summary>
    byte[] PayloadBytes();
    /// <summary>Tests value equality.</summary>
    bool EqualsValue(KsuidValue? other);
    /// <summary>Tests byte equality.</summary>
    bool EqualsBytes(byte[]? bytes);
    /// <summary>Compares to another KSUID.</summary>
    int CompareTo(KsuidValue? other);
}
