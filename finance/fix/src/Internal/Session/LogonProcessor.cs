using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fix;

internal class LogonProcessor
{
    internal SessionRole Role;
    internal SessionId Id = new SessionId();
    internal int ConfiguredHeartbeatIntervalSeconds;
    internal SessionResetMode ResetMode;
    internal string Username = "";
    internal string Password = "";
    internal string ConfiguredDefaultApplicationVersion = "";
    internal FixDictionaryIdentity TransportIdentity = new FixDictionaryIdentity();
    internal FixDictionaryIdentity ConfiguredApplicationIdentity = new FixDictionaryIdentity();
    internal ValidationOptions ValidationOptions = new ValidationOptions();
    internal FixDictionary TransportDictionary;
    internal IMessageValidator MessageValidator;
    internal IDictionaryProvider DictionaryProvider;
    internal ISessionAuthenticator Authenticator;
    internal ISessionClock Clock;
    internal SessionHeaderValidator HeaderValidator;

    internal LogonProcessor(SessionSettings settings, FixDictionary transportDictionary, IMessageValidator messageValidator, IDictionaryProvider dictionaryProvider, ISessionAuthenticator authenticator, ISessionClock clock)
    {
        if (settings == null || settings.Id == null || settings.Validation == null || settings.TransportDictionary == null || settings.ApplicationDictionary == null)
        {
            throw new FixSessionException("Logon settings are missing.");
        }
        if (transportDictionary == null || messageValidator == null || dictionaryProvider == null || authenticator == null || clock == null)
        {
            throw new FixSessionException("A Logon processing dependency is missing.");
        }
        if (settings.Role != SessionRole.Initiator && settings.Role != SessionRole.Acceptor)
        {
            throw new FixSessionException("The Logon session role is invalid.");
        }
        if (settings.HeartbeatIntervalSeconds < 0)
        {
            throw new FixSessionException("The configured heartbeat interval cannot be negative.");
        }
        if (settings.Id.BeginString.Length == 0 || settings.Id.SenderCompID.Length == 0 || settings.Id.TargetCompID.Length == 0)
        {
            throw new FixSessionException("The Logon session identity is incomplete.");
        }
        if (settings.Id.BeginString == FixBeginStrings.Fixt11 && settings.DefaultApplicationVersion.Length == 0)
        {
            throw new FixSessionException("A FIXT Logon requires a configured default application version.");
        }

        Role = settings.Role;
        Id = CopySessionId(settings.Id);
        ConfiguredHeartbeatIntervalSeconds = settings.HeartbeatIntervalSeconds;
        ResetMode = settings.ResetMode;
        Username = settings.Username == null ? "" : settings.Username;
        Password = settings.Password == null ? "" : settings.Password;
        ConfiguredDefaultApplicationVersion = settings.DefaultApplicationVersion;
        TransportIdentity = CopyIdentity(settings.TransportDictionary);
        if (TransportIdentity.BeginString.Length == 0)
        {
            TransportIdentity.BeginString = Id.BeginString;
        }
        ConfiguredApplicationIdentity = CopyIdentity(settings.ApplicationDictionary);
        ValidationOptions = CopyValidationOptions(settings.Validation);
        TransportDictionary = transportDictionary;
        MessageValidator = messageValidator;
        DictionaryProvider = dictionaryProvider;
        Authenticator = authenticator;
        Clock = clock;
        HeaderValidator = new SessionHeaderValidator(settings, clock);
    }

    internal LogonNegotiationState CreateNegotiationState()
    {
        LogonNegotiationState state = new LogonNegotiationState();
        state.HeartbeatIntervalSeconds = ConfiguredHeartbeatIntervalSeconds;
        state.DefaultApplicationVersion = ConfiguredDefaultApplicationVersion;
        state.ApplicationIdentity = CopyIdentity(ConfiguredApplicationIdentity);
        state.ResetSequenceNumbers = (ResetMode & SessionResetMode.OnLogon) == SessionResetMode.OnLogon;
        return state;
    }

