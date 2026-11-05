using System;
using System.Text;

namespace token;

/// <summary>
/// Contains a decoded PEM block.
/// </summary>
public class PemBlock
{
    /// <summary>
    /// Contains the PEM label, such as <c>CERTIFICATE</c> or <c>PUBLIC KEY</c>.
    /// </summary>
    public string? Label;

    /// <summary>
    /// Contains the decoded binary data.
    /// </summary>
    public byte[]? Bytes;

    /// <summary>
    /// Initializes an empty PEM block.
    /// </summary>
    public PemBlock()
    {
        Label = null;
        Bytes = null;
    }
}
