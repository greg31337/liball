using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class ThreadJsonCodec
{
    internal StandardMethodJsonCodec Standard;
    internal SessionJsonCodec Values;
    internal ProtocolJsonCodec Errors;

    internal ThreadJsonCodec(JmapJsonReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        Standard = new StandardMethodJsonCodec(reader);
        Values = new SessionJsonCodec(reader);
        Errors = new ProtocolJsonCodec(reader);
    }

    internal Thread DecodeThread(JsonElement value, string path)
    {
        Values.RequireObject(value, path);
        Thread result = new Thread();
        result.Id = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Id), path + "/id");
        result.EmailIds = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.EmailIds), path + "/emailIds");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.Id, JmapProperties.EmailIds });
        return result;
    }

    internal void EncodeThread(Thread value, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A Thread cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Id, value.Id, path + "/id");
        writer.WriteMemberName(JmapProperties.EmailIds);
        Values.EncodeStringArray(value.EmailIds, writer, path + "/emailIds");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.Id, JmapProperties.EmailIds }, writer);
        writer.WriteObjectEnd();
    }

    internal ThreadGetRequest DecodeThreadGetRequest(JsonElement value)
    {
        JmapGetRequest common = Standard.DecodeGetRequest(value);
        ThreadGetRequest result = new ThreadGetRequest();
        result.AccountId = common.AccountId;
        result.Ids = common.Ids;
        result.Properties = common.Properties;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeThreadGetRequest(ThreadGetRequest value, JmapJsonWriter writer)
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

    internal ThreadGetResponse DecodeThreadGetResponse(JsonElement value)
    {
        Values.RequireObject(value, "");
        ThreadGetResponse result = new ThreadGetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.State = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.State), "/state");
        JsonElement list = Values.GetMember(value, JmapProperties.List);
        if (list.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("A Thread array is required.", "/list", Values.CloneContext(list));
        }

        int index = 0;
        foreach (JsonElement item in list.EnumerateArray())
        {
            result.List.Add(DecodeThread(item, "/list/" + index));
            index = index + 1;
        }

        result.NotFound = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound });
        return result;
    }

    internal void EncodeThreadGetResponse(ThreadGetResponse value, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.State, value.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        writer.WriteArrayStart();
        int index = 0;
        foreach (Thread item in value.List)
        {
            EncodeThread(item, writer, "/list/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
        writer.WriteMemberName(JmapProperties.NotFound);
        Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound }, writer);
        writer.WriteObjectEnd();
    }

    internal ThreadGetResponse DecodeProjectedThreadGetResponse(JsonElement value, List<string>? properties)
    {
        Values.RequireObject(value, "");
        ThreadGetResponse result = new ThreadGetResponse();
        result.AccountId = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.AccountId), "/accountId");
        result.State = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.State), "/state");
        JsonElement list = Values.GetMember(value, JmapProperties.List);
        if (list.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("A Thread array is required.", "/list", Values.CloneContext(list));
        }

        int index = 0;
        foreach (JsonElement item in list.EnumerateArray())
        {
            result.List.Add(DecodeProjectedThread(item, "/list/" + index, properties));
            index = index + 1;
        }

        result.NotFound = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.NotFound), "/notFound");
        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound });
        return result;
    }

    internal Thread DecodeProjectedThread(JsonElement value, string path, List<string>? properties)
    {
        Values.RequireObject(value, path);
        Thread result = new Thread();
        result.Id = Values.ReadRequiredString(Values.GetMember(value, JmapProperties.Id), path + "/id");
        bool includeEmailIds = properties == null || properties.Contains(JmapProperties.EmailIds);
        if (includeEmailIds)
        {
            result.EmailIds = Values.DecodeStringArray(Values.GetMember(value, JmapProperties.EmailIds), path + "/emailIds");
        }

        Values.CaptureUnknownMembers(value, result.UnknownProperties, new List<string> { JmapProperties.Id, JmapProperties.EmailIds });
        return result;
    }

    internal void EncodeProjectedThreadGetResponse(ThreadGetResponse value, List<string>? properties, JmapJsonWriter writer)
    {
        Standard.RequireEncodeInputs(value, writer);
        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, value.AccountId, "/accountId");
        Values.WriteRequiredStringMember(writer, JmapProperties.State, value.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        writer.WriteArrayStart();
        int index = 0;
        foreach (Thread item in value.List)
        {
            EncodeProjectedThread(item, properties, writer, "/list/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
        writer.WriteMemberName(JmapProperties.NotFound);
        Values.EncodeStringArray(value.NotFound, writer, "/notFound");
        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List, JmapProperties.NotFound }, writer);
        writer.WriteObjectEnd();
    }

    internal void EncodeProjectedThread(Thread value, List<string>? properties, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw Standard.Validation("A Thread cannot be null.", path);
        }

        writer.WriteObjectStart();
        Values.WriteRequiredStringMember(writer, JmapProperties.Id, value.Id, path + "/id");
        if (properties == null || properties.Contains(JmapProperties.EmailIds))
        {
            writer.WriteMemberName(JmapProperties.EmailIds);
            Values.EncodeStringArray(value.EmailIds, writer, path + "/emailIds");
        }

        Values.WriteUnknownMembers(value.UnknownProperties, new List<string> { JmapProperties.Id, JmapProperties.EmailIds }, writer);
        writer.WriteObjectEnd();
    }

    internal ThreadChangesRequest DecodeThreadChangesRequest(JsonElement value)
    {
        JmapChangesRequest common = Standard.DecodeChangesRequest(value);
        ThreadChangesRequest result = new ThreadChangesRequest();
        result.AccountId = common.AccountId;
        result.SinceState = common.SinceState;
        result.MaxChanges = common.MaxChanges;
        result.HasMaxChanges = common.HasMaxChanges;
        result.IsMaxChangesNull = common.IsMaxChangesNull;
        result.UnknownProperties = common.UnknownProperties;
        return result;
    }

    internal void EncodeThreadChangesRequest(ThreadChangesRequest value, JmapJsonWriter writer)
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

    internal ThreadChangesResponse DecodeThreadChangesResponse(JsonElement value)
    {
        JmapChangesResponse common = Standard.DecodeChangesResponse(value);
        ThreadChangesResponse result = new ThreadChangesResponse();
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

    internal void EncodeThreadChangesResponse(ThreadChangesResponse value, JmapJsonWriter writer)
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
}
