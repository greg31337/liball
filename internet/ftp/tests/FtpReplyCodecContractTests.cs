using System.Reflection;
using System.Text;

namespace ftp.Tests;

public class FtpReplyCodecContractTests
{
    [Test]
    public void Every_named_reply_code_reads_and_classifies_through_the_contract()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        int[] replyCodes = typeof(FtpReplyCodes).GetFields(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Select(field => (int)field.GetRawConstantValue()!)
            .OrderBy(code => code)
            .ToArray();

        Assert.That(replyCodes, Has.Length.EqualTo(58));
        foreach (int code in replyCodes)
        {
            string wire = code.ToString("D3") + " typed reply\r\n";
            using MemoryStream source = new MemoryStream(
                Encoding.UTF8.GetBytes(wire));

            FtpReply reply = codec.ReadReply(source);
            FtpReplyClassification classification =
                codec.ClassifyReply(reply);

            Assert.Multiple(() =>
            {
                Assert.That(reply.Code, Is.EqualTo(code), code.ToString());
                Assert.That(reply.Text, Is.EqualTo("typed reply"),
                    code.ToString());
                Assert.That(reply.RawText, Is.EqualTo(wire), code.ToString());
                Assert.That(reply.Classification, Is.SameAs(classification),
                    code.ToString());
                Assert.That(classification.Malformed,
                    Is.EqualTo(code >= 600), code.ToString());
            });
        }
    }

    [Test]
    public void ClassifyReply_covers_every_category_and_functional_group()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        FtpReplyCategory[] categories =
        {
            FtpReplyCategory.PositivePreliminary,
            FtpReplyCategory.PositiveCompletion,
            FtpReplyCategory.PositiveIntermediate,
            FtpReplyCategory.TransientNegativeCompletion,
            FtpReplyCategory.PermanentNegativeCompletion
        };
        FtpReplyKind[] kinds =
        {
            FtpReplyKind.Syntax,
            FtpReplyKind.Information,
            FtpReplyKind.Connections,
            FtpReplyKind.AuthenticationAndAccounting,
            FtpReplyKind.Unspecified,
            FtpReplyKind.FileSystem
        };

        int categoryIndex = 0;
        while (categoryIndex < categories.Length)
        {
            int kindIndex = 0;
            while (kindIndex < kinds.Length)
            {
                FtpReply reply = new FtpReply();
                reply.Code = ((categoryIndex + 1) * 100) +
                    (kindIndex * 10);
                FtpReplyClassification classification =
                    codec.ClassifyReply(reply);

                Assert.Multiple(() =>
                {
                    Assert.That(classification.Category,
                        Is.EqualTo(categories[categoryIndex]),
                        reply.Code.ToString());
                    Assert.That(classification.Kind,
                        Is.EqualTo(kinds[kindIndex]),
                        reply.Code.ToString());
                    Assert.That(classification.Succeeded,
                        Is.EqualTo(categoryIndex == 1),
                        reply.Code.ToString());
                    Assert.That(classification.TransientFailure,
                        Is.EqualTo(categoryIndex == 3),
                        reply.Code.ToString());
                    Assert.That(classification.PermanentFailure,
                        Is.EqualTo(categoryIndex == 4),
                        reply.Code.ToString());
                    Assert.That(classification.AuthenticationFailure,
                        Is.EqualTo(
                            kindIndex == 3 &&
                            (categoryIndex == 3 || categoryIndex == 4)),
                        reply.Code.ToString());
                });
                kindIndex++;
            }
            categoryIndex++;
        }

