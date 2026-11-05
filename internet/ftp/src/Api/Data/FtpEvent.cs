namespace ftp;

/// <summary>
/// Carries structured, non-secret context for an FTP lifecycle or operation event.
/// </summary>
public class FtpEvent
{
    /// <summary>
    /// Identifies the event category.
    /// </summary>
    public FtpEventKind Kind;

    /// <summary>
    /// Indicates whether <see cref="AddressFamily"/> is available for this event.
    /// </summary>
    public bool HasAddressFamily;

    /// <summary>
    /// Identifies the relevant network address family.
    /// </summary>
    public FtpAddressFamily AddressFamily;

    /// <summary>
    /// Indicates whether <see cref="AuthenticationStatus"/> is available for this event.
    /// </summary>
    public bool HasAuthenticationStatus;

    /// <summary>
    /// Provides an authentication outcome.
    /// </summary>
    public FtpAuthenticationStatus AuthenticationStatus;

    /// <summary>
    /// Indicates whether <see cref="AuthorizationOperation"/> is available for this event.
    /// </summary>
    public bool HasAuthorizationOperation;

    /// <summary>
    /// Identifies the authorization operation.
    /// </summary>
    public FtpAuthorizationOperation AuthorizationOperation;

    /// <summary>
    /// Indicates whether <see cref="CollisionPolicy"/> is available for this event.
    /// </summary>
    public bool HasCollisionPolicy;

    /// <summary>
    /// Identifies the applicable collision policy.
    /// </summary>
    public FtpCollisionPolicy CollisionPolicy;

    /// <summary>
    /// Indicates whether <see cref="CommandKind"/> is available for this event.
    /// </summary>
    public bool HasCommandKind;

    /// <summary>
    /// Identifies the relevant FTP command.
    /// </summary>
    public FtpCommandKind CommandKind;

    /// <summary>
    /// Indicates whether <see cref="DataConnectionMode"/> is available for this event.
    /// </summary>
    public bool HasDataConnectionMode;

    /// <summary>
    /// Identifies the data-connection mode.
    /// </summary>
    public FtpDataConnectionMode DataConnectionMode;

    /// <summary>
    /// Indicates whether <see cref="DataProtectionLevel"/> is available for this event.
    /// </summary>
    public bool HasDataProtectionLevel;

    /// <summary>
    /// Identifies the data-channel protection level.
    /// </summary>
    public FtpDataProtectionLevel DataProtectionLevel;

    /// <summary>
    /// Indicates whether <see cref="DirectorySyncMode"/> is available for this event.
    /// </summary>
    public bool HasDirectorySyncMode;

    /// <summary>
    /// Identifies the directory synchronization mode.
    /// </summary>
    public FtpDirectorySyncMode DirectorySyncMode;

    /// <summary>
    /// Indicates whether <see cref="EntryType"/> is available for this event.
    /// </summary>
    public bool HasEntryType;

    /// <summary>
    /// Identifies the relevant directory-entry type.
    /// </summary>
    public FtpEntryType EntryType;

    /// <summary>
    /// Indicates whether <see cref="ExtensionDisposition"/> is available for this event.
    /// </summary>
    public bool HasExtensionDisposition;

    /// <summary>
    /// Provides an extension-handler disposition.
    /// </summary>
    public FtpExtensionDisposition ExtensionDisposition;

    /// <summary>
    /// Indicates whether <see cref="FailurePolicy"/> is available for this event.
    /// </summary>
    public bool HasFailurePolicy;

    /// <summary>
    /// Identifies the multi-item failure policy.
    /// </summary>
    public FtpFailurePolicy FailurePolicy;

    /// <summary>
    /// Indicates whether <see cref="FeatureSupport"/> is available for this event.
    /// </summary>
    public bool HasFeatureSupport;

    /// <summary>
    /// Provides the relevant feature-support state.
    /// </summary>
    public FtpFeatureSupport FeatureSupport;

    /// <summary>
    /// Indicates whether <see cref="FileOpenMode"/> is available for this event.
    /// </summary>
    public bool HasFileOpenMode;

    /// <summary>
    /// Identifies the virtual file open mode.
    /// </summary>
    public FtpFileOpenMode FileOpenMode;

    /// <summary>
    /// Indicates whether <see cref="FileStructure"/> is available for this event.
    /// </summary>
    public bool HasFileStructure;

    /// <summary>
    /// Identifies the selected file structure.
    /// </summary>
    public FtpFileStructure FileStructure;

    /// <summary>
    /// Indicates whether <see cref="Permission"/> is available for this event.
    /// </summary>
    public bool HasPermission;

