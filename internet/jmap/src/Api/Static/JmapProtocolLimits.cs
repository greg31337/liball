namespace jmap;

/// <summary>
/// Defines fixed numeric bounds and interoperability limits from JMAP Core and JMAP Mail.
/// </summary>
public static class JmapProtocolLimits
{
    /// <summary>The minimum number of ASCII characters in a JMAP Id.</summary>
    public const int MinimumIdLength = 1;

    /// <summary>The minimum safe signed JMAP integer.</summary>
    public const long MinimumInt = -9007199254740991L;

    /// <summary>The maximum safe signed JMAP integer and maximum JMAP UnsignedInt.</summary>
    public const long MaximumInt = 9007199254740991L;

    /// <summary>The minimum JMAP UnsignedInt.</summary>
    public const long MinimumUnsignedInt = 0L;

    /// <summary>The maximum JMAP UnsignedInt.</summary>
    public const long MaximumUnsignedInt = 9007199254740991L;

    /// <summary>The maximum number of ASCII characters in a JMAP Id.</summary>
    public const int MaximumIdLength = 255;

    /// <summary>The minimum number of ASCII characters in a JMAP mail keyword.</summary>
    public const int MinimumKeywordLength = 1;

    /// <summary>The maximum number of ASCII characters in a JMAP mail keyword.</summary>
    public const int MaximumKeywordLength = 255;

    /// <summary>The minimum number of characters in a JMAP Mailbox name.</summary>
    public const int MinimumMailboxNameLength = 1;

    /// <summary>The maximum permitted Mailbox sort order.</summary>
    public const int MaximumMailboxSortOrder = 2147483647;

    /// <summary>The maximum number of characters in an Email preview.</summary>
    public const int MaximumEmailPreviewLength = 256;

    /// <summary>The maximum UTF-8 size of a SearchSnippet preview, in octets.</summary>
    public const int MaximumSearchSnippetPreviewSize = 255;

    /// <summary>The suggested minimum advertised maxSizeUpload value.</summary>
    public const int SuggestedMinimumMaxSizeUpload = 50000000;

    /// <summary>The suggested minimum advertised maxConcurrentUpload value.</summary>
    public const int SuggestedMinimumMaxConcurrentUpload = 4;

    /// <summary>The suggested minimum advertised maxSizeRequest value.</summary>
    public const int SuggestedMinimumMaxSizeRequest = 10000000;

    /// <summary>The suggested minimum advertised maxConcurrentRequests value.</summary>
    public const int SuggestedMinimumMaxConcurrentRequests = 4;

    /// <summary>The suggested minimum advertised maxCallsInRequest value.</summary>
    public const int SuggestedMinimumMaxCallsInRequest = 16;

    /// <summary>The suggested minimum advertised maxObjectsInGet value.</summary>
    public const int SuggestedMinimumMaxObjectsInGet = 500;

    /// <summary>The suggested minimum advertised maxObjectsInSet value.</summary>
    public const int SuggestedMinimumMaxObjectsInSet = 500;

    /// <summary>The minimum permitted advertised maxSizeMailboxName value.</summary>
    public const int MinimumMaxSizeMailboxName = 100;

    /// <summary>The recommended minimum changes-history retention, in days.</summary>
    public const int SuggestedMinimumChangesRetentionDays = 30;

    /// <summary>The normal minimum lifetime for an unreferenced blob, in seconds.</summary>
    public const int MinimumUnreferencedBlobLifetimeSeconds = 3600;

    /// <summary>The minimum server-supported push-subscription maximum lifetime, in seconds.</summary>
    public const int MinimumPushSubscriptionMaximumLifetimeSeconds = 172800;

    /// <summary>The suggested server-supported push-subscription maximum lifetime, in seconds.</summary>
    public const int SuggestedPushSubscriptionMaximumLifetimeSeconds = 604800;

    /// <summary>The highest interoperable server minimum EventSource ping interval, in seconds.</summary>
    public const int MaximumEventSourceMinimumPingIntervalSeconds = 30;

    /// <summary>The lowest interoperable server maximum EventSource ping interval, in seconds.</summary>
    public const int MinimumEventSourceMaximumPingIntervalSeconds = 300;
}