    internal FixMessage CreateLogonMessage(int senderSequenceNumber, int nextExpectedMsgSeqNum, LogonNegotiationState state)
    {
        if (state == null)
        {
            throw new FixSessionException("The Logon negotiation state is missing.");
        }
        if (senderSequenceNumber < FixDefaults.InitialSequenceNumber || nextExpectedMsgSeqNum < 0)
        {
            throw new FixSessionException("Logon sequence numbers are invalid.");
        }
        if (state.HeartbeatIntervalSeconds < 0)
        {
            throw new FixSessionException("The negotiated heartbeat interval cannot be negative.");
        }
        string applicationVersion = state.DefaultApplicationVersion.Length == 0 ? ConfiguredDefaultApplicationVersion : state.DefaultApplicationVersion;
        if (Id.BeginString == FixBeginStrings.Fixt11 && applicationVersion.Length == 0)
        {
            throw new FixSessionException("A FIXT Logon requires DefaultApplVerID.");
        }
        DateTime sendingTime = Clock.GetUtcNow();
        if (sendingTime.Kind != DateTimeKind.Utc)
        {
            throw new FixSessionException("The session clock returned a non-UTC value.");
        }

        int actualSequenceNumber = state.ResetSequenceNumbers ? FixDefaults.InitialSequenceNumber : senderSequenceNumber;
        FixMessage message = new FixMessage();
        message.BeginString = Id.BeginString;
        message.MsgType = FixAdminMessageTypes.Logon;
        AddHeaderText(message, FixTags.BeginString, Id.BeginString);
        AddHeaderText(message, FixTags.MsgType, FixAdminMessageTypes.Logon);
        AddHeaderInteger(message, FixTags.MsgSeqNum, actualSequenceNumber);
        AddHeaderText(message, FixTags.SenderCompID, Id.SenderCompID);
        AddOptionalHeaderText(message, FixTags.SenderSubID, Id.SenderSubID);
        AddOptionalHeaderText(message, FixTags.SenderLocationID, Id.SenderLocationID);
        AddHeaderText(message, FixTags.TargetCompID, Id.TargetCompID);
        AddOptionalHeaderText(message, FixTags.TargetSubID, Id.TargetSubID);
        AddOptionalHeaderText(message, FixTags.TargetLocationID, Id.TargetLocationID);
        AddHeaderText(message, FixTags.SendingTime, FormatSendingTime(sendingTime));
        AddBodyInteger(message, FixTags.EncryptMethod, 0);
        AddBodyInteger(message, FixTags.HeartBtInt, state.HeartbeatIntervalSeconds);
        if (state.ResetSequenceNumbers)
        {
            AddBodyText(message, FixTags.ResetSeqNumFlag, "Y");
        }
        if (nextExpectedMsgSeqNum > 0)
        {
            AddBodyInteger(message, FixTags.NextExpectedMsgSeqNum, nextExpectedMsgSeqNum);
        }
        if (Id.BeginString == FixBeginStrings.Fixt11)
        {
            AddBodyText(message, FixTags.DefaultApplVerID, applicationVersion);
        }
        if (Role == SessionRole.Initiator)
        {
            AddOptionalBodyText(message, FixTags.Username, Username);
            AddOptionalBodyText(message, FixTags.Password, Password);
        }
        return message;
    }

