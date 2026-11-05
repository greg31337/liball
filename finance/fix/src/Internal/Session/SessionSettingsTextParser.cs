using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace fix;

internal class SessionSettingsTextEntry
{
    internal string Key = "";
    internal string Value = "";
    internal int Line;
}

internal class SessionSettingsTextSection
{
    internal string Name = "";
    internal int Line;
    internal List<SessionSettingsTextEntry> Entries = [];
    internal Dictionary<string, bool> Keys = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
}

internal class SessionSettingsBuildState
{
    internal SessionSettings Settings = new SessionSettings();
    internal bool HasStartTime;
    internal bool HasEndTime;
    internal bool HasStartDay;
    internal bool HasEndDay;
    internal TimeSpan StartTime;
    internal TimeSpan EndTime;
    internal DayOfWeek StartDay;
    internal DayOfWeek EndDay;
    internal List<DayOfWeek> Weekdays = [];
    internal int SectionLine;
}

internal static class SessionSettingsTextParser
{
    internal static SessionSettings[] ParseSessionSettings(Stream source)
    {
        if (source == null || !source.CanRead)
        {
            throw Diagnostic("missing-settings-source", 0, "The session settings source is missing or unreadable.");
        }
        List<SessionSettingsTextSection> sections = ReadSections(source);
        SessionSettingsTextSection? defaults = null;
        List<SessionSettingsTextSection> sessions = [];
        for (int index = 0; index < sections.Count; index++)
        {
            SessionSettingsTextSection section = sections[index];
            if (StringEquals(section.Name, "DEFAULT"))
            {
                if (defaults != null)
                {
                    throw Diagnostic("duplicate-default-section", section.Line, "Only one DEFAULT section is permitted.");
                }
                defaults = section;
            }
            else if (StringEquals(section.Name, "SESSION"))
            {
                sessions.Add(section);
            }
            else
            {
                throw Diagnostic("unknown-settings-section", section.Line, $"Unknown settings section '{section.Name}'.");
            }
        }
        if (sessions.Count == 0)
        {
            throw Diagnostic("missing-session-section", 0, "At least one SESSION section is required.");
        }
        SessionSettings[] results = new SessionSettings[sessions.Count];
        for (int index = 0; index < sessions.Count; index++)
        {
            SessionSettingsBuildState state = new SessionSettingsBuildState();
            state.SectionLine = sessions[index].Line;
            SessionSettingsTextSection effective = MergeSections(defaults, sessions[index]);
            ApplySection(state, effective);
            CompleteSchedule(state);
            CompleteDictionaryIdentities(state.Settings);
            ValidateSettings(state.Settings, state.SectionLine);
            results[index] = state.Settings;
        }
        return results;
    }

    internal static List<SessionSettingsTextSection> ReadSections(Stream source)
    {
        List<SessionSettingsTextSection> sections = [];
        SessionSettingsTextSection? current = null;
        using (StreamReader reader = new StreamReader(source, new UTF8Encoding(false, true), true, 1024, true))
        {
            int lineNumber = 0;
            string? line = reader.ReadLine();
            while (line != null)
            {
                lineNumber++;
                string text = line.Trim();
                if (text.Length == 0 || text[0] == '#' || text[0] == ';')
                {
                    line = reader.ReadLine();
                    continue;
                }
                if (text[0] == '[')
                {
                    if (text.Length < 3 || text[text.Length - 1] != ']')
                    {
                        throw Diagnostic("malformed-section-header", lineNumber, "A section header must end with ']'.");
                    }
                    current = new SessionSettingsTextSection();
                    current.Name = text.Substring(1, text.Length - 2).Trim();
                    current.Line = lineNumber;
                    if (current.Name.Length == 0)
                    {
                        throw Diagnostic("empty-section-name", lineNumber, "A settings section name cannot be empty.");
                    }
                    sections.Add(current);
                    line = reader.ReadLine();
                    continue;
                }
                if (current == null)
                {
                    throw Diagnostic("setting-outside-section", lineNumber, "A setting must belong to a DEFAULT or SESSION section.");
                }
                int equals = text.IndexOf('=');
                if (equals <= 0)
                {
                    throw Diagnostic("malformed-setting", lineNumber, "A setting must use key=value syntax.");
                }
                SessionSettingsTextEntry entry = new SessionSettingsTextEntry();
                entry.Key = text.Substring(0, equals).Trim();
                entry.Value = text.Substring(equals + 1).Trim();
                entry.Line = lineNumber;
                if (entry.Key.Length == 0)
                {
                    throw Diagnostic("empty-setting-key", lineNumber, "A setting key cannot be empty.");
                }
                if (current.Keys.ContainsKey(entry.Key))
                {
                    throw Diagnostic("duplicate-setting", lineNumber, $"Setting '{entry.Key}' is duplicated in its section.");
                }
                current.Keys[entry.Key] = true;
                current.Entries.Add(entry);
                line = reader.ReadLine();
            }
        }
        return sections;
    }

