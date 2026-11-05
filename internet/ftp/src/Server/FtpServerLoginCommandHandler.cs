using System;
using System.Threading;

namespace ftp;

internal static class FtpServerLoginCommandHandler
{
    internal static FtpCommandResult HandleCommand(
        FtpServerCommandContext context)
    {
        FtpSequenceValidationResult sequence =
            FtpSequenceValidator.InspectCommand(
                context.Command, context.State.State);
        if (!sequence.Allowed)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                sequence.ReplyCode,
                sequence.Message,
                sequence.ReplyCode == FtpReplyCodes.ServiceUnavailable);
        }
        if (context.Command.Kind == FtpCommandKind.Lprt ||
            context.Command.Kind == FtpCommandKind.Lpsv)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "The obsolete FTP long-address command is not supported.",
                false);
        }
        if (context.Command.Kind == FtpCommandKind.Algs)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "ALGS is not applicable to this FTP endpoint.",
                false);
        }
        if (context.Session.Settings.EnablePret &&
            FtpReplyCoordinator.IsDataCommand(context.Command.Kind))
        {
            if (!context.Session.MatchesPretTransfer(context.Command))
            {
                context.Session.ClearPretIntent();
                context.Session.DiscardDataConnectionSetup();
                return FtpServerSession.CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.RequestedActionNotTaken,
                    "The transfer command does not match the prepared PRET intent.",
                    false);
            }
            context.Session.ClearPretIntent();
        }

        switch (context.Command.Kind)
        {
            case FtpCommandKind.User:
            case FtpCommandKind.Pass:
            case FtpCommandKind.Acct:
                return HandleAuthenticationStep(context);
            case FtpCommandKind.Rein:
                return HandleReinitialize(context);
            case FtpCommandKind.Quit:
                return FtpServerSession.CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.ServiceClosingControlConnection,
                    "FTP service closing control connection.",
                    true);
            case FtpCommandKind.Auth:
                return HandleAuthorization(context);
            case FtpCommandKind.Pbsz:
                return FtpServerSession.CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.CommandOkay,
                    "Protection buffer size accepted.",
                    false);
            case FtpCommandKind.Prot:
                return FtpServerSession.CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.CommandOkay,
                    "Data protection level accepted.",
                    false);
            case FtpCommandKind.Ccc:
                return HandleClearControl(context);
            default:
                return FtpServerTransferCommandHandler.HandleCommand(context);
        }
    }

    internal static FtpCommandResult HandleAuthenticationStep(
        FtpServerCommandContext context)
    {
        FtpServerSession session = context.Session;
        if (!CredentialsAllowed(session))
        {
            session.RecordPolicyEvent(
                "FTP credentials require a protected control channel.");
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.RequestDeniedByPolicy,
                "FTP credentials require a protected control channel.",
                false);
        }
        if (context.Command.Kind == FtpCommandKind.User)
        {
            if (context.State.State.SessionState ==
                    FtpSessionState.Authenticated &&
                !context.Connection.TryResetAuthentication())
            {
                session.RecordPolicyEvent(
                    "The pending FTP login limit was exceeded.");
                return FtpServerSession.CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.ServiceUnavailable,
                    "No pending FTP login slot is available.",
                    true);
            }
            session.AuthenticationPrincipal = null;
        }
        if (session.AuthenticationAttempts >=
            session.Settings.MaximumAuthenticationAttempts)
        {
            session.RecordPolicyEvent(
                "The FTP authentication attempt limit was exceeded.");
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.ServiceUnavailable,
                "Too many FTP authentication attempts.",
                true);
        }

        session.AuthenticationAttempts++;
        FtpAuthenticationRequest request =
            CreateAuthenticationRequest(context);
        FtpAuthenticationResult result =
            InvokeAuthenticator(context, request);
        session.RecordAuthenticationEvent(request, result);
        ApplyAuthenticationDelay(result);
        if (result.Status ==
            FtpAuthenticationStatus.AdditionalInformationRequired)
        {
            if (result.Principal != null)
            {
                session.AuthenticationPrincipal = result.Principal;
            }
            int replyCode = context.Command.Kind == FtpCommandKind.User &&
                !RequiresAccount(result) ?
                    FtpReplyCodes.UserNameOkayNeedPassword :
                    FtpReplyCodes.NeedAccountForLogin;
            string text = replyCode ==
                FtpReplyCodes.UserNameOkayNeedPassword ?
                    "User name accepted; password required." :
                    "Account information required.";
            return FtpServerSession.CreateCommandResult(
                context.Command, replyCode, text, false);
        }
        if (result.Status == FtpAuthenticationStatus.Success &&
            IsValidAuthenticatedPrincipal(result.Principal))
        {
            FtpPrincipal principal = result.Principal == null ?
                new FtpPrincipal() : result.Principal;
            if (!context.Connection.TryAuthenticateUser(principal.Name))
            {
                session.RecordPolicyEvent(
                    "The authenticated FTP user session limit was exceeded.");
                return FtpServerSession.CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.ServiceUnavailable,
                    "No authenticated FTP user session slot is available.",
                    true);
            }
            session.AuthenticationPrincipal = principal;
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.UserLoggedIn,
                "User logged in; proceed.",
                false);
        }

        if (session.AuthenticationAttempts >=
            session.Settings.MaximumAuthenticationAttempts)
        {
            session.RecordPolicyEvent(
                "The FTP authentication attempt limit was exceeded.");
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.ServiceUnavailable,
                "Too many FTP authentication attempts.",
                true);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.NotLoggedIn,
            "Authentication failed.",
            false);
    }

    internal static FtpAuthenticationRequest CreateAuthenticationRequest(
        FtpServerCommandContext context)
    {
        FtpServerSession session = context.Session;
        FtpAuthenticationRequest request = new FtpAuthenticationRequest();
        request.UserName = context.Command.Kind == FtpCommandKind.User ?
            context.Command.Argument == null ? "" : context.Command.Argument :
            context.State.State.UserName == null ?
                "" : context.State.State.UserName;
        if (context.Command.Kind == FtpCommandKind.Pass)
        {
            request.Password = context.Command.Argument;
        }
        if (context.Command.Kind == FtpCommandKind.Acct)
        {
            request.Account = context.Command.Argument;
        }
        request.Principal = session.AuthenticationPrincipal;
        request.CertificateIdentity = session.CertificateIdentity;
        request.ControlProtected = context.State.State.ControlProtected;
        request.AttemptNumber = session.AuthenticationAttempts;
        return request;
    }

    internal static FtpAuthenticationResult InvokeAuthenticator(
        FtpServerCommandContext context,
        FtpAuthenticationRequest request)
    {
        if (context.Authenticator == null)
        {
            return CreateAuthenticationFailure();
        }
        FtpFaultIsolationResult invocation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return context.Authenticator.AuthenticateIdentity(request);
            });
        FtpAuthenticationResult? result =
            invocation.Value as FtpAuthenticationResult;
        if (!invocation.Succeeded || result == null ||
            !IsKnownAuthenticationStatus(result.Status))
        {
            if (!invocation.Succeeded)
            {
                context.Session.RecordFaultEvent(
                    "authentication provider");
            }
            return CreateAuthenticationFailure();
        }
        return result;
    }

    internal static FtpAuthenticationResult CreateAuthenticationFailure()
    {
        FtpAuthenticationResult failure = new FtpAuthenticationResult();
        failure.Status = FtpAuthenticationStatus.Failure;
        failure.FailureMessage = "Authentication failed.";
        return failure;
    }

    internal static FtpCommandResult HandleReinitialize(
        FtpServerCommandContext context)
    {
        if (!context.Connection.TryResetAuthentication())
        {
            context.Session.RecordPolicyEvent(
                "The pending FTP login limit was exceeded.");
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.ServiceUnavailable,
                "No pending FTP login slot is available.",
                true);
        }
        context.Session.AuthenticationAttempts = 0;
        context.Session.AuthenticationPrincipal = null;
        context.Connection.ResetHostSelection();
        context.Session.ResetActiveHost();
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.ServiceReady,
            "FTP session reinitialized; ready for a new user.",
            false);
    }

    internal static FtpCommandResult HandleAuthorization(
        FtpServerCommandContext context)
    {
        FtpServerSession session = context.Session;
        if (!IsTlsMechanism(context.Command.Argument))
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandParameterNotImplemented,
                "Only the TLS and TLS-C security mechanisms are supported.",
                false);
        }
        if (context.State.State.SecurityExchangeComplete ||
            context.State.State.ControlProtected)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.BadCommandSequence,
                "The FTP control channel is already protected.",
                false);
        }
        if (!ExplicitTlsAvailable(session))
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.RequestDeniedByPolicy,
                "TLS is not available for this FTP listener.",
                false);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.SecurityExchangeComplete,
            "AUTH TLS accepted; proceed with TLS negotiation.",
            false);
    }

    internal static FtpCommandResult HandleClearControl(
        FtpServerCommandContext context)
    {
        FtpTlsOptions options = ResolveTlsOptions(context.Session);
        if (!options.AllowClearCommandChannel)
        {
            context.Session.RecordPolicyEvent(
                "Clearing the protected FTP control channel is disabled.");
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.RequestDeniedByPolicy,
                "Clearing the protected FTP control channel is disabled.",
                false);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandOkay,
            "Protected command channel will be cleared.",
            false);
    }

    internal static bool CredentialsAllowed(FtpServerSession session)
    {
        if (session.State.State.ControlProtected ||
            session.Context.ListenerSettings.SecurityMode ==
                FtpSecurityMode.Plain)
        {
            return true;
        }
        return session.Context.ListenerSettings.AllowPlaintextCredentials;
    }

    internal static bool ExplicitTlsAvailable(FtpServerSession session)
    {
        FtpSecurityMode mode =
            session.Context.ListenerSettings.SecurityMode;
        return mode == FtpSecurityMode.ExplicitTlsRequired ||
            mode == FtpSecurityMode.ExplicitTlsOpportunistic;
    }

    internal static FtpTlsOptions ResolveTlsOptions(
        FtpServerSession session)
    {
        return session.ActiveHost == null ?
            session.Context.ListenerSettings.Tls : session.ActiveHost.Tls;
    }

    internal static bool IsTlsMechanism(string? mechanism)
    {
        return string.Equals(
                mechanism, "TLS", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                mechanism, "TLS-C", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool RequiresAccount(FtpAuthenticationResult result)
    {
        return result.Principal != null &&
            result.Principal.Authenticated;
    }

    internal static bool IsValidAuthenticatedPrincipal(
        FtpPrincipal? principal)
    {
        return principal != null && principal.Authenticated &&
            !string.IsNullOrWhiteSpace(principal.Name) &&
            !FtpConfigurationValidator.HasInvalidText(principal.Name) &&
            FtpResourceLimitPolicy.AreValid(principal.ResourceLimits);
    }

    internal static bool IsKnownAuthenticationStatus(
        FtpAuthenticationStatus status)
    {
        return status == FtpAuthenticationStatus.NotHandled ||
            status == FtpAuthenticationStatus.Success ||
            status == FtpAuthenticationStatus.Failure ||
            status ==
                FtpAuthenticationStatus.AdditionalInformationRequired;
    }

    internal static void ApplyAuthenticationDelay(
        FtpAuthenticationResult result)
    {
        if (result.Status == FtpAuthenticationStatus.Failure &&
            result.RetryDelayMilliseconds > 0)
        {
            int delay = Math.Min(result.RetryDelayMilliseconds, 10000);
            Thread.Sleep(delay);
        }
    }
}
