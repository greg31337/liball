namespace ftp.Tests;

public class FtpCommandCatalogSequenceTests
{
    [Test]
    public void Catalog_has_one_unique_definition_for_every_command_kind()
    {
        FtpCommandDefinition[] definitions = FtpCommandCatalog.GetDefinitions();
        FtpCommandKind[] kinds = Enum.GetValues<FtpCommandKind>();
        HashSet<FtpCommandKind> foundKinds = new HashSet<FtpCommandKind>();
        HashSet<string> foundVerbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (FtpCommandDefinition definition in definitions)
        {
            Assert.That(foundKinds.Add(definition.Kind), Is.True, definition.Kind.ToString());
            if (definition.Kind != FtpCommandKind.Unknown)
            {
                Assert.That(definition.Verb, Is.Not.Empty);
                Assert.That(foundVerbs.Add(definition.Verb), Is.True, definition.Verb);
                Assert.That(FtpCommandCatalog.ResolveKind(definition.Verb.ToLowerInvariant()),
                    Is.EqualTo(definition.Kind));
                Assert.That(FtpCommandCatalog.ResolveVerb(definition.Kind),
                    Is.EqualTo(definition.Verb));
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(definitions, Has.Length.EqualTo(kinds.Length));
            Assert.That(foundKinds, Is.EquivalentTo(kinds));
            Assert.That(FtpCommandCatalog.ResolveKind("X-NOT-REGISTERED"),
                Is.EqualTo(FtpCommandKind.Unknown));
        });
    }

    [Test]
    public void Catalog_records_argument_alias_obsolete_security_and_compatibility_metadata()
    {
        FtpCommandDefinition feat = RequireDefinition(FtpCommandKind.Feat);
        FtpCommandDefinition list = RequireDefinition(FtpCommandKind.List);
        FtpCommandDefinition cwd = RequireDefinition(FtpCommandKind.Cwd);
        FtpCommandDefinition stou = RequireDefinition(FtpCommandKind.Stou);
        FtpCommandDefinition algs = RequireDefinition(FtpCommandKind.Algs);
        FtpCommandDefinition auth = RequireDefinition(FtpCommandKind.Auth);
        FtpCommandDefinition lprt = RequireDefinition(FtpCommandKind.Lprt);
        FtpCommandDefinition xcwd = RequireDefinition(FtpCommandKind.Xcwd);
        FtpCommandDefinition pret = RequireDefinition(FtpCommandKind.Pret);

        Assert.Multiple(() =>
        {
            Assert.That(feat.ArgumentAllowed, Is.False);
            Assert.That(list.ArgumentAllowed, Is.True);
            Assert.That(list.ArgumentRequired, Is.False);
            Assert.That(cwd.ArgumentRequired, Is.True);
            Assert.That(stou.ArgumentAllowed, Is.False);
            Assert.That(algs.ArgumentRequired, Is.True);
            Assert.That(auth.SecurityCommand, Is.True);
            Assert.That(lprt.Obsolete, Is.True);
            Assert.That(xcwd.HistoricAlias, Is.True);
            Assert.That(xcwd.AliasTarget, Is.EqualTo(FtpCommandKind.Cwd));
            Assert.That(FtpCommandCatalog.FindHistoricAliasForTarget(
                FtpCommandKind.Cwd), Is.SameAs(xcwd));
            Assert.That(FtpCommandCatalog.FindHistoricAliasForTarget(
                FtpCommandKind.Dele), Is.Null);
            Assert.That(pret.Compatibility, Is.True);
        });
    }

    [Test]
    public void Parser_and_writer_use_catalog_mappings_for_every_registered_command()
    {
        foreach (FtpCommandDefinition definition in FtpCommandCatalog.GetDefinitions())
        {
            if (definition.Kind == FtpCommandKind.Unknown)
            {
                continue;
            }

            FtpCommand parsed = FtpCommandParser.ParseDecodedCommand(
                BuildParseableCommand(definition));
            FtpCommand outbound = new FtpCommand();
            outbound.Kind = definition.Kind;
            outbound.Argument = BuildValidArgument(definition.Kind, definition);

            Assert.Multiple(() =>
            {
                Assert.That(parsed.Kind, Is.EqualTo(definition.Kind), definition.Verb);
                Assert.That(FtpCommandWriter.ResolveCanonicalVerb(definition.Kind, "WRONG"),
                    Is.EqualTo(definition.Verb), definition.Verb);
            });
        }
    }

