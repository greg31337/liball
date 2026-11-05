namespace jmap;

/// <summary>
/// Defines the representations available for an Email header field.
/// </summary>
public enum JmapHeaderForm
{
    /// <summary>The original header field value without decoding.</summary>
    Raw = 0,

    /// <summary>The header field value decoded as human-readable text.</summary>
    Text = 1,

    /// <summary>The header field value parsed as a flat address list.</summary>
    Addresses = 2,

    /// <summary>The header field value parsed as a grouped address list.</summary>
    GroupedAddresses = 3,

    /// <summary>The header field value parsed as a list of message identifiers.</summary>
    MessageIds = 4,

    /// <summary>The header field value parsed as a date.</summary>
    Date = 5,

    /// <summary>The header field value parsed as a list of URLs.</summary>
    Urls = 6
}
