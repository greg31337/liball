using System.Collections.Generic;

namespace ftp;

internal class TelnetControlFilter
{
    internal const byte EndSubnegotiation = 240;
    internal const byte DataMark = 242;
    internal const byte InterruptProcess = 244;
    internal const byte StartSubnegotiation = 250;
    internal const byte Will = 251;
    internal const byte Wont = 252;
    internal const byte Do = 253;
    internal const byte Dont = 254;
    internal const byte InterpretAsCommand = 255;

    internal const int DataState = 0;
    internal const int CommandState = 1;
    internal const int OptionState = 2;
    internal const int SubnegotiationState = 3;
    internal const int SubnegotiationCommandState = 4;

    internal int State;
    internal byte PendingOptionCommand;
    internal int MaximumApplicationBytes;
    internal int MaximumNegotiationBytes;
    internal Queue<byte> ApplicationBytes;
    internal Queue<byte> NegotiationBytes;
    internal bool InterruptSignal;
    internal bool DataMarkSignal;
    internal bool UrgentSignal;
    internal bool AbortSynchronization;
    internal bool SynchronizationInterruptSeen;
    internal bool SynchronizationDataMarkSeen;
    internal bool DiscardUntilDataMark;
    internal bool Completed;
    internal bool Failed;

    internal TelnetControlFilter(int maximumApplicationBytes, int maximumNegotiationBytes)
    {
        if (maximumApplicationBytes <= 0 || maximumNegotiationBytes <= 0)
        {
            throw new FtpConfigurationException("Telnet filter byte limits must be positive.", null);
        }

        State = DataState;
        PendingOptionCommand = 0;
        MaximumApplicationBytes = maximumApplicationBytes;
        MaximumNegotiationBytes = maximumNegotiationBytes;
        ApplicationBytes = new Queue<byte>();
        NegotiationBytes = new Queue<byte>();
        InterruptSignal = false;
        DataMarkSignal = false;
        UrgentSignal = false;
        AbortSynchronization = false;
        SynchronizationInterruptSeen = false;
        SynchronizationDataMarkSeen = false;
        DiscardUntilDataMark = false;
        Completed = false;
        Failed = false;
    }

    internal void AppendBytes(byte[] bytes, int offset, int count)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        if (offset < 0 || count < 0 || offset > bytes.Length - count)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (Completed)
        {
            FailFilter("Telnet control data arrived after the input was completed.");
        }

        if (Failed)
        {
            throw new FtpProtocolException("The Telnet control filter is in a failed state.", null);
        }