    internal static SessionSettingsTextSection MergeSections(SessionSettingsTextSection? defaults, SessionSettingsTextSection session)
    {
        SessionSettingsTextSection merged = new SessionSettingsTextSection();
        merged.Name = "SESSION";
        merged.Line = session.Line;
        Dictionary<string, int> positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (defaults != null)
        {
            for (int index = 0; index < defaults.Entries.Count; index++)
            {
                positions[defaults.Entries[index].Key] = merged.Entries.Count;
                merged.Entries.Add(defaults.Entries[index]);
            }
        }
        for (int index = 0; index < session.Entries.Count; index++)
        {
            SessionSettingsTextEntry entry = session.Entries[index];
            if (positions.ContainsKey(entry.Key))
            {
                merged.Entries[positions[entry.Key]] = entry;
            }
            else
            {
                positions[entry.Key] = merged.Entries.Count;
                merged.Entries.Add(entry);
            }
        }
        return merged;
    }

    internal static void ApplySection(SessionSettingsBuildState state, SessionSettingsTextSection section)
    {
        for (int index = 0; index < section.Entries.Count; index++)
        {
            ApplyEntry(state, section.Entries[index]);
        }
    }

    internal static void ApplyEntry(SessionSettingsBuildState state, SessionSettingsTextEntry entry)
    {
        SessionSettings settings = state.Settings;
        string key = entry.Key.ToUpperInvariant();
        switch (key)
        {
            case "BEGINSTRING": settings.Id.BeginString = RequiredText(entry); return;
            case "SENDERCOMPID": settings.Id.SenderCompID = RequiredText(entry); return;
            case "SENDERSUBID": settings.Id.SenderSubID = entry.Value; return;
            case "SENDERLOCATIONID": settings.Id.SenderLocationID = entry.Value; return;
            case "TARGETCOMPID": settings.Id.TargetCompID = RequiredText(entry); return;
            case "TARGETSUBID": settings.Id.TargetSubID = entry.Value; return;
            case "TARGETLOCATIONID": settings.Id.TargetLocationID = entry.Value; return;
            case "SESSIONQUALIFIER": settings.Id.Qualifier = entry.Value; return;
            case "QUALIFIER": settings.Id.Qualifier = entry.Value; return;
            case "CONNECTIONTYPE": settings.Role = ParseRole(entry); return;
            case "ROLE": settings.Role = ParseRole(entry); return;
            case "HEARTBTINT": settings.HeartbeatIntervalSeconds = ParsePositiveInteger(entry); return;
            case "HEARTBEATINTERVALSECONDS": settings.HeartbeatIntervalSeconds = ParsePositiveInteger(entry); return;
            case "CONNECTTIMEOUTMILLISECONDS": settings.ConnectTimeoutMilliseconds = ParsePositiveInteger(entry); return;
            case "LOGONTIMEOUTMILLISECONDS": settings.LogonTimeoutMilliseconds = ParsePositiveInteger(entry); return;
            case "LOGOUTTIMEOUTMILLISECONDS": settings.LogoutTimeoutMilliseconds = ParsePositiveInteger(entry); return;
            case "RECONNECTDELAYMILLISECONDS": settings.ReconnectDelayMilliseconds = ParseNonNegativeInteger(entry); return;
            case "MAXRECONNECTDELAYMILLISECONDS": settings.MaxReconnectDelayMilliseconds = ParseNonNegativeInteger(entry); return;
            case "RECONNECTINTERVAL": settings.ReconnectDelayMilliseconds = MultiplyMilliseconds(ParseNonNegativeInteger(entry), entry); return;
            case "ALLOWSENDBEFORELOGON": settings.AllowSendBeforeLogon = ParseBoolean(entry); return;
            case "USERNAME": settings.Username = entry.Value; return;
            case "PASSWORD": settings.Password = entry.Value; return;
            case "RESETMODE": settings.ResetMode = ParseResetMode(entry); return;
            case "RESETONLOGON": settings.ResetMode = ApplyResetFlag(settings.ResetMode, SessionResetMode.OnLogon, ParseBoolean(entry)); return;
            case "RESETONLOGOUT": settings.ResetMode = ApplyResetFlag(settings.ResetMode, SessionResetMode.OnLogout, ParseBoolean(entry)); return;
            case "RESETONDISCONNECT": settings.ResetMode = ApplyResetFlag(settings.ResetMode, SessionResetMode.OnDisconnect, ParseBoolean(entry)); return;
            case "RESETONSESSIONBOUNDARY": settings.ResetMode = ApplyResetFlag(settings.ResetMode, SessionResetMode.OnSessionBoundary, ParseBoolean(entry)); return;
            case "NONSTOPSESSION": settings.Schedule.NonStop = ParseBoolean(entry); return;
            case "TIMEZONE": settings.Schedule.TimeZoneId = RequiredText(entry); return;
            case "TIMEZONEID": settings.Schedule.TimeZoneId = RequiredText(entry); return;
            case "STARTTIME": state.StartTime = ParseTime(entry); state.HasStartTime = true; settings.Schedule.NonStop = false; return;
            case "ENDTIME": state.EndTime = ParseTime(entry); state.HasEndTime = true; settings.Schedule.NonStop = false; return;
            case "STARTDAY": state.StartDay = ParseDay(entry); state.HasStartDay = true; settings.Schedule.NonStop = false; return;
            case "ENDDAY": state.EndDay = ParseDay(entry); state.HasEndDay = true; settings.Schedule.NonStop = false; return;
            case "WEEKDAYS": state.Weekdays = ParseDays(entry); settings.Schedule.NonStop = false; return;
            case "TLSENABLED": settings.Tls.Enabled = ParseBoolean(entry); ApplyTlsToEndpoints(settings); return;
            case "SOCKETUSESSL": settings.Tls.Enabled = ParseBoolean(entry); ApplyTlsToEndpoints(settings); return;
            case "TLSTARGETHOST": settings.Tls.TargetHost = entry.Value; return;
            case "TLSCERTIFICATEPATH": settings.Tls.CertificatePath = entry.Value; return;
            case "TLSCERTIFICATEPASSWORD": settings.Tls.CertificatePassword = entry.Value; return;
            case "TLSREQUIREPEERCERTIFICATE": settings.Tls.RequirePeerCertificate = ParseBoolean(entry); return;
            case "TLSCHECKCERTIFICATEREVOCATION": settings.Tls.CheckCertificateRevocation = ParseBoolean(entry); return;
            case "TLSALPN": settings.Tls.Alpn = RequiredText(entry); return;
            case "THROTTLEMAXMESSAGES": settings.Throttle.MaxMessages = ParsePositiveInteger(entry); return;
            case "THROTTLEINTERVALMILLISECONDS": settings.Throttle.IntervalMilliseconds = ParsePositiveInteger(entry); return;
            case "THROTTLEBURSTMESSAGES": settings.Throttle.BurstMessages = ParseNonNegativeInteger(entry); return;
            case "QUEUECAPACITY": settings.Throttle.QueueCapacity = ParseNonNegativeInteger(entry); return;
            case "STORETYPE": settings.StoreType = RequiredText(entry); return;
            case "MESSAGESTORETYPE": settings.StoreType = RequiredText(entry); return;
            case "STOREPATH": settings.StorePath = entry.Value; return;
            case "FILESTOREPATH": settings.StorePath = entry.Value; settings.StoreType = "File"; return;
            case "STOREMAXIMUMMESSAGES": settings.StoreMaximumMessages = ParseNonNegativeInteger(entry); return;
            case "LOGTYPE": settings.LogType = RequiredText(entry); return;
            case "LOGPATH": settings.LogPath = entry.Value; return;
            case "FILELOGPATH": settings.LogPath = entry.Value; settings.LogType = "File"; return;
            case "REDACTLOGSENSITIVEFIELDS": settings.RedactLogSensitiveFields = ParseBoolean(entry); return;
            case "TRANSPORTDATADICTIONARY": SetTransportDictionary(settings, entry.Value); return;
            case "TRANSPORTDICTIONARY": SetTransportDictionary(settings, entry.Value); return;
            case "APPDATADICTIONARY": SetApplicationDictionary(settings, entry.Value); return;
            case "APPLICATIONDICTIONARY": SetApplicationDictionary(settings, entry.Value); return;
            case "DEFAULTAPPLVERID": settings.DefaultApplicationVersion = RequiredText(entry); settings.ApplicationDictionary.ApplicationVersion = entry.Value; return;
            case "DEFAULTAPPLICATIONVERSION": settings.DefaultApplicationVersion = RequiredText(entry); settings.ApplicationDictionary.ApplicationVersion = entry.Value; return;
            case "TRANSPORTDICTIONARYNAME": settings.TransportDictionary.Name = entry.Value; return;
            case "TRANSPORTDICTIONARYSOURCE": settings.TransportDictionary.Source = entry.Value; return;
            case "APPLICATIONDICTIONARYNAME": settings.ApplicationDictionary.Name = entry.Value; return;
            case "APPLICATIONDICTIONARYSOURCE": settings.ApplicationDictionary.Source = entry.Value; return;
            case "APPLICATIONDICTIONARYSCENARIO": settings.ApplicationDictionary.Scenario = entry.Value; return;
            case "APPLICATIONDICTIONARYCUSTOMVERSION": settings.ApplicationDictionary.CustomVersion = entry.Value; return;
            case "APPLICATIONDICTIONARYEXTENSIONPACK": settings.ApplicationDictionary.ExtensionPack = ParseNonNegativeInteger(entry); settings.ApplicationDictionary.HasExtensionPack = true; return;
            case "VALIDATIONLAYERS": settings.Validation.Layers = ParseValidationLayers(entry); return;
            case "VALIDATIONMINIMUMSEVERITY": settings.Validation.MinimumSeverity = ParseValidationSeverity(entry); return;
            case "VALIDATIONMAXMESSAGESIZE": settings.Validation.MaxMessageSize = ParsePositiveInteger(entry); return;
            case "VALIDATIONMAXFIELDSIZE": settings.Validation.MaxFieldSize = ParsePositiveInteger(entry); return;
            case "VALIDATIONMAXFIELDSPERMESSAGE": settings.Validation.MaxFieldsPerMessage = ParsePositiveInteger(entry); return;
            case "VALIDATIONMAXISSUES": settings.Validation.MaxIssues = ParsePositiveInteger(entry); return;
            case "REJECTEMPTYVALUES": settings.Validation.RejectEmptyValues = ParseBoolean(entry); return;
            case "REJECTDUPLICATEFIELDS": settings.Validation.RejectDuplicateFields = ParseBoolean(entry); return;
            case "REJECTUNKNOWNFIELDS": settings.Validation.RejectUnknownFields = ParseBoolean(entry); return;
            case "ALLOWUSERDEFINEDFIELDS": settings.Validation.AllowUserDefinedFields = ParseBoolean(entry); return;
            case "CHECKFIELDORDER": settings.Validation.CheckFieldOrder = ParseBoolean(entry); return;
            case "CHECKREQUIREDFIELDS": settings.Validation.CheckRequiredFields = ParseBoolean(entry); return;
            case "CHECKGROUPS": settings.Validation.CheckGroups = ParseBoolean(entry); return;
            case "CHECKCONDITIONALRULES": settings.Validation.CheckConditionalRules = ParseBoolean(entry); return;
            case "VALIDATEFIELDSHAVEVALUES": settings.Validation.RejectEmptyValues = ParseBoolean(entry); return;
            case "CHECKCOMPID": settings.CheckCompIds = ParseBoolean(entry); return;
            case "CHECKCOMPIDS": settings.CheckCompIds = ParseBoolean(entry); return;
            case "CHECKLATENCY": settings.CheckLatency = ParseBoolean(entry); return;
            case "MAXLATENCY": settings.MaxLatencySeconds = ParsePositiveInteger(entry); return;
            case "MAXLATENCYSECONDS": settings.MaxLatencySeconds = ParsePositiveInteger(entry); return;
            case "REQUIRESORIGSENDINGTIME": settings.RequireOrigSendingTime = ParseBoolean(entry); return;
            case "CLOSEDRESENDINTERVAL": settings.ClosedResendInterval = ParseBoolean(entry); return;
            case "SENDREDUNDANTRESENDREQUESTS": settings.SendRedundantResendRequests = ParseBoolean(entry); return;
            case "RESENDREQUESTCHUNKSIZE": settings.ResendRequestChunkSize = ParseNonNegativeInteger(entry); return;
        }
        if (IsNumberedKey(key, "ENDPOINT"))
        {
            AddEndpoint(settings, entry);
            return;
        }
        if (IsNumberedKey(key, "SCHEDULEWINDOW"))
        {
            AddScheduleWindow(settings, entry);
            return;
        }
        if (IsNumberedKey(key, "SOCKETCONNECTHOST"))
        {
            SetEndpointHost(settings, EndpointIndex(key, "SOCKETCONNECTHOST"), entry);
            return;
        }
        if (IsNumberedKey(key, "SOCKETCONNECTPORT"))
        {
            SetEndpointPort(settings, EndpointIndex(key, "SOCKETCONNECTPORT"), entry);
            return;
        }
        if (key == "SOCKETACCEPTADDRESS" || key == "SOCKETACCEPTHOST")
        {
            SetEndpointHost(settings, 0, entry);
            return;
        }
        if (key == "SOCKETACCEPTPORT")
        {
            SetEndpointPort(settings, 0, entry);
            return;
        }
        throw Diagnostic("unknown-setting", entry.Line, $"Unknown setting '{entry.Key}'.");
    }

