using System.Security.Authentication;
using System.Threading;

namespace ftp;

internal static class FtpConfigurationValidator
{
    internal static void ValidateRemoteEndpoint(FtpEndpoint? endpoint)
    {
        if (endpoint == null)
        {
            throw new FtpConfigurationException("A remote endpoint is required.", null);
        }

        if (string.IsNullOrWhiteSpace(endpoint.Host) || HasInvalidText(endpoint.Host))
        {
            throw new FtpConfigurationException("A remote endpoint host is required and cannot contain control characters.", null);
        }

        ValidateRequiredPort(endpoint.Port, "remote endpoint");
        ValidateAddressFamily(endpoint.AddressFamily);
    }

    internal static void ValidateListenerEndpoint(FtpEndpoint? endpoint)
    {
        if (endpoint == null)
        {
            throw new FtpConfigurationException("A listener endpoint is required.", null);
        }

        if (HasInvalidText(endpoint.Host))
        {
            throw new FtpConfigurationException("A listener endpoint host cannot contain control characters.", null);
        }

        ValidateRequiredPort(endpoint.Port, "listener endpoint");
        ValidateAddressFamily(endpoint.AddressFamily);
    }

    internal static void ValidateBindEndpoint(FtpEndpoint? endpoint)
    {
        if (endpoint == null)
        {
            return;
        }

        if (HasInvalidText(endpoint.Host))
        {
            throw new FtpConfigurationException("A bind endpoint host cannot contain control characters.", null);
        }

        if (endpoint.Port < 0 || endpoint.Port > 65535)
        {
            throw new FtpConfigurationException("A bind endpoint port must be between 0 and 65535.", null);
        }

        ValidateAddressFamily(endpoint.AddressFamily);
    }

    internal static void ValidateTimeoutOptions(FtpTimeoutOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException("Timeout options are required.", null);
        }

