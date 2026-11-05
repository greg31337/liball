using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class VacationResponseJsonCodec
{
    internal StandardMethodJsonCodec Standard;
    internal SessionJsonCodec Values;
    internal ProtocolJsonCodec Optional;

    internal VacationResponseJsonCodec(JmapJsonReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        Standard = new StandardMethodJsonCodec(reader);
        Values = new SessionJsonCodec(reader);
        Optional = new ProtocolJsonCodec(reader);
    }

    internal VacationResponse DecodeVacationResponse(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        VacationResponse result = new VacationResponse();
        result.Id = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Id), path + "/id");
        JsonElement enabled = Values.GetMember(value, JmapProperties.IsEnabled);
        if (enabled.ValueKind != JsonValueKind.Undefined)
        {
            result.IsEnabled = Values.ReadRequiredBoolean(enabled, path + "/isEnabled");
        }

        result.FromDate = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.FromDate), path + "/fromDate");
        result.ToDate = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.ToDate), path + "/toDate");
        result.Subject = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Subject), path + "/subject");
        result.TextBody = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.TextBody), path + "/textBody");
        result.HtmlBody = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.HtmlBody), path + "/htmlBody");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, VacationNames());
        return result;
    }

    internal void EncodeVacationResponse(VacationResponse value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A VacationResponse cannot be null.", path);
        }

        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.Id, value.Id, false);
        writer.WriteBooleanMember(JmapProperties.IsEnabled, value.IsEnabled, true);
        writer.WriteStringMember(JmapProperties.FromDate, value.FromDate, true);
        writer.WriteStringMember(JmapProperties.ToDate, value.ToDate, true);
        writer.WriteStringMember(JmapProperties.Subject, value.Subject, true);
        writer.WriteStringMember(JmapProperties.TextBody, value.TextBody, true);
        writer.WriteStringMember(JmapProperties.HtmlBody, value.HtmlBody, true);
        Values.WriteUnknownMembers(value.UnknownProperties, VacationNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void EncodeVacationCreate(VacationResponse value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A VacationResponse create value cannot be null.", path);
        }

        writer.WriteObjectStart();
        writer.WriteBooleanMember(JmapProperties.IsEnabled, value.IsEnabled, true);
        writer.WriteStringMember(JmapProperties.FromDate, value.FromDate, true);
        writer.WriteStringMember(JmapProperties.ToDate, value.ToDate, true);
        writer.WriteStringMember(JmapProperties.Subject, value.Subject, true);
        writer.WriteStringMember(JmapProperties.TextBody, value.TextBody, true);
        writer.WriteStringMember(JmapProperties.HtmlBody, value.HtmlBody, true);
        Values.WriteUnknownMembers(value.UnknownProperties, VacationNames(), writer);
        writer.WriteObjectEnd();
    }

    internal VacationResponseGetRequest DecodeVacationGetRequest(JsonElement value)
    {
        JmapGetRequest common = Standard.DecodeGetRequest(value);
        VacationResponseGetRequest result = new VacationResponseGetRequest();
        result.AccountId = common.AccountId;
        result.Ids = common.Ids;
        result.Properties = common.Properties;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeVacationGetRequest(VacationResponseGetRequest value, JmapJsonWriter writer)
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

    internal VacationResponseGetResponse DecodeVacationGetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        VacationResponseGetResponse result = new VacationResponseGetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.State = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.State), "/state");
        result.List = DecodeVacationList(Values.GetMember(value, JmapProperties.List), "/list");
        result.NotFound = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, GetResponseNames());
        return result;
    }

    internal void EncodeVacationGetResponse(VacationResponseGetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.State, value.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        EncodeVacationList(value.List, writer, "/list");
        writer.WriteMemberName(JmapProperties.NotFound);
        Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, GetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal VacationResponseSetRequest DecodeVacationSetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        VacationResponseSetRequest result = new VacationResponseSetRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.IfInState = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.IfInState), "/ifInState");
        result.Create = DecodeOptionalVacationMap(Values.GetMember(value, JmapProperties.Create), "/create");
        result.Update = Standard.DecodeOptionalPatchMap(Values.GetMember(value, JmapProperties.Update), "/update");
        result.Destroy = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroy), "/destroy");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.SetRequestNames());
        return result;
    }

    internal void EncodeVacationSetRequest(VacationResponseSetRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.IfInState, value.IfInState, false);
        WriteOptionalVacationCreateMap(JmapProperties.Create, value.Create, writer, "/create");
        Standard.WriteOptionalPatchMap(JmapProperties.Update, value.Update, writer, "/update");
        Optional.WriteOptionalStringArray(JmapProperties.Destroy, value.Destroy, writer, "/destroy");
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.SetRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal VacationResponseSetResponse DecodeVacationSetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        VacationResponseSetResponse result = new VacationResponseSetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldState = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.OldState), "/oldState");
        result.NewState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewState), "/newState");
        result.Created = DecodeOptionalVacationMap(Values.GetMember(value, JmapProperties.Created), "/created");
        result.Updated = Standard.DecodeOptionalElementMap(Values.GetMember(value, JmapProperties.Updated), "/updated");
        result.Destroyed = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroyed), "/destroyed");
        result.NotCreated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCreated), "/notCreated");
        result.NotUpdated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotUpdated), "/notUpdated");
        result.NotDestroyed = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotDestroyed), "/notDestroyed");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.SetResponseNames());
        return result;
    }

    internal void EncodeVacationSetResponse(VacationResponseSetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.OldState, value.OldState, true);
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        WriteOptionalVacationMap(JmapProperties.Created, value.Created, writer, "/created");
        Standard.WriteOptionalElementMap(JmapProperties.Updated, value.Updated, writer, "/updated");
        Optional.WriteOptionalStringArray(JmapProperties.Destroyed, value.Destroyed, writer, "/destroyed");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotCreated, value.NotCreated, writer, "/notCreated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotUpdated, value.NotUpdated, writer, "/notUpdated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotDestroyed, value.NotDestroyed, writer, "/notDestroyed");
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.SetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal List<VacationResponse> DecodeVacationList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("A VacationResponse array is required.", path, Values.CloneContext(value));
        }

        List<VacationResponse> result = new List<VacationResponse>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeVacationResponse(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeVacationList(List<VacationResponse> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A VacationResponse array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (VacationResponse item in value)
        {
            EncodeVacationResponse(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal Dictionary<string, VacationResponse>? DecodeOptionalVacationMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, VacationResponse> result = new Dictionary<string, VacationResponse>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeVacationResponse(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void WriteOptionalVacationMap(string name, Dictionary<string, VacationResponse>? value, JmapJsonWriter writer, string path)
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
            EncodeVacationResponse(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal void WriteOptionalVacationCreateMap(string name, Dictionary<string, VacationResponse>? value, JmapJsonWriter writer, string path)
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
            EncodeVacationCreate(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal List<string> VacationNames()
    {
        return new List<string> { JmapProperties.Id, JmapProperties.IsEnabled, JmapProperties.FromDate, JmapProperties.ToDate, JmapProperties.Subject, JmapProperties.TextBody, JmapProperties.HtmlBody };
    }

    internal List<string> GetResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound };
    }
}
