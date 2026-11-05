using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class EmailMethodJsonCodec
{
    internal EmailJsonCodec Emails;
    internal StandardMethodJsonCodec Standard;
    internal SessionJsonCodec Values;
    internal ProtocolJsonCodec Optional;

    internal EmailMethodJsonCodec(JmapJsonReader reader)
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

    internal EmailGetRequest DecodeEmailGetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailGetRequest result = new EmailGetRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.Ids = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Ids), "/ids");
        result.Properties = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Properties), "/properties");
        result.BodyProperties = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.BodyProperties), "/bodyProperties");
        ReadEmailGetOptions(value, result);
        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailGetRequestNames());
        return result;
    }

    internal void EncodeEmailGetRequest(EmailGetRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Optional.WriteOptionalStringArray(JmapProperties.Ids, value.Ids, writer, "/ids");
        Optional.WriteOptionalStringArray(JmapProperties.Properties, value.Properties, writer, "/properties");
        Optional.WriteOptionalStringArray(JmapProperties.BodyProperties, value.BodyProperties, writer, "/bodyProperties");
        WriteEmailGetOptions(value, writer);
        Values.WriteUnknownMembers(value.UnknownProperties, EmailGetRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailGetResponse DecodeEmailGetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailGetResponse result = new EmailGetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.State = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.State), "/state");
        result.List = DecodeEmailList(Values.GetMember(value, JmapProperties.List), "/list");
        result.NotFound = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, GetResponseNames());
        return result;
    }

    internal void EncodeEmailGetResponse(EmailGetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.State, value.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        EncodeEmailList(value.List, writer, "/list");
        writer.WriteMemberName(JmapProperties.NotFound);
        Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, GetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailChangesRequest DecodeEmailChangesRequest(JsonElement value)
    {
        JmapChangesRequest common = Standard.DecodeChangesRequest(value);
        EmailChangesRequest result = new EmailChangesRequest();
        result.AccountId = common.AccountId;
        result.SinceState = common.SinceState;
        result.MaxChanges = common.MaxChanges;
        result.HasMaxChanges = common.HasMaxChanges;
        result.IsMaxChangesNull = common.IsMaxChangesNull;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeEmailChangesRequest(EmailChangesRequest value, JmapJsonWriter writer)
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

    internal EmailChangesResponse DecodeEmailChangesResponse(JsonElement value)
    {
        JmapChangesResponse common = Standard.DecodeChangesResponse(value);
        EmailChangesResponse result = new EmailChangesResponse();
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

    internal void EncodeEmailChangesResponse(EmailChangesResponse value, JmapJsonWriter writer)
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

    internal EmailSetRequest DecodeEmailSetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailSetRequest result = new EmailSetRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.IfInState = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.IfInState), "/ifInState");
        result.Create = Emails.DecodeOptionalEmailCreateMap(Values.GetMember(value, JmapProperties.Create), "/create");
        result.Update = Standard.DecodeOptionalPatchMap(Values.GetMember(value, JmapProperties.Update), "/update");
        result.Destroy = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroy), "/destroy");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.SetRequestNames());
        return result;
    }

    internal void EncodeEmailSetRequest(EmailSetRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.IfInState, value.IfInState, false);
        Emails.WriteOptionalEmailCreateMap(JmapProperties.Create, value.Create, writer, "/create");
        Standard.WriteOptionalPatchMap(JmapProperties.Update, value.Update, writer, "/update");
        Optional.WriteOptionalStringArray(JmapProperties.Destroy, value.Destroy, writer, "/destroy");
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.SetRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailSetResponse DecodeEmailSetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailSetResponse result = new EmailSetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldState = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.OldState), "/oldState");
        result.NewState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewState), "/newState");
        result.Created = Emails.DecodeOptionalEmailMap(Values.GetMember(value, JmapProperties.Created), "/created");
        result.Updated = Standard.DecodeOptionalElementMap(Values.GetMember(value, JmapProperties.Updated), "/updated");
        result.Destroyed = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroyed), "/destroyed");
        result.NotCreated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCreated), "/notCreated");
        result.NotUpdated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotUpdated), "/notUpdated");
        result.NotDestroyed = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotDestroyed), "/notDestroyed");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.SetResponseNames());
        return result;
    }

    internal void EncodeEmailSetResponse(EmailSetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.OldState, value.OldState, true);
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        Emails.WriteOptionalEmailSetCreatedMap(JmapProperties.Created, value.Created, writer, "/created");
        Standard.WriteOptionalElementMap(JmapProperties.Updated, value.Updated, writer, "/updated");
        Optional.WriteOptionalStringArray(JmapProperties.Destroyed, value.Destroyed, writer, "/destroyed");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotCreated, value.NotCreated, writer, "/notCreated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotUpdated, value.NotUpdated, writer, "/notUpdated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotDestroyed, value.NotDestroyed, writer, "/notDestroyed");
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.SetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailCopyRequest DecodeEmailCopyRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailCopyRequest result = new EmailCopyRequest();
        result.FromAccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.FromAccountId), "/fromAccountId");
        result.IfFromInState = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.IfFromInState), "/ifFromInState");
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.IfInState = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.IfInState), "/ifInState");
        result.Create = Emails.DecodeEmailMap(Values.GetMember(value, JmapProperties.Create), "/create");
        JsonElement destroyOriginal = Values.GetMember(value, JmapProperties.OnSuccessDestroyOriginal);
        if (destroyOriginal.ValueKind != JsonValueKind.Undefined)
        {
            result.OnSuccessDestroyOriginal = Values.ReadRequiredBoolean(destroyOriginal, "/onSuccessDestroyOriginal");
            result.HasOnSuccessDestroyOriginal = true;
        }

        result.DestroyFromIfInState = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.DestroyFromIfInState), "/destroyFromIfInState");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.CopyRequestNames());
        return result;
    }

    internal void EncodeEmailCopyRequest(EmailCopyRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.FromAccountId, value.FromAccountId, "/fromAccountId");
        writer.WriteStringMember(JmapProperties.IfFromInState, value.IfFromInState, false);
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.IfInState, value.IfInState, false);
        writer.WriteMemberName(JmapProperties.Create);
        Emails.EncodeEmailCopyMap(value.Create, writer, "/create");
        if (value.HasOnSuccessDestroyOriginal)
        {
            writer.WriteBooleanMember(JmapProperties.OnSuccessDestroyOriginal, value.OnSuccessDestroyOriginal, true);
        }

        writer.WriteStringMember(JmapProperties.DestroyFromIfInState, value.DestroyFromIfInState, false);
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.CopyRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailCopyResponse DecodeEmailCopyResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailCopyResponse result = new EmailCopyResponse();
        result.FromAccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.FromAccountId), "/fromAccountId");
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldState = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.OldState), "/oldState");
        result.NewState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewState), "/newState");
        result.Created = Emails.DecodeOptionalEmailMap(Values.GetMember(value, JmapProperties.Created), "/created");
        result.NotCreated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCreated), "/notCreated");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, Standard.CopyResponseNames());
        return result;
    }

    internal void EncodeEmailCopyResponse(EmailCopyResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.FromAccountId, value.FromAccountId, "/fromAccountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.OldState, value.OldState, true);
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        Emails.WriteOptionalEmailSetCreatedMap(JmapProperties.Created, value.Created, writer, "/created");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotCreated, value.NotCreated, writer, "/notCreated");
        Values.WriteUnknownMembers(value.UnknownProperties, Standard.CopyResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailImport DecodeEmailImport(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        EmailImport result = new EmailImport();
        result.BlobId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.BlobId), path + "/blobId");
        result.MailboxIds = Emails.DecodeBooleanMap(Values.GetMember(value, JmapProperties.MailboxIds), path + "/mailboxIds");
        JsonElement keywords = Values.GetMember(value, JmapProperties.Keywords);
        if (keywords.ValueKind != JsonValueKind.Undefined)
        {
            result.Keywords = Emails.DecodeBooleanMap(keywords, path + "/keywords");
        }

        result.ReceivedAt = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.ReceivedAt), path + "/receivedAt");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailImportNames());
        return result;
    }

    internal void EncodeEmailImport(EmailImport value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email import value cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.BlobId, value.BlobId, path + "/blobId");
        writer.WriteMemberName(JmapProperties.MailboxIds);
        Emails.EncodeBooleanMap(value.MailboxIds, writer, path + "/mailboxIds");
        writer.WriteMemberName(JmapProperties.Keywords);
        Emails.EncodeBooleanMap(value.Keywords, writer, path + "/keywords");
        writer.WriteStringMember(JmapProperties.ReceivedAt, value.ReceivedAt, false);
        Values.WriteUnknownMembers(value.UnknownProperties, EmailImportNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailImportRequest DecodeEmailImportRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailImportRequest result = new EmailImportRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.IfInState = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.IfInState), "/ifInState");
        result.Emails = DecodeImportMap(Values.GetMember(value, JmapProperties.Emails), "/emails");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailImportRequestNames());
        return result;
    }

    internal EmailImportRequest DecodeEmailImportDispatchRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailImportRequest result = new EmailImportRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.IfInState = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.IfInState), "/ifInState");
        JsonElement emails = Values.GetMember(value, JmapProperties.Emails);
        Values.RequireObject(emails, "/emails");
        foreach (JsonProperty property in emails.EnumerateObject())
        {
            result.Emails.Add(property.Name, DecodeEmailImportDispatchValue(property.Value,
                "/emails/" + property.Name));
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailImportRequestNames());
        return result;
    }

    internal EmailImport DecodeEmailImportDispatchValue(JsonElement value, string path)
    {
        EmailImport result = new EmailImport();
        if (value.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        JsonElement blobId = Values.GetMember(value, JmapProperties.BlobId);
        if (blobId.ValueKind == JsonValueKind.String)
        {
            result.BlobId = blobId.GetString();
        }

        DecodeImportBooleanMapForDispatch(
            Values.GetMember(value, JmapProperties.MailboxIds), result.MailboxIds, true);
        DecodeImportBooleanMapForDispatch(
            Values.GetMember(value, JmapProperties.Keywords), result.Keywords, false);
        JsonElement receivedAt = Values.GetMember(value, JmapProperties.ReceivedAt);
        if (receivedAt.ValueKind == JsonValueKind.String)
        {
            result.ReceivedAt = receivedAt.GetString();
        }
        else if (receivedAt.ValueKind != JsonValueKind.Undefined && receivedAt.ValueKind != JsonValueKind.Null)
        {
            result.ReceivedAt = "invalid";
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailImportNames());
        return result;
    }

    internal void DecodeImportBooleanMapForDispatch(
        JsonElement value,
        Dictionary<string, bool> destination,
        bool required)
    {
        if (value.ValueKind == JsonValueKind.Undefined && !required)
        {
            return;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            destination.Add("", false);
            return;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            destination.Add(property.Name, property.Value.ValueKind == JsonValueKind.True);
        }
    }

    internal void EncodeEmailImportRequest(EmailImportRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.IfInState, value.IfInState, false);
        writer.WriteMemberName(JmapProperties.Emails);
        EncodeImportMap(value.Emails, writer, "/emails");
        Values.WriteUnknownMembers(value.UnknownProperties, EmailImportRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailImportResponse DecodeEmailImportResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailImportResponse result = new EmailImportResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.OldState = Standard.ReadRequiredNullableString(Values.GetMember(value, JmapProperties.OldState), "/oldState");
        result.NewState = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.NewState), "/newState");
        result.Created = Emails.DecodeOptionalEmailMap(Values.GetMember(value, JmapProperties.Created), "/created");
        result.NotCreated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCreated), "/notCreated");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailImportResponseNames());
        return result;
    }

    internal void EncodeEmailImportResponse(EmailImportResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteStringMember(JmapProperties.OldState, value.OldState, true);
        Values.WriteRequiredStringMember(writer, JmapProperties.NewState, value.NewState, "/newState");
        Emails.WriteOptionalEmailSetCreatedMap(JmapProperties.Created, value.Created, writer, "/created");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotCreated, value.NotCreated, writer, "/notCreated");
        Values.WriteUnknownMembers(value.UnknownProperties, EmailImportResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailParseRequest DecodeEmailParseRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailParseRequest result = new EmailParseRequest();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.BlobIds = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.BlobIds), "/blobIds");
        result.Properties = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Properties), "/properties");
        result.BodyProperties = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.BodyProperties), "/bodyProperties");
        ReadEmailParseOptions(value, result);
        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailParseRequestNames());
        return result;
    }

    internal void EncodeEmailParseRequest(EmailParseRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteMemberName(JmapProperties.BlobIds);
        Values.EncodeStringArray(value.BlobIds, writer, "/blobIds");
        Optional.WriteOptionalStringArray(JmapProperties.Properties, value.Properties, writer, "/properties");
        Optional.WriteOptionalStringArray(JmapProperties.BodyProperties, value.BodyProperties, writer, "/bodyProperties");
        WriteEmailParseOptions(value, writer);
        Values.WriteUnknownMembers(value.UnknownProperties, EmailParseRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal EmailParseResponse DecodeEmailParseResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        EmailParseResponse result = new EmailParseResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.Parsed = Emails.DecodeOptionalEmailMap(Values.GetMember(value, JmapProperties.Parsed), "/parsed");
        result.NotParsable = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.NotParsable), "/notParsable");
        result.NotFound = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, EmailParseResponseNames());
        return result;
    }

    internal void EncodeEmailParseResponse(EmailParseResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Emails.WriteOptionalEmailMap(JmapProperties.Parsed, value.Parsed, writer, "/parsed");
        Optional.WriteOptionalStringArray(JmapProperties.NotParsable, value.NotParsable, writer, "/notParsable");
        Optional.WriteOptionalStringArray(JmapProperties.NotFound, value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, EmailParseResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal List<Email> DecodeEmailList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("An Email array is required.", path, Values.CloneContext(value));
        }

        List<Email> result = new List<Email>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(Emails.DecodeEmail(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeEmailList(List<Email> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (Email item in value)
        {
            Emails.EncodeEmail(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal Dictionary<string, EmailImport> DecodeImportMap(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Dictionary<string, EmailImport> result = new Dictionary<string, EmailImport>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeEmailImport(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void EncodeImportMap(Dictionary<string, EmailImport> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("An Email import map cannot be null.", path);
        }

        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeEmailImport(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal void ReadEmailGetOptions(JsonElement source, EmailGetRequest destination)
    {
        JsonElement text = Values.GetMember(source, JmapProperties.FetchTextBodyValues);
        if (text.ValueKind != JsonValueKind.Undefined)
        {
            destination.FetchTextBodyValues = Values.ReadRequiredBoolean(text, "/fetchTextBodyValues");
            destination.HasFetchTextBodyValues = true;
        }

        JsonElement html = Values.GetMember(source, JmapProperties.FetchHtmlBodyValues);
        if (html.ValueKind != JsonValueKind.Undefined)
        {
            destination.FetchHtmlBodyValues = Values.ReadRequiredBoolean(html, "/fetchHTMLBodyValues");
            destination.HasFetchHtmlBodyValues = true;
        }

        JsonElement all = Values.GetMember(source, JmapProperties.FetchAllBodyValues);
        if (all.ValueKind != JsonValueKind.Undefined)
        {
            destination.FetchAllBodyValues = Values.ReadRequiredBoolean(all, "/fetchAllBodyValues");
            destination.HasFetchAllBodyValues = true;
        }

        JsonElement maximum = Values.GetMember(source, JmapProperties.MaxBodyValueBytes);
        if (maximum.ValueKind != JsonValueKind.Undefined)
        {
            destination.MaxBodyValueBytes = Values.ReadRequiredUnsignedInteger(maximum, "/maxBodyValueBytes");
            destination.HasMaxBodyValueBytes = true;
        }
    }

    internal void WriteEmailGetOptions(EmailGetRequest value, JmapJsonWriter writer)
    {
        if (value.HasFetchTextBodyValues)
        {
            writer.WriteBooleanMember(JmapProperties.FetchTextBodyValues, value.FetchTextBodyValues, true);
        }

        if (value.HasFetchHtmlBodyValues)
        {
            writer.WriteBooleanMember(JmapProperties.FetchHtmlBodyValues, value.FetchHtmlBodyValues, true);
        }

        if (value.HasFetchAllBodyValues)
        {
            writer.WriteBooleanMember(JmapProperties.FetchAllBodyValues, value.FetchAllBodyValues, true);
        }

        if (value.HasMaxBodyValueBytes)
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.MaxBodyValueBytes, value.MaxBodyValueBytes, "/maxBodyValueBytes");
        }
    }

    internal void ReadEmailParseOptions(JsonElement source, EmailParseRequest destination)
    {
        JsonElement text = Values.GetMember(source, JmapProperties.FetchTextBodyValues);
        if (text.ValueKind != JsonValueKind.Undefined)
        {
            destination.FetchTextBodyValues = Values.ReadRequiredBoolean(text, "/fetchTextBodyValues");
            destination.HasFetchTextBodyValues = true;
        }

        JsonElement html = Values.GetMember(source, JmapProperties.FetchHtmlBodyValues);
        if (html.ValueKind != JsonValueKind.Undefined)
        {
            destination.FetchHtmlBodyValues = Values.ReadRequiredBoolean(html, "/fetchHTMLBodyValues");
            destination.HasFetchHtmlBodyValues = true;
        }

        JsonElement all = Values.GetMember(source, JmapProperties.FetchAllBodyValues);
        if (all.ValueKind != JsonValueKind.Undefined)
        {
            destination.FetchAllBodyValues = Values.ReadRequiredBoolean(all, "/fetchAllBodyValues");
            destination.HasFetchAllBodyValues = true;
        }

        JsonElement maximum = Values.GetMember(source, JmapProperties.MaxBodyValueBytes);
        if (maximum.ValueKind != JsonValueKind.Undefined)
        {
            destination.MaxBodyValueBytes = Values.ReadRequiredUnsignedInteger(maximum, "/maxBodyValueBytes");
            destination.HasMaxBodyValueBytes = true;
        }
    }

    internal void WriteEmailParseOptions(EmailParseRequest value, JmapJsonWriter writer)
    {
        if (value.HasFetchTextBodyValues)
        {
            writer.WriteBooleanMember(JmapProperties.FetchTextBodyValues, value.FetchTextBodyValues, true);
        }

        if (value.HasFetchHtmlBodyValues)
        {
            writer.WriteBooleanMember(JmapProperties.FetchHtmlBodyValues, value.FetchHtmlBodyValues, true);
        }

        if (value.HasFetchAllBodyValues)
        {
            writer.WriteBooleanMember(JmapProperties.FetchAllBodyValues, value.FetchAllBodyValues, true);
        }

        if (value.HasMaxBodyValueBytes)
        {
            Values.WriteUnsignedIntegerMember(writer, JmapProperties.MaxBodyValueBytes, value.MaxBodyValueBytes, "/maxBodyValueBytes");
        }
    }

    internal List<string> EmailGetRequestNames()
    {
        return new List<string>
        {
            JmapProperties.AccountId, JmapProperties.Ids, JmapProperties.Properties, JmapProperties.BodyProperties,
            JmapProperties.FetchTextBodyValues, JmapProperties.FetchHtmlBodyValues, JmapProperties.FetchAllBodyValues,
            JmapProperties.MaxBodyValueBytes
        };
    }

    internal List<string> GetResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound };
    }

    internal List<string> EmailImportNames()
    {
        return new List<string> { JmapProperties.BlobId, JmapProperties.MailboxIds, JmapProperties.Keywords, JmapProperties.ReceivedAt };
    }

    internal List<string> EmailImportRequestNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.IfInState, JmapProperties.Emails };
    }

    internal List<string> EmailImportResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.OldState, JmapProperties.NewState, JmapProperties.Created, JmapProperties.NotCreated };
    }

    internal List<string> EmailParseRequestNames()
    {
        return new List<string>
        {
            JmapProperties.AccountId, JmapProperties.BlobIds, JmapProperties.Properties, JmapProperties.BodyProperties,
            JmapProperties.FetchTextBodyValues, JmapProperties.FetchHtmlBodyValues, JmapProperties.FetchAllBodyValues,
            JmapProperties.MaxBodyValueBytes
        };
    }

    internal List<string> EmailParseResponseNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.Parsed, JmapProperties.NotParsable, JmapProperties.NotFound };
    }
}