        FtpReply shortCode = new FtpReply();
        shortCode.Code = 99;
        FtpReply unknownCategory = new FtpReply();
        unknownCategory.Code = 699;
        FtpReply unknownKind = new FtpReply();
        unknownKind.Code = 260;
        Assert.Multiple(() =>
        {
            Assert.That(codec.ClassifyReply(shortCode).Malformed, Is.True);
            Assert.That(codec.ClassifyReply(unknownCategory).Malformed, Is.True);
            Assert.That(codec.ClassifyReply(unknownKind).Malformed, Is.True);
            Assert.Throws<ArgumentNullException>(() =>
                codec.ClassifyReply(null!));
        });
    }

    [Test]
    public void ReadReply_preserves_single_multiline_blank_and_Unicode_forms()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        string wire =
            "220 Prêt 😀\r\n" +
            "200 \r\n" +
            "211-機能\r\n" +
            " UTF8\r\n" +
            "234 unrelated\r\n" +
            "\r\n" +
            "211-Still open\r\n" +
            "211 完了\r\n";
        using MemoryStream source = new MemoryStream(
            Encoding.UTF8.GetBytes(wire));

        FtpReply greeting = codec.ReadReply(source);
        FtpReply blank = codec.ReadReply(source);
        FtpReply multiline = codec.ReadReply(source);

        Assert.Multiple(() =>
        {
            Assert.That(greeting.Text, Is.EqualTo("Prêt 😀"));
            Assert.That(blank.Text, Is.Empty);
            Assert.That(multiline.Code, Is.EqualTo(211));
            Assert.That(multiline.Multiline, Is.True);
            Assert.That(multiline.Text, Is.EqualTo("完了"));
            Assert.That(multiline.Lines, Has.Length.EqualTo(6));
            Assert.That(multiline.Lines[1].HasCode, Is.False);
            Assert.That(multiline.Lines[2].Code, Is.EqualTo(234));
            Assert.That(multiline.Lines[3].Text, Is.Empty);
            Assert.That(multiline.Lines[4].TerminatesMultiline, Is.False);
            Assert.That(multiline.Lines[5].TerminatesMultiline, Is.True);
            Assert.That(source.Position, Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void ReadReply_enforces_exact_line_and_physical_line_limits()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        string bounded = "220 " + new string(
            'a', FtpDefaults.MaximumControlLineBytes - 6) + "\r\n";
        using MemoryStream boundedSource = new MemoryStream(
            Encoding.UTF8.GetBytes(bounded));
        FtpReply accepted = codec.ReadReply(boundedSource);

        string tooLong = "220 " + new string(
            'a', FtpDefaults.MaximumControlLineBytes - 5) + "\r\n";
        AssertReadFailure(codec, Encoding.UTF8.GetBytes(tooLong));

        string maximumLines = CreateMultilineWire(
            FtpDefaults.MaximumReplyLines - 2);
        using MemoryStream maximumLineSource = new MemoryStream(
            Encoding.UTF8.GetBytes(maximumLines));
        FtpReply lineBounded = codec.ReadReply(maximumLineSource);
        string tooManyLines = CreateMultilineWire(
            FtpDefaults.MaximumReplyLines - 1);
        AssertReadFailure(codec, Encoding.UTF8.GetBytes(tooManyLines));
        string maximumBytes = CreateByteBoundedMultilineWire(6);
        using MemoryStream maximumByteSource = new MemoryStream(
            Encoding.UTF8.GetBytes(maximumBytes));
        FtpReply byteBounded = codec.ReadReply(maximumByteSource);
        string tooManyBytes = CreateByteBoundedMultilineWire(7);
        AssertReadFailure(codec, Encoding.UTF8.GetBytes(tooManyBytes));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Text.Length,
                Is.EqualTo(FtpDefaults.MaximumControlLineBytes - 6));
            Assert.That(lineBounded.Lines,
                Has.Length.EqualTo(FtpDefaults.MaximumReplyLines));
            Assert.That(Encoding.UTF8.GetByteCount(byteBounded.RawText),
                Is.EqualTo(FtpDefaults.MaximumReplyBytes));
        });
    }

    [Test]
    public void ReadReply_rejects_malformed_framing_text_and_encoding()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        byte[][] malformed =
        {
            new byte[0],
            Encoding.UTF8.GetBytes("220 Ready"),
            Encoding.UTF8.GetBytes("220 Ready\n"),
            Encoding.UTF8.GetBytes("220 Ready\r"),
            Encoding.UTF8.GetBytes("220Ready\r\n"),
            Encoding.UTF8.GetBytes("Ready\r\n"),
            Encoding.UTF8.GetBytes("22 Ready\r\n"),
            Encoding.UTF8.GetBytes("211-Features\r\n UTF8\r\n"),
            Encoding.UTF8.GetBytes("211-Features\r\n212 wrong\r\n"),
            Encoding.UTF8.GetBytes("220 bad\0text\r\n"),
            Encoding.UTF8.GetBytes("220 bad\u0001text\r\n"),
            new byte[] { 50, 50, 48, 32, 192, 175, 13, 10 }
        };

        Assert.Throws<ArgumentNullException>(() => codec.ReadReply(null!));
        foreach (byte[] bytes in malformed)
        {
            AssertReadFailure(codec, bytes);
        }
    }

    [Test]
    public void WriteReply_round_trips_every_outbound_named_code()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        int[] outboundCodes = typeof(FtpReplyCodes).GetFields(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Select(field => (int)field.GetRawConstantValue()!)
            .Where(code => code <= 599)
            .OrderBy(code => code)
            .ToArray();

        Assert.That(outboundCodes, Has.Length.EqualTo(55));
        foreach (int code in outboundCodes)
        {
            FtpReply outbound = new FtpReply();
            outbound.Code = code;
            outbound.Text = "reply " + code.ToString();
            using MemoryStream stream = new MemoryStream();

            codec.WriteReply(stream, outbound);
            stream.Position = 0;
            FtpReply inbound = codec.ReadReply(stream);

            Assert.Multiple(() =>
            {
                Assert.That(inbound.Code, Is.EqualTo(code), code.ToString());
                Assert.That(inbound.Text, Is.EqualTo(outbound.Text),
                    code.ToString());
                Assert.That(stream.CanWrite, Is.True, code.ToString());
            });
        }
    }

    [Test]
    public void WriteReply_round_trips_multiline_Unicode_and_blank_body_lines()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        FtpReply outbound = CreateMultilineReply();
        using MemoryStream stream = new MemoryStream();

        codec.WriteReply(stream, outbound);
        string wire = Encoding.UTF8.GetString(stream.ToArray());
        stream.Position = 0;
        FtpReply inbound = codec.ReadReply(stream);

        Assert.Multiple(() =>
        {
            Assert.That(wire, Is.EqualTo(
                "211-機能\r\n UTF8\r\n\r\n234 unrelated\r\n211 完了\r\n"));
            Assert.That(inbound.Multiline, Is.True);
            Assert.That(inbound.Lines, Has.Length.EqualTo(5));
            Assert.That(inbound.Text, Is.EqualTo("完了"));
            Assert.That(stream.CanWrite, Is.True);
        });
    }

    [Test]
    public void WriteReply_rejects_invalid_codes_framing_text_and_limits()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        using MemoryStream destination = new MemoryStream();
        FtpReply invalidLow = CreateSingleReply(99, "invalid");
        FtpReply invalidHigh = CreateSingleReply(600, "invalid");
        FtpReply injected = CreateSingleReply(
            220, "Ready\r\n421 closing");
        FtpReply nul = CreateSingleReply(220, "bad\0text");
        FtpReply unpaired = CreateSingleReply(220, "bad" + '\ud800');
        FtpReply nullText = CreateSingleReply(220, "text");
        nullText.Text = null!;
        FtpReply nullLines = CreateSingleReply(220, "text");
        nullLines.Lines = null!;
        FtpReply singleWithManyLines = CreateSingleReply(220, "text");
        singleWithManyLines.Lines =
            new[] { new FtpReplyLine(), new FtpReplyLine() };
        FtpReply tooShort = CreateSingleReply(211, "End");
        tooShort.Multiline = true;
        tooShort.Lines = new[] { new FtpReplyLine() };
        FtpReply mismatched = CreateMultilineReply();
        mismatched.Lines[0].Code = 212;
        FtpReply premature = CreateMultilineReply();
        premature.Lines[1].RawText = "211 premature";
        FtpReply tooLong = CreateSingleReply(
            220,
            new string('a', FtpDefaults.MaximumControlLineBytes - 5));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                codec.WriteReply(null!, invalidLow));
            Assert.Throws<ArgumentNullException>(() =>
                codec.WriteReply(destination, null!));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, invalidLow));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, invalidHigh));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, injected));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, nul));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, unpaired));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, nullText));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, nullLines));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, singleWithManyLines));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, tooShort));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, mismatched));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, premature));
            Assert.Throws<FtpProtocolException>(() =>
                codec.WriteReply(destination, tooLong));
        });
    }

    [Test]
    public void Preliminary_and_final_sequences_are_read_individually_and_typed()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        string wire =
            "150 Opening data\r\n226 Transfer complete\r\n" +
            "125 Data already open\r\n250 File action okay\r\n" +
            "425 Cannot open data connection\r\n";
        using MemoryStream source = new MemoryStream(
            Encoding.UTF8.GetBytes(wire));
        FtpCommand command = new FtpCommandCodec().ParseCommand("RETR file");

        FtpReply firstPreliminary = codec.ReadReply(source);
        FtpReply firstFinal = codec.ReadReply(source);
        FtpReply secondPreliminary = codec.ReadReply(source);
        FtpReply secondFinal = codec.ReadReply(source);
        FtpReply failure = codec.ReadReply(source);
        FtpCommandResult first = FtpReplyExtractor.ExtractTransferCompletion(
            command, new[] { firstPreliminary, firstFinal });
        FtpCommandResult second = FtpReplyExtractor.ExtractTransferCompletion(
            command, new[] { secondPreliminary, secondFinal });
        FtpCommandResult failed = FtpReplyExtractor.ExtractTransferCompletion(
            command, new[] { failure });

        Assert.Multiple(() =>
        {
            Assert.That(first.PreliminaryReply, Is.SameAs(firstPreliminary));
            Assert.That(first.FinalReply, Is.SameAs(firstFinal));
            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.PreliminaryReply,
                Is.SameAs(secondPreliminary));
            Assert.That(second.FinalReply, Is.SameAs(secondFinal));
            Assert.That(second.Succeeded, Is.True);
            Assert.That(failed.PreliminaryReply, Is.Null);
            Assert.That(failed.FinalReply, Is.SameAs(failure));
            Assert.That(failed.Succeeded, Is.False);
            Assert.That(source.Position, Is.EqualTo(source.Length));
        });
    }

    private static void AssertReadFailure(
        IFtpReplyCodec codec,
        byte[] bytes)
    {
        using MemoryStream source = new MemoryStream(bytes);
        Assert.Throws<FtpProtocolException>(() => codec.ReadReply(source),
            Convert.ToHexString(bytes));
    }

    private static string CreateMultilineWire(int middleLineCount)
    {
        StringBuilder value = new StringBuilder();
        value.Append("211-Start\r\n");
        int index = 0;
        while (index < middleLineCount)
        {
            value.Append(" body\r\n");
            index++;
        }
        value.Append("211 End\r\n");
        return value.ToString();
    }

    private static string CreateByteBoundedMultilineWire(int middleLineCount)
    {
        StringBuilder value = new StringBuilder();
        value.Append("211-");
        value.Append(new string(
            'a', FtpDefaults.MaximumControlLineBytes - 6));
        value.Append("\r\n");
        int index = 0;
        while (index < middleLineCount)
        {
            value.Append(new string(
                'b', FtpDefaults.MaximumControlLineBytes - 2));
            value.Append("\r\n");
            index++;
        }
        value.Append("211 ");
        value.Append(new string(
            'c', FtpDefaults.MaximumControlLineBytes - 6));
        value.Append("\r\n");
        return value.ToString();
    }

    private static FtpReply CreateSingleReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        return reply;
    }

    private static FtpReply CreateMultilineReply()
    {
        FtpReply reply = new FtpReply();
        reply.Code = 211;
        reply.Text = "完了";
        reply.Multiline = true;
        FtpReplyLine opening = new FtpReplyLine();
        opening.HasCode = true;
        opening.Code = 211;
        opening.Text = "機能";
        FtpReplyLine feature = new FtpReplyLine();
        feature.Text = " UTF8";
        FtpReplyLine blank = new FtpReplyLine();
        FtpReplyLine unrelated = new FtpReplyLine();
        unrelated.HasCode = true;
        unrelated.Code = 234;
        unrelated.Text = "unrelated";
        FtpReplyLine terminal = new FtpReplyLine();
        terminal.HasCode = true;
        terminal.Code = 211;
        terminal.Text = "完了";
        reply.Lines = new[]
        {
            opening, feature, blank, unrelated, terminal
        };
        return reply;
    }
}
