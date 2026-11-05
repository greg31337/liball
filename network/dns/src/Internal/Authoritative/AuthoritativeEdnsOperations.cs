using System.Collections.Generic;

namespace dns;

internal class DnsAuthoritativeEdnsResult
{
    internal DnsMessage Response = new DnsMessage();
    internal int PayloadSizeLimit = DnsProtocolLimits.ClassicUdpPayloadSize;
    internal bool UsedEdns;
    internal bool CookieAuthenticated;
    internal bool BadVersion;
    internal bool BadCookie;
}

internal static class AuthoritativeEdnsOperations
{
    const int MaximumCookiePastAgeSeconds = 3600;
    const int MaximumCookieFutureSkewSeconds = 300;
    const int MaximumUnauthenticatedAmplificationFactor = 3;

    internal static DnsAuthoritativeEdnsResult PrepareResponse(
        DnsAuthoritativeRequestState state,
        DnsAuthoritativeResult authoritative,
        DnsMessage response)
    {
        DnsAuthoritativeEdnsResult result =
            new DnsAuthoritativeEdnsResult();
        result.Response = response;
        result.PayloadSizeLimit = Smaller(
            state.Request.Options.MaximumUdpResponseLength,
            DnsProtocolLimits.ClassicUdpPayloadSize);
        if (!state.Request.Options.EnableEdns ||
            state.RequestOpt == null || state.EdnsMalformed)
        {
            ApplyAmplificationLimit(state, result);
            return result;
        }

        DnsMessage candidate = response;
        DnsResourceRecord responseOpt = MessageOperations.CreateOptRecord();
        candidate.Additionals.Add(responseOpt);
        if (responseOpt.Data == null)
        {
            responseOpt.Data = new DnsRecordData();
        }

        if (responseOpt.Data.Edns == null)
        {
            responseOpt.Data.Edns = new DnsEdnsData();
        }

        DnsEdnsData responseEdns = responseOpt.Data.Edns;
        DnsEdnsOption? requestCookie = FindRequestCookie(state.RequestOpt);
        if (requestCookie != null && requestCookie.Cookie != null)
        {
            ApplyCookie(
                state,
                result,
                responseEdns.Options,
                requestCookie.Cookie);
        }

        if (result.BadCookie)
        {
            ClearAnswerData(authoritative, candidate);
            MessageOperations.WriteEffectiveResponseCode(
                candidate,
                (int)DnsResponseCode.BadCookie);
            AddExtendedError(
                responseEdns.Options,
                DnsExtendedErrorCode.InvalidData,
                "DNS Cookie validation failed.");
        }
        else
        {
            AddProtocolExtendedError(
                authoritative,
                responseEdns.Options);
        }

        EdnsResponsePreparation prepared = EdnsOperations.PrepareResponse(
            state.Request.Message,
            candidate,
            state.Request.Options.MaximumUdpResponseLength);
        result.Response = prepared.Message;
        result.PayloadSizeLimit = prepared.PayloadSizeLimit;
        result.UsedEdns = true;
        result.BadVersion = prepared.BadVersion;
        if (prepared.BadVersion)
        {
            result.BadCookie = false;
            result.CookieAuthenticated = false;
            ClearAnswerData(authoritative, result.Response);
            DnsResourceRecord? opt = MessageOperations.FindOptRecord(
                result.Response);
            if (opt != null && opt.Data != null && opt.Data.Edns != null)
            {
                AddExtendedError(
                    opt.Data.Edns.Options,
                    DnsExtendedErrorCode.NotSupported,
                    "The requested EDNS version is not supported.");
            }
        }

        SynchronizeCounts(result.Response);
        ApplyAmplificationLimit(state, result);
        return result;
    }

    static void ApplyCookie(
        DnsAuthoritativeRequestState state,
        DnsAuthoritativeEdnsResult result,
        List<DnsEdnsOption> responseOptions,
        DnsEdnsCookie requestCookie)
    {
        byte[] address = state.Request.ClientEndpoint.Address;
        bool addressValid = address.Length == 4 || address.Length == 16;
        bool secretValid = state.CookieSecret.Length == 16;
        uint timestamp = ReadTimestamp(state.Request.ReceivedAt);
        if (requestCookie.ServerCookie.Length > 0)
        {
            if (addressValid && secretValid &&
                requestCookie.ServerCookie.Length == 16)
            {
                result.CookieAuthenticated =
                    DnsCookieOperations.VerifyServerCookie(
                        requestCookie,
                        address,
                        state.CookieSecret,
                        timestamp,
                        MaximumCookiePastAgeSeconds,
                        MaximumCookieFutureSkewSeconds);
            }

            if (!result.CookieAuthenticated)
            {
                result.BadCookie = true;
            }
        }

        if (!addressValid || !secretValid)
        {
            return;
        }

        DnsEdnsCookie responseCookie =
            DnsCookieOperations.CreateServerCookie(
                requestCookie,
                address,
                state.CookieSecret,
                timestamp);
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = DnsEdnsOptionCode.Cookie;
        option.CodeValue = (int)DnsEdnsOptionCode.Cookie;
        option.Cookie = responseCookie;
        responseOptions.Add(option);
    }

