namespace ftp.Tests;

public class PublicStaticDataTests
{
    [Test]
    public void Every_public_static_field_is_present_and_category_values_are_unique()
    {
        string[] commands = GetConstantValues<string>(
            typeof(FtpCommandNames), 70);
        string[] features = GetConstantValues<string>(
            typeof(FtpFeatureNames), 25);
        string[] facts = GetConstantValues<string>(
            typeof(FtpFactNames), 9);
        int[] replies = GetConstantValues<int>(typeof(FtpReplyCodes), 58);
        int[] ports = GetConstantValues<int>(typeof(FtpPorts), 2);
        object[] defaults = GetConstantValues<object>(
            typeof(FtpDefaults), 25);

        Assert.Multiple(() =>
        {
            Assert.That(commands.Distinct().Count(),
                Is.EqualTo(commands.Length));
            Assert.That(commands.All(value =>
                value.Length > 0 && value.All(character =>
                    (character >= 'A' && character <= 'Z') ||
                    (character >= '0' && character <= '9'))), Is.True);
            Assert.That(features.Distinct().Count(),
                Is.EqualTo(features.Length));
            Assert.That(features.All(value =>
                value.Length > 0 && value == value.ToUpperInvariant()),
                Is.True);
            Assert.That(facts.Distinct().Count(),
                Is.EqualTo(facts.Length));
            Assert.That(facts.All(value =>
                value.Length > 0 && value == value.ToLowerInvariant()),
                Is.True);
            Assert.That(replies.Distinct().Count(),
                Is.EqualTo(replies.Length));
            Assert.That(replies.All(value => value >= 100 && value <= 699),
                Is.True);
            Assert.That(ports.Distinct().Count(), Is.EqualTo(ports.Length));
            Assert.That(ports.All(value => value > 0 && value <= 65535),
                Is.True);
            Assert.That(defaults, Has.Length.EqualTo(25));
        });
    }