    internal static bool ContainsEndpointEntry(SessionSettingsTextSection section)
    {
        for (int index = 0; index < section.Entries.Count; index++)
        {
            string key = section.Entries[index].Key.ToUpperInvariant();
            if (IsNumberedKey(key, "ENDPOINT") || IsNumberedKey(key, "SOCKETCONNECTHOST") || IsNumberedKey(key, "SOCKETCONNECTPORT") || key == "SOCKETACCEPTADDRESS" || key == "SOCKETACCEPTHOST" || key == "SOCKETACCEPTPORT") return true;
        }
        return false;
    }

    internal static bool ContainsWindowEntry(SessionSettingsTextSection section)
    {
        for (int index = 0; index < section.Entries.Count; index++)
        {
            string key = section.Entries[index].Key.ToUpperInvariant();
            if (IsNumberedKey(key, "SCHEDULEWINDOW") || key == "STARTTIME" || key == "ENDTIME" || key == "STARTDAY" || key == "ENDDAY" || key == "WEEKDAYS") return true;
        }
        return false;
    }

    internal static void AddEndpoint(SessionSettings settings, SessionSettingsTextEntry entry)
    {
        List<string> parts = SplitValues(entry.Value, ',');
        if (parts.Count < 2 || parts.Count > 4)
        {
            throw Diagnostic("invalid-endpoint", entry.Line, "Endpoint must be host,port[,priority[,tls]].");
        }
        SessionEndpoint endpoint = new SessionEndpoint();
        endpoint.Host = parts[0];
        endpoint.Port = ParseIntegerValue(parts[1], entry, false);
        endpoint.Priority = parts.Count >= 3 ? ParseIntegerValue(parts[2], entry, false) : settings.Endpoints.Count;
        endpoint.UseTls = parts.Count >= 4 ? ParseBooleanValue(parts[3], entry) : settings.Tls.Enabled;
        settings.Endpoints.Add(endpoint);
    }

