namespace ftp;

internal static class FtpServerOptionsSnapshot
{
    internal static FtpServerOptions CreateValidated(FtpServerOptions? options)
    {
        FtpConfigurationValidator.ValidateServerOptions(options);
        if (options == null)
        {
            throw new FtpConfigurationException("Server options are required.", null);
        }

        FtpServerOptions snapshot = new FtpServerOptions();
        snapshot.Listeners = CopyListeners(options.Listeners);
        snapshot.VirtualHosts = CopyVirtualHosts(options.VirtualHosts);
        snapshot.Timeouts = FtpClientOptionsSnapshot.CopyTimeouts(options.Timeouts);
        snapshot.DataConnection =
            FtpClientOptionsSnapshot.CopyDataConnection(options.DataConnection);
        snapshot.Tls = FtpClientOptionsSnapshot.CopyTls(options.Tls);
        snapshot.ExtensionHandlers = CopyExtensionHandlers(
            options.ExtensionHandlers);
        snapshot.FixedUsers = CopyFixedUsers(options.FixedUsers);
        snapshot.DefaultHostName = options.DefaultHostName;
        snapshot.MaximumPendingConnections = options.MaximumPendingConnections;
        snapshot.MaximumSessions = options.MaximumSessions;
        snapshot.MaximumSessionsPerAddress = options.MaximumSessionsPerAddress;
        snapshot.MaximumSessionsPerUser = options.MaximumSessionsPerUser;
        snapshot.MaximumSimultaneousTransfers =
            options.MaximumSimultaneousTransfers;
        snapshot.MaximumCommandsPerMinute = options.MaximumCommandsPerMinute;
        snapshot.MaximumMalformedLines = options.MaximumMalformedLines;
        snapshot.MaximumAuthenticationAttempts =
            options.MaximumAuthenticationAttempts;
        snapshot.MaximumListingEntryCount = options.MaximumListingEntryCount;
        snapshot.AllowForeignDataConnections =
            options.AllowForeignDataConnections;
        snapshot.EnableHistoricAliases = options.EnableHistoricAliases;
        snapshot.EnableCompatibilityExtensions =
            options.EnableCompatibilityExtensions;
        snapshot.EnablePret = options.EnablePret;
        snapshot.EnableClnt = options.EnableClnt;
        snapshot.EnableMfmt = options.EnableMfmt;
        snapshot.EnableModeZ = options.EnableModeZ;
        snapshot.EnableHash = options.EnableHash;
        snapshot.EnableMd5 = options.EnableMd5;
        snapshot.EnableXmd5 = options.EnableXmd5;
        snapshot.EnableXsha1 = options.EnableXsha1;
        snapshot.EnableXsha256 = options.EnableXsha256;
        snapshot.EnableXsha512 = options.EnableXsha512;
        snapshot.EnableXcrc = options.EnableXcrc;
        snapshot.RedactSecrets = options.RedactSecrets;
        return snapshot;
    }

    internal static FtpListenerOptions[] CopyListeners(
        FtpListenerOptions[] listeners)
    {
        FtpListenerOptions[] snapshot =
            new FtpListenerOptions[listeners.Length];
        int index = 0;
        while (index < listeners.Length)
        {
            FtpListenerOptions source = listeners[index];
            FtpListenerOptions copy = new FtpListenerOptions();
            copy.Endpoint = FtpClientOptionsSnapshot.CopyEndpoint(source.Endpoint);
            copy.SecurityMode = source.SecurityMode;
            copy.Tls = FtpClientOptionsSnapshot.CopyTls(source.Tls);
            copy.ControlSocket =
                FtpSocketSettings.CreateSnapshot(source.ControlSocket);
            copy.Backlog = source.Backlog;
            copy.DualStack = source.DualStack;
            copy.AllowPlaintextCredentials = source.AllowPlaintextCredentials;
            copy.PassiveAdvertisedAddress = source.PassiveAdvertisedAddress;
            snapshot[index] = copy;
            index++;
        }
        return snapshot;
    }

    internal static FtpVirtualHostOptions[] CopyVirtualHosts(
        FtpVirtualHostOptions[] virtualHosts)
    {
        FtpVirtualHostOptions[] snapshot =
            new FtpVirtualHostOptions[virtualHosts.Length];
        int index = 0;
        while (index < virtualHosts.Length)
        {
            FtpVirtualHostOptions source = virtualHosts[index];
            FtpVirtualHostOptions copy = new FtpVirtualHostOptions();
            copy.HostName = source.HostName;
            copy.Greeting = source.Greeting;
            copy.RootPath = source.RootPath;
            copy.Tls = FtpClientOptionsSnapshot.CopyTls(source.Tls);
            copy.FileSystem = source.FileSystem;
            copy.Authenticator = source.Authenticator;
            copy.Authorizer = source.Authorizer;
            copy.CertificateProvider = source.CertificateProvider;
            copy.Features = CopyFeatureSet(source.Features);
            copy.ExtensionHandlers = source.ExtensionHandlers == null ?
                null : CopyExtensionHandlers(source.ExtensionHandlers);
            copy.AllowAnonymous = source.AllowAnonymous;
            copy.AllowAnonymousWrites = source.AllowAnonymousWrites;
            copy.MaximumSessions = source.MaximumSessions;
            copy.MaximumSessionsPerUser = source.MaximumSessionsPerUser;
            snapshot[index] = copy;
            index++;
        }
        return snapshot;
    }