    internal LogonProcessingResult ProcessInboundLogon(FixMessage message, string qualifier, string remoteAddress, LogonNegotiationState state)
    {
        if (state == null)
        {
            throw new FixSessionException("The Logon negotiation state is missing.");
        }
        if (message == null)
        {
            LogonProcessingResult missing = new LogonProcessingResult();
            AddIssue(missing.Issues, null, "missing-logon-message", "The inbound Logon message is missing.", false, 0, null);
            return RejectFromIssues(missing.Issues);
        }

        List<ValidationIssue> issues = [];
        SessionHeaderValidationResult header = HeaderValidator.ValidateInboundHeader(message, qualifier);
        CopyIssues(issues, header.Issues);
        if (message.MsgType != FixAdminMessageTypes.Logon)
        {
            AddIssue(issues, message, "unexpected-logon-message-type", "The inbound message is not a Logon.", true, FixTags.MsgType, EnvelopeValidator.FindFirstMessageField(message, FixTags.MsgType));
        }

        ValidationContext context = new ValidationContext();
        context.DictionaryIdentity = CopyIdentity(TransportIdentity);
        context.ExpectedBeginString = Id.BeginString;
        ValidationResult validation = MessageValidator.ValidateMessage(message, TransportDictionary, ValidationOptions, context);
        if (validation == null)
        {
            AddIssue(issues, message, "missing-logon-validation-result", "The message validator did not return a result.", false, 0, null);
        }
        else if (!validation.IsValid)
        {
            CopyIssues(issues, validation.Issues);
            if (validation.Issues.Count == 0)
            {
                AddIssue(issues, message, "logon-validation-failed", "The inbound Logon failed message validation.", false, 0, null);
            }
        }
        if (issues.Count != 0)
        {
            RejectMapper.MapValidationIssues(issues, message);
            return RejectFromIssues(issues);
        }

        LogonCandidate candidate = ParseCandidate(message);
        if (candidate.Issues.Count != 0)
        {
            RejectMapper.MapValidationIssues(candidate.Issues, message);
            return RejectFromIssues(candidate.Issues);
        }
        ResolveApplication(candidate, message);
        if (candidate.Issues.Count != 0)
        {
            RejectMapper.MapValidationIssues(candidate.Issues, message);
            return RejectFromIssues(candidate.Issues);
        }

        if (Role == SessionRole.Acceptor)
        {
            AuthenticationRequest request = CreateAuthenticationRequest(message, qualifier, remoteAddress, candidate);
            AuthenticationResult authentication = Authenticator.AuthenticateSession(request);
            if (authentication == null)
            {
                return RejectAuthentication("missing-authentication-result", "The session authenticator did not return a result.", true);
            }
            if (!authentication.Authenticated)
            {
                string code = authentication.Code.Length == 0 ? "authentication-rejected" : authentication.Code;
                string text = authentication.Text.Length == 0 ? "The inbound Logon was not authenticated." : authentication.Text;
                return RejectAuthentication(code, text, authentication.Disconnect);
            }
        }

        ApplyCandidate(state, candidate);
        return AcceptCandidate(candidate);
    }

    internal LogonCandidate ParseCandidate(FixMessage message)
    {
        LogonCandidate candidate = new LogonCandidate();
        LogonFieldValue encryption = ReadField(message, FixTags.EncryptMethod);
        RequireUniqueField(candidate, message, encryption, FixTags.EncryptMethod, "missing-encrypt-method");
        if (encryption.IsPresent && encryption.Count == 1)
        {
            int method = ParseNonNegativeInteger(encryption.Field);
            if (method != 0)
            {
                string code = method < 0 ? "invalid-encrypt-method" : "unsupported-encrypt-method";
                AddIssue(candidate.Issues, message, code, "EncryptMethod must select the unencrypted FIX value 0.", true, FixTags.EncryptMethod, encryption.Field);
            }
        }

        LogonFieldValue heartbeat = ReadField(message, FixTags.HeartBtInt);
        RequireUniqueField(candidate, message, heartbeat, FixTags.HeartBtInt, "missing-heart-bt-int");
        if (heartbeat.IsPresent && heartbeat.Count == 1)
        {
            int interval = ParseNonNegativeInteger(heartbeat.Field);
            if (interval < 0)
            {
                AddIssue(candidate.Issues, message, "invalid-heart-bt-int", "HeartBtInt must contain a non-negative integer number of seconds.", true, FixTags.HeartBtInt, heartbeat.Field);
            }
            else if (Role == SessionRole.Initiator && interval != ConfiguredHeartbeatIntervalSeconds)
            {
                AddIssue(candidate.Issues, message, "heart-bt-int-mismatch", "The Logon response did not confirm the proposed HeartBtInt.", true, FixTags.HeartBtInt, heartbeat.Field);
            }
            else
            {
                candidate.HeartbeatIntervalSeconds = interval;
            }
        }

        ParseResetFlag(candidate, message);
        ParseNextExpected(candidate, message);
        ParseAuthenticationFields(candidate, message);
        ParseDefaultApplicationVersion(candidate, message);
        if (candidate.ResetSequenceNumbers)
        {
            ValidateResetSequenceNumber(candidate, message);
        }
        return candidate;
    }

    internal void ParseResetFlag(LogonCandidate candidate, FixMessage message)
    {
        LogonFieldValue reset = ReadField(message, FixTags.ResetSeqNumFlag);
        ReportDuplicateField(candidate, message, reset, FixTags.ResetSeqNumFlag);
        if (!reset.IsPresent || reset.Count != 1)
        {
            return;
        }
        if (IsFlag(reset.Field, 89))
        {
            candidate.ResetSequenceNumbers = true;
        }
        else if (!IsFlag(reset.Field, 78))
        {
            AddIssue(candidate.Issues, message, "invalid-reset-seq-num-flag", "ResetSeqNumFlag must contain Y or N.", true, FixTags.ResetSeqNumFlag, reset.Field);
        }
    }