    internal static void SetEndpointHost(SessionSettings settings, int priority, SessionSettingsTextEntry entry)
    {
        SessionEndpoint endpoint = FindEndpoint(settings, priority);
        endpoint.Host = RequiredText(entry);
        endpoint.Priority = priority;
        endpoint.UseTls = settings.Tls.Enabled;
    }

    internal static void SetEndpointPort(SessionSettings settings, int priority, SessionSettingsTextEntry entry)
    {
        SessionEndpoint endpoint = FindEndpoint(settings, priority);
        endpoint.Port = ParsePositiveInteger(entry);
        endpoint.Priority = priority;
        endpoint.UseTls = settings.Tls.Enabled;
    }

    internal static SessionEndpoint FindEndpoint(SessionSettings settings, int priority)
    {
        for (int index = 0; index < settings.Endpoints.Count; index++)
        {
            if (settings.Endpoints[index].Priority == priority) return settings.Endpoints[index];
        }
        SessionEndpoint endpoint = new SessionEndpoint();
        endpoint.Priority = priority;
        settings.Endpoints.Add(endpoint);
        return endpoint;
    }

    internal static void ApplyTlsToEndpoints(SessionSettings settings)
    {
        for (int index = 0; index < settings.Endpoints.Count; index++)
        {
            settings.Endpoints[index].UseTls = settings.Tls.Enabled;
        }
    }