    [Test]
    public void Ports_use_registered_defaults()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FtpPorts.Control, Is.EqualTo(21));
            Assert.That(FtpPorts.ImplicitTls, Is.EqualTo(990));
        });
    }

    [Test]
    public void Command_names_use_canonical_wire_verbs()
    {
        string[] actual =
        {
                FtpCommandNames.Abor,
                FtpCommandNames.Acct,
                FtpCommandNames.Allo,
                FtpCommandNames.Appe,
                FtpCommandNames.Cdup,
                FtpCommandNames.Cwd,
                FtpCommandNames.Dele,
                FtpCommandNames.Help,
                FtpCommandNames.List,
                FtpCommandNames.Mkd,
                FtpCommandNames.Mode,
                FtpCommandNames.Nlst,
                FtpCommandNames.Noop,
                FtpCommandNames.Pass,
                FtpCommandNames.Pasv,
                FtpCommandNames.Port,
                FtpCommandNames.Quit,
                FtpCommandNames.Rein,
                FtpCommandNames.Rest,
                FtpCommandNames.Retr,
                FtpCommandNames.Rmd,
                FtpCommandNames.Rnfr,
                FtpCommandNames.Rnto,
                FtpCommandNames.Site,
                FtpCommandNames.Smnt,
                FtpCommandNames.Stat,
                FtpCommandNames.Stor,
                FtpCommandNames.Stou,
                FtpCommandNames.Stru,
                FtpCommandNames.Syst,
                FtpCommandNames.Type,
                FtpCommandNames.User,
                FtpCommandNames.Pwd,
                FtpCommandNames.Feat,
                FtpCommandNames.Opts,
                FtpCommandNames.Eprt,
                FtpCommandNames.Epsv,
                FtpCommandNames.Lang,
                FtpCommandNames.Mdtm,
                FtpCommandNames.Mlst,
                FtpCommandNames.Mlsd,
                FtpCommandNames.Size,
                FtpCommandNames.Auth,
                FtpCommandNames.Pbsz,
                FtpCommandNames.Prot,
                FtpCommandNames.Ccc,
                FtpCommandNames.Adat,
                FtpCommandNames.Mic,
                FtpCommandNames.Conf,
                FtpCommandNames.Enc,
                FtpCommandNames.Host,
                FtpCommandNames.Algs,
                FtpCommandNames.Lprt,
                FtpCommandNames.Lpsv,
                FtpCommandNames.Xcup,
                FtpCommandNames.Xcwd,
                FtpCommandNames.Xmkd,
                FtpCommandNames.Xpwd,
                FtpCommandNames.Xrmd,
                FtpCommandNames.Pret,
                FtpCommandNames.Clnt,
                FtpCommandNames.Mfmt,
                FtpCommandNames.Hash,
                FtpCommandNames.Md5,
                FtpCommandNames.Xmd5,
                FtpCommandNames.Xsha1,
                FtpCommandNames.Xsha256,
                FtpCommandNames.Xsha512,
                FtpCommandNames.Xcrc,
                FtpCommandNames.Rmda,
        };
        string[] expected =
        {
                "ABOR",
                "ACCT",
                "ALLO",
                "APPE",
                "CDUP",
                "CWD",
                "DELE",
                "HELP",
                "LIST",
                "MKD",
                "MODE",
                "NLST",
                "NOOP",
                "PASS",
                "PASV",
                "PORT",
                "QUIT",
                "REIN",
                "REST",
                "RETR",
                "RMD",
                "RNFR",
                "RNTO",
                "SITE",
                "SMNT",
                "STAT",
                "STOR",
                "STOU",
                "STRU",
                "SYST",
                "TYPE",
                "USER",
                "PWD",
                "FEAT",
                "OPTS",
                "EPRT",
                "EPSV",
                "LANG",
                "MDTM",
                "MLST",
                "MLSD",
                "SIZE",
                "AUTH",
                "PBSZ",
                "PROT",
                "CCC",
                "ADAT",
                "MIC",
                "CONF",
                "ENC",
                "HOST",
                "ALGS",
                "LPRT",
                "LPSV",
                "XCUP",
                "XCWD",
                "XMKD",
                "XPWD",
                "XRMD",
                "PRET",
                "CLNT",
                "MFMT",
                "HASH",
                "MD5",
                "XMD5",
                "XSHA1",
                "XSHA256",
                "XSHA512",
                "XCRC",
                "RMDA",
        };

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
        });
    }

    [Test]
    public void Reply_codes_use_standard_numeric_values()
    {
        int[] actual =
        {
                FtpReplyCodes.RestartMarker,
                FtpReplyCodes.ServiceReadySoon,
                FtpReplyCodes.DataConnectionAlreadyOpen,
                FtpReplyCodes.FileStatusOkayOpeningDataConnection,
                FtpReplyCodes.CommandOkay,
                FtpReplyCodes.CommandSuperfluous,
                FtpReplyCodes.SystemStatus,
                FtpReplyCodes.DirectoryStatus,
                FtpReplyCodes.FileStatus,
                FtpReplyCodes.HelpMessage,
                FtpReplyCodes.SystemType,
                FtpReplyCodes.ServiceReady,
                FtpReplyCodes.ServiceClosingControlConnection,
                FtpReplyCodes.DataConnectionOpenNoTransfer,
                FtpReplyCodes.ClosingDataConnection,
                FtpReplyCodes.EnteringPassiveMode,
                FtpReplyCodes.EnteringExtendedPassiveMode,
                FtpReplyCodes.UserLoggedIn,
                FtpReplyCodes.UserLoggedInBySecurityExchange,
                FtpReplyCodes.SecurityExchangeComplete,
                FtpReplyCodes.SecurityExchangeCompleteWithData,
                FtpReplyCodes.RequestedFileActionOkay,
                FtpReplyCodes.PathCreated,
                FtpReplyCodes.UserNameOkayNeedPassword,
                FtpReplyCodes.NeedAccountForLogin,
                FtpReplyCodes.SecurityMechanismOkay,
                FtpReplyCodes.SecurityDataAcceptedNeedMore,
                FtpReplyCodes.UserNameOkayNeedChallengePassword,
                FtpReplyCodes.RequestedFileActionPending,
                FtpReplyCodes.ServiceUnavailable,
                FtpReplyCodes.CannotOpenDataConnection,
                FtpReplyCodes.ConnectionClosedTransferAborted,
                FtpReplyCodes.SecurityResourceUnavailable,
                FtpReplyCodes.RequestedFileActionNotTaken,
                FtpReplyCodes.RequestedActionAbortedLocalError,
                FtpReplyCodes.RequestedActionNotTakenInsufficientStorage,
                FtpReplyCodes.SyntaxError,
                FtpReplyCodes.SyntaxErrorInParameters,
                FtpReplyCodes.CommandNotImplemented,
                FtpReplyCodes.BadCommandSequence,
                FtpReplyCodes.CommandParameterNotImplemented,
                FtpReplyCodes.DataProtectionLevelNotSupported,
                FtpReplyCodes.NetworkProtocolNotSupported,
                FtpReplyCodes.NotLoggedIn,
                FtpReplyCodes.NeedAccountForStorage,
                FtpReplyCodes.CommandProtectionDenied,
                FtpReplyCodes.RequestDeniedByPolicy,
                FtpReplyCodes.SecurityCheckFailed,
                FtpReplyCodes.ProtectionLevelNotSupported,
                FtpReplyCodes.CommandProtectionNotSupported,
                FtpReplyCodes.RequestedActionNotTaken,
                FtpReplyCodes.PageTypeUnknown,
                FtpReplyCodes.FileActionAbortedStorageExceeded,
                FtpReplyCodes.FileNameNotAllowed,
                FtpReplyCodes.HashUnavailableForEntry,
                FtpReplyCodes.IntegrityProtectedReply,
                FtpReplyCodes.ConfidentialityAndIntegrityProtectedReply,
                FtpReplyCodes.ConfidentialityProtectedReply,
        };
        int[] expected =
        {
                110,
                120,
                125,
                150,
                200,
                202,
                211,
                212,
                213,
                214,
                215,
                220,
                221,
                225,
                226,
                227,
                229,
                230,
                232,
                234,
                235,
                250,
                257,
                331,
                332,
                334,
                335,
                336,
                350,
                421,
                425,
                426,
                431,
                450,
                451,
                452,
                500,
                501,
                502,
                503,
                504,
                521,
                522,
                530,
                532,
                533,
                534,
                535,
                536,
                537,
                550,
                551,
                552,
                553,
                556,
                631,
                632,
                633,
        };

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
        });
    }

    [Test]
    public void Feature_names_use_normalized_tokens()
    {
        string[] actual =
        {
                FtpFeatureNames.Auth,
                FtpFeatureNames.Pbsz,
                FtpFeatureNames.Prot,
                FtpFeatureNames.Utf8,
                FtpFeatureNames.Lang,
                FtpFeatureNames.Mdtm,
                FtpFeatureNames.Size,
                FtpFeatureNames.RestStream,
                FtpFeatureNames.Tvfs,
                FtpFeatureNames.Mlst,
                FtpFeatureNames.Host,
                FtpFeatureNames.Pret,
                FtpFeatureNames.ModeZ,
                FtpFeatureNames.Clnt,
                FtpFeatureNames.Mfmt,
                FtpFeatureNames.Hash,
                FtpFeatureNames.Md5,
                FtpFeatureNames.Xmd5,
                FtpFeatureNames.Xsha1,
                FtpFeatureNames.Xsha256,
                FtpFeatureNames.Xsha512,
                FtpFeatureNames.Xcrc,
                FtpFeatureNames.SiteChmod,
                FtpFeatureNames.SiteUmask,
                FtpFeatureNames.Rmda,
        };
        string[] expected =
        {
                "AUTH",
                "PBSZ",
                "PROT",
                "UTF8",
                "LANG",
                "MDTM",
                "SIZE",
                "REST STREAM",
                "TVFS",
                "MLST",
                "HOST",
                "PRET",
                "MODE Z",
                "CLNT",
                "MFMT",
                "HASH",
                "MD5",
                "XMD5",
                "XSHA1",
                "XSHA256",
                "XSHA512",
                "XCRC",
                "SITE CHMOD",
                "SITE UMASK",
                "RMDA",
        };

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
        });
    }

    [Test]
    public void Fact_names_use_normalized_rfc_3659_tokens()
    {
        string[] actual =
        {
                FtpFactNames.Type,
                FtpFactNames.Size,
                FtpFactNames.Modify,
                FtpFactNames.Create,
                FtpFactNames.Unique,
                FtpFactNames.Perm,
                FtpFactNames.Lang,
                FtpFactNames.MediaType,
                FtpFactNames.Charset,
        };
        string[] expected =
        {
                "type",
                "size",
                "modify",
                "create",
                "unique",
                "perm",
                "lang",
                "media-type",
                "charset",
        };

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
        });
    }

    [Test]
    public void Defaults_are_bounded_and_security_conscious()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FtpDefaults.MaximumControlLineBytes, Is.EqualTo(8192));
            Assert.That(FtpDefaults.MaximumReplyBytes, Is.EqualTo(65536));
            Assert.That(FtpDefaults.MaximumReplyLines, Is.EqualTo(256));
            Assert.That(FtpDefaults.MaximumFeatureCount, Is.EqualTo(256));
            Assert.That(FtpDefaults.MaximumFactCount, Is.EqualTo(128));
            Assert.That(FtpDefaults.MaximumListingEntryCount, Is.EqualTo(100000));
            Assert.That(FtpDefaults.TransferBufferBytes, Is.EqualTo(65536));
            Assert.That(FtpDefaults.TransferKeepAliveIntervalMilliseconds,
                Is.EqualTo(30000));
            Assert.That(FtpDefaults.ConnectTimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(FtpDefaults.ControlReadTimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(FtpDefaults.ControlWriteTimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(FtpDefaults.DataConnectionTimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(FtpDefaults.TlsHandshakeTimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(FtpDefaults.IdleTimeoutMilliseconds, Is.EqualTo(300000));
            Assert.That(FtpDefaults.GracefulStopTimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(FtpDefaults.RetryCount, Is.Zero);
            Assert.That(FtpDefaults.MaximumAuthenticationAttempts, Is.EqualTo(5));
            Assert.That(FtpDefaults.PassivePortStart, Is.EqualTo(49152));
            Assert.That(FtpDefaults.PassivePortEnd, Is.EqualTo(65535));
            Assert.That(FtpDefaults.IgnorePassiveAdvertisedAddress, Is.True);
            Assert.That(FtpDefaults.RequireDataPeerMatch, Is.True);
            Assert.That(FtpDefaults.AllowForeignDataConnections, Is.False);
            Assert.That(FtpDefaults.AllowPlaintextCredentials, Is.False);
            Assert.That(FtpDefaults.AllowClearCommandChannel, Is.False);
            Assert.That(FtpDefaults.AllowAnonymousWrites, Is.False);
        });
    }

    private static T[] GetConstantValues<T>(Type type, int expectedCount)
    {
        System.Reflection.FieldInfo[] fields = type.GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.DeclaredOnly);
        Assert.That(fields, Has.Length.EqualTo(expectedCount), type.Name);
        return fields.Select(field => (T)field.GetRawConstantValue()!).ToArray();
    }
}
