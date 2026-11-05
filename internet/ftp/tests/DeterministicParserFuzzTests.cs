using System.Globalization;
using System.Text;

namespace ftp.Tests;

public class DeterministicParserFuzzTests
{
    private const int CaseCount = 128;

    [Test]
    public void Generated_commands_round_trip_and_control_mutations_are_rejected()
    {
        IFtpCommandCodec codec = new FtpCommandCodec();
        DeterministicGenerator generator = new DeterministicGenerator(0x434d4401u);

        for (int index = 0; index < CaseCount; index++)
        {
            string verb = "X-FUZZ" + generator.Next(100000).ToString(CultureInfo.InvariantCulture);
            string argument = CreateToken(generator, 1 + generator.Next(24));
            string input = verb.ToLowerInvariant() + " " + argument;

            FtpCommand first = codec.ParseCommand(input);
            string formatted = codec.FormatCommand(first);
            FtpCommand second = codec.ParseCommand(formatted.Substring(0, formatted.Length - 2));

            Assert.Multiple(() =>
            {
                Assert.That(first.Kind, Is.EqualTo(FtpCommandKind.Unknown), Case(index));
                Assert.That(second.Verb, Is.EqualTo(verb), Case(index));
                Assert.That(second.Argument, Is.EqualTo(argument), Case(index));
                Assert.That(formatted, Is.EqualTo(verb + " " + argument + "\r\n"), Case(index));
            });

            char mutation = new[] { '\r', '\n', '\0' }[generator.Next(3)];
            string malformed = input.Insert(1 + generator.Next(input.Length - 1), mutation.ToString());
            Assert.Throws<FtpProtocolException>(() => codec.ParseCommand(malformed), Case(index));
        }
    }

    [Test]
    public void Generated_replies_round_trip_and_framing_mutations_are_rejected()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        DeterministicGenerator generator = new DeterministicGenerator(0x52504c59u);

