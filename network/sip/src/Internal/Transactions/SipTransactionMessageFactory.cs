namespace sip;

internal static class SipTransactionMessageFactory
{
    internal static SipMessage CreateFailureAck(SipMessage? request, SipMessage? response)
    {
        SipMessage ack = CopyRequiredMessage(request);
        SipMessage copiedResponse = CopyRequiredMessage(response);
        if (ack.Kind != SipMessageKind.Request || ack.Method != SipMethod.Invite
            || ack.RequestUri == null || ack.ViaValues.Length == 0 || ack.CSeq == null
            || ack.From == null || copiedResponse.To == null)
        {
            throw new System.ArgumentException(
                "Complete INVITE request and failure response models are required.", "request");
        }

        ack.Method = SipMethod.Ack;
        ack.MethodToken = SipMethods.Ack;
        ack.ViaValues = new SipViaValue[] { ack.ViaValues[0] };
        ack.To = copiedResponse.To;
        ack.CSeq.Method = SipMethod.Ack;
        ack.CSeq.MethodToken = SipMethods.Ack;
        ack.RecordRouteValues = new SipRouteValue[0];
        ack.ContactValues = new SipContactValue[0];
        ack.ContentType = null;
        ack.ContentDisposition = null;
        ack.Body = new byte[0];
        ack.OriginalBytes = new byte[0];
        ack.Headers = CreateAckHeaders(ack, ack);
        return ack;
    }

    internal static SipMessage CreateTryingResponse(SipMessage? request)
    {
        SipMessage trying = CopyRequiredMessage(request);
        if (trying.Kind != SipMessageKind.Request || trying.Method != SipMethod.Invite
            || trying.ViaValues.Length == 0 || trying.CSeq == null || trying.From == null
            || trying.To == null)
        {
            throw new System.ArgumentException("A complete INVITE request model is required.",
                "request");
        }

        trying.Kind = SipMessageKind.Response;
        trying.Method = SipMethod.Unknown;
        trying.MethodToken = null;
        trying.RequestUri = null;
        trying.StatusClass = SipStatusClass.Provisional;
        trying.StatusCode = SipStatusCodes.Trying;
        trying.ReasonPhrase = SipReasonPhrases.Trying;
        trying.RouteValues = new SipRouteValue[0];
        trying.RecordRouteValues = new SipRouteValue[0];
        trying.ContactValues = new SipContactValue[0];
        trying.ContentType = null;
        trying.ContentDisposition = null;
        trying.Body = new byte[0];
        trying.OriginalBytes = new byte[0];
        trying.Headers = CreateTryingHeaders(trying);
        return trying;
    }

    internal static SipMessage CreateCancel(SipMessage? request)
    {
        SipMessage cancel = CopyRequiredMessage(request);
        if (cancel.Kind != SipMessageKind.Request || cancel.Method != SipMethod.Invite
            || cancel.RequestUri == null || cancel.ViaValues.Length == 0
            || cancel.CSeq == null || cancel.From == null || cancel.To == null)
        {
            throw new System.ArgumentException("A complete INVITE request model is required.",
                "request");
        }

        cancel.Method = SipMethod.Cancel;
        cancel.MethodToken = SipMethods.Cancel;
        cancel.ViaValues = new SipViaValue[] { cancel.ViaValues[0] };
        cancel.CSeq.Method = SipMethod.Cancel;
        cancel.CSeq.MethodToken = SipMethods.Cancel;
        cancel.RecordRouteValues = new SipRouteValue[0];
        cancel.ContactValues = new SipContactValue[0];
        cancel.ContentType = null;
        cancel.ContentDisposition = null;
        cancel.Body = new byte[0];
        cancel.OriginalBytes = new byte[0];
        cancel.Headers = CreateRelatedRequestHeaders(cancel, SipMethods.Cancel);
        return cancel;
    }

    internal static SipMessage CreateCancelOk(SipMessage? cancel, string? toTag)
    {
        return CreateResponse(cancel, SipStatusCodes.Ok, SipReasonPhrases.Ok, toTag);
    }

    internal static SipMessage CreateInviteTerminated(SipMessage? invite, string? toTag)
    {
        return CreateResponse(invite, SipStatusCodes.RequestTerminated,
            SipReasonPhrases.RequestTerminated, toTag);
    }

    internal static SipMessage CreateRequestResponse(
        SipMessage? request, int statusCode, string reasonPhrase, string? toTag)
    {
        return CreateResponse(request, statusCode, reasonPhrase, toTag);
    }

