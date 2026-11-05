namespace ftp.Tests;

public class FtpCommandCodecContractTests
{
    [Test]
    public void Every_catalog_command_round_trips_through_the_public_contract()
    {
        IFtpCommandCodec codec = new FtpCommandCodec();
        FtpCommandDefinition[] definitions = FtpCommandCatalog.GetDefinitions()
            .Where(definition => definition.Kind != FtpCommandKind.Unknown)
            .ToArray();
        FtpCommandKind[] kinds = Enum.GetValues<FtpCommandKind>()
            .Where(kind => kind != FtpCommandKind.Unknown)
            .ToArray();

        Assert.That(definitions.Select(definition => definition.Kind),
            Is.EquivalentTo(kinds));
        Assert.That(definitions.Any(definition => definition.Standard), Is.True);
        Assert.That(definitions.Any(definition => definition.SecurityCommand),
            Is.True);
        Assert.That(definitions.Any(definition => definition.Compatibility),
            Is.True);
        Assert.That(definitions.Any(definition => definition.HistoricAlias),
            Is.True);
        Assert.That(definitions.Any(definition => definition.Obsolete), Is.True);

        foreach (FtpCommandDefinition definition in definitions)
        {
            string? argument = CreateValidArgument(definition.Kind);
            string raw = definition.Verb.ToLowerInvariant() +
                (argument == null ? "" : " " + argument);

            FtpCommand parsed = codec.ParseCommand(raw);
            string formatted = codec.FormatCommand(parsed);

            Assert.Multiple(() =>
            {
                Assert.That(parsed.Kind, Is.EqualTo(definition.Kind),
                    definition.Verb);
                Assert.That(parsed.Verb, Is.EqualTo(definition.Verb),
                    definition.Verb);
                Assert.That(parsed.Argument, Is.EqualTo(argument),
                    definition.Verb);
                Assert.That(parsed.RawText, Is.EqualTo(raw),
                    definition.Verb);
                Assert.That(formatted,
                    Is.EqualTo(definition.Verb +
                        (argument == null ? "" : " " + argument) + "\r\n"),
                    definition.Verb);
            });
        }
    }

    [Test]
    public void Security_commands_mark_only_secret_bearing_arguments_sensitive()
    {
        IFtpCommandCodec codec = new FtpCommandCodec();
        string[] sensitiveVerbs =
        {
            "PASS", "ACCT", "ADAT", "MIC", "CONF", "ENC"
        };
        string[] nonSecretSecurityVerbs =
        {
            "AUTH", "PBSZ", "PROT", "CCC"
        };

        foreach (string verb in sensitiveVerbs)
        {
            FtpCommand command = codec.ParseCommand(
                verb + (verb == "PASS" ? "" : " secret"));
            codec.FormatCommand(command);
            Assert.That(command.Sensitive, Is.True, verb);
        }
        foreach (string verb in nonSecretSecurityVerbs)
        {
            string? argument = verb == "CCC" ? null :
                verb == "PBSZ" ? "0" : "value";
            FtpCommand command = codec.ParseCommand(
                verb + (argument == null ? "" : " " + argument));
            codec.FormatCommand(command);
            Assert.That(command.Sensitive, Is.False, verb);
        }
    }

    [Test]
    public void Unknown_extension_verbs_are_canonicalized_without_losing_arguments()
    {
        IFtpCommandCodec codec = new FtpCommandCodec();

        FtpCommand absent = codec.ParseCommand("x-vendor2");
        FtpCommand empty = codec.ParseCommand("x-vendor2 ");
        FtpCommand supplied = codec.ParseCommand(
            "x-vendor2 opaque café/資料 😀");

        Assert.Multiple(() =>
        {
            Assert.That(absent.Kind, Is.EqualTo(FtpCommandKind.Unknown));
            Assert.That(absent.Verb, Is.EqualTo("X-VENDOR2"));
            Assert.That(absent.Argument, Is.Null);
            Assert.That(codec.FormatCommand(absent),
                Is.EqualTo("X-VENDOR2\r\n"));
            Assert.That(empty.Argument, Is.Empty);
            Assert.That(codec.FormatCommand(empty),
                Is.EqualTo("X-VENDOR2 \r\n"));
            Assert.That(supplied.Argument,
                Is.EqualTo("opaque café/資料 😀"));
            Assert.That(codec.FormatCommand(supplied),
                Is.EqualTo("X-VENDOR2 opaque café/資料 😀\r\n"));
        });
    }

    [Test]
    public void Unicode_arguments_and_exact_UTF8_control_line_limit_are_preserved()
    {
        IFtpCommandCodec codec = new FtpCommandCodec();
        string boundedLine = "CWD " + new string(
            'a', FtpDefaults.MaximumControlLineBytes - 6);
        string oversizedLine = boundedLine + "a";

        FtpCommand unicode = codec.ParseCommand(
            "CWD café/資料/😀/e\u0301");
        FtpCommand bounded = codec.ParseCommand(boundedLine);

        Assert.Multiple(() =>
        {
            Assert.That(unicode.Argument,
                Is.EqualTo("café/資料/😀/e\u0301"));
            Assert.That(codec.FormatCommand(unicode),
                Is.EqualTo("CWD café/資料/😀/e\u0301\r\n"));
            Assert.That(codec.FormatCommand(bounded).Length,
                Is.EqualTo(FtpDefaults.MaximumControlLineBytes));
            Assert.Throws<FtpProtocolException>(() =>
                codec.ParseCommand(oversizedLine));
        });
    }

