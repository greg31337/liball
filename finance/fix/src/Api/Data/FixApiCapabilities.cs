namespace fix;

/// <summary>Describes the enum-defined capabilities exposed by a FIX implementation.</summary>
public class FixApiCapabilities
{
    /// <summary>The supported FIX field types.</summary>
    public FixFieldType[] FieldTypes = [];
    /// <summary>The supported dictionary member kinds.</summary>
    public FixMemberKind[] MemberKinds = [];
    /// <summary>The supported presence modes.</summary>
    public FixPresence[] PresenceModes = [];
    /// <summary>The decode outcomes the implementation can report.</summary>
    public DecodeStatus[] DecodeStatuses = [];
    /// <summary>The encode outcomes the implementation can report.</summary>
    public EncodeStatus[] EncodeStatuses = [];
    /// <summary>The framing outcomes the implementation can report.</summary>
    public FrameStatus[] FrameStatuses = [];
    /// <summary>The validation severities the implementation can report.</summary>
    public ValidationSeverity[] ValidationSeverities = [];
    /// <summary>The supported validation layers.</summary>
    public ValidationLayer[] ValidationLayers = [];
    /// <summary>The supported session roles.</summary>
    public SessionRole[] SessionRoles = [];
    /// <summary>The session states the implementation can report.</summary>
    public SessionState[] SessionStates = [];
    /// <summary>The session event kinds the implementation can report.</summary>
    public SessionEventKind[] SessionEventKinds = [];
    /// <summary>The supported session reset modes.</summary>
    public SessionResetMode[] SessionResetModes = [];
    /// <summary>The supported application message dispositions.</summary>
    public MessageDisposition[] MessageDispositions = [];
    /// <summary>The supported log directions.</summary>
    public LogDirection[] LogDirections = [];
    /// <summary>The transport states the implementation can report.</summary>
    public TransportState[] TransportStates = [];
    /// <summary>The supported Orchestra schema versions.</summary>
    public OrchestraVersion[] OrchestraVersions = [];
    /// <summary>The supported Orchestra rule kinds.</summary>
    public OrchestraRuleKind[] OrchestraRuleKinds = [];
    /// <summary>The supported generation targets.</summary>
    public GenerationTarget[] GenerationTargets = [];
    /// <summary>The supported binary encoding types.</summary>
    public BinaryEncodingType[] BinaryEncodingTypes = [];
}
