using System.Text;

namespace ftp.Tests;

public class RfcTranscriptTests
{
    [Test]
    public void Rfc959_typical_login_transfer_and_multiline_reply_transcripts_round_trip()
    {
        FtpSessionStateMachine session = new FtpSessionStateMachine();
        session.OpenConnection();

        ApplyTranscriptLine(session, "USER Doe", "331 User name ok, need password\r\n");
        Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Authenticating));
        Assert.That(session.State.UserName, Is.EqualTo("Doe"));

        FtpCommand password = ApplyTranscriptLine(
            session, "PASS mumble", "230 User logged in\r\n");
        Assert.That(password.Sensitive, Is.True);
        Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Authenticated));

        ApplyTranscriptLine(
            session, "RETR test.pl1",
            "150 File status okay; about to open data connection\r\n");
        Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Transferring));
        session.ApplyTransferReply(ParseReply(
            "226 Closing data connection, file transfer successful\r\n"));
        Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Authenticated));

        ApplyTranscriptLine(session, "TYPE I", "200 Command OK\r\n");
        Assert.That(session.State.TransferType, Is.EqualTo(FtpTransferType.Image));
        ApplyTranscriptLine(session, "STOR >udd>cn>fd", "550 Access denied\r\n");
        Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Authenticated));

        FtpReply multiline = ParseReply(
            "123-First line\r\n" +
            "Second line\r\n" +
            "  234 A line beginning with numbers\r\n" +
            "123 The last line\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(multiline.Code, Is.EqualTo(123));
            Assert.That(multiline.Multiline, Is.True);
            Assert.That(multiline.Lines, Has.Length.EqualTo(4));
            Assert.That(multiline.Lines[2].HasCode, Is.False);
            Assert.That(multiline.Lines[3].TerminatesMultiline, Is.True);
        });
    }

    [Test]
    public void Rfc959_verified_errata_are_reflected_in_protocol_behavior()
    {
        FtpEndpoint endpoint = FtpEndpointCodec.ParsePortArgument(
            "127,0,0,1,15,107");
        FtpSessionStateMachine session = CreateAuthenticatedSession();

        ApplyTranscriptLine(session, "CWD /usr/dm",
            "250 directory changed to /usr/dm\r\n");
        ApplyTranscriptLine(session, "CDUP",
            "250 directory changed to /usr\r\n");
        ApplyTranscriptLine(session, "CWD overrainbow",
            "550 No such directory\r\n");

        ApplyTranscriptLine(session, "PORT 127,0,0,1,15,107",
            "200 PORT command successful\r\n");
        FtpEndpoint configuredEndpoint = session.State.DataEndpoint!;
        ApplyTranscriptLine(session, "TYPE I", "200 Type set to I.\r\n");

        FtpCommand obsoleteMail = ParseCommand("MAIL recipient@example.test");
        FtpReply syst = ParseReply("215 UNIX Type: L8\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Host, Is.EqualTo("127.0.0.1"),
                "RFC 959 erratum 3039 permits zero-valued number components.");
            Assert.That(endpoint.Port, Is.EqualTo(3947));
            Assert.That(session.State.CurrentDirectory, Is.EqualTo("/usr"),
                "RFC 959 errata 4869 and 5451 correct CWD/CDUP success to 250.");
            Assert.That(session.State.DataEndpoint, Is.SameAs(configuredEndpoint),
                "RFC 959 erratum 7430 leaves the selected port independent of service commands.");
            Assert.That(obsoleteMail.Kind, Is.EqualTo(FtpCommandKind.Unknown),
                "RFC 959 erratum 4138 records that the mail-service commands were removed.");
            Assert.That(FtpReplyExtractor.ExtractSystemType(syst),
                Is.EqualTo("UNIX Type: L8"));
        });
    }

    [Test]
    public void Rfc2389_feature_transcript_preserves_features_and_default_markers()
    {
        FtpReply reply = ParseReply(
            "211-Extensions supported:\r\n" +
            " MLST size*;create;modify*;perm;media-type\r\n" +
            " SIZE\r\n" +
            " COMPRESSION\r\n" +
            " MDTM\r\n" +
            "211 END\r\n");

        FtpFeatureSet features = FtpFeatureCodec.ParseFeatureReply(reply);

        Assert.Multiple(() =>
        {
            Assert.That(features.Negotiated, Is.True);
            Assert.That(features.Features.Select(feature => feature.Name),
                Is.EqualTo(new[] { "MLST", "SIZE", "COMPRESSION", "MDTM" }));
            Assert.That(features.Features[0].Parameters,
                Is.EqualTo("size*;create;modify*;perm;media-type"));
            Assert.That(features.Features[0].RawLine,
                Is.EqualTo(" MLST size*;create;modify*;perm;media-type"));
            Assert.That(FtpFeatureCodec.FindFeatureSupport(features, "size"),
                Is.EqualTo(FtpFeatureSupport.Supported));
            Assert.That(FtpFeatureCodec.FindFeatureSupport(features, "unknown"),
                Is.EqualTo(FtpFeatureSupport.NotSupported));
        });
    }

    [Test]
    public void Rfc2428_extended_address_transcripts_parse_and_EPSV_ALL_is_enforced()
    {
        FtpEndpoint ipv4 = FtpEndpointCodec.ParseExtendedPortArgument(
            "|1|132.235.1.2|6275|");
        FtpEndpoint ipv6 = FtpEndpointCodec.ParseExtendedPortArgument(
            "|2|1080::8:800:200C:417A|5282|");
        FtpPassiveReply passive = FtpEndpointCodec.ParseExtendedPassiveReply(
            ParseReply("229 Entering Extended Passive Mode (|||6446|)\r\n"));
        FtpNetworkProtocolReply unsupported =
            FtpEndpointCodec.ParseNetworkProtocolReply(ParseReply(
                "522 Network protocol not supported, use (1,2)\r\n"));

        FtpSessionStateMachine session = CreateAuthenticatedSession();
        ApplyTranscriptLine(session, "EPSV ALL", "200 EPSV ALL accepted\r\n");
        FtpSequenceValidationResult port = FtpSequenceValidator.InspectCommand(
            ParseCommand("PORT 127,0,0,1,15,107"), session.State);
        FtpSequenceValidationResult eprt = FtpSequenceValidator.InspectCommand(
            ParseCommand("EPRT |1|132.235.1.2|6275|"), session.State);
        FtpSequenceValidationResult epsv = FtpSequenceValidator.InspectCommand(
            ParseCommand("EPSV"), session.State);

        Assert.Multiple(() =>
        {
            Assert.That(ipv4.Host, Is.EqualTo("132.235.1.2"));
            Assert.That(ipv4.Port, Is.EqualTo(6275));
            Assert.That(ipv6.Host, Is.EqualTo("1080::8:800:200C:417A"));
            Assert.That(ipv6.Port, Is.EqualTo(5282));
            Assert.That(passive.Endpoint.Port, Is.EqualTo(6446));
            Assert.That(unsupported.SupportedProtocols, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(port.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(eprt.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(epsv.Allowed, Is.True);
        });
    }

    [Test]
    public void Rfc2640_language_and_UTF8_transcripts_preserve_international_text()
    {
        FtpFeatureSet before = FtpFeatureCodec.ParseFeatureReply(ParseReply(
            "211-Extensions supported\r\n" +
            " UTF8\r\n" +
            " LANG EN*;FR\r\n" +
            "211 end\r\n"));

        FtpSessionStateMachine session = new FtpSessionStateMachine();
        session.OpenConnection();
        ApplyTranscriptLine(session, "LANG fr",
            "200 Le response sera changez au francais\r\n");
        FtpFeatureSet after = FtpFeatureCodec.ParseFeatureReply(ParseReply(
            "211-quelconque descriptif texte\r\n" +
            " UTF8\r\n" +
            " LANG EN;FR*\r\n" +
            "211 end\r\n"));

        FtpCommand storeVav = ParseCommand("STOR \u05d5");
        byte[] encoded = FtpCommandWriter.FormatEncodedCommand(
            storeVav, new UTF8Encoding(false, true));

        Assert.Multiple(() =>
        {
            Assert.That(before.Features[1].Parameters, Is.EqualTo("EN*;FR"));
            Assert.That(session.State.Language, Is.EqualTo("fr"));
            Assert.That(after.Features[1].Parameters, Is.EqualTo("EN;FR*"));
            Assert.That(encoded, Is.EqualTo(new byte[]
            {
                0x53, 0x54, 0x4f, 0x52, 0x20, 0xd7, 0x95, 0x0d, 0x0a
            }), "RFC 2640 erratum 8007 identifies VAV as U+05D5.");
        });
    }

    [Test]
    public void Rfc3659_metadata_restart_and_MLST_transcripts_are_typed()
    {
        FtpTimestamp fractional = FtpReplyExtractor.ExtractModificationTime(
            ParseReply("213 19980615100045.014\r\n"));
        FtpTimestamp whole = FtpReplyExtractor.ExtractModificationTime(
            ParseReply("213 19980705132316\r\n"));
        long imageSize = FtpReplyExtractor.ExtractSize(
            ParseReply("213 1830\r\n"));

        FtpReply mlstReply = ParseReply(
            "250- Listing cap60.pl198.tar.gz\r\n" +
            " Type=file;Size=1024990;Perm=r; /tmp/cap60.pl198.tar.gz\r\n" +
            "250 End\r\n");
        FtpEntry entry = FtpMlsxCodec.ParseControlReply(mlstReply);

        FtpSessionStateMachine session = CreateAuthenticatedSession();
        ApplyTranscriptLine(session, "TYPE I", "200 Type set to I.\r\n");
        ApplyTranscriptLine(session, "PORT 127,0,0,1,15,107",
            "200 PORT command successful.\r\n");
        ApplyTranscriptLine(session, "REST 802816",
            "350 Restarting at 802816. Send STORE or RETRIEVE\r\n");
        ApplyTranscriptLine(session, "RETR cap60.pl198.tar",
            "150 Opening BINARY mode data connection\r\n");
        session.ApplyTransferReply(ParseReply("226 Transfer complete.\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(fractional.ValueUtc,
                Is.EqualTo(new DateTime(1998, 6, 15, 10, 0, 45, 14,
                    DateTimeKind.Utc)));
            Assert.That(fractional.FractionalSeconds, Is.EqualTo("014"));
            Assert.That(whole.ValueUtc,
                Is.EqualTo(new DateTime(1998, 7, 5, 13, 23, 16,
                    DateTimeKind.Utc)));
            Assert.That(imageSize, Is.EqualTo(1830));
            Assert.That(entry.Name, Is.EqualTo("/tmp/cap60.pl198.tar.gz"));
            Assert.That(entry.Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(entry.Size, Is.EqualTo(1024990));
            Assert.That(entry.Permissions, Is.EqualTo(new[] { FtpPermission.Read }));
            Assert.That(session.State.HasRestartMarker, Is.False);
            Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Authenticated));
        });
    }

    [Test]
    public void Rfc4217_feature_and_AUTH_PBSZ_PROT_transcript_updates_TLS_state()
    {
        FtpFeatureSet features = FtpFeatureCodec.ParseFeatureReply(ParseReply(
            "211-Extensions supported\r\n" +
            " AUTH TLS\r\n" +
            " PBSZ\r\n" +
            " PROT\r\n" +
            "211 END\r\n"));

        FtpSessionStateMachine session = new FtpSessionStateMachine();
        session.OpenConnection();
        ApplyTranscriptLine(session, "AUTH TLS",
            "234 AUTH TLS successful\r\n");
        session.CompleteSecurityExchange(true);

        FtpCommand pbsz = ParseCommand("PBSZ 0");
        Assert.That(FtpSequenceValidator.InspectCommand(pbsz, session.State).Allowed,
            Is.True);
        session.ApplyCommandReply(pbsz, ParseReply("200 PBSZ=0\r\n"));

        FtpCommand prot = ParseCommand("PROT P");
        Assert.That(FtpSequenceValidator.InspectCommand(prot, session.State).Allowed,
            Is.True);
        session.ApplyCommandReply(prot, ParseReply("200 Protection set to Private\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(FtpFeatureCodec.ExtractAuthenticationMechanisms(features),
                Is.EqualTo(new[] { "TLS" }));
            Assert.That(session.State.SecurityExchangeComplete, Is.True);
            Assert.That(session.State.SecurityExchangeIsTls, Is.True);
            Assert.That(session.State.ControlProtected, Is.True);
            Assert.That(session.State.ProtectionBufferSizeSet, Is.True);
            Assert.That(session.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Private));
        });
    }

    [Test]
    public void Rfc7151_HOST_literal_and_login_transcripts_route_before_authentication()
    {
        string[] validHosts =
        {
            "xn--e1afmkfd.com",
            "192.0.2.1",
            "[2001:db8::c000:201]",
            "[::192.0.2.1]"
        };

        foreach (string host in validHosts)
        {
            Assert.That(FtpHostCodec.ParseHostArgument(host), Is.EqualTo(host));
        }
        Assert.Throws<FtpProtocolException>(() =>
            ParseCommand("HOST 192.0.2.1:2112"));
        Assert.Throws<FtpProtocolException>(() =>
            ParseCommand("HOST [2001:db8::c000:201]:2112"));

        FtpSessionStateMachine session = new FtpSessionStateMachine();
        session.OpenConnection();
        ApplyTranscriptLine(session, "HOST foo.example.com", "220 Host accepted\r\n");
        ApplyTranscriptLine(session, "HOST bar.example.com", "220 Host accepted\r\n");
        ApplyTranscriptLine(session, "USER foo", "331 Password required\r\n");
        ApplyTranscriptLine(session, "PASS bar", "230 User logged in\r\n");

        FtpSequenceValidationResult lateHost = FtpSequenceValidator.InspectCommand(
            ParseCommand("HOST ftp.example.com"), session.State);

        Assert.Multiple(() =>
        {
            Assert.That(session.State.SelectedHost, Is.EqualTo("bar.example.com"));
            Assert.That(session.State.UserName, Is.EqualTo("foo"));
            Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Authenticated));
            Assert.That(lateHost.ReplyCode, Is.EqualTo(FtpReplyCodes.BadCommandSequence));
        });
    }

    private static FtpSessionStateMachine CreateAuthenticatedSession()
    {
        FtpSessionStateMachine session = new FtpSessionStateMachine();
        session.OpenConnection();
        ApplyTranscriptLine(session, "USER transcript",
            "230 User logged in\r\n");
        return session;
    }

    private static FtpCommand ApplyTranscriptLine(
        FtpSessionStateMachine session, string commandText, string replyWire)
    {
        FtpCommand command = ParseCommand(commandText);
        session.ApplyCommandReply(command, ParseReply(replyWire));
        return command;
    }

    private static FtpCommand ParseCommand(string commandText)
    {
        IFtpCommandCodec codec = new FtpCommandCodec();
        return codec.ParseCommand(commandText);
    }

    private static FtpReply ParseReply(string replyWire)
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        using MemoryStream stream = new MemoryStream(
            Encoding.UTF8.GetBytes(replyWire));
        return codec.ReadReply(stream);
    }
}
