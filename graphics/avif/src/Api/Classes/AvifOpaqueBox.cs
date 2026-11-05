using System;
using System.Buffers.Binary;

namespace avif;

/// <summary>Owns one unsupported nonessential ISO box for property or top-level authoring.</summary>
public class AvifOpaqueBox : IAvifOpaqueBox
{
    byte[]? rawData;
    string? type;
    bool essential;
    AvifResultCode code;

    /// <summary>Creates an opaque property box from its type and payload.</summary>
    /// <param name="type">The unsupported four-character property type.</param>
    /// <param name="payload">The property payload, which is copied.</param>
    /// <param name="essential">Whether understanding the property is required.</param>
    /// <returns>The initialized opaque property.</returns>
    internal static AvifOpaqueBox FromPayload(
        string type,
        byte[] payload,
        bool essential)
    {
        if (payload == null || !IsoBoxType.IsValid(type)
            || type == "uuid" || payload.Length > int.MaxValue - 8)
        {
            AvifOpaqueBox invalid = new AvifOpaqueBox(
                System.Array.Empty<byte>(), essential);
            invalid.code = AvifResultCode.InvalidArgument;
            return invalid;
        }
        byte[] raw = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(raw, (uint)raw.Length);
        raw[4] = (byte)type[0];
        raw[5] = (byte)type[1];
        raw[6] = (byte)type[2];
        raw[7] = (byte)type[3];
        payload.CopyTo(raw, 8);
        return new AvifOpaqueBox(raw, essential);
    }

    /// <summary>Creates an opaque property from one complete ISO box.</summary>
    /// <param name="rawData">The complete property box, which is copied.</param>
    /// <param name="essential">Whether understanding the property is required.</param>
    public AvifOpaqueBox(byte[] rawData, bool essential)
    {
        Initialize(rawData, essential);
    }

    /// <summary>Creates an opaque property from one complete ISO box.</summary>
    /// <returns>The initialized opaque property.</returns>
    internal static AvifOpaqueBox Create(byte[] rawData, bool essential)
    {
        return new AvifOpaqueBox(rawData, essential);
    }

    /// <summary>Creates an opaque property from its type and payload.</summary>
    /// <returns>The initialized opaque property.</returns>
    internal static AvifOpaqueBox Create(
        string type,
        byte[] payload,
        bool essential)
    {
        return FromPayload(type, payload, essential);
    }

    /// <summary>Returns the construction and validation result.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns the opaque four-character box type.</summary>
    /// <returns>The type, or an empty string after failure.</returns>
    public string? GetTypeName()
    {
        return code == AvifResultCode.Success ? type : string.Empty;
    }

    /// <summary>Reports whether the property was marked essential.</summary>
    /// <returns><c>true</c> when the property is essential.</returns>
    public bool IsEssential()
    {
        return essential;
    }

    /// <summary>Returns an independent copy of the complete property box.</summary>
    /// <returns>The raw box bytes, or an empty array after failure.</returns>
    public byte[] GetRawData()
    {
        return code == AvifResultCode.Success
            ? (byte[])(rawData ?? throw new InvalidOperationException("Invalid internal state.")).Clone()
            : System.Array.Empty<byte>();
    }

    void Initialize(byte[] requestedData, bool requestedEssential)
    {
        rawData = requestedData == null
            ? System.Array.Empty<byte>() : (byte[])requestedData.Clone();
        essential = requestedEssential;
        CanonicalPropertyProbe probe =
            CanonicalItemContainerWriter.ProbeProperty(rawData);
        type = probe.Type == null ? string.Empty : probe.Type;
        code = probe.Code;
        if (code == AvifResultCode.Success
            && AvifContainerValidator.IsSupportedProperty(type))
        {
            code = AvifResultCode.InvalidArgument;
        }
    }
}
