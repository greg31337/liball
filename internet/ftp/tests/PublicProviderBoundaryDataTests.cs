using System.Net.Security;

namespace ftp.Tests;

public class PublicProviderBoundaryDataTests
{
    [Test]
    public void Principal_starts_unauthenticated_without_roles()
    {
        FtpPrincipal first = new FtpPrincipal();
        FtpPrincipal second = new FtpPrincipal();

        Assert.Multiple(() =>
        {
            Assert.That(first.Name, Is.Empty);
            Assert.That(first.Authenticated, Is.False);
            Assert.That(first.Anonymous, Is.False);
            Assert.That(first.Account, Is.Null);
            Assert.That(first.CertificateIdentity, Is.Null);
            Assert.That(first.Roles, Is.Empty);
            Assert.That(first.Roles, Is.Not.SameAs(second.Roles));
            Assert.That(first.HomeDirectory, Is.Null);
            Assert.That(first.ResourceLimits,
                Is.Not.SameAs(second.ResourceLimits));
        });
    }

    [Test]
    public void Resource_limits_leave_every_optional_ceiling_unavailable()
    {
        FtpResourceLimits limits = new FtpResourceLimits();

        Assert.Multiple(() =>
        {
            Assert.That(limits.HasStorageQuotaBytes, Is.False);
            Assert.That(limits.StorageQuotaBytes, Is.Zero);
            Assert.That(limits.HasTransferByteCeiling, Is.False);
            Assert.That(limits.TransferByteCeiling, Is.Zero);
            Assert.That(limits.HasReadBytesPerSecond, Is.False);
            Assert.That(limits.ReadBytesPerSecond, Is.Zero);
            Assert.That(limits.HasWriteBytesPerSecond, Is.False);
            Assert.That(limits.WriteBytesPerSecond, Is.Zero);
            Assert.That(limits.HasListingEntryCap, Is.False);
            Assert.That(limits.ListingEntryCap, Is.Zero);
            Assert.That(limits.HasListingByteCap, Is.False);
            Assert.That(limits.ListingByteCap, Is.Zero);
        });
    }