    internal void ParseNextExpected(LogonCandidate candidate, FixMessage message)
    {
        LogonFieldValue nextExpected = ReadField(message, FixTags.NextExpectedMsgSeqNum);
        ReportDuplicateField(candidate, message, nextExpected, FixTags.NextExpectedMsgSeqNum);
        if (!nextExpected.IsPresent || nextExpected.Count != 1)
        {
            return;
        }
        int value = ParsePositiveSequenceNumber(nextExpected.Field);
        if (value < FixDefaults.InitialSequenceNumber)
        {
            AddIssue(candidate.Issues, message, "invalid-next-expected-msg-seq-num", "NextExpectedMsgSeqNum must contain a positive sequence number.", true, FixTags.NextExpectedMsgSeqNum, nextExpected.Field);
            return;
        }
        candidate.HasNextExpectedMsgSeqNum = true;
        candidate.NextExpectedMsgSeqNum = value;
    }

    internal void ParseAuthenticationFields(LogonCandidate candidate, FixMessage message)
    {
        LogonFieldValue username = ReadField(message, FixTags.Username);
        LogonFieldValue password = ReadField(message, FixTags.Password);
        LogonFieldValue rawLength = ReadField(message, FixTags.RawDataLength);
        LogonFieldValue rawData = ReadField(message, FixTags.RawData);
        ReportDuplicateField(candidate, message, username, FixTags.Username);
        ReportDuplicateField(candidate, message, password, FixTags.Password);
        ReportDuplicateField(candidate, message, rawLength, FixTags.RawDataLength);
        ReportDuplicateField(candidate, message, rawData, FixTags.RawData);
        if (username.IsPresent && username.Count == 1) candidate.Username = ReadText(username.Field);
        if (password.IsPresent && password.Count == 1) candidate.Password = ReadText(password.Field);
        if (rawData.IsPresent && rawData.Count == 1) candidate.RawData = CopyBytes(rawData.Field == null ? [] : rawData.Field.Value);
        if (rawLength.IsPresent != rawData.IsPresent)
        {
            AddIssue(candidate.Issues, message, "raw-data-length-pair-missing", "RawDataLength and RawData must occur together in a Logon.", true, rawLength.IsPresent ? FixTags.RawData : FixTags.RawDataLength, rawLength.IsPresent ? rawLength.Field : rawData.Field);
        }
        else if (rawLength.IsPresent && rawLength.Count == 1 && rawData.Count == 1)
        {
            int length = ParseNonNegativeInteger(rawLength.Field);
            if (length < 0 || length != candidate.RawData.Length)
            {
                AddIssue(candidate.Issues, message, "raw-data-length-mismatch", "RawDataLength does not match the Logon RawData byte count.", true, FixTags.RawDataLength, rawLength.Field);
            }
        }
    }

    internal void ParseDefaultApplicationVersion(LogonCandidate candidate, FixMessage message)
    {
        LogonFieldValue version = ReadField(message, FixTags.DefaultApplVerID);
        ReportDuplicateField(candidate, message, version, FixTags.DefaultApplVerID);
        if (Id.BeginString == FixBeginStrings.Fixt11)
        {
            if (!version.IsPresent)
            {
                AddIssue(candidate.Issues, message, "missing-default-application-version", "A FIXT Logon requires DefaultApplVerID.", true, FixTags.DefaultApplVerID, null);
            }
            else if (version.Count == 1)
            {
                candidate.DefaultApplicationVersion = ReadText(version.Field);
                if (candidate.DefaultApplicationVersion.Length == 0)
                {
                    AddIssue(candidate.Issues, message, "invalid-default-application-version", "DefaultApplVerID must not be empty.", true, FixTags.DefaultApplVerID, version.Field);
                }
            }
        }
        else
        {
            candidate.DefaultApplicationVersion = ConfiguredDefaultApplicationVersion;
        }
    }