    static SipHeader[] CreateAckHeaders(SipMessage request, SipMessage ack)
    {
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        headers.Add(CreateViaHeader(ack.ViaValues[0]));
        AddFirstCopiedHeader(headers, request.Headers, SipHeaderNames.MaxForwards, null);
        headers.Add(CreateAddressHeader(SipHeaderNames.From, ack.From));
        headers.Add(CreateAddressHeader(SipHeaderNames.To, ack.To));
        AddFirstCopiedHeader(headers, request.Headers, SipHeaderNames.CallId,
            SipHeaderNames.CompactCallId);
        SipHeader cseq = new SipHeader();
        cseq.Name = SipHeaderNames.CSeq;
        cseq.Value = ack.CSeq == null ? null : ack.CSeq.SequenceNumber.ToString(
            System.Globalization.CultureInfo.InvariantCulture) + " " + SipMethods.Ack;
        headers.Add(cseq);
        AddAllCopiedHeaders(headers, request.Headers, SipHeaderNames.Route, null);
        headers.Add(CreateContentLengthHeader());
        return headers.ToArray();
    }

    static SipHeader[] CreateRelatedRequestHeaders(SipMessage request, string method)
    {
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        headers.Add(CreateViaHeader(request.ViaValues[0]));
        AddFirstCopiedHeader(headers, request.Headers, SipHeaderNames.MaxForwards, null);
        headers.Add(CreateAddressHeader(SipHeaderNames.From, request.From));
        headers.Add(CreateAddressHeader(SipHeaderNames.To, request.To));
        AddFirstCopiedHeader(headers, request.Headers, SipHeaderNames.CallId,
            SipHeaderNames.CompactCallId);
        SipHeader cseq = new SipHeader();
        cseq.Name = SipHeaderNames.CSeq;
        cseq.Value = request.CSeq == null ? null : request.CSeq.SequenceNumber.ToString(
            System.Globalization.CultureInfo.InvariantCulture) + " " + method;
        headers.Add(cseq);
        AddAllCopiedHeaders(headers, request.Headers, SipHeaderNames.Route, null);
        headers.Add(CreateContentLengthHeader());
        return headers.ToArray();
    }

    static SipMessage CreateResponse(SipMessage? request, int statusCode, string reasonPhrase,
        string? toTag)
    {
        SipMessage response = CopyRequiredMessage(request);
        if (response.Kind != SipMessageKind.Request || response.ViaValues.Length == 0
            || response.CSeq == null || response.From == null || response.To == null
            || toTag == null || !SipAscii.IsToken(toTag))
        {
            throw new System.ArgumentException("A complete request and To tag are required.",
                "request");
        }

        response.Kind = SipMessageKind.Response;
        response.Method = SipMethod.Unknown;
        response.MethodToken = null;
        response.RequestUri = null;
        response.StatusCode = statusCode;
        response.StatusClass = ReadStatusClass(statusCode);
        response.ReasonPhrase = reasonPhrase;
        SetTag(response.To, toTag);
        response.RouteValues = new SipRouteValue[0];
        response.RecordRouteValues = new SipRouteValue[0];
        response.ContactValues = new SipContactValue[0];
        response.ContentType = null;
        response.ContentDisposition = null;
        response.Body = new byte[0];
        response.OriginalBytes = new byte[0];
        response.Headers = CreateResponseHeaders(response);
        return response;
    }

    static SipHeader[] CreateResponseHeaders(SipMessage response)
    {
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        int viaIndex = 0;
        while (viaIndex < response.ViaValues.Length)
        {
            headers.Add(CreateViaHeader(response.ViaValues[viaIndex]));
            viaIndex = viaIndex + 1;
        }
        headers.Add(CreateAddressHeader(SipHeaderNames.From, response.From));
        headers.Add(CreateAddressHeader(SipHeaderNames.To, response.To));
        AddFirstCopiedHeader(headers, response.Headers, SipHeaderNames.CallId,
            SipHeaderNames.CompactCallId);
        SipHeader cseq = new SipHeader();
        cseq.Name = SipHeaderNames.CSeq;
        cseq.Value = response.CSeq == null ? null : response.CSeq.SequenceNumber.ToString(
            System.Globalization.CultureInfo.InvariantCulture) + " "
            + response.CSeq.MethodToken;
        headers.Add(cseq);
        headers.Add(CreateContentLengthHeader());
        return headers.ToArray();
    }

