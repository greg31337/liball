namespace ftp;

internal class FtpClientFeatureNegotiator
{
    internal FtpClientOptions Options;
    internal FtpSessionStateMachine Session;
    internal FtpControlConnection Connection;
    internal FtpClientFeatureState State;
    internal FtpClientAuthTlsCoordinator? TlsCoordinator;
    internal FtpControlCommandExchange Exchange;

    internal FtpClientFeatureNegotiator(
        FtpClientOptions options,
        FtpSessionStateMachine session,
        FtpControlConnection connection,
        FtpClientFeatureState state,
        FtpClientAuthTlsCoordinator? tlsCoordinator)
    {
        Options = options;
        Session = session;
        Connection = connection;
        State = state;
        TlsCoordinator = tlsCoordinator;
        Exchange = new FtpControlCommandExchange(options);
    }

    internal FtpFeatureSet NegotiateFeatureSet()
    {
        FtpCommand command = CreateFeatureCommand();
        FtpReply reply = Exchange.ExecuteCommand(Connection, command);
        ApplyFeatureReply(command, reply);
        FtpFeatureSet features;
        if (Options.StrictReplyParsing)
        {
            features = FtpFeatureCodec.ParseFeatureReply(reply);
        }
        else
        {
            features = FtpFeatureCodec.ParseLenientFeatureReply(reply);
        }
        UpdateFeatureState(features);
        if (features.Negotiated)
        {
            ApplyAdaptiveOptions();
        }
        else if (Options.ClientName != null)
        {
            ApplyClientIdentification(Options.ClientName);
        }
        return features;
    }

    internal FtpCommand CreateFeatureCommand()
    {
        if (TlsCoordinator != null &&
            TlsCoordinator.State.FeatureRefreshRequired)
        {
            return TlsCoordinator.BeginFeatureRefresh();
        }

        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Feat;
        command.Verb = FtpCommandNames.Feat;
        command.RawText = FtpCommandNames.Feat;
        FtpSequenceValidator.RequireAllowedCommand(command, Session.State);
        return command;
    }

    internal void ApplyFeatureReply(FtpCommand command, FtpReply reply)
    {
        if (TlsCoordinator != null &&
            TlsCoordinator.State.FeatureRefreshPending)
        {
            TlsCoordinator.ApplyFeatureRefreshReply(reply);
        }
        else
        {
            Session.ApplyCommandReply(command, reply);
        }

        if (reply.Code == FtpReplyCodes.ServiceUnavailable)
        {
            Connection.Close();
            throw new FtpConnectionException(
                "The FTP server closed the connection during FEAT negotiation.",
                null);
        }
    }

    internal void UpdateFeatureState(FtpFeatureSet features)
    {
        State.Features = features;
        Connection.Utf8Enabled = false;
        State.Utf8Enabled = false;
        FtpClientQuirkOptions quirks = Options.Quirks;
        State.Utf8Advertised = !quirks.IgnoreUtf8FeatureClaim &&
            IsSupported(features, FtpFeatureNames.Utf8);
        FtpMlstFeatureInfo mlstInfo = quirks.IgnoreMlstFeatureClaim ?
            new FtpMlstFeatureInfo() :
            FtpFeatureCodec.ExtractMlstFacts(features);
        State.MlstAdvertised = mlstInfo.Advertised;
        State.MachineListing = mlstInfo.Advertised;
        State.FileSize = !quirks.IgnoreSizeFeatureClaim &&
            IsSupported(features, FtpFeatureNames.Size);
        State.ModificationTimeQuery = !quirks.IgnoreMdtmFeatureClaim &&
            IsSupported(features, FtpFeatureNames.Mdtm);
        State.MlstFacts = mlstInfo.DefaultFacts;
        State.RestStream = IsSupported(features, FtpFeatureNames.RestStream);
        State.ExtendedPassive = IsSupported(features, FtpCommandNames.Epsv);
        State.ExtendedActive = IsSupported(features, FtpCommandNames.Eprt);
        State.Host = IsSupported(features, FtpFeatureNames.Host);
        State.Pret = IsSupported(features, FtpFeatureNames.Pret);
        State.ModeZ = IsSupported(features, FtpFeatureNames.ModeZ);
        State.ClientName = IsSupported(features, FtpFeatureNames.Clnt);
        State.ModificationTime = IsSupported(features, FtpFeatureNames.Mfmt);
        State.SiteChmod = IsSupported(features, FtpFeatureNames.SiteChmod);
        State.SiteUmask = IsSupported(features, FtpFeatureNames.SiteUmask);
        State.Hash = IsSupported(features, FtpFeatureNames.Hash);
        State.AuthTls = HasTlsAuthentication(features);
        State.OptionProcessor = new FtpOptionProcessor(
            mlstInfo,
            State.Utf8Advertised,
            false,
            new string[0]);
    }

