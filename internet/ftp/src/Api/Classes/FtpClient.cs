namespace ftp;

/// <summary>
/// Provides the synchronous public FTP client operation surface.
/// </summary>
public class FtpClient : IFtpClient
{
    internal FtpClientState State;

    /// <summary>
    /// Creates a client using the supplied certificate validator for protected sessions.
    /// </summary>
    /// <param name="certificateValidator">The validator for remote TLS certificates, or null for plain-only use.</param>
    public FtpClient(IFtpCertificateValidator? certificateValidator)
    {
        State = new FtpClientState(certificateValidator);
    }

    /// <summary>
    /// Validates and stores an isolated option snapshot before connecting.
    /// </summary>
    /// <param name="options">The client connection and security options.</param>
    public void Connect(FtpClientOptions options)
    {
        State.RecordOperation("Connect");
        State.RequireClosedSession();
        FtpClientOptions snapshot = FtpClientOptionsSnapshot.CreateValidated(
            options, State.CertificateValidator);
        State.ResetReconnectState();
        State.Options = snapshot;
        State.ConnectRequested = true;
        OpenSavedConnection();
    }

    /// <summary>
    /// Validates an isolated option snapshot for credential-free profile probing.
    /// </summary>
    /// <param name="options">The endpoint and profile policy.</param>
    /// <returns>An incomplete probe result until connection probing is available.</returns>
    public FtpConnectionProbeResult ProbeConnectionProfiles(FtpClientOptions options)
    {
        State.RecordOperation("ProbeConnectionProfiles");
        FtpClientOptions snapshot = FtpClientOptionsSnapshot.CreateValidated(
            options, State.CertificateValidator);
        FtpConnectionProfileProbe probe = new FtpConnectionProfileProbe(
            snapshot, State.CertificateValidator);
        return probe.ProbeProfiles();
    }

