using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class IdentityJsonCodec
{
    internal EmailJsonCodec Emails;
    internal StandardMethodJsonCodec Standard;
    internal SessionJsonCodec Values;
    internal ProtocolJsonCodec Optional;

    internal IdentityJsonCodec(JmapJsonReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        Emails = new EmailJsonCodec(reader);
        Standard = new StandardMethodJsonCodec(reader);
        Values = new SessionJsonCodec(reader);
        Optional = new ProtocolJsonCodec(reader);
    }

    internal Identity DecodeIdentity(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Identity result = new Identity();
        result.Id = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Id), path + "/id");
        result.Name = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Name), path + "/name");
        result.Email = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Email), path + "/email");
        result.ReplyTo = Emails.DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.ReplyTo), path + "/replyTo");
        result.Bcc = Emails.DecodeOptionalAddressList(Values.GetMember(value, JmapProperties.Bcc), path + "/bcc");
        result.TextSignature = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.TextSignature), path + "/textSignature");
        result.HtmlSignature = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.HtmlSignature), path + "/htmlSignature");
        JsonElement mayDelete = Values.GetMember(value, JmapProperties.MayDelete);
        if (mayDelete.ValueKind != JsonValueKind.Undefined)
        {
            result.MayDelete = Values.ReadRequiredBoolean(mayDelete, path + "/mayDelete");
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, IdentityNames());
        return result;
    }

    internal void EncodeIdentity(Identity value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Identity cannot be null.", path);
        }

        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.Id, value.Id, false);
        writer.WriteStringMember(JmapProperties.Name, value.Name, false);
        writer.WriteStringMember(JmapProperties.Email, value.Email, false);
        Emails.WriteOptionalAddressList(JmapProperties.ReplyTo, value.ReplyTo, writer, path + "/replyTo");
        Emails.WriteOptionalAddressList(JmapProperties.Bcc, value.Bcc, writer, path + "/bcc");
        writer.WriteStringMember(JmapProperties.TextSignature, value.TextSignature, false);
        writer.WriteStringMember(JmapProperties.HtmlSignature, value.HtmlSignature, false);
        writer.WriteBooleanMember(JmapProperties.MayDelete, value.MayDelete, true);
        Values.WriteUnknownMembers(value.UnknownProperties, IdentityNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void EncodeIdentityCreate(Identity value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Identity create value cannot be null.", path);
        }

        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.Name, value.Name, false);
        Values.WriteRequiredStringMember(writer, JmapProperties.Email, value.Email, path + "/email");
        Emails.WriteOptionalAddressList(JmapProperties.ReplyTo, value.ReplyTo, writer, path + "/replyTo");
        Emails.WriteOptionalAddressList(JmapProperties.Bcc, value.Bcc, writer, path + "/bcc");
        writer.WriteStringMember(JmapProperties.TextSignature, value.TextSignature, false);
        writer.WriteStringMember(JmapProperties.HtmlSignature, value.HtmlSignature, false);
        Values.WriteUnknownMembers(value.UnknownProperties, IdentityNames(), writer);
        writer.WriteObjectEnd();
    }

    internal IdentityGetRequest DecodeIdentityGetRequest(JsonElement value)
    {
        JmapGetRequest common = Standard.DecodeGetRequest(value);
        IdentityGetRequest result = new IdentityGetRequest();
        result.AccountId = common.AccountId;
        result.Ids = common.Ids;
        result.Properties = common.Properties;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeIdentityGetRequest(IdentityGetRequest value, JmapJsonWriter writer)
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

    internal IdentityGetResponse DecodeIdentityGetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        IdentityGetResponse result = new IdentityGetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.State = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.State), "/state");
        result.List = DecodeIdentityList(Values.GetMember(value, JmapProperties.List), "/list");
        result.NotFound = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, GetResponseNames());
        return result;
    }

    internal void EncodeIdentityGetResponse(IdentityGetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.State, value.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        EncodeIdentityList(value.List, writer, "/list");
        writer.WriteMemberName(JmapProperties.NotFound);
        Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, GetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal IdentityChangesRequest DecodeIdentityChangesRequest(JsonElement value)
    {
        JmapChangesRequest common = Standard.DecodeChangesRequest(value);
        IdentityChangesRequest result = new IdentityChangesRequest();
        result.AccountId = common.AccountId;
        result.SinceState = common.SinceState;
        result.MaxChanges = common.MaxChanges;
        result.HasMaxChanges = common.HasMaxChanges;
        result.IsMaxChangesNull = common.IsMaxChangesNull;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeIdentityChangesRequest(IdentityChangesRequest value, JmapJsonWriter writer)
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

    internal IdentityChangesResponse DecodeIdentityChangesResponse(JsonElement value)
    {
        JmapChangesResponse common = Standard.DecodeChangesResponse(value);
        IdentityChangesResponse result = new IdentityChangesResponse();
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

    internal void EncodeIdentityChangesResponse(IdentityChangesResponse value, JmapJsonWriter writer)
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

    internal IdentitySetRequest DecodeIdentitySetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        IdentitySetRequest result = new IdentitySetRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.IfInState = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.IfInState), "/ifInState");
        result.Create = DecodeOptionalIdentityMap(Values.GetMember(value, JmapProperties.Create), "/create");
        result.Update = Standard.DecodeOptionalPatchMap(Values.GetMember(value, JmapProperties.Update), "/update");
        result.Destroy = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroy), "/destroy");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.SetRequestNames());
        return result;
    }

    internal void EncodeIdentitySetRequest(IdentitySetRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.IfInState, value.IfInState, false);
        WriteOptionalIdentityCreateMap(JmapProperties.Create, value.Create, writer, "/create");
        Standard.WriteOptionalPatchMap(JmapProperties.Update, value.Update, writer, "/update");
        Optional.WriteOptionalStringArray(JmapProperties.Destroy, value.Destroy, writer, "/destroy");
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.SetRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal IdentitySetResponse DecodeIdentitySetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        IdentitySetResponse result = new IdentitySetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldState = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.OldState), "/oldState");
        result.NewState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewState), "/newState");
        result.Created = DecodeOptionalIdentityMap(Values.GetMember(value, JmapProperties.Created), "/created");
        result.Updated = Standard.DecodeOptionalElementMap(Values.GetMember(value, JmapProperties.Updated), "/updated");
        result.Destroyed = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroyed), "/destroyed");
        result.NotCreated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCreated), "/notCreated");
        result.NotUpdated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotUpdated), "/notUpdated");
        result.NotDestroyed = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotDestroyed), "/notDestroyed");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.SetResponseNames());
        return result;
    }

    internal void EncodeIdentitySetResponse(IdentitySetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.OldState, value.OldState, true);
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        WriteOptionalIdentityMap(JmapProperties.Created, value.Created, writer, "/created");
        Standard.WriteOptionalElementMap(JmapProperties.Updated, value.Updated, writer, "/updated");
        Optional.WriteOptionalStringArray(JmapProperties.Destroyed, value.Destroyed, writer, "/destroyed");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotCreated, value.NotCreated, writer, "/notCreated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotUpdated, value.NotUpdated, writer, "/notUpdated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotDestroyed, value.NotDestroyed, writer, "/notDestroyed");
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.SetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal List<Identity> DecodeIdentityList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An Identity array is required.", path, Values.CloneContext(value));
        }

        List<Identity> result = new List<Identity>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodeIdentity(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeIdentityList(List<Identity> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Identity array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (Identity item in value)
        {
            EncodeIdentity(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal Dictionary<string, Identity>? DecodeOptionalIdentityMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, Identity> result = new Dictionary<string, Identity>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeIdentity(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void WriteOptionalIdentityMap(string name, Dictionary<string, Identity>? value, JmapJsonWriter writer, string path)
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
            EncodeIdentity(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal void WriteOptionalIdentityCreateMap(string name, Dictionary<string, Identity>? value, JmapJsonWriter writer, string path)
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
            EncodeIdentityCreate(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal List<string> IdentityNames()
    {
        return new List<string> { JmapProperties.Id, JmapProperties.Name, JmapProperties.Email, JmapProperties.ReplyTo, JmapProperties.Bcc, JmapProperties.TextSignature, JmapProperties.HtmlSignature, JmapProperties.MayDelete };
    }

    internal List<string> GetResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound };
    }
}