        for (int index = 0; index < CaseCount; index++)
        {
            FtpReply outbound = new FtpReply();
            outbound.Code = 100 + generator.Next(500);
            outbound.Text = "reply-" + CreateToken(generator, 1 + generator.Next(24));
            using MemoryStream stream = new MemoryStream();

            codec.WriteReply(stream, outbound);
            stream.Position = 0;
            FtpReply inbound = codec.ReadReply(stream);

            Assert.Multiple(() =>
            {
                Assert.That(inbound.Code, Is.EqualTo(outbound.Code), Case(index));
                Assert.That(inbound.Text, Is.EqualTo(outbound.Text), Case(index));
                Assert.That(inbound.Multiline, Is.False, Case(index));
            });

            string code = outbound.Code.ToString("D3", CultureInfo.InvariantCulture);
            string malformed = generator.Next(3) switch
            {
                0 => code + " invalid\n",
                1 => code + " invalid",
                _ => code + " invalid\0text\r\n"
            };
            using MemoryStream invalid = new MemoryStream(Encoding.UTF8.GetBytes(malformed));
            Assert.Throws<FtpProtocolException>(() => codec.ReadReply(invalid), Case(index));
        }
    }

    [Test]
    public void Generated_PORT_EPRT_PASV_and_EPSV_endpoints_round_trip()
    {
        DeterministicGenerator generator = new DeterministicGenerator(0x45504e54u);
        char[] delimiters = { '|', '!', '#', '^', '~' };

        for (int index = 0; index < CaseCount; index++)
        {
            FtpEndpoint endpoint = CreateEndpoint(
                generator.Next(256) + "." + generator.Next(256) + "." +
                generator.Next(256) + "." + generator.Next(256),
                1 + generator.Next(65535), FtpAddressFamily.IPv4);
            char delimiter = delimiters[generator.Next(delimiters.Length)];

            string portWire = FtpEndpointCodec.FormatPortArgument(endpoint);
            FtpEndpoint port = FtpEndpointCodec.ParsePortArgument(portWire);
            string eprtWire = FtpEndpointCodec.FormatExtendedPortArgument(endpoint, delimiter);
            FtpEndpoint eprt = FtpEndpointCodec.ParseExtendedPortArgument(eprtWire);
            FtpPassiveReply pasv = FtpEndpointCodec.ParsePassiveReply(
                FtpEndpointCodec.CreatePassiveReply(endpoint));
            FtpPassiveReply epsv = FtpEndpointCodec.ParseExtendedPassiveReply(
                FtpEndpointCodec.CreateExtendedPassiveReply(endpoint.Port, delimiter));

            Assert.Multiple(() =>
            {
                AssertEndpoint(port, endpoint, index);
                AssertEndpoint(eprt, endpoint, index);
                AssertEndpoint(pasv.Endpoint, endpoint, index);
                Assert.That(epsv.Endpoint.Port, Is.EqualTo(endpoint.Port), Case(index));
            });

            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParsePortArgument("1,2,3,256,1," + generator.Next(256)),
                Case(index));
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPortArgument(
                    eprtWire.Substring(0, eprtWire.Length - 1)), Case(index));

            FtpReply invalidPasv = FtpEndpointCodec.CreatePassiveReply(endpoint);
            invalidPasv.Text = "Entering Passive Mode (1,2,3,256,1,1)";
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParsePassiveReply(invalidPasv), Case(index));

            FtpReply invalidEpsv = FtpEndpointCodec.CreateExtendedPassiveReply(
                endpoint.Port, delimiter);
            invalidEpsv.Text = "Entering Extended Passive Mode (" + delimiter +
                delimiter + endpoint.Port + delimiter + ")";
            Assert.Throws<FtpProtocolException>(() =>
                FtpEndpointCodec.ParseExtendedPassiveReply(invalidEpsv), Case(index));
        }
    }

    [Test]
    public void Generated_IPv6_EPRT_endpoints_round_trip()
    {
        DeterministicGenerator generator = new DeterministicGenerator(0x49505636u);

        for (int index = 0; index < CaseCount; index++)
        {
            string host = "2001:db8:" + generator.Next(65536).ToString("x", CultureInfo.InvariantCulture) +
                "::" + (1 + generator.Next(65535)).ToString("x", CultureInfo.InvariantCulture);
            FtpEndpoint endpoint = CreateEndpoint(
                host, 1 + generator.Next(65535), FtpAddressFamily.IPv6);

            string wire = FtpEndpointCodec.FormatExtendedPortDefault(endpoint);
            FtpEndpoint parsed = FtpEndpointCodec.ParseExtendedPortArgument(wire);

            AssertEndpoint(parsed, endpoint, index);
        }
    }

    [Test]
    public void Generated_timestamps_round_trip_and_non_digit_mutations_are_rejected()
    {
        DeterministicGenerator generator = new DeterministicGenerator(0x54494d45u);

        for (int index = 0; index < CaseCount; index++)
        {
            int year = 1000 + generator.Next(9000);
            int month = 1 + generator.Next(12);
            int day = 1 + generator.Next(DateTime.DaysInMonth(year, month));
            int hour = generator.Next(24);
            int minute = generator.Next(60);
            int second = generator.Next(16) == 0 ? 60 : generator.Next(60);
            int fractionLength = generator.Next(13);
            string fraction = CreateDigits(generator, fractionLength);
            string wire = year.ToString("D4", CultureInfo.InvariantCulture) +
                month.ToString("D2", CultureInfo.InvariantCulture) +
                day.ToString("D2", CultureInfo.InvariantCulture) +
                hour.ToString("D2", CultureInfo.InvariantCulture) +
                minute.ToString("D2", CultureInfo.InvariantCulture) +
                second.ToString("D2", CultureInfo.InvariantCulture) +
                (fractionLength == 0 ? "" : "." + fraction);

            FtpTimestamp parsed = FtpTimestampCodec.ParseTimestampValue(wire);

            Assert.Multiple(() =>
            {
                Assert.That(FtpTimestampCodec.FormatTimestampValue(parsed), Is.EqualTo(wire), Case(index));
                Assert.That(parsed.FractionalSeconds, Is.EqualTo(fraction), Case(index));
                Assert.That(parsed.LeapSecond, Is.EqualTo(second == 60), Case(index));
            });

            int mutationIndex = generator.Next(14);
            string malformed = wire.Substring(0, mutationIndex) + "X" +
                wire.Substring(mutationIndex + 1);
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue(malformed), Case(index));
        }
    }

    [Test]
    public void Generated_facts_and_entries_round_trip_and_token_mutations_are_rejected()
    {
        DeterministicGenerator generator = new DeterministicGenerator(0x46414354u);

        for (int index = 0; index < CaseCount; index++)
        {
            string name = "X.Fuzz" + generator.Next(100000).ToString(CultureInfo.InvariantCulture);
            string value = CreateToken(generator, 1 + generator.Next(24));
            FtpFact fact = FtpMlsxCodec.ParseFact(name + "=" + value);
            string formattedFact = FtpMlsxCodec.FormatFact(fact);
            FtpFact reparsedFact = FtpMlsxCodec.ParseFact(formattedFact);
            string pathname = "item-" + CreateToken(generator, 1 + generator.Next(18));
            string line = formattedFact + ";size=" + generator.Next(100000) + "; " + pathname;
            FtpEntry entry = FtpMlsxCodec.ParseEntry(line);
            string formattedEntry = FtpMlsxCodec.FormatEntry(entry);
            FtpEntry reparsedEntry = FtpMlsxCodec.ParseEntry(formattedEntry);

            Assert.Multiple(() =>
            {
                Assert.That(reparsedFact.Name, Is.EqualTo(name.ToLowerInvariant()), Case(index));
                Assert.That(reparsedFact.Value, Is.EqualTo(value), Case(index));
                Assert.That(reparsedEntry.Name, Is.EqualTo(pathname), Case(index));
                Assert.That(reparsedEntry.Facts[0].Value, Is.EqualTo(value), Case(index));
            });

            Assert.Throws<FtpProtocolException>(() =>
                FtpMlsxCodec.ParseFact(name + value), Case(index));
            Assert.Throws<FtpProtocolException>(() =>
                FtpMlsxCodec.ParseEntry(formattedFact + " " + pathname), Case(index));
        }
    }

    [Test]
    public void Generated_machine_human_and_name_listings_preserve_parsed_data()
    {
        IFtpListingParser parser = new FtpListingParser();
        DeterministicGenerator generator = new DeterministicGenerator(0x4c495354u);

        for (int index = 0; index < CaseCount; index++)
        {
            int size = generator.Next(1000000);
            string name = "file-" + CreateToken(generator, 1 + generator.Next(16));
            string machineLine = "type=file;size=" + size +
                ";modify=20240229123456;X.Case=" + index + "; " + name;
            string humanLine = "-rw-r--r-- 1 owner group " + size +
                " Feb 29 2024 " + name;

            FtpListing machine = parser.ParseMachineListing(new[] { machineLine });
            FtpListing human = parser.ParseHumanListing(new[] { humanLine });
            FtpListing names = parser.ParseNameListing(new[] { name });

            Assert.Multiple(() =>
            {
                Assert.That(machine.Entries[0].Name, Is.EqualTo(name), Case(index));
                Assert.That(machine.Entries[0].Size, Is.EqualTo(size), Case(index));
                Assert.That(machine.Entries[0].Facts, Has.Length.EqualTo(4), Case(index));
                Assert.That(human.FormatName, Is.EqualTo("Unix"), Case(index));
                Assert.That(human.Entries[0].Name, Is.EqualTo(name), Case(index));
                Assert.That(human.Entries[0].Size, Is.EqualTo(size), Case(index));
                Assert.That(names.Entries[0].Name, Is.EqualTo(name), Case(index));
                Assert.That(names.Entries[0].Type, Is.EqualTo(FtpEntryType.Unknown), Case(index));
            });

            Assert.Throws<FtpProtocolException>(() => parser.ParseMachineListing(
                new[] { "X.Case=" + index + " " + name }), Case(index));
        }
    }

    private static FtpEndpoint CreateEndpoint(
        string host, int port, FtpAddressFamily family)
    {
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.AddressFamily = family;
        return endpoint;
    }

    private static void AssertEndpoint(FtpEndpoint actual, FtpEndpoint expected, int index)
    {
        Assert.That(actual.Host, Is.EqualTo(expected.Host), Case(index));
        Assert.That(actual.Port, Is.EqualTo(expected.Port), Case(index));
        Assert.That(actual.AddressFamily, Is.EqualTo(expected.AddressFamily), Case(index));
    }

    private static string CreateToken(DeterministicGenerator generator, int length)
    {
        const string characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.";
        StringBuilder builder = new StringBuilder(length);
        for (int index = 0; index < length; index++)
        {
            builder.Append(characters[generator.Next(characters.Length)]);
        }
        return builder.ToString();
    }

    private static string CreateDigits(DeterministicGenerator generator, int length)
    {
        StringBuilder builder = new StringBuilder(length);
        for (int index = 0; index < length; index++)
        {
            builder.Append((char)('0' + generator.Next(10)));
        }
        return builder.ToString();
    }

    private static string Case(int index)
    {
        return "deterministic case " + index.ToString(CultureInfo.InvariantCulture);
    }

    private class DeterministicGenerator
    {
        private uint State;

        internal DeterministicGenerator(uint seed)
        {
            State = seed;
        }

        internal int Next(int exclusiveMaximum)
        {
            State = (State * 1664525u) + 1013904223u;
            return (int)(State % (uint)exclusiveMaximum);
        }
    }
}