    internal static void AddScheduleWindow(SessionSettings settings, SessionSettingsTextEntry entry)
    {
        List<string> parts = SplitValues(entry.Value, ',');
        if (parts.Count < 4 || parts.Count > 5)
        {
            throw Diagnostic("invalid-schedule-window", entry.Line, "ScheduleWindow must be start-day,start-time,end-day,end-time[,enabled].");
        }
        SessionTimeWindow window = new SessionTimeWindow();
        window.StartDay = ParseDayValue(parts[0], entry);
        window.StartTime = ParseTimeValue(parts[1], entry);
        window.EndDay = ParseDayValue(parts[2], entry);
        window.EndTime = ParseTimeValue(parts[3], entry);
        window.Enabled = parts.Count == 5 ? ParseBooleanValue(parts[4], entry) : true;
        settings.Schedule.Windows.Add(window);
        settings.Schedule.NonStop = false;
    }

    internal static void CompleteSchedule(SessionSettingsBuildState state)
    {
        if (state.Settings.Schedule.NonStop || state.Settings.Schedule.Windows.Count != 0) return;
        if (!state.HasStartTime || !state.HasEndTime)
        {
            throw Diagnostic("incomplete-session-schedule", state.SectionLine, "A non-stop-disabled schedule requires start and end times or explicit windows.");
        }
        if (state.Weekdays.Count != 0)
        {
            for (int index = 0; index < state.Weekdays.Count; index++)
            {
                AddCompletedWindow(state.Settings, state.Weekdays[index], state.StartTime, state.Weekdays[index], state.EndTime);
            }
            return;
        }
        if (state.HasStartDay || state.HasEndDay)
        {
            if (!state.HasStartDay || !state.HasEndDay)
            {
                throw Diagnostic("incomplete-session-days", state.SectionLine, "Both StartDay and EndDay are required when either is configured.");
            }
            AddCompletedWindow(state.Settings, state.StartDay, state.StartTime, state.EndDay, state.EndTime);
            return;
        }
        for (int day = 0; day < 7; day++)
        {
            AddCompletedWindow(state.Settings, (DayOfWeek)day, state.StartTime, (DayOfWeek)day, state.EndTime);
        }
    }