    internal void ApplyAdaptiveOptions()
    {
        if (Options.ClientName != null && State.ClientName)
        {
            ApplyClientIdentification(Options.ClientName);
        }
        if (Options.EnableUtf8 && State.Utf8Advertised)
        {
            FtpOptionRequest utf8 = new FtpOptionRequest();
            utf8.CommandName = FtpFeatureNames.Utf8;
            utf8.Value = "ON";
            FtpCommandResult utf8Result = ApplyOptionRequest(utf8);
            RequireOptionConnection(utf8Result);
            if (utf8Result.Succeeded)
            {
                State.Utf8Enabled = true;
                Connection.Utf8Enabled = true;
            }
        }

        if (State.MlstAdvertised)
        {
            FtpMlstFeatureInfo mlstInfo =
                FtpFeatureCodec.ExtractMlstFacts(State.Features);
            if (mlstInfo.Facts.Length != 0)
            {
                FtpOptionRequest mlst = new FtpOptionRequest();
                mlst.CommandName = FtpFeatureNames.Mlst;
                mlst.Value = FtpOptionProcessor.FormatFactList(mlstInfo.Facts);
                FtpCommandResult mlstResult = ApplyOptionRequest(mlst);
                RequireOptionConnection(mlstResult);
                if (mlstResult.Succeeded && State.OptionProcessor != null)
                {
                    State.MlstFacts = State.OptionProcessor.State.MlstFacts;
                }
            }
        }
    }

    internal void ApplyClientIdentification(string clientName)
    {
        FtpClntExtension.ValidateClientName(clientName);
        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Clnt;
        command.Verb = FtpCommandNames.Clnt;
        command.Argument = clientName;
        command.RawText = FtpCommandWriter.BuildCommandLine(command);
        FtpSequenceValidator.RequireAllowedCommand(command, Session.State);
        FtpReply reply = Exchange.ExecuteCommand(Connection, command);
        Session.ApplyCommandReply(command, reply);
        if (reply.Code == FtpReplyCodes.ServiceUnavailable)
        {
            Connection.Close();
            throw new FtpConnectionException(
                "The FTP server closed the connection during CLNT identification.",
                null);
        }
    }

    internal FtpCommandResult ApplyOptionRequest(FtpOptionRequest request)
    {
        FtpCommand command = FtpOptionCodec.GenerateOptionCommand(request);
        FtpSequenceValidator.RequireAllowedCommand(command, Session.State);
        FtpReply reply = Exchange.ExecuteCommand(Connection, command);
        Session.ApplyCommandReply(command, reply);
        if (reply.Code == FtpReplyCodes.ServiceUnavailable)
        {
            Connection.Close();
        }

        FtpOptionProcessor processor = EnsureOptionProcessor();
        FtpOptionApplicationResult application =
            processor.AcceptOptionReply(request, reply);
        FtpCommandResult result = new FtpCommandResult();
        result.Command = command;
        result.Replies = new[] { reply };
        result.FinalReply = reply;
        result.Succeeded = application.Accepted;
        result.ConnectionClosed = reply.Code == FtpReplyCodes.ServiceUnavailable;
        if (!application.Accepted)
        {
            result.FailureMessage =
                $"FTP OPTS {request.CommandName} failed with reply {reply.Code}: {reply.Text}";
        }
        else if (string.Equals(
            application.Request.CommandName,
            FtpFeatureNames.Utf8,
            System.StringComparison.Ordinal))
        {
            State.Utf8Enabled = processor.State.Utf8Enabled;
            Connection.Utf8Enabled = processor.State.Utf8Enabled;
        }
        else if (string.Equals(
            application.Request.CommandName,
            FtpFeatureNames.Mlst,
            System.StringComparison.Ordinal))
        {
            State.MlstFacts = processor.State.MlstFacts;
        }
        return result;
    }

    internal FtpOptionProcessor EnsureOptionProcessor()
    {
        if (State.OptionProcessor == null)
        {
            State.OptionProcessor = new FtpOptionProcessor(
                null, false, false, new string[0]);
        }
        return State.OptionProcessor;
    }

    internal void RequireOptionConnection(FtpCommandResult result)
    {
        if (result.ConnectionClosed)
        {
            throw new FtpConnectionException(
                "The FTP server closed the connection during adaptive option negotiation.",
                null);
        }
    }

    internal bool IsSupported(FtpFeatureSet features, string name)
    {
        return FtpFeatureCodec.FindFeatureSupport(features, name) ==
            FtpFeatureSupport.Supported;
    }

    internal bool HasTlsAuthentication(FtpFeatureSet features)
    {
        if (!IsSupported(features, FtpFeatureNames.Auth))
        {
            return false;
        }

        string[] mechanisms =
            FtpFeatureCodec.ExtractAuthenticationMechanisms(features);
        int index = 0;
        while (index < mechanisms.Length)
        {
            if (string.Equals(
                mechanisms[index],
                "TLS",
                System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    mechanisms[index],
                    "TLS-C",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }
}
