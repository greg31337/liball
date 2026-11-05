namespace ftp;

/// <summary>
/// Defines synchronous FTP client connection, command, metadata, filesystem, and transfer operations.
/// </summary>
public interface IFtpClient
{
    /// <summary>
    /// Connects using a validated snapshot of the supplied options.
    /// </summary>
    /// <param name="options">The client connection and security options.</param>
    void Connect(FtpClientOptions options);

    /// <summary>
    /// Probes configured plain, explicit-TLS, and implicit-TLS profiles without sending credentials.
    /// </summary>
    /// <param name="options">The endpoint, timeout, TLS, and profile policy.</param>
    /// <returns>The credential-free probe results.</returns>
    FtpConnectionProbeResult ProbeConnectionProfiles(FtpClientOptions options);

    /// <summary>
    /// Reconnects and restores safe session settings.
    /// </summary>
    void Reconnect();

    /// <summary>
    /// Disconnects gracefully when possible.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Closes the control connection without sending QUIT.
    /// </summary>
    void AbortConnection();

    /// <summary>
    /// Sends QUIT and collects its reply.
    /// </summary>
    /// <returns>The QUIT command result.</returns>
    FtpCommandResult QuitSession();

    /// <summary>
    /// Sends REIN and resets session state.
    /// </summary>
    /// <returns>The REIN command result.</returns>
    FtpCommandResult ReinitializeSession();

    /// <summary>
    /// Gets the current connection and authentication state.
    /// </summary>
    /// <returns>The current session state.</returns>
    FtpSessionState GetSessionState();

    /// <summary>
    /// Performs USER, PASS, and optional ACCT authentication.
    /// </summary>
    /// <param name="credentials">The credentials to authenticate.</param>
    /// <returns>The authentication result.</returns>
    FtpAuthenticationResult AuthenticateUser(FtpCredentials credentials);

    /// <summary>
    /// Upgrades the control channel with AUTH TLS.
    /// </summary>
    /// <param name="options">The strict TLS settings.</param>
    /// <returns>The AUTH command result.</returns>
    FtpCommandResult UpgradeControlTls(FtpTlsOptions options);

    /// <summary>
    /// Negotiates clear or private data-channel protection.
    /// </summary>
    /// <param name="level">The requested protection level.</param>
    /// <returns>The PBSZ and PROT command result.</returns>
    FtpCommandResult SetDataProtection(FtpDataProtectionLevel level);

    /// <summary>
    /// Clears TLS from the control channel when explicitly permitted.
    /// </summary>
    /// <returns>The CCC command result.</returns>
    FtpCommandResult ClearControlTls();

    /// <summary>
    /// Requests and normalizes the server feature set.
    /// </summary>
    /// <returns>The negotiated feature set.</returns>
    FtpFeatureSet NegotiateFeatures();

    /// <summary>
    /// Refreshes features after a session state change.
    /// </summary>
    /// <returns>The refreshed feature set.</returns>
    FtpFeatureSet RefreshFeatures();

    /// <summary>
    /// Applies an option to a standard or extension-owned command.
    /// </summary>
    /// <param name="option">The option request.</param>
    /// <returns>The OPTS command result.</returns>
    FtpCommandResult ApplyOption(FtpOptionRequest option);

    /// <summary>
    /// Sends one validated command and collects its ordered replies.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The command and its replies.</returns>
    FtpCommandResult ExecuteCommand(FtpCommand command);

    /// <summary>
    /// Executes validated commands in order.
    /// </summary>
    /// <param name="commands">The command batch.</param>
    /// <returns>The ordered command results.</returns>
    FtpCommandResult[] ExecuteCommandBatch(FtpCommand[] commands);

    /// <summary>
    /// Sends one validated SITE argument.
    /// </summary>
    /// <param name="argument">The SITE subcommand and arguments.</param>
    /// <returns>The SITE command result.</returns>
    FtpCommandResult ExecuteSite(string argument);

    /// <summary>
    /// Requests HELP for an optional command or topic.
    /// </summary>
    /// <param name="topic">The optional HELP topic.</param>
    /// <returns>The HELP command result.</returns>
    FtpCommandResult RequestHelp(string? topic);