    [Test]
    public void ParseCommand_rejects_malformed_verbs_controls_unicode_and_injection()
    {
        IFtpCommandCodec codec = new FtpCommandCodec();
        string[] malformed =
        {
            "",
            " NOOP",
            "1NOOP",
            "_NOOP",
            "NÖOP",
            "NOOP\tvalue",
            "NOOP\r",
            "NOOP\nDELE victim",
            "CWD safe\r\nDELE victim",
            "CWD bad\0path",
            "CWD " + '\ud800',
            "CWD " + '\udc00'
        };

        Assert.Throws<ArgumentNullException>(() => codec.ParseCommand(null!));
        foreach (string line in malformed)
        {
            Assert.Throws<FtpProtocolException>(() => codec.ParseCommand(line),
                line);
        }
    }

    [Test]
    public void ParseCommand_rejects_malformed_command_specific_grammars()
    {
        IFtpCommandCodec codec = new FtpCommandCodec();
        string[] malformed =
        {
            "REST -1",
            "REST 9223372036854775808",
            "PBSZ value",
            "ALLO 1 R value",
            "TYPE Z",
            "STRU Z",
            "MODE Z level=9",
            "PORT 127,0,0,1,0,0",
            "EPRT |1|999.1.1.1|21|",
            "EPSV 0",
            "HOST bad host",
            "LANG en_US"
        };

        foreach (string line in malformed)
        {
            Assert.Throws<FtpProtocolException>(() => codec.ParseCommand(line),
                line);
        }
    }

    [Test]
    public void FormatCommand_canonicalizes_known_kinds_and_rejects_unsafe_commands()
    {
        IFtpCommandCodec codec = new FtpCommandCodec();
        FtpCommand known = new FtpCommand();
        known.Kind = FtpCommandKind.Cwd;
        known.Verb = "NOOP";
        known.Argument = "folder";
        FtpCommand missingUnknownVerb = new FtpCommand();
        missingUnknownVerb.Kind = FtpCommandKind.Unknown;
        FtpCommand invalidUnknownVerb = new FtpCommand();
        invalidUnknownVerb.Kind = FtpCommandKind.Unknown;
        invalidUnknownVerb.Verb = "X BAD";
        FtpCommand invalidKind = new FtpCommand();
        invalidKind.Kind = (FtpCommandKind)999;
        FtpCommand injected = new FtpCommand();
        injected.Kind = FtpCommandKind.Cwd;
        injected.Argument = "safe\r\nDELE victim";
        FtpCommand oversized = new FtpCommand();
        oversized.Kind = FtpCommandKind.Cwd;
        oversized.Argument = new string(
            'a', FtpDefaults.MaximumControlLineBytes);
        FtpCommand invalidUnicode = new FtpCommand();
        invalidUnicode.Kind = FtpCommandKind.Cwd;
        invalidUnicode.Argument = "bad" + '\ud800';

        Assert.Multiple(() =>
        {
            Assert.That(codec.FormatCommand(known),
                Is.EqualTo("CWD folder\r\n"));
            Assert.Throws<ArgumentNullException>(() =>
                codec.FormatCommand(null!));
            Assert.Throws<FtpProtocolException>(() =>
                codec.FormatCommand(missingUnknownVerb));
            Assert.Throws<FtpProtocolException>(() =>
                codec.FormatCommand(invalidUnknownVerb));
            Assert.Throws<FtpProtocolException>(() =>
                codec.FormatCommand(invalidKind));
            Assert.Throws<FtpProtocolException>(() =>
                codec.FormatCommand(injected));
            Assert.Throws<FtpProtocolException>(() =>
                codec.FormatCommand(oversized));
            Assert.Throws<FtpProtocolException>(() =>
                codec.FormatCommand(invalidUnicode));
        });
    }

    private static string? CreateValidArgument(FtpCommandKind kind)
    {
        if (kind == FtpCommandKind.Abor ||
            kind == FtpCommandKind.Cdup ||
            kind == FtpCommandKind.Noop ||
            kind == FtpCommandKind.Pasv ||
            kind == FtpCommandKind.Quit ||
            kind == FtpCommandKind.Rein ||
            kind == FtpCommandKind.Stou ||
            kind == FtpCommandKind.Syst ||
            kind == FtpCommandKind.Pwd ||
            kind == FtpCommandKind.Feat ||
            kind == FtpCommandKind.Ccc ||
            kind == FtpCommandKind.Lpsv ||
            kind == FtpCommandKind.Xcup ||
            kind == FtpCommandKind.Xpwd)
        {
            return null;
        }
        if (kind == FtpCommandKind.Allo || kind == FtpCommandKind.Rest)
        {
            return "1";
        }
        if (kind == FtpCommandKind.Pbsz)
        {
            return "0";
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
        if (kind == FtpCommandKind.Port)
        {
            return "127,0,0,1,8,73";
        }
        if (kind == FtpCommandKind.Eprt)
        {
            return "|1|127.0.0.1|2121|";
        }
        if (kind == FtpCommandKind.Epsv || kind == FtpCommandKind.Lang)
        {
            return null;
        }
        if (kind == FtpCommandKind.Host)
        {
            return "example.test";
        }
        return "value";
    }
}