    internal static IFtpExtensionHandler[] CopyExtensionHandlers(
        IFtpExtensionHandler[] handlers)
    {
        IFtpExtensionHandler[] copy =
            new IFtpExtensionHandler[handlers.Length];
        int index = 0;
        while (index < handlers.Length)
        {
            copy[index] = handlers[index];
            index++;
        }
        return copy;
    }

    internal static FtpFeatureSet CopyFeatureSet(FtpFeatureSet source)
    {
        FtpFeatureSet copy = new FtpFeatureSet();
        copy.Features = new FtpFeature[source.Features.Length];
        int index = 0;
        while (index < source.Features.Length)
        {
            FtpFeature sourceFeature = source.Features[index];
            FtpFeature feature = new FtpFeature();
            feature.Name = sourceFeature.Name;
            feature.Parameters = sourceFeature.Parameters;
            feature.RawLine = sourceFeature.RawLine;
            feature.Support = sourceFeature.Support;
            copy.Features[index] = feature;
            index++;
        }
        if (source.SourceReply != null)
        {
            FtpCommand command = new FtpCommand();
            copy.SourceReply = FtpTranscriptRedactor.RedactReply(
                command, source.SourceReply);
        }
        copy.Negotiated = source.Negotiated;
        return copy;
    }

    internal static FtpFixedUserOptions? CopyFixedUsers(
        FtpFixedUserOptions? fixedUsers)
    {
        if (fixedUsers == null)
        {
            return null;
        }

        FtpFixedUserOptions snapshot = new FtpFixedUserOptions();
        snapshot.Users = CopyUsers(fixedUsers.Users);
        snapshot.AllowAnonymous = fixedUsers.AllowAnonymous;
        snapshot.AnonymousName = fixedUsers.AnonymousName;
        snapshot.RequireProtectedControl = fixedUsers.RequireProtectedControl;
        snapshot.MaximumAttempts = fixedUsers.MaximumAttempts;
        snapshot.FailureDelayMilliseconds = fixedUsers.FailureDelayMilliseconds;
        return snapshot;
    }

    internal static FtpUserRecord[] CopyUsers(FtpUserRecord[] users)
    {
        FtpUserRecord[] snapshot = new FtpUserRecord[users.Length];
        int index = 0;
        while (index < users.Length)
        {
            FtpUserRecord source = users[index];
            FtpUserRecord copy = new FtpUserRecord();
            copy.UserName = source.UserName;
            copy.Salt = CopyBytes(source.Salt);
            copy.PasswordVerifier = CopyBytes(source.PasswordVerifier);
            copy.Roles = CopyStrings(source.Roles);
            copy.HomeDirectory = source.HomeDirectory;
            copy.Account = source.Account;
            copy.Disabled = source.Disabled;
            copy.AllowWrite = source.AllowWrite;
            copy.ResourceLimits = CopyResourceLimits(source.ResourceLimits);
            snapshot[index] = copy;
            index++;
        }
        return snapshot;
    }

    internal static FtpResourceLimits CopyResourceLimits(
        FtpResourceLimits? source)
    {
        if (source == null)
        {
            return new FtpResourceLimits();
        }
        FtpResourceLimits copy = new FtpResourceLimits();
        copy.HasStorageQuotaBytes = source.HasStorageQuotaBytes;
        copy.StorageQuotaBytes = source.StorageQuotaBytes;
        copy.HasTransferByteCeiling = source.HasTransferByteCeiling;
        copy.TransferByteCeiling = source.TransferByteCeiling;
        copy.HasReadBytesPerSecond = source.HasReadBytesPerSecond;
        copy.ReadBytesPerSecond = source.ReadBytesPerSecond;
        copy.HasWriteBytesPerSecond = source.HasWriteBytesPerSecond;
        copy.WriteBytesPerSecond = source.WriteBytesPerSecond;
        copy.HasListingEntryCap = source.HasListingEntryCap;
        copy.ListingEntryCap = source.ListingEntryCap;
        copy.HasListingByteCap = source.HasListingByteCap;
        copy.ListingByteCap = source.ListingByteCap;
        return copy;
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] snapshot = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            snapshot[index] = source[index];
            index++;
        }
        return snapshot;
    }

    internal static string[] CopyStrings(string[] source)
    {
        string[] snapshot = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            snapshot[index] = source[index];
            index++;
        }
        return snapshot;
    }
}