    /// <summary>
    /// Requests control-channel STAT for an optional path.
    /// </summary>
    /// <param name="path">The optional STAT path.</param>
    /// <returns>The STAT command result.</returns>
    FtpCommandResult RequestStatus(string? path);

    /// <summary>
    /// Requests the server system type.
    /// </summary>
    /// <returns>The SYST command result.</returns>
    FtpCommandResult RequestSystemType();

    /// <summary>
    /// Sends a control-channel keepalive command.
    /// </summary>
    /// <returns>The NOOP command result.</returns>
    FtpCommandResult SendNoOperation();

    /// <summary>
    /// Selects TYPE and FORMAT from one transfer option snapshot.
    /// </summary>
    /// <param name="options">The representation options.</param>
    /// <returns>The representation command result.</returns>
    FtpCommandResult SetTransferRepresentation(FtpTransferOptions options);

    /// <summary>
    /// Selects the FTP file structure.
    /// </summary>
    /// <param name="structure">The requested structure.</param>
    /// <returns>The STRU command result.</returns>
    FtpCommandResult SetFileStructure(FtpFileStructure structure);

    /// <summary>
    /// Selects the FTP transfer mode.
    /// </summary>
    /// <param name="mode">The requested mode.</param>
    /// <returns>The MODE command result.</returns>
    FtpCommandResult SetTransferMode(FtpTransferMode mode);

    /// <summary>
    /// Requests storage allocation.
    /// </summary>
    /// <param name="bytes">The non-negative byte count.</param>
    /// <returns>The ALLO command result.</returns>
    FtpCommandResult AllocateStorage(long bytes);

    /// <summary>
    /// Changes the remote working directory.
    /// </summary>
    /// <param name="path">The target directory.</param>
    /// <returns>The CWD command result.</returns>
    FtpCommandResult ChangeWorkingDirectory(string path);

    /// <summary>
    /// Changes to the parent remote directory.
    /// </summary>
    /// <returns>The CDUP command result.</returns>
    FtpCommandResult ChangeToParentDirectory();

    /// <summary>
    /// Selects a mounted file-system structure.
    /// </summary>
    /// <param name="path">The mount path.</param>
    /// <returns>The SMNT command result.</returns>
    FtpCommandResult MountFileStructure(string path);

    /// <summary>
    /// Requests the current remote working directory.
    /// </summary>
    /// <returns>The PWD command result.</returns>
    FtpCommandResult GetWorkingDirectory();

    /// <summary>
    /// Retrieves preferred machine-readable metadata for a path.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <returns>The metadata probe result.</returns>
    FtpPathProbeResult GetMetadata(string path);

    /// <summary>
    /// Retrieves a path size with safe feature-directed fallback.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <returns>The size probe result.</returns>
    FtpPathProbeResult GetFileSize(string path);

    /// <summary>
    /// Retrieves a path modification time with safe feature-directed fallback.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <returns>The timestamp probe result.</returns>
    FtpPathProbeResult GetModificationTime(string path);

    /// <summary>
    /// Probes a path using metadata commands and safe fallbacks.
    /// </summary>
    /// <param name="path">The path to probe.</param>
    /// <returns>The exception-less probe result.</returns>
    FtpPathProbeResult ProbePath(string path);

    /// <summary>
    /// Retrieves a directory listing using adaptive parser selection.
    /// </summary>
    /// <param name="path">The directory path, or an empty string for the current directory.</param>
    /// <returns>The parsed and raw listing data.</returns>
    FtpListing ListDirectory(string path);

    /// <summary>
    /// Retrieves an MLSD machine-readable listing.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The machine-readable listing.</returns>
    FtpListing ListMachineDirectory(string path);

    /// <summary>
    /// Retrieves a LIST human-readable listing.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The detected-format listing.</returns>
    FtpListing ListHumanDirectory(string path);

    /// <summary>
    /// Retrieves an NLST name-only listing.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The name-only listing.</returns>
    FtpListing ListNames(string path);

    /// <summary>
    /// Retrieves and parses a STAT listing without a data connection.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The status listing.</returns>
    FtpListing ListStatus(string path);