    internal void ValidateResetSequenceNumber(LogonCandidate candidate, FixMessage message)
    {
        LogonFieldValue sequence = ReadField(message, FixTags.MsgSeqNum);
        if (!sequence.IsPresent || sequence.Count != 1)
        {
            return;
        }
        int value = ParsePositiveSequenceNumber(sequence.Field);
        if (value != FixDefaults.InitialSequenceNumber)
        {
            AddIssue(candidate.Issues, message, "reset-logon-sequence-number-not-one", "A reset Logon must use MsgSeqNum 1.", true, FixTags.MsgSeqNum, sequence.Field);
        }
    }

    internal void ResolveApplication(LogonCandidate candidate, FixMessage message)
    {
        if (Id.BeginString != FixBeginStrings.Fixt11)
        {
            candidate.ApplicationIdentity = CopyIdentity(ConfiguredApplicationIdentity);
            candidate.ApplicationIdentity.BeginString = Id.BeginString;
            candidate.ApplicationIdentity.ApplicationVersion = "";
            candidate.ApplicationIdentity.HasExtensionPack = false;
            candidate.ApplicationIdentity.ExtensionPack = 0;
            candidate.ApplicationIdentity.CustomVersion = "";
            candidate.ApplicationDictionary = TransportDictionary;
            return;
        }

        DictionarySelectionResult selection = DictionaryVersionSelector.SelectDictionaryIdentities(message, TransportIdentity, ConfiguredApplicationIdentity, ConfiguredDefaultApplicationVersion);
        if (!selection.Success)
        {
            CopyDictionaryDiagnostics(candidate.Issues, message, selection.Diagnostics);
            return;
        }
        DictionaryLoadResult loaded = DictionaryProvider.ResolveDictionary(selection.ApplicationIdentity);
        if (loaded == null || !loaded.Success || loaded.Dictionary == null)
        {
            AddIssue(candidate.Issues, message, "unsupported-default-application-version", "No application dictionary supports the negotiated DefaultApplVerID.", true, FixTags.DefaultApplVerID, EnvelopeValidator.FindFirstMessageField(message, FixTags.DefaultApplVerID));
            return;
        }
        candidate.ApplicationIdentity = CopyIdentity(selection.ApplicationIdentity);
        candidate.ApplicationDictionary = loaded.Dictionary;
        candidate.DefaultApplicationVersion = selection.ApplicationIdentity.ApplicationVersion;
    }

    internal AuthenticationRequest CreateAuthenticationRequest(FixMessage message, string qualifier, string remoteAddress, LogonCandidate candidate)
    {
        AuthenticationRequest request = new AuthenticationRequest();
        request.Id = CopySessionId(Id);
        request.Id.Qualifier = qualifier == null ? "" : qualifier;
        request.Username = candidate.Username;
        request.Password = candidate.Password;
        request.RawData = CopyBytes(candidate.RawData);
        request.RemoteAddress = remoteAddress == null ? "" : remoteAddress;
        request.LogonMessage = message;
        return request;
    }

    internal static void ApplyCandidate(LogonNegotiationState state, LogonCandidate candidate)
    {
        state.HeartbeatIntervalSeconds = candidate.HeartbeatIntervalSeconds;
        state.DefaultApplicationVersion = candidate.DefaultApplicationVersion;
        state.ApplicationIdentity = CopyIdentity(candidate.ApplicationIdentity);
        state.ApplicationDictionary = candidate.ApplicationDictionary;
        state.ResetSequenceNumbers = candidate.ResetSequenceNumbers;
        if (candidate.ResetSequenceNumbers)
        {
            state.NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
            state.NextTargetSeqNum = FixDefaults.InitialSequenceNumber;
        }
        state.HasPeerNextExpectedMsgSeqNum = candidate.HasNextExpectedMsgSeqNum;
        state.PeerNextExpectedMsgSeqNum = candidate.NextExpectedMsgSeqNum;
        state.IsAuthenticated = true;
        state.IsLoggedOn = true;
    }

