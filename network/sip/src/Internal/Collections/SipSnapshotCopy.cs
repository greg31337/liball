namespace sip;

internal static class SipSnapshotCopy
{
    internal static SipMessage? CopyMessageModel(SipMessage? source)
    {
        return CopyMessage(source);
    }

    internal static SipTransactionSnapshot? CopyTransactionSnapshot(SipTransactionSnapshot? source)
    {
        if (source == null)
        {
            return null;
        }

        SipTransactionSnapshot copy = new SipTransactionSnapshot();
        copy.Id = source.Id;
        copy.Key = CopyTransactionKey(source.Key);
        copy.Kind = source.Kind;
        copy.Role = source.Role;
        copy.State = source.State;
        copy.Request = CopyMessage(source.Request);
        copy.Response = CopyMessage(source.Response);
        copy.TimerNames = CopyStringArray(source.TimerNames);
        copy.TimerDeadlines = CopyLongArray(source.TimerDeadlines);
        copy.CreatedAtMilliseconds = source.CreatedAtMilliseconds;
        copy.UpdatedAtMilliseconds = source.UpdatedAtMilliseconds;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    internal static SipTransactionEvent? CopyTransactionEvent(SipTransactionEvent? source)
    {
        if (source == null)
        {
            return null;
        }

        SipTransactionEvent copy = new SipTransactionEvent();
        copy.Kind = source.Kind;
        copy.TransactionId = source.TransactionId;
        copy.State = source.State;
        copy.Message = CopyMessage(source.Message);
        copy.TimerName = source.TimerName;
        copy.Reason = source.Reason;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    internal static SipDialogSnapshot? CopyDialogSnapshot(SipDialogSnapshot? source)
    {
        if (source == null)
        {
            return null;
        }

        SipDialogSnapshot copy = new SipDialogSnapshot();
        copy.Id = CopyDialogId(source.Id);
        copy.Role = source.Role;
        copy.State = source.State;
        copy.LocalUri = CopyUri(source.LocalUri);
        copy.RemoteUri = CopyUri(source.RemoteUri);
        copy.LocalTarget = CopyUri(source.LocalTarget);
        copy.RemoteTarget = CopyUri(source.RemoteTarget);
        copy.RouteSet = CopyRouteArray(source.RouteSet);
        copy.LocalCSeq = source.LocalCSeq;
        copy.RemoteCSeq = source.RemoteCSeq;
        copy.Secure = source.Secure;
        copy.UsageIds = CopyStringArray(source.UsageIds);
        copy.TransactionIds = CopyStringArray(source.TransactionIds);
        copy.AuthenticationContextId = source.AuthenticationContextId;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    internal static SipDialogEvent? CopyDialogEvent(SipDialogEvent? source)
    {
        if (source == null)
        {
            return null;
        }

        SipDialogEvent copy = new SipDialogEvent();
        copy.Kind = source.Kind;
        copy.DialogId = CopyDialogId(source.DialogId);
        copy.UsageId = source.UsageId;
        copy.State = source.State;
        copy.Snapshot = CopyDialogSnapshot(source.Snapshot);
        copy.Message = CopyMessage(source.Message);
        copy.Reason = source.Reason;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    internal static SipOfferAnswer? CopyOfferAnswer(SipOfferAnswer? source)
    {
        if (source == null)
        {
            return null;
        }

        SipOfferAnswer copy = new SipOfferAnswer();
        copy.State = source.State;
        copy.LocalOffer = CopyByteArray(source.LocalOffer);
        copy.RemoteOffer = CopyByteArray(source.RemoteOffer);
        copy.LocalAnswer = CopyByteArray(source.LocalAnswer);
        copy.RemoteAnswer = CopyByteArray(source.RemoteAnswer);
        copy.ContentType = CopyMediaType(source.ContentType);
        copy.NegotiatedBody = CopyByteArray(source.NegotiatedBody);
        copy.NegotiatedContentType = CopyMediaType(source.NegotiatedContentType);
        return copy;
    }

    internal static SipInviteSnapshot? CopyInviteSnapshot(
        SipInviteSnapshot? source)
    {
        if (source == null)
        {
            return null;
        }
        SipInviteSnapshot copy = new SipInviteSnapshot();
        copy.State = source.State;
        copy.Role = source.Role;
        copy.Dialog = CopyDialogSnapshot(source.Dialog);
        copy.OfferAnswer = CopyOfferAnswer(source.OfferAnswer);
        copy.Invite = CopyMessage(source.Invite);
        copy.LastResponse = CopyMessage(source.LastResponse);
        copy.TimerNames = CopyStringArray(source.TimerNames);
        copy.TimerDeadlines = CopyLongArray(source.TimerDeadlines);
        copy.NextDeadlineMilliseconds = source.NextDeadlineMilliseconds;
        copy.CancellationPending = source.CancellationPending;
        copy.CancellationId = source.CancellationId;
        copy.AuthenticationContextId = source.AuthenticationContextId;
        copy.AuthenticationRetryCount = source.AuthenticationRetryCount;
        copy.ErrorMessage = source.ErrorMessage;
        copy.CreatedAtMilliseconds = source.CreatedAtMilliseconds;
        copy.UpdatedAtMilliseconds = source.UpdatedAtMilliseconds;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    internal static SipNameAddress? CopyNameAddressModel(SipNameAddress? source)
    {
        return CopyNameAddress(source);
    }

    internal static SipExtensionHeaderValue[] CopyExtensionHeaderValues(
        SipExtensionHeaderValue[]? source)
    {
        if (source == null)
        {
            return new SipExtensionHeaderValue[0];
        }
        SipExtensionHeaderValue[] copy =
            new SipExtensionHeaderValue[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyExtensionHeaderValue(source[index]);
            index = index + 1;
        }
        return copy;
    }

    internal static SipCapabilitySet? CopyCapabilitySet(SipCapabilitySet? source)
    {
        if (source == null)
        {
            return null;
        }
        SipCapabilitySet copy = new SipCapabilitySet();
        copy.Methods = CopyStringArray(source.Methods);
        copy.OptionTags = CopyStringArray(source.OptionTags);
        copy.MediaTypes = CopyStringArray(source.MediaTypes);
        copy.EventPackages = CopyStringArray(source.EventPackages);
        copy.HeaderNames = CopyStringArray(source.HeaderNames);
        copy.UriSchemes = CopyStringArray(source.UriSchemes);
        copy.ContentCodings = CopyStringArray(source.ContentCodings);
        return copy;
    }

    internal static SipRegistrationSnapshot? CopyRegistrationSnapshot(SipRegistrationSnapshot? source)
    {
        if (source == null)
        {
            return null;
        }

        SipRegistrationSnapshot copy = new SipRegistrationSnapshot();
        copy.State = source.State;
        copy.AddressOfRecord = CopyUri(source.AddressOfRecord);
        copy.Bindings = CopyRegistrationBindingArray(source.Bindings);
        copy.Target = CopyUri(source.Target);
        copy.OutboundProxy = CopyUri(source.OutboundProxy);
        copy.ServiceRoute = CopyRouteArray(source.ServiceRoute);
        copy.Flows = CopyFlowArray(source.Flows);
        copy.ActiveFlow = CopyFlowModel(source.ActiveFlow);
        copy.CallId = source.CallId;
        copy.CSeq = source.CSeq;
        copy.AuthenticationContextId = source.AuthenticationContextId;
        copy.CorrelationId = source.CorrelationId;
        copy.RefreshAtMilliseconds = source.RefreshAtMilliseconds;
        copy.ExpiresAtMilliseconds = source.ExpiresAtMilliseconds;
        copy.RetryAtMilliseconds = source.RetryAtMilliseconds;
        copy.AuthenticationAttemptCount = source.AuthenticationAttemptCount;
        copy.RedirectCount = source.RedirectCount;
        copy.Response = CopyMessage(source.Response);
        copy.ErrorMessage = source.ErrorMessage;
        return copy;
    }

    internal static SipRegistrationEvent? CopyRegistrationEvent(
        SipRegistrationEvent? source)
    {
        if (source == null)
        {
            return null;
        }

        SipRegistrationEvent copy = new SipRegistrationEvent();
        copy.State = source.State;
        copy.Snapshot = CopyRegistrationSnapshot(source.Snapshot);
        copy.Message = CopyMessage(source.Message);
        copy.Reason = source.Reason;
        copy.CorrelationId = source.CorrelationId;
        copy.Version = source.Version;
        copy.DocumentState = source.DocumentState;
        copy.RegistrationState = source.RegistrationState;
        copy.ContactState = source.ContactState;
        copy.Binding = CopyRegistrationBinding(source.Binding);
        copy.FlowChanged = source.FlowChanged;
        copy.PushChanged = source.PushChanged;
        return copy;
    }

    static SipTransactionKey? CopyTransactionKey(SipTransactionKey? source)
    {
        if (source == null)
        {
            return null;
        }

        SipTransactionKey copy = new SipTransactionKey();
        copy.Branch = source.Branch;
        copy.SentBy = source.SentBy;
        copy.Method = source.Method;
        copy.MethodToken = source.MethodToken;
        copy.CallId = source.CallId;
        copy.CSeq = source.CSeq;
        copy.Role = source.Role;
        if (source.LegacyRequestUri != null)
        {
            copy.LegacyRequestUri = SipUriModelCopy.CreateNormalizedCopy(source.LegacyRequestUri);
        }
        copy.LegacyFrom = CopyTransactionAddress(source.LegacyFrom);
        copy.LegacyTo = CopyTransactionAddress(source.LegacyTo);
        if (source.LegacyTopVia != null)
        {
            copy.LegacyTopVia = CopyViaValue(source.LegacyTopVia);
        }
        return copy;
    }

    static SipNameAddress? CopyTransactionAddress(SipNameAddress? source)
    {
        SipNameAddress? copy = CopyNameAddress(source);
        if (copy != null && source != null && source.Uri != null)
        {
            copy.Uri = SipUriModelCopy.CreateNormalizedCopy(source.Uri);
        }
        return copy;
    }

    static SipDialogId? CopyDialogId(SipDialogId? source)
    {
        if (source == null)
        {
            return null;
        }

        SipDialogId copy = new SipDialogId();
        copy.CallId = source.CallId;
        copy.LocalTag = source.LocalTag;
        copy.RemoteTag = source.RemoteTag;
        return copy;
    }

    static SipMessage? CopyMessage(SipMessage? source)
    {
        if (source == null)
        {
            return null;
        }

        SipMessage copy = new SipMessage();
        copy.Kind = source.Kind;
        copy.Method = source.Method;
        copy.MethodToken = source.MethodToken;
        copy.RequestUri = CopyUri(source.RequestUri);
        copy.StatusClass = source.StatusClass;
        copy.StatusCode = source.StatusCode;
        copy.ReasonPhrase = source.ReasonPhrase;
        copy.ProtocolVersion = source.ProtocolVersion;
        copy.Headers = CopyHeaderArray(source.Headers);
        copy.ViaValues = CopyViaArray(source.ViaValues);
        copy.RouteValues = CopyRouteArray(source.RouteValues);
        copy.RecordRouteValues = CopyRouteArray(source.RecordRouteValues);
        copy.ContactValues = CopyContactArray(source.ContactValues);
        copy.From = CopyNameAddress(source.From);
        copy.To = CopyNameAddress(source.To);
        copy.CSeq = CopyCSeq(source.CSeq);
        copy.ContentType = CopyMediaType(source.ContentType);
        copy.ContentDisposition = CopyContentDisposition(source.ContentDisposition);
        copy.Body = CopyByteArray(source.Body);
        copy.OriginalBytes = CopyByteArray(source.OriginalBytes);
        return copy;
    }

    static SipUri? CopyUri(SipUri? source)
    {
        if (source == null)
        {
            return null;
        }

        SipUri copy = new SipUri();
        copy.Scheme = source.Scheme;
        copy.SchemeToken = source.SchemeToken;
        copy.User = source.User;
        copy.Password = source.Password;
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Parameters = CopyParameterArray(source.Parameters);
        copy.Headers = CopyUriHeaderArray(source.Headers);
        copy.OpaquePart = source.OpaquePart;
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipNameAddress? CopyNameAddress(SipNameAddress? source)
    {
        if (source == null)
        {
            return null;
        }

        SipNameAddress copy = new SipNameAddress();
        copy.DisplayName = source.DisplayName;
        copy.Uri = CopyUri(source.Uri);
        copy.Parameters = CopyParameterArray(source.Parameters);
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipExtensionHeaderValue CopyExtensionHeaderValue(
        SipExtensionHeaderValue source)
    {
        SipExtensionHeaderValue copy = new SipExtensionHeaderValue();
        copy.Token = source.Token;
        copy.SecondaryToken = source.SecondaryToken;
        copy.TertiaryToken = source.TertiaryToken;
        copy.Number = source.Number;
        copy.SecondNumber = source.SecondNumber;
        copy.Method = source.Method;
        copy.MethodToken = source.MethodToken;
        copy.Flag = source.Flag;
        copy.HasFlag = source.HasFlag;
        copy.Uri = CopyUri(source.Uri);
        copy.Address = CopyNameAddress(source.Address);
        copy.Parameters = CopyParameterArray(source.Parameters);
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipCSeqValue? CopyCSeq(SipCSeqValue? source)
    {
        if (source == null)
        {
            return null;
        }

        SipCSeqValue copy = new SipCSeqValue();
        copy.SequenceNumber = source.SequenceNumber;
        copy.Method = source.Method;
        copy.MethodToken = source.MethodToken;
        return copy;
    }

    static SipMediaType? CopyMediaType(SipMediaType? source)
    {
        if (source == null)
        {
            return null;
        }

        SipMediaType copy = new SipMediaType();
        copy.Type = source.Type;
        copy.Subtype = source.Subtype;
        copy.Parameters = CopyParameterArray(source.Parameters);
        copy.QualityThousandths = source.QualityThousandths;
        copy.OriginalText = source.OriginalText;
        return copy;
    }

    static SipContentDisposition? CopyContentDisposition(SipContentDisposition? source)
    {
        if (source == null)
        {
            return null;
        }

        SipContentDisposition copy = new SipContentDisposition();
        copy.Disposition = source.Disposition;
        copy.DispositionToken = source.DispositionToken;
        copy.Parameters = CopyParameterArray(source.Parameters);
        return copy;
    }

    static SipRegistrationBinding? CopyRegistrationBinding(SipRegistrationBinding? source)
    {
        if (source == null)
        {
            return null;
        }

        SipRegistrationBinding copy = new SipRegistrationBinding();
        copy.AddressOfRecord = CopyUri(source.AddressOfRecord);
        copy.Contact = CopyContact(source.Contact);
        copy.CallId = source.CallId;
        copy.CSeq = source.CSeq;
        copy.ExpiresAtMilliseconds = source.ExpiresAtMilliseconds;
        copy.QualityThousandths = source.QualityThousandths;
        copy.ReceivedAddress = CopyEndpointAddress(source.ReceivedAddress);
        copy.Path = CopyRouteArray(source.Path);
        copy.ServiceRoute = CopyRouteArray(source.ServiceRoute);
        copy.Flow = CopyFlowModel(source.Flow);
        copy.InstanceId = source.InstanceId;
        copy.RegistrationId = source.RegistrationId;
        return copy;
    }

    internal static SipFlow? CopyFlowModel(SipFlow? source)
    {
        if (source == null)
        {
            return null;
        }

        SipFlow copy = new SipFlow();
        copy.Id = source.Id;
        copy.InstanceId = source.InstanceId;
        copy.RegistrationId = source.RegistrationId;
        copy.Connection = CopyConnectionInfo(source.Connection);
        copy.Path = CopyRouteArray(source.Path);
        copy.Primary = source.Primary;
        copy.Failed = source.Failed;
        copy.FlowTimerSeconds = source.FlowTimerSeconds;
        copy.NextKeepAliveAtMilliseconds = source.NextKeepAliveAtMilliseconds;
        copy.KeepAliveResponseDeadlineMilliseconds =
            source.KeepAliveResponseDeadlineMilliseconds;
        copy.RetryAtMilliseconds = source.RetryAtMilliseconds;
        copy.ConsecutiveFailures = source.ConsecutiveFailures;
        copy.ExpiresAtMilliseconds = source.ExpiresAtMilliseconds;
        return copy;
    }

    internal static SipFlow[] CopyFlowArray(SipFlow[]? source)
    {
        if (source == null)
        {
            return new SipFlow[0];
        }
        SipFlow[] copy = new SipFlow[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipFlow? value = CopyFlowModel(source[index]);
            copy[index] = value == null ? new SipFlow() : value;
            index = index + 1;
        }
        return copy;
    }

    static SipConnectionInfo? CopyConnectionInfo(SipConnectionInfo? source)
    {
        if (source == null)
        {
            return null;
        }

        SipConnectionInfo copy = new SipConnectionInfo();
        copy.Id = source.Id;
        copy.State = source.State;
        copy.Transport = source.Transport;
        copy.LocalAddress = CopyEndpointAddress(source.LocalAddress);
        copy.RemoteAddress = CopyEndpointAddress(source.RemoteAddress);
        copy.Secure = source.Secure;
        copy.PeerIdentity = source.PeerIdentity;
        copy.CertificateIdentity =
            SipCertificateIdentityCopy.Copy(source.CertificateIdentity);
        return copy;
    }

    static SipEndpointAddress? CopyEndpointAddress(SipEndpointAddress? source)
    {
        if (source == null)
        {
            return null;
        }

        SipEndpointAddress copy = new SipEndpointAddress();
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Transport = source.Transport;
        copy.Secure = source.Secure;
        return copy;
    }

    static SipHeader[] CopyHeaderArray(SipHeader[]? source)
    {
        if (source == null)
        {
            return new SipHeader[0];
        }

        SipHeader[] copy = new SipHeader[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipHeader item = new SipHeader();
            if (source[index] != null)
            {
                item.Name = source[index].Name;
                item.Value = source[index].Value;
                item.OriginalBytes = CopyByteArray(source[index].OriginalBytes);
            }

            copy[index] = item;
            index = index + 1;
        }

        return copy;
    }

    static SipViaValue[] CopyViaArray(SipViaValue[]? source)
    {
        if (source == null)
        {
            return new SipViaValue[0];
        }

        SipViaValue[] copy = new SipViaValue[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipViaValue item = new SipViaValue();
            if (source[index] != null)
            {
                item.ProtocolName = source[index].ProtocolName;
                item.ProtocolVersion = source[index].ProtocolVersion;
                item.Transport = source[index].Transport;
                item.Host = source[index].Host;
                item.Port = source[index].Port;
                item.Parameters = CopyParameterArray(source[index].Parameters);
                item.Branch = source[index].Branch;
                item.Received = source[index].Received;
                item.RPort = source[index].RPort;
                item.HasRPort = source[index].HasRPort;
            }

            copy[index] = item;
            index = index + 1;
        }

        return copy;
    }

    static SipViaValue CopyViaValue(SipViaValue source)
    {
        SipViaValue copy = new SipViaValue();
        copy.ProtocolName = source.ProtocolName;
        copy.ProtocolVersion = source.ProtocolVersion;
        copy.Transport = source.Transport;
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Parameters = CopyParameterArray(source.Parameters);
        copy.Branch = source.Branch;
        copy.Received = source.Received;
        copy.RPort = source.RPort;
        copy.HasRPort = source.HasRPort;
        return copy;
    }

    static SipRouteValue[] CopyRouteArray(SipRouteValue[]? source)
    {
        if (source == null)
        {
            return new SipRouteValue[0];
        }

        SipRouteValue[] copy = new SipRouteValue[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipRouteValue item = new SipRouteValue();
            if (source[index] != null)
            {
                item.Address = CopyNameAddress(source[index].Address);
                item.LooseRouting = source[index].LooseRouting;
            }

            copy[index] = item;
            index = index + 1;
        }

        return copy;
    }

    static SipContactValue? CopyContact(SipContactValue? source)
    {
        if (source == null)
        {
            return null;
        }

        SipContactValue copy = new SipContactValue();
        copy.Wildcard = source.Wildcard;
        copy.Address = CopyNameAddress(source.Address);
        copy.ExpiresSeconds = source.ExpiresSeconds;
        copy.QualityThousandths = source.QualityThousandths;
        copy.InstanceId = source.InstanceId;
        copy.RegistrationId = source.RegistrationId;
        copy.Outbound = source.Outbound;
        copy.PublicGruu = source.PublicGruu;
        copy.TemporaryGruu = source.TemporaryGruu;
        copy.PushProvider = source.PushProvider;
        copy.PushParameter = source.PushParameter;
        copy.PushResourceId = source.PushResourceId;
        copy.PushRegistrationReference = source.PushRegistrationReference;
        return copy;
    }

    internal static SipContactValue[] CopyContactArray(SipContactValue[]? source)
    {
        if (source == null)
        {
            return new SipContactValue[0];
        }

        SipContactValue[] copy = new SipContactValue[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipContactValue? item = CopyContact(source[index]);
            copy[index] = item == null ? new SipContactValue() : item;
            index = index + 1;
        }

        return copy;
    }

    static SipUriParameter[] CopyParameterArray(SipUriParameter[]? source)
    {
        if (source == null)
        {
            return new SipUriParameter[0];
        }

        SipUriParameter[] copy = new SipUriParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriParameter item = new SipUriParameter();
            if (source[index] != null)
            {
                item.Name = source[index].Name;
                item.Value = source[index].Value;
                item.HasValue = source[index].HasValue;
                item.OriginalText = source[index].OriginalText;
                item.Quoted = source[index].Quoted;
            }

            copy[index] = item;
            index = index + 1;
        }

        return copy;
    }

    static SipUriHeader[] CopyUriHeaderArray(SipUriHeader[]? source)
    {
        if (source == null)
        {
            return new SipUriHeader[0];
        }

        SipUriHeader[] copy = new SipUriHeader[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriHeader item = new SipUriHeader();
            if (source[index] != null)
            {
                item.Name = source[index].Name;
                item.Value = source[index].Value;
                item.OriginalText = source[index].OriginalText;
            }

            copy[index] = item;
            index = index + 1;
        }

        return copy;
    }

    internal static SipRegistrationBinding[] CopyRegistrationBindingArray(
        SipRegistrationBinding[]? source)
    {
        if (source == null)
        {
            return new SipRegistrationBinding[0];
        }

        SipRegistrationBinding[] copy = new SipRegistrationBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipRegistrationBinding? item = CopyRegistrationBinding(source[index]);
            copy[index] = item == null ? new SipRegistrationBinding() : item;
            index = index + 1;
        }

        return copy;
    }

    static string[] CopyStringArray(string[]? source)
    {
        if (source == null)
        {
            return new string[0];
        }

        string[] copy = new string[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static long[] CopyLongArray(long[]? source)
    {
        if (source == null)
        {
            return new long[0];
        }

        long[] copy = new long[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static byte[] CopyByteArray(byte[]? source)
    {
        if (source == null)
        {
            return new byte[0];
        }

        byte[] copy = new byte[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }
}
