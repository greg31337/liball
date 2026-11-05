namespace whois;

/// <summary>
/// Identifies the processing stage associated with a WHOIS operation status.
/// </summary>
public enum WhoisOperationStage
{
    /// <summary>
    /// No stage is associated with the status.
    /// </summary>
    None = 0,

    /// <summary>
    /// Initial server selection.
    /// </summary>
    Select = 1,

    /// <summary>
    /// Endpoint host resolution.
    /// </summary>
    Resolve = 2,

    /// <summary>
    /// TCP connection establishment.
    /// </summary>
    Connect = 3,

    /// <summary>
    /// Framed query transmission.
    /// </summary>
    Write = 4,

    /// <summary>
    /// Response reception.
    /// </summary>
    Read = 5,

    /// <summary>
    /// Raw response decoding.
    /// </summary>
    Decode = 6,

    /// <summary>
    /// Generic response parsing.
    /// </summary>
    Parse = 7,

    /// <summary>
    /// Referral selection or traversal.
    /// </summary>
    Refer = 8
}
