using System;
using System.Collections.Generic;
using System.Net;

namespace diameter;

/// <summary>
/// Contains the outcome of a Diameter encoding operation.
/// </summary>
public class DiameterEncodeResult
{
    /// <summary>The encoding status.</summary>
    public DiameterCodecStatus Status;

    /// <summary>The required destination size in octets.</summary>
    public int RequiredCount;

    /// <summary>The number of octets written.</summary>
    public int WrittenCount;

    /// <summary>The owned encoded bytes when array output was requested.</summary>
    public byte[] Data = new byte[0];

    /// <summary>The issues reported by the encoder.</summary>
    public List<DiameterCodecIssue> Issues = new List<DiameterCodecIssue>();
}

/// <summary>
/// Contains the outcome of a Diameter message decoding operation.
/// </summary>
public class DiameterDecodeResult
{
    /// <summary>The decoding status.</summary>
    public DiameterCodecStatus Status;

    /// <summary>The minimum total input size required to continue decoding.</summary>
    public int RequiredCount;

    /// <summary>The number of input octets consumed.</summary>
    public int ConsumedCount;

    /// <summary>The decoded message, or <c>null</c> when no message was produced.</summary>
    public DiameterMessage? Message;

    /// <summary>The issues reported by the decoder.</summary>
    public List<DiameterCodecIssue> Issues = new List<DiameterCodecIssue>();
}

/// <summary>
/// Contains the outcome of a Diameter AVP decoding operation.
/// </summary>
public class DiameterAvpDecodeResult
{
    /// <summary>The decoding status.</summary>
    public DiameterCodecStatus Status;

    /// <summary>The minimum total input size required to continue decoding.</summary>
    public int RequiredCount;

    /// <summary>The number of input octets consumed, including AVP padding.</summary>
    public int ConsumedCount;

    /// <summary>The decoded AVP, or <c>null</c> when no AVP was produced.</summary>
    public DiameterAvp? Avp;

    /// <summary>The issues reported by the AVP decoder.</summary>
    public List<DiameterCodecIssue> Issues = new List<DiameterCodecIssue>();
}

/// <summary>
/// Contains complete frames and retained input from an incremental framing operation.
/// </summary>
public class DiameterFramingResult
{
    /// <summary>The framing status.</summary>
    public DiameterCodecStatus Status;

    /// <summary>The minimum total input size required to complete the next frame.</summary>
    public int RequiredCount;

    /// <summary>The number of input octets consumed into complete frames.</summary>
    public int ConsumedCount;

    /// <summary>The complete owned Diameter frame byte arrays.</summary>
    public List<byte[]> Frames = new List<byte[]>();

    /// <summary>The owned input bytes retained for the next framing operation.</summary>
    public byte[] Remainder = new byte[0];

    /// <summary>Indicates that the input stream has ended.</summary>
    public bool EndOfStream;

    /// <summary>Indicates that end of stream occurred without a partial frame.</summary>
    public bool CleanEndOfStream;

    /// <summary>The issues reported by the framer.</summary>
    public List<DiameterCodecIssue> Issues = new List<DiameterCodecIssue>();
}

/// <summary>
/// Describes a structured codec issue at a stable byte offset.
/// </summary>
public class DiameterCodecIssue
{
    /// <summary>The issue severity.</summary>
    public DiameterIssueSeverity Severity;

    /// <summary>The status associated with the issue.</summary>
    public DiameterCodecStatus Status;

    /// <summary>The zero-based byte offset associated with the issue.</summary>
    public int Offset;

    /// <summary>The stable machine-readable issue code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable issue description.</summary>
    public string Message = string.Empty;
}

/// <summary>
/// Contains the outcome and typed storage for a Diameter AVP value operation.
/// </summary>
/// <remarks>
/// A codec populates only the value field appropriate to the requested AVP type.
/// </remarks>
public class DiameterValueResult
{
    /// <summary>The value operation status.</summary>
    public DiameterCodecStatus Status;

    /// <summary>The minimum total input or output size required to continue.</summary>
    public int RequiredCount;

    /// <summary>The number of input octets consumed.</summary>
    public int ConsumedCount;

    /// <summary>The number of output octets written.</summary>
    public int WrittenCount;

    /// <summary>The owned octet-string or encoded value bytes.</summary>
    public byte[] Data = new byte[0];

    /// <summary>The decoded signed 32-bit value.</summary>
    public int Integer32;

    /// <summary>The decoded signed 64-bit value.</summary>
    public long Integer64;

    /// <summary>The decoded unsigned 32-bit value.</summary>
    public uint Unsigned32;

    /// <summary>The decoded unsigned 64-bit value.</summary>
    public ulong Unsigned64;

    /// <summary>The decoded single-precision floating-point value.</summary>
    public float Float32;

    /// <summary>The decoded double-precision floating-point value.</summary>
    public double Float64;

    /// <summary>The decoded text or filter-rule value.</summary>
    public string Text = string.Empty;

    /// <summary>The decoded IP address, or <c>null</c> for another value type.</summary>
    public IPAddress? Address;

    /// <summary>The decoded time value.</summary>
    public DateTimeOffset Time;

    /// <summary>The decoded Diameter identity, or <c>null</c> for another value type.</summary>
    public DiameterIdentity? Identity;

    /// <summary>The decoded Diameter URI, or <c>null</c> for another value type.</summary>
    public DiameterUri? Uri;

    /// <summary>The decoded ordered AVPs for a Grouped value.</summary>
    public List<DiameterAvp> Avps = new List<DiameterAvp>();

    /// <summary>The issues reported by the value codec.</summary>
    public List<DiameterCodecIssue> Issues = new List<DiameterCodecIssue>();
}