    /// <summary>
    /// Identifies the relevant entry permission.
    /// </summary>
    public FtpPermission Permission;

    /// <summary>
    /// Indicates whether <see cref="ReplyCategory"/> is available for this event.
    /// </summary>
    public bool HasReplyCategory;

    /// <summary>
    /// Identifies the reply result category.
    /// </summary>
    public FtpReplyCategory ReplyCategory;

    /// <summary>
    /// Indicates whether <see cref="ReplyKind"/> is available for this event.
    /// </summary>
    public bool HasReplyKind;

    /// <summary>
    /// Identifies the reply functional group.
    /// </summary>
    public FtpReplyKind ReplyKind;

    /// <summary>
    /// Indicates whether <see cref="ResumePolicy"/> is available for this event.
    /// </summary>
    public bool HasResumePolicy;

    /// <summary>
    /// Identifies the transfer resume policy.
    /// </summary>
    public FtpResumePolicy ResumePolicy;

    /// <summary>
    /// Indicates whether <see cref="SecurityMode"/> is available for this event.
    /// </summary>
    public bool HasSecurityMode;

    /// <summary>
    /// Identifies the control-channel security mode.
    /// </summary>
    public FtpSecurityMode SecurityMode;

    /// <summary>
    /// Indicates whether <see cref="ServerState"/> is available for this event.
    /// </summary>
    public bool HasServerState;

    /// <summary>
    /// Identifies the server lifecycle state.
    /// </summary>
    public FtpServerState ServerState;

    /// <summary>
    /// Indicates whether <see cref="SessionState"/> is available for this event.
    /// </summary>
    public bool HasSessionState;

    /// <summary>
    /// Identifies the session lifecycle state.
    /// </summary>
    public FtpSessionState SessionState;

    /// <summary>
    /// Indicates whether <see cref="SymlinkPolicy"/> is available for this event.
    /// </summary>
    public bool HasSymlinkPolicy;

    /// <summary>
    /// Identifies the recursive symbolic-link policy.
    /// </summary>
    public FtpSymlinkPolicy SymlinkPolicy;

    /// <summary>
    /// Indicates whether <see cref="TextFormat"/> is available for this event.
    /// </summary>
    public bool HasTextFormat;

    /// <summary>
    /// Identifies the selected text format.
    /// </summary>
    public FtpTextFormat TextFormat;

    /// <summary>
    /// Indicates whether <see cref="TransferDirection"/> is available for this event.
    /// </summary>
    public bool HasTransferDirection;

    /// <summary>
    /// Identifies the transfer direction.
    /// </summary>
    public FtpTransferDirection TransferDirection;

    /// <summary>
    /// Indicates whether <see cref="TransferMode"/> is available for this event.
    /// </summary>
    public bool HasTransferMode;

    /// <summary>
    /// Identifies the selected transfer mode.
    /// </summary>
    public FtpTransferMode TransferMode;

    /// <summary>
    /// Indicates whether <see cref="TransferType"/> is available for this event.
    /// </summary>
    public bool HasTransferType;

    /// <summary>
    /// Identifies the selected representation type.
    /// </summary>
    public FtpTransferType TransferType;

    /// <summary>
    /// Indicates whether <see cref="VerificationMode"/> is available for this event.
    /// </summary>
    public bool HasVerificationMode;

    /// <summary>
    /// Identifies the transfer verification mode.
    /// </summary>
    public FtpVerificationMode VerificationMode;

    /// <summary>
    /// The UTC event timestamp.
    /// </summary>
    public FtpTimestamp Timestamp = new FtpTimestamp();

    /// <summary>
    /// A redacted public event message.
    /// </summary>
    public string Message = "";

    /// <summary>
    /// The optional normalized path associated with the event.
    /// </summary>
    public string? Path;

    /// <summary>
    /// The optional remote endpoint associated with the event.
    /// </summary>
    public FtpEndpoint? RemoteEndpoint;

    /// <summary>
    /// The optional principal associated with the event.
    /// </summary>
    public FtpPrincipal? Principal;

    /// <summary>
    /// The optional redacted command associated with the event.
    /// </summary>
    public FtpCommand? Command;

    /// <summary>
    /// The optional reply associated with the event.
    /// </summary>
    public FtpReply? Reply;

    /// <summary>
    /// The optional transfer progress associated with the event.
    /// </summary>
    public FtpTransferProgress? TransferProgress;

    /// <summary>
    /// Optional negotiated TLS facts without certificate or key material.
    /// </summary>
    public FtpTlsDiagnostics? TlsDiagnostics;

    /// <summary>
    /// Indicates that secret-bearing values were removed from the event.
    /// </summary>
    public bool Redacted = true;
}