    static DnsEdnsOption? FindRequestCookie(DnsResourceRecord opt)
    {
        if (opt.Data == null || opt.Data.Edns == null ||
            opt.Data.Edns.Options == null)
        {
            return null;
        }

        int index = 0;
        while (index < opt.Data.Edns.Options.Count)
        {
            DnsEdnsOption option = opt.Data.Edns.Options[index];
            int code = DnsHeaderCodec.ResolveCode(
                option.CodeValue,
                (int)option.Code);
            if (code == (int)DnsEdnsOptionCode.Cookie)
            {
                return option;
            }

            index = index + 1;
        }

        return null;
    }

    static void AddProtocolExtendedError(
        DnsAuthoritativeResult authoritative,
        List<DnsEdnsOption> options)
    {
        int responseCode = authoritative.ResponseCodeValue;
        if (responseCode == (int)DnsResponseCode.NoError ||
            responseCode == (int)DnsResponseCode.NameError)
        {
            return;
        }

        DnsExtendedErrorCode code = DnsExtendedErrorCode.Other;
        if (responseCode == (int)DnsResponseCode.FormatError)
        {
            code = DnsExtendedErrorCode.InvalidData;
        }
        else if (responseCode == (int)DnsResponseCode.NotImplemented)
        {
            code = DnsExtendedErrorCode.NotSupported;
        }
        else if (responseCode == (int)DnsResponseCode.Refused)
        {
            code = DnsExtendedErrorCode.NotAuthoritative;
        }

        string text = string.Empty;
        if (authoritative.Diagnostics.Count > 0)
        {
            text = authoritative.Diagnostics[0].Reason;
        }

        AddExtendedError(options, code, text);
    }

    static void AddExtendedError(
        List<DnsEdnsOption> options,
        DnsExtendedErrorCode code,
        string text)
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = DnsEdnsOptionCode.ExtendedDnsError;
        option.CodeValue = (int)DnsEdnsOptionCode.ExtendedDnsError;
        option.ExtendedError = new DnsEdnsExtendedError();
        option.ExtendedError.Code = code;
        option.ExtendedError.CodeValue = (int)code;
        option.ExtendedError.ExtraText = text;
        options.Add(option);
    }

    static void ApplyAmplificationLimit(
        DnsAuthoritativeRequestState state,
        DnsAuthoritativeEdnsResult result)
    {
        if (state.Request.Transport != DnsTransportKind.Udp ||
            !state.Request.Options.RequireCookieForAmplification ||
            result.CookieAuthenticated)
        {
            return;
        }

        int requestLength = ReadRequestLength(state.Request);
        int amplificationLimit = requestLength *
            MaximumUnauthenticatedAmplificationFactor;
        if (amplificationLimit < DnsProtocolLimits.HeaderLength)
        {
            amplificationLimit = DnsProtocolLimits.HeaderLength;
        }

        if (amplificationLimit < result.PayloadSizeLimit)
        {
            result.PayloadSizeLimit = amplificationLimit;
        }
    }

    static int ReadRequestLength(DnsAuthoritativeRequest request)
    {
        if (request.MessageBytes != null && request.MessageBytes.Length > 0)
        {
            return request.MessageBytes.Length;
        }

        DnsEncodeOptions options = new DnsEncodeOptions();
        options.MaximumMessageLength =
            DnsProtocolLimits.MaximumTcpMessageLength;
        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            request.Message,
            options);
        if (encoded.Success && encoded.BytesWritten > 0)
        {
            return encoded.BytesWritten;
        }

        return DnsProtocolLimits.HeaderLength;
    }

    static uint ReadTimestamp(DnsClockValue clock)
    {
        if (clock == null || clock.UnixTimeMilliseconds <= 0)
        {
            return 0;
        }

        long seconds = clock.UnixTimeMilliseconds / 1000;
        if (seconds > uint.MaxValue)
        {
            return uint.MaxValue;
        }

        return (uint)seconds;
    }

    static void ClearAnswerData(
        DnsAuthoritativeResult authoritative,
        DnsMessage response)
    {
        response.Answers.Clear();
        response.Authorities.Clear();
        int index = response.Additionals.Count - 1;
        while (index >= 0)
        {
            DnsResourceRecord record = response.Additionals[index];
            int typeValue = DnsHeaderCodec.ResolveCode(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (typeValue != (int)DnsRecordType.Opt)
            {
                response.Additionals.RemoveAt(index);
            }

            index = index - 1;
        }

        authoritative.AnswerRecordSets.Clear();
        authoritative.AuthorityRecordSets.Clear();
        authoritative.AdditionalRecordSets.Clear();
        authoritative.Authoritative = false;
        response.Header.Flags = response.Header.Flags &
            ~DnsHeaderFlag.AuthoritativeAnswer &
            ~DnsHeaderFlag.AuthenticData;
        response.Header.FlagsValue = response.Header.FlagsValue &
            ~(int)DnsHeaderFlag.AuthoritativeAnswer &
            ~(int)DnsHeaderFlag.AuthenticData;
        SynchronizeCounts(response);
    }

    static void SynchronizeCounts(DnsMessage response)
    {
        response.Header.AnswerCount = response.Answers.Count;
        response.Header.AuthorityCount = response.Authorities.Count;
        response.Header.AdditionalCount = response.Additionals.Count;
    }

    static int Smaller(int left, int right)
    {
        if (left < right)
        {
            return left;
        }

        return right;
    }
}