    static SipStatusClass ReadStatusClass(int statusCode)
    {
        if (statusCode < 200)
        {
            return SipStatusClass.Provisional;
        }
        if (statusCode < 300)
        {
            return SipStatusClass.Successful;
        }
        if (statusCode < 400)
        {
            return SipStatusClass.Redirection;
        }
        if (statusCode < 500)
        {
            return SipStatusClass.ClientError;
        }
        if (statusCode < 600)
        {
            return SipStatusClass.ServerError;
        }
        return SipStatusClass.GlobalFailure;
    }

    static void SetTag(SipNameAddress address, string tag)
    {
        int index = 0;
        while (index < address.Parameters.Length)
        {
            SipUriParameter parameter = address.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "tag"))
            {
                parameter.Value = tag;
                parameter.HasValue = true;
                parameter.Quoted = false;
                return;
            }
            index = index + 1;
        }

        SipUriParameter added = new SipUriParameter();
        added.Name = "tag";
        added.Value = tag;
        added.HasValue = true;
        SipUriParameter[] parameters = new SipUriParameter[address.Parameters.Length + 1];
        System.Array.Copy(address.Parameters, parameters, address.Parameters.Length);
        parameters[parameters.Length - 1] = added;
        address.Parameters = parameters;
    }

    static SipHeader[] CreateTryingHeaders(SipMessage request)
    {
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        AddAllCopiedHeaders(headers, request.Headers, SipHeaderNames.Via,
            SipHeaderNames.CompactVia);
        AddFirstCopiedHeader(headers, request.Headers, SipHeaderNames.From,
            SipHeaderNames.CompactFrom);
        AddFirstCopiedHeader(headers, request.Headers, SipHeaderNames.To,
            SipHeaderNames.CompactTo);
        AddFirstCopiedHeader(headers, request.Headers, SipHeaderNames.CallId,
            SipHeaderNames.CompactCallId);
        AddFirstCopiedHeader(headers, request.Headers, SipHeaderNames.CSeq, null);
        headers.Add(CreateContentLengthHeader());
        return headers.ToArray();
    }

    static SipHeader CreateViaHeader(SipViaValue via)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        SipWriteResult formatted = SipViaCodec.FormatViaHeaderValues(
            new SipViaValue[] { via }, options);
        if (!formatted.Success)
        {
            throw new System.InvalidOperationException("The ACK Via header cannot be formatted.");
        }
        string text = System.Text.Encoding.UTF8.GetString(formatted.Bytes);
        int colon = text.IndexOf(':');
        SipHeader header = new SipHeader();
        header.Name = SipHeaderNames.Via;
        header.Value = colon < 0 ? null : text.Substring(colon + 1).Trim();
        return header;
    }

    static SipHeader CreateAddressHeader(string name, SipNameAddress? address)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = SipHeaderPrimitives.FormatNameAddressValue(address, options);
        return header;
    }

    static SipHeader CreateContentLengthHeader()
    {
        SipHeader contentLength = new SipHeader();
        contentLength.Name = SipHeaderNames.ContentLength;
        contentLength.Value = "0";
        return contentLength;
    }

    static void AddFirstCopiedHeader(System.Collections.Generic.List<SipHeader> destination,
        SipHeader[] source, string name, string? compactName)
    {
        int index = 0;
        while (index < source.Length)
        {
            SipHeader? header = source[index];
            if (header != null && (SipAscii.EqualsIgnoreCase(header.Name, name)
                || compactName != null && SipAscii.EqualsIgnoreCase(header.Name, compactName)))
            {
                destination.Add(CopyHeader(header));
                return;
            }
            index = index + 1;
        }
    }

    static void AddAllCopiedHeaders(System.Collections.Generic.List<SipHeader> destination,
        SipHeader[] source, string name, string? compactName)
    {
        int index = 0;
        while (index < source.Length)
        {
            SipHeader? header = source[index];
            if (header != null && (SipAscii.EqualsIgnoreCase(header.Name, name)
                || compactName != null && SipAscii.EqualsIgnoreCase(header.Name, compactName)))
            {
                destination.Add(CopyHeader(header));
            }
            index = index + 1;
        }
    }

    static SipHeader CopyHeader(SipHeader source)
    {
        SipHeader copy = new SipHeader();
        copy.Name = source.Name;
        copy.Value = source.Value;
        copy.OriginalBytes = CopyBytes(source.OriginalBytes);
        return copy;
    }

    static byte[] CopyBytes(byte[]? source)
    {
        if (source == null)
        {
            return new byte[0];
        }
        byte[] copy = new byte[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static SipMessage CopyRequiredMessage(SipMessage? source)
    {
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(source);
        if (copy == null)
        {
            throw new System.ArgumentException("A SIP message is required.", "source");
        }
        return copy;
    }
}
