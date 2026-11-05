using System;
using System.Collections.Generic;

namespace dns;

internal class EdnsResponsePreparation
{
    internal DnsMessage Message = new DnsMessage();
    internal int PayloadSizeLimit = DnsProtocolLimits.ClassicUdpPayloadSize;
    internal bool BadVersion;
}

internal class EdnsResponseMetadata
{
    internal bool RequestUsedEdns;
    internal bool ResponseUsedEdns;
    internal bool HasEdnsCapability;
    internal bool SupportsEdns;
    internal int UdpPayloadSize = DnsProtocolLimits.ClassicUdpPayloadSize;
    internal int Version;
    internal bool BadVersion;
    internal bool ShouldRetryWithoutEdns;
}

internal static class EdnsOperations
{
    const int DnssecAnswerOkFlag = 32768;

    internal static DnsMessage PrepareRequest(
        DnsMessage message,
        int udpPayloadSize,
        bool requestDnssec)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        DnsMessage prepared = MessageOperations.CloneMessage(message);
        DnsResourceRecord? opt = MessageOperations.FindOptRecord(prepared);
        if (opt == null)
        {
            opt = MessageOperations.CreateOptRecord();
            prepared.Additionals.Add(opt);
        }

        DnsEdnsData edns = EnsureTypedOpt(opt);
        edns.UdpPayloadSize = ClampPayloadSize(udpPayloadSize);
        edns.ExtendedResponseCode = 0;
        edns.Version = 0;
        int flags = EdnsRecordDataCodec.ReadEffectiveFlags(edns);
        flags = flags & ~DnssecAnswerOkFlag;
        if (requestDnssec)
        {
            flags = flags | DnssecAnswerOkFlag;
        }

