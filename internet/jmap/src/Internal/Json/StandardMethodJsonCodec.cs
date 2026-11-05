using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class StandardMethodJsonCodec
{
    internal JmapJsonReader Reader;
    internal SessionJsonCodec Values;
    internal ProtocolJsonCodec Errors;

    internal StandardMethodJsonCodec(JmapJsonReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        Reader = reader;
        Values = new SessionJsonCodec(reader);
        Errors = new ProtocolJsonCodec(reader);
    }

    internal JmapGetRequest DecodeGetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapGetRequest result = new JmapGetRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.Ids = Errors.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Ids), "/ids");
        result.Properties = Errors.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Properties), "/properties");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.Ids, JmapProperties.Properties });
        return result;
    }

    internal void EncodeGetRequest(JmapGetRequest value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Errors.WriteOptionalStringArray(JmapProperties.Ids, value.Ids, writer, "/ids");
        Errors.WriteOptionalStringArray(JmapProperties.Properties, value.Properties, writer, "/properties");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.Ids, JmapProperties.Properties }, writer);
        writer.WriteObjectEnd();
    }

    internal JmapGetResponse DecodeGetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapGetResponse result = new JmapGetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.State = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.State), "/state");
        result.List = DecodeElementList(Values.GetMember(value, JmapProperties.List), "/list");
        result.NotFound = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound });
        return result;
    }

    internal void EncodeGetResponse(JmapGetResponse value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.State, value.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        EncodeElementList(value.List, writer, "/list");
        writer.WriteMemberName(JmapProperties.NotFound);
        Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound }, writer);
        writer.WriteObjectEnd();
    }

    internal JmapChangesRequest DecodeChangesRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapChangesRequest result = new JmapChangesRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.SinceState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.SinceState), "/sinceState");
        ReadOptionalUnsigned(Values.GetMember(value, JmapProperties.MaxChanges), result, null);
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.SinceState, JmapProperties.MaxChanges });
        return result;
    }

    internal void EncodeChangesRequest(JmapChangesRequest value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.SinceState, value.SinceState, "/sinceState");
        WriteOptionalUnsigned(JmapProperties.MaxChanges, value.MaxChanges, value.HasMaxChanges, value.IsMaxChangesNull, writer, "/maxChanges");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.SinceState, JmapProperties.MaxChanges }, writer);
        writer.WriteObjectEnd();
    }

    internal JmapChangesResponse DecodeChangesResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapChangesResponse result = new JmapChangesResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.OldState), "/oldState");
        result.NewState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewState), "/newState");
        result.HasMoreChanges = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.HasMoreChanges), "/hasMoreChanges");
        result.Created = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.Created), "/created");
        result.Updated = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.Updated), "/updated");
        result.Destroyed = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.Destroyed), "/destroyed");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, ChangesResponseNames());
        return result;
    }

    internal void EncodeChangesResponse(JmapChangesResponse value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.OldState, value.OldState, "/oldState");
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        writer.WriteBooleanMember(JmapProperties.HasMoreChanges, value.HasMoreChanges, true);
        writer.WriteMemberName(JmapProperties.Created);
        Values.EncodeStringArray(value.Created, writer, "/created");
        writer.WriteMemberName(JmapProperties.Updated);
        Values.EncodeStringArray(value.Updated, writer, "/updated");
        writer.WriteMemberName(JmapProperties.Destroyed);
        Values.EncodeStringArray(value.Destroyed, writer, "/destroyed");
        Values.WriteUnknownMembers(value.UnknownProperties, ChangesResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapPatchObject DecodePatchObject(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        JmapPatchObject result = new JmapPatchObject();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Values.Add(property.Name, Reader.CloneUnknownValue(property.Value));
        }

        return result;
    }

    internal void EncodePatchObject(JmapPatchObject value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Validation("A PatchObject cannot be null.", path);
        }

        writer.WriteObjectStart();
        List<string> keys = Values.SortNames(value.Values.Keys);
        foreach (string key in keys)
        {
            writer.WriteElementMember(key, value.Values[key]);
        }

        writer.WriteObjectEnd();
    }

    internal JmapSetRequest DecodeSetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapSetRequest result = new JmapSetRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.IfInState = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.IfInState), "/ifInState");
        result.Create = DecodeOptionalElementMap(Values.GetMember(value, JmapProperties.Create), "/create");
        result.Update = DecodeOptionalPatchMap(Values.GetMember(value, JmapProperties.Update), "/update");
        result.Destroy = Errors.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroy), "/destroy");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, SetRequestNames());
        return result;
    }

    internal void EncodeSetRequest(JmapSetRequest value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.IfInState, value.IfInState, false);
        WriteOptionalElementMap(JmapProperties.Create, value.Create, writer, "/create");
        WriteOptionalPatchMap(JmapProperties.Update, value.Update, writer, "/update");
        Errors.WriteOptionalStringArray(JmapProperties.Destroy, value.Destroy, writer, "/destroy");
        Values.WriteUnknownMembers(value.UnknownProperties, SetRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapSetResponse DecodeSetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapSetResponse result = new JmapSetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldState = ReadRequiredNullableString(Values.GetMember(value, JmapProperties.OldState), "/oldState");
        result.NewState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewState), "/newState");
        result.Created = DecodeOptionalElementMap(Values.GetMember(value, JmapProperties.Created), "/created");
        result.Updated = DecodeOptionalElementMap(Values.GetMember(value, JmapProperties.Updated), "/updated");
        result.Destroyed = Errors.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroyed), "/destroyed");
        result.NotCreated = DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCreated), "/notCreated");
        result.NotUpdated = DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotUpdated), "/notUpdated");
        result.NotDestroyed = DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotDestroyed), "/notDestroyed");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, SetResponseNames());
        return result;
    }

    internal void EncodeSetResponse(JmapSetResponse value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.OldState, value.OldState, true);
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        WriteOptionalElementMap(JmapProperties.Created, value.Created, writer, "/created");
        WriteOptionalElementMap(JmapProperties.Updated, value.Updated, writer, "/updated");
        Errors.WriteOptionalStringArray(JmapProperties.Destroyed, value.Destroyed, writer, "/destroyed");
        WriteOptionalSetErrorMap(JmapProperties.NotCreated, value.NotCreated, writer, "/notCreated");
        WriteOptionalSetErrorMap(JmapProperties.NotUpdated, value.NotUpdated, writer, "/notUpdated");
        WriteOptionalSetErrorMap(JmapProperties.NotDestroyed, value.NotDestroyed, writer, "/notDestroyed");
        Values.WriteUnknownMembers(value.UnknownProperties, SetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapCopyRequest DecodeCopyRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapCopyRequest result = new JmapCopyRequest();
        result.FromAccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.FromAccountId), "/fromAccountId");
        result.IfFromInState = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.IfFromInState), "/ifFromInState");
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.IfInState = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.IfInState), "/ifInState");
        result.Create = DecodeElementMap(Values.GetMember(value, JmapProperties.Create), "/create");
        JsonElement destroyOriginal = Values.GetMember(value, JmapProperties.OnSuccessDestroyOriginal);
        if (destroyOriginal.ValueKind != JsonValueKind.Undefined)
        {
            result.OnSuccessDestroyOriginal = Values.ReadRequiredBoolean(destroyOriginal, "/onSuccessDestroyOriginal");
            result.HasOnSuccessDestroyOriginal = true;
        }

        result.DestroyFromIfInState = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.DestroyFromIfInState), "/destroyFromIfInState");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, CopyRequestNames());
        return result;
    }

    internal void EncodeCopyRequest(JmapCopyRequest value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.FromAccountId, value.FromAccountId, "/fromAccountId");
        writer.WriteStringMember(JmapProperties.IfFromInState, value.IfFromInState, false);
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.IfInState, value.IfInState, false);
        writer.WriteMemberName(JmapProperties.Create);
        EncodeElementMap(value.Create, writer, "/create");
        if (value.HasOnSuccessDestroyOriginal)
        {
            writer.WriteBooleanMember(JmapProperties.OnSuccessDestroyOriginal, value.OnSuccessDestroyOriginal, true);
        }

        writer.WriteStringMember(JmapProperties.DestroyFromIfInState, value.DestroyFromIfInState, false);
        Values.WriteUnknownMembers(value.UnknownProperties, CopyRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapCopyResponse DecodeCopyResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapCopyResponse result = new JmapCopyResponse();
        result.FromAccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.FromAccountId), "/fromAccountId");
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldState = ReadRequiredNullableString(Values.GetMember(value, JmapProperties.OldState), "/oldState");
        result.NewState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewState), "/newState");
        result.Created = DecodeOptionalElementMap(Values.GetMember(value, JmapProperties.Created), "/created");
        result.NotCreated = DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCreated), "/notCreated");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, CopyResponseNames());
        return result;
    }

    internal void EncodeCopyResponse(JmapCopyResponse value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.FromAccountId, value.FromAccountId, "/fromAccountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.OldState, value.OldState, true);
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        WriteOptionalElementMap(JmapProperties.Created, value.Created, writer, "/created");
        WriteOptionalSetErrorMap(JmapProperties.NotCreated, value.NotCreated, writer, "/notCreated");
        Values.WriteUnknownMembers(value.UnknownProperties, CopyResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapFilterNode DecodeFilterNode(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        JmapFilterNode result = new JmapFilterNode();
        JsonElement operatorValue = Values.GetMember(value, JmapProperties.Operator);
        if (operatorValue.ValueKind != JsonValueKind.Undefined)
        {
            result.Operator = DecodeFilterOperator(Values.ReadRequiredString(operatorValue, path + "/operator"), path + "/operator");
            result.HasOperator = true;
        }

        JsonElement conditions = Values.GetMember(value, JmapProperties.Conditions);
        if (conditions.ValueKind != JsonValueKind.Undefined && conditions.ValueKind != JsonValueKind.Null)
        {
            if (conditions.ValueKind != JsonValueKind.Array)
            {
                throw new JmapProtocolException("Filter conditions must be an array.", path + "/conditions", Values.CloneContext(conditions));
            }

            result.Conditions = new List<JmapFilterNode>();
            int index = 0;
            foreach (JsonElement condition in conditions.EnumerateArray())
            {
                result.Conditions.Add(DecodeFilterNode(condition, path + "/conditions/" + index));
                index = index + 1;
            }
        }

        Values.CaptureUnknownMembers(value, result.FilterProperties, new List<string> { JmapProperties.Operator, JmapProperties.Conditions });
        return result;
    }

    internal void EncodeFilterNode(JmapFilterNode value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Validation("A filter node cannot be null.", path);
        }

        writer.WriteObjectStart();
        if (value.HasOperator)
        {
            writer.WriteStringMember(JmapProperties.Operator, EncodeFilterOperator(value.Operator, path + "/operator"), false);
            if (value.Conditions == null)
            {
                throw Validation("An operator filter requires conditions.", path + "/conditions");
            }

            writer.WriteMemberName(JmapProperties.Conditions);
            writer.WriteArrayStart();
            int index = 0;
            foreach (JmapFilterNode condition in value.Conditions)
            {
                EncodeFilterNode(condition, writer, path + "/conditions/" + index);
                index = index + 1;
            }

            writer.WriteArrayEnd();
        }

        Values.WriteUnknownMembers(value.FilterProperties, new List<string> { JmapProperties.Operator, JmapProperties.Conditions }, writer);
        writer.WriteObjectEnd();
    }

    internal JmapComparator DecodeComparator(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        JmapComparator result = new JmapComparator();
        result.Property = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Property), path + "/property");
        JsonElement ascending = Values.GetMember(value, JmapProperties.IsAscending);
        if (ascending.ValueKind != JsonValueKind.Undefined)
        {
            result.IsAscending = Values.ReadRequiredBoolean(ascending, path + "/isAscending");
            result.HasIsAscending = true;
        }

        result.Collation = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.Collation), path + "/collation");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.Property, JmapProperties.IsAscending, JmapProperties.Collation });
        return result;
    }

    internal void EncodeComparator(JmapComparator value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Validation("A comparator cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Property, value.Property, path + "/property");
        if (value.HasIsAscending)
        {
            writer.WriteBooleanMember(JmapProperties.IsAscending, value.IsAscending, true);
        }

        writer.WriteStringMember(JmapProperties.Collation, value.Collation, false);
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.Property, JmapProperties.IsAscending, JmapProperties.Collation }, writer);
        writer.WriteObjectEnd();
    }

    internal JmapQueryRequest DecodeQueryRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapQueryRequest result = new JmapQueryRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        JsonElement filter = Values.GetMember(value, JmapProperties.Filter);
        if (filter.ValueKind != JsonValueKind.Undefined && filter.ValueKind != JsonValueKind.Null)
        {
            result.Filter = DecodeFilterNode(filter, "/filter");
        }

        result.Sort = DecodeOptionalComparatorList(Values.GetMember(value, JmapProperties.Sort), "/sort");
        ReadQueryWindow(value, result);
        Values.CaptureUnknownMembers(value, result.UnknownProperties, QueryRequestNames());
        return result;
    }

    internal void EncodeQueryRequest(JmapQueryRequest value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        if (value.Filter != null)
        {
            writer.WriteMemberName(JmapProperties.Filter);
            EncodeFilterNode(value.Filter, writer, "/filter");
        }

        WriteOptionalComparatorList(JmapProperties.Sort, value.Sort, writer, "/sort");
        WriteQueryWindow(value, writer);
        Values.WriteUnknownMembers(value.UnknownProperties, QueryRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapQueryResponse DecodeQueryResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapQueryResponse result = new JmapQueryResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.QueryState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.QueryState), "/queryState");
        result.CanCalculateChanges = Values.ReadRequiredBoolean(Values.GetMember(value, JmapProperties.CanCalculateChanges), "/canCalculateChanges");
        result.Position = ReadRequiredInteger(Values.GetMember(value, JmapProperties.Position), "/position");
        result.Ids = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.Ids), "/ids");
        JsonElement total = Values.GetMember(value, JmapProperties.Total);
        if (total.ValueKind != JsonValueKind.Undefined)
        {
            result.Total = Values.ReadRequiredUnsignedInteger(total, "/total");
            result.HasTotal = true;
        }

        JsonElement limit = Values.GetMember(value, JmapProperties.Limit);
        if (limit.ValueKind != JsonValueKind.Undefined)
        {
            result.Limit = Values.ReadRequiredUnsignedInteger(limit, "/limit");
            result.HasLimit = true;
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, QueryResponseNames());
        return result;
    }

    internal void EncodeQueryResponse(JmapQueryResponse value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.QueryState, value.QueryState, "/queryState");
        writer.WriteBooleanMember(JmapProperties.CanCalculateChanges, value.CanCalculateChanges, true);
        writer.WriteIntegerMember(JmapProperties.Position, value.Position, true);
        writer.WriteMemberName(JmapProperties.Ids);
        Values.EncodeStringArray(value.Ids, writer, "/ids");
        if (value.HasTotal)
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.Total, value.Total, "/total");
        }

        if (value.HasLimit)
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.Limit, value.Limit, "/limit");
        }

        Values.WriteUnknownMembers(value.UnknownProperties, QueryResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapQueryChangesRequest DecodeQueryChangesRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapQueryChangesRequest result = new JmapQueryChangesRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        JsonElement filter = Values.GetMember(value, JmapProperties.Filter);
        if (filter.ValueKind != JsonValueKind.Undefined && filter.ValueKind != JsonValueKind.Null)
        {
            result.Filter = DecodeFilterNode(filter, "/filter");
        }

        result.Sort = DecodeOptionalComparatorList(Values.GetMember(value, JmapProperties.Sort), "/sort");
        result.SinceQueryState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.SinceQueryState), "/sinceQueryState");
        ReadOptionalUnsigned(Values.GetMember(value, JmapProperties.MaxChanges), null, result);
        result.UpToId = Errors.ReadOptionalString(Values.GetMember(value, JmapProperties.UpToId), "/upToId");
        JsonElement calculate = Values.GetMember(value, JmapProperties.CalculateTotal);
        if (calculate.ValueKind != JsonValueKind.Undefined)
        {
            result.CalculateTotal = Values.ReadRequiredBoolean(calculate, "/calculateTotal");
            result.HasCalculateTotal = true;
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, QueryChangesRequestNames());
        return result;
    }

    internal void EncodeQueryChangesRequest(JmapQueryChangesRequest value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        if (value.Filter != null)
        {
            writer.WriteMemberName(JmapProperties.Filter);
            EncodeFilterNode(value.Filter, writer, "/filter");
        }

        WriteOptionalComparatorList(JmapProperties.Sort, value.Sort, writer, "/sort");
        Values.WriteRequiredStringMember(writer, JmapProperties.SinceQueryState, value.SinceQueryState, "/sinceQueryState");
        WriteOptionalUnsigned(JmapProperties.MaxChanges, value.MaxChanges, value.HasMaxChanges, value.IsMaxChangesNull, writer, "/maxChanges");
        writer.WriteStringMember(JmapProperties.UpToId, value.UpToId, false);
        if (value.HasCalculateTotal)
        {
            writer.WriteBooleanMember(JmapProperties.CalculateTotal, value.CalculateTotal, true);
        }

        Values.WriteUnknownMembers(value.UnknownProperties, QueryChangesRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapAddedItem DecodeAddedItem(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        JmapAddedItem result = new JmapAddedItem();
        result.Id = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Id), path + "/id");
        result.Index = Values.ReadRequiredUnsignedInteger(Values.GetMember(value, JmapProperties.Index), path + "/index");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.Id, JmapProperties.Index });
        return result;
    }

    internal void EncodeAddedItem(JmapAddedItem value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Validation("An added item cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Id, value.Id, path + "/id");
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.Index, value.Index, path + "/index");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.Id, JmapProperties.Index }, writer);
        writer.WriteObjectEnd();
    }

    internal JmapQueryChangesResponse DecodeQueryChangesResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        JmapQueryChangesResponse result = new JmapQueryChangesResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldQueryState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.OldQueryState), "/oldQueryState");
        result.NewQueryState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewQueryState), "/newQueryState");
        JsonElement total = Values.GetMember(value, JmapProperties.Total);
        if (total.ValueKind != JsonValueKind.Undefined)
        {
            result.Total = Values.ReadRequiredUnsignedInteger(total, "/total");
            result.HasTotal = true;
        }

        result.Removed = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.Removed), "/removed");
        result.Added = DecodeAddedList(Values.GetMember(value, JmapProperties.Added), "/added");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, QueryChangesResponseNames());
        return result;
    }

    internal void EncodeQueryChangesResponse(JmapQueryChangesResponse value, JmapJsonWriter writer)
    {
        RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.OldQueryState, value.OldQueryState, "/oldQueryState");
        Values.WriteRequiredStringMember(writer, JmapProperties.NewQueryState, value.NewQueryState, "/newQueryState");
        if (value.HasTotal)
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.Total, value.Total, "/total");
        }

        writer.WriteMemberName(JmapProperties.Removed);
        Values.EncodeStringArray(value.Removed, writer, "/removed");
        writer.WriteMemberName(JmapProperties.Added);
        EncodeAddedList(value.Added, writer, "/added");
        Values.WriteUnknownMembers(value.UnknownProperties, QueryChangesResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal List<JsonElement> DecodeElementList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("A JSON array is required.", path, Values.CloneContext(value));
        }

        List<JsonElement> result = new List<JsonElement>();
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(Reader.CloneUnknownValue(item));
        }

        return result;
    }

    internal void EncodeElementList(List<JsonElement> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Validation("A JSON value list cannot be null.", path);
        }

        writer.WriteArrayStart();
        foreach (JsonElement item in value)
        {
            writer.WriteElementValue(item);
        }

        writer.WriteArrayEnd();
    }

    internal Dictionary<string, JsonElement> DecodeElementMap(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Dictionary<string, JsonElement> result = new Dictionary<string, JsonElement>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, Reader.CloneUnknownValue(property.Value));
        }

        return result;
    }

    internal void EncodeElementMap(Dictionary<string, JsonElement> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Validation("A JSON value map cannot be null.", path);
        }

        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteElementMember(key, value[key]);
        }

        writer.WriteObjectEnd();
    }

    internal Dictionary<string, JsonElement>? DecodeOptionalElementMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return DecodeElementMap(value, path);
    }

    internal void WriteOptionalElementMap(string name, Dictionary<string, JsonElement>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        EncodeElementMap(value, writer, path);
    }

    internal Dictionary<string, JmapPatchObject>? DecodeOptionalPatchMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, JmapPatchObject> result = new Dictionary<string, JmapPatchObject>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodePatchObject(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void WriteOptionalPatchMap(string name, Dictionary<string, JmapPatchObject>? value, JmapJsonWriter writer, string path)
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
            EncodePatchObject(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal Dictionary<string, JmapSetError>? DecodeOptionalSetErrorMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, JmapSetError> result = new Dictionary<string, JmapSetError>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, Errors.DecodeSetError(property.Value));
        }

        return result;
    }

    internal void WriteOptionalSetErrorMap(string name, Dictionary<string, JmapSetError>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            JmapSetError error = value[key];
            if (error == null)
            {
                throw Validation("A SetError map value cannot be null.", path + "/" + key);
            }

            writer.WriteMemberName(key);
            Errors.EncodeSetError(error, writer);
        }

        writer.WriteObjectEnd();
    }

    internal List<JmapComparator>? DecodeOptionalComparatorList(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("A comparator array is required.", path, Values.CloneContext(value));
        }

        List<JmapComparator> result = new List<JmapComparator>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeComparator(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void WriteOptionalComparatorList(string name, List<JmapComparator>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        writer.WriteArrayStart();
        int index = 0;
        foreach (JmapComparator comparator in value)
        {
            EncodeComparator(comparator, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal List<JmapAddedItem> DecodeAddedList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An added-item array is required.", path, Values.CloneContext(value));
        }

        List<JmapAddedItem> result = new List<JmapAddedItem>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeAddedItem(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeAddedList(List<JmapAddedItem> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Validation("An added-item array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (JmapAddedItem item in value)
        {
            EncodeAddedItem(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal long ReadRequiredInteger(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            throw new JmapProtocolException("A JSON integer is required.", path, Values.CloneContext(value));
        }

        return value.GetInt64();
    }

    internal string? ReadRequiredNullableString(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return Values.ReadRequiredString(value, path);
    }

    internal JmapFilterOperator DecodeFilterOperator(string value, string path)
    {
        switch (value)
        {
            case "AND":
                return JmapFilterOperator.And;
            case "OR":
                return JmapFilterOperator.Or;
            case "NOT":
                return JmapFilterOperator.Not;
            default:
                throw new JmapProtocolException("The filter operator is not recognized.", path, default(JsonElement));
        }
    }

    internal string EncodeFilterOperator(JmapFilterOperator value, string path)
    {
        switch (value)
        {
            case JmapFilterOperator.And:
                return "AND";
            case JmapFilterOperator.Or:
                return "OR";
            case JmapFilterOperator.Not:
                return "NOT";
            default:
                throw Validation("The filter operator is not recognized.", path);
        }
    }

    internal void ReadQueryWindow(JsonElement source, JmapQueryRequest destination)
    {
        JsonElement position = Values.GetMember(source, JmapProperties.Position);
        if (position.ValueKind != JsonValueKind.Undefined)
        {
            destination.Position = ReadRequiredInteger(position, "/position");
            destination.HasPosition = true;
        }

        destination.Anchor = Errors.ReadOptionalString(Values.GetMember(source, JmapProperties.Anchor), "/anchor");
        JsonElement anchorOffset = Values.GetMember(source, JmapProperties.AnchorOffset);
        if (anchorOffset.ValueKind != JsonValueKind.Undefined)
        {
            destination.AnchorOffset = ReadRequiredInteger(anchorOffset, "/anchorOffset");
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

    internal void WriteQueryWindow(JmapQueryRequest value, JmapJsonWriter writer)
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

        WriteOptionalUnsigned(JmapProperties.Limit, value.Limit, value.HasLimit, value.IsLimitNull, writer, "/limit");
        if (value.HasCalculateTotal)
        {
            writer.WriteBooleanMember(JmapProperties.CalculateTotal, value.CalculateTotal, true);
        }
    }

    internal void ReadOptionalUnsigned(JsonElement source, JmapChangesRequest? changes, JmapQueryChangesRequest? queryChanges)
    {
        if (source.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        if (changes != null)
        {
            changes.HasMaxChanges = true;
            if (source.ValueKind == JsonValueKind.Null)
            {
                changes.IsMaxChangesNull = true;
            }
            else
            {
                changes.MaxChanges = Values.ReadRequiredUnsignedInteger(source, "/maxChanges");
            }
        }

        if (queryChanges != null)
        {
            queryChanges.HasMaxChanges = true;
            if (source.ValueKind == JsonValueKind.Null)
            {
                queryChanges.IsMaxChangesNull = true;
            }
            else
            {
                queryChanges.MaxChanges = Values.ReadRequiredUnsignedInteger(source, "/maxChanges");
            }
        }
    }

    internal void WriteOptionalUnsigned(string name, long value, bool present, bool isNull, JmapJsonWriter writer, string path)
    {
        if (!present)
        {
            return;
        }

        if (isNull)
        {
            writer.WriteNullMember(name);
        }
        else
        {
            Values.WriteUnsignedIntegerMember(writer, name, value, path);
        }
    }

    internal void RequireEncodeInputs(object? value, JmapJsonWriter? writer)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }
    }

    internal JmapValidationException Validation(string message, string path)
    {
        return new JmapValidationException(message, new List<string> { path }, default(JsonElement));
    }

    internal List<string> ChangesResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.OldState, JmapProperties.NewState, JmapProperties.HasMoreChanges, JmapProperties.Created, JmapProperties.Updated, JmapProperties.Destroyed };
    }

    internal List<string> SetRequestNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.IfInState, JmapProperties.Create, JmapProperties.Update, JmapProperties.Destroy };
    }

    internal List<string> SetResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.OldState, JmapProperties.NewState, JmapProperties.Created, JmapProperties.Updated, JmapProperties.Destroyed, JmapProperties.NotCreated, JmapProperties.NotUpdated, JmapProperties.NotDestroyed };
    }

    internal List<string> CopyRequestNames()
    {
        return new List<string> { JmapProperties.FromAccountId, JmapProperties.IfFromInState, JmapProperties.AccountId, JmapProperties.IfInState, JmapProperties.Create, JmapProperties.OnSuccessDestroyOriginal, JmapProperties.DestroyFromIfInState };
    }

    internal List<string> CopyResponseNames()
    {
        return new List<string> { JmapProperties.FromAccountId, JmapProperties.AccountId, JmapProperties.OldState, JmapProperties.NewState, JmapProperties.Created, JmapProperties.NotCreated };
    }

    internal List<string> QueryRequestNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.Filter, JmapProperties.Sort, JmapProperties.Position, JmapProperties.Anchor, JmapProperties.AnchorOffset, JmapProperties.Limit, JmapProperties.CalculateTotal };
    }

    internal List<string> QueryResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.QueryState, JmapProperties.CanCalculateChanges, JmapProperties.Position, JmapProperties.Ids, JmapProperties.Total, JmapProperties.Limit };
    }

    internal List<string> QueryChangesRequestNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.Filter, JmapProperties.Sort, JmapProperties.SinceQueryState, JmapProperties.MaxChanges, JmapProperties.UpToId, JmapProperties.CalculateTotal };
    }

    internal List<string> QueryChangesResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.OldQueryState, JmapProperties.NewQueryState, JmapProperties.Total, JmapProperties.Removed, JmapProperties.Added };
    }
}
