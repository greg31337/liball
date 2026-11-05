using fix;

namespace fix.Tests;

internal sealed class InMemoryDuplexTransportPair
{
    private readonly List<InMemoryScheduledDelivery> scheduled = [];
    private int nextSequence;

    internal InMemoryDuplexTransportEndpoint First;
    internal InMemoryDuplexTransportEndpoint Second;
    internal int CurrentTick;
    internal int ConnectionGeneration;

    internal InMemoryDuplexTransportPair()
    {
        First = new InMemoryDuplexTransportEndpoint(this);
        Second = new InMemoryDuplexTransportEndpoint(this);
        First.Peer = Second;
        Second.Peer = First;
    }

    internal void Connect()
    {
        scheduled.Clear();
        First.ResetForConnection();
        Second.ResetForConnection();
        ConnectionGeneration++;
    }

    internal void Disconnect()
    {
        scheduled.Clear();
        First.ResetForDisconnection();
        Second.ResetForDisconnection();
    }

    internal void Close(InMemoryDuplexTransportEndpoint endpoint)
    {
        scheduled.Clear();
        endpoint.ResetForDisconnection();
        endpoint.State = TransportState.Closed;
        endpoint.Peer.MarkPeerEnded();
    }

    internal void Reconnect()
    {
        Connect();
    }

    internal void Advance(int ticks)
    {
        if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
        CurrentTick += ticks;
        DeliverDueTraffic();
    }

    internal void Schedule(InMemoryDuplexTransportEndpoint destination, byte[] payload, int delayTicks)
    {
        InMemoryScheduledDelivery delivery = new InMemoryScheduledDelivery
        {
            Destination = destination,
            Payload = payload,
            DueTick = CurrentTick + delayTicks,
            Sequence = nextSequence
        };
        nextSequence++;
        scheduled.Add(delivery);
        DeliverDueTraffic();
    }

    private void DeliverDueTraffic()
    {
        scheduled.Sort((left, right) =>
        {
            int due = left.DueTick.CompareTo(right.DueTick);
            return due != 0 ? due : left.Sequence.CompareTo(right.Sequence);
        });
        int index = 0;
        while (index < scheduled.Count)
        {
            InMemoryScheduledDelivery delivery = scheduled[index];
            if (delivery.DueTick > CurrentTick) break;
            scheduled.RemoveAt(index);
            if (delivery.Destination.State == TransportState.Connected) delivery.Destination.Enqueue(delivery.Payload);
        }
    }
}

internal sealed class InMemoryDuplexTransportEndpoint : ISessionTransport
{
    private readonly InMemoryDuplexTransportPair pair;
    private readonly Queue<InMemoryDeliveryInstruction> instructions = new Queue<InMemoryDeliveryInstruction>();
    private readonly Queue<byte[]> inbound = new Queue<byte[]>();
    private byte[] current = [];
    private int currentOffset;
    private int coalesceRemaining;
    private readonly List<byte> coalesced = [];
    private byte[]? reorderedFirst;
    private bool peerEnded;

    internal InMemoryDuplexTransportEndpoint Peer = null!;
    internal TransportState State = TransportState.Disconnected;

    internal InMemoryDuplexTransportEndpoint(InMemoryDuplexTransportPair pair)
    {
        this.pair = pair;
    }

    public TransportWriteResult OpenTransport(SessionEndpoint endpoint, SessionTlsSettings tls)
    {
        if (State != TransportState.Connected) pair.Connect();
        return SuccessWrite(State, 0);
    }

    public TransportReadResult ReadTransport(Span<byte> destination)
    {
        if (State == TransportState.Closed) return FailedRead(State, "transport-closed");
        if (State != TransportState.Connected)
        {
            return new TransportReadResult { State = State, EndOfStream = peerEnded };
        }
        if (destination.Length == 0) return new TransportReadResult { State = State };
        if (currentOffset >= current.Length)
        {
            if (inbound.Count == 0)
            {
                if (peerEnded)
                {
                    State = TransportState.Disconnected;
                    return new TransportReadResult { State = State, EndOfStream = true };
                }
                return new TransportReadResult { State = State, WouldBlock = true };
            }
            current = inbound.Dequeue();
            currentOffset = 0;
        }

        int available = current.Length - currentOffset;
        int count = available < destination.Length ? available : destination.Length;
        current.AsSpan(currentOffset, count).CopyTo(destination);
        currentOffset += count;
        if (currentOffset >= current.Length)
        {
            current = [];
            currentOffset = 0;
        }
        return new TransportReadResult { State = State, BytesRead = count };
    }

    public TransportWriteResult WriteTransport(ReadOnlySpan<byte> source)
    {
        if (State == TransportState.Closed) return FailedWrite(State, "transport-closed");
        if (State != TransportState.Connected) return FailedWrite(State, "transport-disconnected");
        byte[] payload = source.ToArray();
        if (payload.Length == 0) return SuccessWrite(State, 0);

        if (coalesceRemaining > 0)
        {
            AddCoalesced(payload);
            return SuccessWrite(State, payload.Length);
        }
        if (reorderedFirst != null)
        {
            Deliver(payload);
            Deliver(reorderedFirst);
            reorderedFirst = null;
            return SuccessWrite(State, payload.Length);
        }

        InMemoryDeliveryInstruction instruction = instructions.Count == 0
            ? new InMemoryDeliveryInstruction { Kind = InMemoryDeliveryKind.Immediate }
            : instructions.Dequeue();
        ApplyInstruction(instruction, payload);
        return SuccessWrite(State, payload.Length);
    }

