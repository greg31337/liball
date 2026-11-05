using System.Text;

namespace ftp.Tests;

public class FtpLineAndTelnetBoundaryTests
{
    [Test]
    public void Reader_handles_every_fragment_boundary_in_coalesced_CRLF_input()
    {
        byte[] wire = Encoding.ASCII.GetBytes(
            "USER guest\r\nNOOP\r\n\r\nQUIT\r\n");
        string[] expected = { "USER guest", "NOOP", "", "QUIT" };

        int split = 0;
        while (split <= wire.Length)
        {
            FtpLineReader reader = new FtpLineReader(32, expected.Length);
            reader.AppendBytes(wire, 0, split);
            reader.AppendBytes(wire, split, wire.Length - split);
            reader.CompleteInput();

            Assert.That(reader.GetPendingLineCount(),
                Is.EqualTo(expected.Length), "split " + split);
            foreach (string line in expected)
            {
                Assert.That(ReadAscii(reader), Is.EqualTo(line),
                    "split " + split);
            }
            Assert.That(reader.ReadLine(), Is.Null, "split " + split);
            split++;
        }
    }

    [Test]
    public void Reader_enforces_exact_bounds_completion_and_all_EOF_shapes()
    {
        FtpLineReader exactLine = new FtpLineReader(8, 2);
        AppendAscii(exactLine, "ABCDEF\r\n");
        FtpLineReader exactQueue = new FtpLineReader(4, 2);
        AppendAscii(exactQueue, "A\r\nB\r\n");
        FtpLineReader empty = new FtpLineReader(4, 1);
        empty.CompleteInput();
        FtpLineReader partial = new FtpLineReader(8, 1);
        AppendAscii(partial, "ABC");
        FtpLineReader carriageReturn = new FtpLineReader(8, 1);
        AppendAscii(carriageReturn, "ABC\r");
        FtpLineReader oversized = new FtpLineReader(8, 1);

        Assert.Multiple(() =>
        {
            Assert.That(ReadAscii(exactLine), Is.EqualTo("ABCDEF"));
            Assert.That(exactQueue.GetPendingLineCount(), Is.EqualTo(2));
            Assert.That(ReadAscii(exactQueue), Is.EqualTo("A"));
            Assert.That(ReadAscii(exactQueue), Is.EqualTo("B"));
            Assert.That(empty.Completed, Is.True);
            Assert.That(empty.HasPartialLine(), Is.False);
            Assert.Throws<FtpProtocolException>(() => partial.CompleteInput());
            Assert.Throws<FtpProtocolException>(() =>
                carriageReturn.CompleteInput());
            Assert.Throws<FtpProtocolException>(() =>
                AppendAscii(oversized, "ABCDEFG"));
            Assert.Throws<FtpProtocolException>(() =>
                empty.AppendBytes(new byte[] { 13, 10 }, 0, 2));
            Assert.Throws<ArgumentNullException>(() =>
                exactLine.AppendBytes(null!, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                exactLine.AppendBytes(new byte[1], 1, 1));
        });
    }

    [Test]
    public void Framed_commands_reject_every_invalid_negotiated_encoding_shape()
    {
        byte[][] invalidUtf8 =
        {
            new byte[] { 78, 79, 79, 80, 32, 128 },
            new byte[] { 78, 79, 79, 80, 32, 192, 175 },
            new byte[] { 78, 79, 79, 80, 32, 226, 130 },
            new byte[] { 78, 79, 79, 80, 32, 237, 160, 128 },
            new byte[] { 78, 79, 79, 80, 32, 244, 144, 128, 128 }
        };

        foreach (byte[] invalid in invalidUtf8)
        {
            byte[] line = ReadOneFramedLine(invalid);
            Assert.Throws<FtpProtocolException>(() =>
                FtpCommandParser.ParseEncodedCommand(line, Encoding.UTF8));
        }

        byte[] nonAscii = ReadOneFramedLine(
            new byte[] { 78, 79, 79, 80, 32, 255 });
        byte[] utf16 = ReadOneFramedLine(
            Encoding.Unicode.GetBytes("NOOP"));

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpCommandParser.ParseEncodedCommand(
                    nonAscii, Encoding.ASCII));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpCommandParser.ParseEncodedCommand(
                    utf16, Encoding.Unicode));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpCommandParser.ParseEncodedCommand(
                    Encoding.ASCII.GetBytes("NOOP"), null));
        });
    }

    [Test]
    public void Filter_handles_every_IAC_command_form_when_fragmented_byte_by_byte()
    {
        int command = TelnetControlFilter.EndSubnegotiation;
        while (command <= TelnetControlFilter.InterpretAsCommand)
        {
            TelnetControlFilter filter = new TelnetControlFilter(32, 8);
            byte[] input = CreateIacForm((byte)command);
            int index = 0;
            while (index < input.Length)
            {
                filter.AppendBytes(input, index, 1);
                index++;
            }
            filter.CompleteInput();

            byte[] expectedApplication = ExpectedIacApplication((byte)command);
            byte[] expectedNegotiation =
                ExpectedIacNegotiation((byte)command);
            Assert.Multiple(() =>
            {
                Assert.That(ReadApplicationBytes(filter),
                    Is.EqualTo(expectedApplication), "IAC " + command);
                Assert.That(ReadNegotiationBytes(filter),
                    Is.EqualTo(expectedNegotiation), "IAC " + command);
                Assert.That(filter.ConsumeInterruptSignal(),
                    Is.EqualTo(command == TelnetControlFilter.InterruptProcess),
                    "IAC " + command);
                Assert.That(filter.ConsumeDataMarkSignal(),
                    Is.EqualTo(
                        command == TelnetControlFilter.DataMark ||
                        command == TelnetControlFilter.InterruptProcess),
                    "IAC " + command);
                Assert.That(filter.ConsumeAbortSynchronization(),
                    Is.EqualTo(command == TelnetControlFilter.InterruptProcess),
                    "IAC " + command);
                Assert.That(filter.State,
                    Is.EqualTo(TelnetControlFilter.DataState),
                    "IAC " + command);
            });
            command++;
        }
    }

    [Test]
    public void Urgent_IP_and_DM_orderings_discard_stale_data_and_deliver_ABOR()
    {
        TelnetControlFilter ipThenDm = new TelnetControlFilter(32, 8);
        AppendBytes(ipThenDm, new byte[]
        {
            83, 84, 65, 76, 69,
            255, TelnetControlFilter.InterruptProcess,
            68, 82, 79, 80,
            255, TelnetControlFilter.DataMark,
            65, 66, 79, 82, 13, 10
        });

        TelnetControlFilter ipThenUrgent = new TelnetControlFilter(32, 8);
        AppendBytes(ipThenUrgent, new byte[]
        {
            83, 84, 65, 76, 69,
            255, TelnetControlFilter.InterruptProcess,
            68, 82, 79, 80
        });
        ipThenUrgent.SignalUrgentData();
        AppendAscii(ipThenUrgent, "ABOR\r\n");

        TelnetControlFilter urgentThenIp = new TelnetControlFilter(32, 8);
        urgentThenIp.SignalUrgentData();
        AppendAscii(urgentThenIp, "STALE");
        AppendBytes(urgentThenIp, new byte[]
        {
            255, TelnetControlFilter.InterruptProcess
        });
        AppendAscii(urgentThenIp, "ABOR\r\n");

        AssertSynchronizedAbort(ipThenDm, false, true);
        AssertSynchronizedAbort(ipThenUrgent, true, false);
        AssertSynchronizedAbort(urgentThenIp, true, false);
    }

    [Test]
    public void Clear_control_filters_Telnet_while_TLS_application_data_bypasses_it()
    {
        byte[] negotiationPrefixedCommand =
        {
            255, TelnetControlFilter.Will, 1,
            78, 79, 79, 80
        };
        TelnetControlFilter clearFilter = new TelnetControlFilter(16, 8);
        AppendBytes(clearFilter, negotiationPrefixedCommand);
        byte[] clearApplication = ReadApplicationBytes(clearFilter);
        FtpCommand clearCommand = FtpCommandParser.ParseEncodedCommand(
            clearApplication, Encoding.ASCII);

        byte[] protectedApplication = Encoding.UTF8.GetBytes(
            "CWD café/資料/😀");
        FtpCommand protectedCommand = FtpCommandParser.ParseEncodedCommand(
            protectedApplication, Encoding.UTF8);

        Assert.Multiple(() =>
        {
            Assert.That(clearCommand.Kind, Is.EqualTo(FtpCommandKind.Noop));
            Assert.That(ReadNegotiationBytes(clearFilter), Is.EqualTo(
                new byte[] { 255, TelnetControlFilter.Dont, 1 }));
            Assert.That(protectedCommand.Argument,
                Is.EqualTo("café/資料/😀"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpCommandParser.ParseEncodedCommand(
                    negotiationPrefixedCommand, Encoding.UTF8));
        });
    }

    [Test]
    public void Filter_enforces_exact_queue_bounds_and_all_incomplete_EOF_states()
    {
        TelnetControlFilter exact = new TelnetControlFilter(4, 6);
        AppendAscii(exact, "ABCD");
        Assert.That(ReadApplicationBytes(exact),
            Is.EqualTo(Encoding.ASCII.GetBytes("ABCD")));
        AppendBytes(exact, new byte[]
        {
            255, TelnetControlFilter.Will, 1,
            255, TelnetControlFilter.Do, 2
        });
        Assert.That(ReadNegotiationBytes(exact), Is.EqualTo(new byte[]
        {
            255, TelnetControlFilter.Dont, 1,
            255, TelnetControlFilter.Wont, 2
        }));

        TelnetControlFilter command = IncompleteFilter(
            new byte[] { 255 });
        TelnetControlFilter option = IncompleteFilter(
            new byte[] { 255, TelnetControlFilter.Will });
        TelnetControlFilter subnegotiation = IncompleteFilter(
            new byte[] { 255, TelnetControlFilter.StartSubnegotiation, 1 });
        TelnetControlFilter escapedSubnegotiation = IncompleteFilter(
            new byte[]
            {
                255, TelnetControlFilter.StartSubnegotiation, 1, 255
            });
        TelnetControlFilter synchronization = IncompleteFilter(
            new byte[] { 255, TelnetControlFilter.InterruptProcess });

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => command.CompleteInput());
            Assert.Throws<FtpProtocolException>(() => option.CompleteInput());
            Assert.Throws<FtpProtocolException>(() =>
                subnegotiation.CompleteInput());
            Assert.Throws<FtpProtocolException>(() =>
                escapedSubnegotiation.CompleteInput());
            Assert.Throws<FtpProtocolException>(() =>
                synchronization.CompleteInput());
            Assert.Throws<FtpConfigurationException>(() =>
                new TelnetControlFilter(0, 1));
            Assert.Throws<FtpConfigurationException>(() =>
                new TelnetControlFilter(1, 0));
            Assert.Throws<ArgumentNullException>(() =>
                exact.AppendBytes(null!, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                exact.ReadApplicationBytes(new byte[1], 1, 1));
        });

        exact.CompleteInput();
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                exact.AppendBytes(new byte[] { 65 }, 0, 1));
            Assert.Throws<FtpProtocolException>(() =>
                exact.SignalUrgentData());
        });
    }

    private static byte[] CreateIacForm(byte command)
    {
        if (command == TelnetControlFilter.InterruptProcess)
        {
            return new byte[]
            {
                255, command, 88, 255, TelnetControlFilter.DataMark, 65
            };
        }
        if (command == TelnetControlFilter.StartSubnegotiation)
        {
            return new byte[]
            {
                255, command, 1, 2, 255, 255, 3,
                255, TelnetControlFilter.EndSubnegotiation, 65
            };
        }
        if (command == TelnetControlFilter.Will ||
            command == TelnetControlFilter.Wont ||
            command == TelnetControlFilter.Do ||
            command == TelnetControlFilter.Dont)
        {
            return new byte[] { 255, command, 1, 65 };
        }
        return new byte[] { 255, command, 65 };
    }

    private static byte[] ExpectedIacApplication(byte command)
    {
        if (command == TelnetControlFilter.InterpretAsCommand)
        {
            return new byte[] { 255, 65 };
        }
        return new byte[] { 65 };
    }

    private static byte[] ExpectedIacNegotiation(byte command)
    {
        if (command == TelnetControlFilter.Will)
        {
            return new byte[] { 255, TelnetControlFilter.Dont, 1 };
        }
        if (command == TelnetControlFilter.Do)
        {
            return new byte[] { 255, TelnetControlFilter.Wont, 1 };
        }
        return new byte[0];
    }

    private static void AssertSynchronizedAbort(
        TelnetControlFilter filter,
        bool urgent,
        bool dataMark)
    {
        byte[] application = ReadApplicationBytes(filter);
        FtpLineReader reader = new FtpLineReader(16, 1);
        reader.AppendBytes(application, 0, application.Length);
        FtpCommand? command = FtpCommandParser.ParseEncodedCommand(
            reader.ReadLine()!, Encoding.ASCII);

        Assert.Multiple(() =>
        {
            Assert.That(command.Kind, Is.EqualTo(FtpCommandKind.Abor));
            Assert.That(filter.ConsumeInterruptSignal(), Is.True);
            Assert.That(filter.ConsumeUrgentSignal(), Is.EqualTo(urgent));
            Assert.That(filter.ConsumeDataMarkSignal(), Is.EqualTo(dataMark));
            Assert.That(filter.ConsumeAbortSynchronization(), Is.True);
            Assert.That(filter.ConsumeAbortSynchronization(), Is.False);
            Assert.That(filter.DiscardUntilDataMark, Is.False);
        });
    }

    private static TelnetControlFilter IncompleteFilter(byte[] bytes)
    {
        TelnetControlFilter filter = new TelnetControlFilter(16, 8);
        AppendBytes(filter, bytes);
        return filter;
    }

    private static byte[] ReadOneFramedLine(byte[] content)
    {
        byte[] wire = new byte[content.Length + 2];
        Array.Copy(content, wire, content.Length);
        wire[wire.Length - 2] = 13;
        wire[wire.Length - 1] = 10;
        FtpLineReader reader = new FtpLineReader(wire.Length, 1);
        int index = 0;
        while (index < wire.Length)
        {
            reader.AppendBytes(wire, index, 1);
            index++;
        }
        return reader.ReadLine()!;
    }

    private static void AppendAscii(FtpLineReader reader, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        reader.AppendBytes(bytes, 0, bytes.Length);
    }

    private static void AppendAscii(
        TelnetControlFilter filter,
        string text)
    {
        AppendBytes(filter, Encoding.ASCII.GetBytes(text));
    }

    private static void AppendBytes(
        TelnetControlFilter filter,
        byte[] bytes)
    {
        filter.AppendBytes(bytes, 0, bytes.Length);
    }

    private static string? ReadAscii(FtpLineReader reader)
    {
        byte[]? line = reader.ReadLine();
        return line == null ? null : Encoding.ASCII.GetString(line);
    }

    private static byte[] ReadApplicationBytes(TelnetControlFilter filter)
    {
        byte[] result = new byte[filter.GetApplicationByteCount()];
        int count = filter.ReadApplicationBytes(result, 0, result.Length);
        Assert.That(count, Is.EqualTo(result.Length));
        return result;
    }

    private static byte[] ReadNegotiationBytes(TelnetControlFilter filter)
    {
        byte[] result = new byte[filter.GetNegotiationByteCount()];
        int count = filter.ReadNegotiationBytes(result, 0, result.Length);
        Assert.That(count, Is.EqualTo(result.Length));
        return result;
    }
}
