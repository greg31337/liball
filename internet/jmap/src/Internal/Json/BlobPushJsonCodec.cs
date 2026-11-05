using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace jmap;

internal class BlobPushJsonCodec
{
    internal JmapJsonReader Reader;
    internal StandardMethodJsonCodec Standard;
    internal SessionJsonCodec Values;
    internal ProtocolJsonCodec Optional;

    internal BlobPushJsonCodec(JmapJsonReader reader)
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

    internal BlobUploadResponse DecodeBlobUploadResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        BlobUploadResponse result = new BlobUploadResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.BlobId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.BlobId), "/blobId");
        result.Type = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Type), "/type");
        result.Size = Values.ReadRequiredUnsignedInteger(Values.GetMember(value, JmapProperties.Size), "/size");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, BlobUploadNames());
        return result;
    }

    internal void EncodeBlobUploadResponse(BlobUploadResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.BlobId, value.BlobId, "/blobId");
        Values.WriteRequiredStringMember(writer, JmapProperties.Type, value.Type, "/type");
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.Size, value.Size, "/size");
        Values.WriteUnknownMembers(value.UnknownProperties, BlobUploadNames(), writer);
        writer.WriteObjectEnd();
    }

    internal BlobCopyRequest DecodeBlobCopyRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        BlobCopyRequest result = new BlobCopyRequest();
        result.FromAccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.FromAccountId), "/fromAccountId");
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.BlobIds = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.BlobIds), "/blobIds");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, BlobCopyRequestNames());
        return result;
    }

    internal void EncodeBlobCopyRequest(BlobCopyRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.FromAccountId, value.FromAccountId, "/fromAccountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        writer.WriteMemberName(JmapProperties.BlobIds);
        Values.EncodeStringArray(value.BlobIds, writer, "/blobIds");
        Values.WriteUnknownMembers(value.UnknownProperties, BlobCopyRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal BlobCopyResponse DecodeBlobCopyResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        BlobCopyResponse result = new BlobCopyResponse();
        result.FromAccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.FromAccountId), "/fromAccountId");
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.Copied = DecodeOptionalStringMap(Values.GetMember(value, JmapProperties.Copied), "/copied");
        result.NotCopied = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCopied), "/notCopied");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, BlobCopyResponseNames());
        return result;
    }

    internal void EncodeBlobCopyResponse(BlobCopyResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.FromAccountId, value.FromAccountId, "/fromAccountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        WriteOptionalStringMap(JmapProperties.Copied, value.Copied, writer, "/copied");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotCopied, value.NotCopied, writer, "/notCopied");
        Values.WriteUnknownMembers(value.UnknownProperties, BlobCopyResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal StateChange DecodeStateChange(JsonElement value)
    {
        Values.RequireObject(value, "");
        StateChange result = new StateChange();
        result.Type = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AtType), "/@type");
        result.Changed = DecodeChangedMap(Values.GetMember(value, JmapProperties.Changed), "/changed");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, StateChangeNames());
        return result;
    }

    internal void EncodeStateChange(StateChange value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AtType, value.Type, "/@type");
        writer.WriteMemberName(JmapProperties.Changed);
        EncodeChangedMap(value.Changed, writer, "/changed");
        Values.WriteUnknownMembers(value.UnknownProperties, StateChangeNames(), writer);
        writer.WriteObjectEnd();
    }

    internal PushSubscriptionKeys DecodePushKeys(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        PushSubscriptionKeys result = new PushSubscriptionKeys();
        result.P256dh = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.P256Dh), path + "/p256dh");
        result.Auth = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Auth), path + "/auth");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, PushKeyNames());
        return result;
    }

    internal void EncodePushKeys(PushSubscriptionKeys value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("PushSubscription keys cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.P256Dh, value.P256dh, path + "/p256dh");
        Values.WriteRequiredStringMember(writer, JmapProperties.Auth, value.Auth, path + "/auth");
        Values.WriteUnknownMembers(value.UnknownProperties, PushKeyNames(), writer);
        writer.WriteObjectEnd();
    }

    internal PushSubscription DecodePushSubscription(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        PushSubscription result = new PushSubscription();
        result.Id = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Id), path + "/id");
        result.DeviceClientId = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.DeviceClientId), path + "/deviceClientId");
        result.Url = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Url), path + "/url");
        JsonElement keys = Values.GetMember(value, JmapProperties.Keys);
        if (keys.ValueKind != JsonValueKind.Undefined && keys.ValueKind != JsonValueKind.Null)
        {
            result.Keys = DecodePushKeys(keys, path + "/keys");
        }

        result.VerificationCode = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.VerificationCode), path + "/verificationCode");
        result.Expires = Optional.ReadOptionalString(Values.GetMember(value, JmapProperties.Expires), path + "/expires");
        result.Types = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Types), path + "/types");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, PushSubscriptionNames());
        return result;
    }

    internal void EncodePushSubscription(PushSubscription value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A PushSubscription cannot be null.", path);
        }

        writer.WriteObjectStart();
        writer.WriteStringMember(JmapProperties.Id, value.Id, false);
        writer.WriteStringMember(JmapProperties.DeviceClientId, value.DeviceClientId, false);
        writer.WriteStringMember(JmapProperties.Url, value.Url, false);
        if (value.Keys != null)
        {
            writer.WriteMemberName(JmapProperties.Keys);
            EncodePushKeys(value.Keys, writer, path + "/keys");
        }

        writer.WriteStringMember(JmapProperties.VerificationCode, value.VerificationCode, false);
        writer.WriteStringMember(JmapProperties.Expires, value.Expires, false);
        Optional.WriteOptionalStringArray(JmapProperties.Types, value.Types, writer, path + "/types");
        Values.WriteUnknownMembers(value.UnknownProperties, PushSubscriptionNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void EncodePushSubscriptionCreate(PushSubscription value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A PushSubscription create value cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.DeviceClientId, value.DeviceClientId, path + "/deviceClientId");
        Values.WriteRequiredStringMember(writer, JmapProperties.Url, value.Url, path + "/url");
        if (value.Keys != null)
        {
            writer.WriteMemberName(JmapProperties.Keys);
            EncodePushKeys(value.Keys, writer, path + "/keys");
        }

        writer.WriteStringMember(JmapProperties.Expires, value.Expires, false);
        Optional.WriteOptionalStringArray(JmapProperties.Types, value.Types, writer, path + "/types");
        Values.WriteUnknownMembers(value.UnknownProperties, PushSubscriptionNames(), writer);
        writer.WriteObjectEnd();
    }

    internal PushVerification DecodePushVerification(JsonElement value)
    {
        Values.RequireObject(value, "");
        PushVerification result = new PushVerification();
        result.Type = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AtType), "/@type");
        result.PushSubscriptionId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.PushSubscriptionId), "/pushSubscriptionId");
        result.VerificationCode = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.VerificationCode), "/verificationCode");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, PushVerificationNames());
        return result;
    }

    internal void EncodePushVerification(PushVerification value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AtType, value.Type, "/@type");
        Values.WriteRequiredStringMember(writer, JmapProperties.PushSubscriptionId, value.PushSubscriptionId, "/pushSubscriptionId");
        Values.WriteRequiredStringMember(writer, JmapProperties.VerificationCode, value.VerificationCode, "/verificationCode");
        Values.WriteUnknownMembers(value.UnknownProperties, PushVerificationNames(), writer);
        writer.WriteObjectEnd();
    }

    internal PushSubscriptionGetRequest DecodePushGetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        PushSubscriptionGetRequest result = new PushSubscriptionGetRequest();
        result.Ids = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Ids), "/ids");
        result.Properties = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Properties), "/properties");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, PushGetRequestNames());
        return result;
    }

    internal void EncodePushGetRequest(PushSubscriptionGetRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Optional.WriteOptionalStringArray(JmapProperties.Ids, value.Ids, writer, "/ids");
        Optional.WriteOptionalStringArray(JmapProperties.Properties, value.Properties, writer, "/properties");
        Values.WriteUnknownMembers(value.UnknownProperties, PushGetRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal PushSubscriptionGetResponse DecodePushGetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        PushSubscriptionGetResponse result = new PushSubscriptionGetResponse();
        result.List = DecodePushList(Values.GetMember(value, JmapProperties.List), "/list");
        result.NotFound = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, PushGetResponseNames());
        return result;
    }

    internal void EncodePushGetResponse(PushSubscriptionGetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        writer.WriteMemberName(JmapProperties.List);
        EncodePushList(value.List, writer, "/list");
        writer.WriteMemberName(JmapProperties.NotFound);
        Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, PushGetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal PushSubscriptionSetRequest DecodePushSetRequest(JsonElement value)
    {
        Values.RequireObject(value, "");
        PushSubscriptionSetRequest result = new PushSubscriptionSetRequest();
        result.Create = DecodeOptionalPushMap(Values.GetMember(value, JmapProperties.Create), "/create");
        result.Update = Standard.DecodeOptionalPatchMap(Values.GetMember(value, JmapProperties.Update), "/update");
        result.Destroy = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroy), "/destroy");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, PushSetRequestNames());
        return result;
    }

    internal void EncodePushSetRequest(PushSubscriptionSetRequest value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        WriteOptionalPushCreateMap(JmapProperties.Create, value.Create, writer, "/create");
        Standard.WriteOptionalPatchMap(JmapProperties.Update, value.Update, writer, "/update");
        Optional.WriteOptionalStringArray(JmapProperties.Destroy, value.Destroy, writer, "/destroy");
        Values.WriteUnknownMembers(value.UnknownProperties, PushSetRequestNames(), writer);
        writer.WriteObjectEnd();
    }

    internal PushSubscriptionSetResponse DecodePushSetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        PushSubscriptionSetResponse result = new PushSubscriptionSetResponse();
        result.Created = DecodeOptionalPushMap(Values.GetMember(value, JmapProperties.Created), "/created");
        result.Updated = Standard.DecodeOptionalElementMap(Values.GetMember(value, JmapProperties.Updated), "/updated");
        result.Destroyed = Optional.ReadOptionalStringArray(Values.GetMember(value, JmapProperties.Destroyed), "/destroyed");
        result.NotCreated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotCreated), "/notCreated");
        result.NotUpdated = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotUpdated), "/notUpdated");
        result.NotDestroyed = Standard.DecodeOptionalSetErrorMap(Values.GetMember(value, JmapProperties.NotDestroyed), "/notDestroyed");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, PushSetResponseNames());
        return result;
    }

    internal void EncodePushSetResponse(PushSubscriptionSetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        WriteOptionalPushMap(JmapProperties.Created, value.Created, writer, "/created");
        Standard.WriteOptionalElementMap(JmapProperties.Updated, value.Updated, writer, "/updated");
        Optional.WriteOptionalStringArray(JmapProperties.Destroyed, value.Destroyed, writer, "/destroyed");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotCreated, value.NotCreated, writer, "/notCreated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotUpdated, value.NotUpdated, writer, "/notUpdated");
        Standard.WriteOptionalSetErrorMap(JmapProperties.NotDestroyed, value.NotDestroyed, writer, "/notDestroyed");
        Values.WriteUnknownMembers(value.UnknownProperties, PushSetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapEvent DecodeEventRecord(string? name, string? id, string? data, string? comment, long retryMilliseconds, bool hasRetryMilliseconds)
    {
        JmapEvent result = new JmapEvent();
        result.Name = name;
        result.Id = id;
        result.Data = data;
        result.Comment = comment;
        result.RetryMilliseconds = retryMilliseconds;
        result.HasRetryMilliseconds = hasRetryMilliseconds;
        if (comment != null && name == null)
        {
            result.Kind = JmapEventKind.Comment;
            return result;
        }

        if (name == null)
        {
            result.Kind = JmapEventKind.Unknown;
            return result;
        }

        result.Kind = ClassifyEventName(name);
        if (result.Kind == JmapEventKind.State && data != null)
        {
            result.StateChange = DecodeStateEventData(data);
        }
        else if (result.Kind == JmapEventKind.Ping && data != null)
        {
            result.PingIntervalSeconds = DecodePingEventData(data);
            result.HasPingIntervalSeconds = true;
        }

        return result;
    }

    internal StateChange DecodeStateEventData(string data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JsonElement value = Reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(data));
        return DecodeStateChange(value);
    }

    internal string EncodeStateEventData(StateChange value)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        EncodeStateChange(value, writer);
        writer.DisposeWriter();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal long DecodePingEventData(string data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JsonElement value = Reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(data));
        Values.RequireObject(value, "");
        return Values.ReadRequiredUnsignedInteger(Values.GetMember(value, JmapProperties.Interval), "/interval");
    }

    internal string EncodePingEventData(long interval)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        writer.WriteObjectStart();
        Values.WriteUnsignedIntegerMember(writer, JmapProperties.Interval, interval, "/interval");
        writer.WriteObjectEnd();
        writer.DisposeWriter();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal JmapEventKind ClassifyEventName(string name)
    {
        switch (name)
        {
            case "state":
                return JmapEventKind.State;
            case "ping":
                return JmapEventKind.Ping;
            default:
                return JmapEventKind.Unknown;
        }
    }

    internal Dictionary<string, string>? DecodeOptionalStringMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return DecodeStringMap(value, path);
    }

    internal Dictionary<string, string> DecodeStringMap(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Dictionary<string, string> result = new Dictionary<string, string>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, Values.ReadRequiredString(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void WriteOptionalStringMap(string name, Dictionary<string, string>? value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteMemberName(name);
        EncodeStringMap(value, writer, path);
    }

    internal void EncodeStringMap(Dictionary<string, string> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A string map cannot be null.", path);
        }

        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            Values.WriteRequiredStringMember(writer, key, value[key], path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal Dictionary<string, Dictionary<string, string>> DecodeChangedMap(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Dictionary<string, Dictionary<string, string>> result = new Dictionary<string, Dictionary<string, string>>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeStringMap(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void EncodeChangedMap(Dictionary<string, Dictionary<string, string>> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A StateChange changed map cannot be null.", path);
        }

        writer.WriteObjectStart();
        foreach (string key in Values.SortNames(value.Keys))
        {
            writer.WriteMemberName(key);
            EncodeStringMap(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal List<PushSubscription> DecodePushList(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("A PushSubscription array is required.", path, Values.CloneContext(value));
        }

        List<PushSubscription> result = new List<PushSubscription>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(DecodePushSubscription(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodePushList(List<PushSubscription> value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A PushSubscription array cannot be null.", path);
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (PushSubscription item in value)
        {
            EncodePushSubscription(item, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal Dictionary<string, PushSubscription>? DecodeOptionalPushMap(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Values.RequireObject(value, path);
        Dictionary<string, PushSubscription> result = new Dictionary<string, PushSubscription>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodePushSubscription(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void WriteOptionalPushMap(string name, Dictionary<string, PushSubscription>? value, JmapJsonWriter writer, string path)
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
            EncodePushSubscription(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal void WriteOptionalPushCreateMap(string name, Dictionary<string, PushSubscription>? value, JmapJsonWriter writer, string path)
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
            EncodePushSubscriptionCreate(value[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal List<string> BlobUploadNames()
    {
        return new List<string> { JmapProperties.AccountId, JmapProperties.BlobId, JmapProperties.Type, JmapProperties.Size };
    }

    internal List<string> BlobCopyRequestNames()
    {
        return new List<string> { JmapProperties.FromAccountId, JmapProperties.AccountId, JmapProperties.BlobIds };
    }

    internal List<string> BlobCopyResponseNames()
    {
        return new List<string> { JmapProperties.FromAccountId, JmapProperties.AccountId, JmapProperties.Copied, JmapProperties.NotCopied };
    }

    internal List<string> StateChangeNames()
    {
        return new List<string> { JmapProperties.AtType, JmapProperties.Changed };
    }

    internal List<string> PushKeyNames()
    {
        return new List<string> { JmapProperties.P256Dh, JmapProperties.Auth };
    }

    internal List<string> PushSubscriptionNames()
    {
        return new List<string> { JmapProperties.Id, JmapProperties.DeviceClientId, JmapProperties.Url, JmapProperties.Keys, JmapProperties.VerificationCode, JmapProperties.Expires, JmapProperties.Types };
    }

    internal List<string> PushVerificationNames()
    {
        return new List<string> { JmapProperties.AtType, JmapProperties.PushSubscriptionId, JmapProperties.VerificationCode };
    }

    internal List<string> PushGetRequestNames()
    {
        return new List<string> { JmapProperties.Ids, JmapProperties.Properties };
    }

    internal List<string> PushGetResponseNames()
    {
        return new List<string> { JmapProperties.List, JmapProperties.NotFound };
    }

    internal List<string> PushSetRequestNames()
    {
        return new List<string> { JmapProperties.Create, JmapProperties.Update, JmapProperties.Destroy };
    }

    internal List<string> PushSetResponseNames()
    {
        return new List<string> { JmapProperties.Created, JmapProperties.Updated, JmapProperties.Destroyed, JmapProperties.NotCreated, JmapProperties.NotUpdated, JmapProperties.NotDestroyed };
    }
}