    internal static LogonProcessingResult AcceptCandidate(LogonCandidate candidate)
    {
        LogonProcessingResult result = new LogonProcessingResult();
        result.Accepted = true;
        result.ResetSequenceNumbers = candidate.ResetSequenceNumbers;
        result.HeartbeatIntervalSeconds = candidate.HeartbeatIntervalSeconds;
        result.DefaultApplicationVersion = candidate.DefaultApplicationVersion;
        result.HasNextExpectedMsgSeqNum = candidate.HasNextExpectedMsgSeqNum;
        result.NextExpectedMsgSeqNum = candidate.NextExpectedMsgSeqNum;
        result.Code = "logon-accepted";
        result.Text = "The inbound Logon was validated and accepted.";
        result.ApplicationIdentity = CopyIdentity(candidate.ApplicationIdentity);
        result.ApplicationDictionary = candidate.ApplicationDictionary;
        return result;
    }

    internal static LogonProcessingResult RejectFromIssues(List<ValidationIssue> issues)
    {
        LogonProcessingResult result = new LogonProcessingResult();
        result.Disconnect = true;
        result.SendLogout = true;
        CopyIssues(result.Issues, issues);
        if (issues.Count == 0)
        {
            result.Code = "logon-rejected";
            result.Text = "The inbound Logon was rejected.";
        }
        else
        {
            result.Code = issues[0].Code;
            result.Text = issues[0].Message;
        }
        return result;
    }

    internal static LogonProcessingResult RejectAuthentication(string code, string text, bool disconnect)
    {
        LogonProcessingResult result = new LogonProcessingResult();
        result.Disconnect = disconnect;
        result.SendLogout = true;
        result.Code = code;
        result.Text = text;
        return result;
    }

    internal static void RequireUniqueField(LogonCandidate candidate, FixMessage message, LogonFieldValue value, int tag, string missingCode)
    {
        if (!value.IsPresent)
        {
            AddIssue(candidate.Issues, message, missingCode, "A required Logon field is missing.", true, tag, null);
            return;
        }
        ReportDuplicateField(candidate, message, value, tag);
    }

    internal static void ReportDuplicateField(LogonCandidate candidate, FixMessage message, LogonFieldValue value, int tag)
    {
        if (value.Count > 1)
        {
            AddIssue(candidate.Issues, message, "duplicate-logon-field", "A Logon field occurs more than once.", true, tag, value.Field);
        }
    }

    internal static LogonFieldValue ReadField(FixMessage message, int tag)
    {
        LogonFieldValue result = new LogonFieldValue();
        ReadSection(message.HeaderFields, tag, result);
        ReadSection(message.BodyFields, tag, result);
        ReadSection(message.TrailerFields, tag, result);
        return result;
    }

