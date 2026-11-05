using System.Collections.Generic;

namespace fix;

/// <summary>Contains the complete independent configuration for one FIX session.</summary>
public class SessionSettings
{
    /// <summary>The session identity.</summary>
    public SessionId Id = new SessionId();
    /// <summary>The local session role.</summary>
    public SessionRole Role;
    /// <summary>The ordered connection or listening endpoints.</summary>
    public List<SessionEndpoint> Endpoints = [];
    /// <summary>The active session schedule.</summary>
    public SessionSchedule Schedule = new SessionSchedule();
    /// <summary>The TLS transport settings.</summary>
    public SessionTlsSettings Tls = new SessionTlsSettings();
    /// <summary>The throttling and queue settings.</summary>
    public SessionThrottleSettings Throttle = new SessionThrottleSettings();
    /// <summary>The message store implementation name.</summary>
    public string StoreType = "Memory";
    /// <summary>The file store directory when a file-backed store is selected.</summary>
    public string StorePath = "";
    /// <summary>The maximum retained store message count, or zero for no count limit.</summary>
    public int StoreMaximumMessages;
    /// <summary>The audit log implementation name.</summary>
    public string LogType = "Null";
    /// <summary>The audit log file path when file logging is selected.</summary>
    public string LogPath = "";
    /// <summary>Whether sensitive fields are redacted from audit output.</summary>
    public bool RedactLogSensitiveFields = true;
    /// <summary>The inbound message validation policy.</summary>
    public ValidationOptions Validation = new ValidationOptions();
    /// <summary>The transport dictionary identity.</summary>
    public FixDictionaryIdentity TransportDictionary = new FixDictionaryIdentity();
    /// <summary>The default application dictionary identity.</summary>
    public FixDictionaryIdentity ApplicationDictionary = new FixDictionaryIdentity();
    /// <summary>The default application version sent at logon.</summary>
    public string DefaultApplicationVersion = "";
    /// <summary>The heartbeat interval in seconds.</summary>
    public int HeartbeatIntervalSeconds = FixDefaults.HeartbeatIntervalSeconds;
    /// <summary>The configured sequence reset boundaries.</summary>
    public SessionResetMode ResetMode;
    /// <summary>The transport connection timeout in milliseconds.</summary>
    public int ConnectTimeoutMilliseconds = 10000;
    /// <summary>The logon timeout in milliseconds.</summary>
    public int LogonTimeoutMilliseconds = 10000;
    /// <summary>The logout timeout in milliseconds.</summary>
    public int LogoutTimeoutMilliseconds = 2000;
    /// <summary>The initial reconnect delay in milliseconds.</summary>
    public int ReconnectDelayMilliseconds = 1000;
    /// <summary>The maximum reconnect delay in milliseconds.</summary>
    public int MaxReconnectDelayMilliseconds = 60000;
    /// <summary>Whether application messages may be queued before logon.</summary>
    public bool AllowSendBeforeLogon;
    /// <summary>Whether inbound comp, sub, and location identifiers must match the configured session.</summary>
    public bool CheckCompIds = true;
    /// <summary>Whether inbound SendingTime must be within the configured latency tolerance.</summary>
    public bool CheckLatency = true;
    /// <summary>The maximum absolute inbound SendingTime latency in seconds.</summary>
    public int MaxLatencySeconds = FixDefaults.MaxLatencySeconds;
    /// <summary>Whether a possible duplicate must include OrigSendingTime.</summary>
    public bool RequireOrigSendingTime = true;
    /// <summary>Whether ResendRequest uses the highest known missing sequence number instead of EndSeqNo zero.</summary>
    public bool ClosedResendInterval;
    /// <summary>Whether repeated high sequence numbers generate another ResendRequest while one is outstanding.</summary>
    public bool SendRedundantResendRequests;
    /// <summary>The maximum number of sequence numbers requested per ResendRequest, or zero for no chunking.</summary>
    public int ResendRequestChunkSize;
    /// <summary>The configured logon username.</summary>
    public string Username = "";
    /// <summary>The configured logon password.</summary>
    public string Password = "";
}
