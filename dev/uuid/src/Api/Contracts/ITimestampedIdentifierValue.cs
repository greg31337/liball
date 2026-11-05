namespace uuid;

/// <summary>Defines timestamp extraction for identifier formats that carry creation time.</summary>
public interface ITimestampedIdentifierValue
{
    /// <summary>Returns the timestamp carried by the identifier.</summary>
    /// <returns>Timestamp information or a format-specific failure result.</returns>
    IdentifierTimestampResult Timestamp();
}