    internal static void AddCompletedWindow(SessionSettings settings, DayOfWeek startDay, TimeSpan startTime, DayOfWeek endDay, TimeSpan endTime)
    {
        SessionTimeWindow window = new SessionTimeWindow();
        window.StartDay = startDay;
        window.StartTime = startTime;
        window.EndDay = endDay;
        window.EndTime = endTime;
        settings.Schedule.Windows.Add(window);
    }

    internal static void CompleteDictionaryIdentities(SessionSettings settings)
    {
        settings.TransportDictionary.BeginString = settings.Id.BeginString;
        if (settings.TransportDictionary.Name.Length == 0) settings.TransportDictionary.Name = settings.Id.BeginString;
        if (settings.DefaultApplicationVersion.Length == 0 && settings.Id.BeginString != FixBeginStrings.Fixt11)
        {
            settings.DefaultApplicationVersion = ResolveClassicApplicationVersion(settings.Id.BeginString);
        }
        if (settings.ApplicationDictionary.ApplicationVersion.Length == 0)
        {
            settings.ApplicationDictionary.ApplicationVersion = settings.DefaultApplicationVersion;
        }
        if (settings.ApplicationDictionary.Name.Length == 0)
        {
            settings.ApplicationDictionary.Name = settings.DefaultApplicationVersion;
        }
    }

    internal static void ValidateSettings(SessionSettings settings, int line)
    {
        if (settings.Id.BeginString.Length == 0 || settings.Id.SenderCompID.Length == 0 || settings.Id.TargetCompID.Length == 0)
        {
            throw Diagnostic("incomplete-session-identity", line, "BeginString, SenderCompID, and TargetCompID are required.");
        }
        if (settings.Endpoints.Count == 0)
        {
            throw Diagnostic("missing-session-endpoint", line, "At least one session endpoint is required.");
        }
        for (int index = 0; index < settings.Endpoints.Count; index++)
        {
            if (settings.Endpoints[index].Host.Length == 0 || settings.Endpoints[index].Port <= 0 || settings.Endpoints[index].Port > 65535)
            {
                throw Diagnostic("invalid-session-endpoint", line, "Every session endpoint requires a host and port from 1 through 65535.");
            }
        }
        if (settings.MaxReconnectDelayMilliseconds < settings.ReconnectDelayMilliseconds)
        {
            throw Diagnostic("invalid-reconnect-range", line, "The maximum reconnect delay is below the initial reconnect delay.");
        }
        if (settings.Id.BeginString == FixBeginStrings.Fixt11 && settings.DefaultApplicationVersion.Length == 0)
        {
            throw Diagnostic("missing-fixt-application-version", line, "FIXT sessions require DefaultApplVerID.");
        }
        if (StringEquals(settings.StoreType, "File") && settings.StorePath.Length == 0)
        {
            throw Diagnostic("missing-file-store-path", line, "File message stores require a path.");
        }
        if (StringEquals(settings.LogType, "File") && settings.LogPath.Length == 0)
        {
            throw Diagnostic("missing-file-log-path", line, "File audit logs require a path.");
        }
    }

    internal static void SetTransportDictionary(SessionSettings settings, string value)
    {
        settings.TransportDictionary.Name = value;
        settings.TransportDictionary.Source = value;
    }

    internal static void SetApplicationDictionary(SessionSettings settings, string value)
    {
        settings.ApplicationDictionary.Name = value;
        settings.ApplicationDictionary.Source = value;
    }

    internal static SessionRole ParseRole(SessionSettingsTextEntry entry)
    {
        if (StringEquals(entry.Value, "initiator")) return SessionRole.Initiator;
        if (StringEquals(entry.Value, "acceptor")) return SessionRole.Acceptor;
        throw Diagnostic("invalid-session-role", entry.Line, "ConnectionType must be initiator or acceptor.");
    }

    internal static SessionResetMode ParseResetMode(SessionSettingsTextEntry entry)
    {
        List<string> values = SplitValues(entry.Value.Replace('|', ','), ',');
        SessionResetMode mode = SessionResetMode.None;
        for (int index = 0; index < values.Count; index++)
        {
            string value = values[index];
            if (StringEquals(value, "none")) continue;
            if (StringEquals(value, "onlogon")) mode = mode | SessionResetMode.OnLogon;
            else if (StringEquals(value, "onlogout")) mode = mode | SessionResetMode.OnLogout;
            else if (StringEquals(value, "ondisconnect")) mode = mode | SessionResetMode.OnDisconnect;
            else if (StringEquals(value, "onsessionboundary")) mode = mode | SessionResetMode.OnSessionBoundary;
            else throw Diagnostic("invalid-reset-mode", entry.Line, $"Unknown reset mode '{value}'.");
        }
        return mode;
    }

