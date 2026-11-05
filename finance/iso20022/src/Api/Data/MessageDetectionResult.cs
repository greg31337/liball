using System.Collections.Generic;
using System.IO;

namespace iso20022;

/// <summary>Contains detected message identity, layout, and replayable source data.</summary>
public class MessageDetectionResult
{
    /// <summary>Whether a supported message layout was detected.</summary>
    public bool Success;
    /// <summary>The detected physical message layout.</summary>
    public EnvelopeKind Layout;
    /// <summary>The first physical root name.</summary>
    public QualifiedNameData? RootName;
    /// <summary>The configured wrapper name when a custom wrapper was detected.</summary>
    public QualifiedNameData? WrapperName;
    /// <summary>The detected envelope namespace when present.</summary>
    public NamespaceDescriptor? EnvelopeNamespace;
    /// <summary>The detected header namespace when present.</summary>
    public NamespaceDescriptor? HeaderNamespace;
    /// <summary>The primary payload namespace when present.</summary>
    public NamespaceDescriptor? MessageNamespace;
    /// <summary>The primary payload identifier when present.</summary>
    public MessageIdentifier? MessageIdentifier;
    /// <summary>The message identifier declared by the primary header when present.</summary>
    public MessageIdentifier? DeclaredMessageIdentifier;
    /// <summary>The ordered payload identifiers found in the input.</summary>
    public List<MessageIdentifier> PayloadIdentifiers = new List<MessageIdentifier>();
    /// <summary>The ordered message identifiers declared by headers.</summary>
    public List<MessageIdentifier> DeclaredMessageIdentifiers = new List<MessageIdentifier>();
    /// <summary>Whether an application header precedes its payload in a sibling layout.</summary>
    public bool HeaderPrecedesPayload;
    /// <summary>Whether the supplied stream supported seeking.</summary>
    public bool InputWasSeekable;
    /// <summary>The stream position at which detection began.</summary>
    public long OriginalStreamPosition;
    /// <summary>The original stream or a replacement stream positioned for replay.</summary>
    public Stream? ReplayStream;
    /// <summary>A preserved byte copy of the detected input region.</summary>
    public byte[] PreservedBytes = [];
    /// <summary>Ordered namespace repairs made during lenient detection.</summary>
    public List<NamespaceRepair> NamespaceRepairs = new List<NamespaceRepair>();
    /// <summary>Ordered declaration, fragment, namespace, and wrapper repairs made during lenient detection.</summary>
    public List<MessageRepair> Repairs = new List<MessageRepair>();
    /// <summary>A stable error code when detection failed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when detection failed.</summary>
    public string ErrorMessage = string.Empty;
}
