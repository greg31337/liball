using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents an openPGP literal data.
/// </summary>
public class EmailOpenPgpLiteralData : IEmailOpenPgpLiteralDataContract
{
    int format;
    byte[] fileName;
    uint timestamp;
    byte[] data;
    /// <summary>
    /// Creates a new openPGP literal data.
    /// </summary>
    /// <param name = "format">The format used by the operation.</param>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <param name = "timestamp">The timestamp used by the operation.</param>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpLiteralData(int format, byte[] fileName, uint timestamp, byte[] data)
    {
        if (fileName == null || data == null)
            throw new ArgumentNullException("fileName");
        this.format = format;
        this.fileName = new EmailOpenPgpArmorResultOperations().Copy(fileName);
        this.timestamp = timestamp;
        this.data = new EmailOpenPgpArmorResultOperations().Copy(data);
    }

    /// <summary>
    /// Gets the format.
    /// </summary>
    /// <returns>The current format.</returns>
    public int GetFormat()
    {
        return format;
    }

    /// <summary>
    /// Gets the file name bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current file name bytes.</returns>
    public byte[] GetFileNameBytes()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(fileName);
    }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    public string GetFileName()
    {
        return Encoding.UTF8.GetString(fileName);
    }

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    public uint GetTimestamp()
    {
        return timestamp;
    }

    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current data.</returns>
    public byte[] GetData()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(data);
    }
}