    [Test]
    public void Validator_reports_argument_presence_errors_and_allows_unknown_extensions()
    {
        FtpSequenceState connected = CreateState(FtpSessionState.Connected);
        FtpSequenceState authenticated = CreateState(FtpSessionState.Authenticated);

        FtpSequenceValidationResult featArgument = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Feat, "extra"), connected);
        FtpSequenceValidationResult cwdMissing = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Cwd, null), authenticated);
        FtpSequenceValidationResult listAbsent = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.List, null), authenticated);
        FtpSequenceValidationResult extension = FtpSequenceValidator.InspectCommand(
            CreateUnknownCommand("X-VENDOR", "value"), connected);
        FtpSequenceValidationResult malformedExtension = FtpSequenceValidator.InspectCommand(
            CreateUnknownCommand("1-BAD", null), connected);
        FtpSequenceValidationResult invalidKind = FtpSequenceValidator.InspectCommand(
            CreateInvalidCommand(), connected);

        Assert.Multiple(() =>
        {
            Assert.That(featArgument.ReplyCode, Is.EqualTo(FtpReplyCodes.SyntaxErrorInParameters));
            Assert.That(cwdMissing.ReplyCode, Is.EqualTo(FtpReplyCodes.SyntaxErrorInParameters));
            Assert.That(listAbsent.Allowed, Is.True);
            Assert.That(extension.Allowed, Is.True);
            Assert.That(extension.Definition.Kind, Is.EqualTo(FtpCommandKind.Unknown));
            Assert.That(malformedExtension.ReplyCode, Is.EqualTo(FtpReplyCodes.SyntaxError));
            Assert.That(invalidKind.ReplyCode, Is.EqualTo(FtpReplyCodes.SyntaxError));
        });
    }

    [Test]
    public void Validator_distinguishes_pre_authentication_and_authenticated_commands()
    {
        FtpSequenceState connected = CreateState(FtpSessionState.Connected);
        FtpSequenceState authenticating = CreateState(FtpSessionState.Authenticating);
        FtpSequenceState authenticated = CreateState(FtpSessionState.Authenticated);

        FtpSequenceValidationResult user = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.User, "alice"), connected);
        FtpSequenceValidationResult feat = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Feat, null), connected);
        FtpSequenceValidationResult lang = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Lang, null), connected);
        FtpSequenceValidationResult cwd = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Cwd, "/"), connected);
        FtpSequenceValidationResult hostDuringLogin = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Host, "ftp.example"), authenticating);
        FtpSequenceValidationResult hostAfterLogin = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Host, "ftp.example"), authenticated);
        FtpSequenceValidationResult reauthenticate = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Auth, "TLS"), authenticated);

        Assert.Multiple(() =>
        {
            Assert.That(user.Allowed, Is.True);
            Assert.That(feat.Allowed, Is.True);
            Assert.That(lang.Allowed, Is.True);
            Assert.That(cwd.ReplyCode, Is.EqualTo(FtpReplyCodes.NotLoggedIn));
            Assert.That(hostDuringLogin.Allowed, Is.True);
            Assert.That(hostAfterLogin.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(reauthenticate.Allowed, Is.True);
        });
    }

    [Test]
    public void Validator_enforces_USER_PASS_ACCT_and_AUTH_ADAT_exchanges()
    {
        FtpSequenceState login = CreateState(FtpSessionState.Authenticating);
        FtpSequenceValidationResult earlyPassword = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pass, "secret"), login);
        login.UserNameSupplied = true;
        FtpSequenceValidationResult password = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pass, "secret"), login);
        FtpSequenceValidationResult earlyAccount = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Acct, "billing"), login);
        login.AccountRequired = true;
        FtpSequenceValidationResult account = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Acct, "billing"), login);

        FtpSequenceState security = CreateState(FtpSessionState.Authenticating);
        FtpSequenceValidationResult earlyData = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Adat, "token"), security);
        security.SecurityExchangeInProgress = true;
        FtpSequenceValidationResult data = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Adat, "token"), security);
        FtpSequenceValidationResult interrupted = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Auth, "TLS"), security);

        Assert.Multiple(() =>
        {
            Assert.That(earlyPassword.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(password.Allowed, Is.True);
            Assert.That(earlyAccount.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(account.Allowed, Is.True);
            Assert.That(earlyData.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(data.Allowed, Is.True);
            Assert.That(interrupted.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
        });
    }

    [Test]
    public void Validator_enforces_security_exchange_PBSZ_PROT_and_CCC_ordering()
    {
        FtpSequenceState state = CreateState(FtpSessionState.Connected);
        FtpSequenceValidationResult earlyPbsz = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "0"), state);
        state.SecurityExchangeComplete = true;
        state.SecurityExchangeIsTls = true;
        FtpSequenceValidationResult pbsz = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Pbsz, "0"), state);
        FtpSequenceValidationResult earlyProt = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "P"), state);
        state.ProtectionBufferSizeSet = true;
        FtpSequenceValidationResult clearCcc = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Ccc, null), state);
        state.ControlProtected = true;
        FtpSequenceValidationResult prot = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Prot, "P"), state);
        FtpSequenceValidationResult protectedCcc = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Ccc, null), state);
        FtpSequenceValidationResult protectedCommand = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Mic, "token"), state);

        Assert.Multiple(() =>
        {
            Assert.That(earlyPbsz.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(pbsz.Allowed, Is.True);
            Assert.That(earlyProt.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(prot.Allowed, Is.True);
            Assert.That(clearCcc.ReplyCode, Is.EqualTo(FtpReplyCodes.CommandProtectionDenied));
            Assert.That(protectedCcc.Allowed, Is.True);
            Assert.That(protectedCommand.Allowed, Is.True);
        });
    }

    [Test]
    public void Validator_enforces_RNFR_RNTO_and_EPSV_ALL_sequences()
    {
        FtpSequenceState state = CreateState(FtpSessionState.Authenticated);
        FtpSequenceValidationResult earlyRename = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Rnto, "new-name"), state);
        state.RenameSourceReady = true;
        FtpSequenceValidationResult rename = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Rnto, "new-name"), state);
        state.ExtendedPassiveOnly = true;
        FtpSequenceValidationResult port = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Port, "192,0,2,1,8,1"), state);
        FtpSequenceValidationResult epsv = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Epsv, null), state);

        Assert.Multiple(() =>
        {
            Assert.That(earlyRename.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(rename.Allowed, Is.True);
            Assert.That(port.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(epsv.Allowed, Is.True);
        });
    }

    [Test]
    public void Validator_limits_commands_during_transfer_and_after_connection_closure()
    {
        FtpSequenceState transferring = CreateState(FtpSessionState.Transferring);
        transferring.SecurityExchangeComplete = true;
        FtpCommandKind[] permitted = new FtpCommandKind[]
        {
            FtpCommandKind.Abor,
            FtpCommandKind.Noop,
            FtpCommandKind.Stat,
            FtpCommandKind.Lang,
            FtpCommandKind.Quit,
            FtpCommandKind.Mic
        };

        foreach (FtpCommandKind kind in permitted)
        {
            FtpCommandDefinition definition = RequireDefinition(kind);
            FtpSequenceValidationResult allowed = FtpSequenceValidator.InspectCommand(
                CreateCommand(kind, BuildValidArgument(kind, definition)), transferring);
            Assert.That(allowed.Allowed, Is.True, kind.ToString());
        }

        FtpSequenceValidationResult retr = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Retr, "file.bin"), transferring);
        FtpSequenceState closed = CreateState(FtpSessionState.Closed);
        FtpSequenceValidationResult noop = FtpSequenceValidator.InspectCommand(
            CreateCommand(FtpCommandKind.Noop, null), closed);

        Assert.Multiple(() =>
        {
            Assert.That(retr.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(noop.ReplyCode, Is.EqualTo(FtpReplyCodes.ServiceUnavailable));
            Assert.Throws<FtpProtocolException>(() =>
                FtpSequenceValidator.RequireAllowedCommand(
                    CreateCommand(FtpCommandKind.Retr, "file.bin"), transferring));
        });
    }

    private static FtpCommandDefinition RequireDefinition(FtpCommandKind kind)
    {
        FtpCommandDefinition? definition = FtpCommandCatalog.FindByKind(kind);
        Assert.That(definition, Is.Not.Null);
        return definition!;
    }

    private static FtpSequenceState CreateState(FtpSessionState state)
    {
        FtpSequenceState sequence = new FtpSequenceState();
        sequence.SessionState = state;
        return sequence;
    }

    private static FtpCommand CreateCommand(FtpCommandKind kind, string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = kind == FtpCommandKind.Unknown ? "X-UNKNOWN" :
            RequireDefinition(kind).Verb;
        command.Argument = argument;
        return command;
    }

    private static FtpCommand CreateUnknownCommand(string verb, string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Unknown;
        command.Verb = verb;
        command.Argument = argument;
        return command;
    }

    private static FtpCommand CreateInvalidCommand()
    {
        FtpCommand command = new FtpCommand();
        command.Kind = (FtpCommandKind)999;
        command.Verb = "INVALID";
        return command;
    }

    private static string BuildParseableCommand(FtpCommandDefinition definition)
    {
        string? argument = BuildValidArgument(definition.Kind, definition);
        return argument == null ? definition.Verb : definition.Verb + " " + argument;
    }

    private static string? BuildValidArgument(
        FtpCommandKind kind, FtpCommandDefinition definition)
    {
        if (!definition.ArgumentRequired)
        {
            return null;
        }
        if (kind == FtpCommandKind.Allo)
        {
            return "1";
        }
        if (kind == FtpCommandKind.Rest || kind == FtpCommandKind.Pbsz)
        {
            return "0";
        }
        if (kind == FtpCommandKind.Port)
        {
            return "192,0,2,1,8,1";
        }
        if (kind == FtpCommandKind.Eprt)
        {
            return "|1|192.0.2.1|2049|";
        }
        if (kind == FtpCommandKind.Host)
        {
            return "ftp.example";
        }
        if (kind == FtpCommandKind.Type)
        {
            return "I";
        }
        if (kind == FtpCommandKind.Stru)
        {
            return "F";
        }
        if (kind == FtpCommandKind.Mode)
        {
            return "S";
        }
        if (kind == FtpCommandKind.Lang)
        {
            return "en";
        }
        return "value";
    }
}
