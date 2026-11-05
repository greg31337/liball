namespace fix;

internal class FixSessionEngineEntry
{
    internal SessionId Id;
    internal IFixSession Session;

    internal FixSessionEngineEntry(SessionId id, IFixSession session)
    {
        Id = id;
        Session = session;
    }
}
