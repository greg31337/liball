using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class SessionJsonCodec
{
    internal JmapJsonReader Reader;

    internal SessionJsonCodec(JmapJsonReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        Reader = reader;
    }

    internal JmapSession DecodeSession(JsonElement value)
    {
        RequireObject(value, "");

        JmapSession session = new JmapSession();
        session.Capabilities = DecodeServerCapabilities(GetMember(value, JmapProperties.Capabilities), "/capabilities");
        session.Accounts = DecodeAccounts(GetMember(value, JmapProperties.Accounts), "/accounts");
        session.PrimaryAccounts = DecodePrimaryAccounts(GetMember(value, JmapProperties.PrimaryAccounts), "/primaryAccounts");
        session.Username = ReadRequiredString(GetMember(value, JmapProperties.Username), "/username");
        session.ApiUrl = ReadRequiredString(GetMember(value, JmapProperties.ApiUrl), "/apiUrl");
        session.DownloadUrl = ReadRequiredString(GetMember(value, JmapProperties.DownloadUrl), "/downloadUrl");
        session.UploadUrl = ReadRequiredString(GetMember(value, JmapProperties.UploadUrl), "/uploadUrl");
        session.EventSourceUrl = ReadRequiredString(GetMember(value, JmapProperties.EventSourceUrl), "/eventSourceUrl");
        session.State = ReadRequiredString(GetMember(value, JmapProperties.State), "/state");

        CaptureUnknownMembers(
            value,
            session.UnknownProperties,
            new List<string>
            {
                JmapProperties.Capabilities,
                JmapProperties.Accounts,
                JmapProperties.PrimaryAccounts,
                JmapProperties.Username,
                JmapProperties.ApiUrl,
                JmapProperties.DownloadUrl,
                JmapProperties.UploadUrl,
                JmapProperties.EventSourceUrl,
                JmapProperties.State
            });

        return session;
    }

    internal void EncodeSession(JmapSession session, JmapJsonWriter writer)
    {
        if (session == null)
        {
            throw new System.ArgumentNullException("session");
        }

        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        writer.WriteObjectStart();
        writer.WriteMemberName(JmapProperties.Capabilities);
        EncodeServerCapabilities(session.Capabilities, writer);
        writer.WriteMemberName(JmapProperties.Accounts);
        EncodeAccounts(session.Accounts, writer);
        writer.WriteMemberName(JmapProperties.PrimaryAccounts);
        EncodePrimaryAccounts(session.PrimaryAccounts, writer);
        WriteRequiredStringMember(writer, JmapProperties.Username, session.Username, "/username");
        WriteRequiredStringMember(writer, JmapProperties.ApiUrl, session.ApiUrl, "/apiUrl");
        WriteRequiredStringMember(writer, JmapProperties.DownloadUrl, session.DownloadUrl, "/downloadUrl");
        WriteRequiredStringMember(writer, JmapProperties.UploadUrl, session.UploadUrl, "/uploadUrl");
        WriteRequiredStringMember(writer, JmapProperties.EventSourceUrl, session.EventSourceUrl, "/eventSourceUrl");
        WriteRequiredStringMember(writer, JmapProperties.State, session.State, "/state");
        WriteUnknownMembers(
            session.UnknownProperties,
            new List<string>
            {
                JmapProperties.Capabilities,
                JmapProperties.Accounts,
                JmapProperties.PrimaryAccounts,
                JmapProperties.Username,
                JmapProperties.ApiUrl,
                JmapProperties.DownloadUrl,
                JmapProperties.UploadUrl,
                JmapProperties.EventSourceUrl,
                JmapProperties.State
            },
            writer);
        writer.WriteObjectEnd();
    }

    internal Dictionary<string, JmapCapability> DecodeServerCapabilities(JsonElement value, string path)
    {
        RequireObject(value, path);
        Dictionary<string, JmapCapability> capabilities = new Dictionary<string, JmapCapability>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            capabilities.Add(property.Name, DecodeServerCapability(property.Name, property.Value, path + "/" + property.Name));
        }

        return capabilities;
    }

    internal void EncodeServerCapabilities(Dictionary<string, JmapCapability> capabilities, JmapJsonWriter writer)
    {
        if (capabilities == null)
        {
            throw new JmapValidationException("Session capabilities cannot be null.", new List<string> { JmapProperties.Capabilities }, default(JsonElement));
        }

        writer.WriteObjectStart();
        List<string> keys = SortNames(capabilities.Keys);
        foreach (string key in keys)
        {
            JmapCapability capability = capabilities[key];
            if (capability == null)
            {
                throw new JmapValidationException("A Session capability object cannot be null.", new List<string> { key }, default(JsonElement));
            }

            writer.WriteMemberName(key);
            EncodeServerCapability(key, capability, writer);
        }

        writer.WriteObjectEnd();
    }

    internal Dictionary<string, JmapAccount> DecodeAccounts(JsonElement value, string path)
    {
        RequireObject(value, path);
        Dictionary<string, JmapAccount> accounts = new Dictionary<string, JmapAccount>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            accounts.Add(property.Name, DecodeAccount(property.Value, path + "/" + property.Name));
        }

        return accounts;
    }

    internal void EncodeAccounts(Dictionary<string, JmapAccount> accounts, JmapJsonWriter writer)
    {
        if (accounts == null)
        {
            throw new JmapValidationException("Session accounts cannot be null.", new List<string> { JmapProperties.Accounts }, default(JsonElement));
        }

        writer.WriteObjectStart();
        List<string> keys = SortNames(accounts.Keys);
        foreach (string key in keys)
        {
            JmapAccount account = accounts[key];
            if (account == null)
            {
                throw new JmapValidationException("A Session account object cannot be null.", new List<string> { key }, default(JsonElement));
            }

            writer.WriteMemberName(key);
            EncodeAccount(account, writer, "/accounts/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal Dictionary<string, string> DecodePrimaryAccounts(JsonElement value, string path)
    {
        RequireObject(value, path);
        Dictionary<string, string> primaryAccounts = new Dictionary<string, string>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            primaryAccounts.Add(property.Name, ReadRequiredString(property.Value, path + "/" + property.Name));
        }

        return primaryAccounts;
    }

    internal void EncodePrimaryAccounts(Dictionary<string, string> primaryAccounts, JmapJsonWriter writer)
    {
        if (primaryAccounts == null)
        {
            throw new JmapValidationException("Primary accounts cannot be null.", new List<string> { JmapProperties.PrimaryAccounts }, default(JsonElement));
        }

        writer.WriteObjectStart();
        List<string> keys = SortNames(primaryAccounts.Keys);
        foreach (string key in keys)
        {
            string? accountId = primaryAccounts[key];
            WriteRequiredStringMember(writer, key, accountId, "/primaryAccounts/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal JmapAccount DecodeAccount(JsonElement value, string path)
    {
        RequireObject(value, path);
        JmapAccount account = new JmapAccount();
        account.Name = ReadRequiredString(GetMember(value, JmapProperties.Name), path + "/name");
        account.IsPersonal = ReadRequiredBoolean(GetMember(value, JmapProperties.IsPersonal), path + "/isPersonal");
        account.IsReadOnly = ReadRequiredBoolean(GetMember(value, JmapProperties.IsReadOnly), path + "/isReadOnly");
        account.AccountCapabilities = DecodeAccountCapabilities(GetMember(value, JmapProperties.AccountCapabilities), path + "/accountCapabilities");
        CaptureUnknownMembers(
            value,
            account.UnknownProperties,
            new List<string>
            {
                JmapProperties.Name,
                JmapProperties.IsPersonal,
                JmapProperties.IsReadOnly,
                JmapProperties.AccountCapabilities
            });
        return account;
    }

    internal void EncodeAccount(JmapAccount account, JmapJsonWriter writer, string path)
    {
        writer.WriteObjectStart();
        WriteRequiredStringMember(writer, JmapProperties.Name, account.Name, path + "/name");
        writer.WriteBooleanMember(JmapProperties.IsPersonal, account.IsPersonal, true);
        writer.WriteBooleanMember(JmapProperties.IsReadOnly, account.IsReadOnly, true);
        writer.WriteMemberName(JmapProperties.AccountCapabilities);
        EncodeAccountCapabilities(account.AccountCapabilities, writer, path + "/accountCapabilities");
        WriteUnknownMembers(
            account.UnknownProperties,
            new List<string>
            {
                JmapProperties.Name,
                JmapProperties.IsPersonal,
                JmapProperties.IsReadOnly,
                JmapProperties.AccountCapabilities
            },
            writer);
        writer.WriteObjectEnd();
    }

    internal Dictionary<string, JmapCapability> DecodeAccountCapabilities(JsonElement value, string path)
    {
        RequireObject(value, path);
        Dictionary<string, JmapCapability> capabilities = new Dictionary<string, JmapCapability>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            capabilities.Add(property.Name, DecodeAccountCapability(property.Name, property.Value, path + "/" + property.Name));
        }

        return capabilities;
    }

    internal void EncodeAccountCapabilities(Dictionary<string, JmapCapability> capabilities, JmapJsonWriter writer, string path)
    {
        if (capabilities == null)
        {
            throw new JmapValidationException("Account capabilities cannot be null.", new List<string> { path }, default(JsonElement));
        }

        writer.WriteObjectStart();
        List<string> keys = SortNames(capabilities.Keys);
        foreach (string key in keys)
        {
            JmapCapability capability = capabilities[key];
            if (capability == null)
            {
                throw new JmapValidationException("An account capability object cannot be null.", new List<string> { path + "/" + key }, default(JsonElement));
            }

            writer.WriteMemberName(key);
            EncodeAccountCapability(key, capability, writer);
        }

        writer.WriteObjectEnd();
    }

    internal JmapCapability DecodeServerCapability(string uri, JsonElement value, string path)
    {
        RequireObject(value, path);
        JmapCapability capability = new JmapCapability();
        if (uri == JmapCapabilities.Core)
        {
            capability.Core = DecodeCoreCapability(value, path);
        }
        else
        {
            CaptureUnknownMembers(value, capability.UnknownProperties, new List<string>());
        }

        return capability;
    }

    internal void EncodeServerCapability(string uri, JmapCapability capability, JmapJsonWriter writer)
    {
        if (uri == JmapCapabilities.Core && capability.Core != null)
        {
            EncodeCoreCapability(capability.Core, writer);
            return;
        }

        writer.WriteObjectStart();
        WriteUnknownMembers(capability.UnknownProperties, new List<string>(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapCapability DecodeAccountCapability(string uri, JsonElement value, string path)
    {
        RequireObject(value, path);
        JmapCapability capability = new JmapCapability();
        if (uri == JmapCapabilities.Mail)
        {
            capability.Mail = DecodeMailCapability(value, path);
        }
        else if (uri == JmapCapabilities.Submission)
        {
            capability.Submission = DecodeSubmissionCapability(value, path);
        }
        else if (uri == JmapCapabilities.VacationResponse)
        {
            capability.VacationResponse = DecodeVacationCapability(value, path);
        }
        else
        {
            CaptureUnknownMembers(value, capability.UnknownProperties, new List<string>());
        }

        return capability;
    }

    internal void EncodeAccountCapability(string uri, JmapCapability capability, JmapJsonWriter writer)
    {
        if (uri == JmapCapabilities.Mail && capability.Mail != null)
        {
            EncodeMailCapability(capability.Mail, writer);
            return;
        }

        if (uri == JmapCapabilities.Submission && capability.Submission != null)
        {
            EncodeSubmissionCapability(capability.Submission, writer);
            return;
        }

        if (uri == JmapCapabilities.VacationResponse && capability.VacationResponse != null)
        {
            EncodeVacationCapability(capability.VacationResponse, writer);
            return;
        }

        writer.WriteObjectStart();
        WriteUnknownMembers(capability.UnknownProperties, new List<string>(), writer);
        writer.WriteObjectEnd();
    }

    internal JmapCoreCapabilities DecodeCoreCapability(JsonElement value, string path)
    {
        JmapCoreCapabilities core = new JmapCoreCapabilities();
        core.MaxSizeUpload = ReadRequiredUnsignedInteger(GetMember(value, JmapProperties.MaxSizeUpload), path + "/maxSizeUpload");
        core.MaxConcurrentUpload = ReadRequiredUnsignedInteger(GetMember(value, JmapProperties.MaxConcurrentUpload), path + "/maxConcurrentUpload");
        core.MaxSizeRequest = ReadRequiredUnsignedInteger(GetMember(value, JmapProperties.MaxSizeRequest), path + "/maxSizeRequest");
        core.MaxConcurrentRequests = ReadRequiredUnsignedInteger(GetMember(value, JmapProperties.MaxConcurrentRequests), path + "/maxConcurrentRequests");
        core.MaxCallsInRequest = ReadRequiredUnsignedInteger(GetMember(value, JmapProperties.MaxCallsInRequest), path + "/maxCallsInRequest");
        core.MaxObjectsInGet = ReadRequiredUnsignedInteger(GetMember(value, JmapProperties.MaxObjectsInGet), path + "/maxObjectsInGet");
        core.MaxObjectsInSet = ReadRequiredUnsignedInteger(GetMember(value, JmapProperties.MaxObjectsInSet), path + "/maxObjectsInSet");
        core.CollationAlgorithms = DecodeStringArray(GetMember(value, JmapProperties.CollationAlgorithms), path + "/collationAlgorithms");
        CaptureUnknownMembers(
            value,
            core.UnknownProperties,
            new List<string>
            {
                JmapProperties.MaxSizeUpload,
                JmapProperties.MaxConcurrentUpload,
                JmapProperties.MaxSizeRequest,
                JmapProperties.MaxConcurrentRequests,
                JmapProperties.MaxCallsInRequest,
                JmapProperties.MaxObjectsInGet,
                JmapProperties.MaxObjectsInSet,
                JmapProperties.CollationAlgorithms
            });
        return core;
    }

    internal void EncodeCoreCapability(JmapCoreCapabilities core, JmapJsonWriter writer)
    {
        writer.WriteObjectStart();
        WriteUnsignedIntegerMember(writer, JmapProperties.MaxSizeUpload, core.MaxSizeUpload, "/capabilities/core/maxSizeUpload");
        WriteUnsignedIntegerMember(writer, JmapProperties.MaxConcurrentUpload, core.MaxConcurrentUpload, "/capabilities/core/maxConcurrentUpload");
        WriteUnsignedIntegerMember(writer, JmapProperties.MaxSizeRequest, core.MaxSizeRequest, "/capabilities/core/maxSizeRequest");
        WriteUnsignedIntegerMember(writer, JmapProperties.MaxConcurrentRequests, core.MaxConcurrentRequests, "/capabilities/core/maxConcurrentRequests");
        WriteUnsignedIntegerMember(writer, JmapProperties.MaxCallsInRequest, core.MaxCallsInRequest, "/capabilities/core/maxCallsInRequest");
        WriteUnsignedIntegerMember(writer, JmapProperties.MaxObjectsInGet, core.MaxObjectsInGet, "/capabilities/core/maxObjectsInGet");
        WriteUnsignedIntegerMember(writer, JmapProperties.MaxObjectsInSet, core.MaxObjectsInSet, "/capabilities/core/maxObjectsInSet");
        writer.WriteMemberName(JmapProperties.CollationAlgorithms);
        EncodeStringArray(core.CollationAlgorithms, writer, "/capabilities/core/collationAlgorithms");
        WriteUnknownMembers(
            core.UnknownProperties,
            new List<string>
            {
                JmapProperties.MaxSizeUpload,
                JmapProperties.MaxConcurrentUpload,
                JmapProperties.MaxSizeRequest,
                JmapProperties.MaxConcurrentRequests,
                JmapProperties.MaxCallsInRequest,
                JmapProperties.MaxObjectsInGet,
                JmapProperties.MaxObjectsInSet,
                JmapProperties.CollationAlgorithms
            },
            writer);
        writer.WriteObjectEnd();
    }

    internal JmapMailCapabilities DecodeMailCapability(JsonElement value, string path)
    {
        JmapMailCapabilities mail = new JmapMailCapabilities();
        JsonElement maxMailboxes = GetMember(value, JmapProperties.MaxMailboxesPerEmail);
        if (maxMailboxes.ValueKind != JsonValueKind.Undefined)
        {
            mail.HasMaxMailboxesPerEmail = true;
            if (maxMailboxes.ValueKind == JsonValueKind.Null)
            {
                mail.IsMaxMailboxesPerEmailNull = true;
            }
            else
            {
                mail.MaxMailboxesPerEmail = ReadRequiredUnsignedInteger(maxMailboxes, path + "/maxMailboxesPerEmail");
            }
        }

        JsonElement maxDepth = GetMember(value, JmapProperties.MaxMailboxDepth);
        if (maxDepth.ValueKind != JsonValueKind.Undefined)
        {
            mail.HasMaxMailboxDepth = true;
            if (maxDepth.ValueKind == JsonValueKind.Null)
            {
                mail.IsMaxMailboxDepthNull = true;
            }
            else
            {
                mail.MaxMailboxDepth = ReadRequiredUnsignedInteger(maxDepth, path + "/maxMailboxDepth");
            }
        }

        mail.MaxSizeMailboxName = ReadRequiredUnsignedInteger(GetMember(value, JmapProperties.MaxSizeMailboxName), path + "/maxSizeMailboxName");
        mail.MaxSizeAttachmentsPerEmail = ReadRequiredUnsignedInteger(GetMember(value, JmapProperties.MaxSizeAttachmentsPerEmail), path + "/maxSizeAttachmentsPerEmail");
        mail.EmailQuerySortOptions = DecodeStringArray(GetMember(value, JmapProperties.EmailQuerySortOptions), path + "/emailQuerySortOptions");
        mail.MayCreateTopLevelMailbox = ReadRequiredBoolean(GetMember(value, JmapProperties.MayCreateTopLevelMailbox), path + "/mayCreateTopLevelMailbox");
        CaptureUnknownMembers(
            value,
            mail.UnknownProperties,
            new List<string>
            {
                JmapProperties.MaxMailboxesPerEmail,
                JmapProperties.MaxMailboxDepth,
                JmapProperties.MaxSizeMailboxName,
                JmapProperties.MaxSizeAttachmentsPerEmail,
                JmapProperties.EmailQuerySortOptions,
                JmapProperties.MayCreateTopLevelMailbox
            });
        return mail;
    }

    internal void EncodeMailCapability(JmapMailCapabilities mail, JmapJsonWriter writer)
    {
        writer.WriteObjectStart();
        if (mail.HasMaxMailboxesPerEmail)
        {
            if (mail.IsMaxMailboxesPerEmailNull)
            {
                writer.WriteNullMember(JmapProperties.MaxMailboxesPerEmail);
            }
            else
            {
                WriteUnsignedIntegerMember(writer, JmapProperties.MaxMailboxesPerEmail, mail.MaxMailboxesPerEmail, "/accountCapabilities/mail/maxMailboxesPerEmail");
            }
        }

        if (mail.HasMaxMailboxDepth)
        {
            if (mail.IsMaxMailboxDepthNull)
            {
                writer.WriteNullMember(JmapProperties.MaxMailboxDepth);
            }
            else
            {
                WriteUnsignedIntegerMember(writer, JmapProperties.MaxMailboxDepth, mail.MaxMailboxDepth, "/accountCapabilities/mail/maxMailboxDepth");
            }
        }

        WriteUnsignedIntegerMember(writer, JmapProperties.MaxSizeMailboxName, mail.MaxSizeMailboxName, "/accountCapabilities/mail/maxSizeMailboxName");
        WriteUnsignedIntegerMember(writer, JmapProperties.MaxSizeAttachmentsPerEmail, mail.MaxSizeAttachmentsPerEmail, "/accountCapabilities/mail/maxSizeAttachmentsPerEmail");
        writer.WriteMemberName(JmapProperties.EmailQuerySortOptions);
        EncodeStringArray(mail.EmailQuerySortOptions, writer, "/accountCapabilities/mail/emailQuerySortOptions");
        writer.WriteBooleanMember(JmapProperties.MayCreateTopLevelMailbox, mail.MayCreateTopLevelMailbox, true);
        WriteUnknownMembers(
            mail.UnknownProperties,
            new List<string>
            {
                JmapProperties.MaxMailboxesPerEmail,
                JmapProperties.MaxMailboxDepth,
                JmapProperties.MaxSizeMailboxName,
                JmapProperties.MaxSizeAttachmentsPerEmail,
                JmapProperties.EmailQuerySortOptions,
                JmapProperties.MayCreateTopLevelMailbox
            },
            writer);
        writer.WriteObjectEnd();
    }

    internal JmapSubmissionCapabilities DecodeSubmissionCapability(JsonElement value, string path)
    {
        JmapSubmissionCapabilities submission = new JmapSubmissionCapabilities();
        submission.MaxDelayedSend = ReadRequiredUnsignedInteger(GetMember(value, JmapProperties.MaxDelayedSend), path + "/maxDelayedSend");
        submission.SubmissionExtensions = DecodeStringListMap(GetMember(value, JmapProperties.SubmissionExtensions), path + "/submissionExtensions");
        CaptureUnknownMembers(
            value,
            submission.UnknownProperties,
            new List<string> { JmapProperties.MaxDelayedSend, JmapProperties.SubmissionExtensions });
        return submission;
    }

    internal void EncodeSubmissionCapability(JmapSubmissionCapabilities submission, JmapJsonWriter writer)
    {
        writer.WriteObjectStart();
        WriteUnsignedIntegerMember(writer, JmapProperties.MaxDelayedSend, submission.MaxDelayedSend, "/accountCapabilities/submission/maxDelayedSend");
        writer.WriteMemberName(JmapProperties.SubmissionExtensions);
        EncodeStringListMap(submission.SubmissionExtensions, writer, "/accountCapabilities/submission/submissionExtensions");
        WriteUnknownMembers(
            submission.UnknownProperties,
            new List<string> { JmapProperties.MaxDelayedSend, JmapProperties.SubmissionExtensions },
            writer);
        writer.WriteObjectEnd();
    }

    internal JmapVacationResponseCapabilities DecodeVacationCapability(JsonElement value, string path)
    {
        JmapVacationResponseCapabilities vacation = new JmapVacationResponseCapabilities();
        CaptureUnknownMembers(value, vacation.UnknownProperties, new List<string>());
        return vacation;
    }

    internal void EncodeVacationCapability(JmapVacationResponseCapabilities vacation, JmapJsonWriter writer)
    {
        writer.WriteObjectStart();
        WriteUnknownMembers(vacation.UnknownProperties, new List<string>(), writer);
        writer.WriteObjectEnd();
    }

    internal List<string> DecodeStringArray(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JmapProtocolException("A JSON array is required.", path, CloneContext(value));
        }

        List<string> result = new List<string>();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result.Add(ReadRequiredString(item, path + "/" + index));
            index = index + 1;
        }

        return result;
    }

    internal void EncodeStringArray(List<string> values, JmapJsonWriter writer, string path)
    {
        if (values == null)
        {
            throw new JmapValidationException("A string array cannot be null.", new List<string> { path }, default(JsonElement));
        }

        writer.WriteArrayStart();
        int index = 0;
        foreach (string value in values)
        {
            if (value == null)
            {
                throw new JmapValidationException("A string array item cannot be null.", new List<string> { path + "/" + index }, default(JsonElement));
            }

            writer.WriteStringValue(value);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal Dictionary<string, List<string>> DecodeStringListMap(JsonElement value, string path)
    {
        RequireObject(value, path);
        Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            result.Add(property.Name, DecodeStringArray(property.Value, path + "/" + property.Name));
        }

        return result;
    }

    internal void EncodeStringListMap(Dictionary<string, List<string>> values, JmapJsonWriter writer, string path)
    {
        if (values == null)
        {
            throw new JmapValidationException("A string-array map cannot be null.", new List<string> { path }, default(JsonElement));
        }

        writer.WriteObjectStart();
        List<string> keys = SortNames(values.Keys);
        foreach (string key in keys)
        {
            writer.WriteMemberName(key);
            EncodeStringArray(values[key], writer, path + "/" + key);
        }

        writer.WriteObjectEnd();
    }

    internal JsonElement GetMember(JsonElement value, string name)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                return property.Value;
            }
        }

        return default(JsonElement);
    }

    internal string ReadRequiredString(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new JmapProtocolException("A JSON string is required.", path, CloneContext(value));
        }

        string? result = value.GetString();
        if (result == null)
        {
            throw new JmapProtocolException("A JSON string is required.", path, CloneContext(value));
        }

        return result;
    }

    internal bool ReadRequiredBoolean(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
        {
            throw new JmapProtocolException("A JSON boolean is required.", path, CloneContext(value));
        }

        return value.GetBoolean();
    }

    internal long ReadRequiredUnsignedInteger(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            throw new JmapProtocolException("A JSON unsigned integer is required.", path, CloneContext(value));
        }

        long result = value.GetInt64();
        if (result < JmapProtocolLimits.MinimumUnsignedInt || result > JmapProtocolLimits.MaximumUnsignedInt)
        {
            throw new JmapProtocolException("A JSON unsigned integer is outside the interoperable range.", path, Reader.CloneUnknownValue(value));
        }

        return result;
    }

    internal void WriteRequiredStringMember(JmapJsonWriter writer, string name, string? value, string path)
    {
        if (value == null)
        {
            throw new JmapValidationException("A required string is missing.", new List<string> { path }, default(JsonElement));
        }

        writer.WriteStringMember(name, value, false);
    }

    internal void WriteUnsignedIntegerMember(JmapJsonWriter writer, string name, long value, string path)
    {
        if (value < JmapProtocolLimits.MinimumUnsignedInt || value > JmapProtocolLimits.MaximumUnsignedInt)
        {
            throw new JmapValidationException("An unsigned integer is outside the interoperable range.", new List<string> { path }, default(JsonElement));
        }

        writer.WriteIntegerMember(name, value, true);
    }

    internal void RequireObject(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new JmapProtocolException("A JSON object is required.", path, CloneContext(value));
        }
    }

    internal JsonElement CloneContext(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return default(JsonElement);
        }

        return Reader.CloneUnknownValue(value);
    }

    internal void CaptureUnknownMembers(JsonElement value, Dictionary<string, JsonElement> destination, List<string> knownNames)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (!knownNames.Contains(property.Name))
            {
                destination.Add(property.Name, Reader.CloneUnknownValue(property.Value));
            }
        }
    }

    internal void WriteUnknownMembers(Dictionary<string, JsonElement> properties, List<string> knownNames, JmapJsonWriter writer)
    {
        if (properties == null)
        {
            throw new JmapValidationException("Unknown property storage cannot be null.", new List<string>(), default(JsonElement));
        }

        List<string> keys = SortNames(properties.Keys);
        foreach (string key in keys)
        {
            if (!knownNames.Contains(key))
            {
                writer.WriteElementMember(key, properties[key]);
            }
        }
    }

    internal List<string> SortNames(IEnumerable<string> values)
    {
        List<string> keys = new List<string>(values);
        keys.Sort(System.StringComparer.Ordinal);
        return keys;
    }
}
