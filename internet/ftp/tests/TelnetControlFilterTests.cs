namespace ftp.Tests;

public class TelnetControlFilterTests
{
    [Test]
    public void Filter_passes_application_data_and_unescapes_fragmented_iac()
    {
        TelnetControlFilter filter = new TelnetControlFilter(16, 16);

        filter.AppendBytes(new byte[] { 65, TelnetControlFilter.InterpretAsCommand }, 0, 2);
        filter.AppendBytes(new byte[] { TelnetControlFilter.InterpretAsCommand, 66 }, 0, 2);

        Assert.That(ReadApplicationBytes(filter), Is.EqualTo(new byte[] { 65, 255, 66 }));
        Assert.That(filter.GetNegotiationByteCount(), Is.Zero);
    }

    [Test]
    public void Filter_refuses_enable_negotiation_without_looping_on_disable_messages()
    {
        TelnetControlFilter filter = new TelnetControlFilter(16, 16);
        byte[] input =
        {
            TelnetControlFilter.InterpretAsCommand, TelnetControlFilter.Will, 1,
            TelnetControlFilter.InterpretAsCommand, TelnetControlFilter.Do, 3,
            TelnetControlFilter.InterpretAsCommand, TelnetControlFilter.Wont, 5,
            TelnetControlFilter.InterpretAsCommand, TelnetControlFilter.Dont, 6,
            78, 79, 79, 80,
        };

        filter.AppendBytes(input, 0, input.Length);

        Assert.Multiple(() =>
        {
            Assert.That(ReadNegotiationBytes(filter), Is.EqualTo(new byte[]
            {
                255, TelnetControlFilter.Dont, 1,
                255, TelnetControlFilter.Wont, 3,
            }));
            Assert.That(ReadApplicationBytes(filter), Is.EqualTo(new byte[] { 78, 79, 79, 80 }));
        });
    }

    [Test]
    public void Filter_skips_fragmented_subnegotiation_and_escaped_subnegotiation_iac()
    {
        TelnetControlFilter filter = new TelnetControlFilter(16, 16);
        byte[] first =
        {
            TelnetControlFilter.InterpretAsCommand,
            TelnetControlFilter.StartSubnegotiation,
            24,
            1,
            TelnetControlFilter.InterpretAsCommand,
        };
        byte[] second =
        {
            TelnetControlFilter.InterpretAsCommand,
            2,
            TelnetControlFilter.InterpretAsCommand,
            TelnetControlFilter.EndSubnegotiation,
            88,
        };

        filter.AppendBytes(first, 0, first.Length);
        filter.AppendBytes(second, 0, second.Length);

        Assert.That(ReadApplicationBytes(filter), Is.EqualTo(new byte[] { 88 }));
        Assert.That(filter.State, Is.EqualTo(TelnetControlFilter.DataState));
    }

    [Test]
    public void Ip_and_dm_discard_stale_data_and_signal_abort_synchronization()
    {
        TelnetControlFilter filter = new TelnetControlFilter(32, 16);
        byte[] input =
        {
            83, 84, 65, 76, 69,
            TelnetControlFilter.InterpretAsCommand, TelnetControlFilter.InterruptProcess,
            68, 82, 79, 80,
            TelnetControlFilter.InterpretAsCommand, TelnetControlFilter.DataMark,
            65, 66, 79, 82, 13, 10,
        };

        filter.AppendBytes(input, 0, input.Length);

        Assert.Multiple(() =>
        {
            Assert.That(ReadApplicationBytes(filter), Is.EqualTo(new byte[] { 65, 66, 79, 82, 13, 10 }));
            Assert.That(filter.ConsumeInterruptSignal(), Is.True);
            Assert.That(filter.ConsumeInterruptSignal(), Is.False);
            Assert.That(filter.ConsumeDataMarkSignal(), Is.True);
            Assert.That(filter.ConsumeAbortSynchronization(), Is.True);
            Assert.That(filter.ConsumeAbortSynchronization(), Is.False);
            Assert.That(filter.DiscardUntilDataMark, Is.False);
        });
    }

    [Test]
    public void Urgent_mark_can_arrive_before_ip_and_still_complete_synchronization()
    {
        TelnetControlFilter filter = new TelnetControlFilter(16, 16);
        filter.SignalUrgentData();
        byte[] input =
        {
            TelnetControlFilter.InterpretAsCommand,
            TelnetControlFilter.InterruptProcess,
            65, 66, 79, 82,
        };

        filter.AppendBytes(input, 0, input.Length);

        Assert.Multiple(() =>
        {
            Assert.That(filter.ConsumeUrgentSignal(), Is.True);
            Assert.That(filter.ConsumeInterruptSignal(), Is.True);
            Assert.That(filter.ConsumeAbortSynchronization(), Is.True);
            Assert.That(ReadApplicationBytes(filter), Is.EqualTo(new byte[] { 65, 66, 79, 82 }));
        });
    }

    [Test]
    public void Filter_enforces_application_and_negotiation_output_bounds()
    {
        TelnetControlFilter application = new TelnetControlFilter(3, 3);
        TelnetControlFilter negotiation = new TelnetControlFilter(3, 2);
        byte[] willOption =
        {
            TelnetControlFilter.InterpretAsCommand,
            TelnetControlFilter.Will,
            1,
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => application.AppendBytes(new byte[] { 1, 2, 3, 4 }, 0, 4));
            Assert.Throws<FtpProtocolException>(() => negotiation.AppendBytes(willOption, 0, willOption.Length));
            Assert.That(application.GetApplicationByteCount(), Is.EqualTo(3));
            Assert.That(negotiation.GetNegotiationByteCount(), Is.Zero);
            Assert.That(application.Failed, Is.True);
            Assert.That(negotiation.Failed, Is.True);
        });
    }

    [Test]
    public void Filter_rejects_end_of_input_during_telnet_command_or_sync()
    {
        TelnetControlFilter command = new TelnetControlFilter(8, 8);
        command.AppendBytes(new byte[] { TelnetControlFilter.InterpretAsCommand }, 0, 1);
        TelnetControlFilter synchronization = new TelnetControlFilter(8, 8);
        synchronization.AppendBytes(new byte[]
        {
            TelnetControlFilter.InterpretAsCommand,
            TelnetControlFilter.InterruptProcess,
        }, 0, 2);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => command.CompleteInput());
            Assert.Throws<FtpProtocolException>(() => synchronization.CompleteInput());
        });
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