    /// <summary>
    /// Reconnects using the retained validated option snapshot.
    /// </summary>
    public void Reconnect()
    {
        State.RecordOperation("Reconnect");
        State.RequireClosedSession();
        if (State.Options == null)
        {
            throw new FtpConnectionException(
                "Reconnect requires a previously validated client configuration.", null);
        }

        State.ConnectRequested = true;
        FtpClientReconnectState reconnectState = State.ReconnectState;
        State.ReconnectInProgress = true;
        FtpFaultIsolationResult reconnectResult =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                OpenSavedConnection();
                RestoreSavedSession(reconnectState);
                return null;
            });
        if (!reconnectResult.Succeeded && State.Connection != null)
        {
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                State.CloseSession();
                return null;
            });
        }
        State.ReconnectInProgress = false;
        if (!reconnectResult.Succeeded)
        {
            if (reconnectResult.Failure != null)
            {
                throw reconnectResult.Failure;
            }
            throw new FtpConnectionException(
                "The FTP reconnect did not reach a terminal result.", null);
        }
        State.UpdateReconnectState();
    }

    /// <summary>
    /// Closes the current session gracefully when one is open.
    /// </summary>
    public void Disconnect()
    {
        State.RecordOperation("Disconnect");
        if (State.Connection == null)
        {
            State.CloseSession();
            return;
        }

        FtpFaultIsolationRunner.RunOperation(() =>
        {
            FtpClientCommandExecutor executor = CreateCommandExecutor();
            executor.ExecuteRawCommand(executor.CreateSimpleCommand(
                FtpCommandKind.Quit, null));
            return null;
        });
        if (State.Connection != null)
        {
            State.CloseSession();
        }
    }

    /// <summary>
    /// Closes the current session immediately when one is open.
    /// </summary>
    public void AbortConnection()
    {
        State.RecordOperation("AbortConnection");
        State.CloseSession();
    }

    /// <summary>
    /// Sends QUIT and collects its reply.
    /// </summary>
    /// <returns>The QUIT command result.</returns>
    public FtpCommandResult QuitSession()
    {
        State.RecordOperation("QuitSession");
        State.RequireOpenSession("QuitSession");
        FtpClientCommandExecutor executor = CreateCommandExecutor();
        return executor.ExecuteRawCommand(executor.CreateSimpleCommand(
            FtpCommandKind.Quit, null));
    }

    /// <summary>
    /// Sends REIN and resets session state.
    /// </summary>
    /// <returns>The REIN command result.</returns>
    public FtpCommandResult ReinitializeSession()
    {
        State.RecordOperation("ReinitializeSession");
        State.RequireOpenSession("ReinitializeSession");
        FtpClientCommandExecutor executor = CreateCommandExecutor();
        return executor.ExecuteRawCommand(executor.CreateSimpleCommand(
            FtpCommandKind.Rein, null));
    }

    /// <summary>
    /// Gets the current connection and authentication state.
    /// </summary>
    /// <returns>The current session state.</returns>
    public FtpSessionState GetSessionState()
    {
        State.RecordOperation("GetSessionState");
        return State.Session.State.SessionState;
    }

    /// <summary>
    /// Performs a USER, PASS, and optional ACCT authentication exchange.
    /// </summary>
    /// <param name="credentials">The credentials to authenticate.</param>
    /// <returns>The authentication result.</returns>
    public FtpAuthenticationResult AuthenticateUser(FtpCredentials credentials)
    {
        State.RecordOperation("AuthenticateUser");
        State.RequireOpenSession("AuthenticateUser");
        FtpClientLoginSequence login = CreateLoginSequence();
        login.RouteVirtualHost();
        FtpAuthenticationResult result = login.Authenticate(credentials);
        State.LastAuthentication = result;
        if (result.Status == FtpAuthenticationStatus.Success)
        {
            State.ActiveCredentials =
                FtpClientReconnectState.CopyCredentials(credentials);
            State.UpdateReconnectState();
        }
        if (State.Session.State.SessionState == FtpSessionState.Closed)
        {
            State.CloseSession();
        }
        return result;
    }

    /// <summary>
    /// Upgrades the control channel with AUTH TLS.
    /// </summary>
    /// <param name="options">The strict TLS settings.</param>
    /// <returns>The AUTH command result.</returns>
    public FtpCommandResult UpgradeControlTls(FtpTlsOptions options)
    {
        State.RecordOperation("UpgradeControlTls");
        State.RequireOpenSession("UpgradeControlTls");
        FtpConfigurationValidator.ValidateTlsOptions(options);
        if (State.CertificateValidator == null)
        {
            throw new FtpConfigurationException(
                "A certificate validator is required for an FTP-over-TLS client.", null);
        }
        if (State.Options == null || State.Connection == null)
        {
            throw State.CreateUnavailableOperation("UpgradeControlTls");
        }
        if (State.Connection.ControlProtected ||
            State.Session.State.SecurityExchangeComplete)
        {
            throw new FtpProtocolException(
                "The FTP control session has already completed a security exchange.", null);
        }

        FtpTlsOptions snapshot = FtpClientOptionsSnapshot.CopyTls(options);
        FtpClientAuthTlsCoordinator coordinator =
            new FtpClientAuthTlsCoordinator(
                FtpSecurityMode.ExplicitTlsRequired,
                false,
                State.Session);
        FtpCommand command = coordinator.BeginAuthorization("TLS");
        FtpFaultIsolationResult operation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return UpgradeControlTlsCore(snapshot, coordinator, command);
        });
        if (operation.Succeeded)
        {
            FtpCommandResult? result = operation.Value as FtpCommandResult;
            if (result != null)
            {
                return result;
            }
        }

        FtpFaultIsolationRunner.RunOperation(() =>
        {
            State.CloseSession();
            return null;
        });
        if (operation.Failure != null)
        {
            throw operation.Failure;
        }
        throw new FtpTlsException(
            "The FTP control-channel TLS upgrade did not reach a terminal result.", null);
    }

    /// <summary>
    /// Negotiates the requested data-channel protection level.
    /// </summary>
    /// <param name="level">The requested protection level.</param>
    /// <returns>The protection command result.</returns>
    public FtpCommandResult SetDataProtection(FtpDataProtectionLevel level)
    {
        State.RecordOperation("SetDataProtection");
        State.RequireOpenSession("SetDataProtection");
        if (level != FtpDataProtectionLevel.Clear &&
            level != FtpDataProtectionLevel.Private)
        {
            throw new FtpConfigurationException(
                "FTP over TLS supports only clear or private data protection.", null);
        }
        if (!State.Session.State.SecurityExchangeComplete ||
            !State.Session.State.SecurityExchangeIsTls ||
            !State.Session.State.ControlProtected)
        {
            throw new FtpProtocolException(
                "FTP data protection requires a protected TLS control session.", null);
        }
        if (!State.Session.State.ProtectionBufferSizeSet)
        {
            FtpCommandResult bufferResult = ExecuteClientCommand(
                FtpCommandKind.Pbsz, "0");
            if (!bufferResult.Succeeded || bufferResult.FinalReply == null ||
                bufferResult.FinalReply.Code != FtpReplyCodes.CommandOkay)
            {
                return bufferResult;
            }
        }
        string argument = level == FtpDataProtectionLevel.Private ? "P" : "C";
        return ExecuteClientCommand(FtpCommandKind.Prot, argument);
    }

    /// <summary>
    /// Clears TLS from an eligible control channel.
    /// </summary>
    /// <returns>The CCC command result.</returns>
    public FtpCommandResult ClearControlTls()
    {
        State.RecordOperation("ClearControlTls");
        State.RequireOpenSession("ClearControlTls");
        if (State.Options == null || State.Connection == null ||
            State.Connection.TlsClient == null ||
            !State.Connection.ControlProtected)
        {
            throw new FtpProtocolException(
                "CCC requires an active protected FTP control connection.", null);
        }

        FtpClientClearControlCoordinator coordinator =
            new FtpClientClearControlCoordinator(
                State.Options.Tls,
                State.Session,
                State.Connection.TlsClient,
                null);
        FtpCommand command = coordinator.BeginClearControl();
        FtpFaultIsolationResult operation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return ClearControlTlsCore(coordinator, command);
        });
        if (operation.Succeeded)
        {
            FtpCommandResult? result = operation.Value as FtpCommandResult;
            if (result != null)
            {
                return result;
            }
        }

        FtpFaultIsolationRunner.RunOperation(() =>
        {
            State.CloseSession();
            return null;
        });
        if (operation.Failure != null)
        {
            throw operation.Failure;
        }
        throw new FtpTlsException(
            "The FTP clear-control operation did not reach a terminal result.", null);
    }

    /// <summary>
    /// Requests and normalizes the server feature set.
    /// </summary>
    /// <returns>The negotiated feature set.</returns>
    public FtpFeatureSet NegotiateFeatures()
    {
        State.RecordOperation("NegotiateFeatures");
        State.RequireOpenSession("NegotiateFeatures");
        FtpFeatureSet features = CreateFeatureNegotiator().NegotiateFeatureSet();
        State.Features = features;
        State.UpdateReconnectState();
        return features;
    }

    /// <summary>
    /// Refreshes features after a session state change.
    /// </summary>
    /// <returns>The refreshed feature set.</returns>
    public FtpFeatureSet RefreshFeatures()
    {
        State.RecordOperation("RefreshFeatures");
        State.RequireOpenSession("RefreshFeatures");
        FtpFeatureSet features = CreateFeatureNegotiator().NegotiateFeatureSet();
        State.Features = features;
        State.UpdateReconnectState();
        return features;
    }

    /// <summary>
    /// Applies an option to a standard or extension command.
    /// </summary>
    /// <param name="option">The option request.</param>
    /// <returns>The OPTS command result.</returns>
    public FtpCommandResult ApplyOption(FtpOptionRequest option)
    {
        State.RecordOperation("ApplyOption");
        State.RequireOpenSession("ApplyOption");
        FtpCommandResult result =
            CreateFeatureNegotiator().ApplyOptionRequest(option);
        if (result.ConnectionClosed)
        {
            State.CloseSession();
        }
        else if (result.Succeeded)
        {
            State.UpdateReconnectState();
        }
        return result;
    }

    /// <summary>
    /// Sends one validated command and collects its replies.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The command result.</returns>
    public FtpCommandResult ExecuteCommand(FtpCommand command)
    {
        State.RecordOperation("ExecuteCommand");
        State.RequireOpenSession("ExecuteCommand");
        FtpCommandResult result =
            CreateCommandExecutor().ExecuteRawCommand(command);
        if (!result.ConnectionClosed)
        {
            State.UpdateReconnectState();
        }
        return result;
    }

    /// <summary>
    /// Executes validated commands in order.
    /// </summary>
    /// <param name="commands">The command batch.</param>
    /// <returns>The ordered command results.</returns>
    public FtpCommandResult[] ExecuteCommandBatch(FtpCommand[] commands)
    {
        State.RecordOperation("ExecuteCommandBatch");
        State.RequireOpenSession("ExecuteCommandBatch");
        FtpCommandResult[] results =
            CreateCommandExecutor().ExecuteRawBatch(commands);
        State.UpdateReconnectState();
        return results;
    }

    /// <summary>
    /// Sends one validated SITE argument.
    /// </summary>
    /// <param name="argument">The SITE argument.</param>
    /// <returns>The SITE command result.</returns>
    public FtpCommandResult ExecuteSite(string argument)
    {
        State.RecordOperation("ExecuteSite");
        State.RequireOpenSession("ExecuteSite");
        FtpClientCommandExecutor executor = CreateCommandExecutor();
        return executor.ExecuteRawCommand(executor.CreateSimpleCommand(
            FtpCommandKind.Site, argument));
    }

    /// <summary>
    /// Requests HELP for an optional topic.
    /// </summary>
    /// <param name="topic">The optional HELP topic.</param>
    /// <returns>The HELP command result.</returns>
    public FtpCommandResult RequestHelp(string? topic)
    {
        State.RecordOperation("RequestHelp");
        State.RequireOpenSession("RequestHelp");
        FtpClientCommandExecutor executor = CreateCommandExecutor();
        return executor.ExecuteRawCommand(executor.CreateSimpleCommand(
            FtpCommandKind.Help, topic));
    }

    /// <summary>
    /// Requests control-channel status for an optional path.
    /// </summary>
    /// <param name="path">The optional STAT path.</param>
    /// <returns>The STAT command result.</returns>
    public FtpCommandResult RequestStatus(string? path)
    {
        State.RecordOperation("RequestStatus");
        State.RequireOpenSession("RequestStatus");
        FtpClientCommandExecutor executor = CreateCommandExecutor();
        return executor.ExecuteRawCommand(executor.CreateSimpleCommand(
            FtpCommandKind.Stat, path));
    }

    /// <summary>
    /// Requests the server system type.
    /// </summary>
    /// <returns>The SYST command result.</returns>
    public FtpCommandResult RequestSystemType()
    {
        State.RecordOperation("RequestSystemType");
        State.RequireOpenSession("RequestSystemType");
        FtpClientCommandExecutor executor = CreateCommandExecutor();
        return executor.ExecuteRawCommand(executor.CreateSimpleCommand(
            FtpCommandKind.Syst, null));
    }

    /// <summary>
    /// Sends a control-channel keepalive command.
    /// </summary>
    /// <returns>The NOOP command result.</returns>
    public FtpCommandResult SendNoOperation()
    {
        State.RecordOperation("SendNoOperation");
        State.RequireOpenSession("SendNoOperation");
        FtpClientCommandExecutor executor = CreateCommandExecutor();
        return executor.ExecuteRawCommand(executor.CreateSimpleCommand(
            FtpCommandKind.Noop, null));
    }

    /// <summary>
    /// Selects TYPE and FORMAT from a transfer option snapshot.
    /// </summary>
    /// <param name="options">The representation options.</param>
    /// <returns>The representation command result.</returns>
    public FtpCommandResult SetTransferRepresentation(FtpTransferOptions options)
    {
        State.RecordOperation("SetTransferRepresentation");
        State.RequireOpenSession("SetTransferRepresentation");
        FtpTransferParameterCodec.ValidateRepresentationForState(
            options, State.Session.State);
        string argument =
            FtpTransferParameterCodec.FormatRepresentation(options);
        return ExecuteClientCommand(FtpCommandKind.Type, argument);
    }

    /// <summary>
    /// Selects the FTP file structure.
    /// </summary>
    /// <param name="structure">The requested structure.</param>
    /// <returns>The STRU command result.</returns>
    public FtpCommandResult SetFileStructure(FtpFileStructure structure)
    {
        State.RecordOperation("SetFileStructure");
        State.RequireOpenSession("SetFileStructure");
        FtpTransferParameterCodec.ValidateStructureForState(
            structure, State.Session.State);
        return ExecuteClientCommand(
            FtpCommandKind.Stru,
            FtpTransferParameterCodec.FormatStructure(structure));
    }

    /// <summary>
    /// Selects the FTP transfer mode.
    /// </summary>
    /// <param name="mode">The requested mode.</param>
    /// <returns>The MODE command result.</returns>
    public FtpCommandResult SetTransferMode(FtpTransferMode mode)
    {
        State.RecordOperation("SetTransferMode");
        State.RequireOpenSession("SetTransferMode");
        FtpTransferParameterCodec.ValidateModeForState(
            mode, State.Session.State);
        if (mode == FtpTransferMode.Deflate &&
            State.Features.Negotiated && !State.FeatureState.ModeZ)
        {
            throw new FtpConfigurationException(
                "MODE Z was not advertised by the negotiated FTP feature set.",
                null);
        }
        return ExecuteClientCommand(
            FtpCommandKind.Mode,
            FtpTransferParameterCodec.FormatMode(mode));
    }

    /// <summary>
    /// Requests storage allocation.
    /// </summary>
    /// <param name="bytes">The non-negative byte count.</param>
    /// <returns>The ALLO command result.</returns>
    public FtpCommandResult AllocateStorage(long bytes)
    {
        State.RecordOperation("AllocateStorage");
        State.RequireOpenSession("AllocateStorage");
        if (bytes < 0)
        {
            throw new FtpConfigurationException(
                "The FTP allocation byte count cannot be negative.", null);
        }
        FtpAllocationArgument allocation = new FtpAllocationArgument();
        allocation.ByteCount = bytes;
        return ExecuteClientCommand(
            FtpCommandKind.Allo,
            FtpArgumentCodec.FormatAllocateArgument(allocation));
    }

    /// <summary>
    /// Changes the remote working directory.
    /// </summary>
    /// <param name="path">The target directory.</param>
    /// <returns>The CWD command result.</returns>
    public FtpCommandResult ChangeWorkingDirectory(string path)
    {
        State.RecordOperation("ChangeWorkingDirectory");
        State.RequireOpenSession("ChangeWorkingDirectory");
        FtpTransferValidator.ValidateRequiredPath(path, "remote working-directory");
        return ExecuteClientCommand(FtpCommandKind.Cwd, path);
    }

    /// <summary>
    /// Changes to the parent remote directory.
    /// </summary>
    /// <returns>The CDUP command result.</returns>
    public FtpCommandResult ChangeToParentDirectory()
    {
        State.RecordOperation("ChangeToParentDirectory");
        State.RequireOpenSession("ChangeToParentDirectory");
        return ExecuteClientCommand(FtpCommandKind.Cdup, null);
    }

    /// <summary>
    /// Selects a mounted file-system structure.
    /// </summary>
    /// <param name="path">The mount path.</param>
    /// <returns>The SMNT command result.</returns>
    public FtpCommandResult MountFileStructure(string path)
    {
        State.RecordOperation("MountFileStructure");
        State.RequireOpenSession("MountFileStructure");
        FtpTransferValidator.ValidateRequiredPath(path, "remote mount");
        return ExecuteClientCommand(FtpCommandKind.Smnt, path);
    }

    /// <summary>
    /// Requests the current remote working directory.
    /// </summary>
    /// <returns>The PWD command result.</returns>
    public FtpCommandResult GetWorkingDirectory()
    {
        State.RecordOperation("GetWorkingDirectory");
        State.RequireOpenSession("GetWorkingDirectory");
        return ExecuteClientCommand(FtpCommandKind.Pwd, null);
    }

    /// <summary>
    /// Retrieves preferred machine-readable metadata for a path.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <returns>The metadata result.</returns>
    public FtpPathProbeResult GetMetadata(string path)
    {
        State.RecordOperation("GetMetadata");
        return new FtpClientMetadataOperation(State).RetrievePreferred(path);
    }

    /// <summary>
    /// Retrieves a path size with safe fallback.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <returns>The size result.</returns>
    public FtpPathProbeResult GetFileSize(string path)
    {
        State.RecordOperation("GetFileSize");
        return new FtpClientMetadataOperation(State).RetrieveSize(path);
    }

    /// <summary>
    /// Retrieves a path modification time with safe fallback.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <returns>The timestamp result.</returns>
    public FtpPathProbeResult GetModificationTime(string path)
    {
        State.RecordOperation("GetModificationTime");
        return new FtpClientMetadataOperation(State).RetrieveModification(path);
    }

    /// <summary>
    /// Probes a path using metadata commands and safe fallbacks.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <returns>The path probe result.</returns>
    public FtpPathProbeResult ProbePath(string path)
    {
        State.RecordOperation("ProbePath");
        return new FtpClientMetadataOperation(State).Probe(path);
    }

    /// <summary>
    /// Retrieves a directory listing with adaptive parser selection.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The directory listing.</returns>
    public FtpListing ListDirectory(string path)
    {
        State.RecordOperation("ListDirectory");
        return new FtpClientListingOperation(State).RetrieveAdaptive(path);
    }

    /// <summary>
    /// Retrieves an MLSD machine-readable listing.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The machine-readable listing.</returns>
    public FtpListing ListMachineDirectory(string path)
    {
        State.RecordOperation("ListMachineDirectory");
        return new FtpClientMachineListingOperation(State).RetrieveDirectory(path);
    }

    /// <summary>
    /// Retrieves a LIST human-readable listing.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The detected-format listing.</returns>
    public FtpListing ListHumanDirectory(string path)
    {
        State.RecordOperation("ListHumanDirectory");
        return new FtpClientListingOperation(State).RetrieveHuman(path);
    }

    /// <summary>
    /// Retrieves an NLST name-only listing.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The name-only listing.</returns>
    public FtpListing ListNames(string path)
    {
        State.RecordOperation("ListNames");
        return new FtpClientListingOperation(State).RetrieveNames(path);
    }

    /// <summary>
    /// Retrieves a STAT listing without a data connection.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The status listing.</returns>
    public FtpListing ListStatus(string path)
    {
        State.RecordOperation("ListStatus");
        return new FtpClientListingOperation(State).RetrieveStatus(path);
    }

    /// <summary>
    /// Creates one remote directory.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The MKD command result.</returns>
    public FtpCommandResult CreateDirectory(string path)
    {
        State.RecordOperation("CreateDirectory");
        return new FtpClientFileSystemOperation(State).CreateDirectory(path);
    }

    /// <summary>
    /// Creates every missing remote parent directory.
    /// </summary>
    /// <param name="path">The final directory path.</param>
    /// <returns>The ordered command results.</returns>
    public FtpCommandResult[] CreateParentDirectories(string path)
    {
        State.RecordOperation("CreateParentDirectories");
        return new FtpClientFileSystemOperation(State).CreateParentDirectories(path);
    }

    /// <summary>
    /// Removes one empty remote directory.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The RMD command result.</returns>
    public FtpCommandResult RemoveDirectory(string path)
    {
        State.RecordOperation("RemoveDirectory");
        return new FtpClientFileSystemOperation(State).RemoveDirectory(path);
    }

    /// <summary>
    /// Deletes one remote file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The DELE command result.</returns>
    public FtpCommandResult DeleteFile(string path)
    {
        State.RecordOperation("DeleteFile");
        return new FtpClientFileSystemOperation(State).DeleteFile(path);
    }

    /// <summary>
    /// Renames or moves one remote path.
    /// </summary>
    /// <param name="sourcePath">The source path.</param>
    /// <param name="destinationPath">The destination path.</param>
    /// <returns>The rename command results.</returns>
    public FtpCommandResult[] RenamePath(string sourcePath, string destinationPath)
    {
        State.RecordOperation("RenamePath");
        return new FtpClientFileSystemOperation(State).RenamePath(
            sourcePath, destinationPath);
    }

    /// <summary>
    /// Recursively deletes a directory under explicit policy.
    /// </summary>
    /// <param name="request">The guarded recursive request.</param>
    /// <returns>The per-item deletion results.</returns>
    public FtpDirectoryTransferResult DeleteDirectoryTree(
        FtpDirectoryTransferRequest request)
    {
        State.RecordOperation("DeleteDirectoryTree");
        return new FtpClientRecursiveDeletionOperation(State).DeleteTree(request);
    }

    /// <summary>
    /// Sets a remote modification timestamp.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <param name="timestamp">The UTC timestamp.</param>
    /// <returns>The modification command result.</returns>
    public FtpCommandResult SetModificationTime(
        string path,
        FtpTimestamp timestamp)
    {
        State.RecordOperation("SetModificationTime");
        return new FtpClientMetadataMutationOperation(State).SetModificationTime(
            path, timestamp);
    }

    /// <summary>
    /// Sets remote Unix-style permission bits.
    /// </summary>
    /// <param name="path">The remote path.</param>
    /// <param name="mode">The permission bits.</param>
    /// <returns>The permission command result.</returns>
    public FtpCommandResult SetPermissions(string path, uint mode)
    {
        State.RecordOperation("SetPermissions");
        return new FtpClientMetadataMutationOperation(State).SetPermissions(
            path, mode);
    }

    /// <summary>
    /// Sets the remote session's Unix-style creation mask.
    /// </summary>
    /// <param name="mode">The permission-mask bits.</param>
    /// <returns>The SITE UMASK command result.</returns>
    public FtpCommandResult SetUmask(uint mode)
    {
        State.RecordOperation("SetUmask");
        return new FtpClientMetadataMutationOperation(State).SetUmask(mode);
    }

    /// <summary>
    /// Executes one file or stream transfer.
    /// </summary>
    /// <param name="request">The transfer request.</param>
    /// <returns>The transfer result.</returns>
    public FtpTransferResult Transfer(FtpTransferRequest request)
    {
        State.RecordOperation("Transfer");
        State.RequireOpenSession("Transfer");
        if (State.Options == null || State.Connection == null)
        {
            throw State.CreateUnavailableOperation("Transfer");
        }
        IFtpTransferObserver? observer = request == null ? null : request.Observer;
        FtpTransferResult result;
        if (request != null && request.Direction == FtpTransferDirection.Upload)
        {
            result = new FtpClientUploadOperation(State).Upload(request);
        }
        else
        {
            result = new FtpClientDownloadOperation(State).Download(request);
        }
        FtpClientTransferMonitor.NotifyCompleted(observer, result);
        return result;
    }

    /// <summary>
    /// Aborts the current transfer and consumes completion replies.
    /// </summary>
    /// <returns>The ABOR command result.</returns>
    public FtpCommandResult AbortTransfer()
    {
        State.RecordOperation("AbortTransfer");
        State.RequireOpenSession("AbortTransfer");
        if (State.Options == null || State.Connection == null)
        {
            throw State.CreateUnavailableOperation("AbortTransfer");
        }
        return new FtpClientAbortOperation(State).AbortCurrentTransfer();
    }

    /// <summary>
    /// Verifies a transfer without transferring content.
    /// </summary>
    /// <param name="request">The verification request.</param>
    /// <returns>The verification result.</returns>
    public FtpTransferResult VerifyTransfer(FtpTransferRequest request)
    {
        State.RecordOperation("VerifyTransfer");
        State.RequireOpenSession("VerifyTransfer");
        if (State.Options == null || State.Connection == null)
        {
            throw State.CreateUnavailableOperation("VerifyTransfer");
        }
        return new FtpClientChecksumOperation(State).VerifyTransfer(request);
    }

    /// <summary>
    /// Gets the latest accepted or emitted restart marker.
    /// </summary>
    /// <returns>The marker text, or null when unavailable.</returns>
    public string? GetLatestRestartMarker()
    {
        State.RecordOperation("GetLatestRestartMarker");
        return State.LatestRestartMarker;
    }

    /// <summary>
    /// Builds a deterministic directory transfer plan.
    /// </summary>
    /// <param name="request">The recursive transfer request.</param>
    /// <returns>The non-mutating plan.</returns>
    public FtpDirectoryPlan PlanDirectoryTransfer(FtpDirectoryTransferRequest request)
    {
        State.RecordOperation("PlanDirectoryTransfer");
        State.RequireOpenSession("PlanDirectoryTransfer");
        return new FtpClientDirectoryPlanningOperation(State)
            .Plan(request);
    }

    /// <summary>
    /// Executes a recursive directory transfer.
    /// </summary>
    /// <param name="request">The recursive transfer request.</param>
    /// <returns>The aggregate transfer result.</returns>
    public FtpDirectoryTransferResult TransferDirectory(
        FtpDirectoryTransferRequest request)
    {
        State.RecordOperation("TransferDirectory");
        State.RequireOpenSession("TransferDirectory");
        if (State.Options == null || State.Connection == null)
        {
            throw State.CreateUnavailableOperation("TransferDirectory");
        }
        if (request != null && request.Options != null &&
            request.Options.DryRun)
        {
            FtpClientDirectoryPlanningOperation planning =
                new FtpClientDirectoryPlanningOperation(State);
            return planning.CreateDryRunResult(planning.Plan(request));
        }
        if (request != null && request.Options != null &&
            request.Options.SyncMode != FtpDirectorySyncMode.Copy)
        {
            return new FtpClientDirectorySynchronizationOperation(State)
                .Synchronize(request);
        }
        if (request == null ||
            request.Direction == FtpTransferDirection.Download)
        {
            return new FtpClientDirectoryDownloadOperation(State)
                .Download(request);
        }
        return new FtpClientDirectoryUploadOperation(State)
            .Upload(request);
    }

    /// <summary>
    /// Executes a direct server-to-server transfer.
    /// </summary>
    /// <param name="request">The paired-session transfer request.</param>
    /// <returns>The paired-session result.</returns>
    public FtpServerTransferResult TransferServerToServer(
        FtpServerTransferRequest request)
    {
        State.RecordOperation("TransferServerToServer");
        return new FtpClientServerTransferOperation(request).Transfer();
    }

    internal FtpCommandResult UpgradeControlTlsCore(
        FtpTlsOptions tlsOptions,
        FtpClientAuthTlsCoordinator coordinator,
        FtpCommand command)
    {
        FtpClientOptions? clientOptions = State.Options;
        FtpControlConnection? connection = State.Connection;
        if (clientOptions == null || connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for AUTH TLS.", null);
        }

        FtpControlCommandExchange exchange =
            new FtpControlCommandExchange(clientOptions);
        FtpReply reply = exchange.ExecuteCommand(connection, command);
        coordinator.ApplyAuthorizationReply(reply);
        FtpCommandResult result = CreateSecurityCommandResult(command, reply);
        if (!coordinator.State.HandshakePending)
        {
            result.ConnectionClosed = true;
            State.CloseSession();
            return result;
        }

        FtpClientSecurityConnector security =
            new FtpClientSecurityConnector(
                clientOptions,
                State.CertificateValidator,
                State.Session);
        security.AuthenticateTls(connection, false, reply, tlsOptions);
        coordinator.CompleteHandshake();
        State.TlsCoordinator = coordinator;
        clientOptions.Tls = tlsOptions;
        clientOptions.SecurityMode = FtpSecurityMode.ExplicitTlsRequired;
        State.Features = CreateFeatureNegotiator().NegotiateFeatureSet();
        State.UpdateReconnectState();
        return result;
    }

    internal FtpCommandResult ClearControlTlsCore(
        FtpClientClearControlCoordinator coordinator,
        FtpCommand command)
    {
        FtpClientOptions? options = State.Options;
        FtpControlConnection? connection = State.Connection;
        if (options == null || connection == null ||
            connection.TlsClient == null)
        {
            throw new FtpConnectionException(
                "An active protected FTP connection is required for CCC.", null);
        }

        FtpControlCommandExchange exchange =
            new FtpControlCommandExchange(options);
        FtpReply reply = exchange.ExecuteCommand(connection, command);
        FtpCommandResult result = CreateSecurityCommandResult(command, reply);
        coordinator.ApplyClearControlReply(reply);
        if (coordinator.State.ConnectionMustClose)
        {
            result.ConnectionClosed = true;
            State.CloseSession();
            return result;
        }
        if (!coordinator.State.TlsShutdownPending)
        {
            return result;
        }

        coordinator.CompleteClearControl();
        connection.TlsClient = null;
        connection.TlsResult = null;
        connection.Stream = connection.TransportStream;
        connection.ControlProtected = false;
        connection.PlaintextFallback = false;
        State.TlsCoordinator = null;
        State.UpdateReconnectState();
        return result;
    }

    internal FtpCommandResult CreateSecurityCommandResult(
        FtpCommand command,
        FtpReply reply)
    {
        FtpReplyClassification classification =
            FtpReplyClassifier.ClassifyCode(reply.Code);
        FtpCommandResult result = new FtpCommandResult();
        result.Command = command;
        result.Replies = new[] { reply };
        result.FinalReply = reply;
        result.Succeeded = classification.Succeeded;
        result.ConnectionClosed =
            reply.Code == FtpReplyCodes.ServiceUnavailable;
        return result;
    }

    internal void OpenSavedConnection()
    {
        FtpClientOptions? options = State.Options;
        if (options == null)
        {
            throw new FtpConnectionException(
                "An FTP control connection requires retained client options.", null);
        }
        FtpClientSecurityConnector connector = new FtpClientSecurityConnector(
            options, State.CertificateValidator, State.Session);
        FtpControlConnection connection = connector.OpenConnection();
        State.Features = new FtpFeatureSet();
        State.FeatureState = new FtpClientFeatureState();
        State.Connection = connection;
        State.TlsCoordinator = connector.AuthorizationCoordinator;
        FtpFaultIsolationResult loginResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            CompleteConfiguredSession();
            return null;
        });
        if (!loginResult.Succeeded)
        {
            State.CloseSession();
            if (loginResult.Failure != null)
            {
                throw loginResult.Failure;
            }

            throw new FtpAuthenticationException(
                "The configured FTP login did not reach a terminal result.", null);
        }
    }

    internal void CompleteConfiguredLogin()
    {
        FtpClientLoginSequence login = CreateLoginSequence();
        FtpClientOptions? options = State.Options;
        if (options == null)
        {
            return;
        }

        string? selectedHost = options.VirtualHost;
        FtpCredentials credentials = options.Credentials;
        if (State.ReconnectInProgress && State.ReconnectState.HasSession)
        {
            selectedHost = State.ReconnectState.SelectedHost;
            if (State.ReconnectState.Authenticated &&
                State.ReconnectState.Credentials != null)
            {
                credentials = State.ReconnectState.Credentials;
            }
        }
        login.RouteVirtualHostSelection(selectedHost);
        if (!login.HasCredentials(credentials))
        {
            return;
        }

        FtpAuthenticationResult result = login.Authenticate(credentials);
        State.LastAuthentication = result;
        if (result.Status != FtpAuthenticationStatus.Success)
        {
            string message = result.FailureMessage == null ?
                "The configured FTP authentication requires additional information." :
                result.FailureMessage;
            throw new FtpAuthenticationException(message, null);
        }
        State.ActiveCredentials =
            FtpClientReconnectState.CopyCredentials(credentials);
    }

    internal void CompleteConfiguredSession()
    {
        FtpClientOptions? options = State.Options;
        bool refreshRequired = State.TlsCoordinator != null &&
            State.TlsCoordinator.State.FeatureRefreshRequired;
        bool restoreFeatures = State.ReconnectInProgress &&
            State.ReconnectState.FeaturesNegotiated;
        if (options != null &&
            (options.NegotiateFeatures || refreshRequired || restoreFeatures))
        {
            FtpFeatureSet features =
                CreateFeatureNegotiator().NegotiateFeatureSet();
            State.Features = features;
        }
        else if (options != null && options.ClientName != null)
        {
            CreateFeatureNegotiator().ApplyClientIdentification(
                options.ClientName);
        }
        CompleteConfiguredLogin();
        State.UpdateReconnectState();
    }

    internal void RestoreSavedSession(FtpClientReconnectState reconnectState)
    {
        if (!reconnectState.HasSession)
        {
            return;
        }

        RestoreFeatureOptions(reconnectState);
        RestoreTransferParameters(reconnectState);
        if (reconnectState.DataProtectionLevel !=
            State.Session.State.DataProtectionLevel)
        {
            FtpCommandResult protection = SetDataProtection(
                reconnectState.DataProtectionLevel);
            RequireRestoredCommand(protection, FtpReplyCodes.CommandOkay, "PROT");
        }
        if (!string.Equals(
            reconnectState.CurrentDirectory,
            State.Session.State.CurrentDirectory,
            System.StringComparison.Ordinal))
        {
            FtpCommandResult directory = ExecuteClientCommand(
                FtpCommandKind.Cwd, reconnectState.CurrentDirectory);
            RequireRestoredCommand(
                directory, FtpReplyCodes.RequestedFileActionOkay, "CWD");
        }
    }

    internal void RestoreFeatureOptions(FtpClientReconnectState reconnectState)
    {
        if (reconnectState.Utf8Configured &&
            reconnectState.Utf8Enabled != State.FeatureState.Utf8Enabled)
        {
            FtpOptionRequest utf8 = new FtpOptionRequest();
            utf8.CommandName = FtpFeatureNames.Utf8;
            utf8.Value = reconnectState.Utf8Enabled ? "ON" : "OFF";
            RequireRestoredOption(
                CreateFeatureNegotiator().ApplyOptionRequest(utf8));
        }

        if (reconnectState.MlstConfigured &&
            !AreEqualStrings(
                reconnectState.MlstFacts, State.FeatureState.MlstFacts))
        {
            FtpOptionRequest mlst = new FtpOptionRequest();
            mlst.CommandName = FtpFeatureNames.Mlst;
            mlst.Value = FtpOptionProcessor.FormatFactList(
                reconnectState.MlstFacts);
            RequireRestoredOption(
                CreateFeatureNegotiator().ApplyOptionRequest(mlst));
        }

        int index = 0;
        while (index < reconnectState.ExtensionOptions.Length)
        {
            RequireRestoredOption(
                CreateFeatureNegotiator().ApplyOptionRequest(
                    reconnectState.ExtensionOptions[index]));
            index++;
        }
    }

    internal void RestoreTransferParameters(
        FtpClientReconnectState reconnectState)
    {
        bool representationChanged =
            reconnectState.TransferType != State.Session.State.TransferType ||
            reconnectState.TextFormat != State.Session.State.TextFormat ||
            reconnectState.HasLocalByteSize !=
                State.Session.State.HasLocalByteSize ||
            reconnectState.LocalByteSize != State.Session.State.LocalByteSize;
        if (representationChanged)
        {
            FtpTransferOptions representation = new FtpTransferOptions();
            representation.Type = reconnectState.TransferType;
            representation.TextFormat = reconnectState.TextFormat;
            representation.HasLocalByteSize = reconnectState.HasLocalByteSize;
            representation.LocalByteSize = reconnectState.LocalByteSize;
            FtpCommandResult type = ExecuteClientCommand(
                FtpCommandKind.Type,
                FtpTransferParameterCodec.FormatRepresentation(representation));
            RequireRestoredCommand(type, FtpReplyCodes.CommandOkay, "TYPE");
        }
        if (reconnectState.FileStructure != State.Session.State.FileStructure)
        {
            FtpCommandResult structure = ExecuteClientCommand(
                FtpCommandKind.Stru,
                FtpTransferParameterCodec.FormatStructure(
                    reconnectState.FileStructure));
            RequireRestoredCommand(
                structure, FtpReplyCodes.CommandOkay, "STRU");
        }
        if (reconnectState.TransferMode != State.Session.State.TransferMode)
        {
            FtpCommandResult mode = ExecuteClientCommand(
                FtpCommandKind.Mode,
                FtpTransferParameterCodec.FormatMode(
                    reconnectState.TransferMode));
            RequireRestoredCommand(mode, FtpReplyCodes.CommandOkay, "MODE");
        }
    }

    internal void RequireRestoredOption(FtpCommandResult result)
    {
        if (!result.Succeeded)
        {
            throw new FtpCommandException(
                $"The FTP reconnect could not restore {result.Command.Verb} options.",
                null);
        }
    }

    internal void RequireRestoredCommand(
        FtpCommandResult result,
        int expectedCode,
        string operation)
    {
        if (result.FinalReply == null || result.FinalReply.Code != expectedCode)
        {
            throw new FtpCommandException(
                $"The FTP reconnect could not restore {operation} state.", null);
        }
    }

    internal bool AreEqualStrings(string[] first, string[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (!string.Equals(
                first[index], second[index], System.StringComparison.Ordinal))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal FtpClientLoginSequence CreateLoginSequence()
    {
        FtpClientOptions? options = State.Options;
        FtpControlConnection? connection = State.Connection;
        if (options == null || connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for authentication.", null);
        }

        return new FtpClientLoginSequence(options, State.Session, connection);
    }

    internal FtpClientFeatureNegotiator CreateFeatureNegotiator()
    {
        FtpClientOptions? options = State.Options;
        FtpControlConnection? connection = State.Connection;
        if (options == null || connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for feature negotiation.",
                null);
        }

        return new FtpClientFeatureNegotiator(
            options,
            State.Session,
            connection,
            State.FeatureState,
            State.TlsCoordinator);
    }

    internal FtpClientCommandExecutor CreateCommandExecutor()
    {
        return new FtpClientCommandExecutor(State);
    }

    internal FtpClientDataConnectionStrategy PrepareDataConnection()
    {
        if (State.DataStrategy != null)
        {
            State.DataStrategy.AbortDataConnection();
            State.DataStrategy = null;
        }
        FtpClientDataConnectionStrategy strategy =
            new FtpClientDataConnectionStrategy(State);
        strategy.PrepareDataConnection();
        State.DataStrategy = strategy;
        return strategy;
    }

    internal FtpCommandResult ExecuteClientCommand(
        FtpCommandKind kind,
        string? argument)
    {
        FtpClientCommandExecutor executor = CreateCommandExecutor();
        FtpCommandResult result = executor.ExecuteRawCommand(
            executor.CreateSimpleCommand(kind, argument));
        if (!result.ConnectionClosed)
        {
            State.UpdateReconnectState();
        }
        return result;
    }
}
