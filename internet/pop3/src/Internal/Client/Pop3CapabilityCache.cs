namespace pop3;

internal class Pop3CapabilityCache
{
    bool CapaAvailable;
    bool UserAdvertised;
    bool TopAdvertised;
    bool UidlAdvertised;
    bool ProbeUserWhenUnavailable;
    bool ProbeTopWhenUnavailable;
    bool ProbeUidlWhenUnavailable;

    internal void Record(
        Pop3CapabilitySet capabilities,
        bool capaAvailable,
        Pop3ClientOptions options)
    {
        if (capabilities == null)
        {
            throw new System.ArgumentNullException("capabilities");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        CapaAvailable = capaAvailable;
        UserAdvertised = capaAvailable && capabilities.SupportsUser;
        TopAdvertised = capaAvailable && capabilities.SupportsTop;
        UidlAdvertised = capaAvailable && capabilities.SupportsUidl;
        ProbeUserWhenUnavailable = options.ProbeUserWhenCapaUnavailable;
        ProbeTopWhenUnavailable = options.ProbeTopWhenCapaUnavailable;
        ProbeUidlWhenUnavailable = options.ProbeUidlWhenCapaUnavailable;
    }

    internal bool IsAvailable()
    {
        return CapaAvailable;
    }

    internal bool CanAttemptUser()
    {
        return UserAdvertised || !CapaAvailable && ProbeUserWhenUnavailable;
    }

    internal bool CanAttemptTop()
    {
        return TopAdvertised || !CapaAvailable && ProbeTopWhenUnavailable;
    }

    internal bool CanAttemptUidl()
    {
        return UidlAdvertised || !CapaAvailable && ProbeUidlWhenUnavailable;
    }

    internal void Invalidate()
    {
        CapaAvailable = false;
        UserAdvertised = false;
        TopAdvertised = false;
        UidlAdvertised = false;
        ProbeUserWhenUnavailable = false;
        ProbeTopWhenUnavailable = false;
        ProbeUidlWhenUnavailable = false;
    }
}