    internal static void ReadSection(List<FixField> fields, int tag, LogonFieldValue result)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].Tag == tag)
            {
                result.Count++;
                if (!result.IsPresent)
                {
                    result.IsPresent = true;
                    result.Field = fields[index];
                }
            }
        }
    }

    internal static int ParseNonNegativeInteger(FixField? field)
    {
        if (field == null)
        {
            return -1;
        }
        ScalarParseResult parsed = ScalarParser.ParseScalar(field.Value, FixFieldType.Int, FixDefaults.MaxFieldSize);
        if (!parsed.Success || parsed.IntegerValue < 0)
        {
            return -1;
        }
        return parsed.IntegerValue;
    }

    internal static int ParsePositiveSequenceNumber(FixField? field)
    {
        if (field == null)
        {
            return -1;
        }
        ScalarParseResult parsed = ScalarParser.ParseScalar(field.Value, FixFieldType.SeqNum, FixDefaults.MaxFieldSize);
        if (!parsed.Success || parsed.IntegerValue < FixDefaults.InitialSequenceNumber)
        {
            return -1;
        }
        return parsed.IntegerValue;
    }

    internal static bool IsFlag(FixField? field, byte expected)
    {
        return field != null && field.Value.Length == 1 && field.Value[0] == expected;
    }

    internal static string ReadText(FixField? field)
    {
        return field == null ? "" : Encoding.Latin1.GetString(field.Value);
    }

    internal static void AddHeaderText(FixMessage message, int tag, string value)
    {
        AddField(message.HeaderFields, tag, Encoding.Latin1.GetBytes(value));
    }

    internal static void AddOptionalHeaderText(FixMessage message, int tag, string value)
    {
        if (value.Length != 0)
        {
            AddHeaderText(message, tag, value);
        }
    }

    internal static void AddHeaderInteger(FixMessage message, int tag, int value)
    {
        AddHeaderText(message, tag, value.ToString(CultureInfo.InvariantCulture));
    }

    internal static void AddBodyText(FixMessage message, int tag, string value)
    {
        AddField(message.BodyFields, tag, Encoding.Latin1.GetBytes(value));
    }

    internal static void AddOptionalBodyText(FixMessage message, int tag, string value)
    {
        if (value.Length != 0)
        {
            AddBodyText(message, tag, value);
        }
    }

    internal static void AddBodyInteger(FixMessage message, int tag, int value)
    {
        AddBodyText(message, tag, value.ToString(CultureInfo.InvariantCulture));
    }

    internal static void AddField(List<FixField> fields, int tag, byte[] value)
    {
        FixField field = new FixField();
        field.Tag = tag;
        field.Value = value;
        fields.Add(field);
    }

    internal static string FormatSendingTime(DateTime value)
    {
        return value.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }

    internal static void AddIssue(List<ValidationIssue> issues, FixMessage? message, string code, string text, bool hasTag, int tag, FixField? field)
    {
        ValidationIssue issue = new ValidationIssue();
        issue.Severity = ValidationSeverity.Error;
        issue.Layer = ValidationLayer.SessionHeader;
        issue.Code = code;
        issue.Message = text;
        issue.HasTag = hasTag;
        issue.Tag = tag;
        if (message != null)
        {
            issue.MsgType = message.MsgType;
        }
        if (field != null && field.SourceOffset >= 0)
        {
            issue.HasSourceOffset = true;
            issue.SourceOffset = field.SourceOffset;
        }
        issues.Add(issue);
    }

    internal static void CopyIssues(List<ValidationIssue> destination, List<ValidationIssue> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            destination.Add(source[index]);
        }
    }

    internal static void CopyDictionaryDiagnostics(List<ValidationIssue> issues, FixMessage message, List<DictionaryDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            DictionaryDiagnostic diagnostic = diagnostics[index];
            AddIssue(issues, message, diagnostic.Code, diagnostic.Message, diagnostic.HasTag, diagnostic.Tag, diagnostic.HasTag ? EnvelopeValidator.FindFirstMessageField(message, diagnostic.Tag) : null);
        }
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            copy[index] = source[index];
        }
        return copy;
    }

    internal static SessionId CopySessionId(SessionId source)
    {
        SessionId copy = new SessionId();
        copy.BeginString = source.BeginString;
        copy.SenderCompID = source.SenderCompID;
        copy.SenderSubID = source.SenderSubID;
        copy.SenderLocationID = source.SenderLocationID;
        copy.TargetCompID = source.TargetCompID;
        copy.TargetSubID = source.TargetSubID;
        copy.TargetLocationID = source.TargetLocationID;
        copy.Qualifier = source.Qualifier;
        return copy;
    }

    internal static FixDictionaryIdentity CopyIdentity(FixDictionaryIdentity source)
    {
        FixDictionaryIdentity copy = new FixDictionaryIdentity();
        copy.Name = source.Name;
        copy.BeginString = source.BeginString;
        copy.ApplicationVersion = source.ApplicationVersion;
        copy.HasExtensionPack = source.HasExtensionPack;
        copy.ExtensionPack = source.ExtensionPack;
        copy.CustomVersion = source.CustomVersion;
        copy.Scenario = source.Scenario;
        copy.Source = source.Source;
        return copy;
    }

    internal static ValidationOptions CopyValidationOptions(ValidationOptions source)
    {
        ValidationOptions copy = new ValidationOptions();
        copy.Layers = source.Layers;
        copy.MaxMessageSize = source.MaxMessageSize;
        copy.MaxFieldSize = source.MaxFieldSize;
        copy.MaxFieldsPerMessage = source.MaxFieldsPerMessage;
        copy.MaxIssues = source.MaxIssues;
        copy.MinimumSeverity = source.MinimumSeverity;
        copy.RejectEmptyValues = source.RejectEmptyValues;
        copy.RejectDuplicateFields = source.RejectDuplicateFields;
        copy.RejectUnknownFields = source.RejectUnknownFields;
        copy.AllowUserDefinedFields = source.AllowUserDefinedFields;
        copy.CheckFieldOrder = source.CheckFieldOrder;
        copy.CheckRequiredFields = source.CheckRequiredFields;
        copy.CheckGroups = source.CheckGroups;
        copy.CheckConditionalRules = source.CheckConditionalRules;
        return copy;
    }
}
