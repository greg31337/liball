using System.Collections.Generic;

namespace dns;

internal class DnsTsigStreamState
{
    internal DnsTsigKey Key = new DnsTsigKey();
    internal byte[] RequestMac = [];
    internal byte[] PriorMac = [];
    internal List<byte[]> UnsignedMessages = new List<byte[]>();
    internal bool FirstMessage = true;
    internal bool LastMessageWasSigned;
    internal bool HasTimeSigned;
    internal ulong LastTimeSigned;
    internal int MessagesSinceSignature;
    internal int MessageCount;
}

internal static class TsigStreamOperations
{
    internal static DnsTsigStreamState CreateState(
        DnsTsigKey key,
        byte[] requestMac)
    {
        if (key == null || requestMac == null ||
            requestMac.Length == 0 || requestMac.Length > 65535)
        {
            throw new System.ArgumentNullException("key");
        }

        DnsTsigStreamState result = new DnsTsigStreamState();
        result.Key = key;
        result.RequestMac = RecordSetOperations.CopyBytes(requestMac);
        result.PriorMac = RecordSetOperations.CopyBytes(requestMac);
        return result;
    }

    internal static DnsTsigResult SignNext(
        DnsTsigStreamState state,
        DnsMessage message,
        DnsClockValue now)
    {
        if (state == null)
        {
            return Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "tsig.chain",
                "A TSIG signing chain state is required.");
        }

        DnsTsigResult result;
        if (state.FirstMessage)
        {
            result = TsigOperations.Sign(
                message,
                state.Key,
                state.RequestMac,
                now);
        }
        else
        {
            result = TsigOperations.SignChained(
                message,
                state.Key,
                state.PriorMac,
                new List<byte[]>(),
                now);
        }

        state.MessageCount = state.MessageCount + 1;
        if (result.Success && state.HasTimeSigned &&
            result.TimeSigned < state.LastTimeSigned)
        {
            return TimeFailure(
                "Successive signed transfer messages require nondecreasing signing times.");
        }

        if (result.Success)
        {
            state.PriorMac = RecordSetOperations.CopyBytes(result.Mac);
            state.FirstMessage = false;
            state.LastMessageWasSigned = true;
            state.MessagesSinceSignature = 0;
            state.UnsignedMessages.Clear();
            state.HasTimeSigned = true;
            state.LastTimeSigned = result.TimeSigned;
        }

        return result;
    }

    internal static DnsTsigResult VerifyNext(
        DnsTsigStreamState state,
        DnsMessage message,
        DnsClockValue now)
    {
        if (state == null || message == null)
        {
            return Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "tsig.chain",
                "A TSIG verification chain state and message are required.");
        }

        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(message);
        if (!placement.Success)
        {
            DnsTsigResult invalid = new DnsTsigResult();
            CopyDiagnostics(placement.Diagnostics, invalid.Diagnostics);
            return invalid;
        }

        state.MessageCount = state.MessageCount + 1;
        if (!placement.HasTsig)
        {
            return AcceptUnsignedMessage(state, message);
        }

        DnsTsigResult result;
        if (state.FirstMessage)
        {
            result = TsigOperations.Verify(
                message,
                state.Key,
                state.RequestMac,
                now);
        }
        else
        {
            result = TsigOperations.VerifyChained(
                message,
                state.Key,
                state.PriorMac,
                state.UnsignedMessages,
                now);
        }

        if (result.Success && result.Authenticated &&
            result.ErrorValue == 0 && state.HasTimeSigned &&
            result.TimeSigned < state.LastTimeSigned)
        {
            return TimeFailure(
                "Successive authenticated transfer messages have decreasing signing times.");
        }

        if (result.Success && result.Authenticated &&
            result.ErrorValue == 0)
        {
            state.PriorMac = RecordSetOperations.CopyBytes(result.Mac);
            state.FirstMessage = false;
            state.LastMessageWasSigned = true;
            state.MessagesSinceSignature = 0;
            state.UnsignedMessages.Clear();
            state.HasTimeSigned = true;
            state.LastTimeSigned = result.TimeSigned;
        }

        return result;
    }

    internal static DnsTsigResult Complete(
        DnsTsigStreamState state)
    {
        if (state == null || state.FirstMessage ||
            !state.LastMessageWasSigned)
        {
            return Failure(
                DnsDiagnosticCode.InvalidTsigPlacement,
                "tsig.chain.final",
                "A TSIG transfer chain must start and end with signed messages.");
        }

        DnsTsigResult result = new DnsTsigResult();
        result.Success = true;
        result.Authenticated = true;
        result.Mac = RecordSetOperations.CopyBytes(state.PriorMac);
        result.PriorMac = RecordSetOperations.CopyBytes(state.PriorMac);
        return result;
    }

    static DnsTsigResult AcceptUnsignedMessage(
        DnsTsigStreamState state,
        DnsMessage message)
    {
        if (state.FirstMessage)
        {
            return Failure(
                DnsDiagnosticCode.InvalidTsigPlacement,
                "tsig.chain.first",
                "The first transfer response message must contain TSIG.");
        }

        if (state.MessagesSinceSignature >= 99)
        {
            return Failure(
                DnsDiagnosticCode.InvalidTsigPlacement,
                "tsig.chain.interval",
                "A transfer chain cannot contain more than 99 unsigned intermediary messages.");
        }

        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            message,
            new DnsEncodeOptions());
        if (!encoded.Success)
        {
            DnsTsigResult failure = new DnsTsigResult();
            CopyDiagnostics(encoded.Diagnostics, failure.Diagnostics);
            return failure;
        }

        state.UnsignedMessages.Add(encoded.Bytes);
        state.MessagesSinceSignature = state.MessagesSinceSignature + 1;
        state.LastMessageWasSigned = false;
        DnsTsigResult result = new DnsTsigResult();
        result.Success = true;
        result.Message = message;
        result.PriorMac = RecordSetOperations.CopyBytes(state.PriorMac);
        return result;
    }

    static DnsTsigResult Failure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        DnsTsigResult result = new DnsTsigResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    static DnsTsigResult TimeFailure(string reason)
    {
        DnsTsigResult result = Failure(
            DnsDiagnosticCode.ResponseMismatch,
            "tsig.chain.timeSigned",
            reason);
        result.Success = true;
        result.Authenticated = true;
        result.Error = DnsTsigError.BadTime;
        result.ErrorValue = (int)DnsTsigError.BadTime;
        return result;
    }

    static void CopyDiagnostics(
        List<DnsDiagnostic> source,
        List<DnsDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
}