    public TransportWriteResult CloseTransport()
    {
        if (State == TransportState.Closed) return SuccessWrite(State, 0);
        pair.Close(this);
        return SuccessWrite(State, 0);
    }

    internal void FragmentNextWrite(params int[] fragmentSizes)
    {
        if (fragmentSizes.Length == 0 || fragmentSizes.Any(size => size <= 0)) throw new ArgumentException("Fragment sizes must be positive.", nameof(fragmentSizes));
        instructions.Enqueue(new InMemoryDeliveryInstruction { Kind = InMemoryDeliveryKind.Fragment, FragmentSizes = fragmentSizes.ToArray() });
    }

    internal void CoalesceNextWrites(int writeCount)
    {
        if (writeCount < 2) throw new ArgumentOutOfRangeException(nameof(writeCount));
        instructions.Enqueue(new InMemoryDeliveryInstruction { Kind = InMemoryDeliveryKind.Coalesce, Count = writeCount });
    }

    internal void DelayNextWrite(int ticks)
    {
        if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
        instructions.Enqueue(new InMemoryDeliveryInstruction { Kind = InMemoryDeliveryKind.Delay, Count = ticks });
    }

    internal void DropNextWrite()
    {
        instructions.Enqueue(new InMemoryDeliveryInstruction { Kind = InMemoryDeliveryKind.Drop });
    }

    internal void DuplicateNextWrite(int copies)
    {
        if (copies < 2) throw new ArgumentOutOfRangeException(nameof(copies));
        instructions.Enqueue(new InMemoryDeliveryInstruction { Kind = InMemoryDeliveryKind.Duplicate, Count = copies });
    }

    internal void ReorderNextWrites()
    {
        instructions.Enqueue(new InMemoryDeliveryInstruction { Kind = InMemoryDeliveryKind.Reorder });
    }

    internal void Enqueue(byte[] payload)
    {
        inbound.Enqueue(payload);
    }

    internal void ResetForConnection()
    {
        ClearTraffic();
        State = TransportState.Connected;
        peerEnded = false;
    }

    internal void ResetForDisconnection()
    {
        ClearTraffic();
        State = TransportState.Disconnected;
        peerEnded = true;
    }

    internal void MarkPeerEnded()
    {
        peerEnded = true;
    }

    private void ApplyInstruction(InMemoryDeliveryInstruction instruction, byte[] payload)
    {
        if (instruction.Kind == InMemoryDeliveryKind.Immediate) Deliver(payload);
        else if (instruction.Kind == InMemoryDeliveryKind.Fragment) DeliverFragments(payload, instruction.FragmentSizes);
        else if (instruction.Kind == InMemoryDeliveryKind.Coalesce)
        {
            coalesceRemaining = instruction.Count;
            coalesced.Clear();
            AddCoalesced(payload);
        }
        else if (instruction.Kind == InMemoryDeliveryKind.Delay) pair.Schedule(Peer, payload, instruction.Count);
        else if (instruction.Kind == InMemoryDeliveryKind.Drop)
        {
        }
        else if (instruction.Kind == InMemoryDeliveryKind.Duplicate)
        {
            for (int index = 0; index < instruction.Count; index++) Deliver(payload.ToArray());
        }
        else if (instruction.Kind == InMemoryDeliveryKind.Reorder) reorderedFirst = payload;
    }

    private void DeliverFragments(byte[] payload, int[] sizes)
    {
        int offset = 0;
        for (int index = 0; index < sizes.Length; index++)
        {
            if (offset + sizes[index] > payload.Length) throw new InvalidOperationException("Fragment sizes exceed the scripted write length.");
            Deliver(payload.AsSpan(offset, sizes[index]).ToArray());
            offset += sizes[index];
        }
        if (offset < payload.Length) Deliver(payload.AsSpan(offset).ToArray());
    }

    private void AddCoalesced(byte[] payload)
    {
        coalesced.AddRange(payload);
        coalesceRemaining--;
        if (coalesceRemaining == 0)
        {
            Deliver(coalesced.ToArray());
            coalesced.Clear();
        }
    }

    private void Deliver(byte[] payload)
    {
        if (Peer.State == TransportState.Connected) Peer.Enqueue(payload);
    }

    private void ClearTraffic()
    {
        instructions.Clear();
        inbound.Clear();
        current = [];
        currentOffset = 0;
        coalesceRemaining = 0;
        coalesced.Clear();
        reorderedFirst = null;
    }

    private static TransportWriteResult SuccessWrite(TransportState state, int bytesWritten)
    {
        return new TransportWriteResult { State = state, BytesWritten = bytesWritten };
    }

    private static TransportWriteResult FailedWrite(TransportState state, string code)
    {
        return new TransportWriteResult { State = state, ErrorCode = code, ErrorMessage = code };
    }

    private static TransportReadResult FailedRead(TransportState state, string code)
    {
        return new TransportReadResult { State = state, ErrorCode = code, ErrorMessage = code };
    }
}

internal sealed class InMemoryDeliveryInstruction
{
    internal InMemoryDeliveryKind Kind;
    internal int Count;
    internal int[] FragmentSizes = [];
}

internal sealed class InMemoryScheduledDelivery
{
    internal InMemoryDuplexTransportEndpoint Destination = null!;
    internal byte[] Payload = [];
    internal int DueTick;
    internal int Sequence;
}

internal enum InMemoryDeliveryKind
{
    Immediate,
    Fragment,
    Coalesce,
    Delay,
    Drop,
    Duplicate,
    Reorder
}