        edns.Flags = (DnsEdnsFlag)(flags & 57344);
        edns.FlagsValue = flags;
        ApplyRequestOptionPolicy(edns.Options);
        SynchronizeOptHeader(opt, edns);
        return prepared;
    }

    internal static DnsMessage PrepareWithoutEdns(DnsMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        DnsMessage prepared = MessageOperations.CloneMessage(message);
        RemoveOptRecord(prepared);
        return prepared;
    }

    internal static EdnsResponsePreparation PrepareResponse(
        DnsMessage request,
        DnsMessage response,
        int maximumUdpPayloadSize)
    {
        if (request == null || response == null)
        {
            throw new ArgumentNullException("message");
        }

        DnsResourceRecord? requestOpt = MessageOperations.FindOptRecord(request);
        DnsMessage prepared = MessageOperations.CloneMessage(response);
        EdnsResponsePreparation result = new EdnsResponsePreparation();
        result.Message = prepared;
        if (requestOpt == null)
        {
            RemoveOptRecord(prepared);
            return result;
        }

        int requestPayloadSize = ClampPayloadSize(ReadPayloadSize(requestOpt));
        int responderPayloadSize = ClampPayloadSize(maximumUdpPayloadSize);
        result.PayloadSizeLimit = SmallerPayloadSize(
            requestPayloadSize, responderPayloadSize);

        DnsResourceRecord? responseOpt = MessageOperations.FindOptRecord(prepared);
        if (responseOpt == null)
        {
            responseOpt = MessageOperations.CreateOptRecord();
            prepared.Additionals.Add(responseOpt);
        }

        DnsEdnsData responseEdns = EnsureTypedOpt(responseOpt);
        DnsEdnsData? requestEdns = ReadEdnsData(requestOpt);
        int requestFlags = ReadOptFlags(requestOpt);
        int responseFlags = EdnsRecordDataCodec.ReadEffectiveFlags(responseEdns);
        responseFlags = responseFlags & ~DnssecAnswerOkFlag;
        responseFlags = responseFlags | (requestFlags & DnssecAnswerOkFlag);
        responseEdns.UdpPayloadSize = responderPayloadSize;
        responseEdns.Version = 0;
        responseEdns.Flags = (DnsEdnsFlag)(responseFlags & 57344);
        responseEdns.FlagsValue = responseFlags;

        int requestVersion = ReadVersion(requestOpt);
        if (requestVersion != 0)
        {
            result.BadVersion = true;
            prepared.Answers.Clear();
            prepared.Authorities.Clear();
            RemoveNonOptAdditionalRecords(prepared);
            responseEdns.Options.Clear();
            SynchronizeOptHeader(responseOpt, responseEdns);
            MessageOperations.WriteEffectiveResponseCode(
                prepared, (int)DnsResponseCode.BadVersion);
            return result;
        }

        List<DnsEdnsOption> requestOptions = new List<DnsEdnsOption>();
        if (requestEdns != null && requestEdns.Options != null)
        {
            requestOptions = requestEdns.Options;
        }

        ApplyResponseOptionPolicy(responseEdns.Options, requestOptions);
        SynchronizeOptHeader(responseOpt, responseEdns);
        return result;
    }

    internal static EdnsResponseMetadata InspectResponse(
        DnsMessage request,
        DnsMessage? response,
        bool allowDowngrade,
        bool ednsRequired)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }

        EdnsResponseMetadata result = new EdnsResponseMetadata();
        DnsResourceRecord? requestOpt = MessageOperations.FindOptRecord(request);
        result.RequestUsedEdns = requestOpt != null;
        if (requestOpt == null || response == null)
        {
            return result;
        }

        DnsResourceRecord? responseOpt = MessageOperations.FindOptRecord(response);
        result.HasEdnsCapability = true;
        result.ResponseUsedEdns = responseOpt != null;
        result.SupportsEdns = responseOpt != null;
        if (responseOpt != null)
        {
            result.UdpPayloadSize = ClampPayloadSize(ReadPayloadSize(responseOpt));
            result.Version = ReadVersion(responseOpt);
            result.BadVersion = MessageOperations.ReadEffectiveResponseCode(response) ==
                (int)DnsResponseCode.BadVersion;
            return result;
        }

        int responseCode = MessageOperations.ReadEffectiveResponseCode(response);
        result.ShouldRetryWithoutEdns = allowDowngrade && !ednsRequired &&
            responseCode == (int)DnsResponseCode.FormatError;
        return result;
    }

    internal static void ApplyResponseMetadata(
        EdnsResponseMetadata metadata,
        DnsQueryResult queryResult,
        DnsServerState serverState,
        bool ednsDowngraded)
    {
        if (metadata == null || queryResult == null || serverState == null)
        {
            throw new ArgumentNullException("metadata");
        }

        if (metadata.HasEdnsCapability)
        {
            serverState.HasEdnsCapability = true;
            serverState.SupportsEdns = metadata.SupportsEdns;
            serverState.EdnsUdpPayloadSize = metadata.UdpPayloadSize;
        }

        if (ednsDowngraded)
        {
            queryResult.EdnsDowngraded = true;
            serverState.HasEdnsCapability = true;
            serverState.SupportsEdns = false;
            serverState.EdnsUdpPayloadSize = DnsProtocolLimits.ClassicUdpPayloadSize;
        }
    }

    internal static int ClampPayloadSize(int udpPayloadSize)
    {
        if (udpPayloadSize < DnsProtocolLimits.ClassicUdpPayloadSize)
        {
            return DnsProtocolLimits.ClassicUdpPayloadSize;
        }

        if (udpPayloadSize > DnsProtocolLimits.MaximumEdnsUdpPayloadSize)
        {
            return DnsProtocolLimits.MaximumEdnsUdpPayloadSize;
        }

        return udpPayloadSize;
    }

    static DnsEdnsData EnsureTypedOpt(DnsResourceRecord opt)
    {
        opt.UseRawData = false;
        opt.RawData = [];
        if (opt.Data == null)
        {
            opt.Data = new DnsRecordData();
        }

        if (opt.Data.Edns == null)
        {
            opt.Data.Edns = new DnsEdnsData();
        }

        if (opt.Data.Edns.Options == null)
        {
            opt.Data.Edns.Options = new List<DnsEdnsOption>();
        }

        return opt.Data.Edns;
    }

    static DnsEdnsData? ReadEdnsData(DnsResourceRecord opt)
    {
        if (opt.Data == null)
        {
            return null;
        }

        return opt.Data.Edns;
    }

    static int ReadPayloadSize(DnsResourceRecord opt)
    {
        DnsEdnsData? edns = ReadEdnsData(opt);
        if (edns != null)
        {
            return edns.UdpPayloadSize;
        }

        return DnsHeaderCodec.ResolveCode(opt.ClassValue, (int)opt.Class);
    }

    static int ReadVersion(DnsResourceRecord opt)
    {
        DnsEdnsData? edns = ReadEdnsData(opt);
        if (edns != null)
        {
            return edns.Version;
        }

        return (int)((opt.TimeToLive >> 16) & 255);
    }

    static int ReadOptFlags(DnsResourceRecord opt)
    {
        DnsEdnsData? edns = ReadEdnsData(opt);
        if (edns != null)
        {
            return EdnsRecordDataCodec.ReadEffectiveFlags(edns);
        }

        return (int)(opt.TimeToLive & 65535);
    }

    static void SynchronizeOptHeader(
        DnsResourceRecord opt,
        DnsEdnsData edns)
    {
        opt.RecordType = DnsRecordType.Opt;
        opt.RecordTypeValue = 41;
        opt.Class = (DnsClass)edns.UdpPayloadSize;
        opt.ClassValue = edns.UdpPayloadSize;
        opt.TimeToLive = ((uint)edns.ExtendedResponseCode << 24) |
            ((uint)edns.Version << 16) |
            (uint)EdnsRecordDataCodec.ReadEffectiveFlags(edns);
    }

    static int SmallerPayloadSize(int left, int right)
    {
        if (left < right)
        {
            return left;
        }

        return right;
    }

    static void ApplyRequestOptionPolicy(List<DnsEdnsOption> options)
    {
        int index = options.Count - 1;
        while (index >= 0)
        {
            DnsEdnsOption option = options[index];
            if (option == null)
            {
                options.RemoveAt(index);
                index = index - 1;
                continue;
            }

            int code = ReadOptionCode(option);
            if (code == 15 || code == 18)
            {
                options.RemoveAt(index);
                index = index - 1;
                continue;
            }

            NormalizeRequestOption(option, code);
            index = index - 1;
        }
    }

    static void NormalizeRequestOption(DnsEdnsOption option, int code)
    {
        if (code == 3)
        {
            option.NameServerIdentifier = [];
            option.RawData = [];
        }

        if (code == 8 && option.ClientSubnet != null)
        {
            option.ClientSubnet.ScopePrefixLength = 0;
        }

        if (code == 9)
        {
            option.HasExpire = false;
            option.ExpireSeconds = 0;
            option.RawData = [];
        }

        if (code == 11 && option.Keepalive != null)
        {
            option.Keepalive.HasTimeout = false;
            option.Keepalive.TimeoutUnits = 0;
        }

        if (code == 19)
        {
            if (option.ZoneVersion == null)
            {
                option.ZoneVersion = new DnsEdnsZoneVersion();
            }

            option.ZoneVersion.HasVersion = false;
            option.ZoneVersion.LabelCount = 0;
            option.ZoneVersion.Type = 0;
            option.ZoneVersion.Version = [];
            option.RawData = [];
        }
    }

    static void ApplyResponseOptionPolicy(
        List<DnsEdnsOption> responseOptions,
        List<DnsEdnsOption> requestOptions)
    {
        int index = responseOptions.Count - 1;
        while (index >= 0)
        {
            DnsEdnsOption option = responseOptions[index];
            if (option == null)
            {
                responseOptions.RemoveAt(index);
                index = index - 1;
                continue;
            }

            int code = ReadOptionCode(option);
            DnsEdnsOption? requestOption = FindRequestOption(
                requestOptions, code);
            if (!ResponseOptionAllowed(code, requestOption != null))
            {
                responseOptions.RemoveAt(index);
                index = index - 1;
                continue;
            }

            if (requestOption != null)
            {
                NormalizeResponseOption(option, requestOption, code);
            }

            index = index - 1;
        }
    }

    static bool ResponseOptionAllowed(int code, bool requested)
    {
        if (code == 5 || code == 6 || code == 7 || code == 14)
        {
            return false;
        }

        if (code == 12 || code == 15 || code == 18)
        {
            return true;
        }

        return requested;
    }

    static void NormalizeResponseOption(
        DnsEdnsOption responseOption,
        DnsEdnsOption requestOption,
        int code)
    {
        if (code == 8 && responseOption.ClientSubnet != null &&
            requestOption.ClientSubnet != null)
        {
            responseOption.ClientSubnet.AddressFamily =
                requestOption.ClientSubnet.AddressFamily;
            responseOption.ClientSubnet.SourcePrefixLength =
                requestOption.ClientSubnet.SourcePrefixLength;
            responseOption.ClientSubnet.Address = CopyBytes(
                requestOption.ClientSubnet.Address);
        }

        if (code == 10 && responseOption.Cookie != null &&
            requestOption.Cookie != null)
        {
            responseOption.Cookie.ClientCookie = CopyBytes(
                requestOption.Cookie.ClientCookie);
        }
    }

    static DnsEdnsOption? FindRequestOption(
        List<DnsEdnsOption> options,
        int code)
    {
        int index = 0;
        while (index < options.Count)
        {
            DnsEdnsOption option = options[index];
            if (option != null && ReadOptionCode(option) == code)
            {
                return option;
            }

            index = index + 1;
        }

        return null;
    }

    static int ReadOptionCode(DnsEdnsOption option)
    {
        return DnsHeaderCodec.ResolveCode(option.CodeValue, (int)option.Code);
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static void RemoveOptRecord(DnsMessage message)
    {
        int index = message.Additionals.Count - 1;
        while (index >= 0)
        {
            DnsResourceRecord record = message.Additionals[index];
            if (record != null && DnsHeaderCodec.ResolveCode(
                record.RecordTypeValue, (int)record.RecordType) == 41)
            {
                message.Additionals.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    static void RemoveNonOptAdditionalRecords(DnsMessage message)
    {
        int index = message.Additionals.Count - 1;
        while (index >= 0)
        {
            DnsResourceRecord record = message.Additionals[index];
            if (record == null || DnsHeaderCodec.ResolveCode(
                record.RecordTypeValue, (int)record.RecordType) != 41)
            {
                message.Additionals.RemoveAt(index);
            }

            index = index - 1;
        }
    }
}
