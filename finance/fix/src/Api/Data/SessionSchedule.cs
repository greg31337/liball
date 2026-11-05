using System.Collections.Generic;

namespace fix;

/// <summary>Defines non-stop or time-zone-aware weekly session availability.</summary>
public class SessionSchedule
{
    /// <summary>Whether the session is continuously active.</summary>
    public bool NonStop = true;
    /// <summary>The IANA or system timezone identifier for configured windows.</summary>
    public string TimeZoneId = "UTC";
    /// <summary>The active weekly intervals.</summary>
    public List<SessionTimeWindow> Windows = [];
}