    internal static SessionResetMode ApplyResetFlag(SessionResetMode current, SessionResetMode flag, bool enabled)
    {
        if (enabled) return current | flag;
        return current & ~flag;
    }

    internal static ValidationLayer ParseValidationLayers(SessionSettingsTextEntry entry)
    {
        if (StringEquals(entry.Value, "all")) return ValidationLayer.All;
        if (StringEquals(entry.Value, "none")) return ValidationLayer.None;
        List<string> values = SplitValues(entry.Value.Replace('|', ','), ',');
        ValidationLayer layers = ValidationLayer.None;
        for (int index = 0; index < values.Count; index++)
        {
            string value = values[index];
            if (StringEquals(value, "framing")) layers = layers | ValidationLayer.Framing;
            else if (StringEquals(value, "envelope")) layers = layers | ValidationLayer.Envelope;
            else if (StringEquals(value, "sessionheader")) layers = layers | ValidationLayer.SessionHeader;
            else if (StringEquals(value, "dictionarygrammar")) layers = layers | ValidationLayer.DictionaryGrammar;
            else if (StringEquals(value, "datatype")) layers = layers | ValidationLayer.Datatype;
            else if (StringEquals(value, "codeset")) layers = layers | ValidationLayer.CodeSet;
            else if (StringEquals(value, "group")) layers = layers | ValidationLayer.Group;
            else if (StringEquals(value, "conditionalrule")) layers = layers | ValidationLayer.ConditionalRule;
            else if (StringEquals(value, "applicationsemantic")) layers = layers | ValidationLayer.ApplicationSemantic;
            else throw Diagnostic("invalid-validation-layer", entry.Line, $"Unknown validation layer '{value}'.");
        }
        return layers;
    }

    internal static ValidationSeverity ParseValidationSeverity(SessionSettingsTextEntry entry)
    {
        if (StringEquals(entry.Value, "information")) return ValidationSeverity.Information;
        if (StringEquals(entry.Value, "warning")) return ValidationSeverity.Warning;
        if (StringEquals(entry.Value, "error")) return ValidationSeverity.Error;
        if (StringEquals(entry.Value, "fatal")) return ValidationSeverity.Fatal;
        throw Diagnostic("invalid-validation-severity", entry.Line, "ValidationMinimumSeverity is invalid.");
    }

    internal static DayOfWeek ParseDay(SessionSettingsTextEntry entry)
    {
        return ParseDayValue(entry.Value, entry);
    }

    internal static DayOfWeek ParseDayValue(string value, SessionSettingsTextEntry entry)
    {
        if (StringEquals(value, "sunday") || StringEquals(value, "sun")) return DayOfWeek.Sunday;
        if (StringEquals(value, "monday") || StringEquals(value, "mon")) return DayOfWeek.Monday;
        if (StringEquals(value, "tuesday") || StringEquals(value, "tue")) return DayOfWeek.Tuesday;
        if (StringEquals(value, "wednesday") || StringEquals(value, "wed")) return DayOfWeek.Wednesday;
        if (StringEquals(value, "thursday") || StringEquals(value, "thu")) return DayOfWeek.Thursday;
        if (StringEquals(value, "friday") || StringEquals(value, "fri")) return DayOfWeek.Friday;
        if (StringEquals(value, "saturday") || StringEquals(value, "sat")) return DayOfWeek.Saturday;
        throw Diagnostic("invalid-schedule-day", entry.Line, $"Unknown schedule day '{value}'.");
    }

    internal static List<DayOfWeek> ParseDays(SessionSettingsTextEntry entry)
    {
        List<string> values = SplitValues(entry.Value, ',');
        List<DayOfWeek> days = [];
        for (int index = 0; index < values.Count; index++)
        {
            days.Add(ParseDayValue(values[index], entry));
        }
        if (days.Count == 0) throw Diagnostic("empty-schedule-days", entry.Line, "Weekdays cannot be empty.");
        return days;
    }

    internal static TimeSpan ParseTime(SessionSettingsTextEntry entry)
    {
        return ParseTimeValue(entry.Value, entry);
    }

    internal static TimeSpan ParseTimeValue(string value, SessionSettingsTextEntry entry)
    {
        List<string> parts = SplitValues(value, ':');
        if (parts.Count < 2 || parts.Count > 3)
        {
            throw Diagnostic("invalid-schedule-time", entry.Line, "Schedule times must use HH:mm or HH:mm:ss.");
        }
        int hour = ParseIntegerValue(parts[0], entry, false);
        int minute = ParseIntegerValue(parts[1], entry, false);
        int second = parts.Count == 3 ? ParseIntegerValue(parts[2], entry, false) : 0;
        if (hour > 23 || minute > 59 || second > 59)
        {
            throw Diagnostic("invalid-schedule-time", entry.Line, "Schedule time components are outside their valid ranges.");
        }
        return new TimeSpan(hour, minute, second);
    }

