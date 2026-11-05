using System;
using System.Globalization;
using System.Text;

namespace ftp;

internal static class FtpServerServiceCommandHandler
{
    internal static readonly string CoreSupportedCommands =
        "USER PASS ACCT REIN QUIT HOST AUTH PBSZ PROT CCC " +
        "TYPE STRU MODE ALLO REST PORT PASV EPRT EPSV ABOR " +
        "CWD CDUP SMNT PWD MKD RMD DELE RNFR RNTO " +
        "RETR STOR STOU APPE LIST NLST STAT MLST MLSD " +
        "MDTM SIZE SYST HELP SITE NOOP FEAT OPTS LANG";

    internal static FtpCommandResult HandleCommand(
        FtpServerCommandContext context)
    {
        switch (context.Command.Kind)
        {
            case FtpCommandKind.Clnt:
                return HandleClientIdentification(context);
            case FtpCommandKind.Mfmt:
                return HandleModificationTimeMutation(context);
            case FtpCommandKind.Hash:
            case FtpCommandKind.Md5:
            case FtpCommandKind.Xmd5:
            case FtpCommandKind.Xsha1:
            case FtpCommandKind.Xsha256:
            case FtpCommandKind.Xsha512:
            case FtpCommandKind.Xcrc:
                return HandleChecksum(context);
            case FtpCommandKind.Mdtm:
                return HandleMetadata(context, false);
            case FtpCommandKind.Size:
                return HandleMetadata(context, true);
            case FtpCommandKind.Syst:
                return HandleSystem(context);
            case FtpCommandKind.Help:
                return HandleHelp(context);
            case FtpCommandKind.Site:
                return HandleSite(context);
            case FtpCommandKind.Noop:
                return FtpServerSession.CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.CommandOkay,
                    "NOOP command successful.",
                    false);
            case FtpCommandKind.Feat:
                return HandleFeatures(context);
            case FtpCommandKind.Opts:
                return HandleOptions(context);
            case FtpCommandKind.Lang:
                return HandleLanguage(context);
            default:
                return FtpServerSession.CreateNotImplementedResult(context);
        }
    }

    internal static FtpCommandResult HandleClientIdentification(
        FtpServerCommandContext context)
    {
        if (!context.Session.Settings.EnableClnt)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "CLNT compatibility is disabled.",
                false);
        }
        FtpFaultIsolationResult parsing =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return FtpClntExtension.ParseClientName(
                    context.Command.Argument);
            });
        string? clientName = parsing.Value as string;
        if (!parsing.Succeeded || clientName == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.SyntaxErrorInParameters,
                "The CLNT identifier is invalid.",
                false);
        }
        context.Session.ClientName = clientName;
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandOkay,
            "Client identification accepted.",
            false);
    }

    internal static FtpCommandResult HandleModificationTimeMutation(
        FtpServerCommandContext context)
    {
        if (!context.Session.Settings.EnableMfmt)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "MFMT compatibility is disabled.",
                false);
        }
        IFtpFileSystem? fileSystem = context.FileSystem;
        if (fileSystem == null)
        {
            return FtpServerFileSystemCommandHandler.CreateUnavailableResult(
                context);
        }
        FtpFaultIsolationResult parsing =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return FtpMfmtExtension.ParseRequest(
                    context.Command.Argument);
            });
        FtpMfmtRequest? mutation = parsing.Value as FtpMfmtRequest;
        if (!parsing.Succeeded || mutation == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.SyntaxErrorInParameters,
                "The MFMT timestamp or pathname is invalid.",
                false);
        }
        FtpFileSystemRequest request =
            FtpServerFileSystemCommandHandler.CreateRequest(
                context,
                FtpPathCodec.ResolveWorkingPath(
                    context.State.State.CurrentDirectory,
                    mutation.Path),
                null);
        request.ModificationTime = mutation.Timestamp;
        FtpFileSystemResult result =
            FtpServerFileSystemCommandHandler.InvokeAuthorized(
                context,
                FtpAuthorizationOperation.WriteMetadata,
                request,
                null,
                () =>
                {
                    return fileSystem.SetModificationTime(request);
                });
        FtpEntry? entry = result.Entry;
        if (!result.Succeeded || entry == null ||
            entry.Modified == null || !entry.Modified.HasValue)
        {
            return CreateMetadataFailure(context);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.FileStatus,
            FtpMfmtExtension.FormatReply(entry.Modified, request.Path),
            false);
    }

    internal static FtpCommandResult HandleMetadata(
        FtpServerCommandContext context,
        bool size)
    {
        IFtpFileSystem? fileSystem = context.FileSystem;
        if (fileSystem == null)
        {
            return FtpServerFileSystemCommandHandler.CreateUnavailableResult(
                context);
        }
        FtpFileSystemRequest request =
            FtpServerFileSystemCommandHandler.CreatePathRequest(context);
        if (!FtpServerFileSystemCommandHandler.Authorize(
            context,
            FtpAuthorizationOperation.ReadMetadata,
            request,
            null))
        {
            return CreateMetadataFailure(context);
        }
        FtpFileSystemResult result =
            FtpServerFileSystemCommandHandler.InvokeFileSystem(
                context, () =>
                {
                    return fileSystem.ResolveEntry(request);
                });
        FtpEntry? entry = result.Entry;
        if (!result.Succeeded || entry == null ||
            (size && (!entry.HasSize || entry.Size < 0)) ||
            (!size &&
                (entry.Modified == null || !entry.Modified.HasValue)))
        {
            return CreateMetadataFailure(context);
        }

        string value = size ?
            entry.Size.ToString(CultureInfo.InvariantCulture) :
            FtpTimestampCodec.FormatTimestampValue(entry.Modified);
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.FileStatus,
            value,
            false);
    }

    internal static FtpCommandResult HandleSystem(
        FtpServerCommandContext context)
    {
        string systemType = OperatingSystem.IsWindows() ?
            "Windows_NT" : "UNIX Type: L8";
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.SystemType,
            systemType,
            false);
    }

    internal static FtpCommandResult HandleHelp(
        FtpServerCommandContext context)
    {
        string supportedCommands = BuildSupportedCommands(context);
        string? topic = context.Command.Argument;
        if (topic != null)
        {
            string normalized = topic.ToUpperInvariant();
            if (!ContainsSupportedCommand(
                supportedCommands, normalized))
            {
                return FtpServerSession.CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.CommandNotImplemented,
                    "No help is available for that FTP command.",
                    false);
            }
            string message = normalized == FtpCommandNames.Site ?
                "SITE parameters are supplied by configured extension handlers." :
                normalized + " is supported by this FTP server.";
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.HelpMessage,
                message,
                false);
        }

        FtpReply reply = FtpServerListingCommandHandler.CreateMultilineReply(
            FtpReplyCodes.HelpMessage,
            "Supported FTP commands",
            new string[] { " " + supportedCommands },
            "End of HELP");
        context.Session.ReplyWriter.FormatReply(reply);
        return FtpServerListingCommandHandler.CreateControlResult(
            context, reply);
    }

    internal static FtpCommandResult HandleSite(
        FtpServerCommandContext context)
    {
        if (context.Session.ActiveExtensionHandlers.Length == 0)
        {
            FtpAuthorizationResult authorization =
                context.AuthorizationGate.AuthorizeSiteOperation(
                    context.State.State.Principal,
                    context.Command.Argument,
                    context.State.State.ControlProtected);
            if (!authorization.Allowed)
            {
                return FtpServerSession.CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.RequestedActionNotTaken,
                    "The SITE operation was not authorized.",
                    false);
            }
        }
        string argument = context.Command.Argument == null ?
            "" : context.Command.Argument;
        if (context.Session.Settings.EnableCompatibilityExtensions)
        {
            if (IsSiteCommand(argument, "CHMOD"))
            {
                return HandleSiteChmod(
                    context, GetSiteParameter(argument, "CHMOD"));
            }
            if (IsSiteCommand(argument, "UMASK"))
            {
                return HandleSiteUmask(
                    context, GetSiteParameter(argument, "UMASK"));
            }
            if (IsSiteCommand(argument, "MKDIR"))
            {
                return HandleSiteMkdir(
                    context, GetSiteParameter(argument, "MKDIR"));
            }
            if (IsSiteCommand(argument, "RMDIR"))
            {
                return HandleSiteRmdir(
                    context, GetSiteParameter(argument, "RMDIR"));
            }
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandNotImplemented,
            "The requested SITE parameter is not implemented.",
            false);
    }

    internal static bool IsSiteCommand(string argument, string commandName)
    {
        return string.Equals(
                argument, commandName, StringComparison.OrdinalIgnoreCase) ||
            argument.StartsWith(
                commandName + " ", StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetSiteParameter(
        string argument,
        string commandName)
    {
        if (argument.Length == commandName.Length)
        {
            return "";
        }
        return argument.Substring(commandName.Length + 1);
    }

    internal static FtpCommandResult HandleSiteChmod(
        FtpServerCommandContext context,
        string parameter)
    {
        int separator = parameter.IndexOf(' ');
        if (separator < 0 || separator == parameter.Length - 1)
        {
            return CreateSiteSyntaxFailure(context);
        }
        string modeText = parameter.Substring(0, separator);
        string pathArgument = parameter.Substring(separator + 1);
        int mode = ParseOctalMode(modeText, 3, 4);
        if (mode < 0 || string.IsNullOrWhiteSpace(pathArgument))
        {
            return CreateSiteSyntaxFailure(context);
        }
        IFtpFileSystem? fileSystem = context.FileSystem;
        if (fileSystem == null)
        {
            return FtpServerFileSystemCommandHandler.CreateUnavailableResult(
                context);
        }
        FtpFileSystemRequest request =
            FtpServerFileSystemCommandHandler.CreateRequest(
                context,
                FtpPathCodec.ResolveWorkingPath(
                    context.State.State.CurrentDirectory, pathArgument),
                null);
        request.HasUnixMode = true;
        request.UnixMode = mode;
        FtpFileSystemResult result =
            FtpServerFileSystemCommandHandler.InvokeAuthorized(
                context,
                FtpAuthorizationOperation.WriteMetadata,
                request,
                null,
                () =>
                {
                    return fileSystem.SetPermissions(request);
                });
        if (!result.Succeeded)
        {
            return FtpServerFileSystemCommandHandler.CreateActionFailureResult(
                context);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandOkay,
            "Permissions changed.",
            false);
    }

    internal static FtpCommandResult HandleSiteUmask(
        FtpServerCommandContext context,
        string parameter)
    {
        if (parameter.Length == 0)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandOkay,
                "Current UMASK is " + FormatUnixMode(
                    context.Session.OptionState.UnixUmask, 3) + ".",
                false);
        }
        int mode = ParseOctalMode(parameter, 3, 3);
        if (mode < 0)
        {
            return CreateSiteSyntaxFailure(context);
        }
        int previous = context.Session.OptionState.UnixUmask;
        context.Session.OptionState.UnixUmask = mode;
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandOkay,
            "UMASK changed from " + FormatUnixMode(previous, 3) +
                " to " + FormatUnixMode(mode, 3) + ".",
            false);
    }

    internal static FtpCommandResult HandleSiteMkdir(
        FtpServerCommandContext context,
        string parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter))
        {
            return CreateSiteSyntaxFailure(context);
        }
        IFtpFileSystem? fileSystem = context.FileSystem;
        if (fileSystem == null)
        {
            return FtpServerFileSystemCommandHandler.CreateUnavailableResult(
                context);
        }
        FtpFileSystemRequest request =
            FtpServerFileSystemCommandHandler.CreateRequest(
                context,
                FtpPathCodec.ResolveWorkingPath(
                    context.State.State.CurrentDirectory, parameter),
                null);
        request.Recursive = true;
        FtpServerFileSystemCommandHandler.ApplyDirectoryCreationMode(
            context, request);
        FtpFileSystemResult result =
            FtpServerFileSystemCommandHandler.InvokeAuthorized(
                context,
                FtpAuthorizationOperation.CreateDirectory,
                request,
                null,
                () =>
                {
                    return fileSystem.CreateDirectoryEntry(request);
                });
        if (!result.Succeeded)
        {
            return FtpServerFileSystemCommandHandler.CreateActionFailureResult(
                context);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.PathCreated,
            FtpPathCodec.FormatQuotedPathname(request.Path) +
                " directory tree created.",
            false);
    }

    internal static FtpCommandResult HandleSiteRmdir(
        FtpServerCommandContext context,
        string parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter))
        {
            return CreateSiteSyntaxFailure(context);
        }
        FtpFileSystemRequest request =
            FtpServerFileSystemCommandHandler.CreateRequest(
                context,
                FtpPathCodec.ResolveWorkingPath(
                    context.State.State.CurrentDirectory, parameter),
                null);
        request.Recursive = true;
        return FtpServerFileSystemCommandHandler.RemoveDirectoryTree(
            context, request);
    }

    internal static int ParseOctalMode(
        string value,
        int minimumDigits,
        int maximumDigits)
    {
        if (value.Length < minimumDigits || value.Length > maximumDigits)
        {
            return -1;
        }
        int mode = 0;
        int index = 0;
        while (index < value.Length)
        {
            int digit = value[index] - '0';
            if (digit < 0 || digit > 7)
            {
                return -1;
            }
            mode = (mode * 8) + digit;
            index++;
        }
        return mode;
    }

    internal static string FormatUnixMode(int mode, int digits)
    {
        string value = Convert.ToString(mode, 8);
        while (value.Length < digits)
        {
            value = "0" + value;
        }
        return value;
    }

    internal static FtpCommandResult CreateSiteSyntaxFailure(
        FtpServerCommandContext context)
    {
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.SyntaxErrorInParameters,
            "The SITE compatibility parameters are invalid.",
            false);
    }

    internal static FtpCommandResult HandleFeatures(
        FtpServerCommandContext context)
    {
        FtpFeatureSet features = CreateSessionFeatureSet(context);
        FtpReply reply = FtpFeatureCodec.GenerateFeatureReply(
            features, "Extensions supported", "End");
        context.Session.ReplyWriter.FormatReply(reply);
        return FtpServerListingCommandHandler.CreateControlResult(
            context, reply);
    }

    internal static FtpCommandResult HandleOptions(
        FtpServerCommandContext context)
    {
        FtpFaultIsolationResult parsing =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return FtpOptionCodec.ParseOptionCommand(context.Command);
            });
        FtpOptionRequest? request = parsing.Value as FtpOptionRequest;
        if (!parsing.Succeeded || request == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.SyntaxErrorInParameters,
                "The FTP OPTS request is invalid.",
                false);
        }
        if (request.CommandName == FtpCommandNames.Hash)
        {
            return HandleHashOptions(context, request);
        }
        if (request.CommandName == FtpCommandNames.Mode &&
            request.Value != null &&
            (string.Equals(
                request.Value,
                "Z",
                StringComparison.OrdinalIgnoreCase) ||
             request.Value.StartsWith(
                "Z ",
                StringComparison.OrdinalIgnoreCase)))
        {
            return HandleModeZOptions(context, request);
        }
        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                FtpMlstFeatureInfo mlst =
                    FtpFeatureCodec.ExtractMlstFacts(context.Features);
                bool utf8 = HasFeature(
                    context.Features, FtpFeatureNames.Utf8);
                FtpOptionProcessor processor = new FtpOptionProcessor(
                    mlst, utf8, false, new string[0]);
                processor.State = context.Session.OptionState;
                return processor.ApplyOptionRequest(request);
            });
        FtpOptionApplicationResult? application =
            operation.Value as FtpOptionApplicationResult;
        if (!operation.Succeeded || application == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.SyntaxErrorInParameters,
                "The FTP OPTS request is invalid.",
                false);
        }
        if (!application.Handled || application.Reply == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandParameterNotImplemented,
                "The requested FTP option is not supported.",
                false);
        }
        return FtpServerListingCommandHandler.CreateControlResult(
            context, application.Reply);
    }

    internal static FtpCommandResult HandleLanguage(
        FtpServerCommandContext context)
    {
        FtpFaultIsolationResult parsing =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return ParseLanguageInfo(context.Features);
            });
        FtpServerLanguageInfo? info =
            parsing.Value as FtpServerLanguageInfo;
        if (!parsing.Succeeded || info == null || !info.Available)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "Language negotiation is not available.",
                false);
        }

        string selected = info.DefaultLanguage;
        if (context.Command.Argument != null)
        {
            selected = FtpArgumentCodec.ParseLanguageTag(
                context.Command.Argument);
            if (!ContainsLanguage(info.Languages, selected))
            {
                return FtpServerSession.CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.CommandParameterNotImplemented,
                    "The requested language is not supported.",
                    false);
            }
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandOkay,
            "Language set to " + selected + ".",
            false);
    }

    internal static FtpFeatureSet CreateSessionFeatureSet(
        FtpServerCommandContext context)
    {
        FtpFeatureSet features =
            FtpServerOptionsSnapshot.CopyFeatureSet(context.Features);
        UpdateSessionHashFeature(
            features, context.Session.OptionState.HashAlgorithm);
        FtpServerLanguageInfo info = ParseLanguageInfo(features);
        if (!info.Available)
        {
            return features;
        }
        string selected = context.State.State.Language == null ?
            info.DefaultLanguage : context.State.State.Language;
        int index = 0;
        while (index < features.Features.Length)
        {
            FtpFeature feature = features.Features[index];
            if (string.Equals(
                feature.Name,
                FtpFeatureNames.Lang,
                StringComparison.OrdinalIgnoreCase))
            {
                feature.Parameters = FormatLanguageParameters(
                    info.Languages, selected);
                feature.RawLine = " " + FtpFeatureNames.Lang + " " +
                    feature.Parameters;
                return features;
            }
            index++;
        }
        return features;
    }

    internal static void UpdateSessionHashFeature(
        FtpFeatureSet features,
        string selectedAlgorithm)
    {
        int index = 0;
        while (index < features.Features.Length)
        {
            FtpFeature feature = features.Features[index];
            if (string.Equals(
                feature.Name,
                FtpFeatureNames.Hash,
                StringComparison.OrdinalIgnoreCase))
            {
                feature.Parameters = FtpChecksumExtensions.FormatHashFeature(
                    selectedAlgorithm);
                feature.RawLine = " " + FtpFeatureNames.Hash + " " +
                    feature.Parameters;
                return;
            }
            index++;
        }
    }

    internal static FtpCommandResult HandleHashOptions(
        FtpServerCommandContext context,
        FtpOptionRequest request)
    {
        if (!context.Session.Settings.EnableHash || context.FileSystem == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandParameterNotImplemented,
                "The HASH option is not supported.",
                false);
        }
        FtpFaultIsolationResult selection =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return FtpChecksumExtensions.ParseHashOption(
                    request.Value,
                    context.Session.OptionState.HashAlgorithm);
            });
        string? algorithm = selection.Value as string;
        if (!selection.Succeeded || algorithm == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.SyntaxErrorInParameters,
                "The requested HASH algorithm is not supported.",
                false);
        }
        context.Session.OptionState.HashAlgorithm = algorithm;
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandOkay,
            algorithm,
            false);
    }

    internal static FtpCommandResult HandleModeZOptions(
        FtpServerCommandContext context,
        FtpOptionRequest request)
    {
        if (!context.Session.Settings.EnableModeZ || context.FileSystem == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandParameterNotImplemented,
                "The MODE Z option is not supported.",
                false);
        }
        if (!string.Equals(
            request.Value,
            "Z",
            StringComparison.OrdinalIgnoreCase))
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.SyntaxErrorInParameters,
                "Alternate MODE Z compression settings are not supported.",
                false);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandOkay,
            "MODE Z default ZLIB settings restored.",
            false);
    }

    internal static FtpCommandResult HandleChecksum(
        FtpServerCommandContext context)
    {
        if (!IsChecksumCommandEnabled(context))
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "The checksum command is disabled.",
                false);
        }
        IFtpFileSystem? fileSystem = context.FileSystem;
        if (fileSystem == null)
        {
            return FtpServerFileSystemCommandHandler.CreateUnavailableResult(
                context);
        }
        FtpFileSystemRequest request =
            FtpServerFileSystemCommandHandler.CreatePathRequest(context);
        request.OpenMode = FtpFileOpenMode.Read;
        request.Structure = FtpFileStructure.File;
        if (!FtpServerFileSystemCommandHandler.Authorize(
            context,
            FtpAuthorizationOperation.Read,
            request,
            null))
        {
            return CreateChecksumFailure(
                context,
                context.Command.Kind == FtpCommandKind.Hash ?
                    FtpChecksumExtensions.HashAuthorizationDeniedReplyCode :
                    FtpReplyCodes.RequestedActionNotTaken,
                "The requested checksum was not authorized.");
        }
        FtpFileSystemResult resolved =
            FtpServerFileSystemCommandHandler.InvokeFileSystem(
                context, () => fileSystem.ResolveEntry(request));
        FtpEntry? entry = resolved.Entry;
        if (!resolved.Succeeded || entry == null)
        {
            return CreateChecksumFailure(
                context,
                FtpReplyCodes.RequestedActionNotTaken,
                "The requested checksum source was not found.");
        }
        if (entry.Type == FtpEntryType.Directory)
        {
            return CreateChecksumFailure(
                context,
                FtpReplyCodes.FileNameNotAllowed,
                "A checksum is not available for a directory.");
        }
        FtpPrincipal? principal = context.State.State.Principal;
        if (principal != null &&
            principal.ResourceLimits.HasTransferByteCeiling &&
            entry.HasSize && entry.Size >
                principal.ResourceLimits.TransferByteCeiling)
        {
            return CreateChecksumFailure(
                context,
                FtpReplyCodes.HashUnavailableForEntry,
                "The checksum exceeds the configured resource policy.");
        }
        FtpFaultIsolationResult opening =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return fileSystem.OpenFileRead(request);
            });
        FtpFileOpenResult? open = opening.Value as FtpFileOpenResult;
        if (!opening.Succeeded || open == null || !open.Succeeded ||
            open.Stream == null)
        {
            if (open != null && open.Stream != null &&
                !open.LeaveStreamOpen)
            {
                FtpServerSessionContext.CloseResource(open.Stream);
            }
            if (!opening.Succeeded)
            {
                context.Session.RecordFaultEvent("file-system provider");
            }
            return CreateChecksumFailure(
                context,
                FtpReplyCodes.RequestedActionNotTaken,
                "The checksum source could not be opened.");
        }
        string algorithm = context.Command.Kind == FtpCommandKind.Hash ?
            context.Session.OptionState.HashAlgorithm :
            FtpChecksumExtensions.GetCommandAlgorithm(context.Command.Kind);
        FtpTransferHashRequest hashRequest = new FtpTransferHashRequest();
        hashRequest.Source = open.Stream;
        hashRequest.Algorithm = algorithm;
        hashRequest.BufferBytes = FtpDefaults.TransferBufferBytes;
        hashRequest.CancellationToken =
            context.Session.Settings.Timeouts.CancellationToken;
        hashRequest.DeadlineMilliseconds =
            context.Session.Settings.Timeouts.IdleTimeoutMilliseconds;
        hashRequest.LeaveOpen = open.LeaveStreamOpen;
        FtpFaultIsolationResult calculation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return TransferHash.Calculate(hashRequest);
            });
        FtpTransferHashResult? hash =
            calculation.Value as FtpTransferHashResult;
        if (!calculation.Succeeded || hash == null)
        {
            context.Session.RecordFaultEvent("checksum calculation");
            return CreateChecksumFailure(
                context,
                FtpReplyCodes.PageTypeUnknown,
                "The checksum could not be calculated.");
        }
        if (context.Command.Kind == FtpCommandKind.Hash)
        {
            long length = open.HasLength ? open.Length : hash.BytesRead;
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.FileStatus,
                FtpChecksumExtensions.FormatHashReply(
                    algorithm, length, hash.Checksum, request.Path),
                false);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            context.Command.Kind == FtpCommandKind.Md5 ?
                FtpChecksumExtensions.Md5ReplyCode :
                FtpReplyCodes.RequestedFileActionOkay,
            FtpChecksumExtensions.FormatVendorReply(
                algorithm, hash.Checksum, request.Path),
            false);
    }

    internal static bool IsChecksumCommandEnabled(
        FtpServerCommandContext context)
    {
        FtpCommandKind kind = context.Command.Kind;
        return context.FileSystem != null &&
            ((kind == FtpCommandKind.Hash &&
                    context.Session.Settings.EnableHash) ||
                (kind == FtpCommandKind.Md5 &&
                    context.Session.Settings.EnableMd5) ||
                (kind == FtpCommandKind.Xmd5 &&
                    context.Session.Settings.EnableXmd5) ||
                (kind == FtpCommandKind.Xsha1 &&
                    context.Session.Settings.EnableXsha1) ||
                (kind == FtpCommandKind.Xsha256 &&
                    context.Session.Settings.EnableXsha256) ||
                (kind == FtpCommandKind.Xsha512 &&
                    context.Session.Settings.EnableXsha512) ||
                (kind == FtpCommandKind.Xcrc &&
                    context.Session.Settings.EnableXcrc));
    }

    internal static FtpCommandResult CreateChecksumFailure(
        FtpServerCommandContext context,
        int code,
        string message)
    {
        return FtpServerSession.CreateCommandResult(
            context.Command, code, message, false);
    }

    internal static FtpServerLanguageInfo ParseLanguageInfo(
        FtpFeatureSet features)
    {
        FtpServerLanguageInfo info = new FtpServerLanguageInfo();
        int featureIndex = 0;
        while (featureIndex < features.Features.Length)
        {
            FtpFeature feature = features.Features[featureIndex];
            if (string.Equals(
                feature.Name,
                FtpFeatureNames.Lang,
                StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(feature.Parameters))
                {
                    throw new FtpProtocolException(
                        "The FTP LANG feature requires supported languages.",
                        null);
                }
                ParseLanguageParameters(feature.Parameters, info);
                info.Available = true;
                return info;
            }
            featureIndex++;
        }
        return info;
    }

    internal static void ParseLanguageParameters(
        string parameters,
        FtpServerLanguageInfo info)
    {
        string[] values = parameters.Split(';');
        if (values.Length == 0 ||
            values.Length > FtpDefaults.MaximumFeatureCount)
        {
            throw new FtpProtocolException(
                "The FTP LANG feature language list is invalid.", null);
        }
        string[] languages = new string[values.Length];
        string defaultLanguage = "";
        int index = 0;
        while (index < values.Length)
        {
            string value = values[index];
            bool selected = value.Length > 0 &&
                value[value.Length - 1] == '*';
            if (selected)
            {
                value = value.Substring(0, value.Length - 1);
            }
            string language = FtpArgumentCodec.ParseLanguageTag(value);
            if (ContainsLanguagePrefix(languages, index, language))
            {
                throw new FtpProtocolException(
                    "The FTP LANG feature contains a duplicate language.",
                    null);
            }
            if (selected)
            {
                if (defaultLanguage.Length > 0)
                {
                    throw new FtpProtocolException(
                        "The FTP LANG feature has multiple defaults.", null);
                }
                defaultLanguage = language;
            }
            languages[index] = language;
            index++;
        }
        info.Languages = languages;
        info.DefaultLanguage = defaultLanguage.Length == 0 ?
            languages[0] : defaultLanguage;
    }

    internal static string FormatLanguageParameters(
        string[] languages,
        string selected)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < languages.Length)
        {
            if (index > 0)
            {
                builder.Append(';');
            }
            builder.Append(languages[index]);
            if (string.Equals(
                languages[index], selected,
                StringComparison.OrdinalIgnoreCase))
            {
                builder.Append('*');
            }
            index++;
        }
        return builder.ToString();
    }

    internal static bool HasFeature(
        FtpFeatureSet features,
        string featureName)
    {
        int index = 0;
        while (index < features.Features.Length)
        {
            if (features.Features[index].Support ==
                    FtpFeatureSupport.Supported &&
                string.Equals(
                    features.Features[index].Name,
                    featureName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool ContainsLanguage(
        string[] languages,
        string expected)
    {
        return ContainsLanguagePrefix(
            languages, languages.Length, expected);
    }

    internal static bool ContainsLanguagePrefix(
        string[] languages,
        int count,
        string expected)
    {
        int index = 0;
        while (index < count)
        {
            if (string.Equals(
                languages[index], expected,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static string BuildSupportedCommands(
        FtpServerCommandContext context)
    {
        StringBuilder commands = new StringBuilder(CoreSupportedCommands);
        if (context.Session.Settings.EnableHistoricAliases)
        {
            AppendSupportedCommand(commands, FtpCommandNames.Xcwd);
            AppendSupportedCommand(commands, FtpCommandNames.Xcup);
            AppendSupportedCommand(commands, FtpCommandNames.Xpwd);
            AppendSupportedCommand(commands, FtpCommandNames.Xmkd);
            AppendSupportedCommand(commands, FtpCommandNames.Xrmd);
        }
        if (context.Session.Settings.EnablePret ||
            HasActiveFeature(context.Session, FtpFeatureNames.Pret))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Pret);
        }
        if (context.Session.Settings.EnableClnt ||
            HasActiveFeature(context.Session, FtpFeatureNames.Clnt))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Clnt);
        }
        if ((context.Session.Settings.EnableMfmt &&
             context.FileSystem != null) ||
            HasActiveFeature(context.Session, FtpFeatureNames.Mfmt))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Mfmt);
        }
        if ((context.Session.Settings.EnableHash &&
             context.FileSystem != null) ||
            HasActiveFeature(context.Session, FtpFeatureNames.Hash))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Hash);
        }
        if ((context.Session.Settings.EnableMd5 &&
             context.FileSystem != null) ||
            HasActiveFeature(context.Session, FtpFeatureNames.Md5))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Md5);
        }
        if ((context.Session.Settings.EnableXmd5 &&
             context.FileSystem != null) ||
            HasActiveFeature(context.Session, FtpFeatureNames.Xmd5))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Xmd5);
        }
        if ((context.Session.Settings.EnableXsha1 &&
             context.FileSystem != null) ||
            HasActiveFeature(context.Session, FtpFeatureNames.Xsha1))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Xsha1);
        }
        if ((context.Session.Settings.EnableXsha256 &&
             context.FileSystem != null) ||
            HasActiveFeature(context.Session, FtpFeatureNames.Xsha256))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Xsha256);
        }
        if ((context.Session.Settings.EnableXsha512 &&
             context.FileSystem != null) ||
            HasActiveFeature(context.Session, FtpFeatureNames.Xsha512))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Xsha512);
        }
        if ((context.Session.Settings.EnableXcrc &&
             context.FileSystem != null) ||
            HasActiveFeature(context.Session, FtpFeatureNames.Xcrc))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Xcrc);
        }
        if ((context.Session.Settings.EnableCompatibilityExtensions &&
             context.FileSystem != null) ||
            HasActiveFeature(context.Session, FtpFeatureNames.Rmda))
        {
            AppendSupportedCommand(commands, FtpCommandNames.Rmda);
        }
        return commands.ToString();
    }

    internal static bool HasActiveFeature(
        FtpServerSession session,
        string featureName)
    {
        int index = 0;
        while (index < session.ActiveFeatures.Features.Length)
        {
            if (string.Equals(
                session.ActiveFeatures.Features[index].Name,
                featureName,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void AppendSupportedCommand(
        StringBuilder commands,
        string command)
    {
        commands.Append(' ');
        commands.Append(command);
    }

    internal static bool ContainsSupportedCommand(
        string supportedCommands,
        string command)
    {
        string padded = " " + supportedCommands + " ";
        return padded.IndexOf(
            " " + command + " ",
            StringComparison.Ordinal) >= 0;
    }

    internal static FtpCommandResult CreateMetadataFailure(
        FtpServerCommandContext context)
    {
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.RequestedActionNotTaken,
            "The requested metadata was not available.",
            false);
    }
}