    /// <summary>
    /// Creates one remote directory.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The MKD command result.</returns>
    FtpCommandResult CreateDirectory(string path);

    /// <summary>
    /// Creates every missing directory in a remote parent path.
    /// </summary>
    /// <param name="path">The final directory path.</param>
    /// <returns>The ordered probe and MKD results.</returns>
    FtpCommandResult[] CreateParentDirectories(string path);

    /// <summary>
    /// Removes one empty remote directory.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The RMD command result.</returns>
    FtpCommandResult RemoveDirectory(string path);

    /// <summary>
    /// Deletes one remote file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The DELE command result.</returns>
    FtpCommandResult DeleteFile(string path);

    /// <summary>
    /// Renames or moves one remote path.
    /// </summary>
    /// <param name="sourcePath">The source path.</param>
    /// <param name="destinationPath">The destination path.</param>
    /// <returns>The RNFR and RNTO command results.</returns>
    FtpCommandResult[] RenamePath(string sourcePath, string destinationPath);

    /// <summary>
    /// Recursively deletes a directory under explicit destructive-operation policy.
    /// </summary>
    /// <param name="request">The guarded recursive request.</param>
    /// <returns>The per-item deletion results.</returns>
    FtpDirectoryTransferResult DeleteDirectoryTree(FtpDirectoryTransferRequest request);

    /// <summary>
    /// Sets a remote modification timestamp through a configured supported extension.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <param name="timestamp">The UTC timestamp.</param>
    /// <returns>The MFMT or SITE command result.</returns>
    FtpCommandResult SetModificationTime(string path, FtpTimestamp timestamp);

    /// <summary>
    /// Sets remote Unix-style permission bits through a configured SITE extension.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <param name="mode">The permission bits.</param>
    /// <returns>The SITE CHMOD command result.</returns>
    FtpCommandResult SetPermissions(string path, uint mode);

    /// <summary>
    /// Sets the remote session's Unix-style creation mask through SITE UMASK.
    /// </summary>
    /// <param name="mode">The three permission-mask bits.</param>
    /// <returns>The SITE UMASK command result.</returns>
    FtpCommandResult SetUmask(uint mode);

    /// <summary>
    /// Executes one file or stream transfer.
    /// </summary>
    /// <param name="request">The transfer source, destination, and options.</param>
    /// <returns>The transfer, progress, verification, and command results.</returns>
    FtpTransferResult Transfer(FtpTransferRequest request);

    /// <summary>
    /// Aborts the current transfer and consumes its completion replies.
    /// </summary>
    /// <returns>The ABOR command result.</returns>
    FtpCommandResult AbortTransfer();

    /// <summary>
    /// Performs the configured metadata or checksum verification without transferring content.
    /// </summary>
    /// <param name="request">The paths, expected values, and verification policy.</param>
    /// <returns>The verification result.</returns>
    FtpTransferResult VerifyTransfer(FtpTransferRequest request);

    /// <summary>
    /// Gets the latest accepted or emitted restart marker.
    /// </summary>
    /// <returns>The marker text, or null when no marker is available.</returns>
    string? GetLatestRestartMarker();

    /// <summary>
    /// Builds a deterministic directory transfer or synchronization plan.
    /// </summary>
    /// <param name="request">The recursive transfer roots and policies.</param>
    /// <returns>The non-mutating plan.</returns>
    FtpDirectoryPlan PlanDirectoryTransfer(FtpDirectoryTransferRequest request);

    /// <summary>
    /// Executes a recursive directory transfer or synchronization.
    /// </summary>
    /// <param name="request">The recursive transfer roots and policies.</param>
    /// <returns>The aggregate and per-item results.</returns>
    FtpDirectoryTransferResult TransferDirectory(FtpDirectoryTransferRequest request);

    /// <summary>
    /// Executes a direct server-to-server transfer.
    /// </summary>
    /// <param name="request">The paired sessions, paths, and transfer options.</param>
    /// <returns>The paired-session command and completion results.</returns>
    FtpServerTransferResult TransferServerToServer(FtpServerTransferRequest request);
}