    internal static bool ParseBoolean(SessionSettingsTextEntry entry)
    {
        return ParseBooleanValue(entry.Value, entry);
    }

    internal static bool ParseBooleanValue(string value, SessionSettingsTextEntry entry)
    {
        if (StringEquals(value, "true") || StringEquals(value, "yes") || StringEquals(value, "y") || value == "1") return true;
        if (StringEquals(value, "false") || StringEquals(value, "no") || StringEquals(value, "n") || value == "0") return false;
        throw Diagnostic("invalid-boolean", entry.Line, $"Setting '{entry.Key}' requires a boolean value.");
    }

    internal static int ParsePositiveInteger(SessionSettingsTextEntry entry)
    {
        int value = ParseIntegerValue(entry.Value, entry, false);
        if (value <= 0) throw Diagnostic("invalid-positive-integer", entry.Line, $"Setting '{entry.Key}' must be positive.");
        return value;
    }

    internal static int ParseNonNegativeInteger(SessionSettingsTextEntry entry)
    {
        return ParseIntegerValue(entry.Value, entry, false);
    }

    internal static int ParseIntegerValue(string text, SessionSettingsTextEntry entry, bool allowNegative)
    {
        if (text.Length == 0) throw Diagnostic("invalid-integer", entry.Line, $"Setting '{entry.Key}' requires an integer.");
        int index = 0;
        bool negative = false;
        if (text[0] == '-')
        {
            if (!allowNegative) throw Diagnostic("invalid-integer", entry.Line, $"Setting '{entry.Key}' cannot be negative.");
            negative = true;
            index = 1;
        }
        if (index == text.Length) throw Diagnostic("invalid-integer", entry.Line, $"Setting '{entry.Key}' requires digits.");
        long value = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9') throw Diagnostic("invalid-integer", entry.Line, $"Setting '{entry.Key}' requires invariant decimal digits.");
            value = value * 10L + character - '0';
            if (value > 2147483648L) throw Diagnostic("integer-overflow", entry.Line, $"Setting '{entry.Key}' exceeds Int32 range.");
            index++;
        }
        if (negative) value = -value;
        if (value > int.MaxValue || value < int.MinValue) throw Diagnostic("integer-overflow", entry.Line, $"Setting '{entry.Key}' exceeds Int32 range.");
        return (int)value;
    }

    internal static int MultiplyMilliseconds(int seconds, SessionSettingsTextEntry entry)
    {
        long value = (long)seconds * 1000L;
        if (value > int.MaxValue) throw Diagnostic("integer-overflow", entry.Line, "ReconnectInterval exceeds the millisecond range.");
        return (int)value;
    }

    internal static string RequiredText(SessionSettingsTextEntry entry)
    {
        if (entry.Value.Length == 0) throw Diagnostic("missing-setting-value", entry.Line, $"Setting '{entry.Key}' cannot be empty.");
        return entry.Value;
    }

    internal static bool IsNumberedKey(string key, string prefix)
    {
        if (!key.StartsWith(prefix, StringComparison.Ordinal)) return false;
        if (key.Length == prefix.Length) return true;
        for (int index = prefix.Length; index < key.Length; index++)
        {
            if (key[index] < '0' || key[index] > '9') return false;
        }
        return true;
    }

    internal static int EndpointIndex(string key, string prefix)
    {
        if (key.Length == prefix.Length) return 0;
        SessionSettingsTextEntry entry = new SessionSettingsTextEntry();
        entry.Key = key;
        entry.Value = key.Substring(prefix.Length);
        return ParseIntegerValue(entry.Value, entry, false);
    }

    internal static List<string> SplitValues(string value, char separator)
    {
        List<string> values = [];
        int start = 0;
        for (int index = 0; index <= value.Length; index++)
        {
            if (index == value.Length || value[index] == separator)
            {
                values.Add(value.Substring(start, index - start).Trim());
                start = index + 1;
            }
        }
        return values;
    }

    internal static string ResolveClassicApplicationVersion(string beginString)
    {
        if (beginString == FixBeginStrings.Fix40) return FixApplicationVersions.Fix40;
        if (beginString == FixBeginStrings.Fix41) return FixApplicationVersions.Fix41;
        if (beginString == FixBeginStrings.Fix42) return FixApplicationVersions.Fix42;
        if (beginString == FixBeginStrings.Fix43) return FixApplicationVersions.Fix43;
        if (beginString == FixBeginStrings.Fix44) return FixApplicationVersions.Fix44;
        return "";
    }

    internal static bool StringEquals(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal static FixSessionException Diagnostic(string code, int line, string message)
    {
        return new FixSessionException($"settings:{code}:line:{line}: {message}");
    }
}
