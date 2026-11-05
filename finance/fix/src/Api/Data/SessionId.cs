namespace fix;

/// <summary>Identifies one independently configured FIX session.</summary>
public class SessionId
{
    /// <summary>The session BeginString.</summary>
    public string BeginString = "";
    /// <summary>The local sender identifier.</summary>
    public string SenderCompID = "";
    /// <summary>The local sender subsystem identifier.</summary>
    public string SenderSubID = "";
    /// <summary>The local sender location identifier.</summary>
    public string SenderLocationID = "";
    /// <summary>The remote target identifier.</summary>
    public string TargetCompID = "";
    /// <summary>The remote target subsystem identifier.</summary>
    public string TargetSubID = "";
    /// <summary>The remote target location identifier.</summary>
    public string TargetLocationID = "";
    /// <summary>An optional discriminator for otherwise identical identifiers.</summary>
    public string Qualifier = "";
}