    [Test]
    public void Authentication_request_contains_no_credentials_by_default()
    {
        FtpAuthenticationRequest request = new FtpAuthenticationRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.UserName, Is.Empty);
            Assert.That(request.Password, Is.Null);
            Assert.That(request.Account, Is.Null);
            Assert.That(request.Principal, Is.Null);
            Assert.That(request.CertificateIdentity, Is.Null);
            Assert.That(request.ControlProtected, Is.False);
            Assert.That(request.AttemptNumber, Is.Zero);
        });
    }

    [Test]
    public void Authentication_result_is_not_handled_or_successful()
    {
        FtpAuthenticationResult result = new FtpAuthenticationResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FtpAuthenticationStatus.NotHandled));
            Assert.That(result.Principal, Is.Null);
            Assert.That(result.Challenge, Is.Null);
            Assert.That(result.FailureMessage, Is.Null);
            Assert.That(result.RetryDelayMilliseconds, Is.Zero);
        });
    }

    [Test]
    public void User_record_is_disabled_and_contains_no_verifier()
    {
        FtpUserRecord first = new FtpUserRecord();
        FtpUserRecord second = new FtpUserRecord();

        Assert.Multiple(() =>
        {
            Assert.That(first.UserName, Is.Empty);
            Assert.That(first.Salt, Is.Empty);
            Assert.That(first.Salt, Is.Not.SameAs(second.Salt));
            Assert.That(first.PasswordVerifier, Is.Empty);
            Assert.That(first.PasswordVerifier, Is.Not.SameAs(second.PasswordVerifier));
            Assert.That(first.Roles, Is.Empty);
            Assert.That(first.Roles, Is.Not.SameAs(second.Roles));
            Assert.That(first.HomeDirectory, Is.Null);
            Assert.That(first.Account, Is.Null);
            Assert.That(first.Disabled, Is.True);
            Assert.That(first.AllowWrite, Is.False);
            Assert.That(first.ResourceLimits,
                Is.Not.SameAs(second.ResourceLimits));
        });
    }

    [Test]
    public void Fixed_user_options_require_protection_and_disable_anonymous_access()
    {
        FtpFixedUserOptions first = new FtpFixedUserOptions();
        FtpFixedUserOptions second = new FtpFixedUserOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.Users, Is.Empty);
            Assert.That(first.Users, Is.Not.SameAs(second.Users));
            Assert.That(first.AllowAnonymous, Is.False);
            Assert.That(first.AnonymousName, Is.EqualTo("anonymous"));
            Assert.That(first.RequireProtectedControl, Is.True);
            Assert.That(first.MaximumAttempts, Is.EqualTo(FtpDefaults.MaximumAuthenticationAttempts));
            Assert.That(first.FailureDelayMilliseconds, Is.EqualTo(1000));
        });
    }

    [Test]
    public void Authorization_request_owns_an_unauthenticated_principal()
    {
        FtpAuthorizationRequest first = new FtpAuthorizationRequest();
        FtpAuthorizationRequest second = new FtpAuthorizationRequest();

        Assert.Multiple(() =>
        {
            Assert.That(first.Principal, Is.Not.SameAs(second.Principal));
            Assert.That(first.Operation, Is.EqualTo(FtpAuthorizationOperation.List));
            Assert.That(first.Path, Is.Empty);
            Assert.That(first.DestinationPath, Is.Null);
            Assert.That(first.Entry, Is.Null);
            Assert.That(first.ControlProtected, Is.False);
        });
    }

    [Test]
    public void Authorization_result_is_unhandled_and_denied()
    {
        FtpAuthorizationResult result = new FtpAuthorizationResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(result.Allowed, Is.False);
            Assert.That(result.Reason, Is.Null);
        });
    }

    [Test]
    public void File_system_request_has_no_optional_ranges()
    {
        FtpFileSystemRequest first = new FtpFileSystemRequest();
        FtpFileSystemRequest second = new FtpFileSystemRequest();

        Assert.Multiple(() =>
        {
            Assert.That(first.Principal, Is.Not.SameAs(second.Principal));
            Assert.That(first.Path, Is.Empty);
            Assert.That(first.DestinationPath, Is.Null);
            Assert.That(first.OpenMode, Is.EqualTo(FtpFileOpenMode.Read));
            Assert.That(first.Structure, Is.EqualTo(FtpFileStructure.File));
            Assert.That(first.HasOffset, Is.False);
            Assert.That(first.Offset, Is.Zero);
            Assert.That(first.HasLength, Is.False);
            Assert.That(first.Length, Is.Zero);
            Assert.That(first.UniquePrefix, Is.Null);
            Assert.That(first.ModificationTime, Is.Null);
            Assert.That(first.Recursive, Is.False);
            Assert.That(first.HasUnixMode, Is.False);
            Assert.That(first.UnixMode, Is.Zero);
        });
    }

    [Test]
    public void Space_info_marks_every_capacity_value_unavailable()
    {
        FtpSpaceInfo info = new FtpSpaceInfo();

        Assert.Multiple(() =>
        {
            Assert.That(info.HasTotalBytes, Is.False);
            Assert.That(info.TotalBytes, Is.Zero);
            Assert.That(info.HasAvailableBytes, Is.False);
            Assert.That(info.AvailableBytes, Is.Zero);
            Assert.That(info.HasUsedBytes, Is.False);
            Assert.That(info.UsedBytes, Is.Zero);
            Assert.That(info.QuotaName, Is.Null);
        });
    }

    [Test]
    public void File_system_result_is_unsuccessful_and_owns_empty_entries()
    {
        FtpFileSystemResult first = new FtpFileSystemResult();
        FtpFileSystemResult second = new FtpFileSystemResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Succeeded, Is.False);
            Assert.That(first.Entry, Is.Null);
            Assert.That(first.Entries, Is.Empty);
            Assert.That(first.Entries, Is.Not.SameAs(second.Entries));
            Assert.That(first.Path, Is.Null);
            Assert.That(first.Space, Is.Null);
            Assert.That(first.HasAllocatedBytes, Is.False);
            Assert.That(first.AllocatedBytes, Is.Zero);
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void File_open_result_contains_no_stream_or_length()
    {
        FtpFileOpenResult result = new FtpFileOpenResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Stream, Is.Null);
            Assert.That(result.LeaveStreamOpen, Is.False);
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.HasLength, Is.False);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Virtual_host_request_contains_no_endpoint_or_sni()
    {
        FtpVirtualHostRequest request = new FtpVirtualHostRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.HostName, Is.Empty);
            Assert.That(request.ServerName, Is.Null);
            Assert.That(request.LocalEndpoint, Is.Null);
            Assert.That(request.RemoteEndpoint, Is.Null);
            Assert.That(request.ImplicitTls, Is.False);
        });
    }

    [Test]
    public void Virtual_host_result_is_unhandled_and_not_found()
    {
        FtpVirtualHostResult result = new FtpVirtualHostResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(result.Found, Is.False);
            Assert.That(result.Host, Is.Null);
            Assert.That(result.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Certificate_request_starts_without_host_or_client_certificate_requirement()
    {
        FtpCertificateRequest request = new FtpCertificateRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.HostName, Is.Empty);
            Assert.That(request.ServerName, Is.Null);
            Assert.That(request.LocalEndpoint, Is.Null);
            Assert.That(request.RemoteEndpoint, Is.Null);
            Assert.That(request.ImplicitTls, Is.False);
            Assert.That(request.RequestClientCertificate, Is.False);
        });
    }

    [Test]
    public void Certificate_result_contains_no_certificate_material()
    {
        FtpCertificateResult first = new FtpCertificateResult();
        FtpCertificateResult second = new FtpCertificateResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Handled, Is.False);
            Assert.That(first.Succeeded, Is.False);
            Assert.That(first.Certificate, Is.Null);
            Assert.That(first.Intermediates, Is.Empty);
            Assert.That(first.Intermediates, Is.Not.SameAs(second.Intermediates));
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Certificate_validation_request_preserves_platform_policy_state()
    {
        FtpCertificateValidationRequest request = new FtpCertificateValidationRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.TargetHost, Is.Empty);
            Assert.That(request.Certificate, Is.Null);
            Assert.That(request.Chain, Is.Null);
            Assert.That(request.PolicyErrors, Is.EqualTo(SslPolicyErrors.None));
            Assert.That(request.RemoteEndpoint, Is.Null);
            Assert.That(request.ServerCertificate, Is.True);
        });
    }

    [Test]
    public void Certificate_validation_result_does_not_accept_by_default()
    {
        FtpCertificateValidationResult result = new FtpCertificateValidationResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Connection_profile_starts_non_viable_without_a_greeting()
    {
        FtpConnectionProfile first = new FtpConnectionProfile();
        FtpConnectionProfile second = new FtpConnectionProfile();

        Assert.Multiple(() =>
        {
            Assert.That(first.Endpoint, Is.Not.SameAs(second.Endpoint));
            Assert.That(first.SecurityMode,
                Is.EqualTo(FtpSecurityMode.ExplicitTlsRequired));
            Assert.That(first.Viable, Is.False);
            Assert.That(first.ControlProtected, Is.False);
            Assert.That(first.Greeting, Is.Null);
            Assert.That(first.Features, Is.Not.SameAs(second.Features));
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Connection_probe_owns_an_empty_incomplete_profile_collection()
    {
        FtpConnectionProbeResult first = new FtpConnectionProbeResult();
        FtpConnectionProbeResult second = new FtpConnectionProbeResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Profiles, Is.Empty);
            Assert.That(first.Profiles, Is.Not.SameAs(second.Profiles));
            Assert.That(first.Complete, Is.False);
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Extension_context_owns_command_and_feature_state()
    {
        FtpExtensionContext first = new FtpExtensionContext();
        FtpExtensionContext second = new FtpExtensionContext();

        Assert.Multiple(() =>
        {
            Assert.That(first.Command, Is.Not.SameAs(second.Command));
            Assert.That(first.Principal, Is.Null);
            Assert.That(first.SessionState, Is.EqualTo(FtpSessionState.Disconnected));
            Assert.That(first.CurrentDirectory, Is.Empty);
            Assert.That(first.ControlProtected, Is.False);
            Assert.That(first.TransferInProgress, Is.False);
            Assert.That(first.Features, Is.Not.SameAs(second.Features));
        });
    }

    [Test]
    public void Extension_result_does_not_handle_or_close()
    {
        FtpExtensionResult first = new FtpExtensionResult();
        FtpExtensionResult second = new FtpExtensionResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Disposition, Is.EqualTo(FtpExtensionDisposition.NotHandled));
            Assert.That(first.Replies, Is.Empty);
            Assert.That(first.Replies, Is.Not.SameAs(second.Replies));
            Assert.That(first.CloseConnection, Is.False);
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Transfer_filter_request_owns_an_empty_entry()
    {
        FtpTransferFilterRequest first = new FtpTransferFilterRequest();
        FtpTransferFilterRequest second = new FtpTransferFilterRequest();

        Assert.Multiple(() =>
        {
            Assert.That(first.Direction, Is.EqualTo(FtpTransferDirection.Download));
            Assert.That(first.Entry, Is.Not.SameAs(second.Entry));
            Assert.That(first.LocalPath, Is.Empty);
            Assert.That(first.RemotePath, Is.Empty);
            Assert.That(first.Depth, Is.Zero);
            Assert.That(first.Deletion, Is.False);
        });
    }

    [Test]
    public void Event_starts_redacted_without_optional_context()
    {
        FtpEvent first = new FtpEvent();
        FtpEvent second = new FtpEvent();

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(FtpEventKind.Unknown));
            Assert.That(first.HasAddressFamily, Is.False);
            Assert.That(first.AddressFamily,
                Is.EqualTo(default(FtpAddressFamily)));
            Assert.That(first.HasAuthenticationStatus, Is.False);
            Assert.That(first.AuthenticationStatus,
                Is.EqualTo(default(FtpAuthenticationStatus)));
            Assert.That(first.HasAuthorizationOperation, Is.False);
            Assert.That(first.AuthorizationOperation,
                Is.EqualTo(default(FtpAuthorizationOperation)));
            Assert.That(first.HasCollisionPolicy, Is.False);
            Assert.That(first.CollisionPolicy,
                Is.EqualTo(default(FtpCollisionPolicy)));
            Assert.That(first.HasCommandKind, Is.False);
            Assert.That(first.CommandKind,
                Is.EqualTo(default(FtpCommandKind)));
            Assert.That(first.HasDataConnectionMode, Is.False);
            Assert.That(first.DataConnectionMode,
                Is.EqualTo(default(FtpDataConnectionMode)));
            Assert.That(first.HasDataProtectionLevel, Is.False);
            Assert.That(first.DataProtectionLevel,
                Is.EqualTo(default(FtpDataProtectionLevel)));
            Assert.That(first.HasDirectorySyncMode, Is.False);
            Assert.That(first.DirectorySyncMode,
                Is.EqualTo(default(FtpDirectorySyncMode)));
            Assert.That(first.HasEntryType, Is.False);
            Assert.That(first.EntryType,
                Is.EqualTo(default(FtpEntryType)));
            Assert.That(first.HasExtensionDisposition, Is.False);
            Assert.That(first.ExtensionDisposition,
                Is.EqualTo(default(FtpExtensionDisposition)));
            Assert.That(first.HasFailurePolicy, Is.False);
            Assert.That(first.FailurePolicy,
                Is.EqualTo(default(FtpFailurePolicy)));
            Assert.That(first.HasFeatureSupport, Is.False);
            Assert.That(first.FeatureSupport,
                Is.EqualTo(default(FtpFeatureSupport)));
            Assert.That(first.HasFileOpenMode, Is.False);
            Assert.That(first.FileOpenMode,
                Is.EqualTo(default(FtpFileOpenMode)));
            Assert.That(first.HasFileStructure, Is.False);
            Assert.That(first.FileStructure,
                Is.EqualTo(default(FtpFileStructure)));
            Assert.That(first.HasPermission, Is.False);
            Assert.That(first.Permission,
                Is.EqualTo(default(FtpPermission)));
            Assert.That(first.HasReplyCategory, Is.False);
            Assert.That(first.ReplyCategory,
                Is.EqualTo(default(FtpReplyCategory)));
            Assert.That(first.HasReplyKind, Is.False);
            Assert.That(first.ReplyKind,
                Is.EqualTo(default(FtpReplyKind)));
            Assert.That(first.HasResumePolicy, Is.False);
            Assert.That(first.ResumePolicy,
                Is.EqualTo(default(FtpResumePolicy)));
            Assert.That(first.HasSecurityMode, Is.False);
            Assert.That(first.SecurityMode,
                Is.EqualTo(default(FtpSecurityMode)));
            Assert.That(first.HasServerState, Is.False);
            Assert.That(first.ServerState,
                Is.EqualTo(default(FtpServerState)));
            Assert.That(first.HasSessionState, Is.False);
            Assert.That(first.SessionState,
                Is.EqualTo(default(FtpSessionState)));
            Assert.That(first.HasSymlinkPolicy, Is.False);
            Assert.That(first.SymlinkPolicy,
                Is.EqualTo(default(FtpSymlinkPolicy)));
            Assert.That(first.HasTextFormat, Is.False);
            Assert.That(first.TextFormat,
                Is.EqualTo(default(FtpTextFormat)));
            Assert.That(first.HasTransferDirection, Is.False);
            Assert.That(first.TransferDirection,
                Is.EqualTo(default(FtpTransferDirection)));
            Assert.That(first.HasTransferMode, Is.False);
            Assert.That(first.TransferMode,
                Is.EqualTo(default(FtpTransferMode)));
            Assert.That(first.HasTransferType, Is.False);
            Assert.That(first.TransferType,
                Is.EqualTo(default(FtpTransferType)));
            Assert.That(first.HasVerificationMode, Is.False);
            Assert.That(first.VerificationMode,
                Is.EqualTo(default(FtpVerificationMode)));
            Assert.That(first.Timestamp, Is.Not.SameAs(second.Timestamp));
            Assert.That(first.Timestamp.HasValue, Is.False);
            Assert.That(first.Message, Is.Empty);
            Assert.That(first.Path, Is.Null);
            Assert.That(first.RemoteEndpoint, Is.Null);
            Assert.That(first.Principal, Is.Null);
            Assert.That(first.Command, Is.Null);
            Assert.That(first.Reply, Is.Null);
            Assert.That(first.TransferProgress, Is.Null);
            Assert.That(first.TlsDiagnostics, Is.Null);
            Assert.That(first.Redacted, Is.True);
        });
    }

    [Test]
    public void TLS_diagnostics_start_without_negotiated_or_certificate_facts()
    {
        FtpTlsDiagnostics diagnostics = new FtpTlsDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Authenticated, Is.False);
            Assert.That(diagnostics.ServerRole, Is.False);
            Assert.That(diagnostics.ImplicitTls, Is.False);
            Assert.That(diagnostics.ExplicitUpgrade, Is.False);
            Assert.That(diagnostics.DataChannel, Is.False);
            Assert.That(diagnostics.TlsResumeAllowed, Is.False);
            Assert.That(diagnostics.Encrypted, Is.False);
            Assert.That(diagnostics.Signed, Is.False);
            Assert.That(diagnostics.MutuallyAuthenticated, Is.False);
            Assert.That(diagnostics.Protocol, Is.Empty);
            Assert.That(diagnostics.CipherSuite, Is.Empty);
            Assert.That(diagnostics.LocalCertificateAvailable, Is.False);
            Assert.That(diagnostics.RemoteCertificateAvailable, Is.False);
            Assert.That(diagnostics.RemoteCertificateSha256Fingerprint, Is.Null);
            Assert.That(diagnostics.SessionReuseEvidenceAvailable, Is.False);
            Assert.That(diagnostics.SessionReused, Is.False);
            Assert.That(diagnostics.SessionMatchesControl, Is.False);
        });
    }
}
