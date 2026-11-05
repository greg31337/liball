using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Requests a deterministic whole-message or body checksum and optional expected-value verification.</summary>
public class ChecksumRequest
{
    /// <summary>The raw XML bytes used for byte or canonical XML checksum modes.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The generic message document used when supplied instead of raw XML.</summary>
    public MessageDocument? Document;
    /// <summary>The generated strongly typed model used for semantic checksum mode.</summary>
    public object? Model;
    /// <summary>The selected complete-message or message-body scope.</summary>
    public ChecksumScope Scope = ChecksumScope.WholeMessage;
    /// <summary>The selected byte, canonical XML, or semantic representation.</summary>
    public MessageEqualityMode EqualityMode = MessageEqualityMode.CanonicalXml;
    /// <summary>The hash algorithm name.</summary>
    public string Algorithm = "SHA-256";
    /// <summary>The expected checksum bytes, or an empty array when only calculating.</summary>
    public byte[] ExpectedChecksum = Array.Empty<byte>();
    /// <summary>The semantic paths whose values form uniqueness keys.</summary>
    public List<XmlPathData> KeyPaths = new List<XmlPathData>();
    /// <summary>Whether duplicate semantic keys make the operation invalid.</summary>
    public bool RejectDuplicateKeys = true;
    /// <summary>Whether duplicate key values are omitted from diagnostics.</summary>
    public bool RedactDuplicateKeys = true;
    /// <summary>The resource bounds applied during parsing, canonicalization, and hashing.</summary>
    public ResourceLimits Limits = new ResourceLimits();
}