        if (options.ConnectTimeoutMilliseconds <= 0 ||
            options.ControlReadTimeoutMilliseconds <= 0 ||
            options.ControlWriteTimeoutMilliseconds <= 0 ||
            options.DataConnectionTimeoutMilliseconds <= 0 ||
            options.TlsHandshakeTimeoutMilliseconds <= 0 ||
            options.IdleTimeoutMilliseconds <= 0 ||
            options.GracefulStopTimeoutMilliseconds <= 0)
        {
            throw new FtpConfigurationException("Every FTP timeout must be a positive bounded duration.", null);
        }
    }

    internal static void ValidateDataConnectionOptions(FtpDataConnectionOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException("Data-connection options are required.", null);
        }

        if (options.Mode != FtpDataConnectionMode.Automatic &&
            options.Mode != FtpDataConnectionMode.Passive &&
            options.Mode != FtpDataConnectionMode.Active)
        {
            throw new FtpConfigurationException("The data-connection mode is invalid.", null);
        }

        FtpSocketSettings.Validate(options.Socket);

        if (options.SendExtendedPassiveAll && !options.UseExtendedPassive)
        {
            throw new FtpConfigurationException("EPSV ALL requires extended passive mode.", null);
        }

        if (options.SendExtendedPassiveAll && options.AllowPassiveFallback)
        {
            throw new FtpConfigurationException("PASV fallback cannot be enabled after EPSV ALL.", null);
        }

        if (options.SendExtendedPassiveAll && options.Mode == FtpDataConnectionMode.Active)
        {
            throw new FtpConfigurationException("EPSV ALL cannot be combined with active-only mode.", null);
        }

        if (options.AllowForeignDataConnections && options.RequireDataPeerMatch)
        {
            throw new FtpConfigurationException("Foreign data connections require control-peer matching to be disabled.", null);
        }

        ValidatePortRange(options.ActivePortStart, options.ActivePortEnd, true, "active");
        ValidatePortRange(options.PassivePortStart, options.PassivePortEnd, false, "passive");
        ValidateBindEndpoint(options.ActiveBindEndpoint);

        if (options.ActiveAdvertisedAddress != null &&
            (string.IsNullOrWhiteSpace(options.ActiveAdvertisedAddress) || HasInvalidText(options.ActiveAdvertisedAddress)))
        {
            throw new FtpConfigurationException("An active advertised address cannot be empty or contain control characters.", null);
        }
    }

    internal static void ValidateTlsOptions(FtpTlsOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException("TLS options are required.", null);
        }

        SslProtocols supportedProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        if (options.Protocols != SslProtocols.None && (options.Protocols & ~supportedProtocols) != 0)
        {
            throw new FtpConfigurationException("TLS protocols may use system defaults or TLS 1.2 and TLS 1.3.", null);
        }

        if (options.DataProtectionLevel != FtpDataProtectionLevel.Clear &&
            options.DataProtectionLevel != FtpDataProtectionLevel.Private)
        {
            throw new FtpConfigurationException("The TLS data-protection level is invalid.", null);
        }

        if (options.RequireDataSessionReuse && options.DataProtectionLevel == FtpDataProtectionLevel.Clear)
        {
            throw new FtpConfigurationException("TLS session reuse requires private data-channel protection.", null);
        }

        if (options.TargetHost != null &&
            (string.IsNullOrWhiteSpace(options.TargetHost) || HasInvalidText(options.TargetHost)))
        {
            throw new FtpConfigurationException("A TLS target host cannot be empty or contain control characters.", null);
        }

        if (options.ServerCertificate != null && !options.ServerCertificate.HasPrivateKey)
        {
            throw new FtpConfigurationException("A server certificate must include its private key.", null);
        }

        if (options.ClientCertificate != null && !options.ClientCertificate.HasPrivateKey)
        {
            throw new FtpConfigurationException("A client certificate must include its private key.", null);
        }
    }

    internal static void ValidateListenerOptions(FtpListenerOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException("Listener options are required.", null);
        }

        ValidateListenerEndpoint(options.Endpoint);
        ValidateSecurityMode(options.SecurityMode);
        ValidateTlsOptions(options.Tls);
        FtpSocketSettings.Validate(options.ControlSocket);

        if (options.Backlog <= 0)
        {
            throw new FtpConfigurationException("A listener backlog must be positive.", null);
        }

        if (options.PassiveAdvertisedAddress != null &&
            (string.IsNullOrWhiteSpace(options.PassiveAdvertisedAddress) || HasInvalidText(options.PassiveAdvertisedAddress)))
        {
            throw new FtpConfigurationException("A passive advertised address cannot be empty or contain control characters.", null);
        }

        if (options.SecurityMode == FtpSecurityMode.ExplicitTlsOpportunistic && !options.AllowPlaintextCredentials)
        {
            throw new FtpConfigurationException("An opportunistic-TLS listener requires explicit plaintext-credential opt-in.", null);
        }
    }

    internal static void ValidateClientOptions(FtpClientOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException("Client options are required.", null);
        }

        ValidateRemoteEndpoint(options.Endpoint);
        ValidateBindEndpoint(options.LocalBindEndpoint);
        ValidateCredentialOptions(options.Credentials);
        ValidateTimeoutOptions(options.Timeouts);
        FtpSocketSettings.Validate(options.ControlSocket);
        ValidateDataConnectionOptions(options.DataConnection);
        ValidateTlsOptions(options.Tls);
        ValidateClientQuirkOptions(options.Quirks);
        ValidateSecurityMode(options.SecurityMode);

        if (options.RetryCount < 0)
        {
            throw new FtpConfigurationException("The retry count cannot be negative.", null);
        }

        if (options.VirtualHost != null &&
            (string.IsNullOrWhiteSpace(options.VirtualHost) || HasInvalidText(options.VirtualHost)))
        {
            throw new FtpConfigurationException("A virtual-host name cannot be empty or contain control characters.", null);
        }

        if (options.ClientName != null)
        {
            FtpClntExtension.ValidateClientName(options.ClientName);
        }

        bool hasCredentials = options.Credentials.Anonymous ||
            !string.IsNullOrEmpty(options.Credentials.UserName) ||
            options.Credentials.Password != null ||
            options.Credentials.Account != null;
        if (hasCredentials && options.SecurityMode == FtpSecurityMode.Plain && !options.AllowPlaintextCredentials)
        {
            throw new FtpConfigurationException("Plaintext credentials require explicit opt-in.", null);
        }

        if (options.SecurityMode == FtpSecurityMode.ExplicitTlsOpportunistic && !options.AllowPlaintextCredentials)
        {
            throw new FtpConfigurationException("Opportunistic TLS requires explicit plaintext fallback opt-in.", null);
        }
    }

    internal static void ValidateClientQuirkOptions(
        FtpClientQuirkOptions? options)
    {
        if (options == null || options.CommandsBeforeAuthorization == null ||
            options.CommandsAfterAuthorization == null)
        {
            throw new FtpConfigurationException(
                "Client quirk options and AUTH command collections are required.", null);
        }
        if (options.CommandsBeforeAuthorization.Length >
            FtpDefaults.MaximumFeatureCount ||
            options.CommandsAfterAuthorization.Length >
                FtpDefaults.MaximumFeatureCount)
        {
            throw new FtpConfigurationException(
                "The configured AUTH compatibility command limit was exceeded.", null);
        }

        ValidateAuthorizationCommands(
            options.CommandsBeforeAuthorization, "before AUTH");
        ValidateAuthorizationCommands(
            options.CommandsAfterAuthorization, "after AUTH");
    }

    internal static void ValidateAuthorizationCommands(
        FtpCommand[] commands,
        string stage)
    {
        int index = 0;
        while (index < commands.Length)
        {
            FtpCommand? command = commands[index];
            if (command == null)
            {
                throw new FtpConfigurationException(
                    $"Every FTP compatibility command {stage} is required.", null);
            }
            string commandLine = FtpCommandWriter.BuildCommandLine(command);
            FtpCommand prepared = FtpCommandParser.ParseDecodedCommand(commandLine);
            bool prohibited = FtpReplyCoordinator.IsDataCommand(prepared.Kind) ||
                prepared.Kind == FtpCommandKind.Auth ||
                prepared.Kind == FtpCommandKind.Adat ||
                prepared.Kind == FtpCommandKind.Mic ||
                prepared.Kind == FtpCommandKind.Conf ||
                prepared.Kind == FtpCommandKind.Enc ||
                prepared.Kind == FtpCommandKind.Ccc ||
                prepared.Kind == FtpCommandKind.User ||
                prepared.Kind == FtpCommandKind.Pass ||
                prepared.Kind == FtpCommandKind.Acct ||
                prepared.Kind == FtpCommandKind.Quit ||
                prepared.Kind == FtpCommandKind.Rein ||
                prepared.Kind == FtpCommandKind.Abor;
            if (prohibited)
            {
                throw new FtpConfigurationException(
                    $"FTP compatibility commands {stage} cannot perform authentication, security, transfer, abort, or disconnect operations.",
                    null);
            }
            index++;
        }
    }

    internal static void ValidateVirtualHostOptions(FtpVirtualHostOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException("Virtual-host options are required.", null);
        }

        if (string.IsNullOrWhiteSpace(options.HostName) || HasInvalidText(options.HostName))
        {
            throw new FtpConfigurationException("A virtual host requires a valid host name.", null);
        }

        if (string.IsNullOrWhiteSpace(options.Greeting) || HasInvalidText(options.Greeting))
        {
            throw new FtpConfigurationException("A virtual host requires a single-line greeting.", null);
        }

        if (string.IsNullOrWhiteSpace(options.RootPath) || HasInvalidText(options.RootPath))
        {
            throw new FtpConfigurationException("A virtual host requires a valid root path.", null);
        }

        ValidateTlsOptions(options.Tls);
        if (options.Features == null || options.Features.Features == null)
        {
            throw new FtpConfigurationException(
                "A virtual host requires a feature collection.", null);
        }
        if (options.ExtensionHandlers != null)
        {
            ValidateExtensionHandlers(options.ExtensionHandlers);
        }
        if (options.Features.Features.Length > FtpDefaults.MaximumFeatureCount)
        {
            throw new FtpConfigurationException(
                "A virtual host feature collection exceeds the configured bound.", null);
        }
        int featureIndex = 0;
        while (featureIndex < options.Features.Features.Length)
        {
            FtpFeature? feature = options.Features.Features[featureIndex];
            if (feature == null || string.IsNullOrWhiteSpace(feature.Name) ||
                HasInvalidText(feature.Name) || feature.RawLine == null ||
                HasInvalidText(feature.RawLine) ||
                (feature.Parameters != null && HasInvalidText(feature.Parameters)))
            {
                throw new FtpConfigurationException(
                    "A virtual host contains an invalid feature.", null);
            }
            featureIndex++;
        }
        if (options.AllowAnonymousWrites && !options.AllowAnonymous)
        {
            throw new FtpConfigurationException("Anonymous writes require anonymous authentication to be enabled.", null);
        }

        if (options.MaximumSessions <= 0 ||
            options.MaximumSessionsPerUser <= 0 ||
            options.MaximumSessionsPerUser > options.MaximumSessions)
        {
            throw new FtpConfigurationException("Virtual-host session limits must be positive and internally consistent.", null);
        }
    }

    internal static void ValidateServerOptions(FtpServerOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException("Server options are required.", null);
        }

        if (options.Listeners == null || options.Listeners.Length == 0)
        {
            throw new FtpConfigurationException("At least one server listener is required.", null);
        }

        if (options.VirtualHosts == null)
        {
            throw new FtpConfigurationException("The virtual-host collection is required.", null);
        }

        ValidateTimeoutOptions(options.Timeouts);
        ValidateDataConnectionOptions(options.DataConnection);
        ValidateTlsOptions(options.Tls);
        ValidateExtensionHandlers(options.ExtensionHandlers);

        int listenerIndex = 0;
        while (listenerIndex < options.Listeners.Length)
        {
            ValidateListenerOptions(options.Listeners[listenerIndex]);
            listenerIndex++;
        }

        int hostIndex = 0;
        while (hostIndex < options.VirtualHosts.Length)
        {
            FtpVirtualHostOptions host = options.VirtualHosts[hostIndex];
            ValidateVirtualHostOptions(host);
            int comparisonIndex = hostIndex + 1;
            while (comparisonIndex < options.VirtualHosts.Length)
            {
                FtpVirtualHostOptions comparison = options.VirtualHosts[comparisonIndex];
                if (comparison != null && string.Equals(host.HostName, comparison.HostName, System.StringComparison.OrdinalIgnoreCase))
                {
                    throw new FtpConfigurationException("Virtual-host names must be unique.", null);
                }

                comparisonIndex++;
            }

            hostIndex++;
        }

        if (options.FixedUsers != null)
        {
            ValidateFixedUserOptions(options.FixedUsers);
        }

        if (options.DefaultHostName != null &&
            (string.IsNullOrWhiteSpace(options.DefaultHostName) || HasInvalidText(options.DefaultHostName)))
        {
            throw new FtpConfigurationException("A default host name cannot be empty or contain control characters.", null);
        }

        if (options.MaximumPendingConnections <= 0 ||
            options.MaximumSessions <= 0 ||
            options.MaximumSessionsPerAddress <= 0 ||
            options.MaximumSessionsPerUser <= 0 ||
            options.MaximumSimultaneousTransfers <= 0 ||
            options.MaximumCommandsPerMinute <= 0 ||
            options.MaximumMalformedLines <= 0 ||
            options.MaximumAuthenticationAttempts <= 0 ||
            options.MaximumListingEntryCount <= 0)
        {
            throw new FtpConfigurationException("Server resource limits must be positive.", null);
        }

        if (options.MaximumSessionsPerAddress > options.MaximumSessions ||
            options.MaximumSessionsPerUser > options.MaximumSessions ||
            options.MaximumSimultaneousTransfers > options.MaximumSessions)
        {
            throw new FtpConfigurationException("Per-address, per-user, and transfer limits cannot exceed the global session limit.", null);
        }

        if (options.AllowForeignDataConnections != options.DataConnection.AllowForeignDataConnections)
        {
            throw new FtpConfigurationException("Server and data-connection foreign-endpoint policies must agree.", null);
        }
    }

    internal static void ValidateExtensionHandlers(
        IFtpExtensionHandler[]? handlers)
    {
        if (handlers == null)
        {
            throw new FtpConfigurationException(
                "The extension-handler collection is required.", null);
        }
        if (handlers.Length > FtpDefaults.MaximumFeatureCount)
        {
            throw new FtpConfigurationException(
                "The extension-handler collection exceeds its configured bound.",
                null);
        }
        int index = 0;
        while (index < handlers.Length)
        {
            if (handlers[index] == null)
            {
                throw new FtpConfigurationException(
                    "Every configured extension handler is required.", null);
            }
            index++;
        }
    }

    internal static void ValidateClientProviderPresence(FtpClientOptions options, IFtpCertificateValidator? certificateValidator)
    {
        if (options.SecurityMode != FtpSecurityMode.Plain && certificateValidator == null)
        {
            throw new FtpConfigurationException("A certificate validator is required for an FTP-over-TLS client.", null);
        }
    }

    internal static void ValidateServerProviderPresence(
        IFtpFileSystem? fileSystem,
        IFtpAuthenticator? authenticator,
        IFtpAuthorizer? authorizer,
        IFtpHostResolver? hostResolver,
        IFtpCertificateProvider? certificateProvider,
        IFtpEventSink? eventSink)
    {
        if (fileSystem == null)
        {
            throw new FtpConfigurationException("A server file-system provider is required.", null);
        }

        if (authenticator == null)
        {
            throw new FtpConfigurationException("A server authenticator is required.", null);
        }

        if (authorizer == null)
        {
            throw new FtpConfigurationException("A server authorizer is required.", null);
        }

        if (hostResolver == null)
        {
            throw new FtpConfigurationException("A server host resolver is required.", null);
        }

        if (certificateProvider == null)
        {
            throw new FtpConfigurationException("A server certificate provider is required.", null);
        }

        if (eventSink == null)
        {
            throw new FtpConfigurationException("A server event sink is required.", null);
        }
    }

    internal static void ValidateFixedUserOptions(FtpFixedUserOptions? options)
    {
        if (options == null || options.Users == null)
        {
            throw new FtpConfigurationException("Fixed-user options and their user collection are required.", null);
        }

        if (options.MaximumAttempts <= 0 || options.FailureDelayMilliseconds < 0)
        {
            throw new FtpConfigurationException("Fixed-user attempt limits and failure delays are invalid.", null);
        }

        if (string.IsNullOrWhiteSpace(options.AnonymousName) ||
            HasInvalidText(options.AnonymousName))
        {
            throw new FtpConfigurationException("Anonymous authentication requires a principal name.", null);
        }

        int index = 0;
        while (index < options.Users.Length)
        {
            FtpUserRecord user = options.Users[index];
            if (user == null || string.IsNullOrWhiteSpace(user.UserName) || user.Salt == null ||
                user.PasswordVerifier == null || user.Roles == null ||
                user.ResourceLimits == null)
            {
                throw new FtpConfigurationException("Every fixed-user record requires a name, verifier arrays, and roles.", null);
            }

            if (!user.Disabled && (user.Salt.Length == 0 || user.PasswordVerifier.Length == 0))
            {
                throw new FtpConfigurationException("An enabled fixed-user record requires a salted password verifier.", null);
            }

            if (HasInvalidText(user.UserName) ||
                HasInvalidOptionalText(user.HomeDirectory) ||
                HasInvalidOptionalText(user.Account))
            {
                throw new FtpConfigurationException(
                    "A fixed-user record contains invalid text.", null);
            }
            ValidateResourceLimits(user.ResourceLimits);
            int roleIndex = 0;
            while (roleIndex < user.Roles.Length)
            {
                string? role = user.Roles[roleIndex];
                if (string.IsNullOrWhiteSpace(role) || HasInvalidText(role))
                {
                    throw new FtpConfigurationException(
                        "A fixed-user role is invalid.", null);
                }
                roleIndex++;
            }
            int comparisonIndex = index + 1;
            while (comparisonIndex < options.Users.Length)
            {
                FtpUserRecord? comparison = options.Users[comparisonIndex];
                if (comparison != null && string.Equals(
                    user.UserName,
                    comparison.UserName,
                    System.StringComparison.Ordinal))
                {
                    throw new FtpConfigurationException(
                        "Fixed-user names must be unique.", null);
                }
                comparisonIndex++;
            }

            index++;
        }
    }

    internal static void ValidateResourceLimits(FtpResourceLimits limits)
    {
        if ((limits.HasStorageQuotaBytes && limits.StorageQuotaBytes < 0) ||
            (!limits.HasStorageQuotaBytes && limits.StorageQuotaBytes != 0) ||
            (limits.HasTransferByteCeiling && limits.TransferByteCeiling < 0) ||
            (!limits.HasTransferByteCeiling && limits.TransferByteCeiling != 0) ||
            (limits.HasReadBytesPerSecond && limits.ReadBytesPerSecond <= 0) ||
            (!limits.HasReadBytesPerSecond && limits.ReadBytesPerSecond != 0) ||
            (limits.HasWriteBytesPerSecond && limits.WriteBytesPerSecond <= 0) ||
            (!limits.HasWriteBytesPerSecond && limits.WriteBytesPerSecond != 0) ||
            (limits.HasListingEntryCap && limits.ListingEntryCap <= 0) ||
            (!limits.HasListingEntryCap && limits.ListingEntryCap != 0) ||
            (limits.HasListingByteCap && limits.ListingByteCap < 0) ||
            (!limits.HasListingByteCap && limits.ListingByteCap != 0))
        {
            throw new FtpConfigurationException(
                "FTP resource-limit flags and values are inconsistent.", null);
        }
    }

    internal static void ValidateCredentialOptions(FtpCredentials? credentials)
    {
        if (credentials == null)
        {
            throw new FtpConfigurationException("Credential options are required.", null);
        }

        if (HasInvalidText(credentials.UserName) ||
            HasInvalidOptionalText(credentials.Password) ||
            HasInvalidOptionalText(credentials.Account))
        {
            throw new FtpConfigurationException("Credentials cannot contain CR, LF, or NUL characters.", null);
        }

        if ((credentials.Anonymous || credentials.Password != null || credentials.Account != null) &&
            string.IsNullOrWhiteSpace(credentials.UserName))
        {
            throw new FtpConfigurationException("Configured credentials require a user name.", null);
        }
    }

    internal static bool HasInvalidText(string text)
    {
        return text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0 || text.IndexOf('\0') >= 0;
    }

    internal static bool HasInvalidOptionalText(string? text)
    {
        return text != null && HasInvalidText(text);
    }

    internal static void ValidateRequiredPort(int port, string endpointName)
    {
        if (port < 1 || port > 65535)
        {
            throw new FtpConfigurationException($"The {endpointName} port must be between 1 and 65535.", null);
        }
    }

    internal static void ValidatePortRange(int start, int end, bool allowOperatingSystemSelection, string rangeName)
    {
        if (allowOperatingSystemSelection && start == 0 && end == 0)
        {
            return;
        }

        if (start < 1 || start > 65535 || end < 1 || end > 65535 || start > end)
        {
            throw new FtpConfigurationException($"The {rangeName} port range must be ordered within 1 through 65535.", null);
        }
    }

    internal static void ValidateAddressFamily(FtpAddressFamily addressFamily)
    {
        if (addressFamily != FtpAddressFamily.Unspecified &&
            addressFamily != FtpAddressFamily.IPv4 &&
            addressFamily != FtpAddressFamily.IPv6)
        {
            throw new FtpConfigurationException("The endpoint address family is invalid.", null);
        }
    }

    internal static void ValidateSecurityMode(FtpSecurityMode securityMode)
    {
        if (securityMode != FtpSecurityMode.Plain &&
            securityMode != FtpSecurityMode.ExplicitTlsRequired &&
            securityMode != FtpSecurityMode.ExplicitTlsOpportunistic &&
            securityMode != FtpSecurityMode.ImplicitTls)
        {
            throw new FtpConfigurationException("The FTP security mode is invalid.", null);
        }
    }
}
