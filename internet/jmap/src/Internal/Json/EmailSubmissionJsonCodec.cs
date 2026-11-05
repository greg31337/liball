using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class EmailSubmissionJsonCodec
{
    internal JmapJsonReader Reader;
    internal StandardMethodJsonCodec Standard;
    internal SessionJsonCodec Values;
    internal ProtocolJsonCodec Optional;

    internal EmailSubmissionJsonCodec(JmapJsonReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        Reader = reader;
        Standard = new StandardMethodJsonCodec(reader);
        Values = new SessionJsonCodec(reader);
        Optional = new ProtocolJsonCodec(reader);
    }

    internal EmailSubmission DecodeSubmission(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailSubmission result = new EmailSubmission();
        result.Id = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Id), path + "/id");
        result.IdentityId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.IdentityId), path + "/identityId");
        result.EmailId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.EmailId), path + "/emailId");
        result.ThreadId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.ThreadId), path + "/threadId");
        JsonElement envelope = Values.GetMember(value, JmapProperties.Envelope);
        if (envelope.ValueKind != JsonValueKind.Undefined && envelope.ValueKind != JsonValueKind.Null)
        {
            result.Envelope = DecodeEnvelope(envelope, path + "/envelope");
        }

        result.SendAt = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.SendAt), path + "/sendAt");
        JsonElement undoStatus = Values.GetMember(value, JmapProperties.UndoStatus);
        if (undoStatus.ValueKind != JsonValueKind.Undefined)
        {
            result.UndoStatus = DecodeUndoStatus(Values.ReadRequiredString(undoStatus, path + "/undoStatus"), path + "/undoStatus");
            result.HasUndoStatus = true;
        }

        result.DeliveryStatus = DecodeOptionalDeliveryMap(Values.GetMember(value, JmapProperties.DeliveryStatus), path + "/deliveryStatus");
        result.DsnBlobIds = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.DsnBlobIds), path + "/dsnBlobIds");
        result.MdnBlobIds = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.MdnBlobIds), path + "/mdnBlobIds");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, SubmissionNames());
        return result;
    }

    internal void EncodeSubmission(EmailSubmission value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An EmailSubmission cannot be null.", path);
        }

        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.Id, value.Id, false);
        writer.WriteStringMember(JmapProperties.IdentityId, value.IdentityId, false);
        writer.WriteStringMember(JmapProperties.EmailId, value.EmailId, false);
        writer.WriteStringMember(JmapProperties.ThreadId, value.ThreadId, false);
        if (value.Envelope != null)
        {
            writer.WriteMemberName(JmapProperties.Envelope);
            EncodeEnvelope(value.Envelope, writer, path + "/envelope");
        }

        writer.WriteStringMember(JmapProperties.SendAt, value.SendAt, false);
        if (value.HasUndoStatus)
        {
            writer.WriteStringMember(JmapProperties.UndoStatus, EncodeUndoStatus(value.UndoStatus, path + "/undoStatus"), false);
        }

        WriteOptionalDeliveryMap(JmapProperties.DeliveryStatus, value.DeliveryStatus, writer, path + "/deliveryStatus");
        Optional.WriteOptionalStringArray(JmapProperties.DsnBlobIds, value.DsnBlobIds, writer, path + "/dsnBlobIds");
        Optional.WriteOptionalStringArray(JmapProperties.MdnBlobIds, value.MdnBlobIds, writer, path + "/mdnBlobIds");
        Values.WriteUnknownMembers(value.UnknownProperties, SubmissionNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void EncodeSubmissionCreate(EmailSubmission value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An EmailSubmission create value cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.IdentityId, value.IdentityId, path + "/identityId");
        Values.WriteRequiredStringMember(writer, JmapProperties.EmailId, value.EmailId, path + "/emailId");
        if (value.Envelope != null)
        {
            writer.WriteMemberName(JmapProperties.Envelope);
            EncodeEnvelope(value.Envelope, writer, path + "/envelope");
        }

        writer.WriteStringMember(JmapProperties.SendAt, value.SendAt, false);
        Values.WriteUnknownMembers(value.UnknownProperties, SubmissionNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailSubmissionEnvelope DecodeEnvelope(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailSubmissionEnvelope result = new EmailSubmissionEnvelope();
        result.MailFrom = DecodeSubmissionAddress(Values.GetMember(value, JmapProperties.MailFrom), path + "/mailFrom");
        result.RcptTo = DecodeSubmissionAddressList(Values.GetMember(value, JmapProperties.RcptTo), path + "/rcptTo");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.MailFrom, JmapProperties.RcptTo });
        return result;
    }

    internal void EncodeEnvelope(EmailSubmissionEnvelope value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An EmailSubmission envelope cannot be null.", path);
        }

        if (value.MailFrom == null)
        {
            throw Standard.Validation("An EmailSubmission envelope requires mailFrom.", path + "/mailFrom");
        }

        writer.WriteObjectStart();
        writer.WriteMemberName(JmapProperties.MailFrom);
        EncodeSubmissionAddress(value.MailFrom, writer, path + "/mailFrom");
        writer.WriteMemberName(JmapProperties.RcptTo);
        EncodeSubmissionAddressList(value.RcptTo, writer, path + "/rcptTo");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.MailFrom, JmapProperties.RcptTo }, writer);
        writer.WriteObjectEnd();
    }

    internal EmailSubmissionAddress DecodeSubmissionAddress(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailSubmissionAddress result = new EmailSubmissionAddress();
        result.Email = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Email), path + "/email");
        result.Parameters = DecodeOptionalParameterMap(Values.GetMember(value, JmapProperties.Parameters), path + "/parameters");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.Email, JmapProperties.Parameters });
        return result;
    }

    internal void EncodeSubmissionAddress(EmailSubmissionAddress value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An EmailSubmission address cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Email, value.Email, path + "/email");
        WriteOptionalParameterMap(JmapProperties.Parameters, value.Parameters, writer, path + "/parameters");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.Email, JmapProperties.Parameters }, writer);
        writer.WriteObjectEnd();
    }

    internal EmailDeliveryStatus DecodeDeliveryStatus(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailDeliveryStatus result = new EmailDeliveryStatus();
        result.SmtpReply = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.SmtpReply), path + "/smtpReply");
        result.Delivered = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Delivered), path + "/delivered");
        result.Displayed = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Displayed), path + "/displayed");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, DeliveryStatusNames());
        return result;
    }

    internal void EncodeDeliveryStatus(EmailDeliveryStatus value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email delivery status cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.SmtpReply, value.SmtpReply, path + "/smtpReply");
        Values.WriteRequiredStringMember(writer, JmapProperties.Delivered, value.Delivered, path + "/delivered");
        Values.WriteRequiredStringMember(writer, JmapProperties.Displayed, value.Displayed, path + "/displayed");
        Values.WriteUnknownMembers(value.UnknownProperties, DeliveryStatusNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailSubmissionFilter DecodeSubmissionFilter(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailSubmissionFilter result = new EmailSubmissionFilter();
        JsonElement operatorValue = Values.GetMember(value, JmapProperties.Operator);
        if (operatorValue.ValueKind != JsonValueKind.Undefined)
        {
            result.Operator = Standard.DecodeFilterOperator(Values.ReadRequiredString(operatorValue, path + "/operator"), path + "/operator");
            result.HasOperator = true;
        }

        JsonElement conditions = Values.GetMember(value, JmapProperties.Conditions);
        if (conditions.ValueKind != JsonValueKind.Undefined && conditions.ValueKind != JsonValueKind.Null)
        {
            if (conditions.ValueKind != JsonValueKind.Array)
            {
                throw new JmapProtocolException("Filter conditions must be an array.", path + "/conditions", Values.CloneContext(conditions));
            }

            result.Conditions = new List<EmailSubmissionFilter>();
            int index = 0;
            foreach (JsonElement condition in conditions.EnumerateArray())
            {
                result.Conditions.Add(DecodeSubmissionFilter(condition, path + "/conditions/" + index));
                index = index + 1;
            }
        }

        result.IdentityIds = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.IdentityIds), path + "/identityIds");
        result.EmailIds = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.EmailIds), path + "/emailIds");
        result.ThreadIds = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.ThreadIds), path + "/threadIds");
        JsonElement undo = Values.GetMember(value, JmapProperties.UndoStatus);
        if (undo.ValueKind != JsonValueKind.Undefined)
        {
            result.UndoStatus = DecodeUndoStatus(Values.ReadRequiredString(undo, path + "/undoStatus"), path + "/undoStatus");
            result.HasUndoStatus = true;
        }

        result.Before = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Before), path + "/before");
        result.After = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.After), path + "/after");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, SubmissionFilterNames());
        return result;
    }

    internal void EncodeSubmissionFilter(EmailSubmissionFilter value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An EmailSubmission filter cannot be null.", path);
        }

        writer.WriteObjectStart();
        if (value.HasOperator)
        {
            writer.WriteStringMember(JmapProperties.Operator, Standard.EncodeFilterOperator(value.Operator, path + "/operator"), false);
            if (value.Conditions == null)
            {
                throw Standard.Validation("An operator filter requires conditions.", path + "/conditions");
            }

            writer.WriteMemberName(JmapProperties.Conditions);
            writer.WriteArrayStart();
            int index = 0;
            foreach (EmailSubmissionFilter condition in value.Conditions)
            {
                EncodeSubmissionFilter(condition, writer, path + "/conditions/" + index);
                index = index + 1;
            }

            writer.WriteArrayEnd();
        }

        Optional.WriteOptionalStringArray(JmapProperties.IdentityIds, value.IdentityIds, writer, path + "/identityIds");
        Optional.WriteOptionalStringArray(JmapProperties.EmailIds, value.EmailIds, writer, path + "/emailIds");
        Optional.WriteOptionalStringArray(JmapProperties.ThreadIds, value.ThreadIds, writer, path + "/threadIds");
        if (value.HasUndoStatus)
        {
            writer.WriteStringMember(JmapProperties.UndoStatus, EncodeUndoStatus(value.UndoStatus, path + "/undoStatus"), false);
        }

        writer.WriteStringMember(JmapProperties.Before, value.Before, false);
        writer.WriteStringMember(JmapProperties.After, value.After, false);
        Values.WriteUnknownMembers(value.UnknownProperties, SubmissionFilterNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailSubmissionComparator DecodeSubmissionComparator(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailSubmissionComparator result = new EmailSubmissionComparator();
        result.Property = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Property), path + "/property");
        JsonElement ascending = Values.GetMember(value, JmapProperties.IsAscending);
        if (ascending.ValueKind != JsonValueKind.Undefined)
        {
            result.IsAscending = Values.ReadRequiredBoolean(ascending, path + "/isAscending");
            result.HasIsAscending = true;
        }

        result.Collation = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Collation), path + "/collation");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, ComparatorNames());
        return result;
    }

    internal void EncodeSubmissionComparator(EmailSubmissionComparator value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An EmailSubmission comparator cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Property, value.Property, path + "/property");
        if (value.HasIsAscending)
        {
            writer.WriteBooleanMember(JmapProperties.IsAscending, value.IsAscending, true);
        }

        writer.WriteStringMember(JmapProperties.Collation, value.Collation, false);
        Values.WriteUnknownMembers(value.UnknownProperties, ComparatorNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailSubmissionGetRequest DecodeSubmissionGetRequest(JsonElement value)
    {
        JmapGetRequest common = Standard.DecodeGetRequest(value);
        EmailSubmissionGetRequest result = new EmailSubmissionGetRequest();
        result.AccountId = common.AccountId;
        result.Ids = common.Ids;
        result.Properties = common.Properties;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeSubmissionGetRequest(EmailSubmissionGetRequest value, JmapJsonWriter writer)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        JmapGetRequest common = new JmapGetRequest();
        common.AccountId = value.AccountId;
        common.Ids = value.Ids;
        common.Properties = value.Properties;
        common.UnknownProperties = value.UnknownProperties;
        Standard.EncodeGetRequest(common, writer);
    }

    internal EmailSubmissionGetResponse DecodeSubmissionGetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailSubmissionGetResponse result = new EmailSubmissionGetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.State = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.State), "/state");
        result.List = DecodeSubmissionList(Values.GetMember(value, JmapProperties.List), "/list");
        result.NotFound = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, GetResponseNames());
        return result;
    }

    internal void EncodeSubmissionGetResponse(EmailSubmissionGetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.State, value.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        EncodeSubmissionList(value.List, writer, "/list");
        writer.WriteMemberName(JmapProperties.NotFound);
        Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, GetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailSubmissionChangesRequest DecodeSubmissionChangesRequest(JsonElement value)
    {
        JmapChangesRequest common = Standard.DecodeChangesRequest(value);
        EmailSubmissionChangesRequest result = new EmailSubmissionChangesRequest();
        result.AccountId = common.AccountId;
        result.SinceState = common.SinceState;
        result.MaxChanges = common.MaxChanges;
        result.HasMaxChanges = common.HasMaxChanges;
        result.IsMaxChangesNull = common.IsMaxChangesNull;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeSubmissionChangesRequest(EmailSubmissionChangesRequest value, JmapJsonWriter writer)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        JmapChangesRequest common = new JmapChangesRequest();
        common.AccountId = value.AccountId;
        common.SinceState = value.SinceState;
        common.MaxChanges = value.MaxChanges;
        common.HasMaxChanges = value.HasMaxChanges;
        common.IsMaxChangesNull = value.IsMaxChangesNull;
        common.UnknownProperties = value.UnknownProperties;
        Standard.EncodeChangesRequest(common, writer);
    }

    internal EmailSubmissionChangesResponse DecodeSubmissionChangesResponse(JsonElement value)
    {
        JmapChangesResponse common = Standard.DecodeChangesResponse(value);
        EmailSubmissionChangesResponse result = new EmailSubmissionChangesResponse();
        result.AccountId = common.AccountId;
        result.OldState = common.OldState;
        result.NewState = common.NewState;
        result.HasMoreChanges = common.HasMoreChanges;
        result.Created = common.Created;
        result.Updated = common.Updated;
        result.Destroyed = common.Destroyed;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeSubmissionChangesResponse(EmailSubmissionChangesResponse value, JmapJsonWriter writer)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        JmapChangesResponse common = new JmapChangesResponse();
        common.AccountId = value.AccountId;
        common.OldState = value.OldState;
        common.NewState = value.NewState;
        common.HasMoreChanges = value.HasMoreChanges;
        common.Created = value.Created;
        common.Updated = value.Updated;
        common.Destroyed = value.Destroyed;
        common.UnknownProperties = value.UnknownProperties;
        Standard.EncodeChangesResponse(common, writer);
    }

    internal EmailSubmissionQueryRequest DecodeSubmissionQueryRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailSubmissionQueryRequest result = new EmailSubmissionQueryRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        JsonElement filter = Values.GetMember(value, JmapProperties.Filter);
        if (filter.ValueKind != JsonValueKind.Undefined && filter.ValueKind != JsonValueKind.Null)
        {
            result.Filter = DecodeSubmissionFilter(filter, "/filter");
        }

        result.Sort = DecodeOptionalComparatorList(Values.GetMember(value, JmapProperties.Sort), "/sort");
        ReadSubmissionQueryWindow(value, result);
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.QueryRequestNames());
        return result;
    }

    internal void EncodeSubmissionQueryRequest(EmailSubmissionQueryRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        if (value.Filter != null)
        {
            writer.WriteMemberName(JmapProperties.Filter);
            EncodeSubmissionFilter(value.Filter, writer, "/filter");
        }

        WriteOptionalComparatorList(JmapProperties.Sort, value.Sort, writer, "/sort");
        WriteSubmissionQueryWindow(value, writer);
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.QueryRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailSubmissionQueryResponse DecodeSubmissionQueryResponse(JsonElement value)
    {
        JmapQueryResponse common = Standard.DecodeQueryResponse(value);
        EmailSubmissionQueryResponse result = new EmailSubmissionQueryResponse();
        result.AccountId = common.AccountId;
        result.QueryState = common.QueryState;
        result.CanCalculateChanges = common.CanCalculateChanges;
        result.Position = common.Position;
        result.Ids = common.Ids;
        result.Total = common.Total;
        result.HasTotal = common.HasTotal;
        result.Limit = common.Limit;
        result.HasLimit = common.HasLimit;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeSubmissionQueryResponse(EmailSubmissionQueryResponse value, JmapJsonWriter writer)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        JmapQueryResponse common = new JmapQueryResponse();
        common.AccountId = value.AccountId;
        common.QueryState = value.QueryState;
        common.CanCalculateChanges = value.CanCalculateChanges;
        common.Position = value.Position;
        common.Ids = value.Ids;
        common.Total = value.Total;
        common.HasTotal = value.HasTotal;
        common.Limit = value.Limit;
        common.HasLimit = value.HasLimit;
        common.UnknownProperties = value.UnknownProperties;
        Standard.EncodeQueryResponse(common, writer);
    }

    internal EmailSubmissionQueryChangesRequest DecodeSubmissionQueryChangesRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailSubmissionQueryChangesRequest result = new EmailSubmissionQueryChangesRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        JsonElement filter = Values.GetMember(value, JmapProperties.Filter);
        if (filter.ValueKind != JsonValueKind.Undefined && filter.ValueKind != JsonValueKind.Null)
        {
            result.Filter = DecodeSubmissionFilter(filter, "/filter");
        }

        result.Sort = DecodeOptionalComparatorList(Values.GetMember(value, JmapProperties.Sort), "/sort");
        result.SinceQueryState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.SinceQueryState), "/sinceQueryState");
        ReadSubmissionMaxChanges(Values.GetMember(value, JmapProperties.MaxChanges), result);
        result.UpToId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.UpToId), "/upToId");
        JsonElement calculate = Values.GetMember(value, JmapProperties.CalculateTotal);
        if (calculate.ValueKind != JsonValueKind.Undefined)
        {
            result.CalculateTotal = Values.ReadRequiredBoolean(calculate, "/calculateTotal");
            result.HasCalculateTotal = true;
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.QueryChangesRequestNames());
        return result;
    }

    internal void EncodeSubmissionQueryChangesRequest(EmailSubmissionQueryChangesRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        if (value.Filter != null)
        {
            writer.WriteMemberName(JmapProperties.Filter);
            EncodeSubmissionFilter(value.Filter, writer, "/filter");
        }

        WriteOptionalComparatorList(JmapProperties.Sort, value.Sort, writer, "/sort");
        Values.WriteRequiredStringMember(writer, JmapProperties.SinceQueryState, value.SinceQueryState, "/sinceQueryState");
        Standard.WriteOptionalUnsigned(JmapProperties.MaxChanges, value.MaxChanges, value.HasMaxChanges, value.IsMaxChangesNull, writer, "/maxChanges");
        writer.WriteStringMember(JmapProperties.UpToId, value.UpToId, false);
        if (value.HasCalculateTotal)
        {
            writer.WriteBooleanMember(JmapProperties.CalculateTotal, value.CalculateTotal, true);
        }

        Values.WriteUnknownMembers(value.UnknownProperties, Standard.QueryChangesRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailSubmissionQueryChangesResponse DecodeSubmissionQueryChangesResponse(JsonElement value)
    {
        JmapQueryChangesResponse common = Standard.DecodeQueryChangesResponse(value);
        EmailSubmissionQueryChangesResponse result = new EmailSubmissionQueryChangesResponse();
        result.AccountId = common.AccountId;
        result.OldQueryState = common.OldQueryState;
        result.NewQueryState = common.NewQueryState;
        result.Total = common.Total;
        result.HasTotal = common.HasTotal;
        result.Removed = common.Removed;
        result.Added = common.Added;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeSubmissionQueryChangesResponse(EmailSubmissionQueryChangesResponse value, JmapJsonWriter writer)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        JmapQueryChangesResponse common = new JmapQueryChangesResponse();
        common.AccountId = value.AccountId;
        common.OldQueryState = value.OldQueryState;
        common.NewQueryState = value.NewQueryState;
        common.Total = value.Total;
        common.HasTotal = value.HasTotal;
        common.Removed = value.Removed;
        common.Added = value.Added;
        common.UnknownProperties = value.UnknownProperties;
        Standard.EncodeQueryChangesResponse(common, writer);
    }

    internal EmailSubmissionSetRequest DecodeSubmissionSetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailSubmissionSetRequest result = new EmailSubmissionSetRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.IfInState = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.IfInState), "/ifInState");
        result.Create = DecodeOptionalSubmissionMap(Values.GetMember(value, JmapProperties.Create), "/create");
        result.Update = Standard.DecodeOptionalPatchMap(Values.GetMember(value, JmapProperties.Update), "/update");
        result.Destroy = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroy), "/destroy");
        result.OnSuccessUpdateEmail = Standard.DecodeOptionalPatchMap(Values.GetMember(value, JmapProperties.OnSuccessUpdateEmail), "/onSuccessUpdateEmail");
        result.OnSuccessDestroyEmail = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.OnSuccessDestroyEmail), "/onSuccessDestroyEmail");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, SubmissionSetRequestNames());
        return result;
    }

    internal void EncodeSubmissionSetRequest(EmailSubmissionSetRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.IfInState, value.IfInState, false);
        WriteOptionalSubmissionCreateMap(JmapProperties.Create, value.Create, writer, "/create");
        Standard.WriteOptionalPatchMap(JmapProperties.Update, value.Update, writer, "/update");
        Optional.WriteOptionalStringArray(JmapProperties.Destroy, value.Destroy, writer, "/destroy");
        Standard.WriteOptionalPatchMap(JmapProperties.OnSuccessUpdateEmail, value.OnSuccessUpdateEmail, writer, "/onSuccessUpdateEmail");
        Optional.WriteOptionalStringArray(JmapProperties.OnSuccessDestroyEmail, value.OnSuccessDestroyEmail, writer, "/onSuccessDestroyEmail");
        Values.WriteUnknownMembers(value.UnknownProperties, SubmissionSetRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailSubmissionSetResponse DecodeSubmissionSetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailSubmissionSetResponse result = new EmailSubmissionSetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldState = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.OldState), "/oldState");
        result.NewState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewState), "/newState");
        result.Created = DecodeOptionalSubmissionMap(Values.GetMember(value, JmapProperties.Created), "/created");
        result.Updated = Standard.DecodeOptionalElementMap(Values.GetMember(value, JmapProperties.Updated), "/updated");
        result.Destroyed = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroyed), "/destroyed");
        result.NotCreated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCreated), "/notCreated");
        result.NotUpdated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotUpdated), "/notUpdated");
        result.NotDestroyed = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotDestroyed), "/notDestroyed");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.SetResponseNames());
        return result;
    }

    internal void EncodeSubmissionSetResponse(EmailSubmissionSetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.OldState, value.OldState, true);
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        WriteOptionalSubmissionMap(JmapProperties.Created, value.Created, writer, "/created");
        Standard.WriteOptionalElementMap(JmapProperties.Updated, value.Updated, writer, "/updated");
        Optional.WriteOptionalStringArray(JmapProperties.Destroyed, value.Destroyed, writer, "/destroyed");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotCreated, value.NotCreated, writer, "/notCreated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotUpdated, value.NotUpdated, writer, "/notUpdated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotDestroyed, value.NotDestroyed, writer, "/notDestroyed");
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.SetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal Dictionary<string, JsonElement>? DecodeOptionalParameterMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, JsonElement> result = new Dictionary<string, JsonElement>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String && property.Value.ValueKind != JsonValueKind.Null)
            {
                throw new JmapProtocolException("An SMTP parameter value must be a string or null.", path + "/" + property.Name, Values.CloneContext(property.Value));
            }

            result.Add(property.Name, Reader.CloneUnknownValue(property.Value));
        }

        return result;
    }

    internal void WriteOptionalParameterMap(string name, Dictionary<string, JsonElement>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            JsonElement parameter = value[key];
            if (parameter.ValueKind != JsonValueKind.String && parameter.ValueKind != JsonValueKind.Null)
            {
                throw Standard.Validation("An SMTP parameter value must be a string or null.", path + "/" + key);
            }

            writer.WriteElementMember(key, parameter);
        }

        writer.WriteObjectEnd();
    }

    internal List<EmailSubmissionAddress> DecodeSubmissionAddressList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An EmailSubmission address array is required.", path, Values.CloneContext(value));
        }

        List<EmailSubmissionAddress> result = new List<EmailSubmissionAddress>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeSubmissionAddress(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeSubmissionAddressList(List<EmailSubmissionAddress> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An EmailSubmission address array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (EmailSubmissionAddress item in value)
        {
            EncodeSubmissionAddress(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal Dictionary<string, EmailDeliveryStatus>? DecodeOptionalDeliveryMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, EmailDeliveryStatus> result = new Dictionary<string, EmailDeliveryStatus>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeDeliveryStatus(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void WriteOptionalDeliveryMap(string name, Dictionary<string, EmailDeliveryStatus>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeDeliveryStatus(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal List<EmailSubmission> DecodeSubmissionList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An EmailSubmission array is required.", path, Values.CloneContext(value));
        }

        List<EmailSubmission> result = new List<EmailSubmission>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeSubmission(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeSubmissionList(List<EmailSubmission> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An EmailSubmission array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (EmailSubmission item in value)
        {
            EncodeSubmission(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal Dictionary<string, EmailSubmission>? DecodeOptionalSubmissionMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, EmailSubmission> result = new Dictionary<string, EmailSubmission>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeSubmission(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void WriteOptionalSubmissionMap(string name, Dictionary<string, EmailSubmission>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeSubmission(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal void WriteOptionalSubmissionCreateMap(string name, Dictionary<string, EmailSubmission>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeSubmissionCreate(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal List<EmailSubmissionComparator>? DecodeOptionalComparatorList(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An EmailSubmission comparator array is required.", path, Values.CloneContext(value));
        }

        List<EmailSubmissionComparator> result = new List<EmailSubmissionComparator>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeSubmissionComparator(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void WriteOptionalComparatorList(string name, List<EmailSubmissionComparator>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteArrayStart();
        int index = 0;
        foreach (EmailSubmissionComparator item in value)
        {
            EncodeSubmissionComparator(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal void ReadSubmissionQueryWindow(JsonElement source, EmailSubmissionQueryRequest destination)
    {
        JsonElement position = Values.GetMember(source, JmapProperties.Position);
        if (position.ValueKind != JsonValueKind.Undefined)
        {
            destination.Position = Standard.ReadRequiredInteger(position, "/position");
            destination.HasPosition = true;
        }

        destination.Anchor = Optional.ReadOptionalString(Values.GetMember(source, JmapProperties.Anchor), "/anchor");
        JsonElement anchorOffset = Values.GetMember(source, JmapProperties.AnchorOffset);
        if (anchorOffset.ValueKind != JsonValueKind.Undefined)
        {
            destination.AnchorOffset = Standard.ReadRequiredInteger(anchorOffset, "/anchorOffset");
            destination.HasAnchorOffset = true;
        }

        JsonElement limit = Values.GetMember(source, JmapProperties.Limit);
        if (limit.ValueKind != JsonValueKind.Undefined)
        {
            destination.HasLimit = true;
            if (limit.ValueKind == JsonValueKind.Null)
            {
                destination.IsLimitNull = true;
            }
            else
            {
                destination.Limit = Values.ReadRequiredUnsignedInteger(limit, "/limit");
            }
        }

        JsonElement calculate = Values.GetMember(source, JmapProperties.CalculateTotal);
        if (calculate.ValueKind != JsonValueKind.Undefined)
        {
            destination.CalculateTotal = Values.ReadRequiredBoolean(calculate, "/calculateTotal");
            destination.HasCalculateTotal = true;
        }
    }

    internal void WriteSubmissionQueryWindow(EmailSubmissionQueryRequest value, JmapJsonWriter writer)
    {
        if (value.HasPosition)
        {
            writer.WriteIntegerMember(JmapProperties.Position, value.Position, true);
        }

        writer.WriteStringMember(JmapProperties.Anchor, value.Anchor, false);
        if (value.HasAnchorOffset)
        {
            writer.WriteIntegerMember(JmapProperties.AnchorOffset, value.AnchorOffset, true);
        }

        Standard.WriteOptionalUnsigned(JmapProperties.Limit, value.Limit, value.HasLimit, value.IsLimitNull, writer, "/limit");
        if (value.HasCalculateTotal)
        {
            writer.WriteBooleanMember(JmapProperties.CalculateTotal, value.CalculateTotal, true);
        }
    }

    internal void ReadSubmissionMaxChanges(JsonElement source, EmailSubmissionQueryChangesRequest destination)
    {
        if (source.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        destination.HasMaxChanges = true;
        if (source.ValueKind == JsonValueKind.Null)
        {
            destination.IsMaxChangesNull = true;
        }
        else
        {
            destination.MaxChanges = Values.ReadRequiredUnsignedInteger(source, "/maxChanges");
        }
    }

    internal JmapSubmissionUndoStatus DecodeUndoStatus(string value, string path)
    {
        switch (value)
        {
            case "pending":
                return JmapSubmissionUndoStatus.Pending;
            case "final":
                return JmapSubmissionUndoStatus.Final;
            case "canceled":
                return JmapSubmissionUndoStatus.Canceled;
            default:
                throw new JmapProtocolException("The EmailSubmission undo status is not recognized.", path, default(JsonElement));
        }
    }

    internal string EncodeUndoStatus(JmapSubmissionUndoStatus value, string path)
    {
        switch (value)
        {
            case JmapSubmissionUndoStatus.Pending:
                return "pending";
            case JmapSubmissionUndoStatus.Final:
                return "final";
            case JmapSubmissionUndoStatus.Canceled:
                return "canceled";
            default:
                throw Standard.Validation("The EmailSubmission undo status is not recognized.", path);
        }
    }

    internal List<string> SubmissionNames()
    {
        return new List<string> { JmapProperties.Id, JmapProperties.IdentityId, JmapProperties.EmailId, JmapProperties.ThreadId, JmapProperties.Envelope, JmapProperties.SendAt, JmapProperties.UndoStatus, JmapProperties.DeliveryStatus, JmapProperties.DsnBlobIds, JmapProperties.MdnBlobIds };
    }

    internal List<string> DeliveryStatusNames()
    {
        return new List<string> { JmapProperties.SmtpReply, JmapProperties.Delivered, JmapProperties.Displayed };
    }

    internal List<string> SubmissionFilterNames()
    {
        return new List<string> { JmapProperties.Operator, JmapProperties.Conditions, JmapProperties.IdentityIds, JmapProperties.EmailIds, JmapProperties.ThreadIds, JmapProperties.UndoStatus, JmapProperties.Before, JmapProperties.After };
    }

    internal List<string> ComparatorNames()
    {
        return new List<string> { JmapProperties.Property, JmapProperties.IsAscending, JmapProperties.Collation };
    }

    internal List<string> GetResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound };
    }

    internal List<string> SubmissionSetRequestNames()
    {
        List<string> result = Standard.SetRequestNames();
        result.Add(JmapProperties.OnSuccessUpdateEmail);
        result.Add(JmapProperties.OnSuccessDestroyEmail);
        return result;
    }
}