        int index = offset;
        int end = offset + count;
        while (index < end)
        {
            ProcessByte(bytes[index]);
            index++;
        }
    }

    internal int ReadApplicationBytes(byte[] destination, int offset, int count)
    {
        ValidateDestinationRange(destination, offset, count);
        return DrainBytes(ApplicationBytes, destination, offset, count);
    }

    internal int ReadNegotiationBytes(byte[] destination, int offset, int count)
    {
        ValidateDestinationRange(destination, offset, count);
        return DrainBytes(NegotiationBytes, destination, offset, count);
    }

    internal int GetApplicationByteCount()
    {
        return ApplicationBytes.Count;
    }

    internal int GetNegotiationByteCount()
    {
        return NegotiationBytes.Count;
    }

    internal bool ConsumeInterruptSignal()
    {
        bool value = InterruptSignal;
        InterruptSignal = false;
        return value;
    }

    internal bool ConsumeDataMarkSignal()
    {
        bool value = DataMarkSignal;
        DataMarkSignal = false;
        return value;
    }

    internal bool ConsumeUrgentSignal()
    {
        bool value = UrgentSignal;
        UrgentSignal = false;
        return value;
    }

    internal bool ConsumeAbortSynchronization()
    {
        bool value = AbortSynchronization;
        AbortSynchronization = false;
        return value;
    }

    internal void SignalUrgentData()
    {
        if (Completed || Failed)
        {
            FailFilter("Urgent Telnet data arrived after the filter stopped accepting input.");
        }

        UrgentSignal = true;
        SynchronizationDataMarkSeen = true;
        DiscardUntilDataMark = false;
        CompleteSynchronization();
    }

    internal void CompleteInput()
    {
        if (Completed)
        {
            return;
        }

        if (Failed)
        {
            throw new FtpProtocolException("The Telnet control filter is in a failed state.", null);
        }

        if (State != DataState || DiscardUntilDataMark)
        {
            FailFilter("The Telnet control input ended during an incomplete command or synchronization.");
        }

        Completed = true;
    }

    internal void ProcessByte(byte value)
    {
        if (State == DataState)
        {
            if (value == InterpretAsCommand)
            {
                State = CommandState;
            }
            else if (!DiscardUntilDataMark)
            {
                EnqueueApplicationByte(value);
            }

            return;
        }

        if (State == CommandState)
        {
            ProcessCommandByte(value);
            return;
        }

        if (State == OptionState)
        {
            ProcessOptionByte(value);
            return;
        }

        if (State == SubnegotiationState)
        {
            if (value == InterpretAsCommand)
            {
                State = SubnegotiationCommandState;
            }

            return;
        }

        if (value == EndSubnegotiation)
        {
            State = DataState;
        }
        else
        {
            State = SubnegotiationState;
        }
    }

    internal void ProcessCommandByte(byte value)
    {
        if (value == InterpretAsCommand)
        {
            if (!DiscardUntilDataMark)
            {
                EnqueueApplicationByte(value);
            }

            State = DataState;
            return;
        }

        if (value == Will || value == Wont || value == Do || value == Dont)
        {
            PendingOptionCommand = value;
            State = OptionState;
            return;
        }

        if (value == StartSubnegotiation)
        {
            State = SubnegotiationState;
            return;
        }

        if (value == InterruptProcess)
        {
            ApplicationBytes.Clear();
            InterruptSignal = true;
            SynchronizationInterruptSeen = true;
            DiscardUntilDataMark = true;
            State = DataState;
            CompleteSynchronization();
            return;
        }

        if (value == DataMark)
        {
            DataMarkSignal = true;
            SynchronizationDataMarkSeen = true;
            DiscardUntilDataMark = false;
            State = DataState;
            CompleteSynchronization();
            return;
        }

        State = DataState;
    }

    internal void ProcessOptionByte(byte option)
    {
        if (PendingOptionCommand == Will)
        {
            EnqueueNegotiationRefusal(Dont, option);
        }
        else if (PendingOptionCommand == Do)
        {
            EnqueueNegotiationRefusal(Wont, option);
        }

        PendingOptionCommand = 0;
        State = DataState;
    }

    internal void EnqueueApplicationByte(byte value)
    {
        if (ApplicationBytes.Count >= MaximumApplicationBytes)
        {
            FailFilter("The filtered Telnet application-byte limit was exceeded.");
        }

        ApplicationBytes.Enqueue(value);
    }

    internal void EnqueueNegotiationRefusal(byte command, byte option)
    {
        if (MaximumNegotiationBytes - NegotiationBytes.Count < 3)
        {
            FailFilter("The Telnet negotiation-response byte limit was exceeded.");
        }

        NegotiationBytes.Enqueue(InterpretAsCommand);
        NegotiationBytes.Enqueue(command);
        NegotiationBytes.Enqueue(option);
    }

    internal void CompleteSynchronization()
    {
        if (SynchronizationInterruptSeen && SynchronizationDataMarkSeen)
        {
            AbortSynchronization = true;
            SynchronizationInterruptSeen = false;
            SynchronizationDataMarkSeen = false;
            DiscardUntilDataMark = false;
        }
    }

    internal int DrainBytes(Queue<byte> source, byte[] destination, int offset, int count)
    {
        int copied = 0;
        while (copied < count && source.Count != 0)
        {
            destination[offset + copied] = source.Dequeue();
            copied++;
        }

        return copied;
    }

    internal void ValidateDestinationRange(byte[] destination, int offset, int count)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (offset < 0 || count < 0 || offset > destination.Length - count)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }
    }

    internal void FailFilter(string message)
    {
        Failed = true;
        throw new FtpProtocolException(message, null);
    }
}
