namespace ftp.Tests;

public class LowLevelContractTests
{
    [Test]
    public void Command_codec_contract_can_parse_and_format()
    {
        IFtpCommandCodec codec = new CommandCodecStub();

        FtpCommand parsed = codec.ParseCommand("NOOP");
        string formatted = codec.FormatCommand(parsed);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Kind, Is.EqualTo(FtpCommandKind.Noop));
            Assert.That(parsed.Verb, Is.EqualTo(FtpCommandNames.Noop));
            Assert.That(parsed.RawText, Is.EqualTo("NOOP"));
            Assert.That(formatted, Is.EqualTo("NOOP\r\n"));
        });
    }

    [Test]
    public void Reply_codec_contract_can_read_write_and_classify()
    {
        IFtpReplyCodec codec = new ReplyCodecStub();
        using MemoryStream source = new MemoryStream(new byte[] { 1 });
        using MemoryStream destination = new MemoryStream();

        FtpReply reply = codec.ReadReply(source);
        FtpReplyClassification classification = codec.ClassifyReply(reply);
        codec.WriteReply(destination, reply);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.ServiceReady));
            Assert.That(classification.Category, Is.EqualTo(FtpReplyCategory.PositiveCompletion));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 2, 2, 0 }));
        });
    }

    [Test]
    public void Listing_parser_contract_exposes_distinct_listing_forms()
    {
        IFtpListingParser parser = new ListingParserStub();
        string[] lines = new[] { "entry" };

        FtpListing machine = parser.ParseMachineListing(lines);
        FtpListing human = parser.ParseHumanListing(lines);
        FtpListing names = parser.ParseNameListing(lines);

        Assert.Multiple(() =>
        {
            Assert.That(machine.MachineReadable, Is.True);
            Assert.That(machine.FormatName, Is.EqualTo("MLSx"));
            Assert.That(machine.RawLines, Is.SameAs(lines));
            Assert.That(human.MachineReadable, Is.False);
            Assert.That(human.FormatName, Is.EqualTo("Human"));
            Assert.That(names.MachineReadable, Is.False);
            Assert.That(names.FormatName, Is.EqualTo("Names"));
        });
    }

    private class CommandCodecStub : IFtpCommandCodec
    {
        public FtpCommand ParseCommand(string line)
        {
            FtpCommand command = new FtpCommand();
            command.Kind = FtpCommandKind.Noop;
            command.Verb = FtpCommandNames.Noop;
            command.RawText = line;
            return command;
        }

        public string FormatCommand(FtpCommand command)
        {
            return command.Verb + "\r\n";
        }
    }

    private class ReplyCodecStub : IFtpReplyCodec
    {
        public FtpReply ReadReply(Stream source)
        {
            source.ReadByte();
            FtpReply reply = new FtpReply();
            reply.Code = FtpReplyCodes.ServiceReady;
            return reply;
        }

        public void WriteReply(Stream destination, FtpReply reply)
        {
            destination.WriteByte((byte)(reply.Code / 100));
            destination.WriteByte((byte)((reply.Code / 10) % 10));
            destination.WriteByte((byte)(reply.Code % 10));
        }

        public FtpReplyClassification ClassifyReply(FtpReply reply)
        {
            FtpReplyClassification classification = new FtpReplyClassification();
            classification.Category = FtpReplyCategory.PositiveCompletion;
            classification.Succeeded = true;
            return classification;
        }
    }

    private class ListingParserStub : IFtpListingParser
    {
        public FtpListing ParseMachineListing(string[] lines)
        {
            FtpListing listing = new FtpListing();
            listing.RawLines = lines;
            listing.MachineReadable = true;
            listing.FormatName = "MLSx";
            return listing;
        }

        public FtpListing ParseHumanListing(string[] lines)
        {
            FtpListing listing = new FtpListing();
            listing.RawLines = lines;
            listing.FormatName = "Human";
            return listing;
        }

        public FtpListing ParseNameListing(string[] lines)
        {
            FtpListing listing = new FtpListing();
            listing.RawLines = lines;
            listing.FormatName = "Names";
            return listing;
        }
    }
}

